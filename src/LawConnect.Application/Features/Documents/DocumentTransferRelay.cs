#pragma warning disable CS1591
using System.Collections.Concurrent;

namespace LawConnect.Application.Features.Documents;

/// <summary>
/// Joins one customer upload stream to one lawyer download stream.
/// Only an 80 KB copy buffer is held. The file is not written to disk or cached.
/// </summary>
public interface IDocumentTransferRelay
{
    Task DownloadAsync(Guid sessionId, Stream download, long expectedBytes, TimeSpan wait, CancellationToken cancellationToken);

    Task UploadAsync(Guid sessionId, Stream upload, long expectedBytes, TimeSpan wait, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IDocumentTransferRelay" />
public sealed class DocumentTransferRelay : IDocumentTransferRelay
{
    private readonly ConcurrentDictionary<Guid, RelaySlot> _slots = new();

    /// <inheritdoc />
    public async Task DownloadAsync(
        Guid sessionId,
        Stream download,
        long expectedBytes,
        TimeSpan wait,
        CancellationToken cancellationToken)
    {
        var slot = _slots.GetOrAdd(sessionId, _ => new RelaySlot());
        if (Interlocked.Exchange(ref slot.DownloadJoined, 1) == 1)
        {
            throw new InvalidOperationException("This transfer already has a downloader.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(wait);
        Stream upload;
        try
        {
            upload = await slot.Upload.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            _slots.TryRemove(sessionId, out _);
            slot.Finished.TrySetCanceled(cancellationToken);
            throw;
        }

        try
        {
            await CopyExactAsync(upload, download, expectedBytes, cancellationToken);
            slot.Finished.TrySetResult();
        }
        catch (Exception ex)
        {
            slot.Finished.TrySetException(ex);
            throw;
        }
        finally
        {
            _slots.TryRemove(sessionId, out _);
        }
    }

    /// <inheritdoc />
    public async Task UploadAsync(
        Guid sessionId,
        Stream upload,
        long expectedBytes,
        TimeSpan wait,
        CancellationToken cancellationToken)
    {
        _ = expectedBytes;
        var slot = _slots.GetOrAdd(sessionId, _ => new RelaySlot());
        if (Interlocked.Exchange(ref slot.UploadOffered, 1) == 1)
        {
            throw new InvalidOperationException("This transfer already has an uploader.");
        }

        slot.Upload.TrySetResult(upload);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(wait);
        try
        {
            await slot.Finished.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            _slots.TryRemove(sessionId, out _);
            slot.Upload.TrySetCanceled(cancellationToken);
            throw;
        }
    }

    private static async Task CopyExactAsync(Stream source, Stream destination, long expectedBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[80 * 1024];
        long total = 0;
        while (total < expectedBytes)
        {
            var want = (int)Math.Min(buffer.Length, expectedBytes - total);
            var read = await source.ReadAsync(buffer.AsMemory(0, want), cancellationToken);
            if (read == 0)
            {
                throw new IOException("The upload ended before the declared file size.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
        }

        var extra = new byte[1];
        if (await source.ReadAsync(extra.AsMemory(0, 1), cancellationToken) > 0)
        {
            throw new IOException("The upload was larger than the declared file size.");
        }

        await destination.FlushAsync(cancellationToken);
    }

    private sealed class RelaySlot
    {
        public int DownloadJoined;
        public int UploadOffered;
        public TaskCompletionSource<Stream> Upload { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

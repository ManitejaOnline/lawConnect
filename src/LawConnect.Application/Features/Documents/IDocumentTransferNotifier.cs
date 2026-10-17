#pragma warning disable CS1591
namespace LawConnect.Application.Features.Documents;

/// <summary>Tells a connected app that a transfer needs attention. No file bytes are included.</summary>
public interface IDocumentTransferNotifier
{
    Task RequestedAsync(Guid customerUserId, DocumentTransferNotice notice, CancellationToken cancellationToken = default);

    Task ReadyAsync(Guid lawyerUserId, DocumentTransferNotice notice, CancellationToken cancellationToken = default);
}

/// <param name="SessionId">Transfer session id.</param>
/// <param name="DocumentId">Customer document id.</param>
/// <param name="FileName">Display name only.</param>
public sealed record DocumentTransferNotice(Guid SessionId, Guid DocumentId, string FileName);

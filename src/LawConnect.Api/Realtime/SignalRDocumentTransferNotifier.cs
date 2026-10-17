#pragma warning disable CS1591
using LawConnect.Api.Hubs;
using LawConnect.Application.Features.Documents;
using Microsoft.AspNetCore.SignalR;

namespace LawConnect.Api.Realtime;

/// <summary>Pushes transfer signals to the signed-in user's open app. Payloads contain ids and a file name only.</summary>
public sealed class SignalRDocumentTransferNotifier : IDocumentTransferNotifier
{
    private readonly IHubContext<DocumentHub> _hub;

    public SignalRDocumentTransferNotifier(IHubContext<DocumentHub> hub) => _hub = hub;

    /// <inheritdoc />
    public Task RequestedAsync(Guid customerUserId, DocumentTransferNotice notice, CancellationToken cancellationToken = default) =>
        _hub.Clients.User(customerUserId.ToString()).SendAsync("TransferRequested", notice, cancellationToken);

    /// <inheritdoc />
    public Task ReadyAsync(Guid lawyerUserId, DocumentTransferNotice notice, CancellationToken cancellationToken = default) =>
        _hub.Clients.User(lawyerUserId.ToString()).SendAsync("TransferReady", notice, cancellationToken);
}

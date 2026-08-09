using System.Net.Http.Json;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Domain.Enums;
using Microsoft.AspNetCore.SignalR.Client;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class ChatHubSignalRTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public ChatHubSignalRTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChatHub_ConnectJoinAndReceiveMessage_Succeeds()
    {
        var (alice, aliceLogin) = await _factory.CreateAuthenticatedClientAsync("+919700200201");
        var (bob, bobLogin) = await _factory.CreateAuthenticatedClientAsync("+919700200202");

        var start = await alice.PostAsJsonAsync(
            "/api/chat/conversations",
            new { otherUserId = bobLogin.UserId });
        Assert.True(start.IsSuccessStatusCode, await start.Content.ReadAsStringAsync());
        var conversation = await start.Content.ReadFromJsonAsync<ConversationListDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(conversation);

        await using var bobConnection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress!, $"/hubs/chat?access_token={Uri.EscapeDataString(bobLogin.Tokens.AccessToken)}"),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .WithAutomaticReconnect()
            .Build();

        var received = new TaskCompletionSource<MessageResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        bobConnection.On<MessageResponse>("MessageReceived", payload =>
        {
            received.TrySetResult(payload);
        });

        await bobConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, bobConnection.State);

        await bobConnection.InvokeAsync("JoinConversation", conversation.ConversationId);

        var send = await alice.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.ConversationId}/messages",
            new
            {
                body = "Realtime hello",
                type = MessageType.Text,
                attachmentStorageKey = (string?)null,
                attachmentFileName = (string?)null,
                attachmentContentType = (string?)null,
                attachmentFileSizeBytes = (long?)null
            });
        Assert.True(send.IsSuccessStatusCode, await send.Content.ReadAsStringAsync());

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(received.Task, completed);
        var message = await received.Task;
        Assert.Equal("Realtime hello", message.Body);

        await bobConnection.InvokeAsync("Typing", conversation.ConversationId, true);
        await bobConnection.StopAsync();

        _ = bob;
        _ = aliceLogin;
    }
}

using System.Net.Http.Json;
using LawConnect.Domain.Enums;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class ChatAndNotificationApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public ChatAndNotificationApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConversationMessageReadDeleteAndNotifications_Flow_Succeeds()
    {
        var (alice, aliceLogin) = await _factory.CreateAuthenticatedClientAsync("+919700100101");
        var (bob, bobLogin) = await _factory.CreateAuthenticatedClientAsync("+919700100102");

        var start = await alice.PostAsJsonAsync(
            "/api/chat/conversations",
            new { otherUserId = bobLogin.UserId });
        Assert.True(start.IsSuccessStatusCode, await start.Content.ReadAsStringAsync());
        var conversation = await start.Content.ReadFromJsonAsync<ConversationListDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(conversation);
        Assert.Equal(bobLogin.UserId, conversation.OtherUserId);

        var send = await alice.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.ConversationId}/messages",
            new
            {
                body = "Hello Bob",
                type = MessageType.Text,
                attachmentStorageKey = (string?)null,
                attachmentFileName = (string?)null,
                attachmentContentType = (string?)null,
                attachmentFileSizeBytes = (long?)null
            });
        Assert.True(send.IsSuccessStatusCode, await send.Content.ReadAsStringAsync());
        var message = await send.Content.ReadFromJsonAsync<MessageDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(message);
        Assert.Equal("Hello Bob", message.Body);

        var list = await bob.GetFromJsonAsync<PagedDto<ConversationListDto>>(
            "/api/chat/conversations?pageNumber=1&pageSize=20",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(list);
        Assert.Contains(list.Items, c => c.ConversationId == conversation.ConversationId && c.UnreadCount >= 1);

        var messages = await bob.GetFromJsonAsync<MessageCursorDto>(
            $"/api/chat/conversations/{conversation.ConversationId}/messages?pageSize=30",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(messages);
        Assert.Contains(messages.Items, m => m.MessageId == message.MessageId);

        var read = await bob.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.ConversationId}/read",
            new { messageId = message.MessageId });
        Assert.True(read.IsSuccessStatusCode, await read.Content.ReadAsStringAsync());

        var notifications = await bob.GetFromJsonAsync<PagedDto<NotificationItemDto>>(
            "/api/notifications?pageNumber=1&pageSize=20",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(notifications);
        Assert.Contains(notifications.Items, n => n.Type == "chat.new_message");

        var markNotification = await bob.PostAsync(
            $"/api/notifications/{notifications.Items.First(n => n.Type == "chat.new_message").Id}/read",
            null);
        Assert.True(markNotification.IsSuccessStatusCode, await markNotification.Content.ReadAsStringAsync());

        var delete = await alice.DeleteAsync($"/api/chat/messages/{message.MessageId}");
        Assert.True(delete.IsSuccessStatusCode, await delete.Content.ReadAsStringAsync());

        var afterDelete = await bob.GetFromJsonAsync<MessageCursorDto>(
            $"/api/chat/conversations/{conversation.ConversationId}/messages?pageSize=30",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(afterDelete);
        Assert.DoesNotContain(afterDelete.Items, m => m.MessageId == message.MessageId);

        var forbidden = await alice.GetAsync($"/api/chat/conversations/{Guid.NewGuid()}/messages");
        Assert.True(
            forbidden.StatusCode is System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.Forbidden,
            await forbidden.Content.ReadAsStringAsync());

        var upload = await alice.PostAsJsonAsync(
            "/api/chat/uploads/sign",
            new
            {
                conversationId = conversation.ConversationId,
                fileName = "note.pdf",
                contentType = "application/pdf",
                fileSizeBytes = 2048
            });
        Assert.True(upload.IsSuccessStatusCode, await upload.Content.ReadAsStringAsync());
        var signed = await upload.Content.ReadFromJsonAsync<UploadUrlDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(signed);
        Assert.False(string.IsNullOrWhiteSpace(signed.UploadUrl));
        Assert.False(string.IsNullOrWhiteSpace(signed.StorageKey));

        _ = aliceLogin;
    }

    [Fact]
    public async Task ChatAttachment_RequiresSignedSingleUseUpload_AndSignedDownload()
    {
        var (alice, _) = await _factory.CreateAuthenticatedClientAsync("+919700100201");
        var (bob, bobLogin) = await _factory.CreateAuthenticatedClientAsync("+919700100202");
        var anonymous = _factory.CreateClient();

        var start = await alice.PostAsJsonAsync("/api/chat/conversations", new { otherUserId = bobLogin.UserId });
        Assert.True(start.IsSuccessStatusCode, await start.Content.ReadAsStringAsync());
        var conversation = await start.Content.ReadFromJsonAsync<ConversationListDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(conversation);

        var stranger = await bob.PostAsJsonAsync(
            "/api/chat/uploads/sign",
            new
            {
                conversationId = Guid.NewGuid(),
                fileName = "brief.pdf",
                contentType = "application/pdf",
                fileSizeBytes = 16
            });
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, stranger.StatusCode);

        var sign = await alice.PostAsJsonAsync(
            "/api/chat/uploads/sign",
            new
            {
                conversationId = conversation.ConversationId,
                fileName = "brief.pdf",
                contentType = "application/pdf",
                fileSizeBytes = 16
            });
        Assert.True(sign.IsSuccessStatusCode, await sign.Content.ReadAsStringAsync());
        var signed = await sign.Content.ReadFromJsonAsync<UploadUrlDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(signed);
        Assert.Contains("sig=", signed.UploadUrl);

        static ByteArrayContent Pdf(string contentType = "application/pdf")
        {
            var content = new ByteArrayContent("%PDF-1.4 test"u8.ToArray());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return content;
        }

        var unsignedPath = signed.UploadUrl.Split('?')[0];
        var unsigned = await bob.PutAsync(unsignedPath, Pdf());
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, unsigned.StatusCode);

        var otherKey = await bob.PutAsync("/api/storage/uploads/profiles%2Fvictim.jpg" + signed.UploadUrl[signed.UploadUrl.IndexOf('?')..], Pdf());
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, otherKey.StatusCode);

        var wrongType = await alice.PutAsync(signed.UploadUrl, Pdf("text/html"));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, wrongType.StatusCode);

        var upload = await alice.PutAsync(signed.UploadUrl, Pdf());
        Assert.Equal(System.Net.HttpStatusCode.NoContent, upload.StatusCode);

        var overwrite = await bob.PutAsync(signed.UploadUrl, Pdf());
        Assert.Equal(System.Net.HttpStatusCode.Conflict, overwrite.StatusCode);

        var privateKeyMessage = await alice.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.ConversationId}/messages",
            new
            {
                body = (string?)null,
                type = MessageType.Pdf,
                attachmentStorageKey = "private/lawyers/someone/documents/id.pdf",
                attachmentFileName = "id.pdf",
                attachmentContentType = "application/pdf",
                attachmentFileSizeBytes = 16L
            });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, privateKeyMessage.StatusCode);

        var send = await alice.PostAsJsonAsync(
            $"/api/chat/conversations/{conversation.ConversationId}/messages",
            new
            {
                body = (string?)null,
                type = MessageType.Pdf,
                attachmentStorageKey = signed.StorageKey,
                attachmentFileName = "brief.pdf",
                attachmentContentType = "application/pdf",
                attachmentFileSizeBytes = 16L
            });
        Assert.True(send.IsSuccessStatusCode, await send.Content.ReadAsStringAsync());
        var message = await send.Content.ReadFromJsonAsync<MessageDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(message);
        var downloadUrl = Assert.Single(message.Attachments).DownloadUrl;
        Assert.NotNull(downloadUrl);
        Assert.StartsWith("/api/storage/files/", downloadUrl);

        var download = await anonymous.GetAsync(downloadUrl);
        Assert.Equal(System.Net.HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("%PDF-1.4 test", await download.Content.ReadAsStringAsync());

        var tampered = await anonymous.GetAsync(downloadUrl[..(downloadUrl.IndexOf("sig=") + 4)] + "not-a-signature");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, tampered.StatusCode);

        var staticFile = await anonymous.GetAsync($"/files/{signed.StorageKey}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, staticFile.StatusCode);
    }
}

public sealed record ConversationListDto(
    Guid ConversationId,
    Guid OtherUserId,
    string OtherFirstName,
    string OtherLastName,
    string? OtherProfileImageUrl,
    bool OtherIsOnline,
    DateTime? OtherLastSeenAtUtc,
    string? LastMessagePreview,
    DateTime? LastMessageAtUtc,
    int UnreadCount);

public sealed record MessageDto(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? Body,
    string Type,
    DateTime SentAtUtc,
    AttachmentDto[] Attachments,
    string DeliveryState);

public sealed record AttachmentDto(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? DownloadUrl);

public sealed record MessageCursorDto(MessageDto[] Items, string? NextCursor, bool HasMore);

public sealed record NotificationItemDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    string Channel,
    bool IsRead,
    DateTime CreatedAt,
    string? DataJson);

public sealed record UploadUrlDto(string StorageKey, string UploadUrl, DateTime ExpiresAtUtc);

public sealed record PagedDto<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);

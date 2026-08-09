using FluentValidation.TestHelper;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Commands.CreateChatUploadUrl;
using LawConnect.Application.Features.Chat.Commands.SendMessage;
using LawConnect.Domain.Enums;

namespace LawConnect.Tests.Unit.Chat;

public sealed class ChatValidatorAndPresenceTests
{
    [Fact]
    public void SendMessage_TextWithoutBody_Fails()
    {
        var validator = new SendMessageCommandValidator();
        var result = validator.TestValidate(new SendMessageCommand(
            Guid.NewGuid(),
            "   ",
            MessageType.Text,
            null,
            null,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void SendMessage_ImageWithoutAttachment_Fails()
    {
        var validator = new SendMessageCommandValidator();
        var result = validator.TestValidate(new SendMessageCommand(
            Guid.NewGuid(),
            null,
            MessageType.Image,
            null,
            null,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void CreateUploadUrl_RejectsDisallowedContentType()
    {
        var validator = new CreateChatUploadUrlCommandValidator();
        var result = validator.TestValidate(new CreateChatUploadUrlCommand(
            "virus.exe",
            "application/octet-stream",
            1024));

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public async Task PresenceTracker_SupportsMultipleDevices()
    {
        var tracker = new PresenceTracker();
        var userId = Guid.NewGuid();

        await tracker.UserConnectedAsync(userId, "c1");
        await tracker.UserConnectedAsync(userId, "c2");
        Assert.True(tracker.IsOnline(userId));
        Assert.Equal(2, tracker.GetConnections(userId).Count);

        await tracker.UserDisconnectedAsync(userId, "c1");
        Assert.True(tracker.IsOnline(userId));

        await tracker.UserDisconnectedAsync(userId, "c2");
        Assert.False(tracker.IsOnline(userId));
    }
}

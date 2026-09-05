using LawConnect.Infrastructure.Storage;

namespace LawConnect.Tests.Unit.Storage;

public sealed class StoragePathTests
{
    [Fact]
    public void SanitizeFolder_KeepsPrivatePrefixAndSeparators()
    {
        var lawyerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var folder = StoragePath.SanitizeFolder($"private/lawyers/{lawyerId:D}/documents");
        Assert.Equal($"private/lawyers/{lawyerId:D}/documents", folder);
        Assert.True(StoragePath.IsPrivateKey($"{folder}/file.pdf"));
    }

    [Fact]
    public void GetPresignedUrl_PrivateKey_IsRejected()
    {
        Assert.True(StoragePath.IsPrivateKey("private/cases/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/documents/a.pdf"));
        Assert.False(StoragePath.IsPrivateKey("profiles/avatar.png"));
    }
}

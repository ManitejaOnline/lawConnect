using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// AWS S3 storage options.
/// </summary>
public sealed class AwsS3Options
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AwsS3";

    /// <summary>When true, S3 is used instead of local storage.</summary>
    public bool Enabled { get; set; }

    /// <summary>AWS region.</summary>
    public string Region { get; set; } = "ap-south-1";

    /// <summary>Bucket name.</summary>
    public string BucketName { get; set; } = "lawconnect";

    /// <summary>Optional access key (prefer IAM role in production).</summary>
    public string? AccessKey { get; set; }

    /// <summary>Optional secret key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Upload URL lifetime in minutes.</summary>
    [Range(1, 60)]
    public int UploadUrlExpirationMinutes { get; set; } = 15;

    /// <summary>Download URL lifetime in minutes.</summary>
    [Range(1, 1440)]
    public int DownloadUrlExpirationMinutes { get; set; } = 60;
}

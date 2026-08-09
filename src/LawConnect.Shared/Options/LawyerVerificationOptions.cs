using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// Configurable lawyer document verification policy.
/// </summary>
public sealed class LawyerVerificationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "LawyerVerification";

    /// <summary>
    /// Document types that must be uploaded before an application can be submitted.
    /// Names must match DocumentType enum members (for example BarCouncilCertificate).
    /// CertificateOfPractice is not required unless listed here.
    /// </summary>
    [MinLength(1)]
    public List<string> RequiredDocumentTypes { get; set; } =
    [
        "BarCouncilCertificate",
        "GovernmentId"
    ];
}

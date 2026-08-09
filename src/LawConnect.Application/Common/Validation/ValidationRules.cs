using FluentValidation;

namespace LawConnect.Application.Common.Validation;

/// <summary>
/// Shared FluentValidation rule helpers to avoid duplicated validation logic.
/// </summary>
public static class ValidationRules
{
    /// <summary>
    /// Allowed image MIME types.
    /// </summary>
    public static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    /// <summary>
    /// Allowed verification document MIME types.
    /// </summary>
    public static readonly HashSet<string> AllowedDocumentContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    /// <summary>
    /// Applies person name rules.
    /// </summary>
    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[\p{L}\p{M}'\-\s]+$")
            .WithMessage("Name may contain letters, spaces, apostrophes, and hyphens only.");

    /// <summary>
    /// Applies non-negative money rules.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> NonNegativeMoney<T>(this IRuleBuilder<T, decimal> ruleBuilder) =>
        ruleBuilder
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, true);

    /// <summary>
    /// Applies content-type allow-list for images.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ImageContentType<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .Must(ct => AllowedImageContentTypes.Contains(ct))
            .WithMessage("Only JPEG, PNG, or WebP images are allowed.");

    /// <summary>
    /// Applies content-type allow-list for verification documents.
    /// </summary>
    public static IRuleBuilderOptions<T, string> DocumentContentType<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .Must(ct => AllowedDocumentContentTypes.Contains(ct))
            .WithMessage("Only PDF, JPEG, or PNG documents are allowed.");
}

using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.UploadCaseDocument;

/// <summary>
/// Uploads a file attached to a case, optionally scoped to a specific update, hearing, or
/// milestone within it.
/// </summary>
public sealed record UploadCaseDocumentCommand(
    Guid CaseId,
    Guid? CaseUpdateId,
    Guid? HearingId,
    Guid? MilestoneId,
    Stream Content,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<CaseDocumentResponse>>;

/// <summary>
/// Validates <see cref="UploadCaseDocumentCommand"/>.
/// </summary>
public sealed class UploadCaseDocumentCommandValidator : AbstractValidator<UploadCaseDocumentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadCaseDocumentCommandValidator"/> class.
    /// </summary>
    public UploadCaseDocumentCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).DocumentContentType();
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
    }
}

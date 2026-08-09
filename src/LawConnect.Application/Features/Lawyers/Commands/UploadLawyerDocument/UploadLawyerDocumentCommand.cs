using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UploadLawyerDocument;

/// <summary>
/// Uploads a verification document for the current lawyer.
/// </summary>
public sealed record UploadLawyerDocumentCommand(
    DocumentType DocumentType,
    Stream Content,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<LawyerDocumentResponse>>;

/// <summary>
/// Validates <see cref="UploadLawyerDocumentCommand"/>.
/// </summary>
public sealed class UploadLawyerDocumentCommandValidator : AbstractValidator<UploadLawyerDocumentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadLawyerDocumentCommandValidator"/> class.
    /// </summary>
    public UploadLawyerDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).DocumentContentType();
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
    }
}

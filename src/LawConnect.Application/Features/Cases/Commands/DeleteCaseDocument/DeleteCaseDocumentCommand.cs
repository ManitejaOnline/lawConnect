using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.DeleteCaseDocument;

/// <summary>
/// Deletes a file attached to a case. Permitted for whoever uploaded it, or for the case's lawyer
/// (case-management authority over all documents on their own case) — never for a customer
/// deleting someone else's upload.
/// </summary>
public sealed record DeleteCaseDocumentCommand(Guid CaseId, Guid DocumentId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteCaseDocumentCommand"/>.
/// </summary>
public sealed class DeleteCaseDocumentCommandValidator : AbstractValidator<DeleteCaseDocumentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCaseDocumentCommandValidator"/> class.
    /// </summary>
    public DeleteCaseDocumentCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}

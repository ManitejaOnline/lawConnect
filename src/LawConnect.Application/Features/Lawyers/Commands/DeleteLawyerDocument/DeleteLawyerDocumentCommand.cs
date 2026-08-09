using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerDocument;

/// <summary>
/// Deletes a verification document for the current lawyer.
/// </summary>
public sealed record DeleteLawyerDocumentCommand(Guid DocumentId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteLawyerDocumentCommand"/>.
/// </summary>
public sealed class DeleteLawyerDocumentCommandValidator : AbstractValidator<DeleteLawyerDocumentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerDocumentCommandValidator"/> class.
    /// </summary>
    public DeleteLawyerDocumentCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

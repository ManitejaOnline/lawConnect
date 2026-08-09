using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerEducation;

/// <summary>
/// Deletes an education record for the current lawyer.
/// </summary>
public sealed record DeleteLawyerEducationCommand(Guid EducationId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteLawyerEducationCommand"/>.
/// </summary>
public sealed class DeleteLawyerEducationCommandValidator : AbstractValidator<DeleteLawyerEducationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerEducationCommandValidator"/> class.
    /// </summary>
    public DeleteLawyerEducationCommandValidator()
    {
        RuleFor(x => x.EducationId).NotEmpty();
    }
}

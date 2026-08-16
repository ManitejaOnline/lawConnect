using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerComplaint;

/// <summary>
/// Submits a complaint about a lawyer. Always starts Pending — only an admin marking it Verified
/// can ever affect the lawyer's Trust Score.
/// </summary>
public sealed record SubmitLawyerComplaintCommand(
    Guid LawyerId,
    Guid? AppointmentId,
    string Category,
    string Description) : IRequest<Result<LawyerComplaintSubmittedResponse>>;

/// <summary>
/// Validates <see cref="SubmitLawyerComplaintCommand"/>.
/// </summary>
public sealed class SubmitLawyerComplaintCommandValidator : AbstractValidator<SubmitLawyerComplaintCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitLawyerComplaintCommandValidator"/> class.
    /// </summary>
    public SubmitLawyerComplaintCommandValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}

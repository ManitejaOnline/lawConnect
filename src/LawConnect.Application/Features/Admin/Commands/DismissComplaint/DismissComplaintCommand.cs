using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;

namespace LawConnect.Application.Features.Admin.Commands.DismissComplaint;

/// <summary>
/// Admin marks a complaint Dismissed — it never affects Trust Score, verified or not.
/// </summary>
public sealed record DismissComplaintCommand(Guid ComplaintId, string? Notes)
    : IRequest<Result<AdminComplaintSummary>>;

/// <summary>
/// Validates <see cref="DismissComplaintCommand"/>.
/// </summary>
public sealed class DismissComplaintCommandValidator : AbstractValidator<DismissComplaintCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DismissComplaintCommandValidator"/> class.
    /// </summary>
    public DismissComplaintCommandValidator()
    {
        RuleFor(x => x.ComplaintId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

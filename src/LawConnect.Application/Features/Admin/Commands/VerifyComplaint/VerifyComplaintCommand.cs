using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Commands.VerifyComplaint;

/// <summary>
/// Admin marks a complaint Verified with a severity — this is the only way a complaint can ever
/// affect a lawyer's Trust Score.
/// </summary>
public sealed record VerifyComplaintCommand(Guid ComplaintId, ComplaintSeverity Severity, string? Notes)
    : IRequest<Result<AdminComplaintSummary>>;

/// <summary>
/// Validates <see cref="VerifyComplaintCommand"/>.
/// </summary>
public sealed class VerifyComplaintCommandValidator : AbstractValidator<VerifyComplaintCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyComplaintCommandValidator"/> class.
    /// </summary>
    public VerifyComplaintCommandValidator()
    {
        RuleFor(x => x.ComplaintId).NotEmpty();
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

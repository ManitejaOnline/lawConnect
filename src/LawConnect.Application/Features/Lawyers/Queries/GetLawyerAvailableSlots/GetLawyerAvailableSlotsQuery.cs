using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerAvailableSlots;

/// <summary>
/// Returns bookable slots for a lawyer on a local calendar date.
/// </summary>
public sealed record GetLawyerAvailableSlotsQuery(
    Guid LawyerId,
    DateOnly Date,
    string TimeZoneId,
    int SlotMinutes) : IRequest<Result<IReadOnlyList<AvailableSlotResponse>>>;

/// <summary>
/// Validates <see cref="GetLawyerAvailableSlotsQuery"/>.
/// </summary>
public sealed class GetLawyerAvailableSlotsQueryValidator : AbstractValidator<GetLawyerAvailableSlotsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerAvailableSlotsQueryValidator"/> class.
    /// </summary>
    public GetLawyerAvailableSlotsQueryValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SlotMinutes).InclusiveBetween(15, 120);
    }
}

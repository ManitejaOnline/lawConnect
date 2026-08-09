using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Queries.GetUpcomingAppointments;

/// <summary>
/// Lists upcoming scheduled appointments for the current user.
/// </summary>
public sealed record GetUpcomingAppointmentsQuery(
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AppointmentResponse>>>;

/// <summary>
/// Validates <see cref="GetUpcomingAppointmentsQuery"/>.
/// </summary>
public sealed class GetUpcomingAppointmentsQueryValidator : AbstractValidator<GetUpcomingAppointmentsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUpcomingAppointmentsQueryValidator"/> class.
    /// </summary>
    public GetUpcomingAppointmentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}

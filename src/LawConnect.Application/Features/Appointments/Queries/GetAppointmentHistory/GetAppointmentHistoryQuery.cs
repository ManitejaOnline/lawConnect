using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentHistory;

/// <summary>
/// Lists historical appointments for the current user.
/// </summary>
public sealed record GetAppointmentHistoryQuery(
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AppointmentResponse>>>;

/// <summary>
/// Validates <see cref="GetAppointmentHistoryQuery"/>.
/// </summary>
public sealed class GetAppointmentHistoryQueryValidator : AbstractValidator<GetAppointmentHistoryQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentHistoryQueryValidator"/> class.
    /// </summary>
    public GetAppointmentHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}

using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentById;

/// <summary>
/// Gets a single appointment by id for a participant.
/// </summary>
public sealed record GetAppointmentByIdQuery(Guid AppointmentId) : IRequest<Result<AppointmentResponse>>;

/// <summary>
/// Validates <see cref="GetAppointmentByIdQuery"/>.
/// </summary>
public sealed class GetAppointmentByIdQueryValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentByIdQueryValidator"/> class.
    /// </summary>
    public GetAppointmentByIdQueryValidator() => RuleFor(x => x.AppointmentId).NotEmpty();
}

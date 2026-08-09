using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateOnlineStatus;

/// <summary>
/// Updates the current lawyer online status.
/// </summary>
public sealed record UpdateOnlineStatusCommand(bool IsOnline) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="UpdateOnlineStatusCommand"/>.
/// </summary>
public sealed class UpdateOnlineStatusCommandValidator : AbstractValidator<UpdateOnlineStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOnlineStatusCommandValidator"/> class.
    /// </summary>
    public UpdateOnlineStatusCommandValidator()
    {
    }
}

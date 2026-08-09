using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.DeleteAddress;

/// <summary>
/// Soft-deletes an address owned by the current user.
/// </summary>
public sealed record DeleteAddressCommand(Guid AddressId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteAddressCommand"/>.
/// </summary>
public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAddressCommandValidator"/> class.
    /// </summary>
    public DeleteAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}

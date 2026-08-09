using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.CreateAddress;

/// <summary>
/// Creates an address for the current user.
/// </summary>
public sealed record CreateAddressCommand(
    string Line1,
    string? Line2,
    string? PostalCode,
    string? Landmark,
    Guid? CityId,
    double? Latitude,
    double? Longitude,
    bool IsPrimary,
    string? Label) : IRequest<Result<AddressResponse>>;

/// <summary>
/// Validates <see cref="CreateAddressCommand"/>.
/// </summary>
public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAddressCommandValidator"/> class.
    /// </summary>
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Line2).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Line2));
        RuleFor(x => x.PostalCode).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
        RuleFor(x => x.Landmark).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Landmark));
        RuleFor(x => x.Label).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.Label));
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

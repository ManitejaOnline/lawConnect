using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UpdateAddress;

/// <summary>
/// Updates an address owned by the current user.
/// </summary>
public sealed record UpdateAddressCommand(
    Guid AddressId,
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
/// Validates <see cref="UpdateAddressCommand"/>.
/// </summary>
public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAddressCommandValidator"/> class.
    /// </summary>
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Line2).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Line2));
        RuleFor(x => x.PostalCode).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
        RuleFor(x => x.Landmark).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Landmark));
        RuleFor(x => x.Label).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.Label));
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

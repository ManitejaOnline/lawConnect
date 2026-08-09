using System.Text.RegularExpressions;
using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.RegisterCustomer;

/// <summary>
/// Registers a customer account with password authentication.
/// </summary>
public sealed record RegisterCustomerCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion,
    string? IpAddress) : IRequest<Result<AuthLoginResponse>>;

/// <summary>
/// Validates <see cref="RegisterCustomerCommand"/>.
/// </summary>
public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterCustomerCommandValidator"/> class.
    /// </summary>
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phone => PhoneRegex.IsMatch(phone.Trim()))
            .WithMessage("Phone number must be in E.164 format (e.g. +919876543210).");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

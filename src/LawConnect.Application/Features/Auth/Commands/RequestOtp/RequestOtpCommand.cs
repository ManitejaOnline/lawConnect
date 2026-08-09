using System.Text.RegularExpressions;
using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.RequestOtp;

/// <summary>
/// Requests a one-time password for passwordless phone login.
/// </summary>
/// <param name="PhoneNumber">E.164 phone number.</param>
public sealed record RequestOtpCommand(string PhoneNumber) : IRequest<Result<RequestOtpResponse>>;

/// <summary>
/// Validates <see cref="RequestOtpCommand"/>.
/// </summary>
public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOtpCommandValidator"/> class.
    /// </summary>
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phone => PhoneRegex.IsMatch(phone.Trim()))
            .WithMessage("Phone number must be in E.164 format (e.g. +919876543210).");
    }
}

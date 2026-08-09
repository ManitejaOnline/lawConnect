using LawConnect.Application.Features.Auth.Commands.DevLogin;
using LawConnect.Application.Features.Auth.Commands.LoginWithPassword;
using LawConnect.Application.Features.Auth.Commands.Logout;
using LawConnect.Application.Features.Auth.Commands.RefreshToken;
using LawConnect.Application.Features.Auth.Commands.RegisterCustomer;
using LawConnect.Application.Features.Auth.Commands.RegisterLawyerAccount;
using LawConnect.Application.Features.Auth.Commands.RequestOtp;
using LawConnect.Application.Features.Auth.Commands.VerifyOtp;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Queries.GetCurrentAuthUser;
using LawConnect.Api.Extensions;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Passwordless phone authentication endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthConstants.RateLimitPolicy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    /// <param name="environment">Host environment.</param>
    public AuthController(ISender sender, IHostEnvironment environment)
    {
        _sender = sender;
        _environment = environment;
    }

    /// <summary>
    /// Registers a customer account and issues JWT tokens. Designed so Google Sign-In can attach later to the same User.
    /// </summary>
    [HttpPost("register/customer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCustomerCommand(
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Registers a lawyer account in Pending status and issues JWT tokens.
    /// </summary>
    [HttpPost("register/lawyer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterLawyer(
        [FromBody] RegisterLawyerAccountApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterLawyerAccountCommand(
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Logs in with email or phone plus password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] PasswordLoginApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginWithPasswordCommand(
                request.Identifier,
                request.Password,
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Temporary phone login without OTP (Development / Docker only).
    /// </summary>
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DevLogin(
        [FromBody] DevLoginApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Docker"))
        {
            return NotFound();
        }

        var command = new DevLoginCommand(
            request.PhoneNumber,
            request.DeviceIdentifier,
            request.Platform,
            request.FcmToken,
            request.AppVersion,
            request.OsVersion,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Requests an OTP for phone login.
    /// </summary>
    /// <param name="request">OTP request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OTP request acknowledgment.</returns>
    /// <response code="200">OTP dispatched.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="429">Rate limited.</response>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RequestOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestOtpApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RequestOtpCommand(request.PhoneNumber), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Verifies an OTP and issues JWT + refresh tokens.
    /// </summary>
    /// <param name="request">OTP verification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated session tokens.</returns>
    /// <response code="200">Login succeeded.</response>
    /// <response code="401">Invalid OTP.</response>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VerifyOtpCommand(
            request.PhoneNumber,
            request.Code,
            request.DeviceIdentifier,
            request.Platform,
            request.FcmToken,
            request.AppVersion,
            request.OsVersion,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Rotates a refresh token and returns a new token pair.
    /// </summary>
    /// <param name="request">Refresh payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New token pair.</returns>
    /// <response code="200">Tokens rotated.</response>
    /// <response code="401">Invalid refresh token.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken,
            request.DeviceIdentifier,
            request.Platform,
            request.FcmToken,
            request.AppVersion,
            request.OsVersion,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revokes the current refresh token.
    /// </summary>
    /// <param name="request">Logout payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Logged out.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LogoutCommand(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the authenticated user's auth profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current auth user.</returns>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentAuthUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentAuthUserQuery(), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>
/// API request for temporary phone login without OTP.
/// </summary>
public sealed record DevLoginApiRequest(
    string PhoneNumber,
    string DeviceIdentifier,
    DevicePlatform Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// API request for OTP issuance.
/// </summary>
/// <param name="PhoneNumber">E.164 phone number.</param>
public sealed record RequestOtpApiRequest(string PhoneNumber);

/// <summary>
/// API request for OTP verification.
/// </summary>
public sealed record VerifyOtpApiRequest(
    string PhoneNumber,
    string Code,
    string DeviceIdentifier,
    DevicePlatform Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// API request for refresh-token rotation.
/// </summary>
public sealed record RefreshTokenApiRequest(
    string RefreshToken,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// API request for logout.
/// </summary>
/// <param name="RefreshToken">Refresh token to revoke.</param>
public sealed record LogoutApiRequest(string RefreshToken);

/// <summary>
/// Customer password registration payload.
/// </summary>
public sealed record RegisterCustomerApiRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// Lawyer password registration payload (step 1).
/// </summary>
public sealed record RegisterLawyerAccountApiRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// Email/phone + password login payload.
/// </summary>
public sealed record PasswordLoginApiRequest(
    string Identifier,
    string Password,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

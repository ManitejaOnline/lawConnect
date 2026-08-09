using LawConnect.Api.Extensions;
using LawConnect.Application.Features.Users.Commands.CreateAddress;
using LawConnect.Application.Features.Users.Commands.DeactivateDevice;
using LawConnect.Application.Features.Users.Commands.DeleteAddress;
using LawConnect.Application.Features.Users.Commands.DeleteProfilePicture;
using LawConnect.Application.Features.Users.Commands.UpdateAddress;
using LawConnect.Application.Features.Users.Commands.UpdateDevice;
using LawConnect.Application.Features.Users.Commands.UpdateMyProfile;
using LawConnect.Application.Features.Users.Commands.UpdateNotificationSettings;
using LawConnect.Application.Features.Users.Commands.UploadProfilePicture;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Queries.GetMyAddresses;
using LawConnect.Application.Features.Users.Queries.GetMyDevices;
using LawConnect.Application.Features.Users.Queries.GetMyProfile;
using LawConnect.Application.Features.Users.Queries.GetNotificationSettings;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Authenticated user profile, address, device, and notification settings endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyProfileQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMyProfileCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.DateOfBirth,
                request.Gender,
                request.PreferredLanguage),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Uploads a profile picture.
    /// </summary>
    [HttpPost("me/profile-picture")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new UploadProfilePictureCommand(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes the current profile picture.
    /// </summary>
    [HttpDelete("me/profile-picture")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProfilePicture(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteProfilePictureCommand(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists addresses for the current user.
    /// </summary>
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(IReadOnlyList<AddressResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyAddressesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates an address.
    /// </summary>
    [HttpPost("me/addresses")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAddress(
        [FromBody] AddressApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateAddressCommand(
                request.Line1,
                request.Line2,
                request.PostalCode,
                request.Landmark,
                request.CityId,
                request.Latitude,
                request.Longitude,
                request.IsPrimary,
                request.Label),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates an address.
    /// </summary>
    [HttpPut("me/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAddress(
        Guid addressId,
        [FromBody] AddressApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateAddressCommand(
                addressId,
                request.Line1,
                request.Line2,
                request.PostalCode,
                request.Landmark,
                request.CityId,
                request.Latitude,
                request.Longitude,
                request.IsPrimary,
                request.Label),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes an address.
    /// </summary>
    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteAddressCommand(addressId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists devices for the current user.
    /// </summary>
    [HttpGet("me/devices")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyDevicesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates a device registration.
    /// </summary>
    [HttpPut("me/devices/{deviceIdentifier}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDevice(
        string deviceIdentifier,
        [FromBody] UpdateDeviceApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateDeviceCommand(
                deviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deactivates a device.
    /// </summary>
    [HttpDelete("me/devices/{deviceIdentifier}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateDevice(string deviceIdentifier, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateDeviceCommand(deviceIdentifier), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets notification settings.
    /// </summary>
    [HttpGet("me/notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationSettings(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNotificationSettingsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates notification settings.
    /// </summary>
    [HttpPut("me/notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotificationSettings(
        [FromBody] UpdateNotificationSettingsApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateNotificationSettingsCommand(
                request.EnablePush,
                request.EnableSms,
                request.EnableEmail,
                request.EnableInApp),
            cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>
/// Update profile API request.
/// </summary>
public sealed record UpdateMyProfileApiRequest(
    string FirstName,
    string LastName,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PreferredLanguage);

/// <summary>
/// Address API request.
/// </summary>
public sealed record AddressApiRequest(
    string Line1,
    string? Line2,
    string? PostalCode,
    string? Landmark,
    Guid? CityId,
    double? Latitude,
    double? Longitude,
    bool IsPrimary,
    string? Label);

/// <summary>
/// Update device API request.
/// </summary>
public sealed record UpdateDeviceApiRequest(
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// Notification settings API request.
/// </summary>
public sealed record UpdateNotificationSettingsApiRequest(
    bool EnablePush,
    bool EnableSms,
    bool EnableEmail,
    bool EnableInApp);

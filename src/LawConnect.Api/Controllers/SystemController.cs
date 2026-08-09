using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// System liveness endpoints (no business data).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SystemController : ControllerBase
{
    /// <summary>
    /// Returns API liveness metadata.
    /// </summary>
    /// <returns>System information payload.</returns>
    /// <response code="200">API is running.</response>
    [HttpGet("info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        return Ok(new SystemInfoResponse(
            Name: "LawConnect API",
            Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            UtcNow: DateTime.UtcNow));
    }
}

/// <summary>
/// System information response.
/// </summary>
/// <param name="Name">API name.</param>
/// <param name="Version">Assembly version.</param>
/// <param name="Environment">Hosting environment name.</param>
/// <param name="UtcNow">Current UTC timestamp.</param>
public sealed record SystemInfoResponse(
    string Name,
    string Version,
    string Environment,
    DateTime UtcNow);

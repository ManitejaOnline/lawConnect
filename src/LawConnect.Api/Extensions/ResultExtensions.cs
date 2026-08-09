using LawConnect.Application.Common.Models;
using LawConnect.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Extensions;

/// <summary>
/// Maps application <see cref="Result"/> values to HTTP responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a non-generic result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">Application result.</param>
    /// <returns>HTTP action result.</returns>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Converts a generic result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="result">Application result.</param>
    /// <returns>HTTP action result.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToErrorResult(result.Error);
    }

    private static IActionResult ToErrorResult(Error error)
    {
        var statusCode = error.Code switch
        {
            "NotFound" => StatusCodes.Status404NotFound,
            "Conflict" => StatusCodes.Status409Conflict,
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            "Forbidden" => StatusCodes.Status403Forbidden,
            "Validation.Error" => StatusCodes.Status400BadRequest,
            "Auth.OtpRateLimited" => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message
        })
        {
            StatusCode = statusCode
        };
    }
}

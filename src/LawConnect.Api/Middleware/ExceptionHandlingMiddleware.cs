using System.Net;
using System.Text.Json;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using DomainValidationException = LawConnect.Domain.Exceptions.ValidationException;

namespace LawConnect.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 problem details responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="environment">Host environment.</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}. StatusCode={StatusCode} CorrelationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            (int)statusCode,
            context.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;

        if (exception is FluentValidation.ValidationException fluentValidationException)
        {
            problemDetails.Extensions["errors"] = fluentValidationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }
        else if (exception is DomainValidationException domainValidationException)
        {
            problemDetails.Extensions["errors"] = domainValidationException.Errors;
        }

        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;
            context.Response.Headers[HeaderNames.CorrelationId] = context.TraceIdentifier;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }

    private (HttpStatusCode StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException fluentValidationException => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                fluentValidationException.Message),

            DomainValidationException domainValidationException => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                domainValidationException.Message),

            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                "Resource not found",
                notFoundException.Message),

            ConflictException conflictException => (
                HttpStatusCode.Conflict,
                "Conflict",
                conflictException.Message),

            UnauthorizedException unauthorizedException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                unauthorizedException.Message),

            ForbiddenException forbiddenException => (
                HttpStatusCode.Forbidden,
                "Forbidden",
                forbiddenException.Message),

            BusinessRuleException businessRuleException => (
                HttpStatusCode.UnprocessableEntity,
                "Business rule violated",
                businessRuleException.Message),

            DomainException domainException => (
                HttpStatusCode.UnprocessableEntity,
                "Domain rule violated",
                domainException.Message),

            UnauthorizedAccessException unauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                unauthorizedAccessException.Message),

            KeyNotFoundException keyNotFoundException => (
                HttpStatusCode.NotFound,
                "Resource not found",
                keyNotFoundException.Message),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.")
        };
    }
}

using LawConnect.Shared.Constants;
using Serilog.Context;

namespace LawConnect.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation identifier for distributed tracing and logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.CorrelationId] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out var values)
            && !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return values.First()!;
        }

        return Guid.NewGuid().ToString("N");
    }
}

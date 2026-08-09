using System.Diagnostics;

namespace LawConnect.Api.Middleware;

/// <summary>
/// Logs request duration and flags slow requests.
/// </summary>
public sealed class RequestPerformanceMiddleware
{
    private const int SlowRequestThresholdMs = 1000;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPerformanceMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="logger">Logger instance.</param>
    public RequestPerformanceMiddleware(
        RequestDelegate next,
        ILogger<RequestPerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs >= SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request {Method} {Path} completed in {ElapsedMilliseconds} ms with status {StatusCode}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMs,
                    context.Response.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Request {Method} {Path} completed in {ElapsedMilliseconds} ms with status {StatusCode}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMs,
                    context.Response.StatusCode);
            }
        }
    }
}

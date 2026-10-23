using System.Diagnostics;

namespace SchedulerApp.Api.Services.Logging;

public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILoggingService loggingService)
    {
        var startedAt = Stopwatch.GetTimestamp();
        await next(context);
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        await loggingService.LogAsync(
            $"HTTP {context.Request.Method} {context.Request.Path} responded {context.Response.StatusCode} in {elapsedMilliseconds:F0} ms");
    }
}

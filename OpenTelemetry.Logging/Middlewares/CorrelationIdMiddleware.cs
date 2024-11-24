using Microsoft.Extensions.Primitives;

namespace OpenTelemetry.Logging.Middlewares;

public class CorrelationIdMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var value) && !string.IsNullOrWhiteSpace(value) 
            ? value! : Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Request.Headers["X-Correlation-Id"] = correlationId;

        AddCorrelationIdHeaderToResponse(context, correlationId);

        await next(context);
    }

    private static void AddCorrelationIdHeaderToResponse(HttpContext context, StringValues correlationId)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            return Task.CompletedTask;
        });
    }
}

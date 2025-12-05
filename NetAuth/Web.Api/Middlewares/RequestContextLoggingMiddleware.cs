using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace NetAuth.Web.Api.Middlewares;

// NOTE: https://learn.microsoft.com/vi-vn/aspnet/core/fundamentals/middleware/write?view=aspnetcore-10.0#middleware-class
// The middleware class must include:
// - A public constructor with a parameter of type RequestDelegate.
// - A public method named Invoke or InvokeAsync. This method must:
//     - Return a Task.
//     - Accept a first parameter of type HttpContext.

internal sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public Task Invoke(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = new StringValues(correlationId);
            }

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            return next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
        }

        return context.TraceIdentifier;
    }
}
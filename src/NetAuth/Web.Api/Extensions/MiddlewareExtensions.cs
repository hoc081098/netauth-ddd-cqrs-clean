using NetAuth.Web.Api.Middlewares;

namespace NetAuth.Web.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();
        return app;
    }
}
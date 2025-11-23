using NetAuth.Web.Api.ExceptionHandler;
using NetAuth.Web.Api.Extensions;

namespace NetAuth.Web.Api;

public static class WebApiDiModule
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services
    )
    {
        services.AddEndpoints(typeof(WebApiDiModule).Assembly);

        // Chain multiple exception handlers together, and they'll run in the order we register them.
        // ASP.NET Core will use the first one that returns true from TryHandleAsync.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(); // Provide IProblemDetailsService

        return services;
    }
}
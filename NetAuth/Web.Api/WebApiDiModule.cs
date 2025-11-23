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

        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddProblemDetails(); // Provide IProblemDetailsService

        return services;
    }
}
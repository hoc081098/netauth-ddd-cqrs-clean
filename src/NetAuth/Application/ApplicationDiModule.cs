using FluentValidation;
using NetAuth.Application.Core.Behaviors;

namespace NetAuth.Application;

public static class ApplicationDiModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationDiModule).Assembly, includeInternalTypes: true);

        // Add MediatR with Behaviors
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ApplicationDiModule).Assembly);
            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        return services;
    }
}
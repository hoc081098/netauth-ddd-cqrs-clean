using System.Reflection;
using FluentValidation;
using NetAuth.Application.Core.Behaviors;

namespace NetAuth.Application;

public static class ApplicationDiModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add MediatR with Behaviors
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
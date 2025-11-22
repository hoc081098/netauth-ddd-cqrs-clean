using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetAuth.Web.Api.Endpoints;

namespace NetAuth.Web.Api.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        // Take only non-abstract classes that implement IEndpoint,
        // and register them as transient services.
        var serviceDescriptors = assembly
            .DefinedTypes
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(service: typeof(IEndpoint), implementationType: type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        // Retrieve all registered IEndpoint services and call MapEndpoint on each with the provided builder.
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>().ToArray();
        IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        app.Logger.LogInformation("Mapped {Count} endpoints.", endpoints.Length);
        return app;
    }
}
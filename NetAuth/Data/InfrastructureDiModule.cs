using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Abstractions.Data;

namespace NetAuth.Data;

public static class InfrastructureDiModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(optionsBuilder =>
            optionsBuilder
                .UseNpgsql(configuration.GetConnectionString("Database"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AppDbContext>());

        // Bind section "Jwt" → JwtConfig
        services.Configure<JwtConfig>(configuration.GetSection("Jwt"));
        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        // Add authentication and authorization services
        services
            .AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddAuthorization();

        // Add repositories, providers and services
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        services.AddSingleton<IAuthenticationRepository, FakeAuthenticationRepository>();

        return services;
    }
}
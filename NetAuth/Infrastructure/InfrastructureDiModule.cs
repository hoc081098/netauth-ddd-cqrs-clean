using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Authentication;
using NetAuth.Infrastructure.Common;
using NetAuth.Infrastructure.Cryptography;
using NetAuth.Infrastructure.Repositories;

namespace NetAuth.Infrastructure;

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
        services.Configure<JwtConfig>(configuration.GetSection(JwtConfig.SectionKey));
        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        // Add authentication and authorization services
        services
            .AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddAuthorization();

        // Add HttpContextAccessor
        services.AddHttpContextAccessor();

        services.AddSingleton<IAuthenticationRepository, FakeAuthenticationRepository>();

        // Add repositories and providers
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IUserIdentifierProvider, UserIdentifierProvider>();

        // Add password hasher
        services.AddTransient<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<IPasswordHashChecker, Pbkdf2PasswordHasher>();

        // Common
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
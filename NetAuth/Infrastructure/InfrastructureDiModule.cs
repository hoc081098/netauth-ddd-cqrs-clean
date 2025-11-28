using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Authentication;
using NetAuth.Infrastructure.Authorization;
using NetAuth.Infrastructure.Cryptography;
using NetAuth.Infrastructure.Interceptors;
using NetAuth.Infrastructure.Outbox;
using NetAuth.Infrastructure.Repositories;
using Npgsql;
using Quartz;
using SystemClock = NetAuth.Infrastructure.Common.SystemClock;

namespace NetAuth.Infrastructure;

public static class InfrastructureDiModule
{
    private const string QuartzSchedulerId = "NetAuth.Scheduler.Core";
    private const string QuartzSchedulerName = "NetAuth.Quartz.AspNetCore.Scheduler";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SoftDeletableEntityInterceptor>();
        services.AddSingleton(_ =>
        {
            var connectionString = configuration.GetConnectionString("Database");
            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });
        // https://www.npgsql.org/efcore/release-notes/7.0.html#support-for-dbdatasource
        services.AddDbContext<AppDbContext>((serviceProvider, optionsBuilder) =>
            optionsBuilder
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

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
        services.AddScoped<IPermissionService, PermissionService>();
        // IClaimsTransformation should be registered as transient
        services.AddTransient<IClaimsTransformation, PermissionClaimsTransformation>();
        // IAuthorizationHandler and IAuthorizationPolicyProvider can be registered as singletons
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        // Add HttpContextAccessor
        services.AddHttpContextAccessor();

        // Add repositories and providers
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IUserIdentifierProvider, UserIdentifierProvider>();

        // Add password hasher
        services.AddTransient<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<IPasswordHashChecker, Pbkdf2PasswordHasher>();

        // Common
        services.AddSingleton<IClock, SystemClock>();

        // Add Quartz.NET services
        // Bind section "OutboxSettings" → OutboxSettings
        services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionKey));
        services.AddTransient<IOutboxMessageResolver, OutboxMessageResolver>();
        services.AddQuartz(options =>
        {
            // base Quartz scheduler, job and trigger configuration
            options.SchedulerId = QuartzSchedulerId;
            options.SchedulerName = QuartzSchedulerName;
        });
        // ASP.NET Core hosting
        services.AddQuartzHostedService(options =>
        {
            // When shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });
        services.ConfigureOptions<OutboxMessagesProcessorJobSetup>();
        services.AddScoped<OutboxProcessor>();

        return services;
    }
}
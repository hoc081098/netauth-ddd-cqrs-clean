using System.Text;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Authorization;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Domain.TodoItems;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Authentication;
using NetAuth.Infrastructure.Authorization;
using NetAuth.Infrastructure.Cryptography;
using NetAuth.Infrastructure.HealthChecks;
using NetAuth.Infrastructure.Interceptors;
using NetAuth.Infrastructure.Outbox;
using NetAuth.Infrastructure.Repositories;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
        var dbConnectionString = configuration.GetConnectionString("Database");
        Guard.Against.NullOrEmpty(dbConnectionString);

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SoftDeletableEntityInterceptor>();
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(dbConnectionString).Build());
        // https://www.npgsql.org/efcore/release-notes/7.0.html#support-for-dbdatasource
        services.AddDbContext<AppDbContext>((serviceProvider, optionsBuilder) =>
            optionsBuilder
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AppDbContext>());

        // Bind section "Jwt" → JwtConfig
        services.AddOptions<JwtConfig>()
            .Bind(configuration.GetSection(JwtConfig.SectionKey))
            .ValidateDataAnnotations()
            .Validate(
                validation: jwtConfig =>
                    jwtConfig.Expiration > TimeSpan.Zero &&
                    jwtConfig.RefreshTokenExpiration > TimeSpan.Zero &&
                    jwtConfig.Expiration < jwtConfig.RefreshTokenExpiration,
                failureMessage: "Jwt Expiration must be less than Refresh Token Expiration and" +
                                " both must be greater than zero.")
            .Validate(
                validation: jwtConfig =>
                    !string.IsNullOrWhiteSpace(jwtConfig.SecretKey) &&
                    Encoding.UTF8.GetByteCount(jwtConfig.SecretKey) >= 32,
                failureMessage: "Jwt SecretKey must be at least 256 bits (32 bytes) long.")
            .ValidateOnStart();
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

        // Add caching services
        services.AddHybridCache(options =>
        {
            // Maximum size of cached items
            options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10MB
            options.MaximumKeyLength = 512; // 512 characters

            // Default timeouts
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });

        var redisConnectionString = configuration.GetConnectionString("Redis");
        Guard.Against.NullOrEmpty(redisConnectionString);
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "netauth:cache:";
        });

        // Add HttpContextAccessor
        services.AddHttpContextAccessor();

        // Add repositories and providers
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITodoItemRepository, TodoItemRepository>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IUserContext, UserContext>();

        // Add password hasher
        services.AddTransient<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<IPasswordHashChecker, Pbkdf2PasswordHasher>();

        // Common
        services.AddSingleton<IClock, SystemClock>();

        // Bind section "Outbox" → OutboxSettings
        services.AddOptions<OutboxSettings>()
            .Bind(configuration.GetSection(OutboxSettings.SectionKey))
            .ValidateDataAnnotations()
            .Validate(
                validation: outboxSettings =>
                    outboxSettings.Interval > TimeSpan.Zero &&
                    outboxSettings.CleanupRetention > TimeSpan.Zero,
                failureMessage: "Outbox Interval and CleanupRetention must be greater than zero.")
            .ValidateOnStart();

        // Add Quartz.NET services
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

        // Add health checks
        services
            .AddHealthChecks()
            .AddRedis(redisConnectionString)
            .AddNpgSql(dbConnectionString)
            .AddDbContextCheck<AppDbContext>()
            .AddCheck<OutboxHealthCheck>(name: "outbox");

        services.AddOpenTelemetryConfiguration();

        return services;
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddOpenTelemetryConfiguration()
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("NetAuth.Api"))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddRedisInstrumentation()
                        .AddNpgsql();

                    tracing.AddOtlpExporter();
                });

            return services;
        }
    }
}
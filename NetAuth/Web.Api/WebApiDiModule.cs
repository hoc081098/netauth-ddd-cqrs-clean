using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using NetAuth.Web.Api.ExceptionHandlers;
using NetAuth.Web.Api.Extensions;
using NetAuth.Web.Api.OpenApi;

namespace NetAuth.Web.Api;

public static class WebApiDiModule
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services
    )
    {
        // Register all endpoints from this assembly
        services.AddEndpoints(typeof(WebApiDiModule).Assembly);

        // Chain multiple exception handlers together, and they'll run in the order we register them.
        // ASP.NET Core will use the first one that returns true from TryHandleAsync.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(); // Provide IProblemDetailsService

        // Configure API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);

            // The HTTP headers "api-supported-versions" and "api-deprecated-versions" will be added to all valid service routes
            options.ReportApiVersions = true;

            // When a client does not provide an API version, use the default API version
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Read api version from URL segment, for example: /v1/resource
            options.ApiVersionReader = new UrlSegmentApiVersionReader();

            // options.ApiVersionReader = new HeaderApiVersionReader("X-ApiVersion");
            // options.ApiVersionReader = new UrlSegmentApiVersionReader();
            // options.ApiVersionReader = new QueryStringApiVersionReader();
            // options.ApiVersionReader = ApiVersionReader.Combine(
            //     new HeaderApiVersionReader("X-ApiVersion"),
            //     new UrlSegmentApiVersionReader(),
            //     new QueryStringApiVersionReader()
            // );
        }).AddApiExplorer(options =>
        {
            // See ApiVersionFormatProvider
            // V is The major version of the API version.
            options.GroupNameFormat = "'v'V"; // => "v1", "v2", etc.

            // - Tells ApiExplorer to replace the API version placeholder in route templates with the actual version when generating metadata/OpenAPI groups.
            // - Useful when using URL-segment versioning (you already set `ApiVersionReader = new UrlSegmentApiVersionReader()`).
            // - With this enabled, a route like:
            // ```csharp
            // app.MapGet("/v{version:apiVersion}/users", () => ...)
            // ```
            // will be exposed in the API explorer/docs as `/v1/users` (for version 1) instead of keeping the `{version:apiVersion}` placeholder.
            options.SubstituteApiVersionInUrl = true; // Replace the version in route templates
        });

        // Add Swagger UI
        services.AddEndpointsApiExplorer(); // required for Swashbuckle to discover Minimal API endpoints
        services.ConfigureOptions<ConfigureSwaggerGenOptions>();
        services.AddSwaggerGen();

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Add a rate limiter that limits login attempts per IP address
            rateLimiterOptions.AddPolicy(
                policyName: RateLimiterPolicyNames.LoginLimiter,
                partitioner: httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"{RateLimiterPolicyNames.LoginLimiter}-{ipAddress}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            // 5 req / 20s
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(20),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0, // No queueing
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        }
                    );
                });

            // Add a rate limiter that limits registration attempts per IP address
            rateLimiterOptions.AddPolicy(
                policyName: RateLimiterPolicyNames.RegisterLimiter,
                partitioner: httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"{RateLimiterPolicyNames.RegisterLimiter}-{ipAddress}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            //  3 req / 1 minute
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 3,
                            QueueLimit = 0, // No queueing
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        }
                    );
                });


            // Add a rate limiter that limits refresh token attempts per IP address
            rateLimiterOptions.AddPolicy(
                policyName: RateLimiterPolicyNames.RefreshTokenLimiter,
                partitioner: httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"{RateLimiterPolicyNames.RefreshTokenLimiter}-{ipAddress}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            //  20 req / 1 minute
                            PermitLimit = 20, // allow auto-refresh
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 3,
                            QueueLimit = 0, // No queueing
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }
                    );
                });

            // Policy for all other endpoints: rate limited to 100 requests per minute
            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"GlobalLimiter-{ipAddress}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4
                    });
            });
        });

        return services;
    }
}
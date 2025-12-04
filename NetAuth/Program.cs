using System.Security.Claims;
using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Infrastructure;
using NetAuth.Web.Api;
using NetAuth.Web.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddWebApi();

var app = builder.Build();

// Enables rate limiting for the application.
// Should call before mapping endpoints.
app.UseRateLimiter();

// Map endpoints with API versioning
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

var versionedGroupBuilder = app
    .MapGroup("v{apiVersion:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

app.MapEndpoints(routeGroupBuilder: versionedGroupBuilder);

versionedGroupBuilder.MapGet("/me",
        (IUserIdentifierProvider userIdentifierProvider) => new { userIdentifierProvider.UserId })
    .RequireAuthorization("permission:users:read")
    .WithName("GetCurrentUser")
    .WithSummary("Get current authenticated user.")
    .WithDescription("Returns the details of the currently authenticated user.")
    .Produces(StatusCodes.Status401Unauthorized);

versionedGroupBuilder
    .MapGet("/me-public-v1", (ClaimsPrincipal user) => new { user.Identity })
    .MapToApiVersion(1);
versionedGroupBuilder
    .MapGet("/me-public-v2", (ClaimsPrincipal user) => new { user.Identity })
    .MapToApiVersion(2);
versionedGroupBuilder
    .MapGet("/me-required-auth",
        (ClaimsPrincipal user, IUserIdentifierProvider identifierProvider) =>
            new { user.Identity, identifierProvider.UserId })
    .RequireAuthorization();
versionedGroupBuilder
    .MapGet("/me-required-permission",
        (ClaimsPrincipal user, IUserIdentifierProvider identifierProvider) =>
            new { user.Identity, identifierProvider.UserId })
    .RequireAuthorization("permission:users:read");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var serviceScope = app.Services.CreateScope();
    var appDbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
    appDbContext.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Build a swagger endpoint for each discovered API version
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint(
                url: $"/swagger/{description.GroupName}/swagger.json",
                name: description.GroupName.ToUpperInvariant()
            );
        }
    });
}

// Adds middleware for redirecting HTTP Requests to HTTPS.
app.UseHttpsRedirection();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Adds middleware for streamlined request logging
app.UseSerilogRequestLogging();

// Adds exception handling middleware to the request pipeline
app.UseExceptionHandler();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
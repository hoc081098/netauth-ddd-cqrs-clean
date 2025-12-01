using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NetAuth.Application;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Infrastructure;
using NetAuth.Web.Api;
using NetAuth.Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddWebApi();

// Add Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // This code customizes the schema IDs used by Swagger/OpenAPI to represent .NET types.
    // By default, Swagger uses just the class name, which can lead to conflicts if there are multiple classes with the same name
    // in different namespaces or nested classes.
    // This customization changes the schema ID to use the full name of the type,
    // including its namespace, and replaces '+' characters (used in nested class names) with '-' to ensure valid schema IDs.
    o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

    // This code configures Swagger/OpenAPI to recognize JWT Bearer authentication. It defines a security scheme that:
    // - Registers a JWT authentication method in the Swagger UI
    // - Specifies the token should be sent in the HTTP Authorization header
    // - Uses the Bearer authentication scheme
    // - Indicates the expected token format is JWT
    // - Provides a description for users to know they need to enter a JWT token
    // - This definition enables Swagger UI to show an "Authorize" button where users can input their JWT token,
    //   which will then be automatically included in API requests for testing authenticated endpoints.
    o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token in this field",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });

    // This code configures Swagger UI to require JWT authentication for all API endpoints by default.
    // It adds a global security requirement that tells Swagger:
    // - All endpoints should display a lock icon
    // - Users must provide a JWT token to test endpoints
    // - The security scheme references the JWT Bearer authentication defined earlier
    // - The empty array [] means no specific scopes are required—just a valid JWT token.
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            []
        }
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var serviceScope = app.Services.CreateScope();
    var appDbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
    appDbContext.Database.Migrate();

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Adds exception handling middleware to the request pipeline
app.UseExceptionHandler();

// Map endpoints with API versioning
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
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
    .MapGet("/me-public", (ClaimsPrincipal user) => new { user.Identity });
versionedGroupBuilder
    .MapGet("/me-required-auth", (ClaimsPrincipal user) => new { user.Identity })
    .RequireAuthorization();
versionedGroupBuilder
    .MapGet("/me-required-permission", (ClaimsPrincipal user) => new { user.Identity })
    .RequireAuthorization("permission:users:read");

app.Run();
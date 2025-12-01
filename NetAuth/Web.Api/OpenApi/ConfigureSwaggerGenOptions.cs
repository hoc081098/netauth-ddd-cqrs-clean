using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetAuth.Web.Api.OpenApi;

internal sealed class ConfigureSwaggerGenOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider
) : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            var apiVersion = description.ApiVersion;
            var apiInfo = new OpenApiInfo
            {
                Title = $"NetAuth API v{apiVersion}",
                Version = apiVersion.ToString(),
            };
            options.SwaggerDoc(name: description.GroupName, info: apiInfo);
        }

        // This code customizes the schema IDs used by Swagger/OpenAPI to represent .NET types.
        // By default, Swagger uses just the class name, which can lead to conflicts if there are multiple classes with the same name
        // in different namespaces or nested classes.
        // This customization changes the schema ID to use the full name of the type,
        // including its namespace, and replaces '+' characters (used in nested class names) with '-' to ensure valid schema IDs.
        options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

        // This code configures Swagger/OpenAPI to recognize JWT Bearer authentication. It defines a security scheme that:
        // - Registers a JWT authentication method in the Swagger UI
        // - Specifies the token should be sent in the HTTP Authorization header
        // - Uses the Bearer authentication scheme
        // - Indicates the expected token format is JWT
        // - Provides a description for users to know they need to enter a JWT token
        // - This definition enables Swagger UI to show an "Authorize" button where users can input their JWT token,
        //   which will then be automatically included in API requests for testing authenticated endpoints.
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
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
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);
}
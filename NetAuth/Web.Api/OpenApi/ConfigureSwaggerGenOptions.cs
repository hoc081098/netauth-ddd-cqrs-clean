using Asp.Versioning.ApiExplorer;
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
            var apiInfo = new OpenApiInfo()
            {
                Title = $"NetAuth API v{apiVersion}",
                Version = apiVersion.ToString(),
            };
            options.SwaggerDoc(name: description.GroupName, info: apiInfo);
        }
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);
}
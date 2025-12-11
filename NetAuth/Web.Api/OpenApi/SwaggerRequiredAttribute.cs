using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetAuth.Web.Api.OpenApi;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class SwaggerRequiredAttribute : Attribute;

[UsedImplicitly]
internal sealed class AddSwaggerRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var schemaRequired = schema.Required;
        if (schema.Properties is null || schemaRequired is null) return;

        // If the entire type is marked as required, mark all properties as required
        if (Attribute.IsDefined(element: context.Type, attributeType: typeof(SwaggerRequiredAttribute), inherit: true))
        {
            foreach (var property in schema.Properties.Keys)
            {
                schemaRequired.Add(property);
            }

            return;
        }

        var properties = context.Type.GetProperties();
        foreach (var key in schema.Properties.Keys)
        {
            var propertyInfo = FindClrProperty(properties, key);
            if (propertyInfo is not null && IsMarkedRequired(propertyInfo))
            {
                schemaRequired.Add(key);
            }
        }
    }

    private static PropertyInfo? FindClrProperty(PropertyInfo[] props, string key) =>
        props.FirstOrDefault(prop =>
        {
            var jsonPropertyNameAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();

            return jsonPropertyNameAttr is not null
                ? jsonPropertyNameAttr.Name == key
                : JsonNamingPolicy.CamelCase.ConvertName(prop.Name) == key;
        });

    /// <summary>
    /// Determines if a property is marked as required via <see cref="SwaggerRequiredAttribute"/>
    /// or <see cref="System.Runtime.CompilerServices.RequiredMemberAttribute"/> (C# 11 `required` members)
    /// </summary>
    /// <param name="prop"></param>
    /// <returns></returns>
    private static bool IsMarkedRequired(PropertyInfo prop) =>
        Attribute.IsDefined(prop, typeof(SwaggerRequiredAttribute)) ||
        Attribute.IsDefined(prop, typeof(System.Runtime.CompilerServices.RequiredMemberAttribute));
}
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NetAuth.Infrastructure.Authentication;

/// <summary>
/// Represents the JWT configuration settings.
/// </summary>
public record JwtConfig
{
    public const string SectionKey = "Jwt";

    private readonly Lock _lock = new();

    [Required(ErrorMessage = "SecretKey is required.")]
    public required string SecretKey { get; init; }

    [Required(ErrorMessage = "Issuer is required.")]
    public required string Issuer { get; init; }

    [Required(ErrorMessage = "Audience is required.")]
    public required string Audience { get; init; }

    public required TimeSpan Expiration { get; init; }

    public required TimeSpan RefreshTokenExpiration { get; init; }

    [JsonIgnore]
    [field: JsonIgnore]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code")]
    public SecurityKey IssuerSigningKey
    {
        get
        {
            if (field is not null) return field;
            lock (_lock)
            {
                return field
                    ??= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            }
        }
    }
}

public sealed class ConfigureJwtBearerOptions(
    IOptions<JwtConfig> jwtConfigOptions
) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        var jwtConfig = jwtConfigOptions.Value;

        // Preserve all claims from the token (including "sub")
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtConfig.IssuerSigningKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // don't allow clock skew
        };
    }

    public void Configure(string? name, JwtBearerOptions options)
        => Configure(options);
}
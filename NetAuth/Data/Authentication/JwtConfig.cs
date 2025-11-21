using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NetAuth.Data.Authentication;

public record JwtConfig
{
    private readonly Lock _lock = new();

    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required TimeSpan Expiration { get; init; }

    [JsonIgnore] private SecurityKey? _issuerSigningKey;

    [JsonIgnore]
    public SecurityKey IssuerSigningKey
    {
        get
        {
            if (_issuerSigningKey is not null) return _issuerSigningKey;
            lock (_lock)
            {
                return _issuerSigningKey
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
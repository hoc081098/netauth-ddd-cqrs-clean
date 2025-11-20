using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NetAuth;

public interface IJwtTokenProvider
{
    string CreateJwtToken(LegacyUser user);
}

internal sealed class JwtTokenProvider(
    IOptions<JwtConfig> jwtConfigOptions
) : IJwtTokenProvider
{
    public string CreateJwtToken(LegacyUser user)
    {
        var jwtConfig = jwtConfigOptions.Value;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var creds = new SigningCredentials(
            jwtConfig.IssuerSigningKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtConfig.Issuer,
            audience: jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(jwtConfig.Expiration),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Authentication;

public interface IJwtProvider
{
    [Obsolete("Use CreateJwtToken(User user) instead.")]
    string CreateJwtToken(LegacyUser user);

    string CreateJwtToken(User user);
}

internal sealed class JwtProvider(
    IOptions<JwtConfig> jwtConfigOptions
) : IJwtProvider
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

    public string CreateJwtToken(User user)
    {
        var jwtConfig = jwtConfigOptions.Value;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var credentials = new SigningCredentials(
            jwtConfig.IssuerSigningKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtConfig.Issuer,
            audience: jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(jwtConfig.Expiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
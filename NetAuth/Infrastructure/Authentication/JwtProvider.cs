using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class JwtProvider(
    IOptions<JwtConfig> jwtConfigOptions
) : IJwtProvider
{
    public string Create(User user)
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
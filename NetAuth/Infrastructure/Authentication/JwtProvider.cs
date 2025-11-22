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
    private readonly JwtSecurityTokenHandler _handler = new();

    public string Create(User user)
    {
        var jwtConfig = jwtConfigOptions.Value;

        var claims = new[]
        {
            // Subject: uniquely identifies the user
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // JWT ID: uniquely identifies the token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            // Email: user's email
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // Preferred Username: user's username
            new Claim(JwtRegisteredClaimNames.PreferredUsername, user.Username),
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

        return _handler.WriteToken(token);
    }
}
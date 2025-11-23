using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class UserIdentifierProvider(
    IHttpContextAccessor httpContextAccessor
) : IUserIdentifierProvider
{
    public Guid UserId => GetUserId(httpContextAccessor);

    private static Guid GetUserId(IHttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = httpContextAccessor.HttpContext
            ?.User
            .FindFirstValue(JwtRegisteredClaimNames.Sub);

        Guard.Against.NullOrEmpty(userIdClaim);

        return Guid.Parse(userIdClaim);
    }
}
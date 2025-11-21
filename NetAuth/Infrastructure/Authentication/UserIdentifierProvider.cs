using System.Security.Claims;
using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class UserIdentifierProvider(
    HttpContextAccessor httpContextAccessor
) : IUserIdentifierProvider
{
    public Guid UserId { get; } = GetUserId(httpContextAccessor);

    private static Guid GetUserId(HttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = httpContextAccessor.HttpContext
            ?.User
            ?.FindFirstValue(ClaimTypes.NameIdentifier);

        Guard.Against.NullOrEmpty(userIdClaim);

        return Guid.Parse(userIdClaim);
    }
}
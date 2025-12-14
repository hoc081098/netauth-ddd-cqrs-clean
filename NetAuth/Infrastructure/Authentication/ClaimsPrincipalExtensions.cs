using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NetAuth.Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user identifier from the claims principal or `null` if not found/invalid/empty.
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    internal static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (
            string.IsNullOrWhiteSpace(userIdClaim) ||
            !Guid.TryParse(userIdClaim, out var userId) ||
            userId == Guid.Empty
        )
        {
            return null;
        }

        return userId;
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NetAuth.Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    /// <param name="principal"></param>
    extension(ClaimsPrincipal principal)
    {
        /// <summary>
        /// Gets the user identifier from the claims principal or `null` if not found/invalid/empty.
        /// </summary>
        /// <returns></returns>
        internal Guid? GetUserIdOrNull()
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

        /// <summary>
        /// Checks if the claims principal has the specified permission.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        internal bool HasPermission(string permission)
        {
            return principal
                .HasClaim(claim =>
                    string.Equals(claim.Type, CustomClaimTypes.Permission, StringComparison.Ordinal) &&
                    string.Equals(claim.Value, permission, StringComparison.Ordinal));
        }
    }
}
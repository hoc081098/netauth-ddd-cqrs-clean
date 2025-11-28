using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using NetAuth.Infrastructure.Authentication;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionClaimsTransformation(
    IPermissionService permissionService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
        {
            return principal;
        }

        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (
            string.IsNullOrEmpty(userIdClaim) ||
            !Guid.TryParse(userIdClaim, out var userId) ||
            principal.Identity is not ClaimsIdentity claimsIdentity)
        {
            return principal;
        }

        if (claimsIdentity.HasClaim(c => c.Type == CustomClaims.Permission))
        {
            // Permissions already added
            return principal;
        }

        // Fetch permissions from database, then cache
        // IMPORTANT: Cache these results to avoid DB hits on every request
        var permissions = await permissionService.GetUserPermissionsAsync(userId);
        foreach (var permission in permissions)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaims.Permission, permission));
        }

        return principal;
    }
}
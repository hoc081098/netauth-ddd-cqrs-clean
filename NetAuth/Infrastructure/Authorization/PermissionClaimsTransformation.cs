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

        // If already hydrated, nothing to do.
        if (claimsIdentity.HasClaim(CustomClaimTypes.PermissionsHydrated, CustomClaimValues.True))
        {
            return principal;
        }

        var permissions = await permissionService.GetUserPermissionsAsync(userId);
        foreach (var permission in permissions)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Permission, permission));
        }

        // Add hydration marker to avoid re-fetching next time.
        claimsIdentity.AddClaim(new Claim(CustomClaimTypes.PermissionsHydrated, CustomClaimValues.True));

        return principal;
    }
}
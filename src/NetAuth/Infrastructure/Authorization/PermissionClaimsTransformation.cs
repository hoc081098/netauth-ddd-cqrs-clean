using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using NetAuth.Application.Abstractions.Authorization;
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

        var userId = principal.GetUserIdOrNull();
        if (userId is null || principal.Identity is not ClaimsIdentity claimsIdentity)
        {
            return principal;
        }

        // If already hydrated, nothing to do.
        if (claimsIdentity.HasClaim(CustomClaimTypes.PermissionsHydrated, CustomClaimValues.True))
        {
            return principal;
        }

        var permissions = await permissionService.GetUserPermissionsAsync(userId.Value);
        foreach (var permission in permissions)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Permission, permission));
        }

        // Add hydration marker to avoid re-fetching next time.
        claimsIdentity.AddClaim(new Claim(CustomClaimTypes.PermissionsHydrated, CustomClaimValues.True));

        return principal;
    }
}
using Microsoft.AspNetCore.Authorization;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    private const string PermissionPolicyPrefix = "permission:";
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.Ordinal);

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            return _cache.GetOrAdd(policyName, s =>
            {
                var permission = s[PermissionPolicyPrefix.Length..];

                return new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
            });
        }

        return null;
    }
}
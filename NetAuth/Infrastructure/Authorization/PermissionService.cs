namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionService : IPermissionService
{
    public Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        // TODO: Implement permission retrieval logic
        IEnumerable<string> result = ["users:read"];
        return Task.FromResult(result);
    }
}
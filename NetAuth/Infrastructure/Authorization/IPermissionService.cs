namespace NetAuth.Infrastructure.Authorization;

internal interface IPermissionService
{
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
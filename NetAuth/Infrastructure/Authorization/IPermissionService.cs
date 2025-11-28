namespace NetAuth.Infrastructure.Authorization;

internal interface IPermissionService
{
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
}
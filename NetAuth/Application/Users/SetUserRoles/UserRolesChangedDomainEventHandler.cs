using NetAuth.Application.Abstractions.Authorization;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.SetUserRoles;

internal sealed class UserRolesChangedDomainEventHandler(
    ILogger<UserRolesChangedDomainEventHandler> logger,
    IPermissionService permissionService
) : IDomainEventHandler<UserRolesChangedDomainEvent>
{
    public async Task Handle(UserRolesChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} roles changed from {@OldRoleIds} to {@NewRoleIds}",
            notification.UserId,
            notification.OldRoleIds,
            notification.NewRoleIds);

        try
        {
            await permissionService.InvalidatePermissionsCacheAsync(notification.UserId, cancellationToken);
            logger.LogInformation("Invalidated permissions cache for User ID: {UserId}", notification.UserId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Error while invalidating permissions cache for User ID: {UserId}",
                notification.UserId);
        }
    }
}
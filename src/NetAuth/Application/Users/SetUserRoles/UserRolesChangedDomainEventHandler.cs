using System.Diagnostics.CodeAnalysis;
using NetAuth.Application.Abstractions.Authorization;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.SetUserRoles;

internal sealed class UserRolesChangedDomainEventHandler(
    ILogger<UserRolesChangedDomainEventHandler> logger,
    IPermissionService permissionService
) : IDomainEventHandler<UserRolesChangedDomainEvent>
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
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

            // NOTE:
            // Cache invalidation here only affects the local instance
            // (L1 in-memory cache and the local L2/distributed cache access of this service).
            // It does NOT invalidate caches held by other nodes or other services.
            //
            // To invalidate permission caches across multiple nodes or services,
            // we must publish an integration event via a message broker.
            //
            // Proposed approach:
            // - Persist an integration event to the Outbox table within the same transaction.
            // - A background worker (Outbox Processor) publishes the event to a message broker (e.g. RabbitMQ).
            // - Use a topic exchange to fan-out the event to multiple queues.
            // - Each service (or node) owns its own queue and subscribes to the event.
            // - Upon receiving the event, each consumer invalidates its local permission cache.
            //
            // This ensures reliable, eventually consistent cache invalidation across the system.
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to invalidate permissions cache for User ID: {UserId}",
                notification.UserId);
        }
    }
}
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.Login;

internal sealed class RefreshTokenCreatedDomainEventHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IClock clock,
    ILogger<RefreshTokenCreatedDomainEventHandler> logger) : IDomainEventHandler<RefreshTokenCreatedDomainEvent>
{
    public async Task Handle(RefreshTokenCreatedDomainEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            var rowsDelete = await refreshTokenRepository.DeleteExpiredByUserIdAsync(@event.UserId,
                clock.UtcNow,
                cancellationToken);

            logger.LogInformation(
                "Deleted {ExpiredRefreshTokenRowsDeleted} expired refresh tokens for User ID: {UserId}",
                rowsDelete,
                @event.UserId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error deleting expired refresh tokens for User ID: {UserId}",
                @event.UserId);
        }
    }
}
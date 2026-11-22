using System.Diagnostics.CodeAnalysis;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.Login;

internal sealed partial class RefreshTokenCreatedDomainEventHandler(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<RefreshTokenCreatedDomainEventHandler> logger) : IDomainEventHandler<RefreshTokenCreatedDomainEvent>
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task Handle(RefreshTokenCreatedDomainEvent @event,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        try
        {
            var rowsDelete = await refreshTokenRepository.DeleteExpiredByUserIdAsync(@event.UserId,
                clock.UtcNow,
                cancellationToken);

            LogDeletedExpiredRefreshTokens(rowsDelete, @event.UserId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogFailedToDeleteExpiredRefreshTokens(@event.UserId, ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Deleted {ExpiredRefreshTokenRowsDeleted} expired refresh tokens for User ID: {UserId}")]
    partial void LogDeletedExpiredRefreshTokens(int expiredRefreshTokenRowsDeleted, Guid userId);

    [LoggerMessage(LogLevel.Error, "Error deleting expired refresh tokens for User ID: {UserId}")]
    partial void LogFailedToDeleteExpiredRefreshTokens(Guid userId, Exception exception);
}
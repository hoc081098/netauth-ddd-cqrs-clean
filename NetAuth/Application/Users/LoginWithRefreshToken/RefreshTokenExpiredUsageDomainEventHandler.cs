using MediatR;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

/// <summary>
/// Handles the RefreshTokenExpiredUsageDomainEvent for security audit logging.
/// Logs attempts to use expired tokens for monitoring and potential attack detection.
/// </summary>
internal sealed class RefreshTokenExpiredUsageDomainEventHandler(
    ILogger<RefreshTokenExpiredUsageDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenExpiredUsageDomainEvent>
{
    public Task Handle(RefreshTokenExpiredUsageDomainEvent notification, CancellationToken cancellationToken)
    {
        RefreshTokenExpiredUsageDomainEventHandlerLoggers.LogExpiredTokenUsage(
            logger: logger,
            userId: notification.UserId,
            tokenId: notification.RefreshTokenId,
            expiredAt: notification.ExpiresOnUtc,
            attemptedAt: notification.AttemptedAt);

        return Task.CompletedTask;
    }
}

internal static partial class RefreshTokenExpiredUsageDomainEventHandlerLoggers
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Expired refresh token usage detected. UserId: {UserId}, TokenId: {TokenId}, ExpiredAt: {ExpiredAt}, AttemptedAt: {AttemptedAt}")]
    internal static partial void LogExpiredTokenUsage(
        ILogger logger,
        Guid userId,
        Guid tokenId,
        DateTimeOffset expiredAt,
        DateTimeOffset attemptedAt);
}
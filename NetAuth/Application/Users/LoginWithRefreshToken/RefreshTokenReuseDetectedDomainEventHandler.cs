using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

/// <summary>
/// Handles the RefreshTokenReuseDetectedDomainEvent for security audit logging.
/// Logs token reuse attempts which indicate potential security breaches.
/// </summary>
internal sealed class RefreshTokenReuseDetectedDomainEventHandler(
    ILogger<RefreshTokenReuseDetectedDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenReuseDetectedDomainEvent>
{
    public Task Handle(RefreshTokenReuseDetectedDomainEvent notification, CancellationToken cancellationToken)
    {
        RefreshTokenReuseDetectedDomainEventHandlerLoggers.LogTokenReuseDetected(
            logger: logger,
            userId: notification.UserId,
            tokenId: notification.RefreshTokenId,
            deviceId: notification.DeviceId,
            previousStatus: notification.PreviousStatus);

        return Task.CompletedTask;
    }
}

internal static partial class RefreshTokenReuseDetectedDomainEventHandlerLoggers
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SECURITY ALERT: Refresh token reuse detected! UserId: {UserId}, TokenId: {TokenId}, DeviceId: {DeviceId}, PreviousStatus: {PreviousStatus}")]
    internal static partial void LogTokenReuseDetected(
        ILogger logger,
        Guid userId,
        Guid tokenId,
        string deviceId,
        RefreshTokenStatus previousStatus);
}
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

/// <summary>
/// Handles the RefreshTokenRotatedDomainEvent for security audit logging.
/// Logs successful token rotation for monitoring and compliance.
/// </summary>
internal sealed class RefreshTokenRotatedDomainEventHandler(
    ILogger<RefreshTokenRotatedDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenRotatedDomainEvent>
{
    public Task Handle(RefreshTokenRotatedDomainEvent notification, CancellationToken cancellationToken)
    {
        RefreshTokenRotatedDomainEventHandlerLoggers.LogTokenRotated(
            logger: logger,
            userId: notification.UserId,
            oldTokenId: notification.OldRefreshTokenId,
            newTokenId: notification.NewRefreshTokenId,
            deviceId: notification.DeviceId);

        return Task.CompletedTask;
    }
}

internal static partial class RefreshTokenRotatedDomainEventHandlerLoggers
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Refresh token rotated successfully. UserId: {UserId}, OldTokenId: {OldTokenId}, NewTokenId: {NewTokenId}, DeviceId: {DeviceId}")]
    internal static partial void LogTokenRotated(
        ILogger logger,
        Guid userId,
        Guid oldTokenId,
        Guid newTokenId,
        string deviceId);
}
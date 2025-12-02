using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

/// <summary>
/// Handles the RefreshTokenDeviceMismatchDetectedDomainEvent for security audit logging.
/// Logs device mismatch attempts which indicate potential token theft.
/// </summary>
internal sealed class RefreshTokenDeviceMismatchDetectedDomainEventHandler(
    ILogger<RefreshTokenDeviceMismatchDetectedDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenDeviceMismatchDetectedDomainEvent>
{
    public Task Handle(RefreshTokenDeviceMismatchDetectedDomainEvent notification, CancellationToken cancellationToken)
    {
        RefreshTokenDeviceMismatchDetectedDomainEventHandlerLoggers.LogDeviceMismatchDetected(
            logger: logger,
            userId: notification.UserId,
            tokenId: notification.RefreshTokenId,
            expectedDeviceId: notification.ExpectedDeviceId,
            actualDeviceId: notification.ActualDeviceId);

        return Task.CompletedTask;
    }
}

internal static partial class RefreshTokenDeviceMismatchDetectedDomainEventHandlerLoggers
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SECURITY ALERT: Device mismatch detected! UserId: {UserId}, TokenId: {TokenId}, ExpectedDeviceId: {ExpectedDeviceId}, ActualDeviceId: {ActualDeviceId}")]
    internal static partial void LogDeviceMismatchDetected(
        ILogger logger,
        Guid userId,
        Guid tokenId,
        string expectedDeviceId,
        string actualDeviceId);
}
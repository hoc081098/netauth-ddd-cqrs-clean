using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

/// <summary>
/// Handles the RefreshTokenChainCompromisedDomainEvent for security audit logging.
/// Logs when an entire token chain is compromised (all user tokens revoked).
/// </summary>
internal sealed class RefreshTokenChainCompromisedDomainEventHandler(
    ILogger<RefreshTokenChainCompromisedDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenChainCompromisedDomainEvent>
{
    public Task Handle(RefreshTokenChainCompromisedDomainEvent notification, CancellationToken cancellationToken)
    {
        RefreshTokenChainCompromisedDomainEventHandlerLoggers.LogTokenChainCompromised(
            logger: logger,
            userId: notification.UserId);

        return Task.CompletedTask;
    }
}

internal static partial class RefreshTokenChainCompromisedDomainEventHandlerLoggers
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "CRITICAL SECURITY ALERT: Entire refresh token chain compromised! UserId: {UserId}")]
    internal static partial void LogTokenChainCompromised(
        ILogger logger,
        Guid userId);
}
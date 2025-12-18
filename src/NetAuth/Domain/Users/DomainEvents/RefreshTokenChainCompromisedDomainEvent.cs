using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

/// <summary>
/// Domain event raised when all refresh tokens for a user are compromised.
/// This happens when token reuse is detected, indicating a security breach.
/// </summary>
public sealed record RefreshTokenChainCompromisedDomainEvent(
    Guid UserId
) : IDomainEvent;


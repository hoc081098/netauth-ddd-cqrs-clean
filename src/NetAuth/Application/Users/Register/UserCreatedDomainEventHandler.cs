using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.Register;

internal sealed class UserCreatedDomainEventHandler(
    ILogger<UserCreatedDomainEventHandler> logger) : IDomainEventHandler<UserCreatedDomainEvent>
{
    public Task Handle(UserCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // TODO: Send welcome email or perform other actions upon user registration.
        logger.LogInformation("User registered with ID: {UserId}", notification.UserId);
        return Task.CompletedTask;
    }
}
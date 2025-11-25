using MediatR;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.Register;

internal sealed class UserRegisteredDomainEventHandler(
    ILogger<UserRegisteredDomainEventHandler> logger) : INotificationHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // TODO: Send welcome email or perform other actions upon user registration.
        logger.LogInformation("User registered with ID: {UserId}", notification.UserId);
        return Task.CompletedTask;
    }
}
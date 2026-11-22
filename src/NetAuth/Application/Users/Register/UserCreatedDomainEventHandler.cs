using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Application.Users.Register;

internal sealed partial class UserCreatedDomainEventHandler(
    ILogger<UserCreatedDomainEventHandler> logger) : IDomainEventHandler<UserCreatedDomainEvent>
{
    public Task Handle(UserCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // TODO: Send welcome email or perform other actions upon user registration.
        LogUserRegistered(notification.UserId);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information, "User registered with ID: {UserId}")]
    partial void LogUserRegistered(Guid userId);
}
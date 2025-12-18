using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.Users.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : ICommand<RegisterResult>;

public sealed record RegisterResult(Guid UserId);

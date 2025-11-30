using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.Users.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string DeviceId
) : ICommand<LoginResult>;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken);
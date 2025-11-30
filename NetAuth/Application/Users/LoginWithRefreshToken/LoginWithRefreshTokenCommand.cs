using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

public sealed record LoginWithRefreshTokenCommand(
    string RefreshToken
) : ICommand<LoginWithRefreshTokenResult>;

public sealed record LoginWithRefreshTokenResult(
    string AccessToken,
    string RefreshToken);
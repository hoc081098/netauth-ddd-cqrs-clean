using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Either<DomainError, LoginResult>>;

public sealed record LoginResult(string AccessToken);

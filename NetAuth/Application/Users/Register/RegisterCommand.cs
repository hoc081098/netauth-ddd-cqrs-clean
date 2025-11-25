using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : ICommand<Either<DomainError, RegisterResult>>;

public sealed record RegisterResult(string AccessToken);

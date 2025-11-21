using LanguageExt;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Data.Authentication;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenProvider jwtTokenProvider
) : ICommandHandler<RegisterCommand, Either<DomainError, RegisterResponse>>
{
    public Task<Either<DomainError, RegisterResponse>> Handle(RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var userEither = from email in Email.Create(command.Email)
            from username in Username.Create(command.Username)
            from password in Password.Create(command.Password)
            let passwordHash = passwordHasher.HashPassword(password)
            select User.Create(email: email, username: username, passwordHash: passwordHash);

        return userEither.BindAsync<DomainError, User, RegisterResponse>(async user =>
        {
            if (!await userRepository.IsEmailUniqueAsync(user.Email, cancellationToken))
            {
                return UsersDomainErrors.User.DuplicateEmail;
            }

            userRepository.Insert(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = jwtTokenProvider.CreateJwtToken(user);
            return new RegisterResponse(AccessToken: accessToken);
        });
    }
}
using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider
) : ICommandHandler<RegisterCommand, Either<DomainError, RegisterResult>>
{
    public Task<Either<DomainError, RegisterResult>> Handle(RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var userInfoEither = from email in Email.Create(command.Email)
            from username in Username.Create(command.Username)
            from password in Password.Create(command.Password)
            select new UserInfo(email, username, password);

        return userInfoEither.BindAsync(info => CheckUniquenessAndInsertAsync(info, cancellationToken));
    }

    private record UserInfo(Email Email, Username Username, Password Password);

    private async Task<Either<DomainError, RegisterResult>> CheckUniquenessAndInsertAsync(
        UserInfo userInfo,
        CancellationToken cancellationToken)
    {
        if (!await userRepository.IsEmailUniqueAsync(userInfo.Email, cancellationToken))
        {
            return UsersDomainErrors.User.DuplicateEmail;
        }

        var passwordHash = passwordHasher.HashPassword(userInfo.Password);
        var user = User.Create(
            email: userInfo.Email,
            username: userInfo.Username,
            passwordHash: passwordHash
        );

        userRepository.Insert(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.CreateJwtToken(user);
        return new RegisterResult(AccessToken: accessToken);
    }
}
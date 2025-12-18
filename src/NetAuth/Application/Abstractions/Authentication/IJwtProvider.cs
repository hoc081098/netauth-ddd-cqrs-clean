using NetAuth.Domain.Users;

namespace NetAuth.Application.Abstractions.Authentication;

public interface IJwtProvider
{
    string Create(User user);
}
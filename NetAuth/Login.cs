using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth;

public static class Login
{
    public record Request(string Email, string Password);

    public record Response(string AccessToken);

    public class Handler(
        IAuthenticationRepository authenticationRepository,
        IJwtProvider jwtProvider
    )
    {
        public async Task<Response> Handle(
            Request request,
            CancellationToken cancellationToken = default)
        {
            var user = await authenticationRepository.Login(email: request.Email,
                password: request.Password,
                cancellationToken);
            var accessToken = jwtProvider.CreateJwtToken(user);
            return new Response(AccessToken: accessToken);
        }
    }
}
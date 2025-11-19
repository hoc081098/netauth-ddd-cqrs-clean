namespace NetAuth;

public static class Login
{
    public record Request(string Email, string Password);

    public record Response(string AccessToken);

    public class Handler(
        IAuthenticationRepository authenticationRepository,
        IJwtTokenProvider jwtTokenProvider
    )
    {
        public async Task<Response> Handle(
            Request request,
            CancellationToken cancellationToken = default)
        {
            var user = await authenticationRepository.Login(email: request.Email,
                password: request.Password,
                cancellationToken);
            var accessToken = jwtTokenProvider.CreateJwtToken(user);
            return new Response(AccessToken: accessToken);
        }
    }
}
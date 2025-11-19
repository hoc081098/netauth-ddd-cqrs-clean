namespace NetAuth;

public static class Register
{
    public record Request(string Email, string Password);

    public record Response(
        Guid Id,
        string Email
    );

    public class Handler(
        IAuthenticationRepository authenticationRepository)
    {
        public async Task<Response> Handle(
            Request request,
            CancellationToken cancellationToken = default)
        {
            var user = await authenticationRepository.Register(email: request.Email,
                password: request.Password,
                cancellationToken);
            return new Response(user.Id, user.Email);
        }
    }
}
// namespace NetAuth;
//
// public static class Register
// {
//     public record Request(string Username, string Email, string Password);
//
//     public record Response(
//         Guid Id,
//         string Username,
//         string Email
//     );
//
//     public class Handler(
//         IAuthenticationRepository authenticationRepository)
//     {
//         public async Task<Response> Handle(
//             Request request,
//             CancellationToken cancellationToken = default)
//         {
//             var user = await authenticationRepository.Register(
//                 username: request.Username,
//                 email: request.Email,
//                 password: request.Password,
//                 cancellationToken);
//             return new Response(user.Id, user.Username, user.Email);
//         }
//     }
// }
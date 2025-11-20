using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NetAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Bind section "Jwt" → JwtConfig
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));
builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();

builder.Services
    .AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
builder.Services.AddSingleton<IAuthenticationRepository, FakeAuthenticationRepository>();
builder.Services.AddScoped<Login.Handler>();
builder.Services.AddScoped<Register.Handler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints

app.MapPost("/auth/login",
        async (Login.Request request, Login.Handler handler) =>
        {
            try
            {
                var response = await handler.Handle(request);
                return Results.Ok(response);
            }
            catch (Exception exception) when (exception is UserNotFoundException or WrongPasswordException)
            {
                return Results.Json(new { error = "invalid_credentials" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }
        })
    .WithName("Login")
    .WithSummary("Authenticate user with email & password.")
    .WithDescription("Returns JWT access token when credentials are valid.")
    .Produces<Login.Response>()
    .Produces(StatusCodes.Status401Unauthorized);
;

app.MapPost("/auth/register",
        async (Register.Request request, Register.Handler handler) =>
        {
            try
            {
                var response = await handler.Handle(request);
                return Results.Created($"/users/{response.Id}", response);
            }
            catch (UserAlreadyExistsException)
            {
                return Results.Json(new { error = "user_already_exists" },
                    statusCode: StatusCodes.Status409Conflict);
            }
        })
    .WithName("Register")
    .WithSummary("Register a new user with email & password.")
    .WithDescription("Creates a new user account.")
    .Produces<Register.Response>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status409Conflict);

app.MapGet("/me", (ClaimsPrincipal user) =>
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email)!;

        return Results.Ok(new MeResponse(Guid.Parse(id), email));
    })
    .RequireAuthorization()
    .WithName("GetCurrentUser")
    .WithSummary("Get current authenticated user.")
    .WithDescription("Returns the details of the currently authenticated user.")
    .Produces<MeResponse>()
    .Produces(StatusCodes.Status401Unauthorized);

app.Run();

public record MeResponse(Guid Id, string Email);
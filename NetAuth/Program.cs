using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LanguageExt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NetAuth;
using NetAuth.Data;
using NetAuth.Domain.Users;
using User = NetAuth.Domain.Users.User;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration);

// Add Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // This code customizes the schema IDs used by Swagger/OpenAPI to represent .NET types.
    // By default, Swagger uses just the class name, which can lead to conflicts if there are multiple classes with the same name
    // in different namespaces or nested classes.
    // This customization changes the schema ID to use the full name of the type,
    // including its namespace, and replaces '+' characters (used in nested class names) with '-' to ensure valid schema IDs.
    o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

    // This code configures Swagger/OpenAPI to recognize JWT Bearer authentication. It defines a security scheme that:
    // - Registers a JWT authentication method in the Swagger UI
    // - Specifies the token should be sent in the HTTP Authorization header
    // - Uses the Bearer authentication scheme
    // - Indicates the expected token format is JWT
    // - Provides a description for users to know they need to enter a JWT token
    // - This definition enables Swagger UI to show an "Authorize" button where users can input their JWT token,
    //   which will then be automatically included in API requests for testing authenticated endpoints.
    o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token in this field",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });

    // This code configures Swagger UI to require JWT authentication for all API endpoints by default.
    // It adds a global security requirement that tells Swagger:
    // - All endpoints should display a lock icon
    // - Users must provide a JWT token to test endpoints
    // - The security scheme references the JWT Bearer authentication defined earlier
    // - The empty array [] means no specific scopes are required—just a valid JWT token.
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            []
        }
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<Login.Handler>();
builder.Services.AddScoped<Register.Handler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var serviceScope = app.Services.CreateScope();
    var appDbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

    var isDbAvailable = appDbContext
        .Database
        .CanConnect();
    Console.WriteLine("isDbAvailable=" + isDbAvailable);
    appDbContext.Database.Migrate();

    await serviceScope.ServiceProvider.GetRequiredService<IUserRepository>()
        .GetByEmailAsync(
            Email.Create("hoc081098@gmail.com").Match(
                Right: Prelude.identity,
                Left: (e) => throw new Exception(e.Message)
            )
        );

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
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

(from username in Username.Create("johndoe")
        from email in Email.Create("hoc081098@gmail.com")
        select User.Create(email: email, username: username, passwordHash: "123456"))
    .Match(
        Right: user => Console.WriteLine("Created user: " + user),
        Left: error => Console.WriteLine("Failed to create user: " + error));

app.Run();

public record MeResponse(Guid Id, string Email);
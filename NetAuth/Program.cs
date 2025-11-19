using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NetAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Bind section "Jwt" → JwtConfig
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtConfig>(jwtSection);

var jwtConfig = jwtSection.Get<JwtConfig>() ?? throw new InvalidOperationException("Missing Jwt config.");
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwtConfig.Issuer,

    ValidateAudience = true,
    ValidAudience = jwtConfig.Audience,

    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),

    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero // don't allow clock skew
};

builder.Services
    .AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = tokenValidationParameters);
builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
builder.Services.AddSingleton<IAuthenticationRepository, FakeAuthenticationRepository>();
builder.Services.AddScoped<Login.Handler>();

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
        catch (UserNotFoundException)
        {
            return Results.Json(new { error = "invalid_credentials" },
                statusCode: StatusCodes.Status401Unauthorized);
        }
    });

app.MapGet("/me", (ClaimsPrincipal user, IJwtTokenProvider tokenProvider) =>
{
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);

    return Results.Ok(new
    {
        Message = "Hello, authenticated user",
        UserId = id,
        Email = email
    });
}).RequireAuthorization();

app.Run();
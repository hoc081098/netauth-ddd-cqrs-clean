# NetAuth Project - GitHub Copilot Instructions

## Project Overview
NetAuth is an ASP.NET Core authentication service built with .NET 9, implementing:
- **Domain-Driven Design (DDD)**
- **CQRS (Command Query Responsibility Segregation)**
- **Clean Architecture**
- **Vertical Slice Architecture**
- **Role-Based Access Control (RBAC)** with **Permission-based authorization**
- **Outbox Pattern** for reliable event processing

## Architecture Layers

### 1. Domain Layer (`/Domain`)
**Core business logic and entities - framework-agnostic.**

#### Guidelines:
- **Entities**: Inherit from `AggregateRoot<TId>` or `Entity<TId>`
- **Value Objects**: Inherit from `ValueObject` and implement `GetAtomicValues()`
- **Domain Errors**: Use `DomainError` class with proper `ErrorType` enum
- **Domain Events**: Implement `IDomainEvent` and use `AddDomainEvent()` in aggregates
- **Factory Methods**: Use static `Create()` methods with `Either<DomainError, T>` return type
- **Validation**: Validate in factory methods using switch expressions with pattern matching

#### Patterns:
```csharp
// Value Object with validation
public sealed class Email : ValueObject
{
    public required string Value { get; init; }
    
    private Email() { }
    
    [Pure]
    public static Either<DomainError, Email> Create(string email) =>
        email switch
        {
            _ when string.IsNullOrWhiteSpace(email) 
                => DomainErrors.Email.NullOrEmpty,
            { Length: > MaxLength } 
                => DomainErrors.Email.TooLong,
            _ when !EmailRegex.Value.IsMatch(email) 
                => DomainErrors.Email.InvalidFormat,
            _ => new Email { Value = email }
        };
    
    protected override IEnumerable<object> GetAtomicValues() => [Value];
    
    public static implicit operator string(Email email) => email.Value;
}

// Aggregate Root
public sealed class User : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<Role> _roles = [];
    public IReadOnlyCollection<Role> Roles => [.._roles];
    
    private User() { } // Required by EF Core
    
    private User(Guid id, Email email, Username username) : base(id)
    {
        Email = email;
        Username = username;
        AddDomainEvent(new UserCreatedDomainEvent(id));
    }
}

// Domain Errors
public static class UsersDomainErrors
{
    public static class Email
    {
        public static readonly DomainError NullOrEmpty = new(
            code: "User.Email.NullOrEmpty",
            message: "Email is required.",
            type: DomainError.ErrorType.Validation);
    }
}
```

### 2. Application Layer (`/Application`)
**Use cases, commands, queries, and business workflows.**

#### Structure:
```
Application/
  ├── Abstractions/          # Interfaces and contracts
  │   ├── Authentication/    # Auth abstractions (IJwtProvider, IUserIdentifierProvider)
  │   ├── Common/            # Common abstractions (IClock)
  │   ├── Cryptography/      # Crypto abstractions (IPasswordHasher, IPasswordHashChecker)
  │   ├── Data/              # Data abstractions (IDbConnectionFactory, IUnitOfWork)
  │   └── Messaging/         # CQRS abstractions (ICommand, IQuery, ICommandHandler, IQueryHandler)
  ├── Core/
  │   ├── Behaviors/         # MediatR pipeline behaviors (ValidationBehavior, UnitOfWorkBehavior)
  │   ├── Exceptions/        # Application exceptions (ValidationError)
  │   └── Extensions/        # Extension methods
  └── Users/                 # Feature: Vertical slice
      ├── Login/
      │   ├── LoginCommand.cs
      │   ├── LoginCommandHandler.cs
      │   └── LoginCommandValidator.cs
      └── Register/
          ├── RegisterCommand.cs
          ├── RegisterCommandHandler.cs
          └── RegisterCommandValidator.cs
```

#### Guidelines:
- **Commands**: Return `Either<DomainError, TResult>` for operations that can fail
- **Queries**: Return direct results or `Option<T>` for nullable results
- **Handlers**: Implement `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`
- **Validators**: Use FluentValidation and inherit from `AbstractValidator<T>`
- **Vertical Slices**: Group related features (Login, Register) with all their components

#### Patterns:
```csharp
// Command
public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Either<DomainError, LoginResult>>;

public sealed record LoginResult(string AccessToken);

// Command Handler
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHashChecker passwordHashChecker,
    IJwtProvider jwtProvider
) : ICommandHandler<LoginCommand, Either<DomainError, LoginResult>>
{
    public Task<Either<DomainError, LoginResult>> Handle(
        LoginCommand command, 
        CancellationToken cancellationToken) =>
        Email.Create(command.Email)
            .MapAsync(email => userRepository.GetByEmailAsync(email, cancellationToken))
            .BindAsync(user => AuthenticateUser(command, user));
    
    private Either<DomainError, LoginResult> AuthenticateUser(LoginCommand command, User? user)
    {
        if (user is null || !user.VerifyPasswordHash(command.Password, passwordHashChecker))
            return UsersDomainErrors.User.InvalidCredentials;
        
        var accessToken = jwtProvider.Create(user);
        return new LoginResult(AccessToken: accessToken);
    }
}

// Validator
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
```

### 3. Infrastructure Layer (`/Infrastructure`)
**Technical implementations and external dependencies.**

#### Guidelines:
- **Repositories**: Implement domain repository interfaces
- **EF Core**: Use `AppDbContext`, configure entities with `IEntityTypeConfiguration<T>`
- **Interceptors**: Use `SaveChangesInterceptor` for cross-cutting concerns (auditing, soft delete)
- **Authentication**: JWT-based with claims transformation for permissions
- **Authorization**: Permission-based using `AuthorizationHandler<PermissionRequirement>`
- **Outbox Pattern**: Process domain events asynchronously with retry logic

#### Patterns:
```csharp
// Repository
internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    
    public void Add(User user) => dbContext.Users.Add(user);
}

// Entity Configuration
internal sealed class UserTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => Email.Create(v).ThrowIfFailed())
            .HasMaxLength(Email.MaxLength)
            .IsRequired();
        
        builder.HasIndex(u => u.Email).IsUnique();
        
        builder.HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity(join => join.ToTable("user_roles"));
    }
}

// Permission Authorization
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.HasClaim(c => 
            c.Type == CustomClaimTypes.Permission && 
            c.Value == requirement.Permission);
        
        if (hasPermission)
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}
```

### 4. Web.Api Layer (`/Web.Api`)
**HTTP endpoints and API contracts.**

#### Structure:
```
Web.Api/
  ├── Contracts/             # API responses and shared types
  ├── Endpoints/             # Minimal API endpoints (organized by feature)
  ├── ExceptionHandlers/     # Global exception handling
  └── Extensions/            # API extensions (endpoint mapping)
```

#### Guidelines:
- **Minimal APIs**: Use endpoint classes implementing `IEndpoint`
- **Contracts**: Define separate `Request` and `Response` records per endpoint
- **Error Handling**: Map `DomainError` to proper HTTP status codes via `CustomResults.Err()`
- **OpenAPI**: Add `.WithName()`, `.WithSummary()`, `.WithDescription()`, `.Produces<T>()`, `.ProducesProblem()`
- **Authorization**: Use `.RequireAuthorization("permission:resource:action")`

#### Patterns:
```csharp
internal sealed class LoginEndpoint : IEndpoint
{
    public sealed record Request(string Email, string Password);
    public sealed record Response(string AccessToken);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(
                    Email: request.Email,
                    Password: request.Password);
                
                var result = await sender.Send(command, cancellationToken);
                
                return result
                    .Select(r => new Response(AccessToken: r.AccessToken))
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("Login")
            .WithSummary("Authenticate user with email & password.")
            .WithDescription("Returns JWT access token when credentials are valid.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
```

## Key Technologies & Libraries

### Core
- **.NET 9** with C# 13
- **ASP.NET Core** Minimal APIs
- **Entity Framework Core** with PostgreSQL
- **MediatR** for CQRS
- **FluentValidation** for validation
- **LanguageExt** for functional programming (Either, Option)

### Security
- **JWT Bearer Authentication**
- **PBKDF2** for password hashing
- **Permission-based authorization**

### Infrastructure
- **Dapper** for raw SQL queries (Outbox)
- **Npgsql** for PostgreSQL
- **Quartz.NET** for background jobs (Outbox processing)

## Coding Standards

### General
- Use **primary constructors** for dependency injection
- Use **file-scoped namespaces**
- Use **collection expressions** `[..collection]` instead of `.ToList()`
- Use **target-typed new** expressions where possible
- Prefer **expression-bodied members** for simple methods
- Use **implicit usings** where appropriate
- Add XML doc comments for public APIs

### Naming Conventions
- **Commands**: `{Action}Command` (e.g., `LoginCommand`, `RegisterCommand`)
- **Handlers**: `{Action}CommandHandler` or `{Action}QueryHandler`
- **Validators**: `{Action}CommandValidator` or `{Action}QueryValidator`
- **Endpoints**: `{Action}Endpoint` (e.g., `LoginEndpoint`)
- **Repositories**: `{Entity}Repository`
- **Domain Events**: `{Entity}{Action}DomainEvent`
- **Errors**: `{Entity}DomainErrors` with nested classes per property

### Error Handling
- Use `Either<DomainError, T>` for operations that can fail at domain level
- Use `Option<T>` for potentially null values
- Map `DomainError` types to HTTP status codes:
  - `Validation` → 400 Bad Request
  - `NotFound` → 404 Not Found
  - `Conflict` → 409 Conflict
  - `Unauthorized` → 401 Unauthorized
  - `Forbidden` → 403 Forbidden
  - `Failure` → 500 Internal Server Error

### Service Lifetimes
- **Scoped**: DbContext, Repositories, UnitOfWork
- **Singleton**: Configuration, Clock, Validators, PasswordHasher, JwtProvider
- **Transient**: Default for handlers (managed by MediatR)

### Validation
- **Domain validation**: In entity factory methods using `Either<DomainError, T>`
- **Application validation**: FluentValidation in command validators
- **Pipeline**: `ValidationBehavior` intercepts and validates before handler execution

## Outbox Pattern

### Purpose
Ensures reliable processing of domain events using transactional outbox pattern.

### Flow
1. Domain events are saved as `OutboxMessage` records in the same transaction as entities
2. Background job (`OutboxProcessorJob`) polls for unprocessed messages
3. Messages are published via MediatR to event handlers
4. Successfully processed messages are marked with `processed_on_utc`
5. Failed messages increment `attempt_count` and store error details

### Guidelines
- Store serialized domain events in `outbox_messages` table
- Use `FOR UPDATE SKIP LOCKED` to prevent concurrent processing
- Process messages in batches with configurable `BatchSize`
- Retry failed messages up to `MaxAttempts`
- Use parallel processing with `MaxDegreeOfParallelism`

## Database Conventions

### Naming
- **Tables**: `snake_case` (e.g., `users`, `outbox_messages`)
- **Columns**: `snake_case` (e.g., `created_on_utc`, `processed_on_utc`)
- **Audit columns**: `created_on_utc`, `modified_on_utc`, `deleted_on_utc`, `is_deleted`

### Patterns
- **Soft delete**: Use `ISoftDeletableEntity` interface and `SoftDeletableEntityInterceptor`
- **Audit**: Use `IAuditableEntity` interface and `AuditableEntityInterceptor`
- **Concurrency**: Use `byte[] Version` with `[Timestamp]` attribute
- **Many-to-many**: Use `.UsingEntity()` to configure join tables

## Testing Approach

### Unit Tests
- Test domain logic in isolation
- Test command/query handlers with mocked dependencies
- Test validators with various input scenarios

### Integration Tests
- Test endpoints with `WebApplicationFactory`
- Test database operations with test containers
- Test authorization policies

## Common Patterns

### Railway-Oriented Programming
Use `Either<L, R>` for chaining operations that can fail:

```csharp
public Task<Either<DomainError, Result>> Handle(Command command, CancellationToken ct) =>
    Email.Create(command.Email)
        .Bind(email => ValidateEmail(email))
        .MapAsync(email => userRepository.GetByEmailAsync(email, ct))
        .BindAsync(user => ProcessUser(user));
```

### Option for Nullable Values
Use `Option<T>` instead of nullable references:

```csharp
public Option<User> FindUser(Guid id) =>
    users.TryGetValue(id, out var user) ? user : Option<User>.None;
```

### Primary Constructors with Dependency Injection
```csharp
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IJwtProvider jwtProvider
) : ICommandHandler<LoginCommand, Either<DomainError, LoginResult>>
{
    // Dependencies are available as fields
}
```

### Switch Expressions for Validation
```csharp
public static Either<DomainError, Email> Create(string email) =>
    email switch
    {
        _ when string.IsNullOrWhiteSpace(email) => Errors.NullOrEmpty,
        { Length: > MaxLength } => Errors.TooLong,
        _ when !IsValid(email) => Errors.InvalidFormat,
        _ => new Email { Value = email }
    };
```

## Security Best Practices

### Authentication
- Use JWT with proper expiration times
- Include user ID and permissions in claims
- Validate tokens on every request

### Authorization
- Use permission-based authorization instead of role checks
- Transform role permissions to claims via `IClaimsTransformation`
- Define permissions as constants: `"permission:{resource}:{action}"`

### Password Security
- Use PBKDF2 with at least 100,000 iterations
- Generate unique random salt per password
- Store format: `{algorithm}.{iterations}.{salt}.{hash}`

### Input Validation
- Validate at domain level (factory methods)
- Validate at application level (FluentValidation)
- Sanitize inputs before storage

## Performance Considerations

### Database
- Use indexes on frequently queried columns (email, username)
- Use `.AsNoTracking()` for read-only queries
- Batch database operations where possible
- Use compiled queries for hot paths

### Caching
- Cache permission lookups
- Cache JWT validation keys
- Use distributed cache for scaled environments

### Outbox Processing
- Tune `BatchSize` and `MaxDegreeOfParallelism` based on load
- Use JSON bulk updates for marking messages as processed
- Monitor `attempt_count` and error patterns

## Common Commands

### Database Migrations
```bash
# Add migration
dotnet ef migrations add <MigrationName> --project NetAuth

# Apply migrations
dotnet ef database update --project NetAuth

# Remove last migration
dotnet ef migrations remove --project NetAuth
```

### Run Application
```bash
# Development
dotnet run --project NetAuth

# Watch mode
dotnet watch --project NetAuth
```

### Docker Compose
```bash
# Start PostgreSQL
docker-compose up -d

# Stop services
docker-compose down
```

## AI Assistant Guidelines

When generating code for this project:

1. **Follow the established architecture** - respect layer boundaries
2. **Use existing patterns** - look at similar features before creating new ones
3. **Implement complete vertical slices** - include command, handler, validator, endpoint
4. **Return Either<DomainError, T>** for failable operations
5. **Add proper validation** at both domain and application levels
6. **Include OpenAPI documentation** for all endpoints
7. **Use primary constructors** for dependency injection
8. **Follow naming conventions** consistently
9. **Add XML comments** for public APIs
10. **Consider security implications** for all authentication/authorization code

## Example: Adding a New Feature

To add a "ChangePassword" feature:

1. **Domain** - Add domain errors if needed
2. **Application** - Create command, handler, validator:
   ```
   Application/Users/ChangePassword/
     ├── ChangePasswordCommand.cs
     ├── ChangePasswordCommandHandler.cs
     └── ChangePasswordCommandValidator.cs
   ```
3. **Endpoint** - Create endpoint class:
   ```
   Web.Api/Endpoints/Users/ChangePasswordEndpoint.cs
   ```
4. **Authorization** - Add permission requirement if needed
5. **Tests** - Add unit and integration tests

---

**Remember**: This is a production-grade authentication service. Security, correctness, and maintainability are paramount.


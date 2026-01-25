# NetAuth - ASP.NET Core Authentication Service

A production-ready authentication service built with .NET 10, implementing Domain-Driven Design (DDD), CQRS, Clean Architecture.

[![Build & Test 🧪](https://github.com/hoc081098/NetAuth/actions/workflows/build.yml/badge.svg)](https://github.com/hoc081098/NetAuth/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/hoc081098/netauth-ddd-cqrs-clean/graph/badge.svg?token=MNmihx6Pxl)](https://codecov.io/gh/hoc081098/netauth-ddd-cqrs-clean)
[![Hits](https://hits.sh/github.com/hoc081098/netauth-ddd-cqrs-clean.svg)](https://hits.sh/github.com/hoc081098/netauth-ddd-cqrs-clean/)

## 🏗️ Architecture

NetAuth follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer** - Core business logic and entities (framework-agnostic)
- **Application Layer** - Use cases, commands, queries, and business workflows
- **Infrastructure Layer** - Technical implementations and external dependencies
- **Web.Api Layer** - HTTP endpoints and API contracts

## ✨ Features

- ✅ **User Registration & Authentication**
- ✅ **JWT-based Authentication** with access tokens
- ✅ **Refresh Token Rotation** with automatic revocation on reuse detection
- ✅ **Device Binding** for enhanced security
- ✅ **Permission-Based Authorization** (RBAC with fine-grained permissions)
- ✅ **Audit Logging** via domain events
- ✅ **Outbox Pattern** for reliable event processing (batching, SKIP LOCKED, retry with max attempts)
- ✅ **Rate Limiting** on authentication endpoints
- ✅ **Health Checks** for database and Redis
- ✅ **OpenAPI/Swagger** documentation
- ✅ **Hybrid Cache** for permission lookups (memory + Redis)
- ✅ **API Versioning** (v1, v2) with grouped endpoints

## 🛠️ Technology Stack

### Core
- **.NET 10** with C# 14
- **ASP.NET Core** Minimal APIs
- **Entity Framework Core** with PostgreSQL
- **MediatR** for CQRS
- **FluentValidation** for validation
- **LanguageExt** for functional programming (Either, Option)
- **Serilog** for structured logging (Console/File/Seq)

### Security
- **JWT Bearer Authentication**
- **PBKDF2** for password hashing
- **Permission-based authorization**

### Infrastructure
- **Dapper** for raw SQL queries
- **Npgsql** for PostgreSQL
- **Quartz.NET** for background jobs
- **Redis** for distributed caching

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 16+
- Redis 7+
- Docker & Docker Compose (optional)

### Running with Docker Compose

```bash
# Start PostgreSQL and Redis (compose.yaml)
docker compose up -d

# Apply database migrations
dotnet ef database update --project src/NetAuth/NetAuth.csproj --startup-project src/NetAuth

# Run the application
dotnet run --project src/NetAuth/NetAuth.csproj
```

### Running Locally

```bash
# Update connection strings in appsettings.Development.json
# Or copy .env.example to .env and fill Jwt__SecretKey, connection strings, Seq URL, etc.
# Then run:
dotnet run --project src/NetAuth/NetAuth.csproj
```

The API will be available at:
- HTTPS: `https://localhost:7169`
- HTTP: `http://localhost:5215`
- Swagger UI: `https://localhost:7169/swagger`

## 📁 Project Structure

```
NetAuth/
├── Domain/                    # Core business logic
│   ├── Core/                 # Base classes and abstractions
│   │   ├── Abstractions/     # Interfaces (IAuditableEntity, ISoftDeletableEntity)
│   │   ├── Events/           # Domain event base classes
│   │   └── Primitives/       # Entity, AggregateRoot, ValueObject, DomainError
│   ├── Users/                # User bounded context
│   │   ├── User.cs           # User aggregate root
│   │   ├── Email.cs          # Email value object
│   │   ├── Username.cs       # Username value object
│   │   ├── Password.cs       # Password value object
│   │   ├── RefreshToken.cs   # Refresh token entity
│   │   ├── Role.cs           # Role entity with permissions
│   │   └── UsersDomainErrors.cs  # Domain errors (static readonly fields)
│   └── TodoItems/            # TodoItem bounded context
│       ├── TodoItem.cs       # TodoItem aggregate root
│       ├── TodoTitle.cs      # TodoTitle value object
│       ├── TodoDescription.cs # TodoDescription value object
│       └── TodoItemDomainErrors.cs  # Domain errors
├── Application/              # Use cases and workflows
│   ├── Abstractions/         # Application interfaces
│   │   ├── Authentication/   # Auth abstractions (IJwtProvider, IUserContext)
│   │   ├── Common/           # Common abstractions (IClock)
│   │   ├── Cryptography/     # Password hashing
│   │   ├── Data/             # Repository, UnitOfWork
│   │   └── Messaging/        # CQRS abstractions (ICommand, IQuery)
│   ├── Core/
│   │   ├── Behaviors/        # MediatR pipeline behaviors (Validation, Logging)
│   │   ├── Exceptions/       # Application exceptions
│   │   └── Extensions/       # Extension methods
│   ├── Users/                # User feature slices
│   │   ├── Login/            # Login command, handler, validator
│   │   ├── LoginWithRefreshToken/
│   │   ├── Register/         # Registration command, handler, validator
│   │   ├── SetUserRoles/     # Role management
│   │   ├── GetRoles/         # Query all roles
│   │   └── GetUserRoles/     # Query user's roles
│   └── TodoItems/            # TodoItem feature slices
│       ├── Create/           # Create todo item
│       ├── Update/           # Update todo item
│       ├── Complete/         # Mark as completed
│       ├── MarkAsIncomplete/ # Undo completion
│       └── Get/              # Query todo items
├── Infrastructure/           # Technical implementations
│   ├── Authentication/       # JWT provider, refresh token generator
│   ├── Authorization/        # Permission service, policies
│   ├── Configurations/       # EF Core entity configurations
│   ├── Cryptography/         # Password hasher
│   ├── Interceptors/         # EF Core interceptors (audit, soft delete)
│   ├── Migrations/           # EF Core migrations
│   ├── Outbox/               # Outbox pattern implementation
│   └── Repositories/         # Repository implementations
└── Web.Api/                  # HTTP layer
    ├── Endpoints/            # Minimal API endpoints
    ├── ExceptionHandlers/    # Global exception handling
    ├── Extensions/           # API extensions
    └── OpenApi/              # OpenAPI configuration
```

## 🎯 Design Patterns & Principles

### Domain-Driven Design (DDD)
- **Aggregates**: User is the aggregate root managing RefreshTokens
- **Value Objects**: Email, Username, Password with validation
- **Domain Events**: UserCreatedDomainEvent, UserRolesChangedDomainEvent, RefreshTokenCreated/Rotated/ReuseDetected/DeviceMismatchDetected/ExpiredUsage/ChainCompromised
- **Domain Errors**: Immutable error types using `static readonly` fields for performance

### CQRS (Command Query Responsibility Segregation)
- **Commands**: Operations that change state (Login, Register)
- **Queries**: Read operations (future: GetUserProfile)
- **Handlers**: Separate handler per command/query
- **Validation**: FluentValidation in pipeline behavior

### Clean Architecture
- **Dependency Rule**: Dependencies point inward (Infrastructure → Application → Domain)
- **Framework Independence**: Domain layer has no external dependencies
- **Testability**: Clear boundaries enable easy unit testing

### Functional Programming
- **Railway-Oriented Programming**: Using `Either<DomainError, T>` for operations that can fail
- **Option Type**: Using `Option<T>` for nullable values
- **Monadic Composition**: Chaining operations with `Bind`, `Map`, `MapAsync`

### Outbox Pattern
Ensures reliable event processing:
1. Domain events saved as `OutboxMessage` in same transaction
2. Quartz job processes messages on an interval (`Outbox:Interval`) with batch size and max attempts
3. Uses `FOR UPDATE SKIP LOCKED` to avoid double processing
4. Parallel publish with a capped degree of parallelism and bulk update of processed rows

## 🔒 Security Features

### Password Security
- **PBKDF2** algorithm with 80,000 iterations (v1, salted, constant-time verify)
- **Unique random salt** per password
- **Versioned storage format**: `v1.{iterations}.{salt}.{hash}`

### Refresh Token Security
- **Token Rotation**: New token issued on every refresh
- **Reuse Detection**: Automatic chain revocation on suspicious activity
- **Device Binding**: Tokens bound to specific devices
- **Expiration**: Configurable token lifetime (default config 7 days; development config shorter)
- **Audit Trail**: Complete history via domain events

### Authorization
- **Permission-Based**: Fine-grained permissions (`permission:resource:action`)
- **Claims Transformation**: Role permissions loaded and cached
- **Policy-Based**: Custom authorization policies

## 📝 Domain Errors Best Practice

All domain and validation errors use `static readonly` fields for optimal performance:

```csharp
public static class UsersDomainErrors
{
    public static class Email
    {
        // ✅ CORRECT - static readonly field (single allocation)
        public static readonly DomainError InvalidFormat = new(
            code: "User.Email.InvalidFormat",
            message: "The email format is invalid.",
            type: DomainError.ErrorType.Validation);
        
        // ❌ WRONG - property (new allocation on every access)
        // public static DomainError InvalidFormat => new(...);
    }
}
```

**Benefits:**
- Single allocation per error, no per-call allocations
- Thread-safe by CLR static initialization guarantee
- Clear, centralized error catalog

## 🧪 Testing

### Test Structure

```
tests/
├── UnitTests/                    # 459 tests
│   ├── Domain/
│   │   ├── Core/Primitives/     # ValueObject, Entity, AggregateRoot, DomainError tests
│   │   ├── Users/               # Email, Username, Password, User, RefreshToken tests
│   │   └── TodoItems/           # TodoItem, TodoTitle, TodoDescription tests
│   └── Application/
│       ├── Core/                # ValidationError, DateTimeExtensions tests
│       ├── Users/               # Login, Register, RefreshToken handlers & validators
│       └── TodoItems/           # Create, Update, Complete, MarkAsIncomplete handlers & validators
│
└── ArchitectureTests/           # 6 tests
    └── LayerTest.cs             # Domain, Application, Infrastructure, WebApi layer rules
```

### Unit Tests
- Domain logic (value objects, entities, aggregates)
- Command/query handlers with mocked dependencies (NSubstitute)
- Validators with FluentValidation test helpers
- Uses xUnit and LanguageExt.UnitTesting

### Architecture Tests
- Dependency rules enforcement (NetArchTest)
- Layer isolation verification
- Naming conventions

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test category
dotnet test --filter "FullyQualifiedName~UnitTests.Domain"
```

## 🔧 Configuration

### First-Time Setup (Required)

Before running the application, you must configure the JWT Secret Key. This key is required for generating and validating JWT tokens.

#### Option 1: Using User Secrets (Recommended for Development)

```bash
# Navigate to the project directory
cd src/NetAuth

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set the JWT Secret Key (minimum 32 characters)
dotnet user-secrets set "Jwt:SecretKey" "your-super-secret-key-here-minimum-32-characters-long"
```

#### Option 2: Using Environment Variables (Recommended for Production)

```bash
# Linux/macOS
export Jwt__SecretKey="your-super-secret-key-here-minimum-32-characters-long"

# Windows (PowerShell)
$env:Jwt__SecretKey="your-super-secret-key-here-minimum-32-characters-long"

# Windows (Command Prompt)
set Jwt__SecretKey=your-super-secret-key-here-minimum-32-characters-long
```

#### Option 3: Using Docker Compose

Add to your compose file (e.g., `compose.yaml`):

```yaml
services:
  netauth:
    environment:
      - Jwt__SecretKey=${JWT_SECRET_KEY}
```

Then set the environment variable before running Docker Compose:

```bash
export JWT_SECRET_KEY="your-super-secret-key-here-minimum-32-characters-long"
docker compose up -d
```

> ⚠️ **Security Notes:**
> - Never commit the actual secret key to source control
> - Use a cryptographically secure random key (minimum 32 bytes / 256 bits for HS256)
> - Rotate keys periodically in production
> - Use different keys for each environment (dev, staging, production)

### Configuration Reference

Key configuration sections in `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "",
    "Issuer": "hoc081098",
    "Audience": "MyAppClients",
    "Expiration": "00:10:00",
    "RefreshTokenExpiration": "7.00:00:00"
  },
  "Outbox": {
    "Interval": "00:00:10",
    "BatchSize": 500,
    "MaxAttempts": 3,
    "CleanupRetention": "30.00:00:00",
    "CleanupBatchSize": 5000
  }
}
```
> Development settings override JWT expirations (access: 1 hour, refresh: 2 hours) and include localhost connection strings for PostgreSQL and Redis.

## 📚 API Documentation

Visit `/swagger` for interactive API documentation.
> Available in Development environment (enabled when `ASPNETCORE_ENVIRONMENT=Development`).

### Key Endpoints (versioned)

#### Authentication
- `POST /v1/auth/register` - Register new user
- `POST /v1/auth/login` - Login with email/password
- `POST /v1/auth/refresh` - Refresh access token
> Replace `v1` with `v2` for the alternate API version.

### Rate Limiting

Authentication endpoints are protected with rate limiting:
- **/auth/login**: Sliding window 5 requests per 20s per IP
- **/auth/register**: Sliding window 3 requests per minute per IP
- **/auth/refresh**: Sliding window 20 requests per minute per IP
- **Global**: Sliding window 100 requests per minute per IP for all other endpoints

## 🎯 Performance Considerations

- Static readonly domain errors (zero allocation per access)
- Outbox processor uses SKIP LOCKED + bulk updates + limited parallel publish
- Permission caching via HybridCache (memory + Redis)
- Rate limiting on auth endpoints and global limiter

## 📊 Observability

### Health Checks
- PostgreSQL database connectivity
- Redis connectivity
- DbContext health
- Outbox backlog/processing health

### Logging
- Structured logging with Serilog
- **Correlation ID tracking** for request tracing (X-Correlation-Id header)
- Audit logging via domain events
- Request/response logging with timing

## 🛣️ Roadmap

### ✅ Completed
- [x] Unit tests and architecture tests in place (465 tests: 459 Unit + 6 Architecture)
- [x] CI/CD pipeline with GitHub Actions
- [x] Correlation ID logging for request tracing
- [x] JWT SecretKey configuration with documentation
- [x] XML documentation for complex business logic
- [x] API versioning (v1, v2)
- [x] Integration tests for critical flows (24 tests: Register, Login, RefreshToken, SetUserRoles, TodoItem CRUD)

### 🔄 In Progress / Planned
- [ ] Implement user profile management
- [ ] Add email verification
- [ ] Implement password reset flow
- [ ] Add account lockout after failed attempts
- [ ] Implement MFA (Multi-Factor Authentication)
- [ ] Add distributed tracing with OpenTelemetry
- [ ] Add GraphQL endpoint
- [ ] Add response compression and caching
- [ ] Implement pagination and sorting

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using .NET 10 and Clean Architecture principles**

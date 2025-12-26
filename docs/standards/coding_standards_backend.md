# Backend Coding Standards - ExoAuth

> Lies diese Datei bevor du Code schreibst.

---

## Task File Workflow

**Bevor du Code schreibst, lies IMMER:**
1. `docs/standards/task_standards_backend.md` - Aktueller Projekt Stand, File Tree, Packages
2. Das aktuelle Task File in `docs/tasks/` - Was genau gebaut werden soll

**Während der Implementierung:**
- Halte das Task File IMMER aktuell (neue Files, geänderte Files, neue Packages)
- Das Task File ist die Single Source of Truth

**Nach Completion:**
- Error Codes hier in dieser Datei hinzufügen
- File Tree in `task_standards_backend.md` aktualisieren
- Packages in `task_standards_backend.md` aktualisieren

---

## Projekt Struktur

```
backend/
├── src/
│   ├── ExoAuth.Domain/          # Entities, Value Objects, Domain Events
│   ├── ExoAuth.Application/     # CQRS Commands/Queries, Interfaces
│   ├── ExoAuth.Infrastructure/  # EF Core, Redis, RabbitMQ
│   └── ExoAuth.Api/             # Controllers, Middleware
└── tests/
    ├── ExoAuth.UnitTests/
    └── ExoAuth.IntegrationTests/
```

---

## Naming Conventions

| Was | Convention | Beispiel |
|-----|------------|----------|
| Classes | PascalCase | `UserService` |
| Interfaces | I + PascalCase | `IUserRepository` |
| Methods | PascalCase | `GetUserById` |
| Properties | PascalCase | `FirstName` |
| Private fields | _camelCase | `_userRepository` |
| Constants | PascalCase | `MaxRetryCount` |
| Enums | PascalCase | `UserStatus` |
| Files | PascalCase.cs | `UserService.cs` |
| Folders | PascalCase | `Commands` |

---

## DO's

### Allgemein
- ✅ Verwende `sealed` für Klassen die nicht vererbt werden
- ✅ Verwende `record` für DTOs und Commands/Queries
- ✅ Verwende `readonly` wo möglich
- ✅ Verwende `CancellationToken` in async Methoden
- ✅ Verwende Dependency Injection
- ✅ Schreibe Unit Tests für Handler

### Domain Layer
- ✅ Entities haben private setters
- ✅ Entities haben Factory Methods (`Create`, `Update`)
- ✅ Value Objects sind immutable
- ✅ Domain Events für wichtige Änderungen

### Application Layer
- ✅ Ein Command/Query pro File
- ✅ Ein Handler pro Command/Query
- ✅ Validators für Input Validation
- ✅ Interfaces für externe Dependencies

### Infrastructure Layer
- ✅ Repository Pattern für Datenzugriff
- ✅ EF Configurations in separaten Files
- ✅ Migrations mit sprechenden Namen

### API Layer
- ✅ Thin Controllers (nur Mediator aufrufen)
- ✅ Proper HTTP Status Codes
- ✅ Swagger Dokumentation
- ✅ Globale Exception Handling

---

## DON'Ts

### Allgemein
- ❌ Keine Business Logic in Controllers
- ❌ Keine `public` fields
- ❌ Keine Magic Strings (verwende Constants)
- ❌ Keine verschachtelten try-catch
- ❌ Keine `async void` (außer Event Handler)

### Domain Layer
- ❌ Keine Abhängigkeiten zu anderen Layers
- ❌ Keine EF Core Abhängigkeiten
- ❌ Keine DTOs im Domain

### Application Layer
- ❌ Keine direkten DB Calls (nur über Interfaces)
- ❌ Keine HTTP Context Zugriffe
- ❌ Keine Infrastructure Abhängigkeiten

### Infrastructure Layer
- ❌ Keine Business Logic
- ❌ Keine Domain Logic

### API Layer
- ❌ Keine Business Logic
- ❌ Keine direkten Repository Calls
- ❌ Keine EF Core DbContext Injection in Controller

---

## Code Beispiele

### Entity (Richtig)
```csharp
public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public static User Create(string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateEmail(string newEmail)
    {
        Email = newEmail;
    }
}
```

### Command + Handler (Richtig)
```csharp
// Command
public sealed record CreateUserCommand(
    string Email,
    string Password
) : ICommand<Guid>;

// Handler
public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async ValueTask<Guid> Handle(
        CreateUserCommand command,
        CancellationToken ct)
    {
        var hash = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hash);

        await _userRepository.AddAsync(user, ct);

        return user.Id;
    }
}

// Validator
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
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

### Controller (Richtig)
```csharp
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Email, request.Password);
        var userId = await _mediator.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = userId }, userId);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.Send(query, ct);

        return user is null ? NotFound() : Ok(user);
    }
}
```

---

## Folder Struktur für Features

```
Features/
└── Users/
    ├── Commands/
    │   ├── CreateUser/
    │   │   ├── CreateUserCommand.cs
    │   │   ├── CreateUserHandler.cs
    │   │   └── CreateUserValidator.cs
    │   └── UpdateUser/
    │       ├── UpdateUserCommand.cs
    │       ├── UpdateUserHandler.cs
    │       └── UpdateUserValidator.cs
    └── Queries/
        ├── GetUserById/
        │   ├── GetUserByIdQuery.cs
        │   └── GetUserByIdHandler.cs
        └── GetAllUsers/
            ├── GetAllUsersQuery.cs
            └── GetAllUsersHandler.cs
```

---

## Error Handling

```csharp
// Custom Exception
public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid userId)
        : base($"User with ID {userId} was not found.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}

// In Handler
public async ValueTask<UserDto> Handle(GetUserByIdQuery query, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(query.Id, ct);

    if (user is null)
        throw new UserNotFoundException(query.Id);

    return new UserDto(user.Id, user.Email);
}
```

---

## API Response Standard

Alle API Endpoints verwenden `ApiResponse<T>` als Wrapper:

```csharp
// Erfolgreiche Response
return ApiResponse<UserDto>.Success(userDto);
return ApiResponse<UserDto>.Success(userDto, "User created successfully");

// Mit Pagination
return ApiResponse<List<UserDto>>.Success(users, pagination);

// Error Response
return ApiResponse<object>.Error(
    statusCode: 400,
    message: "Validation failed",
    errors: new List<ApiError> { new("email", ErrorCodes.VALIDATION_REQUIRED, "Email is required") }
);
```

### Response Schema

```json
{
  "status": "success | error",
  "statusCode": 200,
  "message": "OK",
  "data": { ... },
  "meta": {
    "timestamp": "2024-12-24T10:00:00Z",
    "requestId": "uuid",
    "pagination": {
      "cursor": "base64...",
      "nextCursor": "base64...",
      "hasMore": true,
      "pageSize": 20
    }
  },
  "errors": [{ "field": "email", "code": "VALIDATION_REQUIRED", "message": "..." }]
}
```

---

## Error Codes

Verwende die vordefinierten Error Codes aus `ErrorCodes.cs`:

### Validation Errors (400)

| Code | HTTP Status | Verwendung |
|------|-------------|------------|
| `VALIDATION_REQUIRED` | 400 | Pflichtfeld fehlt |
| `VALIDATION_INVALID_FORMAT` | 400 | Ungültiges Format (Email, etc.) |
| `VALIDATION_MIN_LENGTH` | 400 | Mindestlänge nicht erreicht |
| `VALIDATION_MAX_LENGTH` | 400 | Maximallänge überschritten |

### Auth Errors (401/403/429)

| Code | HTTP Status | Verwendung |
|------|-------------|------------|
| `AUTH_UNAUTHORIZED` | 401 | Nicht authentifiziert |
| `AUTH_FORBIDDEN` | 403 | Keine Berechtigung |
| `AUTH_TOKEN_EXPIRED` | 401 | Access Token abgelaufen |
| `AUTH_TOKEN_INVALID` | 401 | Token ungültig |
| `AUTH_INVALID_CREDENTIALS` | 401 | E-Mail oder Passwort falsch |
| `AUTH_USER_INACTIVE` | 401 | User ist deaktiviert |
| `AUTH_REFRESH_TOKEN_INVALID` | 401 | Refresh Token ungültig oder revoked |
| `AUTH_INVITE_EXPIRED` | 400 | Einladung abgelaufen (24h) |
| `AUTH_INVITE_INVALID` | 400 | Einladungs-Token ungültig |
| `AUTH_REGISTRATION_CLOSED` | 400 | Registration nur für ersten User |
| `AUTH_EMAIL_EXISTS` | 409 | E-Mail bereits vergeben |
| `AUTH_PASSWORD_TOO_WEAK` | 400 | Passwort erfüllt Anforderungen nicht |
| `AUTH_TOO_MANY_ATTEMPTS` | 429 | Zu viele fehlgeschlagene Login-Versuche |

### System Errors (400/403/404)

| Code | HTTP Status | Verwendung |
|------|-------------|------------|
| `SYSTEM_USER_NOT_FOUND` | 404 | SystemUser nicht gefunden |
| `SYSTEM_PERMISSION_NOT_FOUND` | 404 | Permission nicht gefunden |
| `SYSTEM_LAST_PERMISSION_HOLDER` | 400 | Kann nicht entfernen - letzter User mit dieser Permission |
| `SYSTEM_CANNOT_DELETE_SELF` | 400 | Kann sich selbst nicht löschen |
| `SYSTEM_FORBIDDEN` | 403 | Keine Berechtigung für System-Aktion |

### General Errors

| Code | HTTP Status | Verwendung |
|------|-------------|------------|
| `RESOURCE_NOT_FOUND` | 404 | Resource nicht gefunden |
| `RATE_LIMIT_EXCEEDED` | 429 | Rate Limit überschritten |
| `INTERNAL_ERROR` | 500 | Interner Serverfehler |

---

## Rate Limiting

Verwende das `[RateLimit]` Attribut auf Controller oder Actions:

```csharp
// Standard Rate Limit (100 requests/minute aus appsettings)
[RateLimit]
public async Task<IActionResult> GetAll() { ... }

// Custom Rate Limit
[RateLimit(RequestsPerMinute = 10)]
public async Task<IActionResult> Login() { ... }
```

Rate Limit wird per IP in Redis gespeichert (Sliding Window).

---

## Caching

Verwende `ICacheService` für Redis Caching:

```csharp
public class MyHandler
{
    private readonly ICacheService _cache;

    public async ValueTask<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
    {
        var cacheKey = $"user:{query.Id}";

        // Try cache first
        var cached = await _cache.GetAsync<UserDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // Fetch from DB
        var user = await _repository.GetByIdAsync(query.Id, ct);
        if (user is null) return null;

        var dto = new UserDto(user);

        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);

        return dto;
    }
}
```

---

## Message Bus

Verwende `IMessageBus` für async Events:

```csharp
public class CreateUserHandler
{
    private readonly IMessageBus _messageBus;

    public async ValueTask<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var user = User.Create(cmd.Email, cmd.PasswordHash);
        await _repository.AddAsync(user, ct);

        // Publish event
        await _messageBus.PublishAsync(
            "user.created",
            new { UserId = user.Id, Email = user.Email },
            ct
        );

        return user.Id;
    }
}
```

---

## DateTime Handling

**IMMER UTC verwenden!** Nutze `IDateTimeProvider`:

```csharp
public class MyHandler
{
    private readonly IDateTimeProvider _dateTime;

    public async ValueTask Handle(...)
    {
        var now = _dateTime.UtcNow;  // ✅ Richtig
        // var now = DateTime.Now;   // ❌ Falsch - verwendet lokale Zeit!
    }
}
```

---

## Logging

Serilog ist konfiguriert. Nutze strukturiertes Logging:

```csharp
// Via ILogger<T> Injection
public class MyHandler
{
    private readonly ILogger<MyHandler> _logger;

    public async ValueTask Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        _logger.LogInformation("Creating user with email {Email}", cmd.Email);

        try
        {
            // ...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Email}", cmd.Email);
            throw;
        }
    }
}
```

**Wichtig:** Sensitive Daten (Passwords, Tokens) werden automatisch vom `RequestLoggingMiddleware` gefiltert.

---

## Base Controller

Alle API Controller erben von `ApiControllerBase`:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ApiControllerBase
{
    // Inherited: IMediator _mediator, CurrentUserId, CurrentUserEmail

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, ct);
        return Ok(ApiResponse<UserDto>.Success(result));
    }
}
```

---

## Health Checks

Health Check Endpoint: `/health`

Custom Health Checks hinzufügen:

```csharp
// In DependencyInjection.cs
services.AddHealthChecks()
    .AddCheck<MyCustomHealthCheck>("my-service");

// Custom Health Check
public class MyCustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // Check logic...
        return HealthCheckResult.Healthy("All good");
    }
}
```

---

## Letzte Änderung

- **Datum:** 2025-12-26
- **Added:** Task File Workflow Section, Auth Error Codes, System Error Codes
- **Updated:** Error Codes in kategorisierte Sections aufgeteilt

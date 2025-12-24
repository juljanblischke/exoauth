# Backend Coding Standards - ExoAuth

> Lies diese Datei bevor du Code schreibst.

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

## Letzte Änderung

- **Datum:** 2024-12-24

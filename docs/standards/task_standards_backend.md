# Backend Task Standards - ExoAuth

> **MEGA BRAIN** - Lies diese Datei KOMPLETT bevor du einen Task erstellst.

---

## Wichtige Regeln für Task Files

### Task File Pflege (KRITISCH!)

**Während der Implementierung MUSS das Task File IMMER aktuell gehalten werden:**

1. **Neue Files erstellt?** → Sofort in Section "Files zu erstellen" eintragen mit vollem Pfad
2. **Bestehende Files geändert?** → Sofort in Section "Files zu ändern" eintragen
3. **Neue Packages installiert?** → Sofort in Section "Neue Packages" eintragen
4. **Implementierung abgeschlossen?** → Checkbox abhaken `[x]`
5. **Tests geschrieben?** → Mit Anzahl dokumentieren (z.B. "106 Tests ✅")
6. **Helper/Utilities erstellt?** → Auch dokumentieren!

**Warum?**
- Das Task File ist die **Single Source of Truth**
- Standards Update am Ende wird einfacher (File Tree aktualisieren)
- Code Review wird einfacher
- Andere (und du selbst später) müssen wissen was geändert wurde

---

## Task Vorlage

Wenn ein neues Feature geplant wird, MUSS dieser Template verwendet werden:

```markdown
# Task: [Feature Name]

## 1. Übersicht
**Was wird gebaut?**
[Kurze Beschreibung]

**Warum?**
[Business Grund / User Need]

## 2. User Experience / Anforderungen

### User Stories
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]

### Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2
- [ ] Kriterium 3

### Edge Cases / Error Handling
- Was passiert wenn...?
- Was passiert wenn...?

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| POST | /api/... | `{ ... }` | `{ ... }` | ... |
| GET | /api/... | - | `{ ... }` | ... |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `ERROR_CODE_NAME` | 4xx | Beschreibung des Fehlers |

> ⚠️ **Nach Completion:** Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities
| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| ... | ... | ... |

### Migrations
- [ ] Migration Name: `Add{EntityName}`

## 6. Files zu erstellen

### Domain Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Domain/Entities/...` | ... |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/.../Commands/.../...Command.cs` | ... |
| Handler | `src/ExoAuth.Application/Features/.../Commands/.../...Handler.cs` | ... |
| Validator | `src/ExoAuth.Application/Features/.../Commands/.../...Validator.cs` | ... |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Infrastructure/...` | ... |

### API Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Controller | `src/ExoAuth.Api/Controllers/...Controller.cs` | ... |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/.../AppDbContext.cs` | DbSet hinzufügen |
| `src/.../DependencyInjection.cs` | Service registrieren |

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| ... | ... | ExoAuth.XXX | ... |

## 9. Implementation Reihenfolge

1. [ ] **Domain**: Entity erstellen
2. [ ] **Infrastructure**: Configuration + DbContext + Migration
3. [ ] **Application**: Commands/Queries + Handlers + Validators
4. [ ] **API**: Controller + Endpoints
5. [ ] **Tests**: Unit Tests schreiben
6. [ ] **Standards updaten**: task_standards_backend.md + coding_standards_backend.md aktualisieren

## 10. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Features/.../...Tests.cs` | ... | ... |
| `tests/ExoAuth.UnitTests/Services/.../...Tests.cs` | ... | ... |

### Test Helpers (falls erstellt)
| Datei | Beschreibung |
|-------|--------------|
| `tests/ExoAuth.UnitTests/Helpers/...` | ... |

## 11. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `task_standards_backend.md` aktualisiert (File Tree, Packages)
- [ ] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [ ] Code reviewed
```

---

## Aktueller Projekt Stand

### File Tree (Was existiert)

```
backend/
├── ExoAuth.sln
│
├── src/
│   ├── ExoAuth.Domain/
│   │   ├── ExoAuth.Domain.csproj
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs                    ✅
│   │   │   ├── SystemUser.cs                    ✅
│   │   │   ├── SystemPermission.cs              ✅
│   │   │   ├── SystemUserPermission.cs          ✅
│   │   │   ├── SystemAuditLog.cs                ✅
│   │   │   ├── SystemInvite.cs                  ✅
│   │   │   └── RefreshToken.cs                  ✅
│   │   ├── Enums/
│   │   │   └── UserType.cs                      ✅
│   │   ├── Constants/
│   │   │   └── SystemPermissions.cs             ✅
│   │   ├── ValueObjects/                        [LEER]
│   │   ├── Exceptions/                          [LEER]
│   │   └── Events/                              [LEER]
│   │
│   ├── ExoAuth.Application/
│   │   ├── ExoAuth.Application.csproj
│   │   ├── DependencyInjection.cs               ✅
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICacheService.cs             ✅
│   │   │   │   ├── IMessageBus.cs               ✅
│   │   │   │   ├── ICurrentUserService.cs       ✅
│   │   │   │   ├── IDateTimeProvider.cs         ✅
│   │   │   │   ├── IAppDbContext.cs             ✅
│   │   │   │   ├── IPasswordHasher.cs           ✅
│   │   │   │   ├── ITokenService.cs             ✅
│   │   │   │   ├── IAuditService.cs             ✅
│   │   │   │   ├── IEmailService.cs             ✅
│   │   │   │   ├── ISystemUserRepository.cs     ✅
│   │   │   │   ├── IPermissionCacheService.cs   ✅
│   │   │   │   ├── IBruteForceProtectionService.cs ✅
│   │   │   │   ├── ITokenBlacklistService.cs    ✅
│   │   │   │   └── IEmailTemplateService.cs     ✅
│   │   │   ├── Behaviors/
│   │   │   │   └── ValidationBehavior.cs        ✅
│   │   │   ├── Messages/
│   │   │   │   └── EmailMessage.cs              ✅
│   │   │   ├── Mappings/                        [LEER]
│   │   │   └── Models/
│   │   │       ├── ApiResponse.cs               ✅
│   │   │       ├── ApiError.cs                  ✅
│   │   │       ├── ErrorCodes.cs                ✅
│   │   │       ├── PaginationMeta.cs            ✅
│   │   │       └── CursorPagedList.cs           ✅
│   │   └── Features/
│   │       ├── Auth/
│   │       │   ├── Commands/
│   │       │   │   ├── Register/
│   │       │   │   │   ├── RegisterCommand.cs       ✅
│   │       │   │   │   ├── RegisterHandler.cs       ✅
│   │       │   │   │   └── RegisterValidator.cs     ✅
│   │       │   │   ├── Login/
│   │       │   │   │   ├── LoginCommand.cs          ✅
│   │       │   │   │   ├── LoginHandler.cs          ✅
│   │       │   │   │   └── LoginValidator.cs        ✅
│   │       │   │   ├── RefreshToken/
│   │       │   │   │   ├── RefreshTokenCommand.cs   ✅
│   │       │   │   │   └── RefreshTokenHandler.cs   ✅
│   │       │   │   ├── Logout/
│   │       │   │   │   ├── LogoutCommand.cs         ✅
│   │       │   │   │   └── LogoutHandler.cs         ✅
│   │       │   │   └── AcceptInvite/
│   │       │   │       ├── AcceptInviteCommand.cs   ✅
│   │       │   │       ├── AcceptInviteHandler.cs   ✅
│   │       │   │       └── AcceptInviteValidator.cs ✅
│   │       │   ├── Queries/
│   │       │   │   └── GetCurrentUser/
│   │       │   │       ├── GetCurrentUserQuery.cs   ✅
│   │       │   │       └── GetCurrentUserHandler.cs ✅
│   │       │   └── Models/
│   │       │       └── AuthResponse.cs              ✅
│   │       ├── SystemUsers/
│   │       │   ├── Commands/
│   │       │   │   ├── InviteSystemUser/
│   │       │   │   │   ├── InviteSystemUserCommand.cs   ✅
│   │       │   │   │   ├── InviteSystemUserHandler.cs   ✅
│   │       │   │   │   └── InviteSystemUserValidator.cs ✅
│   │       │   │   ├── UpdateSystemUser/
│   │       │   │   │   ├── UpdateSystemUserCommand.cs   ✅
│   │       │   │   │   ├── UpdateSystemUserHandler.cs   ✅
│   │       │   │   │   └── UpdateSystemUserValidator.cs ✅
│   │       │   │   ├── UpdatePermissions/
│   │       │   │   │   ├── UpdateSystemUserPermissionsCommand.cs ✅
│   │       │   │   │   └── UpdateSystemUserPermissionsHandler.cs ✅
│   │       │   │   └── DeleteSystemUser/
│   │       │   │       ├── DeleteSystemUserCommand.cs   ✅
│   │       │   │       └── DeleteSystemUserHandler.cs   ✅
│   │       │   ├── Queries/
│   │       │   │   ├── GetSystemUsers/
│   │       │   │   │   ├── GetSystemUsersQuery.cs       ✅
│   │       │   │   │   └── GetSystemUsersHandler.cs     ✅
│   │       │   │   └── GetSystemUser/
│   │       │   │       ├── GetSystemUserQuery.cs        ✅
│   │       │   │       └── GetSystemUserHandler.cs      ✅
│   │       │   └── Models/
│   │       │       └── SystemUserDto.cs                 ✅
│   │       ├── SystemPermissions/
│   │       │   ├── Queries/
│   │       │   │   └── GetSystemPermissions/
│   │       │   │       ├── GetSystemPermissionsQuery.cs   ✅
│   │       │   │       └── GetSystemPermissionsHandler.cs ✅
│   │       │   └── Models/
│   │       │       └── SystemPermissionDto.cs             ✅
│   │       └── SystemAuditLogs/
│   │           ├── Queries/
│   │           │   ├── GetSystemAuditLogs/
│   │           │   │   ├── GetSystemAuditLogsQuery.cs     ✅
│   │           │   │   └── GetSystemAuditLogsHandler.cs   ✅
│   │           │   └── GetAuditLogFilters/
│   │           │       ├── GetAuditLogFiltersQuery.cs     ✅
│   │           │       └── GetAuditLogFiltersHandler.cs   ✅
│   │           └── Models/
│   │               └── SystemAuditLogDto.cs               ✅
│   │
│   ├── ExoAuth.Infrastructure/
│   │   ├── ExoAuth.Infrastructure.csproj
│   │   ├── DependencyInjection.cs               ✅
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                  ✅
│   │   │   ├── Configurations/
│   │   │   │   ├── SystemUserConfiguration.cs           ✅
│   │   │   │   ├── SystemPermissionConfiguration.cs     ✅
│   │   │   │   ├── SystemUserPermissionConfiguration.cs ✅
│   │   │   │   ├── SystemAuditLogConfiguration.cs       ✅
│   │   │   │   ├── SystemInviteConfiguration.cs         ✅
│   │   │   │   └── RefreshTokenConfiguration.cs         ✅
│   │   │   ├── Migrations/                      ✅ (mehrere)
│   │   │   └── Repositories/
│   │   │       └── SystemUserRepository.cs      ✅
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs             ✅
│   │   │   └── RedisConnectionFactory.cs        ✅
│   │   ├── Messaging/
│   │   │   ├── RabbitMqMessageBus.cs            ✅
│   │   │   ├── RabbitMqConnectionFactory.cs     ✅
│   │   │   ├── RabbitMqBackgroundService.cs     ✅
│   │   │   └── Consumers/
│   │   │       └── SendEmailConsumer.cs         ✅
│   │   ├── Services/
│   │   │   ├── DateTimeProvider.cs              ✅
│   │   │   ├── PasswordHasher.cs                ✅
│   │   │   ├── TokenService.cs                  ✅
│   │   │   ├── AuditService.cs                  ✅
│   │   │   ├── EmailService.cs                  ✅
│   │   │   ├── EmailTemplateService.cs          ✅
│   │   │   ├── PermissionCacheService.cs        ✅
│   │   │   ├── BruteForceProtectionService.cs   ✅
│   │   │   └── TokenBlacklistService.cs         ✅
│   │   └── Sessions/                            [LEER]
│   │
│   └── ExoAuth.Api/
│       ├── ExoAuth.Api.csproj
│       ├── Program.cs                           ✅ (JWT Auth, Permission Cache Invalidation)
│       ├── appsettings.json                     ✅ (Jwt, Email, BruteForce, Cache, etc.)
│       ├── appsettings.Development.json         ✅
│       ├── appsettings.Production.json
│       ├── Controllers/
│       │   ├── ApiControllerBase.cs             ✅
│       │   ├── HealthController.cs              ✅
│       │   ├── AuthController.cs                ✅
│       │   ├── SystemUsersController.cs         ✅
│       │   ├── SystemPermissionsController.cs   ✅
│       │   └── SystemAuditLogsController.cs     ✅
│       ├── Middleware/
│       │   ├── ExceptionMiddleware.cs           ✅
│       │   └── RequestLoggingMiddleware.cs      ✅
│       ├── Filters/
│       │   ├── RateLimitAttribute.cs            ✅
│       │   ├── RateLimitFilter.cs               ✅
│       │   ├── SystemPermissionAttribute.cs     ✅
│       │   └── SystemPermissionFilter.cs        ✅
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs   ✅
│       └── Services/
│           └── CurrentUserService.cs            ✅
│
├── templates/
│   └── emails/
│       ├── en/
│       │   └── system-invite.html               ✅
│       └── de/
│           └── system-invite.html               ✅
│
└── tests/
    └── ExoAuth.UnitTests/
        ├── ExoAuth.UnitTests.csproj
        ├── Features/
        │   ├── Auth/
        │   │   ├── RegisterHandlerTests.cs      ✅
        │   │   ├── LoginHandlerTests.cs         ✅
        │   │   └── RefreshTokenHandlerTests.cs  ✅
        │   └── SystemUsers/
        │       ├── InviteSystemUserHandlerTests.cs      ✅
        │       └── UpdatePermissionsHandlerTests.cs     ✅
        ├── Services/
        │   ├── PasswordHasherTests.cs           ✅
        │   ├── TokenServiceTests.cs             ✅
        │   ├── BruteForceProtectionServiceTests.cs ✅
        │   ├── PermissionCacheServiceTests.cs   ✅
        │   └── EmailTemplateServiceTests.cs     ✅
        └── Helpers/
            ├── MockDbContext.cs                 ✅
            ├── TestDataFactory.cs               ✅
            └── AsyncQueryableExtensions.cs      ✅
```

### Installierte Packages (NICHT NOCHMAL INSTALLIEREN)

| Projekt | Package | Version |
|---------|---------|---------|
| ExoAuth.Application | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Application | FluentValidation | 12.1.1 |
| ExoAuth.Application | FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore | 8.0.11 |
| ExoAuth.Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore.Design | 8.0.11 |
| ExoAuth.Infrastructure | StackExchange.Redis | 2.10.1 |
| ExoAuth.Infrastructure | RabbitMQ.Client | 7.2.0 |
| ExoAuth.Infrastructure | Konscious.Security.Cryptography.Argon2 | 1.3.1 |
| ExoAuth.Infrastructure | Microsoft.Extensions.Hosting.Abstractions | 8.0.1 |
| ExoAuth.Infrastructure | System.IdentityModel.Tokens.Jwt | 8.x |
| ExoAuth.Api | Mediator.SourceGenerator | 3.0.1 |
| ExoAuth.Api | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Api | Swashbuckle.AspNetCore | default |
| ExoAuth.Api | Serilog.AspNetCore | 8.0.3 |
| ExoAuth.Api | Serilog.Sinks.Console | 6.0.0 |
| ExoAuth.Api | AspNetCore.HealthChecks.NpgSql | 8.0.2 |
| ExoAuth.Api | AspNetCore.HealthChecks.Redis | 8.0.1 |
| ExoAuth.Api | Microsoft.AspNetCore.Authentication.JwtBearer | 8.x |
| ExoAuth.UnitTests | xunit | default |
| ExoAuth.UnitTests | Moq | default |
| ExoAuth.UnitTests | FluentAssertions | default |

**Hinweis:** `AspNetCore.HealthChecks.Rabbitmq` wurde entfernt - inkompatibel mit `RabbitMQ.Client 7.x`

### Projekt Referenzen (NICHT NOCHMAL HINZUFÜGEN)

```
Application → Domain
Infrastructure → Application
Api → Application, Infrastructure
UnitTests → Application, Domain
IntegrationTests → Api
```

---

## Code Strukturen (Copy-Paste Templates)

### Entity
```csharp
namespace ExoAuth.Domain.Entities;

public class {Name}
{
    public Guid Id { get; private set; }
    // Properties...

    private {Name}() { } // EF Core

    public static {Name} Create(...)
    {
        return new {Name} { ... };
    }
}
```

### Command
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed record {Name}Command(
    // Properties...
) : ICommand<{Response}>;
```

### Command Handler
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed class {Name}Handler : ICommandHandler<{Name}Command, {Response}>
{
    public {Name}Handler(/* dependencies */)
    {
    }

    public async ValueTask<{Response}> Handle({Name}Command command, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Validator
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed class {Name}Validator : AbstractValidator<{Name}Command>
{
    public {Name}Validator()
    {
        RuleFor(x => x.Property).NotEmpty();
    }
}
```

### Query
```csharp
namespace ExoAuth.Application.Features.{Feature}.Queries.{Name};

public sealed record {Name}Query(
    // Properties...
) : IQuery<{Response}>;
```

### Controller
```csharp
namespace ExoAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class {Feature}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Feature}Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<Response>> Create(Request request, CancellationToken ct)
    {
        var command = new CreateCommand(request.Property);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
```

### EF Configuration
```csharp
namespace ExoAuth.Infrastructure.Persistence.Configurations;

public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{
    public void Configure(EntityTypeBuilder<{Name}> builder)
    {
        builder.ToTable("{Name}s");
        builder.HasKey(x => x.Id);
        // Weitere Config...
    }
}
```

---

## Regeln für Task Erstellung

1. **IMMER** zuerst diese Datei lesen
2. **IMMER** prüfen ob Files/Packages schon existieren
3. **IMMER** die Reihenfolge einhalten: Domain → Application → Infrastructure → Api
4. **IMMER** Tests mit einplanen
5. **IMMER** am Ende diese Datei updaten
6. **NIE** Packages doppelt installieren
7. **NIE** Files überschreiben ohne zu fragen

---

## Letzte Änderung

- **Datum:** 2025-12-26
- **Status:** System Authentication komplett (Task 002)
- **Erledigte Tasks:**
  - Task 001: Foundation & Infrastructure Setup ✅
  - Task 002: System Authentication, Users, Permissions & Audit ✅ (106 Unit Tests)
- **Nächster Task:** Frontend Foundation (Task 003)
- **Template Updates:**
  - Section "Wichtige Regeln für Task Files" hinzugefügt
  - Integration Tests aus Template entfernt (nur Unit Tests)
  - Test Helpers Section hinzugefügt

### Bekannte Einschränkungen
- RabbitMQ Health Check nicht in `/health` enthalten (`RabbitMQ.Client 7.x` Inkompatibilität mit Health Check Package)
- RabbitMQ Verbindung wird via `RabbitMqBackgroundService` beim Start geprüft

# Backend Task Standards - ExoAuth

> **MEGA BRAIN** - Lies diese Datei KOMPLETT bevor du einen Task erstellst.

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

## 4. Datenbank Änderungen

### Neue Entities
| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| ... | ... | ... |

### Migrations
- [ ] Migration Name: `Add{EntityName}`

## 5. Files zu erstellen

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

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/.../AppDbContext.cs` | DbSet hinzufügen |
| `src/.../DependencyInjection.cs` | Service registrieren |

## 7. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| ... | ... | ExoAuth.XXX | ... |

## 8. Implementation Reihenfolge

1. [ ] **Domain**: Entity erstellen
2. [ ] **Infrastructure**: Configuration + DbContext + Migration
3. [ ] **Application**: Commands/Queries + Handlers + Validators
4. [ ] **API**: Controller + Endpoints
5. [ ] **Tests**: Unit + Integration Tests
6. [ ] **Standards updaten**: task_standards_backend.md aktualisieren

## 9. Tests

### Unit Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `tests/ExoAuth.UnitTests/Features/.../...Tests.cs` | ... |

### Integration Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `tests/ExoAuth.IntegrationTests/Features/.../...Tests.cs` | ... |

## 10. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_backend.md` aktualisiert (neue Files, Packages)
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
│   │   ├── Entities/                    [LEER]
│   │   ├── ValueObjects/                [LEER]
│   │   ├── Enums/                       [LEER]
│   │   ├── Exceptions/                  [LEER]
│   │   └── Events/                      [LEER]
│   │
│   ├── ExoAuth.Application/
│   │   ├── ExoAuth.Application.csproj
│   │   ├── Common/
│   │   │   ├── Interfaces/              [LEER]
│   │   │   ├── Behaviors/               [LEER]
│   │   │   ├── Mappings/                [LEER]
│   │   │   └── Models/                  [LEER]
│   │   └── Features/
│   │       ├── Auth/
│   │       │   ├── Commands/
│   │       │   │   ├── Login/           [LEER]
│   │       │   │   ├── Register/        [LEER]
│   │       │   │   └── RefreshToken/    [LEER]
│   │       │   └── Queries/
│   │       │       └── GetCurrentUser/  [LEER]
│   │       ├── Users/
│   │       │   ├── Commands/            [LEER]
│   │       │   └── Queries/             [LEER]
│   │       ├── Roles/
│   │       │   ├── Commands/            [LEER]
│   │       │   └── Queries/             [LEER]
│   │       └── Permissions/
│   │           ├── Commands/            [LEER]
│   │           └── Queries/             [LEER]
│   │
│   ├── ExoAuth.Infrastructure/
│   │   ├── ExoAuth.Infrastructure.csproj
│   │   ├── Persistence/
│   │   │   ├── Configurations/          [LEER]
│   │   │   ├── Migrations/              [LEER]
│   │   │   └── Repositories/            [LEER]
│   │   ├── Caching/                     [LEER]
│   │   ├── Messaging/                   [LEER]
│   │   └── Sessions/                    [LEER]
│   │
│   └── ExoAuth.Api/
│       ├── ExoAuth.Api.csproj
│       ├── Program.cs                   [DEFAULT - muss konfiguriert werden]
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Production.json
│       ├── Controllers/                 [LEER]
│       ├── Middleware/                  [LEER]
│       ├── Filters/                     [LEER]
│       └── Extensions/                  [LEER]
│
└── tests/
    ├── ExoAuth.UnitTests/
    │   ├── ExoAuth.UnitTests.csproj
    │   ├── Features/                    [LEER]
    │   ├── Domain/                      [LEER]
    │   └── Helpers/                     [LEER]
    │
    └── ExoAuth.IntegrationTests/
        ├── ExoAuth.IntegrationTests.csproj
        ├── Features/                    [LEER]
        └── Fixtures/                    [LEER]
```

### Installierte Packages (NICHT NOCHMAL INSTALLIEREN)

| Projekt | Package | Version |
|---------|---------|---------|
| ExoAuth.Application | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Application | FluentValidation | 12.1.1 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore | 8.0.11 |
| ExoAuth.Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore.Design | 8.0.11 |
| ExoAuth.Infrastructure | StackExchange.Redis | 2.10.1 |
| ExoAuth.Infrastructure | RabbitMQ.Client | 7.2.0 |
| ExoAuth.Api | Mediator.SourceGenerator | 3.0.1 |
| ExoAuth.Api | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Api | Swashbuckle.AspNetCore | default |
| ExoAuth.UnitTests | xunit | default |
| ExoAuth.IntegrationTests | xunit | default |

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

## Noch zu erstellen (Foundation)

Diese Files müssen ZUERST erstellt werden bevor Features gebaut werden:

| Priorität | Datei | Status |
|-----------|-------|--------|
| 1 | `src/ExoAuth.Domain/Entities/BaseEntity.cs` | ❌ |
| 2 | `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | ❌ |
| 3 | `src/ExoAuth.Infrastructure/DependencyInjection.cs` | ❌ |
| 4 | `src/ExoAuth.Application/DependencyInjection.cs` | ❌ |
| 5 | `src/ExoAuth.Api/Program.cs` konfigurieren | ❌ |

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

- **Datum:** 2024-12-24
- **Status:** Initial Setup - Alle Ordner leer, keine Logik implementiert
- **Nächster Task:** Foundation Files erstellen

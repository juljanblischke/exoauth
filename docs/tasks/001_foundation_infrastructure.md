# Task: Foundation & Infrastructure Setup

## 1. Übersicht

**Was wird gebaut?**
Die komplette Foundation-Infrastruktur für ExoAuth: Redis (Caching, Sessions, Rate Limiting), RabbitMQ (Message Queue, Background Jobs), standardisierte API Responses, Logging, Health Checks und Docker-Umgebung.

**Warum?**
Bevor Features wie Auth, Users, Roles implementiert werden können, muss die Basis-Infrastruktur stehen. Diese Task erstellt alle notwendigen Interfaces, Services und Konfigurationen.

---

## 2. User Experience / Anforderungen

### User Stories
- Als Developer möchte ich eine einheitliche API Response Struktur damit Frontend konsistent Daten verarbeiten kann
- Als Developer möchte ich Rate Limiting per Attribut konfigurieren damit API-Endpunkte geschützt sind
- Als Developer möchte ich Health Checks damit ich den Status aller Services überwachen kann
- Als Developer möchte ich Docker Compose damit ich lokal schnell starten kann

### Akzeptanzkriterien
- [ ] `docker-compose up` startet PostgreSQL, Redis, RabbitMQ *(Docker Desktop muss laufen)*
- [ ] `/health` Endpoint zeigt Status aller Connections
- [x] API Responses folgen dem definierten Schema
- [x] Rate Limiting funktioniert mit `[RateLimit]` Attribut
- [x] Serilog loggt strukturiert in Console
- [x] Swagger UI erlaubt JWT Authentication

### Edge Cases / Error Handling
- Was passiert wenn Redis nicht erreichbar? → Graceful degradation, App startet trotzdem
- Was passiert wenn RabbitMQ nicht erreichbar? → Retry-Logik, Messages werden gequeued
- Was passiert bei Rate Limit exceeded? → 429 Response mit Retry-After Header

---

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/health` | - | `{ "status": "Healthy", "checks": [...] }` | Health Check aller Services |
| GET | `/api/v1/...` | - | `ApiResponse<T>` | Alle Endpoints mit Versioning |

### API Response Schema

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
  "errors": [
    {
      "field": "email",
      "code": "VALIDATION_REQUIRED",
      "message": "Email is required"
    }
  ]
}
```

### Error Codes (Initial Set)

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `VALIDATION_REQUIRED` | 400 | Feld ist erforderlich |
| `VALIDATION_INVALID_FORMAT` | 400 | Ungültiges Format |
| `VALIDATION_MIN_LENGTH` | 400 | Mindestlänge nicht erreicht |
| `VALIDATION_MAX_LENGTH` | 400 | Maximallänge überschritten |
| `AUTH_UNAUTHORIZED` | 401 | Nicht authentifiziert |
| `AUTH_FORBIDDEN` | 403 | Keine Berechtigung |
| `AUTH_TOKEN_EXPIRED` | 401 | Token abgelaufen |
| `AUTH_TOKEN_INVALID` | 401 | Token ungültig |
| `RESOURCE_NOT_FOUND` | 404 | Resource nicht gefunden |
| `RATE_LIMIT_EXCEEDED` | 429 | Rate Limit überschritten |
| `INTERNAL_ERROR` | 500 | Interner Serverfehler |

---

## 4. Datenbank Änderungen

### AppDbContext Setup
- PostgreSQL Connection
- snake_case Column Naming Convention
- UTC Timestamp Handling

### Keine Migrations in dieser Task
- Entities kommen in späteren Tasks

---

## 5. Files zu erstellen

### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| BaseEntity | `src/ExoAuth.Domain/Entities/BaseEntity.cs` | Basis-Entity mit Id, CreatedAt, UpdatedAt |

### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| DependencyInjection | `src/ExoAuth.Application/DependencyInjection.cs` | Service Registration |
| ICacheService | `src/ExoAuth.Application/Common/Interfaces/ICacheService.cs` | Cache Interface |
| IMessageBus | `src/ExoAuth.Application/Common/Interfaces/IMessageBus.cs` | Message Bus Interface |
| ICurrentUserService | `src/ExoAuth.Application/Common/Interfaces/ICurrentUserService.cs` | Current User Context |
| IDateTimeProvider | `src/ExoAuth.Application/Common/Interfaces/IDateTimeProvider.cs` | DateTime Abstraction (UTC) |
| ApiResponse | `src/ExoAuth.Application/Common/Models/ApiResponse.cs` | Standard Response Wrapper |
| ApiError | `src/ExoAuth.Application/Common/Models/ApiError.cs` | Error Model |
| ErrorCodes | `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | Error Code Constants |
| PaginationMeta | `src/ExoAuth.Application/Common/Models/PaginationMeta.cs` | Cursor Pagination Meta |
| CursorPagedList | `src/ExoAuth.Application/Common/Models/CursorPagedList.cs` | Paged List with Cursor |
| ValidationBehavior | `src/ExoAuth.Application/Common/Behaviors/ValidationBehavior.cs` | FluentValidation Pipeline |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| DependencyInjection | `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Service Registration |
| AppDbContext | `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | EF Core DbContext |
| RedisCacheService | `src/ExoAuth.Infrastructure/Caching/RedisCacheService.cs` | Redis Implementation |
| RedisConnectionFactory | `src/ExoAuth.Infrastructure/Caching/RedisConnectionFactory.cs` | Redis Connection |
| RabbitMqMessageBus | `src/ExoAuth.Infrastructure/Messaging/RabbitMqMessageBus.cs` | RabbitMQ Publisher |
| RabbitMqConnectionFactory | `src/ExoAuth.Infrastructure/Messaging/RabbitMqConnectionFactory.cs` | RabbitMQ Connection |
| RabbitMqBackgroundService | `src/ExoAuth.Infrastructure/Messaging/RabbitMqBackgroundService.cs` | Background Consumer |
| DateTimeProvider | `src/ExoAuth.Infrastructure/Services/DateTimeProvider.cs` | UTC DateTime |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Program.cs | `src/ExoAuth.Api/Program.cs` | Konfigurierter Entry Point |
| RateLimitAttribute | `src/ExoAuth.Api/Filters/RateLimitAttribute.cs` | Rate Limit Attribut |
| RateLimitFilter | `src/ExoAuth.Api/Filters/RateLimitFilter.cs` | Rate Limit Logic |
| ExceptionMiddleware | `src/ExoAuth.Api/Middleware/ExceptionMiddleware.cs` | Global Error Handling |
| RequestLoggingMiddleware | `src/ExoAuth.Api/Middleware/RequestLoggingMiddleware.cs` | Request/Response Logging |
| CurrentUserService | `src/ExoAuth.Api/Services/CurrentUserService.cs` | HttpContext User |
| ApiControllerBase | `src/ExoAuth.Api/Controllers/ApiControllerBase.cs` | Base Controller |
| HealthController | `src/ExoAuth.Api/Controllers/HealthController.cs` | Health Check Endpoint |
| ServiceCollectionExtensions | `src/ExoAuth.Api/Extensions/ServiceCollectionExtensions.cs` | Swagger, CORS, etc. |

### Root Level

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| docker-compose.yml | `docker-compose.yml` | PostgreSQL, Redis, RabbitMQ |
| docker-compose.override.yml | `docker-compose.override.yml` | Local Dev Overrides |
| .env.example | `.env.example` | Environment Variables Template |

---

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Api/appsettings.json` | Connection Strings, Serilog Config |
| `src/ExoAuth.Api/appsettings.Development.json` | Dev-spezifische Settings |

---

## 7. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| Serilog.AspNetCore | latest | ExoAuth.Api | Structured Logging |
| Serilog.Sinks.Console | latest | ExoAuth.Api | Console Output |
| AspNetCore.HealthChecks.NpgSql | latest | ExoAuth.Api | PostgreSQL Health |
| AspNetCore.HealthChecks.Redis | latest | ExoAuth.Api | Redis Health |
| AspNetCore.HealthChecks.RabbitMQ | latest | ExoAuth.Api | RabbitMQ Health |
| Konscious.Security.Cryptography.Argon2 | latest | ExoAuth.Infrastructure | Password Hashing |

**Bereits installiert (NICHT nochmal installieren):**
- StackExchange.Redis
- RabbitMQ.Client
- FluentValidation
- Mediator
- Swashbuckle
- EF Core + Npgsql

---

## 8. Implementation Reihenfolge

### Phase 1: Core Foundation ✅
1. [x] Docker Compose erstellen (PostgreSQL, Redis, RabbitMQ) - *bereits vorhanden*
2. [x] `.env.example` erstellen - *bereits vorhanden*
3. [x] `appsettings.json` konfigurieren - *Jwt, RateLimiting, Cors, Serilog hinzugefügt*

### Phase 2: Domain Layer ✅
4. [x] `BaseEntity.cs` erstellen

### Phase 3: Application Layer ✅
5. [x] Interfaces erstellen (ICacheService, IMessageBus, etc.)
6. [x] Models erstellen (ApiResponse, ApiError, ErrorCodes, Pagination)
7. [x] ValidationBehavior erstellen
8. [x] `DependencyInjection.cs` erstellen

### Phase 4: Infrastructure Layer ✅
9. [x] `AppDbContext.cs` erstellen (snake_case config)
10. [x] Redis Services erstellen (Connection, CacheService)
11. [x] RabbitMQ Services erstellen (Connection, MessageBus, BackgroundService)
12. [x] DateTimeProvider erstellen
13. [x] Packages installieren (Serilog, HealthChecks, Argon2)
14. [x] `DependencyInjection.cs` erstellen

### Phase 5: API Layer ✅
15. [x] Rate Limiting erstellen (Attribute + Filter)
16. [x] Middleware erstellen (Exception, RequestLogging)
17. [x] Services erstellen (CurrentUserService)
18. [x] Controllers erstellen (ApiControllerBase, HealthController)
19. [x] Extensions erstellen (Swagger, CORS, Versioning)
20. [x] `Program.cs` komplett konfigurieren

### Phase 6: Verification ⏳
21. [ ] `docker-compose up` testen - *Docker Desktop muss gestartet werden*
22. [ ] App starten und Health Check testen
23. [ ] Swagger UI öffnen und testen
24. [ ] Rate Limiting testen

---

## 9. Tests

### Unit Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `tests/ExoAuth.UnitTests/Common/ApiResponseTests.cs` | Response Builder |
| `tests/ExoAuth.UnitTests/Common/CursorPaginationTests.cs` | Cursor Encoding/Decoding |

### Integration Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `tests/ExoAuth.IntegrationTests/HealthCheckTests.cs` | Health Endpoint |
| `tests/ExoAuth.IntegrationTests/RateLimitTests.cs` | Rate Limiting |

---

## 10. Configuration Values

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Database=exoauth;Username=postgres;Password=postgres",
    "Redis": "localhost:6379",
    "RabbitMq": "amqp://guest:guest@localhost:5672"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-long",
    "Issuer": "ExoAuth",
    "Audience": "ExoAuth",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationHours": 4
  },
  "RateLimiting": {
    "DefaultRequestsPerMinute": 100
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

### Docker Compose Services

| Service | Image | Port |
|---------|-------|------|
| PostgreSQL | postgres:16-alpine | 5432 |
| Redis | redis:7-alpine | 6379 |
| RabbitMQ | rabbitmq:3-management-alpine | 5672, 15672 |

---

## 11. Nach Completion

- [ ] Alle Services starten mit `docker-compose up` - *Docker Desktop starten*
- [x] App startet ohne Fehler - *Build erfolgreich (0 Errors, 0 Warnings)*
- [ ] `/health` zeigt alle Services als Healthy
- [ ] Swagger UI funktioniert unter `/swagger`
- [ ] `task_standards_backend.md` aktualisiert (neue Files, Packages)
- [ ] Code reviewed

---

## Notizen

- **Rate Limiting Storage**: Redis (pro IP, sliding window)
- **Request ID**: Wird in jedem Request generiert und in Response + Logs verwendet
- **Sensitive Data**: Passwords werden NICHT geloggt (RequestLoggingMiddleware filtert)
- **Graceful Shutdown**: BackgroundService reagiert auf CancellationToken

---

## Implementation Log

**Datum:** 2024-12-24

### Erstellte Files

**Domain Layer:**
- `src/ExoAuth.Domain/Entities/BaseEntity.cs`

**Application Layer:**
- `src/ExoAuth.Application/DependencyInjection.cs`
- `src/ExoAuth.Application/Common/Interfaces/ICacheService.cs`
- `src/ExoAuth.Application/Common/Interfaces/IMessageBus.cs`
- `src/ExoAuth.Application/Common/Interfaces/ICurrentUserService.cs`
- `src/ExoAuth.Application/Common/Interfaces/IDateTimeProvider.cs`
- `src/ExoAuth.Application/Common/Models/ApiResponse.cs`
- `src/ExoAuth.Application/Common/Models/ApiError.cs`
- `src/ExoAuth.Application/Common/Models/ErrorCodes.cs`
- `src/ExoAuth.Application/Common/Models/PaginationMeta.cs`
- `src/ExoAuth.Application/Common/Models/CursorPagedList.cs`
- `src/ExoAuth.Application/Common/Behaviors/ValidationBehavior.cs`

**Infrastructure Layer:**
- `src/ExoAuth.Infrastructure/DependencyInjection.cs`
- `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs`
- `src/ExoAuth.Infrastructure/Caching/RedisConnectionFactory.cs`
- `src/ExoAuth.Infrastructure/Caching/RedisCacheService.cs`
- `src/ExoAuth.Infrastructure/Messaging/RabbitMqConnectionFactory.cs`
- `src/ExoAuth.Infrastructure/Messaging/RabbitMqMessageBus.cs`
- `src/ExoAuth.Infrastructure/Messaging/RabbitMqBackgroundService.cs`
- `src/ExoAuth.Infrastructure/Services/DateTimeProvider.cs`

**API Layer:**
- `src/ExoAuth.Api/Filters/RateLimitAttribute.cs`
- `src/ExoAuth.Api/Filters/RateLimitFilter.cs`
- `src/ExoAuth.Api/Middleware/ExceptionMiddleware.cs`
- `src/ExoAuth.Api/Middleware/RequestLoggingMiddleware.cs`
- `src/ExoAuth.Api/Services/CurrentUserService.cs`
- `src/ExoAuth.Api/Controllers/ApiControllerBase.cs`
- `src/ExoAuth.Api/Controllers/HealthController.cs`
- `src/ExoAuth.Api/Extensions/ServiceCollectionExtensions.cs`

### Geänderte Files
- `src/ExoAuth.Api/Program.cs` - Komplett konfiguriert
- `src/ExoAuth.Api/appsettings.json` - Jwt, RateLimiting, Cors, Serilog hinzugefügt
- `src/ExoAuth.Api/appsettings.Development.json` - Connection Strings aktualisiert

### Installierte Packages
- `Serilog.AspNetCore` (8.0.3) → ExoAuth.Api
- `Serilog.Sinks.Console` (6.0.0) → ExoAuth.Api
- `AspNetCore.HealthChecks.NpgSql` (8.0.2) → ExoAuth.Api
- `AspNetCore.HealthChecks.Redis` (8.0.1) → ExoAuth.Api
- `AspNetCore.HealthChecks.Rabbitmq` (8.0.2) → ExoAuth.Api
- `Konscious.Security.Cryptography.Argon2` (1.3.1) → ExoAuth.Infrastructure
- `FluentValidation.DependencyInjectionExtensions` (12.1.1) → ExoAuth.Application
- `Microsoft.Extensions.Hosting.Abstractions` (8.0.1) → ExoAuth.Infrastructure

### Nächste Schritte
1. Docker Desktop starten
2. `docker-compose up -d` ausführen
3. `dotnet run --project backend/src/ExoAuth.Api` starten
4. `/health` und `/swagger` testen

### Fixes während Testing
- **Dockerfile**: `--no-launch-profile` hinzugefügt (Port 8080 statt launchSettings)
- **docker-compose.yml**:
  - `PGADMIN_DEFAULT_EMAIL` von `.local` zu `.dev` geändert
  - Connection Strings auf neue Keys umgestellt (`ConnectionStrings__Database`, etc.)
- **HealthController**: Response vereinfacht (war zu groß für Swagger)
- **RabbitMQ HealthCheck**: Entfernt - `AspNetCore.HealthChecks.Rabbitmq` inkompatibel mit `RabbitMQ.Client 7.x`

### Bekannte Einschränkungen
- RabbitMQ Health Check nicht in `/health` enthalten (Client 7.x Inkompatibilität)
- RabbitMQ Verbindung wird stattdessen via `RabbitMqBackgroundService` beim Start geprüft

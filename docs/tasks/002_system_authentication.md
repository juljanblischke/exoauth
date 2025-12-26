# Task: System Authentication, Users, Permissions & Audit

## 1. Übersicht

**Was wird gebaut?**
Das komplette System-Level für ExoAuth: SystemUser Authentifizierung, Berechtigungssystem, Audit-Logging und E-Mail-Infrastruktur.

**Warum?**
- SystemUsers sind die höchste Ebene - sie verwalten die gesamte ExoAuth Plattform
- Basis für alle weiteren Features (Organizations, Projects kommen später)
- Saubere Trennung: System → Organizations → Projects → EndUsers

**Architektur-Kontext:**
```
┌─────────────────────────────────────────────────────────────┐
│  SYSTEM (Diese Task)                                        │
│  ├── SystemUsers (Plattform-Admins)                         │
│  ├── SystemPermissions (Granulare Berechtigungen)           │
│  ├── SystemAuditLog (System-Level Actions)                  │
│  └── SystemSettings (E-Mail Config, etc.) - appsettings     │
├─────────────────────────────────────────────────────────────┤
│  ORGANIZATIONS (Später - Task 003)                          │
│  ├── Organizations ("Firmen" - Kunden)                      │
│  ├── OrganizationUsers                                      │
│  └── OrganizationAuditLog                                   │
├─────────────────────────────────────────────────────────────┤
│  PROJECTS (Später - Task 004)                               │
│  ├── Projects (Apps der Kunden)                             │
│  ├── ProjectUsers (End-Users)                               │
│  └── ProjectAuditLog                                        │
└─────────────────────────────────────────────────────────────┘
```

## 2. User Experience / Anforderungen

### User Stories

- Als **erster Benutzer** möchte ich mich registrieren und automatisch alle System-Berechtigungen erhalten, damit ich ExoAuth einrichten kann
- Als **SystemUser** möchte ich mich einloggen und einen JWT Token erhalten, damit ich auf das System-Dashboard zugreifen kann
- Als **SystemUser** möchte ich andere SystemUsers einladen können, damit mein Team die Plattform verwalten kann
- Als **eingeladener Benutzer** möchte ich die Einladung annehmen und mein Passwort setzen können
- Als **SystemUser** möchte ich die Berechtigungen anderer SystemUsers verwalten können
- Als **SystemUser** möchte ich alle System-Aktionen im Audit-Log sehen können
- Als **SystemUser** möchte ich Listen mit Pagination, Sortierung und Suche haben

### Akzeptanzkriterien

- [ ] Erster registrierter User wird automatisch SystemUser mit allen Permissions
- [ ] Login funktioniert mit E-Mail + Passwort, gibt JWT zurück
- [ ] JWT enthält `type: "system"` und Permissions
- [ ] Refresh Token in DB gespeichert, revocable
- [ ] Einladungen per E-Mail mit 24h Ablaufzeit
- [ ] Passwort: Min 12 Zeichen, Groß-/Kleinbuchstaben, Zahl, Sonderzeichen
- [ ] Listen: Cursor-Pagination, Multi-Column-Sorting, Search
- [ ] Audit-Log: Alle System-Aktionen werden geloggt
- [ ] Security: Letzter User mit `system:users:update` kann nicht entfernt werden
- [ ] E-Mail global unique (nicht in SystemUsers UND später OrganizationUsers)
- [ ] Redis: User Permissions gecached für Performance
- [ ] Redis: Revoked Tokens in Blacklist für schnelle Prüfung
- [ ] Redis: Brute Force Protection (5 Versuche → 15 Min Block)
- [ ] Startup: Permission Cache wird bei App-Start invalidiert
- [ ] E-Mail Templates in EN + DE verfügbar

### Edge Cases / Error Handling

- Was passiert wenn jemand sich registriert aber schon SystemUsers existieren?
  → `organizationName` wird required (für später), jetzt Error: "Registration closed"
- Was passiert wenn Einladung abgelaufen ist?
  → Error: "Invite expired", neuer Invite muss gesendet werden
- Was passiert wenn letzter User mit Permission-Verwaltung seine eigene Permission entfernt?
  → Error: "Cannot remove - you are the last user with this permission"
- Was passiert wenn E-Mail bereits existiert (später auch in OrgUsers)?
  → Error: "Email already in use"
- Was passiert bei falschem Login?
  → Error: "Invalid credentials" (nicht verraten ob E-Mail existiert)
- Was passiert bei zu vielen fehlgeschlagenen Login-Versuchen?
  → Nach 5 Versuchen: 15 Minuten Sperre, Error: "Too many attempts"
- Was passiert wenn Redis nicht verfügbar ist?
  → Graceful Degradation: Fallback auf DB-Queries (langsamer, aber funktioniert)

## 3. API Endpoints

### Authentication

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| POST | `/api/auth/register` | `{ email, password, firstName, lastName, organizationName? }` | `{ user, accessToken, refreshToken }` | Erster User → SystemUser |
| POST | `/api/auth/login` | `{ email, password }` | `{ user, accessToken, refreshToken }` | Login (prüft SystemUsers, später auch OrgUsers) |
| POST | `/api/auth/refresh` | `{ refreshToken }` | `{ accessToken, refreshToken }` | Token erneuern |
| POST | `/api/auth/logout` | `{ refreshToken }` | `{ success }` | Logout (Refresh Token revoken) |
| GET | `/api/auth/me` | - | `{ user }` | Aktueller User mit Permissions |
| POST | `/api/auth/accept-invite` | `{ token, password }` | `{ user, accessToken, refreshToken }` | Einladung annehmen |

### System Users

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/users` | - | `{ data[], pagination }` | Liste mit Pagination, Sort, Search |
| GET | `/api/system/users/{id}` | - | `{ user, permissions[] }` | Einzelner User mit Permissions |
| POST | `/api/system/users/invite` | `{ email, firstName, lastName, permissionIds[] }` | `{ invite }` | User einladen (sendet E-Mail) |
| PUT | `/api/system/users/{id}` | `{ firstName?, lastName?, isActive? }` | `{ user }` | User updaten |
| PUT | `/api/system/users/{id}/permissions` | `{ permissionIds[] }` | `{ user, permissions[] }` | Permissions setzen (ersetzt alle) |
| DELETE | `/api/system/users/{id}` | - | `{ success }` | User deaktivieren/löschen |

### System Permissions

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/permissions` | - | `{ data[] }` | Alle verfügbaren System-Permissions |

### System Audit Logs

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/audit-logs` | - | `{ data[], pagination }` | Logs mit Pagination, Sort, Filter |
| GET | `/api/system/audit-logs/filters` | - | `{ actions[], users[], dateRange }` | Verfügbare Filter-Optionen |

### Query Parameter für Listen

```
# Pagination (Cursor-based für Infinite Scroll)
?cursor=abc123&limit=20

# Sorting (Multi-Column)
?sort=createdAt:desc,email:asc

# Search
?search=john

# Filters (Audit Logs)
?action=user.login&userId=xxx&from=2025-01-01&to=2025-12-31
```

### Response Format

```json
// Liste
{
  "data": [...],
  "pagination": {
    "cursor": "next_cursor_here",
    "hasMore": true,
    "total": 150
  }
}

// Einzelnes Item
{
  "data": { ... }
}

// Error
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "...",
    "details": { ... }
  }
}
```

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `AUTH_INVALID_CREDENTIALS` | 401 | E-Mail oder Passwort falsch |
| `AUTH_USER_INACTIVE` | 401 | User ist deaktiviert |
| `AUTH_TOKEN_EXPIRED` | 401 | Access Token abgelaufen |
| `AUTH_REFRESH_TOKEN_INVALID` | 401 | Refresh Token ungültig oder revoked |
| `AUTH_INVITE_EXPIRED` | 400 | Einladung abgelaufen (24h) |
| `AUTH_INVITE_INVALID` | 400 | Einladungs-Token ungültig |
| `AUTH_REGISTRATION_CLOSED` | 400 | Registration nur für ersten User (später: orgName required) |
| `AUTH_EMAIL_EXISTS` | 409 | E-Mail bereits vergeben |
| `AUTH_PASSWORD_TOO_WEAK` | 400 | Passwort erfüllt Anforderungen nicht |
| `AUTH_TOO_MANY_ATTEMPTS` | 429 | Zu viele fehlgeschlagene Login-Versuche, 15 Min warten |
| `SYSTEM_USER_NOT_FOUND` | 404 | SystemUser nicht gefunden |
| `SYSTEM_PERMISSION_NOT_FOUND` | 404 | Permission nicht gefunden |
| `SYSTEM_LAST_PERMISSION_HOLDER` | 400 | Kann nicht entfernen - letzter User mit dieser Permission |
| `SYSTEM_CANNOT_DELETE_SELF` | 400 | Kann sich selbst nicht löschen |
| `SYSTEM_FORBIDDEN` | 403 | Keine Berechtigung für diese Aktion |

> ⚠️ **Nach Completion:** Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities

| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| SystemUser | Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailVerified, LastLoginAt, CreatedAt, UpdatedAt | → SystemUserPermissions, → SystemAuditLogs, → RefreshTokens |
| SystemPermission | Id, Name, Description, Category, CreatedAt | → SystemUserPermissions |
| SystemUserPermission | SystemUserId, SystemPermissionId, GrantedAt, GrantedBy | → SystemUser, → SystemPermission |
| SystemAuditLog | Id, UserId?, Action, EntityType?, EntityId?, IpAddress?, UserAgent?, Details (JSON), CreatedAt | → SystemUser |
| SystemInvite | Id, Email, FirstName, LastName, Token, PermissionIds (JSON), ExpiresAt, AcceptedAt?, InvitedBy, CreatedAt | → SystemUser (InvitedBy) |
| RefreshToken | Id, UserId, UserType, Token (hashed), ExpiresAt, IsRevoked, DeviceInfo?, IpAddress?, CreatedAt | - |

### Entity Details

**SystemUser:**
```csharp
public class SystemUser
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }           // Unique, lowercase
    public string PasswordHash { get; private set; }    // Argon2
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool IsActive { get; private set; }          // Default: true
    public bool EmailVerified { get; private set; }     // true nach Invite-Accept
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public ICollection<SystemUserPermission> Permissions { get; }
    public ICollection<RefreshToken> RefreshTokens { get; }
}
```

**SystemPermission:**
```csharp
public class SystemPermission
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }            // z.B. "system:users:read"
    public string Description { get; private set; }
    public string Category { get; private set; }        // z.B. "Users", "Audit", "Settings"
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public ICollection<SystemUserPermission> UserPermissions { get; }
}
```

**SystemAuditLog:**
```csharp
public class SystemAuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }           // Null = System/Anonymous
    public string Action { get; private set; }          // z.B. "user.login"
    public string? EntityType { get; private set; }     // z.B. "SystemUser"
    public Guid? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public JsonDocument? Details { get; private set; }  // Additional context
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public SystemUser? User { get; }
}
```

**RefreshToken:**
```csharp
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string UserType { get; private set; }        // "system", "organization", "project"
    public string TokenHash { get; private set; }       // SHA256 hash
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

### Seeded Data: System Permissions

| Name | Description | Category |
|------|-------------|----------|
| `system:users:read` | View system users | Users |
| `system:users:create` | Invite new system users | Users |
| `system:users:update` | Edit system users & permissions | Users |
| `system:users:delete` | Deactivate/delete system users | Users |
| `system:audit:read` | View system audit logs | Audit |
| `system:settings:read` | View system settings | Settings |
| `system:settings:update` | Change system settings | Settings |
| `system:organizations:read` | View all organizations | Organizations |
| `system:organizations:create` | Create organizations | Organizations |
| `system:organizations:update` | Edit organizations | Organizations |
| `system:organizations:delete` | Delete organizations | Organizations |
| `system:projects:read` | View system-owned projects | Projects |
| `system:projects:create` | Create system-owned projects | Projects |
| `system:projects:update` | Edit system-owned projects | Projects |
| `system:projects:delete` | Delete system-owned projects | Projects |

### Migrations

- [ ] Migration Name: `AddSystemUsers`
- [ ] Migration Name: `AddSystemPermissions`
- [ ] Migration Name: `AddSystemAuditLogs`
- [ ] Migration Name: `AddRefreshTokens`
- [ ] Migration Name: `AddSystemInvites`
- [ ] Migration Name: `SeedSystemPermissions`

## 6. Files zu erstellen

### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| SystemUser.cs | `src/ExoAuth.Domain/Entities/SystemUser.cs` | SystemUser Entity |
| SystemPermission.cs | `src/ExoAuth.Domain/Entities/SystemPermission.cs` | Permission Entity |
| SystemUserPermission.cs | `src/ExoAuth.Domain/Entities/SystemUserPermission.cs` | Join Entity |
| SystemAuditLog.cs | `src/ExoAuth.Domain/Entities/SystemAuditLog.cs` | Audit Log Entity |
| SystemInvite.cs | `src/ExoAuth.Domain/Entities/SystemInvite.cs` | Invite Entity |
| RefreshToken.cs | `src/ExoAuth.Domain/Entities/RefreshToken.cs` | Refresh Token Entity |
| UserType.cs | `src/ExoAuth.Domain/Enums/UserType.cs` | Enum: System, Organization, Project |
| SystemPermissions.cs | `src/ExoAuth.Domain/Constants/SystemPermissions.cs` | Permission name constants |

### Application Layer

**Auth Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| RegisterCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterCommand.cs` | Register command |
| RegisterHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterHandler.cs` | Register handler |
| RegisterValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterValidator.cs` | Validation |
| LoginCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginCommand.cs` | Login command |
| LoginHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginHandler.cs` | Login handler |
| LoginValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginValidator.cs` | Validation |
| RefreshTokenCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs` | Refresh command |
| RefreshTokenHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/RefreshToken/RefreshTokenHandler.cs` | Refresh handler |
| LogoutCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/Logout/LogoutCommand.cs` | Logout command |
| LogoutHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/Logout/LogoutHandler.cs` | Logout handler |
| AcceptInviteCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/AcceptInvite/AcceptInviteCommand.cs` | Accept invite |
| AcceptInviteHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/AcceptInvite/AcceptInviteHandler.cs` | Handler |
| AcceptInviteValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/AcceptInvite/AcceptInviteValidator.cs` | Validation |
| GetCurrentUserQuery.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs` | Me query |
| GetCurrentUserHandler.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserHandler.cs` | Handler |
| AuthResponse.cs | `src/ExoAuth.Application/Features/Auth/Models/AuthResponse.cs` | Response DTOs |

**System Users Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| GetSystemUsersQuery.cs | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUsers/GetSystemUsersQuery.cs` | List query |
| GetSystemUsersHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUsers/GetSystemUsersHandler.cs` | Handler |
| GetSystemUserQuery.cs | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUser/GetSystemUserQuery.cs` | Single query |
| GetSystemUserHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUser/GetSystemUserHandler.cs` | Handler |
| InviteSystemUserCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/InviteSystemUser/InviteSystemUserCommand.cs` | Invite command |
| InviteSystemUserHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/InviteSystemUser/InviteSystemUserHandler.cs` | Handler |
| InviteSystemUserValidator.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/InviteSystemUser/InviteSystemUserValidator.cs` | Validation |
| UpdateSystemUserCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdateSystemUser/UpdateSystemUserCommand.cs` | Update command |
| UpdateSystemUserHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdateSystemUser/UpdateSystemUserHandler.cs` | Handler |
| UpdateSystemUserValidator.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdateSystemUser/UpdateSystemUserValidator.cs` | Validation |
| UpdateSystemUserPermissionsCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdatePermissions/UpdateSystemUserPermissionsCommand.cs` | Permissions command |
| UpdateSystemUserPermissionsHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdatePermissions/UpdateSystemUserPermissionsHandler.cs` | Handler |
| DeleteSystemUserCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/DeleteSystemUser/DeleteSystemUserCommand.cs` | Delete command |
| DeleteSystemUserHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/DeleteSystemUser/DeleteSystemUserHandler.cs` | Handler |
| SystemUserDto.cs | `src/ExoAuth.Application/Features/SystemUsers/Models/SystemUserDto.cs` | Response DTOs |

**System Permissions Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| GetSystemPermissionsQuery.cs | `src/ExoAuth.Application/Features/SystemPermissions/Queries/GetSystemPermissions/GetSystemPermissionsQuery.cs` | List query |
| GetSystemPermissionsHandler.cs | `src/ExoAuth.Application/Features/SystemPermissions/Queries/GetSystemPermissions/GetSystemPermissionsHandler.cs` | Handler |
| SystemPermissionDto.cs | `src/ExoAuth.Application/Features/SystemPermissions/Models/SystemPermissionDto.cs` | Response DTO |

**System Audit Logs Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| GetSystemAuditLogsQuery.cs | `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetSystemAuditLogs/GetSystemAuditLogsQuery.cs` | List query |
| GetSystemAuditLogsHandler.cs | `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetSystemAuditLogs/GetSystemAuditLogsHandler.cs` | Handler |
| GetAuditLogFiltersQuery.cs | `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetAuditLogFilters/GetAuditLogFiltersQuery.cs` | Filters query |
| GetAuditLogFiltersHandler.cs | `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetAuditLogFilters/GetAuditLogFiltersHandler.cs` | Handler |
| SystemAuditLogDto.cs | `src/ExoAuth.Application/Features/SystemAuditLogs/Models/SystemAuditLogDto.cs` | Response DTO |

**Common/Interfaces:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| IAppDbContext.cs | `src/ExoAuth.Application/Common/Interfaces/IAppDbContext.cs` | Database context interface (Clean Architecture) |
| IPasswordHasher.cs | `src/ExoAuth.Application/Common/Interfaces/IPasswordHasher.cs` | Password hashing interface |
| ITokenService.cs | `src/ExoAuth.Application/Common/Interfaces/ITokenService.cs` | JWT generation interface |
| IAuditService.cs | `src/ExoAuth.Application/Common/Interfaces/IAuditService.cs` | Audit logging interface |
| IEmailService.cs | `src/ExoAuth.Application/Common/Interfaces/IEmailService.cs` | Email sending interface |
| ISystemUserRepository.cs | `src/ExoAuth.Application/Common/Interfaces/ISystemUserRepository.cs` | Repository interface |
| IPermissionCacheService.cs | `src/ExoAuth.Application/Common/Interfaces/IPermissionCacheService.cs` | Permission caching interface |
| IBruteForceProtectionService.cs | `src/ExoAuth.Application/Common/Interfaces/IBruteForceProtectionService.cs` | Brute force protection interface |
| ITokenBlacklistService.cs | `src/ExoAuth.Application/Common/Interfaces/ITokenBlacklistService.cs` | Token blacklist interface |
| IEmailTemplateService.cs | `src/ExoAuth.Application/Common/Interfaces/IEmailTemplateService.cs` | Email template rendering interface |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| SystemUserConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs` | EF Config |
| SystemPermissionConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemPermissionConfiguration.cs` | EF Config |
| SystemUserPermissionConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemUserPermissionConfiguration.cs` | EF Config |
| SystemAuditLogConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemAuditLogConfiguration.cs` | EF Config |
| SystemInviteConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemInviteConfiguration.cs` | EF Config |
| RefreshTokenConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | EF Config |
| SystemUserRepository.cs | `src/ExoAuth.Infrastructure/Persistence/Repositories/SystemUserRepository.cs` | Repository |
| PasswordHasher.cs | `src/ExoAuth.Infrastructure/Services/PasswordHasher.cs` | Argon2 implementation |
| TokenService.cs | `src/ExoAuth.Infrastructure/Services/TokenService.cs` | JWT implementation |
| AuditService.cs | `src/ExoAuth.Infrastructure/Services/AuditService.cs` | Audit implementation |
| EmailService.cs | `src/ExoAuth.Infrastructure/Services/EmailService.cs` | Email via RabbitMQ |
| PermissionCacheService.cs | `src/ExoAuth.Infrastructure/Services/PermissionCacheService.cs` | Redis permission caching |
| BruteForceProtectionService.cs | `src/ExoAuth.Infrastructure/Services/BruteForceProtectionService.cs` | Redis brute force protection |
| TokenBlacklistService.cs | `src/ExoAuth.Infrastructure/Services/TokenBlacklistService.cs` | Redis token blacklist |
| EmailTemplateService.cs | `src/ExoAuth.Infrastructure/Services/EmailTemplateService.cs` | Email template rendering |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| AuthController.cs | `src/ExoAuth.Api/Controllers/AuthController.cs` | Auth endpoints |
| SystemUsersController.cs | `src/ExoAuth.Api/Controllers/SystemUsersController.cs` | System users endpoints |
| SystemPermissionsController.cs | `src/ExoAuth.Api/Controllers/SystemPermissionsController.cs` | Permissions endpoints |
| SystemAuditLogsController.cs | `src/ExoAuth.Api/Controllers/SystemAuditLogsController.cs` | Audit logs endpoints |
| SystemPermissionAttribute.cs | `src/ExoAuth.Api/Filters/SystemPermissionAttribute.cs` | Permission check attribute |
| SystemPermissionFilter.cs | `src/ExoAuth.Api/Filters/SystemPermissionFilter.cs` | Permission check filter |

### Worker (Email)

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| SendEmailConsumer.cs | `src/ExoAuth.Infrastructure/Messaging/Consumers/SendEmailConsumer.cs` | RabbitMQ consumer |
| EmailMessage.cs | `src/ExoAuth.Application/Common/Messages/EmailMessage.cs` | Message model |

### Email Templates

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| system-invite.html | `templates/emails/en/system-invite.html` | Einladungs-E-Mail Template (English) |
| system-invite.html | `templates/emails/de/system-invite.html` | Einladungs-E-Mail Template (German) |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | DbSets hinzufügen für alle neuen Entities |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Services registrieren (PasswordHasher, TokenService, AuditService, EmailService, Repositories, Redis Services) |
| `src/ExoAuth.Application/DependencyInjection.cs` | Ggf. neue Services registrieren |
| `src/ExoAuth.Api/Program.cs` | JWT Authentication konfigurieren + Permission Cache Invalidation bei Startup |
| `src/ExoAuth.Api/appsettings.json` | Email settings, JWT settings, Redis Cache settings, Brute Force settings hinzufügen |
| `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | Neue Error Codes hinzufügen |
| `src/ExoAuth.Infrastructure/Caching/RedisCacheService.cs` | `DeleteByPatternAsync` Methode hinzufügen falls nicht vorhanden |

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| System.IdentityModel.Tokens.Jwt | 8.x | ExoAuth.Infrastructure | JWT Token Generierung |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.x | ExoAuth.Api | JWT Authentication Middleware |

> **Hinweis:** Argon2 Package bereits vorhanden: `Konscious.Security.Cryptography.Argon2`

## 9. Implementation Reihenfolge

1. [x] **Domain**: Entities erstellen (SystemUser, SystemPermission, SystemUserPermission, SystemAuditLog, SystemInvite, RefreshToken)
2. [x] **Domain**: Enums und Constants erstellen (UserType, SystemPermissions)
3. [x] **Infrastructure**: EF Configurations erstellen
4. [x] **Infrastructure**: AppDbContext updaten (DbSets hinzufügen)
5. [x] **Infrastructure**: Migrations erstellen und ausführen
6. [x] **Infrastructure**: Seed SystemPermissions
7. [x] **Application**: Interfaces erstellen (IPasswordHasher, ITokenService, IAuditService, IEmailService, ISystemUserRepository, IPermissionCacheService, IBruteForceProtectionService, ITokenBlacklistService, IEmailTemplateService)
8. [x] **Infrastructure**: Core Services implementieren (PasswordHasher, TokenService, AuditService)
9. [x] **Infrastructure**: Redis Services implementieren (PermissionCacheService, BruteForceProtectionService, TokenBlacklistService)
10. [x] **Infrastructure**: Email Services implementieren (EmailService, EmailTemplateService)
11. [x] **Infrastructure**: Repository implementieren (SystemUserRepository)
12. [x] **Infrastructure**: DependencyInjection updaten (alle neuen Services registrieren)
13. [x] **Application**: Auth Commands/Queries implementieren (Register, Login, Refresh, Logout, AcceptInvite, GetCurrentUser)
14. [x] **Application**: SystemUsers Commands/Queries implementieren
15. [x] **Application**: SystemPermissions Queries implementieren
16. [x] **Application**: SystemAuditLogs Queries implementieren
17. [x] **Api**: JWT Authentication in Program.cs konfigurieren
18. [x] **Api**: Permission Cache Invalidation bei Startup hinzufügen
19. [x] **Api**: Permission Filter/Attribute erstellen
20. [x] **Api**: Controllers erstellen (Auth, SystemUsers, SystemPermissions, SystemAuditLogs)
21. [x] **Infrastructure**: Email Consumer für RabbitMQ
22. [x] **Templates**: Email Templates erstellen (EN + DE)
23. [x] **Api**: Auto-Migration bei Startup hinzufügen
24. [x] **Application**: IAppDbContext Interface erstellen (Clean Architecture Fix)
25. [x] **Tests**: Unit Tests (106 Tests - Auth, SystemUsers, Services) ✅
26. [ ] **Tests**: Integration Tests (inkl. Redis Mocking)
27. [ ] **Standards updaten**: task_standards_backend.md + coding_standards_backend.md aktualisieren

## 10. Tests

### Unit Tests ✅ (106 Tests)

**Helper Files erstellt:**
| Datei | Beschreibung |
|-------|--------------|
| `tests/ExoAuth.UnitTests/Helpers/MockDbContext.cs` | Mock DbContext mit async DbSet Support |
| `tests/ExoAuth.UnitTests/Helpers/TestDataFactory.cs` | Factory für Test-Entities |
| `tests/ExoAuth.UnitTests/Helpers/AsyncQueryableExtensions.cs` | IAsyncQueryProvider für EF Core Mocking |

**Test Files erstellt:**
| Test Datei | Was wird getestet? | Tests |
|------------|-------------------|-------|
| `tests/ExoAuth.UnitTests/Features/Auth/RegisterHandlerTests.cs` | Register: First user gets all permissions, registration closed | 3 |
| `tests/ExoAuth.UnitTests/Features/Auth/LoginHandlerTests.cs` | Login: Credentials, blocked user, inactive user, brute force | 6 |
| `tests/ExoAuth.UnitTests/Features/Auth/RefreshTokenHandlerTests.cs` | Refresh: Valid/invalid/revoked/blacklisted tokens | 8 |
| `tests/ExoAuth.UnitTests/Features/SystemUsers/InviteSystemUserHandlerTests.cs` | Invite: Email unique, permissions, email sent | 6 |
| `tests/ExoAuth.UnitTests/Features/SystemUsers/UpdatePermissionsHandlerTests.cs` | Permissions: Update, last holder check, cache invalidation | 6 |
| `tests/ExoAuth.UnitTests/Services/PasswordHasherTests.cs` | Argon2id hashing und verification | 6 |
| `tests/ExoAuth.UnitTests/Services/TokenServiceTests.cs` | JWT generation, validation, expiration, claims | 13 |
| `tests/ExoAuth.UnitTests/Services/BruteForceProtectionServiceTests.cs` | Counting, blocking threshold, reset | 13 |
| `tests/ExoAuth.UnitTests/Services/PermissionCacheServiceTests.cs` | Cache get/set, invalidation, GetOrSet | 6 |
| `tests/ExoAuth.UnitTests/Services/EmailTemplateServiceTests.cs` | Template rendering, language fallback, variables | 6 |

### Integration Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `tests/ExoAuth.IntegrationTests/Features/Auth/RegisterTests.cs` | Full register flow |
| `tests/ExoAuth.IntegrationTests/Features/Auth/LoginTests.cs` | Full login flow |
| `tests/ExoAuth.IntegrationTests/Features/Auth/InviteFlowTests.cs` | Invite → Accept → Login |
| `tests/ExoAuth.IntegrationTests/Features/SystemUsers/SystemUsersTests.cs` | CRUD operations |
| `tests/ExoAuth.IntegrationTests/Features/SystemAuditLogs/AuditLogTests.cs` | Actions are logged |
| `tests/ExoAuth.IntegrationTests/Features/Auth/BruteForceTests.cs` | Login blocking after failed attempts |

## 11. Token Strategie (HttpOnly Cookies)

**Für Dashboard (Browser) → HttpOnly Cookies**

| Cookie | Path | TTL | Zweck |
|--------|------|-----|-------|
| `access_token` | `/api` | 15 min | JWT Access Token |
| `refresh_token` | `/api/auth` | 30 Tage | Refresh Token |

**Cookie Optionen:**
```csharp
new CookieOptions
{
    HttpOnly = true,           // JS kann nicht zugreifen
    Secure = true,             // Nur HTTPS
    SameSite = SameSiteMode.Strict,  // CSRF Schutz
}
```

**Warum Cookies:**
- XSS kann Tokens nicht stehlen
- Browser sendet automatisch
- Kein Token-Handling im Frontend

**Frontend muss nur:**
```typescript
fetch('/api/...', { credentials: 'include' })
```

## 12. Konfiguration

### appsettings.json Additions

```json
{
  "ExoAuth": {
    "DeploymentMode": "SaaS"  // "SaaS" | "SingleTenant" | "MultiTenant"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-long-here",
    "Issuer": "ExoAuth",
    "Audience": "ExoAuth",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "Email": {
    "Provider": "SMTP",
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUsername": "user@example.com",
    "SmtpPassword": "password",
    "SmtpUseSsl": true,
    "FromEmail": "noreply@exoauth.com",
    "FromName": "ExoAuth"
  },
  "SystemInvite": {
    "ExpirationHours": 24,
    "BaseUrl": "https://exoauth.com"
  },
  "Password": {
    "MinLength": 12,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialChar": true
  },
  "Cache": {
    "PermissionCacheTtlMinutes": 60
  },
  "BruteForce": {
    "MaxAttempts": 5,
    "LockoutMinutes": 15
  }
}
```

## 12. Redis Caching Strategy

### Redis Keys

| Key Pattern | Beschreibung | TTL |
|-------------|--------------|-----|
| `user:permissions:{userId}` | Gecachte Permissions eines Users | 1 Stunde |
| `revoked:refresh:{tokenId}` | Blacklist für revoked Refresh Tokens | Bis Token Expiration |
| `login:attempts:{email}` | Zähler für fehlgeschlagene Login-Versuche | 15 Minuten |
| `login:blocked:{email}` | Markiert blockierte E-Mail Adressen | 15 Minuten |

### Permission Cache Flow

```
┌─────────────────────────────────────────────────────────────┐
│  Request mit JWT kommt rein                                 │
├─────────────────────────────────────────────────────────────┤
│  1. JWT validieren (Signatur, Expiration)                   │
│  2. UserId aus JWT extrahieren                              │
│  3. Redis: GET user:permissions:{userId}                    │
│     ├── HIT → Permissions aus Cache verwenden               │
│     └── MISS → DB Query, dann in Redis cachen (TTL: 1h)     │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Permission Change (PUT /api/system/users/{id}/permissions) │
├─────────────────────────────────────────────────────────────┤
│  1. Permissions in DB updaten                               │
│  2. Redis: DEL user:permissions:{userId}                    │
│  3. Nächster Request lädt fresh aus DB                      │
└─────────────────────────────────────────────────────────────┘
```

### Token Blacklist Flow

```
┌─────────────────────────────────────────────────────────────┐
│  Logout / Token Revoke                                      │
├─────────────────────────────────────────────────────────────┤
│  1. RefreshToken in DB als revoked markieren                │
│  2. Redis: SET revoked:refresh:{tokenId} "1"                │
│     TTL = Token ExpiresAt - Now                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Refresh Token Validation                                   │
├─────────────────────────────────────────────────────────────┤
│  1. Redis: EXISTS revoked:refresh:{tokenId}                 │
│     ├── EXISTS → Token ist revoked, reject                  │
│     └── NOT EXISTS → DB Check (fallback)                    │
└─────────────────────────────────────────────────────────────┘
```

### Brute Force Protection Flow

```
┌─────────────────────────────────────────────────────────────┐
│  Login Attempt                                              │
├─────────────────────────────────────────────────────────────┤
│  1. Redis: EXISTS login:blocked:{email}                     │
│     └── EXISTS → 429 Too Many Attempts                      │
│                                                             │
│  2. Credentials prüfen                                      │
│     ├── SUCCESS → Redis: DEL login:attempts:{email}         │
│     └── FAILURE → Redis: INCR login:attempts:{email}        │
│                   Redis: EXPIRE login:attempts:{email} 900  │
│                   Wenn Count >= 5:                          │
│                     Redis: SET login:blocked:{email} "1"    │
│                     Redis: EXPIRE login:blocked:{email} 900 │
└─────────────────────────────────────────────────────────────┘
```

### Startup Cache Invalidation

```csharp
// Program.cs - Bei Application Start
public static async Task Main(string[] args)
{
    var app = builder.Build();

    // Invalidate permission cache on startup
    using (var scope = app.Services.CreateScope())
    {
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.DeleteByPatternAsync("user:permissions:*");

        // Optional: Log
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Permission cache invalidated on startup");
    }

    await app.RunAsync();
}
```

### Graceful Degradation

Wenn Redis nicht verfügbar ist:
- **Permission Check**: Fallback auf DB Query
- **Token Blacklist**: Nur DB Check
- **Brute Force**: Deaktiviert (nur Logging)

```csharp
public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
{
    try
    {
        // Try Redis first
        var cached = await _cache.GetAsync<List<string>>($"user:permissions:{userId}");
        if (cached != null) return cached;
    }
    catch (RedisConnectionException)
    {
        // Redis down, continue to DB
        _logger.LogWarning("Redis unavailable, falling back to DB");
    }

    // Fallback: DB query
    var permissions = await _db.SystemUserPermissions
        .Where(x => x.UserId == userId)
        .Select(x => x.Permission.Name)
        .ToListAsync();

    // Try to cache (ignore if Redis down)
    try
    {
        await _cache.SetAsync($"user:permissions:{userId}", permissions, TimeSpan.FromHours(1));
    }
    catch { /* ignore */ }

    return permissions;
}
```

## 13. Email Templates

### Template Struktur

```
templates/
├── emails/
│   ├── en/
│   │   └── system-invite.html
│   └── de/
│       └── system-invite.html
```

### Template Engine

Einfache `{{variable}}` Replacement (kein komplexes Templating für jetzt):

```csharp
public string RenderTemplate(string templatePath, Dictionary<string, string> variables)
{
    var content = File.ReadAllText(templatePath);
    foreach (var (key, value) in variables)
    {
        content = content.Replace($"{{{{{key}}}}}", value);
    }
    return content;
}
```

### System Invite Template

**Variables:**
| Variable | Beschreibung | Beispiel |
|----------|--------------|----------|
| `{{firstName}}` | Vorname des Eingeladenen | Max |
| `{{lastName}}` | Nachname des Eingeladenen | Mustermann |
| `{{inviterName}}` | Name des Einladenden | Admin User |
| `{{inviteLink}}` | Link zur Einladung | https://exoauth.com/invite?token=abc123 |
| `{{expirationHours}}` | Gültigkeit in Stunden | 24 |
| `{{year}}` | Aktuelles Jahr | 2025 |

**English Template (`templates/emails/en/system-invite.html`):**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>You're invited to ExoAuth</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
    <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
        <h1 style="color: #2563eb;">Welcome to ExoAuth!</h1>

        <p>Hello {{firstName}},</p>

        <p>{{inviterName}} has invited you to join the ExoAuth administration team.</p>

        <p>Click the button below to accept your invitation and set up your account:</p>

        <p style="text-align: center; margin: 30px 0;">
            <a href="{{inviteLink}}"
               style="background-color: #2563eb; color: white; padding: 12px 24px;
                      text-decoration: none; border-radius: 6px; display: inline-block;">
                Accept Invitation
            </a>
        </p>

        <p style="color: #666; font-size: 14px;">
            This invitation will expire in {{expirationHours}} hours.
        </p>

        <p style="color: #666; font-size: 14px;">
            If you didn't expect this invitation, you can safely ignore this email.
        </p>

        <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">

        <p style="color: #999; font-size: 12px;">
            © {{year}} ExoAuth. All rights reserved.
        </p>
    </div>
</body>
</html>
```

**German Template (`templates/emails/de/system-invite.html`):**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Einladung zu ExoAuth</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
    <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
        <h1 style="color: #2563eb;">Willkommen bei ExoAuth!</h1>

        <p>Hallo {{firstName}},</p>

        <p>{{inviterName}} hat Sie eingeladen, dem ExoAuth-Administrationsteam beizutreten.</p>

        <p>Klicken Sie auf den Button unten, um Ihre Einladung anzunehmen und Ihr Konto einzurichten:</p>

        <p style="text-align: center; margin: 30px 0;">
            <a href="{{inviteLink}}"
               style="background-color: #2563eb; color: white; padding: 12px 24px;
                      text-decoration: none; border-radius: 6px; display: inline-block;">
                Einladung annehmen
            </a>
        </p>

        <p style="color: #666; font-size: 14px;">
            Diese Einladung läuft in {{expirationHours}} Stunden ab.
        </p>

        <p style="color: #666; font-size: 14px;">
            Wenn Sie diese Einladung nicht erwartet haben, können Sie diese E-Mail ignorieren.
        </p>

        <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">

        <p style="color: #999; font-size: 12px;">
            © {{year}} ExoAuth. Alle Rechte vorbehalten.
        </p>
    </div>
</body>
</html>
```

### Language Selection

```csharp
public string GetTemplatePath(string templateName, string language = "en")
{
    var path = Path.Combine("templates", "emails", language, $"{templateName}.html");

    // Fallback to English if language not found
    if (!File.Exists(path))
    {
        path = Path.Combine("templates", "emails", "en", $"{templateName}.html");
    }

    return path;
}
```

### Email Sending via RabbitMQ

```csharp
// Message published to RabbitMQ
public record SendEmailMessage(
    string To,
    string Subject,
    string TemplateName,
    string Language,
    Dictionary<string, string> Variables
);

// Usage
await _messageBus.PublishAsync(new SendEmailMessage(
    To: "user@example.com",
    Subject: "You're invited to ExoAuth",
    TemplateName: "system-invite",
    Language: "en",
    Variables: new Dictionary<string, string>
    {
        ["firstName"] = "Max",
        ["lastName"] = "Mustermann",
        ["inviterName"] = "Admin User",
        ["inviteLink"] = "https://exoauth.com/invite?token=abc123",
        ["expirationHours"] = "24",
        ["year"] = "2025"
    }
));
```

## 14. Audit Log Actions

| Action | Wann | Details |
|--------|------|---------|
| `system.user.registered` | Erster User registriert | userId |
| `system.user.login` | Erfolgreicher Login | userId |
| `system.user.login.failed` | Fehlgeschlagener Login | email, reason |
| `system.user.logout` | Logout | userId |
| `system.user.invited` | User eingeladen | inviteId, email, invitedBy |
| `system.user.invite.accepted` | Einladung angenommen | userId, inviteId |
| `system.user.updated` | User Daten geändert | userId, changes |
| `system.user.permissions.updated` | Permissions geändert | userId, added[], removed[] |
| `system.user.deleted` | User gelöscht/deaktiviert | userId, deletedBy |
| `system.token.refreshed` | Token erneuert | userId |
| `system.login.blocked` | User wegen zu vieler Versuche blockiert | email, attempts |

## 15. Force Re-Auth on Permission Change

### Warum?

Wenn Permissions geändert werden, hat der User noch bis zu 15 Minuten Zugriff mit alten Permissions (Access Token Lifetime). Das ist ein Sicherheitsrisiko.

**Lösung:** Force Re-Auth Flag in Redis → User wird sofort ausgeloggt.

### Flow

```
1. Admin ändert User Permissions
   → Permissions in DB aktualisiert
   → Permission Cache invalidiert
   → Redis: SET user:force-reauth:{userId} "1" (TTL: 15 min)
   → Alle Refresh Tokens revoked + blacklisted

2. User macht nächsten Request (sofort)
   → Middleware checkt: EXISTS user:force-reauth:{userId}?
   → Key existiert → 401 Unauthorized

3. User versucht Refresh
   → Refresh Token revoked → 401

4. User muss neu einloggen
   → Bei erfolgreichem Login: DEL user:force-reauth:{userId}
   → Neue Permissions aus DB
   → Alles gut ✅
```

### Redis Keys

| Key | Wert | TTL | Zweck |
|-----|------|-----|-------|
| `user:force-reauth:{userId}` | `"1"` | 15 min | Erzwingt Re-Login nach Permission-Änderung |

### Files zu erstellen

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| IForceReauthService.cs | `src/ExoAuth.Application/Common/Interfaces/IForceReauthService.cs` | Interface |
| ForceReauthService.cs | `src/ExoAuth.Infrastructure/Services/ForceReauthService.cs` | Redis Implementation |
| ForceReauthMiddleware.cs | `src/ExoAuth.Api/Middleware/ForceReauthMiddleware.cs` | Middleware für Check |

### Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `UpdateSystemUserPermissionsHandler.cs` | Force-Reauth setzen + Refresh Tokens revoken |
| `LoginHandler.cs` | Force-Reauth Key löschen bei erfolgreichem Login |
| `DependencyInjection.cs` | ForceReauthService registrieren |
| `Program.cs` | ForceReauthMiddleware hinzufügen |

### Warum kein Version Counter?

| Approach | Problem |
|----------|---------|
| Version Counter im JWT | Wächst ewig (1, 2, 3... 923232), muss in JWT gespeichert werden |
| Timestamp Vergleich | Edge Cases, Zeitzone-Probleme |
| **Force-Reauth Flag** | ✅ Simpel, selbst-löschend (TTL), kein State im JWT nötig |

### Skalierung

- Key existiert nur wenn Permission geändert wurde (nicht für alle User)
- Redis `EXISTS` = O(1), ~0.1ms
- Auto-Cleanup via TTL (15 min)
- 100.000 Users, 5 Permission-Änderungen = 5 Keys

### Implementation Reihenfolge

1. [ ] **Interface**: IForceReauthService erstellen
2. [ ] **Service**: ForceReauthService implementieren
3. [ ] **Middleware**: ForceReauthMiddleware erstellen
4. [ ] **Handler**: UpdateSystemUserPermissionsHandler updaten (set flag + revoke tokens)
5. [ ] **Handler**: LoginHandler updaten (clear flag on login)
6. [ ] **DI**: Service registrieren
7. [ ] **Program.cs**: Middleware hinzufügen
8. [ ] **Tests**: Unit Tests schreiben

---

## 16. Error Audit Logging

### Warum?

Für Security Auditing müssen nicht nur erfolgreiche Aktionen, sondern auch Fehler/Zugriffsverweigerungen geloggt werden.

### Was wird geloggt?

| Error Type | HTTP Status | Log to AuditLog? | Warum |
|------------|-------------|------------------|-------|
| Failed Login | 401 | ✅ Bereits implementiert | Brute Force Detection |
| Forbidden | 403 | ✅ Ja | Unauthorized Access Attempts |
| Internal Error | 500 | ✅ Ja | System Health Monitoring |
| Validation | 400 | ❌ Nein | Zu noisy, User-Fehler |
| Not Found | 404 | ❌ Nein | Zu noisy, nicht security-relevant |
| Unauthorized | 401 | ⚠️ Nur bei Force-Reauth | Normale Token-Expiry ist zu noisy |

### Neue Audit Actions

| Action | Wann | Details |
|--------|------|---------|
| `system.access.forbidden` | 403 Response | userId, endpoint, requiredPermission, ipAddress |
| `system.error.internal` | 500 Response | endpoint, errorType, requestId, ipAddress |
| `system.access.forced_reauth` | Force-Reauth 401 | userId, endpoint, ipAddress |

### Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `ExceptionMiddleware.cs` | AuditService injecten, bei 403/500 loggen |
| `ForceReauthMiddleware.cs` | Bei Force-Reauth 401 loggen |
| `AuditActions.cs` | Neue Action Constants hinzufügen |

### Implementation

```csharp
// In ExceptionMiddleware - bei 403:
await _auditService.LogAsync(
    AuditActions.AccessForbidden,
    userId,        // aus JWT falls vorhanden
    null,
    null,
    new {
        Endpoint = context.Request.Path,
        RequiredPermission = ...,
        IpAddress = GetIpAddress(context)
    }
);

// Bei 500:
await _auditService.LogAsync(
    AuditActions.InternalError,
    userId,
    null,
    null,
    new {
        Endpoint = context.Request.Path,
        ErrorType = exception.GetType().Name,
        RequestId = context.TraceIdentifier,
        IpAddress = GetIpAddress(context)
    }
);
```

### Implementation Reihenfolge

1. [ ] **Constants**: Neue AuditActions hinzufügen
2. [ ] **Middleware**: ExceptionMiddleware updaten (inject AuditService)
3. [ ] **Middleware**: ForceReauthMiddleware mit Audit Logging
4. [ ] **Tests**: Unit Tests für Audit Logging

---

## 17. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_backend.md` aktualisiert (neue Files, Packages, File Tree)
- [ ] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [ ] Code reviewed
- [ ] Swagger/OpenAPI dokumentiert
- [ ] README mit Setup-Anleitung für Development

---

## Notizen

- **Globally Unique Email**: E-Mail muss unique sein über SystemUsers UND (später) OrganizationUsers
- **First User = SystemUser**: Erster registrierter User bekommt automatisch alle System-Permissions
- **One Login Endpoint**: `/api/auth/login` prüft SystemUsers (später auch OrgUsers), JWT enthält `type` Claim
- **HttpOnly Cookies**: Access + Refresh Token als HttpOnly Cookies (nicht im Response Body)
- **Password Requirements**: Min 12 chars, upper, lower, digit, special (für SystemUsers)
- **Invite Expiration**: 24 Stunden
- **Cursor Pagination**: Für Infinite Scroll optimiert
- **Multi-Column Sorting**: `sort=field1:asc,field2:desc`
- **Search**: Durchsucht relevante Felder (Email, Name, etc.)
- **Redis Caching**: Permissions gecached (1h TTL), Invalidierung bei Änderung + Startup
- **Brute Force Protection**: 5 Versuche → 15 Min Block (via Redis)
- **Token Blacklist**: Revoked Refresh Tokens in Redis für schnelle Prüfung
- **Graceful Degradation**: Bei Redis-Ausfall Fallback auf DB
- **Email Templates**: EN + DE, simple `{{variable}}` replacement, via RabbitMQ
- **Force Re-Auth**: Bei Permission-Änderung sofortige Invalidierung via Redis Flag (Section 15)
- **Error Audit Logging**: 403/500 Errors werden in SystemAuditLog geloggt für Security Auditing (Section 16)

---

**Letzte Änderung:** 2025-12-26
**Status:** In Progress (Unit Tests ✅ 106 Tests, Force Re-Auth pending, Error Audit Logging pending, Integration Tests pending)

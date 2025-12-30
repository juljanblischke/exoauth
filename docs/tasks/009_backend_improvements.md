# Task 009: Backend Improvements - Reauth, Emails, Lists & Cleanup

## 1. Übersicht

**Was wird gebaut?**
Verbesserungen an bestehenden Backend-Systemen: Session-basierte Reauth, Email-Subjects Internationalisierung, List-Performance & Filtering, Invite-Editing und automatische Cleanup-Jobs.

**Warum?**
- Reauth-Bug: Ein Device re-auth cleared Flag für alle Sessions
- Email-Subjects hardcoded mit if/else - skaliert nicht für 20+ Sprachen
- Invite-Liste lädt ALLE Daten in Memory - kritisches Performance-Problem
- Fehlende Filter/Sortierung in Listen
- Expired/Revoked Invites bleiben ewig - unnötiger Datenmüll

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich Invites bearbeiten können (Name, Permissions) ohne neue Email zu senden
- Als Admin möchte ich User nach Status filtern können (active, anonymized, locked, mfa)
- Als Admin möchte ich Invites sortieren können
- Als System möchte ich alte Invites automatisch aufräumen

### Akzeptanzkriterien
- [x] Session-Reauth: Jede Session muss individuell re-authentifizieren
- [x] Email-Subjects kommen aus JSON-Dateien pro Sprache
- [x] Alle Emails haben korrektes Jahr im Footer
- [x] Invites können bearbeitet werden (pending only)
- [x] User-Liste hat Filter für isActive, isAnonymized, isLocked, mfaEnabled
- [x] User-Liste versteckt anonymisierte User standardmäßig
- [x] Invite-Liste filtert in SQL, nicht in Memory
- [x] Invite-Liste hat Sortierung
- [x] Invite-Liste versteckt expired/revoked standardmäßig
- [x] Background Job löscht expired/revoked Invites nach 30 Tagen

### Edge Cases / Error Handling
- Was passiert wenn Invite schon accepted? → 400 Bad Request
- Was passiert wenn Invite expired/revoked? → 400 Bad Request
- Was passiert wenn Email-Subject JSON fehlt? → Fallback zu en-US
- Was passiert wenn Jahr-Variable fehlt? → Template zeigt {{year}} literal

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| PATCH | /api/system-invites/{id} | `{ firstName?, lastName?, permissionIds? }` | `SystemInviteDto` | Invite bearbeiten |
| GET | /api/system-users | Query: `isActive`, `isAnonymized`, `isLocked`, `mfaEnabled` | `CursorPagedList<SystemUserDto>` | Erweiterte Filter |
| GET | /api/system-invites | Query: `sort` | `CursorPagedList<SystemInviteDto>` | Mit Sortierung |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `INVITE_NOT_EDITABLE` | 400 | Invite ist nicht mehr pending (accepted/expired/revoked) |

> Nach Completion: Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

### Neue Indexes
| Tabelle | Index | Beschreibung |
|---------|-------|--------------|
| system_users | IsAnonymized | Filter Performance |
| system_users | IsLocked | Filter Performance |
| system_invites | CreatedAt | Cursor Pagination & Sorting |

### Migrations
- [ ] Migration Name: `AddUserAndInviteIndexes`

## 6. Files zu erstellen

### Templates (Email Subjects)
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| subjects.json | `templates/emails/en-US/subjects.json` | English subjects |
| subjects.json | `templates/emails/de-DE/subjects.json` | German subjects |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| UpdateInviteCommand.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/UpdateInvite/` | Command |
| UpdateInviteHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/UpdateInvite/` | Handler |
| UpdateInviteValidator.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/UpdateInvite/` | Validator |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| InviteCleanupService.cs | `src/ExoAuth.Infrastructure/Services/` | Background Job Service |
| InviteCleanupBackgroundService.cs | `src/ExoAuth.Infrastructure/Services/` | Hosted Service |

## 7. Files zu ändern

### Phase 1: Session-Based Reauth
| Datei | Was ändern? |
|-------|-------------|
| `IForceReauthService.cs` | Methoden auf sessionId umstellen |
| `ForceReauthService.cs` | Redis key: `session:force-reauth:{sessionId}` statt `user:force-reauth:{userId}` |
| `ForceReauthMiddleware.cs` | SessionId statt UserId verwenden |
| `UpdateSystemUserPermissionsHandler.cs` | Alle Sessions des Users markieren |
| `ResetUserMfaHandler.cs` | Alle Sessions des Users markieren |
| `ResetPasswordHandler.cs` | Alle Sessions des Users markieren |
| `LoginHandler.cs` | ClearFlagAsync mit sessionId |
| `MfaVerifyHandler.cs` | ClearFlagAsync mit sessionId |
| `ForceReauthServiceTests.cs` | Tests anpassen |

### Phase 2: Email Subjects & Year Fix
| Datei | Was ändern? |
|-------|-------------|
| `IEmailTemplateService.cs` | `GetSubject(templateName, language)` Methode hinzufügen |
| `EmailTemplateService.cs` | Subject aus JSON laden |
| `EmailService.cs` | Hardcoded subjects entfernen, Service nutzen |
| `MfaDisableHandler.cs` | `year` Variable hinzufügen |
| `MfaConfirmHandler.cs` | `year` Variable hinzufügen |
| Andere Handler | `year` Variable prüfen/hinzufügen |
| `EmailTemplateServiceTests.cs` | Tests für GetSubject |

### Phase 3: User List Filters & Indexes
| Datei | Was ändern? |
|-------|-------------|
| `GetSystemUsersQuery.cs` | Filter Parameter hinzufügen |
| `ISystemUserRepository.cs` | GetPagedAsync Parameter erweitern |
| `SystemUserRepository.cs` | Filter Logic implementieren |
| `SystemUsersController.cs` | Query Parameter annehmen |
| `SystemUserConfiguration.cs` | Indexes hinzufügen |

### Phase 4: Invite List Performance & Sorting
| Datei | Was ändern? |
|-------|-------------|
| `GetSystemInvitesQuery.cs` | Sort Parameter hinzufügen |
| `GetSystemInvitesHandler.cs` | SQL-basiertes Filtering, Sorting implementieren |
| `SystemInviteConfiguration.cs` | CreatedAt Index hinzufügen |
| `SystemInvitesController.cs` | Sort Parameter annehmen |

### Phase 5: Edit Invites
| Datei | Was ändern? |
|-------|-------------|
| `SystemInvitesController.cs` | PATCH Endpoint hinzufügen |
| `ErrorCodes.cs` | INVITE_NOT_EDITABLE hinzufügen |
| `AuthException.cs` | InviteNotEditableException hinzufügen |

### Phase 6: Invite Cleanup Job
| Datei | Was ändern? |
|-------|-------------|
| `DependencyInjection.cs` (Infrastructure) | Hosted Service registrieren |
| `appsettings.json` | Cleanup Config (Interval, RetentionDays) |

## 8. Neue Packages

Keine neuen Packages erforderlich.

## 9. Implementation Reihenfolge

### Phase 1: Session-Based Reauth ✅
1. [X] `IForceReauthService.cs` - Interface ändern (sessionId statt userId)
2. [X] `ForceReauthService.cs` - Redis key pattern ändern
3. [X] `ForceReauthMiddleware.cs` - SessionId verwenden
4. [X] Handler updaten (Permissions, MfaReset, PasswordReset, Login, MfaVerify)
5. [X] Unit Tests anpassen

### Phase 2: Email Subjects & Year Fix ✅
1. [x] `templates/emails/en-US/subjects.json` erstellen
2. [x] `templates/emails/de-DE/subjects.json` erstellen
3. [x] `IEmailTemplateService.cs` - GetSubject Methode
4. [x] `EmailTemplateService.cs` - JSON laden & parsen
5. [x] `EmailService.cs` - Hardcoded subjects entfernen
6. [x] Handler mit fehlender `year` Variable fixen
7. [x] Unit Tests

### Phase 3: User List Filters & Indexes ✅
1. [x] Migration für neue Indexes
2. [x] `GetSystemUsersQuery.cs` - Filter Parameter
3. [x] `ISystemUserRepository.cs` - Interface erweitern
4. [x] `SystemUserRepository.cs` - Filter implementieren (default: hideAnonymized=true)
5. [x] `SystemUsersController.cs` - Query Parameter
6. [x] Unit Tests

### Phase 4: Invite List Performance & Sorting ✅
1. [x] Migration für CreatedAt Index
2. [x] `GetSystemInvitesQuery.cs` - Sort Parameter, default status filter
3. [x] `GetSystemInvitesHandler.cs` - **KOMPLETT NEU GESCHRIEBEN**
   - Status in SQL filtern (nicht in Memory!)
   - Sorting implementieren
   - Default: hideExpired=true, hideRevoked=true
4. [x] `SystemInvitesController.cs` - Sort Parameter
5. [x] Unit Tests

### Phase 5: Edit Invites ✅
1. [x] `ErrorCodes.cs` + `SystemException.cs` - INVITE_NOT_EDITABLE
2. [x] `UpdateInviteCommand.cs`
3. [x] `UpdateInviteValidator.cs`
4. [x] `UpdateInviteHandler.cs` (nur pending Invites!)
5. [x] `SystemInvitesController.cs` - PATCH Endpoint
6. [x] Unit Tests

### Phase 6: Invite Cleanup Job ✅
1. [x] `appsettings.json` - Config Section
2. [x] `IInviteCleanupService.cs` - Interface
3. [x] `InviteCleanupService.cs` - Delete Logic
4. [x] `InviteCleanupBackgroundService.cs` - Hosted Service (daily run)
5. [x] `DependencyInjection.cs` - Registration
6. [x] Unit Tests

### Phase 7: Finalisierung
1. [ ] Alle Unit Tests grün
2. [ ] `task_standards_backend.md` aktualisieren
3. [ ] `coding_standards_backend.md` aktualisieren (Error Codes)

## 10. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `ForceReauthServiceTests.cs` | Session-based reauth | ~5 |
| `EmailTemplateServiceTests.cs` | GetSubject, JSON loading | ~5 |
| `GetSystemUsersHandlerTests.cs` | New filters | ~8 |
| `GetSystemInvitesHandlerTests.cs` | SQL filtering, sorting | ~10 |
| `UpdateInviteHandlerTests.cs` | Edit invite logic | ~8 |
| `InviteCleanupServiceTests.cs` | Cleanup logic | ~5 |

## 11. Technische Details

### Session-Based Reauth - Redis Keys

**Alt (User-based):**
```
user:force-reauth:{userId}  → TTL 15 min
```

**Neu (Session-based):**
```
session:force-reauth:{sessionId}  → TTL 15 min
```

**Trigger Flow:**
```
Permission Change / MFA Reset / Password Reset
    ↓
Get all active sessionIds for user
    ↓
Set flag for EACH session
    ↓
Each session must re-auth individually
    ↓
On re-auth: clear only THAT session's flag
```

### Email Subjects JSON Structure

```json
// templates/emails/en-US/subjects.json
{
  "system-invite": "Invitation to ExoAuth",
  "password-reset": "Reset your password",
  "password-changed": "Your password was changed",
  "mfa-enabled": "Two-factor authentication enabled",
  "mfa-disabled": "Two-factor authentication disabled",
  "mfa-backup-code-used": "Backup code used for login",
  "mfa-reset-admin": "Your MFA has been reset",
  "new-device-login": "New device login detected",
  "new-location-login": "Login from new location",
  "account-locked": "Account temporarily locked",
  "account-unlocked": "Account unlocked",
  "sessions-revoked-admin": "Your sessions have been revoked"
}
```

### Invite Status SQL Filtering

**Status zu SQL Mapping:**
```csharp
// Statt computed Status property in Memory:
queryable = status switch
{
    "pending" => queryable.Where(i =>
        i.AcceptedAt == null &&
        i.RevokedAt == null &&
        i.ExpiresAt > DateTime.UtcNow),
    "accepted" => queryable.Where(i => i.AcceptedAt != null),
    "revoked" => queryable.Where(i => i.RevokedAt != null),
    "expired" => queryable.Where(i =>
        i.AcceptedAt == null &&
        i.RevokedAt == null &&
        i.ExpiresAt <= DateTime.UtcNow),
    _ => queryable
};
```

### Invite Sorting Options

| Sort Value | SQL |
|------------|-----|
| `email:asc` | `OrderBy(i => i.Email)` |
| `email:desc` | `OrderByDescending(i => i.Email)` |
| `firstName:asc` | `OrderBy(i => i.FirstName)` |
| `lastName:asc` | `OrderBy(i => i.LastName)` |
| `createdAt:asc` | `OrderBy(i => i.CreatedAt)` |
| `createdAt:desc` | `OrderByDescending(i => i.CreatedAt)` (default) |
| `expiresAt:asc` | `OrderBy(i => i.ExpiresAt)` |
| `expiresAt:desc` | `OrderByDescending(i => i.ExpiresAt)` |

### Invite Cleanup Job Config

```json
// appsettings.json
{
  "InviteCleanup": {
    "Enabled": true,
    "RetentionDays": 30,
    "RunIntervalHours": 24
  }
}
```

### User List Default Behavior

```csharp
// Default: verstecke anonymisierte User
public sealed record GetSystemUsersQuery(
    // ... existing params
    bool? IsActive = null,
    bool? IsAnonymized = false,  // DEFAULT: false = hide anonymized
    bool? IsLocked = null,
    bool? MfaEnabled = null
) : IQuery<CursorPagedList<SystemUserDto>>;
```

### Invite List Default Behavior

```csharp
// Default: verstecke expired & revoked
public sealed record GetSystemInvitesQuery(
    // ... existing params
    string? Sort = "createdAt:desc",
    List<string>? Statuses = null,  // null = default to ["pending", "accepted"]
    bool IncludeExpired = false,
    bool IncludeRevoked = false
) : IQuery<CursorPagedList<SystemInviteListDto>>;
```

## 12. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `task_standards_backend.md` aktualisiert
- [ ] `coding_standards_backend.md` aktualisiert (INVITE_NOT_EDITABLE error code)
- [ ] Code reviewed
- [ ] Background Job in Production getestet

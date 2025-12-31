# Task 011: Backend Fixes - Session Revoke, GDPR, Audit Search & Permissions

## 1. Übersicht

**Was wird gebaut?**
Kritische Bug Fixes und Verbesserungen: Single Session Revoke API, Invite-Löschung bei User-Anonymisierung (GDPR), Audit-Suche in Details-Feld, und Permission-Safeguard für users:read.

**Warum?**
- Admins können nur ALLE Sessions revoken, nicht einzelne
- GDPR-Lücke: Invites werden bei User-Anonymisierung nicht gelöscht
- Audit-Suche findet keine Daten im Details-JSON (z.B. OriginalEmail)
- Permission-Safeguard prüft nur users:update, nicht users:read

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich eine einzelne Session eines Users revoken können
- Als Admin möchte ich bei User-Anonymisierung sicher sein, dass alle PII gelöscht werden (inkl. Invites)
- Als Admin möchte ich in Audit-Logs nach Details suchen können (z.B. Original-Email nach Anonymisierung)
- Als System möchte ich sicherstellen, dass immer mindestens ein User users:read UND users:update hat

### Akzeptanzkriterien
- [X] Neuer Endpoint: DELETE /api/system/users/{userId}/sessions/{sessionId}
- [X] Bei User-Anonymisierung werden alle Invites mit der Original-Email gelöscht
- [X] Audit-Suche durchsucht auch das Details JSON-Feld
- [X] Permission-Check prüft beide Permissions: users:update UND users:read
- [X] Unit Tests für alle neuen Features

### Edge Cases / Error Handling
- Was passiert wenn Session nicht existiert? → 404 Not Found
- Was passiert wenn Session zu anderem User gehört? → 404 Not Found
- Was passiert wenn keine Invites zum Löschen da sind? → Kein Fehler, einfach weiter
- Was passiert wenn letzter User mit users:read seine Permission verliert? → 400 Bad Request

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| DELETE | /api/system/users/{userId}/sessions/{sessionId} | - | `{ revoked: true }` | Einzelne Session revoken |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `SESSION_NOT_FOUND` | 404 | Session existiert nicht oder gehört zu anderem User |

> Nach Completion: Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

Keine Datenbank-Änderungen erforderlich.

## 6. Files zu erstellen

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| RevokeUserSessionCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSession/` | Command für einzelne Session |
| RevokeUserSessionHandler.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSession/` | Handler |

## 7. Files zu ändern

### Phase 1: Single Session Revoke
| Datei | Was ändern? |
|-------|-------------|
| `SystemUsersController.cs` | DELETE /{id}/sessions/{sessionId} Endpoint |
| `ErrorCodes.cs` | SESSION_NOT_FOUND hinzufügen |
| `SystemException.cs` | SessionNotFoundException hinzufügen |




### Phase 2: Invite Cleanup on Anonymize
| Datei | Was ändern? |
|-------|-------------|
| `AnonymizeUserHandler.cs` | Invites mit Original-Email löschen (VOR Anonymisierung!) |

### Phase 3: Audit Details Search
| Datei | Was ändern? |
|-------|-------------|
| `GetSystemAuditLogsHandler.cs` | Details::text ILIKE '%search%' hinzufügen |

### Phase 4: Permission Safeguard
| Datei | Was ändern? |
|-------|-------------|
| `UpdateSystemUserPermissionsHandler.cs` | Auch users:read prüfen |
| `AnonymizeUserHandler.cs` | Auch users:read prüfen |
| `DeactivateSystemUserHandler.cs` | Auch users:read prüfen (falls vorhanden) |

## 8. Neue Packages

Keine neuen Packages erforderlich.

## 9. Implementation Reihenfolge

### Phase 1: Single Session Revoke
1. [X] `ErrorCodes.cs` + `SystemException.cs` - SESSION_NOT_FOUND
2. [X] `RevokeUserSessionCommand.cs` - Command erstellen
3. [X] `RevokeUserSessionHandler.cs` - Handler implementieren
4. [X] `SystemUsersController.cs` - DELETE Endpoint hinzufügen
5. [X] Unit Tests

Files Created:

  | File                                                                                                | Description                              |
  |-----------------------------------------------------------------------------------------------------|------------------------------------------|
  | src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSession/RevokeUserSessionCommand.cs | Command record with UserId and SessionId |
  | src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSession/RevokeUserSessionHandler.cs | Handler that revokes a single session    |
  | tests/ExoAuth.UnitTests/Features/SystemUsers/RevokeUserSessionHandlerTests.cs                       | 6 unit tests                             |

  Files Modified:

  | File                                     | Change                                               |
  |------------------------------------------|------------------------------------------------------|
  | Common/Exceptions/SystemException.cs     | Added UserSessionNotFoundException (404)             |
  | Common/Interfaces/IAuditService.cs       | Added SessionRevokedByAdmin audit action (singular)  |
  | Features/Auth/Models/MfaModels.cs        | Added RevokeUserSessionResponse                      |
  | Api/Controllers/SystemUsersController.cs | Added DELETE /{userId}/sessions/{sessionId} endpoint |

  API Endpoint:

  DELETE /api/system/users/{userId}/sessions/{sessionId}
  - Permission: UsersSessionsRevoke
  - Response: { "revoked": true }
  - Error cases:
    - User not found → 404 (SYSTEM_USER_NOT_FOUND)
    - Session not found or belongs to other user → 404 (SESSION_NOT_FOUND)

### Phase 2: Invite Cleanup on Anonymize (GDPR)
1. [X] `AnonymizeUserHandler.cs` - Invites löschen (alle Status: pending, accepted, revoked, expired)
2. [X] Unit Tests

Files Modified:

| File | Change |
|------|--------|
| src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserHandler.cs | Added invite deletion before anonymization |
| tests/ExoAuth.UnitTests/Features/SystemUsers/AnonymizeUserHandlerTests.cs | Added 3 tests for invite deletion |

Tests Added:
- `Handle_DeletesInvitesWithUserEmail` - Verifies only matching email invites are deleted
- `Handle_DeletesAllInvitesWithUserEmail_RegardlessOfStatus` - Verifies all invites with email are deleted
- `Handle_WhenNoInvitesExist_SucceedsWithoutError` - Verifies handler works when no invites exist

### Phase 3: Audit Details Search
1. [X] `GetSystemAuditLogsHandler.cs` - Details-Feld in Suche einbeziehen
2. [X] Unit Tests

Files Modified:

| File | Change |
|------|--------|
| src/ExoAuth.Domain/Entities/SystemAuditLog.cs | Changed Details from JsonDocument? to string? for LINQ compatibility |
| src/ExoAuth.Application/Features/SystemAuditLogs/Models/SystemAuditLogDto.cs | Changed Details from JsonDocument? to string? |
| src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetSystemAuditLogs/GetSystemAuditLogsHandler.cs | Added Details field search (case-insensitive) |

Files Created:

| File | Description |
|------|-------------|
| tests/ExoAuth.UnitTests/Features/SystemAuditLogs/GetSystemAuditLogsHandlerTests.cs | 5 unit tests for Details search |

Tests Added:
- `Handle_SearchInDetails_FindsMatchingLogs` - Verifies search finds logs by Details content
- `Handle_SearchInDetails_IsCaseInsensitive` - Verifies case-insensitive Details search
- `Handle_SearchInUserEmail_StillWorks` - Verifies existing user search still works
- `Handle_SearchMatchesBothUserAndDetails` - Verifies search matches both user and Details
- `Handle_WithNoSearch_ReturnsAllLogs` - Verifies no filter returns all logs

### Phase 4: Permission Safeguard
1. [X] `UpdateSystemUserPermissionsHandler.cs` - users:read Check hinzufügen
2. [X] `AnonymizeUserHandler.cs` - users:read Check hinzufügen
3. [X] `DeactivateSystemUserHandler.cs` - users:read Check hinzufügen
4. [X] Unit Tests

Files Modified:

| File | Change |
|------|--------|
| src/ExoAuth.Application/Features/SystemUsers/Commands/UpdatePermissions/UpdateSystemUserPermissionsHandler.cs | Added users:read last-holder check |
| src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserHandler.cs | Added users:read last-holder check |
| src/ExoAuth.Application/Features/SystemUsers/Commands/DeactivateSystemUser/DeactivateSystemUserHandler.cs | Added users:read last-holder check |
| tests/ExoAuth.UnitTests/Features/SystemUsers/UpdatePermissionsHandlerTests.cs | Added 2 tests for users:read safeguard |
| tests/ExoAuth.UnitTests/Features/SystemUsers/AnonymizeUserHandlerTests.cs | Added 2 tests for users:read safeguard |
| tests/ExoAuth.UnitTests/Features/SystemUsers/DeactivateSystemUserHandlerTests.cs | Added 2 tests for users:read safeguard |

Tests Added:
- `Handle_WhenRemovingLastUsersReadPermission_ThrowsLastPermissionHolderException`
- `Handle_WhenRemovingUsersReadButOthersHaveIt_AllowsRemoval`
- `Handle_WhenLastUsersReadHolder_ThrowsLastPermissionHolderException`
- `Handle_WhenNotLastUsersReadHolder_Succeeds`
- `Handle_WhenOthersHaveUsersRead_AllowsDeactivation`

### Phase 5: Finalisierung
1. [X] Alle Unit Tests grün (303 tests passing)
2. [ ] `task_standards_backend.md` aktualisieren
3. [ ] `coding_standards_backend.md` aktualisieren (Error Codes)

## 10. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `RevokeUserSessionHandlerTests.cs` | Single session revoke | ~5 |
| `AnonymizeUserHandlerTests.cs` | Invite deletion + users:read check | ~3 |
| `GetSystemAuditLogsHandlerTests.cs` | Details search | ~2 |
| `UpdatePermissionsHandlerTests.cs` | users:read check | ~2 |

## 11. Technische Details

### Single Session Revoke

```csharp
// RevokeUserSessionCommand.cs
public sealed record RevokeUserSessionCommand(
    Guid UserId,
    Guid SessionId
) : ICommand<RevokeUserSessionResponse>;

// Handler prüft:
// 1. User existiert
// 2. Session existiert UND gehört zu diesem User
// 3. Session revoken (Redis + DB)
// 4. Refresh Token für diese Session revoken
```

### Invite Cleanup bei Anonymisierung

```csharp
// AnonymizeUserHandler.cs - VOR user.Anonymize()
var originalEmail = user.Email;

// Alle Invites mit dieser Email löschen (unabhängig vom Status)
var invitesToDelete = await _context.SystemInvites
    .Where(i => i.Email == originalEmail)
    .ToListAsync(ct);

if (invitesToDelete.Count > 0)
{
    _context.SystemInvites.RemoveRange(invitesToDelete);
}

// Dann erst anonymisieren
user.Anonymize();
```

**Warum alle Status?**
| Status | Enthält PII? | Löschen? |
|--------|--------------|----------|
| pending | Email, FirstName, LastName | Ja |
| expired | Email, FirstName, LastName | Ja |
| revoked | Email, FirstName, LastName | Ja |
| accepted | Email, FirstName, LastName | Ja |

### Audit Details Search

```csharp
// GetSystemAuditLogsHandler.cs
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var searchLower = query.Search.ToLowerInvariant();
    queryable = queryable.Where(l =>
        // Existing: User search
        (l.User != null && (
            l.User.Email.ToLower().Contains(searchLower) ||
            l.User.FirstName.ToLower().Contains(searchLower) ||
            l.User.LastName.ToLower().Contains(searchLower)
        )) ||
        // Existing: Target user search
        (l.TargetUser != null && (
            l.TargetUser.Email.ToLower().Contains(searchLower) ||
            l.TargetUser.FirstName.ToLower().Contains(searchLower) ||
            l.TargetUser.LastName.ToLower().Contains(searchLower)
        )) ||
        // NEW: Details JSON search
        (l.Details != null && EF.Functions.ILike(l.Details, $"%{query.Search}%"))
    );
}
```

**PostgreSQL Notes:**
- `EF.Functions.ILike()` translates to PostgreSQL `ILIKE` (case-insensitive)
- `Details` is stored as `text` (JSON string), so ILIKE works directly

### Permission Safeguard - Both Permissions

```csharp
// UpdateSystemUserPermissionsHandler.cs
// Check for users:update (existing)
if (removedPermissions.Contains(SystemPermissions.UsersUpdate))
{
    var holdersCount = await _userRepository.CountUsersWithPermissionAsync(
        SystemPermissions.UsersUpdate, ct);
    if (holdersCount <= 1)
        throw new LastPermissionHolderException(SystemPermissions.UsersUpdate);
}

// NEW: Check for users:read
if (removedPermissions.Contains(SystemPermissions.UsersRead))
{
    var holdersCount = await _userRepository.CountUsersWithPermissionAsync(
        SystemPermissions.UsersRead, ct);
    if (holdersCount <= 1)
        throw new LastPermissionHolderException(SystemPermissions.UsersRead);
}
```

### Controller Endpoint

```csharp
// SystemUsersController.cs
/// <summary>
/// Revoke a specific session for a system user.
/// </summary>
[HttpDelete("{userId:guid}/sessions/{sessionId:guid}")]
[SystemPermission(SystemPermissions.UsersSessionsRevoke)]
[ProducesResponseType(typeof(RevokeUserSessionResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> RevokeSession(Guid userId, Guid sessionId, CancellationToken ct)
{
    var command = new RevokeUserSessionCommand(userId, sessionId);
    var result = await Mediator.Send(command, ct);
    return ApiOk(result);
}
```

## 12. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `task_standards_backend.md` aktualisiert
- [ ] `coding_standards_backend.md` aktualisiert (SESSION_NOT_FOUND error code)
- [ ] Code reviewed

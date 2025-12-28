# Task: Backend Improvements - Audit, Users, Invitations & Security

## 1. Übersicht

**Was wird gebaut?**
Verbesserungen am Backend: Erweiterte Audit-Logs mit Target-User Info, verbesserte Filter-APIs, Permission-Filter für Users, komplettes Invitation-Management und Security-Features (Force Re-Auth, Error Audit Logging).

**Warum?**
- Audit Logs brauchen mehr Kontext (wer wurde von einer Aktion betroffen?)
- Invitation Management fehlt komplett (Liste, Revoke, Resend)
- Security: Bei Permission-Änderungen sofortige Invalidierung
- Bessere Filterung für Users und Audit Logs

## 2. User Experience / Anforderungen

### User Stories

- Als **SystemUser** möchte ich im Audit Log sehen können, wer von einer Aktion betroffen war (nicht nur wer sie ausgeführt hat)
- Als **SystemUser** möchte ich Audit Logs nach involvierten Usern filtern können (egal ob Actor oder Target)
- Als **SystemUser** möchte ich Users nach Permissions filtern können, um schnell zu sehen wer welche Rechte hat
- Als **SystemUser** möchte ich alle Einladungen sehen können mit Status (pending, accepted, expired, revoked)
- Als **SystemUser** möchte ich Einladungen widerrufen können
- Als **SystemUser** möchte ich Einladungen erneut senden können
- Als **eingeladener User** möchte ich bei der Einladungs-Validierung mehr Details sehen (wer hat mich eingeladen, etc.)
- Als **SystemUser** erwarte ich, dass bei Permission-Änderungen der betroffene User sofort ausgeloggt wird

### Akzeptanzkriterien

- [x] Audit Log Details enthalten Target-User Info (id, email, fullName) wo relevant
- [x] Audit Log Filter: `?involvedUserIds=` findet Logs wo User Actor ODER Target ist
- [x] Audit Log Search durchsucht auch Target-User Email/Name
- [x] Audit Log Filter: `?actions=` unterstützt mehrere Actions (komma-separiert)
- [x] Audit Log Filters API gibt keine Users mehr zurück
- [x] System Users API: `?permissionIds=` Filter funktioniert
- [x] SystemInvite Entity hat `RevokedAt` und `ResentAt` Felder
- [x] Invitation List mit Pagination, Search, Status-Filter
- [x] Invitation Revoke funktioniert (setzt RevokedAt)
- [x] Invitation Resend mit 5 Minuten Cooldown
- [x] Invitation Validate Endpoint gibt Details zurück (inviter, expiration, permissions, etc.)
- [x] Force Re-Auth: Bei Permission-Änderung wird User sofort ausgeloggt via Redis Flag
- [x] Error Audit: 403 und 500 Errors werden in Audit Log geschrieben

### Edge Cases / Error Handling

- Was passiert wenn Invitation bereits revoked ist und nochmal revoked wird?
  → Error: "Invitation already revoked"
- Was passiert wenn Invitation bereits accepted ist und revoked wird?
  → Error: "Cannot revoke accepted invitation"
- Was passiert bei Resend vor Ablauf des 5 Minuten Cooldowns?
  → Error: "Please wait X minutes before resending"
- Was passiert wenn Target User gelöscht wurde?
  → Audit Log Details enthalten immer noch die Info (Snapshot)
- Was passiert wenn involvedUserIds einen nicht-existierenden User enthält?
  → Kein Error, einfach keine Ergebnisse für diesen User

## 3. API Endpoints

### Audit Logs (Änderungen)

| Method | Route | Query Params | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/audit-logs` | `?involvedUserIds=guid1,guid2&actions=action1,action2&search=...&from=...&to=...&cursor=...&limit=...` | `{ data[], pagination }` | Erweiterte Filter |
| GET | `/api/system/audit-logs/filters` | - | `{ actions[], dateRange }` | Ohne Users! |

### System Users (Änderungen)

| Method | Route | Query Params | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/users` | `?permissionIds=guid1,guid2&search=...&cursor=...&limit=...` | `{ data[], pagination }` | Mit Permission Filter |

### System Invites (Neu)

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/system/invites` | - | `{ data[], pagination }` | Liste mit Pagination, Search, Status-Filter |
| GET | `/api/system/invites/{id}` | - | `{ data }` | Invite Details |
| POST | `/api/system/invites/{id}/revoke` | - | `{ data }` | Invite widerrufen |
| POST | `/api/system/invites/{id}/resend` | - | `{ data }` | Invite erneut senden (5min Cooldown) |

### Auth (Änderungen)

| Method | Route | Query Params | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | `/api/auth/invite` | `?token=xxx` | `{ data }` | Validate & Details (public) |

### Query Parameter für Invites

```
# Pagination
?cursor=abc123&limit=20

# Search (by email)
?search=john@example.com

# Status Filter
?status=pending,expired,revoked,accepted
```

### Response Formate

**Invite List Item:**
```json
{
  "id": "guid",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "status": "pending",
  "expiresAt": "2025-01-01T00:00:00Z",
  "createdAt": "2024-12-31T00:00:00Z",
  "acceptedAt": null,
  "revokedAt": null,
  "resentAt": null,
  "invitedBy": {
    "id": "guid",
    "email": "admin@example.com",
    "fullName": "Admin User"
  }
}
```

**Invite Validate Response (`GET /api/auth/invite?token=xxx`):**
```json
{
  "valid": true,
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "expiresAt": "2025-01-01T00:00:00Z",
  "invitedBy": {
    "fullName": "Admin User"
  },
  "permissions": [
    { "name": "system:users:read", "description": "View system users" }
  ]
}
```

**Audit Log Detail (erweitert):**
```json
{
  "id": "guid",
  "userId": "guid",
  "userEmail": "admin@example.com",
  "userFullName": "Admin User",
  "action": "system.user.permissions.updated",
  "entityType": "SystemUser",
  "entityId": "guid",
  "details": {
    "added": ["system:audit:read"],
    "removed": ["system:settings:read"],
    "targetUser": {
      "id": "guid",
      "email": "john@example.com",
      "fullName": "John Doe"
    }
  },
  "ipAddress": "192.168.1.1",
  "createdAt": "2024-12-27T10:00:00Z"
}
```

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `INVITE_NOT_FOUND` | 404 | Einladung nicht gefunden |
| `INVITE_ALREADY_REVOKED` | 400 | Einladung bereits widerrufen |
| `INVITE_ALREADY_ACCEPTED` | 400 | Einladung bereits angenommen, kann nicht widerrufen werden |
| `INVITE_RESEND_COOLDOWN` | 429 | Bitte X Minuten warten vor erneutem Senden |
| `INVITE_EXPIRED` | 400 | Einladung abgelaufen |

> Nach Completion: Diese Codes zu `coding_standards_backend.md` hinzufügen!

## 5. Datenbank Änderungen

### Entity Änderungen

**SystemInvite (erweitert):**
| Property | Type | Beschreibung |
|----------|------|--------------|
| RevokedAt | DateTime? | Wann wurde die Einladung widerrufen |
| ResentAt | DateTime? | Wann wurde die Einladung zuletzt erneut gesendet |

**SystemAuditLog (erweitert):**
| Property | Type | Beschreibung |
|----------|------|--------------|
| TargetUserId | Guid? | User der von der Aktion betroffen war (FK mit SET NULL für GDPR) |

### Migrations

- [x] Migration Name: `AddInviteRevokedAndResentAt`
- [x] Migration Name: `AddAuditLogTargetUserId`

## 6. Files zu erstellen

### Application Layer

**System Invites Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| GetSystemInvitesQuery.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/GetSystemInvites/GetSystemInvitesQuery.cs` | List query |
| GetSystemInvitesHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/GetSystemInvites/GetSystemInvitesHandler.cs` | Handler |
| GetSystemInviteQuery.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/GetSystemInvite/GetSystemInviteQuery.cs` | Single query |
| GetSystemInviteHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/GetSystemInvite/GetSystemInviteHandler.cs` | Handler |
| ValidateInviteQuery.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/ValidateInvite/ValidateInviteQuery.cs` | Validate query |
| ValidateInviteHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Queries/ValidateInvite/ValidateInviteHandler.cs` | Handler |
| RevokeInviteCommand.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/RevokeInvite/RevokeInviteCommand.cs` | Revoke command |
| RevokeInviteHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/RevokeInvite/RevokeInviteHandler.cs` | Handler |
| ResendInviteCommand.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/ResendInvite/ResendInviteCommand.cs` | Resend command |
| ResendInviteHandler.cs | `src/ExoAuth.Application/Features/SystemInvites/Commands/ResendInvite/ResendInviteHandler.cs` | Handler |
| SystemInviteDto.cs | `src/ExoAuth.Application/Features/SystemInvites/Models/SystemInviteDto.cs` | DTOs |

**Force Re-Auth Feature:**
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| IForceReauthService.cs | `src/ExoAuth.Application/Common/Interfaces/IForceReauthService.cs` | Interface |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ForceReauthService.cs | `src/ExoAuth.Infrastructure/Services/ForceReauthService.cs` | Redis implementation |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| SystemInvitesController.cs | `src/ExoAuth.Api/Controllers/SystemInvitesController.cs` | Invites endpoints |
| ForceReauthMiddleware.cs | `src/ExoAuth.Api/Middleware/ForceReauthMiddleware.cs` | Middleware für Re-Auth Check |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Domain/Entities/SystemInvite.cs` | `RevokedAt`, `ResentAt` Properties + Methods hinzufügen |
| `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemInviteConfiguration.cs` | Neue Columns konfigurieren |
| `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetSystemAuditLogs/GetSystemAuditLogsQuery.cs` | `InvolvedUserIds`, `Actions` Parameter hinzufügen |
| `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetSystemAuditLogs/GetSystemAuditLogsHandler.cs` | Filter-Logik für involvedUserIds, actions, search in target |
| `src/ExoAuth.Application/Features/SystemAuditLogs/Queries/GetAuditLogFilters/GetAuditLogFiltersHandler.cs` | Users entfernen |
| `src/ExoAuth.Application/Features/SystemAuditLogs/Models/SystemAuditLogDto.cs` | Falls nötig anpassen |
| `src/ExoAuth.Application/Features/SystemAuditLogs/Models/AuditLogFiltersDto.cs` | Users Property entfernen |
| `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUsers/GetSystemUsersQuery.cs` | `PermissionIds` Parameter hinzufügen |
| `src/ExoAuth.Application/Features/SystemUsers/Queries/GetSystemUsers/GetSystemUsersHandler.cs` | Permission Filter Logik |
| `src/ExoAuth.Application/Features/SystemUsers/Commands/UpdatePermissions/UpdateSystemUserPermissionsHandler.cs` | Target User Info in Audit Details + Force Re-Auth setzen |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginHandler.cs` | Force Re-Auth Flag löschen bei Login |
| `src/ExoAuth.Api/Controllers/AuthController.cs` | `GET /api/auth/invite` Endpoint hinzufügen |
| `src/ExoAuth.Api/Controllers/SystemAuditLogsController.cs` | Neue Query Parameter |
| `src/ExoAuth.Api/Controllers/SystemUsersController.cs` | PermissionIds Query Parameter |
| `src/ExoAuth.Api/Middleware/ExceptionMiddleware.cs` | Audit Logging für 403/500 Errors |
| `src/ExoAuth.Api/Program.cs` | ForceReauthMiddleware hinzufügen |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | ForceReauthService registrieren |
| `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | Neue Error Codes |

## 8. Neue Packages

Keine neuen Packages erforderlich.

## 9. Implementation Reihenfolge

### Phase 1: Database & Entity Changes
1. [x] **Domain**: SystemInvite Entity erweitern (RevokedAt, ResentAt, Revoke(), MarkResent(), Status methods)
2. [x] **Infrastructure**: SystemInviteConfiguration updaten (RevokedAt, ResentAt, Index)
3. [x] **Infrastructure**: Migration erstellen: `AddInviteRevokedAndResentAt`

### Phase 2: Audit Log Improvements
4. [x] **Application**: GetSystemAuditLogsQuery erweitern (Search, Actions, InvolvedUserIds Parameter)
5. [x] **Application**: GetSystemAuditLogsHandler erweitern (Filter-Logik für Actions, InvolvedUserIds, Search)
6. [x] **Application**: AuditLogFiltersDto - Users Property entfernt
7. [x] **Application**: GetAuditLogFiltersHandler - Users Query entfernt
8. [x] **Application**: UpdateSystemUserPermissionsHandler - Target User Info (id, email, fullName) in Details
9. [x] **API**: SystemAuditLogsController - Query Parameter (search, actions, involvedUserIds)

### Phase 3: System Users Permission Filter
10. [x] **Application**: GetSystemUsersQuery erweitern (PermissionIds Parameter)
11. [x] **Application**: GetSystemUsersHandler erweitern (Permission Filter Logik)
12. [x] **API**: SystemUsersController - Query Parameter hinzufügen

### Phase 4: Invitation Management
13. [x] **Application**: SystemInviteDto + weitere DTOs erstellen
14. [x] **Application**: GetSystemInvitesQuery + Handler (List mit Pagination, Search, Status)
15. [x] **Application**: GetSystemInviteQuery + Handler (Single)
16. [x] **Application**: ValidateInviteQuery + Handler (Public validation)
17. [x] **Application**: RevokeInviteCommand + Handler
18. [x] **Application**: ResendInviteCommand + Handler (mit 5min Cooldown)
19. [x] **API**: SystemInvitesController erstellen
20. [x] **API**: AuthController - GET /api/auth/invite Endpoint hinzufügen

### Phase 5: Force Re-Auth
21. [x] **Application**: IForceReauthService Interface erstellen
22. [x] **Infrastructure**: ForceReauthService implementieren (Redis)
23. [x] **API**: ForceReauthMiddleware erstellen
24. [x] **Application**: UpdateSystemUserPermissionsHandler - Force Re-Auth Flag setzen + Tokens revoken
25. [x] **Application**: LoginHandler - Force Re-Auth Flag löschen
26. [x] **Infrastructure**: DependencyInjection - ForceReauthService registrieren
27. [x] **API**: Program.cs - Middleware hinzufügen

### Phase 6: Error Audit Logging
28. [x] **Application**: Neue Audit Actions definieren (access.forbidden, error.internal)
29. [x] **API**: ExceptionMiddleware - Audit Logging für 403/500

### Phase 7: Tests & Cleanup
30. [x] **Tests**: Unit Tests für neue Features (129 Tests ✅)
31. [x] **Application**: ErrorCodes.cs - Neue Codes hinzugefügt
32. [x] **Standards**: task_standards_backend.md aktualisieren
33. [x] **Standards**: coding_standards_backend.md aktualisieren (neue Error Codes)

## 10. Tests

### Unit Tests

| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Features/SystemInvites/GetSystemInvitesHandlerTests.cs` | List, Search, Status Filter | ~6 |
| `tests/ExoAuth.UnitTests/Features/SystemInvites/RevokeInviteHandlerTests.cs` | Revoke, already revoked, already accepted | ~4 |
| `tests/ExoAuth.UnitTests/Features/SystemInvites/ResendInviteHandlerTests.cs` | Resend, cooldown check | ~4 |
| `tests/ExoAuth.UnitTests/Features/SystemInvites/ValidateInviteHandlerTests.cs` | Validate, expired, revoked | ~4 |
| `tests/ExoAuth.UnitTests/Features/SystemAuditLogs/GetSystemAuditLogsHandlerTests.cs` | InvolvedUserIds, Actions, Search in target | ~6 |
| `tests/ExoAuth.UnitTests/Features/SystemUsers/GetSystemUsersHandlerTests.cs` | PermissionIds filter | ~3 |
| `tests/ExoAuth.UnitTests/Services/ForceReauthServiceTests.cs` | Set, check, clear flag | ~5 |

## 11. Redis Keys (Force Re-Auth)

| Key Pattern | Beschreibung | TTL |
|-------------|--------------|-----|
| `user:force-reauth:{userId}` | Erzwingt Re-Login nach Permission-Änderung | 15 min |

### Force Re-Auth Flow

```
1. Admin ändert User Permissions
   → Permissions in DB aktualisiert
   → Permission Cache invalidiert
   → Redis: SET user:force-reauth:{userId} "1" (TTL: 15 min)
   → Alle Refresh Tokens revoked + blacklisted

2. User macht nächsten Request
   → ForceReauthMiddleware checkt: EXISTS user:force-reauth:{userId}?
   → Key existiert → 401 Unauthorized + Audit Log

3. User muss neu einloggen
   → Bei erfolgreichem Login: DEL user:force-reauth:{userId}
   → Neue Permissions aus DB
```

## 12. Error Audit Logging

### Neue Audit Actions

| Action | Wann | Details |
|--------|------|---------|
| `system.access.forbidden` | 403 Response | userId, endpoint, requiredPermission, ipAddress |
| `system.error.internal` | 500 Response | endpoint, errorType, requestId, ipAddress |
| `system.access.forced_reauth` | Force-Reauth 401 | userId, endpoint, ipAddress |
| `system.invite.revoked` | Invite widerrufen | inviteId, email, revokedBy |
| `system.invite.resent` | Invite erneut gesendet | inviteId, email, resentBy |

## 13. Invite Status Logic

```csharp
public string GetStatus()
{
    if (RevokedAt.HasValue) return "revoked";
    if (AcceptedAt.HasValue) return "accepted";
    if (DateTime.UtcNow > ExpiresAt) return "expired";
    return "pending";
}
```

## 14. Nach Completion

- [x] Alle Unit Tests grün (129 Tests ✅)
- [x] `task_standards_backend.md` aktualisiert (neue Files)
- [x] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [x] Migration läuft durch
- [ ] Code reviewed

---

**Letzte Änderung:** 2025-12-28
**Status:** ✅ Completed (Phase 7 done)

## 15. Additional Changes (2025-12-28)

### Bug Fix: EF Core FullName Translation Error
**Problem**: `GetSystemAuditLogsHandler` threw 500 error because `FullName` is a computed C# property that EF Core cannot translate to SQL.

**Fix** in `GetSystemAuditLogsHandler.cs`:
- Select projection: Changed `l.User.FullName` → `l.User.FirstName + " " + l.User.LastName`
- Search filter: Changed `FullName.ToLower().Contains()` → separate `FirstName` and `LastName` checks

### New Permission: system:permissions:read
- Added `PermissionsRead = "system:permissions:read"` to `SystemPermissions.cs`
- Added to `SystemPermissions.All` for auto-seeding
- Description: "View available permissions" (Category: Permissions)
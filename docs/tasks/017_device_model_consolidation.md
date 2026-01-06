# Task 017: Device Model Consolidation

## 1. Übersicht

**Was wird gebaut?**
Zusammenführung von `DeviceSession`, `TrustedDevice` und `DeviceApprovalRequest` in eine einzige `Device` Entity.

**Warum?**
- Aktuelle Architektur ist überkompliziert (3 Tables für 1 Konzept)
- Sessions werden erstellt bevor Login abgeschlossen ist (orphan records)
- User muss nach Device-Approval erneut einloggen (schlechte UX)
- Verwirrende Trennung zwischen "Sessions" und "Trusted Devices" im Frontend
- 1 Device = 1 Record, nicht 3

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich eine einzige Device-Liste sehen, damit ich alle meine Geräte verwalten kann
- Als User möchte ich nach Device-Approval direkt eingeloggt werden, ohne nochmal Login
- Als User möchte ich von einem bestehenden Gerät aus neue Geräte genehmigen können
- Als User möchte ich Geräte widerrufen können (Logout + Vertrauen entziehen)

### Akzeptanzkriterien
- [x] Eine `Device` Entity ersetzt `DeviceSession`, `TrustedDevice`, `DeviceApprovalRequest`
- [x] Device hat Status: `PendingApproval`, `Trusted`, `Revoked`
- [x] Nach Device-Approval kann User Login erneut versuchen (Device ist jetzt trusted)
- [x] Device wird erst erstellt wenn Approval-Flow startet, nicht vorher
- [x] User kann von bestehendem Device aus pending Devices genehmigen
- [x] Eine API-Liste `/auth/devices` statt zwei getrennte
- [x] Alle bestehenden Unit Tests angepasst und grün (315/315 pass inkl. 37 DeviceService Tests)

### Edge Cases / Error Handling
- Was passiert mit bestehenden Sessions/TrustedDevices? → Migration konvertiert sie
- Was passiert wenn Device revoked wird? → RefreshTokens werden auch revoked
- Was passiert bei Approval-Timeout? → Device bleibt pending, kann erneut versucht werden

## 3. API Endpoints

### Neue/Geänderte Endpoints

| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| GET | `/api/auth/devices` | Yes | - | `List<DeviceDto>` | Alle Devices des Users (ersetzt /sessions + /trusted-devices) |
| DELETE | `/api/auth/devices/{id}` | Yes | - | `ApiResponse` | Device widerrufen (ersetzt /sessions/{id}) |
| PUT | `/api/auth/devices/{id}/name` | Yes | `{ name }` | `DeviceDto` | Device umbenennen |
| POST | `/api/auth/devices/{id}/approve` | Yes | - | `AuthResponse` | Pending Device genehmigen (aus bestehendem Device) |
| POST | `/api/auth/approve-device` | No | `{ approvalToken, code }` | `AuthResponse` | Device genehmigen + Login abschließen |
| GET | `/api/auth/approve-device-link` | No | `?token=...` | `AuthResponse` | Device via Link genehmigen + Login abschließen |

### Entfernte Endpoints
- `GET /api/auth/sessions` → ersetzt durch `/devices`
- `GET /api/auth/trusted-devices` → ersetzt durch `/devices`
- `DELETE /api/auth/trusted-devices/{id}` → ersetzt durch `DELETE /devices/{id}`
- `PUT /api/auth/trusted-devices/{id}/name` → ersetzt durch `PUT /devices/{id}/name`

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `DEVICE_NOT_FOUND` | 404 | Device nicht gefunden |
| `DEVICE_NOT_PENDING` | 400 | Device ist nicht im PendingApproval Status |
| `CANNOT_REVOKE_CURRENT_DEVICE` | 400 | Kann aktuelles Device nicht widerrufen |
| `APPROVAL_TOKEN_INVALID` | 400 | Approval Token ungültig oder abgelaufen |
| `APPROVAL_CODE_INVALID` | 400 | Approval Code falsch |
| `APPROVAL_MAX_ATTEMPTS` | 429 | Zu viele falsche Code-Versuche |

> ⚠️ **Nach Completion:** Diese Codes zu `backend_reference.md` Memory hinzufügen!

## 5. Datenbank Änderungen

### Neue Entity: Device

```csharp
public sealed class Device : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; }  // Generated unique ID
    public string? Fingerprint { get; private set; }
    public string? Name { get; private set; }  // Custom name by user
    
    // Device Info
    public string? UserAgent { get; private set; }
    public string? Browser { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OsVersion { get; private set; }
    public string? DeviceType { get; private set; }  // Desktop, Mobile, Tablet
    
    // Location
    public string? IpAddress { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string? City { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    
    // Status
    public DeviceStatus Status { get; private set; }  // PendingApproval, Trusted, Revoked
    public DateTime? TrustedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    
    // Approval (temporary, cleared after approval)
    public string? ApprovalTokenHash { get; private set; }
    public string? ApprovalCodeHash { get; private set; }
    public DateTime? ApprovalExpiresAt { get; private set; }
    public int ApprovalAttempts { get; private set; }
    public int? RiskScore { get; private set; }
    public string? RiskFactors { get; private set; }  // JSON array
    
    // Navigation
    public SystemUser? User { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

### Enum: DeviceStatus
```csharp
public enum DeviceStatus
{
    PendingApproval = 0,
    Trusted = 1,
    Revoked = 2
}
```

### Entities zu löschen
- `DeviceSession`
- `TrustedDevice`
- `DeviceApprovalRequest`
- `ApprovalStatus` enum

### RefreshToken Änderung
```csharp
// Alt:
public Guid? DeviceSessionId { get; private set; }
public DeviceSession? DeviceSession { get; set; }

// Neu:
public Guid? DeviceId { get; private set; }
public Device? Device { get; set; }
```

### Migration
- [ ] Create `Devices` table
- [ ] Migrate data from `DeviceSessions` + `TrustedDevices` (matching by DeviceId)
- [ ] Update `RefreshTokens.DeviceId` FK
- [ ] Drop `DeviceSessions`, `TrustedDevices`, `DeviceApprovalRequests` tables

## 6. Files zu erstellen

### Domain Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Device.cs | `src/ExoAuth.Domain/Entities/Device.cs` | Neue consolidated Entity |
| DeviceStatus.cs | `src/ExoAuth.Domain/Enums/DeviceStatus.cs` | Status enum |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| IDeviceService.cs | `src/ExoAuth.Application/Common/Interfaces/IDeviceService.cs` | Merged service interface |
| DeviceDto.cs | `src/ExoAuth.Application/Features/Auth/Models/DeviceDto.cs` | DTO (ersetzt DeviceSessionDto + TrustedDeviceDto) |
| GetDevicesQuery.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetDevices/GetDevicesQuery.cs` | Query |
| GetDevicesHandler.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetDevices/GetDevicesHandler.cs` | Handler |
| RevokeDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RevokeDevice/RevokeDeviceCommand.cs` | Command |
| RevokeDeviceHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/RevokeDevice/RevokeDeviceHandler.cs` | Handler |
| RenameDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RenameDevice/RenameDeviceCommand.cs` | Command |
| RenameDeviceHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/RenameDevice/RenameDeviceHandler.cs` | Handler |
| ApproveDeviceFromSessionCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDeviceFromSession/...` | Approve from existing device |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| DeviceConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceConfiguration.cs` | EF Config |
| DeviceService.cs | `src/ExoAuth.Infrastructure/Services/DeviceService.cs` | Merged service |
| Migration | `src/ExoAuth.Infrastructure/Persistence/Migrations/...` | DB Migration |

## 7. Files zu ändern

### Domain Layer
| Datei | Was ändern? |
|-------|-------------|
| `RefreshToken.cs` | `DeviceSessionId` → `DeviceId` |

### Application Layer
| Datei | Was ändern? |
|-------|-------------|
| `LoginHandler.cs` | Nutze neuen DeviceService, erstelle Device nur bei Approval-Flow |
| `MfaVerifyHandler.cs` | Nutze neuen DeviceService, erstelle Device nur bei Approval-Flow |
| `ApproveDeviceHandler.cs` | Vereinfachen: Update Device status + return tokens |
| `ApproveDeviceLinkHandler.cs` | Vereinfachen: Update Device status + return tokens |
| `DenyDeviceHandler.cs` | Update Device status statt ApprovalRequest |
| `RefreshTokenHandler.cs` | DeviceSessionId → DeviceId |
| `LogoutHandler.cs` | DeviceSessionId → DeviceId |
| `AcceptInviteHandler.cs` | Anpassen für neues Device model |
| `AuthResponse.cs` | Ggf. anpassen |

### Infrastructure Layer
| Datei | Was ändern? |
|-------|-------------|
| `AppDbContext.cs` | `DbSet<Device>` hinzufügen, alte DbSets entfernen |
| `DependencyInjection.cs` | DeviceService registrieren, alte Services entfernen |
| `RefreshTokenConfiguration.cs` | FK zu Device statt DeviceSession |
| `TokenService.cs` | SessionId → DeviceId in token claims |

### API Layer
| Datei | Was ändern? |
|-------|-------------|
| `AuthController.cs` | Endpoints aktualisieren |
| `CurrentUserService.cs` | SessionId → DeviceId |

## 8. Files zu löschen

### Domain Layer
| Datei | Pfad |
|-------|------|
| DeviceSession.cs | `src/ExoAuth.Domain/Entities/DeviceSession.cs` |
| TrustedDevice.cs | `src/ExoAuth.Domain/Entities/TrustedDevice.cs` |
| DeviceApprovalRequest.cs | `src/ExoAuth.Domain/Entities/DeviceApprovalRequest.cs` |
| ApprovalStatus.cs | `src/ExoAuth.Domain/Enums/ApprovalStatus.cs` |

### Application Layer
| Datei | Pfad |
|-------|------|
| IDeviceSessionService.cs | `src/ExoAuth.Application/Common/Interfaces/IDeviceSessionService.cs` |
| ITrustedDeviceService.cs | `src/ExoAuth.Application/Common/Interfaces/ITrustedDeviceService.cs` |
| IDeviceApprovalService.cs | `src/ExoAuth.Application/Common/Interfaces/IDeviceApprovalService.cs` |
| DeviceSessionDto.cs | `src/ExoAuth.Application/Features/Auth/Models/DeviceSessionDto.cs` |
| TrustedDeviceDto.cs | `src/ExoAuth.Application/Features/Auth/Models/TrustedDeviceDto.cs` |
| GetSessions/* | `src/ExoAuth.Application/Features/Auth/Queries/GetSessions/` |
| GetTrustedDevices/* | `src/ExoAuth.Application/Features/Auth/Queries/GetTrustedDevices/` |
| RevokeSession/* | `src/ExoAuth.Application/Features/Auth/Commands/RevokeSession/` |
| RevokeAllSessions/* | `src/ExoAuth.Application/Features/Auth/Commands/RevokeAllSessions/` |
| UpdateSession/* | `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/` |
| RemoveTrustedDevice/* | `src/ExoAuth.Application/Features/Auth/Commands/RemoveTrustedDevice/` |
| RenameTrustedDevice/* | `src/ExoAuth.Application/Features/Auth/Commands/RenameTrustedDevice/` |

### Infrastructure Layer
| Datei | Pfad |
|-------|------|
| DeviceSessionService.cs | `src/ExoAuth.Infrastructure/Services/DeviceSessionService.cs` |
| TrustedDeviceService.cs | `src/ExoAuth.Infrastructure/Services/TrustedDeviceService.cs` |
| DeviceApprovalService.cs | `src/ExoAuth.Infrastructure/Services/DeviceApprovalService.cs` |
| DeviceSessionConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceSessionConfiguration.cs` |
| TrustedDeviceConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/TrustedDeviceConfiguration.cs` |
| DeviceApprovalRequestConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceApprovalRequestConfiguration.cs` |

## 9. Neue Packages

Keine neuen Packages erforderlich.

## 10. Implementation Reihenfolge

### Phase 1: Neue Entity + Service ✅
1. [x] **Domain**: `Device.cs` Entity + `DeviceStatus.cs` enum erstellen
2. [x] **Infrastructure**: `DeviceConfiguration.cs` erstellen
3. [x] **Infrastructure**: `DeviceService.cs` erstellen (merged logic)
4. [x] **Application**: `IDeviceService.cs` interface erstellen
5. [x] **Infrastructure**: `AppDbContext.cs` - DbSet<Device> hinzufügen
6. [x] **Infrastructure**: DI registrieren

### Phase 2: Handlers aktualisieren ✅
7. [x] **Application**: `DeviceDto.cs` erstellen
8. [x] **Application**: `GetDevicesQuery` + Handler erstellen
9. [x] **Application**: `RevokeDeviceCommand` + Handler erstellen  
10. [x] **Application**: `RenameDeviceCommand` + Handler erstellen
11. [x] **Application**: `ApproveDeviceFromSessionCommand` + Handler erstellen
12. [x] **Application**: `LoginHandler.cs` aktualisieren (uses IDeviceService)
13. [x] **Application**: `MfaVerifyHandler.cs` aktualisieren (uses IDeviceService)
14. [x] **Application**: `ApproveDeviceHandler.cs` aktualisieren (marks device trusted, no tokens)
15. [x] **Application**: `ApproveDeviceLinkHandler.cs` aktualisieren (marks device trusted, no tokens)
16. [x] **Application**: `DenyDeviceHandler.cs` aktualisieren
17. [x] **Application**: `RefreshTokenHandler.cs` aktualisieren (DeviceId)
18. [x] **Application**: `LogoutHandler.cs` aktualisieren (DeviceId)
19. [x] **Application**: `AcceptInviteHandler.cs` aktualisieren (uses IDeviceService)
19a. [x] **Application**: `MfaConfirmHandler.cs` aktualisieren (uses IDeviceService)
19b. [x] **Application**: `RegisterHandler.cs` aktualisieren (removed unused dependency)
19c. [x] **Infrastructure**: `ForceReauthService.cs` aktualisieren (uses IDeviceService)
19d. [x] **Application**: Admin handlers (GetUserSessions, RevokeUserSession, RevokeUserSessions)
19e. [x] **Application**: User handlers (DeactivateSystemUser, UpdateSystemUser, AnonymizeUser)

### Phase 3: RefreshToken + API ✅
20. [x] **Domain**: `RefreshToken.cs` - DeviceSessionId → DeviceId
21. [x] **Infrastructure**: `RefreshTokenConfiguration.cs` aktualisieren
22. [x] **Infrastructure**: `TokenService.cs` - no changes needed (uses session_id claim)
23. [x] **API**: `AuthController.cs` Endpoints aktualisieren (GET/DELETE/PUT/POST /devices)
24. [x] **API**: `CurrentUserService.cs` - no changes needed (SessionId maps to DeviceId)

### Phase 4: Migration + Cleanup ✅
25. [x] **Infrastructure**: Migration erstellen (create table, migrate data, drop old)
26. [x] **Delete**: Alte Entities, Services, Handlers, Configs (siehe Liste oben)
27. [x] **Tests**: Alle Unit Tests anpassen (315/315 pass)
28. [x] **Tests**: 37 Tests für DeviceService geschrieben (`DeviceServiceTests.cs`)

### Phase 5: Frontend
29. [ ] **Types**: `device.ts` - DeviceDto type erstellen
30. [ ] **API**: `devices-api.ts` - Neue consolidated API (ersetzt sessions-api + trusted-devices-api)
31. [ ] **Hooks**: `use-devices.ts`, `use-revoke-device.ts`, `use-rename-device.ts`, `use-approve-device-from-session.ts`
32. [ ] **Components**: `device-card.tsx`, `devices-list.tsx`, `device-status-badge.tsx`
33. [ ] **Settings**: `security-settings.tsx` aktualisieren - Sessions + Trusted Devices → Devices section
34. [ ] **User Admin**: `user-sessions-section.tsx` aktualisieren (return DeviceDto)
35. [ ] **i18n**: Device status texts hinzufügen (EN + DE)
36. [ ] **Cleanup**: Alte API/Hooks/Components/Types löschen:
    - `sessions-api.ts`, `trusted-devices-api.ts`
    - `use-sessions.ts`, `use-revoke-session.ts`, `use-revoke-all-sessions.ts`, `use-update-session.ts`
    - `use-trusted-devices.ts`, `use-remove-trusted-device.ts`, `use-rename-trusted-device.ts`, `use-remove-all-other-devices.ts`
    - `session-card.tsx`, `sessions-list.tsx`, `session-details-sheet.tsx`
    - `trusted-device-card.tsx`, `trusted-devices-section.tsx`, `rename-device-modal.tsx`
    - `sessions.ts`, `trusted-device.ts` (types)
37. [ ] **Memory**: `frontend_reference.md` aktualisieren

## 11. Tests

### Unit Tests zu erstellen
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `DeviceServiceTests.cs` | Device CRUD, approval flow, trust logic |
| `GetDevicesHandlerTests.cs` | Query handler |
| `RevokeDeviceHandlerTests.cs` | Revoke handler |
| `RenameDeviceHandlerTests.cs` | Rename handler |
| `ApproveDeviceFromSessionHandlerTests.cs` | Approve from existing device |

### Unit Tests zu aktualisieren
| Test Datei | Was ändern? |
|------------|-------------|
| `LoginHandlerTests.cs` | Device statt DeviceSession |
| `MfaVerifyHandlerTests.cs` | Device statt DeviceSession |
| `ApproveDeviceHandlerTests.cs` | Neuer flow mit tokens |
| `RefreshTokenHandlerTests.cs` | DeviceId statt DeviceSessionId |

## 12. Key Flow Changes

### Login Flow (Trusted Device)
```
Password OK → MFA OK (if enabled) → Device trusted? 
  → YES: Create tokens, return AuthResponse with user
  → NO: See "New Device Flow" below
```

### New Device Flow
```
Password OK → MFA OK (if enabled) → Device not trusted
  → Create Device with Status=PendingApproval
  → Send approval email
  → Return AuthResponse with deviceApprovalRequired=true, approvalToken
  
User enters code OR clicks link OR approves from other device
  → Update Device Status=Trusted, TrustedAt=now
  → Create RefreshToken linked to Device
  → Return AuthResponse with tokens (LOGIN COMPLETE!)
```

### Approve from Existing Session
```
User logged in on Device A (trusted)
  → Sees pending Device B in device list
  → Clicks "Approve"
  → Device B Status=Trusted
  → Device B can now complete login (if still waiting) or login fresh
```

## 13. Nach Completion

- [x] Alle Unit Tests grün (315/315)
- [ ] Migration getestet (lokale DB)
- [x] `backend_reference.md` Memory aktualisiert
- [x] Alte Backend Files gelöscht (all old entities, services, handlers, configs, DTOs)
- [x] Build successful
- [ ] Phase 5 (Frontend) abgeschlossen
- [ ] `frontend_reference.md` Memory aktualisiert

## 14. Letzte Änderung

- **Datum:** 2026-01-06
- **Status:** ✅ Phase 4 COMPLETE - Backend Consolidation Done (+ Bugfixes)
- **Build:** ✅ Successful (0 errors, 8 warnings)
- **Tests:** ✅ 315/315 pass
- **Bugfixes (2026-01-05):**
  - ✅ **FIX**: Duplicate key constraint `i_x_devices_user_id_device_id` - Device now reuses existing records instead of creating new ones
    - Added `Device.ResetToPending()` method to reset existing device to pending status
    - Modified `DeviceService.CreatePendingDeviceAsync()` to find and reuse existing devices
    - Removed unused `InvalidateExistingDevicesAsync()` method
  - ✅ **FIX**: Column `device_id1` not found in `refresh_tokens` - EF Core navigation property misconfiguration
    - Fixed `RefreshTokenConfiguration.cs`: `.WithMany()` → `.WithMany(d => d.RefreshTokens)`
  - ✅ Added 2 new tests for device reuse scenarios
  - ✅ **API**: Renamed admin endpoints in `SystemUsersController`:
    - `GET /system-users/{id}/sessions` → `GET /system-users/{id}/devices`
    - `DELETE /system-users/{id}/sessions` → `DELETE /system-users/{id}/devices`
    - `DELETE /system-users/{userId}/sessions/{sessionId}` → `DELETE /system-users/{userId}/devices/{deviceId}`
  - ✅ **FIX**: SESSION_REVOKED error when approving device after admin revocation
    - Root cause: Redis revoked session key not cleared when device reset to pending
    - Added `IRevokedSessionService.ClearRevokedSessionAsync()` method
    - `DeviceService.CreatePendingDeviceAsync()` now clears Redis key when reusing existing device
  - ✅ **CHANGE**: Approve device no longer returns tokens
    - `ApproveDeviceHandler` simplified - just marks device as trusted
    - `ApproveDeviceLinkHandler` simplified - just marks device as trusted  
    - User retries login on original device after approval (tokens issued then)
    - Prevents tokens being sent to wrong device (e.g., mobile when approving for desktop)
- **Bugfixes (2026-01-06):**
  - ✅ **FIX**: Logout no longer revokes device trust
    - Logout should end session, not device trust
    - Removed `_deviceService.RevokeAsync()` from `LogoutHandler`
    - User can log back in on same device without re-approval
  - ✅ **FIX**: Device approval/deny link format changed to path parameter
    - Email URLs: `/approve-device/{token}` and `/deny-device/{token}` (was query param)
    - API endpoint: `GET /api/auth/approve-device-link/{token}` (was query param)
  - ✅ **FEATURE**: RememberMe expiration now configurable
    - Added `Jwt:RememberMeExpirationDays` config (default: 30)
    - Changed `Jwt:RefreshTokenExpirationDays` default from 30 to 7
    - Added `ITokenService.RememberMeExpirationDays` property
    - Updated `LoginHandler`, `MfaVerifyHandler`, `RefreshTokenHandler` to use config
- **Completed:**
  - ✅ New Device entity, DeviceStatus enum, DeviceConfiguration
  - ✅ New IDeviceService interface and DeviceService implementation
  - ✅ RefreshToken uses DeviceId (migrated from DeviceSessionId)
  - ✅ ApproveDeviceHandler and ApproveDeviceLinkHandler mark device as trusted (user retries login after)
  - ✅ DenyDeviceHandler uses IDeviceService
  - ✅ AuthController new endpoints: GET/DELETE/PUT/POST /devices
  - ✅ New commands: GetDevices, RevokeDevice, RenameDevice, ApproveDeviceFromSession
  - ✅ Database migration created (DeviceModelConsolidation)
  - ✅ LoginHandler updated to use IDeviceService
  - ✅ MfaVerifyHandler updated to use IDeviceService
  - ✅ RefreshTokenHandler updated to use IDeviceService
  - ✅ MfaConfirmHandler updated to use IDeviceService
  - ✅ AcceptInviteHandler updated to use IDeviceService
  - ✅ RegisterHandler updated (removed unused IDeviceSessionService)
  - ✅ ForceReauthService updated to use IDeviceService.GetTrustedDevicesAsync
  - ✅ Admin session handlers updated (GetUserSessions, RevokeUserSession, RevokeUserSessions)
  - ✅ LogoutHandler, DeactivateSystemUserHandler, UpdateSystemUserHandler, AnonymizeUserHandler updated
  - ✅ IRiskScoringService.CheckForSpoofingAsync updated to accept Device instead of TrustedDevice
  - ✅ **DELETED** old entities: DeviceSession, TrustedDevice, DeviceApprovalRequest
  - ✅ **DELETED** old services: IDeviceSessionService, ITrustedDeviceService, IDeviceApprovalService, DeviceSessionService, TrustedDeviceService, DeviceApprovalService
  - ✅ **DELETED** old handlers: GetSessions, RevokeSession, RevokeAllSessions, UpdateSession, GetTrustedDevices, RemoveTrustedDevice, RenameTrustedDevice, GetUserTrustedDevices, RemoveUserTrustedDevice, RemoveAllUserTrustedDevices
  - ✅ **DELETED** old configurations: DeviceSessionConfiguration, TrustedDeviceConfiguration, DeviceApprovalRequestConfiguration
  - ✅ **DELETED** old DTOs: DeviceSessionDto
  - ✅ DbSets removed from IAppDbContext/AppDbContext (DeviceSessions, TrustedDevices, DeviceApprovalRequests)
  - ✅ DependencyInjection cleaned up (removed old service registrations)
  - ✅ AuthController cleaned up (removed old session/trusted device endpoints)
  - ✅ SystemUsersController cleaned up (removed trusted device endpoints, updated return types)
  - ✅ All test files updated (LoginHandlerTests, RefreshTokenHandlerTests, AnonymizeUserHandlerTests, DeactivateSystemUserHandlerTests, etc.)
  - ✅ TestDataFactory cleaned up (removed CreateDeviceSession, CreateTrustedDevice helpers)
- **Nächster Schritt:** Phase 5 (Frontend)
  1. Apply migration to database (wenn bereit für Test)
  2. Frontend aktualisieren (siehe Phase 5 oben)

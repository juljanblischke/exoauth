# Task 015: Device Trust Refactoring

## 1. Übersicht

**Was wird gebaut?**
Refactoring des Device Trust Systems: Trust wird von Sessions auf eigenständige Devices verschoben. Neues `TrustedDevice` Entity, klare Trennung zwischen Device-Trust und Session-Management.

**Warum?**
Aktuelles Problem:
- `IsTrusted` liegt auf `DeviceSession` → Session revoken = Trust verloren
- Risk-Scoring entscheidet ob Approval nötig → Low-Risk Devices werden nie "trusted"
- User kann keine "Trusted Devices" Liste sehen
- Unklare Semantik: "NewDevice" Check prüft eigentlich "!IsTrusted"

**Neuer Flow:**
```
Login Attempt
     ↓
Is Device Trusted? (DeviceId + Fingerprint in TrustedDevices?)
     ├── NO → ALWAYS require approval (egal welche Location/Risk)
     │         → User approves → Add to TrustedDevices → Create Session
     │
     └── YES → Check for spoofing (Risk Scoring)
               ├── Suspicious (impossible travel, etc.) → Re-verify
               └── OK → Create Session, allow login
```

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich eine Liste meiner vertrauenswürdigen Geräte sehen
- Als User möchte ich ein Gerät aus der Trusted-Liste entfernen können
- Als User möchte ich bei JEDEM neuen Gerät eine Bestätigung durchführen
- Als User möchte ich, dass meine Geräte trusted bleiben auch wenn ich Sessions revoke
- Als Admin möchte ich alle trusted Devices eines Users sehen/entfernen können

### Akzeptanzkriterien
- [ ] Neues `TrustedDevice` Entity mit DeviceId, Fingerprint, Name, TrustedAt
- [ ] `IsTrusted` von `DeviceSession` entfernt
- [ ] Jedes NEUE Gerät erfordert Approval (unabhängig von Risk Score)
- [ ] Trusted Devices überleben Session-Revokes
- [ ] Risk Scoring prüft Spoofing bei trusted Devices
- [ ] User kann Trusted Devices in Settings sehen
- [ ] User kann Trusted Devices entfernen
- [ ] First Registration Device wird auto-trusted

### Edge Cases / Error Handling
- Was wenn DeviceId gespoofed wird? → Risk Scoring erkennt impossible travel
- Was wenn Fingerprint sich ändert (Browser Update)? → Fallback auf DeviceId
- Was wenn User alle Trusted Devices entfernt? → Nächster Login braucht Approval
- Was wenn Admin User's Trusted Devices entfernt? → User muss alle neu approven

## 3. API Endpoints

### Neue Endpoints
| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| GET | /api/auth/devices | JWT | - | `TrustedDevice[]` | Liste trusted Devices |
| DELETE | /api/auth/devices/{id} | JWT | - | 204 | Trusted Device entfernen |
| PUT | /api/auth/devices/{id}/name | JWT | `{ name }` | `TrustedDevice` | Device umbenennen |

### Admin Endpoints
| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| GET | /api/system-users/{id}/devices | JWT + Perm | - | `TrustedDevice[]` | User's Devices |
| DELETE | /api/system-users/{id}/devices/{deviceId} | JWT + Perm | - | 204 | Admin entfernt Device |
| DELETE | /api/system-users/{id}/devices | JWT + Perm | - | 204 | Alle Devices entfernen |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `DEVICE_NOT_FOUND` | 404 | Trusted Device nicht gefunden |
| `CANNOT_REMOVE_CURRENT_DEVICE` | 400 | Kann aktuelles Device nicht entfernen |
| `DEVICE_NOT_TRUSTED` | 403 | Device ist nicht in Trusted Liste |

> ⚠️ **Nach Completion:** Diese Codes zu `backend_reference.md` Memory hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities
| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| `TrustedDevice` | Id, UserId, DeviceId, DeviceFingerprint, Name, Browser, OperatingSystem, DeviceType, TrustedAt, LastUsedAt, LastIpAddress, LastCountry, LastCity | SystemUser (N:1) |

### Entity zu ändern
| Entity | Änderung |
|--------|----------|
| `DeviceSession` | `IsTrusted` Property ENTFERNEN |
| `DeviceSession` | `TrustedDeviceId` (FK, nullable) hinzufügen |

### Migrations
- [ ] Migration: `AddTrustedDevice`
- [ ] Migration: `RemoveIsTrustedFromDeviceSession`

## 6. Files zu erstellen

### Domain Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| TrustedDevice.cs | `src/ExoAuth.Domain/Entities/TrustedDevice.cs` | Neues Entity |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ITrustedDeviceService.cs | `src/ExoAuth.Application/Common/Interfaces/ITrustedDeviceService.cs` | Interface |
| GetTrustedDevicesQuery.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetTrustedDevices/` | Query + Handler |
| RemoveTrustedDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RemoveTrustedDevice/` | Command + Handler |
| RenameTrustedDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RenameTrustedDevice/` | Command + Handler |
| GetUserTrustedDevicesQuery.cs | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetUserTrustedDevices/` | Admin Query |
| RemoveUserTrustedDeviceCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/RemoveUserTrustedDevice/` | Admin Command |
| RemoveAllUserTrustedDevicesCommand.cs | `src/ExoAuth.Application/Features/SystemUsers/Commands/RemoveAllUserTrustedDevices/` | Admin Command |
| TrustedDeviceDto.cs | `src/ExoAuth.Application/Features/Auth/Models/TrustedDeviceDto.cs` | DTO |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| TrustedDeviceConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/TrustedDeviceConfiguration.cs` | EF Config |
| TrustedDeviceService.cs | `src/ExoAuth.Infrastructure/Services/TrustedDeviceService.cs` | Service Implementation |

## 7. Files zu ändern

### Domain Layer
| Datei | Was ändern? |
|-------|-------------|
| `DeviceSession.cs` | `IsTrusted` entfernen, `TrustedDeviceId` hinzufügen |

### Application Layer
| Datei | Was ändern? |
|-------|-------------|
| `LoginHandler.cs` | Neue Trust-Check Logic |
| `MfaVerifyHandler.cs` | Neue Trust-Check Logic |
| `AcceptInviteHandler.cs` | Neue Trust-Check Logic |
| `MfaConfirmHandler.cs` | Neue Trust-Check Logic |
| `ApproveDeviceHandler.cs` | Add to TrustedDevices nach Approval |
| `ApproveDeviceLinkHandler.cs` | Add to TrustedDevices nach Approval |
| `IRiskScoringService.cs` | Interface anpassen |
| `RiskScoringService.cs` | Spoofing-Detection statt NewDevice Check |

### Infrastructure Layer
| Datei | Was ändern? |
|-------|-------------|
| `AppDbContext.cs` | DbSet<TrustedDevice> hinzufügen |
| `DeviceSessionService.cs` | Trust-Logic entfernen, TrustedDeviceId setzen |
| `DeviceSessionConfiguration.cs` | IsTrusted entfernen, FK hinzufügen |
| `DependencyInjection.cs` | TrustedDeviceService registrieren |

### API Layer
| Datei | Was ändern? |
|-------|-------------|
| `AuthController.cs` | Neue Endpoints für Trusted Devices |
| `SystemUsersController.cs` | Admin Endpoints für User Devices |

## 8. Logic Changes im Detail

### A) Neuer Trust Check Flow

```csharp
// In LoginHandler/MfaVerifyHandler:

// 1. Check if device is trusted
var trustedDevice = await _trustedDeviceService.FindAsync(
    userId, 
    deviceId, 
    deviceFingerprint, 
    ct);

if (trustedDevice is null)
{
    // NEW DEVICE → Always require approval
    return AuthResponse.RequiresDeviceApproval(...);
}

// 2. Device is trusted → Check for spoofing
var riskScore = await _riskScoringService.CheckForSpoofingAsync(
    userId,
    trustedDevice,
    geoLocation,
    deviceInfo,
    ct);

if (riskScore.IsSuspicious)
{
    // Possible spoofing → Re-verify
    return AuthResponse.RequiresReVerification(...);
}

// 3. All good → Create session
var session = await _deviceSessionService.CreateSessionAsync(...);
session.LinkToTrustedDevice(trustedDevice.Id);
```

### B) Risk Scoring Changes

**Alt (Task 013):**
- Check if device is trusted → reduce score
- Check if new device → add score
- Threshold decides approval

**Neu (Task 015):**
- Trust check happens BEFORE risk scoring
- Risk scoring only for TRUSTED devices
- Detects: impossible travel, different device type, unusual patterns
- Returns: IsSuspicious (bool), not a score for approval decision

```csharp
public interface IRiskScoringService
{
    // OLD - remove
    Task<RiskScore> CalculateAsync(...);
    bool RequiresApproval(RiskScore riskScore);
    
    // NEW
    Task<SpoofingCheckResult> CheckForSpoofingAsync(
        Guid userId,
        TrustedDevice trustedDevice,
        GeoLocation currentLocation,
        DeviceInfo currentDeviceInfo,
        CancellationToken ct);
}

public record SpoofingCheckResult(
    bool IsSuspicious,
    List<string> SuspiciousFactors,
    int RiskScore
);
```

### C) ApproveDevice Flow Update

```csharp
// After device is approved:
// 1. Create TrustedDevice entry
var trustedDevice = await _trustedDeviceService.AddAsync(
    userId,
    deviceId,
    fingerprint,
    deviceInfo,
    location,
    ct);

// 2. Update session to link to trusted device
await _deviceSessionService.LinkToTrustedDeviceAsync(
    sessionId, 
    trustedDevice.Id, 
    ct);
```

### D) First Registration (Auto-Trust)

```csharp
// In MfaConfirmHandler (forced setup flow):
// After successful MFA setup, auto-trust first device

var hasAnyTrustedDevices = await _trustedDeviceService
    .HasAnyAsync(userId, ct);

if (!hasAnyTrustedDevices)
{
    // First device ever → auto-trust
    var trustedDevice = await _trustedDeviceService.AddAsync(...);
    session.LinkToTrustedDevice(trustedDevice.Id);
}
```

## 9. Implementation Reihenfolge

### Phase 1: New Entity & Service
1. [x] **Domain**: `TrustedDevice` Entity erstellen
2. [x] **Infrastructure**: `TrustedDeviceConfiguration` + DbContext
3. [x] **Migration**: `AddTrustedDevice`
4. [x] **Application**: `ITrustedDeviceService` Interface
5. [x] **Infrastructure**: `TrustedDeviceService` Implementation

### Phase 2: Update Existing Logic
6. [x] **Domain**: `DeviceSession` - IsTrusted entfernen, TrustedDeviceId hinzufügen
7. [x] **Infrastructure**: `DeviceSessionConfiguration` anpassen
8. [x] **Migration**: `UpdateDeviceSessionForTrustedDevice`
9. [x] **Application**: `RiskScoringService` → `CheckForSpoofingAsync`
10. [x] **Application**: `LoginHandler` - neue Trust-Check Logic
11. [x] **Application**: `MfaVerifyHandler` - neue Trust-Check Logic
12. [x] **Application**: `AcceptInviteHandler` - Auto-Trust erste Device
13. [x] **Application**: `MfaConfirmHandler` - Auto-Trust erste Device
14. [x] **Application**: `ApproveDeviceHandler` - Add to TrustedDevices
15. [x] **Application**: `ApproveDeviceLinkHandler` - Add to TrustedDevices

### Phase 3: New Endpoints
16. [x] **Application**: `GetTrustedDevicesQuery`
17. [x] **Application**: `RemoveTrustedDeviceCommand`
18. [x] **Application**: `RenameTrustedDeviceCommand`
19. [x] **API**: `AuthController` - Trusted Device Endpoints
20. [x] **Application**: Admin Commands für User Devices
21. [x] **API**: `SystemUsersController` - Admin Endpoints

### Phase 4: Tests & Cleanup
22. [x] **Tests**: Unit Tests für neue Logic (47 new tests created)
23. [x] **Tests**: Update bestehende Tests (not needed, all pass)
24. [x] **Memory**: `backend_reference.md` aktualisieren

## 10. Tests

### Unit Tests (47 new tests created)
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `TrustedDeviceServiceTests.cs` | Add, Find, Remove, Rename, HasAny | 16 |
| `GetTrustedDevicesHandlerTests.cs` | Query Handler | 5 |
| `RemoveTrustedDeviceHandlerTests.cs` | Command Handler | 8 |
| `RenameTrustedDeviceHandlerTests.cs` | Command Handler | 7 |
| `GetUserTrustedDevicesHandlerTests.cs` | Admin Query Handler | 5 |
| `RemoveUserTrustedDeviceHandlerTests.cs` | Admin Command Handler | 6 |
| `RemoveAllUserTrustedDevicesHandlerTests.cs` | Admin Command Handler | 5 |

## 11. Frontend Changes (Separate Task 016?)

### Neue UI
- Settings → "Trusted Devices" Tab
- Liste aller trusted Devices mit Name, Browser, OS, Last Used, Location
- "Remove" Button pro Device
- "Rename" Option
- Current device markiert (kann nicht entfernt werden)

### Admin UI
- User Detail → "Trusted Devices" Section
- Admin kann alle Devices sehen/entfernen

## 12. Nach Completion

- [x] Alle Unit Tests grün (350 total)
- [x] `backend_reference.md` Memory aktualisiert:
  - Neue Entity: `TrustedDevice`
  - Neue Error Codes
  - Geänderte Files
- [ ] Bestehende Device Trust Endpoints funktionieren noch
- [ ] Migration auf Dev/Staging getestet

## 13. Breaking Changes

⚠️ **Wichtig:** 
- Bestehende `DeviceSession.IsTrusted` Daten gehen verloren
- Migration muss bestehende trusted Sessions zu TrustedDevices konvertieren:

```csharp
// In Migration:
// 1. Create TrustedDevice entries from existing trusted sessions
// 2. Link sessions to new TrustedDevice entries
// 3. Remove IsTrusted column
```

## 14. Letzte Änderung

- **Datum:** 2026-01-04
- **Status:** Backend Complete ✅
- **Build:** 0 errors, 350 unit tests pass
- **New Tests:** 47 unit tests for TrustedDevice functionality
- **Migrations:** AddTrustedDevice, UpdateDeviceSessionForTrustedDevice
- **Nächster Schritt:** Frontend Implementation (Task 016)

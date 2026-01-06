# Task 018: Device Model Frontend

## 1. Übersicht

**Was wird gebaut?**
Frontend-Update für das konsolidierte Device Model (Task 017 Phase 5)

**Warum?**
- Backend API geändert: `/devices` ersetzt `/sessions` + `/trusted-devices`
- Eine einheitliche Device-Liste statt zwei getrennte Sections
- Bessere UX: User sieht alle Geräte an einem Ort mit einheitlichem Status

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich eine einzige Device-Liste sehen, damit ich alle meine Geräte verwalten kann
- Als User möchte ich den Status meiner Geräte sehen (Trusted, Pending, Revoked)
- Als User möchte ich Geräte umbenennen können für bessere Übersicht
- Als User möchte ich Geräte widerrufen können (Logout + Vertrauen entziehen)
- Als User möchte ich pending Geräte von einem bestehenden Gerät aus genehmigen können
- Als Admin möchte ich die Geräte eines Users sehen und verwalten können

### UI/UX Beschreibung
- **Settings → Security**: Sessions + Trusted Devices Sections → Eine "Devices" Section
- **User Admin → Details Sheet**: Sessions Section → Devices Section
- **Device Card**: Zeigt Status Badge, Browser/OS, Location, letzte Nutzung, Actions
- **Device Status Badge**: Trusted (grün), Pending (gelb), Revoked (rot/grau)

### Akzeptanzkriterien
- [x] Eine Device-Liste in Settings Security (statt Sessions + Trusted Devices)
- [x] DeviceStatus Badge zeigt korrekten Status mit Farben
- [x] Aktuelles Device ist markiert ("Current device")
- [x] Device umbenennen funktioniert mit Modal
- [x] Device widerrufen funktioniert mit Confirm Dialog
- [x] Pending Devices können aus bestehendem Device genehmigt werden
- [x] Admin kann User-Devices sehen in User Details Sheet
- [x] Admin kann User-Devices widerrufen
- [x] i18n vollständig (EN + DE)
- [x] Alte Files gelöscht (Sessions + Trusted Devices)

### Edge Cases / Error Handling
- Was passiert wenn API fehlt? → ErrorState anzeigen
- Was passiert bei letztem Device? → "Cannot revoke current device" Warnung
- Was passiert bei leeren Daten? → EmptyState anzeigen

## 3. API Integration

### Auth Endpoints (User's own devices)
| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/auth/devices | GET | - | `DeviceDto[]` | useDevices |
| /api/auth/devices/{id} | DELETE | - | `ApiResponse` | useRevokeDevice |
| /api/auth/devices/{id}/name | PUT | `{ name }` | `DeviceDto` | useRenameDevice |
| /api/auth/devices/{id}/approve | POST | - | `AuthResponse` | useApproveDeviceFromSession |

### Admin Endpoints (User management)
| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/admin/users/{userId}/sessions | GET | - | `DeviceDto[]` | useUserDevices |
| /api/admin/users/{userId}/sessions/{deviceId} | DELETE | - | `ApiResponse` | useRevokeUserDevice |
| /api/admin/users/{userId}/sessions | DELETE | - | `ApiResponse` | useRevokeAllUserDevices |

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| DeviceStatusBadge | Shared | Status Badge (Trusted/Pending/Revoked) |
| DeviceCard | Feature | Device info card mit Actions |
| DevicesList | Feature | Liste von DeviceCards |
| RenameDeviceModal | Feature | Modal zum Umbenennen |
| DevicesSection | Feature | Settings Security Section |

### Bestehende Komponenten nutzen
| Komponente | Woher |
|------------|-------|
| Badge | @/components/ui/badge |
| Button | @/components/ui/button |
| Dialog | @/components/ui/dialog |
| Input | @/components/ui/input |
| ConfirmDialog | @/components/shared/feedback |
| LoadingSpinner | @/components/shared/feedback |
| EmptyState | @/components/shared/feedback |
| ErrorState | @/components/shared/feedback |
| RelativeTime | @/components/shared |

## 5. Files zu erstellen

### Types
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| device.ts | `src/features/auth/types/device.ts` | DeviceDto, DeviceStatus types |

### API
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| devices-api.ts | `src/features/auth/api/devices-api.ts` | Device API calls |
| user-devices-api.ts | `src/features/users/api/user-devices-api.ts` | Admin Device API (update existing) |

### Hooks
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| use-devices.ts | `src/features/auth/hooks/use-devices.ts` | Get user's devices |
| use-revoke-device.ts | `src/features/auth/hooks/use-revoke-device.ts` | Revoke own device |
| use-rename-device.ts | `src/features/auth/hooks/use-rename-device.ts` | Rename own device |
| use-approve-device-from-session.ts | `src/features/auth/hooks/use-approve-device-from-session.ts` | Approve pending device |

### Components
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| device-status-badge.tsx | `src/features/auth/components/device-status-badge.tsx` | Status badge component |
| device-card.tsx | `src/features/auth/components/device-card.tsx` | Device info card |
| devices-list.tsx | `src/features/auth/components/devices-list.tsx` | List of device cards |
| rename-device-modal.tsx | `src/features/auth/components/rename-device-modal.tsx` | Rename modal |
| devices-section.tsx | `src/features/settings/components/devices-section.tsx` | Settings section |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/types/index.ts` | Export device types, remove sessions/trusted-device exports |
| `src/features/auth/hooks/index.ts` | Export neue hooks, remove alte hook exports |
| `src/features/auth/components/index.ts` | Export neue components, remove alte component exports |
| `src/features/auth/api/index.ts` | Export devices-api, remove alte api exports |
| `src/features/settings/components/security-settings.tsx` | Replace SessionsSection + TrustedDevicesSection → DevicesSection |
| `src/features/settings/components/index.ts` | Export DevicesSection, remove alte exports |
| `src/features/users/components/user-details-sheet.tsx` | Update to use DeviceDto |
| `src/features/users/components/user-sessions-section.tsx` | Rename → user-devices-section.tsx, use DeviceDto |
| `src/features/users/hooks/index.ts` | Update hook exports |
| `src/features/users/api/user-devices-api.ts` | Update to use Device endpoints |
| `src/i18n/locales/en/auth.json` | Add devices translations |
| `src/i18n/locales/de/auth.json` | Add devices translations |

## 7. Files zu löschen

### API
| Datei | Pfad |
|-------|------|
| sessions-api.ts | `src/features/auth/api/sessions-api.ts` |
| trusted-devices-api.ts | `src/features/auth/api/trusted-devices-api.ts` |

### Hooks
| Datei | Pfad |
|-------|------|
| use-sessions.ts | `src/features/auth/hooks/use-sessions.ts` |
| use-revoke-session.ts | `src/features/auth/hooks/use-revoke-session.ts` |
| use-revoke-all-sessions.ts | `src/features/auth/hooks/use-revoke-all-sessions.ts` |
| use-update-session.ts | `src/features/auth/hooks/use-update-session.ts` |
| use-trusted-devices.ts | `src/features/auth/hooks/use-trusted-devices.ts` |
| use-remove-trusted-device.ts | `src/features/auth/hooks/use-remove-trusted-device.ts` |
| use-rename-trusted-device.ts | `src/features/auth/hooks/use-rename-trusted-device.ts` |
| use-remove-all-other-devices.ts | `src/features/auth/hooks/use-remove-all-other-devices.ts` |

### Components
| Datei | Pfad |
|-------|------|
| session-card.tsx | `src/features/auth/components/session-card.tsx` |
| sessions-list.tsx | `src/features/auth/components/sessions-list.tsx` |
| session-details-sheet.tsx | `src/features/auth/components/session-details-sheet.tsx` |
| trusted-device-card.tsx | `src/features/auth/components/trusted-device-card.tsx` |
| trusted-devices-section.tsx | `src/features/auth/components/trusted-devices-section.tsx` |
| rename-device-modal.tsx | `src/features/auth/components/rename-device-modal.tsx` (alte Version) |

### Types
| Datei | Pfad |
|-------|------|
| sessions.ts | `src/features/auth/types/sessions.ts` |
| trusted-device.ts | `src/features/auth/types/trusted-device.ts` |

### Settings Components
| Datei | Pfad |
|-------|------|
| sessions-section.tsx | `src/features/settings/components/sessions-section.tsx` |

### Users Components/Hooks
| Datei | Pfad |
|-------|------|
| use-user-trusted-devices.ts | `src/features/users/hooks/use-user-trusted-devices.ts` |
| use-remove-user-trusted-device.ts | `src/features/users/hooks/use-remove-user-trusted-device.ts` |
| use-remove-all-user-trusted-devices.ts | `src/features/users/hooks/use-remove-all-user-trusted-devices.ts` |
| user-devices-section.tsx | `src/features/users/components/user-devices-section.tsx` (alte Version) |

## 8. Neue Dependencies

Keine neuen Dependencies erforderlich.

## 9. Implementation Reihenfolge

### Phase 1: Types + API + Hooks
1. [x] **Types**: `device.ts` - DeviceDto, DeviceStatus enum
2. [x] **API**: `devices-api.ts` - CRUD operations
3. [x] **Hooks**: `use-devices.ts` - Get devices query
4. [x] **Hooks**: `use-revoke-device.ts` - Revoke mutation
5. [x] **Hooks**: `use-rename-device.ts` - Rename mutation
6. [x] **Hooks**: `use-approve-device-from-session.ts` - Approve mutation

### Phase 2: Components
7. [x] **Component**: `device-status-badge.tsx` - Status badge
8. [x] **Component**: `device-card.tsx` - Device info card
9. [x] **Component**: `devices-list.tsx` - List container
10. [x] **Component**: `rename-device-modal.tsx` - Rename dialog

### Phase 3: Integration
11. [x] **Settings**: `devices-section.tsx` - New section component
12. [x] **Settings**: Update `security-settings.tsx` - Use DevicesSection
13. [x] **Users**: Update `user-devices-api.ts` - Use Device endpoints
14. [x] **Users**: Update hooks - use-user-devices, use-revoke-user-device
15. [x] **Users**: Rename `user-sessions-section.tsx` → use DeviceDto

### Phase 4: i18n
16. [x] **i18n**: Add device keys to `en/auth.json`
17. [x] **i18n**: Add device keys to `de/auth.json`

### Phase 5: Cleanup + Index Updates
18. [x] **Delete**: Old API files (sessions-api, trusted-devices-api)
19. [x] **Delete**: Old hook files (use-sessions, use-trusted-devices, etc.)
20. [x] **Delete**: Old component files (session-card, trusted-device-card, etc.)
21. [x] **Delete**: Old type files (sessions.ts, trusted-device.ts)
22. [x] **Delete**: Old settings component (sessions-section.tsx)
23. [x] **Update**: Export indexes (types, hooks, components, api)
24. [x] **Memory**: Update `frontend_reference.md`

## 10. TypeScript Types

### DeviceStatus Enum
```typescript
export type DeviceStatus = 'PendingApproval' | 'Trusted' | 'Revoked'
```

### DeviceDto
```typescript
export interface DeviceDto {
  id: string
  deviceId: string
  name: string | null
  
  // Device Info
  userAgent: string | null
  browser: string | null
  browserVersion: string | null
  operatingSystem: string | null
  osVersion: string | null
  deviceType: string | null // Desktop, Mobile, Tablet
  
  // Location
  ipAddress: string | null
  country: string | null
  countryCode: string | null
  city: string | null
  
  // Status
  status: DeviceStatus
  trustedAt: string | null
  revokedAt: string | null
  lastUsedAt: string
  
  // Flags
  isCurrent: boolean
  
  // Timestamps
  createdAt: string
}
```

### RenameDeviceRequest
```typescript
export interface RenameDeviceRequest {
  name: string
}
```

## 11. i18n Keys

### English (auth.json)
```json
{
  "devices": {
    "title": "Devices",
    "description": "Manage your logged-in devices. Revoke access to devices you no longer use.",
    "empty": {
      "title": "No devices",
      "description": "You don't have any devices yet."
    },
    "current": "Current device",
    "status": {
      "trusted": "Trusted",
      "pending": "Pending Approval",
      "revoked": "Revoked"
    },
    "info": {
      "lastUsed": "Last used",
      "trustedSince": "Trusted since",
      "browser": "Browser",
      "os": "Operating System",
      "location": "Location"
    },
    "actions": {
      "revoke": "Revoke",
      "revokeAll": "Revoke All Other Devices",
      "rename": "Rename",
      "approve": "Approve"
    },
    "rename": {
      "title": "Rename Device",
      "description": "Give this device a custom name for easier identification.",
      "label": "Device Name",
      "placeholder": "e.g. Work Laptop, Home PC",
      "success": "Device renamed successfully"
    },
    "revoke": {
      "title": "Revoke Device",
      "description": "This will log out the device and remove its trusted status. The user will need to verify this device again on next login.",
      "success": "Device revoked successfully",
      "cannotRevokeCurrent": "You cannot revoke your current device"
    },
    "approve": {
      "title": "Approve Device",
      "description": "Approve this pending device to allow it to complete login.",
      "success": "Device approved successfully"
    }
  }
}
```

### German (auth.json)
```json
{
  "devices": {
    "title": "Geräte",
    "description": "Verwalten Sie Ihre angemeldeten Geräte. Widerrufen Sie den Zugriff für Geräte, die Sie nicht mehr verwenden.",
    "empty": {
      "title": "Keine Geräte",
      "description": "Sie haben noch keine Geräte."
    },
    "current": "Aktuelles Gerät",
    "status": {
      "trusted": "Vertraut",
      "pending": "Ausstehende Genehmigung",
      "revoked": "Widerrufen"
    },
    "info": {
      "lastUsed": "Zuletzt verwendet",
      "trustedSince": "Vertraut seit",
      "browser": "Browser",
      "os": "Betriebssystem",
      "location": "Standort"
    },
    "actions": {
      "revoke": "Widerrufen",
      "revokeAll": "Alle anderen Geräte widerrufen",
      "rename": "Umbenennen",
      "approve": "Genehmigen"
    },
    "rename": {
      "title": "Gerät umbenennen",
      "description": "Geben Sie diesem Gerät einen benutzerdefinierten Namen zur leichteren Identifizierung.",
      "label": "Gerätename",
      "placeholder": "z.B. Arbeitslaptop, Heim-PC",
      "success": "Gerät erfolgreich umbenannt"
    },
    "revoke": {
      "title": "Gerät widerrufen",
      "description": "Dadurch wird das Gerät abgemeldet und der Vertrauensstatus entfernt. Der Benutzer muss dieses Gerät bei der nächsten Anmeldung erneut verifizieren.",
      "success": "Gerät erfolgreich widerrufen",
      "cannotRevokeCurrent": "Sie können Ihr aktuelles Gerät nicht widerrufen"
    },
    "approve": {
      "title": "Gerät genehmigen",
      "description": "Genehmigen Sie dieses ausstehende Gerät, um die Anmeldung abzuschließen.",
      "success": "Gerät erfolgreich genehmigt"
    }
  }
}
```

## 12. Nach Completion

- [x] TypeScript keine Errors (`yarn typecheck`)
- [x] Lint passed (`yarn lint`)
- [x] Alle alten Files gelöscht
- [x] `frontend_reference.md` Memory aktualisiert
- [x] `i18n_translations.md` Memory aktualisiert (devices keys)
- [x] Task 018 als vollständig markieren

## 13. Bug Fixes (Post-Implementation)

### Modal Overlay Z-Index Issue
- **Problem:** Modals blocked by black overlay in some browsers
- **Cause:** Dialog overlay and content both had `z-50`
- **Fix:** Updated z-index in UI components:
  - `dialog.tsx`: content `z-[51]`, overlay `z-50`
  - `alert-dialog.tsx`: content `z-[61]`, overlay `z-[60]`
  - `sheet.tsx`: content `z-[51]`, overlay `z-50`

### Device Approval Modal Not Showing After MFA
- **Problem:** After MFA verification with `deviceApprovalRequired: true`, modal didn't appear
- **Cause:** MfaVerifyModal wasn't passing `onDeviceApprovalRequired` callback
- **Fix:** Added callback in `login-form.tsx` to chain MFA → Device Approval

### Risk Factors Not Translated
- **Problem:** Raw risk factor strings like "new_device" showed instead of translated text
- **Fix:** Added `riskFactors` translations to auth.json (EN + DE)

### "This wasn't me" Button Removed
- **Problem:** Deny button didn't make sense in the approval flow
- **Fix:** Removed deny button and related `onDeny` prop from DeviceApprovalModal

### DeviceStatus Numeric Enum Handling
- **Problem:** `TypeError: Cannot read properties of undefined (reading 'bg')`
- **Cause:** Backend returns `status: 1` (number) but frontend expected string `"Trusted"`
- **Fix:** Added `normalizeDeviceStatus()` function and `deviceStatusMap` to convert numeric enum values

### DeviceDetailsSheet Creation
- **Problem:** No way to see device details, cards weren't clickable
- **Fix:** 
  - Created `device-details-sheet.tsx` with full device info
  - Added `onClick` prop to `DeviceCard`
  - Updated `DevicesList` to handle selection and show sheet
  - Added footer with Rename/Revoke actions

### User Devices API Endpoints
- **Problem:** API calls returning 404
- **Cause:** Endpoints used `/admin/users/.../sessions` instead of `/system/users/.../devices`
- **Fix:** Updated `user-devices-api.ts` to use correct paths

### Axios Interceptor Auth Endpoints
- **Problem:** Wrong device approval code caused page refresh instead of showing error
- **Cause:** 401 errors triggered token refresh flow on login page
- **Fix:** Added `/auth/mfa`, `/auth/approve-device`, `/auth/deny-device`, `/auth/forgot-password`, `/auth/reset-password` to excluded endpoints

### MFA Token Expiry Handling
- **Problem:** Expired MFA token showed generic error, no way to restart login
- **Fix:** Added `isTokenExpired` state and expired UI with "Start New Login" button

### Nested Button in UserDevicesSection
- **Problem:** `<button>` cannot be descendant of `<button>` warning
- **Cause:** DeviceItem was a button containing revoke button
- **Fix:** Changed outer wrapper from `<button>` to `<div role="button">` with keyboard support

### UserDevicesSection Device Details
- **Problem:** Device cards in admin user sheet not clickable
- **Fix:** Added DeviceDetailsSheet integration to UserDevicesSection

## 14. Added Components

| Component | Description |
|-----------|-------------|
| DeviceDetailsSheet | Sheet showing full device details with actions |
| DeviceApprovalCodeInput | Code input for device approval flow |

## 15. Letzte Änderung

- **Datum:** 2026-01-06
- **Status:** Completed + Bug Fixes
- **Abgeschlossen:** All phases implemented, bug fixes applied, build passes

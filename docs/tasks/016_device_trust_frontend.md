# Task 016: Device Trust Frontend

## 1. Übersicht

**Was wird gebaut?**
Frontend für das neue Trusted Devices System aus Task 015. User können ihre vertrauenswürdigen Geräte in den Security Settings sehen, umbenennen und entfernen. Admins können User-Devices im User Detail verwalten.

**Warum?**
Backend (Task 015) ist fertig. User brauchen UI um:
- Ihre trusted Devices zu sehen
- Devices umzubenennen (z.B. "Mein Laptop", "Büro PC")
- Nicht mehr genutzte Devices zu entfernen
- Admins müssen User-Devices verwalten können

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich meine trusted Devices in den Security Settings sehen
- Als User möchte ich ein Device umbenennen können (über Modal)
- Als User möchte ich ein Device entfernen können (außer aktuelles)
- Als User möchte ich alle anderen Devices auf einmal entfernen können
- Als Admin möchte ich die Devices eines Users im User Detail sehen
- Als Admin möchte ich einzelne oder alle Devices eines Users entfernen können

### UI/UX Beschreibung

#### User: Settings → Security → Trusted Devices Section
- Section unterhalb von MFA oder Sessions
- Header: "Trusted Devices" mit "Remove All Other" Button
- Liste von Device Cards:
  - Device Icon (💻/📱/📟 basierend auf deviceType)
  - Device Name (editierbar)
  - Browser + Version, OS + Version
  - Location (City, Country)
  - Last Used (relative time)
  - "This Device" Badge für aktuelles Device
  - Rename Button → öffnet Modal
  - Remove Button (versteckt für aktuelles Device)

#### Admin: User Detail Sheet → Trusted Devices Section
- Ähnlich wie Sessions Section
- Header: "Trusted Devices" mit "Remove All" Button
- Device Cards (gleiche Komponente, aber ohne Rename)
- Remove Button pro Device
- Remove All mit Simple Confirm Dialog

### Akzeptanzkriterien
- [ ] Trusted Devices Section in Security Settings sichtbar
- [ ] Device Cards zeigen alle relevanten Infos
- [ ] Aktuelles Device hat Badge und keinen Remove Button
- [ ] Rename öffnet Modal, speichert neuen Namen
- [ ] Remove zeigt Confirm Dialog, entfernt Device
- [ ] "Remove All Other" entfernt alle außer aktuellem Device
- [ ] Admin sieht Devices in User Detail
- [ ] Admin kann einzelne/alle Devices entfernen
- [ ] Loading States für alle Actions
- [ ] Error Handling mit Toast Messages
- [ ] i18n für EN + DE

### Edge Cases / Error Handling
- Was wenn API fehlt? → Error State mit Retry
- Was wenn nur 1 Device (aktuelles)? → "Remove All Other" disabled/hidden
- Was wenn Device bereits entfernt? → Toast + Refresh Liste
- Was wenn Rename fehlschlägt? → Toast Error, Modal bleibt offen

## 3. API Integration

### User Endpoints
| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/auth/devices | GET | - | `TrustedDevice[]` | useTrustedDevices |
| /api/auth/devices/{id} | DELETE | - | 204 | useRemoveTrustedDevice |
| /api/auth/devices/{id}/name | PUT | `{ name }` | `TrustedDevice` | useRenameTrustedDevice |

### Admin Endpoints
| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/system-users/{id}/devices | GET | - | `TrustedDevice[]` | useUserTrustedDevices |
| /api/system-users/{id}/devices/{deviceId} | DELETE | - | 204 | useRemoveUserTrustedDevice |
| /api/system-users/{id}/devices | DELETE | - | 204 | useRemoveAllUserTrustedDevices |

### TrustedDevice Type
```typescript
interface TrustedDevice {
  id: string
  deviceId: string
  name: string
  browser: string | null
  browserVersion: string | null
  operatingSystem: string | null
  osVersion: string | null
  deviceType: string | null  // "Desktop" | "Mobile" | "Tablet"
  lastIpAddress: string | null
  lastCountry: string | null
  lastCity: string | null
  locationDisplay: string | null  // "Berlin, Germany"
  isCurrent: boolean
  trustedAt: string  // ISO date
  lastUsedAt: string | null  // ISO date
}
```

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| TrustedDevicesSection | Feature | Section für Security Settings |
| TrustedDeviceCard | Feature | Einzelnes Device Card |
| RenameDeviceModal | Feature | Modal zum Umbenennen |
| UserDevicesSection | Feature | Admin Section in User Detail |

### Bestehende Komponenten nutzen
| Komponente | Woher | Verwendung |
|------------|-------|------------|
| Button | @/components/ui/button | Actions |
| Dialog | @/components/ui/dialog | Rename Modal |
| Badge | @/components/ui/badge | "This Device" |
| Input | @/components/ui/input | Name Input |
| ConfirmDialog | @/components/shared/feedback | Remove Confirm |
| RelativeTime | @/components/shared | Last Used |
| LoadingSpinner | @/components/shared/feedback | Loading State |
| EmptyState | @/components/shared/feedback | No Devices (unlikely) |

## 5. Files zu erstellen

### Feature: auth (Trusted Devices)
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| trusted-devices-api.ts | `src/features/auth/api/` | API client functions |
| use-trusted-devices.ts | `src/features/auth/hooks/` | GET devices query |
| use-remove-trusted-device.ts | `src/features/auth/hooks/` | DELETE mutation |
| use-rename-trusted-device.ts | `src/features/auth/hooks/` | PUT mutation |
| use-remove-all-other-devices.ts | `src/features/auth/hooks/` | Bulk remove (filter current) |
| trusted-devices-section.tsx | `src/features/auth/components/` | Main section |
| trusted-device-card.tsx | `src/features/auth/components/` | Device card |
| rename-device-modal.tsx | `src/features/auth/components/` | Rename modal |
| trusted-device.ts | `src/features/auth/types/` | TypeScript types |

### Feature: users (Admin Devices)
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| user-devices-api.ts | `src/features/users/api/` | Admin API |
| use-user-trusted-devices.ts | `src/features/users/hooks/` | Admin GET |
| use-remove-user-trusted-device.ts | `src/features/users/hooks/` | Admin DELETE |
| use-remove-all-user-trusted-devices.ts | `src/features/users/hooks/` | Admin DELETE all |
| user-devices-section.tsx | `src/features/users/components/` | Admin section |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/hooks/index.ts` | Neue hooks exportieren |
| `src/features/auth/components/index.ts` | Neue components exportieren |
| `src/features/auth/types/index.ts` | Trusted device types exportieren |
| `src/features/auth/index.ts` | Re-exports |
| `src/features/settings/components/security-settings.tsx` | TrustedDevicesSection einbinden |
| `src/features/users/hooks/index.ts` | Admin hooks exportieren |
| `src/features/users/components/index.ts` | Admin section exportieren |
| `src/features/users/components/user-details-sheet.tsx` | UserDevicesSection einbinden |
| `src/i18n/locales/en/auth.json` | EN translations |
| `src/i18n/locales/de/auth.json` | DE translations |

## 7. Neue Dependencies

Keine neuen Dependencies nötig.

## 8. Implementation Reihenfolge

### Phase 1: Types & API
1. [ ] **Types**: `trusted-device.ts` - TrustedDevice interface
2. [ ] **API**: `trusted-devices-api.ts` - User API functions
3. [ ] **API**: `user-devices-api.ts` - Admin API functions

### Phase 2: User Hooks
4. [ ] **Hook**: `use-trusted-devices.ts` - GET query
5. [ ] **Hook**: `use-remove-trusted-device.ts` - DELETE mutation
6. [ ] **Hook**: `use-rename-trusted-device.ts` - PUT mutation
7. [ ] **Hook**: `use-remove-all-other-devices.ts` - Bulk remove

### Phase 3: User Components
8. [ ] **Component**: `trusted-device-card.tsx` - Device card
9. [ ] **Component**: `rename-device-modal.tsx` - Rename modal
10. [ ] **Component**: `trusted-devices-section.tsx` - Main section
11. [ ] **Integration**: `security-settings.tsx` - Add section

### Phase 4: Admin Hooks
12. [ ] **Hook**: `use-user-trusted-devices.ts` - Admin GET
13. [ ] **Hook**: `use-remove-user-trusted-device.ts` - Admin DELETE
14. [ ] **Hook**: `use-remove-all-user-trusted-devices.ts` - Admin DELETE all

### Phase 5: Admin Components
15. [ ] **Component**: `user-devices-section.tsx` - Admin section
16. [ ] **Integration**: `user-details-sheet.tsx` - Add section

### Phase 6: i18n & Polish
17. [ ] **i18n**: EN translations in auth.json
18. [ ] **i18n**: DE translations in auth.json
19. [ ] **Exports**: Update all index.ts files
20. [ ] **Memory**: Update frontend_reference.md

## 9. i18n Keys

### English (auth.json)
```json
{
  "trustedDevices": {
    "title": "Trusted Devices",
    "description": "Devices that can access your account without additional verification.",
    "thisDevice": "This device",
    "removeOther": "Remove All Other",
    "removeOtherConfirm": "Remove all other trusted devices? You'll need to verify again when logging in from those devices.",
    "noDevices": "No trusted devices",
    "lastUsed": "Last used",
    "trustedSince": "Trusted since",
    "location": "Location",
    "rename": "Rename",
    "remove": "Remove",
    "removeConfirm": "Remove this device from your trusted devices?",
    "removeSuccess": "Device removed",
    "renameTitle": "Rename Device",
    "renameLabel": "Device name",
    "renamePlaceholder": "e.g. My Laptop, Office PC",
    "renameSuccess": "Device renamed",
    "unknownDevice": "Unknown Device",
    "unknownLocation": "Unknown location",
    "unknownBrowser": "Unknown browser"
  }
}
```

### German (auth.json)
```json
{
  "trustedDevices": {
    "title": "Vertrauenswürdige Geräte",
    "description": "Geräte, die ohne zusätzliche Verifizierung auf dein Konto zugreifen können.",
    "thisDevice": "Dieses Gerät",
    "removeOther": "Alle anderen entfernen",
    "removeOtherConfirm": "Alle anderen vertrauenswürdigen Geräte entfernen? Du musst dich bei der nächsten Anmeldung von diesen Geräten erneut verifizieren.",
    "noDevices": "Keine vertrauenswürdigen Geräte",
    "lastUsed": "Zuletzt verwendet",
    "trustedSince": "Vertraut seit",
    "location": "Standort",
    "rename": "Umbenennen",
    "remove": "Entfernen",
    "removeConfirm": "Dieses Gerät aus den vertrauenswürdigen Geräten entfernen?",
    "removeSuccess": "Gerät entfernt",
    "renameTitle": "Gerät umbenennen",
    "renameLabel": "Gerätename",
    "renamePlaceholder": "z.B. Mein Laptop, Büro PC",
    "renameSuccess": "Gerät umbenannt",
    "unknownDevice": "Unbekanntes Gerät",
    "unknownLocation": "Unbekannter Standort",
    "unknownBrowser": "Unbekannter Browser"
  }
}
```

### Admin Keys (users.json)
```json
{
  "trustedDevices": {
    "title": "Trusted Devices",
    "removeAll": "Remove All",
    "removeAllConfirm": "Remove all trusted devices for this user?",
    "removeSuccess": "Device removed",
    "removeAllSuccess": "All devices removed",
    "noDevices": "No trusted devices"
  }
}
```

## 10. Design Notes

### Device Icons
```typescript
const deviceIcons: Record<string, string> = {
  Desktop: "💻",
  Mobile: "📱",
  Tablet: "📟",
  default: "🖥️"
}
```

### Device Card Layout
```
┌─────────────────────────────────────────────────┐
│ 💻  My Laptop                    [This Device]  │
│     Chrome 120 · Windows 11                     │
│     Berlin, Germany                             │
│     Last used: 2 hours ago                      │
│                              [Rename] [Remove]  │
└─────────────────────────────────────────────────┘
```

### Current Device Card (no Remove button)
```
┌─────────────────────────────────────────────────┐
│ 💻  My Laptop                    [This Device]  │
│     Chrome 120 · Windows 11                     │
│     Berlin, Germany                             │
│     Last used: Just now                         │
│                                       [Rename]  │
└─────────────────────────────────────────────────┘
```

## 11. Nach Completion

- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] Alle Translations vorhanden (EN + DE)
- [ ] `frontend_reference.md` Memory aktualisiert
- [ ] `i18n_translations.md` Memory aktualisiert
- [ ] Manual testing completed

## 12. Letzte Änderung

- **Datum:** 2026-01-04
- **Status:** Ready for Implementation
- **Nächster Schritt:** Phase 1 - Types & API

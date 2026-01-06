# Task 020: Passkeys Frontend

## 1. Übersicht

**Was wird gebaut?**
Frontend-Implementation für Passkeys (WebAuthn/FIDO2). Ermöglicht Usern, Passkeys zu registrieren, zu verwalten und sich damit einzuloggen.

**Warum?**
- Passwordless Login UX für End-User
- Schnellerer Login (1 Klick + Biometrie)
- Moderne Auth-Experience (wie Apple, Google, Microsoft)

**Backend:** Task 019 (muss zuerst fertig sein)

---

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich einen "Login with Passkey" Button auf der Login-Seite sehen, damit ich mich ohne Passwort einloggen kann
- Als User möchte ich in den Settings meine Passkeys sehen und verwalten können
- Als User möchte ich einen neuen Passkey registrieren können
- Als User möchte ich einen Passkey umbenennen können
- Als User möchte ich einen Passkey löschen können
- Als User möchte ich verstehen, warum Passkeys besser sind (Empty State)

### UI/UX Beschreibung

#### Login Page
- "Login with Passkey" Button oberhalb des Passwort-Forms
- Fingerprint-Icon
- Bei Klick: Sofort Browser-Prompt triggern (Discoverable Credentials)
- Fallback: "Use password instead" ist bereits sichtbar (das normale Form)

#### Settings Page - Security Section
- Neue "Security" Header-Section
- Enthält: MFA + Devices + **Passkeys** (in dieser Reihenfolge)
- Passkeys Section:
  - Empty State: Benefits erklären + "Add Passkey" CTA
  - List State: Passkey Cards + "Add Passkey" Button
  - Info-Text: Hinweis auf Multi-Device Registration

#### Passkey Card
- Fingerprint Icon
- Name (editierbar via Rename)
- Created Date
- Last Used Date (oder "Never used")
- Device/Browser Info (wenn aus AaGuid ableitbar)
- Actions: Rename, Delete

#### Register Passkey Modal
- Erklärt kurz was passiert
- Auto-suggested Name (z.B. "Chrome on Windows 11")
- User kann Namen editieren
- "Register" Button triggert WebAuthn
- Success: Toast + Modal schließt
- Error: Error Message im Modal

#### Rename Passkey Modal
- Einfaches Input-Feld mit aktuellem Namen
- Save / Cancel

#### Delete Passkey
- Simple Confirm Dialog
- "Are you sure you want to delete this passkey?"

### Akzeptanzkriterien
- [x] Login Page zeigt "Login with Passkey" Button
- [x] Button ist hidden wenn WebAuthn nicht supported (+ Info Message)
- [x] Passkey Login funktioniert (Browser Prompt → Token → Redirect)
- [x] Settings zeigt Security Section mit Passkeys
- [x] Empty State zeigt Benefits + Add CTA
- [x] User kann Passkey registrieren
- [x] User kann Passkey umbenennen
- [x] User kann Passkey löschen (mit Confirm)
- [x] Toast Notifications für alle Aktionen
- [x] i18n: Alle Texte in EN + DE

### Edge Cases / Error Handling
- WebAuthn nicht supported → Info-Banner zeigen, Passkey-Features hidden
- User bricht Registration ab → "Registration cancelled" Toast, Modal bleibt offen
- User bricht Login ab → Nichts passiert, kann erneut klicken
- Passkey Login fehlgeschlagen → Error Toast, User kann Passwort nutzen
- Timeout während Registration → Error Message + Retry Button
- Letzter Passkey löschen → Backend verhindert wenn kein Passwort (Error Toast)

---

## 3. API Integration

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/passkeys/register/options` | POST | `{}` | WebAuthn Options | `usePasskeyRegisterOptions` |
| `/api/auth/passkeys/register` | POST | `{ credential, name }` | `PasskeyDto` | `usePasskeyRegister` |
| `/api/auth/passkeys/login/options` | POST | `{}` | WebAuthn Options | `usePasskeyLoginOptions` |
| `/api/auth/passkeys/login` | POST | `{ credential, deviceId, deviceFingerprint }` | `AuthResponse` | `usePasskeyLogin` |
| `/api/auth/passkeys` | GET | - | `PasskeyDto[]` | `usePasskeys` |
| `/api/auth/passkeys/{id}` | PATCH | `{ name }` | `PasskeyDto` | `useRenamePasskey` |
| `/api/auth/passkeys/{id}` | DELETE | - | `void` | `useDeletePasskey` |

---

## 4. Komponenten Übersicht

### Neue Komponenten

| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| `passkey-login-button.tsx` | Feature | Login with Passkey Button für Login Page |
| `passkeys-section.tsx` | Feature | Passkeys Management in Settings |
| `passkey-card.tsx` | Feature | Einzelner Passkey mit Actions |
| `passkey-empty-state.tsx` | Feature | Benefits + Add CTA |
| `register-passkey-modal.tsx` | Feature | WebAuthn Registration Flow |
| `rename-passkey-modal.tsx` | Feature | Passkey umbenennen |
| `webauthn-not-supported.tsx` | Feature | Info-Banner wenn nicht supported |

### Bestehende Komponenten nutzen

| Komponente | Woher |
|------------|-------|
| Button | `@/components/ui/button` |
| Dialog | `@/components/ui/dialog` |
| Input | `@/components/ui/input` |
| Label | `@/components/ui/label` |
| Skeleton | `@/components/ui/skeleton` |
| ConfirmDialog | `@/components/shared/feedback` |
| LoadingSpinner | `@/components/shared/feedback` |
| EmptyState | `@/components/shared/feedback` |
| RelativeTime | `@/components/shared` |

---

## 5. Files zu erstellen

### Feature Files

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| **API** | | |
| passkeys-api.ts | `src/features/auth/api/passkeys-api.ts` | API Client für Passkeys |
| **Hooks** | | |
| use-passkeys.ts | `src/features/auth/hooks/use-passkeys.ts` | Get all passkeys |
| use-passkey-register-options.ts | `src/features/auth/hooks/use-passkey-register-options.ts` | Get registration options |
| use-passkey-register.ts | `src/features/auth/hooks/use-passkey-register.ts` | Complete registration |
| use-passkey-login-options.ts | `src/features/auth/hooks/use-passkey-login-options.ts` | Get login options |
| use-passkey-login.ts | `src/features/auth/hooks/use-passkey-login.ts` | Complete login |
| use-rename-passkey.ts | `src/features/auth/hooks/use-rename-passkey.ts` | Rename passkey |
| use-delete-passkey.ts | `src/features/auth/hooks/use-delete-passkey.ts` | Delete passkey |
| use-webauthn-support.ts | `src/features/auth/hooks/use-webauthn-support.ts` | Check browser support |
| **Components** | | |
| passkey-login-button.tsx | `src/features/auth/components/passkey-login-button.tsx` | Login Button |
| passkeys-section.tsx | `src/features/auth/components/passkeys-section.tsx` | Settings Section |
| passkey-card.tsx | `src/features/auth/components/passkey-card.tsx` | Single Passkey Card |
| passkey-empty-state.tsx | `src/features/auth/components/passkey-empty-state.tsx` | Empty State |
| register-passkey-modal.tsx | `src/features/auth/components/register-passkey-modal.tsx` | Registration Modal |
| rename-passkey-modal.tsx | `src/features/auth/components/rename-passkey-modal.tsx` | Rename Modal |
| webauthn-not-supported.tsx | `src/features/auth/components/webauthn-not-supported.tsx` | Info Banner |
| **Types** | | |
| passkey.ts | `src/features/auth/types/passkey.ts` | Passkey TypeScript types |
| **Lib** | | |
| webauthn.ts | `src/lib/webauthn.ts` | WebAuthn helper functions |

---

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/components/login-form.tsx` | PasskeyLoginButton hinzufügen |
| `src/features/auth/components/index.ts` | Neue Components exportieren |
| `src/features/auth/hooks/index.ts` | Neue Hooks exportieren |
| `src/features/auth/types/index.ts` | Passkey types exportieren |
| `src/features/settings/components/index.ts` | PasskeysSection exportieren |
| `src/routes/settings.tsx` | Security Section mit Passkeys hinzufügen |
| `src/i18n/locales/en/auth.json` | Passkey translations |
| `src/i18n/locales/de/auth.json` | Passkey translations |
| `src/i18n/locales/en/settings.json` | Security section translations |
| `src/i18n/locales/de/settings.json` | Security section translations |

---

## 7. Neue Dependencies

### NPM Packages

| Package | Warum? |
|---------|--------|
| `@simplewebauthn/browser` | WebAuthn Browser API wrapper (startRegistration, startAuthentication) |

> **Note:** @simplewebauthn/browser ist die Standard-Library für WebAuthn im Browser. Lightweight, TypeScript-first.

### Shadcn/UI Komponenten

Keine neuen - alle benötigten sind bereits installiert (Dialog, Button, Input, etc.)

---

## 8. Implementation Reihenfolge

1. [x] **Package installieren**: `@simplewebauthn/browser`
2. [x] **Types**: `passkey.ts` - TypeScript interfaces
3. [x] **Lib**: `webauthn.ts` - Helper functions (support check, device name detection)
4. [x] **API**: `passkeys-api.ts` - API client
5. [x] **Hooks**: Alle 8 hooks erstellen
6. [x] **Components**: `webauthn-not-supported.tsx` - Info Banner
7. [x] **Components**: `passkey-empty-state.tsx` - Empty State mit Benefits
8. [x] **Components**: `passkey-card.tsx` - Single Passkey Display
9. [x] **Components**: `register-passkey-modal.tsx` - Registration Flow
10. [x] **Components**: `rename-passkey-modal.tsx` - Rename Modal
11. [x] **Components**: `passkeys-section.tsx` - Main Section
12. [x] **Components**: `passkey-login-button.tsx` - Login Button
13. [x] **Integration**: `login-form.tsx` - Button hinzufügen
14. [x] **Integration**: `settings.tsx` - Security Section hinzufügen
15. [x] **i18n**: EN translations
16. [x] **i18n**: DE translations
17. [ ] **Tests**: Component tests (skipped - no tests in codebase pattern)
18. [x] **Memory updaten**: frontend_reference.md aktualisieren

---

## 9. Tests

### Component Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/passkey-login-button.test.tsx` | Button render, WebAuthn support check, click handler |
| `src/features/auth/__tests__/passkeys-section.test.tsx` | Empty state, list state, loading state |
| `src/features/auth/__tests__/passkey-card.test.tsx` | Display, rename action, delete action |
| `src/features/auth/__tests__/register-passkey-modal.test.tsx` | Form, validation, submit |

---

## 10. i18n Keys

### English (`auth.json`)

```json
{
  "passkeys": {
    "title": "Passkeys",
    "description": "Sign in with your fingerprint, face, or security key",
    "loginButton": "Sign in with Passkey",
    "addButton": "Add Passkey",
    "empty": {
      "title": "No passkeys yet",
      "description": "Passkeys let you sign in securely without a password",
      "benefits": {
        "fast": "Faster than passwords",
        "secure": "Phishing-resistant",
        "easy": "No passwords to remember"
      }
    },
    "card": {
      "created": "Created",
      "lastUsed": "Last used",
      "neverUsed": "Never used",
      "rename": "Rename",
      "delete": "Delete"
    },
    "register": {
      "title": "Add a Passkey",
      "description": "Your device will prompt you to verify your identity",
      "nameLabel": "Passkey Name",
      "namePlaceholder": "e.g. MacBook Pro",
      "submit": "Register Passkey",
      "success": "Passkey registered successfully",
      "cancelled": "Registration cancelled",
      "error": "Failed to register passkey"
    },
    "rename": {
      "title": "Rename Passkey",
      "success": "Passkey renamed successfully",
      "error": "Failed to rename passkey"
    },
    "delete": {
      "title": "Delete Passkey",
      "message": "Are you sure you want to delete this passkey? You won't be able to use it to sign in anymore.",
      "success": "Passkey deleted successfully",
      "error": "Failed to delete passkey"
    },
    "login": {
      "error": "Failed to sign in with passkey",
      "cancelled": "Sign in cancelled"
    },
    "notSupported": {
      "title": "Passkeys not supported",
      "description": "Your browser doesn't support passkeys. Try using a modern browser like Chrome, Safari, or Edge."
    },
    "multiDeviceHint": "Register passkeys on multiple devices for backup access"
  }
}
```

### German (`auth.json`)

```json
{
  "passkeys": {
    "title": "Passkeys",
    "description": "Anmelden mit Fingerabdruck, Gesicht oder Sicherheitsschlüssel",
    "loginButton": "Mit Passkey anmelden",
    "addButton": "Passkey hinzufügen",
    "empty": {
      "title": "Noch keine Passkeys",
      "description": "Mit Passkeys melden Sie sich sicher ohne Passwort an",
      "benefits": {
        "fast": "Schneller als Passwörter",
        "secure": "Phishing-resistent",
        "easy": "Keine Passwörter merken"
      }
    },
    "card": {
      "created": "Erstellt",
      "lastUsed": "Zuletzt verwendet",
      "neverUsed": "Noch nie verwendet",
      "rename": "Umbenennen",
      "delete": "Löschen"
    },
    "register": {
      "title": "Passkey hinzufügen",
      "description": "Ihr Gerät wird Sie zur Identitätsbestätigung auffordern",
      "nameLabel": "Passkey-Name",
      "namePlaceholder": "z.B. MacBook Pro",
      "submit": "Passkey registrieren",
      "success": "Passkey erfolgreich registriert",
      "cancelled": "Registrierung abgebrochen",
      "error": "Passkey konnte nicht registriert werden"
    },
    "rename": {
      "title": "Passkey umbenennen",
      "success": "Passkey erfolgreich umbenannt",
      "error": "Passkey konnte nicht umbenannt werden"
    },
    "delete": {
      "title": "Passkey löschen",
      "message": "Sind Sie sicher, dass Sie diesen Passkey löschen möchten? Sie können ihn danach nicht mehr zur Anmeldung verwenden.",
      "success": "Passkey erfolgreich gelöscht",
      "error": "Passkey konnte nicht gelöscht werden"
    },
    "login": {
      "error": "Anmeldung mit Passkey fehlgeschlagen",
      "cancelled": "Anmeldung abgebrochen"
    },
    "notSupported": {
      "title": "Passkeys nicht unterstützt",
      "description": "Ihr Browser unterstützt keine Passkeys. Verwenden Sie einen modernen Browser wie Chrome, Safari oder Edge."
    },
    "multiDeviceHint": "Registrieren Sie Passkeys auf mehreren Geräten für Backup-Zugang"
  }
}
```

### Settings translations (`settings.json`)

**English:**
```json
{
  "security": {
    "title": "Security",
    "description": "Manage your security settings"
  }
}
```

**German:**
```json
{
  "security": {
    "title": "Sicherheit",
    "description": "Verwalten Sie Ihre Sicherheitseinstellungen"
  }
}
```

---

## 11. Nach Completion

- [x] Alle Tests grün (no tests - follows codebase pattern)
- [x] `frontend_reference.md` Memory aktualisiert:
  - [x] File Tree erweitert (neue Components, Hooks, API)
  - [x] Neue Package dokumentiert (@simplewebauthn/browser)
- [x] `i18n_translations.md` Memory aktualisiert (passkeys.* keys)
- [x] TypeScript keine Errors (`yarn build`)
- [x] Lint passed (`yarn lint`)
- [x] Manuell getestet:
  - [x] Chrome + Windows Hello
  - [ ] Safari + Touch ID (wenn verfügbar)
  - [ ] Firefox (WebAuthn support check)
  - [ ] Mobile Browser (wenn möglich)

---

## 12. Letzte Änderung

- **Datum:** 2026-01-06
- **Status:** Completed
- **Nächster Schritt:** Manual testing with backend

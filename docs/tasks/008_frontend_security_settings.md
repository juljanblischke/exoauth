# Task 008: Frontend Security & Settings

## 1. Übersicht

**Was wird gebaut?**
Frontend-Implementierung für alle Backend-Features aus Task 007:
- Settings Page (Security + Language)
- MFA Flow (Setup, Verify, Disable, Backup Codes)
- Device Session Management
- Password Reset Flow
- Login/Register/Invite Updates (Device Info, MFA States)
- Admin User Management Erweiterungen

**Warum?**
- Backend Task 007 ist komplett (232+ Tests)
- NIS2/GDPR Compliance erfordert MFA und Account Management
- User Experience für Security Features

**Backend Task Reference:** `docs/tasks/007_auth_security_device_management.md`

---

## 2. User Experience / Anforderungen

### User Stories

**Password Reset:**
- Als User möchte ich "Passwort vergessen?" auf der Login-Seite klicken und meine Email eingeben
- Als User möchte ich über einen Link mein Passwort zurücksetzen können

**MFA:**
- Als User möchte ich MFA in den Settings aktivieren können
- Als User mit System-Permissions MUSS ich MFA bei Login aktivieren (Modal)
- Als User möchte ich meine Backup Codes sehen und herunterladen können
- Als User möchte ich MFA deaktivieren können

**Device Sessions:**
- Als User möchte ich in den Settings sehen, welche Geräte eingeloggt sind
- Als User möchte ich einzelne Sessions beenden können
- Als User möchte ich "Alle anderen Geräte ausloggen" können
- Als User möchte ich Sessions als "Vertrauenswürdig" markieren können

**Preferences:**
- Als User möchte ich meine Sprache in den Settings ändern können

**Admin User Management:**
- Als Admin möchte ich das MFA eines Users zurücksetzen können
- Als Admin möchte ich gesperrte Accounts entsperren können
- Als Admin möchte ich User Sessions sehen und widerrufen können
- Als Admin möchte ich User deaktivieren/aktivieren können
- Als Admin möchte ich User anonymisieren können (GDPR)

### UI/UX Beschreibung

**Login Page Updates:**
- "Passwort vergessen?" Link → öffnet Forgot Password Modal
- "Angemeldet bleiben" Checkbox
- MFA Verify Modal (wenn `mfaRequired`)
- MFA Setup Modal (wenn `mfaSetupRequired`)

**Settings Page (`/settings`):**
- Tabs: Security | Language
- Security Tab:
  - MFA Section (Enable/Disable, Backup Codes)
  - Sessions Section (List, Revoke, Trust)
- Language Tab:
  - Language Selector (DE/EN)

**Users Page Updates:**
- Neue Badges: MFA, Locked, Anonymized
- Neue Actions: Deactivate/Activate, Unlock, Reset MFA, View Sessions, Anonymize
- Delete Action entfernt
- User Details Sheet: Sessions Section (mit Permission)

### Akzeptanzkriterien

- [ ] Forgot Password Modal funktioniert auf Login Page
- [ ] Reset Password Page funktioniert mit Token aus Email
- [ ] MFA Setup Modal zeigt QR Code und Manual Key
- [ ] MFA Confirm zeigt Backup Codes mit Download + Copy
- [ ] MFA Verify Modal erscheint bei Login wenn `mfaRequired`
- [ ] MFA Setup Modal erscheint bei Login wenn `mfaSetupRequired`
- [ ] Settings Page mit Security und Language Tabs
- [ ] Device Sessions werden angezeigt mit Current-Marker
- [ ] Sessions können revoked/trusted/renamed werden
- [ ] Language Preference wird gespeichert und synced mit i18n
- [ ] Login/Register/Invite senden Device Info
- [ ] Users Table zeigt MFA/Locked/Anonymized Status
- [ ] Admin Actions funktionieren mit korrekten Permissions
- [ ] TypeConfirmDialog für Anonymize Action

### Edge Cases / Error Handling

- MFA Setup abgebrochen → Secret wird verworfen, User kann neu starten
- Backup Code Copy ohne Download → Warnung zeigen
- Session Revoke der eigenen Session → Nicht erlaubt (Backend Error)
- Reset Password Token abgelaufen → Error Message + "Neuen Link anfordern"
- MFA Code falsch → Error + Retry Counter
- Locked Account bei Login → Error mit `lockedUntil` Info

---

## 3. API Integration

### 3.1 Password Reset

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/forgot-password` | POST | `{ email }` | `{ success: true }` | `useForgotPassword` |
| `/api/auth/reset-password` | POST | `{ token?, code?, newPassword }` | `{ success: true }` | `useResetPassword` |

### 3.2 MFA

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/mfa/setup` | POST | - | `{ secret, qrCodeUrl, manualEntryKey }` | `useMfaSetup` |
| `/api/auth/mfa/confirm` | POST | `{ code }` | `{ backupCodes: string[] }` | `useMfaConfirm` |
| `/api/auth/mfa/verify` | POST | `{ mfaToken, code }` | `AuthResponse` | `useMfaVerify` |
| `/api/auth/mfa/disable` | POST | `{ code }` | `{ success: true }` | `useMfaDisable` |
| `/api/auth/mfa/backup-codes` | POST | `{ code }` | `{ backupCodes: string[] }` | `useRegenerateBackupCodes` |

### 3.3 Sessions

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/sessions` | GET | - | `DeviceSession[]` | `useSessions` |
| `/api/auth/sessions/{id}` | DELETE | - | `{ success: true }` | `useRevokeSession` |
| `/api/auth/sessions` | DELETE | - | `{ revokedCount: int }` | `useRevokeAllSessions` |
| `/api/auth/sessions/{id}` | PATCH | `{ name }` | `DeviceSession` | `useUpdateSession` |
| `/api/auth/sessions/{id}/trust` | POST | - | `DeviceSession` | `useTrustSession` |

### 3.4 Preferences

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/me/preferences` | PATCH | `{ language }` | `{ success: true }` | `useUpdatePreferences` |

### 3.5 Login/Register/Invite (Updates)

| Endpoint | Method | New Request Fields | New Response Fields |
|----------|--------|-------------------|---------------------|
| `/api/auth/login` | POST | `deviceId, deviceFingerprint, userAgent, ipAddress, rememberMe` | `sessionId, deviceId, isNewDevice, isNewLocation, mfaRequired?, mfaToken?, mfaSetupRequired?, setupToken?` |
| `/api/auth/register` | POST | `deviceId, deviceFingerprint, userAgent, ipAddress, language` | `sessionId, deviceId` |
| `/api/auth/invite/accept` | POST | `deviceId, deviceFingerprint, userAgent, ipAddress, language` | `sessionId, deviceId` |

### 3.6 Current User (Updates)

| Endpoint | Method | New Response Fields |
|----------|--------|---------------------|
| `/api/auth/me` | GET | `mfaEnabled, preferredLanguage, lastLoginAt` |

### 3.7 Admin User Management

| Endpoint | Method | Request | Response | Hook Name | Permission |
|----------|--------|---------|----------|-----------|------------|
| `/api/users/{id}/mfa/reset` | POST | `{ reason? }` | `{ success: true }` | `useResetUserMfa` | `system:users:mfa:reset` |
| `/api/users/{id}/unlock` | POST | `{ reason? }` | `{ success: true }` | `useUnlockUser` | `system:users:unlock` |
| `/api/users/{id}/sessions` | GET | - | `DeviceSession[]` | `useUserSessions` | `system:users:sessions:view` |
| `/api/users/{id}/sessions` | DELETE | - | `{ revokedCount: int }` | `useRevokeUserSessions` | `system:users:sessions:revoke` |
| `/api/users/{id}/deactivate` | POST | - | `204` | `useDeactivateUser` | `system:users:deactivate` |
| `/api/users/{id}/activate` | POST | - | `204` | `useActivateUser` | `system:users:activate` |
| `/api/users/{id}/anonymize` | POST | - | `{ success: true }` | `useAnonymizeUser` | `system:users:anonymize` |

### 3.8 System Users (Updates)

| Endpoint | Method | New Response Fields |
|----------|--------|---------------------|
| `/api/users` | GET | `mfaEnabled, lastLoginAt, lockedUntil, isLocked, isAnonymized, failedLoginAttempts` |
| `/api/users/{id}` | GET | same |

---

## 4. Komponenten Übersicht

### Neue Komponenten

| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| SettingsPage | Page | Settings mit Tabs |
| SecuritySettings | Feature | MFA + Sessions Section |
| LanguageSettings | Feature | Language Selector |
| MfaSetupModal | Feature | QR Code + Manual Key |
| MfaConfirmModal | Feature | Code Input + Backup Codes Display |
| MfaVerifyModal | Feature | Code Input für Login |
| MfaDisableModal | Feature | Code Input zum Deaktivieren |
| BackupCodesDisplay | Feature | Codes + Download + Copy |
| SessionsList | Feature | Device Sessions Liste |
| SessionCard | Feature | Einzelne Session Anzeige |
| ForgotPasswordModal | Feature | Email Input Modal |
| ResetPasswordPage | Page | New Password Form |
| UserSessionsSection | Feature | Admin: User Sessions im Sheet |
| UserAdminActions | Feature | Admin Action Buttons |

### Bestehende Komponenten ändern

| Komponente | Was ändern? |
|------------|-------------|
| LoginForm | + Remember Me, + Device Info, + MFA Modal Handling |
| RegisterForm | + Device Info |
| AcceptInviteForm | + Device Info, + Language |
| UsersTable | + MFA/Locked/Anonymized Badges |
| UsersTableColumns | + neue Columns/Badges |
| UserDetailsSheet | + Sessions Section, + Admin Actions |
| UserMenu | + Link zu Settings |
| AuthContext | + sessionId, deviceId, preferredLanguage |

### Bestehende Komponenten nutzen

| Komponente | Woher? |
|------------|--------|
| Button | `@/components/ui/button` |
| Dialog | `@/components/ui/dialog` |
| Tabs | `@/components/ui/tabs` |
| Input | `@/components/ui/input` |
| Label | `@/components/ui/label` |
| Badge | `@/components/ui/badge` |
| Switch | `@/components/ui/switch` |
| Separator | `@/components/ui/separator` |
| ScrollArea | `@/components/ui/scroll-area` |
| Skeleton | `@/components/ui/skeleton` |
| FormSheet | `@/components/shared/form` |
| TypeConfirmDialog | `@/components/shared/feedback` |
| ConfirmDialog | `@/components/shared/feedback` |
| LoadingSpinner | `@/components/shared/feedback` |
| EmptyState | `@/components/shared/feedback` |
| StatusBadge | `@/components/shared` |
| CopyButton | `@/components/shared` |
| RelativeTime | `@/components/shared` |
| PageHeader | `@/components/shared/layout` |
| LanguageSwitcher | `@/components/shared/layout` |

---

## 5. Files zu erstellen

### Shared Utilities

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Device Utils | `src/lib/device.ts` | getOrCreateDeviceId, getDeviceFingerprint, getDeviceInfo |

### Auth Feature - MFA

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/auth/api/mfa-api.ts` | MFA API calls |
| Hook | `src/features/auth/hooks/use-mfa-setup.ts` | Setup mutation |
| Hook | `src/features/auth/hooks/use-mfa-confirm.ts` | Confirm mutation |
| Hook | `src/features/auth/hooks/use-mfa-verify.ts` | Verify mutation |
| Hook | `src/features/auth/hooks/use-mfa-disable.ts` | Disable mutation |
| Hook | `src/features/auth/hooks/use-regenerate-backup-codes.ts` | Regenerate mutation |
| Component | `src/features/auth/components/mfa-setup-modal.tsx` | QR Code Modal |
| Component | `src/features/auth/components/mfa-confirm-modal.tsx` | Confirm + Backup Codes |
| Component | `src/features/auth/components/mfa-verify-modal.tsx` | Login MFA Input |
| Component | `src/features/auth/components/mfa-disable-modal.tsx` | Disable Confirm |
| Component | `src/features/auth/components/backup-codes-display.tsx` | Codes + Download |
| Types | `src/features/auth/types/mfa.ts` | MFA Types |

### Auth Feature - Password Reset

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/auth/api/password-reset-api.ts` | Reset API calls |
| Hook | `src/features/auth/hooks/use-forgot-password.ts` | Forgot mutation |
| Hook | `src/features/auth/hooks/use-reset-password.ts` | Reset mutation |
| Component | `src/features/auth/components/forgot-password-modal.tsx` | Email Input Modal |
| Types | `src/features/auth/types/password-reset.ts` | Reset Types |

### Auth Feature - Sessions

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/auth/api/sessions-api.ts` | Sessions API calls |
| Hook | `src/features/auth/hooks/use-sessions.ts` | Sessions query |
| Hook | `src/features/auth/hooks/use-revoke-session.ts` | Revoke mutation |
| Hook | `src/features/auth/hooks/use-revoke-all-sessions.ts` | Revoke all mutation |
| Hook | `src/features/auth/hooks/use-update-session.ts` | Update mutation |
| Hook | `src/features/auth/hooks/use-trust-session.ts` | Trust mutation |
| Component | `src/features/auth/components/sessions-list.tsx` | Sessions List |
| Component | `src/features/auth/components/session-card.tsx` | Single Session |
| Types | `src/features/auth/types/sessions.ts` | Session Types |

### Auth Feature - Preferences

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/auth/api/preferences-api.ts` | Preferences API |
| Hook | `src/features/auth/hooks/use-update-preferences.ts` | Update mutation |

### Settings Feature (NEW)

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Component | `src/features/settings/components/security-settings.tsx` | MFA + Sessions |
| Component | `src/features/settings/components/language-settings.tsx` | Language Selector |
| Component | `src/features/settings/components/mfa-section.tsx` | MFA Enable/Disable UI |
| Component | `src/features/settings/components/sessions-section.tsx` | Sessions Management |
| Index | `src/features/settings/index.ts` | Barrel Export |

### Users Feature - Admin Extensions

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/users/api/user-admin-api.ts` | Admin actions API |
| Hook | `src/features/users/hooks/use-reset-user-mfa.ts` | Reset MFA mutation |
| Hook | `src/features/users/hooks/use-unlock-user.ts` | Unlock mutation |
| Hook | `src/features/users/hooks/use-user-sessions.ts` | User sessions query |
| Hook | `src/features/users/hooks/use-revoke-user-sessions.ts` | Revoke user sessions |
| Hook | `src/features/users/hooks/use-deactivate-user.ts` | Deactivate mutation |
| Hook | `src/features/users/hooks/use-activate-user.ts` | Activate mutation |
| Hook | `src/features/users/hooks/use-anonymize-user.ts` | Anonymize mutation |
| Component | `src/features/users/components/user-sessions-section.tsx` | Admin sessions view |
| Component | `src/features/users/components/user-admin-actions.tsx` | Admin action buttons |
| Component | `src/features/users/components/user-status-badges.tsx` | MFA/Locked/Anon badges |

### Routes

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Settings | `src/routes/settings.tsx` | Settings Page |
| Reset Password | `src/routes/reset-password.tsx` | Reset Password Page |

### i18n

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| EN | `src/i18n/locales/en/settings.json` | Settings translations |
| DE | `src/i18n/locales/de/settings.json` | Settings translations |
| EN | `src/i18n/locales/en/mfa.json` | MFA translations |
| DE | `src/i18n/locales/de/mfa.json` | MFA translations |
| EN | `src/i18n/locales/en/sessions.json` | Sessions translations |
| DE | `src/i18n/locales/de/sessions.json` | Sessions translations |

---

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/types/auth.ts` | + mfaEnabled, preferredLanguage, lastLoginAt zu User |
| `src/features/auth/types/index.ts` | + AuthResponse neue Felder, + Device Types |
| `src/features/auth/api/auth-api.ts` | + Device Info in login/register/invite |
| `src/features/auth/components/login-form.tsx` | + Remember Me, + Device Info, + MFA Handling |
| `src/features/auth/components/register-form.tsx` | + Device Info, + Language |
| `src/features/auth/components/accept-invite-form.tsx` | + Device Info, + Language |
| `src/features/auth/hooks/index.ts` | + neue Hook Exports |
| `src/features/auth/components/index.ts` | + neue Component Exports |
| `src/contexts/auth-context.tsx` | + sessionId, deviceId, preferredLanguage, sync i18n |
| `src/features/users/types/index.ts` | + mfaEnabled, lastLoginAt, lockedUntil, isLocked, isAnonymized |
| `src/features/users/components/users-table.tsx` | + neue Badges |
| `src/features/users/components/users-table-columns.tsx` | + MFA/Lock/Anon Columns |
| `src/features/users/components/user-details-sheet.tsx` | + Sessions Section, + Admin Actions |
| `src/features/users/hooks/index.ts` | + neue Hook Exports, - useDeleteUser |
| `src/features/users/api/users-api.ts` | - deleteUser |
| `src/components/shared/layout/user-menu.tsx` | + Settings Link |
| `src/app/router.tsx` | + /settings, + /reset-password Routes |
| `src/config/navigation.ts` | + Settings Nav Item |
| `src/i18n/index.ts` | + neue Namespaces (settings, mfa, sessions) |
| `src/i18n/locales/en/auth.json` | + forgot password, + MFA login texts |
| `src/i18n/locales/de/auth.json` | + forgot password, + MFA login texts |
| `src/i18n/locales/en/users.json` | + admin action texts, + status texts |
| `src/i18n/locales/de/users.json` | + admin action texts, + status texts |

---

## 7. Neue Dependencies

### NPM Packages

| Package | Warum? |
|---------|--------|
| `qrcode.react` | QR Code für MFA Setup anzeigen |
| `@fingerprintjs/fingerprintjs` | Browser Fingerprinting (optional, kann auch simpler) |

### Shadcn/UI Komponenten

Alle benötigten bereits installiert (Dialog, Tabs, Switch, Badge, etc.)

---

## 8. Neue Permissions (Frontend Check)

| Permission | Wo verwendet? |
|------------|---------------|
| `system:users:mfa:reset` | Reset MFA Button in User Sheet |
| `system:users:unlock` | Unlock Button in User Sheet |
| `system:users:sessions:view` | Sessions Section in User Sheet |
| `system:users:sessions:revoke` | Revoke Sessions Button |
| `system:users:deactivate` | Deactivate Action |
| `system:users:activate` | Activate Action |
| `system:users:anonymize` | Anonymize Action |

---

## 9. Implementation Reihenfolge

### Phase 1: Foundation & Types ✅
1. [x] **Types**: Update auth types (User, AuthResponse)
2. [x] **Types**: Update user types (SystemUser)
3. [x] **Types**: Create session types
4. [x] **Types**: Create MFA types
5. [x] **Utils**: Create device utils (deviceId, fingerprint)

### Phase 2: Auth Flow Updates ✅
6. [x] **API**: Update auth-api.ts with device info
7. [x] **Login**: Add Remember Me checkbox
8. [x] **Login**: Send device info
9. [x] **Register**: Send device info
10. [x] **Accept Invite**: Send device info + language
11. [x] **Context**: Add sessionId, deviceId to auth context
12. [x] **Context**: Sync preferredLanguage with i18n

### Phase 3: Settings Page Foundation ✅
13. [x] **Route**: Create /settings route
14. [x] **Page**: Create settings page with tabs
15. [x] **i18n**: Add settings translations
16. [x] **Nav**: Add settings to user menu

### Phase 4: Language Settings ✅
17. [x] **API**: Create preferences API
18. [x] **Hook**: Create useUpdatePreferences
19. [x] **Component**: Create language settings tab
20. [x] **i18n**: Update on preference change

### Phase 5: Sessions Management ✅
21. [x] **API**: Create sessions API
22. [x] **Hooks**: Create session hooks (query, revoke, trust, update, revokeAll)
23. [x] **Component**: Create sessions list
24. [x] **Component**: Create session card
25. [x] **i18n**: Add sessions translations
26. [x] **Settings**: Add sessions section to security tab

### Phase 6: MFA - Settings ✅
27. [x] **API**: Create MFA API
28. [x] **Hooks**: Create MFA hooks (setup, confirm, disable, backup codes)
29. [x] **Component**: Create MFA setup modal
30. [x] **Component**: Create MFA confirm modal (with backup codes)
31. [x] **Component**: Create backup codes display
32. [x] **Component**: Create MFA disable modal
33. [x] **i18n**: Add MFA translations
34. [x] **Settings**: Add MFA section to security tab

### Phase 7: MFA - Login Flow ✅
35. [x] **Component**: Create MFA verify modal
36. [x] **Login**: Handle mfaRequired state
37. [x] **Login**: Handle mfaSetupRequired state
38. [x] **Context**: Update auth flow for MFA

### Phase 8: Password Reset
39. [ ] **API**: Create password reset API
40. [ ] **Hooks**: Create forgot/reset hooks
41. [ ] **Component**: Create forgot password modal
42. [ ] **Route**: Create reset password page
43. [ ] **Login**: Add forgot password link + modal

### Phase 9: Users Page - Display Updates
44. [ ] **Types**: Verify user types updated
45. [ ] **Component**: Create status badges (MFA, Locked, Anonymized)
46. [ ] **Columns**: Update users table columns
47. [ ] **Sheet**: Update user details display

### Phase 10: Users Page - Admin Actions
48. [ ] **API**: Create user admin API
49. [ ] **Hooks**: Create admin hooks (all 7)
50. [ ] **Component**: Create user sessions section
51. [ ] **Component**: Create admin actions component
52. [ ] **Sheet**: Add admin sections to user details
53. [ ] **i18n**: Add admin action translations
54. [ ] **Remove**: Delete user action entfernen

### Phase 11: Finalization
55. [ ] **Tests**: Component tests
56. [ ] **Tests**: Hook tests
57. [ ] **i18n**: Verify all translations
58. [ ] **Standards**: Update task_standards_frontend.md

---

## 10. Tests

### Component Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/mfa-setup-modal.test.tsx` | QR Display, Manual Key |
| `src/features/auth/__tests__/mfa-verify-modal.test.tsx` | Code Input, Submit |
| `src/features/auth/__tests__/backup-codes-display.test.tsx` | Copy, Download |
| `src/features/auth/__tests__/sessions-list.test.tsx` | List, Current Marker |
| `src/features/auth/__tests__/forgot-password-modal.test.tsx` | Email Submit |
| `src/features/settings/__tests__/security-settings.test.tsx` | MFA + Sessions |
| `src/features/users/__tests__/user-admin-actions.test.tsx` | Permission Checks |

### Hook Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/use-mfa-setup.test.ts` | Setup Flow |
| `src/features/auth/__tests__/use-sessions.test.ts` | Query, Cache |
| `src/features/users/__tests__/use-anonymize-user.test.ts` | Mutation |

---

## 11. i18n Keys (Übersicht)

### settings.json
```json
{
  "title": "Settings",
  "tabs": {
    "security": "Security",
    "language": "Language"
  },
  "language": {
    "title": "Language Preference",
    "description": "Choose your preferred language for the interface and emails"
  }
}
```

### mfa.json
```json
{
  "setup": {
    "title": "Set Up Two-Factor Authentication",
    "scanQr": "Scan this QR code with your authenticator app",
    "manualEntry": "Or enter this code manually",
    "enterCode": "Enter the 6-digit code from your app"
  },
  "verify": {
    "title": "Two-Factor Authentication",
    "enterCode": "Enter the code from your authenticator app"
  },
  "backupCodes": {
    "title": "Backup Codes",
    "description": "Save these codes in a safe place. Each code can only be used once.",
    "download": "Download Codes",
    "copied": "Codes copied to clipboard"
  },
  "disable": {
    "title": "Disable Two-Factor Authentication",
    "warning": "This will make your account less secure",
    "enterCode": "Enter your current code to confirm"
  },
  "status": {
    "enabled": "MFA Enabled",
    "disabled": "MFA Disabled"
  }
}
```

### sessions.json
```json
{
  "title": "Active Sessions",
  "current": "Current Session",
  "trusted": "Trusted",
  "revoke": "Log Out",
  "revokeAll": "Log Out All Other Devices",
  "trust": "Trust This Device",
  "rename": "Rename",
  "lastActive": "Last active",
  "location": "Location",
  "empty": "No other active sessions"
}
```

---

## 12. Error Codes (Frontend Handling)

| Backend Error Code | Frontend Message Key | Beschreibung |
|-------------------|---------------------|--------------|
| `PASSWORD_RESET_TOKEN_INVALID` | `errors:passwordReset.tokenInvalid` | Token ungültig |
| `PASSWORD_RESET_TOKEN_EXPIRED` | `errors:passwordReset.tokenExpired` | Token abgelaufen |
| `MFA_REQUIRED` | - | Zeige MFA Verify Modal |
| `MFA_SETUP_REQUIRED` | - | Zeige MFA Setup Modal |
| `MFA_CODE_INVALID` | `errors:mfa.codeInvalid` | Code falsch |
| `MFA_TOKEN_INVALID` | `errors:mfa.tokenInvalid` | MFA/Setup token ungültig oder abgelaufen |
| `MFA_TOKEN_EXPIRED` | `errors:mfa.tokenExpired` | MFA/Setup token abgelaufen |
| `MFA_ALREADY_ENABLED` | `errors:mfa.alreadyEnabled` | Bereits aktiv |
| `MFA_NOT_ENABLED` | `errors:mfa.notEnabled` | Nicht aktiv |
| `SESSION_NOT_FOUND` | `errors:sessions.notFound` | Session nicht gefunden |
| `SESSION_CANNOT_REVOKE_CURRENT` | `errors:sessions.cannotRevokeCurrent` | Aktuelle Session |
| `ACCOUNT_LOCKED` | `errors:auth.accountLocked` | Account gesperrt |
| `SYSTEM_USER_ALREADY_DEACTIVATED` | `errors:users.alreadyDeactivated` | Bereits deaktiviert |
| `SYSTEM_USER_ALREADY_ACTIVATED` | `errors:users.alreadyActivated` | Bereits aktiv |
| `SYSTEM_USER_ANONYMIZED` | `errors:users.anonymized` | Anonymisiert |

---

## 13. Nach Completion

- [ ] Alle Tests grün
- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] Alle i18n Keys vorhanden (EN + DE)
- [ ] `task_standards_frontend.md` aktualisiert
- [ ] Neue Components in File Tree
- [ ] Neue Packages dokumentiert
- [ ] Permissions in Frontend korrekt geprüft

---

## 14. Entscheidungen

| Frage | Entscheidung |
|-------|--------------|
| Device Identification? | ✅ localStorage mit custom Format (siehe unten) |
| Backup Codes Download Format? | ✅ `.txt` Datei |
| Backup Codes Delay? | ✅ 3 Sekunden bevor "Weiter" aktiv wird |
| Session Rename erlaubt? | ✅ Ja, frei editierbar |
| MFA in User Menu anzeigen? | ❌ Nicht nötig |

### DeviceId Format

```typescript
// Format: {timestamp-first-half}-{random}-{os}-{browser}-{timestamp-second-half}
// Beispiel: "1735474-a1b2c3d4-win-chrome-800000"
// Max: 100 Zeichen (DB Limit), typisch ~35-45 Zeichen

function generateDeviceId(): string {
  const ts = Date.now().toString();
  const tsFirst = ts.slice(0, 7);
  const tsSecond = ts.slice(7);

  const random = crypto.randomUUID().slice(0, 8);
  const os = detectOS();      // "win" | "mac" | "linux" | "android" | "ios"
  const browser = detectBrowser();  // "chrome" | "firefox" | "safari" | "edge"

  return `${tsFirst}-${random}-${os}-${browser}-${tsSecond}`;
}
```

Wird in `localStorage` gespeichert, bleibt bis Cache clear.

---

## 15. Implementation Notes

### MFA SetupToken Flow (2025-12-29)

The backend returns `mfaSetupRequired: true` with a `setupToken` when:
1. User with system permissions logs in but hasn't set up MFA
2. First user registers (gets all permissions, must have MFA)

**Problem solved:** The MFA setup/confirm endpoints required JWT auth, but the user only has a `setupToken` at this point.

**Frontend changes:**
- `mfa-api.ts`: `setup()` and `confirm()` accept optional `setupToken` parameter
- When `setupToken` is provided, it's passed in the Authorization header
- `MfaConfirmRequest` extended with `Partial<DeviceInfo>` for session creation
- `MfaConfirmResponse` extended with optional auth data (user, accessToken, refreshToken, etc.)
- `login-form.tsx`: Stores `setupToken`, passes to MfaSetupModal, handles auth after MFA confirm
- `register-form.tsx`: Same handling for first user registration
- `mfa-setup-modal.tsx`: Accepts `setupToken` prop, includes device info when confirming
- `mfa-confirm-modal.tsx`: Accepts `onContinue` callback for navigation after showing backup codes

**Flow:**
1. Login/Register → `mfaSetupRequired: true, setupToken: "..."`
2. MfaSetupModal opens with `setupToken`
3. MFA setup sends in body: `{ setupToken }`
4. MFA confirm sends in body: `{ code, setupToken, deviceId, deviceFingerprint }`
5. After confirm → returns backup codes + full auth response (user, tokens, sessionId)
6. MfaConfirmModal shows backup codes
7. User clicks Continue → Navigate to dashboard

### Enhanced Device Fingerprint (2025-12-29)

Improved `getDeviceFingerprint()` in `src/lib/device.ts` to collect 20+ data points and return a 64-character hex hash.

**Data points collected:**
- **Navigator:** userAgent, language, languages, platform, vendor, hardwareConcurrency, maxTouchPoints, cookieEnabled, doNotTrack, deviceMemory
- **Screen:** width, height, availWidth, availHeight, colorDepth, devicePixelRatio
- **Timezone:** offset, name (via Intl)
- **Canvas:** rendering fingerprint (draws shapes/text, extracts data URL)
- **WebGL:** vendor, renderer (via WEBGL_debug_renderer_info)
- **Audio:** sample rate (via AudioContext)

**Hashing:**
- Uses SHA-256 via Web Crypto API (async, upgraded in background)
- Fallback to synchronous murmur-style hash if crypto.subtle unavailable
- Result is cached for performance

**Output:** 64-character hex string (e.g., `"a1b2c3d4e5f67890..."`)

### Language Switcher Sync (2025-12-29)

The header `LanguageSwitcher` component now uses `useUpdatePreferences` hook (same as settings page) instead of just `i18n.changeLanguage()`. This ensures:
- Language preference is persisted to backend
- User cache is updated with `preferredLanguage`
- Both header and settings page are synchronized

### Language Locale Format (2025-12-29)

Updated all language codes from short format (`en`, `de`) to full locale format (`en-US`, `de-DE`):

**Files changed:**
- `src/i18n/index.ts` - resources keys and fallbackLng
- `src/components/shared/layout/language-switcher.tsx` - language codes
- `src/features/settings/components/language-settings.tsx` - language codes

**Request format:**
- All API calls (register, accept-invite, preferences) now send full locale: `"en-US"` or `"de-DE"`
- Register now sends `language: i18n.language` to set user's preferred language on account creation

---

## 16. Letzte Änderung

- **Datum:** 2025-12-29
- **Status:** In Progress (Phase 1, 2, 3, 4, 5, 6 & 7 Complete)
- **Backend Dependency:** Task 007 ✅ Complete (232 tests)

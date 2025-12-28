# Task 007: Auth Security & Device Management

## 1. Übersicht

**Was wird gebaut?**
Umfassende Sicherheitserweiterungen für das Auth-System:
- Email Worker als separater Microservice
- Password Reset Flow
- Device Session Management mit GeoIP
- Multi-Factor Authentication (TOTP + Backup Codes)
- User Preferences (Sprache)
- Account Anonymisierung (GDPR)
- Progressive Account Lockout
- Remember Me Funktionalität

**Warum?**
- **NIS2 Compliance**: MFA, Access Control, Incident Detection
- **GDPR Compliance**: Datenexport, Löschung/Anonymisierung
- **Security Best Practices**: Device Tracking, Brute Force Protection
- **Skalierbarkeit**: Email Service unabhängig skalierbar
- **User Experience**: Password Reset, Spracheinstellungen

---

## 2. User Experience / Anforderungen

### User Stories

**Password Reset:**
- Als User möchte ich mein Passwort zurücksetzen können, wenn ich es vergessen habe
- Als User möchte ich per Link ODER Code zurücksetzen können (Flexibilität)

**MFA:**
- Als Admin möchte ich MFA aktivieren, um meinen Account zu schützen
- Als Admin mit System-Permissions MUSS ich MFA aktivieren (Pflicht)
- Als User möchte ich Backup Codes haben, falls ich mein Handy verliere
- Als Admin möchte ich das MFA eines Users zurücksetzen können

**Device Sessions:**
- Als User möchte ich sehen, welche Geräte auf meinen Account zugreifen
- Als User möchte ich einzelne Sessions beenden können
- Als User möchte ich über neue Geräte/Standorte informiert werden
- Als User möchte ich "Remember Me" nutzen können für längere Sessions

**Preferences:**
- Als User möchte ich meine bevorzugte Sprache einstellen können
- Als User möchte ich Emails in meiner Sprache erhalten

**Account Management:**
- Als User möchte ich meinen Account löschen können (Anonymisierung)
- Als Admin möchte ich gesperrte Accounts entsperren können

### Akzeptanzkriterien

- [ ] Password Reset funktioniert mit Link und 6-stelligem Code
- [ ] MFA (TOTP) kann aktiviert/deaktiviert werden
- [ ] Backup Codes werden generiert (10 Stück, Format XXXX-XXXX)
- [ ] Nach Backup Code Nutzung: MFA Reset erforderlich
- [ ] Users mit `system:` Permissions müssen MFA aktivieren
- [ ] Device Sessions werden getrackt mit GeoIP Location
- [ ] Email Benachrichtigungen bei neuen Geräten/Standorten
- [ ] Progressive Lockout bei Failed Logins
- [ ] Remember Me verlängert Session auf 30 Tage
- [ ] User kann Sprache ändern
- [ ] Account Deletion anonymisiert statt löscht
- [ ] Email Worker läuft als separater Service

### Edge Cases / Error Handling

- Was wenn User MFA verliert UND keine Backup Codes hat? → Admin muss MFA resetten
- Was wenn Password Reset Link abgelaufen? → Neuen anfordern
- Was wenn alle Backup Codes verbraucht? → MFA Reset erzwungen
- Was wenn GeoIP Database nicht verfügbar? → Login funktioniert, Location = null
- Was wenn Email Service down? → Messages queuen in RabbitMQ

---

## 3. API Endpoints

### 3.1 Password Reset

| Method | Route | Request Body | Response | Auth | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|--------------|
| POST | `/api/auth/forgot-password` | `{ email }` | `{ success: true }` | - | 3/hour/email | Sendet Reset Email |
| POST | `/api/auth/reset-password` | `{ token, code, newPassword }` | `{ success: true }` | - | 5/15min/IP | Setzt Passwort zurück |

### 3.2 MFA

| Method | Route | Request Body | Response | Auth | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|--------------|
| POST | `/api/auth/mfa/setup` | - | `{ secret, qrCodeUrl, manualEntryKey }` | JWT | 3/hour | Startet MFA Setup |
| POST | `/api/auth/mfa/confirm` | `{ code }` | `{ backupCodes: string[] }` | JWT | 5/5min | Bestätigt MFA mit erstem Code |
| POST | `/api/auth/mfa/verify` | `{ mfaToken, code }` | `{ accessToken, refreshToken, ... }` | mfaToken | 5/5min | Verifiziert MFA bei Login |
| POST | `/api/auth/mfa/disable` | `{ code }` | `{ success: true }` | JWT | 3/hour | Deaktiviert MFA |
| POST | `/api/auth/mfa/backup-codes` | `{ code }` | `{ backupCodes: string[] }` | JWT | 3/hour | Regeneriert Backup Codes |

### 3.3 Sessions

| Method | Route | Request Body | Response | Auth | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|--------------|
| GET | `/api/auth/sessions` | - | `DeviceSession[]` | JWT | Default | Listet meine Sessions |
| DELETE | `/api/auth/sessions/{id}` | - | `{ success: true }` | JWT | Default | Beendet spezifische Session |
| DELETE | `/api/auth/sessions` | - | `{ revokedCount: int }` | JWT | Default | Beendet alle außer aktueller |
| PATCH | `/api/auth/sessions/{id}` | `{ name }` | `DeviceSession` | JWT | Default | Benennt Session um |
| POST | `/api/auth/sessions/{id}/trust` | - | `DeviceSession` | JWT | Default | Markiert als trusted |

### 3.4 Preferences

| Method | Route | Request Body | Response | Auth | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|--------------|
| PATCH | `/api/auth/me/preferences` | `{ language }` | `{ success: true }` | JWT | Default | Aktualisiert Preferences |

### 3.5 Login (Änderungen)

| Method | Route | Request Body | Response | Auth | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|--------------|
| POST | `/api/auth/login` | `{ email, password, rememberMe?, deviceId?, fingerprint? }` | `LoginResponse` (siehe unten) | - | 5/min | Login mit MFA Support |
| POST | `/api/auth/refresh` | `{ refreshToken, deviceId? }` | `{ accessToken, refreshToken, ... }` | - | 10/min | Token Refresh mit Device |

**LoginResponse Varianten:**
```json
// Variante 1: MFA Setup erforderlich (neuer Admin)
{ "mfaSetupRequired": true, "setupToken": "..." }

// Variante 2: MFA Verifizierung erforderlich
{ "mfaRequired": true, "mfaToken": "..." }

// Variante 3: Erfolgreich (kein MFA oder MFA verifiziert)
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAt": "...",
  "deviceId": "...",
  "sessionId": "..."
}
```

### 3.6 Admin User Management

| Method | Route | Request Body | Response | Auth | Permission | Rate Limit | Beschreibung |
|--------|-------|--------------|----------|------|------------|------------|--------------|
| POST | `/api/users/{id}/mfa/reset` | `{ reason? }` | `{ success: true }` | JWT | `users:mfa:reset` | Default | Admin resettet User MFA |
| POST | `/api/users/{id}/unlock` | `{ reason? }` | `{ success: true }` | JWT | `users:unlock` | Default | Entsperrt Account |
| GET | `/api/users/{id}/sessions` | - | `DeviceSession[]` | JWT | `users:sessions:view` | Default | Admin sieht User Sessions |
| DELETE | `/api/users/{id}/sessions` | - | `{ revokedCount: int }` | JWT | `users:sessions:revoke` | Default | Admin revoked alle Sessions |
| DELETE | `/api/users/{id}` | - | `{ success: true }` | JWT | `users:delete` | Default | Anonymisiert User |

---

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `PASSWORD_RESET_TOKEN_INVALID` | 400 | Token/Code ungültig oder abgelaufen |
| `PASSWORD_RESET_TOKEN_EXPIRED` | 400 | Token/Code abgelaufen |
| `PASSWORD_RESET_TOKEN_USED` | 400 | Token/Code bereits verwendet |
| `MFA_REQUIRED` | 403 | MFA Verifizierung erforderlich |
| `MFA_SETUP_REQUIRED` | 403 | MFA muss zuerst eingerichtet werden |
| `MFA_CODE_INVALID` | 400 | TOTP Code ungültig |
| `MFA_ALREADY_ENABLED` | 400 | MFA bereits aktiviert |
| `MFA_NOT_ENABLED` | 400 | MFA nicht aktiviert |
| `MFA_BACKUP_CODE_INVALID` | 400 | Backup Code ungültig |
| `MFA_TOKEN_INVALID` | 401 | MFA Token ungültig oder abgelaufen |
| `SESSION_NOT_FOUND` | 404 | Session nicht gefunden |
| `SESSION_CANNOT_REVOKE_CURRENT` | 400 | Aktuelle Session kann nicht revoked werden |
| `ACCOUNT_LOCKED` | 423 | Account temporär gesperrt |
| `ACCOUNT_LOCKED_PERMANENT` | 423 | Account dauerhaft gesperrt |

> ⚠️ **Nach Completion:** Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

---

## 5. Datenbank Änderungen

### Neue Entities

| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| `DeviceSession` | Id, UserId, DeviceId, DeviceName, DeviceFingerprint, IpAddress, Country, City, IsTrusted, LastActivityAt, CreatedAt, RefreshTokenId | SystemUser (N:1), RefreshToken (1:1) |
| `PasswordResetToken` | Id, UserId, TokenHash, CodeHash, ExpiresAt, IsUsed, UsedAt, CreatedAt | SystemUser (N:1) |
| `MfaBackupCode` | Id, UserId, CodeHash, IsUsed, UsedAt, CreatedAt | SystemUser (N:1) |

### Entity Änderungen

**SystemUser (erweitern):**
```csharp
public string PreferredLanguage { get; private set; } = "en";
public bool MfaEnabled { get; private set; }
public string? MfaSecret { get; private set; }          // Encrypted with Data Protection
public DateTime? MfaEnabledAt { get; private set; }
public DateTime? LastLoginAt { get; private set; }
public DateTime? LockedUntil { get; private set; }
public int FailedLoginAttempts { get; private set; }
public bool IsAnonymized { get; private set; }
public DateTime? AnonymizedAt { get; private set; }
```

**RefreshToken (erweitern):**
```csharp
public Guid? DeviceSessionId { get; private set; }
public bool RememberMe { get; private set; }

// Navigation
public DeviceSession? DeviceSession { get; set; }
```

### Migrations

- [ ] Migration: `AddDeviceSessions`
- [ ] Migration: `AddPasswordResetTokens`
- [ ] Migration: `AddMfaBackupCodes`
- [ ] Migration: `ExtendSystemUserForMfa`
- [ ] Migration: `ExtendRefreshTokenForDevice`

---

## 6. Files zu erstellen

### Phase 1: Email Worker

#### Neues Projekt: ExoAuth.EmailWorker

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Project | `src/ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj` | Worker Service Projekt |
| Program | `src/ExoAuth.EmailWorker/Program.cs` | Host Configuration |
| appsettings | `src/ExoAuth.EmailWorker/appsettings.json` | Configuration |
| appsettings.Dev | `src/ExoAuth.EmailWorker/appsettings.Development.json` | Dev Configuration |
| Dockerfile | `src/ExoAuth.EmailWorker/Dockerfile` | Docker Image |
| Worker | `src/ExoAuth.EmailWorker/EmailWorkerService.cs` | Background Service |
| Consumer | `src/ExoAuth.EmailWorker/Consumers/SendEmailConsumer.cs` | RabbitMQ Consumer (moved) |

### Phase 2: Password Reset

#### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/PasswordResetToken.cs` | Password Reset Token Entity |

#### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` | Request Reset |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordHandler.cs` | Sends Email |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordValidator.cs` | Validation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommand.cs` | Execute Reset |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordHandler.cs` | Resets Password |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordValidator.cs` | Validation |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IPasswordResetService.cs` | Service Interface |

#### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs` | EF Config |
| Service | `src/ExoAuth.Infrastructure/Services/PasswordResetService.cs` | Token Generation |

### Phase 3: Device Sessions

#### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/DeviceSession.cs` | Device Session Entity |

#### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Query | `src/ExoAuth.Application/Features/Auth/Queries/GetSessions/GetSessionsQuery.cs` | List Sessions |
| Handler | `src/ExoAuth.Application/Features/Auth/Queries/GetSessions/GetSessionsHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RevokeSession/RevokeSessionCommand.cs` | Revoke Single |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RevokeSession/RevokeSessionHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RevokeAllSessions/RevokeAllSessionsCommand.cs` | Revoke All |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RevokeAllSessions/RevokeAllSessionsHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/UpdateSessionCommand.cs` | Rename/Trust |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/UpdateSessionHandler.cs` | Handler |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IDeviceSessionService.cs` | Service Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IGeoIpService.cs` | GeoIP Interface |
| Model | `src/ExoAuth.Application/Features/Auth/Models/DeviceSessionDto.cs` | DTO |
| Model | `src/ExoAuth.Application/Common/Models/GeoLocation.cs` | GeoIP Result |

#### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceSessionConfiguration.cs` | EF Config |
| Service | `src/ExoAuth.Infrastructure/Services/DeviceSessionService.cs` | Session Management |
| Service | `src/ExoAuth.Infrastructure/Services/GeoIpService.cs` | MaxMind GeoIP |
| Service | `src/ExoAuth.Infrastructure/Services/DeviceDetectionService.cs` | User-Agent Parsing |

### Phase 4: MFA

#### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/MfaBackupCode.cs` | Backup Code Entity |

#### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaSetup/MfaSetupCommand.cs` | Start Setup |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaSetup/MfaSetupHandler.cs` | Returns QR |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaConfirm/MfaConfirmCommand.cs` | Confirm Setup |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaConfirm/MfaConfirmHandler.cs` | Enables MFA |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyCommand.cs` | Verify at Login |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyHandler.cs` | Completes Login |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaDisable/MfaDisableCommand.cs` | Disable MFA |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaDisable/MfaDisableHandler.cs` | Disables MFA |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RegenerateBackupCodes/RegenerateBackupCodesCommand.cs` | New Codes |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RegenerateBackupCodes/RegenerateBackupCodesHandler.cs` | Generates |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IMfaService.cs` | MFA Service Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IEncryptionService.cs` | Data Protection |
| Model | `src/ExoAuth.Application/Features/Auth/Models/MfaSetupResponse.cs` | Setup DTO |

#### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/MfaBackupCodeConfiguration.cs` | EF Config |
| Service | `src/ExoAuth.Infrastructure/Services/MfaService.cs` | TOTP Implementation |
| Service | `src/ExoAuth.Infrastructure/Services/EncryptionService.cs` | Data Protection |
| Service | `src/ExoAuth.Infrastructure/Services/BackupCodeService.cs` | Code Generation |

### Phase 5: User Preferences & Admin

#### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesCommand.cs` | Update Prefs |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesHandler.cs` | Handler |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesValidator.cs` | Validation |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/ResetUserMfa/ResetUserMfaCommand.cs` | Admin Reset |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/ResetUserMfa/ResetUserMfaHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/UnlockUser/UnlockUserCommand.cs` | Unlock Account |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/UnlockUser/UnlockUserHandler.cs` | Handler |
| Query | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetUserSessions/GetUserSessionsQuery.cs` | Admin View |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetUserSessions/GetUserSessionsHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSessions/RevokeUserSessionsCommand.cs` | Admin Revoke |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSessions/RevokeUserSessionsHandler.cs` | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserCommand.cs` | Anonymize |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserHandler.cs` | Handler |

### Email Templates

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Template | `templates/emails/en/password-reset.html` | Password Reset EN |
| Template | `templates/emails/de/password-reset.html` | Password Reset DE |
| Template | `templates/emails/en/password-changed.html` | Password Changed EN |
| Template | `templates/emails/de/password-changed.html` | Password Changed DE |
| Template | `templates/emails/en/mfa-enabled.html` | MFA Enabled EN |
| Template | `templates/emails/de/mfa-enabled.html` | MFA Enabled DE |
| Template | `templates/emails/en/mfa-disabled.html` | MFA Disabled EN |
| Template | `templates/emails/de/mfa-disabled.html` | MFA Disabled DE |
| Template | `templates/emails/en/mfa-reset-admin.html` | MFA Reset by Admin EN |
| Template | `templates/emails/de/mfa-reset-admin.html` | MFA Reset by Admin DE |
| Template | `templates/emails/en/mfa-backup-code-used.html` | Backup Code Used EN |
| Template | `templates/emails/de/mfa-backup-code-used.html` | Backup Code Used DE |
| Template | `templates/emails/en/new-device-login.html` | New Device EN |
| Template | `templates/emails/de/new-device-login.html` | New Device DE |
| Template | `templates/emails/en/new-location-login.html` | New Location EN |
| Template | `templates/emails/de/new-location-login.html` | New Location DE |
| Template | `templates/emails/en/account-locked.html` | Account Locked EN |
| Template | `templates/emails/de/account-locked.html` | Account Locked DE |
| Template | `templates/emails/en/sessions-revoked.html` | Sessions Revoked EN |
| Template | `templates/emails/de/sessions-revoked.html` | Sessions Revoked DE |

---

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Domain/Entities/SystemUser.cs` | MFA Fields, Preferences, Lockout, Anonymization hinzufügen |
| `src/ExoAuth.Domain/Entities/RefreshToken.cs` | DeviceSessionId, RememberMe hinzufügen |
| `src/ExoAuth.Domain/Constants/SystemPermissions.cs` | Neue Permissions hinzufügen |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginCommand.cs` | rememberMe, deviceId, fingerprint hinzufügen |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginHandler.cs` | MFA Flow, Device Session, Lockout Logic |
| `src/ExoAuth.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs` | deviceId hinzufügen |
| `src/ExoAuth.Application/Features/Auth/Commands/RefreshToken/RefreshTokenHandler.cs` | Device Session Update |
| `src/ExoAuth.Application/Features/Auth/Commands/Logout/LogoutHandler.cs` | Device Session invalidieren |
| `src/ExoAuth.Application/Features/Auth/Models/AuthResponse.cs` | mfaRequired, mfaSetupRequired, sessionId hinzufügen |
| `src/ExoAuth.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserHandler.cs` | MFA Status, Preferences hinzufügen |
| `src/ExoAuth.Application/Features/SystemUsers/Models/SystemUserDto.cs` | MFA Status, LastLogin hinzufügen |
| `src/ExoAuth.Application/Common/Messages/EmailMessage.cs` | Language Parameter hinzufügen |
| `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | Neue DbSets hinzufügen |
| `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs` | Neue Columns konfigurieren |
| `src/ExoAuth.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | DeviceSession Relation |
| `src/ExoAuth.Infrastructure/Services/TokenService.cs` | MFA Token Generation |
| `src/ExoAuth.Infrastructure/Services/BruteForceProtectionService.cs` | Progressive Lockout |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Neue Services registrieren |
| `src/ExoAuth.Api/Controllers/AuthController.cs` | Neue Endpoints hinzufügen |
| `src/ExoAuth.Api/Controllers/SystemUsersController.cs` | Admin MFA/Session Endpoints |
| `src/ExoAuth.Api/appsettings.json` | Neue Configuration Sections |
| `src/ExoAuth.Api/appsettings.Development.json` | Dev Config |
| `docker-compose.yml` | Email Worker Service hinzufügen |
| `backend/ExoAuth.sln` | EmailWorker Projekt hinzufügen |

---

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| `Otp.NET` | latest | ExoAuth.Infrastructure | TOTP Generation/Validation |
| `MaxMind.GeoIP2` | latest | ExoAuth.Infrastructure | GeoIP Lookup |
| `UAParser` | latest | ExoAuth.Infrastructure | User-Agent Parsing |
| `Microsoft.AspNetCore.DataProtection` | 8.x | ExoAuth.Infrastructure | MFA Secret Encryption |

---

## 9. Neue Permissions

```csharp
// Add to SystemPermissions.cs
public const string UsersMfaReset = "users:mfa:reset";
public const string UsersUnlock = "users:unlock";
public const string UsersSessionsView = "users:sessions:view";
public const string UsersSessionsRevoke = "users:sessions:revoke";
```

---

## 10. Neue Audit Log Actions

```csharp
// Add to AuditService or Constants
"password_reset_requested"
"password_reset_completed"
"mfa_setup_started"
"mfa_enabled"
"mfa_disabled"
"mfa_reset_by_admin"
"mfa_backup_code_used"
"session_created"
"session_revoked"
"session_revoked_all"
"session_trusted"
"account_locked"
"account_unlocked"
"user_anonymized"
"preferences_updated"
"login_new_device"
"login_new_location"
```

---

## 11. Configuration (appsettings.json)

```json
{
  "Auth": {
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "RefreshTokenExpiryDaysRememberMe": 30,
    "MaxSessionAgeDays": 30,
    "PasswordResetExpiryMinutes": 15,
    "MfaTokenExpiryMinutes": 5,
    "MaxFailedLoginAttempts": 10
  },
  "Mfa": {
    "Issuer": "ExoAuth",
    "BackupCodeCount": 10,
    "BackupCodeLength": 8
  },
  "GeoIp": {
    "DatabasePath": "./data/geoip/GeoLite2-City.mmdb",
    "Enabled": true
  },
  "Lockout": {
    "ProgressiveDelays": [0, 0, 60, 120, 300, 600, 900, 1800, 3600],
    "NotifyAfterSeconds": 900
  }
}
```

---

## 12. Implementation Reihenfolge

### Phase 1: Email Worker Extraction
1. [ ] **Projekt Setup**: ExoAuth.EmailWorker Projekt erstellen
2. [ ] **Worker Service**: Background Service für RabbitMQ
3. [ ] **Consumer Move**: SendEmailConsumer verschieben
4. [ ] **Docker**: Dockerfile + docker-compose Update
5. [ ] **Test**: Email Sending funktioniert

### Phase 2: Password Reset
6. [ ] **Domain**: PasswordResetToken Entity
7. [ ] **Infrastructure**: EF Configuration + Migration
8. [ ] **Infrastructure**: PasswordResetService
9. [ ] **Application**: ForgotPassword Command/Handler
10. [ ] **Application**: ResetPassword Command/Handler
11. [ ] **API**: AuthController Endpoints
12. [ ] **Templates**: Email Templates (EN/DE)
13. [ ] **Tests**: Unit Tests

### Phase 3: Device Sessions
14. [ ] **Domain**: DeviceSession Entity
15. [ ] **Infrastructure**: EF Configuration + Migration
16. [ ] **Infrastructure**: GeoIpService (MaxMind)
17. [ ] **Infrastructure**: DeviceDetectionService (UAParser)
18. [ ] **Infrastructure**: DeviceSessionService
19. [ ] **Application**: Session Queries/Commands
20. [ ] **Application**: Login Handler Update (Device Tracking)
21. [ ] **API**: Session Endpoints
22. [ ] **Templates**: New Device/Location Emails
23. [ ] **Tests**: Unit Tests

### Phase 4: MFA
24. [ ] **Domain**: MfaBackupCode Entity, SystemUser Extensions
25. [ ] **Infrastructure**: EF Configurations + Migration
26. [ ] **Infrastructure**: EncryptionService (Data Protection)
27. [ ] **Infrastructure**: MfaService (TOTP)
28. [ ] **Infrastructure**: BackupCodeService
29. [ ] **Application**: MFA Commands/Handlers
30. [ ] **Application**: Login Handler Update (MFA Flow)
31. [ ] **API**: MFA Endpoints
32. [ ] **Templates**: MFA Email Templates
33. [ ] **Tests**: Unit Tests

### Phase 5: User Preferences & Admin
34. [ ] **Domain**: SystemUser Preferences Extension
35. [ ] **Application**: UpdatePreferences Command/Handler
36. [ ] **Application**: Admin Commands (Reset MFA, Unlock, Sessions)
37. [ ] **API**: Preferences + Admin Endpoints
38. [ ] **Tests**: Unit Tests

### Phase 6: Progressive Lockout & Anonymization
39. [ ] **Domain**: SystemUser Lockout Fields
40. [ ] **Infrastructure**: BruteForceProtectionService Update
41. [ ] **Application**: Lockout in Login Flow
42. [ ] **Application**: AnonymizeUser Command/Handler
43. [ ] **Templates**: Account Locked Email
44. [ ] **Tests**: Unit Tests

### Phase 7: Finalization
45. [ ] **Integration**: Alle Komponenten zusammen testen
46. [ ] **Standards Update**: task_standards_backend.md aktualisieren
47. [ ] **Standards Update**: coding_standards_backend.md Error Codes
48. [ ] **Documentation**: API Documentation aktualisieren

---

## 13. Tests

### Unit Tests (zu erstellen)

| Test Datei | Was wird getestet? | Geschätzte Anzahl |
|------------|-------------------|-------------------|
| `Features/Auth/ForgotPasswordHandlerTests.cs` | Password Reset Request | ~8 |
| `Features/Auth/ResetPasswordHandlerTests.cs` | Password Reset Execute | ~10 |
| `Features/Auth/MfaSetupHandlerTests.cs` | MFA Setup Flow | ~6 |
| `Features/Auth/MfaConfirmHandlerTests.cs` | MFA Confirmation | ~8 |
| `Features/Auth/MfaVerifyHandlerTests.cs` | MFA Verification at Login | ~10 |
| `Features/Auth/MfaDisableHandlerTests.cs` | MFA Deaktivierung | ~6 |
| `Features/Auth/RegenerateBackupCodesHandlerTests.cs` | Backup Code Regeneration | ~6 |
| `Features/Auth/GetSessionsHandlerTests.cs` | Session Listing | ~5 |
| `Features/Auth/RevokeSessionHandlerTests.cs` | Session Revocation | ~8 |
| `Features/Auth/RevokeAllSessionsHandlerTests.cs` | Revoke All | ~6 |
| `Features/Auth/UpdateSessionHandlerTests.cs` | Session Rename/Trust | ~6 |
| `Features/Auth/UpdatePreferencesHandlerTests.cs` | Preference Update | ~5 |
| `Features/Auth/LoginHandlerTests.cs` | Login mit MFA (erweitern) | ~15 |
| `Features/SystemUsers/ResetUserMfaHandlerTests.cs` | Admin MFA Reset | ~8 |
| `Features/SystemUsers/UnlockUserHandlerTests.cs` | Account Unlock | ~6 |
| `Features/SystemUsers/AnonymizeUserHandlerTests.cs` | User Anonymization | ~8 |
| `Services/MfaServiceTests.cs` | TOTP Generation/Validation | ~12 |
| `Services/BackupCodeServiceTests.cs` | Backup Code Generation | ~8 |
| `Services/PasswordResetServiceTests.cs` | Reset Token Generation | ~8 |
| `Services/DeviceSessionServiceTests.cs` | Session Management | ~10 |
| `Services/GeoIpServiceTests.cs` | GeoIP Lookup | ~5 |
| `Services/EncryptionServiceTests.cs` | Data Protection | ~6 |
| `Services/ProgressiveLockoutTests.cs` | Lockout Timing | ~10 |

**Geschätzte Gesamtanzahl:** ~170 neue Tests

---

## 14. Docker Compose Update

```yaml
services:
  # ... existing services ...

  exoauth-email-worker:
    build:
      context: ./backend
      dockerfile: src/ExoAuth.EmailWorker/Dockerfile
    container_name: exoauth-email-worker
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__RabbitMq=amqp://guest:guest@rabbitmq:5672
      - Email__SmtpHost=mailhog
      - Email__SmtpPort=1025
    volumes:
      - ./backend/templates:/app/templates:ro
    depends_on:
      rabbitmq:
        condition: service_healthy
      mailhog:
        condition: service_started
    restart: unless-stopped
    networks:
      - exoauth-network

  # GeoIP data volume (optional, for development)
  # Download GeoLite2-City.mmdb from MaxMind and place in ./data/geoip/

volumes:
  geoip-data:
    driver: local
```

---

## 15. Nach Completion

- [ ] Alle Unit Tests grün (~170 neue)
- [ ] Email Worker funktioniert standalone
- [ ] MFA Flow komplett getestet
- [ ] Device Sessions funktionieren
- [ ] Progressive Lockout funktioniert
- [ ] GeoIP funktioniert (oder graceful fallback)
- [ ] `task_standards_backend.md` aktualisiert (File Tree, Packages)
- [ ] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [ ] Code reviewed

---

## 16. Offene Fragen / Entscheidungen

| Frage | Entscheidung |
|-------|--------------|
| MFA für alle System-Permission User? | ✅ Ja, Pflicht |
| Backup Code Format? | ✅ XXXX-XXXX (8 chars) |
| Progressive Lockout? | ✅ Ja |
| Email Change erlaubt? | ❌ Nein |
| GeoIP Provider? | ✅ MaxMind GeoLite2 (self-hosted) |
| MFA Secret Encryption? | ✅ .NET Data Protection |
| Session Limit? | ❌ Unlimited |
| Remember Me Duration? | ✅ 30 Tage |

---

## 17. Abhängigkeiten

```mermaid
graph TD
    A[Phase 1: Email Worker] --> B[Phase 2: Password Reset]
    A --> C[Phase 3: Device Sessions]
    C --> D[Phase 4: MFA]
    D --> E[Phase 5: Preferences & Admin]
    E --> F[Phase 6: Lockout & Anonymize]
    F --> G[Phase 7: Finalization]
```

**Kritische Abhängigkeiten:**
- Device Sessions müssen vor MFA implementiert werden (Login Flow)
- Email Worker sollte früh fertig sein (für alle Notifications)
- Encryption Service vor MFA (für Secret Storage)

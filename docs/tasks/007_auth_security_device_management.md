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
| POST | `/api/users/{id}/deactivate` | - | `204 No Content` | JWT | `users:deactivate` | Default | Deaktiviert User |
| POST | `/api/users/{id}/activate` | - | `204 No Content` | JWT | `users:activate` | Default | Aktiviert User |
| POST | `/api/users/{id}/anonymize` | - | `{ success: true }` | JWT | `users:anonymize` | Default | Anonymisiert User (GDPR) |

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
| `SYSTEM_USER_ALREADY_DEACTIVATED` | 400 | User ist bereits deaktiviert |
| `SYSTEM_USER_ALREADY_ACTIVATED` | 400 | User ist bereits aktiv |
| `SYSTEM_USER_ANONYMIZED` | 400 | User ist anonymisiert und kann nicht modifiziert werden |

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

### Phase 1: Email Worker ✅

#### Neues Projekt: ExoAuth.EmailWorker

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Project | `src/ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj` | ✅ | Worker Service Projekt |
| Program | `src/ExoAuth.EmailWorker/Program.cs` | ✅ | Host Configuration mit Serilog |
| appsettings | `src/ExoAuth.EmailWorker/appsettings.json` | ✅ | Production Configuration |
| appsettings.Dev | `src/ExoAuth.EmailWorker/appsettings.Development.json` | ✅ | Dev Configuration |
| Dockerfile | `docker/email-worker/Dockerfile` | ✅ | Multi-stage Docker Image |
| Worker | `src/ExoAuth.EmailWorker/EmailWorkerService.cs` | ✅ | Main Background Service |
| Consumer | `src/ExoAuth.EmailWorker/Consumers/SendEmailConsumer.cs` | ✅ | RabbitMQ Email Consumer |
| Connection | `src/ExoAuth.EmailWorker/RabbitMqConnectionFactory.cs` | ✅ | RabbitMQ Connection Management |
| Interface | `src/ExoAuth.EmailWorker/Services/IEmailTemplateService.cs` | ✅ | Template Service Interface |
| Service | `src/ExoAuth.EmailWorker/Services/EmailTemplateService.cs` | ✅ | Template Rendering |
| Model | `src/ExoAuth.EmailWorker/Models/SendEmailMessage.cs` | ✅ | Email Message DTO |
| Model | `src/ExoAuth.EmailWorker/Models/EmailSettings.cs` | ✅ | SMTP Settings |

#### Templates verschoben

| Von | Nach | Status |
|-----|------|--------|
| `src/ExoAuth.Api/templates/emails/` | `backend/templates/emails/` | ✅ |

#### Templates aktualisiert (Frontend Theme)

| Datei | Status | Änderungen |
|-------|--------|------------|
| `templates/emails/en/system-invite.html` | ✅ | Rose/Red Theme, Inter Font, Modern Design |
| `templates/emails/de/system-invite.html` | ✅ | Rose/Red Theme, Inter Font, Du-Form |

### Phase 2: Password Reset ✅

#### Domain Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/PasswordResetToken.cs` | ✅ | Password Reset Token Entity with XXXX-XXXX code format |

#### Application Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` | ✅ | Request Reset |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordHandler.cs` | ✅ | Sends Email (prevents enumeration) |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordValidator.cs` | ✅ | Validation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommand.cs` | ✅ | Execute Reset |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordHandler.cs` | ✅ | Resets Password + Force Re-auth |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/ResetPassword/ResetPasswordValidator.cs` | ✅ | Validation |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IPasswordResetService.cs` | ✅ | Service Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/ISystemInviteService.cs` | ✅ | Invite Token Service (collision prevention) |

#### Infrastructure Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs` | ✅ | EF Config with unique TokenHash index |
| Service | `src/ExoAuth.Infrastructure/Services/PasswordResetService.cs` | ✅ | Token Generation with collision prevention |
| Service | `src/ExoAuth.Infrastructure/Services/SystemInviteService.cs` | ✅ | Invite Token Generation with collision prevention |

#### Email Templates

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Template | `backend/templates/emails/en/password-reset.html` | ✅ | Password Reset EN |
| Template | `backend/templates/emails/de/password-reset.html` | ✅ | Password Reset DE |
| Template | `backend/templates/emails/en/password-changed.html` | ✅ | Password Changed EN |
| Template | `backend/templates/emails/de/password-changed.html` | ✅ | Password Changed DE |

#### Unit Tests

| Datei | Pfad | Status | Tests |
|-------|------|--------|-------|
| Test | `tests/ExoAuth.UnitTests/Features/Auth/ForgotPasswordHandlerTests.cs` | ✅ | 5 tests |
| Test | `tests/ExoAuth.UnitTests/Features/Auth/ResetPasswordHandlerTests.cs` | ✅ | 9 tests |

#### Migrations

| Migration | Status | Beschreibung |
|-----------|--------|--------------|
| `AddPasswordResetTokens` | ✅ | Password reset tokens table |
| `UpdateSystemInviteTokenHash` | ✅ | SystemInvite: Token → TokenHash (collision prevention) |

### Phase 3: Device Sessions ✅

#### Domain Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/DeviceSession.cs` | ✅ | Device Session Entity with location, device info, trust status |

#### Application Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Query | `src/ExoAuth.Application/Features/Auth/Queries/GetSessions/GetSessionsQuery.cs` | ✅ | List Sessions |
| Handler | `src/ExoAuth.Application/Features/Auth/Queries/GetSessions/GetSessionsHandler.cs` | ✅ | Handler with current session marking |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RevokeSession/RevokeSessionCommand.cs` | ✅ | Revoke Single |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RevokeSession/RevokeSessionHandler.cs` | ✅ | Handler + immediate access token invalidation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RevokeAllSessions/RevokeAllSessionsCommand.cs` | ✅ | Revoke All |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RevokeAllSessions/RevokeAllSessionsHandler.cs` | ✅ | Handler + immediate access token invalidation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/UpdateSessionCommand.cs` | ✅ | Rename/Trust |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/UpdateSessionHandler.cs` | ✅ | Handler |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IDeviceSessionService.cs` | ✅ | Service Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IDeviceDetectionService.cs` | ✅ | User-Agent Parsing Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IGeoIpService.cs` | ✅ | GeoIP Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IRevokedSessionService.cs` | ✅ | Immediate session invalidation via Redis |
| Model | `src/ExoAuth.Application/Features/Auth/Models/DeviceSessionDto.cs` | ✅ | DTO with IsCurrent flag |
| Model | `src/ExoAuth.Application/Common/Models/GeoLocation.cs` | ✅ | GeoIP Result |
| Model | `src/ExoAuth.Application/Common/Models/DeviceInfo.cs` | ✅ | Parsed device info |

#### Infrastructure Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceSessionConfiguration.cs` | ✅ | EF Config with indexes |
| Service | `src/ExoAuth.Infrastructure/Services/DeviceSessionService.cs` | ✅ | Session Management with new device/location detection |
| Service | `src/ExoAuth.Infrastructure/Services/GeoIpService.cs` | ✅ | MaxMind GeoIP with graceful fallback |
| Service | `src/ExoAuth.Infrastructure/Services/DeviceDetectionService.cs` | ✅ | UAParser User-Agent Parsing |
| Service | `src/ExoAuth.Infrastructure/Services/RevokedSessionService.cs` | ✅ | Redis-based immediate session invalidation |

#### API Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Middleware | `src/ExoAuth.Api/Middleware/ForceReauthMiddleware.cs` | ✅ | Updated to check revoked sessions |

#### Files Modified

| Datei | Status | Änderungen |
|-------|--------|------------|
| `RefreshToken.cs` | ✅ | Added DeviceSessionId, RememberMe, LinkToSession() |
| `RefreshTokenConfiguration.cs` | ✅ | Added DeviceSession relationship |
| `AppDbContext.cs` | ✅ | Added DeviceSessions DbSet |
| `IAppDbContext.cs` | ✅ | Added DeviceSessions DbSet |
| `ICurrentUserService.cs` | ✅ | Added SessionId property |
| `CurrentUserService.cs` | ✅ | Added SessionId from JWT claim |
| `ITokenService.cs` | ✅ | Added sessionId parameter to GenerateAccessToken |
| `TokenService.cs` | ✅ | Added session_id claim to JWT |
| `LoginCommand.cs` | ✅ | Added DeviceId, DeviceFingerprint, UserAgent, IpAddress, RememberMe |
| `LoginHandler.cs` | ✅ | Device session creation, session ID in tokens |
| `AuthResponse.cs` | ✅ | Added SessionId, DeviceId, IsNewDevice, IsNewLocation |
| `TokenResponse.cs` | ✅ | Added SessionId |
| `ErrorCodes.cs` | ✅ | Added SessionRevoked |
| `AuthException.cs` | ✅ | Added SessionNotFoundException, CannotRevokeCurrentSessionException |
| `IAuditService.cs` | ✅ | Added session-related AuditActions |

#### Packages Installed

| Package | Version | Status |
|---------|---------|--------|
| MaxMind.GeoIP2 | 5.4.1 | ✅ |
| UAParser | 3.1.47 | ✅ |

#### Additional Files Modified

| Datei | Status | Änderungen |
|-------|--------|------------|
| `AuthController.cs` | ✅ | Session endpoints (GET/DELETE/PATCH), device info in login/register |
| `RefreshTokenHandler.cs` | ✅ | Session activity tracking, session ID in new tokens |
| `RefreshTokenCommand.cs` | ✅ | Added IpAddress parameter |
| `RegisterCommand.cs` | ✅ | Added DeviceId, DeviceFingerprint, UserAgent, IpAddress |
| `RegisterHandler.cs` | ✅ | Device session creation on registration |
| `DependencyInjection.cs` | ✅ | All new services registered |
| `LoginHandlerTests.cs` | ✅ | Updated with IDeviceSessionService mock |
| `RefreshTokenHandlerTests.cs` | ✅ | Updated with IDeviceSessionService mock |
| `RegisterHandlerTests.cs` | ✅ | Updated with IDeviceSessionService mock |
| `TestDataFactory.cs` | ✅ | Added CreateDeviceSession helper |

#### Migrations Created

| Migration | Status | Beschreibung |
|-----------|--------|--------------|
| `AddDeviceSessions` | ✅ | DeviceSessions table + RefreshToken.DeviceSessionId FK |

#### Email Templates

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Template | `backend/templates/emails/en/new-device-login.html` | ✅ | New Device Login EN |
| Template | `backend/templates/emails/de/new-device-login.html` | ✅ | New Device Login DE |
| Template | `backend/templates/emails/en/new-location-login.html` | ✅ | New Location Login EN |
| Template | `backend/templates/emails/de/new-location-login.html` | ✅ | New Location Login DE |

#### Unit Tests

| Datei | Pfad | Status | Tests |
|-------|------|--------|-------|
| Test | `tests/ExoAuth.UnitTests/Features/Auth/GetSessionsHandlerTests.cs` | ✅ | 4 tests |
| Test | `tests/ExoAuth.UnitTests/Features/Auth/RevokeSessionHandlerTests.cs` | ✅ | 6 tests |
| Test | `tests/ExoAuth.UnitTests/Features/Auth/RevokeAllSessionsHandlerTests.cs` | ✅ | 4 tests |
| Test | `tests/ExoAuth.UnitTests/Features/Auth/UpdateSessionHandlerTests.cs` | ✅ | 8 tests |

### Phase 4: MFA ✅

#### Domain Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Entity | `src/ExoAuth.Domain/Entities/MfaBackupCode.cs` | ✅ | Backup Code Entity |
| Entity | `src/ExoAuth.Domain/Entities/SystemUser.cs` | ✅ | Extended with MfaEnabled, MfaSecret, MfaEnabledAt, PreferredLanguage |

#### Application Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaSetup/MfaSetupCommand.cs` | ✅ | Start Setup |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaSetup/MfaSetupHandler.cs` | ✅ | Returns QR |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaConfirm/MfaConfirmCommand.cs` | ✅ | Confirm Setup |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaConfirm/MfaConfirmHandler.cs` | ✅ | Enables MFA + Backup Codes |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/MfaConfirm/MfaConfirmValidator.cs` | ✅ | Validation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyCommand.cs` | ✅ | Verify at Login |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyHandler.cs` | ✅ | Completes Login (TOTP + Backup Code) |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyValidator.cs` | ✅ | Validation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/MfaDisable/MfaDisableCommand.cs` | ✅ | Disable MFA |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/MfaDisable/MfaDisableHandler.cs` | ✅ | Disables MFA |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/MfaDisable/MfaDisableValidator.cs` | ✅ | Validation |
| Command | `src/ExoAuth.Application/Features/Auth/Commands/RegenerateBackupCodes/RegenerateBackupCodesCommand.cs` | ✅ | New Codes |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/RegenerateBackupCodes/RegenerateBackupCodesHandler.cs` | ✅ | Generates |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/RegenerateBackupCodes/RegenerateBackupCodesValidator.cs` | ✅ | Validation |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IMfaService.cs` | ✅ | MFA Service Interface |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IEncryptionService.cs` | ✅ | Data Protection |
| Interface | `src/ExoAuth.Application/Common/Interfaces/IBackupCodeService.cs` | ✅ | Backup Code Interface |
| Model | `src/ExoAuth.Application/Features/Auth/Models/MfaModels.cs` | ✅ | MfaSetupResponse, MfaConfirmResponse, etc. |

#### Infrastructure Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/MfaBackupCodeConfiguration.cs` | ✅ | EF Config |
| Config | `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs` | ✅ | Extended for MFA fields |
| Service | `src/ExoAuth.Infrastructure/Services/MfaService.cs` | ✅ | TOTP Implementation (Otp.NET) |
| Service | `src/ExoAuth.Infrastructure/Services/EncryptionService.cs` | ✅ | Data Protection |
| Service | `src/ExoAuth.Infrastructure/Services/BackupCodeService.cs` | ✅ | Code Generation (XXXX-XXXX format) |

#### API Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Controller | `src/ExoAuth.Api/Controllers/AuthController.cs` | ✅ | MFA endpoints (setup, confirm, verify, disable, backup-codes) |

#### Files Modified

| Datei | Status | Änderungen |
|-------|--------|------------|
| `LoginHandler.cs` | ✅ | MFA flow: MfaRequired response, MfaSetupRequired for system permission users |
| `AuthResponse.cs` | ✅ | Added MfaRequired, MfaToken, MfaSetupRequired, SetupToken fields |
| `AuthException.cs` | ✅ | Added MFA exceptions (MfaAlreadyEnabled, MfaNotEnabled, MfaCodeInvalid, etc.) |
| `ErrorCodes.cs` | ✅ | Added MFA error codes |
| `IAuditService.cs` | ✅ | Added MFA audit actions |
| `IAppDbContext.cs` | ✅ | Added MfaBackupCodes DbSet |
| `AppDbContext.cs` | ✅ | Added MfaBackupCodes DbSet |
| `DependencyInjection.cs` | ✅ | Registered MFA services |
| `Program.cs` | ✅ | Added DataProtection |
| `LoginHandlerTests.cs` | ✅ | Updated with IMfaService mock |

#### Packages Installed

| Package | Version | Status |
|---------|---------|--------|
| Otp.NET | 1.4.1 | ✅ |
| Microsoft.AspNetCore.DataProtection.Abstractions | 8.0.11 | ✅ |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | ✅ |
| Microsoft.Extensions.Configuration.Binder | 8.0.2 | ✅ |

#### Migrations Created

| Migration | Status | Beschreibung |
|-----------|--------|--------------|
| `AddMfaSupport` | ✅ | MfaBackupCodes table + SystemUser MFA fields |

#### Email Templates

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Template | `backend/templates/emails/en/mfa-enabled.html` | ✅ | MFA Enabled EN |
| Template | `backend/templates/emails/de/mfa-enabled.html` | ✅ | MFA Enabled DE |
| Template | `backend/templates/emails/en/mfa-disabled.html` | ✅ | MFA Disabled EN |
| Template | `backend/templates/emails/de/mfa-disabled.html` | ✅ | MFA Disabled DE |
| Template | `backend/templates/emails/en/mfa-backup-code-used.html` | ✅ | Backup Code Used EN |
| Template | `backend/templates/emails/de/mfa-backup-code-used.html` | ✅ | Backup Code Used DE |

#### Unit Tests

| Datei | Pfad | Status | Tests |
|-------|------|--------|-------|
| Test | `tests/ExoAuth.UnitTests/Features/Auth/LoginHandlerTests.cs` | ✅ | Updated with 2 new MFA tests (167 total tests pass) |

### Phase 5: User Preferences & Admin ✅

#### Application Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Command | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesCommand.cs` | ✅ | Update Prefs |
| Handler | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesHandler.cs` | ✅ | Handler |
| Validator | `src/ExoAuth.Application/Features/Auth/Commands/UpdatePreferences/UpdatePreferencesValidator.cs` | ✅ | Validation |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/ResetUserMfa/ResetUserMfaCommand.cs` | ✅ | Admin Reset |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/ResetUserMfa/ResetUserMfaHandler.cs` | ✅ | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/UnlockUser/UnlockUserCommand.cs` | ✅ | Unlock Account |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/UnlockUser/UnlockUserHandler.cs` | ✅ | Handler |
| Query | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetUserSessions/GetUserSessionsQuery.cs` | ✅ | Admin View |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Queries/GetUserSessions/GetUserSessionsHandler.cs` | ✅ | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSessions/RevokeUserSessionsCommand.cs` | ✅ | Admin Revoke |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/RevokeUserSessions/RevokeUserSessionsHandler.cs` | ✅ | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserCommand.cs` | ✅ | Anonymize |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/AnonymizeUser/AnonymizeUserHandler.cs` | ✅ | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/DeactivateSystemUser/DeactivateSystemUserCommand.cs` | ✅ | Deactivate User |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/DeactivateSystemUser/DeactivateSystemUserHandler.cs` | ✅ | Handler |
| Command | `src/ExoAuth.Application/Features/SystemUsers/Commands/ActivateSystemUser/ActivateSystemUserCommand.cs` | ✅ | Activate User |
| Handler | `src/ExoAuth.Application/Features/SystemUsers/Commands/ActivateSystemUser/ActivateSystemUserHandler.cs` | ✅ | Handler |

#### API Layer

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Controller | `src/ExoAuth.Api/Controllers/AuthController.cs` | ✅ | Added PATCH /api/auth/me/preferences |
| Controller | `src/ExoAuth.Api/Controllers/SystemUsersController.cs` | ✅ | Admin endpoints (MFA reset, unlock, sessions, deactivate, activate, anonymize) |

#### Email Templates

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Template | `backend/templates/emails/en/mfa-reset-admin.html` | ✅ | MFA Reset by Admin EN |
| Template | `backend/templates/emails/de/mfa-reset-admin.html` | ✅ | MFA Reset by Admin DE |
| Template | `backend/templates/emails/en/account-unlocked.html` | ✅ | Account Unlocked EN |
| Template | `backend/templates/emails/de/account-unlocked.html` | ✅ | Account Unlocked DE |
| Template | `backend/templates/emails/en/sessions-revoked-admin.html` | ✅ | Sessions Revoked by Admin EN |
| Template | `backend/templates/emails/de/sessions-revoked-admin.html` | ✅ | Sessions Revoked by Admin DE |

#### Unit Tests

| Datei | Pfad | Status | Tests |
|-------|------|--------|-------|
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/ResetUserMfaHandlerTests.cs` | ✅ | 5 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/UnlockUserHandlerTests.cs` | ✅ | 6 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/GetUserSessionsHandlerTests.cs` | ✅ | 5 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/RevokeUserSessionsHandlerTests.cs` | ✅ | 6 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/AnonymizeUserHandlerTests.cs` | ✅ | 6 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/DeactivateSystemUserHandlerTests.cs` | ✅ | 7 tests |
| Test | `tests/ExoAuth.UnitTests/Features/SystemUsers/ActivateSystemUserHandlerTests.cs` | ✅ | 5 tests |
| Test | `tests/ExoAuth.UnitTests/Features/Auth/UpdatePreferencesHandlerTests.cs` | ✅ | 5 tests |

#### Security Improvements (Added)

| Datei | Status | Beschreibung |
|-------|--------|--------------|
| `BaseEntity.cs` | ✅ | Added `RegenerateId()` method for GUID collision handling |
| `RegisterHandler.cs` | ✅ | Added GUID collision retry logic (3 attempts) |
| `AcceptInviteHandler.cs` | ✅ | Added GUID collision retry logic (3 attempts) |
| `InviteSystemUserHandler.cs` | ✅ | Added GUID collision retry logic (3 attempts) |
| `RegisterValidator.cs` | ✅ | Block `@deleted.local` domain (reserved for anonymized users) |
| `InviteSystemUserValidator.cs` | ✅ | Block `@deleted.local` domain (reserved for anonymized users) |
| `ActivateSystemUserHandler.cs` | ✅ | Prevent reactivation of anonymized users |
| `UpdateSystemUserHandler.cs` | ✅ | Prevent modification of anonymized users |
| `SystemException.cs` | ✅ | Added `UserAnonymizedException` |

### All Email Templates (Complete List)

| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Template | `backend/templates/emails/en/system-invite.html` | ✅ | System Invite EN |
| Template | `backend/templates/emails/de/system-invite.html` | ✅ | System Invite DE |
| Template | `backend/templates/emails/en/password-reset.html` | ✅ | Password Reset EN |
| Template | `backend/templates/emails/de/password-reset.html` | ✅ | Password Reset DE |
| Template | `backend/templates/emails/en/password-changed.html` | ✅ | Password Changed EN |
| Template | `backend/templates/emails/de/password-changed.html` | ✅ | Password Changed DE |
| Template | `backend/templates/emails/en/mfa-enabled.html` | ✅ | MFA Enabled EN |
| Template | `backend/templates/emails/de/mfa-enabled.html` | ✅ | MFA Enabled DE |
| Template | `backend/templates/emails/en/mfa-disabled.html` | ✅ | MFA Disabled EN |
| Template | `backend/templates/emails/de/mfa-disabled.html` | ✅ | MFA Disabled DE |
| Template | `backend/templates/emails/en/mfa-reset-admin.html` | ✅ | MFA Reset by Admin EN |
| Template | `backend/templates/emails/de/mfa-reset-admin.html` | ✅ | MFA Reset by Admin DE |
| Template | `backend/templates/emails/en/mfa-backup-code-used.html` | ✅ | Backup Code Used EN |
| Template | `backend/templates/emails/de/mfa-backup-code-used.html` | ✅ | Backup Code Used DE |
| Template | `backend/templates/emails/en/new-device-login.html` | ✅ | New Device EN |
| Template | `backend/templates/emails/de/new-device-login.html` | ✅ | New Device DE |
| Template | `backend/templates/emails/en/new-location-login.html` | ✅ | New Location EN |
| Template | `backend/templates/emails/de/new-location-login.html` | ✅ | New Location DE |
| Template | `backend/templates/emails/en/account-unlocked.html` | ✅ | Account Unlocked EN |
| Template | `backend/templates/emails/de/account-unlocked.html` | ✅ | Account Unlocked DE |
| Template | `backend/templates/emails/en/sessions-revoked-admin.html` | ✅ | Sessions Revoked Admin EN |
| Template | `backend/templates/emails/de/sessions-revoked-admin.html` | ✅ | Sessions Revoked Admin DE |
| Template | `backend/templates/emails/en/account-locked.html` | ✅ | Account Locked EN (Phase 6) |
| Template | `backend/templates/emails/de/account-locked.html` | ✅ | Account Locked DE (Phase 6) |

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
public const string UsersMfaReset = "system:users:mfa:reset";
public const string UsersUnlock = "system:users:unlock";
public const string UsersSessionsView = "system:users:sessions:view";
public const string UsersSessionsRevoke = "system:users:sessions:revoke";
public const string UsersDeactivate = "system:users:deactivate";
public const string UsersActivate = "system:users:activate";
public const string UsersAnonymize = "system:users:anonymize";
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
"user_deactivated"
"user_activated"
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

### Phase 1: Email Worker Extraction ✅
1. [x] **Projekt Setup**: ExoAuth.EmailWorker Projekt erstellen
2. [x] **Worker Service**: Background Service für RabbitMQ
3. [x] **Consumer Move**: SendEmailConsumer verschieben
4. [x] **Docker**: Dockerfile + docker-compose Update
5. [x] **Test**: Build erfolgreich, 129 Unit Tests bestanden

### Phase 2: Password Reset
6. [X] **Domain**: PasswordResetToken Entity
7. [X] **Infrastructure**: EF Configuration + Migration
8. [X] **Infrastructure**: PasswordResetService
9. [X] **Application**: ForgotPassword Command/Handler
10. [X] **Application**: ResetPassword Command/Handler
11. [X] **API**: AuthController Endpoints
12. [X] **Templates**: Email Templates (EN/DE)
13. [X] **Tests**: Unit Tests

### Phase 3: Device Sessions ✅
14. [x] **Domain**: DeviceSession Entity
15. [x] **Infrastructure**: EF Configuration
16. [x] **Infrastructure**: GeoIpService (MaxMind)
17. [x] **Infrastructure**: DeviceDetectionService (UAParser)
18. [x] **Infrastructure**: DeviceSessionService
19. [x] **Infrastructure**: RevokedSessionService (Redis-based immediate invalidation)
20. [x] **Application**: Session Queries/Commands (GetSessions, RevokeSession, RevokeAllSessions, UpdateSession)
21. [x] **Application**: Login Handler Update (Device Tracking + Session ID in JWT)
22. [x] **Application**: Register Handler Update (Device Tracking on Registration)
23. [x] **Middleware**: ForceReauthMiddleware (revoked session check)
24. [x] **API**: Session Endpoints in AuthController (GET/DELETE/PATCH)
25. [x] **Application**: RefreshToken Handler Update (Session activity + token linking)
26. [x] **Infrastructure**: Database Migration (AddDeviceSessions)
27. [x] **Infrastructure**: DI Registration (all services)
28. [x] **Tests**: Existing tests fixed for new dependencies (143 tests passing)
29. [x] **Templates**: New Device/Location Emails (EN/DE)
30. [x] **Tests**: New unit tests for session handlers (22 tests - GetSessions, RevokeSession, RevokeAllSessions, UpdateSession)

### Phase 4: MFA ✅
31. [x] **Domain**: MfaBackupCode Entity, SystemUser Extensions
32. [x] **Infrastructure**: EF Configurations + Migration
33. [x] **Infrastructure**: EncryptionService (Data Protection)
34. [x] **Infrastructure**: MfaService (TOTP)
35. [x] **Infrastructure**: BackupCodeService
36. [x] **Application**: MFA Commands/Handlers (Setup, Confirm, Verify, Disable, RegenerateBackupCodes)
37. [x] **Application**: Login Handler Update (MFA Flow - MfaRequired, MfaSetupRequired for system users)
38. [x] **API**: MFA Endpoints (setup, confirm, verify, disable, backup-codes)
39. [x] **Templates**: MFA Email Templates (EN/DE: mfa-enabled, mfa-disabled, mfa-backup-code-used)
40. [X] **Tests**: Unit Tests (to be added)

### Phase 5: User Preferences & Admin ✅
41. [x] **Domain**: SystemUser Preferences Extension (already done in Phase 4)
42. [x] **Application**: UpdatePreferences Command/Handler
43. [x] **Application**: Admin Commands (ResetUserMfa, UnlockUser, GetUserSessions, RevokeUserSessions, AnonymizeUser)
44. [x] **API**: Preferences Endpoint (PATCH /api/auth/me/preferences)
45. [x] **API**: Admin Endpoints in SystemUsersController (POST mfa/reset, POST unlock, GET/DELETE sessions, POST anonymize)
46. [x] **Templates**: Email Templates (mfa-reset-admin, account-unlocked, sessions-revoked-admin - EN/DE)
47. [x] **Tests**: Unit Tests (45 tests for Phase 5 handlers)

### Phase 6: Progressive Lockout ✅
48. [x] **Infrastructure**: BruteForceProtectionService Update (Progressive Delays)
49. [x] **Application**: Lockout in Login Flow
50. [x] **Templates**: Account Locked Email (EN/DE)
51. [x] **Tests**: Unit Tests (24 tests for BruteForceProtectionService)

### Phase 6.5: Email Language Support & Accept Invite Device Sessions ✅
52. [x] **Fix**: InviteSystemUserHandler - Use `invite.Language` instead of hardcoded "en" for invitation emails
53. [x] **Feature**: AcceptInviteCommand - Added device fields (DeviceId, DeviceFingerprint, UserAgent, IpAddress, Language)
54. [x] **Feature**: AcceptInviteHandler - Added device session creation (like RegisterHandler)
55. [x] **Feature**: LoginHandler - Added new device/location email notifications using user's PreferredLanguage
56. [x] **Fix**: AuthController AcceptInvite endpoint - Pass device info and language to command
57. [x] **Tests**: LoginHandlerTests updated with IConfiguration mock (232 tests pass)

### Phase 6.6: Bug Fixes ✅
58. [x] **Bug Fix**: AcceptInviteHandler - Fixed wrong parameter order in `CreateOrUpdateSessionAsync` call
    - Was: `(userId, deviceId, DeviceFingerprint, UserAgent, IpAddress)` ❌
    - Now: `(userId, deviceId, UserAgent, IpAddress, DeviceFingerprint)` ✅
    - Impact: Device detection and GeoIP lookup were failing for users accepting invites
59. [x] **Bug Fix**: BruteForceProtectionService.GetRemainingAttemptsAsync - Fixed type mismatch
    - Was trying to deserialize integer from Redis as `AttemptsInfo` record (always returned 0)
    - Added `ICacheService.GetIntegerAsync()` method for raw integer retrieval
    - Implemented in `RedisCacheService.GetIntegerAsync()`
    - Removed unused `AttemptsInfo` record
60. [x] **Tests**: All 232 tests still passing after fixes

### Phase 7: Finalization
61. [ ] **Integration**: Alle Komponenten zusammen testen
62. [ ] **Standards Update**: task_standards_backend.md aktualisieren
63. [ ] **Standards Update**: coding_standards_backend.md Error Codes
64. [ ] **Documentation**: API Documentation aktualisieren

### Phase 8: Bug Fixes (Code Review Findings) ✅

> ⚠️ **Discovered during code review on 2025-12-29**
>
> ✅ **All critical bugs resolved** (8 fixed, 1 verified as not a bug)

#### 🔴 Critical Bugs (9) - ✅ COMPLETE

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 1 | **Wrong parameter order in CreateOrUpdateSessionAsync** | `RegisterHandler.cs` | 91-97 | DeviceFingerprint/UserAgent/IpAddress swapped - breaks device detection & GeoIP for registrations | [X] |
| 2 | **Missing IsRevoked check** | `AcceptInviteHandler.cs` | 48-56 | Revoked invites can still be accepted | [X] |
| 3 | **Missing RevokedAt filter in pending invite query** | `InviteSystemUserHandler.cs` | 44-47 | Revoked invites block new invites to same email | [X] |
| 4 | **Missing Redis brute force reset on unlock** | `UnlockUserHandler.cs` | 44 | Admin unlocks user in DB but Redis still blocks them | [X] |
| 5 | **Missing session revocation on deactivate** | `DeactivateSystemUserHandler.cs` | 59-61 | Deactivated users stay logged in with existing tokens | [X] |
| 6 | **Missing IsActive/IsLocked checks in MFA verify** | `MfaVerifyHandler.cs` | 58-65 | Locked/deactivated users can complete MFA and login | [X] |
| 7 | **Missing IsLocked check in refresh token** | `RefreshTokenHandler.cs` | 66-71 | Locked users can refresh tokens indefinitely | [X] |
| 8 | **UpdateSystemUser bypasses deactivation safeguards** | `UpdateSystemUserHandler.cs` | 51-61 | Can deactivate self or last admin via IsActive=false | [X] |
| 9 | ~~**MfaDisable missing system permission check**~~ | `MfaDisableHandler.cs` | - | ~~Users with system permissions can disable MFA~~ **VERIFIED - NOT A BUG**: Users CAN disable, but must re-enable on next login (enforced by LoginHandler:158-166) | ✅ |

#### 🟡 Medium Bugs (5) - ✅ COMPLETE

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 10 | **Wrong audit action for MFA required** | `LoginHandler.cs` | 145-153 | Logs "UserLoginFailed" for normal MFA flow - misleading audit trail | [X] |
| 11 | **Missing IsAnonymized check in forgot password** | `ForgotPasswordHandler.cs` | 45-49 | Sends password reset emails to fake anonymized addresses | [X] |
| 12 | **Hardcoded language "en" in resend invite** | `ResendInviteHandler.cs` | 104 | Resent invites always in English, ignoring invite.Language | [X] |
| 13 | **Missing force re-auth after admin MFA reset** | `ResetUserMfaHandler.cs` | 55 | User stays logged in after admin resets their MFA | [X] |
| 14 | **Missing IsAnonymized check in permission update** | `UpdateSystemUserPermissionsHandler.cs` | 39-44 | Can update permissions on anonymized users | [X] |

#### 🟢 Low Priority (5) - ✅ COMPLETE

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 15 | **Missing state checks in GetCurrentUser** | `GetCurrentUserHandler.cs` | 31-36 | Deactivated/locked user can fetch their profile | [X] |
| 16 | **Missing state checks in MfaSetup** | `MfaSetupHandler.cs` | 40-47 | Edge case: user state change between login and MFA setup | [X] |
| 17 | **Missing state checks in MfaConfirm** | `MfaConfirmHandler.cs` | 47-49 | Edge case: user state change between setup and confirm | [X] |
| 18 | **Missing state checks in RegenerateBackupCodes** | `RegenerateBackupCodesHandler.cs` | 44-46 | Edge case: locked/inactive user regenerating codes | [X] |
| 19 | **Missing state checks in UpdatePreferences** | `UpdatePreferencesHandler.cs` | 30-32 | Edge case: locked/inactive user updating preferences | [X] |

#### Bug Details & Fixes

**Bug #1: RegisterHandler Wrong Parameter Order**
```csharp
// CURRENT (WRONG):
await _deviceSessionService.CreateOrUpdateSessionAsync(
    user.Id, deviceId,
    command.DeviceFingerprint,  // ❌ Should be UserAgent
    command.UserAgent,          // ❌ Should be IpAddress
    command.IpAddress, ct);     // ❌ Should be DeviceFingerprint

// FIX:
await _deviceSessionService.CreateOrUpdateSessionAsync(
    user.Id, deviceId,
    command.UserAgent,          // ✅
    command.IpAddress,          // ✅
    command.DeviceFingerprint, ct); // ✅
```

**Bug #2: AcceptInviteHandler Missing IsRevoked**
```csharp
// ADD after line 53:
if (invite.IsRevoked)
{
    throw new AuthException("AUTH_INVITE_REVOKED", "This invitation has been revoked", 400);
}
```

**Bug #3: InviteSystemUserHandler Missing RevokedAt Filter**
```csharp
// CURRENT:
.FirstOrDefaultAsync(i => i.Email == command.Email.ToLowerInvariant()
    && i.AcceptedAt == null
    && i.ExpiresAt > DateTime.UtcNow, ct);

// FIX - ADD:
    && i.RevokedAt == null
```

**Bug #4: UnlockUserHandler Missing Redis Reset**
```csharp
// ADD after user.Unlock():
await _bruteForceService.ResetAsync(user.Email, ct);
```
Requires injecting `IBruteForceProtectionService` into handler.

**Bug #5: DeactivateSystemUserHandler Missing Session Revocation**
```csharp
// ADD after user.Deactivate():
// Revoke all sessions
var sessions = await _context.DeviceSessions.Where(s => s.UserId == command.Id).ToListAsync(ct);
foreach (var session in sessions)
    await _revokedSessionService.RevokeSessionAsync(session.Id, ct);
_context.DeviceSessions.RemoveRange(sessions);

// Revoke all refresh tokens
var tokens = await _context.RefreshTokens.Where(t => t.UserId == command.Id && !t.IsRevoked).ToListAsync(ct);
foreach (var token in tokens)
    token.Revoke();
```

**Bug #6: MfaVerifyHandler Missing State Checks**
```csharp
// ADD after fetching user:
if (!user.IsActive)
    throw new UserInactiveException();
if (user.IsLocked)
    throw new AccountLockedException(user.LockedUntil);
```

**Bug #7: RefreshTokenHandler Missing IsLocked**
```csharp
// CURRENT:
if (user is null || !user.IsActive)

// FIX:
if (user is null || !user.IsActive || user.IsLocked)
```

**Bug #8: UpdateSystemUserHandler Bypasses Safeguards**
```csharp
// ADD checks before user.Update() when IsActive is being set to false:
if (command.IsActive == false && user.IsActive)
{
    // Check self-deactivation
    if (_currentUser.UserId == command.Id)
        throw new CannotDeleteSelfException();

    // Check last permission holder
    // (copy logic from DeactivateSystemUserHandler)
}
```

**Bug #9: MfaDisableHandler Missing Permission Check - ~~INVALID~~**
> **VERIFIED NOT A BUG**: After analysis, this was determined to be secure by design:
> 1. Users must provide valid TOTP code to disable (ownership verified)
> 2. The handler only operates on the current user's own account
> 3. LoginHandler (line 158-166) enforces MFA re-setup on next login for system permission users
> 4. There's no security gap - users cannot access the system without MFA once they login again
>
> No code changes needed.

**Bug #10: LoginHandler Wrong Audit Action**
```csharp
// CHANGE from UserLoginFailed to a neutral action or new action:
// Option A: Don't log as failure
// Option B: Create new AuditActions.MfaChallengeSent
```

**Bug #12: ResendInviteHandler Hardcoded Language**
```csharp
// CURRENT:
language: "en",

// FIX:
language: invite.Language,
```

**Bug #13: ResetUserMfaHandler Missing Force Re-auth**
```csharp
// ADD after user.DisableMfa():
await _forceReauthService.SetFlagAsync(command.UserId, ct);
// And revoke all refresh tokens for this user
```

---

### Phase 9: Additional Bug Fixes (Code Review Round 2) ✅

> ⚠️ **Discovered during comprehensive code review on 2025-12-29**
>
> Second round of code review after Phase 8 fixes, cross-referencing with Tasks 002 and 005.

#### 🔴 Critical Bugs (2)

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 1 | **UpdateSystemUserHandler missing session/token revocation on deactivation** | `UpdateSystemUserHandler.cs` | 39-87 | When admin sets `IsActive=false` via PUT, user stays logged in with valid tokens - security bypass! | [x] |
| 2 | **AnonymizeUserHandler missing last permission holder check** | `AnonymizeUserHandler.cs` | 28-108 | Can anonymize last user with `system:users:update` - orphans the system, no one can manage users | [x] |

#### 🟠 Medium Bugs (3)

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 3 | **MfaDisableHandler missing IsActive/IsLocked checks** | `MfaDisableHandler.cs` | 34-96 | Deactivated or locked users can disable their MFA - all other MFA handlers have these checks | [x] |
| 4 | **ResetPasswordHandler missing IsLocked check** | `ResetPasswordHandler.cs` | 63 | Locked users can reset password and bypass lockout - only checks `!user.IsActive` | [x] |
| 5 | **RegisterHandler missing SessionId/DeviceId in response** | `RegisterHandler.cs` | 138-153 | First user doesn't receive session info in response - inconsistent with AcceptInviteHandler | [x] |

#### 🟢 Low Priority (2)

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 6 | **LogoutHandler doesn't revoke device session** | `LogoutHandler.cs` | 27-57 | Only revokes refresh token, doesn't call `_revokedSessionService` - access token valid until expiry | [x] |
| 7 | **RevokeUserSessionsHandler can email anonymized users** | `RevokeUserSessionsHandler.cs` | 84-96 | Sends email to `anonymized_xxx@deleted.local` - should check IsAnonymized first | [x] |

#### Bug Details & Fixes

**Bug #1: UpdateSystemUserHandler Missing Session/Token Revocation (CRITICAL)**

The handler has deactivation safeguards (self-deactivation, last permission holder) but does NOT revoke sessions/tokens like `DeactivateSystemUserHandler` does.

```csharp
// CURRENT (UpdateSystemUserHandler.cs:80-87):
user.Update(firstName, lastName, isActive);
await _userRepository.UpdateAsync(user, ct);
// ❌ NO session revocation!
// ❌ NO token revocation!
// ❌ NO permission cache invalidation!

// FIX - ADD after user.Update() when IsActive changes to false:
if (command.IsActive == false && user.IsActive)
{
    // Revoke all sessions for immediate logout
    var sessions = await _context.DeviceSessions
        .Where(s => s.UserId == command.Id)
        .ToListAsync(ct);
    foreach (var session in sessions)
        await _revokedSessionService.RevokeSessionAsync(session.Id, ct);
    _context.DeviceSessions.RemoveRange(sessions);

    // Revoke all refresh tokens
    var refreshTokens = await _context.RefreshTokens
        .Where(t => t.UserId == command.Id && !t.IsRevoked)
        .ToListAsync(ct);
    foreach (var token in refreshTokens)
        token.Revoke();

    // Invalidate permission cache
    await _permissionCache.InvalidateAsync(command.Id, ct);
}
```

Requires injecting: `IAppDbContext`, `IRevokedSessionService`, `IPermissionCacheService`

**Bug #2: AnonymizeUserHandler Missing Last Permission Holder Check (CRITICAL)**

```csharp
// CURRENT (AnonymizeUserHandler.cs:87-89):
var permissions = user.Permissions.ToList();
_context.SystemUserPermissions.RemoveRange(permissions);
// ❌ No check if user is last holder of critical permissions!

// FIX - ADD before removing permissions:
var userPermissionNames = user.Permissions.Select(p => p.Permission.Name).ToList();
if (userPermissionNames.Contains(SystemPermissions.UsersUpdate))
{
    var holdersCount = await _context.SystemUserPermissions
        .Include(p => p.Permission)
        .Where(p => p.Permission.Name == SystemPermissions.UsersUpdate && p.User.IsActive && !p.User.IsAnonymized)
        .Select(p => p.UserId)
        .Distinct()
        .CountAsync(ct);

    if (holdersCount <= 1)
        throw new LastPermissionHolderException(SystemPermissions.UsersUpdate);
}
```

**Bug #3: MfaDisableHandler Missing State Checks**

```csharp
// CURRENT (MfaDisableHandler.cs:39-41):
var user = await _context.SystemUsers
    .FirstOrDefaultAsync(u => u.Id == userId, ct)
    ?? throw new UnauthorizedException();
// ❌ No IsActive/IsLocked checks!

// FIX - ADD after fetching user:
if (!user.IsActive)
    throw new UserInactiveException();

if (user.IsLocked)
    throw new AccountLockedException(user.LockedUntil);
```

**Bug #4: ResetPasswordHandler Missing IsLocked Check**

```csharp
// CURRENT (ResetPasswordHandler.cs:63):
if (user is null || !user.IsActive)
// ❌ Missing IsLocked check!

// FIX:
if (user is null || !user.IsActive || user.IsLocked)
```

**Bug #5: RegisterHandler Missing SessionId/DeviceId in Response**

```csharp
// CURRENT (RegisterHandler.cs:138-153):
return new AuthResponse(
    User: new UserDto(...),
    AccessToken: accessToken,
    RefreshToken: refreshTokenString
);
// ❌ Missing SessionId and DeviceId!

// FIX:
return new AuthResponse(
    User: new UserDto(...),
    AccessToken: accessToken,
    RefreshToken: refreshTokenString,
    SessionId: session.Id,      // ✅ ADD
    DeviceId: deviceId          // ✅ ADD
);
```

**Bug #6: LogoutHandler Missing Session Revocation**

```csharp
// CURRENT (LogoutHandler.cs:36-41):
if (storedToken is not null)
{
    storedToken.Revoke();
    await _tokenBlacklist.BlacklistAsync(storedToken.Id, storedToken.ExpiresAt, ct);
    // ❌ Device session not revoked - access token still valid!
}

// FIX - ADD after revoking refresh token:
if (storedToken.DeviceSessionId.HasValue)
{
    await _revokedSessionService.RevokeSessionAsync(storedToken.DeviceSessionId.Value, ct);

    var session = await _context.DeviceSessions
        .FirstOrDefaultAsync(s => s.Id == storedToken.DeviceSessionId.Value, ct);
    if (session is not null)
        _context.DeviceSessions.Remove(session);
}
```

Requires injecting: `IRevokedSessionService`

**Bug #7: RevokeUserSessionsHandler Can Email Anonymized Users**

```csharp
// CURRENT (RevokeUserSessionsHandler.cs:36-38):
var user = await _context.SystemUsers
    .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
    ?? throw new SystemUserNotFoundException(command.UserId);
// Then sends email at line 85-96 without checking IsAnonymized

// FIX - ADD check before sending email:
// Send notification email to user (only if not anonymized)
if (!user.IsAnonymized)
{
    await _emailService.SendAsync(
        user.Email,
        "All Your Sessions Have Been Revoked",
        ...
    );
}
```

#### Cross-Reference with Other Tasks

| Task | Related Issue | Status |
|------|---------------|--------|
| Task 002 | Force Re-Auth marked as "pending" - implemented in Task 005/007 | ✅ Implemented |
| Task 002 | Error Audit Logging marked as "pending" - implemented | ✅ Implemented |
| Task 005 | Invitation management - working correctly | ✅ Verified |
| Task 005 | `@deleted.local` domain blocked - working correctly | ✅ Verified |

---

### Phase 10: MFA Forced Setup Flow Fix ✅

> ⚠️ **Fixed on 2025-12-29**
>
> The MFA forced setup flow during login/registration was incomplete. Users with system permissions were getting `mfaSetupRequired: true` with a `setupToken`, but couldn't complete the setup because `/mfa/setup` required JWT authentication.

#### Problem

The existing implementation had a gap:
1. **Login/Register** → Returns `{ mfaSetupRequired: true, setupToken: "..." }` for system users
2. **POST /mfa/setup** → Required `[Authorize]` (JWT)
3. **Problem**: User only has `setupToken`, not a JWT → 401 Unauthorized!

Similarly, first user registration was granting full access without MFA, creating a security gap.

#### Solution: Dual-Mode Authentication

Modified `/mfa/setup` and `/mfa/confirm` to support two authentication modes:

| Scenario | Authentication Method | Returns |
|----------|----------------------|---------|
| User already logged in (settings page) | JWT in Authorization header | Backup codes only |
| Forced setup during login/registration | `setupToken` in request body | Backup codes + tokens |

#### Files Modified

| File | Changes |
|------|---------|
| `MfaSetupCommand.cs` | Added optional `SetupToken` parameter |
| `MfaSetupHandler.cs` | Dual-mode auth: JWT OR setupToken |
| `MfaConfirmCommand.cs` | Added `SetupToken` + device info parameters |
| `MfaConfirmHandler.cs` | Dual-mode auth + returns tokens for forced setup flow |
| `MfaModels.cs` | Extended `MfaConfirmResponse` with auth fields (User, AccessToken, RefreshToken, SessionId, DeviceId) |
| `RegisterHandler.cs` | First user now returns `mfaSetupRequired: true` instead of tokens |
| `AuthController.cs` | Removed `[Authorize]` from setup/confirm, added request models, set cookies on confirm |
| `IAuditService.cs` | Added `MfaSetupCompleted` audit action |
| `RegisterHandlerTests.cs` | Updated tests for new MFA-required flow |

#### API Changes

**POST /api/auth/mfa/setup**
```json
// Before: No body, JWT required
// After: Optional body, JWT OR setupToken
{
  "setupToken": "eyJhbG..."  // Optional - for forced setup flow
}
```

**POST /api/auth/mfa/confirm**
```json
// Before: JWT required
{
  "code": "123456"
}

// After: JWT OR setupToken
{
  "code": "123456",
  "setupToken": "eyJhbG...",    // Optional - for forced setup flow
  "deviceId": "...",            // Optional - for session creation
  "deviceFingerprint": "..."    // Optional - for session creation
}

// Response when using setupToken (forced setup flow):
{
  "success": true,
  "backupCodes": ["XXXX-XXXX", ...],
  "user": { ... },              // ✅ NEW - User data
  "accessToken": "...",         // ✅ NEW - JWT
  "refreshToken": "...",        // ✅ NEW - Refresh token
  "sessionId": "...",           // ✅ NEW - Session ID
  "deviceId": "..."             // ✅ NEW - Device ID
}
```

**POST /api/auth/register** (first user)
```json
// Before: Returns tokens immediately
{
  "user": { ... },
  "accessToken": "...",
  "refreshToken": "..."
}

// After: Requires MFA setup first
{
  "mfaSetupRequired": true,
  "setupToken": "...",
  "user": null,
  "accessToken": null,
  "refreshToken": null
}
```

#### Security Guarantees

- `setupToken` is JWT-signed (can't be forged)
- 5-minute expiry
- Purpose-locked (`"purpose": "mfa_verification"`)
- Only issued after successful password verification
- Limited scope - only works for MFA setup/confirm

#### Tests

All 233 unit tests pass ✅

---

### Phase 11: Bug Fixes (Code Review Round 3) ✅

> ⚠️ **Discovered during testing on 2025-12-29**
>
> Runtime errors and missing data fields discovered during manual testing.

#### 🔴 Critical Bugs (1)

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 1 | **AuthController.Login calls SetAuthCookies with null tokens during MFA flow** | `AuthController.cs` | 89 | When MFA is required (MfaRequired or MfaSetupRequired), AccessToken and RefreshToken are null but SetAuthCookies is called unconditionally → `System.ArgumentNullException: Value cannot be null. (Parameter 'stringToEscape')` | [x] |

#### 🟠 Medium Bugs (1)

| # | Bug | File | Line | Impact | Status |
|---|-----|------|------|--------|--------|
| 2 | **UserDto missing MfaEnabled and PreferredLanguage fields** | `AuthResponse.cs` | 66-77 | `/api/auth/me` endpoint doesn't return user's MFA status or language preference - frontend can't display these settings | [x] |

#### Bug Details & Fixes

**Bug #1: AuthController.Login SetAuthCookies with null tokens (CRITICAL)**

```csharp
// CURRENT (AuthController.cs:89):
var result = await Mediator.Send(command, ct);
SetAuthCookies(result.AccessToken, result.RefreshToken);  // ❌ Crashes when MFA required!
return ApiOk(result);

// FIX:
var result = await Mediator.Send(command, ct);

// Only set cookies when login is complete (not during MFA flow)
if (!result.MfaRequired && !result.MfaSetupRequired)
{
    SetAuthCookies(result.AccessToken!, result.RefreshToken!);
}

return ApiOk(result);
```

**Bug #2: UserDto missing MfaEnabled and PreferredLanguage**

```csharp
// CURRENT (AuthResponse.cs:66-77):
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Permissions
);  // ❌ Missing MfaEnabled and PreferredLanguage!

// FIX - Added fields:
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    bool MfaEnabled,              // ✅ Added
    string PreferredLanguage,     // ✅ Added
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Permissions
);
```

**Files Updated:**
- `AuthController.cs` - Line 89-93: Added MFA flow check before SetAuthCookies
- `AuthResponse.cs` - Lines 66-79: Added MfaEnabled and PreferredLanguage to UserDto
- `GetCurrentUserHandler.cs` - Lines 54-67: Added new fields to UserDto construction
- `LoginHandler.cs` - Lines 275-288: Added new fields to UserDto construction
- `MfaVerifyHandler.cs` - Lines 211-224: Added new fields to UserDto construction
- `RegisterHandler.cs` - Lines 139-152: Added new fields to UserDto construction
- `AcceptInviteHandler.cs` - Lines 155-168: Added new fields to UserDto construction

**Tests:** All 234 unit tests pass ✅

---

### Phase 12: Infrastructure & API Improvements ✅

> ⚠️ **Fixed on 2025-12-29**
>
> Several infrastructure improvements and missing features discovered during testing.

#### Issues Fixed

| # | Issue | Impact | Status |
|---|-------|--------|--------|
| 1 | Data Protection keys not persisted in Docker | MFA secrets unreadable after container restart | ✅ |
| 2 | MfaVerifyHandler missing new device/location emails | Users with MFA didn't get notified about new devices | ✅ |
| 3 | RegisterRequest missing Language field | Users couldn't set language preference during registration | ✅ |

#### Fix #1: Data Protection Key Persistence

**Problem**: ASP.NET Core Data Protection keys were stored in memory. When Docker container restarts, keys are lost and encrypted MFA secrets become unreadable.

**Error**: `CryptographicException: The key {guid} was not found in the key ring`

**Solution**:

1. **Program.cs** - Persist keys to file system:
```csharp
var keysDirectory = builder.Configuration["DataProtection:KeysPath"] ?? "/app/data/keys";
builder.Services.AddDataProtection()
    .SetApplicationName("ExoAuth")
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
```

2. **docker-compose.yml** - Added volume:
```yaml
volumes:
  - dataprotection-keys:/app/data/keys
```

3. **Dockerfile** - Created directory:
```dockerfile
RUN mkdir -p /app/data/keys /app/data/geoip
```

**Files Modified:**
- `Program.cs` - Added Data Protection configuration with file system persistence
- `docker-compose.yml` - Added `dataprotection-keys` volume
- `docker/api/Dockerfile` - Added `mkdir` for data directories

#### Fix #2: MfaVerifyHandler Missing Email Notifications

**Problem**: When a user with MFA enabled logs in from a new device, they didn't receive email notification. The `LoginHandler` has this logic but `MfaVerifyHandler` (where login completes for MFA users) was missing it.

**Solution**: Added same email notification logic from `LoginHandler` to `MfaVerifyHandler`:
- Added `IConfiguration` dependency
- Added `SendNewDeviceEmailAsync()` method
- Added `SendNewLocationEmailAsync()` method
- Added audit logging for `LoginNewDevice` and `LoginNewLocation`

**Files Modified:**
- `MfaVerifyHandler.cs` - Added email notifications for new device/location

#### Fix #3: Language Field in Registration

**Problem**: `RegisterRequest` DTO was missing `Language` field, so users couldn't set their preferred language during registration.

**Solution**: Added `Language` field to request DTO and pass it to command.

**API Change:**
```json
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "Max",
  "lastName": "Mustermann",
  "language": "de"  // NEW - defaults to "en"
}
```

**Files Modified:**
- `AuthController.cs` - Added `Language` to `RegisterRequest`, passed to command

**Tests:** All 233 unit tests pass ✅

---

### Phase 13: Language Code Standardization ✅

> ⚠️ **Implemented on 2025-12-29**
>
> Changed language codes from short format (`en`, `de`) to full locale format (`en-US`, `de-DE`) throughout the system for better internationalization support.

#### Changes Made

| Category | Changes |
|----------|---------|
| **Template Folders** | Renamed `templates/emails/en/` → `en-US/`, `de/` → `de-DE/` |
| **Default Language** | All defaults changed from `"en"` to `"en-US"` |
| **Language Checks** | Updated from `== "de"` to `.StartsWith("de")` for flexibility |
| **Email Subjects** | Localized all hard-coded English email subjects |

#### Files Modified

**Backend - Commands & DTOs:**
- `RegisterCommand.cs` - Default `Language = "en-US"`
- `AcceptInviteCommand.cs` - Default `Language = "en-US"`
- `InviteSystemUserCommand.cs` - Default `Language = "en-US"`
- `AuthController.cs` - `RegisterRequest` and `AcceptInviteRequest` defaults

**Backend - Entities:**
- `SystemUser.cs` - Default `PreferredLanguage = "en-US"`
- `SystemInvite.cs` - Default `Language = "en-US"` (property and Create method)

**Backend - Services:**
- `IEmailService.cs` - All method defaults to `"en-US"`
- `IEmailTemplateService.cs` - Default parameter `"en-US"`
- `EmailService.cs` - Defaults + subject checks use `.StartsWith("de")`
- `EmailTemplateService.cs` (Infrastructure) - Default + fallback to `"en-US"`
- `EmailTemplateService.cs` (EmailWorker) - Default + fallback to `"en-US"`

**Backend - Handlers (Email Subject Localization):**
- `MfaConfirmHandler.cs` - "Zwei-Faktor-Authentifizierung aktiviert"
- `MfaDisableHandler.cs` - "Zwei-Faktor-Authentifizierung deaktiviert"
- `MfaVerifyHandler.cs` - "Backup-Code verwendet"
- `LoginHandler.cs` - Subject checks use `.StartsWith("de")`
- `ResetUserMfaHandler.cs` - "Ihre Zwei-Faktor-Authentifizierung wurde zurückgesetzt"
- `UnlockUserHandler.cs` - "Ihr Konto wurde entsperrt"
- `RevokeUserSessionsHandler.cs` - "Alle Ihre Sitzungen wurden widerrufen"

**Backend - Tests:**
- `UpdatePreferencesHandlerTests.cs` - Updated to use `"en-US"` / `"de-DE"`
- `ResendInviteHandlerTests.cs` - Updated mock verification to `"en-US"`
- `EmailTemplateServiceTests.cs` - Updated all folder paths and language codes

#### API Impact

Registration and invite acceptance now default to `"en-US"`:
```json
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "Max",
  "lastName": "Mustermann",
  "language": "de-DE"  // Full locale format
}
```

#### Migration Notes

- Existing users with `language = "en"` will get English emails (fallback works)
- Existing users with `language = "de"` will get German emails (`.StartsWith("de")` works)
- New users will have full locale codes stored
- No database migration needed (existing short codes still work)

**Tests:** All 233 unit tests pass ✅

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
| Immediate Access Token Invalidation? | ✅ Ja, via Redis (session_id in JWT + RevokedSessionService) |
| First Device Auto-Trust? | ✅ Ja, erstes Gerät bei Registration wird automatisch trusted (Baseline) |

### Architektur-Entscheidung: Immediate Session Invalidation

Wenn ein User eine Session widerruft, soll das sofort wirksam sein - nicht erst nach 15 Minuten (Access Token Expiry).

**Lösung:**
1. `session_id` Claim wird in jeden JWT Access Token eingebettet
2. `IRevokedSessionService` cached revoked Session IDs in Redis (TTL = Access Token Lifetime + 1 min)
3. `ForceReauthMiddleware` prüft bei jedem Request ob die Session revoked ist
4. Bei Revoke: Session in Redis markieren → sofortige 401 Unauthorized Response

**Flow:**
```
User revokes session → RevokedSessionService.RevokeSessionAsync(sessionId)
                     → Redis: SET revoked_session:{id} "1" EX 960

Other session makes request → ForceReauthMiddleware.InvokeAsync()
                            → RevokedSessionService.IsSessionRevokedAsync(sessionId)
                            → Redis: EXISTS revoked_session:{id}
                            → 401 Unauthorized mit ErrorCodes.SessionRevoked
```

### Architektur-Entscheidung: First Device Auto-Trust

Das erste Gerät bei der Registration wird automatisch als "trusted" markiert und dient als Baseline für zukünftige Logins.

**Flow:**
```
┌─────────────────────────────────────────────────────────────────┐
│                         REGISTER                                 │
├─────────────────────────────────────────────────────────────────┤
│ User registriert sich                                            │
│   → Erste DeviceSession wird erstellt                            │
│   → IsTrusted = TRUE (auto-trusted als Baseline)                │
│   → Location wird als Baseline gespeichert                       │
│   → Kein Alert-Email (es ist Registration)                      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    LOGIN (Gleiches Gerät)                       │
├─────────────────────────────────────────────────────────────────┤
│ Gleiche deviceId gefunden                                        │
│   → Session aktualisiert, Aktivität aufgezeichnet               │
│   → IsNewDevice = false                                          │
│   → IsNewLocation = prüft ob Country/City geändert              │
│   → Bei neuem Standort → Alert Email                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    LOGIN (Neues Gerät)                          │
├─────────────────────────────────────────────────────────────────┤
│ Neue deviceId                                                    │
│   → Neue DeviceSession erstellt                                  │
│   → IsTrusted = FALSE (User kann manuell vertrauen)             │
│   → IsNewDevice = true                                           │
│   → Alert Email gesendet                                        │
└─────────────────────────────────────────────────────────────────┘
```

**Implementierung:** `DeviceSessionService.CreateOrUpdateSessionAsync()` prüft ob der User bereits Sessions hat. Falls nicht (erste Session), wird `session.Trust()` aufgerufen.

### Architektur-Entscheidung: GUID Collision Prevention

Obwohl GUID-Kollisionen extrem unwahrscheinlich sind (1 in 2.71 Quintillionen), implementieren wir Defense-in-Depth:

**Lösung:**
1. `BaseEntity.RegenerateId()` Methode für ID-Neugenerierung
2. Pre-Check vor Insert: Prüfung ob ID bereits existiert
3. Bis zu 3 Retry-Versuche bei Kollision
4. `@deleted.local` Domain blockiert (reserviert für anonymisierte User)

**Flow:**
```
Entity erstellt → ID = Guid.NewGuid()
                → Check: SELECT COUNT(*) WHERE Id = {id}
                → Falls existiert: entity.RegenerateId(), retry
                → Nach 3 Versuchen: InvalidOperationException
```

**Betroffene Handler:**
- `RegisterHandler` (SystemUser)
- `AcceptInviteHandler` (SystemUser)
- `InviteSystemUserHandler` (SystemInvite)

### Architektur-Entscheidung: Anonymized User Protection

Anonymisierte User dürfen nicht reaktiviert oder modifiziert werden (GDPR/Datenschutz).

**Checks:**
- `ActivateSystemUserHandler`: Wirft `UserAnonymizedException` wenn `IsAnonymized = true`
- `UpdateSystemUserHandler`: Wirft `UserAnonymizedException` wenn `IsAnonymized = true`
- `RegisterValidator` & `InviteValidator`: Blockieren `@deleted.local` Domain

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

---

## 18. Future Enhancements: Intelligent Device Trust

> 🚀 **Potential Feature**: Smart device/location approval based on risk scoring
>
> Currently MFA is the only trust mechanism. Users get notified about new devices but login proceeds immediately. This section documents potential enhancements for risk-based device approval.

### Current State

```
New Device Login → MFA Code Correct → ✅ Login Success
                                    → 📧 Email Notification (informational only)
```

- MFA is the trust mechanism
- Email notifications are after-the-fact (login already complete)
- User can only react by revoking session manually

### Proposed Enhancement: Risk-Based Device Approval

#### Risk Scoring System

| Factor | Risk Score | Example |
|--------|------------|---------|
| New Device | +20 | deviceId not seen before |
| New Country | +40 | User normally logs in from Germany, now from Russia |
| New City (same country) | +10 | User normally in Berlin, now in Munich |
| Impossible Travel | +80 | Logged in from Germany 5 min ago, now from USA |
| Known VPN/Proxy IP | +30 | IP detected as datacenter/VPN |
| Unusual Time | +15 | Login at 3am when user typically logs in 9-18h |
| Tor Exit Node | +50 | IP is known Tor exit node |
| Different Device Type | +10 | Usually mobile, now desktop |

**Risk Thresholds:**
- **0-30**: Low risk → Login proceeds, email notification
- **31-60**: Medium risk → Require additional verification (email OTP)
- **61-100+**: High risk → Block login, require email approval link

#### Flow: Medium Risk (Additional Verification)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MEDIUM RISK LOGIN (31-60 points)                      │
└─────────────────────────────────────────────────────────────────────────┘

    NEW DEVICE                          SERVER
        │                                  │
        │  POST /auth/login                │
        │  (new device, new city)          │
        │─────────────────────────────────>│
        │                                  │
        │                                  │  Calculate risk score: 30
        │                                  │  (new device + new city)
        │                                  │
        │                                  │  MFA check...
        │                                  │  ✓ TOTP valid
        │                                  │
        │                                  │  Risk > 30 → Additional
        │                                  │  verification required
        │                                  │
        │  200 OK                          │
        │  {                               │
        │    additionalVerification: true, │
        │    verificationToken: "...",     │
        │    method: "email_otp"           │
        │  }                               │
        │                                  │
        │                                  │  📧 Send 6-digit OTP to email
        │                                  │
        │  POST /auth/verify-device        │
        │  { verificationToken, otp }      │
        │─────────────────────────────────>│
        │                                  │
        │  200 OK { accessToken, ... }  ◄──│
        │                                  │
    ✅ LOGGED IN                           │
```

#### Flow: High Risk (Approval Required)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    HIGH RISK LOGIN (61+ points)                          │
└─────────────────────────────────────────────────────────────────────────┘

    SUSPICIOUS DEVICE                   SERVER                         USER'S EMAIL
        │                                  │                              │
        │  POST /auth/login                │                              │
        │  (impossible travel detected)    │                              │
        │─────────────────────────────────>│                              │
        │                                  │                              │
        │                                  │  Calculate risk: 100         │
        │                                  │  (impossible travel!)        │
        │                                  │                              │
        │                                  │  MFA check...                │
        │                                  │  ✓ TOTP valid (but...)       │
        │                                  │                              │
        │                                  │  Risk > 60 → BLOCK           │
        │                                  │                              │
        │  200 OK                          │                              │
        │  {                               │                              │
        │    loginBlocked: true,           │                              │
        │    reason: "suspicious_activity",│                              │
        │    message: "Approval required"  │                              │
        │  }                               │                              │
        │                                  │                              │
        │                                  │  📧 Send approval email ────>│
        │                                  │     "Someone is trying to    │
        │                                  │      log in from [location]" │
        │                                  │     [Approve] [Block & Alert]│
        │                                  │                              │
        │                                  │  ◄─── User clicks [Approve] ─│
        │                                  │                              │
        │  (User must retry login)         │                              │
        │  POST /auth/login                │                              │
        │─────────────────────────────────>│                              │
        │                                  │                              │
        │  200 OK { accessToken, ... }  ◄──│  Device now approved         │
        │                                  │                              │
    ✅ LOGGED IN                           │
```

#### New API Endpoints (Proposed)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/verify-device` | Submit email OTP for medium-risk login |
| GET | `/api/auth/approve-device/{token}` | Email link to approve blocked login |
| POST | `/api/auth/block-device/{token}` | Email link to block device + alert security |

#### New Database Tables (Proposed)

```sql
-- Track device approval attempts
CREATE TABLE device_approval_requests (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES system_users(id),
    device_id VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45),
    country VARCHAR(100),
    city VARCHAR(100),
    risk_score INT NOT NULL,
    risk_factors JSONB,  -- { "new_device": true, "impossible_travel": true, ... }
    status VARCHAR(20) NOT NULL,  -- pending, approved, blocked, expired
    approval_token_hash VARCHAR(64),
    otp_hash VARCHAR(64),
    otp_attempts INT DEFAULT 0,
    created_at TIMESTAMP NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    resolved_at TIMESTAMP,
    resolved_by VARCHAR(20)  -- user, admin, timeout
);

-- Track historical login patterns for risk calculation
CREATE TABLE login_patterns (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES system_users(id),
    typical_countries VARCHAR(100)[],
    typical_cities VARCHAR(100)[],
    typical_hours INT[],  -- [9, 10, 11, ..., 17, 18] for 9-18h
    typical_device_types VARCHAR(50)[],
    last_updated TIMESTAMP NOT NULL
);
```

#### Configuration (Proposed)

```json
{
  "DeviceTrust": {
    "Enabled": false,  // Feature flag
    "RiskThresholds": {
      "Medium": 31,
      "High": 61
    },
    "RiskScores": {
      "NewDevice": 20,
      "NewCountry": 40,
      "NewCity": 10,
      "ImpossibleTravel": 80,
      "VpnProxy": 30,
      "UnusualTime": 15,
      "TorExitNode": 50,
      "DifferentDeviceType": 10
    },
    "ImpossibleTravelSpeedKmh": 800,  // Faster than commercial flight
    "OtpExpiryMinutes": 10,
    "ApprovalExpiryMinutes": 30,
    "MaxOtpAttempts": 3
  }
}
```

#### Implementation Considerations

1. **GeoIP Database Required**: Need accurate IP → location mapping (already have MaxMind)
2. **VPN/Proxy Detection**: Could use IP reputation services (IPQualityScore, MaxMind minFraud)
3. **Historical Data**: Need to track user's typical login patterns over time
4. **False Positives**: Legitimate users traveling should not be locked out
5. **User Experience**: Balance security vs. friction

#### Priority Assessment

| Feature | Complexity | Security Value | Recommendation |
|---------|------------|----------------|----------------|
| Impossible Travel Detection | Medium | High | ✅ Implement first |
| Email OTP for medium risk | Low | Medium | ✅ Quick win |
| Approval Links for high risk | Medium | High | ✅ Implement |
| VPN/Proxy Detection | High | Medium | ⏸️ Later (needs external service) |
| Unusual Time Detection | Low | Low | ⏸️ Nice to have |
| Tor Detection | Medium | Medium | ⏸️ Later |

#### Status

**Current**: Not implemented - MFA is sole trust mechanism
**Next Steps**: Discuss with team if this level of security is needed for the use case

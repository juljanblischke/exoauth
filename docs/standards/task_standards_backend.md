# Backend Task Standards - ExoAuth

> **MEGA BRAIN** - Lies diese Datei KOMPLETT bevor du einen Task erstellst.

---

## Wichtige Regeln für Task Files

### Task File Pflege (KRITISCH!)

**Während der Implementierung MUSS das Task File IMMER aktuell gehalten werden:**

1. **Neue Files erstellt?** → Sofort in Section "Files zu erstellen" eintragen mit vollem Pfad
2. **Bestehende Files geändert?** → Sofort in Section "Files zu ändern" eintragen
3. **Neue Packages installiert?** → Sofort in Section "Neue Packages" eintragen
4. **Implementierung abgeschlossen?** → Checkbox abhaken `[x]`
5. **Tests geschrieben?** → Mit Anzahl dokumentieren (z.B. "106 Tests ✅")
6. **Helper/Utilities erstellt?** → Auch dokumentieren!

**Warum?**
- Das Task File ist die **Single Source of Truth**
- Standards Update am Ende wird einfacher (File Tree aktualisieren)
- Code Review wird einfacher
- Andere (und du selbst später) müssen wissen was geändert wurde

---

## Task Vorlage

Wenn ein neues Feature geplant wird, MUSS dieser Template verwendet werden:

```markdown
# Task: [Feature Name]

## 1. Übersicht
**Was wird gebaut?**
[Kurze Beschreibung]

**Warum?**
[Business Grund / User Need]

## 2. User Experience / Anforderungen

### User Stories
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]

### Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2
- [ ] Kriterium 3

### Edge Cases / Error Handling
- Was passiert wenn...?
- Was passiert wenn...?

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| POST | /api/... | `{ ... }` | `{ ... }` | ... |
| GET | /api/... | - | `{ ... }` | ... |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `ERROR_CODE_NAME` | 4xx | Beschreibung des Fehlers |

> ⚠️ **Nach Completion:** Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities
| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| ... | ... | ... |

### Migrations
- [ ] Migration Name: `Add{EntityName}`

## 6. Files zu erstellen

### Domain Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Domain/Entities/...` | ... |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/.../Commands/.../...Command.cs` | ... |
| Handler | `src/ExoAuth.Application/Features/.../Commands/.../...Handler.cs` | ... |
| Validator | `src/ExoAuth.Application/Features/.../Commands/.../...Validator.cs` | ... |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Infrastructure/...` | ... |

### API Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Controller | `src/ExoAuth.Api/Controllers/...Controller.cs` | ... |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/.../AppDbContext.cs` | DbSet hinzufügen |
| `src/.../DependencyInjection.cs` | Service registrieren |

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| ... | ... | ExoAuth.XXX | ... |

## 9. Implementation Reihenfolge

1. [ ] **Domain**: Entity erstellen
2. [ ] **Infrastructure**: Configuration + DbContext + Migration
3. [ ] **Application**: Commands/Queries + Handlers + Validators
4. [ ] **API**: Controller + Endpoints
5. [ ] **Tests**: Unit Tests schreiben
6. [ ] **Standards updaten**: task_standards_backend.md + coding_standards_backend.md aktualisieren

## 10. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Features/.../...Tests.cs` | ... | ... |
| `tests/ExoAuth.UnitTests/Services/.../...Tests.cs` | ... | ... |

### Test Helpers (falls erstellt)
| Datei | Beschreibung |
|-------|--------------|
| `tests/ExoAuth.UnitTests/Helpers/...` | ... |

## 11. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `task_standards_backend.md` aktualisiert (File Tree, Packages)
- [ ] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [ ] Code reviewed
```

---

## Aktueller Projekt Stand

### File Tree (Was existiert)

```
backend/
├── ExoAuth.sln
│
├── src/
│   ├── ExoAuth.Domain/
│   │   ├── ExoAuth.Domain.csproj
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs                    ✅
│   │   │   ├── SystemUser.cs                    ✅ (Task 007: MFA, Preferences, Lockout, Anonymization)
│   │   │   ├── SystemPermission.cs              ✅
│   │   │   ├── SystemUserPermission.cs          ✅
│   │   │   ├── SystemAuditLog.cs                ✅
│   │   │   ├── SystemInvite.cs                  ✅
│   │   │   ├── RefreshToken.cs                  ✅ (Task 007: DeviceSessionId, RememberMe)
│   │   │   ├── PasswordResetToken.cs            ✅ (Task 007)
│   │   │   ├── DeviceSession.cs                 ✅ (Task 007)
│   │   │   └── MfaBackupCode.cs                 ✅ (Task 007)
│   │   ├── Enums/
│   │   │   └── UserType.cs                      ✅
│   │   ├── Constants/
│   │   │   └── SystemPermissions.cs             ✅ (Task 007: MFA, Unlock, Sessions, Deactivate, Activate, Anonymize)
│   │   ├── ValueObjects/                        [LEER]
│   │   ├── Exceptions/                          [LEER]
│   │   └── Events/                              [LEER]
│   │
│   ├── ExoAuth.Application/
│   │   ├── ExoAuth.Application.csproj
│   │   ├── DependencyInjection.cs               ✅
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICacheService.cs             ✅
│   │   │   │   ├── IMessageBus.cs               ✅
│   │   │   │   ├── ICurrentUserService.cs       ✅ (Task 007: SessionId)
│   │   │   │   ├── IDateTimeProvider.cs         ✅
│   │   │   │   ├── IAppDbContext.cs             ✅ (Task 007: DeviceSessions, MfaBackupCodes DbSets)
│   │   │   │   ├── IPasswordHasher.cs           ✅
│   │   │   │   ├── ITokenService.cs             ✅ (Task 007: sessionId in GenerateAccessToken)
│   │   │   │   ├── IAuditService.cs             ✅ (Task 007: MFA, Session audit actions)
│   │   │   │   ├── IEmailService.cs             ✅
│   │   │   │   ├── ISystemUserRepository.cs     ✅
│   │   │   │   ├── IPermissionCacheService.cs   ✅
│   │   │   │   ├── IBruteForceProtectionService.cs ✅ (Task 007: Progressive Lockout)
│   │   │   │   ├── ITokenBlacklistService.cs    ✅
│   │   │   │   ├── IEmailTemplateService.cs     ✅
│   │   │   │   ├── IForceReauthService.cs       ✅
│   │   │   │   ├── IPasswordResetService.cs     ✅ (Task 007)
│   │   │   │   ├── ISystemInviteService.cs      ✅ (Task 007)
│   │   │   │   ├── IDeviceSessionService.cs     ✅ (Task 007)
│   │   │   │   ├── IDeviceDetectionService.cs   ✅ (Task 007)
│   │   │   │   ├── IGeoIpService.cs             ✅ (Task 007)
│   │   │   │   ├── IRevokedSessionService.cs    ✅ (Task 007)
│   │   │   │   ├── IMfaService.cs               ✅ (Task 007)
│   │   │   │   ├── IEncryptionService.cs        ✅ (Task 007)
│   │   │   │   ├── IBackupCodeService.cs        ✅ (Task 007)
│   │   │   │   └── IInviteCleanupService.cs     ✅ (Task 009)
│   │   │   ├── Behaviors/
│   │   │   │   └── ValidationBehavior.cs        ✅
│   │   │   ├── Messages/
│   │   │   │   └── EmailMessage.cs              ✅
│   │   │   ├── Mappings/                        [LEER]
│   │   │   └── Models/
│   │   │       ├── ApiResponse.cs               ✅
│   │   │       ├── ApiError.cs                  ✅
│   │   │       ├── ErrorCodes.cs                ✅ (Task 007: MFA, Session, Account error codes)
│   │   │       ├── PaginationMeta.cs            ✅
│   │   │       ├── CursorPagedList.cs           ✅
│   │   │       ├── GeoLocation.cs               ✅ (Task 007)
│   │   │       └── DeviceInfo.cs                ✅ (Task 007)
│   │   └── Features/
│   │       ├── Auth/
│   │       │   ├── Commands/
│   │       │   │   ├── Register/
│   │       │   │   │   ├── RegisterCommand.cs       ✅ (Task 007: Device fields)
│   │       │   │   │   ├── RegisterHandler.cs       ✅ (Task 007: Device sessions, MFA flow)
│   │       │   │   │   └── RegisterValidator.cs     ✅ (Task 007: @deleted.local blocked)
│   │       │   │   ├── Login/
│   │       │   │   │   ├── LoginCommand.cs          ✅ (Task 007: Device fields, RememberMe)
│   │       │   │   │   ├── LoginHandler.cs          ✅ (Task 007: MFA, Device sessions, Lockout)
│   │       │   │   │   └── LoginValidator.cs        ✅
│   │       │   │   ├── RefreshToken/
│   │       │   │   │   ├── RefreshTokenCommand.cs   ✅ (Task 007: IpAddress)
│   │       │   │   │   └── RefreshTokenHandler.cs   ✅ (Task 007: Session activity, IsLocked check)
│   │       │   │   ├── Logout/
│   │       │   │   │   ├── LogoutCommand.cs         ✅
│   │       │   │   │   └── LogoutHandler.cs         ✅ (Task 007: Session revocation)
│   │       │   │   ├── AcceptInvite/
│   │       │   │   │   ├── AcceptInviteCommand.cs   ✅ (Task 007: Device fields, Language)
│   │       │   │   │   ├── AcceptInviteHandler.cs   ✅ (Task 007: Device sessions, IsRevoked check)
│   │       │   │   │   └── AcceptInviteValidator.cs ✅
│   │       │   │   ├── ForgotPassword/              ✅ (Task 007)
│   │       │   │   │   ├── ForgotPasswordCommand.cs     ✅
│   │       │   │   │   ├── ForgotPasswordHandler.cs     ✅
│   │       │   │   │   └── ForgotPasswordValidator.cs   ✅
│   │       │   │   ├── ResetPassword/               ✅ (Task 007)
│   │       │   │   │   ├── ResetPasswordCommand.cs      ✅
│   │       │   │   │   ├── ResetPasswordHandler.cs      ✅
│   │       │   │   │   └── ResetPasswordValidator.cs    ✅
│   │       │   │   ├── RevokeSession/               ✅ (Task 007)
│   │       │   │   │   ├── RevokeSessionCommand.cs      ✅
│   │       │   │   │   └── RevokeSessionHandler.cs      ✅
│   │       │   │   ├── RevokeAllSessions/           ✅ (Task 007)
│   │       │   │   │   ├── RevokeAllSessionsCommand.cs  ✅
│   │       │   │   │   └── RevokeAllSessionsHandler.cs  ✅
│   │       │   │   ├── UpdateSession/               ✅ (Task 007)
│   │       │   │   │   ├── UpdateSessionCommand.cs      ✅
│   │       │   │   │   └── UpdateSessionHandler.cs      ✅
│   │       │   │   ├── MfaSetup/                    ✅ (Task 007)
│   │       │   │   │   ├── MfaSetupCommand.cs           ✅
│   │       │   │   │   └── MfaSetupHandler.cs           ✅
│   │       │   │   ├── MfaConfirm/                  ✅ (Task 007)
│   │       │   │   │   ├── MfaConfirmCommand.cs         ✅
│   │       │   │   │   ├── MfaConfirmHandler.cs         ✅
│   │       │   │   │   └── MfaConfirmValidator.cs       ✅
│   │       │   │   ├── MfaVerify/                   ✅ (Task 007)
│   │       │   │   │   ├── MfaVerifyCommand.cs          ✅
│   │       │   │   │   ├── MfaVerifyHandler.cs          ✅
│   │       │   │   │   └── MfaVerifyValidator.cs        ✅
│   │       │   │   ├── MfaDisable/                  ✅ (Task 007)
│   │       │   │   │   ├── MfaDisableCommand.cs         ✅
│   │       │   │   │   ├── MfaDisableHandler.cs         ✅
│   │       │   │   │   └── MfaDisableValidator.cs       ✅
│   │       │   │   ├── RegenerateBackupCodes/       ✅ (Task 007)
│   │       │   │   │   ├── RegenerateBackupCodesCommand.cs   ✅
│   │       │   │   │   ├── RegenerateBackupCodesHandler.cs   ✅
│   │       │   │   │   └── RegenerateBackupCodesValidator.cs ✅
│   │       │   │   └── UpdatePreferences/           ✅ (Task 007)
│   │       │   │       ├── UpdatePreferencesCommand.cs  ✅
│   │       │   │       ├── UpdatePreferencesHandler.cs  ✅
│   │       │   │       └── UpdatePreferencesValidator.cs ✅
│   │       │   ├── Queries/
│   │       │   │   ├── GetCurrentUser/
│   │       │   │   │   ├── GetCurrentUserQuery.cs   ✅
│   │       │   │   │   └── GetCurrentUserHandler.cs ✅ (Task 007: State checks)
│   │       │   │   └── GetSessions/                 ✅ (Task 007)
│   │       │   │       ├── GetSessionsQuery.cs          ✅
│   │       │   │       └── GetSessionsHandler.cs        ✅
│   │       │   └── Models/
│   │       │       ├── AuthResponse.cs              ✅ (Task 007: MFA fields, SessionId, DeviceId, UserDto extended)
│   │       │       ├── DeviceSessionDto.cs          ✅ (Task 007)
│   │       │       └── MfaModels.cs                 ✅ (Task 007)
│   │       ├── SystemUsers/
│   │       │   ├── Commands/
│   │       │   │   ├── InviteSystemUser/
│   │       │   │   │   ├── InviteSystemUserCommand.cs   ✅
│   │       │   │   │   ├── InviteSystemUserHandler.cs   ✅ (Task 007: RevokedAt filter)
│   │       │   │   │   └── InviteSystemUserValidator.cs ✅ (Task 007: @deleted.local blocked)
│   │       │   │   ├── UpdateSystemUser/
│   │       │   │   │   ├── UpdateSystemUserCommand.cs   ✅
│   │       │   │   │   ├── UpdateSystemUserHandler.cs   ✅ (Task 007: Deactivation safeguards, session revocation)
│   │       │   │   │   └── UpdateSystemUserValidator.cs ✅
│   │       │   │   ├── UpdatePermissions/
│   │       │   │   │   ├── UpdateSystemUserPermissionsCommand.cs ✅
│   │       │   │   │   └── UpdateSystemUserPermissionsHandler.cs ✅ (Task 007: IsAnonymized check)
│   │       │   │   ├── DeleteSystemUser/
│   │       │   │   │   ├── DeleteSystemUserCommand.cs   ✅
│   │       │   │   │   └── DeleteSystemUserHandler.cs   ✅
│   │       │   │   ├── ResetUserMfa/                ✅ (Task 007)
│   │       │   │   │   ├── ResetUserMfaCommand.cs       ✅
│   │       │   │   │   └── ResetUserMfaHandler.cs       ✅
│   │       │   │   ├── UnlockUser/                  ✅ (Task 007)
│   │       │   │   │   ├── UnlockUserCommand.cs         ✅
│   │       │   │   │   └── UnlockUserHandler.cs         ✅
│   │       │   │   ├── RevokeUserSessions/          ✅ (Task 007)
│   │       │   │   │   ├── RevokeUserSessionsCommand.cs ✅
│   │       │   │   │   └── RevokeUserSessionsHandler.cs ✅
│   │       │   │   ├── AnonymizeUser/               ✅ (Task 007)
│   │       │   │   │   ├── AnonymizeUserCommand.cs      ✅
│   │       │   │   │   └── AnonymizeUserHandler.cs      ✅
│   │       │   │   ├── DeactivateSystemUser/        ✅ (Task 007)
│   │       │   │   │   ├── DeactivateSystemUserCommand.cs   ✅
│   │       │   │   │   └── DeactivateSystemUserHandler.cs   ✅
│   │       │   │   └── ActivateSystemUser/          ✅ (Task 007)
│   │       │   │       ├── ActivateSystemUserCommand.cs     ✅
│   │       │   │       └── ActivateSystemUserHandler.cs     ✅
│   │       │   ├── Queries/
│   │       │   │   ├── GetSystemUsers/
│   │       │   │   │   ├── GetSystemUsersQuery.cs       ✅
│   │       │   │   │   └── GetSystemUsersHandler.cs     ✅
│   │       │   │   ├── GetSystemUser/
│   │       │   │   │   ├── GetSystemUserQuery.cs        ✅
│   │       │   │   │   └── GetSystemUserHandler.cs      ✅
│   │       │   │   └── GetUserSessions/             ✅ (Task 007)
│   │       │   │       ├── GetUserSessionsQuery.cs      ✅
│   │       │   │       └── GetUserSessionsHandler.cs    ✅
│   │       │   └── Models/
│   │       │       └── SystemUserDto.cs                 ✅
│   │       ├── SystemPermissions/
│   │       │   ├── Queries/
│   │       │   │   └── GetSystemPermissions/
│   │       │   │       ├── GetSystemPermissionsQuery.cs   ✅
│   │       │   │       └── GetSystemPermissionsHandler.cs ✅
│   │       │   └── Models/
│   │       │       └── SystemPermissionDto.cs             ✅
│   │       ├── SystemAuditLogs/
│   │       │   ├── Queries/
│   │       │   │   ├── GetSystemAuditLogs/
│   │       │   │   │   ├── GetSystemAuditLogsQuery.cs     ✅
│   │       │   │   │   └── GetSystemAuditLogsHandler.cs   ✅
│   │       │   │   └── GetAuditLogFilters/
│   │       │   │       ├── GetAuditLogFiltersQuery.cs     ✅
│   │       │   │       └── GetAuditLogFiltersHandler.cs   ✅
│   │       │   └── Models/
│   │       │       └── SystemAuditLogDto.cs               ✅
│   │       └── SystemInvites/
│   │           ├── Commands/
│   │           │   ├── RevokeInvite/
│   │           │   │   ├── RevokeInviteCommand.cs         ✅
│   │           │   │   └── RevokeInviteHandler.cs         ✅
│   │           │   ├── ResendInvite/
│   │           │   │   ├── ResendInviteCommand.cs         ✅
│   │           │   │   └── ResendInviteHandler.cs         ✅ (Task 007: invite.Language)
│   │           │   └── UpdateInvite/                      ✅ (Task 009)
│   │           │       ├── UpdateInviteCommand.cs         ✅ (Task 009)
│   │           │       ├── UpdateInviteHandler.cs         ✅ (Task 009)
│   │           │       └── UpdateInviteValidator.cs       ✅ (Task 009)
│   │           ├── Queries/
│   │           │   ├── GetSystemInvites/
│   │           │   │   ├── GetSystemInvitesQuery.cs       ✅
│   │           │   │   └── GetSystemInvitesHandler.cs     ✅
│   │           │   ├── GetSystemInvite/
│   │           │   │   ├── GetSystemInviteQuery.cs        ✅
│   │           │   │   └── GetSystemInviteHandler.cs      ✅
│   │           │   └── ValidateInvite/
│   │           │       ├── ValidateInviteQuery.cs         ✅
│   │           │       └── ValidateInviteHandler.cs       ✅
│   │           └── Models/
│   │               └── SystemInviteDto.cs                 ✅
│   │
│   ├── ExoAuth.Infrastructure/
│   │   ├── ExoAuth.Infrastructure.csproj
│   │   ├── DependencyInjection.cs               ✅ (Task 007: All new services)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                  ✅ (Task 007: DeviceSessions, MfaBackupCodes)
│   │   │   ├── Configurations/
│   │   │   │   ├── SystemUserConfiguration.cs           ✅ (Task 007: MFA fields)
│   │   │   │   ├── SystemPermissionConfiguration.cs     ✅
│   │   │   │   ├── SystemUserPermissionConfiguration.cs ✅
│   │   │   │   ├── SystemAuditLogConfiguration.cs       ✅
│   │   │   │   ├── SystemInviteConfiguration.cs         ✅
│   │   │   │   ├── RefreshTokenConfiguration.cs         ✅ (Task 007: DeviceSession relation)
│   │   │   │   ├── PasswordResetTokenConfiguration.cs   ✅ (Task 007)
│   │   │   │   ├── DeviceSessionConfiguration.cs        ✅ (Task 007)
│   │   │   │   └── MfaBackupCodeConfiguration.cs        ✅ (Task 007)
│   │   │   ├── Migrations/                      ✅ (mehrere + Task 007 migrations)
│   │   │   └── Repositories/
│   │   │       └── SystemUserRepository.cs      ✅
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs             ✅ (Task 007: GetIntegerAsync)
│   │   │   └── RedisConnectionFactory.cs        ✅
│   │   ├── Messaging/
│   │   │   ├── RabbitMqMessageBus.cs            ✅
│   │   │   ├── RabbitMqConnectionFactory.cs     ✅
│   │   │   ├── RabbitMqBackgroundService.cs     ✅
│   │   │   └── Consumers/
│   │   │       └── SendEmailConsumer.cs         ✅
│   │   ├── Services/
│   │   │   ├── DateTimeProvider.cs              ✅
│   │   │   ├── PasswordHasher.cs                ✅
│   │   │   ├── TokenService.cs                  ✅ (Task 007: session_id claim)
│   │   │   ├── AuditService.cs                  ✅
│   │   │   ├── EmailService.cs                  ✅ (Task 007: Localized subjects)
│   │   │   ├── EmailTemplateService.cs          ✅ (Task 007: en-US/de-DE folders)
│   │   │   ├── PermissionCacheService.cs        ✅
│   │   │   ├── BruteForceProtectionService.cs   ✅ (Task 007: Progressive lockout)
│   │   │   ├── TokenBlacklistService.cs         ✅
│   │   │   ├── ForceReauthService.cs            ✅
│   │   │   ├── PasswordResetService.cs          ✅ (Task 007)
│   │   │   ├── SystemInviteService.cs           ✅ (Task 007)
│   │   │   ├── DeviceSessionService.cs          ✅ (Task 007)
│   │   │   ├── GeoIpService.cs                  ✅ (Task 007)
│   │   │   ├── DeviceDetectionService.cs        ✅ (Task 007)
│   │   │   ├── RevokedSessionService.cs         ✅ (Task 007)
│   │   │   ├── MfaService.cs                    ✅ (Task 007)
│   │   │   ├── EncryptionService.cs             ✅ (Task 007)
│   │   │   ├── BackupCodeService.cs             ✅ (Task 007)
│   │   │   ├── InviteCleanupService.cs          ✅ (Task 009)
│   │   │   └── InviteCleanupBackgroundService.cs ✅ (Task 009)
│   │   └── Sessions/                            [LEER]
│   │
│   ├── ExoAuth.EmailWorker/                     ✅ (Task 007 - Neues Projekt)
│   │   ├── ExoAuth.EmailWorker.csproj           ✅
│   │   ├── Program.cs                           ✅
│   │   ├── appsettings.json                     ✅
│   │   ├── appsettings.Development.json         ✅
│   │   ├── EmailWorkerService.cs                ✅
│   │   ├── RabbitMqConnectionFactory.cs         ✅
│   │   ├── Consumers/
│   │   │   └── SendEmailConsumer.cs             ✅
│   │   ├── Models/
│   │   │   ├── SendEmailMessage.cs              ✅
│   │   │   └── EmailSettings.cs                 ✅
│   │   └── Services/
│   │       ├── IEmailTemplateService.cs         ✅
│   │       └── EmailTemplateService.cs          ✅
│   │
│   └── ExoAuth.Api/
│       ├── ExoAuth.Api.csproj
│       ├── Program.cs                           ✅ (Task 007: DataProtection)
│       ├── appsettings.json                     ✅ (Task 007: MFA, GeoIP, Lockout config)
│       ├── appsettings.Development.json         ✅
│       ├── appsettings.Production.json
│       ├── Controllers/
│       │   ├── ApiControllerBase.cs             ✅
│       │   ├── HealthController.cs              ✅
│       │   ├── AuthController.cs                ✅ (Task 007: MFA, Sessions, Preferences endpoints)
│       │   ├── SystemUsersController.cs         ✅ (Task 007: Admin MFA/Sessions/Deactivate endpoints)
│       │   ├── SystemPermissionsController.cs   ✅
│       │   ├── SystemAuditLogsController.cs     ✅
│       │   └── SystemInvitesController.cs       ✅
│       ├── Middleware/
│       │   ├── ExceptionMiddleware.cs           ✅
│       │   ├── RequestLoggingMiddleware.cs      ✅
│       │   └── ForceReauthMiddleware.cs         ✅ (Task 007: Revoked session check)
│       ├── Filters/
│       │   ├── RateLimitAttribute.cs            ✅
│       │   ├── RateLimitFilter.cs               ✅
│       │   ├── SystemPermissionAttribute.cs     ✅
│       │   └── SystemPermissionFilter.cs        ✅
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs   ✅
│       └── Services/
│           └── CurrentUserService.cs            ✅ (Task 007: SessionId)
│
├── templates/
│   └── emails/
│       ├── en-US/                               ✅ (Task 007: Renamed from en/)
│       │   ├── system-invite.html               ✅
│       │   ├── password-reset.html              ✅ (Task 007)
│       │   ├── password-changed.html            ✅ (Task 007)
│       │   ├── new-device-login.html            ✅ (Task 007)
│       │   ├── new-location-login.html          ✅ (Task 007)
│       │   ├── mfa-enabled.html                 ✅ (Task 007)
│       │   ├── mfa-disabled.html                ✅ (Task 007)
│       │   ├── mfa-backup-code-used.html        ✅ (Task 007)
│       │   ├── mfa-reset-admin.html             ✅ (Task 007)
│       │   ├── account-unlocked.html            ✅ (Task 007)
│       │   ├── account-locked.html              ✅ (Task 007)
│       │   ├── sessions-revoked-admin.html      ✅ (Task 007)
│       │   └── subjects.json                    ✅ (Task 009: Email subjects i18n)
│       └── de-DE/                               ✅ (Task 007: Renamed from de/)
│           ├── system-invite.html               ✅
│           ├── password-reset.html              ✅ (Task 007)
│           ├── password-changed.html            ✅ (Task 007)
│           ├── new-device-login.html            ✅ (Task 007)
│           ├── new-location-login.html          ✅ (Task 007)
│           ├── mfa-enabled.html                 ✅ (Task 007)
│           ├── mfa-disabled.html                ✅ (Task 007)
│           ├── mfa-backup-code-used.html        ✅ (Task 007)
│           ├── mfa-reset-admin.html             ✅ (Task 007)
│           ├── account-unlocked.html            ✅ (Task 007)
│           ├── account-locked.html              ✅ (Task 007)
│           ├── sessions-revoked-admin.html      ✅ (Task 007)
│           └── subjects.json                    ✅ (Task 009: Email subjects i18n)
│
├── docker/
│   └── email-worker/
│       └── Dockerfile                           ✅ (Task 007)
│
└── tests/
    └── ExoAuth.UnitTests/
        ├── ExoAuth.UnitTests.csproj
        ├── Features/
        │   ├── Auth/
        │   │   ├── RegisterHandlerTests.cs      ✅
        │   │   ├── LoginHandlerTests.cs         ✅ (Task 007: MFA tests)
        │   │   ├── RefreshTokenHandlerTests.cs  ✅
        │   │   ├── ForgotPasswordHandlerTests.cs    ✅ (Task 007)
        │   │   ├── ResetPasswordHandlerTests.cs     ✅ (Task 007)
        │   │   ├── GetSessionsHandlerTests.cs       ✅ (Task 007)
        │   │   ├── RevokeSessionHandlerTests.cs     ✅ (Task 007)
        │   │   ├── RevokeAllSessionsHandlerTests.cs ✅ (Task 007)
        │   │   ├── UpdateSessionHandlerTests.cs     ✅ (Task 007)
        │   │   └── UpdatePreferencesHandlerTests.cs ✅ (Task 007)
        │   ├── SystemUsers/
        │   │   ├── InviteSystemUserHandlerTests.cs      ✅
        │   │   ├── UpdatePermissionsHandlerTests.cs     ✅
        │   │   ├── ResetUserMfaHandlerTests.cs          ✅ (Task 007)
        │   │   ├── UnlockUserHandlerTests.cs            ✅ (Task 007)
        │   │   ├── GetUserSessionsHandlerTests.cs       ✅ (Task 007)
        │   │   ├── RevokeUserSessionsHandlerTests.cs    ✅ (Task 007)
        │   │   ├── AnonymizeUserHandlerTests.cs         ✅ (Task 007)
        │   │   ├── DeactivateSystemUserHandlerTests.cs  ✅ (Task 007)
        │   │   └── ActivateSystemUserHandlerTests.cs    ✅ (Task 007)
        │   └── SystemInvites/
        │       ├── RevokeInviteHandlerTests.cs          ✅
        │       ├── ResendInviteHandlerTests.cs          ✅
        │       ├── ValidateInviteHandlerTests.cs        ✅
        │       └── UpdateInviteHandlerTests.cs          ✅ (Task 009)
        ├── Services/
        │   ├── PasswordHasherTests.cs           ✅
        │   ├── TokenServiceTests.cs             ✅
        │   ├── BruteForceProtectionServiceTests.cs ✅ (Task 007: Progressive lockout tests)
        │   ├── PermissionCacheServiceTests.cs   ✅
        │   ├── EmailTemplateServiceTests.cs     ✅ (Task 007: en-US/de-DE paths, Task 009: GetSubject)
        │   ├── ForceReauthServiceTests.cs       ✅ (Task 009: Session-based)
        │   └── InviteCleanupServiceTests.cs     ✅ (Task 009)
        └── Helpers/
            ├── MockDbContext.cs                 ✅
            ├── TestDataFactory.cs               ✅ (Task 007: CreateDeviceSession)
            └── AsyncQueryableExtensions.cs      ✅
```

### Installierte Packages (NICHT NOCHMAL INSTALLIEREN)

| Projekt | Package | Version |
|---------|---------|---------|
| ExoAuth.Application | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Application | FluentValidation | 12.1.1 |
| ExoAuth.Application | FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore | 8.0.11 |
| ExoAuth.Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| ExoAuth.Infrastructure | Microsoft.EntityFrameworkCore.Design | 8.0.11 |
| ExoAuth.Infrastructure | StackExchange.Redis | 2.10.1 |
| ExoAuth.Infrastructure | RabbitMQ.Client | 7.2.0 |
| ExoAuth.Infrastructure | Konscious.Security.Cryptography.Argon2 | 1.3.1 |
| ExoAuth.Infrastructure | Microsoft.Extensions.Hosting.Abstractions | 8.0.1 |
| ExoAuth.Infrastructure | System.IdentityModel.Tokens.Jwt | 8.x |
| ExoAuth.Infrastructure | Otp.NET | 1.4.1 | (Task 007: TOTP) |
| ExoAuth.Infrastructure | MaxMind.GeoIP2 | 5.4.1 | (Task 007: GeoIP) |
| ExoAuth.Infrastructure | UAParser | 3.1.47 | (Task 007: User-Agent) |
| ExoAuth.Infrastructure | Microsoft.AspNetCore.DataProtection.Abstractions | 8.0.11 | (Task 007: Encryption) |
| ExoAuth.Infrastructure | Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | (Task 007) |
| ExoAuth.Infrastructure | Microsoft.Extensions.Configuration.Binder | 8.0.2 | (Task 007) |
| ExoAuth.Api | Mediator.SourceGenerator | 3.0.1 |
| ExoAuth.Api | Mediator.Abstractions | 3.0.1 |
| ExoAuth.Api | Swashbuckle.AspNetCore | default |
| ExoAuth.Api | Serilog.AspNetCore | 8.0.3 |
| ExoAuth.Api | Serilog.Sinks.Console | 6.0.0 |
| ExoAuth.Api | AspNetCore.HealthChecks.NpgSql | 8.0.2 |
| ExoAuth.Api | AspNetCore.HealthChecks.Redis | 8.0.1 |
| ExoAuth.Api | Microsoft.AspNetCore.Authentication.JwtBearer | 8.x |
| ExoAuth.UnitTests | xunit | default |
| ExoAuth.UnitTests | Moq | default |
| ExoAuth.UnitTests | FluentAssertions | default |

**Hinweis:** `AspNetCore.HealthChecks.Rabbitmq` wurde entfernt - inkompatibel mit `RabbitMQ.Client 7.x`

### Projekt Referenzen (NICHT NOCHMAL HINZUFÜGEN)

```
Application → Domain
Infrastructure → Application
Api → Application, Infrastructure
UnitTests → Application, Domain
IntegrationTests → Api
```

---

## Code Strukturen (Copy-Paste Templates)

### Entity
```csharp
namespace ExoAuth.Domain.Entities;

public class {Name}
{
    public Guid Id { get; private set; }
    // Properties...

    private {Name}() { } // EF Core

    public static {Name} Create(...)
    {
        return new {Name} { ... };
    }
}
```

### Command
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed record {Name}Command(
    // Properties...
) : ICommand<{Response}>;
```

### Command Handler
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed class {Name}Handler : ICommandHandler<{Name}Command, {Response}>
{
    public {Name}Handler(/* dependencies */)
    {
    }

    public async ValueTask<{Response}> Handle({Name}Command command, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Validator
```csharp
namespace ExoAuth.Application.Features.{Feature}.Commands.{Name};

public sealed class {Name}Validator : AbstractValidator<{Name}Command>
{
    public {Name}Validator()
    {
        RuleFor(x => x.Property).NotEmpty();
    }
}
```

### Query
```csharp
namespace ExoAuth.Application.Features.{Feature}.Queries.{Name};

public sealed record {Name}Query(
    // Properties...
) : IQuery<{Response}>;
```

### Controller
```csharp
namespace ExoAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class {Feature}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Feature}Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<Response>> Create(Request request, CancellationToken ct)
    {
        var command = new CreateCommand(request.Property);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
```

### EF Configuration
```csharp
namespace ExoAuth.Infrastructure.Persistence.Configurations;

public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{
    public void Configure(EntityTypeBuilder<{Name}> builder)
    {
        builder.ToTable("{Name}s");
        builder.HasKey(x => x.Id);
        // Weitere Config...
    }
}
```

---

## API Security Checkliste

Bei jedem neuen Endpoint MUSS geprüft werden:

### Rate Limiting

| Frage | Wenn Ja |
|-------|---------|
| Kann der Endpoint ohne Auth aufgerufen werden? | `[RateLimit(10)]` |
| Ist es ein sensitiver Endpoint (Login, Register, Password Reset)? | `[RateLimit(5)]` |
| Ist es ein normaler Auth-geschützter Endpoint? | `[RateLimit]` (nutzt Default: 100/min) |
| Ist es ein interner/Admin Endpoint? | `[RateLimit]` oder höheres Limit |

**Wichtig:** Rate Limiting schützt auch bei 401/403 Responses vor Spam/DoS!

```csharp
// Beispiele
[RateLimit(5)]   // Streng: Login, Register, AcceptInvite
[RateLimit(10)]  // Moderat: Refresh, Logout
[RateLimit]      // Default (100/min): Auth-geschützte Endpoints
```

### Brute Force vs Rate Limiting

| Schutz | Zweck | Key | TTL |
|--------|-------|-----|-----|
| **Brute Force** | Schutz einzelner Accounts vor Password-Guessing | `login:attempts:{email}` | 15 min |
| **Rate Limiting** | Schutz API vor Spam/DoS (auch bei 401s!) | `rate_limit:{ip}:{endpoint}` | 1 min |

**Beide sind nötig!** Brute Force schützt Accounts, Rate Limiting schützt die API.

---

## Regeln für Task Erstellung

1. **IMMER** zuerst diese Datei lesen
2. **IMMER** prüfen ob Files/Packages schon existieren
3. **IMMER** die Reihenfolge einhalten: Domain → Application → Infrastructure → Api
4. **IMMER** Tests mit einplanen
5. **IMMER** am Ende diese Datei updaten
6. **IMMER** Rate Limiting für neue Endpoints prüfen (siehe API Security Checkliste)
7. **NIE** Packages doppelt installieren
8. **NIE** Files überschreiben ohne zu fragen

---

## Letzte Änderung

- **Datum:** 2025-12-31
- **Status:** Backend Improvements komplett (Task 009)
- **Erledigte Tasks:**
  - Task 001: Foundation & Infrastructure Setup ✅
  - Task 002: System Authentication, Users, Permissions & Audit ✅ (106 Unit Tests)
  - Task 003: Frontend Foundation ✅
  - Task 004: Frontend System Authentication & User Management ✅
  - Task 005: Backend Improvements - Audit, Users, Invitations & Security ✅ (129 Unit Tests)
  - Task 006: Frontend Improvements ✅
  - Task 007: Auth Security & Device Management ✅ (234 Unit Tests)
  - Task 008: Frontend Security & Settings ✅
  - Task 009: Backend Improvements - Reauth, Emails, Lists & Cleanup ✅
  - Task 010: Frontend List Improvements & Session Details ✅
- **Nächster Task:** Task 011 (TBD)
- **Task 009 Updates:**
  - Session-Based Reauth (Redis key per session statt per user)
  - Email Subjects aus JSON Dateien (i18n: en-US/de-DE)
  - Konfigurierbarer Templates BasePath für Docker
  - User List Filter (isActive, isAnonymized, isLocked, mfaEnabled)
  - User List versteckt anonymisierte User standardmäßig
  - Invite List SQL-basiertes Filtering (Performance Fix)
  - Invite List Sorting (email, firstName, lastName, createdAt, expiresAt)
  - Invite List versteckt expired/revoked standardmäßig
  - Edit Invites (PATCH endpoint, nur pending)
  - Invite Cleanup Background Job (30 Tage Retention)
  - Neue Indexes für User & Invite Performance
- **Task 007 Updates:**
  - Email Worker als separater Microservice
  - Password Reset Flow (Token + Code)
  - MFA (TOTP + Backup Codes)
  - Device Session Management mit GeoIP
  - Progressive Account Lockout
  - Remember Me Funktionalität
  - User Preferences (Sprache)
  - Account Anonymisierung (GDPR)
  - Admin MFA Reset, Unlock, Session Management
  - Immediate Session Invalidation via Redis
  - Language Code Standardization (en-US, de-DE)
  - 24 Email Templates (EN/DE)
  - Umfangreiche Bug Fixes (19 bugs fixed)

### Bekannte Einschränkungen
- RabbitMQ Health Check nicht in `/health` enthalten (`RabbitMQ.Client 7.x` Inkompatibilität mit Health Check Package)
- RabbitMQ Verbindung wird via `RabbitMqBackgroundService` beim Start geprüft
- GeoIP erfordert MaxMind GeoLite2 Database (selbst zu beschaffen, graceful fallback wenn nicht vorhanden)

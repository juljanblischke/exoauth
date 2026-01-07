# Backend Reference - ExoAuth

> **Read this file completely before any backend work.**

---

## File Tree (Current State)

```
backend/
├── ExoAuth.sln
│
├── src/
│   ├── ExoAuth.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── SystemUser.cs                    (MFA, Preferences, Lockout, Anonymization)
│   │   │   ├── SystemPermission.cs
│   │   │   ├── SystemUserPermission.cs
│   │   │   ├── SystemAuditLog.cs
│   │   │   ├── SystemInvite.cs
│   │   │   ├── RefreshToken.cs                  (DeviceId, RememberMe)
│   │   │   ├── PasswordResetToken.cs
│   │   │   ├── MfaBackupCode.cs
│   │   │   ├── Device.cs                        (Task 017 - consolidated from DeviceSession, TrustedDevice, DeviceApprovalRequest)
│   │   │   ├── LoginPattern.cs                  (Task 013)
│   │   │   ├── Passkey.cs                       (Task 019 - WebAuthn credentials)
│   │   │   └── IpRestriction.cs                 (Task 023 - Whitelist/Blacklist)
│   │   ├── Enums/
│   │   │   ├── UserType.cs
│   │   │   ├── DeviceStatus.cs                  (Task 017 - PendingApproval, Trusted, Revoked)
│   │   │   ├── IpRestrictionType.cs             (Task 023 - Whitelist, Blacklist)
│   │   │   └── IpRestrictionSource.cs           (Task 023 - Manual, Automatic)
│   │   └── Constants/
│   │       └── SystemPermissions.cs
│   │
│   ├── ExoAuth.Application/
│   │   ├── DependencyInjection.cs
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── IMessageBus.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── IAppDbContext.cs
│   │   │   │   ├── IPasswordHasher.cs
│   │   │   │   ├── ITokenService.cs
│   │   │   │   ├── IAuditService.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   ├── ISystemUserRepository.cs
│   │   │   │   ├── IPermissionCacheService.cs
│   │   │   │   ├── IBruteForceProtectionService.cs
│   │   │   │   ├── ITokenBlacklistService.cs
│   │   │   │   ├── IEmailTemplateService.cs
│   │   │   │   ├── IForceReauthService.cs
│   │   │   │   ├── IPasswordResetService.cs
│   │   │   │   ├── ISystemInviteService.cs
│   │   │   │   ├── IDeviceService.cs             (Task 017 - consolidated device management)
│   │   │   │   ├── IDeviceDetectionService.cs
│   │   │   │   ├── IGeoIpService.cs
│   │   │   │   ├── IRevokedSessionService.cs
│   │   │   │   ├── IMfaService.cs
│   │   │   │   ├── IEncryptionService.cs
│   │   │   │   ├── IBackupCodeService.cs
│   │   │   │   ├── IInviteCleanupService.cs
│   │   │   │   ├── IRiskScoringService.cs       (Task 013)
│   │   │   │   ├── ILoginPatternService.cs      (Task 013)
│   │   │   │   ├── IPasskeyService.cs           (Task 019)
│   │   │   │   ├── ICaptchaProvider.cs          (Task 021)
│   │   │   │   ├── ICaptchaService.cs           (Task 021)
│   │   │   │   ├── IRateLimitService.cs         (Task 023)
│   │   │   │   └── IIpRestrictionService.cs     (Task 023)
│   │   │   └── Models/
│   │   │       ├── ApiResponse.cs
│   │   │       ├── ApiError.cs
│   │   │       ├── ErrorCodes.cs
│   │   │       ├── PaginationMeta.cs
│   │   │       ├── CursorPagedList.cs
│   │   │       ├── GeoLocation.cs
│   │   │       ├── DeviceInfo.cs
│   │   │       ├── RiskScore.cs                 (Task 013)
│   │   │       ├── PasskeyCredentialResult.cs   (Task 019)
│   │   │       ├── CaptchaSettings.cs           (Task 021)
│   │   │       ├── CaptchaResult.cs             (Task 021)
│   │   │       ├── CaptchaPublicConfig.cs       (Task 021)
│   │   │       └── RateLimitSettings.cs         (Task 023)
│   │   │   ├── Behaviors/
│   │   │   │   └── ValidationBehavior.cs
│   │   │   ├── Messages/
│   │   │   │   └── EmailMessage.cs
│   │   └── Features/
│   │       ├── Auth/
│   │       │   ├── Commands/
│   │       │   │   ├── Register/
│   │       │   │   ├── Login/
│   │       │   │   ├── RefreshToken/
│   │       │   │   ├── Logout/
│   │       │   │   ├── AcceptInvite/
│   │       │   │   ├── ForgotPassword/
│   │       │   │   ├── ResetPassword/
│   │       │   │   ├── MfaSetup/
│   │       │   │   ├── MfaConfirm/
│   │       │   │   ├── MfaVerify/
│   │       │   │   ├── MfaDisable/
│   │       │   │   ├── RegenerateBackupCodes/
│   │       │   │   ├── UpdatePreferences/
│   │       │   │   ├── ApproveDevice/           (Task 013/017)
│   │       │   │   ├── ApproveDeviceLink/       (Task 013/017)
│   │       │   │   ├── ApproveDeviceFromSession/(Task 017)
│   │       │   │   ├── DenyDevice/              (Task 013/017)
│   │       │   │   ├── RevokeDevice/            (Task 017)
│   │       │   │   ├── RenameDevice/            (Task 017)
│   │       │   │   ├── PasskeyRegisterOptions/  (Task 019)
│   │       │   │   ├── PasskeyRegister/         (Task 019)
│   │       │   │   ├── PasskeyLoginOptions/     (Task 019)
│   │       │   │   ├── PasskeyLogin/            (Task 019)
│   │       │   │   ├── RenamePasskey/           (Task 019)
│   │       │   │   └── DeletePasskey/           (Task 019)
│   │       │   ├── Queries/
│   │       │   │   ├── GetCurrentUser/
│   │       │   │   ├── GetDevices/              (Task 017)
│   │       │   │   └── GetPasskeys/             (Task 019)
│   │       │   └── Models/
│   │       │       ├── AuthResponse.cs
│   │       │       ├── DeviceDto.cs             (Task 017)
│   │       │       ├── MfaModels.cs
│   │       │       └── PasskeyDto.cs            (Task 019)
│   │       ├── SystemUsers/
│   │       │   ├── Commands/
│   │       │   │   ├── InviteSystemUser/
│   │       │   │   ├── UpdateSystemUser/
│   │       │   │   ├── UpdatePermissions/
│   │       │   │   ├── DeleteSystemUser/
│   │       │   │   ├── ResetUserMfa/
│   │       │   │   ├── UnlockUser/
│   │       │   │   ├── RevokeUserSessions/          (returns DeviceDto)
│   │       │   │   ├── RevokeUserSession/           (returns DeviceDto)
│   │       │   │   ├── AnonymizeUser/
│   │       │   │   ├── DeactivateSystemUser/
│   │       │   │   └── ActivateSystemUser/
│   │       │   ├── Queries/
│   │       │   │   ├── GetSystemUsers/
│   │       │   │   ├── GetSystemUser/
│   │       │   │   └── GetUserSessions/             (returns List<DeviceDto>)
│   │       │   └── Models/
│   │       │       └── SystemUserDto.cs
│   │       ├── SystemPermissions/
│   │       ├── SystemAuditLogs/
│   │       ├── SystemInvites/
│   │       └── IpRestrictions/                      (Task 023)
│   │           ├── Commands/
│   │           │   ├── CreateIpRestriction/
│   │           │   └── DeleteIpRestriction/
│   │           ├── Queries/
│   │           │   └── GetIpRestrictions/
│   │           └── Models/
│   │               └── IpRestrictionDto.cs
│   │
│   ├── ExoAuth.Infrastructure/
│   │   ├── DependencyInjection.cs
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── SystemUserConfiguration.cs
│   │   │   │   ├── SystemPermissionConfiguration.cs
│   │   │   │   ├── SystemUserPermissionConfiguration.cs
│   │   │   │   ├── SystemAuditLogConfiguration.cs
│   │   │   │   ├── SystemInviteConfiguration.cs
│   │   │   │   ├── RefreshTokenConfiguration.cs
│   │   │   │   ├── PasswordResetTokenConfiguration.cs
│   │   │   │   ├── MfaBackupCodeConfiguration.cs
│   │   │   │   ├── DeviceConfiguration.cs                 (Task 017)
│   │   │   │   ├── LoginPatternConfiguration.cs           (Task 013)
│   │   │   │   ├── PasskeyConfiguration.cs                (Task 019)
│   │   │   │   └── IpRestrictionConfiguration.cs          (Task 023)
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   │       └── SystemUserRepository.cs
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs
│   │   │   └── RedisConnectionFactory.cs
│   │   ├── Messaging/
│   │   │   ├── RabbitMqMessageBus.cs
│   │   │   ├── RabbitMqConnectionFactory.cs
│   │   │   ├── RabbitMqBackgroundService.cs
│   │   │   └── Consumers/
│   │   │       └── SendEmailConsumer.cs
│   │   └── Services/
│   │       ├── DateTimeProvider.cs
│   │       ├── PasswordHasher.cs
│   │       ├── TokenService.cs
│   │       ├── AuditService.cs
│   │       ├── EmailService.cs
│   │       ├── EmailTemplateService.cs
│   │       ├── PermissionCacheService.cs
│   │       ├── BruteForceProtectionService.cs
│   │       ├── TokenBlacklistService.cs
│   │       ├── ForceReauthService.cs
│   │       ├── PasswordResetService.cs
│   │       ├── SystemInviteService.cs
│   │       ├── DeviceService.cs              (Task 017)
│   │       ├── GeoIpService.cs
│   │       ├── DeviceDetectionService.cs
│   │       ├── RevokedSessionService.cs
│   │       ├── MfaService.cs
│   │       ├── EncryptionService.cs
│   │       ├── BackupCodeService.cs
│   │       ├── InviteCleanupService.cs
│   │       ├── InviteCleanupBackgroundService.cs
│   │       ├── RiskScoringService.cs            (Task 013)
│   │       ├── LoginPatternService.cs           (Task 013)
│       ├── PasskeyService.cs                (Task 019)
│       ├── CaptchaService.cs                (Task 021)
│       ├── Captcha/
│       │   ├── TurnstileProvider.cs         (Task 021)
│       │   ├── RecaptchaV3Provider.cs       (Task 021)
│       │   ├── HcaptchaProvider.cs          (Task 021)
│       │   └── DisabledCaptchaProvider.cs   (Task 021)
│       ├── RateLimitService.cs              (Task 023)
│       └── IpRestrictionService.cs          (Task 023)
│   │
│   ├── ExoAuth.EmailWorker/                     (Separate Microservice)
│   │   ├── Program.cs
│   │   ├── EmailWorkerService.cs
│   │   ├── Consumers/SendEmailConsumer.cs
│   │   ├── Models/
│   │   └── Services/
│   │
│   └── ExoAuth.Api/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── ApiControllerBase.cs
│       │   ├── HealthController.cs
│       │   ├── AuthController.cs
│       │   ├── SystemUsersController.cs
│       │   ├── SystemPermissionsController.cs
│       │   ├── SystemAuditLogsController.cs
│       │   ├── SystemInvitesController.cs
│       │   ├── CaptchaController.cs             (Task 021)
│       │   └── IpRestrictionsController.cs      (Task 023)
│       ├── Middleware/
│       │   ├── ExceptionMiddleware.cs
│       │   ├── RequestLoggingMiddleware.cs
│       │   └── ForceReauthMiddleware.cs
│       ├── Filters/
│       │   ├── RateLimitAttribute.cs
│       │   ├── RateLimitFilter.cs
│       │   ├── SystemPermissionAttribute.cs
│       │   └── SystemPermissionFilter.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       └── Services/
│           └── CurrentUserService.cs
│
├── templates/emails/
│   ├── en-US/
│   │   ├── system-invite.html
│   │   ├── password-reset.html
│   │   ├── password-changed.html
│   │   ├── new-device-login.html
│   │   ├── new-location-login.html
│   │   ├── mfa-enabled.html
│   │   ├── mfa-disabled.html
│   │   ├── mfa-backup-code-used.html
│   │   ├── mfa-reset-admin.html
│   │   ├── account-unlocked.html
│   │   ├── account-locked.html
│   │   ├── sessions-revoked-admin.html
│   │   ├── device-approval-required.html        (Task 013)
│   │   ├── device-denied-alert.html             (Task 013)
│   │   ├── passkey-registered.html          (Task 019)
│   │   ├── passkey-removed.html             (Task 019)
│   │   └── subjects.json
│   └── de-DE/
│       └── (same files as en-US)
│
└── tests/ExoAuth.UnitTests/
    ├── Features/
    │   ├── Auth/
    │   │   ├── Passkeys/                        (Task 019 - 41 tests)
│   │   └── Captcha/                         (Task 021 - 49 tests)
    │   ├── SystemUsers/
    │   ├── SystemAuditLogs/
    │   ├── SystemInvites/
    │   └── IpRestrictions/                      (Task 023 - 27 tests)
    ├── Services/
    │   ├── RateLimitServiceTests.cs             (Task 023 - 15 tests)
    │   └── IpRestrictionServiceTests.cs         (Task 023 - 9 tests)
    └── Helpers/
        ├── MockDbContext.cs
        ├── TestDataFactory.cs
        └── AsyncQueryableExtensions.cs
```

---

## Installed Packages (DO NOT REINSTALL)

| Projekt | Package | Version |
|---------|---------|---------|
| Application | Mediator.Abstractions | 3.0.1 |
| Application | FluentValidation | 12.1.1 |
| Application | FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| Infrastructure | Microsoft.EntityFrameworkCore | 8.0.11 |
| Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| Infrastructure | Microsoft.EntityFrameworkCore.Design | 8.0.11 |
| Infrastructure | StackExchange.Redis | 2.10.1 |
| Infrastructure | RabbitMQ.Client | 7.2.0 |
| Infrastructure | Konscious.Security.Cryptography.Argon2 | 1.3.1 |
| Infrastructure | Microsoft.Extensions.Hosting.Abstractions | 8.0.1 |
| Infrastructure | System.IdentityModel.Tokens.Jwt | 8.x |
| Infrastructure | Otp.NET | 1.4.1 |
| Infrastructure | MaxMind.GeoIP2 | 5.4.1 |
| Infrastructure | UAParser | 3.1.47 |
| Infrastructure | Microsoft.AspNetCore.DataProtection.Abstractions | 8.0.11 |
| Api | Mediator.SourceGenerator | 3.0.1 |
| Api | Swashbuckle.AspNetCore | default |
| Api | Serilog.AspNetCore | 8.0.3 |
| Api | Serilog.Sinks.Console | 6.0.0 |
| Api | AspNetCore.HealthChecks.NpgSql | 8.0.2 |
| Api | AspNetCore.HealthChecks.Redis | 8.0.1 |
| Api | Microsoft.AspNetCore.Authentication.JwtBearer | 8.x |
| UnitTests | xunit | default |
| UnitTests | Moq | default |
| UnitTests | FluentAssertions | default |
| Application | Fido2.Models | 3.0.1 |
| Infrastructure | Fido2 | 3.0.1 |
| Api | Fido2.AspNet | 3.0.1 |

---

## Coding Standards

### Naming Conventions

| What | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `UserService` |
| Interfaces | I + PascalCase | `IUserRepository` |
| Methods | PascalCase | `GetUserById` |
| Properties | PascalCase | `FirstName` |
| Private fields | _camelCase | `_userRepository` |
| Constants | PascalCase | `MaxRetryCount` |
| Files | PascalCase.cs | `UserService.cs` |

### DO's ✅

- Use `sealed` for classes not inherited
- Use `record` for DTOs and Commands/Queries
- Use `CancellationToken` in async methods
- Use Dependency Injection
- Use `IDateTimeProvider.UtcNow` (never `DateTime.Now`)
- Entities: private setters, Factory Methods (`Create`, `Update`)
- One Command/Query per file, one Handler per Command/Query
- Thin Controllers (only call Mediator)
- Write Unit Tests for handlers

### DON'Ts ❌

- No business logic in Controllers
- No `public` fields
- No magic strings (use Constants)
- No `async void` (except event handlers)
- No direct DB calls in Application layer (use interfaces)
- No EF Core dependencies in Domain layer

---

## Code Templates

### Entity
```csharp
public sealed class {Name}
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

### Command + Handler
```csharp
// Command
public sealed record {Name}Command(...) : ICommand<{Response}>;

// Handler
public sealed class {Name}Handler : ICommandHandler<{Name}Command, {Response}>
{
    public {Name}Handler(/* dependencies */) { }

    public async ValueTask<{Response}> Handle({Name}Command command, CancellationToken ct)
    {
        // Implementation
    }
}

// Validator
public sealed class {Name}Validator : AbstractValidator<{Name}Command>
{
    public {Name}Validator()
    {
        RuleFor(x => x.Property).NotEmpty();
    }
}
```

### Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class {Feature}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Feature}Controller(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [RateLimit("default")] // Use preset: strict, sensitive, default, lenient, admin
    public async Task<IActionResult> Create(Request request, CancellationToken ct)
    {
        var command = new CreateCommand(request.Property);
        var result = await _mediator.Send(command, ct);
        return Ok(ApiResponse<T>.Success(result));
    }
}
```

---

## Error Codes

### Auth Errors
| Code | HTTP | Description |
|------|------|-------------|
| `AUTH_INVALID_CREDENTIALS` | 401 | Wrong email/password |
| `AUTH_USER_INACTIVE` | 401 | User deactivated |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token expired |
| `AUTH_REFRESH_TOKEN_INVALID` | 401 | Refresh token invalid |
| `AUTH_FORCE_REAUTH` | 401 | Must re-login (permission change) |
| `AUTH_TOO_MANY_ATTEMPTS` | 429 | Brute force lockout |
| `AUTH_INVITE_EXPIRED` | 400 | Invite expired (24h) |
| `AUTH_INVITE_INVALID` | 400 | Invalid invite token |
| `AUTH_INVITE_REVOKED` | 400 | Invite revoked |
| `AUTH_EMAIL_EXISTS` | 409 | Email taken |

### MFA Errors
| Code | HTTP | Description |
|------|------|-------------|
| `MFA_REQUIRED` | 403 | MFA verification needed |
| `MFA_CODE_INVALID` | 400 | Wrong TOTP code |
| `MFA_ALREADY_ENABLED` | 400 | MFA already on |
| `MFA_NOT_ENABLED` | 400 | MFA not on |
| `MFA_TOKEN_INVALID` | 401 | MFA token invalid/expired |

### Session Errors
| Code | HTTP | Description |
|------|------|-------------|
| `SESSION_NOT_FOUND` | 404 | Session not found |
| `SESSION_CANNOT_REVOKE_CURRENT` | 400 | Can't revoke current session |
| `SESSION_REVOKED` | 401 | Session was revoked |

### Device Errors (Task 017 - consolidated)
| Code | HTTP | Description |
|------|------|-------------|
| `DEVICE_NOT_FOUND` | 404 | Device not found |
| `DEVICE_NOT_PENDING` | 400 | Device is not in PendingApproval status |
| `CANNOT_REVOKE_CURRENT_DEVICE` | 400 | Can't revoke current device |
| `DEVICE_APPROVAL_REQUIRED` | 200 | Login needs device approval |
| `APPROVAL_TOKEN_INVALID` | 400 | Approval token invalid |
| `APPROVAL_TOKEN_EXPIRED` | 400 | Approval token expired |
| `APPROVAL_CODE_INVALID` | 400 | Wrong approval code |
| `APPROVAL_MAX_ATTEMPTS` | 429 | Too many wrong codes |
| `DEVICE_APPROVAL_DENIED` | 403 | Device was denied |

### Passkey Errors (Task 019)
| Code | HTTP | Description |
|------|------|-------------|
| `PASSKEY_NOT_FOUND` | 404 | Passkey not found |
| `PASSKEY_INVALID_CREDENTIAL` | 401 | Invalid passkey credential |
| `PASSKEY_REGISTRATION_FAILED` | 400 | Passkey registration failed |
| `PASSKEY_ALREADY_REGISTERED` | 409 | Passkey already registered |
| `PASSKEY_CANNOT_DELETE_LAST` | 400 | Can't delete last passkey without password |

### CAPTCHA Errors (Task 021)
| Code | HTTP | Description |
|------|------|-------------|
| `CAPTCHA_REQUIRED` | 400 | CAPTCHA verification required |
| `CAPTCHA_INVALID` | 400 | CAPTCHA verification failed |

### Rate Limiting / IP Restriction Errors (Task 023)
| Code | HTTP | Description |
|------|------|-------------|
| `RATE_LIMIT_EXCEEDED` | 429 | Rate limit exceeded |
| `IP_BLACKLISTED` | 403 | IP address is blacklisted |
| `IP_RESTRICTION_NOT_FOUND` | 404 | IP restriction not found |
| `IP_RESTRICTION_ALREADY_EXISTS` | 409 | IP restriction already exists |
| `IP_RESTRICTION_INVALID_CIDR` | 400 | Invalid IP address or CIDR notation |

### Account Errors
| Code | HTTP | Description |
|------|------|-------------|
| `ACCOUNT_LOCKED` | 423 | Temp locked |
| `ACCOUNT_LOCKED_PERMANENT` | 423 | Permanently locked |

### System Errors
| Code | HTTP | Description |
|------|------|-------------|
| `SYSTEM_USER_NOT_FOUND` | 404 | User not found |
| `SYSTEM_LAST_PERMISSION_HOLDER` | 400 | Can't remove last permission holder |
| `SYSTEM_CANNOT_DELETE_SELF` | 400 | Can't delete self |
| `SYSTEM_USER_ANONYMIZED` | 400 | User is anonymized |

---

## API Security Checklist

Every new endpoint MUST have:

| Question | Action |
|----------|--------|
| Very sensitive (login, register, reset)? | `[RateLimit("strict")]` (3/min) |
| Sensitive (MFA, password change)? | `[RateLimit("sensitive")]` (5/min) |
| Public endpoint? | `[RateLimit("default")]` (30/min) |
| Auth-protected general? | `[RateLimit("lenient")]` (60/min) |
| Admin endpoint? | `[RateLimit("admin")]` (100/min) |

**Rate Limit Presets (Task 023):**
- `strict`: 3/min, 15/hour, 50/day - Login, Register, Password Reset
- `sensitive`: 5/min, 30/hour, 100/day - MFA, Device Approval
- `default`: 30/min, 200/hour, 1000/day - Public endpoints
- `lenient`: 60/min, 500/hour, 5000/day - Authenticated users
- `admin`: 100/min, 1000/hour, 10000/day - Admin operations

**Both Brute Force AND Rate Limiting are needed:**
- Brute Force: Protects individual accounts
- Rate Limiting: Protects API from spam/DoS
- IP Whitelist: Bypasses rate limiting (trusted IPs)
- IP Blacklist: Blocks all requests (malicious IPs)

---

## Implementation Order

1. **Domain**: Entity erstellen
2. **Infrastructure**: Configuration + DbContext + Migration
3. **Application**: Commands/Queries + Handlers + Validators
4. **API**: Controller + Endpoints
5. **Tests**: Unit Tests
6. **Update the Task file**
7. **Update this memory file**

---

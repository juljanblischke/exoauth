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
│   │   │   ├── RefreshToken.cs                  (DeviceSessionId, RememberMe)
│   │   │   ├── PasswordResetToken.cs
│   │   │   ├── DeviceSession.cs
│   │   │   ├── MfaBackupCode.cs
│   │   │   ├── DeviceApprovalRequest.cs         (Task 013)
│   │   │   └── LoginPattern.cs                  (Task 013)
│   │   ├── Enums/
│   │   │   ├── UserType.cs
│   │   │   └── ApprovalStatus.cs                (Task 013)
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
│   │   │   │   ├── IDeviceSessionService.cs
│   │   │   │   ├── IDeviceDetectionService.cs
│   │   │   │   ├── IGeoIpService.cs
│   │   │   │   ├── IRevokedSessionService.cs
│   │   │   │   ├── IMfaService.cs
│   │   │   │   ├── IEncryptionService.cs
│   │   │   │   ├── IBackupCodeService.cs
│   │   │   │   ├── IInviteCleanupService.cs
│   │   │   │   ├── IRiskScoringService.cs       (Task 013)
│   │   │   │   ├── ILoginPatternService.cs      (Task 013)
│   │   │   │   └── IDeviceApprovalService.cs    (Task 013)
│   │   │   ├── Behaviors/
│   │   │   │   └── ValidationBehavior.cs
│   │   │   ├── Messages/
│   │   │   │   └── EmailMessage.cs
│   │   │   └── Models/
│   │   │       ├── ApiResponse.cs
│   │   │       ├── ApiError.cs
│   │   │       ├── ErrorCodes.cs
│   │   │       ├── PaginationMeta.cs
│   │   │       ├── CursorPagedList.cs
│   │   │       ├── GeoLocation.cs
│   │   │       ├── DeviceInfo.cs
│   │   │       └── RiskScore.cs                 (Task 013)
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
│   │       │   │   ├── RevokeSession/
│   │       │   │   ├── RevokeAllSessions/
│   │       │   │   ├── UpdateSession/
│   │       │   │   ├── MfaSetup/
│   │       │   │   ├── MfaConfirm/
│   │       │   │   ├── MfaVerify/
│   │       │   │   ├── MfaDisable/
│   │       │   │   ├── RegenerateBackupCodes/
│   │       │   │   ├── UpdatePreferences/
│   │       │   │   ├── ApproveDevice/           (Task 013)
│   │       │   │   ├── ApproveDeviceLink/       (Task 013)
│   │       │   │   └── DenyDevice/              (Task 013)
│   │       │   ├── Queries/
│   │       │   │   ├── GetCurrentUser/
│   │       │   │   └── GetSessions/
│   │       │   └── Models/
│   │       │       ├── AuthResponse.cs
│   │       │       ├── DeviceSessionDto.cs
│   │       │       └── MfaModels.cs
│   │       ├── SystemUsers/
│   │       │   ├── Commands/
│   │       │   │   ├── InviteSystemUser/
│   │       │   │   ├── UpdateSystemUser/
│   │       │   │   ├── UpdatePermissions/
│   │       │   │   ├── DeleteSystemUser/
│   │       │   │   ├── ResetUserMfa/
│   │       │   │   ├── UnlockUser/
│   │       │   │   ├── RevokeUserSessions/
│   │       │   │   ├── RevokeUserSession/
│   │       │   │   ├── AnonymizeUser/
│   │       │   │   ├── DeactivateSystemUser/
│   │       │   │   └── ActivateSystemUser/
│   │       │   ├── Queries/
│   │       │   │   ├── GetSystemUsers/
│   │       │   │   ├── GetSystemUser/
│   │       │   │   └── GetUserSessions/
│   │       │   └── Models/
│   │       │       └── SystemUserDto.cs
│   │       ├── SystemPermissions/
│   │       ├── SystemAuditLogs/
│   │       └── SystemInvites/
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
│   │   │   │   ├── DeviceSessionConfiguration.cs
│   │   │   │   ├── MfaBackupCodeConfiguration.cs
│   │   │   │   ├── DeviceApprovalRequestConfiguration.cs  (Task 013)
│   │   │   │   └── LoginPatternConfiguration.cs           (Task 013)
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
│   │       ├── DeviceSessionService.cs
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
│   │       └── DeviceApprovalService.cs         (Task 013)
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
│       │   └── SystemInvitesController.cs
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
│   │   └── subjects.json
│   └── de-DE/
│       └── (same files as en-US)
│
└── tests/ExoAuth.UnitTests/
    ├── Features/
    │   ├── Auth/
    │   ├── SystemUsers/
    │   ├── SystemAuditLogs/
    │   └── SystemInvites/
    ├── Services/
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
    [RateLimit(10)]
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

### Device Trust Errors (Task 013)
| Code | HTTP | Description |
|------|------|-------------|
| `DEVICE_APPROVAL_REQUIRED` | 200 | Login needs device approval |
| `APPROVAL_TOKEN_INVALID` | 400 | Approval token invalid |
| `APPROVAL_TOKEN_EXPIRED` | 400 | Approval token expired |
| `APPROVAL_CODE_INVALID` | 400 | Wrong approval code |
| `APPROVAL_MAX_ATTEMPTS` | 429 | Too many wrong codes |
| `DEVICE_APPROVAL_DENIED` | 403 | Device was denied |

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
| Public endpoint? | `[RateLimit(10)]` |
| Sensitive (login, register, reset)? | `[RateLimit(5)]` |
| Auth-protected? | `[RateLimit]` (default 100/min) |

**Both Brute Force AND Rate Limiting are needed:**
- Brute Force: Protects individual accounts
- Rate Limiting: Protects API from spam/DoS

---

## Implementation Order

1. **Domain**: Entity erstellen
2. **Infrastructure**: Configuration + DbContext + Migration
3. **Application**: Commands/Queries + Handlers + Validators
4. **API**: Controller + Endpoints
5. **Tests**: Unit Tests
6. **Update this memory file**

---

## Last Updated
- **Date:** 2026-01-03
- **Tasks Completed:** 001-014 (303+ Unit Tests)

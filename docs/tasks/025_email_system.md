# Task 025: Email System Enhancement

## 1. Übersicht

**Was wird gebaut?**
Komplettes Email-System mit Multi-Provider Failover, Retry-Logik, Dead Letter Queue, Email-Logging und Admin-Funktionen.

**Warum?**
- Aktuell nur SMTP ohne Failover → wenn Provider down, gehen Emails verloren
- Keine Email-Historie → Admin kann nicht sehen was gesendet wurde
- Kein Resend für Password Reset / Device Approval
- Konfiguration nur in appsettings → erfordert Redeployment

## 2. Architektur

```
                              Email Queue (RabbitMQ)
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         EMAIL WORKER SERVICE                        │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    FAILOVER CHAIN                            │   │
│  │                                                              │   │
│  │  Provider 1 ──fail──▶ Provider 2 ──fail──▶ Provider 3 ──▶...│   │
│  │  (Priority 1)         (Priority 2)         (Priority 3)      │   │
│  │       │                    │                    │            │   │
│  │   Retry 1-3            Retry 1-3            Retry 1-3        │   │
│  │   (backoff)            (backoff)            (backoff)        │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                      │
│              All providers exhausted after retries                  │
│                              │                                      │
│                              ▼                                      │
│                   ┌─────────────────┐                              │
│                   │  Dead Letter    │                              │
│                   │     Queue       │                              │
│                   └─────────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
```

## 3. Unterstützte Email Provider

| Provider | Type | Configuration Fields |
|----------|------|---------------------|
| **SMTP** | smtp | Host, Port, Username, Password, UseSsl, FromEmail, FromName |
| **SendGrid** | sendgrid | ApiKey, FromEmail, FromName |
| **Mailgun** | mailgun | ApiKey, Domain, FromEmail, FromName, Region (EU/US) |
| **Amazon SES** | ses | AccessKey, SecretKey, Region, FromEmail, FromName |
| **Resend** | resend | ApiKey, FromEmail, FromName |
| **Postmark** | postmark | ServerToken, FromEmail, FromName |

## 4. User Stories

### Admin Stories
- Als Admin möchte ich mehrere Email-Provider konfigurieren können, damit Emails auch bei Provider-Ausfall ankommen
- Als Admin möchte ich die Email-Historie einsehen können, damit ich Zustellprobleme debuggen kann
- Als Admin möchte ich einen Test-Email senden können, damit ich prüfen kann ob die Konfiguration funktioniert
- Als Admin möchte ich Ankündigungen an alle/ausgewählte User senden können
- Als Admin möchte ich fehlgeschlagene Emails manuell erneut senden können (DLQ Retry)

### User Stories
- Als User möchte ich Password-Reset-Emails erneut anfordern können
- Als User möchte ich Device-Approval-Emails erneut anfordern können

## 5. Akzeptanzkriterien

- [x] Mindestens 2 Provider können parallel konfiguriert werden
- [x] Failover funktioniert automatisch bei Provider-Ausfall
- [x] Circuit Breaker verhindert Spam an toten Provider
- [x] Alle Emails werden in EmailLog protokolliert
- [x] DLQ fängt Emails nach erschöpften Retries
- [x] Admin kann Email-Historie filtern (wie Audit Logs)
- [x] Test-Email Funktion verfügbar
- [x] Resend für Password Reset funktioniert
- [x] Resend für Device Approval funktioniert
- [x] Ankündigungen können an User gesendet werden

## 6. Datenbank Entities

### EmailProvider Entity
```csharp
public sealed class EmailProvider : BaseEntity
{
    public string Name { get; private set; }                    // "Primary SendGrid", "Backup SMTP"
    public EmailProviderType Type { get; private set; }         // SMTP, SendGrid, Mailgun, SES, Resend, Postmark
    public int Priority { get; private set; }                   // 1 = primary, 2 = first fallback, etc.
    public bool IsEnabled { get; private set; }
    public string ConfigurationEncrypted { get; private set; }  // Encrypted JSON with provider-specific config
    
    // Circuit Breaker
    public int FailureCount { get; private set; }
    public DateTime? LastFailureAt { get; private set; }
    public DateTime? CircuitBreakerOpenUntil { get; private set; }
    
    // Stats
    public int TotalSent { get; private set; }
    public int TotalFailed { get; private set; }
    public DateTime? LastSuccessAt { get; private set; }
}
```

### EmailConfiguration Entity (Singleton - nur 1 Row)
```csharp
public sealed class EmailConfiguration : BaseEntity
{
    // Retry Settings
    public int MaxRetriesPerProvider { get; private set; }      // Default: 3
    public int InitialRetryDelayMs { get; private set; }        // Default: 1000 (1 sec)
    public int MaxRetryDelayMs { get; private set; }            // Default: 60000 (1 min)
    public double BackoffMultiplier { get; private set; }       // Default: 2.0
    
    // Circuit Breaker Settings
    public int CircuitBreakerFailureThreshold { get; private set; }  // Default: 5 failures
    public int CircuitBreakerWindowMinutes { get; private set; }     // Default: 10 min
    public int CircuitBreakerOpenDurationMinutes { get; private set; } // Default: 30 min
    
    // DLQ Settings
    public bool AutoRetryDlq { get; private set; }              // Default: false
    public int DlqRetryIntervalHours { get; private set; }      // Default: 6
    
    // General
    public bool EmailsEnabled { get; private set; }             // Global on/off switch
    public bool TestMode { get; private set; }                  // Log only, don't actually send
}
```

### EmailLog Entity
```csharp
public sealed class EmailLog : BaseEntity
{
    public Guid? RecipientUserId { get; private set; }          // Null for external recipients
    public string RecipientEmail { get; private set; }
    public string Subject { get; private set; }
    public string TemplateName { get; private set; }            // "password-reset", "system-invite", etc.
    public string? TemplateVariables { get; private set; }      // JSON (for debugging)
    public string Language { get; private set; }                // "en-US", "de-DE"
    
    // Status tracking
    public EmailStatus Status { get; private set; }             // Queued, Sending, Sent, Failed, InDlq, RetriedFromDlq
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public Guid? SentViaProviderId { get; private set; }        // Which provider succeeded
    
    // Timestamps
    public DateTime QueuedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? MovedToDlqAt { get; private set; }
    
    // For announcements
    public Guid? AnnouncementId { get; private set; }           // Link to EmailAnnouncement if applicable
    
    // Navigation
    public SystemUser? RecipientUser { get; private set; }
    public EmailProvider? SentViaProvider { get; private set; }
}
```

### EmailAnnouncement Entity (für Admin-Ankündigungen)
```csharp
public sealed class EmailAnnouncement : BaseEntity
{
    public string Subject { get; private set; }
    public string HtmlBody { get; private set; }
    public string? PlainTextBody { get; private set; }
    
    // Targeting
    public EmailAnnouncementTarget TargetType { get; private set; }  // AllUsers, ByPermission, SelectedUsers
    public string? TargetPermission { get; private set; }            // If ByPermission
    public string? TargetUserIds { get; private set; }               // JSON array if SelectedUsers
    
    // Stats
    public int TotalRecipients { get; private set; }
    public int SentCount { get; private set; }
    public int FailedCount { get; private set; }
    
    // Meta
    public Guid CreatedByUserId { get; private set; }
    public DateTime? SentAt { get; private set; }
    public EmailAnnouncementStatus Status { get; private set; }      // Draft, Sending, Sent, PartiallyFailed
    
    // Navigation
    public SystemUser CreatedByUser { get; private set; }
    public ICollection<EmailLog> EmailLogs { get; private set; }
}
```

## 7. Enums

```csharp
public enum EmailProviderType
{
    Smtp = 0,
    SendGrid = 1,
    Mailgun = 2,
    AmazonSes = 3,
    Resend = 4,
    Postmark = 5
}

public enum EmailStatus
{
    Queued = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3,
    InDlq = 4,
    RetriedFromDlq = 5
}

public enum EmailAnnouncementTarget
{
    AllUsers = 0,
    ByPermission = 1,
    SelectedUsers = 2
}

public enum EmailAnnouncementStatus
{
    Draft = 0,
    Sending = 1,
    Sent = 2,
    PartiallyFailed = 3
}
```

## 8. API Endpoints

### Email Providers (CRUD)
| Method | Route | Beschreibung |
|--------|-------|--------------|
| GET | `/api/system/email/providers` | List all providers (sorted by priority) |
| GET | `/api/system/email/providers/{id}` | Get provider details |
| POST | `/api/system/email/providers` | Create new provider |
| PUT | `/api/system/email/providers/{id}` | Update provider |
| DELETE | `/api/system/email/providers/{id}` | Delete provider |
| POST | `/api/system/email/providers/{id}/test` | Send test email via this provider |
| POST | `/api/system/email/providers/{id}/reset-circuit-breaker` | Manually reset circuit breaker |
| PUT | `/api/system/email/providers/reorder` | Reorder provider priorities |

### Email Configuration
| Method | Route | Beschreibung |
|--------|-------|--------------|
| GET | `/api/system/email/configuration` | Get email configuration |
| PUT | `/api/system/email/configuration` | Update email configuration |

### Email Logs (Read-only, like Audit Logs)
| Method | Route | Beschreibung |
|--------|-------|--------------|
| GET | `/api/system/email/logs` | List emails with filtering |
| GET | `/api/system/email/logs/{id}` | Get email details |
| GET | `/api/system/email/logs/filters` | Get filter options (templates, statuses) |

### Dead Letter Queue
| Method | Route | Beschreibung |
|--------|-------|--------------|
| GET | `/api/system/email/dlq` | List emails in DLQ |
| POST | `/api/system/email/dlq/{id}/retry` | Retry single email from DLQ |
| POST | `/api/system/email/dlq/retry-all` | Retry all emails in DLQ |
| DELETE | `/api/system/email/dlq/{id}` | Remove from DLQ (give up) |

### Announcements
| Method | Route | Beschreibung |
|--------|-------|--------------|
| GET | `/api/system/email/announcements` | List announcements |
| GET | `/api/system/email/announcements/{id}` | Get announcement details |
| POST | `/api/system/email/announcements` | Create announcement (draft) |
| PUT | `/api/system/email/announcements/{id}` | Update draft announcement |
| POST | `/api/system/email/announcements/{id}/send` | Send announcement |
| DELETE | `/api/system/email/announcements/{id}` | Delete draft announcement |
| POST | `/api/system/email/announcements/preview` | Preview announcement email |

### Resend Existing Emails
| Method | Route | Beschreibung |
|--------|-------|--------------|
| POST | `/api/auth/forgot-password/resend` | Resend password reset email |
| POST | `/api/auth/device-approval/resend` | Resend device approval email |

### Test Email
| Method | Route | Beschreibung |
|--------|-------|--------------|
| POST | `/api/system/email/test` | Send test email (uses failover chain) |

## 9. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `EMAIL_PROVIDER_NOT_FOUND` | 404 | Email provider not found |
| `EMAIL_PROVIDER_INVALID_CONFIG` | 400 | Invalid provider configuration |
| `EMAIL_PROVIDER_TEST_FAILED` | 400 | Test email failed to send |
| `EMAIL_NO_PROVIDERS_CONFIGURED` | 400 | No email providers configured |
| `EMAIL_ALL_PROVIDERS_FAILED` | 500 | All providers failed (email in DLQ) |
| `EMAIL_LOG_NOT_FOUND` | 404 | Email log not found |
| `EMAIL_NOT_IN_DLQ` | 400 | Email not in dead letter queue |
| `EMAIL_ANNOUNCEMENT_NOT_FOUND` | 404 | Announcement not found |
| `EMAIL_ANNOUNCEMENT_ALREADY_SENT` | 400 | Cannot modify sent announcement |
| `EMAIL_ANNOUNCEMENT_NO_RECIPIENTS` | 400 | No recipients match criteria |
| `PASSWORD_RESET_RESEND_COOLDOWN` | 429 | Wait before requesting another reset |
| `DEVICE_APPROVAL_RESEND_COOLDOWN` | 429 | Wait before requesting another approval email |
| `DEVICE_APPROVAL_NOT_PENDING` | 400 | No pending device approval to resend |

## 10. Neue Permissions

| Permission | Beschreibung |
|------------|--------------|
| `email:providers:read` | View email providers |
| `email:providers:manage` | Create/update/delete providers |
| `email:config:read` | View email configuration |
| `email:config:manage` | Update email configuration |
| `email:logs:read` | View email logs |
| `email:dlq:manage` | Retry/delete from DLQ |
| `email:announcements:read` | View announcements |
| `email:announcements:manage` | Create/send announcements |
| `email:test` | Send test emails |

## 11. Files zu erstellen

### Domain Layer
| Datei | Beschreibung |
|-------|--------------|
| `Entities/EmailProvider.cs` | Email provider entity |
| `Entities/EmailConfiguration.cs` | Email configuration entity |
| `Entities/EmailLog.cs` | Email log entity |
| `Entities/EmailAnnouncement.cs` | Announcement entity |
| `Enums/EmailProviderType.cs` | Provider type enum |
| `Enums/EmailStatus.cs` | Email status enum |
| `Enums/EmailAnnouncementTarget.cs` | Announcement target enum |
| `Enums/EmailAnnouncementStatus.cs` | Announcement status enum |

### Application Layer
| Datei | Beschreibung |
|-------|--------------|
| `Common/Interfaces/IEmailProviderFactory.cs` | Factory for creating provider instances |
| `Common/Interfaces/IEmailSendingService.cs` | Orchestrates sending with failover |
| `Common/Interfaces/IEmailLogService.cs` | Service for logging emails |
| `Common/Interfaces/ICircuitBreakerService.cs` | Circuit breaker management |
| `Common/Models/EmailProviderConfig.cs` | Base config + provider-specific configs |
| `Features/Email/Providers/Commands/...` | CRUD commands |
| `Features/Email/Providers/Queries/...` | List/Get queries |
| `Features/Email/Configuration/Commands/...` | Update config command |
| `Features/Email/Configuration/Queries/...` | Get config query |
| `Features/Email/Logs/Queries/...` | List/Get/Filters queries |
| `Features/Email/Dlq/Commands/...` | Retry/Delete commands |
| `Features/Email/Announcements/Commands/...` | CRUD + Send commands |
| `Features/Email/Announcements/Queries/...` | List/Get queries |
| `Features/Email/Test/Commands/...` | Test email command |
| `Features/Auth/Commands/ResendPasswordReset/...` | Resend password reset |
| `Features/Auth/Commands/ResendDeviceApproval/...` | Resend device approval |

### Infrastructure Layer
| Datei | Beschreibung |
|-------|--------------|
| `Persistence/Configurations/EmailProviderConfiguration.cs` | EF config |
| `Persistence/Configurations/EmailConfigurationConfiguration.cs` | EF config |
| `Persistence/Configurations/EmailLogConfiguration.cs` | EF config |
| `Persistence/Configurations/EmailAnnouncementConfiguration.cs` | EF config |
| `Services/Email/EmailProviderFactory.cs` | Creates provider instances |
| `Services/Email/EmailSendingService.cs` | Failover orchestration |
| `Services/Email/EmailLogService.cs` | Logging service |
| `Services/Email/CircuitBreakerService.cs` | Circuit breaker |
| `Services/Email/Providers/SmtpEmailProvider.cs` | SMTP implementation |
| `Services/Email/Providers/SendGridEmailProvider.cs` | SendGrid implementation |
| `Services/Email/Providers/MailgunEmailProvider.cs` | Mailgun implementation |
| `Services/Email/Providers/AmazonSesEmailProvider.cs` | SES implementation |
| `Services/Email/Providers/ResendEmailProvider.cs` | Resend implementation |
| `Services/Email/Providers/PostmarkEmailProvider.cs` | Postmark implementation |
| `Services/Email/Providers/IEmailProviderImplementation.cs` | Provider interface |

### API Layer
| Datei | Beschreibung |
|-------|--------------|
| `Controllers/EmailController.cs` | All email endpoints |

### EmailWorker Changes
| Datei | Beschreibung |
|-------|--------------|
| `Consumers/SendEmailConsumer.cs` | Update to use new failover logic |
| `Services/DlqProcessor.cs` | DLQ auto-retry processor |

## 12. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `AppDbContext.cs` | Add DbSets for new entities |
| `DependencyInjection.cs` | Register new services |
| `SystemPermissions.cs` | Add new permissions |
| `ErrorCodes.cs` | Add new error codes |
| Migration | Add new tables |

## 13. Neue Packages

| Package | Projekt | Warum? |
|---------|---------|--------|
| `SendGrid` | EmailWorker | SendGrid API client |
| `RestSharp` or `Mailgun.Api` | EmailWorker | Mailgun API (or use HttpClient) |
| `AWSSDK.SimpleEmail` | EmailWorker | Amazon SES |
| `Resend` | EmailWorker | Resend API client |
| `Postmark` | EmailWorker | Postmark API client |

## 14. Implementation Reihenfolge

### Phase 1: Core Infrastructure
1. [x] **Domain**: Entities + Enums erstellen
2. [x] **Infrastructure**: EF Configurations + Migration
3. [x] **Infrastructure**: Provider implementations (start with SMTP + SendGrid)
4. [x] **Infrastructure**: EmailProviderFactory + EmailSendingService
5. [x] **Infrastructure**: CircuitBreakerService
6. [x] **Infrastructure**: EmailLogService
7. [x] **EmailWorker**: Update SendEmailConsumer with failover logic
8. [x] **EmailWorker**: Add DLQ handling

### Phase 2: Admin API
9. [x] **Application**: Provider CRUD Commands/Queries
10. [x] **Application**: Configuration Commands/Queries
11. [x] **Application**: EmailLogs Queries (like Audit)
12. [x] **Application**: DLQ Commands
13. [x] **Application**: Test Email Command
14. [x] **API**: EmailController

### Phase 3: Announcements
15. [x] **Application**: Announcement CRUD Commands/Queries
16. [x] **Application**: Send Announcement Command
17. [x] **API**: Announcement endpoints

### Phase 4: Resend Features
18. [x] **Application**: ResendPasswordReset Command
19. [x] **Application**: ResendDeviceApproval Command
20. [x] **API**: Resend endpoints

### Phase 5: Additional Providers
21. [x] **Infrastructure**: Mailgun provider
22. [x] **Infrastructure**: Amazon SES provider
23. [x] **Infrastructure**: Resend provider
24. [x] **Infrastructure**: Postmark provider

### Phase 6: Testing & Cleanup
25. [x] **Tests**: Unit Tests für alle Handler
26. [ ] **Tests**: Integration Tests für Failover
27. [x] **Permissions**: Seed neue Permissions (already in SystemPermissions.cs)
28. [x] **Memory**: backend_reference.md aktualisieren

## 15. Circuit Breaker Logic

```csharp
// Pseudocode
public async Task<bool> CanUseProvider(EmailProvider provider)
{
    // If circuit breaker is open, check if it should close
    if (provider.CircuitBreakerOpenUntil.HasValue)
    {
        if (DateTime.UtcNow < provider.CircuitBreakerOpenUntil)
            return false; // Still open, skip this provider
        
        // Time to try again (half-open state)
        await ResetCircuitBreaker(provider);
    }
    
    return provider.IsEnabled;
}

public async Task RecordFailure(EmailProvider provider)
{
    provider.FailureCount++;
    provider.LastFailureAt = DateTime.UtcNow;
    
    // Check if we should open the circuit
    var recentFailures = await CountFailuresInWindow(provider, _config.CircuitBreakerWindowMinutes);
    
    if (recentFailures >= _config.CircuitBreakerFailureThreshold)
    {
        provider.CircuitBreakerOpenUntil = DateTime.UtcNow.AddMinutes(_config.CircuitBreakerOpenDurationMinutes);
        _logger.LogWarning("Circuit breaker opened for provider {Provider} until {Until}", 
            provider.Name, provider.CircuitBreakerOpenUntil);
    }
}

public async Task RecordSuccess(EmailProvider provider)
{
    provider.FailureCount = 0;
    provider.LastSuccessAt = DateTime.UtcNow;
    provider.TotalSent++;
    provider.CircuitBreakerOpenUntil = null; // Close circuit
}
```

## 16. Failover + Retry Logic

```csharp
// Pseudocode
public async Task<EmailResult> SendWithFailover(EmailMessage message)
{
    var providers = await GetEnabledProvidersByPriority();
    var emailLog = await CreateEmailLog(message, EmailStatus.Sending);
    
    foreach (var provider in providers)
    {
        if (!await _circuitBreaker.CanUseProvider(provider))
            continue;
        
        for (int retry = 0; retry <= _config.MaxRetriesPerProvider; retry++)
        {
            try
            {
                await SendViaProvider(provider, message);
                
                await _circuitBreaker.RecordSuccess(provider);
                await UpdateEmailLog(emailLog, EmailStatus.Sent, provider.Id);
                
                return EmailResult.Success(provider.Id);
            }
            catch (Exception ex)
            {
                emailLog.RetryCount++;
                emailLog.LastError = ex.Message;
                
                if (retry < _config.MaxRetriesPerProvider)
                {
                    var delay = CalculateBackoff(retry);
                    await Task.Delay(delay);
                }
            }
        }
        
        // Provider exhausted, record failure and try next
        await _circuitBreaker.RecordFailure(provider);
    }
    
    // All providers failed - move to DLQ
    await UpdateEmailLog(emailLog, EmailStatus.InDlq);
    await MoveToDeadLetterQueue(message, emailLog);
    
    return EmailResult.Failed("All providers exhausted");
}

private int CalculateBackoff(int retryAttempt)
{
    var delay = _config.InitialRetryDelayMs * Math.Pow(_config.BackoffMultiplier, retryAttempt);
    return Math.Min((int)delay, _config.MaxRetryDelayMs);
}
```

## 17. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `Features/Email/Providers/CreateEmailProviderHandlerTests.cs` | Provider creation with all types | 9 |
| `Features/Email/Providers/UpdateEmailProviderHandlerTests.cs` | Provider updates | 8 |
| `Features/Email/Providers/DeleteEmailProviderHandlerTests.cs` | Provider deletion | 5 |
| `Features/Email/Providers/ResetCircuitBreakerHandlerTests.cs` | Circuit breaker reset | 5 |
| `Features/Email/Configuration/EmailConfigurationHandlerTests.cs` | Config get/update | 12 |
| `Features/Email/Logs/EmailLogsHandlerTests.cs` | Log retrieval and queries | 11 |
| `Features/Email/Dlq/DlqHandlerTests.cs` | DLQ retry, delete, queries | 15 |
| `Features/Email/Announcements/EmailAnnouncementHandlerTests.cs` | Announcement CRUD + send | 23 |
| `Features/Email/Test/SendTestEmailHandlerTests.cs` | Test email with failover | 10 |
| `Features/Auth/Resend/ResendDeviceApprovalHandlerTests.cs` | Resend device approval | 10 |

**Total Email Tests:** ~108 tests

## 18. Nach Completion

- [x] Alle Unit Tests grün (584 total, 134 email-related)
- [x] `backend_reference.md` Memory aktualisiert
- [x] Neue Permissions geseeded (already in SystemPermissions.cs)
- [x] Neue Error Codes dokumentiert
- [x] Migration läuft fehlerfrei
- [x] GDPR: Email logs anonymized when user is anonymized

## 19. GDPR Compliance

When a user is anonymized (via `AnonymizeUserHandler`), all their email logs are also anonymized:
- `RecipientEmail` → `anonymized@anonymized.local`
- `TemplateVariables` → `null` (removes personal data from template)
- `RecipientUserId` → `null` (unlinks from user)

The `EmailLog.Anonymize()` method handles this, and `AnonymizeUserHandlerTests` has 2 tests verifying this behavior.

## 20. Letzte Änderung

- **Datum:** 2026-01-08
- **Status:** ✅ Complete
- **Notes:** All unit tests implemented and passing. GDPR compliance added for email log anonymization.

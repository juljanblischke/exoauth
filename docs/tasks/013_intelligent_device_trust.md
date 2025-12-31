# Task 013: Intelligent Device Trust (Risk-Based Authentication)

## 1. Übersicht

**Was wird gebaut?**
Risk-based authentication mit Point-System für Device Trust. Neue/verdächtige Geräte müssen erst verifiziert werden bevor Login erlaubt ist.

**Warum?**
Aktuell ist MFA der einzige Schutzmechanismus. Email-Benachrichtigungen über neue Geräte kommen NACH dem Login - zu spät wenn ein Angreifer bereits Zugriff hat. Mit Device Trust muss ein neues Gerät erst genehmigt werden BEVOR der Login abgeschlossen wird.

## 2. User Experience / Anforderungen

### User Stories

- Als User möchte ich von verdächtigen Logins geschützt werden, damit Angreifer nicht einfach mit gestohlenen Credentials reinkommen
- Als User möchte ich neue Geräte über meine bestehende Session genehmigen können, damit ich keine Email brauche
- Als User möchte ich alternativ per Email-Link/Code genehmigen können, falls keine aktive Session existiert
- Als Admin möchte ich Risk-Scores konfigurieren können, damit ich die Sicherheit an meine Anforderungen anpassen kann

### Risk Scoring System

| Faktor | Punkte | Beispiel |
|--------|--------|----------|
| Neues Gerät | +20 | DeviceId noch nie gesehen |
| Neues Land | +40 | User normalerweise aus Deutschland, jetzt aus Russland |
| Neue Stadt (gleiches Land) | +10 | User normalerweise in Berlin, jetzt in München |
| Impossible Travel | +80 | Vor 5 min aus Deutschland, jetzt aus USA |
| VPN/Proxy IP | +30 | IP als Datacenter/VPN erkannt |
| Ungewöhnliche Zeit | +15 | Login um 3 Uhr nachts, User loggt normalerweise 9-18 Uhr ein |
| Tor Exit Node | +50 | IP ist bekannter Tor Exit Node |
| Anderer Gerätetyp | +10 | Normalerweise Mobile, jetzt Desktop |

### Risk Thresholds

| Score | Risk Level | Aktion |
|-------|------------|--------|
| 0-30 | Low | Login + Email-Benachrichtigung (wie bisher) |
| 31-60 | Medium | DeviceSession erstellen (IsTrusted=false), Approval required |
| 61+ | High | DeviceSession erstellen (IsTrusted=false), Approval required |

### Flows

#### Low Risk (0-30 Punkte)
```
Login → Risk Score: 25 → MFA OK → ✅ Login Success → 📧 Info-Email
```
Wie bisher, keine Änderung.

#### Medium/High Risk (31+ Punkte)
```
Login → Risk Score: 60 → MFA OK → DeviceSession created (IsTrusted=false)
                                → 📧 Approval Email mit Token + Code
                                → Response: { requiresDeviceApproval: true, approvalToken: "..." }

User hat 2 Optionen:

OPTION A: Approve via existing Session
  → Eingeloggt auf anderem Gerät
  → Settings → Sessions → Sieht neues Gerät (Pending)
  → Klickt "Trust" → IsTrusted = true
  → Neues Gerät: Retry Login → ✅ Success

OPTION B: Approve via Email
  → Klickt Link in Email → IsTrusted = true → Redirect zu Login
  → ODER: Gibt XXXX-XXXX Code ein → IsTrusted = true
  → Retry Login → ✅ Success
```

### Akzeptanzkriterien

- [ ] Risk Score wird bei jedem Login berechnet
- [ ] Bei Score > 30: DeviceSession mit IsTrusted=false erstellt
- [ ] Bei Score > 30: Email mit Approval-Link und Code gesendet
- [ ] User kann Gerät über bestehende Session trusten (existiert bereits!)
- [ ] User kann Gerät über Email-Link trusten
- [ ] User kann Gerät über XXXX-XXXX Code trusten
- [ ] Login schlägt fehl wenn IsTrusted=false
- [ ] LoginPattern wird nach jedem erfolgreichen Login aktualisiert
- [ ] Trusted Devices reduzieren Risk Score (-30 Punkte)
- [ ] Alle Risk-Scores sind konfigurierbar
- [ ] Feature ist per Config ein/ausschaltbar

### Edge Cases / Error Handling

- Was passiert wenn User KEIN anderes Gerät eingeloggt hat? → Email ist einzige Option
- Was passiert wenn erster Login ever (Registration)? → Auto-trusted (wie bisher)
- Was passiert wenn Approval abläuft? → User muss neuen Login-Versuch starten
- Was passiert bei zu vielen falschen Code-Eingaben? → Approval invalidiert, neuer Login nötig
- Was passiert wenn User aus normalem Land mit VPN einloggt? → Risk Score addiert sich

## 3. API Endpoints

### Neue Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| POST | `/api/auth/approve-device` | `{ approvalToken, code }` | `{ success: true }` | Code-basierte Approval |
| GET | `/api/auth/approve-device/{token}` | - | Redirect oder JSON | Email-Link Approval |
| POST | `/api/auth/deny-device` | `{ approvalToken }` | `{ success: true }` | Gerät ablehnen + Security Alert |

### Geänderte Endpoints

| Method | Route | Änderung |
|--------|-------|----------|
| POST | `/api/auth/login` | Response erweitert um `requiresDeviceApproval`, `approvalToken` |
| GET | `/api/auth/sessions` | Zeigt auch pending (IsTrusted=false) Sessions |

### Login Response Erweiterung

```json
// Erfolgreicher Login (low risk ODER trusted device)
{
  "accessToken": "...",
  "refreshToken": "...",
  "user": { ... }
}

// Device Approval Required (medium/high risk, untrusted device)
{
  "requiresDeviceApproval": true,
  "approvalToken": "abc123...",
  "riskScore": 60,
  "riskFactors": ["new_device", "new_country"],
  "message": "Please approve this device via email or from another session"
}
```

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `DEVICE_APPROVAL_REQUIRED` | 200 | Login benötigt Device Approval (kein Error, spezieller Response) |
| `DEVICE_NOT_TRUSTED` | 403 | Login-Versuch mit nicht-vertrautem Gerät |
| `APPROVAL_TOKEN_INVALID` | 400 | Approval Token ungültig oder abgelaufen |
| `APPROVAL_TOKEN_EXPIRED` | 400 | Approval Token abgelaufen |
| `APPROVAL_CODE_INVALID` | 400 | XXXX-XXXX Code falsch |
| `APPROVAL_MAX_ATTEMPTS` | 429 | Zu viele falsche Code-Versuche |
| `DEVICE_APPROVAL_DENIED` | 403 | Gerät wurde vom User abgelehnt |

> ⚠️ **Nach Completion:** Diese Codes zu `coding_standards_backend.md` (Error Codes Tabelle) hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities

| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| DeviceApprovalRequest | Id, UserId, DeviceSessionId, TokenHash, CodeHash, RiskScore, RiskFactors (JSON), Status, Attempts, ExpiresAt, ResolvedAt, ResolvedBy | User, DeviceSession |
| LoginPattern | Id, UserId, TypicalCountries[], TypicalCities[], TypicalHours[], TypicalDeviceTypes[], LastLoginAt, LastIpAddress, LastCountry | User |

### DeviceApprovalRequest Details

```csharp
public sealed class DeviceApprovalRequest : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid DeviceSessionId { get; private set; }
    public string TokenHash { get; private set; }      // SHA256 hash of URL token
    public string CodeHash { get; private set; }       // SHA256 hash of XXXX-XXXX code
    public int RiskScore { get; private set; }
    public string RiskFactors { get; private set; }    // JSON array: ["new_device", "new_country"]
    public ApprovalStatus Status { get; private set; } // Pending, Approved, Denied, Expired
    public int Attempts { get; private set; }          // Failed code attempts
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }    // "email_link", "email_code", "session_trust", "timeout"

    // Navigation
    public SystemUser? User { get; set; }
    public DeviceSession? DeviceSession { get; set; }

    // Methods (like PasswordResetToken)
    public static string GenerateToken() { ... }       // 32-byte URL-safe
    public static string GenerateCode() { ... }        // XXXX-XXXX format
    public bool ValidateToken(string token) { ... }
    public bool ValidateCode(string code) { ... }
    public void MarkApproved(string resolvedBy) { ... }
    public void MarkDenied() { ... }
    public void IncrementAttempts() { ... }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Denied,
    Expired
}
```

### LoginPattern Details

```csharp
public sealed class LoginPattern : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TypicalCountries { get; private set; }     // JSON array: ["DE", "AT"]
    public string TypicalCities { get; private set; }        // JSON array: ["Berlin", "Munich"]
    public string TypicalHours { get; private set; }         // JSON array: [9, 10, 11, ..., 18]
    public string TypicalDeviceTypes { get; private set; }   // JSON array: ["Desktop", "Mobile"]
    public DateTime? LastLoginAt { get; private set; }
    public string? LastIpAddress { get; private set; }
    public string? LastCountry { get; private set; }
    public double? LastLatitude { get; private set; }
    public double? LastLongitude { get; private set; }

    // Navigation
    public SystemUser? User { get; set; }

    // Methods
    public void RecordLogin(string country, string city, int hour, string deviceType, string ip, double? lat, double? lng) { ... }
    public bool IsTypicalCountry(string country) { ... }
    public bool IsTypicalCity(string city) { ... }
    public bool IsTypicalHour(int hour) { ... }
    public bool IsTypicalDeviceType(string deviceType) { ... }
    public double? CalculateDistanceKm(double lat, double lng) { ... }  // For impossible travel
}
```

### Migrations

- [ ] Migration Name: `AddDeviceApprovalRequests`
- [ ] Migration Name: `AddLoginPatterns`

## 6. Files zu erstellen

### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| DeviceApprovalRequest.cs | `src/ExoAuth.Domain/Entities/DeviceApprovalRequest.cs` | Approval Request Entity |
| LoginPattern.cs | `src/ExoAuth.Domain/Entities/LoginPattern.cs` | User Login Pattern Entity |
| ApprovalStatus.cs | `src/ExoAuth.Domain/Enums/ApprovalStatus.cs` | Enum für Approval Status |

### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| IRiskScoringService.cs | `src/ExoAuth.Application/Common/Interfaces/IRiskScoringService.cs` | Risk Score Berechnung |
| ILoginPatternService.cs | `src/ExoAuth.Application/Common/Interfaces/ILoginPatternService.cs` | Pattern Tracking |
| IDeviceApprovalService.cs | `src/ExoAuth.Application/Common/Interfaces/IDeviceApprovalService.cs` | Approval Handling |
| RiskScore.cs | `src/ExoAuth.Application/Common/Models/RiskScore.cs` | Risk Score Result Model |
| ApproveDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDevice/ApproveDeviceCommand.cs` | Code-based Approval |
| ApproveDeviceHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDevice/ApproveDeviceHandler.cs` | Handler |
| ApproveDeviceValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDevice/ApproveDeviceValidator.cs` | Validator |
| ApproveDeviceLinkCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDeviceLink/ApproveDeviceLinkCommand.cs` | Link-based Approval |
| ApproveDeviceLinkHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/ApproveDeviceLink/ApproveDeviceLinkHandler.cs` | Handler |
| DenyDeviceCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/DenyDevice/DenyDeviceCommand.cs` | Deny Device |
| DenyDeviceHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/DenyDevice/DenyDeviceHandler.cs` | Handler |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| RiskScoringService.cs | `src/ExoAuth.Infrastructure/Services/RiskScoringService.cs` | Risk Score Implementierung |
| LoginPatternService.cs | `src/ExoAuth.Infrastructure/Services/LoginPatternService.cs` | Pattern Tracking Implementierung |
| DeviceApprovalService.cs | `src/ExoAuth.Infrastructure/Services/DeviceApprovalService.cs` | Approval Handling (wie PasswordResetService) |
| DeviceApprovalRequestConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/DeviceApprovalRequestConfiguration.cs` | EF Config |
| LoginPatternConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/LoginPatternConfiguration.cs` | EF Config |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| - | `src/ExoAuth.Api/Controllers/AuthController.cs` | Neue Endpoints hinzufügen |

### Email Templates

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| device-approval-required.html | `templates/emails/en-US/device-approval-required.html` | Approval Email (EN) |
| device-approval-required.html | `templates/emails/de-DE/device-approval-required.html` | Approval Email (DE) |
| device-denied-alert.html | `templates/emails/en-US/device-denied-alert.html` | Security Alert (EN) |
| device-denied-alert.html | `templates/emails/de-DE/device-denied-alert.html` | Security Alert (DE) |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Application/Common/Interfaces/IAppDbContext.cs` | DbSets für DeviceApprovalRequests, LoginPatterns |
| `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | Neue Error Codes |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginHandler.cs` | Risk Score Check, Approval Flow |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginCommand.cs` | Response erweitern |
| `src/ExoAuth.Application/Features/Auth/Models/AuthResponse.cs` | Approval Response Felder |
| `src/ExoAuth.Application/Features/Auth/Commands/UpdateSession/UpdateSessionHandler.cs` | Bei Trust: Approval resolven |
| `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | DbSets hinzufügen |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Neue Services registrieren |
| `src/ExoAuth.Api/Controllers/AuthController.cs` | Neue Endpoints |
| `src/ExoAuth.Api/appsettings.json` | DeviceTrust Config Section |
| `templates/emails/en-US/subjects.json` | Email Subjects |
| `templates/emails/de-DE/subjects.json` | Email Subjects |

## 8. Neue Packages

Keine neuen Packages erforderlich.

## 9. Configuration

### appsettings.json

```json
{
  "DeviceTrust": {
    "Enabled": true,
    "Thresholds": {
      "Medium": 31,
      "High": 61
    },
    "Scores": {
      "NewDevice": 20,
      "NewCountry": 40,
      "NewCity": 10,
      "ImpossibleTravel": 80,
      "VpnProxy": 30,
      "UnusualTime": 15,
      "TorExitNode": 50,
      "DifferentDeviceType": 10
    },
    "TrustedDeviceReduction": -30,
    "ImpossibleTravelSpeedKmh": 800,
    "ApprovalExpiryMinutes": 30,
    "MaxCodeAttempts": 3,
    "PatternHistoryDays": 90
  }
}
```

## 10. Implementation Reihenfolge

1. [x] **Domain**: Entities erstellen (DeviceApprovalRequest, LoginPattern, ApprovalStatus)
2. [x] **Infrastructure**: Configurations + DbContext + Migration
3. [x] **Application**: Interfaces (IRiskScoringService, ILoginPatternService, IDeviceApprovalService) + RiskScore Model
4. [ ] **Infrastructure**: Services implementieren
   - [ ] RiskScoringService (Point-System)
   - [ ] LoginPatternService (Pattern Tracking)
   - [ ] DeviceApprovalService (Token/Code Generation, Validation mit Retry)
5. [ ] **Application**: Commands/Handlers
   - [ ] ApproveDevice (Code-based)
   - [ ] ApproveDeviceLink (Token-based)
   - [ ] DenyDevice
6. [ ] **Application**: LoginHandler modifizieren (Risk Check, Approval Flow)
7. [ ] **Application**: UpdateSessionHandler erweitern (Trust → Approval resolven)
8. [ ] **API**: Neue Endpoints in AuthController
9. [ ] **Email**: Templates erstellen (EN/DE)
10. [ ] **Config**: appsettings.json erweitern
11. [ ] **Tests**: Unit Tests schreiben
12. [ ] **Standards updaten**: task_standards_backend.md + coding_standards_backend.md aktualisieren

## 11. Tests

### Unit Tests

| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Services/RiskScoringServiceTests.cs` | Risk Score Calculation | ~15 |
| `tests/ExoAuth.UnitTests/Services/LoginPatternServiceTests.cs` | Pattern Tracking | ~10 |
| `tests/ExoAuth.UnitTests/Services/DeviceApprovalServiceTests.cs` | Token/Code Generation, Validation, Retry | ~12 |
| `tests/ExoAuth.UnitTests/Features/Auth/ApproveDeviceHandlerTests.cs` | Code Approval | ~8 |
| `tests/ExoAuth.UnitTests/Features/Auth/ApproveDeviceLinkHandlerTests.cs` | Link Approval | ~6 |
| `tests/ExoAuth.UnitTests/Features/Auth/DenyDeviceHandlerTests.cs` | Deny Flow | ~5 |
| `tests/ExoAuth.UnitTests/Features/Auth/LoginHandlerRiskTests.cs` | Risk-based Login Flow | ~15 |

### Test Scenarios

**RiskScoringService:**
- New device only → 20 points
- New device + new country → 60 points
- Impossible travel → 80+ points
- Trusted device → Score reduction
- All factors combined → Correct sum
- Threshold classification (Low/Medium/High)

**DeviceApprovalService:**
- Generate token (32 bytes, URL-safe)
- Generate code (XXXX-XXXX format)
- Validate token correctly
- Validate code correctly
- Reject wrong code
- Increment attempts on failure
- Max attempts reached → Rejection
- Expired token → Rejection
- Retry on collision (like PasswordResetService)

**LoginHandler:**
- Low risk → Normal login
- Medium risk → Approval required response
- High risk → Approval required response
- Trusted device → Lower risk score
- First device (registration) → Auto-trusted
- Retry after approval → Success

## 12. Wichtige Implementation Details

### DeviceApprovalService Pattern (wie PasswordResetService)

```csharp
public sealed class DeviceApprovalService : IDeviceApprovalService
{
    private const int MaxRetries = 3;

    public async Task<DeviceApprovalResult> CreateApprovalRequestAsync(
        Guid userId,
        Guid deviceSessionId,
        int riskScore,
        List<string> riskFactors,
        CancellationToken ct)
    {
        // Invalidate existing pending requests for this device
        await InvalidatePendingRequestsAsync(deviceSessionId, ct);

        // Generate with collision prevention
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = DeviceApprovalRequest.GenerateToken();
            var code = DeviceApprovalRequest.GenerateCode();

            var tokenHash = HashForCheck(token);
            var exists = await _context.DeviceApprovalRequests
                .AnyAsync(x => x.TokenHash == tokenHash, ct);

            if (exists)
            {
                _logger.LogWarning("Token collision on attempt {Attempt}", attempt + 1);
                continue;
            }

            var entity = DeviceApprovalRequest.Create(
                userId, deviceSessionId, token, code,
                riskScore, riskFactors, _expirationMinutes);

            await _context.DeviceApprovalRequests.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            return new DeviceApprovalResult(entity, token, code);
        }

        throw new InvalidOperationException("Failed to generate unique token");
    }
}
```

### Login Flow Integration

```csharp
// In LoginHandler.Handle()

// After MFA verification...
var riskScore = await _riskScoringService.CalculateAsync(user.Id, deviceInfo, geoLocation, ct);

if (riskScore.Level == RiskLevel.Low)
{
    // Normal flow - create session and return tokens
    var session = await _deviceSessionService.CreateOrUpdateSessionAsync(...);
    session.SetTrusted(); // Auto-trust for low risk
    // ... return tokens
}
else
{
    // Medium/High risk - require approval
    var session = await _deviceSessionService.CreateOrUpdateSessionAsync(...);
    // session.IsTrusted = false (default)

    var approval = await _deviceApprovalService.CreateApprovalRequestAsync(
        user.Id, session.Id, riskScore.Score, riskScore.Factors, ct);

    // Send approval email
    await _emailService.SendDeviceApprovalEmailAsync(user, approval, deviceInfo, geoLocation);

    return new LoginResponse
    {
        RequiresDeviceApproval = true,
        ApprovalToken = approval.Token,
        RiskScore = riskScore.Score,
        RiskFactors = riskScore.Factors,
        Message = "Please approve this device via email or from another session"
    };
}
```

### UpdateSessionHandler Integration

```csharp
// When user trusts a device from existing session
if (command.IsTrusted == true)
{
    await _sessionService.SetTrustStatusAsync(command.SessionId, true, ct);

    // Also resolve any pending approval request for this session
    await _deviceApprovalService.ResolveBySessionTrustAsync(command.SessionId, ct);
}
```

## 13. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `task_standards_backend.md` aktualisiert (File Tree, neue Entities/Services)
- [ ] `coding_standards_backend.md` aktualisiert (neue Error Codes)
- [ ] Code reviewed
- [ ] Feature mit `DeviceTrust:Enabled = false` getestet (Fallback auf altes Verhalten)
- [ ] Feature mit `DeviceTrust:Enabled = true` getestet

## 14. Offene Fragen / Entscheidungen

1. **VPN/Proxy Detection**: Brauchen wir externe API (IPQualityScore, MaxMind minFraud) oder reicht erstmal ohne?
   - Empfehlung: Erstmal ohne, als Future Enhancement markieren

2. **Tor Exit Node Detection**: Eigene Liste pflegen oder externe API?
   - Empfehlung: Erstmal ohne, als Future Enhancement markieren

3. **Frontend**: Wird in separatem Task gemacht?
   - Empfehlung: Ja, Task 014 für Frontend

## 15. Future Enhancements (nicht in diesem Task)

- [ ] VPN/Proxy IP Detection (externe API Integration)
- [ ] Tor Exit Node Detection
- [ ] Behavioral Analysis (Typing patterns, mouse movement)
- [ ] Device Fingerprinting (Canvas, WebGL, etc.)
- [ ] Push Notifications für Approval (statt nur Email)
- [ ] Admin Override für Device Approval

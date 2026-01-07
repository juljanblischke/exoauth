# Task 021: CAPTCHA Integration ✅

## 1. Übersicht

**Was wird gebaut?**
Pluggable CAPTCHA system mit Cloudflare Turnstile als Default-Provider. Smart triggering für optimale UX.

**Warum?**
- Bot-Protection für öffentliche Endpoints (Registration, Forgot Password)
- Zusätzliche Sicherheitsebene bei verdächtigen Login-Versuchen
- Commercial-ready: Kunden können Provider wählen (Turnstile, reCAPTCHA, hCaptcha) oder deaktivieren

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich mich registrieren können ohne nervige CAPTCHAs wenn ich legitim bin
- Als User möchte ich bei Forgot-Password ein unsichtbares CAPTCHA haben das Bots blockt
- Als Admin möchte ich den CAPTCHA-Provider konfigurieren können
- Als Enterprise-Kunde möchte ich CAPTCHA deaktivieren können (eigene WAF)

### Akzeptanzkriterien
- [x] Registration erfordert CAPTCHA (Turnstile widget)
- [x] Forgot Password erfordert CAPTCHA
- [x] Login erfordert CAPTCHA erst nach 2 fehlgeschlagenen Versuchen ODER bei hohem Risk Score
- [x] Device Approval Code erfordert CAPTCHA nach 2 fehlgeschlagenen Versuchen
- [x] Provider ist konfigurierbar (Turnstile/reCAPTCHA/hCaptcha/Disabled)
- [ ] Frontend zeigt CAPTCHA-Widget nur wenn benötigt (Task 022)

### Smart Triggering Logic
```
Login CAPTCHA required when:
  - FailedAttempts >= 2 für diese Email (aus BruteForceProtection)
  - ODER RiskScore > ConfiguredThreshold
  
Device Approval CAPTCHA required when:
  - FailedCodeAttempts >= 2 für dieses Device

MFA Verify CAPTCHA required when:
  - FailedCodeAttempts >= 2 für diesen MFA Token
```

## 3. API Endpoints

### Neue Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| GET | /api/captcha/config | - | `{ provider, siteKey, enabled }` | Public: Get CAPTCHA config für Frontend |

### Geänderte Endpoints (CAPTCHA Token hinzufügen)

| Method | Route | Änderung |
|--------|-------|----------|
| POST | /api/auth/register | + `captchaToken` required |
| POST | /api/auth/forgot-password | + `captchaToken` required |
| POST | /api/auth/login | + `captchaToken` conditional (returned in 401 response) |
| POST | /api/auth/devices/{id}/approve | + `captchaToken` conditional |
| POST | /api/auth/mfa/verify | + `captchaToken` conditional (after 2 failed attempts) |

### Response bei CAPTCHA Required (Login)

```json
{
  "success": false,
  "error": {
    "code": "AUTH_CAPTCHA_REQUIRED",
    "message": "CAPTCHA verification required"
  }
}
```

## 4. Neue Error Codes ✅

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `CAPTCHA_REQUIRED` | 400 | CAPTCHA token required but not provided |
| `CAPTCHA_INVALID` | 400 | CAPTCHA verification failed |

> ✅ Diese Codes wurden zu `backend_reference.md` Memory hinzugefügt!

## 5. Configuration

### appsettings.json
```json
{
  "Captcha": {
    "Enabled": true,
    "Provider": "Turnstile",
    "Turnstile": {
      "SiteKey": "xxx",
      "SecretKey": "xxx"
    },
    "Recaptcha": {
      "SiteKey": "xxx",
      "SecretKey": "xxx",
      "MinScore": 0.5
    },
    "HCaptcha": {
      "SiteKey": "xxx",
      "SecretKey": "xxx"
    },
    "SmartTrigger": {
      "LoginFailedAttemptsThreshold": 2,
      "LoginRiskScoreThreshold": 70,
      "DeviceApprovalFailedAttemptsThreshold": 2,
      "MfaVerifyFailedAttemptsThreshold": 2
    }
  }
}
```

## 6. Files zu erstellen

### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ICaptchaService | `src/ExoAuth.Application/Common/Interfaces/ICaptchaService.cs` | Main service interface |
| ICaptchaProvider | `src/ExoAuth.Application/Common/Interfaces/ICaptchaProvider.cs` | Provider abstraction |
| CaptchaSettings | `src/ExoAuth.Application/Common/Models/CaptchaSettings.cs` | Configuration model |
| CaptchaResult | `src/ExoAuth.Application/Common/Models/CaptchaResult.cs` | Validation result |
| GetCaptchaConfig Query | `src/ExoAuth.Application/Features/Auth/Queries/GetCaptchaConfig/` | Public config query |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CaptchaService | `src/ExoAuth.Infrastructure/Services/CaptchaService.cs` | Main service with smart triggering |
| TurnstileProvider | `src/ExoAuth.Infrastructure/Services/Captcha/TurnstileProvider.cs` | Cloudflare Turnstile |
| RecaptchaProvider | `src/ExoAuth.Infrastructure/Services/Captcha/RecaptchaProvider.cs` | Google reCAPTCHA v3 |
| HCaptchaProvider | `src/ExoAuth.Infrastructure/Services/Captcha/HCaptchaProvider.cs` | hCaptcha |
| DisabledCaptchaProvider | `src/ExoAuth.Infrastructure/Services/Captcha/DisabledCaptchaProvider.cs` | No-op provider |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CaptchaController | `src/ExoAuth.Api/Controllers/CaptchaController.cs` | Public config endpoint |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | CAPTCHA error codes hinzufügen |
| `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterCommand.cs` | + CaptchaToken property |
| `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterHandler.cs` | CAPTCHA validation |
| `src/ExoAuth.Application/Features/Auth/Commands/Register/RegisterValidator.cs` | CaptchaToken required |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginCommand.cs` | + CaptchaToken property |
| `src/ExoAuth.Application/Features/Auth/Commands/Login/LoginHandler.cs` | Smart trigger + validation |
| `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` | + CaptchaToken property |
| `src/ExoAuth.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordHandler.cs` | CAPTCHA validation |
| `src/ExoAuth.Application/Features/Auth/Commands/ApproveDevice/ApproveDeviceCommand.cs` | + CaptchaToken property |
| `src/ExoAuth.Application/Features/Auth/Commands/ApproveDevice/ApproveDeviceHandler.cs` | Smart trigger + validation |
| `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyCommand.cs` | + CaptchaToken property |
| `src/ExoAuth.Application/Features/Auth/Commands/MfaVerify/MfaVerifyHandler.cs` | Smart trigger + validation |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Register CAPTCHA services |
| `src/ExoAuth.Api/appsettings.json` | CAPTCHA configuration |
| `src/ExoAuth.Api/appsettings.Development.json` | Dev config (disabled or test keys) |

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| - | - | - | Keine neuen Packages nötig (HTTP calls via HttpClient) |

## 9. Implementation Reihenfolge

### Phase 1: Core Infrastructure ✅
1. [x] **Models**: CaptchaSettings, CaptchaResult erstellen
2. [x] **Interfaces**: ICaptchaProvider, ICaptchaService erstellen
3. [x] **Providers**: TurnstileProvider implementieren (primary)
4. [x] **Providers**: DisabledCaptchaProvider implementieren (for testing/enterprise)
5. [x] **Service**: CaptchaService mit smart triggering logic
6. [x] **DI**: Services registrieren mit Provider-Factory

### Phase 2: API Integration ✅
7. [x] **Config Endpoint**: GetCaptchaConfig Query + CaptchaController
8. [x] **Register**: CaptchaToken zu Command + Handler validation
9. [x] **ForgotPassword**: CaptchaToken zu Command + Handler validation
10. [x] **Login**: Smart trigger logic + conditional validation
11. [x] **ApproveDevice**: Smart trigger logic + conditional validation
12. [x] **MfaVerify**: Smart trigger logic + conditional validation
13. [x] **Error Codes**: Neue Codes zu ErrorCodes.cs

### Phase 3: Additional Providers ✅
14. [x] **RecaptchaProvider**: Google reCAPTCHA v3 implementieren
15. [x] **HCaptchaProvider**: hCaptcha implementieren

### Phase 4: Testing & Docs ✅
16. [x] **Unit Tests**: Provider tests, Service tests, Handler tests (49 tests)
17. [x] **Config**: appsettings.json + Development config
18. [x] **Task File updaten**: Diese Taskfile aktualisieren
19. [x] **Memory updaten**: backend_reference.md aktualisieren

## 10. Interface Designs

### ICaptchaProvider
```csharp
public interface ICaptchaProvider
{
    string ProviderName { get; }
    Task<CaptchaResult> ValidateAsync(string token, string? expectedAction = null, CancellationToken ct = default);
}
```

### ICaptchaService
```csharp
public interface ICaptchaService
{
    /// <summary>
    /// Validates CAPTCHA token. Throws if invalid and CAPTCHA is required.
    /// </summary>
    Task ValidateAsync(string? token, string action, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if CAPTCHA is required for login (smart trigger).
    /// </summary>
    Task<bool> IsRequiredForLoginAsync(string email, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if CAPTCHA is required for device approval (smart trigger).
    /// </summary>
    Task<bool> IsRequiredForDeviceApprovalAsync(Guid deviceId, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if CAPTCHA is required for MFA verification (smart trigger).
    /// </summary>
    Task<bool> IsRequiredForMfaVerifyAsync(string mfaToken, CancellationToken ct = default);
    
    /// <summary>
    /// Gets public config for frontend.
    /// </summary>
    CaptchaPublicConfig GetPublicConfig();
}
```

### CaptchaResult
```csharp
public sealed record CaptchaResult(
    bool Success,
    float? Score,           // reCAPTCHA v3 score (0.0 - 1.0)
    string? Action,         // Action name if provided
    string? ErrorCode       // Error code if failed
);
```

## 11. Tests ✅

### Unit Tests (49 tests implemented)
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Services/Captcha/TurnstileProviderTests.cs` | Turnstile API calls | 10 |
| `tests/ExoAuth.UnitTests/Services/Captcha/DisabledCaptchaProviderTests.cs` | Disabled provider | 3 |
| `tests/ExoAuth.UnitTests/Services/CaptchaServiceTests.cs` | Smart triggering logic | 36 |

**Actual:** 49 tests (all passing)

## 12. Frontend Task (separate)

Frontend implementation will be Task 022:
- CAPTCHA widget component (Turnstile/reCAPTCHA/hCaptcha)
- Integration in Register form
- Integration in Login form (conditional display)
- Integration in Forgot Password form
- Integration in Device Approval form (conditional display)
- Integration in MFA Verify form (conditional display)
- Auto-fetch config from `/api/captcha/config`

## 13. Security Considerations

- **Never** trust client-side CAPTCHA validation alone
- **Always** validate token server-side
- CAPTCHA tokens are **single-use** (providers enforce this)
- Store SecretKey in secure configuration (not in appsettings.json in production)
- Rate limit the config endpoint to prevent enumeration

## 14. Nach Completion

- [x] Alle Unit Tests grün (405 total, 49 CAPTCHA-specific)
- [x] `backend_reference.md` Memory aktualisiert
- [x] Error Codes dokumentiert (CAPTCHA_REQUIRED, CAPTCHA_INVALID)
- [ ] Code reviewed
- [ ] Task 022 (Frontend) erstellt

## 15. Letzte Änderung

- **Datum:** 2026-01-07
- **Status:** ✅ Backend Completed
- **Tests:** 49 CAPTCHA tests, 405 total tests passing
- **Nächster Schritt:** Task 022 - Frontend CAPTCHA Integration

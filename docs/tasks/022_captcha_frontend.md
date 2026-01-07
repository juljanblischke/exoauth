# Task 022: CAPTCHA Frontend Integration

## 1. Übersicht

**Was wird gebaut?**
Frontend CAPTCHA integration mit dynamischem Provider-Support (Turnstile/reCAPTCHA/hCaptcha). Reaktive UX - CAPTCHA erscheint nur wenn vom Backend angefordert.

**Warum?**
- Task 021 hat Backend CAPTCHA implementiert - Frontend muss Widgets anzeigen
- Bot-Protection für Registration, Forgot Password
- Smart Triggering für Login, Device Approval, MFA Verify

## 2. User Experience / Anforderungen

### User Stories
- Als User möchte ich ein CAPTCHA-Widget sehen wenn es für die Registrierung erforderlich ist
- Als User möchte ich bei Login kein CAPTCHA sehen außer nach fehlgeschlagenen Versuchen
- Als User möchte ich klares Feedback wenn CAPTCHA-Validation fehlschlägt

### UI/UX Beschreibung
- **Register Form**: CAPTCHA-Widget immer sichtbar über Submit-Button
- **Forgot Password Modal**: CAPTCHA-Widget immer sichtbar über Submit-Button
- **Login Form**: CAPTCHA-Widget erscheint erst nach `CAPTCHA_REQUIRED` Error
- **Device Approval**: CAPTCHA-Widget erscheint erst nach `CAPTCHA_REQUIRED` Error
- **MFA Verify**: CAPTCHA-Widget erscheint erst nach `CAPTCHA_REQUIRED` Error

### States
- `loading`: Script wird geladen (Skeleton/Spinner)
- `ready`: Widget bereit für Interaktion
- `verifying`: Token wird validiert
- `error`: CAPTCHA fehlgeschlagen
- `disabled`: CAPTCHA ist deaktiviert (Enterprise)

### Akzeptanzkriterien
- [x] CAPTCHA config wird von `/api/captcha/config` geladen
- [x] Turnstile Widget funktioniert (Primary Provider)
- [x] reCAPTCHA v3 Widget funktioniert
- [x] hCaptcha Widget funktioniert
- [x] Disabled Provider zeigt kein Widget
- [x] Register Form hat CAPTCHA integriert
- [x] Forgot Password Modal hat CAPTCHA integriert
- [x] Login Form zeigt CAPTCHA nach Error
- [x] Device Approval zeigt CAPTCHA nach Error
- [x] MFA Verify zeigt CAPTCHA nach Error
- [x] i18n translations (EN + DE)

### Edge Cases / Error Handling
- Was wenn Config-Endpoint nicht erreichbar? → Form ohne CAPTCHA (graceful degradation)
- Was wenn CAPTCHA-Script nicht lädt? → Error State mit Retry-Button
- Was wenn Token expired? → Auto-refresh Widget
- Was wenn Provider wechselt? → Config wird bei App-Start geladen

## 3. API Integration

| Endpoint | Method | Response | Hook Name |
|----------|--------|----------|-----------|
| /api/captcha/config | GET | `{ provider, siteKey, enabled }` | `useCaptchaConfig` |

### Response Types
```typescript
interface CaptchaConfig {
  provider: 'Turnstile' | 'Recaptcha' | 'HCaptcha' | 'Disabled'
  siteKey: string
  enabled: boolean
}
```

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| CaptchaWidget | Feature | Main wrapper - wählt Provider automatisch |
| TurnstileCaptcha | Feature | Cloudflare Turnstile Widget |
| RecaptchaCaptcha | Feature | Google reCAPTCHA v3 Widget |
| HCaptchaCaptcha | Feature | hCaptcha Widget |

### Bestehende Komponenten ändern
| Komponente | Was ändern? |
|------------|-------------|
| LoginForm | + CaptchaWidget (conditional), handle CAPTCHA_REQUIRED |
| RegisterForm | + CaptchaWidget (always visible) |
| ForgotPasswordModal | + CaptchaWidget (always visible) |
| DeviceApprovalCodeInput | + CaptchaWidget (conditional) |
| MfaVerifyModal | + CaptchaWidget (conditional) |

## 5. Files zu erstellen

### Feature Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/auth/types/captcha.ts` | TypeScript types |
| API | `src/features/auth/api/captcha-api.ts` | API calls |
| Hook | `src/features/auth/hooks/use-captcha-config.ts` | React Query hook |
| CaptchaWidget | `src/features/auth/components/captcha-widget.tsx` | Main component |
| TurnstileCaptcha | `src/features/auth/components/turnstile-captcha.tsx` | Turnstile |
| RecaptchaCaptcha | `src/features/auth/components/recaptcha-captcha.tsx` | reCAPTCHA v3 |
| HCaptchaCaptcha | `src/features/auth/components/hcaptcha-captcha.tsx` | hCaptcha |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/components/login-form.tsx` | CAPTCHA integration |
| `src/features/auth/components/register-form.tsx` | CAPTCHA integration |
| `src/features/auth/components/forgot-password-modal.tsx` | CAPTCHA integration |
| `src/features/auth/components/device-approval-code-input.tsx` | CAPTCHA integration |
| `src/features/auth/components/mfa-verify-modal.tsx` | CAPTCHA integration |
| `src/features/auth/hooks/use-login.ts` | Handle CAPTCHA_REQUIRED response |
| `src/features/auth/hooks/use-register.ts` | Add captchaToken to request |
| `src/features/auth/hooks/use-forgot-password.ts` | Add captchaToken to request |
| `src/features/auth/hooks/use-approve-device-by-code.ts` | Handle CAPTCHA_REQUIRED |
| `src/features/auth/hooks/use-mfa-verify.ts` | Handle CAPTCHA_REQUIRED |
| `src/features/auth/types/index.ts` | Export captcha types |
| `src/features/auth/components/index.ts` | Export captcha components |
| `src/features/auth/hooks/index.ts` | Export captcha hook |
| `src/i18n/locales/en/auth.json` | CAPTCHA translations |
| `src/i18n/locales/de/auth.json` | CAPTCHA translations |
| `src/i18n/locales/en/errors.json` | CAPTCHA error messages |
| `src/i18n/locales/de/errors.json` | CAPTCHA error messages |

## 7. Neue Dependencies

### NPM Packages
| Package | Warum? |
|---------|--------|
| `@marsidev/react-turnstile` | Cloudflare Turnstile React component |
| `react-google-recaptcha-v3` | Google reCAPTCHA v3 React component |
| `@hcaptcha/react-hcaptcha` | hCaptcha React component |

```bash
yarn add @marsidev/react-turnstile react-google-recaptcha-v3 @hcaptcha/react-hcaptcha
```

## 8. Implementation Reihenfolge

### Phase 1: Core Setup ✅
1. [x] **Types**: `captcha.ts` - TypeScript interfaces
2. [x] **API**: `captcha-api.ts` - Config fetch
3. [x] **Hook**: `use-captcha-config.ts` - React Query hook
4. [x] **Install**: Yarn packages installieren

### Phase 2: Widget Components ✅
5. [x] **TurnstileCaptcha**: Turnstile widget component
6. [x] **RecaptchaCaptcha**: reCAPTCHA v3 widget component
7. [x] **HCaptchaCaptcha**: hCaptcha widget component
8. [x] **CaptchaWidget**: Main wrapper component

### Phase 3: Form Integration (Always Required) ✅
9. [x] **RegisterForm**: CAPTCHA integration (always visible)
10. [x] **ForgotPasswordModal**: CAPTCHA integration (always visible)
11. [x] **use-register.ts**: Add captchaToken to request (types updated)
12. [x] **use-forgot-password.ts**: Add captchaToken to request (types updated)

### Phase 4: Form Integration (Conditional) ✅
13. [x] **LoginForm**: CAPTCHA integration (after CAPTCHA_REQUIRED)
14. [x] **use-login.ts**: Handle CAPTCHA_REQUIRED response
15. [x] **DeviceApprovalModal**: CAPTCHA integration (conditional)
16. [x] **use-approve-device-by-code.ts**: Handle CAPTCHA_REQUIRED
17. [x] **MfaVerifyModal**: CAPTCHA integration (conditional)
18. [x] **use-mfa-verify.ts**: Handle CAPTCHA_REQUIRED

### Phase 5: i18n & Cleanup ✅
19. [x] **i18n**: EN + DE translations
20. [x] **Exports**: Update index.ts files
21. [x] **Task File updaten**: Diese Taskfile aktualisieren
22. [x] **Memory updaten**: i18n_translations.md aktualisiert

## 9. Component Designs

### CaptchaWidget Props
```typescript
interface CaptchaWidgetProps {
  onVerify: (token: string) => void
  onError?: (error: string) => void
  onExpire?: () => void
  action?: string  // For reCAPTCHA v3 action tracking
  className?: string
}
```

### Usage in Forms
```tsx
// Register Form (always visible)
const [captchaToken, setCaptchaToken] = useState<string | null>(null)

<CaptchaWidget 
  onVerify={setCaptchaToken}
  onExpire={() => setCaptchaToken(null)}
  action="register"
/>
<Button disabled={!captchaToken || isLoading}>
  {t('auth:register.submit')}
</Button>

// Login Form (conditional)
const [showCaptcha, setShowCaptcha] = useState(false)
const [captchaToken, setCaptchaToken] = useState<string | null>(null)

// In mutation error handler:
if (error.code === 'CAPTCHA_REQUIRED') {
  setShowCaptcha(true)
}

{showCaptcha && (
  <CaptchaWidget 
    onVerify={setCaptchaToken}
    action="login"
  />
)}
```

### TurnstileCaptcha Implementation
```tsx
import { Turnstile } from '@marsidev/react-turnstile'
import { useCaptchaConfig } from '../hooks/use-captcha-config'

export function TurnstileCaptcha({ onVerify, onError, onExpire }: Props) {
  const { data: config } = useCaptchaConfig()
  
  if (!config?.siteKey) return null
  
  return (
    <Turnstile
      siteKey={config.siteKey}
      onSuccess={onVerify}
      onError={onError}
      onExpire={onExpire}
      options={{
        theme: 'auto',  // Respects system theme
        size: 'normal',
      }}
    />
  )
}
```

## 10. i18n Keys

### English (auth.json)
```json
{
  "captcha": {
    "loading": "Loading security check...",
    "error": "Security verification failed. Please try again.",
    "expired": "Security check expired. Please verify again.",
    "required": "Please complete the security check"
  }
}
```

### German (auth.json)
```json
{
  "captcha": {
    "loading": "Sicherheitsprüfung wird geladen...",
    "error": "Sicherheitsprüfung fehlgeschlagen. Bitte erneut versuchen.",
    "expired": "Sicherheitsprüfung abgelaufen. Bitte erneut bestätigen.",
    "required": "Bitte Sicherheitsprüfung abschließen"
  }
}
```

### English (errors.json)
```json
{
  "AUTH_CAPTCHA_REQUIRED": "Please complete the security verification",
  "AUTH_CAPTCHA_INVALID": "Security verification failed. Please try again.",
  "AUTH_CAPTCHA_EXPIRED": "Security verification expired. Please try again."
}
```

### German (errors.json)
```json
{
  "AUTH_CAPTCHA_REQUIRED": "Bitte schließen Sie die Sicherheitsprüfung ab",
  "AUTH_CAPTCHA_INVALID": "Sicherheitsprüfung fehlgeschlagen. Bitte versuchen Sie es erneut.",
  "AUTH_CAPTCHA_EXPIRED": "Sicherheitsprüfung abgelaufen. Bitte versuchen Sie es erneut."
}
```

## 11. Tests

### Component Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/captcha-widget.test.tsx` | Provider selection, callbacks |
| `src/features/auth/__tests__/login-form-captcha.test.tsx` | Conditional CAPTCHA display |
| `src/features/auth/__tests__/register-form-captcha.test.tsx` | CAPTCHA integration |

## 12. Nach Completion

- [x] Alle Forms funktionieren mit CAPTCHA
- [x] Alle drei Provider implementiert (Turnstile, reCAPTCHA, hCaptcha)
- [x] Disabled mode implementiert
- [x] i18n translations vollständig (EN + DE)
- [x] `i18n_translations.md` Memory aktualisiert
- [x] TypeScript keine Errors
- [x] Lint passed (1 warning unrelated to CAPTCHA)
- [x] Build passed

## 13. Letzte Änderung

- **Datum:** 2026-01-07
- **Status:** ✅ Complete
- **Hinweise:**
  - `approve-device-link` API Endpoint von path param auf query param geändert
  - Error codes sind lowercase: `auth_captcha_required`, `auth_captcha_expired`, `auth_captcha_invalid`
  - Backend returns lowercase provider (`turnstile` not `Turnstile`) - fixed with case-insensitive comparison
  - Fixed React warning "Cannot update component while rendering" by using useEffect for auto-verify
  - Empty siteKey treated as disabled (auto-verify, no widget shown)
  - CAPTCHA auto-refresh on expired/invalid error (key-based remount)
  - `ACCOUNT_LOCKED` error shows nice formatted time (seconds/minutes/until time)
  - ApiError type extended with `data` field for response data access

# Task 014: Device Trust Frontend (Risk-Based Authentication UI)

## 1. Übersicht

**Was wird gebaut?**
Frontend für das Intelligent Device Trust System (Task 013). Handling von Device Approval Required Response, Code-Eingabe, Email-Link Approval, und Pending Sessions Anzeige.

**Warum?**
Backend (Task 013) ist fertig. User brauchen jetzt UI um:
- Neue Geräte per Code/Email-Link zu genehmigen
- Pending Devices in der Session-Liste zu sehen
- Verdächtige Login-Versuche abzulehnen

## 2. User Experience / Anforderungen

### User Stories

- Als User möchte ich bei verdächtigem Login einen Code eingeben können, damit ich mein Gerät verifizieren kann
- Als User möchte ich per Email-Link mein Gerät genehmigen können, damit ich keinen Code abtippen muss
- Als User möchte ich verdächtige Logins ablehnen können, damit Angreifer keinen Zugriff bekommen
- Als User möchte ich in meinen Sessions sehen welche Geräte noch pending sind, damit ich sie genehmigen kann

### UI/UX Beschreibung

#### Login Flow - Device Approval Required
1. User logged ein → Backend returned `requiresDeviceApproval: true`
2. Modal erscheint über Login-Form:
   - Titel: "Device Verification Required"
   - Einfache Erklärung: "New device from new location detected"
   - Zwei 4-Zeichen Input-Felder (XXXX - XXXX) mit Auto-Focus
   - "Check your email for the verification code"
   - Submit Button: "Verify Device"
   - Link: "This wasn't me" → Deny Flow
   - Attempts Counter: "2 attempts remaining" (bei Fehler)
3. Nach erfolgreicher Verifizierung: "Retry Login" Button

#### Email-Link Flow
1. User klickt Link in Email → Route `/approve-device/:token`
2. Page zeigt Loading → dann Success/Error
3. Success: "Device approved! You can now login." + Link zu Login
4. Error (expired/invalid): "Link expired. Please try logging in again." + Link zu Login

#### Sessions List - Pending Devices
- Bestehende Sessions-Liste zeigt auch `IsTrusted=false` Sessions
- Badge: "Pending Approval" (Yellow/Orange)
- Trust Button funktioniert wie bisher (schon implementiert)

#### Deny Device Flow
- User klickt "This wasn't me" im Modal
- Direkt deny (kein Confirm Dialog)
- Toast: "Device denied. We recommend changing your password."
- Modal schließt, zurück zum Login

### Akzeptanzkriterien

- [x] Login-Form handled `requiresDeviceApproval` Response
- [x] Device Approval Modal erscheint bei Medium/High Risk
- [x] Zwei 4-Zeichen Inputs mit Auto-Focus funktionieren
- [x] Code-Submission funktioniert (POST /api/auth/approve-device)
- [x] Remaining Attempts werden angezeigt
- [x] "Retry Login" Button erscheint nach Approval
- [x] Email-Link Route funktioniert (GET /api/auth/approve-device/:token)
- [x] Deny Device funktioniert (POST /api/auth/deny-device)
- [x] Sessions zeigen Pending Devices mit Badge
- [x] Expired/Max Attempts zeigt Error + "Start new login" Button
- [x] Alle Texte sind i18n-fähig (EN/DE)

### Edge Cases / Error Handling

| Situation | Handling |
|-----------|----------|
| Wrong code entered | Show error + remaining attempts |
| Max attempts reached | Show error + "Start new login" button |
| Token expired | Show error + "Start new login" button |
| Email link invalid | Show error page + link to login |
| Network error during approval | Show toast error, allow retry |
| User closes modal | Can reopen by trying login again |

## 3. API Integration

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/approve-device` | POST | `{ approvalToken, code }` | `{ success: true }` | useApproveDeviceByCode |
| `/api/auth/approve-device/:token` | GET | - | `{ success: true }` or redirect | useApproveDeviceByLink |
| `/api/auth/deny-device` | POST | `{ approvalToken }` | `{ success: true }` | useDenyDevice |

### Login Response Extension (already exists in backend)

```typescript
// Normal login
{ accessToken, refreshToken, user }

// Device approval required
{
  requiresDeviceApproval: true,
  approvalToken: string,
  riskScore: number,
  riskFactors: string[],  // ["new_device", "new_country"]
  message: string
}
```

## 4. Komponenten Übersicht

### Neue Komponenten

| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| DeviceApprovalModal | Feature | Modal für Code-Eingabe nach Login |
| DeviceApprovalCodeInput | Feature | Zwei 4-Zeichen Inputs mit Auto-Focus |
| DeviceApprovalSuccess | Feature | Success State nach Approval |
| ApproveDevicePage | Route | Email-Link Landing Page |
| PendingApprovalBadge | Feature | Badge für Pending Sessions |

### Bestehende Komponenten nutzen

| Komponente | Woher |
|------------|-------|
| Dialog | @/components/ui/dialog |
| Button | @/components/ui/button |
| Input | @/components/ui/input |
| Badge | @/components/ui/badge |
| LoadingSpinner | @/components/shared/feedback |
| ErrorState | @/components/shared/feedback |
| SessionCard | @/features/auth/components |
| SessionsList | @/features/auth/components |

## 5. Files zu erstellen

### Feature Files

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/auth/api/device-approval-api.ts` | Approve/Deny API calls |
| Hook | `src/features/auth/hooks/use-approve-device-by-code.ts` | Code approval mutation |
| Hook | `src/features/auth/hooks/use-approve-device-by-link.ts` | Link approval query |
| Hook | `src/features/auth/hooks/use-deny-device.ts` | Deny device mutation |
| Component | `src/features/auth/components/device-approval-modal.tsx` | Main approval modal |
| Component | `src/features/auth/components/device-approval-code-input.tsx` | 2x4 digit input |
| Types | `src/features/auth/types/device-approval.ts` | DeviceApproval types |

### Route Files

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Route | `src/routes/approve-device.tsx` | Email link landing page |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/components/login-form.tsx` | Handle requiresDeviceApproval, show modal |
| `src/features/auth/types/index.ts` | Add DeviceApprovalResponse to LoginResponse |
| `src/features/auth/components/session-card.tsx` | Show pending badge for IsTrusted=false |
| `src/features/auth/components/sessions-list.tsx` | Handle pending sessions display |
| `src/features/auth/components/index.ts` | Export new components |
| `src/features/auth/hooks/index.ts` | Export new hooks |
| `src/app/router.tsx` | Add /approve-device/:token route |
| `src/i18n/locales/en/auth.json` | Add deviceApproval translations |
| `src/i18n/locales/de/auth.json` | Add deviceApproval translations |
| `src/i18n/locales/en/errors.json` | Add device approval error messages |
| `src/i18n/locales/de/errors.json` | Add device approval error messages |

## 7. Neue Dependencies

### NPM Packages

Keine neuen Packages erforderlich.

### Shadcn/UI Komponenten

Keine neuen Shadcn Komponenten erforderlich.

## 8. Implementation Reihenfolge

1. [x] **Types**: DeviceApproval types definieren
2. [x] **API**: device-approval-api.ts erstellen
3. [x] **Hooks**: Approval/Deny hooks erstellen
4. [x] **Components**: DeviceApprovalCodeInput erstellen (2x4 digit input)
5. [x] **Components**: DeviceApprovalModal erstellen
6. [x] **Route**: ApproveDevicePage für Email-Link erstellen
7. [x] **Integration**: login-form.tsx anpassen
8. [x] **Sessions**: session-card.tsx pending badge hinzufügen
9. [x] **i18n**: Translations hinzufügen (EN/DE)
10. [ ] **Tests**: Component + Hook tests
11. [ ] **Standards updaten**: task_standards_frontend.md aktualisieren

## 9. Tests

### Component Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/device-approval-modal.test.tsx` | Modal rendering, code input, submit |
| `src/features/auth/__tests__/device-approval-code-input.test.tsx` | Auto-focus, validation, paste handling |
| `src/features/auth/__tests__/approve-device-page.test.tsx` | Link approval flow |

### Hook Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/use-approve-device-by-code.test.ts` | Code approval mutation |
| `src/features/auth/__tests__/use-deny-device.test.ts` | Deny mutation |

## 10. i18n Keys (auth.json)

### English

```json
{
  "deviceApproval": {
    "title": "Device Verification Required",
    "description": "New device from new location detected",
    "codeLabel": "Enter verification code",
    "codePlaceholder": "XXXX",
    "codeHint": "Check your email for the verification code",
    "submitButton": "Verify Device",
    "retryButton": "Retry Login",
    "denyLink": "This wasn't me",
    "attemptsRemaining": "{{count}} attempts remaining",
    "success": "Device verified successfully!",
    "denied": "Device denied. We recommend changing your password.",
    "expired": "Verification expired. Please try logging in again.",
    "maxAttempts": "Too many failed attempts. Please try logging in again.",
    "startNewLogin": "Start New Login",
    "linkApproval": {
      "title": "Device Approval",
      "loading": "Verifying device...",
      "success": "Device approved! You can now login.",
      "error": "Could not approve device. The link may have expired.",
      "backToLogin": "Back to Login"
    }
  }
}
```

### German

```json
{
  "deviceApproval": {
    "title": "Geräte-Verifizierung erforderlich",
    "description": "Neues Gerät von neuem Standort erkannt",
    "codeLabel": "Verifizierungscode eingeben",
    "codePlaceholder": "XXXX",
    "codeHint": "Prüfen Sie Ihre E-Mail für den Verifizierungscode",
    "submitButton": "Gerät verifizieren",
    "retryButton": "Erneut einloggen",
    "denyLink": "Das war ich nicht",
    "attemptsRemaining": "{{count}} Versuche übrig",
    "success": "Gerät erfolgreich verifiziert!",
    "denied": "Gerät abgelehnt. Wir empfehlen, Ihr Passwort zu ändern.",
    "expired": "Verifizierung abgelaufen. Bitte erneut einloggen.",
    "maxAttempts": "Zu viele Fehlversuche. Bitte erneut einloggen.",
    "startNewLogin": "Neuen Login starten",
    "linkApproval": {
      "title": "Geräte-Genehmigung",
      "loading": "Gerät wird verifiziert...",
      "success": "Gerät genehmigt! Sie können sich jetzt einloggen.",
      "error": "Gerät konnte nicht genehmigt werden. Der Link ist möglicherweise abgelaufen.",
      "backToLogin": "Zurück zum Login"
    }
  }
}
```

## 11. Error Codes (errors.json)

```json
{
  "APPROVAL_TOKEN_INVALID": "Invalid verification token",
  "APPROVAL_TOKEN_EXPIRED": "Verification token has expired",
  "APPROVAL_CODE_INVALID": "Invalid verification code",
  "APPROVAL_MAX_ATTEMPTS": "Too many failed attempts",
  "DEVICE_APPROVAL_DENIED": "This device has been denied"
}
```

## 12. Design Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Approval UI Style | Modal on login page | Keeps context, no extra navigation |
| Risk Factors Display | Simple text | Less intimidating for users |
| Code Input | Two 4-digit fields | Better UX, auto-focus between fields |
| Wrong Code | Show remaining attempts | User knows how many tries left |
| After Approval | Manual "Retry Login" | Simple, no polling complexity |
| Expired/Max Attempts | Error + button | Clear call to action |
| Deny Confirmation | No dialog | Quick action, just suggest password change |
| i18n Namespace | Extend auth.json | Related to auth flow |
| Pending Badge | Yellow/Orange | Warning style, needs attention |

## 13. Component Details

### DeviceApprovalCodeInput

```typescript
interface DeviceApprovalCodeInputProps {
  value: string;           // "XXXX-XXXX" format
  onChange: (code: string) => void;
  disabled?: boolean;
  error?: boolean;
}

// Features:
// - Two 4-character input fields
// - Auto-focus to second field after 4 chars
// - Auto-focus back on backspace when empty
// - Paste handling (split "XXXX-XXXX" or "XXXXXXXX")
// - Only allows alphanumeric (uppercase)
// - Visual error state
```

### DeviceApprovalModal

```typescript
interface DeviceApprovalModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  approvalToken: string;
  riskFactors: string[];
  onSuccess: () => void;    // Called after successful approval
  onDeny: () => void;       // Called after deny
}

// States:
// - input: User entering code
// - loading: Submitting code
// - success: Code accepted, show "Retry Login"
// - error: Wrong code, show remaining attempts
// - expired: Token expired, show "Start new login"
// - maxAttempts: Too many tries, show "Start new login"
```

## 14. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_frontend.md` aktualisiert (neue Files, Components)
- [x] TypeScript keine Errors
- [x] Lint passed
- [ ] Manual Testing: Login flow with device approval
- [ ] Manual Testing: Email link approval
- [ ] Manual Testing: Deny device flow
- [ ] Manual Testing: Sessions pending badge

## 15. Letzte Änderung

- **Datum:** 2026-01-01
- **Status:** Steps 1-9 Complete (Implementation)
- **Nächster Schritt:** Step 10 (Tests) oder Step 11 (Standards)

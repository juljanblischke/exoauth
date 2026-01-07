# i18n Translations Reference - ExoAuth

> Languages: **English (en)** and **German (de)**

---

## File Structure

```
frontend/src/i18n/
├── index.ts                    # i18next configuration
└── locales/
    ├── en/
    │   ├── common.json         # Buttons, labels, status
    │   ├── auth.json           # Authentication flows
    │   ├── navigation.json     # Navigation & menus
    │   ├── users.json          # User management
    │   ├── auditLogs.json      # Audit logs
    │   ├── settings.json       # Settings page
    │   ├── mfa.json            # Two-factor auth
    │   ├── sessions.json       # Device sessions
    │   ├── errors.json         # Error messages
    │   └── validation.json     # Form validation
    └── de/
        └── (same files)
```

---

## Namespace Overview

| Namespace | File | Usage |
|-----------|------|-------|
| common | common.json | `t('actions.save')` |
| auth | auth.json | `t('auth:login.title')` |
| navigation | navigation.json | `t('navigation:items.dashboard')` |
| users | users.json | `t('users:title')` |
| auditLogs | auditLogs.json | `t('auditLogs:title')` |
| ipRestrictions | ipRestrictions.json | `t('ipRestrictions:title')` |
| settings | settings.json | `t('settings:title')` |
| mfa | mfa.json | `t('mfa:title')` |
| sessions | sessions.json | `t('sessions:title')` |
| errors | errors.json | `t('errors:codes.AUTH_INVALID_CREDENTIALS')` |
| validation | validation.json | `t('validation:required')` |

---

## Key Categories

### common.json

```
actions.*          - save, cancel, delete, edit, create, add, close, confirm, rename, etc.
status.*           - active, inactive, pending, suspended, enabled, disabled
states.*           - deleting, saving, processing
table.*            - noResults, noData, loading, selected, columns
time.*             - justNow, minutesAgo, hoursAgo, daysAgo
confirm.*          - title, deleteTitle, deleteMessage, typeToConfirm
legal.*            - imprint, privacy, terms, cookies
theme.*            - label, light, dark, system
language.*         - en, de
session.*          - warningTitle, warningDescription, extend
cookies.*          - title, description, accept, reject
forceReauth.*      - title, description
```

### auth.json

```
login.*            - title, subtitle, email, password, rememberMe, signIn, orContinueWith
register.*         - title, subtitle, firstName, lastName, createAccount
invite.*           - title, subtitle, accept, expired, invalid, revoked
forgotPassword.*   - title, subtitle, sendLink, sent, enterCode
resetPassword.*    - title, newPassword, confirmPassword, reset, success
logout.*           - title, message, confirm
session.*          - expiring, expired, extend
mfa.*              - title, code, verify, useBackupCode
password.*         - requirements, minLength, uppercase, lowercase, digit, special
deviceApproval.*   - title, description, codeLabel, submitButton, riskFactors.*, linkApproval.*, etc.
trustedDevices.*   - title, description, current, removeAll, remove, rename, noDevices, etc.
devices.*          - title, details, description, unknownDevice, current, empty.*, status.*, info.*, actions.*, rename.*, revoke.*, approve.*
passkeys.*         - title, description, loginButton, addButton, empty.*, card.*, register.*, rename.*, delete.*, login.*, notSupported.*, multiDeviceHint
captcha.*          - loading, loadError, verificationError, disabled, recaptcha.indicator
```

### users.json

```
title, subtitle, createUser, inviteUser, editUser, deleteUser
fields.*           - name, email, role, status, createdAt, mfaEnabled
roles.*            - admin, user, viewer
status.*           - active, inactive, pending, suspended
search.*, filters.*, tabs.*
permissions.*      - title, success, none
actions.*          - permissions, activate, deactivate, suspend, resetPassword
messages.*         - createSuccess, updateSuccess, deleteSuccess, inviteSuccess
empty.*            - title, message, action
invites.*          - title, search, empty, status.*, fields.*, actions.*
admin.*            - actions, sessions, mfa, unlock, deactivate, activate, anonymize
trustedDevices.*   - title, loading, noDevices, removeAll, remove, errors.*
devices.*          - revokeSuccess, revokeAllSuccess, revokeAllTitle, revokeAllDescription
```

### ipRestrictions.json (Task 024)

```
title, subtitle
table.*            - ipAddress, type, reason, source, expiresAt, createdAt, createdBy, actions, noResults, noResultsDescription
type.*             - whitelist, blacklist
source.*           - manual, auto
filters.*          - search, type, source, allTypes, allSources
create.*           - title, description, ipAddress, ipAddressPlaceholder, ipAddressDescription, type, typePlaceholder, reason, reasonPlaceholder, expiresAt, expiresAtDescription, permanent, submit, success, invalidIp, getMyIp
edit.*             - title, description, ipAddressReadonly
update.*           - success
delete.*           - title, description, confirm, success
details.*          - title, ipAddress, type, reason, source, expiresAt, createdAt, createdBy, never, system
expired, never
```

### sessions.json

```
title, description, current, trusted, pendingApproval
lastActive, location, created, empty, loading
details.*          - title, description, deviceInfo, browser, os, deviceType
revoke.*           - button, title, description, success
revokeAll.*        - button, title, description, success
trust.*            - button, title, description, success
rename.*           - button, title, placeholder, success
errors.*           - loadFailed, revokeFailed, cannotRevokeCurrent
```

### mfa.json

```
title, description
status.*           - enabled, disabled
enable.*           - button, description
setup.*            - title, step1.title, step2.title, step3.title, cancel
confirm.*          - title, description, warning, downloadButton, copyButton
verify.*           - title, description, button, useBackupCode, startNewLogin
disable.*          - title, description, button, success
backupCodes.*      - title, regenerate
errors.*           - codeInvalid, tokenInvalid, tokenExpired, setupFailed
```

### errors.json

```
codes.*            - All backend error codes mapped to user-friendly messages
  AUTH_INVALID_CREDENTIALS, AUTH_USER_INACTIVE, AUTH_TOKEN_EXPIRED
  AUTH_TOO_MANY_ATTEMPTS, AUTH_EMAIL_EXISTS, AUTH_INVITE_EXPIRED
  PASSWORD_RESET_TOKEN_INVALID, PASSWORD_RESET_TOKEN_EXPIRED
  MFA_REQUIRED, MFA_CODE_INVALID, MFA_ALREADY_ENABLED, MFA_TOKEN_INVALID
  SESSION_NOT_FOUND, SESSION_REVOKED
  SYSTEM_USER_NOT_FOUND, SYSTEM_LAST_PERMISSION_HOLDER, SYSTEM_USER_ANONYMIZED
  APPROVAL_TOKEN_INVALID, APPROVAL_CODE_INVALID, APPROVAL_MAX_ATTEMPTS
  AUTH_CAPTCHA_REQUIRED, AUTH_CAPTCHA_INVALID, AUTH_CAPTCHA_EXPIRED
  ACCOUNT_LOCKED, ACCOUNT_LOCKED_SECONDS, ACCOUNT_LOCKED_MINUTES, ACCOUNT_LOCKED_UNTIL
  MFA_CODE_INVALID, MFA_TOKEN_INVALID, MFA_TOKEN_EXPIRED
  IP_BLACKLISTED, IP_RESTRICTION_NOT_FOUND, IP_RESTRICTION_INVALID_CIDR, IP_RESTRICTION_DUPLICATE, IP_RESTRICTION_ALREADY_EXISTS

rateLimited.*      - title, description (Task 024)
ipBlacklisted.*    - title, description (Task 024)

general.*          - title, message, retry, goHome
network.*          - title, message, offline
notFound.*         - title, code, message
forbidden.*        - title, code, message
serverError.*      - title, code
unauthorized.*     - title, code
api.*              - badRequest, conflict, notFound, rateLimited, serverError
```

### validation.json

```
required, email, minLength, maxLength, min, max, pattern, url, number
password.*         - minLength, lowercase, uppercase, number, special, mismatch, weak
password.strength.* - weak, fair, good, strong
unique.*, confirmation.*
```

---

## Usage Examples

```typescript
import { useTranslation } from 'react-i18next'

function Component() {
  const { t } = useTranslation()

  // Default namespace (common)
  t('actions.save')              // "Save"

  // Specific namespace
  t('auth:login.title')          // "Sign In"
  t('users:invites.status.pending')  // "Pending"

  // With interpolation
  t('time.minutesAgo', { count: 5 })  // "5 minute(s) ago"
  t('confirm.typeToConfirm', { text: 'DELETE' })

  // Error codes
  t('errors:codes.AUTH_INVALID_CREDENTIALS')  // "Invalid email or password"
}
```

---

## Adding New Translations

1. Add key to **both** `en/{namespace}.json` and `de/{namespace}.json`
2. Use meaningful nested keys: `feature.section.key`
3. Use interpolation for dynamic values: `{{name}}`, `{{count}}`
4. Update this memory file with new key patterns

### Template for New Feature

**en/{namespace}.json:**
```json
{
  "newFeature": {
    "title": "New Feature",
    "description": "Description here",
    "actions": {
      "create": "Create",
      "edit": "Edit"
    },
    "messages": {
      "success": "Successfully created",
      "error": "Failed to create"
    }
  }
}
```

**de/{namespace}.json:**
```json
{
  "newFeature": {
    "title": "Neue Funktion",
    "description": "Beschreibung hier",
    "actions": {
      "create": "Erstellen",
      "edit": "Bearbeiten"
    },
    "messages": {
      "success": "Erfolgreich erstellt",
      "error": "Erstellen fehlgeschlagen"
    }
  }
}
```

---

## Last Updated
- **Date:** 2026-01-07
- **Latest Additions:** Task 024 - Added ipRestrictions namespace (create/edit/update/delete), rateLimited/ipBlacklisted error toasts, IP_BLACKLISTED/IP_RESTRICTION_* error codes (including IP_RESTRICTION_ALREADY_EXISTS), navigation breadcrumb.ipRestrictions, details.temporary key

# Frontend Task Standards - ExoAuth

> **MEGA BRAIN** - Lies diese Datei KOMPLETT bevor du einen Task erstellst.

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

### UI/UX Beschreibung
- Was sieht der User?
- Welche Interaktionen gibt es?
- Welche States gibt es? (loading, error, success, empty)

### Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2
- [ ] Kriterium 3

### Edge Cases / Error Handling
- Was passiert wenn API fehlt?
- Was passiert bei Validation Error?
- Was passiert bei leeren Daten?

## 3. API Integration

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/... | POST | `{ ... }` | `{ ... }` | use... |

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| ... | Page/Feature/Shared | ... |

### Bestehende Komponenten nutzen
| Komponente | Woher? |
|------------|--------|
| Button | @/components/ui/button |
| DataTable | @/components/shared/data-table |
| ... | ... |

## 5. Files zu erstellen

### Feature Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| API | `src/features/{feature}/api/{name}-api.ts` | API calls |
| Hook | `src/features/{feature}/hooks/use-{name}.ts` | React Query hook |
| Component | `src/features/{feature}/components/{name}.tsx` | UI Component |
| Types | `src/features/{feature}/types/index.ts` | TypeScript types |

### Route Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Route | `src/routes/{name}.tsx` | Page component |

### Shared Components (wenn nötig)
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/components/shared/...` | ... |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/app/router.tsx` | Neue Route hinzufügen |
| `src/features/{feature}/index.ts` | Export hinzufügen |

## 7. Neue Dependencies

### NPM Packages
| Package | Warum? |
|---------|--------|
| ... | ... |

### Shadcn/UI Komponenten
| Komponente | Command |
|------------|---------|
| ... | `npx shadcn@latest add ...` |

## 8. Implementation Reihenfolge

1. [ ] **Types**: TypeScript interfaces/types definieren
2. [ ] **API**: API client functions erstellen
3. [ ] **Hooks**: React Query hooks erstellen
4. [ ] **Components**: UI Komponenten bauen
5. [ ] **Route**: Page/Route erstellen
6. [ ] **Tests**: Component + Hook tests
7. [ ] **Standards updaten**: task_standards_frontend.md aktualisieren

## 9. Tests

### Component Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/{feature}/__tests__/{name}.test.tsx` | ... |

### Hook Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/{feature}/__tests__/use-{name}.test.ts` | ... |

## 10. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_frontend.md` aktualisiert (neue Files, Packages, Components)
- [ ] TypeScript keine Errors
- [ ] Lint passed
```

---

## Aktueller Projekt Stand

### File Tree (Was existiert)

```
frontend/
├── package.json
├── vite.config.ts
├── vitest.config.ts
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── components.json                 (shadcn config)
├── .env.example
├── .prettierrc
│
├── src/
│   ├── main.tsx                   ✅ Updated mit Providers & Router
│   │
│   ├── app/                       [APP SETUP]
│   │   ├── providers.tsx          ✅ QueryClient, Theme, Auth, Sidebar, Toaster
│   │   └── router.tsx             ✅ TanStack Router Setup
│   │
│   ├── components/
│   │   ├── ui/                    [SHADCN COMPONENTS - 23 total]
│   │   │   ├── alert-dialog.tsx   ✅
│   │   │   ├── avatar.tsx         ✅
│   │   │   ├── badge.tsx          ✅
│   │   │   ├── breadcrumb.tsx     ✅
│   │   │   ├── button.tsx         ✅
│   │   │   ├── calendar.tsx       ✅ (Task 006)
│   │   │   ├── checkbox.tsx       ✅
│   │   │   ├── command.tsx        ✅
│   │   │   ├── dialog.tsx         ✅
│   │   │   ├── dropdown-menu.tsx  ✅
│   │   │   ├── input.tsx          ✅
│   │   │   ├── label.tsx          ✅
│   │   │   ├── popover.tsx        ✅
│   │   │   ├── progress.tsx       ✅
│   │   │   ├── scroll-area.tsx    ✅
│   │   │   ├── separator.tsx      ✅
│   │   │   ├── sheet.tsx          ✅
│   │   │   ├── skeleton.tsx       ✅
│   │   │   ├── sonner.tsx         ✅
│   │   │   ├── switch.tsx         ✅ (Task 006)
│   │   │   ├── table.tsx          ✅
│   │   │   ├── tabs.tsx           ✅ (Task 006)
│   │   │   └── tooltip.tsx        ✅
│   │   │
│   │   └── shared/
│   │       ├── index.ts           ✅ Barrel Export
│   │       │
│   │       ├── layout/            [LAYOUT COMPONENTS]
│   │       │   ├── app-layout.tsx       ✅ Main Layout Wrapper
│   │       │   ├── sidebar.tsx          ✅ Collapsible Nav
│   │       │   ├── header.tsx           ✅ Top Bar mit Breadcrumbs
│   │       │   ├── user-menu.tsx        ✅ Profile Dropdown
│   │       │   ├── theme-toggle.tsx     ✅ Dark/Light/System Switch
│   │       │   ├── language-switcher.tsx ✅ EN/DE Switch
│   │       │   ├── breadcrumbs.tsx      ✅ Navigation Breadcrumbs
│   │       │   ├── page-header.tsx      ✅ Title + Description + Actions
│   │       │   ├── footer.tsx           ✅ Legal Links
│   │       │   ├── mobile-nav.tsx       ✅ Hamburger + Sheet Navigation
│   │       │   └── index.ts             ✅ Barrel Export
│   │       │
│   │       ├── feedback/          [FEEDBACK COMPONENTS]
│   │       │   ├── loading-spinner.tsx      ✅
│   │       │   ├── empty-state.tsx          ✅
│   │       │   ├── error-state.tsx          ✅
│   │       │   ├── confirm-dialog.tsx       ✅ Simple Confirm
│   │       │   ├── type-confirm-dialog.tsx  ✅ Type to Confirm
│   │       │   ├── unsaved-warning.tsx      ✅ Leave Form Warning
│   │       │   └── index.ts                 ✅ Barrel Export
│   │       │
│   │       ├── data-table/        [DATATABLE COMPONENTS]
│   │       │   ├── data-table.tsx           ✅ Main Table Component
│   │       │   ├── data-table-toolbar.tsx   ✅ Search, Filter, Columns
│   │       │   ├── data-table-filters.tsx   ✅ Filter Dropdown
│   │       │   ├── data-table-column-toggle.tsx ✅ Show/Hide Columns
│   │       │   ├── data-table-pagination.tsx    ✅ Infinite Scroll
│   │       │   ├── data-table-row-actions.tsx   ✅ Three-Dot Menu
│   │       │   ├── data-table-bulk-actions.tsx  ✅ Floating Bar
│   │       │   ├── data-table-card.tsx          ✅ Mobile Card View
│   │       │   └── index.ts                     ✅ Barrel Export
│   │       │
│   │       ├── form/              [FORM COMPONENTS]
│   │       │   ├── password-input.tsx     ✅ Toggle Visibility
│   │       │   ├── password-strength.tsx  ✅ Strength Indicator
│   │       │   ├── form-sheet.tsx         ✅ Slide-out Form
│   │       │   ├── form-modal.tsx         ✅ Modal Form
│   │       │   ├── date-range-picker.tsx  ✅ Date Range Selection (Task 006)
│   │       │   ├── select-filter.tsx      ✅ Multi-Select Filter (Task 006)
│   │       │   └── index.ts               ✅ Barrel Export
│   │       │
│   │       ├── user-avatar.tsx        ✅ Initials Avatar
│   │       ├── status-badge.tsx       ✅ Colored Pill
│   │       ├── copy-button.tsx        ✅ Copy to Clipboard
│   │       ├── relative-time.tsx      ✅ "2h ago" + Tooltip
│   │       ├── command-menu.tsx       ✅ Cmd+K Spotlight
│   │       ├── session-warning.tsx    ✅ Timeout Modal
│   │       ├── cookie-consent.tsx     ✅ GDPR Banner
│   │       └── help-button.tsx        ✅ Floating ?
│   │
│   ├── config/
│   │   └── navigation.ts          ✅ Sidebar Items mit Permissions
│   │
│   ├── contexts/
│   │   ├── auth-context.tsx       ✅ Auth State, User, Permissions, Force-Reauth
│   │   ├── theme-context.tsx      ✅ Dark/Light/System Mode
│   │   ├── sidebar-context.tsx    ✅ Collapsed State
│   │   └── index.ts               ✅ Barrel Export
│   │
│   ├── features/
│   │   ├── auth/                  [AUTH FEATURE - Task 004/006/008]
│   │   │   ├── api/
│   │   │   │   ├── auth-api.ts          ✅ Login, Register, Logout, Invite APIs
│   │   │   │   ├── mfa-api.ts           ✅ MFA Setup, Confirm, Verify, Disable (Task 008)
│   │   │   │   ├── password-reset-api.ts ✅ Forgot/Reset Password (Task 008)
│   │   │   │   ├── sessions-api.ts      ✅ Sessions CRUD (Task 008)
│   │   │   │   └── preferences-api.ts   ✅ User Preferences (Task 008)
│   │   │   ├── hooks/
│   │   │   │   ├── use-login.ts         ✅ Login mutation
│   │   │   │   ├── use-logout.ts        ✅ Logout mutation
│   │   │   │   ├── use-register.ts      ✅ Register mutation
│   │   │   │   ├── use-current-user.ts  ✅ Current user query
│   │   │   │   ├── use-accept-invite.ts ✅ Accept invite mutation
│   │   │   │   ├── use-validate-invite.ts ✅ Validate invite query (Task 006)
│   │   │   │   ├── use-mfa-setup.ts     ✅ MFA setup mutation (Task 008)
│   │   │   │   ├── use-mfa-confirm.ts   ✅ MFA confirm mutation (Task 008)
│   │   │   │   ├── use-mfa-verify.ts    ✅ MFA verify mutation (Task 008)
│   │   │   │   ├── use-mfa-disable.ts   ✅ MFA disable mutation (Task 008)
│   │   │   │   ├── use-regenerate-backup-codes.ts ✅ Backup codes (Task 008)
│   │   │   │   ├── use-forgot-password.ts ✅ Forgot password mutation (Task 008)
│   │   │   │   ├── use-reset-password.ts ✅ Reset password mutation (Task 008)
│   │   │   │   ├── use-sessions.ts      ✅ Sessions query (Task 008)
│   │   │   │   ├── use-revoke-session.ts ✅ Revoke session mutation (Task 008)
│   │   │   │   ├── use-revoke-all-sessions.ts ✅ Revoke all mutation (Task 008)
│   │   │   │   ├── use-update-session.ts ✅ Update session mutation (Task 008)
│   │   │   │   ├── use-trust-session.ts ✅ Trust session mutation (Task 008)
│   │   │   │   ├── use-update-preferences.ts ✅ Preferences mutation (Task 008)
│   │   │   │   └── index.ts             ✅ Barrel Export
│   │   │   ├── components/
│   │   │   │   ├── login-form.tsx           ✅ Login Form (+ MFA handling Task 008)
│   │   │   │   ├── register-form.tsx        ✅ Register Form (+ MFA handling Task 008)
│   │   │   │   ├── accept-invite-form.tsx   ✅ Accept Invite Form (+ MFA Task 008)
│   │   │   │   ├── password-requirements.tsx ✅ Password Requirements Display
│   │   │   │   ├── mfa-setup-modal.tsx      ✅ MFA QR Code Setup (Task 008)
│   │   │   │   ├── mfa-confirm-modal.tsx    ✅ MFA Confirm + Backup Codes (Task 008)
│   │   │   │   ├── mfa-verify-modal.tsx     ✅ MFA Login Verification (Task 008)
│   │   │   │   ├── mfa-disable-modal.tsx    ✅ MFA Disable Confirm (Task 008)
│   │   │   │   ├── backup-codes-display.tsx ✅ Backup Codes Display (Task 008)
│   │   │   │   ├── forgot-password-modal.tsx ✅ Multi-step Reset (Task 008)
│   │   │   │   ├── session-card.tsx         ✅ Single Session Display (Task 008)
│   │   │   │   ├── sessions-list.tsx        ✅ Sessions List (Task 008)
│   │   │   │   ├── session-details-sheet.tsx ✅ Session Details Sheet (Task 010)
│   │   │   │   └── index.ts                 ✅ Barrel Export
│   │   │   ├── types/
│   │   │   │   ├── index.ts             ✅ Auth Types (+ Device, MFA response)
│   │   │   │   ├── mfa.ts               ✅ MFA Types (Task 008)
│   │   │   │   ├── password-reset.ts    ✅ Password Reset Types (Task 008)
│   │   │   │   └── sessions.ts          ✅ Session Types (Task 008)
│   │   │   ├── __tests__/
│   │   │   │   ├── login-form.test.tsx      ✅
│   │   │   │   ├── register-form.test.tsx   ✅
│   │   │   │   └── use-login.test.ts        ✅
│   │   │   └── index.ts                 ✅ Barrel Export
│   │   │
│   │   ├── users/                 [USERS FEATURE - Task 004/006/008]
│   │   │   ├── api/
│   │   │   │   ├── users-api.ts         ✅ Users CRUD APIs (- delete Task 008)
│   │   │   │   ├── invites-api.ts       ✅ Invites APIs (Task 006)
│   │   │   │   └── user-admin-api.ts    ✅ Admin Actions API (Task 008)
│   │   │   ├── hooks/
│   │   │   │   ├── use-system-users.ts      ✅ Users list query
│   │   │   │   ├── use-system-user.ts       ✅ Single user query
│   │   │   │   ├── use-invite-user.ts       ✅ Invite mutation
│   │   │   │   ├── use-update-user.ts       ✅ Update mutation
│   │   │   │   ├── use-update-permissions.ts ✅ Permissions mutation
│   │   │   │   ├── use-system-invites.ts    ✅ Invites list query (Task 006)
│   │   │   │   ├── use-system-invite.ts     ✅ Single invite query (Task 006)
│   │   │   │   ├── use-revoke-invite.ts     ✅ Revoke mutation (Task 006)
│   │   │   │   ├── use-resend-invite.ts     ✅ Resend mutation (Task 006)
│   │   │   │   ├── use-reset-user-mfa.ts    ✅ Reset MFA mutation (Task 008)
│   │   │   │   ├── use-unlock-user.ts       ✅ Unlock user mutation (Task 008)
│   │   │   │   ├── use-user-sessions.ts     ✅ User sessions query (Task 008)
│   │   │   │   ├── use-revoke-user-session.ts ✅ Revoke single session (Task 008)
│   │   │   │   ├── use-revoke-user-sessions.ts ✅ Revoke all sessions (Task 008)
│   │   │   │   ├── use-deactivate-user.ts   ✅ Deactivate mutation (Task 008)
│   │   │   │   ├── use-activate-user.ts     ✅ Activate mutation (Task 008)
│   │   │   │   ├── use-anonymize-user.ts    ✅ Anonymize mutation (Task 008)
│   │   │   │   ├── use-update-invite.ts     ✅ Update invite mutation (Task 010)
│   │   │   │   └── index.ts                 ✅ Barrel Export
│   │   │   ├── components/
│   │   │   │   ├── users-table.tsx              ✅ Users DataTable (+ admin actions Task 008)
│   │   │   │   ├── users-table-columns.tsx      ✅ Column Definitions (+ security Task 008)
│   │   │   │   ├── user-details-sheet.tsx       ✅ User Details Sheet (+ sessions Task 008)
│   │   │   │   ├── user-edit-modal.tsx          ✅ Edit User Modal
│   │   │   │   ├── user-invite-modal.tsx        ✅ Invite User Modal
│   │   │   │   ├── user-permissions-modal.tsx   ✅ Permissions Modal
│   │   │   │   ├── invitations-table.tsx        ✅ Invitations DataTable (Task 006)
│   │   │   │   ├── invitations-table-columns.tsx ✅ Column Definitions (Task 006)
│   │   │   │   ├── invite-details-sheet.tsx     ✅ Invite Details Sheet (Task 006)
│   │   │   │   ├── user-sessions-section.tsx    ✅ Admin Sessions View (Task 008)
│   │   │   │   ├── user-status-badges.tsx       ✅ MFA/Locked/Anon Badges (Task 008)
│   │   │   │   ├── edit-invite-modal.tsx        ✅ Edit Invite Modal (Task 010)
│   │   │   │   └── index.ts                     ✅ Barrel Export
│   │   │   ├── types/
│   │   │   │   ├── index.ts             ✅ User Types (+ security fields Task 008)
│   │   │   │   └── invites.ts           ✅ Invite Types (Task 006)
│   │   │   ├── __tests__/
│   │   │   │   ├── users-table.test.tsx         ✅
│   │   │   │   ├── invitations-table.test.tsx   ✅ (Task 006)
│   │   │   │   ├── invite-details-sheet.test.tsx ✅ (Task 006)
│   │   │   │   ├── user-invite-modal.test.tsx   ✅
│   │   │   │   ├── use-system-users.test.ts     ✅
│   │   │   │   ├── use-revoke-invite.test.ts    ✅ (Task 006)
│   │   │   │   └── use-resend-invite.test.ts    ✅ (Task 006)
│   │   │   └── index.ts                 ✅ Barrel Export
│   │   │
│   │   ├── settings/              [SETTINGS FEATURE - Task 008]
│   │   │   ├── components/
│   │   │   │   ├── security-settings.tsx    ✅ MFA + Sessions Settings
│   │   │   │   ├── language-settings.tsx    ✅ Language Selector
│   │   │   │   ├── mfa-section.tsx          ✅ MFA Enable/Disable UI
│   │   │   │   ├── sessions-section.tsx     ✅ Sessions Management
│   │   │   │   └── index.ts                 ✅ Barrel Export
│   │   │   └── index.ts                 ✅ Barrel Export
│   │   │
│   │   ├── permissions/           [PERMISSIONS FEATURE - Task 004]
│   │   │   ├── api/
│   │   │   │   └── permissions-api.ts   ✅ Permissions API
│   │   │   ├── hooks/
│   │   │   │   ├── use-system-permissions.ts ✅ Permissions query
│   │   │   │   └── index.ts             ✅ Barrel Export
│   │   │   ├── types/
│   │   │   │   └── index.ts             ✅ Permission Types
│   │   │   ├── components/              [LEER]
│   │   │   └── index.ts                 ✅ Barrel Export
│   │   │
│   │   ├── audit-logs/            [AUDIT LOGS FEATURE - Task 004/006]
│   │   │   ├── api/
│   │   │   │   └── audit-logs-api.ts    ✅ Audit Logs API
│   │   │   ├── hooks/
│   │   │   │   ├── use-audit-logs.ts        ✅ Audit logs query
│   │   │   │   ├── use-audit-log-filters.ts ✅ Filter options query
│   │   │   │   └── index.ts                 ✅ Barrel Export
│   │   │   ├── components/
│   │   │   │   ├── audit-logs-table.tsx         ✅ Audit Logs DataTable
│   │   │   │   ├── audit-logs-table-columns.tsx ✅ Column Definitions
│   │   │   │   ├── audit-log-details-sheet.tsx  ✅ Details Sheet
│   │   │   │   └── index.ts                     ✅ Barrel Export
│   │   │   ├── types/
│   │   │   │   └── index.ts             ✅ Audit Log Types
│   │   │   └── index.ts                 ✅ Barrel Export
│   │   │
│   │   └── roles/                 [ROLES FEATURE - Placeholder]
│   │       ├── api/               [LEER]
│   │       ├── hooks/             [LEER]
│   │       ├── components/        [LEER]
│   │       └── types/             [LEER]
│   │
│   ├── hooks/                     [GLOBAL HOOKS]
│   │   ├── use-debounce.ts        ✅
│   │   ├── use-local-storage.ts   ✅
│   │   ├── use-media-query.ts     ✅ (+ useIsMobile, useIsDesktop)
│   │   ├── use-copy-to-clipboard.ts ✅
│   │   ├── use-table-preferences.ts ✅
│   │   └── index.ts               ✅ Barrel Export
│   │
│   ├── i18n/                      [INTERNATIONALIZATION]
│   │   ├── index.ts               ✅ i18next Config (+ settings, mfa, sessions namespaces)
│   │   └── locales/
│   │       ├── en/
│   │       │   ├── common.json    ✅ (+ typeToConfirm, states Task 008)
│   │       │   ├── auth.json      ✅ (+ forgot password, MFA texts Task 008)
│   │       │   ├── navigation.json ✅
│   │       │   ├── users.json     ✅ (+ admin actions, security Task 008)
│   │       │   ├── auditLogs.json ✅ (Task 006)
│   │       │   ├── errors.json    ✅ (+ MFA, session, user errors Task 008)
│   │       │   ├── validation.json ✅
│   │       │   ├── settings.json  ✅ Settings translations (Task 008)
│   │       │   ├── mfa.json       ✅ MFA translations (Task 008)
│   │       │   └── sessions.json  ✅ Sessions translations (Task 008)
│   │       └── de/
│   │           ├── common.json    ✅ (+ typeToConfirm, states Task 008)
│   │           ├── auth.json      ✅ (+ forgot password, MFA texts Task 008)
│   │           ├── navigation.json ✅
│   │           ├── users.json     ✅ (+ admin actions, security Task 008)
│   │           ├── auditLogs.json ✅ (Task 006)
│   │           ├── errors.json    ✅ (+ MFA, session, user errors Task 008)
│   │           ├── validation.json ✅
│   │           ├── settings.json  ✅ Settings translations (Task 008)
│   │           ├── mfa.json       ✅ MFA translations (Task 008)
│   │           └── sessions.json  ✅ Sessions translations (Task 008)
│   │
│   ├── lib/
│   │   ├── utils.ts               ✅ (cn helper)
│   │   ├── axios.ts               ✅ API Client mit Interceptors + Force-Reauth
│   │   └── device.ts              ✅ Device ID, Fingerprint Utils (Task 008)
│   │
│   ├── routes/
│   │   ├── __root.tsx             ✅ Root Route mit Layout
│   │   ├── protected-route.tsx    ✅ Auth & Permission Guard
│   │   ├── index-page.tsx         ✅ Landing/Redirect Page
│   │   ├── dashboard.tsx          ✅ Dashboard Page
│   │   ├── login.tsx              ✅ Login Page
│   │   ├── register.tsx           ✅ Register Page
│   │   ├── invite.tsx             ✅ Accept Invite Page (Task 006)
│   │   ├── users.tsx              ✅ Users Management Page
│   │   ├── audit-logs.tsx         ✅ Audit Logs Page
│   │   ├── settings.tsx           ✅ Settings Page (Task 008)
│   │   ├── reset-password.tsx     ✅ Password Reset Page (Task 008)
│   │   ├── legal.tsx              ✅ Imprint/Privacy/Terms Pages (Task 006)
│   │   ├── not-found.tsx          ✅ 404 Page
│   │   ├── forbidden.tsx          ✅ 403 Page
│   │   └── server-error.tsx       ✅ 500 Page
│   │
│   ├── styles/
│   │   └── globals.css            ✅ Tailwind v4 @theme, Rose Theme, Print Styles, A11y
│   │
│   ├── test/
│   │   ├── setup.ts               ✅ Vitest Setup
│   │   └── test-utils.tsx         ✅ Test Utilities & Wrappers
│   │
│   └── types/                     [GLOBAL TYPES]
│       ├── auth.ts                ✅ User, Token, LoginCredentials
│       ├── api.ts                 ✅ ApiResponse, ApiError, Pagination
│       ├── table.ts               ✅ Column, Filter, Sort Definitions
│       └── index.ts               ✅ Barrel Export
│
└── public/
```

---

## Installierte Packages (NICHT NOCHMAL INSTALLIEREN)

### Dependencies
| Package | Version |
|---------|---------|
| react | ^19.2.0 |
| react-dom | ^19.2.0 |
| @tanstack/react-query | ^5.90.12 |
| @tanstack/react-router | ^1.143.4 |
| @tanstack/react-table | ^8.21.3 |
| axios | ^1.13.2 |
| react-hook-form | ^7.69.0 |
| @hookform/resolvers | ^5.2.2 |
| zod | ^4.2.1 |
| clsx | ^2.1.1 |
| tailwind-merge | ^3.4.0 |
| class-variance-authority | ^0.7.1 |
| lucide-react | ^0.562.0 |
| sonner | ^2.0.7 |
| next-themes | ^0.4.6 |
| i18next | ^25.7.3 |
| react-i18next | ^16.5.0 |
| i18next-browser-languagedetector | ^8.2.0 |
| cmdk | ^1.1.1 |
| date-fns | ^4.1.0 |
| react-intersection-observer | ^10.0.0 |
| @radix-ui/react-alert-dialog | ^1.1.15 |
| @radix-ui/react-avatar | ^1.1.11 |
| @radix-ui/react-checkbox | ^1.3.3 |
| @radix-ui/react-dialog | ^1.1.15 |
| @radix-ui/react-dropdown-menu | ^2.1.16 |
| @radix-ui/react-label | ^2.1.8 |
| @radix-ui/react-popover | ^1.1.15 |
| @radix-ui/react-progress | ^1.1.8 |
| @radix-ui/react-scroll-area | ^1.2.10 |
| @radix-ui/react-select | ^2.2.6 |
| @radix-ui/react-separator | ^1.1.8 |
| @radix-ui/react-slot | ^1.2.4 |
| @radix-ui/react-tooltip | ^1.2.8 |
| qrcode.react | ^4.2.0 | (Task 008 - MFA QR Code)

### DevDependencies
| Package | Version |
|---------|---------|
| typescript | ~5.9.3 |
| vite | ^7.2.4 |
| vitest | ^4.0.16 |
| @vitejs/plugin-react | ^5.1.1 |
| tailwindcss | ^4.1.18 |
| @tailwindcss/vite | ^4.1.18 |
| postcss | ^8.5.6 |
| autoprefixer | ^10.4.23 |
| eslint | ^9.39.1 |
| @testing-library/react | ^16.3.1 |
| @testing-library/dom | ^10.4.1 |
| @testing-library/jest-dom | ^6.9.1 |
| jsdom | ^27.3.0 |
| @types/node | ^25.0.3 |
| @types/react | ^19.2.5 |
| @types/react-dom | ^19.2.3 |

---

## Shadcn/UI Komponenten

### Installiert (24 Komponenten)
| Komponente | Datei | Status |
|------------|-------|--------|
| Alert | `src/components/ui/alert.tsx` | ✅ (Task 008) |
| Alert Dialog | `src/components/ui/alert-dialog.tsx` | ✅ |
| Avatar | `src/components/ui/avatar.tsx` | ✅ |
| Badge | `src/components/ui/badge.tsx` | ✅ |
| Breadcrumb | `src/components/ui/breadcrumb.tsx` | ✅ |
| Button | `src/components/ui/button.tsx` | ✅ |
| Calendar | `src/components/ui/calendar.tsx` | ✅ (Task 006) |
| Checkbox | `src/components/ui/checkbox.tsx` | ✅ |
| Command | `src/components/ui/command.tsx` | ✅ |
| Dialog | `src/components/ui/dialog.tsx` | ✅ |
| Dropdown Menu | `src/components/ui/dropdown-menu.tsx` | ✅ |
| Input | `src/components/ui/input.tsx` | ✅ |
| Label | `src/components/ui/label.tsx` | ✅ |
| Popover | `src/components/ui/popover.tsx` | ✅ |
| Progress | `src/components/ui/progress.tsx` | ✅ |
| Scroll Area | `src/components/ui/scroll-area.tsx` | ✅ |
| Separator | `src/components/ui/separator.tsx` | ✅ |
| Sheet | `src/components/ui/sheet.tsx` | ✅ |
| Skeleton | `src/components/ui/skeleton.tsx` | ✅ |
| Sonner (Toast) | `src/components/ui/sonner.tsx` | ✅ |
| Switch | `src/components/ui/switch.tsx` | ✅ (Task 006) |
| Table | `src/components/ui/table.tsx` | ✅ |
| Tabs | `src/components/ui/tabs.tsx` | ✅ (Task 006) |
| Tooltip | `src/components/ui/tooltip.tsx` | ✅ |

### Noch nicht installiert (bei Bedarf)
Wenn benötigt, mit `npx shadcn@latest add [name]` installieren:

- accordion, aspect-ratio, card
- carousel, chart, collapsible, context-menu
- date-picker, drawer, form, hover-card, menubar
- navigation-menu, pagination, radio-group, resizable
- select, slider, textarea, toggle, toggle-group

---

## Verfügbare Shared Components

### Layout (`@/components/shared/layout`)
| Component | Import | Beschreibung |
|-----------|--------|--------------|
| AppLayout | `import { AppLayout } from '@/components/shared/layout'` | Main layout mit Sidebar/Header |
| Sidebar | `import { Sidebar } from '@/components/shared/layout'` | Collapsible navigation |
| Header | `import { Header } from '@/components/shared/layout'` | Top bar mit breadcrumbs |
| Footer | `import { Footer } from '@/components/shared/layout'` | Legal links |
| PageHeader | `import { PageHeader } from '@/components/shared/layout'` | Page title + actions |
| Breadcrumbs | `import { Breadcrumbs } from '@/components/shared/layout'` | Navigation trail |
| UserMenu | `import { UserMenu } from '@/components/shared/layout'` | Profile dropdown |
| ThemeToggle | `import { ThemeToggle } from '@/components/shared/layout'` | Dark/Light switch |
| LanguageSwitcher | `import { LanguageSwitcher } from '@/components/shared/layout'` | EN/DE switch |
| MobileNav | `import { MobileNav } from '@/components/shared/layout'` | Mobile hamburger menu |

### Feedback (`@/components/shared/feedback`)
| Component | Import | Beschreibung |
|-----------|--------|--------------|
| LoadingSpinner | `import { LoadingSpinner } from '@/components/shared/feedback'` | Spinner |
| EmptyState | `import { EmptyState } from '@/components/shared/feedback'` | No data view |
| ErrorState | `import { ErrorState } from '@/components/shared/feedback'` | Error display |
| ConfirmDialog | `import { ConfirmDialog } from '@/components/shared/feedback'` | Simple confirm |
| TypeConfirmDialog | `import { TypeConfirmDialog } from '@/components/shared/feedback'` | Type to confirm |
| UnsavedWarning | `import { UnsavedWarning } from '@/components/shared/feedback'` | Leave warning |

### DataTable (`@/components/shared/data-table`)
| Component | Import | Beschreibung |
|-----------|--------|--------------|
| DataTable | `import { DataTable } from '@/components/shared/data-table'` | Main table |
| DataTableToolbar | `import { DataTableToolbar } from '@/components/shared/data-table'` | Search/Filter bar |
| DataTableFilters | `import { DataTableFilters } from '@/components/shared/data-table'` | Filter dropdown |
| DataTableColumnToggle | `import { DataTableColumnToggle } from '@/components/shared/data-table'` | Column visibility |
| DataTablePagination | `import { DataTablePagination } from '@/components/shared/data-table'` | Infinite scroll |
| DataTableRowActions | `import { DataTableRowActions } from '@/components/shared/data-table'` | Row menu |
| DataTableBulkActions | `import { DataTableBulkActions } from '@/components/shared/data-table'` | Bulk actions bar |
| DataTableCard | `import { DataTableCard } from '@/components/shared/data-table'` | Mobile card view |

### Form (`@/components/shared/form`)
| Component | Import | Beschreibung |
|-----------|--------|--------------|
| PasswordInput | `import { PasswordInput } from '@/components/shared/form'` | Password + toggle |
| PasswordStrength | `import { PasswordStrength } from '@/components/shared/form'` | Strength indicator |
| FormSheet | `import { FormSheet } from '@/components/shared/form'` | Slide-out form |
| FormModal | `import { FormModal } from '@/components/shared/form'` | Modal form |
| DateRangePicker | `import { DateRangePicker } from '@/components/shared/form'` | Date range selection (Task 006) |
| SelectFilter | `import { SelectFilter } from '@/components/shared/form'` | Multi-select filter (Task 006) |

### Utility (`@/components/shared`)
| Component | Import | Beschreibung |
|-----------|--------|--------------|
| UserAvatar | `import { UserAvatar } from '@/components/shared'` | Initials avatar |
| StatusBadge | `import { StatusBadge } from '@/components/shared'` | Status pill |
| CopyButton | `import { CopyButton } from '@/components/shared'` | Copy to clipboard |
| RelativeTime | `import { RelativeTime } from '@/components/shared'` | "2h ago" |
| CommandMenu | `import { CommandMenu } from '@/components/shared'` | Cmd+K search |
| SessionWarning | `import { SessionWarning } from '@/components/shared'` | Session timeout |
| CookieConsent | `import { CookieConsent } from '@/components/shared'` | GDPR banner |
| HelpButton | `import { HelpButton } from '@/components/shared'` | Floating help |

---

## Verfügbare Hooks

| Hook | Import | Beschreibung |
|------|--------|--------------|
| useDebounce | `import { useDebounce } from '@/hooks'` | Debounce values |
| useLocalStorage | `import { useLocalStorage } from '@/hooks'` | LocalStorage state |
| useMediaQuery | `import { useMediaQuery } from '@/hooks'` | Responsive checks |
| useIsMobile | `import { useIsMobile } from '@/hooks'` | < 768px check |
| useIsDesktop | `import { useIsDesktop } from '@/hooks'` | >= 1024px check |
| useCopyToClipboard | `import { useCopyToClipboard } from '@/hooks'` | Copy functionality |
| useTablePreferences | `import { useTablePreferences } from '@/hooks'` | Table state persistence |

---

## Verfügbare Contexts

| Context | Hook | Beschreibung |
|---------|------|--------------|
| AuthContext | `useAuth()` | User, permissions, login/logout |
| ThemeContext | `useTheme()` | Dark/Light/System mode |
| SidebarContext | `useSidebar()` | Sidebar collapsed state |

---

## i18n Namespaces

Siehe `docs/standards/i18n-translations.md` für alle Translation Keys.

| Namespace | Datei | Beschreibung |
|-----------|-------|--------------|
| common | `common.json` | Buttons, Labels, Status, Time, TypeConfirm |
| auth | `auth.json` | Login, Register, Session, Forgot Password |
| navigation | `navigation.json` | Sidebar, Breadcrumbs, Menus |
| users | `users.json` | User Management, Admin Actions, Security Status |
| errors | `errors.json` | Error Messages (incl. MFA, Sessions, Users) |
| validation | `validation.json` | Form Validation |
| settings | `settings.json` | Settings Page (Task 008) |
| mfa | `mfa.json` | MFA Setup, Verify, Disable, Backup Codes (Task 008) |
| sessions | `sessions.json` | Device Sessions Management (Task 008) |
| auditLogs | `auditLogs.json` | Audit Logs (Task 006) |

---

## Code Strukturen (Copy-Paste Templates)

### API Client
```typescript
// src/features/{feature}/api/{name}-api.ts
import apiClient from '@/lib/axios'
import type { CreateRequest, Response } from '../types'

export const {feature}Api = {
  create: (data: CreateRequest) =>
    apiClient.post<Response>('/api/{feature}', data),

  getAll: () =>
    apiClient.get<Response[]>('/api/{feature}'),

  getById: (id: string) =>
    apiClient.get<Response>(`/api/{feature}/${id}`),

  update: (id: string, data: UpdateRequest) =>
    apiClient.put<Response>(`/api/{feature}/${id}`, data),

  delete: (id: string) =>
    apiClient.delete(`/api/{feature}/${id}`),
}
```

### React Query Hook
```typescript
// src/features/{feature}/hooks/use-{name}.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { {feature}Api } from '../api/{name}-api'

export const use{Name}s = () => {
  return useQuery({
    queryKey: ['{feature}'],
    queryFn: () => {feature}Api.getAll(),
  })
}

export const useCreate{Name} = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: {feature}Api.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['{feature}'] })
    },
  })
}
```

### Feature Component with i18n
```typescript
// src/features/{feature}/components/{name}-list.tsx
'use client'

import { useTranslation } from 'react-i18next'
import { DataTable } from '@/components/shared/data-table'
import { PageHeader } from '@/components/shared/layout'
import { EmptyState, LoadingSpinner } from '@/components/shared/feedback'
import { use{Name}s } from '../hooks/use-{name}'

export function {Name}List() {
  const { t } = useTranslation()
  const { data, isLoading, error } = use{Name}s()

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorState error={error} />
  if (!data?.length) return <EmptyState title={t('{feature}:empty.title')} />

  return (
    <div>
      <PageHeader
        title={t('{feature}:title')}
        description={t('{feature}:subtitle')}
      />
      <DataTable data={data} columns={columns} />
    </div>
  )
}
```

### Types
```typescript
// src/features/{feature}/types/index.ts
export interface {Name} {
  id: string
  // properties...
  createdAt: string
  updatedAt: string
}

export interface Create{Name}Request {
  // properties...
}

export interface Update{Name}Request {
  // properties...
}
```

### Feature Index (Barrel Export)
```typescript
// src/features/{feature}/index.ts
export * from './components/{name}-form'
export * from './hooks/use-{name}'
export * from './types'
```

---

## Regeln für Task Erstellung

1. **IMMER** zuerst diese Datei lesen
2. **IMMER** prüfen ob Files/Packages/Components schon existieren
3. **IMMER** die Reihenfolge einhalten: Types → API → Hooks → Components → Routes
4. **IMMER** Tests mit einplanen
5. **IMMER** am Ende diese Datei updaten (siehe unten)
6. **IMMER** i18n Keys für alle User-facing Text verwenden
7. **IMMER** bestehende Shared Components nutzen (DataTable, Feedback, Form, Layout)
8. **NIE** Packages doppelt installieren
9. **NIE** Shadcn Komponenten doppelt installieren
10. **NIE** Files überschreiben ohne zu fragen
11. **IMMER** barrel exports in feature/index.ts pflegen

---

## Nach Task Completion: Standards Pflege

> **WICHTIG**: Siehe `coding_standards_frontend.md` → "Standards & Task File Maintenance" für die vollständige Anleitung.

### Quick Checklist

Nach jeder Task MÜSSEN diese Dateien aktualisiert werden:

| Änderung | Datei aktualisieren |
|----------|---------------------|
| Neue Files erstellt | Diese Datei → "File Tree" |
| NPM Package installiert | Diese Datei → "Installierte Packages" |
| Shadcn Component hinzugefügt | Diese Datei → "Shadcn/UI Komponenten" |
| Shared Component erstellt | Diese Datei → "Verfügbare Shared Components" |
| Hook erstellt | Diese Datei → "Verfügbare Hooks" |
| Context erstellt | Diese Datei → "Verfügbare Contexts" |
| i18n Keys hinzugefügt | `i18n-translations.md` |
| Neue Code Patterns | `coding_standards_frontend.md` |

### Task-Datei Status

In der Task-Datei (`docs/tasks/XXX_*.md`):
- Alle erstellten Files mit ✅ markieren
- "Letzte Änderung" Datum updaten
- Status auf "Complete" oder "In Progress" setzen

---

## Letzte Änderung

- **Datum:** 2025-12-31
- **Status:** Task 012 In Progress (Phases 3, 4, 5 complete)
- **Completed Tasks:**
  - Task 003: Foundation Complete
  - Task 004: Auth, Users, Permissions, Audit-Logs Features
  - Task 006: Frontend Improvements (Bugs, Invitations, Legal Pages, Security)
  - Task 008: Frontend Security & Settings (MFA, Sessions, Password Reset, Admin Actions)
  - Task 010: Frontend List Improvements (SelectFilter Bug, User/Invite Filters, Edit Invite, Session Details Sheet)
  - Task 012 (partial): Multi-Tab Auth Sync, Session-Expired Redirect, Audit Log Anonymized Users Filter, Error Message Update
- **In Progress:** Task 012 - Cache Bug Fix, Single Session Revoke UI
- **Nächster Task:** Task 012 completion, then Roles Feature, Tests

# Frontend Reference - ExoAuth

> **Read this file completely before any frontend work.**

---

## File Tree (Current State)

```
frontend/
├── package.json
├── vite.config.ts
├── vitest.config.ts
├── tsconfig.json
├── components.json                 (shadcn config)
│
├── src/
│   ├── main.tsx
│   │
│   ├── app/
│   │   ├── providers.tsx          # QueryClient, Theme, Auth, Sidebar, Toaster
│   │   └── router.tsx             # TanStack Router Setup
│   │
│   ├── components/
│   │   ├── ui/                    [SHADCN - 24 components, DON'T EDIT!]
│   │   │   ├── alert.tsx
│   │   │   ├── alert-dialog.tsx
│   │   │   ├── avatar.tsx
│   │   │   ├── badge.tsx
│   │   │   ├── breadcrumb.tsx
│   │   │   ├── button.tsx
│   │   │   ├── calendar.tsx
│   │   │   ├── checkbox.tsx
│   │   │   ├── command.tsx
│   │   │   ├── dialog.tsx
│   │   │   ├── dropdown-menu.tsx
│   │   │   ├── input.tsx
│   │   │   ├── label.tsx
│   │   │   ├── popover.tsx
│   │   │   ├── progress.tsx
│   │   │   ├── scroll-area.tsx
│   │   │   ├── separator.tsx
│   │   │   ├── sheet.tsx
│   │   │   ├── skeleton.tsx
│   │   │   ├── sonner.tsx
│   │   │   ├── switch.tsx
│   │   │   ├── table.tsx
│   │   │   ├── tabs.tsx
│   │   │   └── tooltip.tsx
│   │   │
│   │   └── shared/
│   │       ├── index.ts
│   │       ├── layout/
│   │       │   ├── app-layout.tsx
│   │       │   ├── sidebar.tsx
│   │       │   ├── header.tsx
│   │       │   ├── user-menu.tsx
│   │       │   ├── theme-toggle.tsx
│   │       │   ├── language-switcher.tsx
│   │       │   ├── breadcrumbs.tsx
│   │       │   ├── page-header.tsx
│   │       │   ├── footer.tsx
│   │       │   ├── mobile-nav.tsx
│   │       │   └── index.ts
│   │       ├── feedback/
│   │       │   ├── loading-spinner.tsx
│   │       │   ├── empty-state.tsx
│   │       │   ├── error-state.tsx
│   │       │   ├── confirm-dialog.tsx
│   │       │   ├── type-confirm-dialog.tsx
│   │       │   ├── unsaved-warning.tsx
│   │       │   └── index.ts
│   │       ├── data-table/
│   │       │   ├── data-table.tsx
│   │       │   ├── data-table-toolbar.tsx
│   │       │   ├── data-table-filters.tsx
│   │       │   ├── data-table-column-toggle.tsx
│   │       │   ├── data-table-pagination.tsx
│   │       │   ├── data-table-row-actions.tsx
│   │       │   ├── data-table-bulk-actions.tsx
│   │       │   ├── data-table-card.tsx
│   │       │   └── index.ts
│   │       ├── form/
│   │       │   ├── password-input.tsx
│   │       │   ├── password-strength.tsx
│   │       │   ├── form-sheet.tsx
│   │       │   ├── form-modal.tsx
│   │       │   ├── date-range-picker.tsx
│   │       │   ├── select-filter.tsx
│   │       │   └── index.ts
│   │       ├── user-avatar.tsx
│   │       ├── status-badge.tsx
│   │       ├── copy-button.tsx
│   │       ├── relative-time.tsx
│   │       ├── command-menu.tsx
│   │       ├── session-warning.tsx
│   │       ├── cookie-consent.tsx
│   │       ├── help-button.tsx
│   │       └── global-error-handler.tsx   (Task 024 - 429/403 toasts)
│   │
│   ├── config/
│   │   └── navigation.ts
│   │
│   ├── contexts/
│   │   ├── auth-context.tsx
│   │   ├── theme-context.tsx
│   │   ├── sidebar-context.tsx
│   │   └── index.ts
│   │
│   ├── features/
│   │   ├── auth/
│   │   │   ├── api/
│   │   │   │   ├── auth-api.ts
│   │   │   │   ├── mfa-api.ts
│   │   │   │   ├── password-reset-api.ts
│   │   │   │   ├── preferences-api.ts
│   │   │   │   ├── device-approval-api.ts         (Task 014)
│   │   │   │   ├── devices-api.ts                 (Task 018)
│   │   │   │   ├── passkeys-api.ts                (Task 020)
│   │   │   │   └── captcha-api.ts                 (Task 022)
│   │   │   ├── hooks/
│   │   │   │   ├── use-login.ts
│   │   │   │   ├── use-logout.ts
│   │   │   │   ├── use-register.ts
│   │   │   │   ├── use-current-user.ts
│   │   │   │   ├── use-accept-invite.ts
│   │   │   │   ├── use-validate-invite.ts
│   │   │   │   ├── use-mfa-setup.ts
│   │   │   │   ├── use-mfa-confirm.ts
│   │   │   │   ├── use-mfa-verify.ts
│   │   │   │   ├── use-mfa-disable.ts
│   │   │   │   ├── use-regenerate-backup-codes.ts
│   │   │   │   ├── use-forgot-password.ts
│   │   │   │   ├── use-reset-password.ts
│   │   │   │   ├── use-update-preferences.ts
│   │   │   │   ├── use-devices.ts                 (Task 018)
│   │   │   │   ├── use-revoke-device.ts           (Task 018)
│   │   │   │   ├── use-rename-device.ts           (Task 018)
│   │   │   │   ├── use-approve-device-from-session.ts (Task 018)
│   │   │   │   ├── use-approve-device-by-code.ts  (Task 014)
│   │   │   │   ├── use-approve-device-by-link.ts  (Task 014)
│   │   │   │   ├── use-deny-device.ts             (Task 014)
│   │   │   │   ├── use-passkeys.ts                (Task 020)
│   │   │   │   ├── use-passkey-register-options.ts (Task 020)
│   │   │   │   ├── use-passkey-register.ts        (Task 020)
│   │   │   │   ├── use-passkey-login-options.ts   (Task 020)
│   │   │   │   ├── use-passkey-login.ts           (Task 020)
│   │   │   │   ├── use-rename-passkey.ts          (Task 020)
│   │   │   │   ├── use-delete-passkey.ts          (Task 020)
│   │   │   │   ├── use-webauthn-support.ts        (Task 020)
│   │   │   │   ├── use-captcha-config.ts          (Task 022)
│   │   │   │   └── index.ts
│   │   │   ├── components/
│   │   │   │   ├── login-form.tsx
│   │   │   │   ├── register-form.tsx
│   │   │   │   ├── accept-invite-form.tsx
│   │   │   │   ├── password-requirements.tsx
│   │   │   │   ├── mfa-setup-modal.tsx
│   │   │   │   ├── mfa-confirm-modal.tsx
│   │   │   │   ├── mfa-verify-modal.tsx
│   │   │   │   ├── mfa-disable-modal.tsx
│   │   │   │   ├── backup-codes-display.tsx
│   │   │   │   ├── forgot-password-modal.tsx
│   │   │   │   ├── device-approval-modal.tsx      (Task 014)
│   │   │   │   ├── device-approval-code-input.tsx (Task 014)
│   │   │   │   ├── device-status-badge.tsx        (Task 018)
│   │   │   │   ├── device-card.tsx                (Task 018)
│   │   │   │   ├── devices-list.tsx               (Task 018)
│   │   │   │   ├── device-details-sheet.tsx       (Task 018)
│   │   │   │   ├── rename-device-modal.tsx        (Task 018)
│   │   │   │   ├── passkey-login-button.tsx       (Task 020)
│   │   │   │   ├── passkeys-section.tsx           (Task 020)
│   │   │   │   ├── passkey-card.tsx               (Task 020)
│   │   │   │   ├── passkey-empty-state.tsx        (Task 020)
│   │   │   │   ├── register-passkey-modal.tsx     (Task 020)
│   │   │   │   ├── rename-passkey-modal.tsx       (Task 020)
│   │   │   │   ├── webauthn-not-supported.tsx     (Task 020)
│   │   │   │   ├── captcha-widget.tsx             (Task 022)
│   │   │   │   ├── turnstile-captcha.tsx          (Task 022)
│   │   │   │   ├── recaptcha-v3-captcha.tsx       (Task 022)
│   │   │   │   ├── hcaptcha-captcha.tsx           (Task 022)
│   │   │   │   └── index.ts
│   │   │   ├── types/
│   │   │   │   ├── index.ts
│   │   │   │   ├── mfa.ts
│   │   │   │   ├── password-reset.ts
│   │   │   │   ├── device-approval.ts             (Task 014)
│   │   │   │   ├── device.ts                      (Task 018)
│   │   │   │   ├── passkey.ts                     (Task 020)
│   │   │   │   └── captcha.ts                     (Task 022)
│   │   │   └── index.ts
│   │   │
│   │   ├── users/
│   │   │   ├── api/
│   │   │   │   ├── users-api.ts
│   │   │   │   ├── invites-api.ts
│   │   │   │   ├── user-admin-api.ts
│   │   │   │   └── user-devices-api.ts         (Task 016)
│   │   │   ├── hooks/
│   │   │   │   ├── use-system-users.ts
│   │   │   │   ├── use-system-user.ts
│   │   │   │   ├── use-invite-user.ts
│   │   │   │   ├── use-update-user.ts
│   │   │   │   ├── use-update-permissions.ts
│   │   │   │   ├── use-system-invites.ts
│   │   │   │   ├── use-system-invite.ts
│   │   │   │   ├── use-revoke-invite.ts
│   │   │   │   ├── use-resend-invite.ts
│   │   │   │   ├── use-reset-user-mfa.ts
│   │   │   │   ├── use-unlock-user.ts
│   │   │   │   ├── use-user-sessions.ts
│   │   │   │   ├── use-revoke-user-session.ts
│   │   │   │   ├── use-revoke-user-sessions.ts
│   │   │   │   ├── use-deactivate-user.ts
│   │   │   │   ├── use-activate-user.ts
│   │   │   │   ├── use-anonymize-user.ts
│   │   │   │   ├── use-update-invite.ts
│   │   │   │   ├── use-user-devices.ts              (Task 018)
│   │   │   │   ├── use-revoke-user-device.ts       (Task 018)
│   │   │   │   ├── use-revoke-all-user-devices.ts  (Task 018)
│   │   │   │   └── index.ts
│   │   │   ├── components/
│   │   │   │   ├── users-table.tsx
│   │   │   │   ├── users-table-columns.tsx
│   │   │   │   ├── user-details-sheet.tsx
│   │   │   │   ├── user-edit-modal.tsx
│   │   │   │   ├── user-invite-modal.tsx
│   │   │   │   ├── user-permissions-modal.tsx
│   │   │   │   ├── invitations-table.tsx
│   │   │   │   ├── invitations-table-columns.tsx
│   │   │   │   ├── invite-details-sheet.tsx
│   │   │   │   ├── user-sessions-section.tsx
│   │   │   │   ├── user-devices-section.tsx       (Task 018)
│   │   │   │   ├── user-status-badges.tsx
│   │   │   │   ├── edit-invite-modal.tsx
│   │   │   │   └── index.ts
│   │   │   ├── types/
│   │   │   │   ├── index.ts
│   │   │   │   └── invites.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── settings/
│   │   │   ├── components/
│   │   │   │   ├── language-settings.tsx
│   │   │   │   ├── mfa-section.tsx
│   │   │   │   ├── devices-section.tsx            (Task 018)
│   │   │   │   ├── passkeys-section.tsx           (Task 020)
│   │   │   │   └── index.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── permissions/
│   │   │   ├── api/permissions-api.ts
│   │   │   ├── hooks/use-system-permissions.ts
│   │   │   ├── types/index.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── audit-logs/
│   │   │   ├── api/audit-logs-api.ts
│   │   │   ├── hooks/
│   │   │   │   ├── use-audit-logs.ts
│   │   │   │   ├── use-audit-log-filters.ts
│   │   │   │   └── index.ts
│   │   │   ├── components/
│   │   │   │   ├── audit-logs-table.tsx
│   │   │   │   ├── audit-logs-table-columns.tsx
│   │   │   │   ├── audit-log-details-sheet.tsx
│   │   │   │   └── index.ts
│   │   │   ├── types/index.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── ip-restrictions/                    (Task 024)
│   │   │   ├── api/ip-restrictions-api.ts
│   │   │   ├── hooks/
│   │   │   │   ├── use-ip-restrictions.ts
│   │   │   │   ├── use-create-ip-restriction.ts
│   │   │   │   ├── use-update-ip-restriction.ts
│   │   │   │   ├── use-delete-ip-restriction.ts
│   │   │   │   └── index.ts
│   │   │   ├── components/
│   │   │   │   ├── ip-restrictions-table.tsx      (+ row actions, mobile card actions)
│   │   │   │   ├── ip-restrictions-table-columns.tsx (+ actions column)
│   │   │   │   ├── ip-restriction-details-sheet.tsx  (+ edit button)
│   │   │   │   ├── create-ip-restriction-modal.tsx   (+ "Get my IP" button)
│   │   │   │   ├── edit-ip-restriction-modal.tsx
│   │   │   │   ├── ip-restriction-type-badge.tsx
│   │   │   │   ├── ip-restriction-source-badge.tsx
│   │   │   │   └── index.ts
│   │   │   ├── types/index.ts
│   │   │   └── index.ts
│   │   │
│   │   └── roles/                 [PLACEHOLDER - Empty]
│   │
│   ├── hooks/
│   │   ├── use-debounce.ts
│   │   ├── use-local-storage.ts
│   │   ├── use-media-query.ts     (+ useIsMobile, useIsDesktop)
│   │   ├── use-copy-to-clipboard.ts
│   │   ├── use-table-preferences.ts
│   │   └── index.ts
│   │
│   ├── i18n/
│   │   ├── index.ts
│   │   └── locales/
│   │       ├── en/
│   │       │   ├── common.json
│   │       │   ├── auth.json
│   │       │   ├── navigation.json
│   │       │   ├── users.json
│   │       │   ├── auditLogs.json
│   │       │   ├── settings.json
│   │       │   ├── mfa.json
│   │       │   ├── sessions.json
│   │       │   ├── errors.json
│   │       │   └── validation.json
│   │       └── de/
│   │           └── (same files)
│   │
│   ├── lib/
│   │   ├── utils.ts               (cn helper)
│   │   ├── axios.ts               (API client + interceptors)
│   │   ├── device.ts              (Device ID, Fingerprint)
│   │   └── webauthn.ts            (WebAuthn helpers, Task 020)
│   │
│   ├── routes/
│   │   ├── __root.tsx
│   │   ├── protected-route.tsx
│   │   ├── index-page.tsx
│   │   ├── dashboard.tsx
│   │   ├── login.tsx
│   │   ├── register.tsx
│   │   ├── invite.tsx
│   │   ├── users.tsx
│   │   ├── audit-logs.tsx
│   │   ├── settings.tsx
│   │   ├── reset-password.tsx
│   │   ├── approve-device.tsx     (Task 014)
│   │   ├── legal.tsx
│   │   ├── not-found.tsx
│   │   ├── ip-restrictions.tsx          (Task 024)
│   │   ├── forbidden.tsx
│   │   └── server-error.tsx
│   │
│   ├── styles/
│   │   └── globals.css
│   │
│   └── types/
│       ├── auth.ts
│       ├── api.ts
│       ├── table.ts
│       └── index.ts
│
└── public/
```

---

## Installed Packages (DO NOT REINSTALL)

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
| qrcode.react | ^4.2.0 |
| @simplewebauthn/browser | ^13.2.2 |
| @marsidev/react-turnstile | ^1.1.4 |
| react-google-recaptcha-v3 | ^1.10.1 |
| @hcaptcha/react-hcaptcha | ^1.11.0 |
| @radix-ui/* | various |

### DevDependencies
| Package | Version |
|---------|---------|
| typescript | ~5.9.3 |
| vite | ^7.2.4 |
| vitest | ^4.0.16 |
| @vitejs/plugin-react | ^5.1.1 |
| tailwindcss | ^4.1.18 |
| @testing-library/react | ^16.3.1 |
| jsdom | ^27.3.0 |

---

## Shadcn/UI Components (25 installed)

**Installed:** alert, alert-dialog, avatar, badge, breadcrumb, button, calendar, checkbox, command, dialog, dropdown-menu, input, label, popover, progress, scroll-area, select, separator, sheet, skeleton, sonner, switch, table, tabs, tooltip

**To add more:** `npx shadcn@latest add [name]`

---

## Available Shared Components

### Layout (`@/components/shared/layout`)
- `AppLayout` - Main layout wrapper
- `Sidebar` - Collapsible nav
- `Header` - Top bar
- `Footer` - Legal links
- `PageHeader` - Page title + actions
- `Breadcrumbs` - Navigation trail
- `UserMenu` - Profile dropdown
- `ThemeToggle` - Dark/Light switch
- `LanguageSwitcher` - EN/DE switch
- `MobileNav` - Hamburger menu

### Feedback (`@/components/shared/feedback`)
- `LoadingSpinner` - Spinner
- `EmptyState` - No data view
- `ErrorState` - Error display
- `ConfirmDialog` - Simple confirm
- `TypeConfirmDialog` - Type to confirm
- `UnsavedWarning` - Leave warning

### DataTable (`@/components/shared/data-table`)
- `DataTable` - Main table
- `DataTableToolbar` - Search/Filter bar
- `DataTableFilters` - Filter dropdown
- `DataTableColumnToggle` - Column visibility
- `DataTablePagination` - Infinite scroll
- `DataTableRowActions` - Row menu
- `DataTableBulkActions` - Bulk actions
- `DataTableCard` - Mobile card view (supports `icon` prop for custom icons)

### Form (`@/components/shared/form`)
- `PasswordInput` - Password + toggle
- `PasswordStrength` - Strength indicator
- `FormSheet` - Slide-out form
- `FormModal` - Modal form
- `DateRangePicker` - Date range
- `SelectFilter` - Multi-select

### Utility (`@/components/shared`)
- `UserAvatar` - Initials avatar
- `StatusBadge` - Status pill
- `CopyButton` - Copy to clipboard
- `RelativeTime` - "2h ago"
- `CommandMenu` - Cmd+K search
- `SessionWarning` - Session timeout
- `CookieConsent` - GDPR banner
- `HelpButton` - Floating help

---

## Available Hooks

| Hook | Import | Description |
|------|--------|-------------|
| `useDebounce` | `@/hooks` | Debounce values |
| `useLocalStorage` | `@/hooks` | LocalStorage state |
| `useMediaQuery` | `@/hooks` | Responsive checks |
| `useIsMobile` | `@/hooks` | < 768px |
| `useIsDesktop` | `@/hooks` | >= 1024px |
| `useCopyToClipboard` | `@/hooks` | Copy function |
| `useTablePreferences` | `@/hooks` | Table state |

---

## Contexts

| Context | Hook | Description |
|---------|------|-------------|
| AuthContext | `useAuth()` | User, permissions, login/logout |
| ThemeContext | `useTheme()` | Dark/Light/System |
| SidebarContext | `useSidebar()` | Collapsed state |

---

## i18n Namespaces

| Namespace | File | Description |
|-----------|------|-------------|
| common | common.json | Buttons, Labels, Status |
| auth | auth.json | Login, Register, MFA |
| navigation | navigation.json | Sidebar, Breadcrumbs |
| users | users.json | User Management |
| errors | errors.json | Error Messages |
| validation | validation.json | Form Validation |
| settings | settings.json | Settings Page |
| mfa | mfa.json | MFA texts |
| sessions | sessions.json | Sessions texts |
| auditLogs | auditLogs.json | Audit Logs |
| ipRestrictions | ipRestrictions.json | IP Restrictions (Task 024) |

---

## Coding Standards

### Naming Conventions
| What | Convention | Example |
|------|------------|---------|
| Components | PascalCase | `UserForm` |
| Component Files | kebab-case.tsx | `user-form.tsx` |
| Hooks | camelCase + use | `useUsers` |
| Hook Files | kebab-case.ts | `use-users.ts` |
| API Files | kebab-case.ts | `users-api.ts` |
| i18n Keys | dot.notation | `users.title` |

### DO's ✅
- TypeScript strict mode
- Functional components
- Named exports (no default)
- Barrel exports in index.ts
- `cn()` for conditional classes
- React Query for server state
- `useTranslation()` for ALL user-facing text
- Use Shared Components

### DON'Ts ❌
- No `any` types
- No inline styles
- No hardcoded strings (use i18n)
- No console.log in production
- No business logic in components
- No API calls directly in components (use hooks)
- Don't edit Shadcn/UI files!
- No files > 200 lines
- No components > 100 lines

---

## Code Templates

### API Client
```typescript
import apiClient from '@/lib/axios'
import type { CreateRequest, Response } from '../types'

export const featureApi = {
  getAll: () => apiClient.get<ApiResponse<Response[]>>('/api/feature'),
  getById: (id: string) => apiClient.get<ApiResponse<Response>>(`/api/feature/${id}`),
  create: (data: CreateRequest) => apiClient.post<ApiResponse<Response>>('/api/feature', data),
}
```

### React Query Hook
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { featureApi } from '../api/feature-api'

const FEATURE_KEY = ['feature'] as const

export function useFeatures() {
  return useQuery({
    queryKey: FEATURE_KEY,
    queryFn: featureApi.getAll,
  })
}

export function useCreateFeature() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: featureApi.create,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: FEATURE_KEY }),
  })
}
```

### Component with i18n
```typescript
import { useTranslation } from 'react-i18next'
import { DataTable } from '@/components/shared/data-table'
import { PageHeader } from '@/components/shared/layout'
import { LoadingSpinner, EmptyState } from '@/components/shared/feedback'
import { useFeatures } from '../hooks/use-features'

export function FeatureList() {
  const { t } = useTranslation()
  const { data, isLoading, error } = useFeatures()

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorState error={error} />
  if (!data?.length) return <EmptyState title={t('feature:empty.title')} />

  return (
    <div>
      <PageHeader title={t('feature:title')} />
      <DataTable data={data} columns={columns} />
    </div>
  )
}
```

---

## Import Order

```typescript
// 1. React
import { useState, useEffect } from 'react'

// 2. Third-party
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

// 3. UI Components (shadcn)
import { Button } from '@/components/ui/button'

// 4. Shared components
import { DataTable } from '@/components/shared/data-table'

// 5. Contexts & Hooks
import { useAuth } from '@/contexts'

// 6. Feature imports
import { useFeatures } from '../hooks/use-features'

// 7. Types
import type { Feature } from '../types'
```

---

## Implementation Order

1. **Types**: TypeScript interfaces
2. **API**: API client functions
3. **Hooks**: React Query hooks
4. **Components**: UI components
5. **Route**: Page/Route
6. **i18n**: Add translations (EN + DE!)
7. **Tests**: Component + Hook tests
8. **Update this memory file**

---

## Last Updated
- **Date:** 2026-01-07
- **Tasks Completed:** 003, 004, 006, 008, 010, 012, 014, 016, 018, 020, 022, 024

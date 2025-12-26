# Task: Frontend - System Authentication & User Management

## 1. Übersicht

**Was wird gebaut?**
Frontend-Implementation für System-Level Authentication und User Management: Login, Register, Accept Invite, Dashboard, System Users CRUD, Permissions Management und Audit Logs.

**Warum?**
- Backend APIs sind fertig (Task 002)
- Frontend Foundation ist fertig (Task 003)
- SystemUsers können die Plattform verwalten

**Architektur-Kontext:**
```
┌─────────────────────────────────────────────────────────────┐
│  FRONTEND (Diese Task)                                      │
│  ├── Auth Pages (Login, Register, Accept Invite)            │
│  ├── Dashboard (Placeholder)                                │
│  ├── System Users (List, Invite, Edit, Permissions)         │
│  ├── Audit Logs (List with Filters)                         │
│  └── Auth Context (Real Implementation)                     │
├─────────────────────────────────────────────────────────────┤
│  BACKEND (Task 002 - Fertig)                                │
│  ├── /api/auth/* (Register, Login, Refresh, Logout, Me)     │
│  ├── /api/system/users/* (CRUD, Invite, Permissions)        │
│  ├── /api/system/permissions (List)                         │
│  └── /api/system/audit-logs (List, Filters)                 │
└─────────────────────────────────────────────────────────────┘
```

## 2. User Experience / Anforderungen

### User Stories

- Als **erster Benutzer** möchte ich mich registrieren können, damit ich ExoAuth einrichten kann
- Als **SystemUser** möchte ich mich einloggen können, damit ich auf das Dashboard zugreifen kann
- Als **eingeladener Benutzer** möchte ich mein Passwort setzen können, damit ich Zugang bekomme
- Als **SystemUser** möchte ich andere Users einladen und verwalten können
- Als **SystemUser** möchte ich Berechtigungen anderer Users verwalten können
- Als **SystemUser** möchte ich alle System-Aktionen im Audit-Log sehen können

### UI/UX Beschreibung

**Login Page (`/login`):**
- Centered form with Email + Password
- Link to Register page
- Error messages for invalid credentials, inactive user, too many attempts

**Register Page (`/register`):**
- Centered form with Email, Password, First Name, Last Name
- Password strength indicator with requirements checklist
- Friendly error if registration closed

**Accept Invite Page (`/invite?token=xxx`):**
- Password set form with strength indicator
- Confirm password field
- Auto-login after success

**Dashboard (`/dashboard`):**
- Welcome card with user name
- Placeholder for future stats

**Users List (`/system/users`):**
- DataTable with search, sort, infinite scroll
- Columns: Name, Email, Status, Permissions Count, Last Login, Actions
- Actions: Edit, Permissions, Delete

**Invite User (Sheet):**
- Slide-out form: Email, First Name, Last Name, Permissions checkboxes

**Edit User (Sheet):**
- Slide-out form: First Name, Last Name, Active toggle

**Permissions Modal:**
- Checkboxes grouped by category
- Save replaces all permissions

**Audit Logs (`/system/audit-logs`):**
- DataTable with filters (Action, User, Date Range)
- Columns: Time, User, Action, Entity, IP Address

### Akzeptanzkriterien

- [ ] Login funktioniert mit Email + Password
- [ ] Register funktioniert für ersten User
- [ ] Accept Invite setzt Passwort und logged ein
- [ ] Dashboard zeigt Welcome message
- [ ] Users Liste mit Pagination, Search, Sort
- [ ] User Invite sendet Email
- [ ] User Edit updated Profile
- [ ] Permissions können geändert werden
- [ ] Audit Logs mit Filtern anzeigbar
- [ ] Alle Texte sind i18n translated
- [ ] Alle Errors werden übersetzt angezeigt
- [ ] Protected Routes funktionieren
- [ ] Logout funktioniert

### Edge Cases / Error Handling

| Error Code | HTTP | Translated Message (DE) | UI Action |
|------------|------|-------------------------|-----------|
| `AUTH_INVALID_CREDENTIALS` | 401 | Ungültige E-Mail oder Passwort | Toast error |
| `AUTH_USER_INACTIVE` | 401 | Benutzerkonto ist deaktiviert | Toast error |
| `AUTH_TOKEN_EXPIRED` | 401 | Sitzung abgelaufen | Auto-refresh or redirect to login |
| `AUTH_REFRESH_TOKEN_INVALID` | 401 | Sitzung ungültig | Redirect to login |
| `AUTH_REGISTRATION_CLOSED` | 400 | Registrierung geschlossen. Kontaktieren Sie einen Administrator. | Show in form |
| `AUTH_EMAIL_EXISTS` | 409 | E-Mail wird bereits verwendet | Show in form |
| `AUTH_PASSWORD_TOO_WEAK` | 400 | Passwort erfüllt die Anforderungen nicht | Show in form |
| `AUTH_TOO_MANY_ATTEMPTS` | 429 | Zu viele Versuche. Bitte 15 Minuten warten. | Toast + disable form |
| `AUTH_INVITE_EXPIRED` | 400 | Einladung abgelaufen. Bitte neue anfordern. | Show message |
| `AUTH_INVITE_INVALID` | 400 | Ungültiger Einladungslink | Show message |
| `SYSTEM_USER_NOT_FOUND` | 404 | Benutzer nicht gefunden | Toast error |
| `SYSTEM_LAST_PERMISSION_HOLDER` | 400 | Kann nicht entfernen - letzter Benutzer mit dieser Berechtigung | Toast error |
| `SYSTEM_CANNOT_DELETE_SELF` | 400 | Kann sich selbst nicht löschen | Toast error |
| `SYSTEM_FORBIDDEN` | 403 | Keine Berechtigung für diese Aktion | Toast error |

## 3. API Integration

### Auth Endpoints

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/auth/register` | POST | `{ email, password, firstName, lastName }` | `{ data: AuthResponse }` | `useRegister` |
| `/api/auth/login` | POST | `{ email, password }` | `{ data: AuthResponse }` | `useLogin` |
| `/api/auth/refresh` | POST | Cookie | `{ data: TokenResponse }` | axios interceptor |
| `/api/auth/logout` | POST | Cookie | `{ data: { success } }` | `useLogout` |
| `/api/auth/me` | GET | - | `{ data: UserDto }` | `useCurrentUser` |
| `/api/auth/accept-invite` | POST | `{ token, password }` | `{ data: AuthResponse }` | `useAcceptInvite` |

### System Users Endpoints

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/system/users` | GET | `?cursor&limit&sort&search` | `{ data: [], meta: { pagination } }` | `useSystemUsers` |
| `/api/system/users/{id}` | GET | - | `{ data: SystemUserDetailDto }` | `useSystemUser` |
| `/api/system/users/invite` | POST | `{ email, firstName, lastName, permissionIds }` | `{ data: SystemInviteDto }` | `useInviteUser` |
| `/api/system/users/{id}` | PUT | `{ firstName?, lastName?, isActive? }` | `{ data: SystemUserDto }` | `useUpdateUser` |
| `/api/system/users/{id}/permissions` | PUT | `{ permissionIds }` | `{ data: SystemUserDetailDto }` | `useUpdatePermissions` |
| `/api/system/users/{id}` | DELETE | - | 204 | `useDeleteUser` |

### System Permissions Endpoints

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/system/permissions` | GET | `?groupByCategory=true` | `{ data: SystemPermissionGroupDto[] }` | `useSystemPermissions` |

### System Audit Logs Endpoints

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/system/audit-logs` | GET | `?cursor&limit&sort&action&userId&from&to` | `{ data: [], meta: { pagination } }` | `useAuditLogs` |
| `/api/system/audit-logs/filters` | GET | - | `{ data: AuditLogFiltersDto }` | `useAuditLogFilters` |

### Response Types

```typescript
// API Response Wrapper
interface ApiResponse<T> {
  status: 'success' | 'error'
  statusCode: number
  message: string
  data: T
  meta?: {
    timestamp: string
    requestId: string
    pagination?: PaginationMeta
  }
  errors?: ApiError[]
}

interface ApiError {
  field?: string
  code: string
  message: string
}

interface PaginationMeta {
  cursor: string | null
  nextCursor: string | null
  hasMore: boolean
  pageSize: number
}

// Auth
interface AuthResponse {
  user: UserDto
  accessToken: string
  refreshToken: string
}

interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
  lastLoginAt: string | null
  createdAt: string
  permissions: string[]
}

// System Users
interface SystemUserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string | null
}

interface SystemUserDetailDto extends SystemUserDto {
  permissions: PermissionDto[]
}

interface PermissionDto {
  id: string
  name: string
  description: string
  category: string
}

interface SystemInviteDto {
  id: string
  email: string
  firstName: string
  lastName: string
  expiresAt: string
  createdAt: string
}

// System Permissions
interface SystemPermissionDto {
  id: string
  name: string
  description: string
  category: string
  createdAt: string
}

interface SystemPermissionGroupDto {
  category: string
  permissions: SystemPermissionDto[]
}

// Audit Logs
interface SystemAuditLogDto {
  id: string
  userId: string | null
  userEmail: string | null
  userFullName: string | null
  action: string
  entityType: string | null
  entityId: string | null
  ipAddress: string | null
  userAgent: string | null
  details: Record<string, unknown> | null
  createdAt: string
}

interface AuditLogFiltersDto {
  actions: string[]
  users: { id: string; email: string; fullName: string }[]
  earliestDate: string | null
  latestDate: string | null
}
```

## 4. Komponenten Übersicht

### Neue Komponenten

| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| LoginForm | Feature | Login form with validation |
| RegisterForm | Feature | Register form with password strength |
| AcceptInviteForm | Feature | Set password form |
| DashboardPage | Page | Welcome placeholder |
| UsersListPage | Page | DataTable with users |
| UserInviteModal | Feature | Invite form in modal |
| UserEditModal | Feature | Edit form in modal |
| UserDetailsSheet | Feature | User details view in sheet (read-only) |
| UserPermissionsModal | Feature | Permissions checkboxes |
| AuditLogsPage | Page | DataTable with filters |
| PasswordRequirements | Shared | Password rules checklist |

### Bestehende Komponenten nutzen

| Komponente | Woher |
|------------|-------|
| Button, Input, Label | `@/components/ui/*` |
| DataTable, DataTableToolbar, etc. | `@/components/shared/data-table` |
| PageHeader, AppLayout | `@/components/shared/layout` |
| LoadingSpinner, ErrorState, EmptyState | `@/components/shared/feedback` |
| ConfirmDialog | `@/components/shared/feedback` |
| FormSheet | `@/components/shared/form` |
| PasswordInput, PasswordStrength | `@/components/shared/form` |
| StatusBadge, RelativeTime, UserAvatar | `@/components/shared` |
| Dialog (for permissions modal) | `@/components/ui/dialog` |
| Checkbox | `@/components/ui/checkbox` |

## 5. Files zu erstellen

### Feature: Auth

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/auth/types/index.ts` | Auth TypeScript types |
| API | `src/features/auth/api/auth-api.ts` | Auth API calls |
| useLogin | `src/features/auth/hooks/use-login.ts` | Login mutation |
| useRegister | `src/features/auth/hooks/use-register.ts` | Register mutation |
| useLogout | `src/features/auth/hooks/use-logout.ts` | Logout mutation |
| useCurrentUser | `src/features/auth/hooks/use-current-user.ts` | Get current user query |
| useAcceptInvite | `src/features/auth/hooks/use-accept-invite.ts` | Accept invite mutation |
| hooks/index | `src/features/auth/hooks/index.ts` | Barrel export |
| LoginForm | `src/features/auth/components/login-form.tsx` | Login form component |
| RegisterForm | `src/features/auth/components/register-form.tsx` | Register form component |
| AcceptInviteForm | `src/features/auth/components/accept-invite-form.tsx` | Accept invite form |
| PasswordRequirements | `src/features/auth/components/password-requirements.tsx` | Password checklist |
| components/index | `src/features/auth/components/index.ts` | Barrel export |
| Feature Index | `src/features/auth/index.ts` | Feature barrel export |

### Feature: Users (System)

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/users/types/index.ts` | User TypeScript types |
| API | `src/features/users/api/users-api.ts` | Users API calls |
| useSystemUsers | `src/features/users/hooks/use-system-users.ts` | Users list query (infinite) |
| useSystemUser | `src/features/users/hooks/use-system-user.ts` | Single user query |
| useInviteUser | `src/features/users/hooks/use-invite-user.ts` | Invite mutation |
| useUpdateUser | `src/features/users/hooks/use-update-user.ts` | Update mutation |
| useUpdatePermissions | `src/features/users/hooks/use-update-permissions.ts` | Permissions mutation |
| useDeleteUser | `src/features/users/hooks/use-delete-user.ts` | Delete mutation |
| hooks/index | `src/features/users/hooks/index.ts` | Barrel export |
| UsersTable | `src/features/users/components/users-table.tsx` | Users DataTable |
| UsersTableColumns | `src/features/users/components/users-table-columns.tsx` | Column definitions |
| UserInviteModal | `src/features/users/components/user-invite-modal.tsx` | Invite form modal |
| UserEditModal | `src/features/users/components/user-edit-modal.tsx` | Edit form modal |
| UserDetailsSheet | `src/features/users/components/user-details-sheet.tsx` | User details sheet (read-only) |
| UserPermissionsModal | `src/features/users/components/user-permissions-modal.tsx` | Permissions dialog |
| components/index | `src/features/users/components/index.ts` | Barrel export |
| Feature Index | `src/features/users/index.ts` | Feature barrel export |

### Feature: Permissions

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/permissions/types/index.ts` | Permission TypeScript types |
| API | `src/features/permissions/api/permissions-api.ts` | Permissions API calls |
| useSystemPermissions | `src/features/permissions/hooks/use-system-permissions.ts` | Permissions query |
| hooks/index | `src/features/permissions/hooks/index.ts` | Barrel export |
| Feature Index | `src/features/permissions/index.ts` | Feature barrel export |

### Feature: Audit Logs

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/audit-logs/types/index.ts` | Audit log TypeScript types |
| API | `src/features/audit-logs/api/audit-logs-api.ts` | Audit logs API calls |
| useAuditLogs | `src/features/audit-logs/hooks/use-audit-logs.ts` | Audit logs query (infinite) |
| useAuditLogFilters | `src/features/audit-logs/hooks/use-audit-log-filters.ts` | Filters query |
| hooks/index | `src/features/audit-logs/hooks/index.ts` | Barrel export |
| AuditLogsTable | `src/features/audit-logs/components/audit-logs-table.tsx` | Audit logs DataTable |
| AuditLogsTableColumns | `src/features/audit-logs/components/audit-logs-table-columns.tsx` | Column definitions |
| AuditLogsFilters | `src/features/audit-logs/components/audit-logs-filters.tsx` | Filter controls |
| components/index | `src/features/audit-logs/components/index.ts` | Barrel export |
| Feature Index | `src/features/audit-logs/index.ts` | Feature barrel export |

### Routes/Pages

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Root Route | `src/routes/__root.tsx` | Route config + layouts (AppLayoutWrapper) |
| Index Page | `src/routes/index-page.tsx` | Index redirect based on auth |
| Login Page | `src/routes/login.tsx` | Login page |
| Register Page | `src/routes/register.tsx` | Register page |
| Accept Invite Page | `src/routes/invite.tsx` | Accept invite page |
| Dashboard Page | `src/routes/dashboard.tsx` | Dashboard page |
| Users Page | `src/routes/users.tsx` | Users list page |
| Not Found Page | `src/routes/not-found.tsx` | 404 page |
| Forbidden Page | `src/routes/forbidden.tsx` | 403 page |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/contexts/auth-context.tsx` | Real implementation mit API calls |
| `src/lib/axios.ts` | Add refresh token interceptor, error handling |
| `src/app/router.tsx` | Add new routes |
| `src/config/navigation.ts` | Update sidebar items with correct permissions |
| `src/i18n/locales/en/auth.json` | Add auth translations |
| `src/i18n/locales/de/auth.json` | Add auth translations |
| `src/i18n/locales/en/users.json` | Add users translations |
| `src/i18n/locales/de/users.json` | Add users translations |
| `src/i18n/locales/en/errors.json` | Add error code translations |
| `src/i18n/locales/de/errors.json` | Add error code translations |

## 7. Neue Dependencies

### NPM Packages

Keine neuen - alle bereits installiert (react-hook-form, zod, @tanstack/react-query, etc.)

### Shadcn/UI Komponenten

Keine neuen - alle bereits installiert.

## 8. Implementation Reihenfolge

### Phase 1: Auth Foundation (BREAKPOINT 1) ✅

1. [x] **Types**: Auth types erstellen (`src/features/auth/types/index.ts`)
2. [x] **API**: Auth API client (`src/features/auth/api/auth-api.ts`)
3. [x] **Axios**: Refresh token interceptor + error translation
4. [x] **Context**: AuthContext real implementation
5. [x] **Hooks**: useLogin, useRegister, useLogout, useCurrentUser, useAcceptInvite
6. [x] **Components**: LoginForm, RegisterForm, PasswordRequirements
7. [x] **Routes**: Login page, Register page
8. [x] **i18n**: Auth translations (EN + DE)
9. [x] **i18n**: Error translations (EN + DE)

**Test:** Login funktioniert, Register funktioniert, Logout funktioniert

---

### Phase 2: Accept Invite + Dashboard (BREAKPOINT 2) ✅

10. [x] **Component**: AcceptInviteForm
11. [x] **Route**: Accept Invite page (`/invite?token=xxx`)
12. [x] **Route**: Dashboard page (with i18n)
13. [x] **Navigation**: Sidebar config already configured

**Test:** Accept invite flow funktioniert, Dashboard zeigt Welcome

---

### Phase 3: System Users (BREAKPOINT 3) ✅

14. [x] **Types**: Users + Permissions types (`src/features/users/types`, `src/features/permissions/types`)
15. [x] **API**: Users API client (`src/features/users/api/users-api.ts`)
16. [x] **API**: Permissions API client (`src/features/permissions/api/permissions-api.ts`)
17. [x] **Hooks**: useSystemUsers (infinite query), useSystemUser
18. [x] **Hooks**: useInviteUser, useUpdateUser, useDeleteUser
19. [x] **Hooks**: useUpdatePermissions, useSystemPermissions
20. [x] **Components**: UsersTable, UsersTableColumns (with emailVerified column, multi-sort support)
21. [x] **Components**: UserInviteModal, UserEditModal (changed from sheets to modals)
22. [x] **Components**: UserDetailsSheet (for viewing user details with permissions read-only)
23. [x] **Components**: UserPermissionsModal
24. [x] **Route**: Users list page (`/users`)
25. [x] **i18n**: Users translations (EN + DE)
26. [x] **UX**: Multi-column sorting with visual indicators
27. [x] **UX**: Row click opens details sheet, 3-dot menu for actions (stopPropagation)
28. [x] **UX**: Unsaved changes modal (closes form modal, reopens on continue editing)
29. [x] **UX**: DataTable pagination centered with border separator, proper i18n pluralization
30. [x] **UX**: Mobile card with StatusBadge and RelativeTime rendering
31. [x] **Refactor**: Routes split into separate files (dashboard.tsx, users.tsx, invite.tsx, index-page.tsx)

**Test:** Users CRUD funktioniert, Permissions funktionieren

---

### Phase 4: Audit Logs (BREAKPOINT 4) ✅

25. [x] **Types**: Audit log types (`src/features/audit-logs/types/index.ts`)
26. [x] **API**: Audit logs API client (`src/features/audit-logs/api/audit-logs-api.ts`)
27. [x] **Hooks**: useAuditLogs (infinite query), useAuditLogFilters
28. [x] **Components**: AuditLogsTable, AuditLogsTableColumns
29. [x] **Components**: SelectFilter, DateRangePicker (separate filter buttons)
30. [x] **Route**: Audit logs page (`src/routes/audit-logs.tsx`)
31. [x] **i18n**: Audit logs translations (EN + DE)
32. [x] **Shared**: DateRangePicker, SelectFilter components added to `@/components/shared/form`
33. [x] **DataTable**: Added `toolbarContent` prop for custom toolbar content (renders before sorting indicator)
34. [x] **UX**: Click on audit log row opens AuditLogDetailsSheet to view all details
35. [x] **UX**: From AuditLogDetailsSheet, click on user opens UserDetailsSheet
36. [x] **Components**: AuditLogDetailsSheet (shows action, user, time, entity, IP, user agent, JSON details)
37. [x] **Docs**: Updated `i18n-translations.md` with auditLogs namespace
38. [x] **UX**: Mobile responsive filters (DateRangePicker shows 1 month on mobile, flex-wrap toolbar)
39. [x] **UX**: Mobile card click support (onClick prop on DataTableCard)
40. [x] **Permissions**: Route-level permission checks (withPermission wrapper for routes)
41. [x] **Permissions**: Users page action visibility based on permissions (system:users:create/update/delete)
42. [x] **Permissions**: Hide 3-dot menu when no actions available
43. [x] **UX**: Search input height matches button height (h-8)

**Test:** Audit logs mit Filtern anzeigbar

---

### Phase 5: Polish & Tests (FINAL) ✅

32. [x] **Tests**: Component tests (LoginForm, RegisterForm, UsersTable, UserInviteModal)
33. [x] **Tests**: Hook tests (useLogin, useSystemUsers)
34. [x] **Cleanup**: Fixed all lint errors and warnings
35. [x] **Test Setup**: Created `src/test/setup.ts` and `src/test/test-utils.tsx`

**Test:** 43 Tests grün, TypeScript keine Errors, Lint passed, Build successful

## 9. i18n Keys zu erstellen

### errors.json (Add)

```json
{
  "AUTH_INVALID_CREDENTIALS": "Invalid email or password",
  "AUTH_USER_INACTIVE": "User account is inactive",
  "AUTH_TOKEN_EXPIRED": "Session expired",
  "AUTH_REFRESH_TOKEN_INVALID": "Session invalid",
  "AUTH_REGISTRATION_CLOSED": "Registration closed. Please contact an administrator.",
  "AUTH_EMAIL_EXISTS": "Email is already in use",
  "AUTH_PASSWORD_TOO_WEAK": "Password does not meet requirements",
  "AUTH_TOO_MANY_ATTEMPTS": "Too many attempts. Please wait 15 minutes.",
  "AUTH_INVITE_EXPIRED": "Invitation expired. Please request a new one.",
  "AUTH_INVITE_INVALID": "Invalid invitation link",
  "SYSTEM_USER_NOT_FOUND": "User not found",
  "SYSTEM_PERMISSION_NOT_FOUND": "Permission not found",
  "SYSTEM_LAST_PERMISSION_HOLDER": "Cannot remove - last user with this permission",
  "SYSTEM_CANNOT_DELETE_SELF": "Cannot delete yourself",
  "SYSTEM_FORBIDDEN": "No permission for this action",
  "VALIDATION_REQUIRED": "This field is required",
  "VALIDATION_INVALID_FORMAT": "Invalid format",
  "VALIDATION_MIN_LENGTH": "Minimum {{min}} characters required",
  "VALIDATION_MAX_LENGTH": "Maximum {{max}} characters allowed"
}
```

### auth.json (Add)

```json
{
  "login": {
    "title": "Welcome back",
    "subtitle": "Sign in to your account",
    "email": "Email",
    "password": "Password",
    "submit": "Sign in",
    "noAccount": "Don't have an account?",
    "register": "Register"
  },
  "register": {
    "title": "Create account",
    "subtitle": "Register to get started",
    "firstName": "First name",
    "lastName": "Last name",
    "email": "Email",
    "password": "Password",
    "submit": "Create account",
    "haveAccount": "Already have an account?",
    "login": "Sign in"
  },
  "invite": {
    "title": "Welcome to ExoAuth",
    "subtitle": "Set your password to complete registration",
    "password": "Password",
    "confirmPassword": "Confirm password",
    "submit": "Accept & Continue",
    "passwordMismatch": "Passwords do not match"
  },
  "password": {
    "requirements": "Password requirements",
    "minLength": "At least 12 characters",
    "uppercase": "One uppercase letter",
    "lowercase": "One lowercase letter",
    "digit": "One number",
    "special": "One special character"
  },
  "logout": {
    "title": "Sign out",
    "confirm": "Are you sure you want to sign out?"
  }
}
```

### users.json (Update)

```json
{
  "title": "System Users",
  "subtitle": "Manage system administrators",
  "table": {
    "name": "Name",
    "email": "Email",
    "status": "Status",
    "permissions": "Permissions",
    "lastLogin": "Last Login",
    "actions": "Actions"
  },
  "status": {
    "active": "Active",
    "inactive": "Inactive",
    "pending": "Pending"
  },
  "actions": {
    "edit": "Edit",
    "permissions": "Permissions",
    "delete": "Delete",
    "invite": "Invite User"
  },
  "invite": {
    "title": "Invite User",
    "subtitle": "Send an invitation to join the team",
    "email": "Email",
    "firstName": "First name",
    "lastName": "Last name",
    "permissions": "Permissions",
    "submit": "Send Invitation",
    "success": "Invitation sent successfully"
  },
  "edit": {
    "title": "Edit User",
    "firstName": "First name",
    "lastName": "Last name",
    "isActive": "Active",
    "submit": "Save Changes",
    "success": "User updated successfully"
  },
  "permissions": {
    "title": "Manage Permissions",
    "subtitle": "Select permissions for this user",
    "submit": "Save Permissions",
    "success": "Permissions updated successfully"
  },
  "delete": {
    "title": "Delete User",
    "confirm": "Are you sure you want to delete this user?",
    "success": "User deleted successfully"
  },
  "empty": {
    "title": "No users found",
    "description": "Invite your first team member"
  },
  "search": {
    "placeholder": "Search users..."
  }
}
```

## 10. Tests

### Component Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/login-form.test.tsx` | Form validation, submit, error display |
| `src/features/auth/__tests__/register-form.test.tsx` | Form validation, password strength |
| `src/features/users/__tests__/users-table.test.tsx` | Table rendering, actions |
| `src/features/users/__tests__/user-invite-sheet.test.tsx` | Form validation, submit |

### Hook Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/auth/__tests__/use-login.test.ts` | Mutation success/error |
| `src/features/users/__tests__/use-system-users.test.ts` | Infinite query, pagination |

## 11. Sidebar Navigation Config

```typescript
// src/config/navigation.ts
export const navigationItems = [
  {
    title: 'navigation:dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
  },
  {
    title: 'navigation:system',
    icon: Shield,
    children: [
      {
        title: 'navigation:users',
        href: '/dashboard/system/users',
        icon: Users,
        permission: 'system:users:read',
      },
      {
        title: 'navigation:auditLogs',
        href: '/dashboard/system/audit-logs',
        icon: FileText,
        permission: 'system:audit:read',
      },
      {
        title: 'navigation:settings',
        href: '/dashboard/system/settings',
        icon: Settings,
        permission: 'system:settings:read',
      },
    ],
  },
]
```

## 12. Nach Completion

- [x] Alle Tests grün (43 tests passing)
- [x] `task_standards_frontend.md` aktualisiert (neue Files)
- [x] `i18n-translations.md` aktualisiert
- [x] TypeScript keine Errors
- [x] Lint passed (0 errors, 0 warnings)
- [x] Build successful
- [ ] Manual testing complete

---

## Notizen

- **HttpOnly Cookies**: Tokens werden als Cookies gesetzt, Frontend muss `credentials: 'include'` verwenden
- **Auto-Refresh**: Axios interceptor refresht Token automatisch bei 401
- **Password Requirements**: Min 12 chars, upper, lower, digit, special
- **Infinite Scroll**: DataTable mit useInfiniteQuery
- **Permissions grouped by category**: Backend liefert mit `?groupByCategory=true`
- **Error Translation**: Alle API error codes werden über i18n übersetzt
- **Route Structure**: Each page in own file (`routes/dashboard.tsx`, `routes/users.tsx`, etc.), `__root.tsx` only contains route config and layouts
- **FormModal UX**: Changed from sheets to modals for edit/invite, unsaved changes closes modal and reopens on "Continue Editing"
- **Mobile Cards**: DataTableCard supports render functions for custom field rendering (StatusBadge, RelativeTime)

- **Audit Logs Filters**: Separate SelectFilter and DateRangePicker components, passed via `toolbarContent` prop to DataTable

---

**Letzte Änderung:** 2025-12-26
**Status:** ✅ COMPLETE - All 5 Phases Done

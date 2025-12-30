# Frontend Coding Standards - ExoAuth

> Lies diese Datei bevor du Code schreibst.

---

## Projekt Struktur

```
frontend/src/
├── app/                    # App setup (router, providers)
│   ├── providers.tsx       # All providers (Query, Theme, Auth, etc.)
│   └── router.tsx          # TanStack Router config
│
├── components/
│   ├── ui/                 # Shadcn/UI Komponenten (nicht editieren!)
│   └── shared/             # Eigene shared components
│       ├── layout/         # Layout components (Sidebar, Header, etc.)
│       ├── feedback/       # Feedback components (Loading, Empty, Error)
│       ├── data-table/     # DataTable components
│       ├── form/           # Form components (PasswordInput, FormSheet)
│       └── *.tsx           # Utility components (Avatar, Badge, etc.)
│
├── config/                 # App configuration
│   └── navigation.ts       # Sidebar navigation config
│
├── contexts/               # React Contexts
│   ├── auth-context.tsx    # Auth state & permissions
│   ├── theme-context.tsx   # Theme (dark/light/system)
│   └── sidebar-context.tsx # Sidebar collapsed state
│
├── features/               # Feature-basierte Module
│   └── {feature}/
│       ├── api/            # API calls
│       ├── hooks/          # React Query hooks
│       ├── components/     # Feature-spezifische UI
│       ├── types/          # TypeScript types
│       └── index.ts        # Barrel export
│
├── hooks/                  # Globale custom hooks
│   ├── use-debounce.ts
│   ├── use-local-storage.ts
│   ├── use-media-query.ts
│   ├── use-copy-to-clipboard.ts
│   ├── use-table-preferences.ts
│   └── index.ts
│
├── i18n/                   # Internationalization
│   ├── index.ts            # i18next config
│   └── locales/
│       ├── en/             # English translations
│       └── de/             # German translations
│
├── lib/                    # Utilities
│   ├── utils.ts            # cn helper
│   └── axios.ts            # API client with interceptors
│
├── routes/                 # TanStack Router routes
│   ├── __root.tsx          # Root route with layout
│   ├── protected-route.tsx # Auth guard
│   └── *.tsx               # Error pages (404, 403, 500)
│
├── styles/
│   └── globals.css         # Global CSS, Theme, Print Styles
│
└── types/                  # Globale TypeScript types
    ├── auth.ts             # User, Token types
    ├── api.ts              # ApiResponse, Pagination
    ├── table.ts            # Table column/filter types
    └── index.ts            # Barrel export
```

---

## Naming Conventions

| Was | Convention | Beispiel |
|-----|------------|----------|
| Components | PascalCase | `UserForm` |
| Component Files | kebab-case.tsx | `user-form.tsx` |
| Hooks | camelCase mit use | `useUsers` |
| Hook Files | kebab-case.ts | `use-users.ts` |
| API Files | kebab-case.ts | `users-api.ts` |
| Types | PascalCase | `UserResponse` |
| Variables | camelCase | `isLoading` |
| Constants | SCREAMING_SNAKE | `API_BASE_URL` |
| CSS Classes | kebab-case | `user-card` |
| Folders | kebab-case | `user-profile` |
| i18n Keys | dot.notation | `users.title` |

---

## DO's

### Allgemein
- ✅ TypeScript strict mode
- ✅ Functional components
- ✅ Named exports (kein default export)
- ✅ Barrel exports in index.ts
- ✅ Kleine, fokussierte Komponenten
- ✅ Custom hooks für Logik

### Components
- ✅ Props Interface definieren
- ✅ Destructuring für Props
- ✅ `cn()` für conditional classes
- ✅ Shadcn/UI Komponenten nutzen
- ✅ Loading/Error states behandeln
- ✅ Shared Components nutzen (DataTable, Feedback, etc.)

### State Management
- ✅ React Query für Server State
- ✅ useState für lokalen UI State
- ✅ Contexts für globalen App State (Auth, Theme, Sidebar)
- ✅ Zustand nur wenn wirklich nötig

### Forms
- ✅ React Hook Form
- ✅ Zod für Validation
- ✅ Shadcn Form components (wenn installiert) oder Input/Label direkt
- ✅ FormSheet für slide-out, FormModal für dialogs

### API
- ✅ Axios instance mit interceptors (`@/lib/axios`)
- ✅ TypeScript für Request/Response
- ✅ Error handling

### i18n (Internationalization)
- ✅ `useTranslation()` hook für alle User-facing Text
- ✅ Namespace prefix für Feature-spezifische Keys (`users:title`)
- ✅ Common namespace für shared text (`common:actions.save`)
- ✅ Interpolation für dynamic values (`t('time.minutesAgo', { count: 5 })`)

---

## DON'Ts

### Allgemein
- ❌ Keine `any` types
- ❌ Keine inline styles
- ❌ Keine magic strings (use i18n keys or constants)
- ❌ Keine console.log in production
- ❌ Keine direkten DOM manipulationen

### Components
- ❌ Keine Business Logic in Components
- ❌ Keine API Calls direkt in Components (use hooks)
- ❌ Keine prop drilling (mehr als 2 levels) - use contexts
- ❌ Shadcn/UI files nicht editieren!

### State
- ❌ Keine redundanten States
- ❌ Keine derived state als useState
- ❌ Redux/Context für Server State (use React Query)

### Files
- ❌ Keine Files > 200 Zeilen
- ❌ Keine Components > 100 Zeilen
- ❌ Mehrere Components in einer Datei

### i18n
- ❌ Keine hardcoded strings für User-facing text
- ❌ Keine fehlenden DE translations

---

## Code Beispiele

### Component mit i18n (Richtig)
```tsx
// src/features/users/components/user-list.tsx
import { useTranslation } from 'react-i18next'
import { DataTable } from '@/components/shared/data-table'
import { PageHeader } from '@/components/shared/layout'
import { LoadingSpinner, EmptyState, ErrorState } from '@/components/shared/feedback'
import { useUsers } from '../hooks/use-users'

export function UserList() {
  const { t } = useTranslation()
  const { data: users, isLoading, error } = useUsers()

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorState error={error} />
  if (!users?.length) {
    return (
      <EmptyState
        title={t('users:empty.title')}
        description={t('users:empty.message')}
        action={{
          label: t('users:createUser'),
          onClick: () => {},
        }}
      />
    )
  }

  return (
    <div>
      <PageHeader
        title={t('users:title')}
        description={t('users:subtitle')}
      />
      <DataTable data={users} columns={columns} />
    </div>
  )
}
```

### Component mit Auth Context (Richtig)
```tsx
// src/features/users/components/user-actions.tsx
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts'
import { Button } from '@/components/ui/button'

export function UserActions() {
  const { t } = useTranslation()
  const { user, hasPermission } = useAuth()

  // Permission-based rendering
  if (!hasPermission('system:users:write')) {
    return null
  }

  return (
    <Button onClick={() => {}}>
      {t('users:createUser')}
    </Button>
  )
}
```

### Hook (Richtig)
```tsx
// src/features/users/hooks/use-users.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'
import type { CreateUserRequest } from '../types'

const USERS_KEY = ['users'] as const

export function useUsers() {
  return useQuery({
    queryKey: USERS_KEY,
    queryFn: usersApi.getAll,
  })
}

export function useUser(id: string) {
  return useQuery({
    queryKey: [...USERS_KEY, id],
    queryFn: () => usersApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateUserRequest) => usersApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: USERS_KEY })
    },
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => usersApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: USERS_KEY })
    },
  })
}
```

### API (Richtig)
```tsx
// src/features/users/api/users-api.ts
import apiClient from '@/lib/axios'
import type { User, CreateUserRequest, UpdateUserRequest } from '../types'
import type { ApiResponse } from '@/types'

export const usersApi = {
  getAll: async (): Promise<User[]> => {
    const { data } = await apiClient.get<ApiResponse<User[]>>('/api/users')
    return data.data
  },

  getById: async (id: string): Promise<User> => {
    const { data } = await apiClient.get<ApiResponse<User>>(`/api/users/${id}`)
    return data.data
  },

  create: async (request: CreateUserRequest): Promise<User> => {
    const { data } = await apiClient.post<ApiResponse<User>>('/api/users', request)
    return data.data
  },

  update: async (id: string, request: UpdateUserRequest): Promise<User> => {
    const { data } = await apiClient.put<ApiResponse<User>>(`/api/users/${id}`, request)
    return data.data
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/users/${id}`)
  },
}
```

### Types (Richtig)
```tsx
// src/features/users/types/index.ts
export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  permissions: string[]
  createdAt: string
  updatedAt: string
}

export interface CreateUserRequest {
  email: string
  firstName: string
  lastName: string
  password: string
}

export interface UpdateUserRequest {
  firstName?: string
  lastName?: string
  isActive?: boolean
}
```

### Form mit Zod & i18n (Richtig)
```tsx
// src/features/users/components/user-form.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { PasswordInput } from '@/components/shared/form'
import { useCreateUser } from '../hooks/use-users'

const userSchema = z.object({
  email: z.string().email(),
  firstName: z.string().min(2),
  lastName: z.string().min(2),
  password: z.string().min(8),
})

type UserFormData = z.infer<typeof userSchema>

interface UserFormProps {
  onSuccess?: () => void
}

export function UserForm({ onSuccess }: UserFormProps) {
  const { t } = useTranslation()
  const { mutate: createUser, isPending } = useCreateUser()

  const form = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
    defaultValues: {
      email: '',
      firstName: '',
      lastName: '',
      password: '',
    },
  })

  const onSubmit = (data: UserFormData) => {
    createUser(data, {
      onSuccess: () => {
        form.reset()
        onSuccess?.()
      },
    })
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="email">{t('users:fields.email')}</Label>
        <Input
          id="email"
          type="email"
          {...form.register('email')}
        />
        {form.formState.errors.email && (
          <p className="text-sm text-destructive">
            {t('validation:email')}
          </p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="firstName">{t('users:fields.firstName')}</Label>
          <Input id="firstName" {...form.register('firstName')} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="lastName">{t('users:fields.lastName')}</Label>
          <Input id="lastName" {...form.register('lastName')} />
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="password">{t('auth:login.password')}</Label>
        <PasswordInput
          id="password"
          {...form.register('password')}
        />
      </div>

      <Button type="submit" disabled={isPending}>
        {isPending ? t('common:actions.loading') : t('users:createUser')}
      </Button>
    </form>
  )
}
```

### Barrel Export (Richtig)
```tsx
// src/features/users/index.ts
// Components
export { UserList } from './components/user-list'
export { UserForm } from './components/user-form'

// Hooks
export { useUsers, useUser, useCreateUser, useDeleteUser } from './hooks/use-users'

// Types
export type { User, CreateUserRequest, UpdateUserRequest } from './types'
```

---

## Import Order

```tsx
// 1. React
import { useState, useEffect } from 'react'

// 2. Third-party libraries
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

// 3. UI Components (shadcn)
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

// 4. Shared components
import { DataTable } from '@/components/shared/data-table'
import { PageHeader } from '@/components/shared/layout'
import { LoadingSpinner } from '@/components/shared/feedback'

// 5. Contexts & Hooks
import { useAuth } from '@/contexts'
import { useDebounce } from '@/hooks'

// 6. Feature imports
import { useUsers } from '../hooks/use-users'
import type { User } from '../types'

// 7. Types (if separate)
import type { ApiResponse } from '@/types'
```

---

## Using Contexts

### Auth Context
```tsx
import { useAuth } from '@/contexts'

function MyComponent() {
  const { user, isAuthenticated, hasPermission, login, logout } = useAuth()

  // Check authentication
  if (!isAuthenticated) {
    return <Navigate to="/login" />
  }

  // Check permissions
  if (!hasPermission('system:users:read')) {
    return <Navigate to="/forbidden" />
  }

  return <div>Welcome {user?.fullName}</div>
}
```

### Theme Context
```tsx
import { useTheme } from '@/contexts'

function MyComponent() {
  const { theme, setTheme } = useTheme()

  return (
    <button onClick={() => setTheme('dark')}>
      Current: {theme}
    </button>
  )
}
```

### Sidebar Context
```tsx
import { useSidebar } from '@/contexts'

function MyComponent() {
  const { isCollapsed, toggle, setCollapsed } = useSidebar()

  return (
    <button onClick={toggle}>
      {isCollapsed ? 'Expand' : 'Collapse'}
    </button>
  )
}
```

---

## Using i18n

### Basic Usage
```tsx
import { useTranslation } from 'react-i18next'

function MyComponent() {
  const { t } = useTranslation()

  return (
    <div>
      {/* Default namespace (common) */}
      <button>{t('actions.save')}</button>

      {/* Specific namespace */}
      <h1>{t('users:title')}</h1>

      {/* With interpolation */}
      <p>{t('time.minutesAgo', { count: 5 })}</p>

      {/* With confirmation text */}
      <p>{t('confirm.typeToConfirm', { text: 'DELETE' })}</p>
    </div>
  )
}
```

### Available Namespaces
- `common` - Default, buttons, labels, status, typeToConfirm
- `auth` - Login, register, session, forgot password
- `navigation` - Sidebar, breadcrumbs
- `users` - User management, admin actions, security status
- `errors` - Error messages (incl. MFA, sessions, users)
- `validation` - Form validation
- `settings` - Settings page (Task 008)
- `mfa` - MFA setup, verify, disable, backup codes (Task 008)
- `sessions` - Device sessions management (Task 008)
- `auditLogs` - Audit logs (Task 006)

See `docs/standards/i18n-translations.md` for all keys.

---

## Using Shared Components

### DataTable
```tsx
import { DataTable } from '@/components/shared/data-table'
import type { ColumnDef } from '@tanstack/react-table'

const columns: ColumnDef<User>[] = [
  { accessorKey: 'email', header: 'Email' },
  { accessorKey: 'name', header: 'Name' },
]

<DataTable data={users} columns={columns} />
```

### Feedback Components
```tsx
import {
  LoadingSpinner,
  EmptyState,
  ErrorState,
  ConfirmDialog,
  TypeConfirmDialog,
} from '@/components/shared/feedback'

// Loading
<LoadingSpinner size="lg" />

// Empty state
<EmptyState
  title="No users"
  description="Get started by creating a user"
  action={{ label: 'Create User', onClick: () => {} }}
/>

// Error
<ErrorState error={error} onRetry={() => refetch()} />

// Confirm dialog
<ConfirmDialog
  open={isOpen}
  onOpenChange={setIsOpen}
  title="Delete User"
  description="Are you sure?"
  onConfirm={handleDelete}
/>

// Type to confirm (for dangerous actions)
<TypeConfirmDialog
  open={isOpen}
  onOpenChange={setIsOpen}
  title="Delete Project"
  confirmText="DELETE"
  onConfirm={handleDelete}
/>
```

### Form Components
```tsx
import { PasswordInput, FormSheet, FormModal } from '@/components/shared/form'

// Password with visibility toggle
<PasswordInput {...register('password')} />

// Slide-out form
<FormSheet
  open={isOpen}
  onOpenChange={setIsOpen}
  title="Edit User"
>
  <UserForm />
</FormSheet>

// Modal form
<FormModal
  open={isOpen}
  onOpenChange={setIsOpen}
  title="Create User"
>
  <UserForm />
</FormModal>
```

### Layout Components
```tsx
import { PageHeader, AppLayout } from '@/components/shared/layout'

<PageHeader
  title="Users"
  description="Manage system users"
  actions={<Button>Create User</Button>}
/>
```

### Utility Components
```tsx
import {
  UserAvatar,
  StatusBadge,
  CopyButton,
  RelativeTime,
} from '@/components/shared'

<UserAvatar user={user} size="md" />
<StatusBadge status="active" />
<CopyButton value="text-to-copy" />
<RelativeTime date={user.createdAt} />
```

---

## Using Hooks

```tsx
import {
  useDebounce,
  useLocalStorage,
  useIsMobile,
  useIsDesktop,
  useCopyToClipboard,
  useTablePreferences,
} from '@/hooks'

// Debounce search input
const [search, setSearch] = useState('')
const debouncedSearch = useDebounce(search, 300)

// Persist to localStorage
const [value, setValue] = useLocalStorage('key', defaultValue)

// Responsive checks
const isMobile = useIsMobile()
const isDesktop = useIsDesktop()

// Copy to clipboard
const { copy, copied } = useCopyToClipboard()

// Table preferences (sorting, columns, etc.)
const { preferences, updatePreferences } = useTablePreferences('users-table')
```

---

## Folder Struktur für Features

```
features/
└── users/
    ├── api/
    │   └── users-api.ts
    ├── hooks/
    │   └── use-users.ts
    ├── components/
    │   ├── user-list.tsx
    │   ├── user-form.tsx
    │   └── user-card.tsx
    ├── types/
    │   └── index.ts
    └── index.ts
```

---

## Standards & Task File Maintenance

### Wann Standards aktualisieren?

Nach jeder Task-Completion MÜSSEN die Standards aktualisiert werden:

| Was wurde gemacht? | Was aktualisieren? |
|-------------------|-------------------|
| Neues NPM Package installiert | `task_standards_frontend.md` → "Installierte Packages" |
| Neue Shadcn Component | `task_standards_frontend.md` → "Shadcn/UI Komponenten" |
| Neuer Shared Component | `task_standards_frontend.md` → "Verfügbare Shared Components" |
| Neuer Custom Hook | `task_standards_frontend.md` → "Verfügbare Hooks" + `coding_standards_frontend.md` |
| Neuer Context | `task_standards_frontend.md` → "Verfügbare Contexts" + `coding_standards_frontend.md` |
| Neue i18n Keys | `docs/standards/i18n-translations.md` |
| Neuer i18n Namespace | Beide Standards + `i18n-translations.md` |
| Neue Folder Struktur | `task_standards_frontend.md` → "File Tree" |
| Neues Code Pattern | `coding_standards_frontend.md` → Examples |

### Task File Pflege

Jede Task-Datei (`docs/tasks/XXX_*.md`) sollte:

1. **Während der Arbeit**: Status der Files aktualisieren (❌ → ✅)
2. **Nach Completion**:
   - Alle Checkboxen abhaken
   - "Letzte Änderung" Datum updaten
   - Status auf "Complete" setzen
3. **Standards updaten**: Als letzter Schritt IMMER die Standards aktualisieren

### Checkliste nach Task Completion

```markdown
- [ ] Alle Files in Task-Datei als ✅ markiert
- [ ] Task Status auf "Complete" gesetzt
- [ ] `task_standards_frontend.md` aktualisiert:
  - [ ] File Tree aktuell
  - [ ] Packages aktuell
  - [ ] Shadcn Components aktuell
  - [ ] Shared Components aktuell
  - [ ] Hooks aktuell
  - [ ] Contexts aktuell
- [ ] `coding_standards_frontend.md` aktualisiert (wenn neue Patterns)
- [ ] `i18n-translations.md` aktualisiert (wenn neue Keys)
- [ ] TypeScript Build passed
- [ ] Lint passed
```

### Warum ist das wichtig?

- **Nächste Session**: Claude liest diese Files um zu wissen was existiert
- **Keine Duplikate**: Verhindert doppeltes Installieren von Packages
- **Konsistenz**: Alle wissen welche Components/Hooks verfügbar sind
- **Onboarding**: Neue Entwickler verstehen die Struktur sofort

---

## Letzte Änderung

- **Datum:** 2025-12-30
- **Änderungen:** Updated i18n namespaces with Task 008 additions (settings, mfa, sessions)

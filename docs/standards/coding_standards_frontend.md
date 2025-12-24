# Frontend Coding Standards - ExoAuth

> Lies diese Datei bevor du Code schreibst.

---

## Projekt Struktur

```
frontend/src/
├── app/                    # App setup (router, providers)
├── components/
│   ├── ui/                 # Shadcn/UI Komponenten (nicht editieren!)
│   └── shared/             # Eigene shared components
├── features/               # Feature-basierte Module
│   └── {feature}/
│       ├── api/            # API calls
│       ├── hooks/          # React Query hooks
│       ├── components/     # Feature-spezifische UI
│       ├── types/          # TypeScript types
│       └── index.ts        # Barrel export
├── hooks/                  # Globale custom hooks
├── lib/                    # Utilities (axios, cn, etc.)
├── routes/                 # TanStack Router routes
├── styles/                 # Global CSS
└── types/                  # Globale TypeScript types
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

### State Management
- ✅ React Query für Server State
- ✅ useState für lokalen UI State
- ✅ Zustand nur wenn wirklich nötig

### Forms
- ✅ React Hook Form
- ✅ Zod für Validation
- ✅ Shadcn Form components

### API
- ✅ Axios instance mit interceptors
- ✅ TypeScript für Request/Response
- ✅ Error handling

---

## DON'Ts

### Allgemein
- ❌ Keine `any` types
- ❌ Keine inline styles
- ❌ Keine magic strings
- ❌ Keine console.log in production
- ❌ Keine direkten DOM manipulationen

### Components
- ❌ Keine Business Logic in Components
- ❌ Keine API Calls direkt in Components
- ❌ Keine prop drilling (mehr als 2 levels)
- ❌ Shadcn/UI files nicht editieren!

### State
- ❌ Keine redundanten States
- ❌ Keine derived state als useState
- ❌ Redux/Context für Server State

### Files
- ❌ Keine Files > 200 Zeilen
- ❌ Keine Components > 100 Zeilen
- ❌ Mehrere Components in einer Datei

---

## Code Beispiele

### Component (Richtig)
```tsx
// src/features/users/components/user-card.tsx
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import type { User } from '../types'

interface UserCardProps {
  user: User
  onEdit: (id: string) => void
  onDelete: (id: string) => void
}

export function UserCard({ user, onEdit, onDelete }: UserCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{user.name}</CardTitle>
      </CardHeader>
      <CardContent>
        <p>{user.email}</p>
        <div className="flex gap-2 mt-4">
          <Button variant="outline" onClick={() => onEdit(user.id)}>
            Edit
          </Button>
          <Button variant="destructive" onClick={() => onDelete(user.id)}>
            Delete
          </Button>
        </div>
      </CardContent>
    </Card>
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

export const usersApi = {
  getAll: async (): Promise<User[]> => {
    const { data } = await apiClient.get<User[]>('/api/users')
    return data
  },

  getById: async (id: string): Promise<User> => {
    const { data } = await apiClient.get<User>(`/api/users/${id}`)
    return data
  },

  create: async (request: CreateUserRequest): Promise<User> => {
    const { data } = await apiClient.post<User>('/api/users', request)
    return data
  },

  update: async (id: string, request: UpdateUserRequest): Promise<User> => {
    const { data } = await apiClient.put<User>(`/api/users/${id}`, request)
    return data
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
  name: string
  role: UserRole
  createdAt: string
  updatedAt: string
}

export type UserRole = 'admin' | 'user' | 'guest'

export interface CreateUserRequest {
  email: string
  name: string
  password: string
  role: UserRole
}

export interface UpdateUserRequest {
  name?: string
  role?: UserRole
}
```

### Form mit Zod (Richtig)
```tsx
// src/features/users/components/user-form.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { useCreateUser } from '../hooks/use-users'

const userSchema = z.object({
  email: z.string().email('Invalid email'),
  name: z.string().min(2, 'Name must be at least 2 characters'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

type UserFormData = z.infer<typeof userSchema>

interface UserFormProps {
  onSuccess?: () => void
}

export function UserForm({ onSuccess }: UserFormProps) {
  const { mutate: createUser, isPending } = useCreateUser()

  const form = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
    defaultValues: {
      email: '',
      name: '',
      password: '',
    },
  })

  const onSubmit = (data: UserFormData) => {
    createUser(
      { ...data, role: 'user' },
      {
        onSuccess: () => {
          form.reset()
          onSuccess?.()
        },
      }
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input type="email" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Password</FormLabel>
              <FormControl>
                <Input type="password" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={isPending}>
          {isPending ? 'Creating...' : 'Create User'}
        </Button>
      </form>
    </Form>
  )
}
```

### Barrel Export (Richtig)
```tsx
// src/features/users/index.ts
// Components
export { UserCard } from './components/user-card'
export { UserForm } from './components/user-form'
export { UserList } from './components/user-list'

// Hooks
export { useUsers, useUser, useCreateUser, useDeleteUser } from './hooks/use-users'

// Types
export type { User, CreateUserRequest, UpdateUserRequest, UserRole } from './types'
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
    │   ├── user-card.tsx
    │   ├── user-form.tsx
    │   └── user-list.tsx
    ├── types/
    │   └── index.ts
    └── index.ts
```

---

## Import Order

```tsx
// 1. React
import { useState, useEffect } from 'react'

// 2. Third-party libraries
import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'

// 3. UI Components (shadcn)
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'

// 4. Shared components
import { LoadingSpinner } from '@/components/shared/feedback/loading-spinner'

// 5. Feature imports
import { useUsers } from '../hooks/use-users'
import type { User } from '../types'

// 6. Styles (wenn nötig)
import './user-card.css'
```

---

## Letzte Änderung

- **Datum:** 2024-12-24

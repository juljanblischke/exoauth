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
│   ├── main.tsx
│   ├── App.tsx
│   ├── App.css
│   ├── index.css
│   │
│   ├── app/                        [LEER - für router, providers]
│   │
│   ├── components/
│   │   ├── ui/                     [SHADCN COMPONENTS]
│   │   │   ├── button.tsx          ✅
│   │   │   ├── card.tsx            ✅
│   │   │   ├── checkbox.tsx        ✅
│   │   │   ├── dialog.tsx          ✅
│   │   │   ├── dropdown-menu.tsx   ✅
│   │   │   ├── form.tsx            ✅
│   │   │   ├── input.tsx           ✅
│   │   │   ├── label.tsx           ✅
│   │   │   ├── select.tsx          ✅
│   │   │   ├── sonner.tsx          ✅
│   │   │   └── table.tsx           ✅
│   │   └── shared/
│   │       ├── layout/             [LEER]
│   │       └── feedback/           [LEER]
│   │
│   ├── features/
│   │   ├── auth/
│   │   │   ├── api/                [LEER]
│   │   │   ├── hooks/              [LEER]
│   │   │   ├── components/         [LEER]
│   │   │   └── types/              [LEER]
│   │   ├── users/
│   │   │   ├── api/                [LEER]
│   │   │   ├── hooks/              [LEER]
│   │   │   ├── components/         [LEER]
│   │   │   └── types/              [LEER]
│   │   ├── roles/
│   │   │   ├── api/                [LEER]
│   │   │   ├── hooks/              [LEER]
│   │   │   ├── components/         [LEER]
│   │   │   └── types/              [LEER]
│   │   └── permissions/
│   │       ├── api/                [LEER]
│   │       ├── hooks/              [LEER]
│   │       ├── components/         [LEER]
│   │       └── types/              [LEER]
│   │
│   ├── hooks/                      [LEER - globale hooks]
│   │
│   ├── lib/
│   │   └── utils.ts                ✅ (cn helper)
│   │
│   ├── routes/
│   │   └── dashboard/              [LEER]
│   │
│   ├── styles/
│   │   └── globals.css             ✅ (tailwind + theme)
│   │
│   ├── test/
│   │   └── setup.ts                [LEER]
│   │
│   └── types/                      [LEER]
│
└── public/
```

### Installierte Packages (NICHT NOCHMAL INSTALLIEREN)

#### Dependencies
| Package | Version |
|---------|---------|
| react | 19.2.0 |
| react-dom | 19.2.0 |
| @tanstack/react-query | 5.90.12 |
| @tanstack/react-router | 1.143.4 |
| axios | 1.13.2 |
| react-hook-form | 7.69.0 |
| @hookform/resolvers | 5.2.2 |
| zod | 4.2.1 |
| clsx | 2.1.1 |
| tailwind-merge | 3.4.0 |
| class-variance-authority | 0.7.1 |
| lucide-react | 0.562.0 |
| sonner | 2.0.7 |
| next-themes | 0.4.6 |
| @radix-ui/react-checkbox | 1.3.3 |
| @radix-ui/react-dialog | 1.1.15 |
| @radix-ui/react-dropdown-menu | 2.1.16 |
| @radix-ui/react-label | 2.1.8 |
| @radix-ui/react-select | 2.2.6 |
| @radix-ui/react-slot | 1.2.4 |

#### DevDependencies
| Package | Version |
|---------|---------|
| typescript | 5.9.3 |
| vite | 7.2.4 |
| vitest | 4.0.16 |
| @vitejs/plugin-react | 5.1.1 |
| tailwindcss | 4.1.18 |
| @tailwindcss/vite | 4.1.18 |
| postcss | 8.5.6 |
| autoprefixer | 10.4.23 |
| eslint | 9.39.1 |
| @testing-library/react | 16.3.1 |
| @testing-library/dom | 10.4.1 |
| @testing-library/jest-dom | 6.9.1 |
| jsdom | 27.3.0 |
| @types/node | 25.0.3 |
| @types/react | 19.2.5 |
| @types/react-dom | 19.2.3 |

### Shadcn/UI Komponenten (SCHON INSTALLIERT)

| Komponente | Datei | Status |
|------------|-------|--------|
| Button | `src/components/ui/button.tsx` | ✅ |
| Card | `src/components/ui/card.tsx` | ✅ |
| Checkbox | `src/components/ui/checkbox.tsx` | ✅ |
| Dialog | `src/components/ui/dialog.tsx` | ✅ |
| Dropdown Menu | `src/components/ui/dropdown-menu.tsx` | ✅ |
| Form | `src/components/ui/form.tsx` | ✅ |
| Input | `src/components/ui/input.tsx` | ✅ |
| Label | `src/components/ui/label.tsx` | ✅ |
| Select | `src/components/ui/select.tsx` | ✅ |
| Sonner (Toast) | `src/components/ui/sonner.tsx` | ✅ |
| Table | `src/components/ui/table.tsx` | ✅ |

### Shadcn/UI Komponenten (NOCH NICHT INSTALLIERT)

Wenn benötigt, mit `npx shadcn@latest add [name]` installieren:

- accordion, alert, alert-dialog, aspect-ratio, avatar
- badge, breadcrumb, calendar, carousel, chart
- collapsible, command, context-menu, data-table
- date-picker, drawer, hover-card, menubar
- navigation-menu, pagination, popover, progress
- radio-group, resizable, scroll-area, separator
- sheet, skeleton, slider, switch, tabs
- textarea, toggle, toggle-group, tooltip

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

### Feature Component
```typescript
// src/features/{feature}/components/{name}-form.tsx
'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'

const schema = z.object({
  name: z.string().min(1, 'Required'),
})

type FormData = z.infer<typeof schema>

export function {Name}Form() {
  const form = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  const onSubmit = (data: FormData) => {
    // handle submit
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
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
        <Button type="submit">Submit</Button>
      </form>
    </Form>
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

## Noch zu erstellen (Foundation)

Diese Files müssen ZUERST erstellt werden bevor Features gebaut werden:

| Priorität | Datei | Status |
|-----------|-------|--------|
| 1 | `src/lib/axios.ts` (API client instance) | ❌ |
| 2 | `src/app/providers.tsx` (QueryClient, Theme) | ❌ |
| 3 | `src/app/router.tsx` (TanStack Router setup) | ❌ |
| 4 | `src/app/app.tsx` (Root component) | ❌ |
| 5 | `src/routes/__root.tsx` (Root route) | ❌ |
| 6 | `src/components/shared/layout/header.tsx` | ❌ |
| 7 | `src/components/shared/layout/sidebar.tsx` | ❌ |
| 8 | `src/test/setup.ts` konfigurieren | ❌ |

---

## Regeln für Task Erstellung

1. **IMMER** zuerst diese Datei lesen
2. **IMMER** prüfen ob Files/Packages/Components schon existieren
3. **IMMER** die Reihenfolge einhalten: Types → API → Hooks → Components → Routes
4. **IMMER** Tests mit einplanen
5. **IMMER** am Ende diese Datei updaten
6. **NIE** Packages doppelt installieren
7. **NIE** Shadcn Komponenten doppelt installieren
8. **NIE** Files überschreiben ohne zu fragen
9. **IMMER** barrel exports in feature/index.ts pflegen

---

## Letzte Änderung

- **Datum:** 2024-12-24
- **Status:** Initial Setup - Feature folders leer, shadcn/ui Basis-Komponenten installiert
- **Nächster Task:** Foundation Files erstellen (axios, providers, router)

# Task: Frontend Foundation & Layout System

## 1. Übersicht

**Was wird gebaut?**
Die komplette Frontend-Grundstruktur für das ExoAuth Admin Dashboard: Layout-System, Theme, i18n, Routing, Shared Components und DataTable.

**Warum?**
- Solide Basis für alle kommenden Features
- Konsistente UX über die gesamte App
- Germany-ready: GDPR-Banner, Impressum, DE-Locale
- Permission-basierte Navigation von Anfang an

**Architektur-Kontext:**
```
┌─────────────────────────────────────────────────────────────┐
│  FRONTEND FOUNDATION (Diese Task)                           │
│  ├── Layout (Header, Sidebar, Main Content)                 │
│  ├── Theme (Red/Rose Primary, Dark Mode)                    │
│  ├── i18n (EN + DE, Multi-File)                             │
│  ├── Routing (TanStack Router, Protected Routes)            │
│  ├── Shared Components (DataTable, Modals, etc.)            │
│  ├── Auth Context (User State, Permission Check)            │
│  └── API Client (Axios, HTTP-only Cookie Auth)              │
├─────────────────────────────────────────────────────────────┤
│  BACKEND (.NET Core)                                        │
│  ├── HTTP-only Cookies for Auth (not localStorage!)         │
│  ├── JWT Access Token (15 min) + Refresh Token (30 days)    │
│  └── API Response: { status, statusCode, data, errors }     │
├─────────────────────────────────────────────────────────────┤
│  FEATURES (Später - Task 004+)                              │
│  ├── Auth Pages (Login, Register, Accept Invite)            │
│  ├── System Users                                           │
│  ├── Permissions                                            │
│  └── Audit Logs                                             │
└─────────────────────────────────────────────────────────────┘
```

## 2. Design Decisions (Alle Entscheidungen)

### Layout & Navigation
| Entscheidung | Wahl |
|--------------|------|
| Sidebar | Fixed, collapsible to icons auf Desktop |
| Mobile | Hamburger menu, slides von links |
| Tabellen auf Mobile | Card Layout statt Tabelle |
| Breadcrumbs | Immer sichtbar |
| Sidebar Sections | Labeled groups (SYSTEM, MANAGEMENT, etc.) |
| Logo | Oben in Sidebar, collapsed = Icon only |
| Navigation | Permission-based (verstecke Items ohne Berechtigung) |

### Theme & Styling
| Entscheidung | Wahl |
|--------------|------|
| Primary Color | Red/Rose (`rose-500` / `#f43f5e` als Basis) |
| Secondary | Slate/Gray neutrals |
| Dark Mode | True dark (#0a0a0a), System Preference als Default |
| Font | Inter |
| Animationen | Minimal/None - schnell & funktional |
| Focus Rings | Sichtbar für Accessibility |

### Components & UX
| Entscheidung | Wahl |
|--------------|------|
| Forms für Create/Edit | Modal Dialogs |
| Forms für Details | Slide-out Sheet |
| Unsaved Changes | Warning beim Verlassen mit Änderungen |
| Confirmations | Simple für Users, Type-to-Confirm für kritisch (Projects) |
| Loading States | Skeletons für Initial, Spinners für Actions |
| Empty States | Illustration + Text + CTA |
| Error Pages | Illustration + freundliche Nachricht |
| Toasts | Bottom-right (Sonner) |
| Avatars | Initials mit farbigem Hintergrund |
| Status Badges | Colored pill mit Dot |

### DataTable Features
| Feature | Implementation |
|---------|----------------|
| Infinite Scroll | Cursor-based Pagination vom Backend |
| Filters | Dropdown Panel |
| Sorting | Multi-Column, Click auf Header |
| Column Visibility | Toggle Dropdown |
| Row Actions | Three-Dot Menu (⋮) |
| Bulk Actions | Checkbox + Floating Bar unten |
| Text Overflow | Truncate + Tooltip |
| Density | Comfortable |
| Persistence | localStorage für Preferences |
| Mobile | Card Layout |

### Search & Navigation
| Feature | Wahl |
|---------|------|
| Global Search | Cmd+K Spotlight |
| Keyboard Shortcuts | Full Support (Esc, Arrows, etc.) |
| Session Timeout | Warning Modal vor Logout |
| Notifications | Placeholder Bell Icon (später) |
| Help Button | Floating "?" in Ecke |

### Forms & Validation
| Feature | Wahl |
|---------|------|
| Validation Timing | On blur + Submit |
| Password | Toggle Visibility + Strength Indicator |
| Copy Feedback | Icon Change + Toast |

### i18n & Germany
| Feature | Wahl |
|---------|------|
| Sprachen | EN + DE von Anfang an |
| Default | Browser Language Detection |
| Date Format | Locale-aware (25.12.2025 für DE) |
| Numbers | Compact Notation (1.2K) |
| Cookie Consent | Full GDPR Banner |
| Legal Footer | Immer sichtbar (Impressum, Privacy, Terms) |
| i18n Struktur | Multi-File (common.json, auth.json, etc.) |

### User Experience
| Feature | Wahl |
|---------|------|
| Profile | Dropdown + Settings Page |
| API Errors | User-friendly übersetzte Messages |
| Login Page | Centered Card auf Gradient |
| Print Styles | Optimiert für Audit Logs |
| Onboarding | Tooltip Tour für neue User (später) |
| Dates | Relative + Tooltip ("2h ago" → Hover zeigt voll) |
| URL Structure | Query Params für Filter/Sort (shareable) |
| State Management | TanStack Query + React Context |
| File Upload (später) | Dropzone Style |

### Authentication & API (Backend Integration)
| Feature | Implementation |
|---------|----------------|
| Auth Method | HTTP-only Cookies (NOT localStorage) |
| Access Token | Cookie `access_token`, HttpOnly, Secure, SameSite=Strict, 15 min |
| Refresh Token | Cookie `refresh_token`, HttpOnly, Secure, SameSite=Strict, 30 days |
| Token Refresh | Automatic via axios interceptor on 401 |
| API Client | Axios with `withCredentials: true` |
| API Base URL | `/api` (via Vite proxy or env var) |

### Backend API Response Format
```typescript
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
  errors?: Array<{
    field?: string
    code: string
    message: string
  }>
}
```

### Auth Endpoints (Backend)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/register` | POST | Register new user (first user = admin) |
| `/api/auth/login` | POST | Login with email/password |
| `/api/auth/logout` | POST | Logout, revoke refresh token |
| `/api/auth/refresh` | POST | Refresh access token |
| `/api/auth/me` | GET | Get current user info |
| `/api/auth/accept-invite` | POST | Accept invitation |

### User DTO (from Backend)
```typescript
interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
  lastLoginAt: string | null
  createdAt: string
  permissions: string[]  // Array of permission strings
}
```

## 3. Theme / CSS Variables

### Light Mode (Rose Theme)
```css
:root {
  /* Primary - Rose/Red */
  --primary: 346.8 77.2% 49.8%;           /* rose-500 */
  --primary-foreground: 0 0% 100%;

  /* Secondary - Slate */
  --secondary: 210 40% 96%;
  --secondary-foreground: 215.4 16.3% 46.9%;

  /* Background & Foreground */
  --background: 0 0% 100%;
  --foreground: 222.2 84% 4.9%;

  /* Card */
  --card: 0 0% 100%;
  --card-foreground: 222.2 84% 4.9%;

  /* Popover */
  --popover: 0 0% 100%;
  --popover-foreground: 222.2 84% 4.9%;

  /* Muted */
  --muted: 210 40% 96%;
  --muted-foreground: 215.4 16.3% 46.9%;

  /* Accent */
  --accent: 210 40% 96%;
  --accent-foreground: 222.2 47.4% 11.2%;

  /* Destructive */
  --destructive: 0 84.2% 60.2%;
  --destructive-foreground: 0 0% 100%;

  /* Border & Input */
  --border: 214.3 31.8% 91.4%;
  --input: 214.3 31.8% 91.4%;
  --ring: 346.8 77.2% 49.8%;              /* Rose ring */

  /* Sidebar */
  --sidebar-background: 0 0% 98%;
  --sidebar-foreground: 240 5.3% 26.1%;
  --sidebar-primary: 346.8 77.2% 49.8%;
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 240 4.8% 95.9%;
  --sidebar-accent-foreground: 240 5.9% 10%;
  --sidebar-border: 220 13% 91%;
  --sidebar-ring: 346.8 77.2% 49.8%;

  /* Chart Colors */
  --chart-1: 346.8 77.2% 49.8%;           /* Primary rose */
  --chart-2: 173 58% 39%;
  --chart-3: 197 37% 24%;
  --chart-4: 43 74% 66%;
  --chart-5: 27 87% 67%;

  /* Radius */
  --radius: 0.5rem;
}
```

### Dark Mode
```css
.dark {
  /* Primary - Rose (slightly adjusted for dark) */
  --primary: 346.8 77.2% 49.8%;
  --primary-foreground: 0 0% 100%;

  /* Secondary */
  --secondary: 217.2 32.6% 17.5%;
  --secondary-foreground: 210 40% 98%;

  /* Background - True Dark */
  --background: 0 0% 3.9%;                /* #0a0a0a */
  --foreground: 0 0% 98%;

  /* Card */
  --card: 0 0% 5.9%;
  --card-foreground: 0 0% 98%;

  /* Popover */
  --popover: 0 0% 5.9%;
  --popover-foreground: 0 0% 98%;

  /* Muted */
  --muted: 0 0% 14.9%;
  --muted-foreground: 0 0% 63.9%;

  /* Accent */
  --accent: 0 0% 14.9%;
  --accent-foreground: 0 0% 98%;

  /* Destructive */
  --destructive: 0 62.8% 30.6%;
  --destructive-foreground: 0 0% 98%;

  /* Border & Input */
  --border: 0 0% 14.9%;
  --input: 0 0% 14.9%;
  --ring: 346.8 77.2% 49.8%;

  /* Sidebar */
  --sidebar-background: 0 0% 5.9%;
  --sidebar-foreground: 240 4.8% 95.9%;
  --sidebar-primary: 346.8 77.2% 49.8%;
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 240 3.7% 15.9%;
  --sidebar-accent-foreground: 240 4.8% 95.9%;
  --sidebar-border: 240 3.7% 15.9%;
  --sidebar-ring: 346.8 77.2% 49.8%;
}
```

## 4. i18n Struktur

### Ordner Struktur
```
frontend/src/i18n/
├── index.ts                    # i18next Konfiguration
├── locales/
│   ├── en/
│   │   ├── common.json         # Buttons, Labels, Generics
│   │   ├── auth.json           # Login, Register, Invite
│   │   ├── navigation.json     # Sidebar, Breadcrumbs
│   │   ├── users.json          # System Users
│   │   ├── permissions.json    # Permissions
│   │   ├── audit.json          # Audit Logs
│   │   ├── settings.json       # Settings
│   │   ├── errors.json         # Error Messages
│   │   └── validation.json     # Form Validation
│   └── de/
│       ├── common.json
│       ├── auth.json
│       ├── navigation.json
│       ├── users.json
│       ├── permissions.json
│       ├── audit.json
│       ├── settings.json
│       ├── errors.json
│       └── validation.json
```

### i18next Config
```typescript
// src/i18n/index.ts
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

// Import all locale files
import enCommon from './locales/en/common.json'
import enAuth from './locales/en/auth.json'
// ... etc

import deCommon from './locales/de/common.json'
import deAuth from './locales/de/auth.json'
// ... etc

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        common: enCommon,
        auth: enAuth,
        // ...
      },
      de: {
        common: deCommon,
        auth: deAuth,
        // ...
      },
    },
    fallbackLng: 'en',
    defaultNS: 'common',
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  })

export default i18n
```

## 5. Sidebar Navigation (Permission-Based)

### Navigation Config
```typescript
// src/config/navigation.ts
import {
  LayoutDashboard,
  Users,
  Shield,
  FileText,
  Settings,
  Building2,
  FolderKanban,
} from 'lucide-react'

export interface NavItem {
  label: string           // i18n key
  href: string
  icon: LucideIcon
  permission?: string     // Required permission (optional = always visible)
  badge?: number | string // Optional badge count
}

export interface NavSection {
  label: string           // i18n key for section header
  items: NavItem[]
}

export const navigation: NavSection[] = [
  {
    label: 'navigation:sections.system',
    items: [
      {
        label: 'navigation:items.dashboard',
        href: '/dashboard',
        icon: LayoutDashboard,
        // No permission = always visible
      },
      {
        label: 'navigation:items.users',
        href: '/users',
        icon: Users,
        permission: 'system:users:read',
      },
      {
        label: 'navigation:items.permissions',
        href: '/permissions',
        icon: Shield,
        permission: 'system:users:read',
      },
      {
        label: 'navigation:items.auditLogs',
        href: '/audit-logs',
        icon: FileText,
        permission: 'system:audit:read',
      },
    ],
  },
  {
    label: 'navigation:sections.management',
    items: [
      {
        label: 'navigation:items.organizations',
        href: '/organizations',
        icon: Building2,
        permission: 'system:organizations:read',
      },
      {
        label: 'navigation:items.projects',
        href: '/projects',
        icon: FolderKanban,
        permission: 'system:projects:read',
      },
    ],
  },
  {
    label: 'navigation:sections.settings',
    items: [
      {
        label: 'navigation:items.settings',
        href: '/settings',
        icon: Settings,
        permission: 'system:settings:read',
      },
    ],
  },
]
```

## 6. Files zu erstellen

### Foundation / Core
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| API Client | `src/lib/axios.ts` | ✅ | Axios instance mit Interceptors |
| i18n Config | `src/i18n/index.ts` | ✅ | i18next Setup |
| EN Common | `src/i18n/locales/en/common.json` | ✅ | Englische Common Translations |
| EN Auth | `src/i18n/locales/en/auth.json` | ✅ | Englische Auth Translations |
| EN Navigation | `src/i18n/locales/en/navigation.json` | ✅ | Englische Nav Translations |
| EN Users | `src/i18n/locales/en/users.json` | ✅ | Englische User Translations |
| EN Errors | `src/i18n/locales/en/errors.json` | ✅ | Englische Error Translations |
| EN Validation | `src/i18n/locales/en/validation.json` | ✅ | Englische Validation Translations |
| DE Common | `src/i18n/locales/de/common.json` | ✅ | Deutsche Common Translations |
| DE Auth | `src/i18n/locales/de/auth.json` | ✅ | Deutsche Auth Translations |
| DE Navigation | `src/i18n/locales/de/navigation.json` | ✅ | Deutsche Nav Translations |
| DE Users | `src/i18n/locales/de/users.json` | ✅ | Deutsche User Translations |
| DE Errors | `src/i18n/locales/de/errors.json` | ✅ | Deutsche Error Translations |
| DE Validation | `src/i18n/locales/de/validation.json` | ✅ | Deutsche Validation Translations |

### Types (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Auth Types | `src/types/auth.ts` | ✅ | User, Token, LoginCredentials, etc. |
| API Types | `src/types/api.ts` | ✅ | ApiResponse, ApiError, Pagination |
| Table Types | `src/types/table.ts` | ✅ | Column, Filter, Sort Definitions |
| Index | `src/types/index.ts` | ✅ | Barrel Export |

### Hooks (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| useDebounce | `src/hooks/use-debounce.ts` | ✅ | Debounce Hook |
| useLocalStorage | `src/hooks/use-local-storage.ts` | ✅ | LocalStorage Hook |
| useMediaQuery | `src/hooks/use-media-query.ts` | ✅ | Responsive Hook (+ useIsMobile, useIsDesktop) |
| useCopyToClipboard | `src/hooks/use-copy-to-clipboard.ts` | ✅ | Copy Hook |
| useTablePreferences | `src/hooks/use-table-preferences.ts` | ✅ | Table State Persistence |
| Index | `src/hooks/index.ts` | ✅ | Barrel Export |

### App Setup
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Providers | `src/app/providers.tsx` | ✅ | QueryClient, Theme, Auth, Sidebar, Toaster |
| Router | `src/app/router.tsx` | ⏳ | TanStack Router Config |
| Root Route | `src/routes/__root.tsx` | ⏳ | Root Route mit Layout |

### Contexts (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Auth Context | `src/contexts/auth-context.tsx` | ✅ | Auth State, User, Permissions, Login/Logout |
| Theme Provider | `src/contexts/theme-context.tsx` | ✅ | Dark/Light/System Mode |
| Sidebar Context | `src/contexts/sidebar-context.tsx` | ✅ | Collapsed State |
| Index | `src/contexts/index.ts` | ✅ | Barrel Export |

### Config (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Navigation Config | `src/config/navigation.ts` | ✅ | Sidebar Items mit Permissions |

### Layout Components (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| App Layout | `src/components/shared/layout/app-layout.tsx` | ✅ | Main Layout Wrapper |
| Sidebar | `src/components/shared/layout/sidebar.tsx` | ✅ | Collapsible Nav (inkl. Item & Section) |
| Header | `src/components/shared/layout/header.tsx` | ✅ | Top Bar mit Breadcrumbs |
| User Menu | `src/components/shared/layout/user-menu.tsx` | ✅ | Profile Dropdown |
| Theme Toggle | `src/components/shared/layout/theme-toggle.tsx` | ✅ | Dark/Light/System Switch |
| Language Switcher | `src/components/shared/layout/language-switcher.tsx` | ✅ | EN/DE Switch |
| Breadcrumbs | `src/components/shared/layout/breadcrumbs.tsx` | ✅ | Navigation Breadcrumbs |
| Page Header | `src/components/shared/layout/page-header.tsx` | ✅ | Title + Description + Actions |
| Footer | `src/components/shared/layout/footer.tsx` | ✅ | Legal Links (Impressum, Privacy, Terms) |
| Mobile Nav | `src/components/shared/layout/mobile-nav.tsx` | ✅ | Hamburger + Sheet Navigation |
| Index | `src/components/shared/layout/index.ts` | ✅ | Barrel Export |

### Feedback Components (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Loading Spinner | `src/components/shared/feedback/loading-spinner.tsx` | ✅ | Spinner Component |
| Empty State | `src/components/shared/feedback/empty-state.tsx` | ✅ | No Data Illustration |
| Error State | `src/components/shared/feedback/error-state.tsx` | ✅ | Error Display |
| Confirm Dialog | `src/components/shared/feedback/confirm-dialog.tsx` | ✅ | Simple Confirm |
| Type Confirm | `src/components/shared/feedback/type-confirm-dialog.tsx` | ✅ | Type to Confirm |
| Unsaved Warning | `src/components/shared/feedback/unsaved-warning.tsx` | ✅ | Leave Form Warning |
| Index | `src/components/shared/feedback/index.ts` | ✅ | Barrel Export |

### DataTable Components (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| DataTable | `src/components/shared/data-table/data-table.tsx` | ✅ | Main Table Component |
| DataTable Toolbar | `src/components/shared/data-table/data-table-toolbar.tsx` | ✅ | Toolbar mit Search, Filter, Columns |
| DataTable Filters | `src/components/shared/data-table/data-table-filters.tsx` | ✅ | Filter Dropdown |
| DataTable Column Toggle | `src/components/shared/data-table/data-table-column-toggle.tsx` | ✅ | Show/Hide Columns |
| DataTable Pagination | `src/components/shared/data-table/data-table-pagination.tsx` | ✅ | Infinite Scroll Logic |
| DataTable Row Actions | `src/components/shared/data-table/data-table-row-actions.tsx` | ✅ | Three-Dot Menu |
| DataTable Bulk Actions | `src/components/shared/data-table/data-table-bulk-actions.tsx` | ✅ | Floating Bar |
| DataTable Card | `src/components/shared/data-table/data-table-card.tsx` | ✅ | Mobile Card View |
| Index | `src/components/shared/data-table/index.ts` | ✅ | Barrel Export |

### Form Components (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Password Input | `src/components/shared/form/password-input.tsx` | ✅ | Toggle + Strength |
| Password Strength | `src/components/shared/form/password-strength.tsx` | ✅ | Strength Indicator |
| Form Sheet | `src/components/shared/form/form-sheet.tsx` | ✅ | Slide-out Form |
| Form Modal | `src/components/shared/form/form-modal.tsx` | ✅ | Modal Form |
| Index | `src/components/shared/form/index.ts` | ✅ | Barrel Export |

### Utility Components (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| User Avatar | `src/components/shared/user-avatar.tsx` | ✅ | Initials Avatar |
| Status Badge | `src/components/shared/status-badge.tsx` | ✅ | Colored Pill |
| Copy Button | `src/components/shared/copy-button.tsx` | ✅ | Copy to Clipboard |
| Relative Time | `src/components/shared/relative-time.tsx` | ✅ | "2h ago" + Tooltip |
| Help Button | `src/components/shared/help-button.tsx` | ✅ | Floating ? |
| Command Menu | `src/components/shared/command-menu.tsx` | ✅ | Cmd+K Spotlight |
| Session Warning | `src/components/shared/session-warning.tsx` | ✅ | Timeout Modal |
| Cookie Consent | `src/components/shared/cookie-consent.tsx` | ✅ | GDPR Banner |
| Index | `src/components/shared/index.ts` | ✅ | Barrel Export |

### Routing & Error Pages (erstellt)
| Datei | Pfad | Status | Beschreibung |
|-------|------|--------|--------------|
| Router Config | `src/app/router.tsx` | ✅ | TanStack Router Setup |
| Root Route | `src/routes/__root.tsx` | ✅ | Route Tree with Layout |
| Protected Route | `src/routes/protected-route.tsx` | ✅ | Auth & Permission Guard |
| Not Found | `src/routes/not-found.tsx` | ✅ | 404 Page |
| Forbidden | `src/routes/forbidden.tsx` | ✅ | 403 Page |
| Server Error | `src/routes/server-error.tsx` | ✅ | 500 Page |
| Index | `src/routes/index.ts` | ✅ | Barrel Export |

## 7. Files zu ändern

| Datei | Status | Was ändern? |
|-------|--------|-------------|
| `src/main.tsx` | ✅ | i18n import, Providers, Router |
| `src/App.tsx` | ⏳ | Router verwenden |
| `src/styles/globals.css` | ✅ | Theme Variables (Rose), Dark Mode |
| `src/lib/utils.ts` | ✅ | Keine Änderungen nötig |
| `package.json` | ✅ | Neue Dependencies hinzugefügt |
| `vite.config.ts` | ✅ | Keine Änderungen nötig |

## 8. Neue Packages

| Package | Status | Warum? |
|---------|--------|--------|
| `i18next` | ✅ | Internationalization |
| `react-i18next` | ✅ | React Integration für i18n |
| `i18next-browser-languagedetector` | ✅ | Auto-detect Browser Language |
| `@tanstack/react-table` | ✅ | Headless Table |
| `cmdk` | ✅ | Command Menu (Cmd+K) |
| `date-fns` | ✅ | Date Formatting |
| `react-intersection-observer` | ✅ | Infinite Scroll |

### Shadcn/UI Komponenten (installiert)
| Komponente | Status | Command |
|------------|--------|---------|
| Sheet | ✅ | `npx shadcn@latest add sheet` |
| Avatar | ✅ | `npx shadcn@latest add avatar` |
| Badge | ✅ | `npx shadcn@latest add badge` |
| Breadcrumb | ✅ | `npx shadcn@latest add breadcrumb` |
| Command | ✅ | `npx shadcn@latest add command` |
| Alert Dialog | ✅ | `npx shadcn@latest add alert-dialog` |
| Tooltip | ✅ | `npx shadcn@latest add tooltip` |
| Progress | ✅ | `npx shadcn@latest add progress` |
| Skeleton | ✅ | `npx shadcn@latest add skeleton` |
| Separator | ✅ | `npx shadcn@latest add separator` |
| Scroll Area | ✅ | `npx shadcn@latest add scroll-area` |
| Dropdown Menu | ✅ | `npx shadcn@latest add dropdown-menu` |
| Sonner | ✅ | `npx shadcn@latest add sonner` |
| Input | ✅ | `npx shadcn@latest add input` |
| Label | ✅ | `npx shadcn@latest add label` |
| Popover | ✅ | `npx shadcn@latest add popover` |
| Table | ✅ | `npx shadcn@latest add table` |
| Checkbox | ✅ | `npx shadcn@latest add checkbox` |

## 9. Implementation Reihenfolge

### Phase 1: Foundation
1. [x] **Packages**: Neue Dependencies installieren
2. [x] **Shadcn**: Neue UI Komponenten installieren
3. [x] **Theme**: globals.css mit Rose Theme updaten
4. [x] **i18n**: i18next Setup + alle Locale Files
5. [x] **Types**: Base Types erstellen (auth, api, table)
6. [x] **Lib**: axios.ts mit Interceptors erstellen
7. [x] **Hooks**: Utility Hooks erstellen

### Phase 2: Context & State
8. [x] **Context**: AuthContext erstellen
9. [x] **Context**: ThemeContext erstellen
10. [x] **Context**: SidebarContext erstellen
11. [x] **Providers**: App Providers Wrapper

### Phase 3: Layout
12. [x] **Layout**: AppLayout Component
13. [x] **Layout**: Sidebar (collapsible)
14. [x] **Layout**: Header mit User Menu
15. [x] **Layout**: Footer mit Legal Links
16. [x] **Layout**: Mobile Navigation
17. [x] **Layout**: Breadcrumbs
18. [x] **Layout**: Page Header

### Phase 4: Shared Components
19. [x] **Feedback**: Loading, Skeleton, Empty, Error States
20. [x] **Feedback**: Confirm Dialogs (simple + type)
21. [x] **Feedback**: Unsaved Warning
22. [x] **Utility**: Avatar, Badge, Copy Button
23. [x] **Utility**: Relative Time
24. [x] **Utility**: Command Menu (Cmd+K)
25. [x] **Utility**: Session Warning Modal
26. [x] **Utility**: Cookie Consent Banner
27. [x] **Utility**: Help Button

### Phase 5: DataTable
28. [x] **DataTable**: Base Table Component
29. [x] **DataTable**: Header (Search, Filter, Columns)
30. [x] **DataTable**: Filter Dropdown
31. [x] **DataTable**: Column Toggle
32. [x] **DataTable**: Infinite Scroll
33. [x] **DataTable**: Row Actions Menu
34. [x] **DataTable**: Bulk Actions Bar
35. [x] **DataTable**: Mobile Card View
36. [x] **DataTable**: Table Preferences Hook (already in hooks/)

### Phase 6: Forms
37. [x] **Form**: Password Input + Strength
38. [x] **Form**: Form Sheet (slide-out)
39. [x] **Form**: Form Modal

### Phase 7: Routing
40. [x] **Router**: TanStack Router Setup
41. [x] **Router**: Root Route mit Layout
42. [x] **Router**: Protected Route Wrapper
43. [x] **Router**: Error Pages (404, 403, 500)

### Phase 8: Polish
44. [x] **Print**: Print Styles für Tables
45. [x] **A11y**: Focus Styles, ARIA Labels, Reduced Motion
46. [x] **Main**: main.tsx updated, App.tsx removed
47. [ ] **Standards**: task_standards_frontend.md updaten

## 10. Component Specs

### Sidebar
```
┌─────────────────────────┐
│  [Logo]     [Collapse]  │ ← Logo + Toggle Button
├─────────────────────────┤
│  SYSTEM                 │ ← Section Label
│  ├─ 📊 Dashboard        │
│  ├─ 👥 Users            │
│  ├─ 🔐 Permissions      │
│  └─ 📝 Audit Logs       │
├─────────────────────────┤
│  MANAGEMENT             │
│  ├─ 🏢 Organizations    │
│  └─ 📁 Projects         │
├─────────────────────────┤
│  SETTINGS               │
│  └─ ⚙️ Settings         │
└─────────────────────────┘

Collapsed:
┌─────┐
│ [E] │ ← Logo Icon
├─────┤
│ 📊  │ ← Icon only, tooltip on hover
│ 👥  │
│ 🔐  │
│ 📝  │
├─────┤
│ 🏢  │
│ 📁  │
├─────┤
│ ⚙️  │
└─────┘
```

### Header
```
┌─────────────────────────────────────────────────────────────┐
│ [☰]  Dashboard > Users           [🔍] [🔔] [🌙] [User ▼]   │
└─────────────────────────────────────────────────────────────┘
  │      │                           │    │    │      │
  │      └─ Breadcrumbs              │    │    │      └─ User Menu
  │                                  │    │    └─ Theme Toggle
  └─ Mobile Hamburger                │    └─ Notifications (placeholder)
                                     └─ Search (opens Cmd+K)
```

### DataTable
```
┌─────────────────────────────────────────────────────────────┐
│ [🔍 Search...]  [Filters ▼]  [Columns ▼]      [+ Invite]   │
├─────────────────────────────────────────────────────────────┤
│ ☐ │ Email ↑↓           │ Name         │ Status  │ ⋮        │
├───┼─────────────────────┼──────────────┼─────────┼──────────┤
│ ☐ │ john@example.com    │ John Doe     │ 🟢 Active│ ⋮       │
│ ☐ │ jane@example.com    │ Jane Smith   │ 🟢 Active│ ⋮       │
│ ☐ │ bob@example.com     │ Bob Wilson   │ ⚫ Inactive│ ⋮     │
│   │ ... infinite scroll loads more ...                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  3 selected    [Delete]  [Export]  [×]                      │ ← Floating Bar
└─────────────────────────────────────────────────────────────┘
```

### Mobile Card (statt Table Row)
```
┌─────────────────────────────────────────┐
│ [JD]  John Doe                    [⋮]  │
│       john@example.com                  │
│                                         │
│  🟢 Active            Dec 25, 2025     │
└─────────────────────────────────────────┘
```

## 11. Keyboard Shortcuts

| Shortcut | Aktion |
|----------|--------|
| `Cmd/Ctrl + K` | Open Command Menu (Search) |
| `Escape` | Close Modal/Sheet/Menu |
| `Enter` | Submit Form |
| `Tab` | Navigate Form Fields |
| `Arrow Up/Down` | Navigate Lists |
| `Cmd/Ctrl + S` | Save (in forms) |

## 12. Responsive Breakpoints

| Breakpoint | Width | Verhalten |
|------------|-------|-----------|
| Mobile | < 640px | Hamburger, Cards statt Tables |
| Tablet | 640px - 1024px | Collapsed Sidebar |
| Desktop | > 1024px | Full Sidebar |

## 13. Nach Completion

- [ ] Alle Components funktionieren
- [x] Dark Mode funktioniert (ThemeContext erstellt)
- [x] i18n EN + DE funktioniert (alle Locale Files erstellt)
- [ ] Responsive funktioniert
- [ ] Keyboard Navigation funktioniert
- [ ] Print Styles funktionieren
- [ ] `task_standards_frontend.md` aktualisiert
- [x] TypeScript keine Errors (Build passed)
- [ ] Lint passed

---

## Notizen

- **Permission Check**: Sidebar Items werden nur gezeigt wenn User die Permission hat
- **Auth Tokens**: Werden in HTTP-only Cookies gespeichert (NICHT localStorage!)
  - Backend setzt Cookies automatisch bei Login/Register
  - Frontend sendet Cookies automatisch via `withCredentials: true`
- **LocalStorage Keys** (nur für UI preferences):
  - `exoauth-theme` für Dark/Light/System
  - `exoauth-sidebar` für Collapsed State
  - `exoauth-sidebar-mobile` für Mobile Sheet State
  - `exoauth-language` für Sprache (i18next)
  - `exoauth-table-{id}` für Table Preferences (sorting, columns, pageSize)
- **Cookie Consent**: Muss vor Analytics/Tracking gezeigt werden
- **Session Warning**: 5 Minuten vor Ablauf zeigen

---

## Erstellte Dateien Übersicht

### Phase 1-3 Complete (39 Files)

**Core/Lib:**
- `src/lib/axios.ts`

**i18n (13 files):**
- `src/i18n/index.ts`
- `src/i18n/locales/en/{common,auth,navigation,users,errors,validation}.json`
- `src/i18n/locales/de/{common,auth,navigation,users,errors,validation}.json`

**Types (4 files):**
- `src/types/{auth,api,table,index}.ts`

**Hooks (6 files):**
- `src/hooks/{use-debounce,use-local-storage,use-media-query,use-copy-to-clipboard,use-table-preferences,index}.ts`

**Contexts (4 files):**
- `src/contexts/{auth-context,theme-context,sidebar-context,index}.tsx`

**Config (1 file):**
- `src/config/navigation.ts`

**App (1 file):**
- `src/app/providers.tsx`

**Layout Components (11 files):**
- `src/components/shared/layout/{app-layout,sidebar,header,user-menu,theme-toggle,language-switcher,breadcrumbs,page-header,footer,mobile-nav,index}.tsx`

### Phase 4 Complete (16 Files)

**Feedback Components (7 files):**
- `src/components/shared/feedback/{loading-spinner,empty-state,error-state,confirm-dialog,type-confirm-dialog,unsaved-warning,index}.tsx`

**Utility Components (9 files):**
- `src/components/shared/user-avatar.tsx`
- `src/components/shared/status-badge.tsx`
- `src/components/shared/copy-button.tsx`
- `src/components/shared/relative-time.tsx`
- `src/components/shared/command-menu.tsx`
- `src/components/shared/session-warning.tsx`
- `src/components/shared/cookie-consent.tsx`
- `src/components/shared/help-button.tsx`
- `src/components/shared/index.ts`

### Phase 5 Complete (9 Files)

**DataTable Components (9 files):**
- `src/components/shared/data-table/data-table.tsx`
- `src/components/shared/data-table/data-table-toolbar.tsx`
- `src/components/shared/data-table/data-table-filters.tsx`
- `src/components/shared/data-table/data-table-column-toggle.tsx`
- `src/components/shared/data-table/data-table-pagination.tsx`
- `src/components/shared/data-table/data-table-row-actions.tsx`
- `src/components/shared/data-table/data-table-bulk-actions.tsx`
- `src/components/shared/data-table/data-table-card.tsx`
- `src/components/shared/data-table/index.ts`

**Shadcn Components added:**
- Table, Checkbox

### Phase 6 Complete (5 Files)

**Form Components (5 files):**
- `src/components/shared/form/password-input.tsx`
- `src/components/shared/form/password-strength.tsx`
- `src/components/shared/form/form-sheet.tsx`
- `src/components/shared/form/form-modal.tsx`
- `src/components/shared/form/index.ts`

### Phase 7 Complete (7 Files)

**Routing (7 files):**
- `src/app/router.tsx`
- `src/routes/__root.tsx`
- `src/routes/protected-route.tsx`
- `src/routes/not-found.tsx`
- `src/routes/forbidden.tsx`
- `src/routes/server-error.tsx`
- `src/routes/index.ts`

**Updated:**
- `src/main.tsx` - Added router, providers, i18n

### Phase 8 Complete (Polish)

**Updated files:**
- `src/styles/globals.css` - Tailwind v4 @theme, Print Styles, A11y Focus, Reduced Motion
- `src/App.tsx` - Removed (using RouterProvider now)
- `src/App.css` - Removed

---

**Letzte Änderung:** 2025-12-26
**Status:** In Progress (Phase 1-8 complete, only step 47 pending)

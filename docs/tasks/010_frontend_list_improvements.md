# Task 010: Frontend List Improvements & Session Details

## 1. Übersicht

**Was wird gebaut?**
Frontend-Anpassungen für Backend Task 009: User/Invite List Filter & Sorting, Edit Invite, Session Details Sheet, und Bug Fixes.

**Warum?**
- Backend hat neue Filter/Sort-Params die Frontend nutzen sollte
- User möchten Invites bearbeiten können
- Sessions zeigen viele Infos die im Detail sichtbar sein sollten
- SelectFilter Bug bei gleichen Namen

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich User nach Status filtern (active, locked, mfa, anonymized)
- Als Admin möchte ich Invites sortieren und nach Status filtern können
- Als Admin möchte ich Invites bearbeiten können (Name, Permissions)
- Als User möchte ich Session-Details sehen können (IP, Location, Browser, etc.)
- Als Admin möchte ich Session-Details von anderen Usern sehen können

### UI/UX Beschreibung

#### User List Filters
- Dropdown für Status-Filter (Active, Locked, MFA Enabled)
- Toggle für "Show Anonymized" (default: off)

#### Invite List Filters & Sorting
- Sort Dropdown: Created (newest/oldest), Expires (soon/later), Name, Email
- Status Multi-Select: Pending, Accepted, Expired, Revoked
- Defaults: Hide expired/revoked, Sort by newest

#### Edit Invite Modal
- Button in Invite Details Sheet (nur für pending invites)
- Modal mit: First Name, Last Name, Permissions (multi-select)
- Disabled wenn invite nicht mehr pending

#### Session Details Sheet
- Klick auf Session Card öffnet Sheet
- Zeigt alle Details: Device, Browser, OS, IP, Location, Timestamps
- Actions: Rename, Trust, Revoke (wie bisher, aber im Sheet)

### Akzeptanzkriterien
- [ ] User-Liste hat Filter für isActive, isLocked, mfaEnabled
- [ ] User-Liste hat Toggle für isAnonymized (default: hidden)
- [ ] Invite-Liste hat Sort-Dropdown
- [ ] Invite-Liste hat Status-Filter (multi-select)
- [ ] Invite-Liste versteckt expired/revoked standardmäßig
- [ ] Edit Invite Button im Details Sheet (nur pending)
- [ ] Edit Invite Modal mit Name & Permissions
- [ ] Session Details Sheet mit allen Device/Location Infos
- [ ] Session Details Sheet funktioniert in Settings UND Admin User Details
- [ ] SelectFilter Bug gefixt (duplicate names)
- [ ] INVITE_NOT_EDITABLE Error in translations

### Edge Cases / Error Handling
- Edit Button disabled wenn invite nicht pending
- Error Toast bei INVITE_NOT_EDITABLE
- Session Details Sheet zeigt "Unknown" für fehlende Daten
- Filter-Reset Button wenn Filter aktiv

## 3. API Integration

### Geänderte Endpoints

| Endpoint | Method | Neue Params | Hook ändern |
|----------|--------|-------------|-------------|
| /api/system-users | GET | `isActive`, `isAnonymized`, `isLocked`, `mfaEnabled` | `use-system-users.ts` |
| /api/system-invites | GET | `sort`, `statuses[]`, `includeExpired`, `includeRevoked` | `use-system-invites.ts` |

### Neue Endpoints

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| /api/system-invites/{id} | PATCH | `{ firstName?, lastName?, permissionIds? }` | `SystemInviteDto` | `use-update-invite.ts` |

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| SessionDetailsSheet | Feature/Auth | Session Details anzeigen |
| EditInviteModal | Feature/Users | Invite bearbeiten Modal |

### Bestehende Komponenten ändern
| Komponente | Was ändern? |
|------------|-------------|
| SelectFilter | Bug fix: `value={option.label}` → unique value |
| UsersTable | Filter Dropdowns hinzufügen |
| InvitationsTable | Sort Dropdown, Status Filter hinzufügen |
| InviteDetailsSheet | Edit Button hinzufügen |
| SessionCard | onClick prop für Sheet öffnen |
| SessionsList | Session Details Sheet integration |
| UserSessionsSection | Session Details Sheet integration |

## 5. Files zu erstellen

### Feature Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| SessionDetailsSheet | `src/features/auth/components/session-details-sheet.tsx` | Session Details Sheet |
| EditInviteModal | `src/features/users/components/edit-invite-modal.tsx` | Edit Invite Modal |
| useUpdateInvite | `src/features/users/hooks/use-update-invite.ts` | Update invite mutation |

## 6. Files zu ändern

### Bug Fix
| Datei | Was ändern? |
|-------|-------------|
| `src/components/shared/form/select-filter.tsx` | Line 123 & 199: `value={option.label}` → `value={option.value}` oder `value={\`${option.value}-${option.label}\`}` |

### User List Filters
| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/types/index.ts` | Filter params zu `SystemUsersQueryParams` hinzufügen |
| `src/features/users/api/users-api.ts` | Neue query params an API senden |
| `src/features/users/hooks/use-system-users.ts` | Filter options in hook |
| `src/features/users/components/users-table.tsx` | Filter UI hinzufügen |

### Invite List Filters & Sorting
| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/types/invites.ts` | Sort param, neue filter params |
| `src/features/users/api/invites-api.ts` | Neue query params (`sort`, `includeExpired`, `includeRevoked`) |
| `src/features/users/hooks/use-system-invites.ts` | Sort & neue filter options |
| `src/features/users/components/invitations-table.tsx` | Sort Dropdown, Status Filter UI |

### Edit Invite
| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/api/invites-api.ts` | `update` method hinzufügen (PATCH) |
| `src/features/users/components/invite-details-sheet.tsx` | Edit Button, Modal öffnen |
| `src/features/users/hooks/index.ts` | Export `useUpdateInvite` |

### Session Details Sheet
| Datei | Was ändern? |
|-------|-------------|
| `src/features/auth/components/session-card.tsx` | `onClick` prop hinzufügen |
| `src/features/auth/components/sessions-list.tsx` | Sheet state, onClick handling |
| `src/features/settings/components/sessions-section.tsx` | Sheet integration |
| `src/features/users/components/user-sessions-section.tsx` | Sheet integration (admin) |
| `src/features/auth/components/index.ts` | Export SessionDetailsSheet |

### Translations
| Datei | Was ändern? |
|-------|-------------|
| `src/i18n/locales/en/users.json` | Filter labels, edit invite texts |
| `src/i18n/locales/de/users.json` | Filter labels, edit invite texts |
| `src/i18n/locales/en/sessions.json` | Details sheet texts |
| `src/i18n/locales/de/sessions.json` | Details sheet texts |
| `src/i18n/locales/en/errors.json` | INVITE_NOT_EDITABLE |
| `src/i18n/locales/de/errors.json` | INVITE_NOT_EDITABLE |

## 7. Neue Dependencies

Keine neuen Packages erforderlich.

## 8. Implementation Reihenfolge

### Phase 1: Bug Fix ✅
1. [ ] `select-filter.tsx` - Fix duplicate name bug

### Phase 2: User List Filters
1. [ ] Types: `SystemUsersQueryParams` erweitern
2. [ ] API: `users-api.ts` params hinzufügen
3. [ ] Hook: `use-system-users.ts` filter options
4. [ ] UI: `users-table.tsx` filter dropdowns & toggle
5. [ ] i18n: Filter labels

### Phase 3: Invite List Filters & Sorting
1. [ ] Types: Sort & filter params in `invites.ts`
2. [ ] API: `invites-api.ts` neue params
3. [ ] Hook: `use-system-invites.ts` sort & filter
4. [ ] UI: `invitations-table.tsx` sort dropdown, status filter
5. [ ] i18n: Sort & filter labels

### Phase 4: Edit Invite
1. [ ] API: `invites-api.ts` update method
2. [ ] Hook: `use-update-invite.ts` erstellen
3. [ ] Component: `edit-invite-modal.tsx` erstellen
4. [ ] UI: `invite-details-sheet.tsx` edit button
5. [ ] i18n: Edit invite texts, INVITE_NOT_EDITABLE error

### Phase 5: Session Details Sheet
1. [ ] Component: `session-details-sheet.tsx` erstellen
2. [ ] Update: `session-card.tsx` onClick prop
3. [ ] Update: `sessions-list.tsx` sheet integration
4. [ ] Update: `sessions-section.tsx` (settings)
5. [ ] Update: `user-sessions-section.tsx` (admin)
6. [ ] i18n: Details sheet texts

### Phase 6: Finalisierung
1. [ ] Alle Tests grün
2. [ ] TypeScript keine Errors
3. [ ] `task_standards_frontend.md` aktualisieren

## 9. Tests

### Component Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/components/shared/form/__tests__/select-filter.test.tsx` | Bug fix - duplicate names |
| `src/features/users/__tests__/edit-invite-modal.test.tsx` | Edit invite flow |
| `src/features/auth/__tests__/session-details-sheet.test.tsx` | Session details display |

### Hook Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/users/__tests__/use-update-invite.test.ts` | Update invite mutation |

## 10. Technische Details

### SelectFilter Bug Fix

**Problem:**
```tsx
// Line 123 & 199 in select-filter.tsx
<CommandItem
  key={option.value}
  value={option.label}  // ← BUG: cmdk uses this for matching
  onSelect={() => handleSelect(option.value)}
>
```

**Fix:**
```tsx
<CommandItem
  key={option.value}
  value={`${option.value}::${option.label}`}  // Unique identifier
  onSelect={() => handleSelect(option.value)}
>
```

### User Filters - API Params

```typescript
// GET /api/system-users
interface SystemUsersQueryParams {
  cursor?: string
  limit?: number
  search?: string
  // NEW:
  isActive?: boolean        // null = all, true = active, false = inactive
  isAnonymized?: boolean    // default false (hide anonymized)
  isLocked?: boolean        // null = all, true = locked only
  mfaEnabled?: boolean      // null = all, true = mfa enabled only
}
```

### Invite Filters - API Params

```typescript
// GET /api/system-invites
interface SystemInvitesQueryParams {
  cursor?: string
  limit?: number
  search?: string
  // CHANGED:
  statuses?: string[]       // ['pending', 'accepted', 'expired', 'revoked']
  // NEW:
  sort?: string             // 'createdAt:desc' (default), 'email:asc', etc.
  includeExpired?: boolean  // default false
  includeRevoked?: boolean  // default false
}
```

### Edit Invite - API

```typescript
// PATCH /api/system-invites/{id}
interface UpdateInviteRequest {
  firstName?: string
  lastName?: string
  permissionIds?: string[]
}

// Response: SystemInviteDto
// Error: INVITE_NOT_EDITABLE (400) wenn nicht pending
```

### Session Details Sheet - Data Display

```typescript
interface DeviceSessionDto {
  id: string
  deviceId: string
  displayName: string          // "Chrome on Windows"
  deviceName: string | null    // Custom name
  browser: string | null       // "Chrome"
  browserVersion: string | null // "120.0.0"
  operatingSystem: string | null // "Windows"
  osVersion: string | null     // "11"
  deviceType: string | null    // "desktop" | "mobile" | "tablet"
  ipAddress: string | null     // "192.168.1.1"
  country: string | null       // "Germany"
  countryCode: string | null   // "DE"
  city: string | null          // "Berlin"
  locationDisplay: string | null // "Berlin, Germany"
  isTrusted: boolean
  isCurrent: boolean
  lastActivityAt: string       // ISO date
  createdAt: string            // ISO date
}
```

**Sheet Layout:**
```
┌─────────────────────────────────────┐
│ Session Details              [X]    │
├─────────────────────────────────────┤
│ ┌─────┐                             │
│ │ 💻  │  Chrome on Windows          │
│ └─────┘  [Current] [Trusted]        │
│                                     │
│ ─────────────────────────────────── │
│                                     │
│ Device Information                  │
│ Browser:     Chrome 120.0.0         │
│ OS:          Windows 11             │
│ Device Type: Desktop                │
│                                     │
│ Location                            │
│ IP Address:  192.168.1.1    [Copy]  │
│ Location:    Berlin, Germany        │
│                                     │
│ Timeline                            │
│ Created:     Dec 25, 2024, 10:30    │
│ Last Active: 2 hours ago            │
│                                     │
│ ─────────────────────────────────── │
│                                     │
│ [Rename]  [Trust]  [Revoke Session] │
└─────────────────────────────────────┘
```

## 11. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_frontend.md` aktualisiert (neue Files)
- [ ] `i18n-translations.md` aktualisiert (neue Keys)
- [ ] TypeScript keine Errors
- [ ] Lint passed

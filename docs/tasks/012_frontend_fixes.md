# Task 012: Frontend Fixes - Session Revoke, Auth Sync, Caching & UX

## 1. Übersicht

**Was wird gebaut?**
Frontend-Verbesserungen: Single Session Revoke UI, Multi-Tab Logout Sync, Cache-Bug Fix, Audit-Filter für anonymisierte User, und Error Message Update.

**Warum?**
- Admins können einzelne Sessions nicht über UI revoken (nur Backend-API existiert)
- Multi-Tab Bug: Logout in Tab A → Tab B bleibt "eingeloggt"
- Cache-Bug: Invite-Edit zeigt alte Daten (Query Key Mismatch)
- Anonymisierte User fehlen im Audit-Log Filter
- Permission-Error zeigt nur eine Permission statt beide

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich eine einzelne Session eines Users über die UI revoken können
- Als User möchte ich bei Logout in einem Tab automatisch in allen Tabs ausgeloggt werden
- Als Admin möchte ich nach Invite-Edit die aktualisierten Daten sehen
- Als Admin möchte ich im Audit-Log auch nach anonymisierten Usern filtern können
- Als Admin möchte ich bei Permission-Fehlern eine klare Fehlermeldung sehen

### Akzeptanzkriterien
- [ ] X-Button in Session-Liste (user-details-sheet) zum Revoken einzelner Sessions
- [ ] Revoke-Button im session-details-sheet
- [x] Multi-Tab Logout Sync via localStorage Event
- [x] Session-Expired führt zu Redirect (nicht nur force-reauth)
- [ ] Cache-Bug in use-update-invite.ts gefixt
- [x] Anonymisierte User im Audit-Log User-Filter mit Smart-Truncation
- [x] Error Message für SYSTEM_LAST_PERMISSION_HOLDER aktualisiert (beide Permissions)

### Edge Cases / Error Handling
- Was passiert wenn Session-Revoke fehlschlägt? → Toast Error
- Was passiert wenn eigene Session revoked wird? → Logout
- Was passiert bei vielen anonymisierten Usern? → Smart truncation "ano...abc1@deleted.local"

## 3. API Integration

| Endpoint | Method | Request | Response | Hook |
|----------|--------|---------|----------|------|
| /api/system/users/{userId}/sessions/{sessionId} | DELETE | - | `{ revoked: true }` | useRevokeUserSession (exists, needs update) |

## 4. Komponenten Übersicht

### Bestehende Komponenten ändern
| Komponente | Was ändern? |
|------------|-------------|
| user-details-sheet.tsx | X-Button pro Session |
| session-details-sheet.tsx | Revoke-Button hinzufügen |
| auth-context.tsx | Storage event listener + session-expired redirect |
| audit-logs-table.tsx | Anonymisierte User in Filter + Smart truncation |

### Hooks ändern
| Hook | Was ändern? |
|------|-------------|
| use-update-invite.ts | Query key fix: `['system-invite', id]` → `[...SYSTEM_INVITES_KEY, id]` |
| use-revoke-user-session.ts | Update to call single session endpoint |

## 5. Files zu ändern

### Phase 1: Cache Bug Fix (Critical)
| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/hooks/use-update-invite.ts` | Fix query key mismatch |

### Phase 2: Single Session Revoke UI
| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/hooks/use-revoke-user-session.ts` | Call single session endpoint |
| `src/features/users/components/user-details-sheet.tsx` | Add X button per session row |
| `src/features/auth/components/session-details-sheet.tsx` | Add Revoke button |

### Phase 3: Multi-Tab Auth Sync
| Datei | Was ändern? |
|-------|-------------|
| `src/contexts/auth-context.tsx` | Add storage event listener + redirect on session-expired |

### Phase 4: Audit Log Filter
| Datei | Was ändern? |
|-------|-------------|
| `src/features/audit-logs/components/audit-logs-table.tsx` | Include anonymized users, smart truncation |

### Phase 5: Error Message
| Datei | Was ändern? |
|-------|-------------|
| `src/i18n/locales/en/errors.json` | Update SYSTEM_LAST_PERMISSION_HOLDER message |
| `src/i18n/locales/de/errors.json` | Update SYSTEM_LAST_PERMISSION_HOLDER message |

## 6. Implementation Reihenfolge

### Phase 1: Cache Bug Fix
1. [ ] `use-update-invite.ts` - Fix query key from `['system-invite', id]` to `[...SYSTEM_INVITES_KEY, id]`

### Phase 2: Single Session Revoke UI
1. [ ] `use-revoke-user-session.ts` - Update to use single session endpoint
2. [ ] `user-details-sheet.tsx` - Add X button in sessions section
3. [ ] `session-details-sheet.tsx` - Add Revoke button with confirmation

### Phase 3: Multi-Tab Auth Sync
1. [x] `auth-context.tsx` - Add storage event listener for `exoauth_has_session`
2. [x] `auth-context.tsx` - Add redirect on `auth:session-expired` event

### Phase 4: Audit Log Filter
1. [x] `audit-logs-table.tsx` - Fetch users with anonymized included
2. [x] `audit-logs-table.tsx` - Smart truncation helper for long anonymized emails

### Phase 5: Error Message Update
1. [x] `en/errors.json` - Update message to mention both permissions
2. [x] `de/errors.json` - Update message to mention both permissions

### Phase 6: Finalisierung
1. [ ] Alle Tests grün
2. [ ] TypeScript keine Errors
3. [ ] Lint passed
4. [ ] `task_standards_frontend.md` aktualisieren

## 7. Technische Details

### Cache Bug Fix
```typescript
// use-update-invite.ts - BEFORE (wrong)
queryClient.invalidateQueries({ queryKey: ['system-invite', variables.id] })

// AFTER (correct)
queryClient.invalidateQueries({ queryKey: [...SYSTEM_INVITES_KEY, variables.id] })
```

### Multi-Tab Logout Sync
```typescript
// auth-context.tsx - Add to useEffect
useEffect(() => {
  const handleStorageChange = (e: StorageEvent) => {
    if (e.key === AUTH_SESSION_KEY && !e.newValue) {
      // Logged out in another tab
      queryClient.setQueryData(AUTH_QUERY_KEY, null)
      queryClient.clear()
    }
  }
  window.addEventListener('storage', handleStorageChange)
  return () => window.removeEventListener('storage', handleStorageChange)
}, [queryClient])
```

### Session Expired Redirect
```typescript
// auth-context.tsx - Update handleSessionExpired
const handleSessionExpired = () => {
  queryClient.setQueryData(AUTH_QUERY_KEY, null)
  queryClient.clear()
  // Add redirect
  window.location.href = '/login'
}
```

### Smart Truncation for Anonymized Users
```typescript
// Helper function for audit-logs-table.tsx
function formatUserLabel(user: SystemUserDto): string {
  if (user.isAnonymized) {
    // "anonymized-abc12345-def6-7890@deleted.local" → "ano...7890@deleted.local"
    const email = user.email
    const atIndex = email.indexOf('@')
    const localPart = email.substring(0, atIndex)
    const domain = email.substring(atIndex)
    // Get last 4 chars of local part
    const suffix = localPart.slice(-4)
    return `ano...${suffix}${domain}`
  }
  return user.fullName || user.email
}
```

### Single Session Revoke Hook Update
```typescript
// use-revoke-user-session.ts
export function useRevokeUserSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, sessionId }: { userId: string; sessionId: string }) =>
      userAdminApi.revokeSession(userId, sessionId),  // Single session endpoint
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['user-sessions', variables.userId] })
    },
  })
}
```

### Error Message Update
```json
// en/errors.json
{
  "SYSTEM_LAST_PERMISSION_HOLDER": "Cannot remove permission '{{permission}}' from the last user who holds it. At least one user must have 'system:users:update' and 'system:users:read' permissions."
}

// de/errors.json
{
  "SYSTEM_LAST_PERMISSION_HOLDER": "Die Berechtigung '{{permission}}' kann nicht vom letzten Benutzer entfernt werden. Mindestens ein Benutzer muss die Berechtigungen 'system:users:update' und 'system:users:read' besitzen."
}
```

## 8. Tests

### Existing Tests to Verify
| Test Datei | Was prüfen? |
|------------|-------------|
| `use-system-users.test.ts` | Verify still passes |
| `users-table.test.tsx` | Verify still passes |

## 9. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_frontend.md` aktualisiert
- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] Manual testing: Multi-tab logout, session revoke, invite edit cache

## 10. Letzte Änderung

- **Datum:** 2025-12-31
- **Status:** In Progress (Phases 3, 4, 5 complete - Phases 1, 2 pending)

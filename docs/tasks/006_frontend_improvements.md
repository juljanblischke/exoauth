# Task: Frontend Improvements - Bugs, Invitations, Legal Pages & Security

## 1. Übersicht

**Was wird gebaut?**
Bug fixes (Audit Log User Sheet, Invite Page Redirect), Invitations Management Tab in Users Page, Force Re-Auth Handling, und Placeholder Legal Pages (Impressum, Datenschutz, AGB).

**Warum?**
- Audit Log zeigt falsche User-Daten (lastLoginAt immer null)
- Eingeloggte User werden von Invite-Seite redirected statt auto-logout
- Backend hat jetzt Invitation Management (Task 005) - Frontend fehlt
- Force Re-Auth bei Permission-Änderungen muss im Frontend behandelt werden
- Legal Pages fehlen (Impressum, Datenschutz, AGB) - rechtlich erforderlich für Deutschland

## 2. User Experience / Anforderungen

### User Stories

**Bugs:**
- Als **User** möchte ich im Audit Log auf einen User klicken und korrekte Daten sehen (lastLoginAt, etc.)
- Als **eingeladener User** möchte ich die Invite-Seite aufrufen können, auch wenn ich eingeloggt bin (auto-logout)

**Invitations:**
- Als **Admin** möchte ich alle Einladungen in einem Tab neben Users sehen
- Als **Admin** möchte ich Einladungen nach Status filtern können (pending, expired, revoked, accepted)
- Als **Admin** möchte ich eine Einladung widerrufen können
- Als **Admin** möchte ich eine Einladung erneut senden können (mit Cooldown-Feedback)
- Als **Admin** möchte ich Details einer Einladung sehen können (wer hat eingeladen, wann läuft sie ab, etc.)

**Security:**
- Als **User** dessen Permissions geändert wurden, erwarte ich einen Toast + Redirect zum Login

**Legal:**
- Als **Besucher** möchte ich Impressum, Datenschutz und AGB im Footer erreichen können

### UI/UX Beschreibung

**Users Page mit Tabs:**
```
┌─────────────────────────────────────────────────────────────┐
│  Users                                                       │
│  ───────────────────────────────────────────────────────────│
│  [Users (24)]  [Invitations (3)]                            │
│  ───────────────────────────────────────────────────────────│
│                                                              │
│  🔍 Search...              [Status ▼]      [+ Invite User]  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Email           │ Name      │ Status  │ Expires  │ ⋮   │ │
│  │ john@test.de    │ John Doe  │ Pending │ in 2d    │ ⋮   │ │
│  │ anna@test.de    │ Anna Mü.. │ Expired │ -        │ ⋮   │ │
│  │ max@test.de     │ Max Must. │ Revoked │ -        │ ⋮   │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Row Actions (⋮):**
- View Details → Opens sheet with full info
- Resend → POST /api/system/invites/{id}/resend (if pending/expired, shows cooldown error)
- Revoke → POST /api/system/invites/{id}/revoke (if pending, confirm dialog)

**Force Re-Auth Toast:**
```
┌─────────────────────────────────────────┐
│ ⚠️ Session expired                      │
│ Your permissions were changed.          │
│ Please log in again.                    │
└─────────────────────────────────────────┘
→ Redirect to /login
```

### Akzeptanzkriterien

**Bugs:**
- [x] Audit Log User Sheet zeigt korrekte lastLoginAt (aus API, nicht aus Prop)
- [x] Invite Page: Auto-logout wenn bereits eingeloggt, dann Invite-Seite anzeigen

**Invitations Tab:**
- [x] Tab-Navigation zwischen Users und Invitations
- [x] Invitations Table mit Pagination, Search, Status-Filter
- [x] Status Badge (pending=warning, accepted=success, expired=muted, revoked=error)
- [x] Row Actions: View Details, Resend, Revoke
- [x] Invite Details Sheet mit allen Infos
- [x] Resend zeigt Toast bei Erfolg oder Cooldown-Error
- [x] Revoke zeigt Confirm Dialog, dann Toast bei Erfolg
- [ ] Badge auf Tab zeigt Anzahl pending Invites

**Table Filters (Multi-Select):**
- [x] UsersTable: Permission filter with multi-select (requires `system:permissions:read`)
- [x] InvitationsTable: Status filter (pending, accepted, expired, revoked)
- [x] AuditLogsTable: Action filter with multi-select
- [x] AuditLogsTable: User filter with multi-select (requires `system:users:read`)
- [x] AuditLogsTable: Target user shown in details sheet
- [x] SelectFilter component updated to support multi-select mode

**Security:**
- [x] 401 mit force-reauth Header → Toast + Redirect zu /login
- [x] Auth Context cleared bei force-reauth

**Legal Pages:**
- [x] /imprint Route existiert mit Placeholder-Content
- [x] /privacy Route existiert mit Placeholder-Content
- [x] /terms Route existiert mit Placeholder-Content
- [ ] Footer Links funktionieren

### Edge Cases / Error Handling

- Was passiert bei Resend während Cooldown? → Toast mit "Bitte X Minuten warten"
- Was passiert bei Revoke einer bereits revoked Invite? → Error Toast
- Was passiert bei Revoke einer accepted Invite? → Error Toast
- Was passiert wenn Invitations-API fehlschlägt? → Error State in Table
- Was passiert bei force-reauth während Form-Eingabe? → Toast, dann Redirect (Daten verloren)

## 3. API Integration

### Bestehende Endpoints (nutzen)

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/system/invites` | GET | `?cursor&limit&search&status` | `{ data[], pagination }` | useSystemInvites |
| `/api/system/invites/{id}` | GET | - | `{ data }` | useSystemInvite |
| `/api/system/invites/{id}/revoke` | POST | - | `{ data }` | useRevokeInvite |
| `/api/system/invites/{id}/resend` | POST | - | `{ data }` | useResendInvite |

### Permissions

| Action | Permission |
|--------|------------|
| View Invites | `system:users:read` |
| Revoke/Resend | `system:users:update` |

## 4. Komponenten Übersicht

### Neue Komponenten

| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| InvitationsTable | Feature | Table mit Invites, Filter, Actions |
| InvitationsTableColumns | Feature | Column Definitions |
| InviteDetailsSheet | Feature | Sheet mit Invite Details |
| InviteStatusBadge | Feature | Status-spezifisches Badge |
| UsersPageTabs | Feature | Tab-Navigation Users/Invitations |
| LegalPageLayout | Shared | Layout für Legal Pages |

### Bestehende Komponenten nutzen

| Komponente | Woher |
|------------|-------|
| DataTable | @/components/shared/data-table |
| StatusBadge | @/components/shared/status-badge |
| Sheet | @/components/ui/sheet |
| Tabs | @/components/ui/tabs (NEU INSTALLIEREN) |
| ConfirmDialog | @/components/shared/feedback |
| RelativeTime | @/components/shared/relative-time |
| UserAvatar | @/components/shared/user-avatar |
| PageHeader | @/components/shared/layout |

## 5. Files zu erstellen

### Feature Files - Invites

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/users/types/invites.ts` | Invite TypeScript types |
| API | `src/features/users/api/invites-api.ts` | API calls für Invites |
| Hook List | `src/features/users/hooks/use-system-invites.ts` | List + Filters hook |
| Hook Single | `src/features/users/hooks/use-system-invite.ts` | Single invite hook |
| Hook Revoke | `src/features/users/hooks/use-revoke-invite.ts` | Revoke mutation |
| Hook Resend | `src/features/users/hooks/use-resend-invite.ts` | Resend mutation |
| Table | `src/features/users/components/invitations-table.tsx` | Main table |
| Columns | `src/features/users/components/invitations-table-columns.tsx` | Column defs |
| Details | `src/features/users/components/invite-details-sheet.tsx` | Details sheet |
| Tabs | `src/features/users/components/users-page-tabs.tsx` | Tab navigation |

### Route Files - Legal Pages

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Imprint | `src/routes/imprint.tsx` | Impressum page |
| Privacy | `src/routes/privacy.tsx` | Datenschutz page |
| Terms | `src/routes/terms.tsx` | AGB page |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/features/users/components/user-details-sheet.tsx` | lastLoginAt aus userDetails statt user prop |
| `src/routes/invite.tsx` | Auto-logout statt redirect wenn authenticated |
| `src/routes/users.tsx` | Tabs hinzufügen, InvitationsTable einbinden |
| `src/lib/axios.ts` | Force-reauth 401 handling |
| `src/contexts/auth-context.tsx` | Logout function exportieren für force-reauth |
| `src/app/router.tsx` | Legal page routes hinzufügen |
| `src/features/users/index.ts` | Neue exports |
| `src/features/users/hooks/index.ts` | Neue hook exports |
| `src/i18n/locales/en/users.json` | Invite translations |
| `src/i18n/locales/de/users.json` | Invite translations |
| `src/i18n/locales/en/common.json` | Legal page translations |
| `src/i18n/locales/de/common.json` | Legal page translations |

## 7. Neue Dependencies

### Shadcn/UI Komponenten

| Komponente | Command |
|------------|---------|
| Tabs | `npx shadcn@latest add tabs` |

## 8. Implementation Reihenfolge

### Phase 1: Bug Fixes
1. [X] **Bug Fix**: user-details-sheet.tsx - lastLoginAt aus API response nutzen
2. [X] **Bug Fix**: invite.tsx - Auto-logout wenn authenticated

### Phase 2: Force Re-Auth
3. [X] **Axios**: 401 force-reauth detection + handling
4. [X] **Auth Context**: Ensure logout clears everything
5. [X] **Toast**: Force-reauth message

### Phase 3: Invitations Feature
6. [X] **Shadcn**: Tabs component installieren
7. [X] **Types**: Invite types definieren
8. [X] **API**: invites-api.ts erstellen
9. [X] **Hooks**: useSystemInvites, useSystemInvite, useRevokeInvite, useResendInvite
10. [X] **Components**: InvitationsTable, Columns, InviteDetailsSheet
11. [X] **Components**: UsersPageTabs
12. [X] **Route**: users.tsx mit Tabs updaten
13. [X] **i18n**: Translations für Invites

### Phase 4: Legal Pages
14. [X] **Routes**: legal.tsx mit ImprintPage, PrivacyPage, TermsPage erstellen
15. [X] **Router**: Routes registrieren (/imprint, /privacy, /terms)
16. [X] **i18n**: Legal page translations

### Phase 5: Cleanup
17. [ ] **Exports**: Barrel exports updaten
18. [ ] **Standards**: task_standards_frontend.md aktualisieren

## 9. Tests

### Component Tests

| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/users/__tests__/invitations-table.test.tsx` | Table rendering, filtering, actions |
| `src/features/users/__tests__/invite-details-sheet.test.tsx` | Sheet content, actions |
| `src/features/users/__tests__/use-revoke-invite.test.ts` | Mutation, error handling |
| `src/features/users/__tests__/use-resend-invite.test.ts` | Mutation, cooldown error |

## 10. i18n Keys

### users.json (neu)

```json
{
  "tabs": {
    "users": "Users",
    "invitations": "Invitations"
  },
  "invites": {
    "title": "Invitations",
    "empty": {
      "title": "No invitations",
      "description": "Invite users to get started"
    },
    "status": {
      "pending": "Pending",
      "accepted": "Accepted",
      "expired": "Expired",
      "revoked": "Revoked"
    },
    "fields": {
      "email": "Email",
      "name": "Name",
      "status": "Status",
      "expiresAt": "Expires",
      "createdAt": "Invited",
      "invitedBy": "Invited by",
      "acceptedAt": "Accepted at",
      "revokedAt": "Revoked at",
      "resentAt": "Last resent"
    },
    "actions": {
      "resend": "Resend",
      "revoke": "Revoke",
      "viewDetails": "View details"
    },
    "resend": {
      "success": "Invitation resent successfully",
      "cooldown": "Please wait before resending"
    },
    "revoke": {
      "confirm": {
        "title": "Revoke invitation?",
        "description": "This will invalidate the invitation link. The user will no longer be able to accept it."
      },
      "success": "Invitation revoked"
    },
    "details": {
      "title": "Invitation Details",
      "permissions": "Permissions"
    }
  }
}
```

### common.json (hinzugefügt)

```json
{
  "legal": {
    "imprint": "Imprint",
    "privacy": "Privacy Policy",
    "terms": "Terms of Service"
  },
  "forceReauth": {
    "title": "Session expired",
    "description": "Your permissions were changed. Please log in again."
  },
  "filters": {
    "dateRange": "Date Range",
    "selected": "{{count}} selected"
  }
}
```

### auditLogs.json (hinzugefügt)

```json
{
  "fields": {
    "targetUser": "Target User"
  }
}
```

## 11. Nach Completion

- [ ] Alle Tests grün
- [ ] `task_standards_frontend.md` aktualisiert (neue Files, Components)
- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] Beide Bugs gefixt und verifiziert
- [ ] Invitations Tab funktioniert
- [ ] Legal Pages erreichbar
- [ ] Force-reauth getestet

---

**Letzte Änderung:** 2025-12-27
**Status:** Phase 3 Complete - Phase 4 & 5 Pending
**Abhängigkeit:** Backend Task 005 (complete)

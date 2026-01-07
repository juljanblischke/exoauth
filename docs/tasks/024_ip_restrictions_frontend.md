# Task 024: IP Restrictions Frontend

## 1. Übersicht
**Was wird gebaut?**
Frontend für das IP Restrictions Management (Whitelist/Blacklist) aus Task 023. Admin UI zum Anzeigen, Erstellen und Löschen von IP-Einschränkungen.

**Warum?**
- Task 023 hat das Backend fertiggestellt (API, Services, Tests)
- Admins brauchen eine UI um IP Whitelist/Blacklist zu verwalten
- Globale Fehlerbehandlung für 429 (Rate Limit) und 403 (IP Blacklisted)

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich alle IP-Restrictions in einer Tabelle sehen
- Als Admin möchte ich nach IP-Adresse oder Grund suchen können
- Als Admin möchte ich nach Typ (Whitelist/Blacklist) und Quelle (Manual/Auto) filtern
- Als Admin möchte ich neue IP-Restrictions (Whitelist/Blacklist) erstellen
- Als Admin möchte ich IP-Restrictions löschen können
- Als User möchte ich bei Rate Limiting einen Toast sehen statt einer kryptischen Fehlermeldung

### UI/UX Beschreibung
- Tabelle mit IP-Restrictions (wie Audit Logs)
- Toolbar mit Search, Filter (Type, Source), Create Button
- Create Modal mit Form (IP Address, Type, Reason, Expiration)
- Delete mit Confirm Dialog
- Details Sheet bei Klick auf Row

### Akzeptanzkriterien
- [ ] Tabelle zeigt alle IP-Restrictions mit Pagination
- [ ] Filter nach Type (Whitelist/Blacklist) funktioniert
- [ ] Filter nach Source (Manual/Auto) funktioniert
- [ ] Search nach IP/Reason funktioniert
- [ ] Create Modal mit Validation
- [ ] Delete mit Confirmation
- [ ] Details Sheet zeigt alle Infos
- [ ] Global Toast bei 429 Rate Limit
- [ ] Global Toast bei 403 IP Blacklisted
- [ ] i18n EN + DE komplett

### Edge Cases / Error Handling
- Was passiert bei ungültiger IP/CIDR? → Validation Error im Form
- Was passiert bei Duplicate? → API Error anzeigen
- Was passiert bei 429? → Global Toast "Too many requests"
- Was passiert bei 403 IP_BLACKLISTED? → Global Toast "IP blocked"

## 3. API Integration

| Endpoint | Method | Request | Response | Hook Name |
|----------|--------|---------|----------|-----------|
| `/api/system/ip-restrictions` | GET | Query params | `CursorPagedList<IpRestrictionDto>` | `useIpRestrictions` |
| `/api/system/ip-restrictions` | POST | `CreateIpRestrictionRequest` | `IpRestrictionDto` | `useCreateIpRestriction` |
| `/api/system/ip-restrictions/{id}` | DELETE | - | 204 | `useDeleteIpRestriction` |

### Query Parameters (GET)
| Param | Type | Description |
|-------|------|-------------|
| `cursor` | string? | Pagination cursor |
| `limit` | int | Items per page (default 20) |
| `type` | enum? | `whitelist` or `blacklist` |
| `source` | enum? | `manual` or `auto` |
| `includeExpired` | bool | Include expired (default false) |
| `search` | string? | Search IP or reason |
| `sort` | string | Field:direction (default `createdAt:desc`) |

### Request/Response Types
```typescript
// Enums
type IpRestrictionType = 'whitelist' | 'blacklist'
type IpRestrictionSource = 'manual' | 'auto'

// Response
interface IpRestriction {
  id: string
  ipAddress: string
  type: IpRestrictionType
  reason: string
  source: IpRestrictionSource
  expiresAt: string | null
  createdAt: string
  createdByUserId: string | null
  createdByUserEmail: string | null
}

// Request
interface CreateIpRestrictionRequest {
  ipAddress: string
  type: IpRestrictionType
  reason: string
  expiresAt?: string | null
}
```

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| IpRestrictionsTable | Feature | Main table with toolbar |
| IpRestrictionsTableColumns | Feature | Column definitions |
| IpRestrictionDetailsSheet | Feature | Details slide-out |
| CreateIpRestrictionModal | Feature | Create form modal |
| IpRestrictionTypeBadge | Feature | Whitelist/Blacklist badge |
| IpRestrictionSourceBadge | Feature | Manual/Auto badge |

### Bestehende Komponenten nutzen
| Komponente | Woher |
|------------|-------|
| DataTable | @/components/shared/data-table |
| DataTableToolbar | @/components/shared/data-table |
| DataTableFilters | @/components/shared/data-table |
| DataTablePagination | @/components/shared/data-table |
| PageHeader | @/components/shared/layout |
| FormModal | @/components/shared/form |
| ConfirmDialog | @/components/shared/feedback |
| Badge | @/components/ui/badge |
| Button | @/components/ui/button |
| Sheet | @/components/ui/sheet |

## 5. Files zu erstellen

### Feature Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Types | `src/features/ip-restrictions/types/index.ts` | TypeScript types |
| API | `src/features/ip-restrictions/api/ip-restrictions-api.ts` | API calls |
| Hook List | `src/features/ip-restrictions/hooks/use-ip-restrictions.ts` | List query |
| Hook Create | `src/features/ip-restrictions/hooks/use-create-ip-restriction.ts` | Create mutation |
| Hook Delete | `src/features/ip-restrictions/hooks/use-delete-ip-restriction.ts` | Delete mutation |
| Hooks Index | `src/features/ip-restrictions/hooks/index.ts` | Barrel export |
| Table | `src/features/ip-restrictions/components/ip-restrictions-table.tsx` | Main table |
| Columns | `src/features/ip-restrictions/components/ip-restrictions-table-columns.tsx` | Column defs |
| Details | `src/features/ip-restrictions/components/ip-restriction-details-sheet.tsx` | Details sheet |
| Create Modal | `src/features/ip-restrictions/components/create-ip-restriction-modal.tsx` | Create form |
| Type Badge | `src/features/ip-restrictions/components/ip-restriction-type-badge.tsx` | Type badge |
| Source Badge | `src/features/ip-restrictions/components/ip-restriction-source-badge.tsx` | Source badge |
| Components Index | `src/features/ip-restrictions/components/index.ts` | Barrel export |
| Feature Index | `src/features/ip-restrictions/index.ts` | Main export |

### Route Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Route | `src/routes/ip-restrictions.tsx` | Page component |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/app/router.tsx` | Route `/ip-restrictions` hinzufügen |
| `src/config/navigation.ts` | Nav item unter System section |
| `src/lib/axios.ts` | Global 429 + 403 toast handling |
| `src/i18n/locales/en/navigation.json` | Nav label |
| `src/i18n/locales/de/navigation.json` | Nav label |
| `src/i18n/locales/en/errors.json` | IP_BLACKLISTED error |
| `src/i18n/locales/de/errors.json` | IP_BLACKLISTED error |

## 7. Neue Dependencies

### NPM Packages
Keine neuen Packages erforderlich.

### Shadcn/UI Komponenten
Alle benötigten Komponenten bereits installiert.

## 8. Implementation Reihenfolge

1. [ ] **Types**: TypeScript interfaces definieren
2. [ ] **API**: API client functions erstellen
3. [ ] **Hooks**: React Query hooks erstellen
4. [ ] **Components**: UI Komponenten bauen
   - [ ] Type Badge
   - [ ] Source Badge
   - [ ] Table Columns
   - [ ] Details Sheet
   - [ ] Create Modal
   - [ ] Main Table
5. [ ] **Route**: Page erstellen
6. [ ] **Router**: Route registrieren
7. [ ] **Navigation**: Sidebar entry hinzufügen
8. [ ] **Axios**: Global error handling (429, 403)
9. [ ] **i18n**: Translations hinzufügen (EN + DE!)
10. [ ] **Memory updaten**: frontend_reference.md aktualisieren

## 9. i18n Keys

### Neuer Namespace: ipRestrictions.json

#### English
```json
{
  "title": "IP Restrictions",
  "subtitle": "Manage IP whitelist and blacklist",
  "table": {
    "ipAddress": "IP Address",
    "type": "Type",
    "reason": "Reason",
    "source": "Source",
    "expiresAt": "Expires",
    "createdAt": "Created",
    "createdBy": "Created By",
    "actions": "Actions",
    "noResults": "No IP restrictions found",
    "noResultsDescription": "Add your first IP restriction to get started."
  },
  "type": {
    "whitelist": "Whitelist",
    "blacklist": "Blacklist"
  },
  "source": {
    "manual": "Manual",
    "auto": "Automatic"
  },
  "filters": {
    "search": "Search IP or reason...",
    "type": "Type",
    "source": "Source",
    "allTypes": "All types",
    "allSources": "All sources"
  },
  "create": {
    "title": "Add IP Restriction",
    "description": "Add an IP address to the whitelist or blacklist.",
    "ipAddress": "IP Address",
    "ipAddressPlaceholder": "e.g., 192.168.1.1 or 10.0.0.0/8",
    "ipAddressDescription": "Single IP or CIDR notation",
    "type": "Type",
    "typePlaceholder": "Select type",
    "reason": "Reason",
    "reasonPlaceholder": "Why is this IP being added?",
    "expiresAt": "Expires At",
    "expiresAtDescription": "Leave empty for permanent restriction",
    "permanent": "Permanent",
    "submit": "Add Restriction",
    "success": "IP restriction created successfully"
  },
  "delete": {
    "title": "Delete IP Restriction",
    "description": "Are you sure you want to delete this IP restriction? This action cannot be undone.",
    "confirm": "Delete",
    "success": "IP restriction deleted successfully"
  },
  "details": {
    "title": "IP Restriction Details",
    "ipAddress": "IP Address",
    "type": "Type",
    "reason": "Reason",
    "source": "Source",
    "expiresAt": "Expires At",
    "createdAt": "Created At",
    "createdBy": "Created By",
    "never": "Never",
    "system": "System"
  },
  "expired": "Expired",
  "never": "Never"
}
```

#### German
```json
{
  "title": "IP-Einschränkungen",
  "subtitle": "IP-Whitelist und Blacklist verwalten",
  "table": {
    "ipAddress": "IP-Adresse",
    "type": "Typ",
    "reason": "Grund",
    "source": "Quelle",
    "expiresAt": "Läuft ab",
    "createdAt": "Erstellt",
    "createdBy": "Erstellt von",
    "actions": "Aktionen",
    "noResults": "Keine IP-Einschränkungen gefunden",
    "noResultsDescription": "Fügen Sie Ihre erste IP-Einschränkung hinzu."
  },
  "type": {
    "whitelist": "Whitelist",
    "blacklist": "Blacklist"
  },
  "source": {
    "manual": "Manuell",
    "auto": "Automatisch"
  },
  "filters": {
    "search": "IP oder Grund suchen...",
    "type": "Typ",
    "source": "Quelle",
    "allTypes": "Alle Typen",
    "allSources": "Alle Quellen"
  },
  "create": {
    "title": "IP-Einschränkung hinzufügen",
    "description": "IP-Adresse zur Whitelist oder Blacklist hinzufügen.",
    "ipAddress": "IP-Adresse",
    "ipAddressPlaceholder": "z.B. 192.168.1.1 oder 10.0.0.0/8",
    "ipAddressDescription": "Einzelne IP oder CIDR-Notation",
    "type": "Typ",
    "typePlaceholder": "Typ wählen",
    "reason": "Grund",
    "reasonPlaceholder": "Warum wird diese IP hinzugefügt?",
    "expiresAt": "Läuft ab am",
    "expiresAtDescription": "Leer lassen für permanente Einschränkung",
    "permanent": "Permanent",
    "submit": "Einschränkung hinzufügen",
    "success": "IP-Einschränkung erfolgreich erstellt"
  },
  "delete": {
    "title": "IP-Einschränkung löschen",
    "description": "Möchten Sie diese IP-Einschränkung wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.",
    "confirm": "Löschen",
    "success": "IP-Einschränkung erfolgreich gelöscht"
  },
  "details": {
    "title": "IP-Einschränkung Details",
    "ipAddress": "IP-Adresse",
    "type": "Typ",
    "reason": "Grund",
    "source": "Quelle",
    "expiresAt": "Läuft ab am",
    "createdAt": "Erstellt am",
    "createdBy": "Erstellt von",
    "never": "Nie",
    "system": "System"
  },
  "expired": "Abgelaufen",
  "never": "Nie"
}
```

### Navigation Keys (add to existing)

#### English (navigation.json)
```json
{
  "items": {
    "ipRestrictions": "IP Restrictions"
  }
}
```

#### German (navigation.json)
```json
{
  "items": {
    "ipRestrictions": "IP-Einschränkungen"
  }
}
```

### Error Keys (add to existing errors.json)

#### English
```json
{
  "codes": {
    "IP_BLACKLISTED": "Your IP address has been blocked.",
    "IP_RESTRICTION_NOT_FOUND": "IP restriction not found.",
    "IP_RESTRICTION_INVALID_CIDR": "Invalid IP address or CIDR notation.",
    "IP_RESTRICTION_DUPLICATE": "This IP address is already in the list."
  }
}
```

#### German
```json
{
  "codes": {
    "IP_BLACKLISTED": "Ihre IP-Adresse wurde blockiert.",
    "IP_RESTRICTION_NOT_FOUND": "IP-Einschränkung nicht gefunden.",
    "IP_RESTRICTION_INVALID_CIDR": "Ungültige IP-Adresse oder CIDR-Notation.",
    "IP_RESTRICTION_DUPLICATE": "Diese IP-Adresse ist bereits in der Liste."
  }
}
```

## 10. Nach Completion

- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] `frontend_reference.md` Memory aktualisiert
- [ ] `i18n_translations.md` Memory aktualisiert
- [ ] Navigation zeigt IP Restrictions unter System

## 11. Letzte Änderung

- **Datum:** 2026-01-07
- **Status:** Planning
- **Nächster Schritt:** Types erstellen

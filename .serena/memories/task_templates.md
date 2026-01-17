# Task Templates - ExoAuth

> Use these templates when creating new task files in `docs/tasks/`.

---

## Repository Classification

Every task MUST be marked with its repository at the top:

| Label | Repository | Description |
|-------|------------|-------------|
| `🌐 COMMUNITY` | `exoauth/exoauth` (public) | Core auth features, SystemUser management |
| `🔒 PRO` | `exoauth-pro/exoauth-pro` (private) | Business features, multi-project, customers |

**What goes where?**

| Feature | Repository | Reason |
|---------|------------|--------|
| Auth (MFA, Passkeys, Social Login) | 🌐 COMMUNITY | Core auth for everyone |
| SystemUsers & Admin | 🌐 COMMUNITY | Platform management |
| Email System | 🌐 COMMUNITY | Core functionality |
| Rate Limiting, IP Restrictions | 🌐 COMMUNITY | Security features |
| Audit Logging | 🌐 COMMUNITY | Compliance |
| Customers & Customer Auth | 🔒 PRO | Business layer |
| Organizations | 🔒 PRO | Business layer |
| Multi-Project | 🔒 PRO | Monetization feature |
| Plans & Subscriptions | 🔒 PRO | Billing |
| License Key Validation | 🔒 PRO | Monetization |

### Frontend-spezifische Regeln

| Wenn du baust... | Repository | Ordner |
|------------------|------------|--------|
| SystemUser Feature | 🌐 COMMUNITY | `frontend/src/features/` |
| Customer Feature | 🔒 PRO | `frontend-pro/src/features/` |
| Organization Feature | 🔒 PRO | `frontend-pro/src/features/` |
| Shared UI Component | 🌐 COMMUNITY | `frontend/src/components/shared/` |

**NIEMALS:**
- Pro-Features in `frontend/` (Community) committen
- `if (edition === 'pro')` Runtime-Checks verwenden
- Pro-spezifische Navigation in Community hinzufügen

**PRO Feature Warning Banner (copy this to PRO tasks):**
```markdown
> ⚠️ **PRO FEATURE** - This task is implemented in the private `exoauth-pro` repository.
> 
> **Setup:** Ensure the community submodule is initialized:
> ```bash
> git submodule update --init --recursive
> ```
```

---

## Task File Naming

```
docs/tasks/XXX_feature_name.md
```

Examples:
- `015_role_management.md`
- `016_oauth_providers.md`

---

## CRITICAL: Task File Maintenance

**During implementation, ALWAYS keep the task file updated:**

1. **New files created?** → Add to "Files zu erstellen" section
2. **Existing files changed?** → Add to "Files zu ändern" section
3. **New packages installed?** → Add to "Neue Packages" section
4. **Step completed?** → Check the checkbox `[x]`
5. **Tests written?** → Document count (e.g., "15 Tests ✅")

**After completion:**
- Update the relevant memory file (backend_reference.md or frontend_reference.md)
- Add new error codes to backend_reference.md
- Add new i18n keys to i18n_translations.md

---

## Backend Task Template

```markdown
# Task: [Feature Name]

> **Repository:** 🌐 COMMUNITY | 🔒 PRO *(delete one)*
> 
> ⚠️ **PRO FEATURE** *(only if PRO - delete this block for COMMUNITY)*
> This task is implemented in the private `exoauth-pro` repository.
> Setup: `git submodule update --init --recursive`

## 1. Übersicht
**Was wird gebaut?**
[Kurze Beschreibung]

**Warum?**
[Business Grund / User Need]

## 2. User Experience / Anforderungen

### User Stories
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]
- Als [Rolle] möchte ich [Aktion] damit [Nutzen]

### Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2
- [ ] Kriterium 3

### Edge Cases / Error Handling
- Was passiert wenn...?
- Was passiert wenn...?

## 3. API Endpoints

| Method | Route | Request Body | Response | Beschreibung |
|--------|-------|--------------|----------|--------------|
| POST | /api/... | `{ ... }` | `{ ... }` | ... |
| GET | /api/... | - | `{ ... }` | ... |

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `ERROR_CODE_NAME` | 4xx | Beschreibung des Fehlers |

> ⚠️ **Nach Completion:** Diese Codes zu `backend_reference.md` Memory hinzufügen!

## 5. Datenbank Änderungen

### Neue Entities
| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| ... | ... | ... |

### Migrations
- [ ] Migration Name: `Add{EntityName}`

## 6. Files zu erstellen

### Domain Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Domain/Entities/...` | ... |

### Application Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Command | `src/ExoAuth.Application/Features/.../Commands/.../...Command.cs` | ... |
| Handler | `src/ExoAuth.Application/Features/.../Commands/.../...Handler.cs` | ... |
| Validator | `src/ExoAuth.Application/Features/.../Commands/.../...Validator.cs` | ... |

### Infrastructure Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ... | `src/ExoAuth.Infrastructure/...` | ... |

### API Layer
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Controller | `src/ExoAuth.Api/Controllers/...Controller.cs` | ... |

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/.../AppDbContext.cs` | DbSet hinzufügen |
| `src/.../DependencyInjection.cs` | Service registrieren |

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| ... | ... | ExoAuth.XXX | ... |

## 9. Implementation Reihenfolge

1. [ ] **Domain**: Entity erstellen
2. [ ] **Infrastructure**: Configuration + DbContext + Migration
3. [ ] **Application**: Commands/Queries + Handlers + Validators
4. [ ] **API**: Controller + Endpoints
5. [ ] **Tests**: Unit Tests schreiben
6. [ ] **Task File updaten**: Diese Taskfile aktualisieren
7. [ ] **Memory updaten**: backend_reference.md aktualisieren

## 10. Tests

### Unit Tests
| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `tests/ExoAuth.UnitTests/Features/.../...Tests.cs` | ... | ... |

## 11. Nach Completion

- [ ] Alle Unit Tests grün
- [ ] `backend_reference.md` Memory aktualisiert (File Tree, Packages, Error Codes)
- [ ] Code reviewed

## 12. Letzte Änderung

- **Datum:** YYYY-MM-DD
- **Status:** In Progress / Complete
- **Nächster Schritt:** ...
```

---

## Frontend Task Template

```markdown
# Task: [Feature Name]

> **Repository:** 🌐 COMMUNITY | 🔒 PRO *(delete one)*
> 
> ⚠️ **PRO FEATURE** *(only if PRO - delete this block for COMMUNITY)*
> This task is implemented in the private `exoauth-pro` repository.
> Setup: `git submodule update --init --recursive`

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
| DataTable | @/components/shared/data-table |

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

1. [ ] **Types**: TypeScript interfaces definieren
2. [ ] **API**: API client functions erstellen
3. [ ] **Hooks**: React Query hooks erstellen
4. [ ] **Components**: UI Komponenten bauen
5. [ ] **Route**: Page/Route erstellen
6. [ ] **i18n**: Translations hinzufügen (EN + DE!)
7. [ ] **Tests**: Component + Hook tests
8. [ ] **Task updaten**: diesen Task aktualisieren
9. [ ] **Memory updaten**: frontend_reference.md aktualisieren

## 9. Tests

### Component Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/{feature}/__tests__/{name}.test.tsx` | ... |

## 10. i18n Keys

### English (auth.json / users.json / etc.)
```json
{
  "feature": {
    "title": "...",
    "description": "..."
  }
}
```

### German
```json
{
  "feature": {
    "title": "...",
    "description": "..."
  }
}
```

## 11. Nach Completion

- [ ] Alle Tests grün
- [ ] `frontend_reference.md` Memory aktualisiert (neue Files, Components)
- [ ] `i18n_translations.md` Memory aktualisiert (neue Keys)
- [ ] TypeScript keine Errors
- [ ] Lint passed

## 12. Letzte Änderung

- **Datum:** YYYY-MM-DD
- **Status:** In Progress / Complete
- **Nächster Schritt:** ...
```

---

## API Routes by Repository

| Route Pattern | Repository | Description |
|---------------|------------|-------------|
| `/api/system/auth/*` | 🌐 COMMUNITY | SystemUser authentication |
| `/api/system/users/*` | 🌐 COMMUNITY | SystemUser management |
| `/api/system/*` | 🌐 COMMUNITY | All admin/system endpoints |
| `/api/auth/*` | 🔒 PRO | Customer authentication |
| `/api/organizations/*` | 🔒 PRO | Organization management |
| `/api/projects/*` | 🔒 PRO | Multi-project management |
| `/api/plans/*` | 🔒 PRO | Subscription/billing |

---

## Quick Reference: Implementation Order

### Backend
```
Domain → Infrastructure (Config + DbContext + Migration) → Application (Commands/Queries + Handlers) → API → Tests → Update Memory
```

### Frontend
```
Types → API → Hooks → Components → Route → i18n (EN + DE!) → Tests → Update Memory
```

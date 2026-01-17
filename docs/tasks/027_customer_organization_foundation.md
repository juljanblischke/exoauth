# Task 027: Customer & Organization Foundation

## 1. Übersicht

**Was wird gebaut?**
Die Grundlage für das Multi-Tenant System: Customer-Accounts (zahlende Kunden) und Organizations (Firmen/Teams die Projects besitzen werden).

**Warum?**
- Trennung zwischen Platform Admins (SystemUsers) und Kunden (Customers)
- Customers können sich selbst registrieren und Organizations erstellen
- Basis für das eigentliche ExoAuth Produkt (Multi-Project Auth Platform)

**Kontext:**
- SystemUsers = ExoAuth Platform Admins (wir)
- Customers = Zahlende Kunden die ExoAuth nutzen
- Organizations = Firmen/Teams der Customers (werden später Projects haben)

## 2. User Stories

### Customer Registration & Auth
- Als **neuer Kunde** möchte ich mich selbst registrieren können, damit ich ExoAuth nutzen kann
- Als **Customer** möchte ich mich einloggen können, damit ich mein Dashboard sehe
- Als **Customer** möchte ich mein Passwort zurücksetzen können, falls ich es vergesse
- Als **Customer** möchte ich meine Profil-Einstellungen ändern können

### Organizations
- Als **Customer** möchte ich eine Organization erstellen können, damit ich meine Projekte organisieren kann
- Als **Customer** möchte ich mehrere Organizations haben können (z.B. für verschiedene Firmen)
- Als **Organization Owner** möchte ich andere Customers einladen können (Member)
- Als **Organization Owner** möchte ich Members entfernen können
- Als **Organization Owner** möchte ich die Organization umbenennen/löschen können

## 3. Architektur-Entscheidungen

### Trennung SystemUser vs Customer
```
SystemUsers (existiert)          Customers (neu)
├── /api/system/auth/*           ├── /api/auth/*
├── /api/system/*               ├── /api/organizations/*
├── Platform Admin Dashboard     ├── Customer Dashboard
└── Verwalten ExoAuth selbst    └── Nutzen ExoAuth für ihre Apps
```

### Organization Rollen
- **Owner**: Volle Kontrolle (erstellt die Org, kann alles)
- **Member**: Kann in der Org arbeiten, aber nicht löschen/verwalten

### Multi-Organization Support
- Ein Customer kann Owner von mehreren Organizations sein
- Ein Customer kann Member in mehreren Organizations sein
- Wie bei GitHub: Ein User, viele Orgs

## 4. API Endpoints

### Route Änderungen (bestehend)

| Vorher | Nachher | Notiz |
|--------|---------|-------|
| `POST /api/auth/login` | `POST /api/system/auth/login` | SystemUser Auth verschoben |
| `POST /api/auth/register` | `POST /api/system/auth/register` | SystemUser Auth verschoben |
| ... alle `/api/auth/*` | ... alle `/api/system/auth/*` | Kompletter Controller (konsistent mit anderen system/* Routes) |

### Neue Customer Auth Endpoints

| Method | Route | Request | Response | Beschreibung |
|--------|-------|---------|----------|--------------|
| POST | `/api/auth/register` | `{ email, password, firstName, lastName }` | `AuthResponse` | Customer Registrierung |
| POST | `/api/auth/login` | `{ email, password, deviceId?, deviceFingerprint? }` | `AuthResponse` | Customer Login |
| POST | `/api/auth/logout` | `{ refreshToken }` | `{ success }` | Logout |
| POST | `/api/auth/refresh` | `{ refreshToken }` | `AuthResponse` | Token Refresh |
| POST | `/api/auth/forgot-password` | `{ email }` | `{ success }` | Password Reset anfordern |
| POST | `/api/auth/reset-password` | `{ token, newPassword }` | `{ success }` | Password Reset durchführen |
| GET | `/api/auth/me` | - | `CustomerDto` | Aktueller Customer |
| PUT | `/api/auth/preferences` | `{ language, theme }` | `CustomerDto` | Einstellungen ändern |

### Neue Organization Endpoints

| Method | Route | Request | Response | Beschreibung |
|--------|-------|---------|----------|--------------|
| POST | `/api/organizations` | `{ name, slug? }` | `OrganizationDto` | Org erstellen |
| GET | `/api/organizations` | - | `List<OrganizationDto>` | Meine Orgs |
| GET | `/api/organizations/{id}` | - | `OrganizationDto` | Org Details |
| PUT | `/api/organizations/{id}` | `{ name }` | `OrganizationDto` | Org bearbeiten |
| DELETE | `/api/organizations/{id}` | - | `{ success }` | Org löschen |
| GET | `/api/organizations/{id}/members` | - | `List<OrgMemberDto>` | Members auflisten |
| POST | `/api/organizations/{id}/members` | `{ email }` | `OrgMemberDto` | Member einladen |
| DELETE | `/api/organizations/{id}/members/{memberId}` | - | `{ success }` | Member entfernen |
| PUT | `/api/organizations/{id}/members/{memberId}` | `{ role }` | `OrgMemberDto` | Rolle ändern |
| POST | `/api/organizations/{id}/leave` | - | `{ success }` | Org verlassen (als Member) |

## 5. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `CUSTOMER_NOT_FOUND` | 404 | Customer nicht gefunden |
| `CUSTOMER_EMAIL_EXISTS` | 409 | Email bereits registriert |
| `CUSTOMER_INVALID_CREDENTIALS` | 401 | Falsche Email/Passwort |
| `CUSTOMER_INACTIVE` | 401 | Customer deaktiviert |
| `ORG_NOT_FOUND` | 404 | Organization nicht gefunden |
| `ORG_NAME_EXISTS` | 409 | Org-Name bereits vergeben (für diesen Customer) |
| `ORG_SLUG_EXISTS` | 409 | Org-Slug bereits vergeben |
| `ORG_NOT_OWNER` | 403 | Nur Owner kann das |
| `ORG_NOT_MEMBER` | 403 | Kein Zugriff auf diese Org |
| `ORG_CANNOT_LEAVE_AS_OWNER` | 400 | Owner kann nicht verlassen (muss löschen oder übertragen) |
| `ORG_MEMBER_NOT_FOUND` | 404 | Member nicht in Org |
| `ORG_MEMBER_ALREADY_EXISTS` | 409 | Bereits Member |
| `ORG_CANNOT_REMOVE_OWNER` | 400 | Owner kann nicht entfernt werden |

> **Nach Completion:** Diese Codes zu `backend_reference.md` hinzufügen!

## 6. Datenbank Änderungen

### Neue Entities

#### Customer
```csharp
public sealed class Customer : BaseEntity
{
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string PreferredLanguage { get; private set; }
    public string? Theme { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Navigation
    public ICollection<OrganizationMember> OrganizationMemberships { get; private set; }
    public ICollection<CustomerRefreshToken> RefreshTokens { get; private set; }
    public ICollection<CustomerPasswordResetToken> PasswordResetTokens { get; private set; }
}
```

#### Organization
```csharp
public sealed class Organization : BaseEntity
{
    public string Name { get; private set; }
    public string Slug { get; private set; }  // URL-freundlich, unique
    public Guid CreatedByCustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Navigation
    public Customer CreatedBy { get; private set; }
    public ICollection<OrganizationMember> Members { get; private set; }
}
```

#### OrganizationMember
```csharp
public sealed class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrganizationRole Role { get; private set; }  // Owner, Member
    public DateTime JoinedAt { get; private set; }
    public Guid? InvitedByCustomerId { get; private set; }
    
    // Navigation
    public Organization Organization { get; private set; }
    public Customer Customer { get; private set; }
    public Customer? InvitedBy { get; private set; }
}
```

#### CustomerRefreshToken
```csharp
public sealed class CustomerRefreshToken : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Token { get; private set; }
    public string? DeviceId { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    
    // Navigation
    public Customer Customer { get; private set; }
}
```

#### CustomerPasswordResetToken
```csharp
public sealed class CustomerPasswordResetToken : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    
    // Navigation
    public Customer Customer { get; private set; }
}
```

### Neues Enum

```csharp
public enum OrganizationRole
{
    Owner = 1,
    Member = 2
}
```

### Migration
- `AddCustomerAndOrganizationTables`

## 7. Files zu erstellen

### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Customer.cs | `src/ExoAuth.Domain/Entities/` | Customer Entity |
| Organization.cs | `src/ExoAuth.Domain/Entities/` | Organization Entity |
| OrganizationMember.cs | `src/ExoAuth.Domain/Entities/` | Membership Entity |
| CustomerRefreshToken.cs | `src/ExoAuth.Domain/Entities/` | Customer Refresh Tokens |
| CustomerPasswordResetToken.cs | `src/ExoAuth.Domain/Entities/` | Customer PW Reset Tokens |
| OrganizationRole.cs | `src/ExoAuth.Domain/Enums/` | Owner/Member Enum |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CustomerConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/` | EF Config |
| OrganizationConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/` | EF Config |
| OrganizationMemberConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/` | EF Config |
| CustomerRefreshTokenConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/` | EF Config |
| CustomerPasswordResetTokenConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/` | EF Config |
| Migration | `src/ExoAuth.Infrastructure/Persistence/Migrations/` | AddCustomerAndOrganizationTables |

### Application Layer - Customer Auth

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CustomerRegisterCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Register/` | Command |
| CustomerRegisterHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Register/` | Handler |
| CustomerRegisterValidator.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Register/` | Validator |
| CustomerLoginCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Login/` | Command |
| CustomerLoginHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Login/` | Handler |
| CustomerLoginValidator.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Login/` | Validator |
| CustomerLogoutCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Logout/` | Command |
| CustomerLogoutHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/Logout/` | Handler |
| CustomerRefreshTokenCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/RefreshToken/` | Command |
| CustomerRefreshTokenHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/RefreshToken/` | Handler |
| CustomerForgotPasswordCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/ForgotPassword/` | Command |
| CustomerForgotPasswordHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/ForgotPassword/` | Handler |
| CustomerResetPasswordCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/ResetPassword/` | Command |
| CustomerResetPasswordHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/ResetPassword/` | Handler |
| CustomerUpdatePreferencesCommand.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/UpdatePreferences/` | Command |
| CustomerUpdatePreferencesHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Commands/UpdatePreferences/` | Handler |
| GetCurrentCustomerQuery.cs | `src/ExoAuth.Application/Features/CustomerAuth/Queries/GetCurrentCustomer/` | Query |
| GetCurrentCustomerHandler.cs | `src/ExoAuth.Application/Features/CustomerAuth/Queries/GetCurrentCustomer/` | Handler |
| CustomerAuthResponse.cs | `src/ExoAuth.Application/Features/CustomerAuth/Models/` | Response DTO |
| CustomerDto.cs | `src/ExoAuth.Application/Features/CustomerAuth/Models/` | Customer DTO |

### Application Layer - Organizations

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CreateOrganizationCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/CreateOrganization/` | Command |
| CreateOrganizationHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/CreateOrganization/` | Handler |
| UpdateOrganizationCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/UpdateOrganization/` | Command |
| UpdateOrganizationHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/UpdateOrganization/` | Handler |
| DeleteOrganizationCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/DeleteOrganization/` | Command |
| DeleteOrganizationHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/DeleteOrganization/` | Handler |
| AddOrganizationMemberCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/AddMember/` | Command |
| AddOrganizationMemberHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/AddMember/` | Handler |
| RemoveOrganizationMemberCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/RemoveMember/` | Command |
| RemoveOrganizationMemberHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/RemoveMember/` | Handler |
| UpdateMemberRoleCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/UpdateMemberRole/` | Command |
| UpdateMemberRoleHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/UpdateMemberRole/` | Handler |
| LeaveOrganizationCommand.cs | `src/ExoAuth.Application/Features/Organizations/Commands/LeaveOrganization/` | Command |
| LeaveOrganizationHandler.cs | `src/ExoAuth.Application/Features/Organizations/Commands/LeaveOrganization/` | Handler |
| GetOrganizationsQuery.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetOrganizations/` | Query |
| GetOrganizationsHandler.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetOrganizations/` | Handler |
| GetOrganizationQuery.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetOrganization/` | Query |
| GetOrganizationHandler.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetOrganization/` | Handler |
| GetOrganizationMembersQuery.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetMembers/` | Query |
| GetOrganizationMembersHandler.cs | `src/ExoAuth.Application/Features/Organizations/Queries/GetMembers/` | Handler |
| OrganizationDto.cs | `src/ExoAuth.Application/Features/Organizations/Models/` | Org DTO |
| OrganizationMemberDto.cs | `src/ExoAuth.Application/Features/Organizations/Models/` | Member DTO |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| CustomerAuthController.cs | `src/ExoAuth.Api/Controllers/` | Customer Auth Endpoints |
| OrganizationsController.cs | `src/ExoAuth.Api/Controllers/` | Organization Endpoints |

### Services (Interfaces)

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| ICurrentCustomerService.cs | `src/ExoAuth.Application/Common/Interfaces/` | Aktueller Customer |
| ICustomerTokenService.cs | `src/ExoAuth.Application/Common/Interfaces/` | Customer JWT Service |
| CurrentCustomerService.cs | `src/ExoAuth.Api/Services/` | Implementation |

## 8. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `AuthController.cs` | Route ändern: `[Route("api/auth")]` → `[Route("api/admin/auth")]` |
| `AppDbContext.cs` | DbSets hinzufügen für neue Entities |
| `IAppDbContext.cs` | Interface erweitern |
| `ErrorCodes.cs` | Neue Error Codes hinzufügen |
| `DependencyInjection.cs` | Neue Services registrieren |
| Frontend API URLs | `/api/auth/*` → `/api/system/auth/*` |

## 9. Neue Packages

Keine neuen Packages erforderlich.

## 10. Implementation Reihenfolge

### Phase 1: Route Migration
- [ ] AuthController Route ändern (`api/auth` → `api/system/auth`)
- [ ] Frontend URLs anpassen
- [ ] Testen dass alles noch funktioniert

### Phase 2: Domain Layer
- [ ] Customer Entity erstellen
- [ ] Organization Entity erstellen
- [ ] OrganizationMember Entity erstellen
- [ ] CustomerRefreshToken Entity erstellen
- [ ] CustomerPasswordResetToken Entity erstellen
- [ ] OrganizationRole Enum erstellen

### Phase 3: Infrastructure Layer
- [ ] EF Configurations erstellen
- [ ] AppDbContext erweitern
- [ ] Migration erstellen und ausführen

### Phase 4: Application Layer - Customer Auth
- [ ] ICurrentCustomerService Interface
- [ ] ICustomerTokenService Interface
- [ ] CustomerRegister Command/Handler/Validator
- [ ] CustomerLogin Command/Handler/Validator
- [ ] CustomerLogout Command/Handler
- [ ] CustomerRefreshToken Command/Handler
- [ ] CustomerForgotPassword Command/Handler
- [ ] CustomerResetPassword Command/Handler
- [ ] CustomerUpdatePreferences Command/Handler
- [ ] GetCurrentCustomer Query/Handler
- [ ] DTOs (CustomerAuthResponse, CustomerDto)

### Phase 5: Application Layer - Organizations
- [ ] CreateOrganization Command/Handler/Validator
- [ ] UpdateOrganization Command/Handler/Validator
- [ ] DeleteOrganization Command/Handler
- [ ] AddOrganizationMember Command/Handler
- [ ] RemoveOrganizationMember Command/Handler
- [ ] UpdateMemberRole Command/Handler
- [ ] LeaveOrganization Command/Handler
- [ ] GetOrganizations Query/Handler
- [ ] GetOrganization Query/Handler
- [ ] GetOrganizationMembers Query/Handler
- [ ] DTOs (OrganizationDto, OrganizationMemberDto)

### Phase 6: API Layer
- [ ] CustomerAuthController erstellen
- [ ] OrganizationsController erstellen
- [ ] CurrentCustomerService implementieren

### Phase 7: Tests
- [ ] Customer Auth Tests
- [ ] Organization Tests

### Phase 8: Abschluss
- [ ] backend_reference.md aktualisieren
- [ ] Error Codes dokumentieren
- [ ] Dieses Task-File aktualisieren

## 11. Tests

### Unit Tests zu erstellen

| Test Datei | Was wird getestet? | Geschätzte Anzahl |
|------------|-------------------|-------------------|
| `CustomerRegisterHandlerTests.cs` | Registration | ~10 |
| `CustomerLoginHandlerTests.cs` | Login | ~10 |
| `CustomerRefreshTokenHandlerTests.cs` | Token Refresh | ~5 |
| `CustomerPasswordResetTests.cs` | Password Reset Flow | ~8 |
| `CreateOrganizationHandlerTests.cs` | Org erstellen | ~8 |
| `UpdateOrganizationHandlerTests.cs` | Org bearbeiten | ~5 |
| `DeleteOrganizationHandlerTests.cs` | Org löschen | ~5 |
| `OrganizationMemberHandlerTests.cs` | Member Management | ~15 |
| `GetOrganizationsHandlerTests.cs` | Org Queries | ~5 |

**Geschätzt: ~70 neue Tests**

## 12. Nicht in Scope (spätere Tasks)

| Feature | Task |
|---------|------|
| Customer MFA (TOTP, Backup Codes) | Task 027b |
| Customer Passkeys | Task 028+ |
| Customer Device Trust | Task 028+ |
| Organization Invites (Email) | Task 028+ |
| Plans & Subscriptions | Task 029+ |
| Projects (innerhalb von Orgs) | Task 030+ |
| Customer Audit Logs | Task 028+ |

## 13. Geklärte Fragen

- [x] Soll Customer sich selbst registrieren? → **Ja**
- [x] Multi-Org Support? → **Ja**
- [x] Welche Rollen? → **Owner + Member (einfach)**
- [x] Billing-Felder jetzt? → **Nein, später**
- [x] Email-Verifizierung für Customers erforderlich? → **Ja, Pflicht**
- [x] Org-Slug Validierung? → **Ja, auto-generiert aus Name, unique mit Nummer-Suffix bei Duplikaten (tech-startup-gmbh-2)**

## 14. Letzte Änderung

- **Datum:** 2026-01-17
- **Status:** Planning Complete
- **Nächster Schritt:** Implementation Phase 1 (Route Migration)

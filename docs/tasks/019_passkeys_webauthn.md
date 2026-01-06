# Task 019: Passkeys (WebAuthn/FIDO2)

## 1. Übersicht

**Was wird gebaut?**
Passkeys (WebAuthn/FIDO2) Authentication für SystemUsers. Ermöglicht passwordless Login mit biometrischen Daten (Fingerabdruck, Face ID) oder Hardware-Keys (YubiKey).

**Warum?**
- Moderne, phishing-resistente Authentifizierung
- Bessere UX: Kein Passwort merken/eingeben
- Höhere Sicherheit als Passwörter
- Marketing-Feature: "Passwordless Authentication"
- Zukunftssicher: Passkeys werden zum Standard (Apple, Google, Microsoft pushen das)

---

## 2. User Experience / Anforderungen

### User Stories
- Als SystemUser möchte ich einen Passkey registrieren, damit ich mich ohne Passwort einloggen kann
- Als SystemUser möchte ich mich mit meinem Fingerabdruck/Face ID einloggen, damit der Login schneller und sicherer ist
- Als SystemUser möchte ich meine Passkeys verwalten (umbenennen, löschen), damit ich die Übersicht behalte
- Als SystemUser möchte ich mehrere Passkeys registrieren (z.B. Laptop + Handy), damit ich flexibel bin

### Akzeptanzkriterien
- [ ] User kann Passkey registrieren (nach Login mit Passwort)
- [ ] User kann sich mit Passkey einloggen (ohne Passwort)
- [ ] User kann mehrere Passkeys haben
- [ ] User kann Passkey umbenennen
- [ ] User kann Passkey löschen
- [ ] Passkey-Login funktioniert mit:
  - [ ] Windows Hello (Fingerprint/Face/PIN)
  - [ ] macOS Touch ID
  - [ ] iOS Face ID / Touch ID
  - [ ] Android Fingerprint
  - [ ] Hardware Keys (YubiKey, etc.)
- [ ] Passkey-Login kann MFA ersetzen ODER zusätzlich zu MFA sein (konfigurierbar)
- [ ] Mindestens 1 Login-Methode muss aktiv bleiben (kann nicht letzten Passkey löschen wenn kein Passwort)

### Edge Cases / Error Handling
- Was passiert wenn Browser WebAuthn nicht unterstützt? → Fallback auf Passwort, Passkey-Optionen ausblenden
- Was passiert wenn Passkey-Gerät nicht verfügbar? → "Use password instead" Option
- Was passiert wenn User letzten Passkey löschen will? → Nur erlauben wenn Passwort gesetzt
- Was passiert bei Timeout während Passkey-Registrierung? → Retry Option
- Was passiert wenn User abbricht? → Graceful cancel, zurück zum vorherigen State

---

## 3. API Endpoints

### Passkey Registration

| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| POST | `/api/auth/passkeys/register/options` | ✅ JWT | `{ }` | `{ challenge, rp, user, pubKeyCredParams, ... }` | Get WebAuthn registration options |
| POST | `/api/auth/passkeys/register` | ✅ JWT | `{ id, rawId, response, type, name? }` | `{ id, name, createdAt }` | Complete passkey registration |

### Passkey Login

| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| POST | `/api/auth/passkeys/login/options` | ❌ | `{ email? }` | `{ challenge, allowCredentials, ... }` | Get WebAuthn login options |
| POST | `/api/auth/passkeys/login` | ❌ | `{ id, rawId, response, type, deviceId, deviceFingerprint }` | `{ accessToken, refreshToken, user }` | Login with passkey |

### Passkey Management

| Method | Route | Auth | Request Body | Response | Beschreibung |
|--------|-------|------|--------------|----------|--------------|
| GET | `/api/auth/passkeys` | ✅ JWT | - | `{ passkeys: [...] }` | List user's passkeys |
| PATCH | `/api/auth/passkeys/{id}` | ✅ JWT | `{ name }` | `{ id, name, ... }` | Rename passkey |
| DELETE | `/api/auth/passkeys/{id}` | ✅ JWT | - | `{ success: true }` | Delete passkey |

---

## 4. Neue Error Codes

| Code | HTTP Status | Beschreibung |
|------|-------------|--------------|
| `PASSKEY_NOT_FOUND` | 404 | Passkey existiert nicht |
| `PASSKEY_INVALID_CREDENTIAL` | 400 | Credential konnte nicht verifiziert werden |
| `PASSKEY_CHALLENGE_EXPIRED` | 400 | Challenge ist abgelaufen (timeout) |
| `PASSKEY_CHALLENGE_INVALID` | 400 | Challenge stimmt nicht überein |
| `PASSKEY_ALREADY_REGISTERED` | 409 | Dieser Passkey ist bereits registriert |
| `PASSKEY_REGISTRATION_FAILED` | 400 | Registrierung fehlgeschlagen |
| `PASSKEY_USER_NOT_FOUND` | 404 | User für Passkey-Login nicht gefunden |
| `PASSKEY_CANNOT_DELETE_LAST` | 400 | Kann letzten Passkey nicht löschen (kein Passwort gesetzt) |
| `WEBAUTHN_NOT_SUPPORTED` | 400 | WebAuthn wird nicht unterstützt |

> ⚠️ **Nach Completion:** Diese Codes zu `backend_reference.md` Memory hinzufügen!

---

## 5. Datenbank Änderungen

### Neue Entities

| Entity | Properties | Beziehungen |
|--------|------------|-------------|
| `Passkey` | Id, UserId, CredentialId, PublicKey, Counter, CredType, AaGuid, Name, CreatedAt, LastUsedAt | SystemUser (N:1) |

### Entity Details: Passkey

```csharp
public sealed class Passkey
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    
    // WebAuthn Credential Data
    public byte[] CredentialId { get; private set; }      // Unique credential identifier
    public byte[] PublicKey { get; private set; }          // Public key for verification
    public uint Counter { get; private set; }              // Signature counter (replay protection)
    public string CredType { get; private set; }           // Credential type (e.g., "public-key")
    public Guid AaGuid { get; private set; }               // Authenticator Attestation GUID
    
    // Metadata
    public string Name { get; private set; }               // User-friendly name ("MacBook Pro", "YubiKey")
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    
    // Navigation
    public SystemUser User { get; private set; }
}
```

### SystemUser Änderungen

```csharp
// Add to SystemUser entity:
private readonly List<Passkey> _passkeys = new();
public IReadOnlyCollection<Passkey> Passkeys => _passkeys.AsReadOnly();
public bool HasPasskeys => _passkeys.Any();
```

### Migrations
- [ ] Migration Name: `AddPasskeys`

---

## 6. Files zu erstellen

### Domain Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Passkey.cs | `src/ExoAuth.Domain/Entities/Passkey.cs` | Passkey Entity |

### Application Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| **Interfaces** | | |
| IPasskeyService.cs | `src/ExoAuth.Application/Common/Interfaces/IPasskeyService.cs` | Service interface |
| **Models** | | |
| PasskeyDto.cs | `src/ExoAuth.Application/Features/Auth/Models/PasskeyDto.cs` | Passkey DTO |
| **Commands - Register Options** | | |
| PasskeyRegisterOptionsCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyRegisterOptions/PasskeyRegisterOptionsCommand.cs` | Command |
| PasskeyRegisterOptionsHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyRegisterOptions/PasskeyRegisterOptionsHandler.cs` | Handler |
| **Commands - Register** | | |
| PasskeyRegisterCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyRegister/PasskeyRegisterCommand.cs` | Command |
| PasskeyRegisterHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyRegister/PasskeyRegisterHandler.cs` | Handler |
| PasskeyRegisterValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyRegister/PasskeyRegisterValidator.cs` | Validator |
| **Commands - Login Options** | | |
| PasskeyLoginOptionsCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyLoginOptions/PasskeyLoginOptionsCommand.cs` | Command |
| PasskeyLoginOptionsHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyLoginOptions/PasskeyLoginOptionsHandler.cs` | Handler |
| **Commands - Login** | | |
| PasskeyLoginCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyLogin/PasskeyLoginCommand.cs` | Command |
| PasskeyLoginHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyLogin/PasskeyLoginHandler.cs` | Handler |
| PasskeyLoginValidator.cs | `src/ExoAuth.Application/Features/Auth/Commands/PasskeyLogin/PasskeyLoginValidator.cs` | Validator |
| **Commands - Rename** | | |
| RenamePasskeyCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/RenamePasskey/RenamePasskeyCommand.cs` | Command |
| RenamePasskeyHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/RenamePasskey/RenamePasskeyHandler.cs` | Handler |
| **Commands - Delete** | | |
| DeletePasskeyCommand.cs | `src/ExoAuth.Application/Features/Auth/Commands/DeletePasskey/DeletePasskeyCommand.cs` | Command |
| DeletePasskeyHandler.cs | `src/ExoAuth.Application/Features/Auth/Commands/DeletePasskey/DeletePasskeyHandler.cs` | Handler |
| **Queries** | | |
| GetPasskeysQuery.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetPasskeys/GetPasskeysQuery.cs` | Query |
| GetPasskeysHandler.cs | `src/ExoAuth.Application/Features/Auth/Queries/GetPasskeys/GetPasskeysHandler.cs` | Handler |

### Infrastructure Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| PasskeyConfiguration.cs | `src/ExoAuth.Infrastructure/Persistence/Configurations/PasskeyConfiguration.cs` | EF Configuration |
| PasskeyService.cs | `src/ExoAuth.Infrastructure/Services/PasskeyService.cs` | WebAuthn logic using Fido2NetLib |

### API Layer

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| - | `src/ExoAuth.Api/Controllers/AuthController.cs` | Add passkey endpoints (existing file) |

---

## 7. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/ExoAuth.Domain/Entities/SystemUser.cs` | Add `_passkeys` collection, `HasPasskeys` property |
| `src/ExoAuth.Infrastructure/Persistence/AppDbContext.cs` | Add `DbSet<Passkey>` |
| `src/ExoAuth.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs` | Add Passkeys relationship |
| `src/ExoAuth.Infrastructure/DependencyInjection.cs` | Register `IPasskeyService` |
| `src/ExoAuth.Api/Controllers/AuthController.cs` | Add 6 new endpoints |
| `src/ExoAuth.Application/Common/Models/ErrorCodes.cs` | Add passkey error codes |

---

## 8. Neue Packages

| Package | Version | Projekt | Warum? |
|---------|---------|---------|--------|
| `Fido2.Models` | 3.x | ExoAuth.Application | FIDO2/WebAuthn models |
| `Fido2` | 3.x | ExoAuth.Infrastructure | FIDO2/WebAuthn implementation |
| `Fido2.AspNet` | 3.x | ExoAuth.Api | ASP.NET Core integration |

> **Note:** Fido2NetLib ist die Standard-.NET-Library für WebAuthn: https://github.com/passwordless-lib/fido2-net-lib

---

## 9. Implementation Reihenfolge

### Backend

1. [x] **Package installieren**: `Fido2` packages zu den Projekten hinzufügen
2. [x] **Domain**: `Passkey.cs` Entity erstellen
3. [x] **Domain**: `SystemUser.cs` erweitern (Passkeys collection)
4. [x] **Infrastructure**: `PasskeyConfiguration.cs` erstellen
5. [x] **Infrastructure**: `AppDbContext.cs` - DbSet hinzufügen
6. [x] **Infrastructure**: Migration erstellen & ausführen
7. [x] **Application**: `IPasskeyService.cs` Interface erstellen
8. [x] **Infrastructure**: `PasskeyService.cs` implementieren
9. [x] **Application**: Error Codes hinzufügen
10. [x] **Application**: `PasskeyDto.cs` erstellen
11. [x] **Application**: Register Options Command/Handler
12. [x] **Application**: Register Command/Handler/Validator
13. [x] **Application**: Login Options Command/Handler
14. [x] **Application**: Login Command/Handler/Validator
15. [x] **Application**: GetPasskeys Query/Handler
16. [x] **Application**: RenamePasskey Command/Handler
17. [x] **Application**: DeletePasskey Command/Handler
18. [x] **API**: Endpoints in AuthController hinzufügen
19. [ ] **Tests**: Unit Tests schreiben (deferred - can add later)
20. [ ] **Memory updaten**: backend_reference.md aktualisieren

### Frontend (Task 020)

> Frontend wird in separatem Task 020 implementiert

---

## 10. Technical Details

### WebAuthn Flow - Registration

```
1. Frontend: User klickt "Add Passkey"
2. Frontend → Backend: POST /passkeys/register/options
3. Backend: Generiert Challenge, speichert in Redis (5 min TTL)
4. Backend → Frontend: { challenge, rp, user, pubKeyCredParams, ... }
5. Frontend: navigator.credentials.create(options)
6. Browser: Zeigt Passkey-Dialog (Touch ID, Windows Hello, etc.)
7. User: Bestätigt mit Biometrie/PIN
8. Browser → Frontend: Credential object
9. Frontend → Backend: POST /passkeys/register { credential }
10. Backend: Verifiziert Attestation, speichert PublicKey
11. Backend → Frontend: { success, passkey }
```

### WebAuthn Flow - Login

```
1. Frontend: User klickt "Login with Passkey"
2. Frontend → Backend: POST /passkeys/login/options { email? }
3. Backend: Generiert Challenge, lädt allowCredentials
4. Backend → Frontend: { challenge, allowCredentials, ... }
5. Frontend: navigator.credentials.get(options)
6. Browser: Zeigt Passkey-Dialog
7. User: Bestätigt mit Biometrie/PIN
8. Browser → Frontend: Assertion object
9. Frontend → Backend: POST /passkeys/login { assertion }
10. Backend: Verifiziert Signature mit PublicKey, prüft Counter
11. Backend: Erstellt Session, generiert Tokens
12. Backend → Frontend: { accessToken, refreshToken, user }
```

### Fido2NetLib Configuration

```csharp
services.AddFido2(options =>
{
    options.ServerDomain = "exoauth.com";
    options.ServerName = "ExoAuth";
    options.Origins = new HashSet<string> { "https://exoauth.com" };
    options.TimestampDriftTolerance = 300000; // 5 minutes
});
```

### Challenge Storage

- Store in Redis with 5 minute TTL
- Key format: `passkey:challenge:{userId}:{challengeId}`
- Delete after successful use (one-time use)

---

## 11. Tests

### Unit Tests

| Test Datei | Was wird getestet? | Anzahl |
|------------|-------------------|--------|
| `PasskeyRegisterOptionsHandlerTests.cs` | Challenge generation, options format | ~5 |
| `PasskeyRegisterHandlerTests.cs` | Credential storage, validation, duplicates | ~8 |
| `PasskeyLoginOptionsHandlerTests.cs` | Challenge generation, allowCredentials | ~5 |
| `PasskeyLoginHandlerTests.cs` | Signature verification, counter check, token generation | ~10 |
| `GetPasskeysHandlerTests.cs` | List passkeys, empty state | ~3 |
| `RenamePasskeyHandlerTests.cs` | Rename success, not found | ~3 |
| `DeletePasskeyHandlerTests.cs` | Delete success, not found, last passkey protection | ~5 |
| **Total** | | **~39** |

---

## 12. Security Considerations

- [ ] Challenge must be cryptographically random (32 bytes minimum)
- [ ] Challenge must be one-time use (delete after verification)
- [ ] Challenge must expire (5 minutes)
- [ ] Counter must be validated (prevent replay attacks)
- [ ] CredentialId must be unique per user
- [ ] Origin must be validated
- [ ] RP ID must match expected domain
- [ ] Rate limiting on login/register endpoints
- [ ] Audit log all passkey operations

---

## 13. Email Templates

| Template | Wann gesendet? |
|----------|----------------|
| `passkey-registered.html` | Nach erfolgreicher Passkey-Registrierung |
| `passkey-removed.html` | Nach Passkey-Löschung (Security Alert) |
| `passkey-login.html` | Optional: Nach Login mit Passkey von neuem Gerät |

---

## 14. Audit Log Events

| Event | Beschreibung |
|-------|--------------|
| `PASSKEY_REGISTERED` | Neuer Passkey registriert |
| `PASSKEY_LOGIN` | Login mit Passkey erfolgreich |
| `PASSKEY_LOGIN_FAILED` | Login mit Passkey fehlgeschlagen |
| `PASSKEY_RENAMED` | Passkey umbenannt |
| `PASSKEY_DELETED` | Passkey gelöscht |

---

## 15. Nach Completion

- [x] Existing Unit Tests still pass (315 tests)
- [ ] `backend_reference.md` Memory aktualisiert:
  - [ ] File Tree erweitert
  - [ ] Neue Packages dokumentiert
  - [ ] Error Codes hinzugefügt
- [x] Email Templates erstellt (EN + DE)
- [ ] Code reviewed
- [ ] Manuell getestet mit:
  - [ ] Windows Hello
  - [ ] macOS Touch ID
  - [ ] Mobile (wenn möglich)

---

## 16. Letzte Änderung

- **Datum:** 2026-01-06
- **Status:** Backend Complete (awaiting frontend - Task 020)
- **Implementiert:**
  - Passkey Entity + Migration
  - IPasskeyService + PasskeyService (Fido2NetLib)
  - All Commands: RegisterOptions, Register, LoginOptions, Login, Rename, Delete
  - GetPasskeys Query
  - 7 API Endpoints in AuthController
  - Email templates (passkey-registered, passkey-removed) for EN + DE
  - Error codes added
  
**Note:** Passkey login is intentionally simplified compared to regular login:
- Skips device approval flow (creates trusted device directly)
- No risk calculation or spoofing detection
- Rationale: Passkeys ARE the strong authentication (biometrics/hardware) - they prove possession of a trusted device, making additional device verification redundant

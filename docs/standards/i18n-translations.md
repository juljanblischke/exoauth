# i18n Translations - ExoAuth

## Overview

The ExoAuth frontend supports **English (EN)** and **German (DE)** translations using i18next.

### File Structure
```
frontend/src/i18n/
├── index.ts                    # i18next configuration
└── locales/
    ├── en/
    │   ├── common.json         # Common UI elements
    │   ├── auth.json           # Authentication
    │   ├── navigation.json     # Navigation & menus
    │   ├── users.json          # User management
    │   ├── errors.json         # Error messages
    │   └── validation.json     # Form validation
    └── de/
        ├── common.json
        ├── auth.json
        ├── navigation.json
        ├── users.json
        ├── errors.json
        └── validation.json
```

---

## Translation Keys

### common.json

| Key | EN | DE |
|-----|----|----|
| `app.name` | ExoAuth | ExoAuth |
| `app.copyright` | All rights reserved. | Alle Rechte vorbehalten. |
| `actions.save` | Save | Speichern |
| `actions.cancel` | Cancel | Abbrechen |
| `actions.delete` | Delete | Löschen |
| `actions.edit` | Edit | Bearbeiten |
| `actions.create` | Create | Erstellen |
| `actions.add` | Add | Hinzufügen |
| `actions.remove` | Remove | Entfernen |
| `actions.close` | Close | Schließen |
| `actions.confirm` | Confirm | Bestätigen |
| `actions.back` | Back | Zurück |
| `actions.next` | Next | Weiter |
| `actions.submit` | Submit | Absenden |
| `actions.search` | Search | Suchen |
| `actions.filter` | Filter | Filtern |
| `actions.reset` | Reset | Zurücksetzen |
| `actions.refresh` | Refresh | Aktualisieren |
| `actions.export` | Export | Exportieren |
| `actions.import` | Import | Importieren |
| `actions.copy` | Copy | Kopieren |
| `actions.copied` | Copied! | Kopiert! |
| `actions.loading` | Loading... | Laden... |
| `actions.view` | View | Ansehen |
| `actions.viewDetails` | View Details | Details anzeigen |
| `actions.viewAll` | View All | Alle anzeigen |
| `actions.showMore` | Show More | Mehr anzeigen |
| `actions.showLess` | Show Less | Weniger anzeigen |
| `actions.selectAll` | Select All | Alle auswählen |
| `actions.deselectAll` | Deselect All | Auswahl aufheben |
| `actions.continue` | Continue | Fortfahren |
| `actions.retry` | Retry | Erneut versuchen |
| `actions.account` | Account | Konto |
| `actions.logout` | Logout | Abmelden |
| `actions.clear` | Clear | Löschen |
| `search.placeholder` | Search... | Suchen... |
| `search.noResults` | No results found. | Keine Ergebnisse gefunden. |
| `status.active` | Active | Aktiv |
| `status.inactive` | Inactive | Inaktiv |
| `status.pending` | Pending | Ausstehend |
| `status.suspended` | Suspended | Gesperrt |
| `status.enabled` | Enabled | Aktiviert |
| `status.disabled` | Disabled | Deaktiviert |
| `table.noResults` | No results found | Keine Ergebnisse gefunden |
| `table.noData` | No data available | Keine Daten vorhanden |
| `table.loading` | Loading data... | Daten werden geladen... |
| `table.selected` | {{count}} selected | {{count}} ausgewählt |
| `table.selectAll` | Select all | Alle auswählen |
| `table.selectRow` | Select row | Zeile auswählen |
| `table.rowsPerPage` | Rows per page | Zeilen pro Seite |
| `table.columns` | Columns | Spalten |
| `table.showColumns` | Show columns | Spalten anzeigen |
| `table.sortAsc` | Sort ascending | Aufsteigend sortieren |
| `table.sortDesc` | Sort descending | Absteigend sortieren |
| `time.justNow` | Just now | Gerade eben |
| `time.minutesAgo` | {{count}} minute(s) ago | vor {{count}} Minute(n) |
| `time.hoursAgo` | {{count}} hour(s) ago | vor {{count}} Stunde(n) |
| `time.daysAgo` | {{count}} day(s) ago | vor {{count}} Tag(en) |
| `confirm.title` | Confirm Action | Aktion bestätigen |
| `confirm.deleteTitle` | Confirm Delete | Löschen bestätigen |
| `confirm.deleteMessage` | Are you sure you want to delete...? | Sind Sie sicher, dass Sie...löschen möchten? |
| `confirm.unsavedTitle` | Unsaved Changes | Ungespeicherte Änderungen |
| `confirm.unsavedMessage` | You have unsaved changes... | Sie haben ungespeicherte Änderungen... |
| `confirm.typeToConfirm` | Type "{{text}}" to confirm | Geben Sie "{{text}}" zur Bestätigung ein |
| `confirm.stay` | Stay | Bleiben |
| `confirm.leave` | Leave | Verlassen |
| `legal.imprint` | Imprint | Impressum |
| `legal.privacy` | Privacy Policy | Datenschutz |
| `legal.terms` | Terms of Service | AGB |
| `legal.cookies` | Cookie Policy | Cookie-Richtlinie |
| `theme.label` | Theme | Design |
| `theme.light` | Light | Hell |
| `theme.dark` | Dark | Dunkel |
| `theme.system` | System | System |
| `language.en` | English | English |
| `language.de` | Deutsch | Deutsch |
| `session.warningTitle` | Session Expiring | Sitzung läuft ab |
| `session.warningDescription` | Your session will expire in {{time}}... | Ihre Sitzung läuft in {{time}} ab... |
| `session.extend` | Extend Session | Sitzung verlängern |
| `cookies.title` | Cookie Consent | Cookie-Einwilligung |
| `cookies.description` | We use cookies to improve your experience... | Wir verwenden Cookies... |
| `cookies.accept` | Accept All | Alle akzeptieren |
| `cookies.reject` | Reject | Ablehnen |
| `cookies.learnMore` | Learn More | Mehr erfahren |
| `help.toggle` | Help | Hilfe |
| `help.title` | Need Help? | Brauchen Sie Hilfe? |
| `help.documentation` | Documentation | Dokumentation |
| `help.support` | Contact Support | Support kontaktieren |
| `help.feedback` | Send Feedback | Feedback senden |

---

### auth.json

| Key | EN | DE |
|-----|----|----|
| `login.title` | Sign In | Anmelden |
| `login.subtitle` | Enter your credentials... | Geben Sie Ihre Anmeldedaten ein |
| `login.email` | Email | E-Mail |
| `login.password` | Password | Passwort |
| `login.rememberMe` | Remember me | Angemeldet bleiben |
| `login.forgotPassword` | Forgot password? | Passwort vergessen? |
| `login.signIn` | Sign In | Anmelden |
| `login.signingIn` | Signing in... | Anmeldung... |
| `login.noAccount` | Don't have an account? | Noch kein Konto? |
| `login.register` | Register | Registrieren |
| `register.title` | Create Account | Konto erstellen |
| `register.name` | Full Name | Vollständiger Name |
| `register.confirmPassword` | Confirm Password | Passwort bestätigen |
| `register.createAccount` | Create Account | Konto erstellen |
| `register.hasAccount` | Already have an account? | Bereits ein Konto? |
| `invite.title` | Accept Invitation | Einladung annehmen |
| `invite.subtitle` | You've been invited to join {{organization}} | Sie wurden eingeladen, {{organization}} beizutreten |
| `invite.accept` | Accept Invitation | Einladung annehmen |
| `invite.expired` | This invitation has expired | Diese Einladung ist abgelaufen |
| `invite.invalid` | This invitation is invalid | Diese Einladung ist ungültig |
| `forgotPassword.title` | Forgot Password | Passwort vergessen |
| `forgotPassword.sendLink` | Send Reset Link | Link senden |
| `forgotPassword.sent` | Reset link sent! | Link gesendet! |
| `forgotPassword.backToLogin` | Back to Sign In | Zurück zur Anmeldung |
| `resetPassword.title` | Reset Password | Passwort zurücksetzen |
| `resetPassword.newPassword` | New Password | Neues Passwort |
| `resetPassword.success` | Password reset successful! | Passwort erfolgreich zurückgesetzt! |
| `logout.title` | Sign Out | Abmelden |
| `logout.message` | Are you sure you want to sign out? | Möchten Sie sich wirklich abmelden? |
| `logout.confirm` | Sign Out | Abmelden |
| `session.expiring` | Session Expiring | Sitzung läuft ab |
| `session.expiringMessage` | Your session will expire in {{minutes}} minutes... | Ihre Sitzung läuft in {{minutes}} Minuten ab... |
| `session.expired` | Session Expired | Sitzung abgelaufen |
| `session.extend` | Stay Signed In | Angemeldet bleiben |
| `mfa.title` | Two-Factor Authentication | Zwei-Faktor-Authentifizierung |
| `mfa.code` | Verification Code | Verifizierungscode |
| `mfa.verify` | Verify | Verifizieren |
| `mfa.useBackupCode` | Use backup code | Backup-Code verwenden |

---

### navigation.json

| Key | EN | DE |
|-----|----|----|
| `sections.navigation` | Navigation | Navigation |
| `sections.system` | System | System |
| `sections.management` | Management | Verwaltung |
| `sections.settings` | Settings | Einstellungen |
| `items.dashboard` | Dashboard | Dashboard |
| `items.users` | Users | Benutzer |
| `items.permissions` | Permissions | Berechtigungen |
| `items.auditLogs` | Audit Logs | Audit-Protokolle |
| `items.organizations` | Organizations | Organisationen |
| `items.projects` | Projects | Projekte |
| `items.settings` | Settings | Einstellungen |
| `breadcrumb.home` | Home | Startseite |
| `breadcrumb.userDetails` | User Details | Benutzerdetails |
| `breadcrumb.createUser` | Create User | Benutzer erstellen |
| `breadcrumb.editUser` | Edit User | Benutzer bearbeiten |
| `userMenu.profile` | Profile | Profil |
| `userMenu.signOut` | Sign Out | Abmelden |
| `search.placeholder` | Search... | Suchen... |
| `search.noResults` | No results found | Keine Ergebnisse gefunden |
| `search.recentSearches` | Recent Searches | Letzte Suchen |
| `search.clearRecent` | Clear recent | Verlauf löschen |
| `notifications.title` | Notifications | Benachrichtigungen |
| `notifications.empty` | No notifications | Keine Benachrichtigungen |
| `notifications.markAllRead` | Mark all as read | Alle als gelesen markieren |
| `help.title` | Help | Hilfe |
| `help.documentation` | Documentation | Dokumentation |
| `help.support` | Support | Support |
| `help.feedback` | Send Feedback | Feedback senden |
| `sidebar.collapse` | Collapse | Einklappen |
| `sidebar.expand` | Expand | Ausklappen |
| `sidebar.toggleMenu` | Toggle menu | Menü umschalten |
| `language.change` | Change language | Sprache ändern |

---

### users.json

| Key | EN | DE |
|-----|----|----|
| `title` | Users | Benutzer |
| `subtitle` | Manage system users and their permissions | Systembenutzer und deren Berechtigungen verwalten |
| `createUser` | Create User | Benutzer erstellen |
| `inviteUser` | Invite User | Benutzer einladen |
| `editUser` | Edit User | Benutzer bearbeiten |
| `deleteUser` | Delete User | Benutzer löschen |
| `userDetails` | User Details | Benutzerdetails |
| `fields.name` | Name | Name |
| `fields.email` | Email | E-Mail |
| `fields.role` | Role | Rolle |
| `fields.status` | Status | Status |
| `fields.createdAt` | Created | Erstellt |
| `fields.updatedAt` | Last Updated | Zuletzt aktualisiert |
| `fields.lastLogin` | Last Login | Letzte Anmeldung |
| `fields.mfaEnabled` | 2FA Enabled | 2FA aktiviert |
| `roles.admin` | Administrator | Administrator |
| `roles.user` | User | Benutzer |
| `roles.viewer` | Viewer | Betrachter |
| `status.active` | Active | Aktiv |
| `status.inactive` | Inactive | Inaktiv |
| `status.pending` | Pending Invite | Einladung ausstehend |
| `status.suspended` | Suspended | Gesperrt |
| `actions.activate` | Activate | Aktivieren |
| `actions.deactivate` | Deactivate | Deaktivieren |
| `actions.suspend` | Suspend | Sperren |
| `actions.resetPassword` | Reset Password | Passwort zurücksetzen |
| `actions.resendInvite` | Resend Invite | Einladung erneut senden |
| `actions.revokeInvite` | Revoke Invite | Einladung widerrufen |
| `messages.createSuccess` | User created successfully | Benutzer erfolgreich erstellt |
| `messages.updateSuccess` | User updated successfully | Benutzer erfolgreich aktualisiert |
| `messages.deleteSuccess` | User deleted successfully | Benutzer erfolgreich gelöscht |
| `messages.inviteSuccess` | Invitation sent successfully | Einladung erfolgreich gesendet |
| `empty.title` | No users yet | Noch keine Benutzer |
| `empty.message` | Get started by creating your first user... | Erstellen Sie Ihren ersten Benutzer... |
| `empty.action` | Create User | Benutzer erstellen |
| `filters.status` | Status | Status |
| `filters.role` | Role | Rolle |
| `filters.all` | All | Alle |

---

### errors.json

| Key | EN | DE |
|-----|----|----|
| `general.title` | Something went wrong | Etwas ist schiefgelaufen |
| `general.message` | An unexpected error occurred... | Ein unerwarteter Fehler ist aufgetreten... |
| `general.retry` | Try Again | Erneut versuchen |
| `general.goHome` | Go to Dashboard | Zum Dashboard |
| `network.title` | Connection Error | Verbindungsfehler |
| `network.message` | Unable to connect to the server... | Verbindung zum Server nicht möglich... |
| `network.offline` | You are offline | Sie sind offline |
| `notFound.title` | Page Not Found | Seite nicht gefunden |
| `notFound.code` | 404 | 404 |
| `notFound.message` | The page you're looking for doesn't exist... | Die gesuchte Seite existiert nicht... |
| `forbidden.title` | Access Denied | Zugriff verweigert |
| `forbidden.code` | 403 | 403 |
| `forbidden.message` | You don't have permission... | Sie haben keine Berechtigung... |
| `serverError.title` | Server Error | Serverfehler |
| `serverError.code` | 500 | 500 |
| `unauthorized.title` | Session Expired | Sitzung abgelaufen |
| `unauthorized.code` | 401 | 401 |
| `api.badRequest` | Invalid request... | Ungültige Anfrage... |
| `api.conflict` | This resource already exists | Diese Ressource existiert bereits |
| `api.notFound` | The requested resource was not found | Die angeforderte Ressource wurde nicht gefunden |
| `api.rateLimited` | Too many requests... | Zu viele Anfragen... |
| `api.serverError` | Server error... | Serverfehler... |
| `api.validationError` | Please fix the errors below | Bitte korrigieren Sie die Fehler unten |
| `api.invalidCredentials` | Invalid email or password | Ungültige E-Mail oder Passwort |
| `api.emailExists` | An account with this email already exists | Ein Konto mit dieser E-Mail existiert bereits |
| `api.tokenExpired` | Your session has expired... | Ihre Sitzung ist abgelaufen... |
| `api.insufficientPermissions` | You don't have permission... | Sie haben keine Berechtigung... |

---

### validation.json

| Key | EN | DE |
|-----|----|----|
| `required` | This field is required | Dieses Feld ist erforderlich |
| `email` | Please enter a valid email address | Bitte geben Sie eine gültige E-Mail-Adresse ein |
| `minLength` | Must be at least {{min}} characters | Muss mindestens {{min}} Zeichen lang sein |
| `maxLength` | Must be at most {{max}} characters | Darf höchstens {{max}} Zeichen lang sein |
| `min` | Must be at least {{min}} | Muss mindestens {{min}} sein |
| `max` | Must be at most {{max}} | Darf höchstens {{max}} sein |
| `pattern` | Invalid format | Ungültiges Format |
| `url` | Please enter a valid URL | Bitte geben Sie eine gültige URL ein |
| `number` | Must be a number | Muss eine Zahl sein |
| `integer` | Must be a whole number | Muss eine ganze Zahl sein |
| `positive` | Must be a positive number | Muss eine positive Zahl sein |
| `date` | Please enter a valid date | Bitte geben Sie ein gültiges Datum ein |
| `password.minLength` | Password must be at least {{min}} characters | Passwort muss mindestens {{min}} Zeichen lang sein |
| `password.lowercase` | Password must contain at least one lowercase letter | Passwort muss mindestens einen Kleinbuchstaben enthalten |
| `password.uppercase` | Password must contain at least one uppercase letter | Passwort muss mindestens einen Großbuchstaben enthalten |
| `password.number` | Password must contain at least one number | Passwort muss mindestens eine Zahl enthalten |
| `password.special` | Password must contain at least one special character | Passwort muss mindestens ein Sonderzeichen enthalten |
| `password.mismatch` | Passwords do not match | Passwörter stimmen nicht überein |
| `password.weak` | Password is too weak | Passwort ist zu schwach |
| `password.strength.weak` | Weak | Schwach |
| `password.strength.fair` | Fair | Ausreichend |
| `password.strength.good` | Good | Gut |
| `password.strength.strong` | Strong | Stark |
| `unique.email` | This email is already registered | Diese E-Mail ist bereits registriert |
| `confirmation.mismatch` | Values do not match | Werte stimmen nicht überein |

---

## Usage

```tsx
import { useTranslation } from 'react-i18next'

function MyComponent() {
  const { t } = useTranslation()

  // Default namespace (common)
  return <button>{t('actions.save')}</button>

  // Specific namespace
  return <h1>{t('auth:login.title')}</h1>

  // With interpolation
  return <p>{t('time.minutesAgo', { count: 5 })}</p>
}
```

---

**Last Updated:** 2025-12-26

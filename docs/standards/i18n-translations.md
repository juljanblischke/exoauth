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
    │   ├── auditLogs.json      # Audit logs
    │   ├── settings.json       # Settings page
    │   ├── mfa.json            # Two-factor authentication
    │   ├── sessions.json       # Device sessions
    │   ├── errors.json         # Error messages
    │   └── validation.json     # Form validation
    └── de/
        ├── common.json
        ├── auth.json
        ├── navigation.json
        ├── users.json
        ├── auditLogs.json
        ├── settings.json
        ├── mfa.json
        ├── sessions.json
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
| `dashboard.title` | Dashboard | Dashboard |
| `dashboard.welcome` | Welcome back, {{name}}! | Willkommen zurück, {{name}}! |
| `dashboard.welcomeGeneric` | Welcome back! | Willkommen zurück! |
| `dashboard.stats.totalUsers` | Total Users | Gesamtbenutzer |
| `dashboard.stats.activeSessions` | Active Sessions | Aktive Sitzungen |
| `dashboard.stats.organizations` | Organizations | Organisationen |
| `dashboard.stats.projects` | Projects | Projekte |
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
| `states.deleting` | Deleting... | Wird gelöscht... |
| `states.saving` | Saving... | Wird gespeichert... |
| `states.processing` | Processing... | Wird verarbeitet... |
| `table.noResults` | No results found | Keine Ergebnisse gefunden |
| `table.noData` | No data available | Keine Daten vorhanden |
| `table.loading` | Loading data... | Daten werden geladen... |
| `table.selected` | {{count}} selected | {{count}} ausgewählt |
| `table.totalRows` | {{count}} row(s) | {{count}} Zeile(n) |
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
| `confirm.typeToConfirm` | Type `<highlight>{{text}}</highlight>` to confirm | Geben Sie `<highlight>{{text}}</highlight>` zur Bestätigung ein |
| `confirm.stay` | Stay | Bleiben |
| `confirm.leave` | Leave | Verlassen |
| `legal.imprint` | Imprint | Impressum |
| `legal.privacy` | Privacy Policy | Datenschutz |
| `legal.terms` | Terms of Service | AGB |
| `legal.cookies` | Cookie Policy | Cookie-Richtlinie |
| `legal.backToHome` | Back to Home | Zurück zur Startseite |
| `legal.lastUpdated` | Last updated | Zuletzt aktualisiert |
| `legal.imprintPage.title` | Imprint | Impressum |
| `legal.imprintPage.subtitle` | Legal information about the operator... | Rechtliche Angaben zum Betreiber... |
| `legal.imprintPage.placeholder` | This is a placeholder for the imprint... | Dies ist ein Platzhalter für den Impressum... |
| `legal.privacyPage.title` | Privacy Policy | Datenschutzerklärung |
| `legal.privacyPage.subtitle` | How we collect, use, and protect your data | Wie wir Ihre Daten erheben, verwenden und schützen |
| `legal.privacyPage.placeholder` | This is a placeholder for the privacy... | Dies ist ein Platzhalter für die Datenschutzerklärung... |
| `legal.termsPage.title` | Terms of Service | Allgemeine Geschäftsbedingungen |
| `legal.termsPage.subtitle` | Terms and conditions for using our services | Bedingungen für die Nutzung unserer Dienste |
| `legal.termsPage.placeholder` | This is a placeholder for the terms... | Dies ist ein Platzhalter für die AGB... |
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
| `unsavedChanges.title` | Unsaved Changes | Ungespeicherte Änderungen |
| `unsavedChanges.description` | You have unsaved changes. Are you sure you want to leave? Your changes will be lost. | Sie haben ungespeicherte Änderungen. Möchten Sie wirklich verlassen? Ihre Änderungen gehen verloren. |
| `unsavedChanges.continueEditing` | Continue Editing | Weiter bearbeiten |
| `unsavedChanges.discard` | Discard Changes | Änderungen verwerfen |
| `unsavedChanges.save` | Save Changes | Änderungen speichern |
| `filters.dateRange` | Date Range | Zeitraum |
| `filters.selected` | {{count}} selected | {{count}} ausgewählt |
| `forceReauth.title` | Session Expired | Sitzung abgelaufen |
| `forceReauth.description` | Your permissions were changed. Please log in again. | Ihre Berechtigungen wurden geändert. Bitte melden Sie sich erneut an. |

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
| `register.subtitle` | Enter your details to create a new account | Geben Sie Ihre Daten ein, um ein neues Konto zu erstellen |
| `register.firstName` | First name | Vorname |
| `register.lastName` | Last name | Nachname |
| `register.name` | Full Name | Vollständiger Name |
| `register.confirmPassword` | Confirm Password | Passwort bestätigen |
| `register.createAccount` | Create Account | Konto erstellen |
| `register.creating` | Creating account... | Wird erstellt... |
| `register.hasAccount` | Already have an account? | Bereits ein Konto? |
| `invite.title` | Accept Invitation | Einladung annehmen |
| `invite.subtitle` | You've been invited to join {{organization}} | Sie wurden eingeladen, {{organization}} beizutreten |
| `invite.setPassword` | Set your password to complete registration | Legen Sie Ihr Passwort fest, um die Registrierung abzuschließen |
| `invite.accept` | Accept Invitation | Einladung annehmen |
| `invite.accepting` | Accepting... | Wird angenommen... |
| `invite.expired` | This invitation has expired | Diese Einladung ist abgelaufen |
| `invite.invalid` | This invitation is invalid | Diese Einladung ist ungültig |
| `invite.revoked` | This invitation has been revoked | Diese Einladung wurde widerrufen |
| `invite.alreadyAccepted` | This invitation has already been accepted | Diese Einladung wurde bereits angenommen |
| `invite.loading` | Loading invitation... | Einladung wird geladen... |
| `invite.welcome` | Welcome, {{name}}! | Willkommen, {{name}}! |
| `invite.invitedBy` | You were invited by {{name}} | Sie wurden von {{name}} eingeladen |
| `invite.permissions` | You will receive the following permissions: | Sie erhalten folgende Berechtigungen: |
| `invite.expiresAt` | Expires {{date}} | Läuft ab am {{date}} |
| `forgotPassword.title` | Forgot Password | Passwort vergessen |
| `forgotPassword.subtitle` | Enter your email address and we'll send you a reset code | Geben Sie Ihre E-Mail-Adresse ein und wir senden Ihnen einen Code zum Zurücksetzen |
| `forgotPassword.email` | Email | E-Mail |
| `forgotPassword.sendLink` | Send Reset Code | Code senden |
| `forgotPassword.sending` | Sending... | Wird gesendet... |
| `forgotPassword.sent` | Reset code sent! | Code gesendet! |
| `forgotPassword.sentMessage` | Check your email for the password reset code | Überprüfen Sie Ihre E-Mails für den Code zum Zurücksetzen des Passworts |
| `forgotPassword.backToLogin` | Back to Sign In | Zurück zur Anmeldung |
| `forgotPassword.enterCode` | Enter Reset Code | Code eingeben |
| `forgotPassword.codeDescription` | Enter the code we sent to your email address | Geben Sie den Code ein, den wir an Ihre E-Mail-Adresse gesendet haben |
| `forgotPassword.code` | Reset Code | Reset-Code |
| `resetPassword.title` | Reset Password | Passwort zurücksetzen |
| `resetPassword.subtitle` | Enter your new password | Geben Sie Ihr neues Passwort ein |
| `resetPassword.newPassword` | New Password | Neues Passwort |
| `resetPassword.confirmPassword` | Confirm Password | Passwort bestätigen |
| `resetPassword.reset` | Reset Password | Passwort zurücksetzen |
| `resetPassword.resetting` | Resetting... | Wird zurückgesetzt... |
| `resetPassword.success` | Password reset successful! | Passwort erfolgreich zurückgesetzt! |
| `resetPassword.successMessage` | You can now sign in with your new password | Sie können sich jetzt mit Ihrem neuen Passwort anmelden |
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
| `password.requirements` | Password requirements | Passwortanforderungen |
| `password.minLength` | At least 12 characters | Mindestens 12 Zeichen |
| `password.uppercase` | One uppercase letter | Ein Großbuchstabe |
| `password.lowercase` | One lowercase letter | Ein Kleinbuchstabe |
| `password.digit` | One number | Eine Zahl |
| `password.special` | One special character | Ein Sonderzeichen |

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
| `fields.emailVerified` | Email Verified | E-Mail verifiziert |
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
| `emailVerified.verified` | Verified | Verifiziert |
| `emailVerified.pending` | Pending | Ausstehend |
| `search.placeholder` | Search users... | Benutzer suchen... |
| `invite.description` | Send an invitation to join the team | Senden Sie eine Einladung zum Team |
| `invite.submit` | Send Invitation | Einladung senden |
| `permissions.title` | Manage Permissions | Berechtigungen verwalten |
| `permissions.success` | Permissions updated successfully | Berechtigungen erfolgreich aktualisiert |
| `permissions.none` | No permissions assigned | Keine Berechtigungen zugewiesen |
| `actions.permissions` | Permissions | Berechtigungen |
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
| `filters.accountLock` | Account Lock | Kontosperre |
| `filters.locked` | Locked | Gesperrt |
| `filters.unlocked` | Unlocked | Entsperrt |
| `filters.mfa` | MFA | MFA |
| `filters.mfaEnabled` | MFA Enabled | MFA aktiviert |
| `filters.mfaDisabled` | MFA Disabled | MFA deaktiviert |
| `filters.showAnonymized` | Show Anonymized | Anonymisierte anzeigen |
| `tabs.users` | Users | Benutzer |
| `tabs.invitations` | Invitations | Einladungen |
| `fields.security` | Security | Sicherheit |
| `security.mfaEnabled` | Two-factor authentication is enabled | Zwei-Faktor-Authentifizierung ist aktiviert |
| `security.mfaDisabled` | Two-factor authentication is not enabled | Zwei-Faktor-Authentifizierung ist nicht aktiviert |
| `security.locked` | Locked | Gesperrt |
| `security.accountLocked` | Account is temporarily locked due to failed login attempts | Konto ist aufgrund fehlgeschlagener Anmeldeversuche vorübergehend gesperrt |
| `security.lockedUntil` | Locked until | Gesperrt bis |
| `security.anonymized` | Anonymized | Anonymisiert |
| `security.userAnonymized` | User data has been anonymized (GDPR) | Benutzerdaten wurden anonymisiert (DSGVO) |
| `security.failedAttempts` | Failed Login Attempts | Fehlgeschlagene Anmeldeversuche |
| `invites.title` | Invitations | Einladungen |
| `invites.search` | Search invitations... | Einladungen suchen... |
| `invites.empty.title` | No invitations | Keine Einladungen |
| `invites.empty.description` | Invite users to get started | Laden Sie Benutzer ein, um loszulegen |
| `invites.status.pending` | Pending | Ausstehend |
| `invites.status.accepted` | Accepted | Angenommen |
| `invites.status.expired` | Expired | Abgelaufen |
| `invites.status.revoked` | Revoked | Widerrufen |
| `invites.filters.showExpired` | Show Expired | Abgelaufene anzeigen |
| `invites.filters.showRevoked` | Show Revoked | Widerrufene anzeigen |
| `invites.fields.email` | Email | E-Mail |
| `invites.fields.name` | Name | Name |
| `invites.fields.status` | Status | Status |
| `invites.fields.expiresAt` | Expires | Läuft ab |
| `invites.fields.createdAt` | Invited | Eingeladen |
| `invites.fields.invitedBy` | Invited by | Eingeladen von |
| `invites.fields.acceptedAt` | Accepted at | Angenommen am |
| `invites.fields.revokedAt` | Revoked at | Widerrufen am |
| `invites.fields.resentAt` | Last resent | Zuletzt erneut gesendet |
| `invites.actions.resend` | Resend | Erneut senden |
| `invites.actions.revoke` | Revoke | Widerrufen |
| `invites.actions.viewDetails` | View details | Details anzeigen |
| `invites.resend.success` | Invitation resent successfully | Einladung erfolgreich erneut gesendet |
| `invites.resend.cooldown` | Please wait before resending | Bitte warten Sie, bevor Sie erneut senden |
| `invites.revoke.confirm.title` | Revoke invitation? | Einladung widerrufen? |
| `invites.revoke.confirm.description` | This will invalidate the invitation link... | Dies macht den Einladungslink ungültig... |
| `invites.revoke.success` | Invitation revoked | Einladung widerrufen |
| `invites.details.title` | Invitation Details | Einladungsdetails |
| `invites.details.permissions` | Permissions | Berechtigungen |
| `invites.edit.title` | Edit Invitation | Einladung bearbeiten |
| `invites.edit.description` | Update the invitation details and permissions | Aktualisieren Sie die Einladungsdetails und Berechtigungen |
| `invites.edit.success` | Invitation updated successfully | Einladung erfolgreich aktualisiert |
| `admin.actions.title` | Admin Actions | Admin-Aktionen |
| `admin.sessions.sectionTitle` | Active Sessions | Aktive Sitzungen |
| `admin.sessions.title` | Sessions | Sitzungen |
| `admin.sessions.empty` | No active sessions | Keine aktiven Sitzungen |
| `admin.sessions.revokeAll` | Revoke All | Alle widerrufen |
| `admin.sessions.revokeSingle` | Revoke session | Sitzung widerrufen |
| `admin.sessions.revokeAllConfirm.title` | Revoke all sessions? | Alle Sitzungen widerrufen? |
| `admin.sessions.revokeAllConfirm.description` | This will log the user out of all devices... | Der Benutzer wird von allen Geräten abgemeldet... |
| `admin.mfa.reset` | Reset MFA | MFA zurücksetzen |
| `admin.mfa.confirmTitle` | Reset MFA? | MFA zurücksetzen? |
| `admin.mfa.confirmDescription` | This will disable two-factor authentication for {{name}}... | Dies deaktiviert die Zwei-Faktor-Authentifizierung für {{name}}... |
| `admin.mfa.resetSuccess` | MFA has been reset successfully | MFA wurde erfolgreich zurückgesetzt |
| `admin.mfa.resetError` | Failed to reset MFA | MFA konnte nicht zurückgesetzt werden |
| `admin.unlock.action` | Unlock Account | Konto entsperren |
| `admin.unlock.button` | Unlock Account | Konto entsperren |
| `admin.unlock.confirmTitle` | Unlock account? | Konto entsperren? |
| `admin.unlock.confirmDescription` | This will unlock {{name}}'s account... | Dies entsperrt das Konto von {{name}}... |
| `admin.unlock.success` | Account has been unlocked | Konto wurde entsperrt |
| `admin.unlock.error` | Failed to unlock account | Konto konnte nicht entsperrt werden |
| `admin.deactivate.action` | Deactivate | Deaktivieren |
| `admin.deactivate.confirmTitle` | Deactivate user? | Benutzer deaktivieren? |
| `admin.deactivate.confirmDescription` | {{name}} will no longer be able to access the system... | {{name}} kann nicht mehr auf das System zugreifen... |
| `admin.deactivate.success` | User has been deactivated | Benutzer wurde deaktiviert |
| `admin.deactivate.error` | Failed to deactivate user | Benutzer konnte nicht deaktiviert werden |
| `admin.activate.action` | Activate | Aktivieren |
| `admin.activate.confirmTitle` | Activate user? | Benutzer aktivieren? |
| `admin.activate.confirmDescription` | {{name}} will be able to access the system again... | {{name}} kann wieder auf das System zugreifen... |
| `admin.activate.success` | User has been activated | Benutzer wurde aktiviert |
| `admin.activate.error` | Failed to activate user | Benutzer konnte nicht aktiviert werden |
| `admin.anonymize.action` | Anonymize (GDPR) | Anonymisieren (DSGVO) |
| `admin.anonymize.button` | Anonymize (GDPR) | Anonymisieren (DSGVO) |
| `admin.anonymize.confirmTitle` | Anonymize user? | Benutzer anonymisieren? |
| `admin.anonymize.confirmDescription` | This will permanently remove all personal data for {{name}}... | Alle personenbezogenen Daten von {{name}} werden dauerhaft gelöscht... |
| `admin.anonymize.placeholder` | Type email to confirm | E-Mail zur Bestätigung eingeben |
| `admin.anonymize.success` | User data has been anonymized | Benutzerdaten wurden anonymisiert |
| `admin.anonymize.error` | Failed to anonymize user | Benutzer konnte nicht anonymisiert werden |
| `admin.anonymizedNoActions` | No actions available for anonymized users | Keine Aktionen für anonymisierte Benutzer verfügbar |

---

### auditLogs.json

| Key | EN | DE |
|-----|----|----|
| `title` | Audit Logs | Audit-Protokolle |
| `subtitle` | View system activity and security events | Systemaktivitäten und Sicherheitsereignisse anzeigen |
| `searchPlaceholder` | Search logs... | Protokolle durchsuchen... |
| `fields.time` | Time | Zeit |
| `fields.user` | User | Benutzer |
| `fields.targetUser` | Target User | Zielbenutzer |
| `fields.action` | Action | Aktion |
| `fields.entity` | Entity | Entität |
| `fields.ipAddress` | IP Address | IP-Adresse |
| `fields.userAgent` | User Agent | User Agent |
| `fields.details` | Details | Details |
| `filters.action` | Action | Aktion |
| `filters.user` | User | Benutzer |
| `filters.dateRange` | Date Range | Zeitraum |
| `filters.entityType` | Entity Type | Entitätstyp |
| `details.title` | Audit Log Details | Audit-Protokoll Details |
| `details.description` | View detailed information about this event | Detaillierte Informationen zu diesem Ereignis anzeigen |
| `details.type` | Type | Typ |
| `system` | System | System |
| `empty.title` | No audit logs yet | Noch keine Audit-Protokolle |
| `empty.description` | Audit logs will appear here as system activity occurs | Audit-Protokolle werden hier angezeigt, sobald Systemaktivitäten auftreten |

---

### settings.json

| Key | EN | DE |
|-----|----|----|
| `title` | Settings | Einstellungen |
| `description` | Manage your account settings and preferences | Verwalten Sie Ihre Kontoeinstellungen und Präferenzen |
| `tabs.security` | Security | Sicherheit |
| `tabs.language` | Language | Sprache |
| `security.title` | Security Settings | Sicherheitseinstellungen |
| `security.description` | Manage your account security, two-factor authentication, and active sessions | Verwalten Sie Ihre Kontosicherheit, Zwei-Faktor-Authentifizierung und aktive Sitzungen |
| `language.title` | Language Preference | Spracheinstellung |
| `language.description` | Choose your preferred language for the interface and emails | Wählen Sie Ihre bevorzugte Sprache für die Benutzeroberfläche und E-Mails |
| `language.select` | Select language | Sprache auswählen |
| `language.current` | Current language | Aktuelle Sprache |
| `language.saved` | Language preference saved | Spracheinstellung gespeichert |

---

### mfa.json

| Key | EN | DE |
|-----|----|----|
| `title` | Two-Factor Authentication | Zwei-Faktor-Authentifizierung |
| `description` | Add an extra layer of security to your account | Schuetzen Sie Ihr Konto mit einer zusaetzlichen Sicherheitsebene |
| `status.enabled` | Enabled | Aktiviert |
| `status.disabled` | Disabled | Deaktiviert |
| `enable.button` | Enable 2FA | 2FA aktivieren |
| `enable.description` | Protect your account with two-factor authentication | Schuetzen Sie Ihr Konto mit Zwei-Faktor-Authentifizierung |
| `setup.title` | Set Up Two-Factor Authentication | Zwei-Faktor-Authentifizierung einrichten |
| `setup.description` | Use an authenticator app to generate verification codes | Verwenden Sie eine Authenticator-App, um Verifizierungscodes zu generieren |
| `setup.step1.title` | Step 1: Install an Authenticator App | Schritt 1: Authenticator-App installieren |
| `setup.step2.title` | Step 2: Scan QR Code | Schritt 2: QR-Code scannen |
| `setup.step3.title` | Step 3: Verify | Schritt 3: Verifizieren |
| `setup.manualEntry.title` | Can't scan? | Scannen nicht moeglich? |
| `setup.manualEntry.button` | Enter code manually | Code manuell eingeben |
| `setup.cancel` | Cancel Setup | Einrichtung abbrechen |
| `confirm.title` | Save Your Backup Codes | Backup-Codes speichern |
| `confirm.description` | These codes can be used to access your account if you lose your authenticator device | Diese Codes koennen verwendet werden, um auf Ihr Konto zuzugreifen, wenn Sie Ihr Authenticator-Geraet verlieren |
| `confirm.warning` | Store these codes in a safe place. You won't be able to see them again! | Bewahren Sie diese Codes an einem sicheren Ort auf. Sie werden sie nicht erneut sehen koennen! |
| `confirm.downloadButton` | Download Codes | Codes herunterladen |
| `confirm.copyButton` | Copy Codes | Codes kopieren |
| `confirm.continueButton` | I've saved my codes | Ich habe meine Codes gespeichert |
| `verify.title` | Two-Factor Authentication | Zwei-Faktor-Authentifizierung |
| `verify.description` | Enter the 6-digit code from your authenticator app | Geben Sie den 6-stelligen Code aus Ihrer Authenticator-App ein |
| `verify.button` | Verify | Verifizieren |
| `verify.useBackupCode` | Use a backup code instead | Stattdessen einen Backup-Code verwenden |
| `disable.title` | Disable Two-Factor Authentication | Zwei-Faktor-Authentifizierung deaktivieren |
| `disable.description` | This will make your account less secure. Are you sure? | Dies macht Ihr Konto weniger sicher. Sind Sie sicher? |
| `disable.button` | Disable 2FA | 2FA deaktivieren |
| `disable.success` | Two-factor authentication disabled | Zwei-Faktor-Authentifizierung deaktiviert |
| `backupCodes.title` | Backup Codes | Backup-Codes |
| `backupCodes.regenerate` | Regenerate Codes | Codes neu generieren |
| `errors.codeInvalid` | Invalid verification code. Please try again. | Ungueltiger Verifizierungscode. Bitte versuchen Sie es erneut. |
| `errors.tokenInvalid` | Invalid or expired authentication token. Please log in again. | Ungueltiges oder abgelaufenes Authentifizierungstoken. Bitte erneut anmelden. |
| `errors.tokenExpired` | Authentication token has expired. Please log in again. | Authentifizierungstoken abgelaufen. Bitte erneut anmelden. |
| `errors.setupFailed` | Failed to start 2FA setup. Please try again. | 2FA-Einrichtung fehlgeschlagen. Bitte versuchen Sie es erneut. |

---

### sessions.json

| Key | EN | DE |
|-----|----|----|
| `title` | Active Sessions | Aktive Sitzungen |
| `description` | Manage your active sessions across devices | Verwalten Sie Ihre aktiven Sitzungen auf verschiedenen Geraeten |
| `current` | Current Session | Aktuelle Sitzung |
| `trusted` | Trusted | Vertrauenswuerdig |
| `lastActive` | Last active | Zuletzt aktiv |
| `location` | Location | Standort |
| `created` | Created | Erstellt |
| `empty` | No other active sessions | Keine weiteren aktiven Sitzungen |
| `loading` | Loading sessions... | Sitzungen werden geladen... |
| `details.title` | Session Details | Sitzungsdetails |
| `details.description` | View detailed information about this session | Detaillierte Informationen zu dieser Sitzung anzeigen |
| `details.deviceInfo` | Device Information | Geraeteinformationen |
| `details.browser` | Browser | Browser |
| `details.os` | Operating System | Betriebssystem |
| `details.deviceType` | Device Type | Geraetetyp |
| `details.location` | Location | Standort |
| `details.ipAddress` | IP Address | IP-Adresse |
| `details.locationLabel` | Location | Standort |
| `details.timeline` | Timeline | Zeitverlauf |
| `revoke.button` | Log Out | Abmelden |
| `revoke.title` | Log Out Device | Geraet abmelden |
| `revoke.description` | This will end the session on this device... | Dies beendet die Sitzung auf diesem Geraet... |
| `revoke.success` | Session ended successfully | Sitzung erfolgreich beendet |
| `revokeAll.button` | Log Out All Other Devices | Alle anderen Geraete abmelden |
| `revokeAll.title` | Log Out All Other Devices | Alle anderen Geraete abmelden |
| `revokeAll.description` | This will end all sessions except your current one... | Dies beendet alle Sitzungen ausser Ihrer aktuellen... |
| `revokeAll.success` | Successfully logged out {{count}} session(s) | {{count}} Sitzung(en) erfolgreich abgemeldet |
| `trust.button` | Trust Device | Geraet vertrauen |
| `trust.title` | Trust This Device | Diesem Geraet vertrauen |
| `trust.description` | Trusted devices may have reduced security checks | Vertrauenswuerdige Geraete haben moeglicherweise reduzierte Sicherheitspruefungen |
| `trust.success` | Device marked as trusted | Geraet als vertrauenswuerdig markiert |
| `rename.button` | Rename | Umbenennen |
| `rename.title` | Rename Session | Sitzung umbenennen |
| `rename.placeholder` | Enter a name for this session | Geben Sie einen Namen fuer diese Sitzung ein |
| `rename.success` | Session renamed successfully | Sitzung erfolgreich umbenannt |
| `errors.loadFailed` | Failed to load sessions | Sitzungen konnten nicht geladen werden |
| `errors.revokeFailed` | Failed to end session | Sitzung konnte nicht beendet werden |
| `errors.cannotRevokeCurrent` | Cannot end your current session | Die aktuelle Sitzung kann nicht beendet werden |

---

### errors.json

#### Error Codes (Backend API Errors)

| Key | EN | DE |
|-----|----|----|
| `codes.UNKNOWN_ERROR` | An unknown error occurred. Please try again. | Ein unbekannter Fehler ist aufgetreten. Bitte versuchen Sie es erneut. |
| `codes.RATE_LIMIT_EXCEEDED` | Too many requests. Please slow down. | Zu viele Anfragen. Bitte langsamer. |
| `codes.AUTH_INVALID_CREDENTIALS` | Invalid email or password | Ungültige E-Mail oder Passwort |
| `codes.AUTH_USER_INACTIVE` | User account is inactive | Benutzerkonto ist deaktiviert |
| `codes.AUTH_TOKEN_EXPIRED` | Session expired | Sitzung abgelaufen |
| `codes.AUTH_REFRESH_TOKEN_INVALID` | Session invalid | Sitzung ungültig |
| `codes.AUTH_REGISTRATION_CLOSED` | Registration closed. Please contact an administrator. | Registrierung geschlossen. Kontaktieren Sie einen Administrator. |
| `codes.AUTH_EMAIL_EXISTS` | Email is already in use | E-Mail wird bereits verwendet |
| `codes.AUTH_PASSWORD_TOO_WEAK` | Password does not meet requirements | Passwort erfüllt die Anforderungen nicht |
| `codes.AUTH_TOO_MANY_ATTEMPTS` | Too many attempts. Please wait 15 minutes. | Zu viele Versuche. Bitte 15 Minuten warten. |
| `codes.AUTH_FORCE_REAUTH` | Your session has been invalidated. Please sign in again. | Ihre Sitzung wurde ungültig. Bitte melden Sie sich erneut an. |
| `codes.AUTH_INVITE_EXPIRED` | Invitation expired. Please request a new one. | Einladung abgelaufen. Bitte neue anfordern. |
| `codes.AUTH_INVITE_INVALID` | Invalid invitation link | Ungültiger Einladungslink |
| `codes.PASSWORD_RESET_TOKEN_INVALID` | Invalid password reset link. Please request a new one. | Ungültiger Passwort-Reset-Link. Bitte fordern Sie einen neuen an. |
| `codes.PASSWORD_RESET_TOKEN_EXPIRED` | Password reset link has expired. Please request a new one. | Der Passwort-Reset-Link ist abgelaufen. Bitte fordern Sie einen neuen an. |
| `codes.AUTH_USER_NOT_FOUND` | No account found with this email address. | Kein Konto mit dieser E-Mail-Adresse gefunden. |
| `codes.SYSTEM_USER_NOT_FOUND` | User not found | Benutzer nicht gefunden |
| `codes.SYSTEM_PERMISSION_NOT_FOUND` | Permission not found | Berechtigung nicht gefunden |
| `codes.SYSTEM_LAST_PERMISSION_HOLDER` | Cannot remove permission '{{permission}}' from the last user who holds it. At least one user must have 'system:users:update' and 'system:users:read' permissions. | Die Berechtigung '{{permission}}' kann nicht vom letzten Benutzer entfernt werden. Mindestens ein Benutzer muss die Berechtigungen 'system:users:update' und 'system:users:read' besitzen. |
| `codes.SYSTEM_CANNOT_DELETE_SELF` | Cannot delete yourself | Kann sich selbst nicht löschen |
| `codes.SYSTEM_USER_ANONYMIZED` | This user has been anonymized and cannot be modified | Dieser Benutzer wurde anonymisiert und kann nicht bearbeitet werden |
| `codes.SYSTEM_FORBIDDEN` | No permission for this action | Keine Berechtigung für diese Aktion |
| `codes.INVITE_NOT_EDITABLE` | This invitation can no longer be edited | Diese Einladung kann nicht mehr bearbeitet werden |
| `codes.INTERNAL_ERROR` | An internal error occurred. Please try again later. | Ein interner Fehler ist aufgetreten. Bitte versuchen Sie es später erneut. |
| `codes.VALIDATION_REQUIRED` | This field is required | Dieses Feld ist erforderlich |
| `codes.VALIDATION_INVALID_FORMAT` | Invalid format | Ungültiges Format |
| `codes.VALIDATION_MIN_LENGTH` | Minimum {{min}} characters required | Mindestens {{min}} Zeichen erforderlich |
| `codes.VALIDATION_MAX_LENGTH` | Maximum {{max}} characters allowed | Maximal {{max}} Zeichen erlaubt |

#### General Errors

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

**Last Updated:** 2025-12-31

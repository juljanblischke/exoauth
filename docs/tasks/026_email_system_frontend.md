# Task 026: Email System Frontend

## 1. Übersicht

**Was wird gebaut?**
Frontend für das Email-System mit Provider-Management, Konfiguration, Email-Logs, Dead Letter Queue und Announcements.

**Warum?**
- Admins brauchen UI um Email-Provider zu konfigurieren (statt appsettings)
- Email-Historie muss einsehbar sein für Debugging
- DLQ-Management für fehlgeschlagene Emails
- Announcements an User senden können

## 2. User Experience / Anforderungen

### User Stories
- Als Admin möchte ich Email-Provider konfigurieren können, damit Emails versendet werden
- Als Admin möchte ich die Provider-Priorität per Drag&Drop ändern können
- Als Admin möchte ich Email-Logs einsehen können, um Zustellprobleme zu debuggen
- Als Admin möchte ich fehlgeschlagene Emails aus der DLQ erneut senden können
- Als Admin möchte ich Ankündigungen an User senden können

### UI/UX Beschreibung

#### Route: `/system/email`
Single Page mit **Tabs** Component:
- **Providers** Tab (requires `email:providers:read`)
- **Configuration** Tab (requires `email:config:read`)
- **Logs** Tab (requires `email:logs:read`)
- **DLQ** Tab (requires `email:dlq:manage`)
- **Announcements** Tab (requires `email:announcements:read`)

Tabs sind nur sichtbar wenn User die entsprechende Permission hat.

#### Providers Tab
- DataTable mit Provider-Liste (sortiert by Priority)
- Drag & Drop zum Reordern
- Row Actions: Edit, Delete, Test, Reset Circuit Breaker
- Status-Badges für Circuit Breaker State
- Empty State mit Setup Guide wenn keine Provider konfiguriert
- "Add Provider" Button öffnet Modal

#### Configuration Tab
- Form mit allen EmailConfiguration Settings
- Sections: Retry Settings, Circuit Breaker Settings, DLQ Settings, General
- Save Button

#### Logs Tab
- DataTable wie Audit Logs
- Filters: Status, Template, Date Range, Recipient
- Details Sheet mit Email-Details
- Read-only (keine Actions)

#### DLQ Tab
- DataTable mit Emails in DLQ
- Row Actions: Retry, Delete
- Bulk Action: "Retry All" Button
- Empty State wenn DLQ leer

#### Announcements Tab
- DataTable mit Announcements
- Status Badges (Draft, Sending, Sent, PartiallyFailed)
- Row Actions: Edit (nur Draft), Send (nur Draft), Delete (nur Draft), View Details
- "Create Announcement" Button
- Create/Edit Modal mit Rich Text Editor (TipTap)

### Akzeptanzkriterien
- [ ] Alle Tabs funktionieren mit korrekten Permissions
- [ ] Provider CRUD funktioniert
- [ ] Provider Drag & Drop Reorder funktioniert
- [ ] Provider Test sendet Test-Email an current user
- [ ] Dynamic Provider Config Form (unterschiedliche Felder je Type)
- [ ] Password/API Key Felder mit reveal toggle
- [ ] Email Logs mit Filtering und Details Sheet
- [ ] DLQ Retry (single + all) funktioniert
- [ ] Announcements CRUD funktioniert
- [ ] Rich Text Editor für Announcement Body
- [ ] User Selection für SelectedUsers Target
- [ ] Announcement Preview Modal
- [ ] Empty States mit Setup Guide
- [ ] Mobile responsive (Cards statt Table)
- [ ] i18n EN + DE

### Edge Cases / Error Handling
- Was wenn kein Provider konfiguriert? → Setup Guide anzeigen
- Was wenn Provider Test fehlschlägt? → Error Toast mit Details
- Was wenn DLQ leer? → Empty State
- Was wenn Announcement keine Recipients hat? → Validation Error

## 3. API Integration

### Email Providers
| Endpoint | Method | Hook Name |
|----------|--------|-----------|
| `/api/system/email/providers` | GET | useEmailProviders |
| `/api/system/email/providers/{id}` | GET | useEmailProvider |
| `/api/system/email/providers` | POST | useCreateEmailProvider |
| `/api/system/email/providers/{id}` | PUT | useUpdateEmailProvider |
| `/api/system/email/providers/{id}` | DELETE | useDeleteEmailProvider |
| `/api/system/email/providers/{id}/test` | POST | useTestEmailProvider |
| `/api/system/email/providers/{id}/reset-circuit-breaker` | POST | useResetCircuitBreaker |
| `/api/system/email/providers/reorder` | PUT | useReorderEmailProviders |

### Email Configuration
| Endpoint | Method | Hook Name |
|----------|--------|-----------|
| `/api/system/email/configuration` | GET | useEmailConfiguration |
| `/api/system/email/configuration` | PUT | useUpdateEmailConfiguration |

### Email Logs
| Endpoint | Method | Hook Name |
|----------|--------|-----------|
| `/api/system/email/logs` | GET | useEmailLogs |
| `/api/system/email/logs/{id}` | GET | useEmailLog |
| `/api/system/email/logs/filters` | GET | useEmailLogFilters |

### Dead Letter Queue
| Endpoint | Method | Hook Name |
|----------|--------|-----------|
| `/api/system/email/dlq` | GET | useDlqEmails |
| `/api/system/email/dlq/{id}/retry` | POST | useRetryDlqEmail |
| `/api/system/email/dlq/retry-all` | POST | useRetryAllDlqEmails |
| `/api/system/email/dlq/{id}` | DELETE | useDeleteDlqEmail |

### Announcements
| Endpoint | Method | Hook Name |
|----------|--------|-----------|
| `/api/system/email/announcements` | GET | useAnnouncements |
| `/api/system/email/announcements/{id}` | GET | useAnnouncement |
| `/api/system/email/announcements` | POST | useCreateAnnouncement |
| `/api/system/email/announcements/{id}` | PUT | useUpdateAnnouncement |
| `/api/system/email/announcements/{id}` | DELETE | useDeleteAnnouncement |
| `/api/system/email/announcements/{id}/send` | POST | useSendAnnouncement |
| `/api/system/email/announcements/preview` | POST | usePreviewAnnouncement |

## 4. Komponenten Übersicht

### Neue Komponenten
| Komponente | Typ | Beschreibung |
|------------|-----|--------------|
| EmailPage | Page | Main page mit Tabs |
| EmailProvidersTab | Feature | Providers tab content |
| EmailConfigurationTab | Feature | Configuration tab content |
| EmailLogsTab | Feature | Logs tab content |
| EmailDlqTab | Feature | DLQ tab content |
| EmailAnnouncementsTab | Feature | Announcements tab content |
| ProviderCard | Feature | Provider in list (for drag) |
| ProviderFormModal | Feature | Create/Edit provider |
| ProviderConfigFields | Feature | Dynamic fields per provider type |
| ProviderTypeBadge | Feature | SMTP/SendGrid/etc badge |
| ProviderStatusBadge | Feature | Circuit breaker status |
| EmailLogDetailsSheet | Feature | Email log details |
| EmailStatusBadge | Feature | Queued/Sent/Failed/InDlq |
| AnnouncementFormModal | Feature | Create/Edit announcement |
| AnnouncementPreviewModal | Feature | Preview announcement |
| AnnouncementStatusBadge | Feature | Draft/Sending/Sent |
| AnnouncementTargetBadge | Feature | AllUsers/ByPermission/Selected |
| UserSelectModal | Feature | Select users for announcement |
| RichTextEditor | Shared | TipTap editor wrapper |
| SetupGuide | Feature | Empty state guide |

### Bestehende Komponenten nutzen
| Komponente | Woher |
|------------|-------|
| DataTable + alle subcomponents | @/components/shared/data-table |
| PageHeader | @/components/shared/layout |
| LoadingSpinner, EmptyState, ErrorState | @/components/shared/feedback |
| ConfirmDialog, TypeConfirmDialog | @/components/shared/feedback |
| FormModal, FormSheet | @/components/shared/form |
| PasswordInput | @/components/shared/form |
| DateRangePicker | @/components/shared/form |
| Tabs, TabsList, TabsTrigger, TabsContent | @/components/ui/tabs |
| Badge | @/components/ui/badge |
| Switch | @/components/ui/switch |
| Select | @/components/ui/select |

## 5. Files zu erstellen

### Feature Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| email-api.ts | `src/features/email/api/email-api.ts` | All API calls |
| index.ts (types) | `src/features/email/types/index.ts` | TypeScript types |
| use-email-providers.ts | `src/features/email/hooks/use-email-providers.ts` | List providers |
| use-email-provider.ts | `src/features/email/hooks/use-email-provider.ts` | Get single provider |
| use-create-email-provider.ts | `src/features/email/hooks/use-create-email-provider.ts` | Create provider |
| use-update-email-provider.ts | `src/features/email/hooks/use-update-email-provider.ts` | Update provider |
| use-delete-email-provider.ts | `src/features/email/hooks/use-delete-email-provider.ts` | Delete provider |
| use-test-email-provider.ts | `src/features/email/hooks/use-test-email-provider.ts` | Test provider |
| use-reset-circuit-breaker.ts | `src/features/email/hooks/use-reset-circuit-breaker.ts` | Reset CB |
| use-reorder-email-providers.ts | `src/features/email/hooks/use-reorder-email-providers.ts` | Reorder |
| use-email-configuration.ts | `src/features/email/hooks/use-email-configuration.ts` | Get config |
| use-update-email-configuration.ts | `src/features/email/hooks/use-update-email-configuration.ts` | Update config |
| use-email-logs.ts | `src/features/email/hooks/use-email-logs.ts` | List logs |
| use-email-log.ts | `src/features/email/hooks/use-email-log.ts` | Get single log |
| use-email-log-filters.ts | `src/features/email/hooks/use-email-log-filters.ts` | Filter options |
| use-dlq-emails.ts | `src/features/email/hooks/use-dlq-emails.ts` | List DLQ |
| use-retry-dlq-email.ts | `src/features/email/hooks/use-retry-dlq-email.ts` | Retry single |
| use-retry-all-dlq-emails.ts | `src/features/email/hooks/use-retry-all-dlq-emails.ts` | Retry all |
| use-delete-dlq-email.ts | `src/features/email/hooks/use-delete-dlq-email.ts` | Delete from DLQ |
| use-announcements.ts | `src/features/email/hooks/use-announcements.ts` | List announcements |
| use-announcement.ts | `src/features/email/hooks/use-announcement.ts` | Get single |
| use-create-announcement.ts | `src/features/email/hooks/use-create-announcement.ts` | Create |
| use-update-announcement.ts | `src/features/email/hooks/use-update-announcement.ts` | Update |
| use-delete-announcement.ts | `src/features/email/hooks/use-delete-announcement.ts` | Delete |
| use-send-announcement.ts | `src/features/email/hooks/use-send-announcement.ts` | Send |
| use-preview-announcement.ts | `src/features/email/hooks/use-preview-announcement.ts` | Preview |
| index.ts (hooks) | `src/features/email/hooks/index.ts` | Barrel export |
| email-providers-tab.tsx | `src/features/email/components/email-providers-tab.tsx` | Providers tab |
| email-providers-list.tsx | `src/features/email/components/email-providers-list.tsx` | Drag & drop list |
| provider-card.tsx | `src/features/email/components/provider-card.tsx` | Single provider card |
| provider-form-modal.tsx | `src/features/email/components/provider-form-modal.tsx` | Create/Edit modal |
| provider-config-fields.tsx | `src/features/email/components/provider-config-fields.tsx` | Dynamic fields |
| provider-type-badge.tsx | `src/features/email/components/provider-type-badge.tsx` | Type badge |
| provider-status-badge.tsx | `src/features/email/components/provider-status-badge.tsx` | CB status |
| email-configuration-tab.tsx | `src/features/email/components/email-configuration-tab.tsx` | Config tab |
| email-configuration-form.tsx | `src/features/email/components/email-configuration-form.tsx` | Config form |
| email-logs-tab.tsx | `src/features/email/components/email-logs-tab.tsx` | Logs tab |
| email-logs-table.tsx | `src/features/email/components/email-logs-table.tsx` | Logs table |
| email-logs-table-columns.tsx | `src/features/email/components/email-logs-table-columns.tsx` | Columns def |
| email-log-details-sheet.tsx | `src/features/email/components/email-log-details-sheet.tsx` | Details sheet |
| email-status-badge.tsx | `src/features/email/components/email-status-badge.tsx` | Status badge |
| email-dlq-tab.tsx | `src/features/email/components/email-dlq-tab.tsx` | DLQ tab |
| email-dlq-table.tsx | `src/features/email/components/email-dlq-table.tsx` | DLQ table |
| email-dlq-table-columns.tsx | `src/features/email/components/email-dlq-table-columns.tsx` | DLQ columns |
| email-announcements-tab.tsx | `src/features/email/components/email-announcements-tab.tsx` | Announcements tab |
| announcements-table.tsx | `src/features/email/components/announcements-table.tsx` | Announcements table |
| announcements-table-columns.tsx | `src/features/email/components/announcements-table-columns.tsx` | Columns def |
| announcement-form-modal.tsx | `src/features/email/components/announcement-form-modal.tsx` | Create/Edit modal |
| announcement-preview-modal.tsx | `src/features/email/components/announcement-preview-modal.tsx` | Preview |
| announcement-status-badge.tsx | `src/features/email/components/announcement-status-badge.tsx` | Status badge |
| announcement-target-badge.tsx | `src/features/email/components/announcement-target-badge.tsx` | Target badge |
| user-select-modal.tsx | `src/features/email/components/user-select-modal.tsx` | User selection |
| setup-guide.tsx | `src/features/email/components/setup-guide.tsx` | Empty state guide |
| index.ts (components) | `src/features/email/components/index.ts` | Barrel export |
| index.ts (feature) | `src/features/email/index.ts` | Feature barrel |

### Shared Component
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| rich-text-editor.tsx | `src/components/shared/form/rich-text-editor.tsx` | TipTap wrapper |

### Route Files
| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| email.tsx | `src/routes/email.tsx` | Email page |

## 6. Files zu ändern

| Datei | Was ändern? |
|-------|-------------|
| `src/app/router.tsx` | Add `/system/email` route |
| `src/config/navigation.ts` | Add Email to System section |
| `src/components/shared/form/index.ts` | Export RichTextEditor |
| `src/i18n/locales/en/email.json` | Create EN translations |
| `src/i18n/locales/de/email.json` | Create DE translations |
| `src/i18n/index.ts` | Register email namespace |

## 7. Neue Dependencies

### NPM Packages
| Package | Warum |
|---------|-------|
| `@tiptap/react` | Rich text editor core |
| `@tiptap/starter-kit` | Basic extensions (bold, italic, etc) |
| `@tiptap/extension-link` | Link support |
| `@tiptap/extension-image` | Image support |
| `@tiptap/extension-placeholder` | Placeholder text |
| `@dnd-kit/core` | Drag and drop |
| `@dnd-kit/sortable` | Sortable list |
| `@dnd-kit/utilities` | DnD utilities |

### Shadcn/UI Komponenten
| Komponente | Command |
|------------|---------|
| (none needed) | All already installed |

## 8. Implementation Reihenfolge

### Phase 1: Foundation
1. [x] **Types**: TypeScript interfaces for all entities
2. [x] **API**: email-api.ts with all endpoints
3. [x] **Route**: Basic email.tsx page with tabs structure
4. [x] **Navigation**: Add to sidebar under System
5. [x] **i18n**: Create email.json (EN + DE)

### Phase 2: Providers Tab
6. [x] **Hooks**: Provider hooks (CRUD + test + reset + reorder)
7. [x] **Components**: ProviderTypeBadge, ProviderStatusBadge
8. [x] **Components**: ProviderConfigFields (dynamic form)
9. [x] **Components**: ProviderFormModal (create/edit)
10. [x] **Components**: ProviderCard (for drag list)
11. [x] **Components**: EmailProvidersList (with reorder buttons)
12. [x] **Components**: SetupGuide (empty state in ProviderList)
13. [x] **Components**: EmailProvidersTab (ProviderList component)

### Phase 3: Configuration Tab
14. [x] **Hooks**: Configuration hooks (get + update)
15. [x] **Components**: EmailConfigurationForm
16. [x] **Components**: EmailConfigurationTab

### Phase 4: Logs Tab
17. [x] **Hooks**: Logs hooks (list + retry)
18. [x] **Components**: EmailStatusBadge
19. [x] **Components**: EmailLogsTableColumns
20. [x] **Components**: EmailLogDetailsSheet
21. [x] **Components**: EmailLogsTable
22. [x] **Components**: EmailLogsTab

### Phase 5: DLQ Tab
23. [x] **Hooks**: DLQ hooks (list + process + delete)
24. [x] **Components**: EmailDlqTableColumns
25. [x] **Components**: EmailDlqTable
26. [x] **Components**: EmailDlqTab

### Phase 6: Announcements Tab
27. [x] **Shared**: RichTextEditor component (TipTap)
28. [x] **Hooks**: Announcement hooks (CRUD + send)
29. [x] **Components**: AnnouncementStatusBadge, AnnouncementTargetBadge
30. [x] **Components**: UserSelectModal (with infinite scroll)
31. [x] **Components**: AnnouncementFormModal (with RichTextEditor)
32. [x] **Components**: AnnouncementDetailsSheet
33. [x] **Components**: AnnouncementsTableColumns
34. [x] **Components**: AnnouncementsTable
35. [x] **Components**: EmailAnnouncementsTab

### Phase 7: Polish & Testing
36. [x] **Fixes**: Permission strings (manage instead of write)
37. [x] **Fixes**: i18n missing key detection
38. [x] **Fixes**: Dropdown z-index in modals
39. [x] **i18n**: Complete EN + DE translations
40. [x] **Task File**: Update this file with created files
41. [ ] **Memory**: Update frontend_reference.md

## 9. Provider Config Fields

Dynamic fields based on EmailProviderType:

### SMTP
| Field | Type | Required |
|-------|------|----------|
| host | text | Yes |
| port | number | Yes |
| username | text | Yes |
| password | password (reveal toggle) | Yes |
| useSsl | switch | No |
| fromEmail | email | Yes |
| fromName | text | Yes |

### SendGrid
| Field | Type | Required |
|-------|------|----------|
| apiKey | password (reveal toggle) | Yes |
| fromEmail | email | Yes |
| fromName | text | Yes |

### Mailgun
| Field | Type | Required |
|-------|------|----------|
| apiKey | password (reveal toggle) | Yes |
| domain | text | Yes |
| region | select (EU/US) | Yes |
| fromEmail | email | Yes |
| fromName | text | Yes |

### Amazon SES
| Field | Type | Required |
|-------|------|----------|
| accessKey | password (reveal toggle) | Yes |
| secretKey | password (reveal toggle) | Yes |
| region | text | Yes |
| fromEmail | email | Yes |
| fromName | text | Yes |

### Resend
| Field | Type | Required |
|-------|------|----------|
| apiKey | password (reveal toggle) | Yes |
| fromEmail | email | Yes |
| fromName | text | Yes |

### Postmark
| Field | Type | Required |
|-------|------|----------|
| serverToken | password (reveal toggle) | Yes |
| fromEmail | email | Yes |
| fromName | text | Yes |

## 10. i18n Keys

### English (`email.json`)
```json
{
  "title": "Email System",
  "subtitle": "Manage email providers, view logs, and send announcements",
  "tabs": {
    "providers": "Providers",
    "configuration": "Configuration",
    "logs": "Logs",
    "dlq": "Dead Letter Queue",
    "announcements": "Announcements"
  },
  "providers": {
    "title": "Email Providers",
    "add": "Add Provider",
    "empty": {
      "title": "No Email Providers Configured",
      "description": "Set up at least one email provider to start sending emails."
    },
    "guide": {
      "title": "Getting Started",
      "step1": "Add your primary email provider (e.g., SendGrid, SMTP)",
      "step2": "Optionally add backup providers for failover",
      "step3": "Drag providers to set priority order",
      "step4": "Send a test email to verify configuration"
    },
    "form": {
      "create": "Add Email Provider",
      "edit": "Edit Email Provider",
      "name": "Provider Name",
      "namePlaceholder": "e.g., Primary SendGrid",
      "type": "Provider Type",
      "priority": "Priority",
      "enabled": "Enabled",
      "configuration": "Configuration"
    },
    "types": {
      "smtp": "SMTP",
      "sendgrid": "SendGrid",
      "mailgun": "Mailgun",
      "ses": "Amazon SES",
      "resend": "Resend",
      "postmark": "Postmark"
    },
    "fields": {
      "host": "SMTP Host",
      "port": "SMTP Port",
      "username": "Username",
      "password": "Password",
      "useSsl": "Use SSL/TLS",
      "apiKey": "API Key",
      "domain": "Domain",
      "region": "Region",
      "accessKey": "Access Key",
      "secretKey": "Secret Key",
      "serverToken": "Server Token",
      "fromEmail": "From Email",
      "fromName": "From Name"
    },
    "status": {
      "healthy": "Healthy",
      "circuitOpen": "Circuit Open",
      "disabled": "Disabled"
    },
    "actions": {
      "test": "Send Test Email",
      "testSuccess": "Test email sent successfully",
      "resetCircuitBreaker": "Reset Circuit Breaker",
      "resetSuccess": "Circuit breaker reset successfully"
    },
    "stats": {
      "totalSent": "Total Sent",
      "totalFailed": "Total Failed",
      "lastSuccess": "Last Success"
    },
    "delete": {
      "title": "Delete Provider",
      "description": "Are you sure you want to delete this email provider? This action cannot be undone."
    }
  },
  "configuration": {
    "title": "Email Configuration",
    "saved": "Configuration saved successfully",
    "sections": {
      "retry": "Retry Settings",
      "circuitBreaker": "Circuit Breaker Settings",
      "dlq": "Dead Letter Queue Settings",
      "general": "General Settings"
    },
    "fields": {
      "maxRetriesPerProvider": "Max Retries Per Provider",
      "initialRetryDelayMs": "Initial Retry Delay (ms)",
      "maxRetryDelayMs": "Max Retry Delay (ms)",
      "backoffMultiplier": "Backoff Multiplier",
      "circuitBreakerFailureThreshold": "Failure Threshold",
      "circuitBreakerWindowMinutes": "Failure Window (minutes)",
      "circuitBreakerOpenDurationMinutes": "Open Duration (minutes)",
      "autoRetryDlq": "Auto-Retry DLQ",
      "dlqRetryIntervalHours": "DLQ Retry Interval (hours)",
      "emailsEnabled": "Emails Enabled",
      "testMode": "Test Mode"
    },
    "hints": {
      "testMode": "In test mode, emails are logged but not actually sent"
    }
  },
  "logs": {
    "title": "Email Logs",
    "empty": {
      "title": "No Email Logs",
      "description": "Email logs will appear here once emails are sent."
    },
    "columns": {
      "recipient": "Recipient",
      "subject": "Subject",
      "template": "Template",
      "status": "Status",
      "provider": "Provider",
      "sentAt": "Sent At",
      "queuedAt": "Queued At"
    },
    "filters": {
      "status": "Status",
      "template": "Template",
      "dateRange": "Date Range"
    },
    "details": {
      "title": "Email Details",
      "recipientUser": "Recipient User",
      "recipientEmail": "Recipient Email",
      "language": "Language",
      "retryCount": "Retry Count",
      "lastError": "Last Error",
      "templateVariables": "Template Variables"
    },
    "status": {
      "queued": "Queued",
      "sending": "Sending",
      "sent": "Sent",
      "failed": "Failed",
      "inDlq": "In DLQ",
      "retriedFromDlq": "Retried from DLQ"
    }
  },
  "dlq": {
    "title": "Dead Letter Queue",
    "empty": {
      "title": "DLQ is Empty",
      "description": "No failed emails in the dead letter queue."
    },
    "retryAll": "Retry All",
    "retryAllConfirm": {
      "title": "Retry All Emails",
      "description": "Are you sure you want to retry all {{count}} emails in the DLQ?"
    },
    "retrySuccess": "Email queued for retry",
    "retryAllSuccess": "All emails queued for retry",
    "deleteSuccess": "Email removed from DLQ",
    "actions": {
      "retry": "Retry",
      "delete": "Give Up"
    },
    "delete": {
      "title": "Remove from DLQ",
      "description": "Are you sure you want to give up on this email? It will not be sent."
    }
  },
  "announcements": {
    "title": "Announcements",
    "create": "Create Announcement",
    "empty": {
      "title": "No Announcements",
      "description": "Create announcements to send emails to your users."
    },
    "form": {
      "create": "Create Announcement",
      "edit": "Edit Announcement",
      "subject": "Subject",
      "subjectPlaceholder": "Enter email subject",
      "body": "Email Body",
      "bodyPlaceholder": "Write your announcement here...",
      "target": "Recipients",
      "targetType": {
        "allUsers": "All Active Users",
        "byPermission": "Users with Permission",
        "selectedUsers": "Selected Users"
      },
      "selectPermission": "Select Permission",
      "selectUsers": "Select Users",
      "selectedCount": "{{count}} users selected"
    },
    "preview": {
      "title": "Preview Announcement",
      "subject": "Subject",
      "body": "Body"
    },
    "send": {
      "title": "Send Announcement",
      "description": "Are you sure you want to send this announcement to {{count}} recipients?",
      "success": "Announcement is being sent"
    },
    "status": {
      "draft": "Draft",
      "sending": "Sending",
      "sent": "Sent",
      "partiallyFailed": "Partially Failed"
    },
    "columns": {
      "subject": "Subject",
      "target": "Target",
      "recipients": "Recipients",
      "status": "Status",
      "sentAt": "Sent At",
      "createdAt": "Created At"
    },
    "stats": {
      "total": "Total",
      "sent": "Sent",
      "failed": "Failed"
    },
    "delete": {
      "title": "Delete Announcement",
      "description": "Are you sure you want to delete this draft announcement?"
    }
  },
  "userSelect": {
    "title": "Select Users",
    "search": "Search users...",
    "selected": "{{count}} selected",
    "noResults": "No users found",
    "loadMore": "Load more"
  }
}
```

### German (`email.json`)
```json
{
  "title": "E-Mail System",
  "subtitle": "E-Mail-Provider verwalten, Logs ansehen und Ankündigungen senden",
  "tabs": {
    "providers": "Provider",
    "configuration": "Konfiguration",
    "logs": "Logs",
    "dlq": "Dead Letter Queue",
    "announcements": "Ankündigungen"
  },
  "providers": {
    "title": "E-Mail Provider",
    "add": "Provider hinzufügen",
    "empty": {
      "title": "Keine E-Mail Provider konfiguriert",
      "description": "Richten Sie mindestens einen E-Mail-Provider ein, um E-Mails zu versenden."
    },
    "guide": {
      "title": "Erste Schritte",
      "step1": "Fügen Sie Ihren primären E-Mail-Provider hinzu (z.B. SendGrid, SMTP)",
      "step2": "Optional: Backup-Provider für Failover hinzufügen",
      "step3": "Provider per Drag & Drop sortieren",
      "step4": "Test-E-Mail senden zur Überprüfung"
    },
    "form": {
      "create": "E-Mail Provider hinzufügen",
      "edit": "E-Mail Provider bearbeiten",
      "name": "Provider Name",
      "namePlaceholder": "z.B. Primärer SendGrid",
      "type": "Provider Typ",
      "priority": "Priorität",
      "enabled": "Aktiviert",
      "configuration": "Konfiguration"
    },
    "types": {
      "smtp": "SMTP",
      "sendgrid": "SendGrid",
      "mailgun": "Mailgun",
      "ses": "Amazon SES",
      "resend": "Resend",
      "postmark": "Postmark"
    },
    "fields": {
      "host": "SMTP Host",
      "port": "SMTP Port",
      "username": "Benutzername",
      "password": "Passwort",
      "useSsl": "SSL/TLS verwenden",
      "apiKey": "API-Schlüssel",
      "domain": "Domain",
      "region": "Region",
      "accessKey": "Zugriffsschlüssel",
      "secretKey": "Geheimer Schlüssel",
      "serverToken": "Server Token",
      "fromEmail": "Absender E-Mail",
      "fromName": "Absender Name"
    },
    "status": {
      "healthy": "Gesund",
      "circuitOpen": "Circuit offen",
      "disabled": "Deaktiviert"
    },
    "actions": {
      "test": "Test-E-Mail senden",
      "testSuccess": "Test-E-Mail erfolgreich gesendet",
      "resetCircuitBreaker": "Circuit Breaker zurücksetzen",
      "resetSuccess": "Circuit Breaker erfolgreich zurückgesetzt"
    },
    "stats": {
      "totalSent": "Gesamt gesendet",
      "totalFailed": "Gesamt fehlgeschlagen",
      "lastSuccess": "Letzter Erfolg"
    },
    "delete": {
      "title": "Provider löschen",
      "description": "Sind Sie sicher, dass Sie diesen E-Mail-Provider löschen möchten? Diese Aktion kann nicht rückgängig gemacht werden."
    }
  },
  "configuration": {
    "title": "E-Mail Konfiguration",
    "saved": "Konfiguration erfolgreich gespeichert",
    "sections": {
      "retry": "Wiederholungs-Einstellungen",
      "circuitBreaker": "Circuit Breaker Einstellungen",
      "dlq": "Dead Letter Queue Einstellungen",
      "general": "Allgemeine Einstellungen"
    },
    "fields": {
      "maxRetriesPerProvider": "Max. Wiederholungen pro Provider",
      "initialRetryDelayMs": "Initiale Verzögerung (ms)",
      "maxRetryDelayMs": "Max. Verzögerung (ms)",
      "backoffMultiplier": "Backoff-Multiplikator",
      "circuitBreakerFailureThreshold": "Fehlerschwelle",
      "circuitBreakerWindowMinutes": "Fehlerfenster (Minuten)",
      "circuitBreakerOpenDurationMinutes": "Offen-Dauer (Minuten)",
      "autoRetryDlq": "DLQ Auto-Retry",
      "dlqRetryIntervalHours": "DLQ Retry-Intervall (Stunden)",
      "emailsEnabled": "E-Mails aktiviert",
      "testMode": "Testmodus"
    },
    "hints": {
      "testMode": "Im Testmodus werden E-Mails protokolliert aber nicht gesendet"
    }
  },
  "logs": {
    "title": "E-Mail Logs",
    "empty": {
      "title": "Keine E-Mail Logs",
      "description": "E-Mail-Logs erscheinen hier sobald E-Mails gesendet werden."
    },
    "columns": {
      "recipient": "Empfänger",
      "subject": "Betreff",
      "template": "Template",
      "status": "Status",
      "provider": "Provider",
      "sentAt": "Gesendet am",
      "queuedAt": "Eingereiht am"
    },
    "filters": {
      "status": "Status",
      "template": "Template",
      "dateRange": "Zeitraum"
    },
    "details": {
      "title": "E-Mail Details",
      "recipientUser": "Empfänger (User)",
      "recipientEmail": "Empfänger E-Mail",
      "language": "Sprache",
      "retryCount": "Wiederholungen",
      "lastError": "Letzter Fehler",
      "templateVariables": "Template-Variablen"
    },
    "status": {
      "queued": "Eingereiht",
      "sending": "Wird gesendet",
      "sent": "Gesendet",
      "failed": "Fehlgeschlagen",
      "inDlq": "In DLQ",
      "retriedFromDlq": "Aus DLQ wiederholt"
    }
  },
  "dlq": {
    "title": "Dead Letter Queue",
    "empty": {
      "title": "DLQ ist leer",
      "description": "Keine fehlgeschlagenen E-Mails in der Dead Letter Queue."
    },
    "retryAll": "Alle wiederholen",
    "retryAllConfirm": {
      "title": "Alle E-Mails wiederholen",
      "description": "Sind Sie sicher, dass Sie alle {{count}} E-Mails in der DLQ wiederholen möchten?"
    },
    "retrySuccess": "E-Mail zur Wiederholung eingereiht",
    "retryAllSuccess": "Alle E-Mails zur Wiederholung eingereiht",
    "deleteSuccess": "E-Mail aus DLQ entfernt",
    "actions": {
      "retry": "Wiederholen",
      "delete": "Aufgeben"
    },
    "delete": {
      "title": "Aus DLQ entfernen",
      "description": "Sind Sie sicher, dass Sie diese E-Mail aufgeben möchten? Sie wird nicht gesendet."
    }
  },
  "announcements": {
    "title": "Ankündigungen",
    "create": "Ankündigung erstellen",
    "empty": {
      "title": "Keine Ankündigungen",
      "description": "Erstellen Sie Ankündigungen, um E-Mails an Ihre Benutzer zu senden."
    },
    "form": {
      "create": "Ankündigung erstellen",
      "edit": "Ankündigung bearbeiten",
      "subject": "Betreff",
      "subjectPlaceholder": "E-Mail-Betreff eingeben",
      "body": "E-Mail Inhalt",
      "bodyPlaceholder": "Schreiben Sie Ihre Ankündigung hier...",
      "target": "Empfänger",
      "targetType": {
        "allUsers": "Alle aktiven Benutzer",
        "byPermission": "Benutzer mit Berechtigung",
        "selectedUsers": "Ausgewählte Benutzer"
      },
      "selectPermission": "Berechtigung auswählen",
      "selectUsers": "Benutzer auswählen",
      "selectedCount": "{{count}} Benutzer ausgewählt"
    },
    "preview": {
      "title": "Ankündigung Vorschau",
      "subject": "Betreff",
      "body": "Inhalt"
    },
    "send": {
      "title": "Ankündigung senden",
      "description": "Sind Sie sicher, dass Sie diese Ankündigung an {{count}} Empfänger senden möchten?",
      "success": "Ankündigung wird gesendet"
    },
    "status": {
      "draft": "Entwurf",
      "sending": "Wird gesendet",
      "sent": "Gesendet",
      "partiallyFailed": "Teilweise fehlgeschlagen"
    },
    "columns": {
      "subject": "Betreff",
      "target": "Ziel",
      "recipients": "Empfänger",
      "status": "Status",
      "sentAt": "Gesendet am",
      "createdAt": "Erstellt am"
    },
    "stats": {
      "total": "Gesamt",
      "sent": "Gesendet",
      "failed": "Fehlgeschlagen"
    },
    "delete": {
      "title": "Ankündigung löschen",
      "description": "Sind Sie sicher, dass Sie diesen Entwurf löschen möchten?"
    }
  },
  "userSelect": {
    "title": "Benutzer auswählen",
    "search": "Benutzer suchen...",
    "selected": "{{count}} ausgewählt",
    "noResults": "Keine Benutzer gefunden",
    "loadMore": "Mehr laden"
  }
}
```

## 11. Tests

### Component Tests
| Test Datei | Was wird getestet? |
|------------|-------------------|
| `src/features/email/__tests__/email-providers-tab.test.tsx` | Provider list, CRUD, reorder |
| `src/features/email/__tests__/email-configuration-tab.test.tsx` | Config form |
| `src/features/email/__tests__/email-logs-tab.test.tsx` | Logs table, filters |
| `src/features/email/__tests__/email-dlq-tab.test.tsx` | DLQ actions |
| `src/features/email/__tests__/email-announcements-tab.test.tsx` | Announcements CRUD |
| `src/features/email/__tests__/provider-form-modal.test.tsx` | Dynamic form fields |
| `src/features/email/__tests__/user-select-modal.test.tsx` | User selection |
| `src/components/shared/form/__tests__/rich-text-editor.test.tsx` | TipTap editor |

## 12. Nach Completion

- [ ] Alle Tests grün
- [ ] `frontend_reference.md` Memory aktualisiert
- [ ] `i18n_translations.md` Memory aktualisiert (neue Keys)
- [ ] TypeScript keine Errors
- [ ] Lint passed
- [ ] Mobile responsive getestet

## 13. Letzte Änderung

- **Datum:** 2026-01-08
- **Status:** ✅ Complete
- **Build:** Passing (yarn build + yarn lint)

### Created Files
- `frontend/src/features/email/types/index.ts`
- `frontend/src/features/email/api/email-api.ts`
- `frontend/src/features/email/hooks/` (all hooks - 24 files)
- `frontend/src/features/email/components/email-provider-type-badge.tsx`
- `frontend/src/features/email/components/email-provider-status-badge.tsx`
- `frontend/src/features/email/components/provider-card.tsx`
- `frontend/src/features/email/components/provider-form-dialog.tsx`
- `frontend/src/features/email/components/provider-list.tsx`
- `frontend/src/features/email/components/test-email-dialog.tsx`
- `frontend/src/features/email/components/email-configuration-form.tsx`
- `frontend/src/features/email/components/email-configuration-tab.tsx`
- `frontend/src/features/email/components/email-status-badge.tsx`
- `frontend/src/features/email/components/email-logs-table-columns.tsx`
- `frontend/src/features/email/components/email-log-details-sheet.tsx`
- `frontend/src/features/email/components/email-logs-table.tsx`
- `frontend/src/features/email/components/email-logs-tab.tsx`
- `frontend/src/features/email/components/email-dlq-table-columns.tsx`
- `frontend/src/features/email/components/email-dlq-table.tsx`
- `frontend/src/features/email/components/email-dlq-tab.tsx`
- `frontend/src/features/email/components/announcement-status-badge.tsx`
- `frontend/src/features/email/components/announcement-target-badge.tsx`
- `frontend/src/features/email/components/announcements-table-columns.tsx`
- `frontend/src/features/email/components/announcements-table.tsx`
- `frontend/src/features/email/components/announcement-form-modal.tsx`
- `frontend/src/features/email/components/announcement-details-sheet.tsx`
- `frontend/src/features/email/components/user-select-modal.tsx`
- `frontend/src/features/email/components/email-announcements-tab.tsx`
- `frontend/src/features/email/components/index.ts`
- `frontend/src/components/shared/form/rich-text-editor.tsx`
- `frontend/src/components/ui/card.tsx`
- `frontend/src/components/ui/textarea.tsx`
- `frontend/src/routes/email.tsx`
- `frontend/src/i18n/locales/en/email.json`
- `frontend/src/i18n/locales/de/email.json`

### Modified Files
- `frontend/src/routes/__root.tsx` (added email route)
- `frontend/src/config/navigation.ts` (added email navigation)
- `frontend/src/i18n/index.ts` (registered email namespace + missing key detection)
- `frontend/src/i18n/locales/en/navigation.json` (added email label + breadcrumb)
- `frontend/src/i18n/locales/de/navigation.json` (added email label + breadcrumb)
- `frontend/src/i18n/locales/en/common.json` (added labels.email)
- `frontend/src/i18n/locales/de/common.json` (added labels.email)
- `frontend/src/i18n/locales/en/validation.json` (added invalidEmail)
- `frontend/src/i18n/locales/de/validation.json` (added invalidEmail)
- `frontend/src/components/ui/select.tsx` (z-index fix for modals)
- `frontend/src/components/shared/layout/breadcrumbs.tsx` (added email route)
- `frontend/src/components/shared/form/index.ts` (export RichTextEditor)

### Key Fixes Applied
1. **Permission strings**: Changed from `email:*:write` to `email:*:manage` to match backend
2. **i18n debugging**: Added `saveMissing`, `missingKeyHandler`, `parseMissingKeyHandler` for dev mode
3. **Dropdown z-index**: Changed SelectContent from `z-50` to `z-[100]` to appear above modals
4. **Breadcrumb routing**: Added email to routeLabels map
5. **Complete translations**: EN + DE with all provider, config, logs, DLQ, announcements keys

# ExoAuth - Central Authentication System

## Project Overview
ExoAuth is a self-hosted, multi-tenant authentication-as-a-service platform (similar to Auth0, Clerk, Supabase Auth) that allows you to manage authentication for multiple projects from a central system.

## Core Concept
- **Admin Dashboard**: Create and manage multiple projects/applications
- **Multi-Project Support**: Each project gets its own isolated user base, API keys, and configuration
- **OAuth 2.0/OpenID Connect**: Industry-standard authentication protocols
- **Self-Hostable**: Can be deployed as SaaS or sold as self-hosted solution
- **Scalable**: Kubernetes-ready but also simple Docker Compose deployment

## Use Case Example: Ticket System Integration
```
Your Ticket System (SaaS)
  └─> Company A signs up
       └─> Auto-create "Project" in ExoAuth via API
            └─> Get API keys
            └─> Company A's employees authenticate through ExoAuth
                 └─> Ticket System validates tokens and checks permissions
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8+ (Web API)
- **Authentication**: OpenIddict (OAuth 2.0/OpenID Connect server)
- **Identity Management**: ASP.NET Core Identity
- **Language**: C#
- **Runtime**: .NET 8+

### Database & Storage
- **Primary Database**: PostgreSQL (with encryption at rest)
- **Caching**: Redis (sessions, rate limiting, caching)
- **Message Queue**: RabbitMQ (async events, webhooks, email sending)
- **Backup Strategy**:
  - Automated daily full backups
  - Hourly incremental backups
  - 30-day backup retention
  - Geo-redundant backup storage (optional)
  - Encrypted backups
  - Point-in-time recovery (PITR)

### Frontend
- **Framework**: React 18+
- **Build Tool**: Vite
- **Package Manager**: Yarn
- **Styling**: Tailwind CSS
- **UI Components**: shadcn/ui
- **Data Fetching**: React Query (TanStack Query)
- **Routing**: React Router
- **Forms**: React Hook Form + Zod (validation)
- **i18n**: i18next
- **Purpose**: Admin dashboard for managing projects, users, settings

### Infrastructure
- **Containerization**: Docker
- **Orchestration**: Kubernetes (optional, for scaling)
- **Simple Deployment**: Docker Compose (single server)
- **Hosting Requirements**:
  - EU/EEA data centers for GDPR compliance (Germany, France, Netherlands)
  - ISO 27001 certified hosting providers
  - DPA (Data Processing Agreement) with hosting provider
  - Optional geo-redundancy across multiple EU regions

## Core Features

### 1. Multi-Project Management
- **Project Creation**: Admin can create unlimited projects via dashboard
- **API Key Generation**: Each project gets public/private key pairs
- **Domain Whitelisting**: Configure allowed CORS origins per project
- **Project Isolation**: Complete data separation between projects
- **Project Settings**: Customize auth flows, branding, features per project

### 2. Authentication Features
- **Email/Password Authentication**
  - Email verification (with customizable templates)
  - Password reset flow
  - Password strength requirements (configurable per project)

- **Multi-Factor Authentication (MFA)**
  - TOTP (Time-based One-Time Password) - authenticator apps
  - SMS codes (optional)
  - Backup codes
  - Remember device option

- **Social Login Providers**
  - Google
  - GitHub
  - Microsoft
  - Facebook
  - Discord
  - Configurable per project

- **Passwordless Authentication**
  - Magic links (email)
  - Email verification codes
  - SMS codes

- **LDAP Integration**
  - Connect to Active Directory
  - Enterprise SSO
  - Configurable per project

- **Device Verification**
  - Remember trusted devices
  - Suspicious login alerts
  - Device fingerprinting

### 3. Authorization & Permissions (NO ROLES - Permission-Based Only)
- **Permission System (Direct Assignment)**
  - Custom permissions per project (e.g., `tickets:read`, `tickets:create`, `admin:dashboard`)
  - Permissions assigned DIRECTLY to users (no roles/RBAC)
  - Permissions embedded in access tokens
  - Fine-grained access control

- **Permission Groups (Optional - UI Template Only)**
  - NOT stored in database as "roles"
  - Just templates for quick assignment (e.g., "Support-Agent Template")
  - Admin selects template → permissions are COPIED to user
  - User only has individual permissions, NOT a "role"
  - Think of it as "permission presets" for convenience

- **Access Token Structure**
```json
{
  "sub": "user-123",
  "email": "user@example.com",
  "project_id": "project-abc",
  "permissions": [
    "tickets:read",
    "tickets:create",
    "tickets:update:own"
  ],
  "metadata": {
    "custom_field": "value"
  }
}
```
**Note**: No "roles" in token - only permissions!

### 4. Security Features & NIS2 Compliance
- **Rate Limiting**
  - Per IP address
  - Per user
  - Per project
  - Configurable thresholds
  - Redis-backed

- **Brute Force Protection**
  - Account lockout after failed attempts
  - CAPTCHA integration
  - Temporary IP bans

- **Geo-Based Security**
  - Region-based connection filtering
  - Suspicious location alerts
  - IP geolocation

- **IP Management**
  - Blacklisting
  - Whitelisting
  - Per project configuration

- **Session Management**
  - Revoke individual tokens
  - Kill all sessions for a user
  - Session expiration policies
  - Refresh token rotation

- **Encryption**
  - TLS 1.3 for all communications
  - Database encryption at rest (PostgreSQL Transparent Data Encryption)
  - Encrypted backups
  - Field-level encryption for sensitive data (PII)

- **Key Management**
  - JWT signing key rotation (automated)
  - API key rotation capabilities
  - Secure key storage (Azure Key Vault, AWS KMS, HashiCorp Vault support)
  - Separate keys per project

- **NIS2 Compliance (Network and Information Security Directive 2)**
  - **Incident Response Plan**
    - Documented incident response procedures
    - Incident classification (critical, high, medium, low)
    - 24-hour reporting obligation for significant incidents
    - Incident response team contacts
    - Communication templates for authorities

  - **Security Incident Management**
    - Real-time threat detection
    - Automated alerts for suspicious activity
    - Incident logging and tracking system
    - Post-incident analysis and reporting
    - Integration with SIEM tools

  - **Business Continuity & Disaster Recovery**
    - Automated database backups (hourly/daily configurable)
    - Point-in-time recovery (PITR)
    - Geo-redundant backup storage
    - Disaster recovery runbooks
    - RTO (Recovery Time Objective): < 4 hours
    - RPO (Recovery Point Objective): < 1 hour
    - Regular DR testing and documentation

  - **Supply Chain Security**
    - Dependency scanning (NuGet packages, npm packages)
    - Software Bill of Materials (SBOM) generation
    - Vulnerability scanning (Dependabot, Snyk)
    - Only use vetted and maintained dependencies
    - License compliance checks

  - **Security Audits & Penetration Testing**
    - Quarterly internal security audits
    - Annual external penetration tests
    - Vulnerability assessment reports
    - Remediation tracking
    - Compliance certifications (ISO 27001 ready)

  - **Risk Management**
    - Risk assessment documentation
    - Threat modeling per feature
    - Security control matrix
    - Regular risk reviews
    - Risk register maintenance

  - **Security Awareness & Training**
    - Security documentation for administrators
    - Best practices guides
    - Security update notifications
    - Secure configuration templates

### 5. Email System
- **Customizable Templates**
  - Email verification
  - Password reset
  - MFA codes
  - Suspicious login alerts
  - Welcome emails
  - Per-project template customization

- **Email Provider Support**
  - SMTP (custom servers)
  - SendGrid
  - Mailgun
  - AWS SES
  - Pluggable architecture

- **Multi-Language Support**
  - i18n for email templates
  - Configurable languages per project
  - User language preference

### 6. User Management & GDPR/DSGVO Compliance
- **Admin API**
  - Create/update/delete users
  - Search and filter users
  - Bulk operations

- **User Metadata**
  - Custom fields per project
  - Profile information
  - User preferences

- **User Impersonation**
  - Support/admin can impersonate users
  - Audit logged

- **GDPR/DSGVO Data Rights**
  - **Right to Access**: User data export (JSON/PDF format)
  - **Right to Rectification**: Users can update their own data
  - **Right to Erasure (Right to be Forgotten)**
    - Complete account deletion
    - Anonymous data retention for legal/audit purposes
    - Cascade deletion of user data across all tables
    - Configurable grace period (e.g., 30 days before permanent deletion)
  - **Right to Data Portability**: Export user data in machine-readable format
  - **Right to Restriction of Processing**: Temporary account suspension without deletion
  - **Right to Object**: Opt-out of specific processing activities

- **Consent Management**
  - Granular consent tracking (marketing, analytics, etc.)
  - Consent version history
  - Withdraw consent functionality
  - Cookie consent for hosted login pages
  - Consent audit trail

- **Data Retention Policies**
  - Configurable retention periods per data type:
    - User accounts (e.g., deleted after 30 days inactivity post-deletion request)
    - Audit logs (e.g., 1-2 years for security)
    - Login attempts (e.g., 90 days)
    - Session data (e.g., 30 days)
  - Automated cleanup jobs
  - Legal hold capabilities (suspend deletion for investigations)

- **Data Minimization**
  - Collect only necessary data
  - Configurable optional fields per project
  - Regular data minimization audits
  - Pseudonymization of archived data

- **Privacy by Design**
  - Encryption at rest and in transit
  - Data anonymization for analytics
  - Privacy impact assessments documented

### 7. Webhooks & Events
- **Event System**
  - `user.created`
  - `user.login`
  - `user.login.failed`
  - `user.mfa.enabled`
  - `user.password.reset`
  - `user.email.verified`
  - `user.deleted`
  - `session.revoked`

- **Webhook Delivery**
  - RabbitMQ for reliable delivery
  - Retry mechanism
  - Webhook signature verification
  - Per-project webhook URLs

- **Webhook Configuration**
  - Select which events to subscribe to
  - Custom headers
  - Authentication

### 8. Audit Logging
- **Comprehensive Logging**
  - All authentication attempts (success/failure)
  - User changes (creation, updates, deletion)
  - Admin actions
  - Permission changes
  - API key usage

- **Queryable Logs**
  - Filter by project, user, action, date range
  - Export logs
  - Retention policies

### 9. Token Management
- **JWT Access Tokens**
  - Short-lived (configurable, default 15 minutes)
  - Include user info and permissions
  - Signed with project-specific keys (private key per project)

- **Refresh Tokens**
  - Long-lived (configurable, default 30 days)
  - Stored in database
  - Can be revoked
  - Rotation on use

- **Token Revocation**
  - Immediate invalidation
  - Revocation lists
  - Redis-cached for performance

### 10. Monitoring & Observability
- **Metrics Dashboard**
  - Active users (real-time)
  - Login attempts (success/failure rates)
  - API usage per project
  - Rate limit hits
  - Performance metrics (response times)
  - Error rates

- **Health Checks**
  - Database connectivity
  - Redis connectivity
  - RabbitMQ connectivity
  - Email service status

- **Error Tracking**
  - Centralized error logging
  - Stack traces
  - Error categorization

### 11. Multi-Language Support (i18n)
- **UI Localization**
  - Admin dashboard in multiple languages
  - Login pages in multiple languages

- **Email Localization**
  - Templates per language
  - User language preference

- **Supported Languages** (configurable)
  - English
  - German
  - Spanish
  - French
  - More as needed

### 12. Hosted Login vs Embedded Auth
- **Hosted Login Pages**
  - Default option
  - User redirects to `https://auth.example.com/login?project_id=xyz`
  - Customizable branding per project
  - After auth → redirect back with token

- **Headless/API Mode**
  - Client apps call API directly
  - `POST /api/auth/login`
  - Full control over UI
  - For custom integrations

## Architecture

### System Components
```
┌─────────────────────────────────────────────┐
│      Admin Dashboard (React + Vite)         │
│   - Manage projects, users, settings        │
│   - View analytics & logs                   │
└─────────────────────────────────────────────┘
                    │ HTTPS
┌─────────────────────────────────────────────┐
│   ExoAuth.API (ASP.NET Core + OpenIddict)   │
│   - OAuth 2.0/OIDC endpoints                │
│   - User authentication                     │
│   - Token issuance/validation               │
└─────────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────────┐
│   ExoAuth.Admin.API (ASP.NET Core)          │
│   - Project management                      │
│   - User management                         │
│   - Configuration API                       │
└─────────────────────────────────────────────┘
         │              │              │
    ┌────┴────┐    ┌────┴────┐   ┌────┴────┐
    │ Redis   │    │PostgreSQL│   │RabbitMQ │
    │ Cache   │    │ Main DB  │   │ Events  │
    │ Sessions│    │ Users    │   │ Jobs    │
    │ Rate    │    │ Projects │   │         │
    └─────────┘    └──────────┘   └─────────┘
                                       │
                            ┌──────────┴────────────┐
                            │  ExoAuth.Worker       │
                            │  - Send emails        │
                            │  - Process webhooks   │
                            │  - Audit log writing  │
                            │  - Cleanup jobs       │
                            └───────────────────────┘
```

### Project Structure
```
exoauth/
├── src/
│   ├── ExoAuth.API/                 # Main OAuth/OIDC server
│   │   ├── Controllers/
│   │   ├── Endpoints/
│   │   │   ├── AuthEndpoints.cs
│   │   │   ├── OAuthEndpoints.cs
│   │   │   └── MfaEndpoints.cs
│   │   ├── Middleware/
│   │   │   ├── RateLimitingMiddleware.cs
│   │   │   └── SecurityHeadersMiddleware.cs
│   │   └── Program.cs
│   │
│   ├── ExoAuth.Admin.API/           # Admin management API
│   │   ├── Controllers/
│   │   │   ├── ProjectsController.cs
│   │   │   ├── UsersController.cs
│   │   │   ├── PermissionsController.cs
│   │   │   ├── PermissionGroupsController.cs
│   │   │   ├── ConsentsController.cs
│   │   │   ├── IncidentsController.cs
│   │   │   └── SettingsController.cs
│   │   └── Program.cs
│   │
│   ├── ExoAuth.Worker/              # Background worker service
│   │   ├── Jobs/
│   │   │   ├── EmailSenderJob.cs
│   │   │   ├── WebhookDeliveryJob.cs
│   │   │   ├── AuditLogWriterJob.cs
│   │   │   ├── DataCleanupJob.cs
│   │   │   ├── BackupJob.cs
│   │   │   ├── AccountDeletionJob.cs
│   │   │   └── KeyRotationJob.cs
│   │   └── Program.cs
│   │
│   ├── ExoAuth.Core/                # Domain models & interfaces
│   │   ├── Entities/
│   │   │   ├── Project.cs
│   │   │   ├── User.cs
│   │   │   ├── Permission.cs
│   │   │   ├── PermissionGroup.cs
│   │   │   ├── ApiKey.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── UserConsent.cs
│   │   │   ├── DataDeletionRequest.cs
│   │   │   ├── SecurityIncident.cs
│   │   │   ├── DeviceFingerprint.cs
│   │   │   ├── LoginAttempt.cs
│   │   │   └── EmailTemplate.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IEmailService.cs
│   │   │   ├── ICacheService.cs
│   │   │   ├── IEventPublisher.cs
│   │   │   └── IEncryptionService.cs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   ├── Users/
│   │   │   └── Projects/
│   │   └── Enums/
│   │
│   ├── ExoAuth.Infrastructure/      # Data access & external services
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Configurations/
│   │   │       ├── UserConfiguration.cs
│   │   │       └── ProjectConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   └── ProjectRepository.cs
│   │   ├── Services/
│   │   │   ├── EmailService.cs
│   │   │   ├── RateLimitService.cs
│   │   │   ├── CacheService.cs
│   │   │   ├── EventPublisher.cs
│   │   │   ├── EncryptionService.cs
│   │   │   ├── BackupService.cs
│   │   │   └── IncidentReportingService.cs
│   │   └── ExternalProviders/
│   │       ├── LDAP/
│   │       │   └── LdapAuthProvider.cs
│   │       ├── SocialAuth/
│   │       │   ├── GoogleAuthProvider.cs
│   │       │   ├── GitHubAuthProvider.cs
│   │       │   └── MicrosoftAuthProvider.cs
│   │       └── EmailProviders/
│   │           ├── SmtpEmailProvider.cs
│   │           └── SendGridEmailProvider.cs
│   │
│   └── ExoAuth.Admin.Web/           # React admin dashboard
│       ├── src/
│       │   ├── components/
│       │   │   ├── common/
│       │   │   ├── layout/
│       │   │   └── forms/
│       │   ├── pages/
│       │   │   ├── Dashboard/
│       │   │   ├── Projects/
│       │   │   │   ├── ProjectList.tsx
│       │   │   │   ├── ProjectCreate.tsx
│       │   │   │   └── ProjectSettings.tsx
│       │   │   ├── Users/
│       │   │   │   ├── UserList.tsx
│       │   │   │   └── UserDetails.tsx
│       │   │   ├── Permissions/
│       │   │   │   ├── PermissionList.tsx
│       │   │   │   └── PermissionGroups.tsx
│       │   │   ├── Compliance/
│       │   │   │   ├── GDPR.tsx
│       │   │   │   ├── Consents.tsx
│       │   │   │   └── DataDeletion.tsx
│       │   │   ├── Security/
│       │   │   │   ├── Incidents.tsx
│       │   │   │   ├── AuditLogs.tsx
│       │   │   │   └── RateLimits.tsx
│       │   │   ├── Settings/
│       │   │   │   ├── EmailTemplates.tsx
│       │   │   │   ├── Webhooks.tsx
│       │   │   │   └── ApiKeys.tsx
│       │   │   └── Analytics/
│       │   │       ├── Overview.tsx
│       │   │       └── Reports.tsx
│       │   ├── services/
│       │   │   ├── api.ts
│       │   │   ├── auth.ts
│       │   │   └── projects.ts
│       │   ├── hooks/
│       │   ├── contexts/
│       │   ├── i18n/
│       │   │   ├── en.json
│       │   │   ├── de.json
│       │   │   └── i18n.ts
│       │   ├── App.tsx
│       │   └── main.tsx
│       ├── public/
│       ├── package.json
│       ├── vite.config.ts
│       └── tsconfig.json
│
├── templates/                       # Legal & email templates
│   ├── legal/
│   │   ├── dpa-template.md
│   │   ├── privacy-policy-template.md
│   │   ├── terms-of-service-template.md
│   │   └── impressum-template.md
│   ├── emails/
│   │   ├── email-verification.html
│   │   ├── password-reset.html
│   │   ├── mfa-code.html
│   │   └── suspicious-login.html
│   └── incident-response/
│       ├── nis2-notification.md
│       └── breach-notification.md
│
├── docs/                            # Documentation
│   ├── api/
│   │   ├── authentication.md
│   │   └── admin-api.md
│   ├── deployment/
│   │   ├── docker-compose.md
│   │   ├── kubernetes.md
│   │   └── self-hosted.md
│   ├── compliance/
│   │   ├── gdpr.md
│   │   ├── nis2.md
│   │   └── security-whitepaper.md
│   └── integration/
│       ├── quickstart.md
│       └── oauth-flow.md
│
├── scripts/                         # Utility scripts
│   ├── backup.sh
│   ├── restore.sh
│   ├── key-rotation.sh
│   └── setup-dev.sh
│
├── docker-compose.yml               # Local development
├── docker-compose.prod.yml          # Production deployment
├── kubernetes/                      # K8s manifests
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secrets.yaml
│   ├── api-deployment.yaml
│   ├── admin-api-deployment.yaml
│   ├── worker-deployment.yaml
│   ├── postgres-statefulset.yaml
│   ├── redis-deployment.yaml
│   ├── rabbitmq-deployment.yaml
│   ├── ingress.yaml
│   └── backup-cronjob.yaml
│
├── tests/                           # Tests
│   ├── ExoAuth.Tests.Unit/
│   ├── ExoAuth.Tests.Integration/
│   └── ExoAuth.Tests.E2E/
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── security-scan.yml
│       └── deploy.yml
│
├── .env.example
├── .gitignore
├── LICENSE
├── README.md
└── brain.md                         # This file
```

## Database Schema (PostgreSQL)

### Core Tables
- **Projects**
  - Id, Name, Description
  - JwtSigningKeyHash (RSA/ECDSA private key for signing JWTs - NEVER exposed)
  - JwtPublicKey (public key published via JWKS for token verification)
  - AllowedDomains (CORS/domain whitelist, JSON array)
  - Settings (JSON: MFA enabled, social providers, etc.)
  - CreatedAt, UpdatedAt

  **Note**: Separate ApiKeys table contains credentials for Admin API access!

- **Users**
  - Id, ProjectId (FK)
  - Email, PasswordHash
  - EmailVerified, EmailVerifiedAt
  - PhoneNumber, PhoneVerified
  - MfaEnabled, MfaSecret
  - Metadata (JSON: custom fields)
  - LastLoginAt, CreatedAt, UpdatedAt

- **Permissions**
  - Id, ProjectId (FK)
  - Name (e.g., "tickets:read", "tickets:create")
  - Description
  - Resource (e.g., "tickets")
  - Action (e.g., "read", "create", "update:own")

- **UserPermissions** (many-to-many - DIRECT assignment)
  - UserId (FK)
  - PermissionId (FK)
  - GrantedAt
  - GrantedBy (admin user ID)

- **PermissionGroups** (OPTIONAL - Templates for UI only)
  - Id, ProjectId (FK)
  - Name (e.g., "Support Agent Template")
  - Description
  - CreatedAt

- **PermissionGroupPermissions** (many-to-many)
  - GroupId (FK)
  - PermissionId (FK)

**IMPORTANT**: NO UserRoles, NO Roles table - users only have direct permissions!

- **ApiKeys** (For Admin API access)
  - Id, ProjectId (FK)
  - PublicKey (format: pub_projectid_random)
  - PrivateKeyHash (format hashed: sec_projectid_random)
  - Name, Description
  - IsActive, ExpiresAt
  - LastUsedAt
  - CreatedAt

  **Note**: These are for Admin API access (project management), separate from JWT signing keys!

- **RefreshTokens**
  - Id, UserId (FK)
  - Token (hashed)
  - ExpiresAt, IsRevoked
  - DeviceFingerprint
  - CreatedAt

- **AuditLogs**
  - Id, ProjectId (FK), UserId (FK, nullable)
  - Action, Resource
  - IpAddress, UserAgent
  - Details (JSON)
  - CreatedAt

- **LoginAttempts**
  - Id, ProjectId (FK), UserId (FK, nullable)
  - Email, IpAddress
  - Success, FailureReason
  - CreatedAt

- **DeviceFingerprints**
  - Id, UserId (FK)
  - Fingerprint (hash)
  - IsTrusted
  - LastSeenAt, CreatedAt

- **EmailTemplates**
  - Id, ProjectId (FK)
  - Type (verification, reset, mfa, etc.)
  - Language
  - Subject, HtmlBody, TextBody
  - CreatedAt, UpdatedAt

- **Webhooks**
  - Id, ProjectId (FK)
  - Url, Events (JSON array)
  - Headers (JSON)
  - Secret
  - IsActive

- **UserConsents**
  - Id, UserId (FK)
  - ConsentType (marketing, analytics, cookies, etc.)
  - ConsentVersion
  - Granted (boolean)
  - GrantedAt, RevokedAt
  - IpAddress, UserAgent

- **DataDeletionRequests**
  - Id, UserId (FK)
  - RequestedAt
  - ScheduledDeletionAt
  - Status (pending, processing, completed, cancelled)
  - CompletedAt

- **SecurityIncidents**
  - Id, ProjectId (FK, nullable)
  - Severity (critical, high, medium, low)
  - IncidentType
  - Description, Details (JSON)
  - DetectedAt, ResolvedAt
  - ReportedToAuthorities (boolean)
  - ReportedAt

## OAuth 2.0 / OpenID Connect Flows

### Supported Grant Types
1. **Authorization Code Flow** (recommended for web apps)
2. **Client Credentials Flow** (service-to-service)
3. **Refresh Token Flow**
4. **Password Flow** (legacy, optional)

### Token Endpoints
- `POST /connect/authorize` - Authorization endpoint
- `POST /connect/token` - Token endpoint
- `POST /connect/revoke` - Token revocation
- `GET /connect/userinfo` - User info endpoint
- `GET /.well-known/openid-configuration` - Discovery document
- `GET /.well-known/jwks.json` - Public keys for token verification (JWKS)

## API Authentication & Authorization

### Two Types of API Access

**1. Auth API (End-Users)** - For user authentication
- Users authenticate directly with ExoAuth
- Get Access Tokens (JWT) for their session
- Use tokens to access YOUR application (Ticket System, etc.)
- **NO direct access to Admin API**

**2. Admin API (Programmatic)** - For managing projects/users/permissions
- YOUR application (Ticket System) uses API Keys
- API Key determines which project
- Used for user management, permission changes, etc.
- **End-users NEVER call this directly**

### Complete Flow Example: User Wants to Change Permissions

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: User Request to Ticket System                      │
└─────────────────────────────────────────────────────────────┘
User Browser:
    POST https://ticketsystem.com/api/users/123/grant-permission
    Header: Authorization: Bearer <user-access-token>
    Body: { "permission": "tickets:create" }

    ↓

┌─────────────────────────────────────────────────────────────┐
│ Step 2: Ticket System Validates User Token                 │
└─────────────────────────────────────────────────────────────┘
Ticket System Backend:
    ├── Validates JWT signature (using ExoAuth's public key)
    ├── Extracts permissions from token
    ├── Checks: Does user have "admin:manage-users" permission?
    └── If YES → Continue to Step 3

    ↓

┌─────────────────────────────────────────────────────────────┐
│ Step 3: Ticket System Calls ExoAuth Admin API              │
└─────────────────────────────────────────────────────────────┘
Ticket System Backend → ExoAuth Admin API:
    POST https://auth.exoauth.com/api/users/user-456/permissions
    Header: X-API-Key: pub_ticketsystem_abc123  ← Ticket System's API Key!
    Header: X-API-Secret: sec_ticketsystem_xyz789
    Body: {
      "permission_name": "tickets:create"
    }

    ↓

┌─────────────────────────────────────────────────────────────┐
│ Step 4: ExoAuth Admin API Processes Request                │
└─────────────────────────────────────────────────────────────┘
ExoAuth Admin API:
    1. ✓ Validates API Key: pub_ticketsystem_abc123
    2. ✓ Identifies Project: "TicketSystem-CompanyA"
    3. ✓ Checks: User user-456 exists in THIS project?
    4. ✓ Checks: Permission "tickets:create" exists in THIS project?
    5. ✓ Assigns permission to user
    6. ✓ Returns success

    ↓

┌─────────────────────────────────────────────────────────────┐
│ Step 5: Ticket System Returns Response to User             │
└─────────────────────────────────────────────────────────────┘
Ticket System → User Browser:
    { "success": true, "message": "Permission granted" }
```

### API Key Structure

Each project gets API credentials when created:

```
API Key Pair:
├── Public Key:  pub_<project-id>_<random>
│   └── Sent in X-API-Key header
│
└── Private Key: sec_<project-id>_<random>
    └── Sent in X-API-Secret header
    └── Stored hashed in database
    └── NEVER exposed to end-users
```

### API Key Scoping & Security

```
Request arrives at Admin API:

1. API Key Authentication:
   ├── Extract X-API-Key and X-API-Secret from headers
   ├── Validate: Keys match a known project?
   └── If INVALID → 401 Unauthorized

2. Project Resolution:
   ├── API Key belongs to Project "TicketSystem-CompanyA"
   ├── ALL operations automatically scoped to this project
   └── Cannot access other projects' data

3. Resource Validation:
   ├── User ID in request: user-456
   ├── Check: user-456 exists in "TicketSystem-CompanyA"?
   └── If NOT in this project → 404 Not Found

4. Permission Validation:
   ├── Permission name: "tickets:create"
   ├── Check: Permission exists in "TicketSystem-CompanyA"?
   └── If NOT in this project → 404 Not Found

5. Execute Operation:
   └── Grant permission to user WITHIN this project only
```

### Two Admin Access Modes

**Mode 1: Internal Admin Dashboard (For You/Your Team)**
```
Admin User Login:
    POST /api/auth/login
    Body: { "email": "admin@exoauth.com", "password": "xxx" }

    ↓

Receives Admin JWT:
    {
      "sub": "admin-user-123",
      "permissions": ["admin:all-projects"],
      "is_super_admin": true
    }

    ↓

Can manage ALL projects:
    GET /api/projects
    GET /api/users?project_id=any-project
    POST /api/projects/{any-id}/settings

Uses: Authorization: Bearer <admin-jwt>
```

**Mode 2: Programmatic API (For Your Customers' Apps)**
```
Ticket System Backend:
    Uses API Key: pub_ticketsystem_abc123

    ↓

Can ONLY manage its own project:
    GET /api/users  ← Only returns users from TicketSystem project
    POST /api/users ← Creates user in TicketSystem project
    PUT /api/users/456/permissions ← Only if user-456 is in TicketSystem project

Uses: X-API-Key + X-API-Secret headers
```

### Important Security Rules

❌ **NEVER DO THIS:**
```javascript
// WRONG: Exposing Admin API directly to end-users
// User's browser making direct call to ExoAuth Admin API
fetch('https://auth.exoauth.com/api/users/456/permissions', {
  headers: {
    'X-API-Key': 'pub_ticketsystem_abc123',  // ❌ EXPOSED TO USER!
    'X-API-Secret': 'sec_ticketsystem_xyz789' // ❌ LEAKED!
  }
});
```

✅ **CORRECT:**
```javascript
// User's browser calls YOUR backend
fetch('https://ticketsystem.com/api/users/456/grant-permission', {
  headers: {
    'Authorization': `Bearer ${userAccessToken}` // ✓ User's token
  }
});

// YOUR backend calls ExoAuth Admin API
// (API keys stored securely in environment variables)
fetch('https://auth.exoauth.com/api/users/456/permissions', {
  headers: {
    'X-API-Key': process.env.EXOAUTH_API_KEY,      // ✓ Secure
    'X-API-Secret': process.env.EXOAUTH_API_SECRET // ✓ Secure
  }
});
```

### Admin API Request Examples

**Example 1: Create User**
```http
POST /api/users
X-API-Key: pub_ticketsystem_abc123
X-API-Secret: sec_ticketsystem_xyz789
Content-Type: application/json

{
  "email": "newuser@company.com",
  "password": "SecurePass123!",
  "metadata": {
    "department": "support"
  }
}

Response:
{
  "id": "user-789",
  "email": "newuser@company.com",
  "project_id": "TicketSystem-CompanyA",  ← Automatically set
  "created_at": "2025-12-22T10:30:00Z"
}
```

**Example 2: Grant Permission**
```http
POST /api/users/user-789/permissions
X-API-Key: pub_ticketsystem_abc123
X-API-Secret: sec_ticketsystem_xyz789
Content-Type: application/json

{
  "permission_name": "tickets:create"
}

Response:
{
  "success": true,
  "user_id": "user-789",
  "permission": "tickets:create",
  "granted_at": "2025-12-22T10:35:00Z"
}
```

**Example 3: List Users (Scoped to Project)**
```http
GET /api/users?limit=50&offset=0
X-API-Key: pub_ticketsystem_abc123
X-API-Secret: sec_ticketsystem_xyz789

Response:
{
  "users": [
    { "id": "user-456", "email": "user1@company.com", ... },
    { "id": "user-789", "email": "user2@company.com", ... }
  ],
  "total": 2,
  "project_id": "TicketSystem-CompanyA"  ← All from this project only
}
```

## Public/Private Key Architecture (JWT Signing)

### How It Works
```
ExoAuth Server (Private Key):
├── Generates RSA/ECDSA key pair when project is created
├── Private Key stored HASHED in database (never exposed)
├── Signs ALL JWTs with private key
└── Publishes public key via JWKS endpoint

Your Application (Public Key):
├── Gets public key from /.well-known/jwks.json
├── Validates JWT signatures LOCALLY (no API call needed)
├── Checks token expiration, issuer, audience
├── Extracts permissions from token
└── CANNOT create tokens (only ExoAuth can)
```

### Token Validation Flow (Client-Side)
```csharp
// Your Ticket System validates tokens WITHOUT calling ExoAuth API
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Get public key from ExoAuth's JWKS endpoint
        options.MetadataAddress = "https://auth.exoauth.com/.well-known/openid-configuration";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,  // Verify signature
            ValidIssuer = "https://auth.exoauth.com",
            ValidAudience = "ticketsystem-project-id",
            ValidateLifetime = true,  // Check expiration
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// In your controller - token is already validated by middleware
[Authorize]
[HttpGet("/api/tickets")]
public IActionResult GetTickets()
{
    // Extract permissions from validated token
    var permissions = User.Claims
        .Where(c => c.Type == "permissions")
        .Select(c => c.Value)
        .ToList();

    if (!permissions.Contains("tickets:read"))
        return Forbid();

    // ... return tickets
}
```

### Security Validation Chain
```
1. Domain Validation (CORS/Origin Check)
   ├── Request from https://ticketsystem.com
   ├── Check: Is this domain in AllowedDomains for this project?
   └── If NO → 403 Forbidden

2. Token Signature Validation
   ├── Client extracts JWT from Authorization header
   ├── Validates signature using public key (JWKS)
   └── If invalid → 401 Unauthorized

3. Token Claims Validation
   ├── Check "iss" (issuer): Must be ExoAuth server
   ├── Check "aud" (audience): Must match project ID
   ├── Check "exp" (expiration): Must not be expired
   └── If invalid → 401 Unauthorized

4. Permission Check
   ├── Extract "permissions" claim from token
   ├── Check if required permission exists
   └── If missing → 403 Forbidden
```

### Key Rotation
```
Automated Key Rotation (Background Job):
├── Generate new key pair every 90 days (configurable)
├── Keep old public keys active for grace period (7 days)
├── Update JWKS endpoint with both keys
├── Sign new tokens with new key
├── Old tokens still validate with old public key
└── After grace period, remove old key from JWKS
```

## API Endpoints

### Auth API
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/verify-email` - Verify email address
- `POST /api/auth/forgot-password` - Initiate password reset
- `POST /api/auth/reset-password` - Complete password reset
- `POST /api/auth/mfa/enable` - Enable MFA
- `POST /api/auth/mfa/verify` - Verify MFA code
- `GET /api/auth/me` - Get current user info

### Admin API
- `GET /api/projects` - List projects
- `POST /api/projects` - Create project
- `GET /api/projects/{id}` - Get project details
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project
- `POST /api/projects/{id}/keys/rotate` - Rotate API keys

- `GET /api/users` - List users (filtered by project)
- `POST /api/users` - Create user
- `GET /api/users/{id}` - Get user details
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user (soft delete with grace period)
- `POST /api/users/{id}/impersonate` - Impersonate user
- `GET /api/users/{id}/export` - Export user data (GDPR)
- `POST /api/users/{id}/deletion-request` - Request account deletion

- `GET /api/permissions` - List permissions
- `POST /api/permissions` - Create permission
- `PUT /api/permissions/{id}` - Update permission
- `DELETE /api/permissions/{id}` - Delete permission

- `GET /api/permission-groups` - List permission groups (templates)
- `POST /api/permission-groups` - Create permission group
- `PUT /api/permission-groups/{id}` - Update permission group
- `DELETE /api/permission-groups/{id}` - Delete permission group
- `POST /api/users/{id}/apply-permission-group` - Apply group template to user (copies permissions)

- `GET /api/users/{id}/permissions` - List user's permissions
- `POST /api/users/{id}/permissions` - Grant permission to user
- `DELETE /api/users/{id}/permissions/{permissionId}` - Revoke permission from user

- `GET /api/audit-logs` - Query audit logs
- `GET /api/analytics/dashboard` - Get dashboard metrics

- `GET /api/consents` - List user consents
- `POST /api/consents` - Grant consent
- `DELETE /api/consents/{id}` - Revoke consent

- `GET /api/incidents` - List security incidents
- `POST /api/incidents` - Report security incident
- `PUT /api/incidents/{id}` - Update incident status

## Deployment Options

### Option 1: Simple Docker Compose (Single Server)
```bash
docker-compose up -d
```
Includes:
- ExoAuth.API
- ExoAuth.Admin.API
- ExoAuth.Worker
- PostgreSQL
- Redis
- RabbitMQ
- Nginx (reverse proxy)

### Option 2: Kubernetes (Scalable)
```bash
kubectl apply -f kubernetes/
```
Features:
- Auto-scaling for API pods
- Persistent volumes for PostgreSQL
- Redis cluster
- RabbitMQ cluster
- Load balancing

### Option 3: Self-Hosted Package (For Selling)

**Two Licensing Modes:**

**A) Single-Tenant License (Sold to customers)**
```
Features:
├── ONLY ONE project (multi-project disabled)
├── No "Create Project" button in admin UI
├── Project created during installation wizard
├── Simpler configuration (no project selection)
├── All auth features enabled (MFA, LDAP, etc.)
├── Cannot be resold/redistributed
└── License key validation

Use Case: Company buys ExoAuth for their internal use
Setup: docker-compose up, wizard asks for company name → creates single project
```

**B) Multi-Tenant License (Full version)**
```
Features:
├── Unlimited projects
├── Full admin dashboard with project management
├── All features enabled
├── White-label option (rebrand as your product)
└── For resellers or your own SaaS hosting

Use Case: You run ExoAuth as SaaS or resell to customers
```

**Technical Implementation:**
- Single executable with embedded UI
- SQLite option for simpler setup (or PostgreSQL)
- Installation wizard:
  - Asks for license key
  - Validates: Single-tenant vs Multi-tenant
  - If single-tenant: Creates ONE project, hides project management
  - If multi-tenant: Enables full features
- Docker image included
- License enforcement in code (cannot bypass without license key)

## Configuration

### Environment Variables
```bash
# Database
DATABASE_CONNECTION_STRING=Host=localhost;Database=exoauth;Username=postgres;Password=xxx

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest

# Email
EMAIL_PROVIDER=smtp
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=xxx
SMTP_PASSWORD=xxx

# JWT
JWT_SIGNING_KEY=your-secret-key
JWT_ISSUER=https://auth.example.com
JWT_AUDIENCE=https://api.example.com

# Feature Flags
ENABLE_SOCIAL_LOGIN=true
ENABLE_LDAP=true
ENABLE_MFA=true
```

## Security Considerations
- All passwords hashed with bcrypt/Argon2
- API keys hashed in database
- HTTPS only in production
- CORS properly configured per project
- Rate limiting on all public endpoints
- SQL injection prevention (EF Core parameterized queries)
- XSS protection (proper input validation)
- CSRF tokens for state-changing operations
- Secrets stored in environment variables or secret manager

## Monitoring Stack (Optional Integration)
- **Prometheus** - Metrics collection
- **Grafana** - Dashboards
- **Elasticsearch + Kibana** - Log aggregation
- **Sentry** - Error tracking

## Future Enhancements
- [ ] Biometric authentication (WebAuthn/FIDO2)
- [ ] Risk-based authentication
- [ ] Machine learning for fraud detection
- [ ] Mobile SDKs (React Native, Flutter)
- [ ] CLI for project management
- [ ] Terraform/Pulumi modules for infrastructure
- [ ] Multi-region deployment support
- [ ] Advanced analytics (user behavior, retention)

## Legal & Compliance Documentation

### Templates Included
- **Data Processing Agreement (DPA/AVV)**
  - Template for customers (when ExoAuth processes their user data)
  - GDPR Article 28 compliant
  - Processor obligations documented
  - Sub-processor list (email providers, hosting, etc.)

- **Privacy Policy Template**
  - Customizable per project
  - GDPR-compliant language
  - Data collection disclosure
  - User rights explained
  - Cookie policy section

- **Terms of Service Template**
  - For ExoAuth SaaS offering
  - For self-hosted licensing
  - Acceptable use policy
  - Service level commitments
  - Limitation of liability

- **Cookie Consent Banner**
  - For hosted login pages
  - Granular consent categories
  - GDPR & ePrivacy Directive compliant
  - Consent withdrawal functionality

- **Incident Response Templates**
  - NIS2 notification templates
  - Customer breach notification
  - Authority reporting templates (within 24h requirement)

- **Security Documentation**
  - Security whitepaper
  - Compliance certifications (ISO 27001 preparation)
  - Penetration test reports (sanitized for customers)
  - Disaster recovery procedures

### Impressum Requirements
- **For EU/German customers**: Provide template for mandatory Impressum (legal notice)
- Hosting provider information
- Data protection officer contact (if required)
- Supervisory authority contact information

## Business Model & Licensing (Updated 2026-01-17)

### Strategy: "EU-First Open Source"

**Positioning:**
> "Auth für EU Companies die GDPR ernst nehmen und nicht von US-Cloud abhängig sein wollen"

**Why this niche?**
- Auth0, Clerk, Firebase = all US companies (GDPR concerns)
- Keycloak is the alternative but terrible UX
- Gap in market for modern, EU-focused, self-hostable auth

**Unique Selling Points (USPs):**
1. **EU-First** - GDPR/NIS2 compliant by design
2. **Self-Hostable** - Unlike Clerk/Auth0
3. **Modern Stack** - Unlike Keycloak (no Java monster)
4. **Fair Pricing** - Not Auth0 prices at scale
5. **Multi-Tenant Ready** - For SaaS builders

### Hybrid Pricing Model

#### Self-Hosted (Free & Paid)

| Edition | Price | Projects | Users | Support |
|---------|-------|----------|-------|---------|
| **Community** (Open Source) | €0 | 1 | Unlimited | Community |
| **Pro License** | €2.999 one-time + €499/year | Unlimited | Unlimited | Email |
| **Enterprise License** | €9.999 one-time + €1.999/year | Unlimited | Unlimited | Priority + SLA |

**Community Edition:**
- MIT or Apache 2.0 license
- All core auth features (MFA, Passkeys, Social Login, etc.)
- Single project only (multi-project disabled)
- Perfect for: Own apps, trying out ExoAuth

**Pro License:**
- Multi-project enabled
- Private repo access
- Email support
- For: Companies self-hosting for internal use

**Enterprise License:**
- Everything in Pro
- Multi-tenant capable (can resell/white-label)
- Custom contracts, SLA
- For: Resellers, agencies, large enterprises

#### Cloud Hosted (SaaS)

| Tier | Price | Projects | MAU | Features |
|------|-------|----------|-----|----------|
| **Free** | €0/mo | 1 | 1.000 | Core Auth |
| **Starter** | €49/mo | 5 | 10.000 | + Social Login, Webhooks |
| **Business** | €199/mo | 20 | 50.000 | + Custom Domain, SAML SSO |
| **Enterprise** | Custom | Unlimited | Unlimited | + SLA, Dedicated Support |

**Overage:** €0.01 per additional MAU (soft limit with upgrade prompt)

### Plan Management System

#### Key Concepts

**Organization:** A paying customer (company/individual) that owns one or more Projects.

**Project:** An isolated auth environment with its own users, settings, API keys.

**Plan:** A pricing tier with defined limits and features (Free, Starter, Business, Enterprise).

**Subscription:** Links an Organization to a Plan with specific limits (supports grandfathering).

**Feature Flag:** A toggleable capability (e.g., `social_login`, `webhooks`, `saml_sso`).

#### Grandfathering (Plan Changes)

When plan limits/features change, existing customers keep their original terms:

```
Example:
- Jan 2026: Starter = 5 Projects, 10k MAU, €49/mo
- Customer A buys Starter
- Mar 2026: Starter changed to 3 Projects, 5k MAU, €49/mo
- Customer A still has 5 Projects, 10k MAU (grandfathered)
- New customers get 3 Projects, 5k MAU
```

**Technical Implementation:**
- Limits are COPIED to Subscription when purchased (not linked)
- Plan changes only affect new subscriptions by default
- Admin can optionally migrate existing customers (with warnings)

#### Admin Controls

The ExoAuth admin can:
1. **Create/Edit Plans** - Set prices, limits, features
2. **Manage Features** - Define feature flags, assign to plans
3. **Assign Subscriptions** - Manually assign plans to organizations
4. **View Usage** - See which orgs are near limits
5. **Grandfather Control** - Choose what happens to existing customers on plan change

### Implementation Phases

#### ✅ Phase 0: Internal Admin Foundation (COMPLETE)
- [x] System Users & Authentication
- [x] MFA, Passkeys, Device Trust
- [x] Rate Limiting, IP Restrictions
- [x] Email System (Multi-Provider)
- [x] Audit Logging
- [x] 577+ Unit Tests

#### 🔄 Phase A: Multi-Tenant Foundation (IN PROGRESS)
- [ ] **Task 027:** Organizations (customers)
- [ ] **Task 028:** Projects & Project Users
- [ ] **Task 029:** API Keys
- [ ] **Task 030:** Plans, Features, Subscriptions
- [ ] Frontend for all above

#### ⏳ Phase B: Billing & Payments (FUTURE)
- [ ] Stripe Integration
- [ ] Automatic Subscriptions
- [ ] Usage-based Billing (MAU tracking)
- [ ] Invoices & Customer Portal

#### ⏳ Phase C: OAuth & Advanced Features (FUTURE)
- [ ] OAuth 2.0 / OpenID Connect Server (OpenIddict)
- [ ] Social Login Providers
- [ ] LDAP Integration
- [ ] Webhooks
- [ ] Hosted Login Pages

#### ⏳ Phase D: Scale & Enterprise (FUTURE)
- [ ] License Key System (for self-hosted)
- [ ] White-Label Support
- [ ] Multi-Region Deployment
- [ ] Advanced Analytics

### Revenue Projections (Hypothetical)

**Year 1 Target:**
- 10 Pro Licenses: €29.990
- 5 Enterprise Licenses: €49.995
- 50 Cloud Starter: €29.400/year
- 20 Cloud Business: €47.760/year
- **Total: ~€157k ARR**

### Competitor Comparison

| Feature | ExoAuth | Auth0 | Clerk | Keycloak |
|---------|---------|-------|-------|----------|
| Self-Hosted | ✅ | ❌ | ❌ | ✅ |
| Cloud Hosted | ✅ | ✅ | ✅ | ❌ |
| Open Source | ✅ | ❌ | ❌ | ✅ |
| Modern UI | ✅ | ✅ | ✅ | ❌ |
| EU-Based | ✅ | ❌ | ❌ | ❌ |
| Fair Pricing | ✅ | ❌ | ⚠️ | ✅ (free) |
| Easy Setup | ✅ | ✅ | ✅ | ❌ |

## License
- **Community Edition**: MIT or Apache 2.0
- **Pro/Enterprise**: Proprietary commercial license

## Support Channels
- **Documentation**: docs.exoauth.com
- **Community Forum**: forum.exoauth.com / Discord
- **GitHub Issues**: github.com/exoauth/exoauth (community edition)
- **Email Support**: support@exoauth.com (paid customers)
- **Enterprise Support**: Dedicated Slack/Teams channel

---

**Last Updated**: 2026-01-17
**Version**: 0.2.0 (Multi-Tenant Development Phase)
**Document Status**: Updated with Business Model refinements
**Document Status**: Complete - Ready for Implementation

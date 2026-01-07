# ExoAuth - Project Overview

## What is ExoAuth?
ExoAuth is an **Authentication & Authorization SaaS Platform** with a focus on security, multi-factor authentication, device trust, and comprehensive audit logging.

---

## Tech Stack

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Runtime |
| C# | 12 | Language |
| PostgreSQL | - | Primary Database |
| Redis | - | Caching, Rate Limiting, Session Store |
| RabbitMQ | 7.x | Message Queue (Email Worker) |
| Entity Framework Core | 8.0.11 | ORM |
| MediatR (Mediator) | 3.0.1 | CQRS Pattern |
| FluentValidation | 12.1.1 | Input Validation |
| Serilog | - | Structured Logging |
| Argon2 | - | Password Hashing |
| OTP.NET | 1.4.1 | TOTP/MFA |
| MaxMind GeoIP2 | 5.4.1 | Geo Location |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI Framework |
| TypeScript | 5.9.x | Language |
| Vite | 7.x | Build Tool |
| TanStack Router | 1.x | Routing |
| TanStack Query | 5.x | Server State |
| TanStack Table | 8.x | Data Tables |
| Tailwind CSS | 4.x | Styling |
| Shadcn/UI | - | Component Library |
| React Hook Form | 7.x | Form Handling |
| Zod | 4.x | Schema Validation |
| i18next | 25.x | Internationalization (EN/DE) |
| Axios | - | HTTP Client |

---

## Architecture

### Backend - Clean Architecture + CQRS
```
backend/
├── src/
│   ├── ExoAuth.Domain/          # Entities, Enums, Constants (no dependencies)
│   ├── ExoAuth.Application/     # Commands, Queries, Handlers, Interfaces
│   ├── ExoAuth.Infrastructure/  # EF Core, Redis, RabbitMQ, Services
│   ├── ExoAuth.Api/             # Controllers, Middleware, Filters
│   └── ExoAuth.EmailWorker/     # Separate microservice for emails
└── tests/
    └── ExoAuth.UnitTests/       # Unit tests (303+ tests)
```

**Project References:**
```
Application → Domain
Infrastructure → Application
Api → Application, Infrastructure
UnitTests → Application, Domain
```

### Frontend - Feature-Based Architecture
```
frontend/src/
├── app/              # Router, Providers
├── components/
│   ├── ui/           # Shadcn/UI (don't edit!)
│   └── shared/       # Reusable components (layout, feedback, data-table, form)
├── contexts/         # Auth, Theme, Sidebar
├── features/         # Feature modules (auth, users, settings, audit-logs, permissions)
│   └── {feature}/
│       ├── api/      # API calls
│       ├── hooks/    # React Query hooks
│       ├── components/
│       └── types/
├── hooks/            # Global hooks
├── i18n/             # Translations (en, de)
├── lib/              # Utils, Axios client
├── routes/           # Page components
└── types/            # Global types
```

---

## Key Features Implemented

| Feature | Backend Task | Frontend Task |
|---------|--------------|---------------|
| Foundation & Infrastructure | 001 | 003 |
| Auth, Users, Permissions, Audit | 002 | 004 |
| Improvements & Bug Fixes | 005, 009, 011 | 006, 010, 012 |
| MFA, Sessions, Device Management | 007 | 008 |
| Risk-Based Device Trust | 013-017 | 014, 016, 018 |
| Passkeys (WebAuthn/FIDO2) | 019 | 020 |
| CAPTCHA Integration | 021 | 022 |
| Advanced Rate Limiting & IP Restrictions | 023 | 024 |

---

## Important Patterns

### Backend
- **CQRS**: Commands (write) and Queries (read) separated
- **MediatR**: All requests go through mediator pipeline
- **Validation Pipeline**: FluentValidation runs before handlers
- **Repository Pattern**: Data access abstracted
- **Factory Methods**: Entities use `Create()` static methods
- **UTC Everywhere**: Use `IDateTimeProvider.UtcNow`

### Frontend
- **React Query**: All server state (no Redux needed)
- **Feature Modules**: Self-contained features with api/hooks/components/types
- **Barrel Exports**: Every folder has index.ts
- **i18n**: All user-facing text uses translation keys
- **Shared Components**: Reuse DataTable, Feedback, Form, Layout components

---

## Current Status

- **Completed Tasks:** 001-024
- **Total Unit Tests:** 303+
- **Languages:** English (en-US), German (de-DE)
- **Last Updated:** 2026-01-07

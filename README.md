# ExoAuth

**Self-hosted authentication platform for modern applications.**

ExoAuth is an open-source, self-hostable authentication system similar to Auth0, Clerk, or Supabase Auth - but with full control over your data and infrastructure. Built with security and GDPR compliance in mind.

## Features

- **Multi-Factor Authentication (MFA)** - TOTP with authenticator apps, backup codes
- **Passkeys / WebAuthn** - Passwordless authentication with biometrics
- **Device Trust** - Intelligent device verification with risk scoring
- **Rate Limiting** - Configurable limits with automatic IP blacklisting
- **IP Restrictions** - Whitelist/blacklist with CIDR support
- **Audit Logging** - Comprehensive activity tracking
- **Email System** - Multi-provider support (SMTP, SendGrid, AWS SES, Mailgun)
- **CAPTCHA Integration** - Cloudflare Turnstile, reCAPTCHA v3, hCaptcha
- **Internationalization** - English and German out of the box

## Tech Stack

### Backend
- **.NET 8** with Clean Architecture
- **PostgreSQL** - Primary database
- **Redis** - Caching, sessions, rate limiting
- **RabbitMQ** - Email queue, async processing

### Frontend
- **React 18** + TypeScript
- **Vite** - Build tooling
- **TanStack Query and Router** - Data fetching and routing
- **Tailwind CSS + shadcn/ui** - Styling

## Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 8 SDK (for development)
- Node.js 20+ and Yarn (for development)

### Run with Docker

```bash
# Clone the repository
git clone https://github.com/juljanblischke/exoauth.git
cd exoauth

# Start all services
docker-compose up -d

# Access the app
# Frontend: http://localhost:5173
# API: http://localhost:5000
# PgAdmin: http://localhost:5050
# Mailhog: http://localhost:8025
```

### Development Setup

```bash
# Start infrastructure
docker-compose up -d postgres redis rabbitmq mailhog

# Backend
cd backend
dotnet restore
dotnet run --project src/ExoAuth.Api

# Frontend (new terminal)
cd frontend
yarn install
yarn dev
```

### Default Credentials (Development)

| Service | Credentials |
|---------|-------------|
| PostgreSQL | `exoauth:exoauth_secret` |
| PgAdmin | `admin@exoauth.dev:admin` |
| RabbitMQ | `guest:guest` |

> **Warning:** Change all credentials for production deployments!

## Configuration

Configuration is done via environment variables or `appsettings.json`.

Key settings:
- `Jwt__Secret` - JWT signing key (min 32 characters)
- `ConnectionStrings__Database` - PostgreSQL connection string
- `Captcha__Turnstile__SecretKey` - Cloudflare Turnstile secret

See `appsettings.json` for all available options.

## Project Structure

```
exoauth/
├── backend/
│   └── src/
│       ├── ExoAuth.Api/            # REST API
│       ├── ExoAuth.Application/    # Business logic (CQRS)
│       ├── ExoAuth.Domain/         # Entities, interfaces
│       ├── ExoAuth.Infrastructure/ # Data access, services
│       └── ExoAuth.EmailWorker/    # Background email processing
├── frontend/
│   └── src/
│       ├── features/               # Feature modules
│       ├── components/             # Shared components
│       └── routes/                 # Page routes
├── docker/                         # Dockerfiles
└── docker-compose.yml
```

## Security

ExoAuth is built with security as a priority:

- Password hashing with Argon2
- JWT with short expiration + refresh tokens
- CSRF protection
- Rate limiting on all endpoints
- Brute force protection with progressive delays
- Device fingerprinting
- GeoIP-based risk assessment

## Contributing

Contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `cd backend && dotnet test`
5. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**ExoAuth** - Authentication you can trust, infrastructure you control.

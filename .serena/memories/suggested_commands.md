# Suggested Commands - ExoAuth

> Commands for developing on **Windows** (Git Bash / PowerShell)

---

## Backend (.NET 8)

### Build & Run

```bash
# Build solution
cd backend
dotnet build

# Run API (with hot reload)
dotnet watch run --project src/ExoAuth.Api

# Run API (production)
dotnet run --project src/ExoAuth.Api

# Run Email Worker
dotnet run --project src/ExoAuth.EmailWorker
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with output
dotnet test --logger "console;verbosity=detailed"

# Run specific test file
dotnet test --filter "FullyQualifiedName~LoginHandlerTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Create new migration
cd backend
dotnet ef migrations add {MigrationName} --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api

# Apply migrations
dotnet ef database update --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api

# Remove last migration (not applied)
dotnet ef migrations remove --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api

# Generate SQL script
dotnet ef migrations script --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api
```

### Package Management

```bash
# Add package
dotnet add src/ExoAuth.{Project} package {PackageName}

# List packages
dotnet list package

# Update packages
dotnet restore
```

---

## Frontend (React + Vite)

### Development

```bash
# Install dependencies
cd frontend
npm install

# Start dev server
npm run dev

# Build production
npm run build

# Preview production build
npm run preview
```

### Testing

```bash
# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage

# Run specific test file
npm test -- login-form.test.tsx
```

### Linting & Formatting

```bash
# Lint
npm run lint

# Fix lint errors
npm run lint:fix

# Type check
npx tsc --noEmit
```

### Shadcn/UI

```bash
# Add new component
npx shadcn@latest add {component-name}

# Example
npx shadcn@latest add card
npx shadcn@latest add select
```

---

## Docker (Development Environment)

```bash
# Start all services (PostgreSQL, Redis, RabbitMQ)
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f

# Reset database
docker-compose down -v
docker-compose up -d
```

---

## Git

```bash
# Status
git status

# Create branch
git checkout -b feature/task-XXX-name

# Commit
git add .
git commit -m "Task XXX: Description"

# Push
git push -u origin feature/task-XXX-name

# Pull latest
git pull origin develop

# Merge develop into feature branch
git merge develop
```

---

## System Commands (Windows Git Bash)

```bash
# List files
ls -la

# Find files
find . -name "*.cs" | grep -i "user"

# Search in files
grep -r "IUserRepository" --include="*.cs"

# Create directory
mkdir -p path/to/dir

# Remove directory
rm -rf path/to/dir
```

---

## Quick Reference

| Task | Command |
|------|---------|
| Build backend | `cd backend && dotnet build` |
| Run backend | `cd backend && dotnet watch run --project src/ExoAuth.Api` |
| Test backend | `cd backend && dotnet test` |
| Run frontend | `cd frontend && npm run dev` |
| Test frontend | `cd frontend && npm test` |
| Create migration | `dotnet ef migrations add {Name} --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api` |
| Apply migration | `dotnet ef database update --project src/ExoAuth.Infrastructure --startup-project src/ExoAuth.Api` |
| Add shadcn | `cd frontend && npx shadcn@latest add {name}` |

---

## Ports

| Service | Port |
|---------|------|
| API | 5000 (HTTP), 5001 (HTTPS) |
| Frontend (Vite) | 5173 |
| PostgreSQL | 5432 |
| Redis | 6379 |
| RabbitMQ | 5672, 15672 (Management) |

---

## Environment Variables

### Backend (appsettings.json / Environment)
```
ConnectionStrings__DefaultConnection
ConnectionStrings__Redis
Jwt__Secret
Jwt__Issuer
Jwt__Audience
RabbitMq__Host
RabbitMq__Username
RabbitMq__Password
Email__SmtpServer
Email__SmtpPort
GeoIp__DatabasePath
```

### Frontend (.env)
```
VITE_API_URL=http://localhost:5000
```

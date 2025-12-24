# ExoAuth

Authentication system with permissions management.

## Tech Stack

### Backend
- .NET 8 / C#
- PostgreSQL
- Redis (caching, sessions)
- RabbitMQ (messaging)
- CQRS +  Mediator
- Clean Architecture + Vertical Slices

### Frontend
- React 18 + TypeScript
- Vite
- TanStack Query + Router
- Tailwind CSS + shadcn/ui
- React Hook Form
- Axios

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- Yarn
- Docker & Docker Compose

### Development

1. Start infrastructure:
```bash
docker-compose up -d postgres redis rabbitmq
```

2. Run backend:
```bash
cd backend
dotnet restore
dotnet run --project src/ExoAuth.Api
```

3. Run frontend:
```bash
cd frontend
yarn install
yarn dev
```

### Docker (Full Stack)

```bash
docker-compose up -d
```

## Project Structure

```
exoauth/
├── backend/           # .NET 8 API
├── frontend/          # React + Vite
├── docker/            # Dockerfiles
└── .github/           # CI/CD workflows
```

## License

MIT

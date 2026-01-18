# Contributing to ExoAuth

Thank you for your interest in contributing to ExoAuth! This document provides guidelines and information about contributing to the project.

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## How to Contribute

### Reporting Bugs

Before creating a bug report, please check existing issues to avoid duplicates. When creating a bug report, include:

- A clear and descriptive title
- Steps to reproduce the issue
- Expected behavior vs actual behavior
- Your environment (OS, .NET version, Node.js version, browser)
- Relevant logs or error messages

### Suggesting Features

Feature requests are welcome! Please:

- Check if the feature has already been requested
- Provide a clear description of the feature
- Explain the use case and why it would be valuable
- Consider if it fits the project's scope

### Pull Requests

1. **Fork the repository** and create your branch from `develop`
2. **Follow the coding standards** (see below)
3. **Write tests** for new functionality
4. **Update documentation** if needed
5. **Ensure CI passes** before requesting review

## Development Setup

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK
- Node.js 20+ and Yarn

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/exoauth.git
cd exoauth

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

### Running Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
yarn test
```

## Coding Standards

### Backend (C# / .NET)

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `dotnet format` before committing
- Write XML documentation for public APIs
- Use async/await for I/O operations
- Follow Clean Architecture patterns

### Frontend (TypeScript / React)

- Follow the existing code style (ESLint + Prettier)
- Use TypeScript strict mode
- Use functional components with hooks
- Keep components small and focused
- Use React Query for server state

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add MFA backup codes display
fix: resolve session timeout issue
docs: update API documentation
test: add unit tests for login handler
refactor: simplify token validation logic
```

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `docs/description` - Documentation
- `refactor/description` - Code refactoring

## Project Structure

```
exoauth/
├── backend/
│   └── src/
│       ├── ExoAuth.Api/            # REST API controllers
│       ├── ExoAuth.Application/    # Business logic (CQRS)
│       ├── ExoAuth.Domain/         # Entities, interfaces
│       ├── ExoAuth.Infrastructure/ # Data access, services
│       └── ExoAuth.EmailWorker/    # Background email processing
├── frontend/
│   └── src/
│       ├── features/               # Feature modules
│       ├── components/             # Shared components
│       └── routes/                 # Page routes
└── docker/                         # Dockerfiles
```

## Review Process

1. All PRs require at least one approval
2. CI must pass (build + tests)
3. Code must follow project conventions
4. Documentation must be updated if needed

## Getting Help

- Open a [Discussion](https://github.com/juljanblischke/exoauth/discussions) for questions
- Check existing issues and discussions
- Join our community (Discord link coming soon)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to ExoAuth!

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore (EmailWorker depends on Domain, Application, Infrastructure)
COPY src/ExoAuth.Domain/*.csproj ./ExoAuth.Domain/
COPY src/ExoAuth.Application/*.csproj ./ExoAuth.Application/
COPY src/ExoAuth.Infrastructure/*.csproj ./ExoAuth.Infrastructure/
COPY src/ExoAuth.EmailWorker/*.csproj ./ExoAuth.EmailWorker/
RUN dotnet restore ./ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj

# Copy everything and build
COPY src/ ./
RUN dotnet publish ./ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
WORKDIR /app

# Create directory for Data Protection keys
RUN mkdir -p /app/data/keys

ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/ExoAuth.EmailWorker", "--no-launch-profile"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS production
WORKDIR /app

# Create directory for Data Protection keys
RUN mkdir -p /app/data/keys

# Copy templates
COPY templates/emails/ ./templates/emails/

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExoAuth.EmailWorker.dll"]

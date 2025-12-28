# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/ExoAuth.EmailWorker/*.csproj ./ExoAuth.EmailWorker/
RUN dotnet restore ./ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj

# Copy everything and build
COPY src/ExoAuth.EmailWorker/ ./ExoAuth.EmailWorker/
RUN dotnet publish ./ExoAuth.EmailWorker/ExoAuth.EmailWorker.csproj -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
WORKDIR /app
ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/ExoAuth.EmailWorker", "--no-launch-profile"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS production
WORKDIR /app

# Copy templates
COPY templates/emails/ ./templates/emails/

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExoAuth.EmailWorker.dll"]

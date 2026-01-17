# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/ExoAuth.Domain/*.csproj ./ExoAuth.Domain/
COPY src/ExoAuth.Application/*.csproj ./ExoAuth.Application/
COPY src/ExoAuth.Infrastructure/*.csproj ./ExoAuth.Infrastructure/
COPY src/ExoAuth.Api/*.csproj ./ExoAuth.Api/
RUN dotnet restore ./ExoAuth.Api/ExoAuth.Api.csproj

# Copy everything and build
COPY src/ ./
RUN dotnet publish ./ExoAuth.Api/ExoAuth.Api.csproj -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Create directories for persistent data
RUN mkdir -p /app/data/keys /app/data/geoip

ENTRYPOINT ["dotnet", "run", "--project", "src/ExoAuth.Api", "--no-launch-profile"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS production
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Create directories for persistent data
RUN mkdir -p /app/data/keys /app/data/geoip

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExoAuth.Api.dll"]

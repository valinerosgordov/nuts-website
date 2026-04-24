# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5100

# Install curl for healthcheck + non-root user for runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -u 1001 -m appuser \
    && mkdir -p /app/data /app/wwwroot/uploads \
    && chown -R appuser:appuser /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (cached if csproj files unchanged)
COPY global.json .
COPY src/Nuts.Domain/Nuts.Domain.csproj Nuts.Domain/
COPY src/Nuts.Application/Nuts.Application.csproj Nuts.Application/
COPY src/Nuts.Infrastructure/Nuts.Infrastructure.csproj Nuts.Infrastructure/
COPY src/Nuts.Api/Nuts.Api.csproj Nuts.Api/
RUN dotnet restore Nuts.Api/Nuts.Api.csproj

# Build + publish
COPY src/Nuts.Domain/ Nuts.Domain/
COPY src/Nuts.Application/ Nuts.Application/
COPY src/Nuts.Infrastructure/ Nuts.Infrastructure/
COPY src/Nuts.Api/ Nuts.Api/
RUN dotnet publish Nuts.Api/Nuts.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .
# Seed catalog is read-only reference data (optional)
COPY --chown=appuser:appuser products_catalog.json ./products_catalog.json

USER appuser
ENV ASPNETCORE_URLS=http://+:5100
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__Default="Data Source=/app/data/nuts.db"
VOLUME ["/app/data", "/app/wwwroot/uploads"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:5100/health/live || exit 1

ENTRYPOINT ["dotnet", "Nuts.Api.dll"]

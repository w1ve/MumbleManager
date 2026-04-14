# ── Stage 1: Build React SPA ──────────────────────────────────────────────────
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json ./
RUN npm install
COPY frontend/ ./
RUN npm run build

# ── Stage 2: Publish .NET API ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS backend-build
WORKDIR /app/backend
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
# Copy frontend build LAST so backend cache busts when frontend changes
COPY --from=frontend-build /app/backend/wwwroot ./wwwroot
RUN dotnet publish -c Release -o /publish

# ── Stage 3: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libssl3 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=backend-build /publish ./
VOLUME ["/data"]
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "MumbleManager.Api.dll"]

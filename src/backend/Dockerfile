# Single-arch build. CI builds amd64 and arm64 separately and joins them into
# one manifest, per the division deploy chain — do not add buildx QEMU here.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project files alone so a code-only change reuses the
# cached restore layer.
COPY src/backend/*.slnx ./src/backend/
COPY src/backend/FrostAura.Integrations.Api/*.csproj ./src/backend/FrostAura.Integrations.Api/
COPY src/backend/FrostAura.Integrations.Core/*.csproj ./src/backend/FrostAura.Integrations.Core/
COPY src/backend/FrostAura.Integrations.Woolworths/*.csproj ./src/backend/FrostAura.Integrations.Woolworths/
RUN dotnet restore src/backend/FrostAura.Integrations.Api/FrostAura.Integrations.Api.csproj

COPY src/backend/ ./src/backend/
RUN dotnet publish src/backend/FrostAura.Integrations.Api/FrostAura.Integrations.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Never run as root: this service holds no secrets, but it does make outbound
# authenticated calls on behalf of callers. The aspnet image already ships a
# non-root "app" user ($APP_UID) — creating another one fails the build.
COPY --from=build --chown=$APP_UID:$APP_UID /app .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=1
EXPOSE 8080

# No in-image HEALTHCHECK on purpose: the runtime image ships no curl or wget,
# so any CMD-based probe would report unhealthy forever. Liveness is checked
# externally against GET /api/health.

ENTRYPOINT ["dotnet", "FrostAura.Integrations.Api.dll"]

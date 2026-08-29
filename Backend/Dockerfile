# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first and restore - Docker caches this layer, so
# `docker build` skips re-downloading NuGet packages unless a .csproj changed.
COPY src/StartupIMS.Domain/*.csproj src/StartupIMS.Domain/
COPY src/StartupIMS.Shared/*.csproj src/StartupIMS.Shared/
COPY src/StartupIMS.Infrastructure/*.csproj src/StartupIMS.Infrastructure/
COPY src/StartupIMS.API/*.csproj src/StartupIMS.API/
RUN dotnet restore src/StartupIMS.API/StartupIMS.API.csproj

# Now copy everything else and build
COPY src/ src/
RUN dotnet publish src/StartupIMS.API/StartupIMS.API.csproj -c Release -o /app --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Uploaded pitch decks and Serilog files need a writable, persisted location -
# mount a volume here in docker-compose/deployment rather than relying on the
# container's own filesystem, which is ephemeral.
RUN mkdir -p /app/App_Data/uploads /app/logs

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StartupIMS.API.dll"]

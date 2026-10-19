# Build from the repository root:
#   docker build -f src/FlipZA.Engine/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY src/FlipZA.Core/FlipZA.Core.csproj src/FlipZA.Core/
COPY src/FlipZA.Infrastructure/FlipZA.Infrastructure.csproj src/FlipZA.Infrastructure/
COPY src/FlipZA.Engine/FlipZA.Engine.csproj src/FlipZA.Engine/
RUN dotnet restore src/FlipZA.Engine/FlipZA.Engine.csproj

COPY src/ src/
RUN dotnet publish src/FlipZA.Engine/FlipZA.Engine.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production \
    ConnectionStrings__FlipZA="Data Source=/data/flipza.db"

# The .NET images ship a non-root "app" user; /data holds the SQLite database.
RUN mkdir -p /data && chown "$APP_UID" /data
USER $APP_UID
VOLUME /data

COPY --from=build /app .
ENTRYPOINT ["dotnet", "FlipZA.Engine.dll"]

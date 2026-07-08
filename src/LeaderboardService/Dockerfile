# Build context is the repository root.
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first (copy only manifests for better layer caching).
COPY global.json Directory.Build.props .editorconfig ./
COPY RpsArena.sln ./
COPY src/Shared/RpsArena.Contracts/*.csproj src/Shared/RpsArena.Contracts/
COPY src/LeaderboardService/RpsArena.Leaderboard.Domain/*.csproj src/LeaderboardService/RpsArena.Leaderboard.Domain/
COPY src/LeaderboardService/RpsArena.Leaderboard.Application/*.csproj src/LeaderboardService/RpsArena.Leaderboard.Application/
COPY src/LeaderboardService/RpsArena.Leaderboard.Infrastructure/*.csproj src/LeaderboardService/RpsArena.Leaderboard.Infrastructure/
COPY src/LeaderboardService/RpsArena.Leaderboard.Api/*.csproj src/LeaderboardService/RpsArena.Leaderboard.Api/
RUN dotnet restore src/LeaderboardService/RpsArena.Leaderboard.Api/RpsArena.Leaderboard.Api.csproj

# Build + publish.
COPY src/ src/
RUN dotnet publish src/LeaderboardService/RpsArena.Leaderboard.Api/RpsArena.Leaderboard.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl lets the compose healthcheck probe /health/ready from inside the container.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Run as the image's non-root user.
USER $APP_UID
ENTRYPOINT ["dotnet", "RpsArena.Leaderboard.Api.dll"]

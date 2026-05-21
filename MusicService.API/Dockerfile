FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MusicServerLab2.sln", "./"]
COPY ["global.json", "./"]
COPY ["MusicService.API/MusicService.API.csproj", "MusicService.API/"]
COPY ["MusicService.Application/MusicService.Application.csproj", "MusicService.Application/"]
COPY ["MusicService.Domain/MusicService.Domain.csproj", "MusicService.Domain/"]
COPY ["MusicService.Infrastructure/MusicService.Infrastructure.csproj", "MusicService.Infrastructure/"]
COPY ["tests/tests.csproj", "tests/"]

RUN dotnet restore "MusicService.API/MusicService.API.csproj"

COPY . .
RUN dotnet publish "MusicService.API/MusicService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MusicService.API.dll"]

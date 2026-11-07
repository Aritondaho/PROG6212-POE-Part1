# RaceDay MVC web application
# Build from the repository root so the solution files are in the build context:
#   docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
#
# Multi-stage build: the SDK image restores and publishes, and only the published output is copied into the
# much smaller ASP.NET runtime image, which runs as the image's built-in non-root "app" user.

# ---- Build stage --------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project and NuGet files, so this layer is cached until dependencies change.
COPY NuGet.config ./
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# Then copy the source and publish a Release build.
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Listen on plain HTTP inside the container; publish the port with -p. HTTPS belongs at a proxy in front.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Where the RaceDay API is. host.docker.internal reaches the API running on the host machine.
    RaceDayApi__BaseUrl=http://host.docker.internal:5285/ \
    # Where the visitor's browser loads locally stored API images from.
    RaceDayApi__PublicUrl=http://localhost:5285/

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root user that the official .NET images provide.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]

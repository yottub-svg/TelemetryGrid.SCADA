# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TelemetryGrid.SCADA.sln .
COPY src ./src
RUN dotnet restore src/TelemetryGrid.Api/TelemetryGrid.Api.csproj
RUN dotnet publish src/TelemetryGrid.Api/TelemetryGrid.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /out .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TelemetryGrid.Api.dll"]

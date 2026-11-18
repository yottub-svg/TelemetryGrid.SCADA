param(
    [string]$Project = "src/TelemetryGrid.Api/TelemetryGrid.Api.csproj",
    [string]$OutputDirectory = "artifacts/module-07-profile"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
dotnet build $Project -c Release
dotnet-trace collect --format Speedscope --output "$OutputDirectory/telemetry.speedscope.json" -- dotnet run --project $Project -c Release --no-build
Write-Host "Откройте telemetry.speedscope.json в https://www.speedscope.app/"

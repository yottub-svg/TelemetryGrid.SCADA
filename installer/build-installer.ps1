param(
    [string]$Configuration = "Release",
    [string]$ApiUrl = "https://localhost:7040"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "src/TelemetryGrid.WinForms/TelemetryGrid.WinForms.csproj"
$publish = Join-Path $root "src/TelemetryGrid.WinForms/bin/$Configuration/net8.0-windows/publish"
$out = Join-Path $root "artifacts/installer"

dotnet publish $project -c $Configuration -r win-x64 --self-contained false -o $publish
New-Item -ItemType Directory -Path $out -Force | Out-Null

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    throw "WiX v4/v5 CLI не найден. Установите WiX Toolset перед сборкой MSI."
}

wix build (Join-Path $PSScriptRoot "TelemetryGrid.Installer/Package.wxs") `
    -d PublishDir=$publish `
    -ext WixToolset.Util.wixext `
    -o (Join-Path $out "TelemetryGrid.SCADA.msi")

Write-Host "MSI создан: $out/TelemetryGrid.SCADA.msi"
Write-Host "Тихая установка: msiexec /i TelemetryGrid.SCADA.msi /qn API_URL=$ApiUrl /l*v install.log"

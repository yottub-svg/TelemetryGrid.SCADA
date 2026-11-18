param(
    [Parameter(Mandatory)] [string]$MsiPath,
    [string]$ApiUrl = "https://localhost:7040",
    [string]$LogPath = ".\install.log"
)

$ErrorActionPreference = "Stop"
$arguments = "/i `"$MsiPath`" /qn /norestart API_URL=$ApiUrl /l*v `"$LogPath`""
$process = Start-Process msiexec.exe -ArgumentList $arguments -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "MSI install failed with exit code $($process.ExitCode)" }
Write-Host "Silent install succeeded; log=$LogPath"

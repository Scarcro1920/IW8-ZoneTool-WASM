param(
  [string]$InstallPath = "C:\IW8ZoneToolRemote"
)
$ErrorActionPreference = "Stop"
$serviceName = "IW8ZoneToolRemoteCompiler"
$source = $PSScriptRoot
if (!(Test-Path (Join-Path $source "IW8ZoneTool.Server.exe"))) {
  throw "Extract the iw8-zonetool-remote-server-win-x64 artifact into a folder before running this script."
}
$compiler = Join-Path $source "iw8-zonetool.exe"
$replay = Join-Path $source "game_dx12_ship_replay.exe"
if (!(Test-Path $compiler)) { throw "iw8-zonetool.exe is missing. Copy the native compiler into this folder." }
if (!(Test-Path $replay)) { throw "game_dx12_ship_replay.exe is missing. Copy your supported Replay 1.20.4 executable into this folder." }
$expected = "68fb1cbcb2924182724004039de55a4c50152bb6561803c4898b7930b38132f0"
$actual = (Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $expected) { throw "Replay SHA-256 mismatch: $actual" }

New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
Copy-Item (Join-Path $source "*") $InstallPath -Recurse -Force
$configPath = Join-Path $InstallPath "appsettings.json"
$config = Get-Content (Join-Path $InstallPath "appsettings.example.json") -Raw | ConvertFrom-Json
$config.Compiler.Executable = Join-Path $InstallPath "iw8-zonetool.exe"
$config.Compiler.ReplayExecutable = Join-Path $InstallPath "game_dx12_ship_replay.exe"
$config.Compiler.WorkDirectory = Join-Path $InstallPath "jobs"
$config.Security.ApiToken = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$config | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $configPath

$serviceExe = Join-Path $InstallPath "IW8ZoneTool.Server.exe"
$binPath = '"' + $serviceExe + '"'
& sc.exe stop $serviceName 2>$null | Out-Null
& sc.exe delete $serviceName 2>$null | Out-Null
& sc.exe create $serviceName binPath= $binPath start= auto DisplayName= "IW8 ZoneTool Remote Fastfile Compiler" | Out-Null
& sc.exe description $serviceName "Authenticated remote Replay fastfile compiler for IW8 ZoneTool WASM." | Out-Null
& sc.exe start $serviceName | Out-Null
Write-Host "Service installed. API listens on port 8787." -ForegroundColor Green
Write-Host "API token (save it now; appsettings.json also contains it):" -ForegroundColor Yellow
Write-Host $config.Security.ApiToken
Write-Host "Configure HTTPS reverse proxy before exposing this service to the Internet." -ForegroundColor Yellow

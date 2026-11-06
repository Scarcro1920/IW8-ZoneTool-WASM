# IW8 ZoneTool native launcher — Windows PowerShell
# Place this script beside iw8-zonetool.exe, then run it.

$ErrorActionPreference = "Stop"
$compiler = Join-Path $PSScriptRoot "iw8-zonetool.exe"
if (!(Test-Path $compiler)) {
  Write-Host "iw8-zonetool.exe is missing. Download the iw8-zonetool-windows-x64 artifact first." -ForegroundColor Red
  Read-Host "Press Enter to exit"
  exit 1
}

Add-Type -AssemblyName System.Windows.Forms

function Select-Folder([string]$description) {
  $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
  $dialog.Description = $description
  $dialog.ShowNewFolderButton = $true
  if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return $null }
  return $dialog.SelectedPath
}

function Select-Exe([string]$description) {
  $dialog = New-Object System.Windows.Forms.OpenFileDialog
  $dialog.Title = $description
  $dialog.Filter = "Replay executable (game_dx12_ship_replay.exe)|game_dx12_ship_replay.exe|Executable (*.exe)|*.exe"
  $dialog.CheckFileExists = $true
  if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return $null }
  return $dialog.FileName
}

function Select-Collision([string]$description) {
  $dialog = New-Object System.Windows.Forms.OpenFileDialog
  $dialog.Title = $description
  $dialog.Filter = "MWCOLL collision (*.mwcoll;*.bin)|*.mwcoll;*.bin|All files (*.*)|*.*"
  $dialog.CheckFileExists = $true
  if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return $null }
  return $dialog.FileName
}

Write-Host "IW8 ZoneTool — Replay 1.20 map compiler" -ForegroundColor Cyan
$dump = Select-Folder "Select the prepared map dump folder"
if (!$dump) { exit 0 }

$map = Read-Host "Map ID (lowercase, for example mp_example)"
if ($map -notmatch '^mp_[a-z0-9_]{1,12}$') {
  throw "Invalid map ID. Use mp_ followed by lowercase letters, numbers or underscores (max 15 chars)."
}

$output = Select-Folder "Select the output folder (it must be empty or contain this map's previous output)"
if (!$output) { exit 0 }

$asset = Join-Path (Join-Path $dump "maps/mp") ($map + ".d3dbsp")
$required = @(
  (Join-Path $dump ($map + "_iw8_ents.txt")),
  ($asset + ".render.json"),
  ($asset + ".lighting.json")
)
$missing = @($required | Where-Object { !(Test-Path $_ -PathType Leaf) })
if ($missing.Count -gt 0) {
  Write-Host "Dump incomplet. Fichiers requis absents :" -ForegroundColor Red
  $missing | ForEach-Object { Write-Host " - $_" }
  Write-Host "Un paquet *_geometry_staging.zip exporté par le navigateur n'est pas un dump natif compilable." -ForegroundColor Yellow
  Read-Host "Press Enter to close"
  exit 2
}

$replay = Select-Exe "Select the exact Replay 1.20 executable"
$collision = Select-Collision "Select native MWCOLL collision input (Cancel only if a valid .havok already exists in the dump)"
$args = @("build-map", $dump, $map, "-o", $output)
if ($replay) { $args += @("--replay", $replay) }
if ($collision) {
  if (!$replay) { throw "Replay executable is required when supplying a native collision input." }
  $args += @("--collision", $collision)
}

Write-Host ""
Write-Host "Launching native compiler..." -ForegroundColor Yellow
Write-Host ('"{0}" {1}' -f $compiler, ($args -join " "))
& $compiler @args
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
  Write-Host "Build completed. Running output validation..." -ForegroundColor Yellow
  & $compiler validate-output $output $map
  $validateCode = $LASTEXITCODE
  if ($validateCode -eq 0) {
    Write-Host "SUCCESS: output passed validate-output." -ForegroundColor Green
  } else {
    Write-Host "WARNING: build command succeeded, but validate-output failed ($validateCode)." -ForegroundColor Red
    $exitCode = $validateCode
  }
} else {
  Write-Host "Build failed with exit code $exitCode. Read the compiler output above." -ForegroundColor Red
}
Read-Host "Press Enter to close"
exit $exitCode

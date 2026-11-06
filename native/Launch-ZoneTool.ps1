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

Write-Host "IW8 ZoneTool — Replay 1.20 map compiler" -ForegroundColor Cyan
$dump = Select-Folder "Select the prepared map dump folder"
if (!$dump) { exit 0 }

$map = Read-Host "Map ID (lowercase, for example mp_example)"
if ($map -notmatch '^mp_[a-z0-9_]{1,12}$') {
  throw "Invalid map ID. Use mp_ followed by lowercase letters, numbers or underscores (max 15 chars)."
}

$output = Select-Folder "Select the output folder (it must be empty or contain this map's previous output)"
if (!$output) { exit 0 }

$replay = Select-Exe "Select Replay 1.20 executable (Cancel if dump already contains baked Havok collision)"
$args = @("build-map", $dump, $map, "-o", $output)
if ($replay) { $args += @("--replay", $replay) }

Write-Host ""
Write-Host "Launching native compiler..." -ForegroundColor Yellow
Write-Host ('"{0}" {1}' -f $compiler, ($args -join " "))
& $compiler @args
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
  Write-Host "Build completed. Validate the package with:" -ForegroundColor Green
  Write-Host ('"{0}" validate-output "{1}" "{2}"' -f $compiler, $output, $map)
} else {
  Write-Host "Build failed with exit code $exitCode. Read the compiler output above." -ForegroundColor Red
}
Read-Host "Press Enter to close"
exit $exitCode

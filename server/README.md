# IW8 ZoneTool remote fastfile compiler

This is the Windows-side compilation service used by the browser editor. The GitHub Pages site sends the exported dump ZIP to this API; the server runs the real native ZoneTool and Replay, validates the result, and returns a ZIP containing the compiled output.

## Important architecture

The browser cannot execute a Windows EXE. The native compiler and Replay therefore run on a Windows host that you control. No compiler executable or Replay binary is downloaded into the browser. The site only uploads the generated dump and downloads the validated result.

## Build and install

1. In GitHub Actions, run **Build IW8 remote compiler server** and download **iw8-zonetool-remote-server-win-x64**.
2. Extract the artifact on a Windows x64 machine with .NET 8 runtime installed.
3. Copy the native **iw8-zonetool.exe** into that folder (download it from the **Build native IW8 ZoneTool (Windows)** workflow artifact).
4. Copy your exact supported **game_dx12_ship_replay.exe** into the same folder.
5. Open an elevated PowerShell in that folder and run: Set-ExecutionPolicy -Scope Process Bypass; .\Install-Server.ps1
6. The installer checks Replay SHA-256, creates a random API token and installs a Windows service listening on port 8787.
7. Put the service behind HTTPS (for example IIS reverse proxy with a valid TLS certificate). Do not expose plain HTTP or the API token to the public Internet.
8. In IW8 ZoneTool WASM, enter the HTTPS API base URL and the API token printed by the installer.

The server needs an inbound firewall rule for the HTTPS reverse proxy. Keep port 8787 private to the proxy where possible. The configured CORS origin is the GitHub Pages origin.

## API

- GET /health: health probe.
- POST /api/jobs: multipart form fields map and dump (ZIP), bearer-token authenticated.
- GET /api/jobs/{id}: job status and compiler log.
- GET /api/jobs/{id}/download: validated output ZIP.

The API validates map IDs, limits upload and expanded ZIP size, blocks ZIP path traversal, serializes builds, checks the Replay binary hash, invokes the compiler without a shell, and only marks a job complete after validate-output succeeds.

## Limitations

- Jobs are held in memory; restarting the service loses status records. Files remain in the jobs directory for manual cleanup.
- One compile runs at a time.
- The editor's generated collision remains approximate AABB graybox collision. Successful fastfile validation does not guarantee a fully playable map.
- The server must be hosted by you; this repository cannot provision a public Windows machine or HTTPS domain automatically.

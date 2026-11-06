# Native Windows compiler

The WASM page is the map editor and browser-side preflight tool. The actual Replay fastfile compiler is the original Windows x64 ZoneTool program; it is built separately because its Replay/Havok integration uses Win32 executable-memory and DirectX APIs that are not available inside browser WASM.

## Build it

1. Open the repository's **Actions** tab.
2. Run **Build native IW8 ZoneTool (Windows)** with **Run workflow**, or push a change to the source archive/workflow.
3. Open the successful run and download artifact **iw8-zonetool-windows-x64**.
4. Extract the artifact.

## Compile a prepared map

Place `Launch-ZoneTool.ps1` beside `iw8-zonetool.exe`, then run it in Windows PowerShell. It opens dialogs for the prepared dump folder, output folder, and optional Replay executable. The script invokes the real command:

```text
iw8-zonetool.exe build-map <dump> <map> -o <output> [--replay <Replay.exe>]
```

The dump must follow the original [INPUT_FORMAT.md](../docs/INPUT_FORMAT.md) schema and include valid IW8 material/techset data and either a prepared Replay Havok file or native collision inputs as required. Choosing Replay does not magically create missing render/material/techset assets.

After building, validate the package:

```powershell
.\iw8-zonetool.exe validate-output "C:\path\to\output" mp_example
```

## Browser limitation

A browser file picker intentionally does not expose the selected executable's full filesystem path, and a web page cannot launch a local EXE. The page can inspect the selected file's bytes and SHA-256; the PowerShell launcher uses native Windows file dialogs to provide the actual path to ZoneTool. No executable is uploaded to the website.

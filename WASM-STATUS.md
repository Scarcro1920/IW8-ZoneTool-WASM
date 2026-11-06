# IW8 ZoneTool WASM — bootstrap milestone

This repository now contains a reproducible Emscripten build and a browser-side dump preflight tool.

## Implemented
- Compiles `wasm/wasm_main.cpp` into `zonetool.js` and `zonetool.wasm`.
- Browser UI selects a prepared dump folder.
- WASM checks for entity, render, material, techset and lighting input files.

## Not implemented
This is **not yet the native ZoneTool `build-map` compiler**. It does not serialize IW8 assets or produce valid Replay 1.20 fastfiles. Windows APIs, DirectX-related code, and Replay/Havok collision baking still require a proper port. Do not treat the preflight PASS as a compiled map.

Run GitHub Actions → **Build IW8 ZoneTool WASM bootstrap**. Download `IW8-ZoneTool-WASM-bootstrap` and serve the files over HTTP(S) so the browser can load the ES module and WASM binary.

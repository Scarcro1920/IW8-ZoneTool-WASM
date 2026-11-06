# IW8 ZoneTool WASM

This repository is being prepared for a browser/WebAssembly port of the native IW8 ZoneTool.

## Important status

The original ZIP is retained unchanged. The first engineering step is a reproducible source audit in GitHub Actions. This is **not yet a working WASM compiler**.

A browser build must isolate and port the native `build-map` pipeline. Windows-only APIs, Replay executable integration, DirectX-specific code, and the Replay/Havok collision bake cannot be assumed to work in WebAssembly. These components must be identified and either ported or replaced with compatible implementations.

## Current workflow

- `.github/workflows/wasm-audit.yml` extracts the original archive and produces an audit report plus an extracted-source artifact.
- Run it from **Actions → IW8 ZoneTool WASM - Source Audit → Run workflow**.
- Download the `IW8-ZoneTool-WASM-Audit` artifact to inspect the report and source tree.

## Target architecture

1. Keep the native source and compiler behavior as the reference.
2. Identify the smallest portable library boundary around `build-map`.
3. Replace platform-specific file I/O with standard C++ / Emscripten virtual filesystem APIs.
4. Port the render/material/techset/lighting/entity serializers.
5. Resolve collision and Havok baking separately; do not emit fake collision data.
6. Compile and test a WASM module, then validate produced fastfiles against Replay 1.20.

The goal is native IW8 Replay 1.20 output, not an IW3-to-IW8 conversion workflow.

# Ember — Dependencies & Plugins

> Audit item EMB-051. What ships under `Assets/Plugins/**`, where it comes from, and how it's
> imported. Plugin import settings are configured deterministically by `Windows64BuildMenu.cs`
> (`ConfigureOnnxNativePlugins`) so a fresh clone + build self-heals stub `.meta` files.

## Native (Assets/Plugins/x86_64/) — Editor + Win64, x86_64
| Plugin | Purpose | Source | LFS |
|---|---|---|---|
| `onnxruntime.dll` (+ `cuda/` variants, `_providers_cuda/_shared/_tensorrt`) | ONNX Runtime for image gen (SDXL-Turbo / SD1.5-LCM) | Microsoft ONNX Runtime 1.x | yes |
| `cuda/cudnn*.dll` (×10, ~830 MB) | cuDNN 9 for CUDA execution provider | NVIDIA cuDNN 9 archive | **gitignored** — devs install locally |
| `llama.dll`, `ggml*.dll`, `mtmd.dll` | llama.cpp native backend for the local LLM | llama.cpp build | yes |

## Managed (Assets/Plugins/x86_64/ + Assets/Plugins/NuGet/)
| Plugin | Purpose |
|---|---|
| `LLamaSharp.dll` (0.27) | managed binding over llama.cpp (GGUF inference) |
| `Microsoft.ML.OnnxRuntime.dll` | managed ONNX runtime binding |
| `Microsoft.Bcl.*` (AsyncInterfaces, HashCode, Numerics, Memory, TimeProvider) | BCL polyfills LLamaSharp's ecosystem needs |
| `Microsoft.Extensions.*`, `CommunityToolkit.HighPerformance`, `System.Numerics.Tensors` | LLamaSharp transitive deps |
| `McpPlugin*.dll`, `Microsoft.AspNetCore.SignalR.*` | Unity-MCP editor tooling (editor-only) |

## Build defines
- `USE_LLAMASHARP` — gates real GGUF inference (ON in the Win64 build menu). Without it,
  `NativeLlmClient` falls back to canned text.
- `EXCLUDE_BCL_MEMORY`, `EXCLUDE_BCL_NUMERICS` — historical excludes from the ai.assistant era.

## Rules
- **Native plugins are configured in code**, not by hand-editing `.meta`. If a `cudnn*.dll` / ONNX /
  llama `.meta` is a stub, the next `Ember/Build/Windows64` run rewrites it (see
  `ConfigureOnnxNativePlugins`).
- **cuDNN is gitignored** (~830 MB) — never commit. Devs install from the NVIDIA cuDNN 9 archive into
  `Assets/Plugins/x86_64/cuda/`.
- Don't bump a plugin/DLL version casually — the BCL ecosystem versions are matched to LLamaSharp 0.27;
  a mismatch resurfaces CS1705 assembly-version conflicts. Verify a full Win64 build after any change.
- Editor-only tooling (`McpPlugin*`, SignalR) must stay Editor-scoped (not bundled into the player).

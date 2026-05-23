# AI Stack Setup — Ember CRPG

This guide describes how to provision the **pure C# / Unity-free** inference
stack that Ember CRPG ships with. The backend uses two libraries:

- **LLamaSharp 0.18.0** — local GGUF inference for the Qwen 2.5 Instruct line.
  Runs in `EmberCrpg.Simulation` (`noEngineReferences=true`) — Unity is not
  involved at runtime.
- **Microsoft.ML.OnnxRuntime 1.18.0** — image generation (SDXL Turbo or
  SD 1.5 LCM ONNX) and NPC-memory embeddings (all-MiniLM-L6-v2 ONNX).
  Also pure managed; runs in `EmberCrpg.Simulation` too.

The Presentation tier (`EmberCrpg.Presentation`) provides `ModelBootstrap`,
which downloads and verifies the model bundle on first launch, and
`ForgeBootstrap`, which wires everything together via `ForgeLocator`.

---

## 1) Native DLL drop (one-time, per developer machine)

DLLs are **not auto-downloaded**. Drop them into `Assets/Plugins/x86_64/`:

| File                                       | Source NuGet package                                     |
|--------------------------------------------|----------------------------------------------------------|
| `LLamaSharp.dll`                           | `LLamaSharp` 0.18.0                                      |
| `llama.dll` (Windows x64)                  | `LLamaSharp.Backend.Cpu` 0.18.0 (`runtimes\win-x64\native\`) |
| `Microsoft.ML.OnnxRuntime.dll`             | `Microsoft.ML.OnnxRuntime` 1.18.0                        |
| `onnxruntime.dll` (Windows x64)            | `Microsoft.ML.OnnxRuntime` 1.18.0 (`runtimes\win-x64\native\`) |
| `onnxruntime_providers_shared.dll`         | `Microsoft.ML.OnnxRuntime` 1.18.0 (same `runtimes` dir)   |
| `Microsoft.ML.Tokenizers.dll`              | `Microsoft.ML.Tokenizers` 0.21.0                         |

How to obtain (Windows):

1. Download each NuGet `.nupkg` (rename `.nupkg` to `.zip` and extract):
   - https://www.nuget.org/packages/LLamaSharp/0.18.0
   - https://www.nuget.org/packages/LLamaSharp.Backend.Cpu/0.18.0
   - https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime/1.18.0
   - https://www.nuget.org/packages/Microsoft.ML.Tokenizers/0.21.0
2. Copy the managed DLLs from `lib/net8.0/` (or `lib/netstandard2.0/`) and
   native DLLs from `runtimes/win-x64/native/` into `Assets/Plugins/x86_64/`.
3. **Add scripting define symbols** in `ProjectSettings → Player → Other
   Settings → Scripting Define Symbols`:
   ```
   USE_LLAMASHARP;USE_ONNX_RUNTIME
   ```
   Unity will recompile, the `#if USE_*` blocks in `NativeLlmClient`,
   `OnnxAssetForge`, and `EmbeddingClient` will activate, and the placeholder
   fallback paths will no longer run.

For Linux / macOS, also drop the matching `.so` / `.dylib` from the same NuGet
packages into `Assets/Plugins/x86_64/` (Unity's plugin importer routes them
per-platform based on the auto-generated `.meta`).

---

## 2) Model bundle (downloaded on first launch)

Models live under `Application.persistentDataPath/Models/`. The manifest lives
at `Assets/StreamingAssets/Models/manifest.json` and is read by
`ModelBootstrap` on the first frame.

| Entry                       | What it is                                  | License             |
|-----------------------------|---------------------------------------------|---------------------|
| `qwen2.5-3b-q4km`           | Qwen 2.5 3B Instruct (Q4_K_M GGUF) primary  | Apache 2.0          |
| `qwen2.5-1.5b-q4km`         | Qwen 2.5 1.5B Instruct (Q4_K_M GGUF) low-VRAM fallback | Apache 2.0 |
| `sdxl-turbo-*` (4 entries)  | SDXL Turbo FP16 ONNX (text enc x2, U-Net, VAE, tokenizer) | CC-BY-NC 4.0 (non-commercial only) |
| `sd15-lcm-unet`             | SD 1.5 LCM (low-VRAM image gen fallback)    | CreativeML Open RAIL++-M |
| `minilm-l6-v2-*` (2 entries)| Sentence-transformer (NPC memory retrieval) | Apache 2.0          |

> **SDXL Turbo is non-commercial.** Shipping Ember commercially with SDXL Turbo
> baked in is against its license. For a paid release, swap to a SD 1.5 LCM
> bundle (Open-RAIL++-M permits commercial use) and adjust `OnnxAssetForge`'s
> default `OnnxDiffusionFlavor` to `Sd15Lcm`.

### Manual model placement (dev / offline)

Skip the download step entirely by populating
`Application.persistentDataPath/Models/` yourself:

- Windows: `%UserProfile%\AppData\LocalLow\<Company>\<Product>\Models\`
- macOS:   `~/Library/Application Support/<Company>/<Product>/Models/`
- Linux:   `~/.config/unity3d/<Company>/<Product>/Models/`

Place files according to the `path` fields in `manifest.json`:

```
Models/
  qwen2.5-3b-instruct-q4_k_m.gguf
  qwen2.5-1.5b-instruct-q4_k_m.gguf
  sdxl-turbo/
    text_encoder.onnx
    text_encoder_2.onnx
    unet.onnx
    vae_decoder.onnx
    tokenizer.json
  sd15-lcm/
    unet.onnx
  minilm-l6-v2/
    model.onnx
    tokenizer.json
```

### Hashes (SHA-256)

`manifest.json` currently has `"sha256": "TBD"` placeholders. `ModelBootstrap`
treats any of `""`, `"TBD"`, `"PENDING"`, or `placeholder*` as "skip hash
check". Once the first official ship is locked, compute hashes via
`ModelManifest.ComputeSha256(filePath)` and update the manifest.

---

## 3) First launch — what happens

1. `ModelBootstrap.Awake()` reads
   `Assets/StreamingAssets/Models/manifest.json` over `UnityWebRequest` so it
   also works inside the Android JAR.
2. `ModelManifest.VerifyAllPresent` checks each entry against
   `persistentDataPath/Models/`. Missing or hash-mismatched entries are
   reported.
3. For each missing entry with a non-empty `url`, `DownloadHandlerFile`
   streams the bytes to disk while the UI shows progress.
4. SHA-256 is recomputed and compared to the manifest. Mismatches delete the
   file (so the next launch retries the download).
5. Resolved paths get handed to `OnnxAssetForge`, `NativeLlmClient`, and
   `EmbeddingClient` via `ForgeLocator`. Gameplay runs in **placeholder
   mode** (deterministic stub PNGs, HTTP-Ollama LLM fallback) until the
   download completes.

---

## 4) Fallback chain

In order of preference:

1. **Native (LLamaSharp + ONNX Runtime)** — bundled.
2. **HTTP (Ollama for LLM, ComfyUI for image)** — useful for dev iteration.
3. **Placeholder** — deterministic stub responses so the game never hard-fails.

The fallback path is encoded in:
- `LlmRoutingService` (`NativeLlmClient` → `LocalQwenClient`)
- `ForgeBootstrap` (`OnnxAssetForge` → `ComfyUiAssetForge`)

---

## 5) CI considerations

CI lives in `.github/workflows/unity-test.yml`. The workflow strips both
`com.unity.ai.assistant` (gitignored embedded editor tool) and
`com.unity.ai.inference` (re-added by Unity Editor because the embedded
`com.unity.ai.assistant` references `Unity.InferenceEngine`) from
`Packages/manifest.json` before invoking the Unity test runner — neither is
required by gameplay or tests.

Pure-C# tests run via `tools/validation/run-validation.sh --mode fallback`
without Unity at all, exercising the `Simulation` and `Domain` asmdefs.
`OnnxAssetForge`, `EmbeddingClient`, and `ModelManifest` all behave in
placeholder mode under fallback, so harness coverage is unaffected by the
native DLL drop.

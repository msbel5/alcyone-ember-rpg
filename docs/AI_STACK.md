# Ember — AI Stack (authoritative)

> Audit items EMB-043 / EMB-044 / EMB-006. The single source of truth for which AI models Ember
> uses, the fallback order, and the provider policy. Supersedes any older "Qwen3" / cloud mentions.

> **Status — Real local-LLM round-trip: UNVERIFIED in source-only checkouts** (needs `git lfs pull`
> to resolve the GGUF + native DLLs, then a runtime run). Likewise generated-art (SDXL/SD1.5) output
> is UNVERIFIED until the model binaries are pulled and a run produces real (non-fallback) images.
> EditMode / source-level tests are valid and pass; they do not stand in for a runtime LLM/art run.

## Local LLM (dialogue, DM narration, ambient barks)
- **Model:** `Qwen2.5-1.5B-Instruct` GGUF (Q4_K_M) —
  `Assets/StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf` (~986 MB **once `git lfs pull` resolves it** — it is a Git-LFS pointer, NOT real bytes, in a source-only checkout).
- **Runtime:** LLamaSharp 0.27 + llama.cpp native (`llama.dll` + `ggml*.dll` + `mtmd.dll` in
  `Assets/Plugins/x86_64/`). Real inference compiles only under the **`USE_LLAMASHARP`** define
  (currently ON in `Windows64BuildMenu.cs`).
- **Larger tier (future, opt-in):** Qwen2.5-3B is NOT bundled and NOT in the shipped verify manifest.
  If a larger local tier is added later it is a deliberate opt-in download, documented here first.

## Image generation
- **Primary:** SDXL-Turbo (CUDA / cuDNN) — `Assets/StreamingAssets/Models/sdxl-turbo/…`.
- **Fallback:** SD1.5-LCM (`…/sd-1.5/…`) when CUDA/SDXL warmup fails.
- **Runtime:** ONNX Runtime 1.x. Editor + Win64 both use the CUDA onnxruntime (so Editor Play Mode
  also gets SDXL, not the blurry SD1.5 fallback) — see the cuDNN/CUDA meta enable commits.

## Embeddings
- all-MiniLM-L6-v2 ONNX — `Assets/StreamingAssets/Models/all-minilm-l6-v2/model.onnx`.

## Model manifest hashes (EMB3-010 / EMB3-012)
- `Assets/StreamingAssets/Models/manifest.json` pins a `sha256` per model file. As of 2026-05-31
  every entry whose real bytes are resolved locally has its hash **filled** (the previous `"TBD"`
  placeholders are gone). `ModelManifest.VerifyAllPresent` recomputes the SHA on first run and only
  re-downloads on a mismatch, so the manifest is now a real integrity contract, not a stub. Entries
  whose file is still a Git-LFS pointer (or absent) in a source-only checkout should carry a
  placeholder (`TBD`/`PENDING`/`placeholder…`, which `IsHashPlaceholder` skips) until `git lfs pull`
  resolves the bytes and a real hash can be computed — never hand-fake a hash.

## Fallback order (LLM capability states — EMB-006)
1. **local-real** — `NativeLlmClient` with `USE_LLAMASHARP` + native DLLs + the GGUF present →
   genuine Qwen inference. This is the shipped path.
2. **fallback** — model/define/native missing → `NativeLlmClient` returns clearly-labelled canned
   text (and `LocalQwenClient` covers the no-model case). **Never present fallback text as real AI.**
3. **disabled** — no provider; dialogue uses deterministic shell text only.
   The runtime logs its state: `Forge Connectivity: … NativeLLM=<bool>, OnnxForge=<bool>, Failure='…'`.

## Provider policy (EMB-044)
- **Default = offline + local.** `ForgeBootstrap` hard-sets `ComfyUiAvailable=false` and
  `OllamaAvailable=false`; the registered LLM is the on-device `NativeLlmClient`. The default build
  makes **no network call** for gameplay.
- **Cloud / network providers are opt-in, disabled by default, and never authoritative.**
  `CloudLlmClient` exists for the AiDm test suite / experiments only — it must not be wired into the
  default runtime path. Keys come from env vars (see `docs/SECURITY_NOTES.md`).
- **LLM is flavour-only.** It never writes authoritative world state except through declared tools
  routed via the validator/tool router (EMB-008). Canned/templated shell answers are a graceful
  floor, not a claim of real inference.

## Model-download policy & forge-provider seam (EMB3-014 / EMB3-017 / EMB-036)
- **Model bytes are not bundled — they are pulled on first run.** Two on-device download paths exist:
  - `ModelBootstrap` (Unity `MonoBehaviour`) reads `StreamingAssets/Models/manifest.json`, verifies
    each entry against `persistentDataPath/Models` via SHA, and for any **missing** entry fetches the
    file from its manifest `url` (HuggingFace) with `UnityWebRequest` + `DownloadHandlerFile`, then
    re-hashes. An entry that fails SHA stays "missing" and is never wired in.
  - `NativeLlmClient` can lazy-download the Qwen GGUF from `DefaultDownloadUrl`
    (`huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/…`) into the model dir on first use.
- **Large downloads must be explicit / opt-in, not silent.** A multi-GB GGUF/ONNX fetch is a
  deliberate, surfaced action (loading-screen progress + logs), gated behind a missing-file check —
  never a hidden background pull on launch. If a larger tier or a new model source is added, document
  it here first (see the "larger tier (future, opt-in)" note above). The shipping contract is
  **code-only + downloader** (see `docs/REPO_HYGIENE.md` → Build delivery); LFS copies are a
  dev-convenience mirror, not the distribution channel.
- **Forge-provider seam.** Providers are swapped behind a registration seam, not hard-wired:
  `ForgeBootstrap` registers the on-device `NativeLlmClient` + `OnnxAssetForge` and hard-disables
  network providers (`ComfyUiAvailable=false`, `OllamaAvailable=false`) by default. `CloudLlmClient`
  / ComfyUI are opt-in experiment seams only and must never be the default authoritative path. This
  keeps "where bytes come from" (download vs LFS vs local cache) decoupled from "who answers"
  (native-local vs fallback vs disabled).

## Authority & determinism
- LLM output is presentation/flavour; it is not part of the deterministic-replay digest
  (`docs/DETERMINISM.md`). World mutations the DM proposes must pass the tool validator/router.

## Verifying "real LLM" (don't fake it)
A "real LLM round-trip" claim requires a screenshot/log of a coherent Qwen reply from the in-game
Ask DM / NPC dialogue under `USE_LLAMASHARP` with the model present — not a green exit code, not the
fallback text. This is still unverified (tracked: AUDIT_COUNTER EMB-006 / T-LLM-Verify).

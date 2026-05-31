# Ember — Repo Hygiene Policy

> Audit items EMB-021 / 023 / 024 / 025 / 031 / 032 / 050 / 053 / 055 / 059. One place that says what
> is tracked source vs cache vs reference vs archive, so nobody mistakes generated/sample/report
> clutter for canonical content. Enforced where possible by `tools/validation/static-audit.sh`.

## Generated assets (EMB-021 / EMB3-009 / EMB3-052)
- Runtime image generation is a **per-playthrough cache on the player's machine**, not tracked source.
- **Code load path:** the forge writes PNGs to `Assets/Generated/Core/` and runtime code reads them
  back from that exact folder (e.g. `LoadingScreenController`, `SceneEnvironmentDresser`, and the
  manifest paths in `CoreAssetManifest`). Code depends on the *path*, not on any file being committed.
- **What is tracked vs ignored (the coherent contract):**
  - **Tracked:** `Assets/Generated/Core.meta`, the `Assets/Generated/Core/` folder, and its
    `Assets/Generated/Core/.gitkeep` — so the load directory exists on a clean clone.
  - **Gitignored (regenerated outputs):** `GeneratedAssets/` (root) plus
    `Assets/Generated/Core/*.png`, `*.png.meta`, `*.jpg`, `*.jpg.meta`, and `*.json`. These are
    present on disk after a run but are **never staged** — they regenerate per machine/playthrough.
- Only seed manifests + curated authored art are tracked. Generation must surface failures (EMB-042),
  never silently ship a placeholder as canonical.

## Build delivery (EMB-053)
- **Decision: ship code-only + on-demand model download.** The repo does not commit the multi-GB ONNX
  model bytes as the distribution path; the player downloads models on first run
  (`ModelManifest` + `ModelBootstrap`, see `docs/AI_STACK.md`). cuDNN is gitignored. This matches
  Ember's bet: ship code, generate/download assets locally. (LFS holds the dev-convenience copies, but
  the *shipping* contract is code-only + downloader.)

## Plugins & native binaries (EMB3-043 / EMB3-053 / EMB-038 / EMB-039 / HYG-05)
Three distinct classes of binary live under `Assets/Plugins/` — they are tracked very differently on
purpose. (See `.gitattributes` for the LFS filters and `.gitignore` for the ignore rules.)

- **LFS-tracked native runtime DLLs** — `Assets/Plugins/x86_64/*.dll` (llama.cpp: `llama.dll`,
  `ggml*.dll`, `mtmd.dll`, `LLamaSharp.dll`; ONNX Runtime: `Microsoft.ML.OnnxRuntime.dll`,
  `onnxruntime.dll`) plus the committed CUDA execution-provider DLLs under
  `Assets/Plugins/x86_64/cuda/` (`onnxruntime.dll`, `onnxruntime_providers_cuda.dll` (~299 MB),
  `onnxruntime_providers_tensorrt.dll`, `onnxruntime_providers_shared.dll`). These are the actual
  inference backends, are **tracked via Git LFS** (`*.dll filter=lfs`), and need `git lfs pull` to
  become real bytes in a source-only checkout. `.so`/`.dylib` siblings are LFS-tracked too.
- **Gitignored cuDNN** — `Assets/Plugins/x86_64/cuda/cudnn*.dll` (~527 MB across `cudnn_*64_9.dll`)
  **and their `.meta`** are gitignored (HYG-02). They are too large and redistribution-restricted, so
  devs install them locally from the NVIDIA cuDNN 9 archive and the forge picks them up at runtime.
  The `cuda/` folder is therefore intentionally **mixed**: LFS-tracked onnxruntime providers next to
  ignored cuDNN.
- **Dev-only NuGet / Roslyn / MCP DLLs** — `Assets/Plugins/NuGet/` (~19 MB). These are **plain tracked
  binaries (NOT LFS)** — `McpPlugin*.dll`, `ReflectorNet.dll`, `Microsoft.CodeAnalysis*` (Roslyn),
  `Microsoft.AspNetCore.SignalR.*`, `Microsoft.Extensions.*`, `System.*` shims, `R3.dll`. They exist to
  drive the in-editor MCP/agent tooling (the same agent skills under `.claude/skills/`), not gameplay.
  - **Recommendation (not applied — do not move here):** these dev-tooling assemblies do not belong to
    the *game* and would be better hosted **outside `Assets/`** (e.g. a `Tools/`-style folder loaded
    only by the editor MCP host, or pulled via UPM/NuGetForUnity) so a player build never imports
    ~19 MB of Roslyn/SignalR. Treat as a future relocation, gated on confirming nothing in a shipped
    assembly references them; **left in place for now** to avoid breaking the MCP plugin wiring.
- **TMP examples/samples footprint** — `Assets/TextMesh Pro/Examples & Extras/` (~284 files) is package
  sample clutter, **not** runtime content (see *Samples (EMB-024)* below). It inflates the asset tree
  and should be removed via Package Manager after a GUID reference scan — pending Editor pass, not a
  blind delete.

## Samples (EMB-024)
- `Assets/TextMesh Pro/Examples & Extras/` (~284 files) is package sample clutter. **To be removed via
  the Unity Package Manager / Editor after a reference scan** (a few example `.mat` were touched by the
  URP upgrade; confirm nothing in active scenes references the Examples GUIDs before deletion). Tracked
  here as a pending Editor-verified cleanup, not a blind `rm`.

## Resources (EMB-025)
- `Assets/Resources/` hides dependencies and loads globally. Keep only tiny truly-global runtime assets
  (the two fonts, theme tokens). Larger/optional assets should move to explicit serialized references.
  Policy recorded; the font/theme migration is a later refactor (needs Editor + build).

## Prefabs vs scene recipes (EMB-032 / 055)
- Scenes are currently authored via `Assets/Editor/Ember/SceneRecipes/**` (recipe-generated) rather
  than reusable prefabs. **Policy: recipes own scene structure; do NOT mass-convert to prefabs blindly.**
  Introduce a prefab only after auditing the affected scene's GUID references, and never mix
  hand-edited scene objects with recipe regeneration without recording which owns what.

## Root scenes (EMB-031)
- `Assets/Scenes/CombatPlayground.unity` + `Sprint4Foundation.unity` are non-build root scenes (the
  duplicate-GUID hazard was fixed in EMB-001). **To be archived or deleted via the Editor** after a
  final reference scan — pending Editor pass.

## Planning docs & stale assets (EMB-023)
- Planning markdown does not belong under `Assets/` (Unity imports it). `Assets/Plans/` was moved to
  `docs/archive/plans/`. `Assets/pold/NavMesh.asset` was unreferenced stale and removed.

## Reports & sprint docs (EMB-050)
- 102 tracked `Reports/**` + 156 `docs/sprint-*` files clutter the active tree. **To be archived under
  `docs/archive/`** as a dedicated reviewed move (large diff; preserve `git mv` history; fix any active
  links). Pending — see AUDIT_COUNTER EMB-050.

## Local agent tooling (EMB-059)
- `.claude/skills/**` (65 files) are **intentionally-shared project dev tooling** (Claude Code skills
  that help any agent working this repo). Classified as dev tooling, **kept tracked on purpose** — they
  are not game source and are not bundled into the build.

## Non-runtime / reference material (EMB3-049 / 050)
These top-level directories are **contributor- and reference-facing material, not runtime source**.
None of them live under `Assets/`, so Unity never imports or compiles them and they are **never wired
into a player build** (no scene, asmdef, or `Resources`/`StreamingAssets` path references them). They
exist to help humans and agents *work on* the project, not to ship inside it.

- **`Reference/`** (~146 tracked files: `OldBackendData/`, `PRDs/`) — frozen historical artefacts and
  product/design specs kept for lookup. Read-only context for contributors; not canonical live state
  (current status lives in `docs/`). Safe to consult, **do not** treat as compiled content.
- **`.claude/skills/`** (65 files) — Claude Code agent skills, as noted under EMB-059 above. Dev tooling
  shared on purpose; not game source, not in the build.
- **`tools/`** (`validation/`, `codex-audit/`) — developer/CI scripts (e.g. the pure-C# NUnit fallback
  harness under `tools/validation/fallback/` and `tools/validation/static-audit.sh`). They run *about*
  the repo in CI and locally; they are not gameplay code and are not part of any shipped artefact.

Rule of thumb: if it is outside `Assets/`, assume it is reference/tooling and **not** runtime-bundled
unless a build script explicitly copies it. Keep new contributor docs, specs, and scripts in these
trees (or `docs/`), never under `Assets/` where Unity would import them (see EMB-023).

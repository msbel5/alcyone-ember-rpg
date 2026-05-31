# `docs/proofs/` — what these artifacts do and don't prove

This folder holds captured evidence (screenshots, logs, scene/LLM round-trip dumps) for the Ember
audit campaign. Read this note before treating any file here — or any green CI check — as proof that
a runtime behaviour actually works.

## 1. Proof reproducibility requires `git lfs pull` + a real runtime run (EMB3-051)

The shippable bytes (LLM GGUF weights, ONNX/diffusion model files, native `onnxruntime` / `llama` /
`ggml` / `mtmd` DLLs, large art) are **Git LFS-tracked and are NOT resolved in a default checkout** —
a plain clone (and every CI job in `.github/workflows/unity-test.yml`, which all check out
`lfs: false` to stay inside the LFS budget) gets LFS *pointer stubs* in their place.

Consequently, any artifact in this folder that claims a **runtime** result — a coherent in-character
LLM reply (`llm-roundtrip-*.md`), generated art, a played scene tour, the Win64 player actually
running — is only reproducible on a machine where the binaries are present. To reproduce or
re-verify one:

1. `git lfs pull` (resolve the real binaries — multi-GB), **or** let the in-game on-first-run
   downloader fetch the models (see `docs/AI_STACK.md` → model-download policy).
2. Do an **actual runtime run** — a Unity PlayMode session or a launched Standalone player — under
   the right scripting defines (e.g. `USE_LLAMASHARP` for native LLM). A captured log/screenshot from
   that run is the proof; a green exit code or a compile-only build is not.

Treat the dated files here as **"this worked once when the binaries were wired,"** not as a standing
"verified" status. The authoritative (and where still open, honestly-UNVERIFIED) state lives in
`docs/AI_STACK.md` and `docs/REMEDIATION_V2_COUNTER.md` (EMB-006 / T-LLM-Verify).

### CI corollary — the opt-in Win64 build job is a compile proof, not a runtime-asset proof

The `build-windows` job (BD-21 / HYG-08, opt-in via `workflow_dispatch` `run_build_windows=true` or
the nightly cron) builds `StandaloneWindows64` through the project's own Ember build menu
(`EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build`). Because it checks out `lfs: false`, a
green run proves the project **compiles into a Win64 player shell** — the produced artifact ships LFS
pointer stubs in place of the model/art binaries and is therefore **not runtime-proof**. For a
runtime-reproducible Win64 artifact, re-run with the checkout flipped to `lfs: true` (or add a
tolerant `git lfs pull` step) and then launch the player.

## 2. The fallback validation harness is source-only — it does NOT compile the Unity Presentation tier (EMB3-047)

`tools/validation/run-validation.sh --mode fallback` (the ~1s local gate used between commits) runs a
**pure-C# NUnit project** — `tools/validation/fallback/ValidationFallbackHarness.csproj` — under the
plain .NET SDK. It is **not a Unity EditMode run.** What it actually compiles and exercises:

- Full source of the engine-independent tiers: `Assets/Scripts/Domain`, `Simulation`,
  `Infrastructure`, `Data`, plus the EditMode test sources under `Assets/Tests/EditMode`.
- Only a hand-picked **allowlist** of individually-named pure-C# Presentation files (formatters,
  view-models, projectors, the JSON save serializer) — with `UnityEngine.JsonUtility` swapped for a
  local stub (`UnityJsonUtilityStub.cs`).

What it explicitly **cannot** validate (it never feeds these to a compiler, and there is no Unity
engine on the path):

- The Unity **Presentation MonoBehaviour / scene tier** — world hosts, interact raycasters, HUD/dialog
  views, controllers (anything that `using UnityEngine` beyond the stubbed JSON surface).
- Unity **compilation** as a whole, **scene** wiring, **prefabs**, **`.meta`** integrity, **native
  plugins**, and **PlayMode** behaviour.
- Anything LFS-backed (models/art) — same pointer-stub caveat as §1.

A green fallback result therefore proves the deterministic Domain/Simulation/Save/UI-formatter logic
compiles and passes — **and nothing more.** The harness prints this scope on its own `PASS` line
(`[PARTIAL — pure-C# source tests only; does NOT validate Unity compile, scenes, assets, .meta,
plugins, or PlayMode]`). To cover the Presentation tier you need a full Unity batchmode build (the
`build-windows` / `build-linux` jobs, or a local `Windows64BuildMenu.Build`) and/or a PlayMode run.

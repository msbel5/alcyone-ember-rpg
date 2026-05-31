# Ember — Current State (truthful snapshot)

> **One concise, current-state doc** (re-audit LEFT-001 / PART-001). For the *vision* read
> `docs/EMBER_VISION_BIBLE.md`; for the *AI stack* read `docs/AI_STACK.md`; for the *live fix
> register* read `docs/REMEDIATION_V2_COUNTER.md` (the single audit+counter tracker — §8 reconciles
> the 2026-05-31 re-audit). This file is the short "where is the project right now" answer and is
> kept current. When it disagrees with an older dated doc, this file wins.

_Last updated: 2026-05-31._

## Engine / shape
- **Unity 6000.3.13f1**, **URP 17.3.0**, single-player **deterministic living-world CRPG + embedded AI DM**.
- Render target: Daggerfall-style **3D world + 2D billboard actors** (not a 2D game).
- Branch model: **`main` only** (no feature branches). Commits are focused; pushed to
  `github.com/msbel5/alcyone-ember-rpg`.

## What is verified (this checkout, this session)
- **Compiles + ships:** Win64 batchmode build → `Build Finished, Result: Success`, **0 `error CS`**
  (`-executeMethod EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build`). Player exe at
  `Builds/Windows64/`.
- **Source tests green:** fallback harness (`tools/validation/run-validation.sh --mode fallback`)
  **1226 passed / 0 failed / 3 skipped** (Domain + Simulation + Data + Infrastructure + EditMode).
  This does *not* validate scenes/PlayMode — see "Not yet proven".
- **All 10 gameplay scenes regenerate** deterministically from recipes
  (`EmberSceneBuilderMenu.BuildAll`) with the HUD authored from a single source.
- **Model bytes are real here:** the Qwen GGUF (~986 MB) + native LLamaSharp/llama.cpp DLLs are
  Git-LFS-resolved in this working tree; `manifest.json` pins real sha256 hashes. A prior runtime run
  showed a genuine local-Qwen NPC greeting (`NativeLLM=True`). `USE_LLAMASHARP` is ON in the build.

## Recently fixed (2026-05-31)
- **Gameplay bugs:** magenta/giant spawned NPCs (real sprite keys + billboard sizing + scatter +
  cap), invisible character-creation portrait (the UI frame had no image slot — added one + a
  deterministic swatch + off-thread LLM portrait), LLM dialog leaking a `User:` turn-marker and
  calling the world "Morrowind" (`SanitizeNpcLine` + `StyleDescriptor`), Consult Fate now
  LLM-flavoured (deterministic outcome preserved), and **one HUD source** (`EmberWorldHost`) so
  orphan needs/population panels no longer appear in every scene.
- **Hardening:** `NativeLlmClient` rejects LFS-pointer/corrupt GGUF (size + `GGUF` magic, not bare
  `File.Exists`); player-build save/portal scene names validated via
  `Application.CanStreamedLevelBeLoaded`; 12 clone-orphan folder metas fixed with `.gitkeep`;
  `test-framework` package skew normalized to 1.6.0.

## Not yet proven / open (tracked in REMEDIATION_V2_COUNTER.md §6 + §8)
- **Live full scene-tour proof** for all build scenes (movement, camera, collision, dialog, portals,
  save/load, screenshots) — needs Unity PlayMode/manual, not headless. `[E]`.
- **Authored-scene actor identity:** generated actors carry `ActorId`; many *authored* scene actors
  are still name-keyed (staged migration).
- **Save architecture:** durable file slot + corrupt-quarantine + menu-Continue path exist; full
  multi-slot UI + schema-migration golden fixtures are staged.
- **Large classes / boundaries:** `DomainSimulationAdapter*`, `EmberWorldHost`, UI controllers, and
  the `Simulation → Data.SliceJson` asmdef edge are reasoned-staged refactors (one no-behaviour-change
  PR each), not blind same-session rewrites.

## Source-of-truth docs (everything else is reference/history)
| Doc | Role |
| --- | --- |
| `docs/CURRENT_STATE.md` (this) | short truthful "where are we now" |
| `docs/REMEDIATION_V2_COUNTER.md` | the single audit + fix counter (V2 register, EMB burndown §6, re-audit reconciliation §8) |
| `docs/AI_STACK.md` | authoritative AI/model/provider policy |
| `docs/EMBER_VISION_BIBLE.md` | canonical vision |
| `docs/reference/**`, `Reference/**` | read-only reference (old Godot/backend), not active Unity work |

# Ember — CURRENT STATE (one-pager)

> **The single concise truth for "where is the project right now".** README points here instead
> of carrying a stale status block. The original audit lives in `docs/Codex_audit.md`; the
> active cleanup pass lives in `docs/AUDIT_COUNTER.md`. Update this page's "Last updated" line
> whenever phase/branch/blockers change.

**Last updated:** 2026-05-30 (audit remediation pass, EMB-058).

## Engine baseline
- **Unity** `6000.3.13f1`, **URP** `17.3.0` (`Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`).
- Scripting backend Mono2x, managed stripping Disabled for the Win64 build (see `Windows64BuildMenu.cs`).
- Windows64 standalone build is ~14 GB (bundles ONNX + cuDNN + GGUF model). Editor must be **closed**
  for batchmode builds.

## Branch / phase
- **Branch:** `main` only. Feature branches were consolidated and deleted to stop context-confusion;
  archive tags `archive/feat-p15-design-alignment`, `archive/feat-llamasharp-t5`,
  `backup/consolidated-2026-05-30` preserve the old tips.
- **Phase:** P1.5 design-alignment **interleaved with** the ChatGPT 5.5 audit remediation
  (60 defects). The audit counter (`docs/AUDIT_COUNTER.md`) is the active work tracker.

## Build scenes (13, in order — `ProjectSettings/EditorBuildSettings.asset`)
`Boot → MainMenu → CharacterCreation → SmithingOverworld → ColonyNeeds → SeasonFarm → TradeMarket
→ CombatDungeon → RitualHall → TavernDialog → OracleShrine → ShowroomOverview → TavernFlavour`.
Two non-build root scenes exist: `Assets/Scenes/CombatPlayground.unity`, `Sprint4Foundation.unity`
(slated for archive/classify — EMB-031).

## Verified vs claimed
- **Verified (build + scene-tour screenshots this session):** Win64 build clean (0 `error CS` /
  `m_LockCount` / `DontSave` / `IL1010`); 10-scene tour renders themed floors + the rebuilt HUD
  (bottom-left HEALTH/FATIGUE/MANA bars + bottom-center 12-button BG1 action strip); TavernDialog
  ask-about shows real deterministic topic IDs (embers/gate/watch) with readable parchment text;
  forge runtime reports `Failure=''` (SDXL Turbo warms up; Editor SDXL fixed via cuDNN/CUDA meta
  enable).
- **NOT verified (claimed by docs, no runtime proof yet):** real local LLM round-trip in-game
  (Ask DM / NPC dialogue producing a coherent Qwen reply on a screenshot); durable multi-slot
  save/load; full playable scene-to-scene route with reachable exits.

## Current blockers (see AUDIT_COUNTER §3 for the full 60)
- **Asset hygiene:** missing `.meta` (fonts, `LLamaSharp.dll`), orphan `.meta` files — need Editor import.
- **LFS/model:** runtime DLL/model files are Git-LFS; CI checks out `lfs:false` → risk of false-green.
  `StreamingAssets/Models/manifest.json` paths don't match the real nested layout; hashes `TBD`.
- **LLM:** capability is fallback-by-default; `USE_LLAMASHARP` real path unproven on a screenshot;
  `Task.Run` dialog/fate mutation needs main-thread marshalling; tool-authority not routed.
- **Architecture:** `DomainSimulationAdapter.cs` (~1173 LOC) is a god object; `Simulation` depends on
  `Data.SliceJson` (inverted persistence); legacy `UnityEngine.Input` widespread.
- **UI:** HUD redirect mostly landed; Ask DM panel, clock widget, character record still missing.
- **Repo:** PRD folders triplicated (`Reference/PRDs`, `docs/reference/prd`, `docs/prds`); 100+ report
  files + sprint docs clutter the active tree.

## AI stack (authoritative — supersedes any "Qwen3" mention in older docs)
- **Local LLM:** Qwen2.5-1.5B-Instruct GGUF (`StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf`)
  via LLamaSharp 0.27 + llama.cpp native (`llama.dll` + `ggml*.dll`). Real inference compiles only
  under the `USE_LLAMASHARP` define (currently ON in the Win64 build menu).
- **Image generation:** SDXL-Turbo (CUDA) preferred, SD1.5-LCM fallback, ONNX Runtime 1.x.
- **Embeddings:** all-MiniLM-L6-v2 ONNX.
- LLM is **flavour-only**: it never writes authoritative world state except through declared tools.
  Cloud/network providers must be opt-in and disabled by default.

## Canonical docs
`docs/CURRENT_STATE.md` (this) · `docs/Codex_audit.md` (original audit) ·
`docs/AUDIT_COUNTER.md` (cleanup tracker) · `docs/EMBER_VISION_BIBLE.md` +
`docs/EMBER_VISION_NOTES_MAMI.md` (spirit) · `Reference/PRDs/PRD_IMPLEMENTATION_MATRIX.md`
(full index) · `docs/PRD_GOVERNANCE.md` (which-PRD-to-obey) · `docs/mechanics/` (sim mechanics faz-3…faz-12).

## Forbidden (for any agent working here)
No features before hygiene is stable · no asset move without its `.meta` · no MonoBehaviour rename
without a scene/prefab GUID ref-check · no doc delete before classify · no trusting README status
over this page · no LLM mutating sim state outside the tool router · no faking canned text as real
LLM · no `USE_LLAMASHARP` without full deps+model present · no default cloud fallback · no large
scene-YAML edits unless minimal+proven · no trusting fallback-harness green as Unity proof.

## Validation commands
- Fallback (partial, source-only): `tools/validation/run-validation.sh --mode fallback`
- Win64 build (Editor closed): `"E:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe"
  -batchmode -quit -projectPath "<repo>" -executeMethod
  EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build -logFile <log>`
- Scene-tour proof: run the built exe with `--ember-scene-tour --ember-proof-screenshots <dir>
  --ember-proof-quit`, then **read the screenshots** (exit code is not proof).

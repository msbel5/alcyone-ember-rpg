# Ember Audit Summary

Static inspection only. I did not run Unity Editor, batchmode tests, or validation scripts; several Unity asset/scene findings require Editor confirmation before Claude changes scene or asset references.

## 1. Executive summary

* **Verified engine baseline:** this is a Unity `6000.3.13f1` project with URP `17.3.0` in `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt`.
* **The repository still has Ember’s core idea in it:** `Domain`, `Simulation`, and `Data` are mostly Unity-free, and the docs consistently describe a deterministic living-world CRPG rather than a generic action RPG.
* **The current active truth is split across too many places.** `README.md` is stale relative to `docs/EMBER_GOAL.md`; `docs/EMBER_GOAL.md` is current but bloated as a live session log; `Reference/PRDs`, `docs/reference/prd`, and `docs/prds` overlap.
* **The biggest correctness risks are Unity asset identity problems:** duplicate scene `.meta` GUIDs, missing `.meta` files, orphan `.meta` files, LFS pointer binaries/models, and sample/generated content living in active project space.
* **The LLM/AI story is not proven.** Docs claim/aspire to real local Qwen/LLamaSharp use, but static code shows fallback behaviour, missing `.meta` on `LLamaSharp.dll`, Git LFS pointer binaries, `USE_LLAMASHARP` dependency risk, model manifest path mismatches, and no safe end-to-end runtime proof.
* **The simulation authority boundary is partially intact but compromised at the edges.** Core assemblies avoid `UnityEngine`, but LLM/HTTP/model-bootstrap code sits in `Simulation`, and `DomainSimulationAdapter` mutates adapter/world state from `Task.Run`.
* **`DomainSimulationAdapter.cs` is the central god object.** At 1173 lines it bridges world creation, ticking, save/load, HUD read models, combat, dialog, LLM routing, fate, and reflection-based restore.
* **Save/load is prototype-grade.** `EmberSaveService` uses `PlayerPrefs` key `ember.save.v1`, a static pending-load object, Unity scene names, and nested domain JSON. It is not yet a durable CRPG save system.
* **UI/player flow has drifted against the PRDs.** `docs/EMBER_GOAL.md` explicitly says the shipped HUD direction was wrong, Ask About is stub/partial, Ask DM is not built, and clock/character record/cross-scene NPC interactions are missing or broken.
* **Legacy `UnityEngine.Input` is widespread.** It appears in combat, player rig, save hotkeys, dialog, pause/menu, and old slice controllers. The InputSystem migration is documented but not done.
* **Scenes appear to be vertical-slice scaffolds more than proven gameplay.** Build settings include the Ember scene chain, but README and goal docs mention unreachable exits, overcrowding, placeholders, and visual proof gaps. Static YAML cannot verify playability.
* **Generated and archival material is too close to active source.** `GeneratedAssets/`, `Reports/`, `Assets/Generated`, `Assets/Plans`, `Assets/pold`, TextMesh Pro samples, `.claude/skills`, and duplicated PRD folders need classification before feature work.
* **Tests are numerous but not sufficient.** The fallback harness compiles only selected Presentation files and no scenes/assets. CI runs EditMode only by default and checks out without LFS.
* **Claude should not add features yet.** First pass should fix Unity asset hygiene, source-map truth, LFS/model manifest policy, scene/build validation, and the adapter/save/LLM boundaries.
* **Do not move Unity assets casually.** Scene references are GUID-based; moving/renaming MonoBehaviours, scenes, materials, fonts, models, or generated assets requires `.meta` preservation and Editor verification.

## 2. Ember soul alignment

### Preserved strengths

Ember’s identity is still visible. `README.md`, `docs/EMBER_GOAL.md`, `docs/EMBER_VISION_BIBLE.md`, and `docs/EMBER_VISION_NOTES_MAMI.md` all point at the same core: deterministic single-player living-world CRPG, colony simulation in the background, actor schedules/needs/memory/faction context, provenance-aware items, weather/season/trade/faction systems, and LLM only as flavour. The assembly split also mostly supports that: `EmberCrpg.Domain`, `EmberCrpg.Simulation`, and `EmberCrpg.Data` use `noEngineReferences: true`.

### Dangerous drift

The project is drifting in two directions at once:

1. **UI/visual scaffolding drift:** runtime UI is heavily procedural and generic Unity-prototype-shaped in places. `docs/EMBER_GOAL.md` says the shipped top-pills/9-hotbar HUD was the wrong direction and must be reversed toward the PRD-defined CRPG interface.
2. **AI/LLM claim drift:** docs speak like embedded AI is central, but runtime proof is not there. `NativeLlmClient` returns fallback text when `USE_LLAMASHARP` is not defined, the managed DLL is a Git LFS pointer with no `.meta`, model manifest paths do not match actual model layout, and `AskDmService` is a deterministic shell.
3. **Adapter drift:** simulation authority is supposed to be clean, but `DomainSimulationAdapter.cs` now owns too much and mutates dialog/fate/world trace state from async tasks.
4. **Generated-content drift:** generation is supposed to be canonical per playthrough, but there are tracked generated assets, `_deleted` generated folders, placeholder fallbacks, and unclear generated-vs-authored ownership.

### Missing pillars

* Durable save slots, save migration, corrupt-save recovery, and deterministic replay proof.
* Real per-actor Ask About using memory/faction/topic context, not only global topic IDs.
* Real Ask DM free-text panel with bounded tool use.
* Clear LLM tool authority: all world mutations must pass through validator/router/tool services, not ad hoc adapter writes.
* Player-readable CRPG interface: action bar, message window, clock, character record, save/load UI, journal/inventory readability.
* Scene proof: full scene tour with reachable transitions, collision sanity, screenshots, and no placeholder prop/portrait artifacts.
* Asset-generation proof: generated assets must be tied to canonical world seed/profile and verified, not silently fallback.

### Systems pretending to exist but not truly wired

* **AI DM:** `docs/EMBER_GOAL.md` says T5 LLM round-trip is not verified; `AskDmService` returns templated shell text; `NativeLlmClient` compiles real inference only under `USE_LLAMASHARP`.
* **Ask About:** there are dialog/topic paths, but `docs/EMBER_GOAL.md` calls the modal a stub/partial and current code exposes `_world.Topics` globally through `DomainSimulationAdapter.GetTopics()`.
* **Save/load:** save exists, but it is `PlayerPrefs` plus scene/player transform plus nested domain JSON, not a robust CRPG persistence layer.
* **Generated assets:** generation pipelines exist, but root `GeneratedAssets/` is tracked, `Assets/Generated/Core.meta` is orphaned, model paths are inconsistent, and fallbacks can mask failure.
* **Living-world UI:** colony/faction/trade/weather/season systems exist in backend docs/code, but current scenes appear to present many of them as panels/scaffolds rather than integrated world play.

## 3. Canonical source map

| Area                      | Canonical source                                                                                                                | Stale/archive/reference source                                                  | Notes                                                                                                                                 |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Project identity          | `docs/EMBER_GOAL.md`, `docs/EMBER_VISION_BIBLE.md`, `docs/EMBER_VISION_NOTES_MAMI.md`                                           | `README.md` status section                                                      | `README.md` says 2026-05-21 “playable vertical slice”; `docs/EMBER_GOAL.md` says 2026-05-29/30 P1.5 reset and prior P1 was premature. |
| Current task/status       | `docs/EMBER_GOAL.md` §1                                                                                                         | Older sprint docs under `docs/sprint-*`                                         | `EMBER_GOAL.md` is current but too long and mutable to remain the only source of truth.                                               |
| Vision/spirit             | `docs/EMBER_VISION_BIBLE.md`, `docs/EMBER_VISION_NOTES_MAMI.md`                                                                 | Older Godot reference docs                                                      | Vision docs should be preserved and summarized, not rewritten.                                                                        |
| Mechanics canon           | `docs/mechanics/MASTER_MECHANICS_BIBLE.md`, `docs/mechanics/faz-*.md`                                                           | Older architecture/mechanic notes with Python/Godot references                  | `MASTER_MECHANICS_BIBLE.md` is broad reference, not always Unity-current.                                                             |
| Architecture              | Actual asmdefs + `docs/mechanics/ARCHITECTURE.md`                                                                               | Old sprint architecture notes                                                   | Code is more authoritative than architecture docs where they conflict.                                                                |
| PRDs                      | `docs/reference/prd/PRD_IMPLEMENTATION_MATRIX.md` plus active Unity PRDs in `docs/prds/`                                        | `Reference/PRDs/` and duplicate `docs/reference/prd/` copies                    | Confusing: `docs/EMBER_GOAL.md` also points to `Reference/PRDs/PRD_frontend_*` as active design source. Needs one matrix.             |
| Runtime source            | `Assets/Scripts/Domain`, `Assets/Scripts/Simulation`, `Assets/Scripts/Data`, `Assets/Scripts/Presentation`, `Assets/Scripts/Ui` | Old `Assets/Scripts/Presentation/Slice*` prototype controllers may be legacy    | Do not delete legacy controllers until build/scene refs are checked.                                                                  |
| Editor tooling            | `Assets/Editor/Ember/**`                                                                                                        | Old scene rescue/patch tooling if not used                                      | Keep active scene/build/validation tools; archive one-off rescue scripts after proof.                                                 |
| Scenes                    | `Assets/Scenes/Ember/**`, `ProjectSettings/EditorBuildSettings.asset`                                                           | `Assets/Scenes/CombatPlayground.unity`, `Assets/Scenes/Sprint4Foundation.unity` | Build settings include 13 Ember scenes. Two root scenes are outside build and have duplicate meta GUIDs.                              |
| Art/assets                | `Assets/Art/**`, `Assets/Settings/**`, `Assets/Resources/**`, `Assets/StreamingAssets/Models/**`                                | TextMesh Pro samples, generated roots, `Assets/pold`                            | Active vs sample/generated/reference assets need classification.                                                                      |
| Generated assets          | Runtime should generate/cache under a defined location                                                                          | `GeneratedAssets/**`, `Assets/Generated/Core.meta`                              | Root generated files and `_deleted` dirs should not be active source.                                                                 |
| Reports/evidence          | Selected latest proof artifacts only                                                                                            | Most of `Reports/**`                                                            | 102 report files are active repo clutter unless intentionally retained.                                                               |
| Old backend data          | Potential import reference in `Reference/OldBackendData/**`                                                                     | Not active Unity runtime                                                        | Keep as reference/import source, not active implementation truth.                                                                     |
| CI/validation             | `.github/workflows/unity-test.yml`, `tools/validation/run-validation.sh`                                                        | Fallback harness as partial proof only                                          | CI does not prove scenes, LFS models, runtime LLM, or builds by default.                                                              |
| Package/dependency source | `Packages/manifest.json`, `Packages/packages-lock.json`, `Assets/Plugins/**`                                                    | `.gitignore` comments about packages not present                                | Manifest/lock/comments conflict around Unity AI packages and test framework resolution.                                               |
| Agent/tooling             | `.github`, `docs/agent-rules-v2.md`, `docs/inspector-audit-checklist.md`                                                        | `.claude/skills/**` if local-only                                               | `.claude/skills` may be useful local tooling but should be classified as tool support, not product source.                            |

## 4. Defect register

| ID      | Severity | Category                                    | Path(s)                                                                                                                                                                                                                                                            | Evidence                                                                                                                                                                                                                    | Why it matters                                                                                                                        | Fix direction                                                                                                                                                              | Claude-safe?                                                   | Validation                                                                                                                      |
| ------- | -------- | ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| EMB-001 | Critical | Unity asset identity                        | `Assets/Scenes/CombatPlayground.unity.meta`, `Assets/Scenes/Sprint4Foundation.unity.meta`                                                                                                                                                                          | Both `.meta` files share GUID `92b2f977c6bb4e4ebc6c7ace4f8484a7`.                                                                                                                                                           | Duplicate GUIDs can corrupt Unity asset identity and cause scene/reference confusion.                                                 | Generate a unique GUID for one scene meta or archive/remove the unused scene through Unity-safe workflow.                                                                  | Partial; safer with Unity Editor.                             | Static GUID scan, then open project and confirm no duplicate GUID warnings. Editor required: yes.                               |
| EMB-002 | Critical | Unity `.meta` integrity                     | `Assets/Plugins/x86_64/LLamaSharp.dll`, `Assets/Resources/Fonts/Jost.ttf`, `Assets/Resources/Fonts/Spectral-Regular.ttf`, `Assets/Resources/Fonts`, `Assets/Plugins/NuGet/.nuget-installed.json`                                                                   | These assets/folders have no `.meta`.                                                                                                                                                                                       | Unity will create local GUIDs, breaking reproducibility and plugin/font references. Missing DLL meta also undermines LLM integration. | Add proper `.meta` files via Unity import, not hand-moved script edits.                                                                                                    | Yes, but only through Unity import/refresh.                   | AssetDatabase refresh and meta audit. Editor required: yes.                                                                     |
| EMB-003 | High     | Unity `.meta` integrity                     | `Assets/AI Toolkit.meta`, `Assets/Audio.meta`, `Assets/Generated/Core.meta`, `Assets/Editor/Ember/Patches.meta`, `Assets/Scripts/Presentation/Ember/AiDm.meta`, multiple `Assets/Art/UI/*.meta`, `Assets/StreamingAssets/Models/sdxl-turbo/*/model.onnx.data.meta` | Orphan `.meta` files exist without matching assets/folders.                                                                                                                                                                 | Orphans make repository state misleading and can mask deleted assets or LFS-excluded model shards.                                    | Classify each: restore missing asset, delete orphan meta, or document intentionally ignored external shard.                                                                | Yes for classification; deletion needs Unity check.           | Static orphan-meta scan plus Unity import log. Editor required: yes for final cleanup.                                          |
| EMB-004 | Critical | LFS/build reliability                       | `.gitattributes`, `.github/workflows/unity-test.yml`, `Assets/Plugins/x86_64/*.dll`, `Assets/StreamingAssets/Models/**/*.onnx`, `*.gguf`                                                                                                                           | CI explicitly uses `lfs: false`; many DLL/model files are 131-byte Git LFS pointers.                                                                                                                                        | EditMode can pass while runtime DLL/model/image assets are unusable. Builds and LLM/forge proofs can be false positives.              | Split CI into source-only tests and LFS-required build/play proof; add pointer-file validation for runtime binary/model paths.                                             | Yes for static validation script; build policy needs CI work. | `git lfs ls-files`, pointer scan, Unity build with LFS restored. Editor required: no for scan, yes for import/build proof.      |
| EMB-005 | Critical | AI/model bootstrap                          | `Assets/StreamingAssets/Models/manifest.json`, `Assets/StreamingAssets/Models/**`, `Assets/Scripts/Simulation/Forge/ModelManifest.cs`, `Assets/Scripts/Presentation/Ember/Forge/ModelBootstrap.cs`                                                                 | Manifest paths use `sdxl-turbo/text_encoder.onnx`, `sdxl-turbo/unet.onnx`, `minilm-l6-v2/model.onnx`; actual files are nested under `text_encoder/model.onnx`, `unet/model.onnx`, and `all-minilm-l6-v2`. Hashes are `TBD`. | Model bootstrap can download/verify the wrong layout, skip hash verification, or fail at runtime while docs claim real generation.    | Normalize manifest to actual runtime layout; replace `TBD` with real hashes or mark dev-only manifest separately.                                                          | Yes, but avoid downloading models in Claude pass.              | Unit test `ModelManifest.VerifyAllPresent` against `StreamingAssets/Models`. Editor required: no.                               |
| EMB-006 | Critical | LLM integration                             | `Assets/Scripts/Simulation/AiDm/NativeLlmClient.cs`, `Assets/Plugins/x86_64/LLamaSharp.dll`, `docs/EMBER_GOAL.md`                                                                                                                                                  | `NativeLlmClient` returns fallback text when `USE_LLAMASHARP` is absent. `LLamaSharp.dll` is an LFS pointer and missing `.meta`. `docs/EMBER_GOAL.md` says LLM round-trip is not verified.                                  | Ember’s LLM must be flavour-only but real when claimed. Current state can look wired while producing canned fallback.                 | Make LLM capability explicit: disabled/fallback/real states, validate DLL/model presence, and block “real LLM” claims without screenshot/log proof.                        | Partial; dependency integration should be a focused PR.       | Compile with and without `USE_LLAMASHARP`, run in-game Ask DM proof. Editor required: yes for runtime proof.                    |
| EMB-007 | Critical | Determinism/threading                       | `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs`                                                                                                                                                                                            | `Task.Run` in greeting/topic/fate paths writes `_currentDialogLine`, `_isDialogThinking`, `_pendingFate`, and `_world.ToolCallTrace` from worker threads.                                                                   | Unity and game state mutation from background threads risks races, nondeterminism, and hard-to-reproduce dialog/fate bugs.            | Return LLM results to main thread via queued result object; apply state changes during tick/update boundary.                                                               | Yes, if narrowly scoped.                                      | Add testable queue seam; PlayMode verify dialog/consult fate does not race or freeze. Editor required: yes for UI proof.        |
| EMB-008 | Critical | LLM authority/tool use                      | `DomainSimulationAdapter.cs`, `Assets/Scripts/Domain/AiDm/*`, `Assets/Scripts/Simulation/AiDm/*`                                                                                                                                                                   | `ConsultFateAsync` synthesizes `ToolCallTraceRecord` directly instead of routing through proposal validator/tool router.                                                                                                    | LLM must never mutate authoritative world state except through given tools. Direct trace mutation bypasses Ember’s safety contract.   | Route fate/dialog tool effects through the same validator/router/log path used by AI DM services.                                                                          | Yes, with tests.                                              | Unit test: invalid LLM tool calls rejected; fate trace produced only by router. Editor required: no for core, yes for UI proof. |
| EMB-009 | High     | asmdef boundary                             | `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef`                                                                                                                                                                                                            | Simulation references `EmberCrpg.Data.SliceJson`.                                                                                                                                                                           | Deterministic simulation should not depend on a concrete JSON save mapper. Persistence direction is inverted.                         | Move save mapper usage to Data/Persistence/Presentation composition; keep Simulation independent from SliceJson.                                                           | Partial; requires careful dependency split.                   | asmdef compile test: Simulation references Domain/Data only or pure persistence abstraction. Editor required: no.               |
| EMB-010 | High     | Architecture/SOLID                          | `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs`                                                                                                                                                                                            | 1173 lines; handles worldgen, tick, HUD, combat, save bridge, dialog, LLM, fate, reflection restore.                                                                                                                        | This is the highest-risk change surface. Claude feature work here will create regressions.                                             | Split into clock/tick adapter, read-model projector, command router, dialog/LLM adapter, save bridge, worldgen bridge.                                                     | Yes only in staged refactor PRs.                              | Characterization tests before split; no scene YAML changes initially. Editor required: no initially, yes after integration.     |
| EMB-011 | High     | Save/load                                   | `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`                                                                                                                                                                                                       | Uses `PlayerPrefs.SetString("ember.save.v1", JsonUtility.ToJson(data))`, F5/F9, static `_pendingLoad`, scene names, GameObject `"PlayerRig"`.                                                                               | PlayerPrefs is not a durable CRPG save system; single-slot saves risk data loss and cannot support migrations/replays.                | Introduce file-based save repository, slots, schema version, corrupt-save quarantine, and explicit migration path. Keep PlayerPrefs only for “continue last slot” pointer. | Yes in phases.                                                | Save/load roundtrip, corrupt JSON handling, cross-scene load, migration test. Editor required: yes for scene load proof.        |
| EMB-012 | High     | Save schema                                 | `Assets/Scripts/Data/Save/SliceJson/SliceSaveMapper.cs`, `Assets/Scripts/Data/Save/SliceSaveData.cs`                                                                                                                                                               | `SliceSaveMapper.cs` 945 lines; `SliceSaveData.cs` 523 lines; DTO has many public fields and legacy role fields.                                                                                                            | Save schema is sprawling, fragile, and hard to migrate. New world systems can silently drift.                                         | Add explicit schema version, split mapper by subsystem, write migration tests, avoid expanding legacy role fields.                                                         | Yes if mapper is split without semantic changes first.        | Golden save roundtrip and old-save migration fixture. Editor required: no.                                                      |
| EMB-013 | High     | Reflection/invariants                       | `DomainSimulationAdapter.RestoreStateJson`                                                                                                                                                                                                                         | Uses reflection to copy every public `SliceWorldState` field into current `_world`.                                                                                                                                         | Reflection restore bypasses invariants and silently copies future fields without migration logic.                                     | Replace with explicit world replacement or restore method that validates schema and resets derived services deliberately.                                                  | Partial; needs tests first.                                   | Save/load roundtrip plus invariant checks after restore. Editor required: no.                                                   |
| EMB-014 | High     | Product/UI drift                            | `docs/EMBER_GOAL.md`, `Assets/Scripts/Presentation/Ember/UI/EmberHud.cs`, `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs`                                                                                                                          | `EMBER_GOAL.md` says T3 HUD fix was wrong and PRD requires bottom-left labeled bars plus bottom-center 12-button action bar. `EmberWorldHost` comments still describe top-pills/9-hotbar standardization.                   | Ember needs readable CRPG interface, not generic action HUD. Current code conflicts with current design direction.                    | Freeze feature work; rebuild HUD against active PRD after docs matrix is settled.                                                                                          | Yes with UI PRD acceptance screenshots.                       | Screenshot proof across all build scenes. Editor required: yes.                                                                 |
| EMB-015 | High     | Input/player flow                           | `Assets/Scripts/Presentation/**`                                                                                                                                                                                                                                   | Legacy `Input.GetKey`, `Input.GetAxis`, `Input.GetMouseButtonDown` used in combat, player rig, dialog, pause, save, world host, old slice controllers.                                                                      | Hardcoded legacy input blocks rebinding/accessibility and causes modal conflicts.                                                     | Introduce input abstraction first; then migrate to InputSystem actions.                                                                                                    | Yes staged.                                                   | Static grep for `Input.` goes to zero in active runtime; PlayMode movement/dialog/save proof. Editor required: yes.             |
| EMB-016 | High     | UI architecture                             | `Assets/Scripts/Ui/Foundation/**`, `Assets/Scripts/Ui/Backends/UiToolkit/UiToolkitPanel.cs`                                                                                                                                                                        | `Ui.Foundation` uses UnityEngine `Texture2D`, `Color`, `ScriptableObject`; `UiToolkitPanel.cs` is 517 lines and hardcodes many panels/layouts.                                                                              | “Foundation” is not backend-neutral and layout code is centralized in a growing backend god class.                                    | Rename boundary honestly or split pure UI model from Unity-bound tokens; split panels/templates by feature.                                                                | Yes, staged.                                                  | asmdef dependency check and UI snapshot/manual proof. Editor required: yes for visual proof.                                    |
| EMB-017 | Medium   | Global state/testability                    | `EmberDomainAdapterLocator`, `UiSurfaceLocator`, `ForgeLocator`, static pending load/intent classes                                                                                                                                                                | Static locators and pending state are used for scene handoff.                                                                                                                                                               | Hidden global state causes scene/test bleed and makes deterministic replay hard.                                                      | Add scene-scoped composition root and reset hooks; keep locators as compatibility wrappers temporarily.                                                                    | Yes with tests.                                               | Tests verify locators cleared on scene unload/domain reload. Editor required: yes for additive scene cases.                     |
| EMB-018 | High     | LLM/network blocking                        | `Assets/Scripts/Simulation/AiDm/LlmClients.cs`, `NativeLlmClient.cs`, `DefaultNpcPortraitJsonProvider.cs`                                                                                                                                                          | Uses synchronous `HttpClient.SendAsync(...).GetAwaiter().GetResult()` and `ReadAsStringAsync().GetAwaiter().GetResult()`; portrait provider uses 8s timeout.                                                                | Blocking LLM/HTTP calls can freeze Unity and make generation/dialog feel broken.                                                      | Move all provider calls behind async job service with timeout/cancellation and main-thread result application.                                                             | Yes with seam first.                                          | Simulated slow provider test and in-game loading/dialog proof. Editor required: yes.                                            |
| EMB-019 | High     | LLM placement                               | `Assets/Scripts/Simulation/AiDm/LlmClients.cs`, `NativeLlmClient.cs`, `Simulation/Forge/*`                                                                                                                                                                         | HTTP/native/model clients live in `Simulation` assembly.                                                                                                                                                                    | Simulation should be deterministic/headless. Providers are infrastructure and can be nondeterministic.                                | Keep request/response/tool contracts in Domain/Simulation; move provider implementations to Infrastructure/Presentation or dedicated AI runtime assembly.                  | Partial; asmdef migration needed.                             | Headless Simulation tests compile without HTTP/native provider refs. Editor required: no.                                       |
| EMB-020 | Medium   | Dialogue duplication                        | `AskAboutService.cs`, `AskDmService.cs`, `NpcDialogueService.cs`, `DomainSimulationAdapter.cs`, `DialogBoxPanel.cs`                                                                                                                                                | Multiple services implement deterministic shell dialogue, topic answers, dialog UI, and direct adapter LLM calls.                                                                                                           | Ask About/Ask DM must be a single readable CRPG conversation model with actor memory/faction/tool authority. Duplicates will drift.   | Define one conversation-state model and make UI consume it; deprecate shells after tests.                                                                                  | Yes after design decision.                                    | Unit tests for per-actor topics and memory; scene proof in `TavernDialog`. Editor required: yes.                                |
| EMB-021 | High     | Generated asset policy                      | `GeneratedAssets/**`, `Assets/Generated/Core.meta`, `.gitignore`, `EmberMainMenuUI.cs`, `BootBootstrap.cs`                                                                                                                                                         | Root `GeneratedAssets` contains hashed dirs and `_deleted` dirs; not ignored. `Assets/Generated/Core.meta` is orphan. Code expects `Assets/Generated/Core` in places.                                                       | Generated content can masquerade as canonical content and hide generation failures.                                                   | Decide one cache/output root; ignore regenerated outputs; track only seed manifests and curated authored assets.                                                           | Yes for policy and ignore updates; asset moves require care.  | Clean clone proof: no generated runtime outputs tracked except intentional fixtures. Editor required: maybe.                    |
| EMB-022 | Medium   | Repo hygiene                                | `Reports/**`                                                                                                                                                                                                                                                       | 102 files / ~11 MB reports, screenshots, diffs, subagent outputs, test archives.                                                                                                                                            | Active repo is cluttered with transient proof artifacts. It slows review and confuses canonical status.                               | Keep latest curated proof under `docs/proofs/` or `Reports/current`; archive old reports outside active tree.                                                              | Yes, but classify before deletion.                            | File inventory before/after; docs index updated. Editor required: no.                                                           |
| EMB-023 | Medium   | Docs in Assets                              | `Assets/Plans/aaa-polish-mandate.md`, `Assets/pold/NavMesh.asset`, `Assets/pold`                                                                                                                                                                                   | Planning doc and old navmesh folder live under `Assets`.                                                                                                                                                                    | Unity imports non-runtime planning and stale assets; `pold` is unclear and unsearchable.                                              | Move planning docs to `docs/archive` using Unity-safe asset moves only if imported assets involved; classify `pold`.                                                       | Partial; `NavMesh.asset` requires Editor check.               | Unity reimport no broken refs. Editor required: yes for `pold`.                                                                 |
| EMB-024 | Medium   | Sample assets                               | `Assets/TextMesh Pro/Examples & Extras/**`                                                                                                                                                                                                                         | 284 files / ~5.7 MB TextMesh Pro examples in active Assets.                                                                                                                                                                 | Samples add noise, import time, and false positives for missing references.                                                           | Remove TMP examples via Unity Package Importer/Editor if not referenced.                                                                                                   | Yes with Editor.                                              | Static reference scan, then Unity open/import log. Editor required: yes.                                                        |
| EMB-025 | Medium   | Resources usage                             | `Assets/Resources/**`, `UiToolkitPanel.cs`, `UiToolkitSurface.cs`                                                                                                                                                                                                  | Fonts/theme/flavours loaded through `Resources`; fonts missing `.meta`.                                                                                                                                                     | `Resources` hides dependencies, loads globally, and complicates build size/asset ownership.                                           | Keep only truly global tiny runtime assets; move fonts/theme to explicit serialized references or addressable-like registry.                                               | Yes after meta fix.                                           | Build size/import check; grep `Resources.Load`. Editor required: yes.                                                           |
| EMB-026 | Medium   | Package hygiene                             | `Packages/manifest.json`, `Packages/packages-lock.json`, `.github/workflows/unity-test.yml`, `.gitignore`                                                                                                                                                          | Manifest has `com.unity.test-framework: 1.4.5`; lock resolves `1.6.0`. CI strips AI packages not present. `.gitignore` re-includes `Packages/com.unity.ai.assistant` though absent.                                         | Package state is confusing and may not clone/open cleanly across machines.                                                            | Normalize manifest/lock; remove stale CI/package comments or restore documented package intentionally.                                                                     | Yes.                                                          | Unity package resolve on clean clone. Editor required: yes for final proof.                                                     |
| EMB-027 | High     | CI coverage                                 | `.github/workflows/unity-test.yml`                                                                                                                                                                                                                                 | Default CI runs EditMode only; PlayMode/build are manual/tag-only; checkout uses `lfs: false`.                                                                                                                              | Green CI does not prove scenes, builds, models, generated assets, or runtime UI.                                                      | Add lightweight static Unity asset audit, pointer audit, and scheduled/opt-in LFS build; document fallback limitations.                                                    | Yes.                                                          | CI run with asset audit and real Unity EditMode. Editor required: CI Unity.                                                     |
| EMB-028 | Medium   | Validation limitations                      | `tools/validation/run-validation.sh`, `tools/validation/fallback/ValidationFallbackHarness.csproj`                                                                                                                                                                 | Fallback harness says it is pure C# and compiles selected Presentation files only.                                                                                                                                          | Fallback green can hide Unity compile errors, missing metas, broken scenes, plugins, and UI.                                          | Rename fallback output as “partial”; add explicit full-Unity validation target and fail if user claims scene proof from fallback only.                                     | Yes.                                                          | Run fallback and Unity modes separately; report coverage. Editor required: yes for Unity mode.                                  |
| EMB-029 | Medium   | Test bloat/overfit                          | `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotals*.cs`, `SpellEffectResolutionServiceTests.cs`                                                                                                                                                   | Many magic shield tests exceed 300–500 lines; README says prior sprint drifted into magic-test matrix.                                                                                                                      | Tests overfit internal mechanics while product-visible play remains underproved.                                                      | Keep core cases; consolidate matrix tests; add product-facing save/dialog/scene/LLM tests.                                                                                 | Yes after preserving coverage.                                | Mutation/coverage comparison and reduced duplicate test count. Editor required: no for EditMode.                                |
| EMB-030 | High     | Scene playability                           | `Assets/Scenes/Ember/**`, `README.md`, `docs/EMBER_GOAL.md`                                                                                                                                                                                                        | README reports unreachable exits and overcrowded Faz 12; goal docs discuss scene-tour proof gaps and placeholders.                                                                                                          | Ember must be playable as a CRPG route, not a set of isolated showrooms.                                                              | Make a scene-tour checklist: spawn, camera, collision, interact, exit, HUD, dialog, save, screenshot. Fix scenes one by one.                                               | Partial; mostly Editor work.                                  | Manual and automated scene tour. Editor required: yes.                                                                          |
| EMB-031 | Medium   | Scene organization                          | `Assets/Scenes/CombatPlayground.unity`, `Assets/Scenes/Sprint4Foundation.unity`, `ProjectSettings/EditorBuildSettings.asset`                                                                                                                                       | Build settings include only `Assets/Scenes/Ember/**`; two root scenes are outside build and share duplicate GUIDs.                                                                                                          | Old scenes create confusion and asset identity risk.                                                                                  | After duplicate GUID fix, classify as archive/test or delete through Unity.                                                                                                | Yes with Editor.                                              | Open project, confirm no refs, scene list clean. Editor required: yes.                                                          |
| EMB-032 | Medium   | Prefab/scenes                               | `Assets/Scenes/Ember/**`, absence of `Assets/Prefabs`                                                                                                                                                                                                              | No active `Assets/Prefabs` folder found; many large scene YAML files imply direct scene authoring.                                                                                                                          | Direct scene object duplication makes UI/actor/portal fixes repetitive and risky.                                                     | Introduce prefabs only after current scene refs are audited; do not mass-convert blindly.                                                                                  | No for blind conversion; yes for audit.                       | Editor scene dependency report. Editor required: yes.                                                                           |
| EMB-033 | High     | Character creation complexity               | `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.cs`, `.Rendering.cs`, `DefaultNpcPortraitJsonProvider.cs`                                                                                                                         | Controller is 707 lines plus 571-line rendering partial; portrait provider performs local/native LLM requests.                                                                                                              | Character creation is now UI, generation, network, worldgen intent, rendering, and transition logic in one subsystem.                 | Split state machine, view renderer, generation provider, portrait validator, scene transition.                                                                             | Yes staged.                                                   | Character creation wizard regression test and screenshot pass. Editor required: yes.                                            |
| EMB-034 | Medium   | Worldgen complexity                         | `Assets/Scripts/Simulation/Worldgen/WorldgenService.cs`                                                                                                                                                                                                            | 649 lines.                                                                                                                                                                                                                  | Living-world generation is central; a monolithic service makes provenance/factions/schedules harder to evolve safely.                 | Split seed/profile, regions, settlements, factions, NPCs, history, validation into deterministic modules.                                                                  | Yes with golden-world tests.                                  | Same seed produces same world digest before/after. Editor required: no.                                                         |
| EMB-035 | Medium   | Job system complexity                       | `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`                                                                                                                                                                                                         | 776 lines.                                                                                                                                                                                                                  | Background colony simulation depends on this; overgrown assignment logic risks invisible sim bugs.                                    | Split job discovery, eligibility scoring, reservation, assignment, event emission.                                                                                         | Yes with characterization tests.                              | Existing job tests plus new deterministic assignment fixtures. Editor required: no.                                             |
| EMB-036 | Medium   | Magic/combat complexity                     | `ShieldBuffService.cs`, `ShieldBuffAbsorptionBatchTotals.cs`, magic tests                                                                                                                                                                                          | Shield buff service/totals files are 529/762 lines; tests are numerous and long.                                                                                                                                            | Combat/magic sits on top of economy/simulation; overfit buff math can consume work without improving play.                            | Simplify service interfaces, consolidate totals pipeline, preserve core tests.                                                                                             | Yes.                                                          | Combat/magic golden tests and in-scene cast proof. Editor required: yes for combat scene.                                       |
| EMB-037 | Medium   | Procedural UI code                          | `EmberHud.cs`, `DialogBoxPanel.cs`, `EmberMainMenuUI.cs`, `UiToolkitPanel.cs`                                                                                                                                                                                      | HUD 398 lines, dialog 334, main menu 345, UI Toolkit panel 517.                                                                                                                                                             | UI design is code-heavy and hard to match PRDs visually.                                                                              | Move layout decisions into templates/styles/tokens where possible; split logic from rendering.                                                                             | Yes staged.                                                   | Screenshot comparison against PRD/design system. Editor required: yes.                                                          |
| EMB-038 | Medium   | Deterministic RNG                           | `Assets/Scripts/Simulation/Forge/LatentNoiseSampler.cs`                                                                                                                                                                                                            | Uses `new System.Random((int)seed)`.                                                                                                                                                                                        | .NET `Random` algorithm/runtime differences can change generated visual outputs; generated canonical assets should be seed-stable.    | Use Ember deterministic RNG or explicitly document forge output as non-authoritative cache.                                                                                | Yes.                                                          | Same seed digest across Unity/.NET fallback. Editor required: no.                                                               |
| EMB-039 | Medium   | Non-authoritative time                      | `Assets/Scripts/Simulation/Generation/GenerationFailureLog.cs`, `VisibleGenerationPipeline.cs`                                                                                                                                                                     | Uses `DateTime.UtcNow` in Simulation generation code.                                                                                                                                                                       | Fine for logs, dangerous if it leaks into canonical world/generation identity.                                                        | Keep timestamps out of authoritative generated asset IDs/world state; isolate logging infrastructure.                                                                      | Yes.                                                          | Test generated manifest IDs independent of wall-clock time. Editor required: no.                                                |
| EMB-040 | Medium   | Visual nondeterminism                       | `EmberLoadingScreen.cs`, `ActorView.cs`                                                                                                                                                                                                                            | Uses `UnityEngine.Random.Range` for loading flavour and actor shake.                                                                                                                                                        | This is acceptable only as presentation-only. It must never affect saved/simulated state.                                             | Document visual-only randomness and keep out of save/state.                                                                                                                | Yes.                                                          | Static test/grep: visual random not referenced by Domain/Simulation save. Editor required: no.                                  |
| EMB-041 | High     | Model/provider duplication                  | `ModelBootstrap.cs`, `ForgeBootstrap.cs`, `OnnxAssetForge.cs`, `ComfyUiAssetForge.cs`                                                                                                                                                                              | Multiple paths choose SDXL/SD15/CUDA/model roots and fallbacks.                                                                                                                                                             | Asset generation is a core Ember bet. Split-brain bootstrap means “generated” proof is unreliable.                                    | Create one model locator and one forge provider factory; Presentation composes it.                                                                                         | Yes after characterization.                                   | Unit tests for model root resolution and provider selection. Editor required: yes for generation proof.                         |
| EMB-042 | Medium   | Placeholder masking                         | `OnnxAssetForge.cs`, generated assets/docs                                                                                                                                                                                                                         | Fallback/placeholder generation exists and docs mention fallbacks; goal says failures must be visible.                                                                                                                      | Silent placeholders break Ember’s canonical generated-world promise.                                                                  | Add visible “generated/fallback/static” provenance metadata in loading log and UI debug proof.                                                                             | Yes.                                                          | Proof file lists each asset and source. Editor required: yes for screenshot proof.                                              |
| EMB-043 | Medium   | AI docs mismatch                            | `README.md`, `docs/EMBER_GOAL.md`, `Assets/StreamingAssets/Models/**`                                                                                                                                                                                              | README says `Qwen3:1.7B`; code/model constants use Qwen2.5 1.5B and manifest also references 3B missing from actual files.                                                                                                  | AI stack claims are inconsistent; fixing wrong target wastes time and can break builds.                                               | Define supported local models and fallback order in one AI stack doc and manifest.                                                                                         | Yes.                                                          | AI stack smoke test prints selected provider/model. Editor required: yes for runtime.                                           |
| EMB-044 | Medium   | Cloud/network policy                        | `CloudLlmClient`, `LocalQwenClient`, `DefaultNpcPortraitJsonProvider`, docs                                                                                                                                                                                        | Cloud client exists; local endpoint defaults to Ollama; docs mention Copilot fallback.                                                                                                                                      | Ember’s LLM is supposed to be flavour-only and bounded; cloud fallback has privacy/determinism implications.                          | Make cloud/network providers explicit opt-in, disabled by default, never authoritative.                                                                                    | Yes.                                                          | Config test: default build does not call cloud/network. Editor required: no.                                                    |
| EMB-045 | Medium   | Ask About topic scope                       | `DomainSimulationAdapter.GetTopics`, `EmberWorldHost.Topics`, `AskAboutService`                                                                                                                                                                                    | Host has static `rumors/work/trade/fate`; adapter returns `_world.Topics` globally.                                                                                                                                         | Ask About should feel like talking to a person with memory/faction context, not a global topic menu.                                  | Route topics through per-actor conversation state and memory/faction filters.                                                                                              | Yes after model chosen.                                       | Tests: two NPCs expose different topics/responses. Editor required: yes for dialog proof.                                       |
| EMB-046 | High     | Docs/source truth conflict                  | `README.md`, `docs/EMBER_GOAL.md`                                                                                                                                                                                                                                  | README says no character creation and AI runtime test-wired only; `EMBER_GOAL.md` says creation wizard exists and LLM is partly wired but not verified.                                                                     | Claude will follow stale instructions and undo current work.                                                                           | Replace README status with pointer to `docs/CURRENT_STATE.md`; keep README stable.                                                                                         | Yes.                                                          | Broken-link check and docs review. Editor required: no.                                                                         |
| EMB-047 | Medium   | Case-sensitive docs links                   | `README.md`, `docs/agent-rules-v2.md`, `docs/inspector-audit-checklist.md`                                                                                                                                                                                         | Links/reference paths use `docs/` while actual folder is `docs/`.                                                                                                                                                           | Links break on Linux/GitHub/case-sensitive tooling.                                                                                   | Normalize to lowercase `docs/`.                                                                                                                                            | Yes.                                                          | Link checker or grep for `docs/`. Editor required: no.                                                                          |
| EMB-048 | High     | PRD duplication                             | `Reference/PRDs/**`, `docs/reference/prd/**`, `docs/prds/**`                                                                                                                                                                                                       | `Reference/PRDs` has 97 files; `docs/reference/prd` has many matching names; active goal references both.                                                                                                                   | No one can know which UI/save/Ask DM PRD Claude should obey.                                                                           | Make `docs/reference/prd/PRD_IMPLEMENTATION_MATRIX.md` the single index; mark each PRD active/reference/deprecated.                                                        | Yes for classification PR.                                    | Matrix contains every PRD path exactly once. Editor required: no.                                                               |
| EMB-049 | Medium   | Old backend reference                       | `Reference/OldBackendData/**`                                                                                                                                                                                                                                      | Many JSON files from old backend: factions, schedules, items, worldgen, UI plan.                                                                                                                                            | Useful reference can be mistaken for active Unity data.                                                                               | Keep under `Reference/OldBackendData` but add README saying import/reference-only; do not wire directly.                                                                   | Yes.                                                          | Source map doc updated. Editor required: no.                                                                                    |
| EMB-050 | Medium   | Reports/reference sprawl                    | `docs/sprint-*`, `Reports/**`, `docs/prds/*audit*`, `docs/*audit*`                                                                                                                                                                                                 | Many old sprint/audit reports are in active docs root.                                                                                                                                                                      | Current-state discovery is slow and error-prone.                                                                                      | Archive old sprint reports under `docs/archive/YYYY-MM/`; keep current index.                                                                                              | Yes.                                                          | Docs inventory before/after; no active docs links broken. Editor required: no.                                                  |
| EMB-051 | Medium   | Plugin dependency hell                      | `Assets/Plugins/**`, `Assets/Plugins/NuGet/**`, `.gitattributes`                                                                                                                                                                                                   | 127 plugin files; NuGet marker missing `.meta`; many DLLs LFS; LLamaSharp XML present.                                                                                                                                      | Unity plugin import settings and transitive dependency versions can silently break builds.                                            | Create `docs/DEPENDENCIES.md` and plugin audit manifest; set import platforms deliberately.                                                                                | Partial; Editor required.                                     | Unity plugin inspector/import log, clean build. Editor required: yes.                                                           |
| EMB-052 | Low      | Secrets                                     | Repository-wide grep                                                                                                                                                                                                                                               | No actual API keys found; only workflow secret references and Authorization header code.                                                                                                                                    | Good, but cloud LLM code creates future risk.                                                                                         | Add `.env`, secret, and config patterns to ignore/docs; require env-only keys.                                                                                             | Yes.                                                          | Secret scan in CI. Editor required: no.                                                                                         |
| EMB-053 | Medium   | Build size/runtime delivery                 | `docs/EMBER_GOAL.md`, `Assets/StreamingAssets/Models/**`, `.gitattributes`                                                                                                                                                                                         | Goal doc mentions Windows build around 14 GB with ONNX/cuDNN models; StreamingAssets holds large LFS model pointers.                                                                                                        | Clone/build/open experience is fragile; users may lack LFS/model bits.                                                                | Decide: ship code-only with downloader, or ship curated LFS models. Do not mix.                                                                                            | Policy PR yes; implementation later.                          | Fresh clone bootstrap test with no LFS and with LFS. Editor required: yes.                                                      |
| EMB-054 | Medium   | Scene YAML static limits                    | `Assets/Scenes/Ember/*.unity`                                                                                                                                                                                                                                      | Static scan found no `m_Script: {fileID: 0}` but package/built-in GUIDs cannot be resolved from `Assets` alone.                                                                                                             | Static inspection cannot prove no missing scripts/material refs; Unity must import packages and scenes.                               | Add Editor validation menu/test to open all scenes and report missing scripts/materials/references.                                                                        | Yes with Editor script.                                       | Run scene validation in Unity. Editor required: yes.                                                                            |
| EMB-055 | Medium   | Missing prefab policy                       | `Assets/Scenes/Ember/**`, `Assets/Editor/Ember/SceneRecipes/**`                                                                                                                                                                                                    | Scene recipes generate scenes, but active reusable prefab structure is unclear.                                                                                                                                             | Repeated scene-generated GameObjects make future UI/actors/portals hard to fix consistently.                                          | Decide prefab vs recipe ownership; do not mix per-scene hand edits and recipe regeneration without policy.                                                                 | No for blind changes; yes for audit.                          | Regenerate one scene and compare diff; manual scene verification. Editor required: yes.                                         |
| EMB-056 | Medium   | Build scene hardcoding                      | `EmberMainMenuUI.cs`, `CharacterCreationController.Rendering.cs`, `EmberSaveService.cs`, `EmberProofScreenshotDriver.cs`, scene recipes                                                                                                                            | Hardcoded scene names and chains appear across runtime/editor/diagnostic code.                                                                                                                                              | Renaming/reordering scenes will break flow and save/load.                                                                             | Centralize scene IDs/build scene registry.                                                                                                                                 | Yes.                                                          | Static grep for scene names reduced; scene transition test. Editor required: yes.                                               |
| EMB-057 | Medium   | `Assets/Scripts/Presentation/Slice*` legacy | `Assets/Scripts/Presentation/SliceGameController.cs`, `SlicePlayerRig.cs`, other `Slice*`                                                                                                                                                                          | Old slice controllers still use legacy input and file saves.                                                                                                                                                                | Legacy prototype code may be unused but still compiles and confuses architecture.                                                     | Determine scene/test references; archive/delete only after no refs.                                                                                                        | Partial.                                                      | Static and Unity reference scan. Editor required: yes.                                                                          |
| EMB-058 | High     | No current-state one-pager                  | `docs/EMBER_GOAL.md`                                                                                                                                                                                                                                               | Current status is buried in a 200+ line session prompt with task logs and historical notes.                                                                                                                                 | Claude will over-read stale sections or follow the wrong “NOW”.                                                                        | Add concise `docs/CURRENT_STATE.md` generated/maintained manually from canonical map.                                                                                      | Yes.                                                          | Reviewer can answer phase/build/known blockers from one page. Editor required: no.                                              |
| EMB-059 | Medium   | `.claude/skills` classification             | `.claude/skills/**`                                                                                                                                                                                                                                                | 65 local skill files are tracked.                                                                                                                                                                                           | Tooling may be useful, but it is not game source and may confuse reviewers/build scope.                                               | Classify as dev tooling, keep if intentionally shared, or move to tooling docs.                                                                                            | Yes.                                                          | Source map and `.gitignore` decision. Editor required: no.                                                                      |
| EMB-060 | Medium   | Test/package lock mismatch                  | `Packages/manifest.json`, `Packages/packages-lock.json`                                                                                                                                                                                                            | Manifest says test framework `1.4.5`; lock resolved `1.6.0` builtin.                                                                                                                                                        | Unity may rewrite lock; CI cache keys can hide package changes.                                                                       | Let Unity resolve once, commit normalized manifest/lock, document expected package versions.                                                                               | Yes with Unity.                                               | Clean package restore. Editor required: yes.                                                                                    |

## 5. Claude-ready work packages

### P0-A — Unity asset identity audit

**Goal:** make the project safe to open/import before feature work.
**Files likely touched:** duplicate/missing/orphan `.meta` files, `Assets/Scenes/CombatPlayground.unity.meta`, `Assets/Scenes/Sprint4Foundation.unity.meta`, font/DLL metas.
**Files not to touch:** scene YAML contents, MonoBehaviour class names, model binaries, generated images.
**Risks:** changing GUIDs can break references if the asset is used.
**Acceptance criteria:** no duplicate GUIDs; no missing metas for assets under `Assets`; orphan metas either restored, removed, or documented.
**Validation:** static meta audit plus Unity import/open with no duplicate-GUID or missing-meta warnings.
**Unity Editor required:** yes.
**Suggested Claude prompt title:** “P0: Fix Unity .meta integrity without changing scenes or code.”

### P0-B — LFS/model/plugin reality check

**Goal:** prevent false green builds caused by LFS pointer files.
**Files likely touched:** validation scripts, CI workflow, docs dependency note, maybe `.gitignore`/`.gitattributes`.
**Files not to touch:** actual model binaries, DLL contents, scene YAML.
**Risks:** CI may become slower if LFS/build proof is added too aggressively.
**Acceptance criteria:** static validation detects pointer files in runtime DLL/model paths; docs say which workflows require LFS.
**Validation:** pointer scan in CI, clean clone with `lfs:false` reports “source-only mode”, LFS clone reports “runtime assets present.”
**Unity Editor required:** no for scan; yes for full build proof.
**Suggested Claude prompt title:** “P0: Add runtime binary/model pointer validation and document LFS build modes.”

### P0-C — Source-of-truth one-pager

**Goal:** stop Claude from following stale README/docs.
**Files likely touched:** `docs/CURRENT_STATE.md`, `README.md`, `docs/EMBER_GOAL.md`, PRD matrix.
**Files not to touch:** gameplay code, scenes, assets.
**Risks:** summarizing incorrectly can codify stale claims.
**Acceptance criteria:** README points to `docs/CURRENT_STATE.md`; current state lists engine, branch/phase, active scenes, real blockers, what is verified, what is not.
**Validation:** grep for stale “P1 done”/wrong status links; docs review.
**Unity Editor required:** no.
**Suggested Claude prompt title:** “P0: Create Ember CURRENT_STATE and de-stale README status.”

### P1-A — Model manifest normalization

**Goal:** align model manifest paths/hashes with actual runtime layout.
**Files likely touched:** `Assets/StreamingAssets/Models/manifest.json`, `ModelManifestTests`, `ModelBootstrap` path tests.
**Files not to touch:** model binaries.
**Risks:** incorrect path changes can break local generation.
**Acceptance criteria:** manifest entries resolve to actual expected files or clearly download to persistent cache; `TBD` hashes are eliminated or explicitly dev-only.
**Validation:** `ModelManifest.VerifyAllPresent` fixture against current layout.
**Unity Editor required:** no for tests; yes for runtime generation proof.
**Suggested Claude prompt title:** “P1: Make AI model manifest paths and verification truthful.”

### P1-B — Save/load safety characterization

**Goal:** protect current save/load before refactor.
**Files likely touched:** EditMode save tests, `EmberSaveService` tests, `SliceSaveMapper` fixtures.
**Files not to touch:** scene YAML, mapper semantics.
**Risks:** current PlayerPrefs behaviour may be ugly but user saves could depend on it.
**Acceptance criteria:** tests cover corrupt JSON, no adapter, domain export failure, cross-scene pending load, and old save fixture.
**Validation:** EditMode save tests.
**Unity Editor required:** no initially; yes for F5/F9 scene proof.
**Suggested Claude prompt title:** “P1: Add save/load characterization tests before changing persistence.”

### P1-C — Scene tour validation harness

**Goal:** prove playability instead of trusting docs.
**Files likely touched:** `EmberProofScreenshotDriver`, editor validation scripts, reports path docs.
**Files not to touch:** scene content in this batch.
**Risks:** automation can produce screenshots without checking readability.
**Acceptance criteria:** validation opens/loads each build scene, captures screenshot, checks EventSystem/camera/player/HUD/portal presence, reports failures.
**Validation:** scene tour report plus screenshots.
**Unity Editor required:** yes.
**Suggested Claude prompt title:** “P1: Add build-scene validation and screenshot proof without fixing content yet.”

### P2-A — DomainSimulationAdapter split, phase 1

**Goal:** reduce god-class risk without changing gameplay.
**Files likely touched:** `DomainSimulationAdapter.cs`, new adapter helper classes under same namespace, tests.
**Files not to touch:** scenes, MonoBehaviour names, save schema.
**Risks:** subtle behavioural drift.
**Acceptance criteria:** no public interface changes; extracted classes have characterization tests; line count drops meaningfully.
**Validation:** fallback/EditMode tests, manual scene smoke.
**Unity Editor required:** yes for final smoke.
**Suggested Claude prompt title:** “P2: Extract read-model/dialog/save responsibilities from DomainSimulationAdapter with no behaviour change.”

### P2-B — Main-thread LLM result queue

**Goal:** remove worker-thread world/UI mutation.
**Files likely touched:** `DomainSimulationAdapter.cs`, LLM routing wrapper, tests.
**Files not to touch:** LLamaSharp dependency integration, model files.
**Risks:** dialog/fate timing changes.
**Acceptance criteria:** background provider returns immutable result; adapter applies result during main-thread tick/update; no `_world` mutation inside `Task.Run`.
**Validation:** grep for `Task.Run` state mutations; PlayMode dialog/fate proof.
**Unity Editor required:** yes.
**Suggested Claude prompt title:** “P2: Marshal LLM dialog/fate results to main thread and preserve simulation authority.”

### P2-C — Persistence boundary cleanup

**Goal:** move concrete save/persistence out of Simulation dependency direction.
**Files likely touched:** asmdefs, save service composition, tests.
**Files not to touch:** save schema content in same PR.
**Risks:** asmdef compile break.
**Acceptance criteria:** `EmberCrpg.Simulation` no longer references `EmberCrpg.Data.SliceJson`; tests compile.
**Validation:** asmdef compile, fallback harness, Unity EditMode.
**Unity Editor required:** yes for asmdef import.
**Suggested Claude prompt title:** “P2: Remove Simulation dependency on SliceJson persistence assembly.”

### P2-D — LLM/tool authority enforcement

**Goal:** ensure LLM can only affect state via declared tools.
**Files likely touched:** `Domain/AiDm`, `Simulation/AiDm`, `DomainSimulationAdapter` fate/dialog paths, tests.
**Files not to touch:** provider DLL/model integration.
**Risks:** over-tightening could break existing flavour fallback.
**Acceptance criteria:** Consult Fate and dialog tool traces use validator/router; invalid tool calls are rejected and logged.
**Validation:** deterministic tool-call tests and one in-scene fate proof.
**Unity Editor required:** yes for UI proof.
**Suggested Claude prompt title:** “P2: Route all LLM tool effects through Ember tool validation.”

### P3-A — Input abstraction before InputSystem migration

**Goal:** stop spreading legacy `UnityEngine.Input`.
**Files likely touched:** active Presentation input handlers only.
**Files not to touch:** old unused Slice controllers until reference audit.
**Risks:** player controls can regress.
**Acceptance criteria:** a single input abstraction owns actions for movement, interact, dialog topics, save/load, pause, spell slots.
**Validation:** grep active runtime for direct `Input.` reduced; manual controls checklist.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P3: Introduce Ember input abstraction without changing controls.”

### P3-B — CRPG UI alignment

**Goal:** rebuild HUD/dialog/clock according to active PRDs after docs matrix is stable.
**Files likely touched:** `EmberHud.cs`, `DialogBoxPanel.cs`, UI Toolkit backend, design tokens, scene UI bindings.
**Files not to touch:** Domain/Simulation mechanics.
**Risks:** visual-only fixes can break readability or modal input.
**Acceptance criteria:** bottom-left labeled bars, bottom-center action bar, message/dialog readability, clock widget, screenshots across scenes.
**Validation:** screenshot proof against PRD checklist.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P3: Align Ember HUD/dialog/clock with frontend PRDs and screenshot proof.”

### P4-A — Docs/reference/archive cleanup

**Goal:** make canonical docs discoverable.
**Files likely touched:** docs index, PRD matrix, archive folder, README links.
**Files not to touch:** source code/assets.
**Risks:** deleting useful reference docs.
**Acceptance criteria:** each doc is tagged current/reference/archive; duplicate PRDs are merged or indexed; no uppercase `docs/` links.
**Validation:** link check and file inventory.
**Unity Editor required:** no.
**Suggested Codex prompt title:** “P4: Classify Ember docs into current, reference, and archive without deleting unreviewed design intent.”

### P4-B — Generated/report/sample cleanup

**Goal:** remove non-source clutter from active project space.
**Files likely touched:** `.gitignore`, generated/report archive paths, TextMesh Pro sample removal plan.
**Files not to touch:** active art registry, active scenes, model files.
**Risks:** deleting imported assets that scenes reference.
**Acceptance criteria:** generated outputs are ignored or moved; reports are indexed/archived; samples removed only after reference check.
**Validation:** clean clone opens; Unity reference check.
**Unity Editor required:** yes for asset/sample removal.
**Suggested Codex prompt title:** “P4: Clean generated/report/sample folders with Unity reference checks.”

## 6. Refactor targets

### LOC / complexity thresholds

**Above 1000 lines**

| File                                                                    | Lines | Acceptable? | Split direction                                                                         |
| ----------------------------------------------------------------------- | ----- | ----------- | --------------------------------------------------------------------------------------- |
| `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs` | 1173  | No          | Split tick/world state, read-model projection, commands, dialog/LLM, fate, save bridge. |
| `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`      | 1033  | No          | Split by effect family and keep a small golden integration test.                        |

**Above 800 lines**

| File                                                    | Lines | Acceptable? | Split direction                                                           |
| ------------------------------------------------------- | ----- | ----------- | ------------------------------------------------------------------------- |
| `Assets/Scripts/Data/Save/SliceJson/SliceSaveMapper.cs` | 945   | No          | Split by actor/item/world/faction/trade/magic/dialog/system save mappers. |

**Above 500 lines**

| File                                                                                                 | Lines | Acceptable?   | Split direction                                                             |
| ---------------------------------------------------------------------------------------------------- | ----- | ------------- | --------------------------------------------------------------------------- |
| `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`                                           | 776   | No            | Job discovery, eligibility, reservation, assignment, event emission.        |
| `Assets/Scripts/Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`                                 | 762   | No            | Batch grouping, filtering, absorption math, result DTOs.                    |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.cs`                 | 707   | No            | Wizard state, user input, validation, generation, scene transition.         |
| `Assets/Scripts/Simulation/Worldgen/WorldgenService.cs`                                              | 649   | Borderline/no | Region, settlement, faction, NPC, history, validation modules.              |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.Rendering.cs`       | 571   | No            | View renderer, style/tokens, portrait renderer, progress/worldgen view.     |
| `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs`                                      | 565   | No            | Composition root, input handler, UI binder, scene lifecycle, host services. |
| `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`                                          | 554   | Borderline/no | Split by assignment scenario and one integration suite.                     |
| `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs`                                               | 529   | No            | Buff registry, application, tick/update, absorption API.                    |
| `Assets/Scripts/Data/Save/SliceSaveData.cs`                                                          | 523   | No            | DTO partial files by subsystem or nested schema modules.                    |
| `Assets/Scripts/Ui/Backends/UiToolkit/UiToolkitPanel.cs`                                             | 517   | No            | One panel renderer per screen; backend host only mounts templates.          |
| `Assets/Tests/EditMode/Magic/ShieldBuffServiceBatchAbsorptionTotalsGroupByManyStackedFilterTests.cs` | 514   | No            | Consolidate duplicated matrix tests.                                        |

**Above 300 lines**

Key additional files needing review: `LlmClients.cs` 497, multiple shield buff tests 300–488, `EmberHud.cs` 398, `LoadingScreenController.cs` 369, `ModelBootstrap.cs` 356, `EmberMainMenuUI.cs` 345, `DialogBoxPanel.cs` 334, `OnnxAssetForge.cs` 319, `ClipBpeTokenizer.cs` 310, `EmberSaveService.cs` 311, `World/ActorStoreTests.cs` 306.

### Top 20 refactor targets

| Rank | File/class                                 | Current responsibility                                                             | Why too large/misplaced                                        | Proposed split                                                                                | Safe migration strategy                                                        | Test/proof required              |
| ---- | ------------------------------------------ | ---------------------------------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | -------------------------------- |
| 1    | `DomainSimulationAdapter.cs`               | Bridges nearly everything between simulation and Unity.                            | Central god class and async mutation risk.                     | Tick adapter, read-model projector, command router, dialog adapter, fate oracle, save bridge. | Characterize outputs first; extract private helpers with no public API change. | Existing EditMode + scene smoke. |
| 2    | `SliceSaveMapper.cs`                       | Converts entire world to/from save DTO.                                            | Monolithic schema coupling.                                    | Sub-mappers per world subsystem.                                                              | Add golden save fixtures; split pure functions.                                | Roundtrip and migration tests.   |
| 3    | `EmberWorldHost.cs`                        | Scene composition, ticking, UI binding, input, save service setup, modal handling. | MonoBehaviour god host.                                        | Composition root, input controller, UI binder, lifecycle service.                             | Extract non-MonoBehaviour helpers first.                                       | Scene load/play smoke.           |
| 4    | `CharacterCreationController.cs`           | Wizard state, UI, worldgen, portrait/LLM, transition.                              | Too much product-critical flow in one controller.              | State machine, view, generation coordinator, transition service.                              | Preserve public serialized fields; avoid scene ref break.                      | Creation screenshot tour.        |
| 5    | `CharacterCreationController.Rendering.cs` | Rendering partial for creation.                                                    | Rendering still huge and coupled to controller state.          | Dedicated view renderer and token/style helper.                                               | Extract pure rendering helpers.                                                | Screenshot comparison.           |
| 6    | `JobAssignmentSystem.cs`                   | Colony job assignment.                                                             | Critical sim logic too dense.                                  | Discovery/eligibility/scoring/reservation/assignment.                                         | Characterization tests before extraction.                                      | Same seed/job result digest.     |
| 7    | `WorldgenService.cs`                       | Entire generated world pipeline.                                                   | Ember’s world generation must be extensible and deterministic. | Regions, factions, settlements, NPCs, history, validation.                                    | Add world digest tests; extract modules.                                       | Same seed same digest.           |
| 8    | `ShieldBuffAbsorptionBatchTotals.cs`       | Magic batch absorption math.                                                       | Overgrown and over-tested internally.                          | Batch filters/grouping/math result.                                                           | Consolidate tests after split.                                                 | Magic golden tests.              |
| 9    | `ShieldBuffService.cs`                     | Shield buff application/resolution.                                                | Service has too many mechanics.                                | Registry, apply, tick, absorb.                                                                | Preserve API; add integration tests.                                           | Combat spell proof.              |
| 10   | `UiToolkitPanel.cs`                        | Renders many UI Toolkit panels.                                                    | Backend god renderer; hardcoded layouts.                       | Screen renderers/templates.                                                                   | Introduce panel interface; move one screen at a time.                          | UI screenshots.                  |
| 11   | `EmberHud.cs`                              | Builds HUD procedurally.                                                           | Current direction conflicts with PRD.                          | HUD model, bars, action bar, message window.                                                  | Wait for PRD source-map cleanup.                                               | Screenshot checklist.            |
| 12   | `DialogBoxPanel.cs`                        | Dialog UI, input, topic selection.                                                 | Modal/input/dialog state mixed.                                | Dialog view + input adapter + topic list component.                                           | Keep existing source interface temporarily.                                    | TavernDialog proof.              |
| 13   | `EmberMainMenuUI.cs`                       | Menu UI, continue, generation top-up, generated directory signature.               | Menu owns generation concerns.                                 | Menu view, save-slot presenter, generation kickoff service.                                   | Extract generation side effect first.                                          | MainMenu proof.                  |
| 14   | `LoadingScreenController.cs`               | Loading/progress/UI orchestration.                                                 | Likely mixes generation, presentation, flow.                   | Progress model, loading view, generation progress adapter.                                    | Add state-transition tests.                                                    | New Game loading screenshot.     |
| 15   | `ModelBootstrap.cs`                        | Downloads/verifies/resolves models and builds forge providers.                     | Duplicates ForgeBootstrap/provider selection.                  | Model locator, verifier, downloader, provider factory.                                        | Normalize manifest first.                                                      | Model path tests.                |
| 16   | `ForgeBootstrap.cs`                        | Runtime registration of forge/LLM services.                                        | Composition and provider selection mixed.                      | Composition root plus factory.                                                                | Keep locator compatibility.                                                    | Generation/LLM smoke.            |
| 17   | `OnnxAssetForge.cs`                        | ONNX asset generation and fallback.                                                | Advanced AI path plus placeholder behaviour.                   | ONNX pipeline, fallback generator, provenance reporter.                                       | Add provenance tests.                                                          | Generated asset proof.           |
| 18   | `LlmClients.cs`                            | Config, local, cloud, HTTP JSON parsing.                                           | Provider infra in Simulation; blocking sync I/O.               | Contracts in core, providers in runtime infra.                                                | Add fake provider tests first.                                                 | No network in default tests.     |
| 19   | `EmberSaveService.cs`                      | Hotkeys, PlayerPrefs, scene load, UI status, domain bridge.                        | Persistence and UI/runtime concerns mixed.                     | Save repository, save controller, status view, scene restore service.                         | Characterize existing saves.                                                   | Save/load scene proof.           |
| 20   | `SliceSaveData.cs`                         | Huge DTO tree.                                                                     | Many public fields and legacy compatibility in one file.       | DTO files by subsystem, explicit schema version.                                              | Split files only first, no field rename.                                       | Existing JSON compatibility.     |

## 7. Unity-specific fix plan

1. **Freeze scene/prefab-affecting renames until after asset audit.** Do not rename MonoBehaviour classes, asmdefs, scenes, materials, fonts, or scripts before duplicate/missing/orphan `.meta` issues are resolved.
2. **Run a static meta audit first.** Check for duplicate GUIDs, missing metas, orphan metas, LFS pointer runtime assets, and `m_Script: {fileID: 0}`.
3. **Fix duplicate GUIDs in Unity-safe order.** For `CombatPlayground`/`Sprint4Foundation`, first determine whether either scene is referenced. If unused, archive/delete one through Unity Editor. If both are needed, let Unity regenerate one `.meta` and commit the new GUID intentionally.
4. **Import missing metas through Unity.** Fonts, plugin DLLs, and Resources folders should be imported by Unity so importer settings are real. Do not hand-create plugin metas unless matching importer settings are known.
5. **Resolve orphan metas deliberately.** For ignored external shards like `.onnx.data`, either remove orphan metas or restore documented local-only files; do not leave orphan metadata in active Assets.
6. **Do not move assets without `.meta`.** If an asset must move, move the asset and its `.meta` together. Prefer Unity Editor move for scenes, materials, fonts, textures, prefabs, and scripts referenced by scenes.
7. **Scripts can be moved by file operations only if class names and GUIDs stay intact.** The `.meta` must move with the `.cs` file. Never rename a MonoBehaviour class/file pair without checking scene/prefab refs.
8. **Scene YAML edits should be last resort.** Combat/Tavern scenes are large and likely generated/authored. Use Editor scene tools for object/material/collider fixes unless a minimal YAML patch is proven safe.
9. **TextMesh Pro samples should be removed only after reference scan.** They are likely sample clutter, but deleting package sample assets should be done with Unity open/import confirmation.
10. **Generated assets need a policy before cleanup.** Decide whether `Assets/Generated/Core` is runtime cache, editor-generated fixture, or tracked content. Then ignore or track consistently.
11. **Manual Play Mode verification is required for:** scene exits, collisions, player rig/camera, dialog modal input, save/load, LLM response UI, generated material assignment, and HUD readability.
12. **Screenshot proof is required for visual/UI changes.** The docs already demand reading screenshots, not just successful exit codes.

## 8. Docs cleanup plan

### Canonical docs to keep active

* `docs/CURRENT_STATE.md` — should be created and become the short current truth.
* `docs/EMBER_GOAL.md` — keep as session/backlog log, but not the only current truth.
* `docs/EMBER_VISION_BIBLE.md` — canonical spirit/identity.
* `docs/EMBER_VISION_NOTES_MAMI.md` — author intent and guardrails.
* `docs/mechanics/MASTER_MECHANICS_BIBLE.md` — mechanics reference, with caveat that some parts are historical/future-facing.
* `docs/mechanics/ARCHITECTURE.md` — architecture reference, but update conflicts against current asmdefs/code.
* `docs/reference/prd/PRD_IMPLEMENTATION_MATRIX.md` — should become the one PRD index.
* Active Unity PRDs under `docs/prds/`: visual architecture, loading asset generation, overland map, visible generation/UI documents.

### Docs to archive

* Old sprint files under `docs/sprint-*`, especially shield-buff micro-loop documents.
* Old audit reports under `docs/*audit*` after extracting current findings.
* Historical status sections from `README.md`.
* `Reports/**` except the latest curated proof set.
* One-off mission docs like `aaa-*` after their outcomes are summarized.
* `Reference/PRDs/**` duplicates after each is mapped to active/reference/deprecated in the matrix.

### Docs to delete only after classification

* Duplicate PRDs that are byte-identical or superseded by `docs/reference/prd`.
* Generated report logs already ignored by `.gitignore`.
* Old proof screenshots if superseded by current scene-tour proof.
* Local-only tool/docs references that cannot be used by contributors.

### Duplicate docs to merge

* `Reference/PRDs/PRD_IMPLEMENTATION_MATRIX.md` and `docs/reference/prd/PRD_IMPLEMENTATION_MATRIX.md`.
* `Reference/PRDs/PRD_frontend_*` and `docs/reference/prd/PRD_frontend_*`.
* `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` and `docs/reference/prd/PRD_IMPLEMENTATION_MATRIX.md`.
* Any “visual generation cutover” docs in `docs/prds` and Reports once the current state is captured.

### `docs/CURRENT_STATE.md` should contain one page

* Unity version and package baseline.
* Current phase and exact active branch/task.
* What is verified in Editor/build and what is only claimed by docs.
* Build scenes list.
* Current blockers: meta integrity, LFS/model manifest, LLM unverified, HUD/Ask About/Ask DM/clock gaps, save/load limits.
* Canonical PRD matrix link.
* Forbidden actions for Codex.
* Validation commands and required screenshot proof.
* “Last updated” date and who/what updated it.

## 9. Tests and validation plan

### Quick static checks

* Duplicate GUID scan over all `.meta`.
* Missing `.meta` scan under `Assets`.
* Orphan `.meta` scan under `Assets`.
* LFS pointer scan for runtime-critical `*.dll`, `*.onnx`, `*.gguf`, `*.png`, `*.jpg`.
* Grep for direct `UnityEngine.Input` in active runtime.
* Grep for `PlayerPrefs`.
* Grep for `Task.Run` and `.GetAwaiter().GetResult()`.
* Grep for uppercase `docs/`.
* Grep for `m_Script: {fileID: 0}` in scenes/prefabs.
* Package manifest/lock consistency check.

Suggested commands for Codex later:

```bash
find Assets -name '*.meta' -print
grep -RIn 'm_Script: {fileID: 0' Assets/Scenes Assets/Art Assets/Resources Assets/Prefabs 2>/dev/null
grep -RIn -E 'UnityEngine\.Input|\bInput\.|PlayerPrefs|Task\.Run|GetAwaiter\(\)\.GetResult\(\)' Assets/Scripts Assets/Tests
grep -RIn 'docs/' README.md docs .github
grep -RIl '^version https://git-lfs.github.com/spec/v1' Assets/Plugins Assets/StreamingAssets Assets/Art
```

### Fallback C# checks

Use fallback only as partial source-level proof. It does not validate Unity import, scenes, resources, plugins, package import settings, or PlayMode.

```bash
tools/validation/run-validation.sh --mode fallback
```

Acceptance: output must clearly state fallback is partial.

### Unity EditMode tests

Run real Unity EditMode in batchmode. Acceptance must include no compiler errors and no Unity import warnings related to missing scripts/metas/plugins.

```bash
tools/validation/run-validation.sh --mode unity --unity-path <Unity executable>
```

### Unity PlayMode tests

Add or run PlayMode tests for:

* MainMenu → CharacterCreation → first world scene.
* Dialog modal opens/closes and consumes number keys.
* F5/F9 save/load in same scene.
* Cross-scene save/load.
* CombatDungeon movement/strike/cast.
* TavernDialog Ask About response path.

### Manual scene tour

For every build scene:

1. Scene loads.
2. EventSystem exists.
3. Camera/player rig usable.
4. HUD readable.
5. Dialog/inventory/pause interactions do not conflict.
6. Exit/portal reachable.
7. Collision sane.
8. No magenta/checker/gray placeholder unless explicitly accepted.
9. Save/load works.
10. Screenshot captured and reviewed.

Scenes to tour: `Boot`, `MainMenu`, `CharacterCreation`, `SmithingOverworld`, `ColonyNeeds`, `SeasonFarm`, `TradeMarket`, `CombatDungeon`, `RitualHall`, `TavernDialog`, `OracleShrine`, `ShowroomOverview`, `TavernFlavour`.

### Screenshot proof

Use the proof screenshot driver, but require human review against PRDs. Exit code is not enough.

Proof set should include:

* Main menu.
* Every creation stage or at least representative beginning/middle/final.
* Each gameplay scene with HUD.
* Dialog open in TavernDialog.
* Ask DM/Oracle response.
* Combat spell/action bar.
* Save/load status.

### Save/load proof

* Save in scene A, quit/reload, continue to scene A.
* Save in scene A, transition to scene B, load back to A.
* Corrupt save JSON produces safe error and does not crash.
* Domain state roundtrips actors, inventories, topics, memory, faction/trade, time, and tool traces.
* Old save fixture migrates or is rejected with explicit message.

### LLM proof

* Default no-model/no-provider path produces labelled fallback, not fake “real AI”.
* Local model path validates real file, not LFS pointer.
* `USE_LLAMASHARP` build compiles only when managed/native dependencies are present.
* Ask About/Ask DM/Consult Fate show real provider label, seed, model ID, and bounded response.
* Invalid tool call is rejected and logged.
* LLM text never mutates authoritative state without tool router.

### Deterministic replay proof

* Same seed + same input command log produces same world digest.
* Save/load mid-run and continue produces same digest as uninterrupted run.
* Wall-clock timestamps and Unity visual randomness are absent from authoritative digest.
* Generated asset cache/provenance is separate from simulation truth unless explicitly part of the seed contract.

## 10. What Codex should NOT do

* Do not rewrite the whole project.
* Do not add gameplay features before asset hygiene, source-map truth, and validation are stable.
* Do not move Unity assets without moving/preserving `.meta`.
* Do not rename MonoBehaviour classes or files without checking scene/prefab refs in Unity.
* Do not delete docs before classifying current/reference/archive.
* Do not treat `README.md` status as current without checking `docs/EMBER_GOAL.md` and the PRD matrix.
* Do not let LLM output mutate simulation state without validator/router/tool calls.
* Do not “fix” LLM by making canned text look real.
* Do not enable `USE_LLAMASHARP` unless managed DLL, native DLLs, transitive dependencies, importer settings, and real model file are present.
* Do not add cloud/network fallbacks enabled by default.
* Do not let generated placeholder art count as canonical generated content.
* Do not edit large scene YAML unless Unity Editor change is impossible and the patch is minimal/proven.
* Do not delete TextMesh Pro examples, old scenes, generated assets, or reports until reference checks/classification are done.
* Do not replace simulation systems with visual-only hacks.
* Do not add another manager/helper/god class to avoid splitting existing ones.
* Do not expand `SliceSaveData` legacy role fields unless a migration plan exists.
* Do not add new static locators for cross-scene handoff.
* Do not trust fallback harness green as Unity proof.
* Do not change package/plugin versions casually; plugin import settings and LFS state must be validated.
* Do not turn Ember into a generic action RPG with only combat/HUD polish.

## 11. Final prioritized checklist

1. Fix duplicate GUID and missing/orphan `.meta` audit in one Unity-safe PR.
2. Add static validation for duplicate GUIDs, missing metas, orphan metas, and LFS pointer runtime assets.
3. Create `docs/CURRENT_STATE.md` and update README to stop carrying stale status.
4. Normalize PRD source map: one matrix, active/reference/deprecated tags, fix uppercase `docs/` links.
5. Normalize AI/model manifest paths and hash policy without downloading/changing binaries.
6. Add LFS/runtime dependency documentation and CI pointer checks.
7. Add save/load characterization tests before changing persistence.
8. Add build-scene validation/screenshot-tour harness that reports scene health without fixing scenes yet.
9. Remove worker-thread world/UI mutation from `DomainSimulationAdapter`.
10. Route Consult Fate/dialog tool traces through the LLM tool validator/router.
11. Start `DomainSimulationAdapter` no-behaviour-change split.
12. Remove `Simulation` dependency on `Data.SliceJson`.
13. Add explicit model/LLM capability state: disabled, fallback, local real, cloud opt-in.
14. Start save system migration: file slots and schema version, keeping PlayerPrefs only as last-slot pointer.
15. Introduce input abstraction while preserving current controls.
16. Rebuild HUD/dialog/clock only after PRD matrix is stable.
17. Audit old root scenes and decide archive/delete after duplicate GUID fix.
18. Classify `GeneratedAssets`, `Assets/Generated`, `Reports`, `Assets/Plans`, `Assets/pold`, and `.claude/skills`.
19. Remove or archive TextMesh Pro examples only after Unity reference check.
20. Consolidate overfit magic/shield tests and add product-visible PlayMode tests.
21. Split `JobAssignmentSystem` with deterministic characterization tests.
22. Split `WorldgenService` with same-seed world digest tests.
23. Split character creation controller with screenshot regression proof.
24. Run full Unity EditMode + PlayMode + scene tour + save/load + LLM proof.
25. Only after the above, allow new Ember gameplay feature work.

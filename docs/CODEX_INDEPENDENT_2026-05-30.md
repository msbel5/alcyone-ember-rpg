## Ember Audit Summary

Static repository audit only. I did **not** open the Unity Editor, run Play Mode, import assets, rebuild scenes, resolve LFS files, or modify anything. Several findings below are therefore marked as requiring Unity Editor inspection. The uploaded zip appears to be a newer cleanup state than the earlier audit: some previous defects are fixed, but several docs still describe old defects as current.

### 1. Executive summary

* **Unity version verified:** `ProjectSettings/ProjectVersion.txt` declares Unity `6000.3.13f1`; `Packages/manifest.json` uses URP `17.3.0`.
* **The requested second file, `docs/EMBER_GOAL.md`, is missing.** That is not a minor doc issue: the user’s expected canonical read order no longer matches the repo.
* **The repo now has `docs/CURRENT_STATE.md`, but it is already stale.** It still claims issues that are fixed or structurally changed, such as root non-build scenes and the old 1173-line single `DomainSimulationAdapter.cs`.
* **The most current remediation tracker appears to be `docs/REMEDIATION_V2_COUNTER.md`, not `docs/AUDIT_COUNTER.md` or `docs/CURRENT_STATE.md`.** Even that tracker conflicts with current filesystem state in places.
* **The actual engine/project structure is coherent enough to open, but runtime proof from this zip is blocked by Git LFS pointers.** Runtime DLLs, models, and many art images are pointer files, not real assets.
* **`.meta` integrity improved but is not clean.** Duplicate GUIDs are no longer present, and non-hidden asset files have metas, but 13 orphan `.meta` files remain.
* **CI is source-only by default.** `.github/workflows/unity-test.yml` checks out with `lfs: false`, runs static audit and EditMode tests, and leaves PlayMode/build optional. It cannot prove real LLM, model generation, art import, Windows build, or scene playability.
* **The LLM/AI stack is better separated than before but still not production-trustworthy.** LLM provider code moved to `Assets/Scripts/Infrastructure`, but runtime model/DLL presence is not strongly validated, manifest hashes are `TBD`, and docs contradict each other about whether real Qwen round-trip is verified.
* **Simulation authority is partially protected but still leaky.** Domain/Data mostly avoid Unity; however `EmberCrpg.Simulation` still references `EmberCrpg.Data.SliceJson`, and save/process side stores live in a Presentation save service.
* **`DomainSimulationAdapter` was split into partials, but the god-object problem remains.** The single file is smaller, but aggregate responsibility still includes world state, tick bridge, UI read models, combat, worldgen, save restore, dialog, fate, and LLM tool traces.
* **The living-world CRPG promise is not yet fully wired.** Systems for plant growth, prices, jobs, schedules, factions, harvest/planting, etc. exist, but `WorldTickComposer` only advances time, magic, hourly needs, and daily caravans.
* **NPC schedules/memory/faction context exist more as data than lived behaviour.** `ActorScheduleState` is saved/mapped, but no production schedule runner appears to consume it.
* **Generated world NPCs are not yet the actual visible world.** `WorldFactory` still creates fixed actors such as Warden, Sage Nera, Quartermaster Ivo, Sentinel Rook, and Ash Rat; generated NPC seeds are stored but not clearly projected into scene ActorViews.
* **Ask About/player interaction is scene-incomplete.** Static scene scan shows several gameplay scenes have interactables but no `DialogBoxPanel`; `EmberPlayerInteractRaycaster` depends on finding one.
* **Dialog identity is still stringly typed.** Interaction passes `target.DisplayName`, not a stable actor/NPC ID, which is unsafe for memory, faction, provenance, and save/load continuity.
* **Input has improved but is not migrated.** Direct `UnityEngine.Input` calls appear centralized behind `EmberInput`, but the project still uses the legacy input API instead of Input System actions/rebinding.
* **Save/load improved but remains prototype-shaped.** There is now a file repository, corrupt-save quarantine, and legacy fallback, but the UI uses one default slot, mirrors into `PlayerPrefs`, and nests domain JSON inside a Unity envelope.
* **Scene build list is cleanly concentrated under `Assets/Scenes/Ember`, but scene playability is not proven statically.** Static tests cannot prove camera feel, collisions, portal reachability, missing material imports, or UI readability.
* **Do not add features yet.** The next Codex passes should be cleanup/proof passes: source-of-truth docs, LFS/runtime validation, scene interaction proof, save/load proof, adapter decomposition, and simulation authority boundaries.

### 2. Ember soul alignment

#### Preserved strengths

The repository still aims at Ember rather than a generic Unity action RPG. `README.md`, `docs/EMBER_VISION_BIBLE.md`, and `docs/EMBER_VISION_NOTES_MAMI.md` preserve the core identity: deterministic single-player living-world CRPG, simulation as source of truth, local/flavour LLM only, persistent NPCs, faction/world context, Daggerfall-scale feeling, Fallout/Morrowind-style readable interaction, and systemic colony/world simulation underneath presentation.

The codebase also preserves some architectural guardrails. `EmberCrpg.Domain`, `EmberCrpg.Data`, `EmberCrpg.Data.SliceJson`, `EmberCrpg.Simulation`, and `EmberCrpg.Infrastructure` use `noEngineReferences: true` where expected, and most Unity-specific code is under Presentation/UI/Editor. LLM provider implementation has moved out of Simulation into `Assets/Scripts/Infrastructure/AiDm`, which is the right direction.

#### Dangerous drift

The project still risks becoming a visual slice with simulation labels rather than a living-world CRPG.

The strongest drift is that many systems exist as isolated services/tests/data mappings but are not demonstrably part of the authoritative tick. `WorldTickComposer` currently advances time, magic, needs, and caravans, while plant growth, jobs, schedules, faction politics, price updates, harvest/planting, and generated NPC life are not obviously composed into the main simulation loop. For Ember, that means the world can look systemic while failing to live.

The second drift is UI/player-flow incompleteness. Static scene data shows multiple gameplay scenes with interactables but no `DialogBoxPanel`, while interaction code depends on a dialog panel being present. Ask About cannot be Ember’s core CRPG interface if it works only in selected scenes.

The third drift is AI proof inflation. Some docs claim LLM proof is complete, while `docs/AI_STACK.md` still says real LLM proof is unverified, and the uploaded zip contains LFS pointer files for model/DLL assets. A source-only repo cannot honestly claim real local Qwen/LLamaSharp proof.

#### Missing pillars

* Canonical current-state doc that matches the actual repo.
* Scene-level proof that every build scene supports interaction, portals, HUD, pause, save/load, and dialog.
* Stable actor/NPC IDs in interaction and dialog.
* Main tick composition for jobs, schedules, plant growth, prices, factions, harvest/planting, and generated NPC behaviour.
* Runtime validation that model/DLL/art LFS pointers are resolved before claiming LLM or generated art works.
* Robust multi-slot save/load UI with schema migration and deterministic replay proof.
* Actor memory/faction/topic Ask About flow tied to the actual actor, not display names.
* Clear generated-asset provenance: real generated, fallback generated, authored, or missing.

#### Systems pretending to exist but not truly wired

* **Living-world simulation:** many systems exist, but main ticking is narrow.
* **Ask About across the world:** dialog panels exist in only some gameplay scenes.
* **Generated NPC world:** generated NPC seeds exist, but visible scene actors still appear fixed/scaffolded.
* **AI proof:** proof docs exist, but the source zip has pointer binaries/models and contradictory docs.
* **Save/load:** file save exists, but player-facing flow still behaves like one-slot prototype with `PlayerPrefs` compatibility.
* **Scene playability:** static/editor tests exist, but they do not replace manual Play Mode scene tours.

#### Generated/placeholder work hiding product problems

Art/model generation paths, proof screenshots, and scene validation docs can hide the fact that the uploaded source-only state does not contain real binary/model/art bytes. Because many `Assets/Art`, `Assets/Plugins`, and `Assets/StreamingAssets/Models` files are LFS pointers, a Codex pass can falsely pass static tests while the actual visual/LLM build remains invalid.

### 3. Canonical source map

| Area                   | Canonical source                                                                                 | Stale/archive/reference source                                          | Notes                                                                                                |
| ---------------------- | ------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Project overview       | `README.md`, but only as entry point                                                             | Old status assumptions from missing `docs/EMBER_GOAL.md`                | README now points to `docs/CURRENT_STATE.md`; it should not carry volatile status.                   |
| Current repo state     | `docs/REMEDIATION_V2_COUNTER.md` plus actual code/filesystem                                     | `docs/CURRENT_STATE.md`, `docs/AUDIT_COUNTER.md`, `docs/Codex_audit.md` | `CURRENT_STATE.md` is partly stale; `AUDIT_COUNTER.md` overclaims “all closed”; code is final truth. |
| Missing expected canon | None                                                                                             | `docs/EMBER_GOAL.md`                                                    | The file requested in read order is absent. Need replacement/update in instructions/docs.            |
| Vision/spirit          | `docs/EMBER_VISION_BIBLE.md`, `docs/EMBER_VISION_NOTES_MAMI.md`                                  | Older phase fences where contradicted by current implementation         | These remain the strongest Ember identity sources.                                                   |
| Mechanics              | `docs/mechanics/MASTER_MECHANICS_BIBLE.md`, `docs/mechanics/ARCHITECTURE.md`                     | Older backend/Godot references                                          | Mechanics docs are broad reference; code/tick composition proves what is real.                       |
| PRD governance         | `docs/PRD_GOVERNANCE.md`, `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`, active `docs/prds/*.md` | `Reference/PRDs/**` as design reference                                 | `docs/PRD_GOVERNANCE.md` still mentions `docs/reference/prd/`, which is absent.                      |
| Active Unity PRDs      | `docs/prds/PRD_overland_map_v1.md`, `docs/prds/PRD_visual_architecture_3d_billboard_v1.md`       | `Reference/PRDs/**`                                                     | Active Unity PRDs are few; old PRDs are reference-only unless matrix says otherwise.                 |
| Runtime source         | `Assets/Scripts/Domain`, `Data`, `Simulation`, `Infrastructure`, `Presentation`, `Ui`            | Any old notes saying LLM is in Simulation                               | Infrastructure split exists; Simulation still has persistence dependency debt.                       |
| Editor tooling         | `Assets/Editor/Ember/**`                                                                         | Old rescue/patch folders if not referenced                              | Editor tools appear active; scene/prefab changes require Unity verification.                         |
| Scenes                 | `Assets/Scenes/Ember/**`, `ProjectSettings/EditorBuildSettings.asset`                            | Old references to root `CombatPlayground` / `Sprint4Foundation`         | Current zip has only 13 Ember build scenes under `Assets/Scenes/Ember`.                              |
| Tests                  | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode`                                                 | Audit/proof docs pretending tests equal playability                     | EditMode coverage is broad; PlayMode scene tour coverage is thin.                                    |
| Generated assets       | Intended `Assets/Generated/Core` runtime/cache policy                                            | `Assets/Generated/Core.meta` orphan; old `GeneratedAssets/` references  | `GeneratedAssets/` root is absent; generated policy still needs cleanup.                             |
| Runtime models         | `Assets/StreamingAssets/Models/manifest.json`                                                    | Proof docs if LFS not resolved                                          | Manifest paths now match nested layout, but hashes are `TBD` and zip has pointers.                   |
| Plugins                | `Assets/Plugins/**`                                                                              | `Assets/Plugins/NuGet/.nuget-installed.json` as hidden marker           | Plugin DLLs are LFS pointers in source zip; import settings need Editor proof.                       |
| Reports/proofs         | `docs/proofs/**` as evidence, not source of truth                                                | Missing top-level `Reports/` from older docs                            | `Reports/` is absent in this upload; stale docs still mention report clutter.                        |
| Reference data         | `Reference/OldBackendData/**`, `Reference/PRDs/**`                                               | Not active runtime source                                               | Must not be wired directly without import/migration.                                                 |
| Dev tooling            | `.claude/skills/**`, `tools/**`                                                                  | Not product source                                                      | Classify as contributor tooling; keep out of runtime decisions.                                      |
| CI/validation          | `.github/workflows/unity-test.yml`, `tools/validation/**`                                        | Proof docs that assume LFS/builds                                       | CI is source-only by default and not enough for runtime proof.                                       |

### 4. Defect register

|CheckBox     | ID          | Severity | Category                         | Path(s)                                                                                                                                                                                                                                 | Evidence                                                                                                                                                                               | Why it matters                                                                                                                          | Fix direction                                                                                                                                  | Codex-safe?                                        | Validation                                                                                                    |
| ----------- | ----------- | -------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
|    `[ ]`    | EMB-AUD-001 | Critical | Docs hygiene / source truth      | `docs/EMBER_GOAL.md`                                                                                                                                                                                                                    | File is absent, but user-required read order expects it immediately after README.                                                                                                      | Codex and reviewers will follow stale external instructions or fail to locate the supposed current goal.                                | Create/restore a replacement only after deciding whether `docs/CURRENT_STATE.md` or `docs/REMEDIATION_V2_COUNTER.md` is the current successor. | Yes, docs-only.                                    | Static file existence and docs link check. Editor: no.                                                        |
|    `[ ]`    | EMB-AUD-002 | High     | Docs conflict                    | `docs/CURRENT_STATE.md`, `docs/REMEDIATION_V2_COUNTER.md`, `docs/AUDIT_COUNTER.md`, actual files                                                                                                                                        | `CURRENT_STATE.md` still mentions old root scenes, old `DomainSimulationAdapter` LOC, `Reports` clutter; `AUDIT_COUNTER.md` claims all closed; remediation doc says work remains.      | Current status is unreliable; Codex could undo fixed work or ignore open defects.                                                       | Make one current-state page authoritative; move old audit counters to archive/reference.                                                       | Yes.                                               | Compare current-state claims against filesystem and static scans. Editor: no.                                 |
|    `[ ]`    | EMB-AUD-003 | High     | AI docs conflict                 | `docs/AI_STACK.md`, `docs/proofs/llm-roundtrip-2026-05-30.md`, `docs/AUDIT_COUNTER.md`                                                                                                                                                  | AI stack doc says real LLM proof still unverified; proof doc and audit counter claim real Qwen proof.                                                                                  | Ember’s LLM must be bounded and truthful. Contradictory proof status invites fake AI claims.                                            | Split AI status into “source-only”, “LFS-resolved local”, and “verified runtime proof.”                                                        | Yes.                                               | Docs consistency check plus LFS-resolved runtime proof. Editor: yes for runtime.                              |
|    `[ ]`    | EMB-AUD-004 | High     | LFS/runtime proof                | `Assets/Plugins/**`, `Assets/StreamingAssets/Models/**`, `Assets/Art/**`, `.gitattributes`                                                                                                                                              | Runtime DLLs/models and many art files are Git LFS pointer files in the uploaded zip. Static audit reports 25 runtime binary/model pointers; broader scan finds many art pointers too. | Source-only tests can pass while LLM, ONNX generation, native plugins, textures, and visual build fail.                                 | Add explicit source-only vs runtime-LFS validation modes; require real bytes for build/AI/art proof.                                           | Yes for validation/docs; no for binary resolution. | Pointer scan; LFS-resolved clone; Unity import/build. Editor: yes for full proof.                             |
|    `[ ]`    | EMB-AUD-005 | Medium   | Unity `.meta` integrity          | `Assets/AI Toolkit.meta`, `Assets/Audio.meta`, `Assets/Generated/Core.meta`, `Assets/Editor/Ember/Patches.meta`, `Assets/Scripts/Presentation/Ember/AiDm.meta`, `Assets/Art/UI/*.meta`                                                  | Static audit warns 13 orphan `.meta` files.                                                                                                                                            | Orphan metas indicate deleted/ignored assets and make Unity asset ownership ambiguous.                                                  | Restore missing assets if intentional, otherwise delete orphan metas through Unity-safe cleanup.                                               | Partial; deletion needs care.                      | Static audit must report zero orphan metas or documented exceptions. Editor: yes.                             |
|    `[ ]`    | EMB-AUD-006 | Low      | Unity import hygiene             | `Assets/Plugins/NuGet/.nuget-installed.json`                                                                                                                                                                                            | Hidden marker file exists in `Assets` and has no `.meta`; static audit skips hidden paths.                                                                                             | Unity usually ignores dot-prefixed paths, but tooling/dev metadata in `Assets` is still noisy.                                          | Move marker outside `Assets` or document why it is ignored.                                                                                    | Yes.                                               | Static hidden-file scan. Editor: no.                                                                          |
|    `[ ]`    | EMB-AUD-007 | Medium   | Generated asset policy           | `Assets/Generated/Core.meta`, `.gitignore`, generated-output docs                                                                                                                                                                       | `Assets/Generated/Core.meta` exists without `Assets/Generated/Core`; `.gitignore` ignores generated core outputs.                                                                      | Generated content policy is unresolved; generated assets can look canonical or disappear locally.                                       | Decide tracked generated fixtures vs local cache; remove orphan meta or restore folder intentionally.                                          | Yes with Unity-safe verification.                  | Static generated folder inventory; Unity import check. Editor: yes.                                           |
|    `[ ]`    | EMB-AUD-008 | High     | Model verification               | `Assets/StreamingAssets/Models/manifest.json`, `Assets/Scripts/Simulation/Forge/ModelManifest.cs`                                                                                                                                       | Manifest paths now match nested layout, but hashes are `TBD`; verification skips placeholder hashes.                                                                                   | Model existence without hash/size validation accepts LFS pointers and corrupted models.                                                 | Add size/hash validation for runtime mode; allow placeholders only in source-only mode.                                                        | Yes.                                               | Model manifest test with pointer rejection under runtime mode. Editor: no.                                    |
|    `[ ]`    | EMB-AUD-009 | High     | Native LLM readiness             | `Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs`, `Assets/StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf`                                                                                                              | Availability is file-presence oriented; source zip contains GGUF pointer file.                                                                                                         | Native LLM can appear available but fail at runtime loading pointer bytes.                                                              | Validate model size/hash/header before enabling native client.                                                                                 | Yes.                                               | Unit test with LFS pointer fixture rejected; real model accepted. Editor: yes for runtime.                    |
|    `[ ]`    | EMB-AUD-010 | Medium   | Network/threading                | `Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs`                                                                                                                                                                                 | `EnsureModelReady` can download via `HttpClient.GetAsync(_downloadUrl)` without a visible timeout/cancellation path equivalent to response timeout.                                    | Loading can hang or silently turn startup into a downloader. Ember should be local/offline by default.                                  | Require explicit opt-in download, timeout, cancellation, and progress/error UI.                                                                | Yes.                                               | Fake slow server test; default config test denies download. Editor: no.                                       |
|    `[ ]`    | EMB-AUD-011 | Medium   | LLM/network blocking             | `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`, `Assets/Scripts/Simulation/Forge/ComfyUiAssetForge.cs`                                                                                                                              | Uses sync-over-async `.GetAwaiter().GetResult()` for HTTP availability/completion.                                                                                                     | Even if called from worker threads, sync blocking makes cancellation and UI responsiveness fragile.                                     | Convert provider API to true async with cancellation, or isolate blocking calls behind dedicated worker service.                               | Partial.                                           | Fake timeout/cancellation tests; Play Mode no-freeze proof. Editor: yes.                                      |
|    `[ ]`    | EMB-AUD-012 | Critical | CI/build reliability             | `.github/workflows/unity-test.yml`                                                                                                                                                                                                      | Default workflow checks out with `lfs: false`, runs static audit and EditMode; PlayMode/build are manual/optional and also source-only.                                                | CI green does not prove the game opens, builds, displays real art, or runs LLM/forge.                                                   | Add separate runtime-LFS workflow or required scheduled build; label current CI “source-only.”                                                 | Yes.                                               | CI matrix: source-only, LFS runtime validation, build, PlayMode. Editor/Unity: yes.                           |
|    `[ ]`    | EMB-AUD-013 | Medium   | Static audit severity            | `tools/validation/static-audit.sh`                                                                                                                                                                                                      | Orphan metas are WARN, not failure; pointer files are INFO unless `--require-runtime` is passed.                                                                                       | Bad asset state can persist through default validation.                                                                                 | Decide which conditions are fatal for PRs; keep source-only exceptions explicit.                                                               | Yes.                                               | Static audit fails on unapproved orphan metas; runtime mode fails on pointers. Editor: no.                    |
|    `[ ]`    | EMB-AUD-014 | Medium   | Package hygiene                  | `Packages/manifest.json`, `Packages/packages-lock.json`                                                                                                                                                                                 | Manifest requests `com.unity.test-framework: 1.4.6`; lock resolves `1.6.0`.                                                                                                            | Package restore may rewrite lock or behave differently across machines/CI.                                                              | Normalize manifest/lock by opening in target Unity and committing resolved state.                                                              | Partial; needs Unity.                              | Clean package restore in Unity `6000.3.13f1`. Editor: yes.                                                    |
|    `[ ]`    | EMB-AUD-015 | High     | asmdef boundary                  | `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef`                                                                                                                                                                                 | Simulation references `EmberCrpg.Data.SliceJson`.                                                                                                                                      | Deterministic simulation should not depend on concrete JSON persistence.                                                                | Move save composition to Data/Persistence/Presentation; expose pure contracts only.                                                            | Partial.                                           | asmdef compile with Simulation independent from SliceJson. Editor: yes for assembly reload.                   |
|    `[ ]`    | EMB-AUD-016 | High     | Persistence boundary             | `Assets/Scripts/Presentation/Ember/Save/JsonSliceSaveService.cs`                                                                                                                                                                        | Presentation save bridge owns `WorksiteStore`, `JobBoard`, `Soils`, `Plants`, and recipe work orders “until full world-root process store lands.”                                      | Authoritative process state should live in the world/save model, not a Presentation service side store.                                 | Move process stores into world state or a pure simulation persistence root; Presentation should only call repository/adapter.                  | Partial.                                           | Save/load roundtrip of jobs/plants/worksites without Presentation side stores. Editor: no initially.          |
|    `[ ]`    | EMB-AUD-017 | High     | Save/load architecture           | `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`, `Assets/Scripts/Data/Save/FileSaveRepository.cs`                                                                                                                          | File saves exist, but `DefaultSlot = 0`, `PlayerPrefs` stores last slot and legacy blob mirror.                                                                                        | CRPG save/load needs durable slots, migration, and predictable player-facing behaviour.                                                 | Add real slot model/UI; keep PlayerPrefs only as compatibility pointer or remove after migration.                                              | Partial.                                           | Save slot tests, corrupt-save quarantine, UI slot proof. Editor: yes.                                         |
|    `[ ]`    | EMB-AUD-018 | Medium   | Save schema                      | `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`                                                                                                                                                                            | Save envelope stores `domainStateJson` as nested string plus Unity scene/player position fields.                                                                                       | Nested JSON makes migrations/search/debug harder and couples Unity scene state to domain save.                                          | Introduce explicit outer schema version and structured domain envelope.                                                                        | Yes after characterization.                        | Golden save fixture before/after migration. Editor: no.                                                       |
|    `[ ]`    | EMB-AUD-019 | High     | Main menu save flow              | `Assets/Scripts/Presentation/Ember/UI/EmberMainMenuUI.cs`, `EmberSaveService.cs`                                                                                                                                                        | Main menu Continue/LoadGame reads `PlayerPrefs.GetString("ember.save.v1")`, not `FileSaveRepository` slots.                                                                            | Player-facing load can ignore file-slot source of truth and preserve legacy single-slot assumptions.                                    | Route menu Continue through the same save repository/service used by runtime saves.                                                            | Yes.                                               | Save in-game, restart, Continue loads from file slot with PlayerPrefs cleared/changed. Editor: yes.           |
|    `[ ]`    | EMB-AUD-020 | Medium   | Save DTO size/schema drift       | `Assets/Scripts/Data/Save/WorldSaveData.cs`, `Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper*.cs`                                                                                                                                   | `WorldSaveData.cs` is 527 LOC and carries legacy role fields; mapper split exists but schema remains broad.                                                                            | Save schema will keep accreting fields and become impossible to migrate safely.                                                         | Split DTOs by subsystem and freeze legacy compatibility fields behind migration tests.                                                         | Yes if no field renames first.                     | Golden old-save/new-save roundtrip. Editor: no.                                                               |
|    `[ ]`    | EMB-AUD-021 | High     | Architecture/SOLID               | `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter*.cs`                                                                                                                                                                | Main file 471 LOC plus partials for worldgen/combat/save/fate; aggregate remains a large command/read-model/dialog/save/LLM bridge.                                                    | Partial classes reduced file size but not responsibility count. Codex changes here remain high-risk.                                    | Split into explicit services: tick bridge, read-model projector, command router, dialog adapter, save bridge, fate/LLM adapter.                | Partial, staged only.                              | Characterization tests before each extraction; scene smoke. Editor: yes for final smoke.                      |
|    `[ ]`    | EMB-AUD-022 | Medium   | Async/state timing               | `DomainSimulationAdapter.Fate.cs`, `DomainSimulationAdapter.cs`                                                                                                                                                                         | LLM/fate results enqueue `_mainThreadApply`, drained in `AdvanceTick`.                                                                                                                 | If ticking is paused or blocked, thinking flags/results can stall; authoritative trace application is tied to presentation tick timing. | Add deterministic main-thread pump independent of sim tick or explicitly drain during UI update.                                               | Partial.                                           | Pause/Ask Fate test; no stuck thinking state. Editor: yes.                                                    |
|    `[ ]`    | EMB-AUD-023 | Critical | Ember soul / simulation wiring   | `Assets/Scripts/Simulation/WorldTickComposer.cs`, `Assets/Scripts/Simulation/Process/**`, `Assets/Scripts/Simulation/Farming/**`, `Assets/Scripts/Simulation/Economy/**`                                                                | Composer advances time, magic, needs, caravans; many systems exist but are not composed into main tick.                                                                                | Ember’s promise is a living world; isolated systems/tests do not make the world live.                                                   | Build a deterministic simulation composition plan and wire systems in small verified passes.                                                   | Partial.                                           | Same-seed tick digest including needs/jobs/plants/prices/factions. Editor: no for sim, yes for visible proof. |
|    `[ ]`    | EMB-AUD-024 | High     | Process/world authority          | `JsonSliceSaveService.cs`, process/farming stores                                                                                                                                                                                       | Process stores live outside core `WorldState` in save bridge.                                                                                                                          | Jobs/plants/worksites cannot be clearly authoritative or replayable if owned by Presentation.                                           | Move stores to `WorldState` or pure simulation aggregate, then map through Data.                                                               | Partial.                                           | Save/load/replay of worksite/job/plant state without Presentation service. Editor: no.                        |
|    `[ ]`    | EMB-AUD-025 | High     | NPC schedules                    | `Assets/Scripts/Domain/World/ActorScheduleState.cs`, save mapper, runtime systems                                                                                                                                                       | Schedule state is saved/mapped, but no production schedule runner appears to consume it.                                                                                               | Ember requires actors with schedules, not static actors with saved schedule DTOs.                                                       | Add deterministic schedule system only after actor IDs/scene proof are stable.                                                                 | No for feature; yes for audit/tests.               | Static references plus Play Mode actor schedule proof. Editor: yes.                                           |
|    `[ ]`    | EMB-AUD-026 | High     | Generated world integration      | `Assets/Scripts/Simulation/Worldgen/**`, `Assets/Scripts/Simulation/WorldFactory.cs`, `DomainSimulationAdapter.Worldgen.cs`                                                                                                             | `WorldFactory` still creates fixed actors; generated NPC seeds are stored separately.                                                                                                  | Procedural genesis is not canonical if generated actors are not the actual world actors.                                                | Define generated-world-to-actor projection with stable IDs and scene spawning.                                                                 | Partial.                                           | Same seed creates same visible actors with IDs, schedules, memory. Editor: yes.                               |
|    `[ ]`    | EMB-AUD-027 | Critical | Dialog/player flow               | `Assets/Scenes/Ember/SmithingOverworld.unity`, `ColonyNeeds.unity`, `SeasonFarm.unity`, `TradeMarket.unity`, `CombatDungeon.unity`, `RitualHall.unity`, `Assets/Scripts/Presentation/Ember/Interaction/EmberPlayerInteractRaycaster.cs` | These scenes have interactables but no `DialogBoxPanel` by static component scan; raycaster opens dialog via `FindFirstObjectByType<DialogBoxPanel>()`.                                | Ask About is a core Ember interface. Interacting with NPCs silently failing in half the route breaks the game identity.                 | Ensure every scene with interactables has a dialog surface or route dialog through a persistent UI host.                                       | Partial; scene edits require Unity.                | Scene tour: press interact on each NPC; dialog opens. Editor: yes.                                            |
|    `[ ]`    | EMB-AUD-028 | High     | Dialog identity                  | `EmberPlayerInteractRaycaster.cs`, `IDomainSimulationAdapter`, `DomainSimulationAdapter.cs`                                                                                                                                             | Interaction passes `target.DisplayName`; remediation tracker flags missing stable `NpcId`.                                                                                             | Memory, faction context, schedules, and save/load cannot depend on display names.                                                       | Add stable actor/NPC ID to interactables and dialog source lookup.                                                                             | Partial.                                           | Two NPCs with same display name resolve distinctly. Editor: yes for scene refs.                               |
|    `[ ]`    | EMB-AUD-029 | Medium   | Scene loading                    | `Assets/Scripts/Presentation/Ember/SceneFlow/EmberScenePortal.cs`, scene YAML                                                                                                                                                           | Portals serialize raw `_targetSceneName` and call `SceneManager.LoadScene(_targetSceneName)`.                                                                                          | Hardcoded scene strings break silently on rename and bypass the scene registry.                                                         | Use `EmberScenes` registry or ScriptableObject scene references; validate targets in Editor.                                                   | Yes with Editor validation.                        | Static target-name validation against build settings; portal tour. Editor: yes.                               |
|    `[ ]`    | EMB-AUD-030 | Medium   | Scene proof                      | `Assets/Scenes/Ember/**`, `docs/proofs/scene-validation-2026-05-30.txt`                                                                                                                                                                 | Proof doc reports 13 scenes, 3 with issues; static YAML cannot prove collisions/reachability/cameras.                                                                                  | Ember must be playable, not just importable.                                                                                            | Add automated scene tour plus manual checklist per scene.                                                                                      | Yes for harness; fixes need Editor.                | Load each scene, move, interact, portal, save, screenshot. Editor: yes.                                       |
|    `[ ]`    | EMB-AUD-031 | Medium   | Boot/menu scene validation       | `Assets/Scenes/Ember/Boot.unity`, `MainMenu.unity`, `CharacterCreation.unity`                                                                                                                                                           | Static scene validation/proof flags missing EventSystem/camera in some non-gameplay scenes; may be runtime-created.                                                                    | Runtime-created UI is acceptable only if proven. Static absence cannot be ignored.                                                      | Document which scenes intentionally bootstrap UI and add PlayMode proof.                                                                       | Yes.                                               | Boot → menu → creation PlayMode test and screenshots. Editor: yes.                                            |
|    `[ ]`    | EMB-AUD-032 | Medium   | Input system                     | `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`                                                                                                                                                                                | Direct legacy input calls are centralized in `EmberInput`, but still wrap `UnityEngine.Input`.                                                                                         | Centralization is good, but no rebinding/action maps/accessibility.                                                                     | Introduce Input System actions behind the same facade.                                                                                         | Yes, staged.                                       | Static direct `Input.` outside facade remains zero; manual controls pass. Editor: yes.                        |
|    `[ ]`    | EMB-AUD-033 | High     | UI/product alignment             | `Assets/Scripts/Presentation/Ember/UI/EmberHud.cs`, `DialogBoxPanel.cs`, `EmberMainMenuUI.cs`, `docs/prds/**`, `docs/CURRENT_STATE.md`                                                                                                  | HUD 538 LOC, dialog 335, main menu 345; docs still list missing clock/character record/Ask DM and PRD alignment gaps.                                                                  | Ember needs readable CRPG UI, not procedural prototype panels.                                                                          | Freeze UI feature creep; align HUD/dialog/clock/action bar to active PRDs with screenshot proof.                                               | Partial.                                           | Screenshot comparison across build scenes. Editor: yes.                                                       |
|    `[ ]`    | EMB-AUD-034 | Medium   | UI boundary                      | `Assets/Scripts/Ui/Foundation/EmberCrpg.Ui.Foundation.asmdef`, `Assets/Scripts/Ui/Foundation/**`                                                                                                                                        | UI Foundation has `noEngineReferences: false` and uses Unity types.                                                                                                                    | “Foundation” is not backend-neutral; UI model and Unity rendering are mixed.                                                            | Rename boundary honestly or split pure UI model from Unity tokens/resources.                                                                   | Yes.                                               | asmdef dependency test; UI compile. Editor: yes.                                                              |
|    `[ ]`    | EMB-AUD-035 | Medium   | LLM provider organization        | `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`                                                                                                                                                                                      | 517 LOC with config, local Qwen, cloud client, HTTP JSON handling in one file.                                                                                                         | AI integration will keep growing; provider risk should be isolated.                                                                     | Split provider contracts/config/local/cloud/JSON parsing after adding tests.                                                                   | Yes.                                               | Fake provider tests; no network by default. Editor: no.                                                       |
|    `[ ]`    | EMB-AUD-036 | Medium   | Asset forge/provider risk        | `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs`, `ComfyUiAssetForge.cs`, `ModelManifest.cs`                                                                                                                                         | ONNX/Comfy/model verification lives in Simulation namespace; Comfy availability does blocking HTTP.                                                                                    | Generation providers are infrastructure; simulation should remain deterministic.                                                        | Keep deterministic generation contracts in Simulation; move provider implementations to Infrastructure.                                        | Partial.                                           | Simulation assembly compiles without HTTP/provider implementation. Editor: yes for asmdef.                    |
|    `[ ]`    | EMB-AUD-037 | Medium   | Resources usage                  | `Assets/Resources/**`, UI/loading/font code                                                                                                                                                                                             | Runtime assets are hidden behind `Resources`; custom fonts/resources depend on import state.                                                                                           | `Resources` makes dependencies implicit and bloats builds if unmanaged.                                                                 | Keep only tiny global assets; move UI/theme/fonts to explicit references or a registry.                                                        | Partial.                                           | `Resources.Load` inventory and build size review. Editor: yes.                                                |
|    `[ ]`    | EMB-AUD-038 | Medium   | Plugin/dependency hygiene        | `Assets/Plugins/**`, `Assets/Plugins/NuGet/**`                                                                                                                                                                                          | Native/managed plugins live under Assets; NuGet marker exists; runtime DLLs are LFS pointers in this upload.                                                                           | Unity plugin import settings and transitive dependencies can silently break platform builds.                                            | Create dependency manifest with platform import settings and source-only/runtime states.                                                       | Partial.                                           | Unity plugin inspector/build on target platforms. Editor: yes.                                                |
|    `[ ]`    | EMB-AUD-039 | Low      | TextMesh Pro footprint           | `Assets/TextMesh Pro/**`                                                                                                                                                                                                                | TMP runtime resources are present; Examples & Extras are not present in this upload.                                                                                                   | Not a current sample-sprawl defect, but should remain monitored for unused sample imports.                                              | Keep TMP essentials; do not re-import examples unless needed.                                                                                  | Yes.                                               | Reference scan. Editor: yes if removing assets.                                                               |
|    `[ ]`    | EMB-AUD-040 | Medium   | Test coverage shape              | `Assets/Tests/EditMode/**`, `Assets/Tests/PlayMode/**`                                                                                                                                                                                  | EditMode coverage is broad; PlayMode mostly tests character creation/boot/fake forge, not all gameplay scenes.                                                                         | Backend tests do not prove Ember is playable as a CRPG route.                                                                           | Add scene tour PlayMode tests and reduce audit-only confidence.                                                                                | Yes.                                               | PlayMode route: menu → creation → every scene interact/portal/save. Editor: yes.                              |
|    `[ ]`    | EMB-AUD-041 | Medium   | Test bloat                       | `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`, magic/job tests                                                                                                                                                     | One magic test file is 1033 LOC; several tests exceed 300 LOC.                                                                                                                         | Overgrown tests are hard for Codex to maintain and can mask missing product tests.                                                      | Split/consolidate by scenario; keep golden integration cases.                                                                                  | Yes.                                               | Same test coverage, smaller files, no product coverage regression. Editor: no.                                |
|    `[ ]`    | EMB-AUD-042 | Medium   | Docs/reference clutter           | `docs/Codex_audit.md`, `docs/AUDIT_INDEPENDENT_2026-05-30.md`, `docs/AUDIT_COUNTER.md`, `docs/proofs/**`                                                                                                                                | These docs contain obsolete findings and overclaims mixed with useful evidence.                                                                                                        | Reviewers can mistake stale audit output for current truth.                                                                             | Move old audits/proofs under dated archive with status header.                                                                                 | Yes.                                               | Docs index: every audit marked current/reference/archive. Editor: no.                                         |
|    `[ ]`    | EMB-AUD-043 | Medium   | PRD governance drift             | `docs/PRD_GOVERNANCE.md`, `Reference/PRDs/**`, `docs/reference/**`                                                                                                                                                                      | Governance says `docs/reference/prd/` is a deprecated mirror, but that folder is absent; `Reference/PRDs` still exists.                                                                | Codex may search the wrong PRD tree or assume duplicates are already removed.                                                           | Update governance to actual folder layout and one active matrix.                                                                               | Yes.                                               | Broken path check. Editor: no.                                                                                |
|    `[ ]`    | EMB-AUD-044 | Medium   | Stale docs claims                | `docs/CURRENT_STATE.md`, old roadmap/docs                                                                                                                                                                                               | Current state references `Reports/` and old root scenes that are absent; roadmap mentions old Qwen/Copilot fallback language.                                                          | Stale references undermine cleanup and AI policy.                                                                                       | Archive or update stale docs; keep one dated status page.                                                                                      | Yes.                                               | Grep for absent paths/model names and resolve. Editor: no.                                                    |
|    `[ ]`    | EMB-AUD-045 | Low      | Reference data placement         | `Reference/OldBackendData/**`                                                                                                                                                                                                           | Large old backend JSON reference remains in repo.                                                                                                                                      | Useful, but easy to confuse with active Unity data.                                                                                     | Keep reference-only README and forbid runtime imports without migration plan.                                                                  | Yes.                                               | Source-map entry and no runtime direct reads. Editor: no.                                                     |
|    `[ ]`    | EMB-AUD-046 | Low      | Dev tooling placement            | `.claude/skills/**`                                                                                                                                                                                                                     | Local agent skills are tracked beside game source.                                                                                                                                     | Not product source; can confuse repository ownership.                                                                                   | Classify as contributor tooling or move under `tools/agent`.                                                                                   | Yes.                                               | Docs/source-map update. Editor: no.                                                                           |
|    `[ ]`    | EMB-AUD-047 | Medium   | Main menu/loading responsibility | `EmberMainMenuUI.cs`, `LoadingScreenController.cs`, `ModelBootstrap.cs`                                                                                                                                                                 | Menu/loading code participates in generation/bootstrap/save flow.                                                                                                                      | Entry flow should not own generation/provider policy.                                                                                   | Split UI presenter, loading coordinator, generation bootstrap, save presenter.                                                                 | Partial.                                           | Menu/loading PlayMode proof. Editor: yes.                                                                     |
|    `[ ]`    | EMB-AUD-048 | Medium   | Worldgen complexity              | `Assets/Scripts/Simulation/Worldgen/WorldgenService*.cs`                                                                                                                                                                                | `WorldgenService.Phases.cs` is 422 LOC; generated data exists but runtime projection is incomplete.                                                                                    | Worldgen is core to Ember; adding more biomes/factions before projection/digest tests is risky.                                         | Split deterministic phases and add world digest/projection tests.                                                                              | Yes.                                               | Same seed → same digest and visible actor projection. Editor: yes for visual proof.                           |
|    `[ ]`    | EMB-AUD-049 | Medium   | Job system isolation             | `Assets/Scripts/Simulation/Process/JobAssignmentSystem*.cs`, tests                                                                                                                                                                      | Job system is split but not visibly composed into main tick.                                                                                                                           | Colony simulation cannot matter if jobs only exist in tests/side stores.                                                                | Wire after process state ownership is moved to world root.                                                                                     | Partial.                                           | Tick digest includes job assignment/reservation changes. Editor: no.                                          |
|    `[ ]`    | EMB-AUD-050 | Low      | Secrets/security                 | Whole repo, `.gitignore`, AI clients                                                                                                                                                                                                    | No actual API keys found by static search; cloud client supports bearer config.                                                                                                        | Good baseline, but cloud/provider expansion could leak secrets.                                                                         | Keep secrets ignored and cloud providers opt-in only.                                                                                          | Yes.                                               | Secret scan in CI. Editor: no.                                                                                |

### 5. Codex-ready work packages

#### P0-A — Rebuild the source-of-truth map

**Goal:** make the repo’s docs reflect the actual filesystem and current remediation state.
**Files likely touched:** `docs/CURRENT_STATE.md`, `docs/REMEDIATION_V2_COUNTER.md`, `docs/AUDIT_COUNTER.md`, `docs/PRD_GOVERNANCE.md`, `README.md`.
**Files not to touch:** `Assets/**`, `Packages/**`, scenes, models, plugins.
**Risks:** accidentally canonizing stale audit claims.
**Acceptance criteria:**

* `docs/EMBER_GOAL.md` absence is explained or a replacement pointer exists.
* `docs/CURRENT_STATE.md` matches current files: no root scenes, no top-level `Reports/`, current adapter split, current LFS state.
* Old audit docs are marked archive/reference.
* One doc says exactly what is verified, unverified, source-only, and LFS-required.

**Suggested validation:** path/link grep; compare current-state checklist to `find` output.
**Unity Editor required:** no.
**Suggested Codex prompt title:** “P0: Make Ember current-state docs match the actual repository.”

#### P0-B — Runtime asset/LFS validation gate

**Goal:** stop source-only CI from being mistaken for runtime proof.
**Files likely touched:** `tools/validation/static-audit.sh`, `.github/workflows/unity-test.yml`, docs validation instructions.
**Files not to touch:** actual `.dll`, `.onnx`, `.gguf`, `.png`, `.jpg` assets.
**Risks:** making default CI too expensive if runtime-LFS checks are required everywhere.
**Acceptance criteria:**

* Source-only validation explicitly says it cannot prove LLM/forge/art/build.
* Runtime validation fails if DLL/model/art runtime files are LFS pointers.
* CI has separate source-only and runtime-LFS modes.

**Suggested validation:** run static audit with and without runtime mode; pointer fixtures fail runtime mode.
**Unity Editor required:** no for scan; yes for full runtime proof.
**Suggested Codex prompt title:** “P0: Add source-only vs runtime-LFS validation for Ember assets and AI.”

#### P0-C — Orphan `.meta` cleanup plan

**Goal:** remove or justify all orphan Unity metas.
**Files likely touched:** listed orphan `.meta` files only, or restored matching folders/assets.
**Files not to touch:** scene YAML, script GUIDs, plugin/model binaries.
**Risks:** deleting a meta for an asset that is intentionally LFS-ignored or temporarily absent.
**Acceptance criteria:**

* Static audit has zero unapproved orphan metas.
* Each removed/restored orphan is documented in the PR.
* No duplicate GUIDs introduced.

**Suggested validation:** `tools/validation/static-audit.sh`, Unity import log.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P0: Resolve orphan Unity .meta files without touching scene contents.”

#### P1-A — AI/model truthfulness pass

**Goal:** make LLM/forge claims match real runtime capability.
**Files likely touched:** `docs/AI_STACK.md`, `docs/proofs/**`, `Assets/StreamingAssets/Models/manifest.json`, `ModelManifest` tests, native client readiness checks.
**Files not to touch:** binary model contents.
**Risks:** breaking local setups if validation assumes all developers have full LFS assets.
**Acceptance criteria:**

* Manifest hash/size policy distinguishes source-only placeholders from runtime-required assets.
* Native LLM refuses LFS pointers.
* Proof docs are marked reproducible only with LFS-resolved assets.
* Default cloud/network providers remain opt-in.

**Suggested validation:** manifest tests, pointer rejection test, LFS-resolved LLM smoke proof.
**Unity Editor required:** yes for runtime proof.
**Suggested Codex prompt title:** “P1: Make Ember AI/model proof reproducible and pointer-safe.”

#### P1-B — Scene interaction proof before scene fixes

**Goal:** prove which scenes actually allow player interaction and dialog.
**Files likely touched:** PlayMode tests/editor validation only.
**Files not to touch:** scene YAML in this batch.
**Risks:** writing tests that only check component existence, not actual playability.
**Acceptance criteria:**

* Every build scene loads.
* For scenes with interactables, pressing interact opens expected UI or logs a clear missing-dialog failure.
* Portal targets validate against build settings.
* Screenshots are captured for human review.

**Suggested validation:** PlayMode scene-tour test plus screenshots.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P1: Add scene interaction and portal validation without modifying scenes.”

#### P1-C — Save/load characterization

**Goal:** protect current save behaviour before persistence refactor.
**Files likely touched:** tests for `EmberSaveService`, `FileSaveRepository`, `WorldSaveMapper`, menu Continue flow.
**Files not to touch:** schema fields or scene YAML.
**Risks:** current behaviour may be inconsistent; characterization will expose uncomfortable legacy dependencies.
**Acceptance criteria:**

* Tests cover file slot save/load, PlayerPrefs legacy fallback, corrupt save quarantine, missing adapter, invalid scene name, and main menu Continue.
* Current single-slot limitation is documented.

**Suggested validation:** EditMode tests plus manual save/load scene proof.
**Unity Editor required:** yes for menu/scene proof.
**Suggested Codex prompt title:** “P1: Characterize Ember save/load and main-menu continue before refactor.”

#### P2-A — Persistence dependency direction cleanup

**Goal:** remove Simulation’s dependency on concrete SliceJson persistence.
**Files likely touched:** asmdefs, save composition code, tests.
**Files not to touch:** save schema semantics in same PR.
**Risks:** assembly reload/compiler breakage.
**Acceptance criteria:**

* `EmberCrpg.Simulation.asmdef` no longer references `EmberCrpg.Data.SliceJson`.
* Simulation has no `JsonUtility`/file/Unity persistence dependency.
* Existing save tests pass.

**Suggested validation:** Unity assembly compile and EditMode tests.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P2: Remove Simulation dependency on SliceJson persistence.”

#### P2-B — Move process stores toward authoritative world state

**Goal:** stop Presentation save service from owning living-world process stores.
**Files likely touched:** `JsonSliceSaveService.cs`, world/process state classes, save mapper tests.
**Files not to touch:** UI, scenes, LLM.
**Risks:** save/load regressions in jobs/plants/worksites.
**Acceptance criteria:**

* Worksites/jobs/soils/plants are reachable from domain/simulation state, not Presentation-owned side stores.
* Save/load roundtrips these stores.
* Existing process/farming tests still pass.

**Suggested validation:** same-seed tick/save/load digest.
**Unity Editor required:** no initially.
**Suggested Codex prompt title:** “P2: Make process and farming stores authoritative, not Presentation side state.”

#### P2-C — Adapter responsibility split, no behaviour change

**Goal:** reduce `DomainSimulationAdapter` risk without changing scenes or gameplay.
**Files likely touched:** `DomainSimulationAdapter*.cs`, new helper classes under Presentation adapter namespace, tests.
**Files not to touch:** scenes, save schema, public MonoBehaviour names.
**Risks:** subtle gameplay/UI drift.
**Acceptance criteria:**

* Extract read-model projection, command routing, save bridge, and fate/dialog bridge behind tests.
* Public adapter API remains stable during this batch.
* No scene YAML changes.

**Suggested validation:** fallback, EditMode, and manual scene smoke.
**Unity Editor required:** yes for smoke.
**Suggested Codex prompt title:** “P2: Split DomainSimulationAdapter responsibilities without changing behaviour.”

#### P2-D — Stable actor/NPC interaction IDs

**Goal:** stop dialog and interaction from depending on display names.
**Files likely touched:** interactable components, adapter dialog lookup, scene validation/tests.
**Files not to touch:** actor memory/faction feature expansion yet.
**Risks:** scene component serialized fields need migration.
**Acceptance criteria:**

* Interactables carry stable actor/NPC IDs.
* Dialog lookup uses IDs.
* Duplicate display-name test passes.
* Existing scenes migrated safely.

**Suggested validation:** PlayMode interact with two named actors; scene reference check.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P2: Replace display-name dialog lookup with stable actor IDs.”

#### P3-A — Living-world tick composition plan

**Goal:** wire existing systems into deterministic tick only after state ownership is safe.
**Files likely touched:** `WorldTickComposer`, process/farming/economy/faction/schedule tests.
**Files not to touch:** visual scene content, LLM flavour.
**Risks:** changing simulation frequency can alter saves and tests.
**Acceptance criteria:**

* A documented tick schedule exists.
* Jobs, plants, prices/factions, and schedules are added one subsystem at a time.
* Same-seed digest tests cover each subsystem.

**Suggested validation:** deterministic replay/save-load digest.
**Unity Editor required:** no for core; yes for visible proof.
**Suggested Codex prompt title:** “P3: Wire Ember living-world systems into deterministic tick composition.”

#### P3-B — UI/Ask About scene fix

**Goal:** make Ask About available wherever NPC interaction exists.
**Files likely touched:** scenes with missing dialog panel, UI host setup, `DialogBoxPanel`, validation tests.
**Files not to touch:** LLM provider or generated world logic.
**Risks:** scene edits can break references if metas or components change.
**Acceptance criteria:**

* Every scene with interactables opens dialog.
* Dialog UI is readable and modal input does not conflict with movement.
* Screenshot proof for all affected scenes.

**Suggested validation:** manual and PlayMode interaction tour.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P3: Ensure every Ember gameplay scene supports Ask About interaction.”

#### P4-A — Docs and reference archive cleanup

**Goal:** make old docs useful but non-authoritative.
**Files likely touched:** `docs/archive/**`, `docs/proofs/**`, `docs/reference/**`, `Reference/PRDs/**`, README links.
**Files not to touch:** code/assets.
**Risks:** losing design intent if docs are deleted instead of classified.
**Acceptance criteria:**

* Every audit/proof/status doc has current/reference/archive status.
* PRD governance references real paths.
* No stale absent-path claims remain.

**Suggested validation:** docs inventory and broken-link check.
**Unity Editor required:** no.
**Suggested Codex prompt title:** “P4: Classify Ember docs into current, reference, proof, and archive.”

#### P5-A — Visual/runtime polish only after proof

**Goal:** improve visuals after asset, scene, save, and simulation proof exist.
**Files likely touched:** art/materials/scenes/UI styles.
**Files not to touch:** core simulation and save architecture unless explicitly scoped.
**Risks:** visual polish can mask systemic defects.
**Acceptance criteria:**

* LFS assets resolved.
* Scene screenshots reviewed.
* Any generated/fallback/static asset source is labelled.

**Suggested validation:** full scene screenshot proof and build.
**Unity Editor required:** yes.
**Suggested Codex prompt title:** “P5: Visual polish with generated/static asset provenance proof.”

### 6. Refactor targets

#### LOC / complexity thresholds

**Above 1000 LOC**

| File                                                               |  LOC | Acceptable? | Split direction                                                  |
| ------------------------------------------------------------------ | ---: | ----------- | ---------------------------------------------------------------- |
| `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` | 1033 | No          | Split by spell/effect family; keep one golden integration suite. |

**Above 800 LOC**

No current non-test source file above 800 LOC in this upload. Earlier 800+ LOC source files were split, but responsibility remains spread across partials.

**Above 500 LOC**

| File                                                                                           | LOC | Acceptable?   | Split direction                                                    |
| ---------------------------------------------------------------------------------------------- | --: | ------------- | ------------------------------------------------------------------ |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.cs`           | 660 | No            | Wizard state, validation, generation coordination, transition.     |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.Rendering.cs` | 571 | No            | View renderer, style/theme helpers, preview/portrait rendering.    |
| `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs`                                | 566 | No            | Composition root, lifecycle, input binding, UI binding, tick host. |
| `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`                                    | 554 | Borderline/no | Split by discovery, eligibility, reservation, assignment cases.    |
| `Assets/Scripts/Presentation/Ember/UI/EmberHud.cs`                                             | 538 | No            | Bars, action strip, message log, status/clock, presenter.          |
| `Assets/Scripts/Data/Save/WorldSaveData.cs`                                                    | 527 | No            | Save DTO modules by subsystem.                                     |
| `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`                                             | 517 | No            | Config, local provider, cloud provider, transport, JSON parser.    |

**Above 300 LOC**

| File                                                                             | LOC | Acceptable?                         | Split direction                                                |
| -------------------------------------------------------------------------------- | --: | ----------------------------------- | -------------------------------------------------------------- |
| `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs`          | 471 | No as aggregate                     | Split with existing partials into real services.               |
| `Assets/Scripts/Simulation/Worldgen/WorldgenService.Phases.cs`                   | 422 | Borderline/no                       | Deterministic phases and projection modules.                   |
| `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`                       | 413 | Borderline/no                       | Discovery/scoring/reservation.                                 |
| `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs`                      | 385 | Borderline                          | Split if adding more cases.                                    |
| `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.cs` | 381 | No as aggregate                     | Move worldgen bridge out of adapter.                           |
| `Assets/Scripts/Simulation/Process/JobAssignmentSystem.Tick.cs`                  | 378 | Borderline                          | Tick policy separate from assignment core.                     |
| `Assets/Scripts/Ui/Backends/UiToolkit/UiToolkitPanel.Frames.cs`                  | 374 | No                                  | Per-screen/panel renderers.                                    |
| `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`                     | 373 | No                                  | Runtime save controller, repository presenter, scene restore.  |
| `Assets/Scripts/Presentation/Ember/Loading/LoadingScreenController.cs`           | 369 | No                                  | Loading state, view, generation progress coordinator.          |
| `Assets/Tests/EditMode/Magic/ShieldBuffServiceRegistryBatchAbsorptionTests.cs`   | 351 | Borderline                          | Consolidate matrix cases.                                      |
| `Assets/Scripts/Presentation/Ember/UI/EmberMainMenuUI.cs`                        | 345 | No                                  | Menu presenter, save-slot presenter, generation entry.         |
| `Assets/Scripts/Presentation/Ember/UI/DialogBoxPanel.cs`                         | 335 | No                                  | Dialog view, topic list, modal input.                          |
| `Assets/Tests/EditMode/Audit/AuditSeventhPassCoverageTests.cs`                   | 325 | Borderline                          | Keep audit tests small and targeted.                           |
| `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs`                              | 320 | Borderline/no                       | Provider implementation, fallback/provenance, tensor pipeline. |
| `Assets/Tests/EditMode/Magic/SpellTargetValidatorTests.cs`                       | 315 | Borderline                          | Split only if growing.                                         |
| `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs`       | 315 | Borderline                          | Keep as acceptance if stable.                                  |
| `Assets/Scripts/Simulation/Forge/ClipBpeTokenizer.cs`                            | 310 | Acceptable if generated/algorithmic | Avoid editing unless tests exist.                              |
| `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.cs`   | 310 | No as aggregate                     | Move combat command adapter out.                               |
| `Assets/Tests/EditMode/Audit/AuditCoverageGapsTests.cs`                          | 308 | Borderline                          | Avoid growing audit-test bloat.                                |
| `Assets/Tests/EditMode/World/ActorStoreTests.cs`                                 | 306 | Borderline                          | Split by actor-store behaviour if expanding.                   |

#### Top 20 refactor targets

| Rank | File/class                                 | Current responsibility                                                                            | Why too large or misplaced                                             | Proposed split                                                                                | Safe migration strategy                                                   | Test/proof required                      |
| ---- | ------------------------------------------ | ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | ---------------------------------------- |
| 1    | `DomainSimulationAdapter*.cs`              | Presentation bridge for world, tick, read models, commands, combat, save, dialog, fate, worldgen. | Partial split reduced LOC but not responsibility count.                | Tick bridge, read-model projector, command router, dialog adapter, fate adapter, save bridge. | Characterize public adapter behaviour, extract one responsibility per PR. | EditMode adapter tests and scene smoke.  |
| 2    | `EmberWorldHost.cs`                        | Scene composition, locators, input, UI binding, tick lifecycle.                                   | Runtime god host.                                                      | Composition root, lifecycle host, input binder, UI binder.                                    | Extract non-MonoBehaviour helpers first.                                  | Boot/gameplay scene smoke.               |
| 3    | `JsonSliceSaveService.cs`                  | Save bridge plus process stores.                                                                  | Presentation owns authoritative process state.                         | Pure save service, process-state owner, mapping adapter.                                      | Add save roundtrip tests before moving state.                             | Process/farming save-load proof.         |
| 4    | `EmberSaveService.cs`                      | Save hotkeys, file repo, PlayerPrefs compatibility, scene restore.                                | UI/runtime/persistence mixed.                                          | Save controller, save repository adapter, scene restore service, status presenter.            | Characterize current behaviour first.                                     | Save/load/menu tests.                    |
| 5    | `WorldSaveData.cs`                         | Entire save DTO tree.                                                                             | Too broad; legacy fields can expand.                                   | DTO files/modules by subsystem.                                                               | File split only first, no field rename.                                   | Golden JSON compatibility.               |
| 6    | `WorldSaveMapper*.cs`                      | World save mapping across subsystems.                                                             | Mapper split exists but remains central migration risk.                | Sub-mappers for actors/items/process/worldgen/dialog/magic.                                   | Golden old/new save tests.                                                | Roundtrip and migration tests.           |
| 7    | `CharacterCreationController.cs`           | Wizard state, generation intent, validation, transition.                                          | Character creation is product-critical but overcoupled.                | State machine, presenter, generation coordinator, transition service.                         | Preserve serialized scene refs; extract pure state first.                 | Character creation PlayMode/screenshots. |
| 8    | `CharacterCreationController.Rendering.cs` | Procedural creation UI rendering.                                                                 | Rendering details dominate controller partial.                         | View renderer, style helpers, portrait/progress widgets.                                      | No behaviour change extraction.                                           | Screenshot comparison.                   |
| 9    | `EmberHud.cs`                              | Procedural HUD/action bar/message/status UI.                                                      | CRPG UI needs proof; file is too large.                                | Bars, action bar, message log, status/clock presenters.                                       | Wait for PRD source map, then split.                                      | Scene screenshots.                       |
| 10   | `DialogBoxPanel.cs`                        | Dialog UI, topic list, input.                                                                     | Dialog is core Ember interface; too much modal behaviour in one panel. | Dialog view, topic list component, input adapter.                                             | Add interaction tests first.                                              | Tavern and all-scene dialog proof.       |
| 11   | `EmberMainMenuUI.cs`                       | Main menu UI, continue/load, new game path.                                                       | Menu still touches legacy PlayerPrefs load path.                       | Menu presenter, save-slot presenter, new-game coordinator.                                    | Add menu save tests.                                                      | Continue/load proof.                     |
| 12   | `LoadingScreenController.cs`               | Loading state/progress/generation UI.                                                             | Loading/generation/progress policy mixed.                              | Loading state machine, progress view, generation coordinator.                                 | Preserve scene flow; add tests.                                           | New Game loading proof.                  |
| 13   | `LlmClients.cs`                            | Local/cloud LLM provider config and transport.                                                    | Too much provider infrastructure in one file.                          | Config, local provider, cloud provider, HTTP transport, parser.                               | Add fake provider tests first.                                            | No-network default tests.                |
| 14   | `NativeLlmClient.cs`                       | LLamaSharp native client, model download/readiness.                                               | Provider readiness and downloader are high-risk.                       | Model validator, downloader, native runtime client.                                           | Add pointer/hash tests before changing load.                              | Real LFS model runtime proof.            |
| 15   | `ModelBootstrap.cs`                        | Model verification/download/provider setup.                                                       | Startup model policy mixed with provider construction.                 | Model locator, verifier, downloader, provider factory.                                        | Normalize manifest/hashes first.                                          | Manifest and startup tests.              |
| 16   | `OnnxAssetForge.cs`                        | ONNX diffusion provider and fallback.                                                             | AI provider implementation in Simulation namespace.                    | Provider impl, fallback generator, provenance reporter.                                       | Add source/fallback provenance tests.                                     | Generated asset proof.                   |
| 17   | `ComfyUiAssetForge.cs`                     | Network forge provider.                                                                           | Blocking network provider in Simulation area.                          | Infrastructure provider with async availability.                                              | Keep opt-in; fake endpoint tests.                                         | Default no-network proof.                |
| 18   | `WorldgenService*.cs`                      | Procedural world generation phases.                                                               | Core to Ember; projection gap remains.                                 | Phases, validators, projection to actors/scenes.                                              | Add same-seed digest.                                                     | Deterministic worldgen proof.            |
| 19   | `JobAssignmentSystem*.cs`                  | Job discovery/assignment/tick logic.                                                              | Important but not clearly composed into main tick.                     | Discovery, scoring, reservation, tick integration.                                            | Move state ownership first.                                               | Job tick/save proof.                     |
| 20   | `UiToolkitPanel*.cs`                       | UI Toolkit backend frames/panels.                                                                 | Backend code grows into UI god renderer.                               | Per-screen renderers/templates.                                                               | Extract one panel at a time.                                              | UI screenshot proof.                     |

### 7. Unity-specific fix plan

1. **Do not rename MonoBehaviours yet.** Dialog, scene host, save, and interaction components are serialized in scenes. Renaming files/classes can break scene refs.
2. **Resolve orphan `.meta` files first.** The current duplicate GUID state is clean, but orphan metas remain. Use Unity Editor or a Unity-aware cleanup PR; do not hand-delete blindly if a matching asset is intentionally ignored.
3. **Never move Unity assets without their `.meta`.** For scripts, materials, scenes, prefabs, fonts, textures, plugins, and resources, move asset plus `.meta` together. Prefer Unity Editor for scenes, prefabs, materials, fonts, textures, and plugins.
4. **Scripts can be physically moved only if GUIDs and class names stay stable.** Moving `.cs` plus `.meta` is usually safe; renaming a `MonoBehaviour` file/class is not safe without scene/prefab reference checks.
5. **Scene edits must be Editor-driven.** Adding `DialogBoxPanel` to scenes, changing portal targets, assigning actor IDs, and validating EventSystems/cameras should be done in Unity Editor or via audited Editor scripts.
6. **Do not patch scene YAML manually unless the change is tiny and verified.** Static YAML cannot prove object hierarchy, prefab overrides, missing built-in/package refs, or serialized field migrations.
7. **Model/plugin/art LFS assets must be resolved before runtime proof.** Opening source-only pointer files in Unity is not equivalent to testing the game.
8. **Plugin import settings require Unity inspection.** Native DLLs and managed DLLs in `Assets/Plugins/**` need platform constraints/import settings confirmed in Inspector.
9. **Resources cleanup should be staged.** Replace `Resources` loads with explicit references/registries only after current scenes and UI styles are documented.
10. **Scene reference checks required for:** actor ID fields, dialog panel additions, portal target registry migration, UI host changes, save/load scene restore, generated material assignment.
11. **Manual Play Mode required for:** movement, camera, collisions, portals, Ask About, Ask Fate/Ask DM, pause, save/load, combat, character creation, and loading/generation flow.
12. **Screenshot proof is required for visual/UI changes.** A passing test without screenshots is not enough for Ember’s CRPG readability standard.

### 8. Docs cleanup plan

#### Canonical docs to keep current

* `README.md` — stable entry point only.
* `docs/CURRENT_STATE.md` — should be rewritten to match this repo state.
* `docs/REMEDIATION_V2_COUNTER.md` — current remediation tracker, but must be reconciled against actual files.
* `docs/EMBER_VISION_BIBLE.md` — vision canon.
* `docs/EMBER_VISION_NOTES_MAMI.md` — author intent and phase guardrails.
* `docs/AI_STACK.md` — AI policy, after contradiction cleanup.
* `docs/PRD_GOVERNANCE.md` — PRD routing, after path correction.
* `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` — PRD index.
* `docs/prds/PRD_overland_map_v1.md`
* `docs/prds/PRD_visual_architecture_3d_billboard_v1.md`
* `docs/mechanics/MASTER_MECHANICS_BIBLE.md`
* `docs/mechanics/ARCHITECTURE.md`

#### Docs to archive

* `docs/AUDIT_COUNTER.md` — overclaims all defects closed.
* `docs/Codex_audit.md` — stale audit snapshot.
* `docs/AUDIT_INDEPENDENT_2026-05-30.md` — useful but partially stale; archive as historical audit input.
* Old proof docs under `docs/proofs/**` unless each is labelled reproducible with source-only or LFS-resolved runtime assets.
* Old roadmap sections mentioning Qwen3/Copilot fallback or old save/PlayerPrefs-only state.
* Old agent/inspector docs if they reference missing sprint files or old phase ownership.

#### Docs to delete only after classification

* Any stale duplicate audit/counter document whose useful findings are already in `CURRENT_STATE.md` or remediation tracker.
* Any proof artifact that cannot be reproduced and is not needed as history.
* Any docs referencing absent paths after the active matrix is updated.

#### Duplicate/conflicting docs to merge or reconcile

* `docs/CURRENT_STATE.md` vs `docs/REMEDIATION_V2_COUNTER.md`
* `docs/AUDIT_COUNTER.md` vs actual open defects
* `docs/AI_STACK.md` vs `docs/proofs/llm-roundtrip-2026-05-30.md`
* `docs/PRD_GOVERNANCE.md` vs actual absence of `docs/reference/prd/`
* `Reference/PRDs/**` vs `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`
* Roadmap AI/save claims vs current AI/save code

#### One-page `docs/CURRENT_STATE.md` should contain

* Unity version: `6000.3.13f1`.
* Render pipeline: URP `17.3.0`.
* Build scenes list from `ProjectSettings/EditorBuildSettings.asset`.
* Current proof state split into:

  * source-only verified,
  * Unity import verified,
  * LFS-runtime verified,
  * PlayMode verified,
  * manually played.
* Current blockers:

  * missing `docs/EMBER_GOAL.md` replacement/source-map mismatch,
  * LFS pointer runtime assets,
  * orphan metas,
  * Simulation → SliceJson dependency,
  * Presentation-owned process stores,
  * scene dialog gaps,
  * string display-name interaction,
  * living-world tick composition gaps,
  * save/menu PlayerPrefs compatibility debt.
* Canonical doc links.
* What Codex must not touch.
* Exact validation commands.
* Last updated date and evidence basis.

### 9. Tests and validation plan

#### Quick static checks

Run or add these checks before any feature PR:

```bash
tools/validation/static-audit.sh
tools/validation/static-audit.sh --require-runtime
grep -RIn 'docs/EMBER_GOAL.md\|EMBER_GOAL' README.md docs Reference .github tools
grep -RIn 'Qwen3\|Copilot fallback\|PlayerPrefs.*save\|Reports/' README.md docs
grep -RIn 'UnityEngine.Input\|Input\.' Assets/Scripts/Presentation
grep -RIn 'PlayerPrefs' Assets/Scripts docs
grep -RIn 'GetAwaiter().GetResult()\|Task.Run' Assets/Scripts
grep -RIn '_targetSceneName\|LoadScene(' Assets/Scripts Assets/Scenes
```

Also check:

* duplicate `.meta` GUIDs,
* missing `.meta`,
* orphan `.meta`,
* LFS pointer files,
* `m_Script: {fileID: 0}` in scenes/prefabs,
* package manifest/lock mismatch,
* stale absent-path references.

#### Fallback C# checks

Use fallback only as partial source-level proof. It does not validate Unity import, packages, scenes, plugin import settings, LFS assets, or Play Mode.

```bash
tools/validation/run-validation.sh --mode fallback
```

Acceptance: output must explicitly say fallback is partial.

#### Unity EditMode tests

Required before merging code changes affecting Domain/Data/Simulation/Infrastructure/Presentation.

```bash
tools/validation/run-validation.sh --mode unity --unity-path <Unity executable>
```

Required additions:

* Simulation no longer depends on SliceJson.
* Model manifest rejects pointer files in runtime mode.
* Save/load golden fixtures.
* Actor ID dialog lookup test.
* World tick digest tests.

#### Unity PlayMode tests

Add/expand tests for:

* Boot → MainMenu → CharacterCreation.
* New Game loading flow with fake forge and with LFS runtime assets.
* Every build scene loads.
* Every scene with interactables opens dialog.
* Portal from each scene reaches correct target.
* Pause/menu input isolation.
* F5/F9 or equivalent save/load.
* Combat input and spell slot use.
* Ask Fate/Ask DM response and timeout handling.

#### Manual scene tour

For each scene:

1. Load scene from build order.
2. Confirm EventSystem/camera/player rig expected state.
3. Move player.
4. Interact with every nearby NPC/interactable.
5. Open dialog/Ask About where applicable.
6. Use portal/exit.
7. Save and load.
8. Confirm HUD readability.
9. Capture screenshot.
10. Record missing material/texture/pointer/fallback indicators.

Scenes:

* `Boot`
* `MainMenu`
* `CharacterCreation`
* `SmithingOverworld`
* `ColonyNeeds`
* `SeasonFarm`
* `TradeMarket`
* `CombatDungeon`
* `RitualHall`
* `TavernDialog`
* `OracleShrine`
* `ShowroomOverview`
* `TavernFlavour`

#### Screenshot proof

Required screenshot set:

* Boot or bootstrap transition.
* Main menu.
* Character creation first/mid/final stage.
* Loading/generation progress.
* Every gameplay scene with HUD visible.
* Dialog open in every scene with interactables.
* Ask Fate/Oracle result.
* Combat scene action/spell UI.
* Save/load status.

Each screenshot must label whether assets are real LFS-resolved, static authored, generated, or fallback.

#### Save/load proof

Required proof:

* New save in default slot.
* Restart and Continue from main menu.
* Clear/change PlayerPrefs and verify file-slot path still works after menu refactor.
* Corrupt save quarantined and reported.
* Save/load actor positions, inventories, topics, memory/faction state, plant/job/process state, time, and tool traces.
* Save/load mid-simulation and compare deterministic digest after continued ticks.
* Old legacy save fallback either migrates or is explicitly rejected.

#### LLM proof

Required proof states:

* Source-only mode: LLM disabled/fallback clearly labelled.
* LFS-resolved local mode: GGUF and native DLLs validated by size/hash/header.
* `USE_LLAMASHARP` compile path verified.
* Local Qwen response produced in-game.
* Provider label/model ID shown in proof log.
* No cloud call by default.
* Invalid LLM tool call rejected.
* Accepted tool call goes through validator/router.
* LLM text never directly mutates world state.

#### Deterministic replay proof

Required proof:

* Same world seed + same command log → same world digest.
* Save/load at tick N and continue → same digest as uninterrupted run.
* LFS/generated art cache does not affect authoritative world digest unless explicitly part of seed contract.
* Wall-clock time, HTTP response timing, Unity visual randomness, and LLM flavour text are excluded from authoritative sim state.

### 10. What Codex should NOT do

* Do not rewrite the whole project.
* Do not add gameplay features before cleanup/proof.
* Do not recreate `docs/EMBER_GOAL.md` as fiction; first decide the real successor.
* Do not trust `docs/AUDIT_COUNTER.md` saying everything is closed.
* Do not treat source-only CI as runtime proof.
* Do not move Unity assets without preserving `.meta`.
* Do not delete orphan metas without checking whether the asset is intentionally ignored or missing.
* Do not rename MonoBehaviour classes without scene/prefab reference checks.
* Do not edit scene YAML by hand except for tiny audited fixes.
* Do not let LLM output mutate simulation state without validator/router/tool calls.
* Do not make canned fallback text look like real local AI.
* Do not enable cloud/network AI by default.
* Do not let model downloads happen silently during normal play.
* Do not treat LFS pointer `.gguf`, `.onnx`, `.dll`, or `.png` files as valid runtime assets.
* Do not add another manager/helper/god class to avoid splitting existing classes.
* Do not expand `WorldSaveData` legacy fields without migration tests.
* Do not keep process/farming/job authority in Presentation.
* Do not continue using display names as actor identity.
* Do not “fix” scenes by deleting interaction content.
* Do not replace simulation gaps with visual-only scene dressing.
* Do not polish HUD/visuals before Ask About, save/load, scene flow, and LFS proof are stable.
* Do not import old Godot/backend reference JSON directly as active Unity data without a migration/import plan.
* Do not delete `Reference/PRDs` until the PRD matrix confirms each document’s status.

### 11. Final prioritized checklist

1. Create a docs-only PR reconciling `docs/CURRENT_STATE.md`, `docs/REMEDIATION_V2_COUNTER.md`, missing `docs/EMBER_GOAL.md`, and stale audit counters.
2. Update PRD governance to reflect actual folders: active `docs/prds`, matrix `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`, reference `Reference/PRDs`.
3. Add/strengthen validation that separates source-only checks from LFS-runtime checks.
4. Resolve or document the 13 orphan `.meta` files with Unity-safe verification.
5. Normalize package manifest/lock with Unity `6000.3.13f1`.
6. Make model manifest runtime mode reject pointer files and placeholder hashes.
7. Make native LLM readiness reject pointer GGUF/DLL assets and disable silent download by default.
8. Reconcile AI proof docs so they state exactly what was proven and under what LFS/runtime conditions.
9. Add PlayMode/editor scene-tour validation for all 13 build scenes without changing scenes yet.
10. Add explicit tests showing which scenes lack dialog surfaces for interactables.
11. Fix dialog panel availability in all gameplay scenes with interactables.
12. Replace display-name dialog lookup with stable actor/NPC IDs.
13. Validate and centralize portal target scene names through a registry/build-settings check.
14. Add save/load characterization tests for file slot, PlayerPrefs fallback, corrupt saves, and main menu Continue.
15. Refactor main menu Continue to use the file save repository/service instead of only legacy PlayerPrefs blob.
16. Add outer save-envelope schema/version tests.
17. Remove `EmberCrpg.Simulation` dependency on `EmberCrpg.Data.SliceJson`.
18. Move process/farming/job stores out of Presentation side stores toward authoritative world state.
19. Split `DomainSimulationAdapter` responsibility by responsibility, one no-behaviour-change PR at a time.
20. Split `EmberWorldHost` into composition/lifecycle/input/UI responsibilities.
21. Add deterministic world tick digest tests.
22. Wire one living-world subsystem into `WorldTickComposer` at a time: jobs, schedules, plants, prices/factions.
23. Add generated-world projection proof: generated NPCs become stable visible actors with schedules/memory.
24. Keep `EmberInput` facade but migrate implementation behind it to Input System actions.
25. Align HUD/dialog/clock/action bar to active PRDs with screenshot proof.
26. Split character creation controller/rendering after UI proof exists.
27. Split LLM provider classes and remove sync-over-async network calls.
28. Move forge provider implementations out of Simulation or clearly isolate them from deterministic sim.
29. Archive stale audit/proof docs and label remaining proof docs by reproducibility mode.
30. Only then allow new Ember gameplay feature work.

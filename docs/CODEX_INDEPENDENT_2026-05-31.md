# Ember Audit Summary

Static audit of the third uploaded zip only. I did **not** open Unity Editor, import the project, resolve Git LFS files, run batchmode Unity, run Play Mode, modify files, or inspect rendered screenshots. Static YAML can identify many scene/component risks, but it cannot prove camera feel, collider reachability, missing material imports, runtime-generated UI, plugin import settings, or actual playability.

## 1. Executive summary

* **Unity baseline verified:** `ProjectSettings/ProjectVersion.txt` declares Unity `6000.3.13f1`; `Packages/manifest.json` uses URP `17.3.0`.
* **The requested canonical read-order file `docs/EMBER_GOAL.md` is missing.** README now points to `docs/REMEDIATION_V2_COUNTER.md`, but the user-facing audit instructions still expect `EMBER_GOAL.md`.
* **There is no `docs/CURRENT_STATE.md` in this zip.** The live status appears to be `docs/REMEDIATION_V2_COUNTER.md`, but that file mixes current counter text, old audit tables, and stale copied findings.
* **The project’s identity is still Ember:** deterministic living-world CRPG, simulation authority, local LLM for flavour, faction/trade/weather/plant/job systems, and Daggerfall-style 3D billboard direction are present in docs and code.
* **The source-only zip cannot prove runtime AI, generated art, native plugins, or final visuals.** There are 908 Git LFS pointer files under `Assets`, including 883 PNGs, 13 DLLs, 10 ONNX files, 1 GGUF, and 1 PB weight file.
* **CI is intentionally source-only by default.** `.github/workflows/unity-test.yml` checks out with `lfs: false`; PlayMode and build jobs are optional and also use `lfs: false`.
* **Static asset hygiene improved but is not clean.** The static audit passes duplicate GUID and missing-meta checks, but reports 13 orphan `.meta` files.
* **The `Reports/` and `GeneratedAssets/` root folders are absent in this upload, but docs/gitignore still mention them.** This is not active source clutter anymore, but stale docs still describe old clutter.
* **`Assets/Generated/Core.meta` exists without `Assets/Generated/Core/`.** The `.gitignore` says the folder and folder meta should stay tracked while generated outputs are ignored. Current state violates that policy.
* **The LLM/AI stack is better separated than before but still not runtime-trustworthy.** Provider implementations moved into `Assets/Scripts/Infrastructure/AiDm`, but model/DLL readiness checks can accept LFS pointer files, manifest hashes are `TBD`, and real LLM proof is source-only-unverified.
* **There is a concrete MiniLM path bug risk.** `manifest.json` declares `all-minilm-l6-v2/model.onnx`, while `ModelBootstrap.ApplyLocator()` constructs `persistentDataPath/Models/minilm-l6-v2/model.onnx`.
* **Simulation authority is improved but still has boundary debt.** `Domain`, `Data`, and `Infrastructure` are mostly Unity-free; however `EmberCrpg.Simulation.asmdef` still references `EmberCrpg.Data.SliceJson`.
* **World ticking is no longer frozen.** `WorldTickComposer` now wires time, magic, needs, caravans, plant growth, job assignment, price update, and schedules. The remaining issue is not “no sim,” but thin/default composition and visual projection gaps.
* **Generated NPCs exist in simulation but are not fully visible in scenes.** `ActorView.cs` explicitly states scenes still author a fixed cast and need a runtime spawner to instantiate billboards for generated actors/NPC seeds.
* **Scene-authored actor identity is still weak.** Static scene scan shows no authored `_actorId:` or `_domainActorId:` values; interaction falls back to display names and domain actor keys.
* **Save/load is improved but remains prototype-shaped.** There is a file repository and corrupt-save quarantine, but the active UI still uses default slot `0`, mirrors to `PlayerPrefs`, nests domain JSON as a string, and main-menu Continue reads `PlayerPrefs` directly.
* **Input is centralized but still legacy.** Direct `UnityEngine.Input` usage is isolated to `EmberInput.cs`, but it still depends on the legacy Input Manager rather than Input System actions/rebinding.
* **The main adapter/host/UI classes remain high-risk.** `DomainSimulationAdapter*.cs`, `EmberWorldHost.cs`, `CharacterCreationController*.cs`, `EmberHud.cs`, `DialogBoxPanel.cs`, and save/menu/loading code still carry too many responsibilities.
* **Do not add new gameplay features yet.** First fix the source-of-truth docs, runtime/LFS validation, orphan metas/generated root policy, model manifest verification, save/menu flow, actor IDs, scene-tour proof, and assembly boundaries.
* **Do not move Unity assets or rename MonoBehaviours casually.** Scene references are serialized by GUID/type; Unity Editor inspection is required for scene, prefab, plugin, generated asset, and visual UI changes.

## 2. Ember soul alignment

### Preserved strengths

The repository still supports the intended Ember direction. `README.md` states the goal directly: deterministic living-world CRPG, one player with a colony-style background simulation, actor schedules, provenance-aware items, weather/season/plant/trade/faction systems, combat and magic on top of economy, and local Qwen/LLamaSharp only for flavour while simulation remains authoritative.

The assembly split mostly supports this: `EmberCrpg.Domain`, `EmberCrpg.Data`, `EmberCrpg.Data.SliceJson`, `EmberCrpg.Simulation`, and `EmberCrpg.Infrastructure` all use `noEngineReferences: true`; Unity-specific work is concentrated under Presentation/UI/Editor. LLM provider implementations have moved out of Simulation into Infrastructure, which is the right direction.

The main simulation loop is also materially better than a frozen prototype. `WorldTickComposer` includes time, needs, magic, caravan, plant growth, job assignment, price update, and schedule systems. `WorldState` now owns process stores such as `Plants`, `Soils`, `Jobs`, and `Worksites`.

### Dangerous drift

The project still risks becoming a **scene scaffold with systemic labels** instead of a living-world CRPG.

The biggest drift is between simulated/generated world state and visible scene state. `DomainSimulationAdapter.Worldgen.cs` hydrates generated NPCs into `WorldState.Actors`, but `ActorView.cs` explicitly says existing scenes still author a fixed cast and the full generated population needs a runtime billboard spawner. That means worldgen can be “real” in data while not being the player’s experienced world.

The second drift is identity/dialog continuity. Scenes do not author stable `_actorId` or `_domainActorId`; interactables still fall back to display names. Ember’s Ask About/memory/faction feel depends on speaking to an actor, not a string label like `Guard` or `Smith_A`.

The third drift is proof inflation around AI and generation. Docs contain proof files, but the uploaded source zip contains LFS pointers for models, DLLs, and most art PNGs. A green source-only test run is not proof of real local Qwen, ONNX generation, or Daggerfall-like visuals.

The fourth drift is UI/control prototype shape. Runtime-created dialog/pause/HUD panels are pragmatic, but Ember needs a readable Fallout-style CRPG interface with stable Ask About, Ask DM, journal/character record/save-load flow, and accessibility. The current UI remains procedural and controller-heavy.

### Missing pillars

* A single current-state document that matches this exact repository state.
* Stable actor IDs authored or spawned in scenes.
* Runtime projection of generated actors/NPCs into visible billboards.
* Save/load UI with real slots, schema versioning, migration, and deterministic replay proof.
* Runtime validation that LFS model/DLL/art files are resolved and not pointer stubs.
* Real LLM proof under `USE_LLAMASHARP` with model/DLL bytes present.
* Ask About/Ask DM proof per actor, using actor memory/faction/topic context.
* Scene-tour proof for all 13 build scenes: movement, camera, collision, dialog, HUD, save/load, portals, screenshots.
* Generated-asset provenance proof: real generated vs fallback vs static authored.
* Explicit CRPG UI alignment against active PRDs.

### Systems pretending to exist but not fully wired

* **Generated living population:** generated NPCs exist in simulation; visible scene actor spawning remains incomplete.
* **Stable actor conversation:** API paths exist for `ActorId`; scene-authored data still does not use them.
* **Local LLM:** code paths exist; runtime proof is blocked by LFS pointer assets and weak file validation.
* **Generated art:** forge/loading flow exists; source-only checkout contains art/model pointers and no trustworthy runtime proof.
* **Save/load:** file slots and quarantine exist; menu/continue flow still reads the legacy `PlayerPrefs` blob.
* **CRPG UI:** HUD/dialog/action concepts exist; visual/readability proof still requires Editor/PlayMode screenshots.

### Generated/placeholder work hiding product problems

`Assets/Generated/Core` is treated as a stable output location in docs/code, but the folder is absent while `Assets/Generated/Core.meta` remains. `CoreAssetManifest` expects generated files under `Assets/Generated/Core/*.png`, `EmberMainMenuUI` loads generated textures from that path, and `.gitignore` says only generated outputs should be ignored. This is an unresolved source/cache policy issue, not a feature.

## 3. Canonical source map

| Area                   | Canonical source                                                                                                                                                          | Stale/archive/reference source                                                                                                 | Notes                                                                                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Project entry          | `README.md` lines 1–27                                                                                                                                                    | `README.md` historical section lines 31+                                                                                       | Top README points to remediation tracker; lower “Faz 0” layout/status is historical and stale.                                               |
| Live status            | `docs/REMEDIATION_V2_COUNTER.md`                                                                                                                                          | `docs/AUDIT_COUNTER.md`, `docs/Codex_audit.md`, `docs/AUDIT_INDEPENDENT_2026-05-30.md`, `docs/CODEX_INDEPENDENT_2026-05-30.md` | Remediation tracker is closest to live, but it contains old embedded findings and internal contradictions.                                   |
| Missing expected canon | None                                                                                                                                                                      | `docs/EMBER_GOAL.md`                                                                                                           | File is absent. User read order and repo reality disagree.                                                                                   |
| Vision                 | `docs/EMBER_VISION_BIBLE.md`, `docs/EMBER_VISION_NOTES_MAMI.md`                                                                                                           | Older roadmap/historical README status                                                                                         | Keep these as identity guardrails.                                                                                                           |
| Mechanics              | `docs/mechanics/MASTER_MECHANICS_BIBLE.md`, `docs/mechanics/ARCHITECTURE.md`, mechanics `faz-*` docs                                                                      | Old backend/Godot docs under `Reference`                                                                                       | Mechanics docs describe intent; code/tick composition proves implementation.                                                                 |
| AI stack               | `docs/AI_STACK.md`, `Assets/Scripts/Infrastructure/AiDm/**`, `Assets/StreamingAssets/Models/manifest.json`                                                                | `docs/proofs/llm-roundtrip-2026-05-30.md` unless reproducible with LFS                                                         | AI_STACK correctly says source-only real LLM is unverified, but also says GGUF is “real on disk,” which is false in the uploaded source zip. |
| Active Unity PRDs      | `docs/prds/PRD_overland_map_v1.md`, `docs/prds/PRD_visual_architecture_3d_billboard_v1.md`, `docs/PRD_living_world_soul_v1.md`, `docs/PRD_loading_asset_generation_v1.md` | `Reference/PRDs/**`                                                                                                            | `Reference/PRDs` is design intent/reference, not active Unity implementation spec.                                                           |
| PRD governance         | `docs/PRD_GOVERNANCE.md`                                                                                                                                                  | `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` old backend matrix                                                               | Governance is current; matrix still lists old `docs/prd/active` and `frp-backend/godot-client` paths.                                        |
| Runtime source         | `Assets/Scripts/Domain`, `Data`, `Simulation`, `Infrastructure`, `Presentation`, `Ui`                                                                                     | Old Slice-era comments and obsolete shims                                                                                      | `SliceSaveMapper.cs` no longer exists; save mapper debt moved to `WorldSaveMapper*` / `WorldSaveData.cs`.                                    |
| Domain authority       | `Assets/Scripts/Domain/**`, `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs`                                                                                  | `JsonSliceSaveService` name/design                                                                                             | World root now owns process stores; persistence naming and dependency direction still lag.                                                   |
| Persistence            | `Assets/Scripts/Data/Save/**`, `Assets/Scripts/Presentation/Ember/Save/**`                                                                                                | PlayerPrefs legacy path                                                                                                        | File repository exists; active UI still depends on PlayerPrefs compatibility.                                                                |
| UI/player flow         | `Assets/Scripts/Presentation/Ember/UI/**`, `Assets/Scripts/Ui/**`, `Reference/PRDs/PRD_frontend_*` as design intent                                                       | Runtime-created fallback UI as proof substitute                                                                                | UI must be judged by PlayMode screenshots, not just code existence.                                                                          |
| Scenes                 | `Assets/Scenes/Ember/**`, `ProjectSettings/EditorBuildSettings.asset`                                                                                                     | Any docs mentioning old root scenes                                                                                            | Current build list has 13 scenes, all under `Assets/Scenes/Ember`.                                                                           |
| Generated assets       | `Assets/Generated/Core` policy in `.gitignore` and generation code                                                                                                        | `GeneratedAssets/` root                                                                                                        | `GeneratedAssets/` is absent; `Assets/Generated/Core.meta` is orphan.                                                                        |
| Runtime models         | `Assets/StreamingAssets/Models/manifest.json`                                                                                                                             | Proof docs if LFS unresolved                                                                                                   | Manifest paths mostly match layout, but hashes are all `TBD` and source zip has pointers.                                                    |
| Plugins                | `Assets/Plugins/**`, `docs/DEPENDENCIES.md`                                                                                                                               | Local-only embedded package comments                                                                                           | Plugin DLLs are LFS pointers in this upload; import settings require Unity.                                                                  |
| Tests                  | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode`, `tools/validation/**`                                                                                                   | Fallback harness as full proof                                                                                                 | Fallback harness is explicitly partial; CI default does not prove runtime.                                                                   |
| Reports/proofs         | `docs/proofs/**`                                                                                                                                                          | Missing `Reports/` root                                                                                                        | Proof docs are evidence snapshots, not source truth.                                                                                         |
| Reference data         | `Reference/OldBackendData/**`, `Reference/PRDs/**`                                                                                                                        | Direct runtime source                                                                                                          | Keep reference-only unless an explicit importer/migration plan exists.                                                                       |
| Dev tooling            | `.claude/skills/**`, `tools/**`                                                                                                                                           | Product source                                                                                                                 | Classify as contributor tooling.                                                                                                             |
| Package baseline       | `Packages/manifest.json`, `Packages/packages-lock.json`                                                                                                                   | CI manifest-stripping comments                                                                                                 | Manifest/lock mismatch needs Unity resolution.                                                                                               |
| Repo hygiene           | `.gitignore`, `.gitattributes`, `docs/REPO_HYGIENE.md`                                                                                                                    | Stale generated/report references                                                                                              | Policies exist but current filesystem does not fully satisfy them.                                                                           |

## 4. Defect register

| ID       | Severity | Category                              | Path(s)                                                                                                                                                                                                             | Evidence                                                                                                                                                                    | Why it matters                                                                                                                                                   | Fix direction                                                                                                                  | Codex-safe?                                         | Validation                                                                                                              |
| -------- | -------- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| EMB3-001 | Critical | Docs hygiene / source truth           | `docs/EMBER_GOAL.md`                                                                                                                                                                                                | Requested read-order file is absent from the zip.                                                                                                                           | The expected canonical goal file does not exist, so future Codex passes may follow stale external instructions or fail early. Ember needs one live truth source. | Restore it as a pointer or formally replace it with `docs/REMEDIATION_V2_COUNTER.md` plus a concise `docs/CURRENT_STATE.md`.   | Yes, docs-only.                                     | File/link check. Editor: no.                                                                                            |
| EMB3-002 | Critical | Docs hygiene / current status         | `README.md`, `docs/REMEDIATION_V2_COUNTER.md`, `docs/AUDIT_COUNTER.md`                                                                                                                                              | README says remediation tracker is live; `AUDIT_COUNTER.md` says old audit 60/60 closed; remediation tracker embeds old audit rows and contradictory “done/remaining” text. | Codex needs unambiguous task order. Conflicting status docs cause duplicate fixes, reverted fixes, or ignored open issues.                                       | Create one current-state page and mark audit counters as live/reference/archive.                                               | Yes.                                                | Docs inventory and stale-claim grep. Editor: no.                                                                        |
| EMB3-003 | High     | README staleness                      | `README.md`                                                                                                                                                                                                         | Lines 31+ still describe “Historical status (2026-05-09), Faz 0,” planned persistence split, old repo layout.                                                               | Top of README is current; lower sections can mislead agents into old Faz-0 assumptions.                                                                          | Collapse old sections under “Historical” or move to archive; keep README stable and short.                                     | Yes.                                                | Grep for old Faz/status claims after cleanup. Editor: no.                                                               |
| EMB3-004 | High     | PRD governance conflict               | `docs/PRD_GOVERNANCE.md`, `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`, `Reference/PRDs/**`                                                                                                                        | Governance says active Unity PRDs are under `docs/prds`; matrix says “Active PRDs: 94” and lists old `frp-backend`/`godot-client` paths.                                    | Ember must translate old design intent to Unity 3D billboard reality, not treat Godot/backend PRDs as current implementation.                                    | Regenerate or annotate matrix so active Unity/current/reference/deprecated states are explicit.                                | Yes.                                                | Broken-path and PRD-status check. Editor: no.                                                                           |
| EMB3-005 | Critical | LFS/runtime proof                     | `Assets/Art/**`, `Assets/Plugins/**`, `Assets/StreamingAssets/Models/**`, `.gitattributes`                                                                                                                          | Broad scan finds 908 LFS pointer files: 883 PNGs, 13 DLLs, 10 ONNX, 1 GGUF, 1 PB.                                                                                           | Source-only checkout cannot prove visuals, native LLM, ONNX forge, or final build. This directly affects Ember’s generated-world/art/LLM identity.               | Separate source-only validation from runtime-LFS validation; require real bytes for build, screenshot, LLM, and forge proof.   | Yes for validation/docs; no for obtaining binaries. | Pointer scan plus LFS-resolved Unity import/build. Editor: yes for runtime proof.                                       |
| EMB3-006 | Critical | CI false-green risk                   | `.github/workflows/unity-test.yml`                                                                                                                                                                                  | Checkout uses `lfs: false` for static, EditMode, optional PlayMode, and optional build jobs.                                                                                | CI green can hide missing DLL/model/art bytes. Codex may think the game is playable when only source compiles.                                                   | Add a runtime-LFS workflow or required manual gate; label current CI as source-only.                                           | Yes.                                                | CI matrix: source-only, runtime-LFS, PlayMode, build. Editor/Unity: yes.                                                |
| EMB3-007 | High     | Static validation gap                 | `tools/validation/static-audit.sh`                                                                                                                                                                                  | Runtime pointer check scans only `Assets/Plugins` and `Assets/StreamingAssets`; art PNG pointers are not flagged. Orphan metas warn only.                                   | Visual proof can be invalid while static audit passes. Ember’s billboard/world art depends on actual images, not pointer files.                                  | Add optional/full art pointer scan; decide which warnings fail PRs.                                                            | Yes.                                                | `static-audit.sh --require-runtime --include-art` or equivalent. Editor: no.                                            |
| EMB3-008 | High     | Unity `.meta` integrity               | `Assets/AI Toolkit.meta`, `Assets/Art/Portraits.meta`, `Assets/Art/UI/*.meta`, `Assets/Audio.meta`, `Assets/Editor/Ember/Patches.meta`, `Assets/Generated/Core.meta`, `Assets/Scripts/Presentation/Ember/AiDm.meta` | Static audit reports 13 orphan `.meta` files.                                                                                                                               | Orphan metas confuse Unity asset ownership and can mask deleted/generated/reference assets.                                                                      | Restore corresponding folders/assets intentionally or remove metas through Unity-safe cleanup.                                 | Partial.                                            | Static audit zero orphan metas or documented exceptions. Editor: yes.                                                   |
| EMB3-009 | Medium   | Generated asset policy                | `.gitignore`, `Assets/Generated/Core.meta`, `Assets/Scripts/Simulation/Generation/CoreAssetManifest.cs`, `EmberMainMenuUI.cs`                                                                                       | `.gitignore` says keep `Assets/Generated/Core/` folder + meta tracked; folder absent, meta present; code expects `Assets/Generated/Core/*.png`.                             | Generated assets must be canonical/cache-separated. Current state is neither clean source nor reliable runtime output location.                                  | Track empty folder via `.gitkeep`/Unity folder asset or move generated cache to persistent path consistently.                  | Partial.                                            | Clean clone path check and Unity import. Editor: yes.                                                                   |
| EMB3-010 | High     | Model verification                    | `Assets/StreamingAssets/Models/manifest.json`, `Assets/Scripts/Simulation/Forge/ModelManifest.cs`                                                                                                                   | All manifest `sha256` values are `TBD`; `VerifyAllPresent()` skips hash verification for placeholders and does not enforce size.                                            | LFS pointer files or corrupted models can be accepted as present. This makes AI/forge proof untrustworthy.                                                       | Add runtime mode that verifies size/hash/header and rejects LFS pointer text.                                                  | Yes.                                                | Unit test: pointer fixture rejected; real model accepted. Editor: no.                                                   |
| EMB3-011 | High     | AI/model path bug                     | `Assets/StreamingAssets/Models/manifest.json`, `Assets/Scripts/Presentation/Ember/Forge/ModelBootstrap.cs`                                                                                                          | Manifest path is `all-minilm-l6-v2/model.onnx`; `ApplyLocator()` uses `Path.Combine(_persistentRoot, "minilm-l6-v2")`.                                                      | Embeddings may silently point to the wrong directory even after model bootstrap succeeds. Ask About/semantic memory may degrade or fail.                         | Use manifest entry resolution for MiniLM paths instead of hardcoded directory.                                                 | Yes.                                                | Unit test `ModelBootstrap` resolves MiniLM from manifest. Editor: no.                                                   |
| EMB3-012 | High     | Native LLM readiness                  | `Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs`, `Assets/StreamingAssets/Models/qwen2.5-1.5b-instruct-q4_k_m.gguf`                                                                                          | `IsAvailable => File.Exists(_modelPath)`; source zip GGUF is an LFS pointer file.                                                                                           | Native Qwen can appear available but fail at load. Ember must not present fallback/corrupt model state as real AI.                                               | Validate size/hash/header before `IsAvailable` returns true.                                                                   | Yes.                                                | Pointer GGUF rejected; LFS-resolved GGUF accepted. Editor: yes for runtime proof.                                       |
| EMB3-013 | Medium   | AI docs conflict                      | `docs/AI_STACK.md`, `docs/proofs/llm-roundtrip-2026-05-30.md`                                                                                                                                                       | AI_STACK says source-only real LLM is unverified, but also says model is “~986 MB, real on disk”; proof docs may imply real roundtrip.                                      | AI claims must be honest. Ember’s LLM is flavour-only but should not be fake when claimed.                                                                       | Split docs into source-only, LFS-resolved, verified-runtime proof states.                                                      | Yes.                                                | Docs grep plus runtime proof log/screenshot. Editor: yes.                                                               |
| EMB3-014 | Medium   | Network/download policy               | `Assets/Scripts/Infrastructure/AiDm/NativeLlmClient.cs`                                                                                                                                                             | `EnsureModelReady()` can `HttpClient.GetAsync(_downloadUrl)` without visible opt-in/cancellation equivalent to gameplay policy.                                             | Default Ember should be local/offline. Silent model downloads during loading are bad UX and make proof nondeterministic.                                         | Require explicit opt-in, timeout, cancellation, progress UI, and documented cache path.                                        | Yes.                                                | Default config denies network; fake slow download test. Editor: no.                                                     |
| EMB3-015 | Medium   | Blocking async / threading            | `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`, `NativeLlmClient.cs`, `ComfyUiAssetForge.cs`                                                                                                                    | Static audit reports 6 `.GetAwaiter().GetResult()` sites; local/cloud LLM and native stream drain use sync-over-async.                                                      | Blocking network/model calls can stall worker services and complicate cancellation.                                                                              | Convert provider layer to async/cancellable or isolate blocking calls behind bounded worker.                                   | Partial.                                            | Fake timeout/cancellation tests; PlayMode no-freeze proof. Editor: yes.                                                 |
| EMB3-016 | High     | asmdef boundary                       | `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef`                                                                                                                                                             | Simulation references `EmberCrpg.Data.SliceJson`.                                                                                                                           | Deterministic simulation should not depend on concrete JSON persistence. Persistence direction remains inverted.                                                 | Move save/rehydration composition to Data/Persistence/Presentation; keep Simulation behind pure contracts.                     | Partial.                                            | Unity assembly reload; Simulation asmdef no longer references SliceJson. Editor: yes.                                   |
| EMB3-017 | Medium   | Forge provider boundary               | `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs`, `ModelManifest.cs`, `Presentation/Ember/Forge/**`                                                                                                              | ONNX forge/provider logic and manifest verification live under Simulation namespace/assembly areas; provider code touches runtime model files.                              | Simulation should be headless/deterministic; provider implementations are infrastructure/runtime.                                                                | Keep pure manifest/contracts in Simulation; move provider implementations to Infrastructure/Presentation runtime assembly.     | Partial.                                            | Simulation compiles without provider I/O implementation. Editor: yes for asmdef.                                        |
| EMB3-018 | High     | Save/load architecture                | `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`, `SaveData.cs`                                                                                                                                         | `DefaultSlot = 0`; `PlayerPrefs` mirror; static `_pendingLoad`; envelope stores `domainStateJson` string plus scene/player transform.                                       | A living-world CRPG needs durable slots, schema migration, and replayable state, not a one-slot escaped-JSON envelope.                                           | Add typed save envelope, real slots, migration, and PlayerPrefs only as legacy pointer.                                        | Partial.                                            | Save slot, corrupt-save, migration, cross-scene restore tests. Editor: yes.                                             |
| EMB3-019 | High     | Main-menu load divergence             | `Assets/Scripts/Presentation/Ember/UI/EmberMainMenuUI.cs`, `EmberSaveService.cs`                                                                                                                                    | `LoadGame()` and `Continue()` read `PlayerPrefs.GetString("ember.save.v1")` directly, while runtime save prefers `FileSaveRepository`.                                      | Main menu can ignore canonical file-slot saves and preserve old one-slot assumptions.                                                                            | Route menu Continue/Load through `FileSaveRepository`/save service.                                                            | Yes.                                                | Save file exists with PlayerPrefs cleared; Continue still works. Editor: yes.                                           |
| EMB3-020 | Medium   | Scene validation scope                | `EmberSaveService.IsKnownBuildScene()`                                                                                                                                                                              | Editor validates against `EditorBuildSettings`; player build returns true.                                                                                                  | Invalid scene names in saves can reach runtime `LoadScene` in player builds.                                                                                     | Store canonical scene IDs and validate against a generated build-scene registry in all builds.                                 | Yes.                                                | Invalid scene save rejected in player-mode test. Editor: yes.                                                           |
| EMB3-021 | Medium   | Save schema size/drift                | `Assets/Scripts/Data/Save/WorldSaveData.cs`, `WorldSaveMapper*.cs`                                                                                                                                                  | `WorldSaveData.cs` is 527 LOC; save mapping spread across many files; legacy role fields remain.                                                                            | Schema accretion will make migration brittle. Ember’s long-lived worlds need stable saves.                                                                       | Split DTO modules by subsystem and add golden migration fixtures.                                                              | Yes, staged.                                        | Golden old/new save roundtrip. Editor: no.                                                                              |
| EMB3-022 | Medium   | Persistence naming drift              | `Assets/Scripts/Presentation/Ember/Save/JsonSliceSaveService.cs`                                                                                                                                                    | File/class still says `Slice`, but comments and code use world-root stores and `WorldSaveData`.                                                                             | Naming hides actual responsibility and encourages old slice-era assumptions.                                                                                     | Rename only after reference scan, or wrap in new persistence service name while preserving GUID/class refs.                    | Partial.                                            | Compile and scene/prefab ref check. Editor: yes if MonoBehaviour/serialized refs involved; this class itself is not MB. |
| EMB3-023 | High     | Adapter god object                    | `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter*.cs`                                                                                                                                            | Main file 557 LOC plus partials for combat, save, fate, worldgen; owns world, save bridge, tick composer, HUD rows, dialog, LLM, worldgen.                                  | Partial classes reduced file size but not responsibility. Codex changes here remain high-risk.                                                                   | Extract tick bridge, read-model projector, command router, dialog adapter, fate adapter, save bridge.                          | Partial, staged only.                               | Characterization tests and scene smoke. Editor: yes for smoke.                                                          |
| EMB3-024 | Medium   | Async result pump                     | `DomainSimulationAdapter.cs`, `DomainSimulationAdapter.Fate.cs`                                                                                                                                                     | LLM/fate results enqueue `_mainThreadApply`, drained only in `AdvanceTick()`.                                                                                               | If tick is paused/stalled/modal-tied, “thinking” states and LLM results can hang.                                                                                | Add explicit main-thread pump independent of simulation tick or prove tick always runs during modals.                          | Partial.                                            | Pause/dialog/fate PlayMode test; no stuck thinking state. Editor: yes.                                                  |
| EMB3-025 | Medium   | Dialog identity still name-based      | `DomainSimulationAdapter.cs`                                                                                                                                                                                        | `SelectTopic()` and NPC lookup still use `_activeDialogActor` name to find actor/NpcSeed.                                                                                   | Even with `ActorId` overloads, conversation state can regress to name uniqueness assumptions.                                                                    | Store active `ActorId`/NpcId in `ConversationState`; use names only for display.                                               | Yes.                                                | Two NPCs with same display name choose separate topics/memory. Editor: no for core; yes for scene proof.                |
| EMB3-026 | Critical | Scene actor identity not authored     | `Assets/Scenes/Ember/*.unity`, `EmberInteractable.cs`, `ActorView.cs`                                                                                                                                               | Static scan finds no `_actorId:` or `_domainActorId:` serialized values; interactables/views rely on names/keys.                                                            | Actor memory, schedules, faction context, and provenance require stable actor identity. This is central Ember, not polish.                                       | Migrate scenes/spawner to set stable actor IDs.                                                                                | Partial; scene work required.                       | Scene validation: every interactable/ActorView has stable ID or documented non-actor reason. Editor: yes.               |
| EMB3-027 | High     | Generated NPCs not fully visible      | `ActorView.cs`, `DomainSimulationAdapter.Worldgen.cs`, scenes                                                                                                                                                       | `ActorView.cs` lines 24–30 says scenes still author fixed cast and full generated population needs spawner.                                                                 | Worldgen is only truly canonical if generated actors become visible, schedulable, speakable world actors.                                                        | Add runtime billboard spawner/prefab after stable actor ID policy.                                                             | No for blind implementation; yes after design.      | Same seed shows same generated actors in scene screenshots. Editor: yes.                                                |
| EMB3-028 | Medium   | Runtime-created UI hides scene issues | `EmberWorldHost.cs`, `Assets/Scenes/Ember/*.unity`                                                                                                                                                                  | Only 4 scenes serialize `DialogBoxPanel`; host creates missing dialog panel at runtime.                                                                                     | Runtime fallback is useful, but static scene health can look worse/better than actual play. It must be proven in PlayMode.                                       | Keep fallback but add scene-tour tests for actual interaction in all scenes.                                                   | Yes.                                                | Interact in every gameplay scene; dialog appears. Editor: yes.                                                          |
| EMB3-029 | Medium   | Portal raw strings                    | `EmberScenePortal.cs`, `Assets/Scenes/Ember/*.unity`, `EmberScenes.cs`                                                                                                                                              | Portal stores `_targetSceneName` string and calls `SceneManager.LoadScene(_targetSceneName)`; serialized scene names exist in 10 scenes.                                    | Hardcoded strings can break on scene rename and bypass registry validation.                                                                                      | Centralize scene IDs/validation; add editor validator for portal targets.                                                      | Yes.                                                | All portal targets match build settings; portal tour. Editor: yes.                                                      |
| EMB3-030 | Medium   | Build scene proof gap                 | `Assets/Scenes/Ember/**`, `Assets/Tests/PlayMode/**`                                                                                                                                                                | Build settings list all 13 scenes; PlayMode tests cover boot/creation/worldgen route, not full scene interaction/collision/save tour.                                       | Ember must be playable as a route, not just importable.                                                                                                          | Add PlayMode/manual scene-tour proof for all build scenes.                                                                     | Yes for tests; scene fixes require Editor.          | Full scene tour screenshots. Editor: yes.                                                                               |
| EMB3-031 | Medium   | Static scene inspection limit         | `Assets/Scenes/Ember/**`                                                                                                                                                                                            | Static grep found no `m_Script: {fileID: 0}`, but package/builtin refs/materials cannot be resolved from YAML alone.                                                        | Static YAML cannot prove no missing materials/scripts after package import.                                                                                      | Add Unity Editor validation that opens every scene and reports missing refs/materials/scripts.                                 | Yes.                                                | Editor validation report. Editor: yes.                                                                                  |
| EMB3-032 | Medium   | UI architecture                       | `EmberHud.cs`, `DialogBoxPanel.cs`, `EmberMainMenuUI.cs`, `LoadingScreenController.cs`, `UiToolkitPanel.Frames.cs`                                                                                                  | These files are 538, 369, 345, 369, and 374 LOC; host dynamically creates UI.                                                                                               | Ember’s CRPG readability needs stable designed screens, not more procedural panels.                                                                              | Split view/presenter/input/state; validate with screenshots against PRDs.                                                      | Partial.                                            | UI screenshot proof across scenes. Editor: yes.                                                                         |
| EMB3-033 | Medium   | UI foundation boundary                | `Assets/Scripts/Ui/Foundation/EmberCrpg.Ui.Foundation.asmdef`                                                                                                                                                       | `noEngineReferences: false`; foundation loads/uses Unity UI resources.                                                                                                      | “Foundation” is not backend-neutral. This complicates testability and future UI backend work.                                                                    | Rename boundary honestly or split pure UI model from Unity-specific tokens/resources.                                          | Yes, staged.                                        | asmdef dependency check and UI tests. Editor: yes.                                                                      |
| EMB3-034 | Medium   | Resources dependency                  | `Assets/Resources/**`, `LoadingScreenController.cs`, `EmberLoadingScreen.cs`, `UiToolkitPanel.Frames.cs`, `UiToolkitSurface.cs`                                                                                     | Code uses `Resources.Load` for loading textures/flavours/fonts/theme and builtin font.                                                                                      | `Resources` hides dependencies, bloats builds, and makes asset ownership implicit.                                                                               | Keep tiny global resources only; move UI/theme/fonts to explicit references/registry.                                          | Partial.                                            | `Resources.Load` inventory and build size/import proof. Editor: yes.                                                    |
| EMB3-035 | Medium   | Input migration                       | `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`                                                                                                                                                            | All 25 legacy `Input.` sites are centralized in this facade, still using legacy Input Manager.                                                                              | Centralization is good, but no rebinding/accessibility/action maps.                                                                                              | Implement Input System behind the facade without changing callers.                                                             | Yes.                                                | Direct `Input.` remains only in facade; manual controls pass. Editor: yes.                                              |
| EMB3-036 | Medium   | Placeholder fallback risk             | `EmberWorldHost.cs`, `PlaceholderSimulationAdapter.cs`                                                                                                                                                              | Host falls back to `PlaceholderSimulationAdapter` if domain adapter bootstrap fails.                                                                                        | Useful for UI sandbox, dangerous if real gameplay silently degrades into fake data.                                                                              | Fail loudly in gameplay scenes or gate placeholder only for explicit UI sandbox scenes.                                        | Yes.                                                | Force adapter failure; gameplay scene reports blocking error, sandbox still works. Editor: yes.                         |
| EMB3-037 | Medium   | World tick thin defaults              | `WorldTickComposer.cs`                                                                                                                                                                                              | Composer uses hardcoded one-crop wheat catalog, local scarcity constants, default calendar, schedule/job cadence.                                                           | Living-world systems are wired but still thin/prototype. Expanding features without digest tests risks nondeterminism.                                           | Add data-driven catalogs and same-seed tick digest tests before expanding.                                                     | Yes.                                                | Deterministic digest covers plant/job/price/schedule. Editor: no for core.                                              |
| EMB3-038 | Medium   | Stale inline comment                  | `WorldTickComposer.cs`                                                                                                                                                                                              | Header comment says plant growth/faction/caravan motion will land later, while fields already include plant/job/price/schedule.                                             | Internal docs mislead Codex during future refactors.                                                                                                             | Update comments to match actual composition and remaining gaps.                                                                | Yes.                                                | Comment/code consistency review. Editor: no.                                                                            |
| EMB3-039 | Medium   | Fixed seed world remains              | `WorldFactory.cs`, `DomainSimulationAdapter.Worldgen.cs`                                                                                                                                                            | Base world still creates fixed slice actors/sites; worldgen hydration adds generated data later.                                                                            | Fine as bootstrap, but it can mask whether generated playthroughs actually control visible gameplay.                                                             | Clearly separate fallback/tutorial seed world from generated playthrough world.                                                | Partial.                                            | New Game digest and visible actor list prove generated world is active. Editor: yes.                                    |
| EMB3-040 | Medium   | Character creation complexity         | `CharacterCreationController.cs`, `CharacterCreationController.Rendering.cs`                                                                                                                                        | 660 + 571 LOC; handles wizard state, generation, validation, rendering, transition.                                                                                         | Character creation is core player intent; overcoupling makes Codex changes dangerous.                                                                            | Split state machine, presenter/view, generation coordinator, transition service.                                               | Yes, staged.                                        | Character creation PlayMode and screenshot regression. Editor: yes.                                                     |
| EMB3-041 | Medium   | Menu/loading/generation coupling      | `EmberMainMenuUI.cs`, `LoadingScreenController.cs`, `ModelBootstrap.cs`                                                                                                                                             | Main menu runs scenario asset top-up, loading UI, save continue, and scene transition.                                                                                      | Entry flow owns too much generation and persistence policy.                                                                                                      | Split menu presenter, save-slot presenter, generation/loading coordinator.                                                     | Partial.                                            | Main menu → New Game → loading → creation proof. Editor: yes.                                                           |
| EMB3-042 | Medium   | LLM provider class size               | `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`                                                                                                                                                                  | 517 LOC with config, local/cloud providers, HTTP transport, parsing.                                                                                                        | Provider risk should be isolated; cloud/local/default policy must remain explicit.                                                                               | Split config/local/cloud/transport/parser after tests.                                                                         | Yes.                                                | Fake provider tests; no network by default. Editor: no.                                                                 |
| EMB3-043 | Medium   | Plugin dependency hygiene             | `Assets/Plugins/**`, `Assets/Plugins/NuGet/**`, `docs/DEPENDENCIES.md`                                                                                                                                              | Native DLLs under `Assets/Plugins/x86_64`; many NuGet DLLs under Assets; source zip has pointer DLLs.                                                                       | Unity plugin import settings and platform filters can silently break builds.                                                                                     | Maintain plugin manifest with import settings and runtime/source-only states.                                                  | Partial.                                            | Unity plugin inspector/build proof. Editor: yes.                                                                        |
| EMB3-044 | Medium   | Package mismatch                      | `Packages/manifest.json`, `Packages/packages-lock.json`                                                                                                                                                             | Manifest requests `com.unity.test-framework: 1.4.6`; lock resolves `1.6.0`.                                                                                                 | Package restore may rewrite lock or vary by machine/CI.                                                                                                          | Resolve in Unity 6000.3.13f1 and commit normalized state.                                                                      | Partial.                                            | Clean package restore. Editor: yes.                                                                                     |
| EMB3-045 | Medium   | Test coverage shape                   | `Assets/Tests/EditMode/**`, `Assets/Tests/PlayMode/**`, CI                                                                                                                                                          | EditMode broad; PlayMode files focus on Boot, CharacterCreation, Loading, Worldgen route, not all gameplay scenes.                                                          | Backend tests do not prove player-visible CRPG flow.                                                                                                             | Add scene-tour PlayMode/manual proof; keep backend tests.                                                                      | Yes.                                                | PlayMode route covers every build scene. Editor: yes.                                                                   |
| EMB3-046 | Medium   | Test bloat                            | `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs`, job/magic tests                                                                                                                                 | One test file is 1033 LOC; several tests exceed 300 LOC.                                                                                                                    | Overgrown tests are hard for Codex to maintain and can crowd out product-visible tests.                                                                          | Split/consolidate by scenario; preserve golden coverage.                                                                       | Yes.                                                | Same assertions pass with smaller files. Editor: no.                                                                    |
| EMB3-047 | Medium   | Fallback validation limitation        | `tools/validation/run-validation.sh`, fallback harness                                                                                                                                                              | Script labels fallback as partial; it compiles selected source, not Unity import/scenes/plugins/PlayMode.                                                                   | Green fallback can hide broken assets/scenes/import settings.                                                                                                    | Keep fallback but require Unity validation for Presentation/asset/scene PRs.                                                   | Yes.                                                | Validation docs and CI gates. Editor: yes for full proof.                                                               |
| EMB3-048 | Low      | Security                              | `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`, `.gitignore`, docs                                                                                                                                              | No hardcoded API keys found; cloud client supports bearer config; `.gitignore` ignores env/key files.                                                                       | Baseline okay, but cloud/network providers must stay opt-in.                                                                                                     | Keep secret scan in CI; document env-only keys.                                                                                | Yes.                                                | Secret grep/CI secret scan. Editor: no.                                                                                 |
| EMB3-049 | Low      | Dev tooling placement                 | `.claude/skills/**`                                                                                                                                                                                                 | Local agent skills are tracked next to game source.                                                                                                                         | Useful contributor tooling but not product/runtime source.                                                                                                       | Classify in repo hygiene docs or move under `tools/agent`.                                                                     | Yes.                                                | Source map updated. Editor: no.                                                                                         |
| EMB3-050 | Low      | Reference data ambiguity              | `Reference/OldBackendData/**`, `Reference/PRDs/**`                                                                                                                                                                  | Large old backend JSON/PRD reference remains in repo.                                                                                                                       | Useful design/import source, but must not be mistaken for active Unity data.                                                                                     | Add clear reference-only README and importer rule.                                                                             | Yes.                                                | No runtime direct reads unless importer/migration exists. Editor: no.                                                   |
| EMB3-051 | Medium   | Docs/proof reproducibility            | `docs/proofs/**`, `docs/AI_STACK.md`, `.github/workflows/unity-test.yml`                                                                                                                                            | Proof screenshots/docs exist, but source zip lacks real LFS bytes and default CI is source-only.                                                                            | Proof must be reproducible. Ember cannot rely on historical screenshots as current runtime truth.                                                                | Tag each proof as source-only, LFS-runtime, Editor, manual, or obsolete.                                                       | Yes.                                                | Re-run proof or archive it. Editor: yes for visual proof.                                                               |
| EMB3-052 | Medium   | Generated art build path              | `docs/PRD_loading_asset_generation_v1.md`, `CoreAssetManifest.cs`, `SceneEnvironmentDresser.cs`, `EmberMainMenuUI.cs`                                                                                               | PRD notes build caveat: built player has no project `Assets/` folder; code still references `Assets/Generated/Core` in editor paths.                                        | Generated art must work in player builds, not just Editor project folders.                                                                                       | Standardize generation output to `Application.persistentDataPath/Generated/Core` for builds and editor mirror for development. | Partial.                                            | Build-mode generation/load proof. Editor: yes.                                                                          |
| EMB3-053 | Low      | TextMesh Pro samples                  | `Assets/TextMesh Pro/**`                                                                                                                                                                                            | TMP runtime resources present; Examples & Extras absent in this upload.                                                                                                     | Previous sample sprawl is not present; keep it that way.                                                                                                         | Do not reimport TMP Examples unless intentionally needed.                                                                      | Yes.                                                | Reference scan. Editor: yes if removing assets.                                                                         |
| EMB3-054 | Medium   | Scene/HUD inconsistency               | `Assets/Scenes/Ember/CombatDungeon.unity`, `EmberWorldHost.cs`                                                                                                                                                      | `EmberHud` serialized in 9 gameplay scenes, not CombatDungeon; host has runtime HUD/CombatHud handling.                                                                     | Runtime fallback may be fine, but combat scene UI must be visually proven.                                                                                       | Add scene-specific screenshot and UI component validation.                                                                     | Partial.                                            | CombatDungeon screenshot with HUD/action/dialog/save status. Editor: yes.                                               |
| EMB3-055 | Medium   | Obsolete role shims                   | `Assets/Scripts/Domain/World/WorldState.cs`                                                                                                                                                                         | `Player`, `Talker`, `Merchant`, `Guard`, `Enemy` role shims remain obsolete but present.                                                                                    | Old slice-era role assumptions conflict with generated multi-actor world.                                                                                        | Remove after call-site migration and save compatibility tests.                                                                 | Partial.                                            | Grep call sites to zero; save/load fixtures pass. Editor: no.                                                           |

## 5. Codex-ready work packages

### P0-A — Current-state and docs truth reset

**Goal:** make docs match this upload and remove ambiguity around `EMBER_GOAL.md`, remediation counters, and current proof state.

**Files likely touched:** `README.md`, `docs/REMEDIATION_V2_COUNTER.md`, new `docs/CURRENT_STATE.md`, `docs/AUDIT_COUNTER.md`, `docs/CODEX_INDEPENDENT_2026-05-30.md`, `docs/AUDIT_INDEPENDENT_2026-05-30.md`.

**Files not to touch:** `Assets/**`, `Packages/**`, scenes, models, plugins.

**Risks:** codifying stale audit claims as current truth.

**Acceptance criteria:**

* `docs/EMBER_GOAL.md` absence is explicitly resolved.
* `docs/CURRENT_STATE.md` exists and is one page.
* Old audit docs are marked historical/reference.
* Current state says source-only vs LFS-runtime vs Editor/PlayMode/manual proof.

**Suggested validation command / Unity verification:** grep for `EMBER_GOAL`, stale root-scene names, old `Reports/`, and false “all closed” claims.

**Unity Editor required:** no.

**Suggested Codex prompt title:** “P0: Rebuild Ember current-state docs from the actual repository.”

### P0-B — Runtime/LFS validation gate

**Goal:** prevent source-only tests from being mistaken for runtime proof.

**Files likely touched:** `tools/validation/static-audit.sh`, `.github/workflows/unity-test.yml`, `docs/validation.md`, `docs/AI_STACK.md`, `docs/REPO_HYGIENE.md`.

**Files not to touch:** actual `.png`, `.dll`, `.onnx`, `.gguf`, `.pb` files.

**Risks:** making default CI too expensive if runtime-LFS is required on every PR.

**Acceptance criteria:**

* Source-only mode clearly reports that art/LLM/forge/build are not proven.
* Runtime mode fails on pointer DLL/model/art files.
* CI exposes separate source-only and runtime-LFS jobs or documented manual gate.

**Suggested validation:** `tools/validation/static-audit.sh`; `tools/validation/static-audit.sh --require-runtime`; add art pointer scan.

**Unity Editor required:** no for scan; yes for runtime proof.

**Suggested Codex prompt title:** “P0: Add honest source-only vs runtime-LFS validation.”

### P0-C — Unity `.meta` and generated root cleanup

**Goal:** resolve orphan metas and the `Assets/Generated/Core` policy safely.

**Files likely touched:** orphan `.meta` files, `Assets/Generated/Core` folder policy, `.gitignore`, `docs/REPO_HYGIENE.md`.

**Files not to touch:** scene YAML, script class names, plugin/model binaries.

**Risks:** deleting metas that correspond to intentionally local/ignored assets.

**Acceptance criteria:**

* Static audit reports zero unapproved orphan metas.
* `Assets/Generated/Core` policy is either restored as an empty tracked Unity folder or moved to persistent runtime path.
* No duplicate GUIDs introduced.

**Suggested validation:** `tools/validation/static-audit.sh`, Unity import log.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P0: Resolve orphan Unity metas and generated output root policy.”

### P1-A — Model manifest and native LLM readiness hardening

**Goal:** make AI/model presence checks reject pointer/corrupt assets.

**Files likely touched:** `Assets/StreamingAssets/Models/manifest.json`, `ModelManifest.cs`, `ModelManifest` tests, `NativeLlmClient.cs`, `ModelBootstrap.cs`.

**Files not to touch:** model binaries.

**Risks:** breaking source-only development unless modes are explicit.

**Acceptance criteria:**

* Runtime mode enforces size/hash/header.
* All `TBD` hashes are forbidden in runtime proof mode.
* Native LLM `IsAvailable` rejects LFS pointer GGUF.
* MiniLM path resolves from manifest, not hardcoded `minilm-l6-v2`.

**Suggested validation:** unit tests using pointer fixtures; runtime LFS proof with real model.

**Unity Editor required:** yes for runtime LLM proof.

**Suggested Codex prompt title:** “P1: Make Ember AI models pointer-safe and manifest-driven.”

### P1-B — Save/load characterization before refactor

**Goal:** protect current behaviour before changing persistence.

**Files likely touched:** tests around `EmberSaveService`, `FileSaveRepository`, `SaveData`, `WorldSaveData`, `WorldSaveMapper*`, `EmberMainMenuUI`.

**Files not to touch:** save schema fields or scene YAML in this batch.

**Risks:** tests may expose current inconsistent behaviour; do not “fix” during characterization.

**Acceptance criteria:**

* Tests cover default slot file save, PlayerPrefs mirror/fallback, corrupt save quarantine, main menu Continue, cross-scene pending load, missing adapter, malformed domain JSON.
* Current limitations are documented.

**Suggested validation:** EditMode save tests plus manual save/load PlayMode proof.

**Unity Editor required:** yes for menu/scene proof.

**Suggested Codex prompt title:** “P1: Characterize Ember save/load and Continue before persistence changes.”

### P1-C — Scene/interaction proof harness

**Goal:** prove scene health before editing scenes.

**Files likely touched:** PlayMode tests, editor validation scripts, proof output docs.

**Files not to touch:** scene YAML in this batch.

**Risks:** shallow tests can pass while gameplay remains unusable.

**Acceptance criteria:**

* Every build scene loads.
* Each gameplay scene has player/camera/EventSystem/HUD/dialog path.
* Every portal target matches build settings.
* Every interactable either has stable actor ID or is reported.
* Screenshots are produced for human review.

**Suggested validation:** PlayMode scene tour plus screenshots.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P1: Add Ember build-scene interaction and portal validation.”

### P2-A — Main menu save-source unification

**Goal:** route menu Continue/Load through the file save repository, not direct PlayerPrefs blob.

**Files likely touched:** `EmberMainMenuUI.cs`, `EmberSaveService.cs`, save presenter/helper tests.

**Files not to touch:** save schema migration in same PR.

**Risks:** breaking legacy saves if fallback is removed too early.

**Acceptance criteria:**

* Continue loads file slot when PlayerPrefs blob is absent.
* Legacy PlayerPrefs blob still migrates or falls back cleanly.
* No direct menu parsing of canonical save payload except through save service/repository.

**Suggested validation:** EditMode + PlayMode menu Continue tests.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P2: Make MainMenu Continue use the canonical save repository.”

### P2-B — Assembly dependency cleanup

**Goal:** remove Simulation’s direct dependency on SliceJson persistence.

**Files likely touched:** asmdefs, save/rehydration composition code, tests.

**Files not to touch:** gameplay behaviour and save schema format in same PR.

**Risks:** assembly reload breakage.

**Acceptance criteria:**

* `EmberCrpg.Simulation.asmdef` no longer references `EmberCrpg.Data.SliceJson`.
* Simulation still compiles headless.
* Save/load tests still pass.

**Suggested validation:** Unity assembly compile, EditMode tests.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P2: Remove Simulation dependency on SliceJson.”

### P2-C — Stable actor ID migration

**Goal:** stop scene interaction/dialog/ActorView sync from relying on display names/domain keys.

**Files likely touched:** `EmberInteractable.cs`, `ActorView.cs`, `EmberPlayerInteractRaycaster.cs`, `DomainSimulationAdapter.cs`, scenes or scene recipes/spawner.

**Files not to touch:** adding new NPC features beyond identity wiring.

**Risks:** serialized scene migration can break interactions.

**Acceptance criteria:**

* All actor interactables and ActorViews carry stable actor IDs or are explicitly non-actor.
* Dialog conversation state stores active ActorId/NpcId.
* Duplicate display-name test passes.

**Suggested validation:** scene validation plus duplicate-name PlayMode test.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P2: Migrate Ember scene interaction to stable actor IDs.”

### P2-D — Adapter/host responsibility split

**Goal:** reduce god-object risk without gameplay changes.

**Files likely touched:** `DomainSimulationAdapter*.cs`, `EmberWorldHost.cs`, new helper classes, tests.

**Files not to touch:** scene YAML, save schema, LFS assets.

**Risks:** subtle runtime behaviour drift.

**Acceptance criteria:**

* Extract read-model projection, command routing, dialog/fate bridge, save bridge, and worldgen bridge one at a time.
* Public interfaces remain compatible.
* Characterization tests pass.

**Suggested validation:** EditMode tests, fallback harness, manual scene smoke.

**Unity Editor required:** yes for smoke.

**Suggested Codex prompt title:** “P2: Split Ember adapter and host responsibilities without behaviour change.”

### P3-A — Generated actor visibility / SOUL-04

**Goal:** make generated NPCs visible, schedulable, and speakable in scenes.

**Files likely touched:** actor billboard prefab/spawner, `ActorView`, scene recipes, worldgen projection tests.

**Files not to touch:** LLM feature expansion, save schema refactor.

**Risks:** scene/prefab reference breakage; performance with large population.

**Acceptance criteria:**

* Generated NPCs are instantiated with stable ActorIds.
* Same seed produces same visible population.
* Actor schedules move visible billboards.
* Screenshots prove generated actors, not only fixed cast.

**Suggested validation:** same-seed digest + PlayMode scene screenshot.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P3: Spawn generated Ember actors as stable billboard views.”

### P3-B — CRPG UI/readability pass

**Goal:** align HUD/dialog/action/clock/save UI with active PRDs.

**Files likely touched:** `EmberHud.cs`, `DialogBoxPanel.cs`, `EmberMainMenuUI.cs`, `PauseMenu.cs`, UI Toolkit backend files.

**Files not to touch:** simulation mechanics.

**Risks:** visual polish can hide systemic defects if done too early.

**Acceptance criteria:**

* HUD readable in every gameplay scene.
* Dialog supports Ask About per actor.
* Save/load status clear.
* Screenshots match PRD intent.

**Suggested validation:** screenshot checklist across all scenes.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P3: Align Ember CRPG UI with active PRDs and screenshots.”

### P4-A — Reference/docs archive cleanup

**Goal:** make reference material useful without looking active.

**Files likely touched:** `docs/reference/**`, `Reference/PRDs/**`, `Reference/OldBackendData/**`, audit/proof docs.

**Files not to touch:** code/assets.

**Risks:** deleting design intent.

**Acceptance criteria:**

* Every audit/proof/reference doc is tagged current/reference/archive.
* Matrix no longer claims old backend paths are active Unity work.
* No duplicate or missing canonical status docs.

**Suggested validation:** docs inventory and broken-link check.

**Unity Editor required:** no.

**Suggested Codex prompt title:** “P4: Classify Ember docs and PRDs into current/reference/archive.”

### P5-A — Visual/runtime polish after proof

**Goal:** polish generated/static visuals only after asset proof exists.

**Files likely touched:** art/materials/scenes/UI styles.

**Files not to touch:** simulation/save/LLM authority.

**Risks:** visual work masking systemic gaps.

**Acceptance criteria:**

* LFS assets resolved.
* Generated/static/fallback provenance visible in logs/proof.
* Full scene screenshots reviewed.

**Suggested validation:** runtime-LFS scene tour and build.

**Unity Editor required:** yes.

**Suggested Codex prompt title:** “P5: Visual polish with asset provenance proof.”

## 6. Refactor targets

### LOC / complexity thresholds

#### Above 1000 lines

| File                                                               |  LOC | Acceptable? | Split direction                                                     |
| ------------------------------------------------------------------ | ---: | ----------- | ------------------------------------------------------------------- |
| `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` | 1033 | No          | Split by effect family and keep one small golden integration suite. |

#### Above 800 lines

No current non-test source file above 800 LOC in this upload. That does **not** mean responsibility is solved; several partial classes are still aggregate god systems.

#### Above 500 lines

| File                                                                                           | LOC | Acceptable?     | Split direction                                                       |
| ---------------------------------------------------------------------------------------------- | --: | --------------- | --------------------------------------------------------------------- |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.cs`           | 660 | No              | Wizard state, validation, generation coordinator, transition service. |
| `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs`                                | 626 | No              | Composition root, lifecycle, input binding, UI binding, tick host.    |
| `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController.Rendering.cs` | 571 | No              | View renderer, style helpers, portrait/progress widgets.              |
| `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs`                        | 557 | No as aggregate | Extract real services; partial split is not enough.                   |
| `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`                                    | 554 | Borderline/no   | Split discovery, eligibility, reservation, assignment cases.          |
| `Assets/Scripts/Presentation/Ember/UI/EmberHud.cs`                                             | 538 | No              | Bars, action strip, message log, clock/status presenter.              |
| `Assets/Scripts/Data/Save/WorldSaveData.cs`                                                    | 527 | No              | DTO modules by subsystem.                                             |
| `Assets/Scripts/Infrastructure/AiDm/LlmClients.cs`                                             | 517 | No              | Config, local provider, cloud provider, transport, parser.            |

#### Above 300 lines

| File                                               | LOC | Acceptable?               | Split direction                                                |
| -------------------------------------------------- | --: | ------------------------- | -------------------------------------------------------------- |
| `DomainSimulationAdapter.Worldgen.cs`              | 472 | No as aggregate           | Worldgen seeding/hydration/spawning bridge.                    |
| `WorldgenService.Phases.cs`                        | 422 | Borderline/no             | Deterministic phases and projection modules.                   |
| `JobAssignmentSystem.cs`                           | 413 | Borderline/no             | Discovery/scoring/reservation.                                 |
| `WorldTickComposer.cs`                             | 392 | Borderline                | Keep as composer only; move catalogs/constants to data.        |
| `SpellExecutionServiceTests.cs`                    | 385 | Borderline                | Split if adding cases.                                         |
| `JobAssignmentSystem.Tick.cs`                      | 378 | Borderline                | Tick policy separate from assignment core.                     |
| `UiToolkitPanel.Frames.cs`                         | 374 | No                        | Per-screen/panel renderers.                                    |
| `EmberSaveService.cs`                              | 373 | No                        | Runtime save controller, repository presenter, scene restore.  |
| `DialogBoxPanel.cs`                                | 369 | No                        | Dialog view, topic list, modal input.                          |
| `LoadingScreenController.cs`                       | 369 | No                        | Loading state, view, generation progress coordinator.          |
| `ShieldBuffServiceRegistryBatchAbsorptionTests.cs` | 351 | Borderline                | Consolidate matrix cases.                                      |
| `EmberMainMenuUI.cs`                               | 345 | No                        | Menu presenter, save-slot presenter, generation entry.         |
| `AuditSeventhPassCoverageTests.cs`                 | 325 | Borderline                | Avoid growing audit-test bloat.                                |
| `OnnxAssetForge.cs`                                | 320 | Borderline/no             | Provider implementation, fallback/provenance, tensor pipeline. |
| `SpellTargetValidatorTests.cs`                     | 315 | Borderline                | Split only if growing.                                         |
| `FazSixToTwelveBackendAcceptanceTests.cs`          | 315 | Borderline                | Keep as acceptance if stable.                                  |
| `ClipBpeTokenizer.cs`                              | 310 | Acceptable if algorithmic | Do not edit without tokenizer fixtures.                        |
| `PlaceholderSimulationAdapter.cs`                  | 310 | No                        | Sandbox fallback only; avoid production masking.               |
| `DomainSimulationAdapter.Combat.cs`                | 310 | No as aggregate           | Move combat command adapter out.                               |
| `AuditCoverageGapsTests.cs`                        | 308 | Borderline                | Avoid audit-test sprawl.                                       |
| `ActorStoreTests.cs`                               | 306 | Borderline                | Split by actor-store behaviour if expanding.                   |

### Top 20 refactor targets

| Rank | File/class                                 | Current responsibility                                                   | Why too large or misplaced                                               | Proposed split                                                                                                 | Safe migration strategy                                 | Test/proof required                                  |
| ---- | ------------------------------------------ | ------------------------------------------------------------------------ | ------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------- |
| 1    | `DomainSimulationAdapter*.cs`              | World/tick/read models/commands/combat/save/dialog/fate/worldgen.        | Partial classes hide an aggregate adapter.                               | Tick bridge, read-model projector, command router, dialog adapter, fate adapter, save bridge, worldgen bridge. | Characterize public API first; extract one role per PR. | EditMode + scene smoke.                              |
| 2    | `EmberWorldHost.cs`                        | Scene composition, adapter setup, input, UI, tick, fallback UI creation. | Central runtime god host.                                                | Composition root, lifecycle host, input binder, UI binder.                                                     | Extract non-MonoBehaviour helpers first.                | Boot/gameplay scene smoke.                           |
| 3    | `EmberSaveService.cs`                      | Hotkeys, repository, PlayerPrefs, scene restore, UI status.              | Runtime, persistence, UI, and scene logic mixed.                         | Save controller, repository adapter, scene restore service, status presenter.                                  | Add characterization tests first.                       | Save/load PlayMode proof.                            |
| 4    | `EmberMainMenuUI.cs`                       | Menu UI, Continue, New Game generation top-up, loading transition.       | Menu owns save and generation policy.                                    | Menu presenter, save-slot presenter, generation/loading coordinator.                                           | Route Continue through save service first.              | Main menu → Continue/New Game proof.                 |
| 5    | `JsonSliceSaveService.cs`                  | Unity JsonUtility bridge, world save mapper, process store bridge.       | Name/design stuck in Slice era; Presentation-owned persistence bridge.   | Pure persistence service, mapper, process rehydration adapter.                                                 | Move after save characterization.                       | Golden save fixtures.                                |
| 6    | `WorldSaveData.cs`                         | Entire save DTO tree.                                                    | Broad schema and legacy role fields.                                     | DTO files/modules by subsystem.                                                                                | File split with no field rename first.                  | JSON compatibility.                                  |
| 7    | `WorldSaveMapper*.cs`                      | Domain↔DTO mapping.                                                      | Migration-critical and broad.                                            | Sub-mappers by actor/item/process/economy/worldgen/dialog/magic.                                               | Golden old/new save tests.                              | Roundtrip/migration tests.                           |
| 8    | `CharacterCreationController.cs`           | Wizard state, validation, character choices, generation, transition.     | Product-critical overcoupled controller.                                 | State machine, presenter, generation coordinator, transition service.                                          | Preserve serialized refs.                               | Character creation PlayMode/screenshots.             |
| 9    | `CharacterCreationController.Rendering.cs` | Procedural UI rendering.                                                 | Rendering dominates controller partial.                                  | View renderer and style helpers.                                                                               | No-behaviour extraction.                                | Screenshot comparison.                               |
| 10   | `EmberHud.cs`                              | HUD/action/message/status rendering.                                     | CRPG UI needs readable design and screenshots.                           | Bars, action strip, message log, clock/status.                                                                 | Align after PRD source map stable.                      | Screenshot checklist.                                |
| 11   | `DialogBoxPanel.cs`                        | Dialog rendering, topic input, modal behaviour.                          | Ask About is core Ember interface.                                       | Dialog view, topic list, modal input adapter.                                                                  | Add interaction tests first.                            | Tavern/all-scene dialog proof.                       |
| 12   | `LoadingScreenController.cs`               | Loading UI, progress, generation state.                                  | Loading/provenance/generation UI mixed.                                  | Loading state, view, generation progress adapter.                                                              | Preserve public API.                                    | New Game loading proof.                              |
| 13   | `LlmClients.cs`                            | Local/cloud provider config, HTTP transport, JSON parsing.               | Provider risk and cloud policy mixed.                                    | Config, local provider, cloud provider, transport, parser.                                                     | Fake provider tests first.                              | No-network default proof.                            |
| 14   | `NativeLlmClient.cs`                       | GGUF readiness, optional download, LLamaSharp runtime.                   | Readiness/download/runtime too coupled.                                  | Model validator, downloader, native runtime client.                                                            | Add pointer/hash tests first.                           | Real LFS model proof.                                |
| 15   | `ModelBootstrap.cs`                        | Manifest read, verification, download, provider locator.                 | Bootstrap policy and provider binding mixed.                             | Locator, verifier, downloader, provider factory.                                                               | Fix MiniLM path first.                                  | Manifest tests.                                      |
| 16   | `OnnxAssetForge.cs`                        | ONNX generation/fallback.                                                | Provider implementation under Simulation area; fallback/provenance risk. | ONNX provider, fallback generator, provenance reporter.                                                        | Add provenance tests.                                   | Generated asset proof.                               |
| 17   | `WorldgenService*.cs`                      | Generated world phases.                                                  | Core Ember system; visual projection still incomplete.                   | Phases, validators, actor/scene projection.                                                                    | Same-seed digest first.                                 | Worldgen digest + visible actors.                    |
| 18   | `JobAssignmentSystem*.cs`                  | Job discovery/assignment/tick logic.                                     | Colony sim core; still complex.                                          | Discovery, scoring, reservation, tick integration.                                                             | Preserve current tests; add digest tests.               | Job tick/save proof.                                 |
| 19   | `WorldTickComposer.cs`                     | Tick cadence and system wiring.                                          | Good composer direction, but hardcoded catalogs/constants.               | Keep composer, move data/catalogs/config out.                                                                  | Tests before expanding systems.                         | Deterministic tick digest.                           |
| 20   | `PlaceholderSimulationAdapter.cs`          | UI fallback fake simulation.                                             | Can mask real gameplay adapter failure.                                  | Sandbox-only adapter and explicit failure gate.                                                                | Add tests for fallback gating.                          | Gameplay scene fails loudly on real adapter failure. |

## 7. Unity-specific fix plan

1. **Do not rename MonoBehaviours yet.** `EmberWorldHost`, `EmberInteractable`, `ActorView`, `DialogBoxPanel`, `EmberScenePortal`, `EmberHud`, and save/UI components are serialized into scenes.
2. **Resolve `.meta` issues first.** The duplicate GUID check passes, but 13 orphan metas remain. Fix those before moving or deleting Unity assets.
3. **Move Unity assets only with their `.meta` files.** For scenes, prefabs, materials, textures, fonts, plugins, models, terrain assets, and scripts, move asset and `.meta` together.
4. **Prefer Unity Editor moves for scene/prefab/material/font/plugin assets.** File operations are safer for pure C# refactors only when `.meta` is preserved and class names stay unchanged.
5. **Script moves are safe only when GUID and type name stay stable.** Moving `.cs` + `.meta` together is usually safe; renaming a MonoBehaviour class/file requires scene/prefab ref checks.
6. **Do not patch scene YAML by hand except for minimal validated fixes.** Stable actor IDs, portal wiring, dialog panels, player rigs, cameras, and UI should be changed via Editor or audited Editor scripts.
7. **Scene reference checks are required for:** actor IDs, generated billboard spawner, portal target registry, UI host changes, save/restore scene flow, generated material assignment, and prefab creation.
8. **Plugin import settings require Unity inspection.** `Assets/Plugins/x86_64/**` and `Assets/Plugins/NuGet/**` cannot be validated from source-only zip.
9. **LFS must be resolved before runtime proof.** Pointer `.png`, `.dll`, `.onnx`, `.gguf`, and `.pb` files are not runtime assets.
10. **Generated output root must be made consistent.** Either track an empty `Assets/Generated/Core/` folder as an Editor output location or move all runtime generation to `Application.persistentDataPath`.
11. **Manual Play Mode verification is mandatory for:** movement, camera, collisions, portals, dialog, Ask About, Ask Fate/Ask DM, pause, save/load, combat, character creation, loading/generation.
12. **Screenshot proof is mandatory for UI/visual changes.** Exit codes do not prove Ember’s readable CRPG interface.

## 8. Docs cleanup plan

### Canonical docs to keep current

* `README.md` — entry point only, with volatile status removed.
* New `docs/CURRENT_STATE.md` — should be created and kept as the concise truth.
* `docs/REMEDIATION_V2_COUNTER.md` — remediation tracker, after stale embedded findings are separated.
* `docs/EMBER_VISION_BIBLE.md` — vision canon.
* `docs/EMBER_VISION_NOTES_MAMI.md` — author intent and “Ember soul.”
* `docs/AI_STACK.md` — AI policy, after source-only/runtime wording is fixed.
* `docs/PRD_GOVERNANCE.md` — PRD precedence.
* `docs/prds/PRD_overland_map_v1.md`
* `docs/prds/PRD_visual_architecture_3d_billboard_v1.md`
* `docs/PRD_living_world_soul_v1.md`
* `docs/PRD_loading_asset_generation_v1.md`
* `docs/mechanics/MASTER_MECHANICS_BIBLE.md`
* `docs/mechanics/ARCHITECTURE.md`
* `docs/validation.md`
* `docs/REPO_HYGIENE.md`
* `docs/DEPENDENCIES.md`
* `docs/SECURITY_NOTES.md`

### Docs to archive

* `docs/AUDIT_COUNTER.md` once its closed/open items are reconciled.
* `docs/Codex_audit.md`
* `docs/AUDIT_INDEPENDENT_2026-05-30.md`
* `docs/CODEX_INDEPENDENT_2026-05-30.md`
* Old proof docs under `docs/proofs/**` unless marked reproducible against current source/LFS/Unity state.
* Historical README Faz-0 sections, or move them to an archive doc.
* Old roadmap claims that conflict with current remediation state.

### Docs to delete only after classification

* Duplicate audit snapshots whose useful findings are represented in `CURRENT_STATE.md`.
* Proof files that cannot be reproduced and no longer explain current behaviour.
* Stale references to missing `Reports/`, `GeneratedAssets/`, or `docs/EMBER_GOAL.md`, after the replacement path is established.

### Duplicate/conflicting docs to merge or reconcile

* `docs/REMEDIATION_V2_COUNTER.md` vs `docs/AUDIT_COUNTER.md`
* `docs/AI_STACK.md` vs `docs/proofs/llm-roundtrip-2026-05-30.md`
* `docs/PRD_GOVERNANCE.md` vs `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`
* `Reference/PRDs/**` vs active Unity `docs/prds/**`
* `docs/REPO_HYGIENE.md` / `.gitignore` generated policy vs actual `Assets/Generated/Core.meta`
* `README.md` current top status vs historical Faz-0 sections

### One-page `docs/CURRENT_STATE.md` should contain

* Unity version: `6000.3.13f1`.
* URP version: `17.3.0`.
* Current build scenes list from `ProjectSettings/EditorBuildSettings.asset`.
* Current proof state:

  * source-only validated,
  * LFS-runtime unresolved,
  * Unity import unverified in this audit,
  * PlayMode/manual proof required.
* Current blockers:

  * missing `docs/EMBER_GOAL.md` replacement,
  * source-only LFS pointer assets,
  * orphan `.meta` files,
  * `Assets/Generated/Core` policy mismatch,
  * model manifest hashes/path bug,
  * Simulation → SliceJson dependency,
  * save/menu PlayerPrefs divergence,
  * no scene-authored stable actor IDs,
  * generated NPCs not fully visible.
* Canonical doc links.
* Codex forbidden actions.
* Validation commands.
* Last updated date and evidence basis.

## 9. Tests and validation plan

### Quick static checks

Run before every cleanup PR:

```bash
tools/validation/static-audit.sh
tools/validation/static-audit.sh --require-runtime
grep -RIn 'EMBER_GOAL\|CURRENT_STATE\|Reports/\|GeneratedAssets/' README.md docs .github tools
grep -RIn 'PlayerPrefs' Assets/Scripts docs
grep -RIn -E 'Input\.|UnityEngine\.Input' Assets/Scripts
grep -RIn -E 'Task\.Run|GetAwaiter\(\)\.GetResult\(\)' Assets/Scripts
grep -RIn '_actorId:\|_domainActorId:\|_targetSceneName:' Assets/Scenes/Ember
grep -RIn 'm_Script: {fileID: 0' Assets/Scenes Assets/Art Assets/Resources 2>/dev/null
grep -RIl '^version https://git-lfs.github.com/spec/v1' Assets | sort
```

Static checks should report:

* duplicate `.meta` GUIDs,
* missing `.meta`,
* orphan `.meta`,
* tracked/generated asset policy violations,
* LFS pointer runtime/art files,
* direct legacy Input usage outside `EmberInput`,
* PlayerPrefs usage,
* sync-over-async provider code,
* scene hardcoded strings,
* missing script sentinels.

### Fallback C# test checks

Fallback is useful only for source-level pure C# proof.

```bash
tools/validation/run-validation.sh --mode fallback
```

Acceptance: fallback output must remain labelled partial and must not be used as Unity import, scene, plugin, LFS, or PlayMode proof.

### Unity EditMode tests

Required for Domain/Data/Simulation/Infrastructure/Persistence changes.

Focus additions:

* `ModelManifest` rejects pointer files in runtime mode.
* `NativeLlmClient.IsAvailable` rejects pointer GGUF.
* MiniLM path resolves from manifest.
* Save/load golden fixtures.
* Main menu Continue uses file repository.
* Simulation no longer references SliceJson.
* Stable actor ID dialog lookup.
* World tick digest for time, needs, jobs, schedules, plants, prices, caravans.

### Unity PlayMode tests

Required additions:

* Boot → MainMenu → CharacterCreation → SmithingOverworld.
* Every build scene loads.
* Every gameplay scene has player rig, camera, EventSystem, HUD/dialog path.
* Every interactable can open dialog or is reported as non-dialog.
* Every portal target loads.
* Pause menu opens/closes.
* Save/load works from gameplay and main menu Continue.
* Ask Fate/Ask DM handles fallback/real provider state.
* CombatDungeon movement and action UI work.

### Manual scene tour

For each scene:

1. Load from build order.
2. Confirm camera/player/EventSystem.
3. Move and look.
4. Interact with all nearby NPCs/interactables.
5. Open dialog/Ask About.
6. Trigger portal/exit.
7. Save and load.
8. Confirm HUD/action/message readability.
9. Confirm no missing materials/textures unless explicitly labelled fallback.
10. Capture screenshot.

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

### Screenshot proof

Required screenshots:

* Main menu.
* Character creation first/mid/final state.
* Loading/generation with provenance log.
* Each gameplay scene with HUD visible.
* Dialog open in every scene with interactables.
* Oracle/Ask Fate result.
* CombatDungeon action/combat UI.
* Save/load status.
* Generated actor proof once spawner exists.

Each screenshot must state whether assets are source-only pointer, LFS-resolved, static authored, generated, or fallback.

### Save/load proof

Required:

* Save to slot 0 and load in same scene.
* Clear PlayerPrefs and Continue via file slot.
* Legacy PlayerPrefs save still migrates or fails with clear message.
* Corrupt save quarantined.
* Invalid scene name rejected.
* Domain state roundtrips actors, IDs, topics, memory/faction, time, jobs, worksites, plants, prices, tool traces.
* Save/load at tick N and continue produces same digest as uninterrupted run.

### LLM proof

Required states:

* Source-only mode: disabled/fallback clearly labelled.
* Runtime-LFS mode: real DLL/GGUF validated by size/hash/header.
* `USE_LLAMASHARP` compile path verified.
* In-game local Qwen response logged with provider/model ID.
* No cloud/network call by default.
* Invalid tool proposal rejected.
* Valid tool proposal routed through validator/router.
* LLM text never writes authoritative world state directly.

### Deterministic replay proof

Required:

* Same seed + same command log → same world digest.
* Save/load mid-run → same post-load digest as uninterrupted run.
* Generated art cache and LFS model state do not alter authoritative simulation digest.
* Wall-clock time, HTTP timing, Unity visual randomness, and LLM flavour text excluded from deterministic digest.

## 10. What Codex should NOT do

* Do not rewrite the whole project.
* Do not add gameplay features before cleanup/proof.
* Do not recreate `docs/EMBER_GOAL.md` as fiction; first decide the real replacement.
* Do not trust `docs/AUDIT_COUNTER.md` or embedded old audit rows as current truth without checking code.
* Do not treat source-only CI as runtime proof.
* Do not treat LFS pointer `.png`, `.dll`, `.onnx`, `.gguf`, or `.pb` files as real runtime assets.
* Do not move Unity assets without moving/preserving `.meta`.
* Do not delete orphan metas without checking whether the asset is intentionally absent/local/generated.
* Do not rename MonoBehaviour classes without scene/prefab reference checks.
* Do not hand-edit scene YAML except for tiny audited changes.
* Do not let LLM output mutate simulation state without validated tool calls.
* Do not make fallback/canned AI text look like real local Qwen.
* Do not enable cloud/network providers by default.
* Do not silently download models during normal gameplay/loading.
* Do not expand save schema fields without migration/golden fixtures.
* Do not keep adding responsibilities to `DomainSimulationAdapter`, `EmberWorldHost`, `EmberHud`, or `EmberMainMenuUI`.
* Do not add another manager/helper/god class to avoid real splits.
* Do not continue using display names as actor identity.
* Do not fix generated actor visibility with visual-only fake wanderers.
* Do not replace simulation gaps with scene dressing.
* Do not polish UI before stable interaction, save/load, and scene-tour proof exist.
* Do not import old Godot/backend reference JSON directly as active Unity runtime data.
* Do not delete `Reference/PRDs` before the matrix records each document’s status.
* Do not rely on historical screenshots unless they are reproducible from the current repo with LFS resolved.

## 11. Final prioritized checklist

1. Create `docs/CURRENT_STATE.md` and resolve the missing `docs/EMBER_GOAL.md` source-of-truth mismatch.
2. Reconcile `README.md`, `docs/REMEDIATION_V2_COUNTER.md`, and `docs/AUDIT_COUNTER.md`; mark old audit docs as archive/reference.
3. Update PRD governance/matrix so active Unity PRDs and Godot-era references are unambiguous.
4. Add full LFS pointer validation for runtime assets and art; label CI source-only.
5. Add or document a runtime-LFS validation/build workflow.
6. Resolve the 13 orphan `.meta` files with Unity-safe verification.
7. Fix `Assets/Generated/Core` policy: restore tracked folder or move runtime generation output to persistent path.
8. Normalize package manifest/lock with Unity `6000.3.13f1`.
9. Make `ModelManifest` runtime mode enforce size/hash/header and reject pointer files.
10. Fix `ModelBootstrap` MiniLM path mismatch.
11. Make `NativeLlmClient.IsAvailable` reject LFS pointer/corrupt GGUF files.
12. Reconcile AI proof docs with actual source-only vs runtime-LFS proof state.
13. Add save/load characterization tests covering file slot, PlayerPrefs fallback, corrupt quarantine, and main-menu Continue.
14. Refactor main-menu Continue/Load to use the canonical file save repository/service.
15. Add scene-tour validation for all 13 build scenes without editing scenes yet.
16. Add portal target validation against build settings/scene registry.
17. Add validation that reports missing stable `_actorId` / `_domainActorId` in scenes.
18. Migrate interactables and ActorViews to stable actor IDs.
19. Store active `ActorId`/NpcId in dialog conversation state, not only display name.
20. Remove `EmberCrpg.Simulation` dependency on `EmberCrpg.Data.SliceJson`.
21. Split `DomainSimulationAdapter` responsibilities one no-behaviour-change PR at a time.
22. Split `EmberWorldHost` into composition/lifecycle/input/UI responsibilities.
23. Add deterministic world tick digest tests for jobs, schedules, plants, prices, needs, caravans, magic.
24. Move hardcoded tick catalogs/constants toward data-driven definitions.
25. Implement generated actor billboard spawner only after stable ID policy is validated.
26. Prove generated actors are visible and deterministic with screenshots.
27. Migrate `EmberInput` internals to Input System behind the existing facade.
28. Split save schema/DTOs by subsystem with golden migration fixtures.
29. Split character creation controller/rendering.
30. Split menu/loading/generation responsibilities.
31. Split LLM provider classes and remove sync-over-async where practical.
32. Move forge provider implementations out of deterministic Simulation boundary or isolate them clearly.
33. Add full PlayMode route: Boot → MainMenu → CharacterCreation → all gameplay scenes → save/load.
34. Align HUD/dialog/action/clock/save UI to active PRDs with screenshot proof.
35. Archive stale proof/audit docs and tag remaining proofs by reproducibility mode.
36. Only after the above, allow new Ember gameplay feature work.

# Ember — Independent Brutal Audit (2026-05-30)

> Produced by 4 parallel independent reviewers (architecture/duplication · Unity hygiene/CI · determinism/LLM/save · Ember-soul/UI/scenes/docs), analysis-only, evidence-cited by exact path+line. This is the **source list for a later claude fixing pass**. It deliberately scrutinizes the just-finished 60-defect remediation for left-behind duplication, dead code, and overstated "done".
>
> One-line verdict: **the code-level cleanup was largely real and the layering is intact — but (a) the remediation left genuine dead code / two real save bugs / one determinism hole, and (b) the bigger truth the docs hide is that the "living world" is a frozen diorama: 6 of the simulation pillars are test-covered code with zero runtime callers.**

---

## 1. Executive summary

**Most broken (fix before any feature work):**
1. **Living world doesn't live.** `SliceTickComposer.Advance()` advances only time / magic-cooldowns / needs / caravans. PlantGrowth, PriceUpdate, FactionReputation, JobAssignment, schedules, harvest are **referenced only by tests** — zero tick-path callers. NPCs never move; worldgen NPCs never enter playable scenes. (SOUL-01/02/03/04)
2. **Save/load is not replay-equivalent.** `SliceTickComposer` hourly/daily accumulators aren't serialized; the purpose-built `RebuildAccumulatorsFrom(world.Time)` is **dead code**; restore only `ResetAnchor()`s. Cold-load desyncs cadence vs a continuous run. (DET-01, Critical)
3. **My new save path has a divergence bug.** `EmberSaveService.Save` writes the PlayerPrefs blob unconditionally, then the file slot in a swallowed try/catch — but `Load` *prefers the file slot*. A file-write failure makes Load return **stale** state while the newer save sits unused in PlayerPrefs. (DET-05)
4. **EMB-008 validation is cosmetic.** The live `consult_fate` validates a hardcoded self-built request and never inspects `response.ProposedToolCalls`; the genuinely-correct gate (`LlmProposalValidator`/`ToolCallRouter`) is dormant ("test-wired only"). Authority holds today only because the LLM is given *zero* authority. (DET-03)

**Structurally risky (architecture debt):**
5. **~1,078 LOC of speculative dead code**: `ShieldBuffAbsorptionBatchTotals` (+`ShieldBuffService.BatchTotals`) — ~23 aggregation permutations, 0 game callers, kept alive by 16 permutation test files. (ARCH-01)
6. **Orphans my Slice deletion left**: `AskDmService` / `NpcDialogueService` / `AskAboutService` now have **no production callers** (ConversationState replaced them) — not deleted. (ARCH-03)
7. **`DomainSimulationAdapter` god class** (~1,280 LOC / 4 partials) doing dialog + hydration + combat + save + fate + factories and reaching into Presentation statics. (ARCH-02)
8. **Two static service locators with an admitted write-race** (`ForgeLocator`, `EmberDomainAdapterLocator`) — hidden global state hostile to deterministic headless runs. (ARCH-07)
9. **Native LLM has no timeout** — EMB-018 fixed HTTP only; `NativeLlmClient` still `.GetAwaiter().GetResult()`s with no CTS. (DET-04)

**Merely messy (cleanup/docs):**
10. **PRDs triple-duplicated** — `Reference/PRDs/` and `docs/reference/prd/` are byte-identical (97 files each) + `docs/prds/` (10). (DOC-02)
11. **`docs/EMBER_GOAL.md` is actually a stale ChatGPT audit** (title line 1 = "Ember Audit Summary"), and README points to it as the goal. (DOC-01)
12. **`AUDIT_COUNTER.md` "60/60 · living-world" overstates gameplay truth** — true at compile/hygiene level, false at runtime. (DOC-03/04)
13. **Untracked cuDNN `.dll.meta` not gitignored** — one `git add -A` commits 10 orphan metas. (HYG-02)
14. **CI never builds the Win64 shipping target**; EditMode CI runs `lfs:false` → green-but-hollow. (HYG-08/09)
15. **`Slice*` is the core domain type name** (`SliceWorldState`, 68 refs) — prototype-era naming on the canonical model. (NAME-01)

**What the remediation got RIGHT (verified, do not "re-fix"):**
- `EmberForgeFactory` is genuine de-duplication (both bootstraps call it; not a parallel system).
- `EmberInput` is clean — **0** leftover `UnityEngine.Input` in active runtime.
- `ConversationState`/`NpcTopicCatalog` are correctly Domain-pure and genuinely wired into the live dialog path.
- Layering intact: Domain/Simulation are UnityEngine-free; no Domain→Sim/Presentation refs; EMB-019 asmdef move genuinely prevents Sim from referencing the LLM providers.
- The 9 deleted `Slice*` MonoBehaviours leave **no** dangling scene/test references.
- Real on-device Qwen round-trip proven; LLM never writes the world.

**What should NOT be touched yet:** the deterministic Domain/Simulation RNG + worldgen + tick math (sound); the forge/SDXL selection (now single-sourced); the EmberInput facade.

---

## 2. Ember soul alignment

**Preserved strengths:** pure deterministic Domain/Simulation shaped around the real pillars (needs, memory, faction reputation, plant growth, jobs, caravans, prices, seasons, worldgen all exist as real services); correct LLM-as-flavour boundary with a real validator layer present; proven local LLM; honest read-models (faction/job HUD panels read live domain state).

**Dangerous drift — NOT toward action-RPG, toward *diorama*:**
- The tick loop runs **2–4 systems**; the other six pillars are dormant test-only code (SOUL-02).
- `ActorScheduleState` is stored but never read → no actor moves (SOUL-03).
- Worldgen produces ~750 NPC seed records that are **never instantiated** as views; playable scenes use 5 hand-authored fixed actors from `SliceWorldFactory.Create(roomSeed:1)` (SOUL-04).
- Ask-About is silently dead in **6/10 gameplay scenes** (no `DialogBoxPanel` present; nothing creates one at runtime) (HUD-02).

**Systems pretending to exist (green tests, zero runtime effect):** `PlantGrowthSystem`, `PriceUpdateSystem`, `FactionReputationSystem`, `JobAssignmentSystem`, `HarvestSystem`, `PlantingSystem`, `MemoryWriteSystem`, schedule resolution.

**Net:** Ember in bones and intent; currently a simulation-shaped shell. Highest-leverage move to become Ember-for-real: wire the dormant systems into `SliceTickComposer` (SOUL-01) + ensure a `DialogBoxPanel` per gameplay scene (HUD-02).

---

## 3. Canonical source map

| Area | Canonical | Stale/archive/reference | Notes |
|---|---|---|---|
| Vision/soul | `docs/EMBER_VISION_BIBLE.md`, `docs/EMBER_VISION_NOTES_MAMI.md` | **`docs/EMBER_GOAL.md`** (actually the old ChatGPT audit) | Rename GOAL → `docs/archive/2026-05-30-chatgpt-audit.md`; repoint README. |
| Live status | `docs/CURRENT_STATE.md` | README "Historical status" block | README delegates correctly. |
| Remediation tracking | `docs/AUDIT_COUNTER.md` | — | Soften "ALL CLOSED/living-world"; add "code-complete vs runtime-wired" column. |
| PRDs | **pick** `docs/reference/prd/` | `Reference/PRDs/` (byte-identical dup) + `docs/prds/` | `diff` of the two 97-file dirs is empty. |
| Mechanics | `docs/mechanics/MASTER_MECHANICS_BIBLE.md` | `mechanic-map-v1.md`, `MICRO_MAPPING.md`, `BIBLE_AUDIT.md` | Declare one canonical. |
| World model | `Domain/World/SliceWorldState.cs` | — | Canonical but mis-named (NAME-01). |
| Composition root | `EmberWorldHost.cs` + `SliceTickComposer.cs` | `PlaceholderSimulationAdapter` (fallback) | The real spine. |
| Scene registry | `EmberScenes.cs` (EMB-056) | raw strings in `EmberScenePortal`/`BootBootstrap`/`EmberMainMenuUI` | Registry exists but is bypassed (SCN-01). |
| Build scenes | `Assets/Scenes/Ember/{Boot,MainMenu,CharacterCreation,SmithingOverworld,ColonyNeeds,SeasonFarm,TradeMarket,CombatDungeon,RitualHall,TavernDialog}` | `OracleShrine`,`ShowroomOverview`,`TavernFlavour` (not in tour) | 13 files, 10 in tour. |

---

## 4. Consolidated defect register

Severity: Blocker / Critical / High / Medium / Low. claude-safe = a headless agent can fix without the Editor.

### Determinism / simulation authority / save (DET)
| ID | Sev | Path+lines | Why it matters | Fix | claude-safe | Validation |
|---|---|---|---|---|---|---|
| DET-01 | **Critical** | `Simulation/Composition/SliceTickComposer.cs:56-57,182-201`; `Adapters/DomainSimulationAdapter.Save.cs:58` | hourly/daily accumulators not serialized; `RebuildAccumulatorsFrom` is dead → save/load not replay-equivalent | call `RebuildAccumulatorsFrom(world.Time)` on restore, or persist the 2 accumulators in `SliceSaveData` | Yes | headless test: tick N → save → reconstruct → next daily boundary == continuous run |
| DET-03 | **High** | `Simulation/AiDm/NarrationServices.cs:6,45-50`; `Adapters/DomainSimulationAdapter.Fate.cs:77-91` | live consult_fate validates a self-built request, never `response.ProposedToolCalls`; real gate dormant | wire `ConsultFateService`/`LlmProposalValidator` into the live adapter; feed real proposed calls | Yes | EditMode: feed malicious `proposed_tool_calls` → assert rejected + no `_world` mutation |
| DET-04 | **High** | `Infrastructure/AiDm/NativeLlmClient.cs:80-135` | native load+infer has no CTS/timeout (EMB-018 fixed HTTP only) → a hung model pins a worker thread | add CTS+timeout around load+infer; empty `LlmResponse` on cancel | Yes | inject stalling `InferAsync`; assert bounded return |
| DET-02 | **High** | `Adapters/DomainSimulationAdapter.cs:340,797`, `.Fate.cs:72` | fire-and-forget `await Task.Run` continuations (incl. `_world.ToolCallTrace.Add`) are main-thread only by implicit SyncContext → reopens EMB-007 race headless | marshal post-await `_world` writes through an explicit main-thread queue drained in OnTick | Yes | run under null-SyncContext host; assert write thread == ctor thread |
| DET-05 | **Medium** | `Presentation/Ember/Save/EmberSaveService.cs:93-100,116` | dual-write divergence: PlayerPrefs gets new save, file slot may keep old, Load prefers file → returns stale | write file slot first; only update legacy blob + `lastslot` on file success | Yes | simulate `FileSaveRepository.Save` throw → assert Load returns newest |
| DET-06 | **Medium** | `Data/Save/FileSaveRepository.cs:42 (doc) vs 61-69` | doc promises timestamped `.corrupt-{ticks}`; code uses fixed `.corrupt` and deletes the prior → no quarantine history; doc/behaviour drift | implement timestamped names or fix the comment | Yes | corrupt twice → assert both quarantines (or doc matches) |
| DET-07 | **Medium** | `Data/Save/FileSaveRepository.cs:34-35` | `Delete(path); Move(tmp,path)` is not atomic (crash window loses a good slot) — "atomic-ish" overstates | use `File.Replace(tmp,path,null)` (atomic on NTFS) | Yes | static; race window obvious |
| DET-08 | Low | `Fate.cs:42-44` vs `NarrationServices.cs:108` | two parallel roll formulas (`(salted%100)+1` vs `(seed%100)+1`) — divergence risk if the service is wired | single shared deterministic roll helper in Domain | Yes | seed-sweep test both paths identical |

### Architecture / duplication / SOLID (ARCH)
| ID | Sev | Path+lines | Why it matters | Fix | claude-safe | Validation |
|---|---|---|---|---|---|---|
| ARCH-01 | **Critical** | `Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs` (762), `ShieldBuffService.BatchTotals.cs` (316), `…Partition.cs` | ~1,078 LOC, 0 game callers, kept alive by 16 test files — test-induced design damage the remediation didn't remove | keep `From` + the 1 filtered overload a real damage path needs; delete the rest + their tests (or move to an `Experimental` excluded folder) | Yes | build + EditMode; remaining shield tests green |
| ARCH-02 | **High** | `Adapters/DomainSimulationAdapter.cs` (804)+`.Combat`(310)+`.Fate`(104)+`.Save`(61) | god class: dialog+hydration+combat+save+fate+factories+ID-math, reaches `ForgeLocator.LlmRouter` static | extract `WorldHydrator`/`DialogController`/`AdapterStatFactory`; inject `ILlmRouter` | Partly | build + full EditMode |
| ARCH-03 | **High** | `Simulation/Narrative/AskDmService.cs`, `NpcDialogueService.cs`, `AskAboutService.cs` | orphaned by Slice deletion + ConversationState; 0 production callers (AskAbout only in comments) | delete all three + their tests, OR repromote exactly one as the NPC-dialog home and route the adapter through it | Yes (delete) | build + EditMode (drop matching tests) |
| ARCH-04 | **High** | `Save/EmberSaveService.cs`, `JsonSliceSaveService.cs`, `Data/Save/FileSaveRepository.cs`, `Adapters/DomainSimulationAdapter.Save.cs` | two nested save pipelines (a save-within-a-save): canonical world persisted as an opaque string inside a Unity transform blob; FileSaveRepository used only by EmberSaveService | one repository persists canonical `SliceSaveData`; transform/scene become fields; PlayerPrefs → read-only legacy | Partly | build + `JsonSliceSaveServiceTests` + F5/F9 round-trip (Editor) |
| ARCH-07 | **High** | `Forge/ForgeLocator.cs`, `Adapters/IDomainSimulationAdapter.cs:176` (`EmberDomainAdapterLocator`) | two mutable static locators with admitted write-race ("overwriting is fine") — order-dependent, blocks headless determinism + test isolation | promote to injected `IForgeServices`/`IAdapterProvider` from one composition root | Partly | build + EditMode; no `ForgeLocator.` reads in Domain/Sim |
| ARCH-06 | **High** | `Bootstrap/EmberWorldHost.cs:407-489` (566 total) | host re-implements `IDialogSource` only to forward to the adapter, plus a dead canned-line `switch` fallback | bind `DialogBoxPanel.Source` to the adapter's `IDialogSource`; delete the proxy + fallback; extract input/UI installers | Partly | build; TavernDialog topics still surface (Editor) |
| ARCH-09 | Medium | `Adapters/PlaceholderSimulationAdapter.cs` (289) vs 203-line `IDomainSimulationAdapter` | full second adapter mirroring snapshot logic; over-broad interface | keep placeholder minimal (throw/empty); segregate the interface | Yes | build + EditMode |
| ARCH-05 | Medium | `Infrastructure/AiDm/LlmClients.cs:11`, `NativeLlmClient.cs:16` | types declare `namespace EmberCrpg.Simulation.AiDm` while compiling into `EmberCrpg.Infrastructure.dll` — namespace lies about the assembly | rename namespaces to `EmberCrpg.Infrastructure.AiDm`; fix usings (placement is correct, only the namespace is wrong) | Yes | build; grep no stale Simulation.AiDm→infra |
| ARCH-08 | Medium | `IDomainSimulationAdapter.cs:54,55,102`; `EmberSaveService` `GameObject.Find("PlayerRig")` | primitive obsession: raw-string actor/site identity despite Domain value-type IDs; `GameObject.Find` lookup | pass `ActorId`/`SiteId` across the seam; replace `Find` with a registry handle | Partly | build + EditMode |
| ARCH-11 | Low | `Simulation/Composition/SliceTickComposer.cs:23,30`, `Magic/SpellExecutionService.cs:23` | XML `<see cref>` to Presentation types from inside Simulation (unresolvable, documents wrong dependency direction) | replace crefs with prose | Yes | build |
| ARCH-12 | Low | `Adapters/DomainSimulationAdapter.Save.cs:41-52` | `RestoreStateJson` reflects every public field onto live world — fragile determinism-critical load | give `SliceWorldState` an explicit `CopyFrom`/replace | Yes | build + round-trip test |

### Large files needing split (LOC) — buckets >300/>500/>800/>1000
`SliceSaveMapper.cs` 961 **SPLIT** (→ Economy/Narrative/Worldgen sub-mappers) · `DomainSimulationAdapter.cs` 804 **SPLIT** · `ShieldBuffAbsorptionBatchTotals.cs` 762 **DELETE-most** · `CharacterCreationController.cs` 660 **SPLIT** (fill the empty `CharacterCreationViewModel`) · `.Rendering.cs` 571 **SPLIT** · `EmberWorldHost.cs` 566 **SPLIT** · `EmberHud.cs` 538 **SPLIT** (construction vs bind) · `SliceSaveData.cs` 527 ACCEPTABLE · `LlmClients.cs` 517 **SPLIT** (per-class) · `EmberMainMenuUI.cs` 345 SPLIT (view vs bootstrap-spawn) · `EmberSaveService.cs` 355 SPLIT (shrinks via ARCH-04). Others 300–422 (`WorldgenService.Phases`, `JobAssignmentSystem*`, `UiToolkitPanel.Frames`, `OnnxAssetForge`, `ClipBpeTokenizer`, `DialogBoxPanel`) = ACCEPTABLE (cohesive/already-partial).

### Unity hygiene / CI / repo (HYG)
| ID | Sev | Path | Why it matters | Fix | claude-safe | Editor? |
|---|---|---|---|---|---|---|
| HYG-02 | **High** | `Assets/Plugins/x86_64/cuda/cudnn_*.dll.meta` (10, untracked) | `.gitignore` ignores `cudnn*.dll` but not the `.meta` → `git add -A` commits 10 orphan metas; constant status noise | add `cudnn*.dll.meta` to `.gitignore` | Yes | No |
| HYG-03 | **High** | `StreamingAssets/Models/sdxl-turbo/{unet,text_encoder_2}/model.onnx.data.meta` | tracked metas whose `.data` payload is gitignored → dangling import on clean clone | `git rm --cached` + ignore `*.onnx.data.meta` | Yes | No |
| HYG-05 | **High** | `Assets/Plugins/NuGet/*.dll` (47, ~19MB incl. Roslyn 12.7MB) | editor-only MCP/Roslyn DLLs committed raw (not LFS) → history bloat + risk of shipping into the player | move MCP plugin out of `Assets/` (UPM/`Packages/` or gitignore+per-dev) or at least LFS-track | Partly | Yes |
| HYG-08 | **High** | `.github/workflows/unity-test.yml:200-248` | CI only opt-in Linux64 build; never compiles the Win64 shipping target → real-build breakage invisible | add a Win64 build job (≥ nightly) invoking the Ember build menu | Yes | runner |
| HYG-09 | **High** | `.github/workflows/unity-test.yml:94,163,216` | EditMode CI runs `lfs:false`; tests touching real assets see 131-byte pointers → false-green | restore LFS for binary-needing jobs or assert pointer-independence; surface SOURCE-ONLY in job name | Partly | partial |
| HYG-11 | Medium | `tools/validation/static-audit.sh:88-128` | audit catches tracked-asset→untracked-meta but not untracked-meta→gitignored-asset (the cuDNN gap) | add: any `.meta` (tracked/staged) whose asset is gitignored → FAIL unless meta also ignored | Yes | No |
| HYG-10 | Medium | `.github/workflows/unity-test.yml:26-41` | `feat/**` branches get no CI until PR-to-main → static-audit gate skipped on feature pushes | add `feat/**` to push branches | Yes | No |
| HYG-06 | Low | `…/LiberationSans SDF - Fallback.asset` | TMP per-machine re-import churn dirty on `main` | `git checkout --`; document do-not-recommit | Yes | No |
| HYG-07 | Low | `Reference/OldBackendData/**` (146 files, 1.7MB `dialog_defs.json`) | stale pre-Unity backend JSON as live source; casing makes ignore-rule miss it | confirm reference-only; move under ignored path | Partly | No |
| HYG-13 | Low | `Assets/Scenes/Ember/TerrainData/*.asset` (20+, multi-MB) | terrain heightmaps committed for a billboard game that may not need them | confirm intent; regenerate/shrink if greybox | Partly | Yes |

### Ember soul / UI / scenes / docs / naming (SOUL/HUD/DLG/SCN/DOC/NAME)
| ID | Sev | Path | Why it matters | Fix | claude-safe | Editor? |
|---|---|---|---|---|---|---|
| SOUL-01 | **Critical** | `Simulation/Composition/SliceTickComposer.cs:111-167` | tick advances only time/magic/needs/caravans; 6 pillars excluded | gate per-hour/day calls to PlantGrowth/PriceUpdate/FactionReputation/JobAssignment; add "world moves over N ticks" test | Yes | No(test)/Yes(play) |
| SOUL-02 | **Critical** | grep: PlantGrowth/PriceUpdate/FactionReputation referenced only by `Assets/Tests` | systems pretend to exist; green tests mask dead runtime | wire via SOUL-01; until then docs must not claim they run | Yes | No |
| SOUL-03 | **High** | `Domain/Actors/ActorRecord.cs:76,131`; adapter:426 | `ScheduleState` stored, never read → NPCs never move | build a ScheduleSystem resolving actor target per game-hour | Yes(domain) | Yes(visual) |
| SOUL-04 | **High** | `Simulation/World/SliceWorldFactory.cs:52-56` | default world = 5 fixed actors; worldgen NPC seeds never instantiated as views | spawn ActorViews from `GeneratedWorld.Npcs`, or document scenes as fixed vignettes | Partly | Yes |
| HUD-02 | **High** | `Interaction/EmberPlayerInteractRaycaster.cs:87-99` | 6/10 gameplay scenes have Interactable NPCs but no `DialogBoxPanel`; none created at runtime → press-E does nothing | have `EmberWorldHost` ensure a DialogBoxPanel (like it does PauseMenu/EmberHud), or author per scene | Yes(host-ensure) | Yes |
| DLG-01 | **High** | `Adapters/DomainSimulationAdapter.cs:288-318` | per-actor topics keyed by `Name == actorName` string vs scene `_displayName` → mismatch silently falls back to global topics | resolve actor by stable ID, not display string | Partly | Yes |
| SCN-01 | **High** | `Interaction/EmberScenePortal.cs:11,24`, `BootBootstrap.cs:17`, `EmberMainMenuUI.cs:19` | portals hold raw scene-name strings; `EmberScenes` registry (EMB-056) bypassed → silent break on rename | drive portal targets from `EmberScenes` constants | Partly | Yes |
| HUD-01 | High(verify) | `EmberHud.cs` wired by `EmberWorldHost.BindUiPanels:304-353` | HUD action-bar state machine IS reachable (good) but visually unverified | Editor play: CAST→SPL1..5, ATK/SRCH route | N/A | Yes |
| DOC-01 | **High** | `docs/EMBER_GOAL.md:1` ("Ember Audit Summary"); README:27 | the file named GOAL is a stale audit; canonical-vision pointer misleads | rename → `docs/archive/2026-05-30-chatgpt-audit.md`; point README to VISION_BIBLE | Yes | No |
| DOC-02 | **High** | `Reference/PRDs/` vs `docs/reference/prd/` (byte-identical) + `docs/prds/` | ~194 PRD files triple-sourced → guaranteed drift | keep `docs/reference/prd/`; ref-scan then delete `Reference/PRDs/`; fold `docs/prds/` | Yes(after scan) | No |
| DOC-03/04 | Medium | `docs/AUDIT_COUNTER.md:46-48,79,82,99,119` | "ALL CLOSED/living-world" + several "DONE" carry self-flagged unexecuted manual proofs | add "code-complete vs runtime-wired vs proven" columns | Yes | No |
| NAME-01 | **High** | `SliceWorldState` (68 refs) + `SliceSaveMapper/ItemCatalog/SpellCatalog/TickComposer/WorldFactory` | "Slice" prototype prefix on the core domain model | dedicated atomic refactor `Slice*`→`Ember*`/`World*` with GUID-safe file renames | Partly | Yes(meta) |
| NAME-03 | Low | `Presentation/SliceHudFormatter.cs`, `SliceAtmosphere*.cs` at Presentation root | loose files outside the `Ember/` tree | move under `Presentation/Ember/...` with `.meta` | Yes | Yes(meta) |
| INP-01 | Low | `Input/EmberInput.cs:3` namespace `…Ember.Inputs` | folder `Input/` ≠ namespace `Inputs` | rename namespace to `.Input` (or folder to `Inputs/`) | Yes | No |
| NAME-02 | Low | `Domain/Actors/*` comments "Faz 4" | stale Turkish phase vocabulary in canonical comments | sweep "Faz N" → neutral | Yes | No |

---

## 5. claude-ready work packages (priority order; cleanup before features)

- **P0 — none.** Project compiles, opens (with LFS), build is green. No blocker.
- **P1 (data-loss / correctness):**
  - **B1 — Save correctness.** Fix DET-01 (persist/rebuild tick accumulators), DET-05 (file-first write order), DET-06/07 (`File.Replace` + quarantine doc). Touch: `SliceTickComposer`, `DomainSimulationAdapter.Save.cs`, `EmberSaveService`, `FileSaveRepository`. Don't touch: `SliceSaveMapper` schema. Accept: headless replay test passes; partial-write returns newest; atomic write. Editor: F5/F9 round-trip.
  - **B2 — LLM authority for real.** Fix DET-03 (route live `response.ProposedToolCalls` through `LlmProposalValidator`/`ToolCallRouter`; delete the synthetic shim) + DET-04 (native timeout) + DET-08 (one roll helper). Accept: malicious tool-call test rejected, no `_world` mutation. Editor: no.
- **P2 (architecture/dead-code):**
  - **B3 — Delete dead code.** ARCH-01 (strip ShieldBuff batch-totals + 16 tests), ARCH-03 (delete or repromote the 3 dialogue services). Accept: build + EditMode green with fewer files. Editor: no.
  - **B4 — Save pipeline unification** (ARCH-04) + namespace fixes (ARCH-05, INP-01) + Sim crefs (ARCH-11). Editor: round-trip check.
  - **B5 — De-static the locators** (ARCH-07) → one composition root passing `IForgeServices`/`IAdapterProvider`. Editor: scene smoke.
- **P3 (playability — turns "done" into real):**
  - **B6 — Wire the living world** (SOUL-01/02/03/04): gate dormant systems into the tick; spawn a ScheduleSystem; surface generated NPCs. Accept: a scene visibly changes over 2 min. Editor: yes.
  - **B7 — Dialog reachability** (HUD-02, DLG-01, SCN-01): host-ensure a DialogBoxPanel per scene; actor-by-ID topic resolution; portals via `EmberScenes`. Editor: yes.
- **P4 (docs):** B8 — DOC-01 (rename GOAL), DOC-02 (dedupe PRDs after ref-scan), DOC-03/04 (honest counter columns), one `CURRENT_STATE.md` refresh.
- **P5 (naming/hygiene):** B9 — HYG-02/03/05/08/09/10/11 (gitignore + CI Win64 + LFS honesty + static-audit gap); B10 — NAME-01 `Slice*`→`Ember*` atomic rename (GUID-safe); NAME-03 file moves.

---

## 6. Top refactor targets (do in this order)
1. `ShieldBuffAbsorptionBatchTotals` (+BatchTotals +16 tests) — delete-most. Biggest complexity win.
2. `DomainSimulationAdapter` (4 partials) — extract hydrator/dialog/factories, inject router.
3. `EmberWorldHost` — kill the dialog proxy + dead switch; extract input/UI installers.
4. Save subsystem (`EmberSaveService`+`JsonSliceSaveService`+`FileSaveRepository`) — one canonical repository.
5. `SliceSaveMapper` (961) — Economy/Narrative/Worldgen sub-mappers.
6. `Infrastructure/AiDm` namespace + per-class file split.
7. Static locators → composition root.
8. `CharacterCreationController` (660+571) — fill the empty ViewModel.
9. `PlaceholderSimulationAdapter` + segregate the 203-line interface.
10. `EmberMainMenuUI` (view vs bootstrap-spawn) · 11. `EmberHud` (build vs bind) · 12. dialogue-service decision.

## 7. Unity-safe fix plan
- Class/file renames (NAME-01/03) and asset moves: do via Unity Editor or move `.cs`+`.meta` together; the GUID lives in the `.meta`. Scan scenes/prefabs for the script GUID first (the `Slice*` core types are referenced by code, not scenes — lower risk, but `SliceHudFormatter`/`SliceAtmosphere*` may be on prefabs: verify).
- DialogBoxPanel host-ensure (HUD-02) and living-world wiring (SOUL-01) need Play Mode verification.
- The `EmberScenes` portal rewire (SCN-01) needs re-binding serialized fields in each scene.

## 8. Docs cleanup
- Canonical: `EMBER_VISION_BIBLE.md`, `EMBER_VISION_NOTES_MAMI.md`, `MASTER_MECHANICS_BIBLE.md`, `CURRENT_STATE.md`, `AUDIT_COUNTER.md`.
- Archive: `EMBER_GOAL.md` (→ chatgpt-audit), README historical block, overlapping mechanics docs.
- Delete (after ref-scan): `Reference/PRDs/` (dup of `docs/reference/prd/`).
- `CURRENT_STATE.md` must state plainly: which simulation systems actually tick vs which are code-only; which scenes prove gameplay vs render.

## 9. Tests & proof plan
- Static: `static-audit.sh` (+ the HYG-11 gitignored-meta check).
- Fallback C#: replay-equivalence test (DET-01), tool-authority rejection test (DET-03), save partial-write test (DET-05).
- EditMode: dialogue per-actor topic resolution by ID (DLG-01).
- PlayMode/Editor: scene tour screenshots (HUD-01, SCN-03), press-E dialog in all 10 scenes (HUD-02), "world moves over 2 min" (SOUL-01), F5/F9 round-trip across scenes.
- LLM proof already captured; add a malicious-tool-call refusal proof.

## 10. What claude must NOT do
- Do not rewrite the project or "fix" by adding another manager/helper/god class.
- Do not move Unity assets without their `.meta`; do not rename MonoBehaviour classes without scanning scene/prefab GUID refs.
- Do not delete docs before classifying canonical vs archive; do not delete `Reference/PRDs/` before a ref-scan.
- Do not let the LLM mutate simulation state except via validated tool calls; do not make the dormant validator "pass" cosmetically.
- Do not replace dormant simulation systems with visual-only hacks (no fake NPC wander) — wire the real systems.
- Do not touch the deterministic Domain/Sim RNG/worldgen/tick math, the `EmberForgeFactory`, or the `EmberInput` facade except for the named namespace rename.
- Do not commit cuDNN/model binaries; do not `git add -A` until HYG-02 is fixed.

## 11. Final prioritized checklist (can work on main)
1. `.gitignore`: cuDNN `.dll.meta` + `*.onnx.data.meta`; `git rm --cached` the 2 orphan metas (HYG-02/03).
2. Save replay-equivalence: persist/rebuild tick accumulators (DET-01).
3. Save write-order + atomic + quarantine (DET-05/06/07).
4. LLM authority: route real proposed tool calls through the validator; native timeout (DET-03/04/08).
5. Delete `ShieldBuff` batch-totals dead code + tests (ARCH-01).
6. Delete/repromote the 3 dialogue services (ARCH-03).
7. Unify save pipeline (ARCH-04) + Infrastructure namespace rename (ARCH-05) + INP-01 + Sim crefs (ARCH-11).
8. De-static the locators → composition root (ARCH-07); kill EmberWorldHost dialog proxy (ARCH-06).
9. Split `SliceSaveMapper`, `DomainSimulationAdapter`, `CharacterCreationController` (LOC).
10. Living world: wire dormant systems into the tick + ScheduleSystem (SOUL-01/02/03).
11. Dialog reachability: host-ensure DialogBoxPanel + actor-by-ID topics + EmberScenes portals (HUD-02/DLG-01/SCN-01).
12. CI: Win64 build job + LFS honesty + static-audit gitignored-meta check (HYG-08/09/11/10).
13. Docs: rename GOAL, dedupe PRDs, honest AUDIT_COUNTER columns (DOC-01/02/03).
14. `Slice*`→`Ember*` atomic GUID-safe rename (NAME-01) + file moves (NAME-03).

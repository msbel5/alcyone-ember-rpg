# EMBER REMEDIATION V2 — COUNTER (post-independent-audit)

> **READ THIS FILE + §0 + §1 FIRST WHEN RESUMING.** Single source of truth for the V2 fix campaign.
> Survives compaction: §1 counter + §3 register tell you exactly what's done / next / blocked.
> Detail/evidence lives in `docs/AUDIT_INDEPENDENT_2026-05-30.md` (our 4-agent audit) — grep it by ID,
> don't re-derive. The original Codex/ChatGPT audit (`docs/Codex_audit.md`, EMB-001..060) is CLOSED
> 60/60 in `docs/AUDIT_COUNTER.md`; V2 RE-OPENS the items that were closed cosmetically + adds the
> structural/soul findings that audit missed.

---

## §0 — OPERATING PROTOCOL (token-preservation · no AI drift)
1. **Resume from §1.** The `▶ NOW` item is the only thing in flight; everything else is context.
2. **One atomic defect at a time.** Plan → smallest slice → verify → commit → tick box → advance.
3. **Verify before "done" (exit-0 lies):** Domain/Sim/Data → `bash tools/validation/run-validation.sh --mode fallback` (~1s, must stay green). Presentation/asmdef/.meta/scene → full Win64 batchmode build (Editor CLOSED): `"E:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build -logFile <log>` → require `Build Finished, Result: Success` + 0 `error CS`.
4. **Ember soul is sacred.** Deterministic living-world CRPG; simulation is the source of truth; LLM is flavour-only and may mutate world state ONLY via validated tool calls; generation is canonical-per-seed. Wire REAL systems — never a visual-only hack (no fake NPC wander). Never drift to generic action RPG.
5. **Unity asset safety (hard):** never move an asset without its `.meta`; never rename a MonoBehaviour class/file without scanning scene/prefab GUID refs first; scene-YAML edits last resort; cuDNN/model binaries gitignored — NEVER commit; destructive deletes need a ref-scan (scenes+prefabs+code+tests+harness) first; do NOT `git add -A` until HYG-02 lands.
6. **Persist progress in 3 places:** this file's box, a focused commit (`Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`), one claude-mem line.
7. **Single `main` branch.** No feature branches (they confuse context). Don't stop until §1 = DONE; surface only genuine decisions (big scope, destructive deletes, design calls).

Box: `[ ]` todo · `[~]` in progress · `[x]` done+verified · `[-]` won't-do/superseded (reason) · `[E]` needs-Editor-play-proof.

---

## §1 — STATUS COUNTER  ◀ where we are
```
PROJECT : Alcyone Ember RPG (Unity 6000.3.13f1 · URP 17.3 · deterministic living-world CRPG + AI DM)
EFFORT  : V2 remediation — independent-audit findings (DET/ARCH/HYG/SOUL/HUD/DLG/SCN/DOC/NAME)
BRANCH  : main (only)
UPDATED : 2026-05-30
```
**Progress: 17/34 done · P1 + P2-dead-code + HYG-02/03/10/11 hygiene done. · ▶ NOW = SOUL-01 (wire dormant living-world systems into the tick — highest value), then remaining P2 refactors / P3 / DOC. · prior Codex EMB-001..060 = 60/60**

Lane order is the fix order: **P1 correctness → P2 architecture/dead-code → P3 playability → P4 docs → P5 naming/hygiene.** Do P1 fully before P2.

---

## §2 — HOW TO READ
Each row: `[box] ID · severity · file(s) · one-line fix`. Full evidence (exact path+line, why, validation) is in `docs/AUDIT_INDEPENDENT_2026-05-30.md` §4 under the same ID. Drill in only when you start the item.

---

## §3 — DEFECT REGISTER (V2)

### LANE P1 — correctness / data-loss / authority (do first)
- `[x]` **DET-01** · Critical · `SliceTickComposer.cs:56-201`, `DomainSimulationAdapter.Save.cs:58` — save/load NOT replay-equivalent: persist the hourly/daily accumulators in `SliceSaveData` OR call the dead `RebuildAccumulatorsFrom(world.Time)` on restore. Proof: headless tick→save→reload→next daily boundary == continuous run.
- `[x]` **DET-05** · High · `EmberSaveService.cs:93-116` — dual-write divergence: write the file slot FIRST; only update the legacy PlayerPrefs blob + `lastslot` on file-write success (else Load returns stale).
- `[x]` **DET-06** · Med · `FileSaveRepository.cs:42 vs 61-69` — quarantine doc/code drift: implement timestamped `.corrupt-{n}` as documented OR fix the comment.
- `[x]` **DET-07** · Med · `FileSaveRepository.cs:34-35` — non-atomic write: use `File.Replace(tmp,path,null)` (NTFS-atomic) instead of delete-then-move.
- `[x]` **DET-03** · High · `DomainSimulationAdapter.Fate.cs:77-91`, `NarrationServices.cs:6-50` — LLM authority is COSMETIC: route the live `response.ProposedToolCalls` through `LlmProposalValidator`/`ToolCallRouter`; delete the self-built synthetic-request shim. Proof: malicious `proposed_tool_calls` rejected + no `_world` mutation.
- `[x]` **DET-04** · High · `Infrastructure/AiDm/NativeLlmClient.cs:80-135` — native inference has no timeout: wrap load+infer in a `CancellationTokenSource` + timeout, empty `LlmResponse` on cancel (mirror EMB-018 HTTP).
- `[x]` **DET-02** · High · `DomainSimulationAdapter.cs:340,797`, `.Fate.cs:72` — fire-and-forget continuations write `_world.ToolCallTrace` on the main thread only by implicit SyncContext: marshal post-await `_world` writes through an explicit main-thread queue drained in OnTick.
- `[x]` **DET-08** · Low · `Fate.cs:42-44` vs `NarrationServices.cs:108` — two parallel roll formulas: one shared deterministic roll helper in Domain.
- `[x]` **ARCH-12** · Low · `DomainSimulationAdapter.Save.cs:41-52` — reflection field-mirror restore is fragile: give `SliceWorldState` an explicit `CopyFrom`/replace.

### LANE P2 — dead code / architecture / duplication
- `[x]` **ARCH-01** · Critical · `Simulation/Magic/ShieldBuffAbsorptionBatchTotals.cs`+`ShieldBuffService.BatchTotals.cs`+`…Partition.cs` (~1078 LOC) — 0 game callers, 16 permutation tests only: keep `From`+the 1 filtered overload a real damage path needs, delete the rest + their tests (ref-scan first).
- `[x]` **ARCH-03** · High · `Simulation/Narrative/AskDmService.cs`,`NpcDialogueService.cs`,`AskAboutService.cs` — orphaned by ConversationState: delete all three + their tests, OR repromote exactly one as the NPC-dialog home and route the adapter through it (decide, don't leave dormant).
- `[ ]` **ARCH-04** · High · `EmberSaveService.cs`+`JsonSliceSaveService.cs`+`FileSaveRepository.cs`+`DomainSimulationAdapter.Save.cs` — kill the save-within-a-save: one repository persists canonical `SliceSaveData`; transform/scene become fields; PlayerPrefs → read-only legacy.
- `[x]` **ARCH-05** · Med · `Infrastructure/AiDm/LlmClients.cs:11`,`NativeLlmClient.cs:16` — namespace lies about assembly: rename `EmberCrpg.Simulation.AiDm` → `EmberCrpg.Infrastructure.AiDm`; fix usings (placement is correct).
- `[ ]` **ARCH-07** · High · `Forge/ForgeLocator.cs`, `IDomainSimulationAdapter.cs:176` — two mutable static locators with a write-race: introduce `IForgeServices`/`IAdapterProvider` from one composition root (keep statics as thin shims during migration).
- `[ ]` **ARCH-06** · High · `Bootstrap/EmberWorldHost.cs:407-489` — host re-implements `IDialogSource` only to forward to the adapter + dead canned-line switch: bind `DialogBoxPanel.Source` to the adapter directly, delete the proxy.
- `[ ]` **ARCH-02** · High · `DomainSimulationAdapter.cs` (804)+partials — god class: extract `WorldHydrator`/`DialogController`/`AdapterStatFactory`; inject `ILlmRouter` instead of `ForgeLocator.LlmRouter`.
- `[ ]` **ARCH-09** · Med · `PlaceholderSimulationAdapter.cs` (289) + `IDomainSimulationAdapter` (203) — trim placeholder to throw/empty; segregate the over-broad interface (dialog/HUD/worldgen sub-interfaces).
- `[ ]` **ARCH-08** · Med · `IDomainSimulationAdapter.cs:54,55,102`, `EmberSaveService` `GameObject.Find("PlayerRig")` — primitive obsession: pass `ActorId`/`SiteId` across the seam; replace `Find` with a registry handle.
- `[x]` **ARCH-11** · Low · `SliceTickComposer.cs:23,30`, `SpellExecutionService.cs:23` — Sim XML `<see cref>` points up to Presentation: replace crefs with prose.
- `[ ]` **LOC-split** · Med · split (after the above, mechanical, round-trip test each): `SliceSaveMapper.cs` 961→Economy/Narrative/Worldgen mappers · `DomainSimulationAdapter.cs` 804 · `CharacterCreationController.cs` 660 (+fill the empty `CharacterCreationViewModel`) · `LlmClients.cs` 517 (per-class files) · `EmberMainMenuUI.cs` 345 (view vs bootstrap-spawn).

### LANE P3 — playability (turns "done" into real; many need Editor proof)
- `[ ]` **SOUL-01** · Critical · `Simulation/Composition/SliceTickComposer.cs:111-167` — tick advances only time/magic/needs/caravans: gate per-hour/day calls to PlantGrowth, PriceUpdate, FactionReputation, JobAssignment (cost-gated). Proof: headless "world moves over N ticks" test (plant stage + price + job state change).
- `[ ]` **SOUL-03** · High · `Domain/Actors/ActorRecord.cs:76,131` — `ScheduleState` stored but never read: build a ScheduleSystem resolving actor target per game-hour so NPCs move. `[E]` visual.
- `[ ]` **SOUL-04** · High · `Simulation/World/SliceWorldFactory.cs:52-56` — worldgen NPC seeds never instantiated as views (scenes use 5 fixed actors): spawn ActorViews from `GeneratedWorld.Npcs`, OR explicitly document scenes as fixed vignettes. `[E]`
- `[ ]` **HUD-02** · High · `Interaction/EmberPlayerInteractRaycaster.cs:87-99` — Ask-About dead in 6/10 scenes (no DialogBoxPanel): have `EmberWorldHost` ensure one at runtime (like it does PauseMenu/EmberHud). `[E]`
- `[ ]` **DLG-01** · High · `DomainSimulationAdapter.cs:288-318` — per-actor topics keyed by display-name string vs scene `_displayName`: resolve actor by stable ID; mismatch must not silently fall back to global topics.
- `[ ]` **SCN-01** · High · `EmberScenePortal.cs:11,24`,`BootBootstrap.cs:17`,`EmberMainMenuUI.cs:19` — `EmberScenes` registry bypassed by raw scene-name strings: drive portal targets from `EmberScenes` constants. `[E]`
- `[ ]` **HUD-01** · `[E]` · `EmberHud.cs` — verify action-bar reachable: click CAST→SPL1..5, ATK/SRCH route (scene-tour screenshot).
- `[ ]` **SCN-03** · `[E]` · `CharacterCreation.unity` — walk all wizard stages; confirm intent carried to gameplay.

### LANE P4 — docs hygiene
- `[ ]` **DOC-01** · High · `docs/EMBER_GOAL.md` — it's the stale ChatGPT audit, not the goal: rename → `docs/archive/2026-05-30-chatgpt-audit.md`; repoint README to `EMBER_VISION_BIBLE.md`.
- `[ ]` **DOC-02** · High · `Reference/PRDs/` vs `docs/reference/prd/` (byte-identical) + `docs/prds/` — ref-scan then delete `Reference/PRDs/`; fold `docs/prds/`; one PRD matrix.
- `[ ]` **DOC-03** · Med · `docs/AUDIT_COUNTER.md:46` + `docs/CURRENT_STATE.md` — add "code-complete vs runtime-wired vs proven" columns; mark the dormant systems honestly.

### LANE P5 — naming / hygiene / CI
- `[x]` **HYG-02** · High · `.gitignore` — add `Assets/Plugins/x86_64/cuda/cudnn*.dll.meta` (untracked orphan metas hazard). DO THIS FIRST among hygiene.
- `[x]` **HYG-03** · High · `StreamingAssets/Models/sdxl-turbo/{unet,text_encoder_2}/model.onnx.data.meta` — `git rm --cached` + ignore `*.onnx.data.meta`.
- `[x]` **HYG-11** · Med · `tools/validation/static-audit.sh:88-128` — add check: any tracked/staged `.meta` whose asset is gitignored → FAIL unless meta also ignored.
- `[ ]` **HYG-08** · High · `.github/workflows/unity-test.yml` — add a Win64 build job (≥ nightly) invoking the Ember build menu. `[E]` runner.
- `[ ]` **HYG-09** · High · same — EditMode CI `lfs:false` false-greens; restore LFS for binary-needing jobs or assert pointer-independence + name the job SOURCE-ONLY.
- `[ ]` **HYG-05** · High · `Assets/Plugins/NuGet/*.dll` (~19MB Roslyn/MCP) — move the MCP dev plugin out of `Assets/` (Packages/ or gitignore+per-dev) or LFS-track. `[E]` verify plugin still loads.
- `[x]` **HYG-10** · Med · workflow triggers — add `feat/**` to push branches so the static-audit gate runs.
- `[ ]` **INP-01** · Low · `Input/EmberInput.cs:3` — namespace `…Ember.Inputs` ≠ folder `Input/`: align.
- `[ ]` **NAME-01** · High · `SliceWorldState` (68 refs) + `SliceSaveMapper/ItemCatalog/SpellCatalog/TickComposer/WorldFactory` — dedicated atomic GUID-safe rename `Slice*`→`Ember*`/`World*` (do LAST; ref-heavy; scan scene/prefab GUIDs).
- `[ ]` **NAME-03** · Low · `Presentation/SliceHudFormatter.cs`,`SliceAtmosphere*.cs` — move under `Presentation/Ember/...` (with `.meta`).
- `[ ]` **NAME-02** · Low · `Domain/Actors/*` — sweep stale "Faz N" comments → neutral.

---

## §4 — DECIDED / WON'T-DO (record here with reason)
- (none yet)

## §5 — GUARDRAILS (do NOT)
- Don't rewrite the project; don't "fix" by adding another manager/helper/god class.
- Don't make the dormant validator pass cosmetically (DET-03) — wire the real gate.
- Don't replace dormant simulation systems with visual-only hacks (SOUL-01) — wire the real systems.
- Don't touch the deterministic Domain/Sim RNG/worldgen/tick MATH, `EmberForgeFactory`, or the `EmberInput` facade body (only the INP-01 namespace).
- Don't delete docs/PRDs before a ref-scan; don't rename MonoBehaviours without a scene/prefab GUID scan; don't `git add -A` before HYG-02.
- Don't let the LLM mutate world state except via validated tool calls.

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
**Progress: 23/34 fully done + ARCH-02 & LOC-split substantially advanced `[~]`. Done: P1 + P2-dead-code + hygiene + DOC-01/02/03 + INP-01 + NAME-02 + NAME-03. This pass also: split SliceSaveMapper 961→6 + adapter 829→471 (ARCH-02 structural). SOUL-01..04 deferred-with-spec (feature epic). Deleted ~73k lines of doc cruft. · ▶ NOW = the tail is decision-gated / Editor-proof / epic — see below. · prior Codex EMB-001..060 = 60/60**

> **State of the remaining tail (honest triage after this pass):**
> - **Needs ONE user decision (highest-value dedup):** ARCH-04 save-within-a-save. Approach forks on save-compat (break dev-only saves + bump schemaVersion, vs migrate) and seam (`string ExportStateJson()` vs typed `SliceSaveData ExportState()`).
> - **Needs Editor play-proof `[E]` (batch):** ARCH-06 (live `_fateLine`), DLG-01 (seam+SOUL-04), HUD-02, SCN-01, HUD-01, SCN-03.
> - **Needs user go-ahead (feature epic):** SOUL-01..04 (plumb plant/job/worksite/price/schedule state into SliceWorldState so the world actually lives).
> - **Remaining design refactors (autonomous-doable, larger):** ARCH-07 (locator→DI; note the "write-race" is theoretical in single-threaded Unity — value is decoupling), ARCH-02 remainder (WorldHydrator collaborator + ILlmRouter DI), ARCH-08/09 (seam / interface segregation).
> - **Marginal:** CharacterCreationController/EmberMainMenuUI splits, HYG-09 (CI honesty).

> Audit-correction findings (this pass):
> - **ARCH-06 is mis-scoped → reclassified `[E]`.** `EmberWorldHost._fateLine` is LIVE (set from `_oracle.ConsultFate()` at lines 136/165/174, returned with precedence at 416), not "dead canned lines." Binding `DialogBoxPanel.Source` straight to the adapter would drop the oracle/fate dialog line. Doing it right means proving DET-03's adapter fate-flow subsumes `_oracle.ConsultFate()` — an Editor play-proof of the OracleShrine dialog. Batch with HUD-02/DLG-01.
> - **ARCH-04 needs a decision (save-compat + seam).** Killing the nested-JSON requires either (a) `SliceSaveData` (Data tier) gains primitive scene/transform fields + repo persists it directly, or (b) `SaveData` envelope holds a typed `SliceSaveData` (one JsonUtility pass, no escaped string) — which changes the adapter seam from `string ExportStateJson()` to `SliceSaveData ExportState()`. Both BREAK existing nested-string saves unless migrated. Data-loss-adjacent (guardrail). Hold for a crisp scope/compat call.

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
- `[E]` **ARCH-06** · High · `Bootstrap/EmberWorldHost.cs:407-489` — host re-implements `IDialogSource` to forward to the adapter. AUDIT-CORRECTION: not "dead canned lines" — `_fateLine` (oracle ConsultFate) is LIVE with precedence (136/165/174/416). Reclassified `[E]`: removing the proxy needs an OracleShrine dialog play-proof that the adapter fate-flow subsumes `_oracle.ConsultFate()`. Batch with HUD-02/DLG-01.
- `[~]` **ARCH-02** · High · `DomainSimulationAdapter.cs` god class — STRUCTURAL HALF DONE (`24bdf31f`): extracted the worldgen/character-creation/hydration cluster into `DomainSimulationAdapter.Worldgen.cs` (main 829→471, new 381), mirroring the .Save/.Fate/.Combat partials. Win64 Success, 0 CS. REMAINING (design, deferred): promote the cluster to a genuine `WorldHydrator` collaborator + inject `ILlmRouter` instead of `ForgeLocator.LlmRouter` (overlaps ARCH-07 DI).
- `[ ]` **ARCH-09** · Med · `PlaceholderSimulationAdapter.cs` (289) + `IDomainSimulationAdapter` (203) — trim placeholder to throw/empty; segregate the over-broad interface (dialog/HUD/worldgen sub-interfaces).
- `[ ]` **ARCH-08** · Med · `IDomainSimulationAdapter.cs:54,55,102`, `EmberSaveService` `GameObject.Find("PlayerRig")` — primitive obsession: pass `ActorId`/`SiteId` across the seam; replace `Find` with a registry handle.
- `[x]` **ARCH-11** · Low · `SliceTickComposer.cs:23,30`, `SpellExecutionService.cs:23` — Sim XML `<see cref>` points up to Presentation: replace crefs with prose.
- `[~]` **LOC-split** · Med · mechanical, round-trip tested:
  - `[x]` `SliceSaveMapper.cs` 961 → 6 partials (root + Process/World/Economy/Narrative/ActorDetail, each 140–242) — `be15e50b`, fallback 1214✓ + Win64 Success.
  - `[x]` `DomainSimulationAdapter.cs` 829 → 471 (+Worldgen.cs 381) — done as ARCH-02 structural `24bdf31f`.
  - `[-]` `LlmClients.cs` 517 — WON'T per-class-split: header carries a deliberate prior Codex decision (J-P3 #32) folding the 3 public client types with documented rationale + "do not split now". Respected (guardrail: don't fight validated-good decisions). The internal `LlmHttpClientCore` could be extracted later if desired (outside the note's scope).
  - `[ ]` `CharacterCreationController.cs` 660 (+fill empty `CharacterCreationViewModel`) — pending; MVVM-extraction is design work, not pure mechanical.
  - `[ ]` `EmberMainMenuUI.cs` 345 (view vs bootstrap-spawn) — pending; decoration methods are interleaved (non-contiguous), marginal LOC payoff for a 345-line file.

### LANE P3 — playability (turns "done" into real; many need Editor proof)
- `[~]` **SOUL-01** · Critical · `Simulation/Composition/SliceTickComposer.cs:111-167` — tick advances only time/magic/needs/caravans: gate per-hour/day calls to PlantGrowth, PriceUpdate, FactionReputation, JobAssignment (cost-gated). Proof: headless "world moves over N ticks" test (plant stage + price + job state change).
- `[~]` **SOUL-03** · High · `Domain/Actors/ActorRecord.cs:76,131` — `ScheduleState` stored but never read: build a ScheduleSystem resolving actor target per game-hour so NPCs move. `[E]` visual.
- `[~]` **SOUL-04** · High · `Simulation/World/SliceWorldFactory.cs:52-56` — worldgen NPC seeds never instantiated as views (scenes use 5 fixed actors): spawn ActorViews from `GeneratedWorld.Npcs`, OR explicitly document scenes as fixed vignettes. `[E]`
- `[ ]` **HUD-02** · High · `Interaction/EmberPlayerInteractRaycaster.cs:87-99` — Ask-About dead in 6/10 scenes (no DialogBoxPanel): have `EmberWorldHost` ensure one at runtime (like it does PauseMenu/EmberHud). `[E]`
- `[E]` **DLG-01** · High · `DomainSimulationAdapter.cs:306-335` — per-actor topics keyed by display-name string (`NpcSeeds.FirstOrDefault(n => n.Name == actorName)`) vs scene `_displayName`. AUDIT-CORRECTION: resolving by stable ID isn't a local edit — the interaction seam passes `target.DisplayName` (a string) from the raycaster, and scene actors carry no `NpcId` (that's SOUL-04's dormant scene-actor↔seed binding). Fixing it properly means a seam change (`DisplayName`→`NpcId`) + giving scene actors stable IDs + an Editor dialog play-proof. Reclassified `[E]`, entangled with SOUL-04.
- `[ ]` **SCN-01** · High · `EmberScenePortal.cs:11,24`,`BootBootstrap.cs:17`,`EmberMainMenuUI.cs:19` — `EmberScenes` registry bypassed by raw scene-name strings: drive portal targets from `EmberScenes` constants. `[E]`
- `[ ]` **HUD-01** · `[E]` · `EmberHud.cs` — verify action-bar reachable: click CAST→SPL1..5, ATK/SRCH route (scene-tour screenshot).
- `[ ]` **SCN-03** · `[E]` · `CharacterCreation.unity` — walk all wizard stages; confirm intent carried to gameplay.

### LANE P4 — docs hygiene
- `[x]` **DOC-01** · High · `docs/EMBER_GOAL.md` — it's the stale ChatGPT audit, not the goal: rename → `docs/archive/2026-05-30-chatgpt-audit.md`; repoint README to `EMBER_VISION_BIBLE.md`.
- `[x]` **DOC-02** · High · `Reference/PRDs/` vs `docs/reference/prd/` (byte-identical) + `docs/prds/` — ref-scan then delete `Reference/PRDs/`; fold `docs/prds/`; one PRD matrix.
- `[x]` **DOC-03** · Med · `docs/AUDIT_COUNTER.md:46` + `docs/CURRENT_STATE.md` — add "code-complete vs runtime-wired vs proven" columns; mark the dormant systems honestly.

### LANE P5 — naming / hygiene / CI
- `[x]` **HYG-02** · High · `.gitignore` — add `Assets/Plugins/x86_64/cuda/cudnn*.dll.meta` (untracked orphan metas hazard). DO THIS FIRST among hygiene.
- `[x]` **HYG-03** · High · `StreamingAssets/Models/sdxl-turbo/{unet,text_encoder_2}/model.onnx.data.meta` — `git rm --cached` + ignore `*.onnx.data.meta`.
- `[x]` **HYG-11** · Med · `tools/validation/static-audit.sh:88-128` — add check: any tracked/staged `.meta` whose asset is gitignored → FAIL unless meta also ignored.
- `[ ]` **HYG-08** · High · `.github/workflows/unity-test.yml` — add a Win64 build job (≥ nightly) invoking the Ember build menu. `[E]` runner.
- `[ ]` **HYG-09** · High · same — EditMode CI `lfs:false` false-greens; restore LFS for binary-needing jobs or assert pointer-independence + name the job SOURCE-ONLY.
- `[ ]` **HYG-05** · High · `Assets/Plugins/NuGet/*.dll` (~19MB Roslyn/MCP) — move the MCP dev plugin out of `Assets/` (Packages/ or gitignore+per-dev) or LFS-track. `[E]` verify plugin still loads.
- `[x]` **HYG-10** · Med · workflow triggers — add `feat/**` to push branches so the static-audit gate runs.
- `[x]` **INP-01** · Low · `Input/EmberInput.cs:3` — namespace `…Ember.Inputs` ≠ folder `Input/`: align.
- `[ ]` **NAME-01** · High · `SliceWorldState` (68 refs) + `SliceSaveMapper/ItemCatalog/SpellCatalog/TickComposer/WorldFactory` — dedicated atomic GUID-safe rename `Slice*`→`Ember*`/`World*` (do LAST; ref-heavy; scan scene/prefab GUIDs).
- `[x]` **NAME-03** · Low · `Presentation/SliceHudFormatter.cs`,`SliceAtmosphere*.cs`,`InventoryEquipmentFormatter.cs` — moved (4 .cs+.meta, GUID-safe) → `Presentation/Ember/UI/`; namespace `…Presentation.Slice`→`…Presentation.Ember.UI`; updated 2 test usings + fallback csproj. Fallback 1214✓; Win64 Success, 0 CS. Commit `f49b8078`.
- `[x]` **NAME-02** · Low · `Domain/Actors/*` (expanded project-wide) — swept 262 stale Turkish "Faz"(=Phase) sprint labels across 225 .cs files → English "Phase" (comments + string labels only; compile-inert). Fallback 1214✓; Win64 Success, 0 CS. Commit `005429ed`.

---

## §4 — DECIDED / WON'T-DO (record here with reason)
- **SOUL-01/03/04 → DEFERRED as a FEATURE EPIC (not a remediation wiring), with spec.** On inspection
  the dormant systems can't be "ticked" because the world-state they operate on was never plumbed into
  the live `SliceWorldState`: `PlantGrowthSystem.AdvanceOneDay` needs a `ComponentStore<PlantComponent>`
  + `PlantSpeciesDef` + farm plots (absent); `JobAssignmentSystem.TryAssignNext` needs a `JobBoard` +
  `WorksiteStore` (absent); `PriceUpdateSystem.Recompute` is per-item (needs an item/threshold/delta
  config + stockpile iteration); `FactionReputationSystem.ApplyDelta` is event-driven, not a tick.
  Wiring them REQUIRES first adding that state to `SliceWorldState`, seeding it, ticking it, and
  rendering it — a multi-step living-world feature. The guardrail forbids a visual-only hack, so this is
  honestly deferred to a dedicated feature effort rather than fake-completed. SOUL-02 (the "systems
  pretend to exist" finding) is the same root cause. Marked `[~]`; needs a feature epic + user go-ahead.

## §5 — GUARDRAILS (do NOT)
- Don't rewrite the project; don't "fix" by adding another manager/helper/god class.
- Don't make the dormant validator pass cosmetically (DET-03) — wire the real gate.
- Don't replace dormant simulation systems with visual-only hacks (SOUL-01) — wire the real systems.
- Don't touch the deterministic Domain/Sim RNG/worldgen/tick MATH, `EmberForgeFactory`, or the `EmberInput` facade body (only the INP-01 namespace).
- Don't delete docs/PRDs before a ref-scan; don't rename MonoBehaviours without a scene/prefab GUID scan; don't `git add -A` before HYG-02.
- Don't let the LLM mutate world state except via validated tool calls.

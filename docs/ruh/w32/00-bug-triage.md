# W32 bug triage — BUG_REPORT_GPT.md (B01-B30) vs CURRENT code

> Verified 2026-07-24 against the working tree (HEAD `4f85ec3c` + 4 uncommitted files:
> `OptionsScreen.cs`, `NeedConsumptionSystem.cs`, `DomainSimulationAdapter.Dialog.Source.cs`,
> `DialogStreamText.cs`). The external report was synthesized from the atlas snapshot; W31
> (`1e9b474b`) and the working tree closed several items after that snapshot. Every status
> below is anchored to code read today, not to the atlas.

**Statuses:** `already-fixed` (proof cited) · `dies-in-W32-eat-slice` (the eat/needs slice
owns it) · `needs-spot-fix` (small, do in W32) · `later-slice` (real, but structural).

---

## Spot-fix queue, in priority order

Player-facing world-loss first, then the four economy loop closers, then the rest.

| # | Bug | Sev | One-line fix direction |
|---|-----|-----|------------------------|
| 1 | B02 overland cold-load | critical | Rebuild Overland+GeneratedWorld from saved `WorldProfile.Seed` inside `RestoreStateJson` — `DomainSimulationAdapter.Save.cs:46` (see caveats below) |
| 2 | B05 recipe 5101 | high | Register 5101/5102 in `ProductionRecipeRegistry.Resolve` (`ProductionRecipeRegistry.cs:49`) or route farming-job completion through `PlantingSystem.TryPlant`; log the swallowed `KeyNotFoundException` (`DefaultTickSystems.cs:~176`) |
| 3 | B06 player-inventory jobs | high | Swap `world.PlayerInventory` for the job site's stockpile/worksite store in `JobAssignmentStep.Run` (`DefaultTickSystems.cs:164-207`) |
| 4 | B07 one-shot caravans | high | Re-arm `Idle` caravans on route cadence (reset `StepsSinceDeparture`, state→`EnRoute`) in `CaravanSystem.Tick` (`CaravanSystem.cs:31`); pin `CadenceDays` meaning with a test |
| 5 | B08 zero-stock price freeze | medium | Reprice over the union of `PriceLedger`-known tags and live `Entries` in `PriceStepSystem.Run` (`DefaultTickSystems.cs:534-549`) |
| 6 | B22 unrest to first site | high | Replace first-site `FallbackSite` with nearest-site-by-position (`CascadeSystems.cs:111-117`) |
| 7 | B17 placeholder stamped fresh | high | `if (entry.RequiresGeneration && !result.IsPlaceholder)` before `GeneratedAssetProvenance.Write` (`VisibleGenerationPipeline.cs:141`, success path at `:75-82`) |
| 8 | B03 stale ownership lint | critical→high | Derive `knownIds` from `DefaultTickSystems` step ids; delete ghost `econ.trade@Daily:28` (`FieldOwnershipRegistry.cs:49`); add reverse lint (declared-but-no-writer fails) |
| 9 | B12 fizzle bypass (remnant) | high→medium | Live cast already runs the real pipeline; switch `TryExecute` → `TryExecuteWithRoll` with world-seeded deterministic RNG (`DomainSimulationAdapter.Combat.Spells.cs:82`, no-roll overload at `SpellExecutionService.cs:71`) |
| 10 | B27 snow-blind crops | medium | Derive `isSnowing` from season/weather instead of the hardcoded `false` (`DefaultTickSystems.cs:505`) |
| 11 | B18 forge cache key | medium | Fold `Width|Height|Negative` into `PromptComposers.CacheKey` (`PromptComposers.cs:47-51`) |

---

## Per-bug verdicts

### B01 — WorldViewProjector compile break — `already-fixed` (critical)
Proof: `WorldViewProjector.cs` current body — ctor assigns `_worksiteViews`/`_eventLogHud`
(`:27-28`), `ReplaceActorViews` (`:35-38`) touches only the non-readonly `_actorViews`.
W31 commit message (`1e9b474b`) explicitly records the ctor repair and adds the
Build-Finished gate to proof chains.

### B02 — Overland gone after cold load — `needs-spot-fix` (critical, P1)
**Verified live.** Chain, each link read today:
1. No save slice persists Overland — zero `Overland` refs under `Assets/Scripts/Data/Save`.
2. `RestoreStateJson` only preserves the *same-session* live map (`DomainSimulationAdapter.Save.cs:46-49`);
   on a fresh process `liveOverland` is null too.
3. Cold `Continue`: `EmberWorldHost.cs:86-97` — worldgen intent is null, `SeedWorld` never runs;
   `TryCreateDomainAdapter` (`EmberWorldHost.AdapterBootstrap.cs:30`) builds a factory world with
   `Overland == null`.
4. `WorldSceneDirector.Realize` hits the BROKEN failsafe — empty Perlin pad, no settlement
   (`WorldSceneDirector.cs:30-43`, its own comment believes this "should be unreachable").
5. Fast travel then refuses: "There is no overland world to travel"
   (`DomainSimulationAdapter.Travel.cs:34-39`); M-map and HUD tile are dead.

Not a crash — a graceful world-loss on **every fresh-process Continue** in the generated scene.

**Fix direction:** `WorldProfile` (with `Seed`, style, genre) IS saved and restored
(`WorldSaveMapper.cs:86,192`). In `RestoreStateJson`, when `Overland == null` after `CopyFrom`
and `WorldProfile != null`, re-run the exact SeedWorld path: `PlanetWorldService.GetOrGenerate(
profile.Seed, WorldgenParameters.For(profile.Style, profile.Genre))` → set `GeneratedWorld` →
`OverlandWorldgen.Generate(generated, new OverlandParameters(geo.Width, geo.Height))`
(extract a shared `RebuildWorldFromProfile()` used by both `SeedWorld` and the restore).
**Two caveats:** (a) MUST use the `Generate(GeneratedWorld, …)` overload — the `uint` overload
takes the flat non-planet path and yields a *different* map for the same seed (B28 interlock);
(b) boot order: `Realize` runs in `EmberWorldHost.Awake` BEFORE `EmberSaveService.Start`
consumes the pending load — after the restore rebuilds the map, re-run `Realize` (or move the
pending-load consumption ahead of the realize). Pin with a fresh-process save/load test.

### B03 — Ownership registry stale / fake seatbelt — `needs-spot-fix` (critical→high)
**Verified live.** `FieldOwnershipRegistry.cs:49` declares ghost writer `econ.trade@Daily:28`;
the actual registered step ids are `world.caravans@Daily:10`, `econ.shortage_response@Daily:27`,
`world.runtime_history@Daily:28` (grepped from every `: base("…")` in `DefaultTickSystems.cs`).
Fix: generate the known-id set from `DefaultTickSystems` (single source), add the reverse lint
(declared id with no real writer ⇒ fail). Mechanical, test-only blast radius.

### B04 — Critical state outside ownership — `later-slice` (high)
**Verified live.** `Writers` covers exactly 7 fields (`FieldOwnershipRegistry.cs:16-53`):
`World.Time`, `Actor.Mood` (written hourly by `NeedsStep.RecomputeMood`!), `World.Plants`
(two daily writers: `econ.plantgrowth` + `world.harvest` replant), `World.NpcMemory` (now
also written by the working-tree dialog seen-marking), `World.CompanionIds`, `World.Factions`,
and every adapter write are all uncovered. Needs a scope decision (tick+adapter+save boundary),
not a one-liner. Do after B03 makes the list derivable.

### B05 — Shortage→plant→harvest loop broken — `needs-spot-fix` (high)
**Verified live, and sharper than reported.** `ProductionRecipeRegistry` registers only
1001/1002; `Resolve` throws for 5101 (`ProductionRecipeRegistry.cs:49-54`), silently caught in
`JobAssignmentStep` (`DefaultTickSystems.cs:~172-183`, "intentionally silent" comment).
`PlantingSystem.TryPlant` still has zero production callers. **New finding:** the shortage
cascade IS live now (`ShortageResponseSystem` posts 5101 jobs daily), but
`HasPendingPlanting` (`ShortageResponseSystem.cs:74-81`) sees the forever-stuck claimed job and
suppresses ALL future shortage responses for that site — one ghost job per site freezes the
whole cascade. Economy survives only via `HarvestStep`'s inline self-replant
(`DefaultTickSystems.cs:463-465`). Fix as queued above; also un-silence the catch (once-per-id log).

### B06 — NPC production uses player inventory — `needs-spot-fix` (high)
**Verified live.** `JobAssignmentStep.Run` passes `world.PlayerInventory` into
`StartRecipeForClaim` and `TickAssignedJobs` (`DefaultTickSystems.cs:164,190,196-200`) — village
production consumes and produces in the player's bag. Fix: resolve the request's site stockpile
(`world.FindStockpile(request.SiteId)`) / worksite store as the recipe IO; keep PlayerInventory
for player-initiated crafting only (W31 smith commission already does this correctly).

### B07 — Caravans are one-shot — `needs-spot-fix` (high)
**Verified live.** `CaravanSystem.Tick` skips `Idle` (`CaravanSystem.cs:31-33`);
`Unload` sets `Idle` at payload 0 (`CaravanInstance.cs:142`); no `.Depart()`/re-arm exists
anywhere (only `WorldFactory.cs:150` seeds the first `EnRoute`). One delivery, then the route
is dead forever. Fix: in `CaravanStep` (or the system), re-depart Idle caravans —
reset `StepsSinceDeparture`, state→`EnRoute` — on route cadence; write the test that pins
whether `CadenceDays` means travel-time or departure-interval (today it is fiilen travel time).

### B08 — Price signal freezes at zero stock — `needs-spot-fix` (medium)
**Verified live.** `PriceStepSystem.Run` iterates `stockpile.Entries`
(`DefaultTickSystems.cs:534-549`); `StockpileComponent` documents "deterministic enumeration of
non-zero entries" (`StockpileComponent.cs:9-10`). Item hits 0 ⇒ drops out of `Entries` ⇒ price
stops rising at peak scarcity. Fix: reprice over `PriceLedger`'s known (site,item) pairs ∪ live
entries so zero-count items still walk up; `PriceUpdateSystem.Recompute` already takes an
arbitrary tag — only the caller loop changes.

### B09 — Everyone hungers, not everyone eats — `dies-in-W32-eat-slice` (high)
**Verified live.** `NeedsStep` ramps every alive actor including Player and Enemy
(`DefaultTickSystems.cs` NeedsStep loop); `NeedConsumptionSystem` skips
`Player || Enemy` (`NeedConsumptionSystem.cs:41`); Guards are in consumption but their
schedule targets are post-bound. The uncommitted working tree is already inside this file
(perf caching + fail-fast). This is the W32 eat slice's core material — no spot-fix here.

### B10 — Sign-step movement, no pathfinding — `later-slice` (medium)
**Verified live.** `ScheduleSystem.StepToward` is a single Chebyshev sign-step
(`ScheduleSystem.cs:191-195`) — no terrain, buildings, water, or collision.
`PathfindingSystem` exists (`Simulation/Process/PathfindingSystem.cs`) with no production
caller (B29 family). A real movement slice; interacts with W32 eating (reach checks) but the
eat slice should consume `StepToward` as-is.

### B11 — Contracts not wired to player — `already-fixed` (high)
Proof: W31 wound #7. `DomainSimulationAdapter.QuestInteraction.cs` — work-giving NPCs offer
`ContractWorkTopicId` ("work for pay", `:24-31`); `HandleContractWork` (`:59-86`) turn-ins
what can conclude, reports the blocking step honestly, else mints via `AcceptGeneratedQuest`
with a deterministic world-derived seed. Journal shows what dialog now actually drives.

### B12 — Magic design vs live line — `needs-spot-fix` for the remnant (high→medium)
**Mostly fixed since the snapshot.** Live cast runs the real domain pipeline —
`SpellExecutionService` composing Cast→Target→Effect→CastRoll services, real mutation, wards,
cooldowns (`DomainSimulationAdapter.Combat.Spells.cs:77-84`); legacy `SpellEffectCode`-only
casting is gone from the live path. **Remnant:** the adapter calls `TryExecute`, which is
`useRoll: false` (`SpellExecutionService.cs:71`) — the Tier-3 fizzle roll
(`SpellCastRollService`) is still test-only; live casts remain 100% success. One-line fix:
call `TryExecuteWithRoll` with a deterministic RNG seeded from world state (keeps replays
bit-identical). `SpellResolver` itself (terrain ops) stays dormant — that part is later-slice.

### B13 — Local LLM off in Editor, fallback null — `later-slice`, spot-fixable (medium)
**Verified live.** `USE_LLAMASHARP` appears in NO scripting define set —
`ProjectSettings.asset:590` lists only `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;USE_ONNX_RUNTIME`
(so the native path is compiled out in Editor AND Standalone). `ForgeBootstrap.cs:48`
constructs `new NativeLlmClient(modelRoot, fallback: null)` ⇒ `EmptyResponse()` for every call
(`NativeLlmClient.cs:150-154`). Cheap mitigation if wanted in W32: pass a `LocalQwenClient`
fallback in `ForgeBootstrap` (the portrait path already does this —
`DefaultNpcPortraitJsonProvider.cs:54-65`); the define decision is a build-infra call.

### B14 — Dialog topics don't live-refresh — `already-fixed` (high)
Proof: W31 wound #5. `DialogView.SetTopics` (`DialogView.cs:332-340`, "the W23 state machine
finally reaches the SCREEN") called per-change from `InGameUiController.cs:172`. The working
tree goes further (W32-pre): exhaustion refill now offers the UNSEEN remainder first via
`NpcMemory` seen-marking (`DomainSimulationAdapter.Dialog.Source.cs:56-71,176-181`).

### B15 — OptionsScreen legacy Input exception — `already-fixed`, uncommitted (critical)
Proof: working-tree `OptionsScreen.cs:53-57` — `Input.GetKeyDown(KeyCode.Escape)` removed,
`if (EmberInput.PauseDown) Close();` with the B15 comment ("threw EVERY FRAME in the
InputSystem-only player"). **Commit this with the W32 batch** — it exists only in the working tree.

### B16 — TTS `_dead` is permanent — `later-slice` (medium)
**Verified live.** `PiperSpeechSynth.cs:24` and `WindowsSpeechService.cs:18` static `_dead`
flags are set-only (no `_dead = false` anywhere); one process/backend failure mutes that
backend for the session. Per-clip retries exist, backend revival doesn't. Fix shape: timed
re-probe (e.g. allow one revival attempt after N minutes) — small but wants audio-focused
testing; not in the W32 critical path.

### B17 — Placeholder stamped as fresh cache — `needs-spot-fix` (high)
**Verified live.** Placeholder results return `success: true, isPlaceholder: true`
(`OnnxAssetForge.cs:119-130`; `AssetGenerationResult.cs:36-41` even documents the trap).
`VisibleGenerationPipeline` writes the 8x8 gray bytes AND stamps `.promptmeta` on the Success
path regardless of `IsPlaceholder` (`:75-82`, `Write` at `:139-142`) ⇒
`AssetManifestScanner.IsFresh` treats it as canonical forever — installing a real model later
never regenerates. Fix: skip `GeneratedAssetProvenance.Write` when `result.IsPlaceholder`
(the file can still be written as a visible stand-in; without the stamp the scanner retries).

### B18 — Forge cache key ignores size/negative/steps — `needs-spot-fix` (medium)
**Verified live.** `PromptComposers.CacheKey` = `Sha256(Prompt|Style|Seed)`
(`PromptComposers.cs:47-51`); changing W/H/negative leaves the key identical ⇒ stale cache
hit. Fix: fold `request.Width|Height|NegativePrompt` into the hash (accepting the one-time
cache invalidation churn). Low-risk one-liner; queued last among spot-fixes.

### B19 — recipeWorkOrders absent from ToData — `later-slice`, downgraded (high→low)
**True but intentional and safe on the production path.** `WorldSaveMapper.cs:72-74` documents
the split ("intentionally absent here" — it is a Simulation type composed by the Presentation
bridge); `JsonSliceSaveService.SaveToJson:92` fills it, and the adapter's `_saveService` IS a
`JsonSliceSaveService` bound to the live world (`DomainSimulationAdapter.cs:31,64-71`), so live
save/load round-trips orders. Residual risk: mapper-direct consumers (digest, tests) silently
drop orders. Keep as an architecture note; no W32 action.

### B20 — Empty spell list reverts to seed defaults — `later-slice`, spot-fixable (medium)
**Verified live.** `WorldSaveMapper.cs:259-261`: `Length > 0 ? saved : world.PlayerKnownSpellIds`
— and the load target is a fresh factory world whose `PlayerKnownSpellIds` are seeded
(`WorldFactory.cs:75`). A legitimately empty save resurrects the default spellbook. Fix shape:
distinguish null from empty (presence flag in `WorldSaveData`), take the array whenever the
field was written. Touches save-format compat — batch with the next save-schema change.

### B21 — Unbounded event log, capped readers — `later-slice` (medium)
**Verified live.** `WorldEventLog` is a bare growing `List` (`WorldEventLog.cs:25-55`, no cap);
save writes it all; readers silently cap (echo replay 256 at
`DomainSimulationAdapter.Clock.cs:46`, `RumorMillSystem.ScanCap = 256`). The worst producer
(per-actor needs spam) was already summarized (NeedsStep comment: ~2M events/1GB by day 90).
Needs a retention design (ring + archival digest), not a spot-fix.

### B22 — All unrest lands on the first site — `needs-spot-fix` (high)
**Verified live.** `CascadeSystems.FallbackSite` returns the first non-null record in
`world.Sites` (`CascadeSystems.cs:111-117`); used by guard-strike, predation, mauling events
and `CompanionSystem.cs:132` ⇒ in multi-site worlds every predation/crime attribution and its
unrest flows to site #1. Fix: nearest-site-by-Chebyshev(actor.Position, site centre) helper —
the attacker/victim position is in hand at every callsite.

### B23 — Presentation guesses sim decisions — `later-slice` (medium)
**Verified live.** `DomainSimulationAdapter.WorldProjection.cs:109-117` hardcodes the 12-14
lunch window with a "MUST match ScheduleSystem" comment — a mirror, not a read. Correct today,
drifts silently tomorrow. Fix shape: export the window constants from `ScheduleSystem` (or a
shared `DayRhythm`) and read the actor's actual chosen activity where possible.

### B24 — Multiple writers on actor renderer — `later-slice` (medium)
**Verified live.** Same renderer driven by `ActorView` (walk flipX `:224-229`, combat color
red/white `:251-255`), `ActorCombatFeedbackView` (flash color `:47`, death tint `:92`) and
`BillboardWalkAnimView` (flipX + localScale `:43-53`) at different cadences. Needs a
single-writer presentation contract (the visual-layer twin of REFORM #2) — design work.

### B25 — Adapter leaks mutable domain state — `later-slice` (high)
**Verified live.** `DomainSimulationAdapter.cs:94` — `public WorldState World => _world;` and
`IWorldViewReadModel` members return Domain types (e.g. `OverlandMap` at
`DomainSimulationAdapter.Overland.cs:15`). Any Presentation caller can mutate sim state outside
the tick contract. Real DTO-boundary work; do not rush in W32.

### B26 — Fallback harness ≠ Unity truth — `later-slice`, partially mitigated (medium)
**Structurally true by design:** `tools/validation/fallback` is pure C# and cannot see Unity
compile/scenes/assets. **Mitigation landed in W31:** proof chains now gate on the Build
Finished line (the exact failure mode — three "verifications" on a stale exe — is what B01's
breakage exposed, per the `1e9b474b` message). Remaining want: a Unity batch-mode
compile+EditMode leg in CI when a license runner exists.

### B27 — Crops grow while it snows — `needs-spot-fix` (medium)
**Verified live.** `PlantGrowthStep` calls growth with `isSnowing: false` hardcoded
(`DefaultTickSystems.cs:505`) while the rules support snow blocking
(`PlantGrowthRule.CanGrow`, `PlantSpeciesDef.cs:91-96`); presentation weather
(`RuntimeWeatherController`) is a separate universe. Minimal honest fix: derive
`isSnowing` deterministically from sim season (+ biome/seeded noise if wanted) at the callsite;
full weather unification is a later slice.

### B28 — Two Generate overloads, two maps — `later-slice`, downgraded (medium→low)
**Defused in production since the snapshot:** both live callers use the
`Generate(GeneratedWorld, …)` planet-pipeline overload (`DomainSimulationAdapter.Worldgen.cs:75`,
`CharacterCreationController.Transitions.cs:311`); the `uint` overload
(`OverlandWorldgen.cs:17` — flat `WorldgenService` path) is now test-only. The footgun remains
for future callers — and matters for the **B02 fix, which must use the GeneratedWorld overload**.
Cheap guard when touched: `[Obsolete]` or route the uint overload through the planet pipeline.

### B29 — Live line vs test-only line — split verdict (high)
Per component, verified today:
- **Closed since snapshot:** `ShortageDetector` logic lives in the wired
  `econ.shortage_response` step; generated-quest accept/turn-in live (B11/W31); dialog
  live-render live (B14/W31).
- **Still dormant:** `TradeService` (zero production callers), `PlantingSystem.TryPlant`
  (owned by B05's fix), `SpellResolver` terrain ops + cast roll (roll owned by B12's fix),
  `PathfindingSystem` (owned by B10).
No separate action: B05/B10/B12 carry the remainder; `TradeService` joins the caravan/economy
later slice.

### B30 — Two keybind universes — `later-slice` (medium)
**Verified live.** `InGameUiController.HandleScreenInput` (`:1498-1515`) is a fixed `KeyCode`
switch (Tab/C/I/M/J/K/R/B/T/H — B was even ADDED here in W31), while Options shows the
remappable action map — the drift is growing, not shrinking. Fix shape: route screen hotkeys
through `EmberInput` named actions so the Options map is the single truth. UX slice.

---

## Scorecard

| Status | Bugs |
|--------|------|
| already-fixed (proof) | B01, B11, B14, B15 (uncommitted — commit it) |
| dies-in-W32-eat-slice | B09 |
| needs-spot-fix (queue order) | B02, B05, B06, B07, B08, B22, B17, B03, B12, B27, B18 |
| later-slice | B04, B10, B13, B16, B19 (downgraded), B20, B21, B23, B24, B25, B26, B28 (downgraded), B30 |
| split (carried by others) | B29 |

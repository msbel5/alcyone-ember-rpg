# PRD — Living-World "Soul" (SOUL-01/02/03/04)

> Implementation PRD for a sub-agent. Branch: `main`. Deterministic-sim rules apply:
> Domain + Simulation stay Unity-free and deterministic; the LLM never mutates world
> state except via validated tool calls. NO visual-only hacks — wire the REAL systems
> (guardrail). Verify each item: `bash tools/validation/run-validation.sh --mode fallback`
> for Domain/Sim/Data; closed-Editor Win64 batchmode build for Presentation/scene/.meta.
> Commit each item separately with trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.

## 0. Goal & acceptance
Make the deterministic world visibly ADVANCE over game-time: crops grow, pending jobs get
assigned and worked, prices drift with stock, and NPCs move on daily schedules — proven by a
**headless "world changes over N ticks" EditMode test** (the real acceptance gate, not a screenshot).

## 1. Verified current state (read these before coding)

**WorldState** — `Assets/Scripts/Domain/World/WorldState.cs` (renamed from SliceWorldState).
Already carries: `Time (GameTime)`, `Actors (ActorStore)`, `Items (ItemStore)`, `Sites (SiteStore)`,
`Factions (FactionStore)`, `Events (WorldEventLog)`, `Prices (PriceLedger)`,
`Stockpiles (List<StockpileComponent>)`, `TradeRoutes`, `Caravans (List<CaravanInstance>)`,
`NpcSeeds (List<NpcSeedRecord>)`, `NpcMemory`, `Topics`, `WorldProfile`.
**MISSING from the world root** (today carried separately by the `JsonSliceSaveService` bridge and
never ticked): `ComponentStore<PlantComponent>`, `ComponentStore<SoilComponent>`, `JobBoard`,
`WorksiteStore`. **This absence is the whole SOUL-01/02 root cause.** Has `void CopyFrom(WorldState other)`.

**WorldTickComposer** — `Assets/Scripts/Simulation/Composition/WorldTickComposer.cs` (renamed from
SliceTickComposer). Constants `MinutesPerTick=1`, `TicksPerGameDay=240`, `TicksPerGameHour=10`.
Ctor injects `GameTimeAdvanceSystem`, `NeedsSystem`, `MagicTickDriver`, `CaravanSystem`.
`public void Advance(WorldState world, int tickIndex)` does: (1) `Time = _timeAdvance.Advance(...)`,
(2) magic per-tick, (3) **hourly** crossing block → `_needs.TickActorNeeds(actor, world.Events, stamp, 1)`,
(4) **daily** crossing block (`_ticksSinceDaily`/`TicksPerGameDay`). Accumulators
`_ticksSinceHourly`/`_ticksSinceDaily` + `RebuildAccumulatorsFrom(GameTime)`. **This is where the new
systems hook in.**

**Dormant systems (exist; zero production/tick callers — confirm with grep):**
- `Simulation/Process/PlantGrowthSystem.cs` → `public int AdvanceOneDay(...)` (match exact signature on read).
- `Simulation/Process/PlantingSystem.cs` → `public bool TryPlant(...)`.
- `Simulation/Process/JobAssignmentSystem.cs` → `public bool TryAssignNext(ActorStore actors, JobBoard board, WorksiteStore worksites, out JobAssignmentResult result)`.
- `Simulation/World/PriceUpdateSystem.cs` → `public void Recompute(...)`.
- `Simulation/World/FactionReputationSystem.cs` → event-driven `ApplyDelta(...)`, NOT a tick (leave out of the tick; note it).

**Stores (Domain):**
- `Domain/World/ComponentStore.cs` → `ComponentStore<T>`: `Add(WorldComponentId,T)`, `Get`, `TryGet`, `Replace`, `Remove`, `Count`.
- `Domain/Process/JobBoard.cs`: `Add(JobRequest)`, `TryPeekNext(out)`, `TryClaim(JobId,ActorId,out)`, `Count`.
- `Domain/Process/WorksiteStore.cs`: `Add(WorksiteRecord)`, `Get(SiteId,GridPosition)`, `TryGet`, `Count`.

**Actor schedule:** `Domain/Actors/ActorScheduleState.cs` — `readonly struct ActorScheduleState`:
`CurrentJobId (JobId)`, `TargetSiteId (SiteId)`, `TargetWorksitePosition (GridPosition)`, `bool IsIdle`,
factories `Idle` / `Assigned(JobId, SiteId, GridPosition)`. Stored on `ActorRecord.ScheduleState`,
**never read** (grep confirms) → there is **no ScheduleSystem**.

## 2. SOUL-01 — tick the production economy  *(Critical; headless-provable)*

1. **Add the 4 stores to `WorldState`** (Domain): fields
   `public ComponentStore<PlantComponent> Plants = new();`,
   `public ComponentStore<SoilComponent> Soils = new();`,
   `public JobBoard Jobs = new();`,
   `public WorksiteStore Worksites = new();`.
   Extend `CopyFrom(other)` to deep-copy all four (mirror the existing store-copy style; add a
   matching reflection guard in `Tests/EditMode/World/WorldStateCopyFromTests.cs`).
2. **Re-home the save mapping** onto the world root: `Data/Save/SliceJson/WorldSaveMapper.*.cs`
   already maps worksites/jobs/soils/plants — repoint `ToData`/`ToWorld` to read/write
   `world.Plants/Soils/Jobs/Worksites` instead of the `JsonSliceSaveService` side-stores; reduce
   `JsonSliceSaveService` to delegate to the world root (keep its public API for callers).
   **Keep green:** `StoreRoundTripTests`, `PlantSeasonRoundTripTests`, `JobAssignmentRoundTripTests`,
   `RecipeWorksiteRoundTripTests`.
3. **Seed something to tick** (deterministic, seed-derived): in
   `Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.cs` `HydrateSites`, when building a
   settlement, add ≥1 `WorksiteRecord` (a farm plot → `SoilComponent` + a seeded `PlantComponent`;
   plus a forge worksite) and ≥1 pending `JobRequest` on `world.Jobs`. All derived from the world seed.
4. **Wire `WorldTickComposer`** (Simulation): add ctor params `PlantGrowthSystem`,
   `JobAssignmentSystem`, `PriceUpdateSystem` (with defaults in the parameterless ctor).
   - **Daily block** (per daily crossing): `_plantGrowth.AdvanceOneDay(world.Plants, world.Soils, …)`;
     `_priceUpdate.Recompute(world.Prices, world.Stockpiles, …)`.
   - **Hourly block**: loop `while (_jobs.TryAssignNext(world.Actors, world.Jobs, world.Worksites, out var r)) { … }`;
     on each assignment set the assigned actor's `ScheduleState = ActorScheduleState.Assigned(r.JobId, r.SiteId, r.WorksitePosition)`
     and append a `WorldEvent`. Match the exact `AdvanceOneDay`/`Recompute` signatures on read.
5. **Acceptance test** (the real proof) — `Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs`:
   seed a world (fixed seed) with a plant, a soil plot, a pending job, an idle actor, a stockpile;
   `Advance` the composer over ≥ 2 game-days (≥ 480 ticks); assert **(a)** a `PlantComponent` growth
   stage increased, **(b)** the pending job is claimed AND some actor's `ScheduleState.IsIdle == false`,
   **(c)** at least one `PriceLedger` entry changed. Deterministic — same seed → same asserts.

## 3. SOUL-03 — ScheduleSystem (NPCs actually move)  *(High; headless logic + [E] visual)*
1. New `Simulation/Living/ScheduleSystem.cs`: `public sealed class ScheduleSystem { public void Advance(ActorStore actors, GameTime time) }`
   — for each actor whose `ScheduleState` is `Assigned`, step the actor's current position one tile toward
   `TargetWorksitePosition` during work hours, or toward home at night (use `time` hour-of-day). Pure Domain/Sim.
2. Confirm `ActorRecord` has a mutable current `GridPosition`; if not, add one (+ CopyFrom + round-trip test).
3. Wire into `WorldTickComposer` hourly block (after job assignment).
4. Test `Tests/EditMode/Living/ScheduleSystemTests.cs`: an `Assigned` actor's position converges toward its worksite over hours.

## 4. SOUL-04 — render worldgen NPCs (or honestly document fixed vignettes)  *([E] visual)*
1. Preferred: an ActorView spawner/sync (Presentation) instantiates a billboard view per
   `WorldState.Actors` entry at its world→scene position and updates it each tick from SOUL-03 movement.
2. Pragmatic fallback (if overworld spawning is out of scope this pass): explicitly document the 10
   authored scenes as fixed vignettes in `docs/CURRENT_STATE.md` so it is not a silent gap. **Decide with user.**
3. `[E]` Editor/screenshot proof.

## 5. SOUL-02
Not a separate defect — it is the umbrella "systems pretend to exist" symptom whose root cause is
SOUL-01's missing world-root stores. Closing SOUL-01 + the acceptance test closes SOUL-02.

## 6. Out of scope / guardrails
- Keep Domain + Simulation deterministic and Unity-free; LLM only via validated tool calls.
- No new manager/helper/god class; reuse the existing systems above.
- cuDNN/model binaries gitignored — never commit. Editor closed for batchmode builds.
- The headless "world changes over N ticks" test is the acceptance gate; a screenshot is not.

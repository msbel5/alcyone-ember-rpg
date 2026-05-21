# Faz 4 - Atom map (Colony needs)

_Date:_ 2026-05-15
_Branch:_ `agent/sprint-4-colony-needs-atom-map`
_Primary boxes:_ `[box=LIVING]`, `[box=PROCESS]`
_Support boxes:_ `[box=MATTER]`, `[box=TIME]`, `[box=PLAYABLE]`
_Thalamus packet:_ `pkt_20260515204915_e1c5ad32792f`
_Resolver:_ `sha256:b288c6a454567c4784185bc4de8dd77d791ba3aae1593887763f9bd529de2e50`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`
_Vision notes:_ `DOCS/EMBER_VISION_NOTES_MAMI.md`
_Inspector checklist:_ `DOCS/inspector-audit-checklist.md`

## Debt ledger (Faz 1/2/3 — open carry-over)

> **GATE.** Before kicking off the next atom in any rail below, Captain MUST take exactly one of the following actions against this ledger and record it in the kickoff doc:
>
> - **CLOSE** a row by landing a PR whose diff satisfies that row's **Exit proof**. Row status flips to `closed` after merge.
> - **ADVANCE** a row by landing a PR that makes partial progress (test fixture, interface scaffold, refactor that unblocks the eventual close). Row status flips to `advanced`. A row may be `advanced` at most twice in a row before the next PR against it must `close`.
> - **DEFER** a row to a specific later faz (`deferred-to-faz-N`) with a one-sentence reason tied to the current bundle. Captain may not defer the same row twice.
>
> An untouched ledger across two consecutive PRs (both PRs report `Carry-over debt row advanced: none-ledger-empty` while rows remain `open`) triggers `agent-rules-v2.md` Rule 8 halt.

| ID | Owner file / class | Primary box | Status | One-line scope | Exit proof (what makes this row `closed`) | Faz origin |
|---|---|---|---|---|---|---|
| CO-01 | `Assets/Scripts/Domain/World/IPathfinder.cs` :: `IPathfinder` | PROCESS | open | Deterministic grid pathfinder interface. Actors currently teleport to `WorksitePosition`; Faz 3 deliverable "Pathing to the worksite" was logical-only. | PR adds the interface file + at least one xUnit contract test pinning `TryFindPath` determinism for one fixture map. | Faz 3 |
| CO-02 | `Assets/Scripts/Simulation/World/GridPathfinder.cs` :: `GridPathfinder` | PROCESS | open | Deterministic A* impl over `GridPosition`. Depends on CO-01. | PR adds the concrete class + at least one xUnit test where the same `PathRequest` produces byte-equal `PathResult.Steps` across two seeded runs. | Faz 3 |
| CO-03 | `Assets/Scripts/Simulation/Process/PathfindingSystem.cs` :: `Tick` | PROCESS | open | Steps assigned actors one cell per tick toward worksite queue position. | PR adds `Tick(ActorStore, JobBoard, WorldEventLog)` + a test where an actor's `Position` advances one cell per tick until `WorksiteSlot.QueuePosition` is reached, emitting `actor_stepped` events. | Faz 3 |
| CO-04 | `Assets/Scripts/Domain/Process/JobBoard.cs` :: `GetQueueIndex` | PROCESS | closed (PR #138) | Deterministic queue order when multiple actors target the same worksite — landed as `JobBoard.GetQueueIndex(JobId)` ranking by claim sequence per `(SiteId, WorksitePosition)`. | PR added `JobAssignmentQueueIndexTests.cs` covering unclaimed=-1, two-actor order, three-actor non-id order, per-worksite isolation, empty=-1. | Faz 3 |
| CO-05 | `Assets/Scripts/Domain/Process/JobStatus.cs` :: `JobStatus` | PROCESS | closed (PR #137) | Stable-string lifecycle value object: `pending / assigned / traveling / queued / active / completed / canceled / blocked(reason)`. Replaces current binary claimed-flag. Faz 11 Atom 1 spec. | PR added the value-object file + `JobStatusTests.cs` pinning all eight codes + `JobBoardTests.TryClaim_ClaimsJobAndSkipsClaimedRows` migrated to assert `GetStatus == Assigned`. | Faz 11 spec |
| CO-06 | `Assets/Scripts/Domain/Process/WorksiteSlot.cs` :: `WorksiteSlot` | PROCESS | open | `SiteId + Position + WorksiteTag + QueuePosition` value type. Faz 11 Atom 8. | PR adds the value type + factory `FromWorksite(WorksiteRecord, tag, queuePosition)` + a test pinning the factory output for one worksite fixture. | Faz 11 spec |
| CO-07 | (data row) :: `BakeBread` production recipe | PROCESS | open | Promote `RecipeFixtureCatalog.BakeBread` (test-only) into a production data row. Faz 3 deliverable "A second recipe so jobs compete" landed as test fixture only. | PR ships a non-test `RecipeDef` data row for `BakeBread` registered in the production recipe registry + a test where a deterministic day produces both ingot and bread from competing jobs. | Faz 3 |
| CO-08 | `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs` :: `Evaluate(memoryPressure)` overload | LIVING | closed | Removed the `memoryPressure` parameter from mood derivation. No in-sprint consumer; Memory is Faz 9 (Phase fence). | PR deletes the overload OR removes the parameter + all call sites updated + existing `NeedMoodEvaluatorTests` still green. | Faz 4 self-correction |
| CO-09 | `Assets/Scripts/Domain/Narrative/AskAboutTopic.cs` + Simulation Narrative iskelet | AI/DM | closed (DOCS/kickoff-faz-9.md) | `AskAboutTopic`, `AskAboutService`, `AskDmService`, `NpcMemoryQueryService`, `GuardInteractionService`, `ThinkService` audit landed: 4 refactor (AskAboutTopic, AskAboutService, GuardInteractionService, NpcMemoryQueryService), 2 deprecate (AskDmService, ThinkService). | Faz 9 atom map kickoff doc explicitly cites each of the 6 files with one of `reuse / refactor / deprecate` decisions per file. | Sprint 1 |

> **Tagging schema** (per `DOCS/mechanic-map-v1.md` "exactly one box"): each row carries exactly one `primary_box` from `{ TIME, WORLD, LIVING, MATTER, PROCESS, SOCIETY, CRPG, AI/DM }`. Optional cross-cutting tags (`infra`, `meta`, `playable`) live in row commentary, not in the `primary_box` column. **Note:** the rail sections of this file below were authored before this schema landed and contain legacy multi-box tags like `[box=TIME][box=LIVING]`; those rows are grandfathered. New rows added to this or any future atom map obey one-box.

## PR audit fields (mandatory PR body section)

Captain writes these six lines into the body of every Captain-authored PR. Inspector rejects any PR missing any line per `DOCS/inspector-audit-checklist.md` checklist A.

```
Primary box: <one of TIME|WORLD|LIVING|MATTER|PROCESS|SOCIETY|CRPG|AI/DM>
Visible proof artifact: <path to test / log / snapshot / event row in the diff, OR "none-this-is-foundational" + CO row ID>
New enum / helper / class added: <yes-with-same-PR-consumer-at-PATH | yes-deferred-to-PR#... | no>
Carry-over debt row advanced: <CO-XX-closed | CO-XX-advanced | CO-XX-deferred-to-faz-N | none-ledger-empty>
Why this is the next bundle: <one sentence tying to ledger + atom map>
Phase fences honored: <yes | called-out-violation-because-...>
```

State vocabulary for `Carry-over debt row advanced`:

- `CO-XX-closed` — the PR diff satisfies the row's Exit proof. Inspector verifies against the Debt ledger before merge.
- `CO-XX-advanced` — partial progress made on the row (e.g. test only, interface scaffold). Limit two consecutive `advanced` reports per row; the third PR against that row must `close` it.
- `CO-XX-deferred-to-faz-N` — moved to a specific later faz with one-sentence reason. Same row may not be deferred twice.
- `none-ledger-empty` — only valid when every row in the active Debt ledger is `closed` or `deferred`. Misusing this value triggers Rule 8 immediately.

If `Visible proof artifact` is `none-this-is-foundational`, the `Carry-over debt row advanced` field MUST be `closed` or `advanced`. Two consecutive PRs with `none-this-is-foundational` and `Carry-over debt row advanced: none-ledger-empty` triggers Rule 8 halt.

## Sprint goal

Faz 4 adds pressure to the living-world loop: actors get hungry and tired,
mood drops when needs are ignored, work can be refused, and recovery paths
restore the actor through food and rest.

Target acceptance sentence from `docs/ROADMAP.md`:

`player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

This atom map decomposes Faz 4 against the 8-box mechanic map before gameplay
implementation begins. Each unchecked row should fit one narrow PR, or a 3-5
atom bundle when the atoms are the same shape and stay in one sub-area.

Format: `- [ ] file/path :: scope :: brief responsibility [box=...]`.

## Atomic decomposition

### 0. Sprint map / kickoff metadata

- [x] `DOCS/sprint-faz-4-atom-map.md` :: `Faz4AtomMap` :: define LIVING/PROCESS atom graph, bundles, and promotion gate [box=LIVING]
- [x] `DOCS/sprint-4-colony-needs-atom-map.md` :: `Faz4Kickoff` :: record kickoff validation, packet/resolver, product-visible count, and next atom [box=LIVING]

### 1. Pure needs component rail

- [x] `Assets/Scripts/Domain/Actors/NeedKind.cs` :: `NeedKind` :: seed hunger/fatigue/thirst categories with `None` sentinel and no speculative extras [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/NeedKindTests.cs` :: tests :: pin `NeedKind` sentinel and named seed values [box=LIVING]
- [x] `Assets/Scripts/Domain/Actors/NeedValue.cs` :: `NeedValue` :: bounded 0-100 scalar for pressure where higher means worse need [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/NeedValueTests.cs` :: tests :: pin clamp, threshold, and comparison semantics [box=LIVING]
- [x] `Assets/Scripts/Domain/Actors/ActorNeeds.cs` :: `ActorNeeds` :: actor component carrying hunger, fatigue, thirst, and immutable update helpers [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/ActorNeedsTests.cs` :: tests :: pin defaults, per-need updates, and defensive normalization [box=LIVING]
- [x] `Assets/Scripts/Domain/Actors/ActorRecord.cs` :: `ActorRecord.ApplyNeeds` :: update needs through actor records without changing identity or role shims [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/ActorRecordNeedsTests.cs` :: tests :: pin ActorStore-compatible needs replacement on records [box=LIVING]

### 2. Mood derivation rail

- [x] `Assets/Scripts/Domain/Actors/ActorMood.cs` :: `ActorMood` :: bounded 0-100 mood value where lower mood means less willing to work [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/ActorMoodTests.cs` :: tests :: pin clamp, neutral default, low-mood threshold, and equality [box=LIVING]
- [x] `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs` :: `Evaluate` :: derive mood from needs plus existing memory pressure without mutating world state [box=LIVING]
- [x] `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs` :: tests :: hunger/fatigue lower mood deterministically and neutral needs preserve baseline [box=LIVING]
- [x] `Assets/Scripts/Domain/Actors/ActorRecord.cs` :: `ActorRecord.ApplyMood` :: store derived mood on actor records without new `SliceWorldState` named fields [box=LIVING]
- [x] `Assets/Tests/EditMode/Actors/ActorRecordMoodTests.cs` :: tests :: pin mood update and identity preservation through ActorStore [box=LIVING]

### 3. Needs tick rail

- [x] `Assets/Scripts/Simulation/Living/NeedsSystem.cs` :: `TickNeeds` :: advance hunger and fatigue by deterministic tick rates [box=TIME][box=LIVING]
- [x] `Assets/Tests/EditMode/Living/NeedsSystemTests.cs` :: tests :: repeated ticks raise hunger/fatigue and clamp at maximum [box=TIME][box=LIVING]
- [x] `Assets/Scripts/Simulation/Living/NeedsSystem.cs` :: `RecomputeMood` :: recalculate mood after needs changes through `NeedMoodEvaluator` [box=LIVING]
- [x] `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs` :: tests :: three in-game days without food lowers mood under refusal threshold [box=TIME][box=LIVING]
- [x] `Assets/Scripts/Domain/World/WorldEventKind.cs` :: `WorldEventKind.NeedChanged` :: add only when `NeedsSystem` emits the event in the same PR [box=LIVING][box=PLAYABLE]
- [x] `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs` :: tests :: pin need-change reason traces with actor id and tick anchors [box=LIVING][box=PLAYABLE]

### 4. Eat / sleep recovery rail

- [x] `Assets/Scripts/Domain/Process/NeedRecoveryRecipe.cs` :: `NeedRecoveryRecipe` :: pure recovery definition for eat/sleep actions and need deltas [box=PROCESS][box=LIVING]
- [x] `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs` :: tests :: reject empty ids, non-recovery deltas, and missing action kind [box=PROCESS][box=LIVING]
- [x] `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs` :: `EatMeal` :: consume one food item from inventory and reduce hunger [box=PROCESS][box=MATTER][box=LIVING]
- [x] `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs` :: tests :: meal consumption lowers hunger, preserves inventory on failure, and logs reason trace [box=PROCESS][box=MATTER][box=LIVING]
- [x] `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs` :: `Sleep` :: reduce fatigue through a deterministic rest action without inventory mutation [box=PROCESS][box=LIVING]
- [x] `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs` :: tests :: sleep lowers fatigue and recomputes mood deterministically [box=PROCESS][box=LIVING]

### 5. Work refusal integration rail

- [x] `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs` :: `CanActorWorkJob` :: reject actors whose hunger or mood crosses refusal threshold [box=PROCESS][box=LIVING]
- [ ] `Assets/Tests/EditMode/Process/JobNeedsRefusalTests.cs` :: tests :: hungry low-mood actor refuses a smith job without claiming the board row [box=PROCESS][box=LIVING]
- [ ] `Assets/Scripts/Domain/World/WorldEventKind.cs` :: `WorldEventKind.JobRefused` :: add only with a concrete `JobAssignmentSystem` emitter [box=PROCESS][box=PLAYABLE]
- [ ] `Assets/Tests/EditMode/Process/JobRefusalEventLogTests.cs` :: tests :: pin refusal event reason trace and preserved pending job state [box=PROCESS][box=PLAYABLE]

### 6. Save/load and replay proof rail

- [x] `Assets/Scripts/Data/Save/SliceSaveData.cs` :: actor needs/mood data :: persist needs and mood through canonical actor store data [box=TIME][box=LIVING]
- [x] `Assets/Scripts/Data/Save/ActorSaveMapper.cs` :: needs/mood mapper :: round-trip actor needs and mood without legacy named-field expansion [box=TIME][box=LIVING]
- [x] `Assets/Tests/EditMode/Save/ActorNeedsRoundTripTests.cs` :: tests :: save/load preserves hunger, fatigue, thirst, and mood [box=TIME][box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/ColonyNeedsAcceptanceReplayTests.cs` :: acceptance replay :: three days unfed lowers mood, refuses work, meal recovery permits work again [box=PLAYABLE]
- [ ] `DOCS/sprint-faz-4-colony-needs-acceptance.md` :: `Faz4AcceptanceProof` :: deterministic replay note with final `player can ...` sentence [box=PLAYABLE]

## Suggested bundles

1. `needs-primitives`: NeedKind, NeedValue, ActorNeeds, focused tests.
2. `mood-evaluator`: ActorMood, NeedMoodEvaluator, ActorRecord mood update, tests.
3. `needs-tick-event-log`: NeedsSystem tick/mood recompute plus NeedChanged event tests.
4. `eat-sleep-recovery`: NeedRecoveryRecipe, NeedRecoverySystem eat/sleep paths, tests.
5. `job-refusal`: JobAssignmentSystem refusal guard plus JobRefused event proof.
6. `needs-save-playable-proof`: save/load round-trip and final acceptance replay.

## Promotion checklist

- [ ] Every Faz 4 atom row above is checked off.
- [ ] Each sub-area has at least one merged PR: needs primitives, mood derivation, needs tick, recovery, job refusal, save/playable proof.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero; at least one PR emits a new EventLog line, replay log, debug HUD dump, or final acceptance proof.
- [ ] Final sprint summary records final atom count, bundle count, product-visible PR count, validation evidence, and the `player can ...` acceptance sentence.
- [ ] Faz 4 promotion summary reports **Debt ledger status**: which CO rows closed during Faz 4, which advanced, which were deferred-to-faz-5 (and why), which carry into Faz 5 atom map's debt ledger. An unaddressed `open` row in the ledger at promotion time is grounds for promotion rejection unless explicitly deferred.

## This atom map

- [x] `DOCS/sprint-faz-4-atom-map.md` :: this file :: canonical Faz 4 decomposition required before the first needs gameplay atom [box=meta]

## Next increment

Implement the `job-refusal` bundle next: add the refusal guard in
`JobAssignmentSystem.CanActorWorkJob`, prove hungry/low-mood actors do not
claim pending work, and emit a concrete refusal EventLog trace only with the
guard consumer.

> **Before kicking off this increment**, Captain reviews the **Debt ledger** at the top of this file and either (a) closes one CO row in the kickoff PR by satisfying its Exit proof, (b) advances one CO row with partial progress and notes the next required step, or (c) marks one CO row `deferred-to-faz-N` with a one-sentence reason. The kickoff doc records the action. The mandatory **PR audit fields** block is included in every PR body that lands as part of this bundle.

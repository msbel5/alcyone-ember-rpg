# Faz 3 — Atom map (Job assignment)

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-job-assignment-atom-map`
_Box:_ `[box=PROCESS]` / `[box=LIVING]` with `[box=MATTER]`, `[box=TIME]`, `[box=WORLD]`
_Thalamus packet:_ `pkt_20260514123115_29f1c700862d`
_Resolver:_ `sha256:58a8f09009a23e253fc532730e6b8f560ee1f8847deac455cc24a17e49577d99`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`

## Sprint goal

Faz 3 turns the Faz 2 recipe/worksite slice into actor-driven job assignment.
The target acceptance sentence is:

`player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

This atom map decomposes Faz 3 against the 8-box living-world mechanic map. It
is the sprint codebook: each unchecked row is small enough for one narrow PR or
a 3-5 atom bundle when the atoms share the same shape.

## Atomic decomposition

### 0. Sprint map / promotion metadata

- [x] DOCS/sprint-faz-3-atom-map.md :: Faz3AtomMap :: define PROCESS/LIVING atom graph, bundles, and promotion gate [box=PROCESS]
- [x] DOCS/sprint-faz-3-atom-map-kickoff.md :: Faz3Kickoff :: record kickoff validation, files changed, packet/resolver, next atom [box=PROCESS]

### 1. Pure job definition rail

- [x] Assets/Scripts/Domain/Process/JobId.cs :: JobId :: ulong-backed value handle with empty sentinel and debug string [box=PROCESS]
- [x] Assets/Tests/EditMode/Process/JobIdTests.cs :: JobIdTests :: pin equality, empty sentinel, hash, and ToString [box=PROCESS]
- [x] Assets/Scripts/Domain/Process/JobKind.cs :: JobKind :: typed job category seed (`None`, `Smith`) with no speculative extras [box=PROCESS]
- [x] Assets/Scripts/Domain/Process/JobPriority.cs :: JobPriority :: validated priority value where lower number wins and disabled priority is explicit [box=LIVING]
- [x] Assets/Tests/EditMode/Process/JobPriorityTests.cs :: JobPriorityTests :: pin disabled/active ordering and invalid values [box=LIVING]

### 2. Job board state

- [ ] Assets/Scripts/Domain/Process/JobRequest.cs :: JobRequest :: immutable work request binding recipe/worksite/site/quantity/requester [box=PROCESS]
- [ ] Assets/Tests/EditMode/Process/JobRequestTests.cs :: JobRequestTests :: reject empty ids, inactive quantity, missing worksite, and None job kind [box=PROCESS]
- [ ] Assets/Scripts/Domain/Process/JobBoard.cs :: JobBoard.Add :: register deterministic pending jobs in insertion order [box=PROCESS]
- [ ] Assets/Scripts/Domain/Process/JobBoard.cs :: JobBoard.TryPeekNext :: select next unclaimed job by priority then insertion order [box=PROCESS]
- [ ] Assets/Scripts/Domain/Process/JobBoard.cs :: JobBoard.TryClaim :: mark one job claimed by one actor without duplicate claims [box=PROCESS]
- [ ] Assets/Scripts/Domain/Process/JobBoard.cs :: JobBoard.Complete/Cancel :: remove terminal jobs and keep deterministic order stable [box=PROCESS]
- [ ] Assets/Tests/EditMode/Process/JobBoardTests.cs :: JobBoardTests :: pin add/peek/claim/complete/cancel ordering and duplicate guards [box=PROCESS]

### 3. Actor job preference and schedule rail

- [ ] Assets/Scripts/Domain/Actors/ActorJobPreference.cs :: ActorJobPreference :: actor-local job kind + priority row [box=LIVING]
- [ ] Assets/Scripts/Domain/Actors/ActorScheduleState.cs :: ActorScheduleState :: current job id, current worksite target, idle state [box=LIVING]
- [ ] Assets/Scripts/Domain/Actors/ActorRecord.cs :: ActorRecord.ApplyJobPreferences :: replace deterministic actor job preferences without changing actor identity [box=LIVING]
- [ ] Assets/Scripts/Domain/Actors/ActorRecord.cs :: ActorRecord.ApplyScheduleState :: update current job/schedule state through ActorStore records [box=LIVING]
- [ ] Assets/Tests/EditMode/Actors/ActorJobPreferenceTests.cs :: ActorJobPreferenceTests :: pin preference normalization and disabled priority handling [box=LIVING]
- [ ] Assets/Tests/EditMode/Actors/ActorScheduleStateTests.cs :: ActorScheduleStateTests :: pin idle/assigned transitions and empty job rejection [box=LIVING]

### 4. Assignment system

- [ ] Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs :: TryAssignNext :: match available actors to eligible JobBoard entries by actor priority [box=PROCESS]
- [ ] Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs :: CanActorWorkJob :: require living actor, matching preference, active worksite, and available recipe inputs [box=LIVING]
- [ ] Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs :: StartRecipeForClaim :: call RecipeSystem.TryStart after claim and store active RecipeWorkOrder [box=PROCESS]
- [ ] Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs :: TickAssignedJobs :: advance active recipe orders and complete board entries when RecipeCompleted fires [box=PROCESS]
- [ ] Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs :: JobAssignmentSystemTests.AssignsTwoSmithsDeterministically :: two smith actors claim furnace jobs in stable order [box=LIVING]
- [ ] Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs :: JobAssignmentSystemTests.IgnoresDisabledOrMismatchedActors :: idle fallback when no preference matches [box=LIVING]

### 5. Second recipe and competition proof

- [ ] Assets/Tests/EditMode/Process/RecipeFixtureCatalog.cs :: RecipeFixtureCatalog.SmeltIronIngot :: canonical smelting fixture for JobAssignment tests [box=MATTER]
- [ ] Assets/Tests/EditMode/Process/RecipeFixtureCatalog.cs :: RecipeFixtureCatalog.BakeBread :: second recipe fixture so job priorities compete [box=MATTER]
- [ ] Assets/Tests/EditMode/Process/JobAssignmentCompetitionTests.cs :: HigherPrioritySmithWinsFurnace :: Smith priority beats BakeBread when furnace/inputs match [box=PROCESS]
- [ ] Assets/Tests/EditMode/Process/JobAssignmentCompetitionTests.cs :: BreadJobWaitsWithoutBakery :: mismatched worksite leaves bread job pending [box=PROCESS]

### 6. Event log, save/load, playable proof

- [ ] Assets/Scripts/Domain/World/WorldEventKind.cs :: WorldEventKind.JobAssigned :: concrete consumer emits assignment event; no speculative event kinds [box=WORLD]
- [ ] Assets/Scripts/Domain/World/WorldEventKind.cs :: WorldEventKind.JobCompleted :: concrete consumer emits completion event linked to RecipeCompleted [box=WORLD]
- [ ] Assets/Tests/EditMode/Process/JobEventLogTests.cs :: JobEventLogTests :: pin JobAssigned/JobCompleted reason traces and actor/site anchors [box=WORLD]
- [ ] Assets/Scripts/Data/Save/SliceSaveData.cs :: SliceSaveData.jobs :: persist JobBoard entries and actor schedule job state [box=PROCESS]
- [ ] Assets/Scripts/Data/Save/SliceSaveMapper.cs :: SliceSaveMapper.ToData/ApplyJobs :: round-trip job board and actor schedule state without new SliceWorldState named fields [box=PROCESS]
- [ ] Assets/Tests/EditMode/Save/JobAssignmentRoundTripTests.cs :: JobAssignmentRoundTripTests :: save/load preserves pending and active jobs [box=PROCESS]
- [ ] DOCS/sprint-faz-3-job-assignment-acceptance.md :: Faz3AcceptanceProof :: deterministic replay note with `player can ...` sentence [box=PLAYABLE]

## Suggested bundles

1. `job-primitives`: JobId, JobKind, JobPriority, focused tests.
2. `job-board`: JobRequest, JobBoard add/peek/claim/complete/cancel, tests.
3. `actor-job-state`: ActorJobPreference, ActorScheduleState, ActorRecord integration, tests.
4. `assignment-system`: JobAssignmentSystem assignment/start/tick, tests.
5. `competition-proof`: BakeBread fixture + competition tests.
6. `job-save-proof`: event kinds, save/load mapping, acceptance replay note.

## Promotion checklist

- [ ] Every atom row above is checked off.
- [ ] Each sub-area has at least one merged PR: pure job rail, job board, actor job state, assignment system, competition proof, save/playable proof.
- [ ] `./tools/validation/run-validation.sh --mode fallback` passes on the promotion branch.
- [ ] Product-visible PR count is greater than zero; at least one PR emits a new EventLog line or produces the deterministic player-facing acceptance proof.
- [ ] Final sprint summary records final atom count, bundle count, product-visible PR count, validation evidence, and the `player can ...` acceptance sentence.

## Next increment

Continue with the `job-board` bundle: add `JobRequest`, `JobBoard.Add`, `TryPeekNext`, `TryClaim`, terminal removal, and focused ordering/duplicate-guard tests. Keep event kinds out until `JobAssignmentSystem` consumes them.

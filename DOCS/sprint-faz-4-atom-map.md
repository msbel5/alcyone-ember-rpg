# Faz 4 - Atom map (Colony needs)

_Date:_ 2026-05-15
_Branch:_ `agent/sprint-4-colony-needs-atom-map`
_Primary boxes:_ `[box=LIVING]`, `[box=PROCESS]`
_Support boxes:_ `[box=MATTER]`, `[box=TIME]`, `[box=PLAYABLE]`
_Thalamus packet:_ `pkt_20260515204915_e1c5ad32792f`
_Resolver:_ `sha256:b288c6a454567c4784185bc4de8dd77d791ba3aae1593887763f9bd529de2e50`
_Mechanic map:_ `DOCS/mechanic-map-v1.md`
_Agent rules:_ `DOCS/agent-rules-v2.md`

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

- [ ] `Assets/Scripts/Domain/Actors/ActorMood.cs` :: `ActorMood` :: bounded 0-100 mood value where lower mood means less willing to work [box=LIVING]
- [ ] `Assets/Tests/EditMode/Actors/ActorMoodTests.cs` :: tests :: pin clamp, neutral default, low-mood threshold, and equality [box=LIVING]
- [ ] `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs` :: `Evaluate` :: derive mood from needs plus existing memory pressure without mutating world state [box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs` :: tests :: hunger/fatigue lower mood deterministically and neutral needs preserve baseline [box=LIVING]
- [ ] `Assets/Scripts/Domain/Actors/ActorRecord.cs` :: `ActorRecord.ApplyMood` :: store derived mood on actor records without new `SliceWorldState` named fields [box=LIVING]
- [ ] `Assets/Tests/EditMode/Actors/ActorRecordMoodTests.cs` :: tests :: pin mood update and identity preservation through ActorStore [box=LIVING]

### 3. Needs tick rail

- [ ] `Assets/Scripts/Simulation/Living/NeedsSystem.cs` :: `TickNeeds` :: advance hunger and fatigue by deterministic tick rates [box=TIME][box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/NeedsSystemTests.cs` :: tests :: repeated ticks raise hunger/fatigue and clamp at maximum [box=TIME][box=LIVING]
- [ ] `Assets/Scripts/Simulation/Living/NeedsSystem.cs` :: `RecomputeMood` :: recalculate mood after needs changes through `NeedMoodEvaluator` [box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs` :: tests :: three in-game days without food lowers mood under refusal threshold [box=TIME][box=LIVING]
- [ ] `Assets/Scripts/Domain/World/WorldEventKind.cs` :: `WorldEventKind.NeedChanged` :: add only when `NeedsSystem` emits the event in the same PR [box=LIVING][box=PLAYABLE]
- [ ] `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs` :: tests :: pin need-change reason traces with actor id and tick anchors [box=LIVING][box=PLAYABLE]

### 4. Eat / sleep recovery rail

- [ ] `Assets/Scripts/Domain/Process/NeedRecoveryRecipe.cs` :: `NeedRecoveryRecipe` :: pure recovery definition for eat/sleep actions and need deltas [box=PROCESS][box=LIVING]
- [ ] `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs` :: tests :: reject empty ids, non-recovery deltas, and missing action kind [box=PROCESS][box=LIVING]
- [ ] `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs` :: `EatMeal` :: consume one food item from inventory and reduce hunger [box=PROCESS][box=MATTER][box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs` :: tests :: meal consumption lowers hunger, preserves inventory on failure, and logs reason trace [box=PROCESS][box=MATTER][box=LIVING]
- [ ] `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs` :: `Sleep` :: reduce fatigue through a deterministic rest action without inventory mutation [box=PROCESS][box=LIVING]
- [ ] `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs` :: tests :: sleep lowers fatigue and recomputes mood deterministically [box=PROCESS][box=LIVING]

### 5. Work refusal integration rail

- [ ] `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs` :: `CanActorWorkJob` :: reject actors whose hunger or mood crosses refusal threshold [box=PROCESS][box=LIVING]
- [ ] `Assets/Tests/EditMode/Process/JobNeedsRefusalTests.cs` :: tests :: hungry low-mood actor refuses a smith job without claiming the board row [box=PROCESS][box=LIVING]
- [ ] `Assets/Scripts/Domain/World/WorldEventKind.cs` :: `WorldEventKind.JobRefused` :: add only with a concrete `JobAssignmentSystem` emitter [box=PROCESS][box=PLAYABLE]
- [ ] `Assets/Tests/EditMode/Process/JobRefusalEventLogTests.cs` :: tests :: pin refusal event reason trace and preserved pending job state [box=PROCESS][box=PLAYABLE]

### 6. Save/load and replay proof rail

- [ ] `Assets/Scripts/Data/Save/SliceSaveData.cs` :: actor needs/mood data :: persist needs and mood through canonical actor store data [box=TIME][box=LIVING]
- [ ] `Assets/Scripts/Data/Save/ActorSaveMapper.cs` :: needs/mood mapper :: round-trip actor needs and mood without legacy named-field expansion [box=TIME][box=LIVING]
- [ ] `Assets/Tests/EditMode/Save/ActorNeedsRoundTripTests.cs` :: tests :: save/load preserves hunger, fatigue, thirst, and mood [box=TIME][box=LIVING]
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

## This atom map

- [x] `DOCS/sprint-faz-4-atom-map.md` :: this file :: canonical Faz 4 decomposition required before the first needs gameplay atom [box=meta]

## Next increment

Implement the `mood-evaluator` bundle next: `ActorMood`,
`NeedMoodEvaluator`, `ActorRecord.ApplyMood`, and focused tests. Keep it
pure Domain/Simulation and Unity-free.

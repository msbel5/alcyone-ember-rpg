# Sprint 4 - Eat / sleep recovery

## Goal

Implement the fourth Faz 4 code bundle from `DOCS/sprint-faz-4-atom-map.md`:
deterministic hunger recovery through meals, fatigue recovery through sleep,
and concrete recovery reason traces.

## Files changed

- `Assets/Scripts/Domain/Process/NeedRecoveryRecipe.cs`
- `Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs`
- `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs`
- `Assets/Scripts/Simulation/Living/NeedsSystem.cs`
- `Assets/Tests/EditMode/Process/NeedRecoveryRecipeTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemEatTests.cs`
- `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs`
- `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs`
- `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs`
- `DOCS/sprint-faz-4-atom-map.md`
- `DOCS/sprint-4-eat-sleep-recovery.md`

## Behaviour

`NeedRecoveryRecipe` defines one recovery action and one need pressure to
reduce. `NeedRecoverySystem.EatMeal` consumes one stackable meal only after it
knows hunger can drop; missing food preserves inventory, actor needs, mood, and
EventLog state. `NeedRecoverySystem.Sleep` reduces fatigue without inventory
mutation. Both successful actions recompute mood and append a
`WorldEventKind.NeedChanged` recovery trace.

## Debt ledger

Carry-over debt row advanced: `CO-08-closed`.

`NeedMoodEvaluator` no longer accepts `memoryPressure`; Faz 4 mood derivation
now uses only current needs. Memory pressure is reserved for the later Faz 9
memory/dialogue rail.

## Product-visible count

Product-visible PR count for this increment: 1. The visible proof is a
`NeedChanged` EventLog recovery row for meal and sleep actions with actor id,
action kind, source item/rest cause, need delta, and derived mood.

Acceptance target:
`player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-16T02:47Z with fallback harness result:
  926 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Thalamus

- packet_id: `pkt_20260516024122_aee62394e031`
- resolver_key: `sha256:c906e7c812eb6817937bf432c8edab6b26a4ac0d2292b81d27af4cc14ba0bf96`
- AoT session: `pkt_20260516024122_aee62394e031` (AoT tool accepted atoms; best_conclusion returned null)

## Next increment

Implement the `job-refusal` bundle: `JobAssignmentSystem.CanActorWorkJob`
refuses actors whose hunger or mood crosses the threshold, keeps the job
pending, and emits a concrete `JobRefused` EventLog trace.

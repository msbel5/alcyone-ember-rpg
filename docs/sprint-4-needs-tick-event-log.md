# Sprint 4 - Needs tick EventLog

## Goal

Implement the third Faz 4 code bundle from `docs/sprint-faz-4-atom-map.md`:
deterministic needs ticking, mood recomputation, and a concrete `NeedChanged`
EventLog trace.

## Files changed

- `Assets/Scripts/Domain/World/WorldEventKind.cs`
- `Assets/Scripts/Simulation/Living/NeedsSystem.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemTests.cs`
- `Assets/Tests/EditMode/Living/NeedsSystemMoodTests.cs`
- `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs`
- `docs/sprint-faz-4-atom-map.md`
- `docs/sprint-4-needs-tick-event-log.md`

## Behaviour

`NeedsSystem` advances hunger and fatigue by deterministic tick rates, leaves
thirst unchanged for the current atom, recomputes mood through
`NeedMoodEvaluator`, and appends one `WorldEventKind.NeedChanged` event when
the EventLog overload is used. The bundle deliberately avoids recovery,
inventory consumption, save/load mapping, and job refusal.

## Product-visible count

Product-visible PR count for this increment: 1. The new visible proof is a
`NeedChanged` EventLog row with actor id, tick count, time anchor, need deltas,
and derived mood in the reason trace.

Acceptance target: `player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T22:25Z with fallback harness result:
  915 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Thalamus

- packet_id: `pkt_20260515222424_55bc617dfa07`
- resolver_key: `sha256:0129bb9f7b351ce19fdf6020e585cee54533fdc7799898b9f9ee4139d15d50dc`
- AoT session: `emspr-20260516-needs-event-log`

## Next increment

Implement the `eat-sleep-recovery` bundle: `NeedRecoveryRecipe`,
`NeedRecoverySystem.EatMeal`, `NeedRecoverySystem.Sleep`, and focused tests.

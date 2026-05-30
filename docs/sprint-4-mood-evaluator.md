# Sprint 4 - Mood evaluator

## Goal

Implement the second Faz 4 code bundle from `docs/sprint-faz-4-atom-map.md`:
pure mood derivation from actor needs plus memory pressure.

## Files changed

- `Assets/Scripts/Domain/Actors/ActorMood.cs`
- `Assets/Scripts/Domain/Actors/ActorRecord.cs`
- `Assets/Scripts/Simulation/Living/NeedMoodEvaluator.cs`
- `Assets/Tests/EditMode/Actors/ActorMoodTests.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordMoodTests.cs`
- `Assets/Tests/EditMode/Living/NeedMoodEvaluatorTests.cs`
- `docs/sprint-faz-4-atom-map.md`
- `docs/sprint-4-mood-evaluator.md`

## Behaviour

Actors can now carry a bounded `ActorMood` value. `NeedMoodEvaluator` derives
mood deterministically from hunger, fatigue, thirst, and optional memory
pressure without mutating actor needs or world state. The bundle remains pure
Domain/Simulation: no ticking, save/load mapping, job refusal, EventLog output,
or new `SliceWorldState` named fields.

## Product-visible count

Product-visible PR count for this increment: 0. This is a foundational mood
derivation bundle; the next visible path is the needs tick/EventLog rail or
final deterministic acceptance replay.

Acceptance target: `player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T21:27Z with fallback harness result:
  907 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Thalamus

- packet_id: `pkt_20260515212245_f9e55de5b73d`
- resolver_key: `sha256:fe157f2745f5b913899eb9ceacb7d9a8f14f31dcda98d391bd65bc4aaf015d66`
- AoT session: `pkt_20260515212245_f9e55de5b73d`

## Next increment

Implement the `needs-tick-event-log` bundle: `NeedsSystem.TickNeeds`,
`NeedsSystem.RecomputeMood`, `WorldEventKind.NeedChanged`, and focused tests.

# Sprint 4 - Needs primitives

## Goal

Implement the first Faz 4 code bundle from `DOCS/sprint-faz-4-atom-map.md`:
pure actor-local needs primitives with focused tests.

## Files changed

- `Assets/Scripts/Domain/Actors/NeedKind.cs`
- `Assets/Scripts/Domain/Actors/NeedValue.cs`
- `Assets/Scripts/Domain/Actors/ActorNeeds.cs`
- `Assets/Scripts/Domain/Actors/ActorRecord.cs`
- `Assets/Tests/EditMode/Actors/NeedKindTests.cs`
- `Assets/Tests/EditMode/Actors/NeedValueTests.cs`
- `Assets/Tests/EditMode/Actors/ActorNeedsTests.cs`
- `Assets/Tests/EditMode/Actors/ActorRecordNeedsTests.cs`
- `DOCS/sprint-faz-4-atom-map.md`
- `DOCS/sprint-4-needs-primitives.md`

## Behaviour

Actors can now carry hunger, fatigue, and thirst pressure values through
`ActorNeeds`. The bundle is deliberately pure Domain: no ticking, mood
derivation, save/load mapping, job refusal, EventLog output, or new
`SliceWorldState` named fields.

## Product-visible count

Product-visible PR count for this increment: 0. This is a foundational data
model bundle; the next visible path is the needs tick/EventLog rail or final
deterministic acceptance replay.

Acceptance target: `player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T21:06Z with fallback harness result:
  894 passed, 0 failed, 0 skipped.
- Unity editor status: blocked on this machine (`not_found`), as expected for
  the fallback validation path.

## Thalamus

- packet_id: `pkt_20260515205707_88cabc212828`
- resolver_key: `sha256:d6043f0c025aa77080e959f8d8c6e1298e97d69314a3b463746e14365f1c00d9`
- AoT session: `pkt_20260515205707_88cabc212828`

## Next increment

Implement the `mood-evaluator` bundle: `ActorMood`, `NeedMoodEvaluator`,
`ActorRecord.ApplyMood`, and focused tests.

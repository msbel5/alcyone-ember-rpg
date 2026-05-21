# Sprint 3 — Job assignment system atom

Date: 2026-05-14
Branch: `agent/sprint-3-job-assignment-system`
Thalamus packet: `pkt_20260514162600_98b09de96b38`
Resolver key: `sha256:5d74228b5ba56d7e88082c28b7e95c2759f84fb545c95a6d7e8d658d155104d8`

## Increment goal

Add the first Faz 3 PROCESS/LIVING bridge: idle, alive actors with enabled
matching job preferences can claim pending `JobBoard` requests and receive an
`ActorScheduleState` target for the requested worksite.

This is intentionally only assignment. Recipe start/tick, EventLog rows,
save/load integration, and completion/cancel follow-up are later atoms.

## Files changed

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
  - Adds `JobAssignmentSystem.TryAssignNext`.
  - Adds `JobAssignmentSystem.CanActorWorkJob`.
  - Adds immutable `JobAssignmentResult`.
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
  - Pins deterministic actor/job claiming order.
  - Pins actor preference priority over actor insertion order.
  - Pins disabled/no-preference rejection.
  - Pins alive, idle, enabled-preference, active-worksite eligibility, and skipping idle actors that already hold a pending claim.

## Validation

- `./tools/validation/run-validation.sh --mode fallback`
- Result: PASS, `859/859` tests after addressing Codex P2 feedback on pre-existing actor claims.
- Unity editor status: BLOCKED locally, `unity_editor=not_found`; fallback is a
  pure C# NUnit harness, not a real Unity EditMode run.

## Product-visible count

Product-visible PR count for this increment: 1. Player-facing acceptance
sentence: world actors can now be assigned to real pending worksite jobs instead
of job requests remaining passive board rows.

## Atom-map updates

Updated `DOCS/sprint-faz-3-atom-map.md` for the completed `TryAssignNext` and focused assignment-system tests. The broader `CanActorWorkJob` recipe-input atom remains open for the claimed-job lifecycle work.

## Next increment

Start the claimed job lifecycle atom: connect claimed `JobBoard` entries to the
recipe/work order start path without broad save/load or completion changes.

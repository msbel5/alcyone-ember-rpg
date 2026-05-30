# Sprint 3 - Job event log

## Goal

Complete the Faz 3 `job-event-log` atom: job assignment and job completion now have explicit `WorldEventLog` rows that downstream debug HUD / replay surfaces can read without inferring state from the board.

## Files changed

- `Assets/Scripts/Domain/World/WorldEventKind.cs`
- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobEventLogTests.cs`
- `docs/sprint-faz-3-atom-map.md`
- `docs/sprint-3-job-event-log.md`

## Behaviour

- `WorldEventKind.JobAssigned` and `WorldEventKind.JobCompleted` landed with concrete `JobAssignmentSystem` consumers.
- `TryAssignNext(..., WorldEventLog, GameTime, out ...)` appends one `JobAssigned` row with actor, site, job id, and worksite reason trace when assignment succeeds.
- `TickAssignedJobs(..., WorldEventLog, GameTime, ...)` appends `JobCompleted` after the final requested recipe execution, after `RecipeCompleted` has already been logged by `RecipeSystem`.
- Legacy overloads keep existing tests stable; callers opt into job-specific event rows by passing deterministic `GameTime`.

## Product-visible count

Product-visible PR count for this increment: 1. The simulation now exposes visible event-log lines for job assignment and completion.

Acceptance sentence: player can watch a smithing job appear in the deterministic event log when it is assigned and again when the requested work completes.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at 2026-05-15T15:18Z (fallback harness: 877 passed, 0 failed, 0 skipped; Unity editor blocked: not found).

## Thalamus

- packet_id: `pkt_20260515151502_cdfc57941bc6`
- resolver_key: `sha256:98a9dd5a953e9ff59cfb9007af673af32ae4abe71aa6cef6ab830e32ed8b6e99`

## Next increment

Continue Faz 3 with the save/load bundle: persist `JobBoard` entries and actor schedule job state through `SliceSaveData` / `SliceSaveMapper` without adding new `SliceWorldState` named fields.

# Sprint 3 — TickAssignedJobs

## Goal

Complete the Faz 3 `TickAssignedJobs` atom: active `RecipeWorkOrder` rows owned by `JobAssignmentSystem` now advance through `RecipeSystem.Tick`. When that tick emits `RecipeCompleted`, the matching `JobBoard` entry is completed/removed and the claimed actor's `ActorScheduleState` is cleared back to idle.

## Files changed

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-3-tick-assigned-jobs.md`

## Behaviour

- `TickAssignedJobs(...)` ticks every active job work order once and returns the number of jobs completed on that call.
- Non-completion ticks keep the active work order, board claim, actor schedule, inventory outputs, and event log stable except for work-order progress.
- Completion ticks use `RecipeSystem.Tick` as the source of truth for `RecipeCompleted`, then remove the active order, call `JobBoard.Complete`, and idle the claimed actor if it still points at that job.

## Product-visible count

Product-visible PR count for this increment: 1. A claimed smithing job can now progress from assignment/start through recipe completion, producing the recipe output and freeing the actor for later work.

Acceptance sentence: player can have a smith claim and start a furnace recipe job, then the simulation tick completes the recipe, removes the job, and returns the smith to idle.

## Validation

- `git diff --check` — PASS at 2026-05-15T09:16Z.
- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-15T09:16Z (872 passed, 0 failed; `fallback_exit_code=0`).

## Thalamus

- packet_id: `pkt_20260515091225_eddcd5ea37ec`
- resolver_key: `sha256:fbef3d1f494b210cb1b483c1ac9b2443029b32b927082019264b80bccbdaeb45`

## Next increment

Continue Faz 3 with the `competition-proof` bundle: extract canonical smelting/bread fixtures and add focused competition tests before event-log/save atoms.

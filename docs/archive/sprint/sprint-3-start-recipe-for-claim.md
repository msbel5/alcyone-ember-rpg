# Sprint 3 — StartRecipeForClaim active work order

## Goal

Complete the Faz 3 `StartRecipeForClaim` atom: a claimed job can now start its matching recipe through `RecipeSystem.TryStart`, consume the recipe inputs, and keep exactly one active `RecipeWorkOrder` tracked by `JobId`.

## Files

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `docs/sprint-faz-3-atom-map.md`
- `docs/sprint-3-start-recipe-for-claim.md`

## Validation

- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-14T21:35Z (868 passed, 0 failed; `fallback_exit_code=0`).
- `git diff --check` — PASS at 2026-05-14T21:35Z.

## Thalamus

- packet: `pkt_20260514213245_54f6e699980c`
- resolver: `sha256:0931fc4042f29a141237370da5bd13ea4f73a9da1112b3e5466ae4ae5b59592f`

## Product-visible count

Product-visible PR count for this increment: 1. A claimed smithing job now crosses from schedule assignment into actual recipe execution state: inputs are consumed and an active work order is visible to the simulation.

Acceptance sentence: player can have a smith claim a furnace recipe job and the simulation starts that claimed recipe work instead of leaving the claim as schedule-only state.

## Next

Add the `TickAssignedJobs` atom so active `RecipeWorkOrder` progress advances and completed recipes close out the corresponding `JobBoard` entry.

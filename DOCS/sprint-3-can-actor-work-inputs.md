# Sprint 3 — CanActorWorkJob recipe input gate

## Goal

Complete the Faz 3 `CanActorWorkJob` atom by adding a mutation-free recipe/input eligibility path while preserving existing assignment-only callers.

## Files

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-3-can-actor-work-inputs.md`

## Validation

- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-14T18:04:43Z (862 passed, 0 failed; `fallback_exit_code=0`).

## Thalamus

- packet: `pkt_20260514180359_10a56d50910f`
- resolver: `sha256:7e22faa7eef5e0d557f78d0267e84965d15ab8fe32c2390c5ab8a4038b0b5dde`

## Product-visible count

Product-visible PR count for this increment: 0. This is an internal PROCESS/LIVING gate that makes later claimed job lifecycle work safe; the next atom should connect claims to `RecipeSystem.TryStart` for a visible job-progress path.

## Next

Wire the later `StartRecipeForClaim` atom so claimed jobs actually call `RecipeSystem.TryStart` and track active `RecipeWorkOrder` state.

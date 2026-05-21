# Sprint 3 — StartRecipeForClaim batch preflight review fix

## Goal

Address Codex PR #114 feedback: `StartRecipeForClaim` must prove the full requested job quantity can start before it consumes real inventory for the first active work order.

## Files

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `DOCS/sprint-3-start-recipe-for-claim-review-fix.md`

## Change

- Shared the recipe-quantity clone preflight between `CanActorWorkJob` and `StartRecipeForClaim`.
- `StartRecipeForClaim` now rejects a quantity-2 job when only one execution's inputs exist, without mutating inventory or opening an active work order.
- Added a positive batch test proving sufficient stock preflights both executions, then starts exactly one active work order and consumes one execution's inputs.

## Validation

- `git diff --check` — PASS at 2026-05-14T21:44Z.
- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-14T21:44Z (870 passed, 0 failed; `fallback_exit_code=0`).

## Thalamus

- packet: `pkt_20260514214153_582a9357e708`
- resolver: `sha256:5dff4177499cd8a62013fe68fa5f641d423b4a9c9f0bb9d9d8d262de6725b5bc`

## Product-visible count

Product-visible PR count for this increment remains 1 for PR #114. This follow-up hardens the visible job-start behavior so batch jobs cannot partially consume materials when the full requested quantity is not actually startable.

Acceptance sentence: player can queue a multi-quantity smithing job and the simulation will only begin consuming resources if the complete batch is feasible.

## Next

Let GitHub Actions finish PR #114 checks, then merge if bot review is resolved and required checks are green.

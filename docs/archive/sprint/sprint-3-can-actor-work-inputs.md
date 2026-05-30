# Sprint 3 — CanActorWorkJob recipe input gate

## Goal

Complete the Faz 3 `CanActorWorkJob` atom by adding a mutation-free recipe/input eligibility path while preserving existing assignment-only callers. Follow-up: address the PR #113 Codex P2 review by preflighting every requested `JobRequest.Quantity` execution against the cloned inventory.

## Files

- `Assets/Scripts/Simulation/Process/JobAssignmentSystem.cs`
- `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs`
- `docs/sprint-faz-3-atom-map.md`
- `docs/sprint-3-can-actor-work-inputs.md`

## Validation

- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-14T18:04:43Z (862 passed, 0 failed; `fallback_exit_code=0`).
- `git diff --check` — PASS at 2026-05-14T20:49Z.
- `./tools/validation/run-validation.sh --mode fallback` — PASS at 2026-05-14T20:49Z (864 passed, 0 failed; `fallback_exit_code=0`).

## Thalamus

- initial packet: `pkt_20260514180359_10a56d50910f`
- initial resolver: `sha256:7e22faa7eef5e0d557f78d0267e84965d15ab8fe32c2390c5ab8a4038b0b5dde`
- bot-review follow-up packet: `pkt_20260514204807_b2a20c5989e8`
- bot-review follow-up resolver: `sha256:ff795d354751c8288efe308b17bc31b2845f996dccf15940a5a2f068acafcfa0`

## Product-visible count

Product-visible PR count for this increment: 0. This is an internal PROCESS/LIVING gate that makes later claimed job lifecycle work safe; the next atom should connect claims to `RecipeSystem.TryStart` for a visible job-progress path.

## Next

Wait for PR #113 CI on the follow-up commit, then wire the later `StartRecipeForClaim` atom so claimed jobs actually call `RecipeSystem.TryStart` and track active `RecipeWorkOrder` state.

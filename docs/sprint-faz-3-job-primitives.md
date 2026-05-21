# Faz 3 — Job primitives bundle

_Date:_ 2026-05-14
_Branch:_ `agent/sprint-faz-3-job-assignment-atom-map`
_Box:_ `[box=PROCESS]` / `[box=LIVING]`
_Thalamus packet:_ `pkt_20260514125131_b2624dc540a1`
_Resolver:_ `sha256:f413bf439e471d8600699f0a8a1d667ee1482d730125ea0795cf3521175ac94d`
_Atom map:_ `DOCS/sprint-faz-3-atom-map.md`

## Increment goal

Land the first Faz 3 pure job-definition rail so later `JobRequest`,
`JobBoard`, and `JobAssignmentSystem` atoms can use stable handles,
non-speculative job categories, and deterministic actor priority ordering.

This is not a playable-proof PR yet; it is a small PROCESS/LIVING foundation
bundle under the Faz 3 atom map.

## Files changed

- `Assets/Scripts/Domain/Process/JobId.cs`
- `Assets/Scripts/Domain/Process/JobKind.cs`
- `Assets/Scripts/Domain/Process/JobPriority.cs`
- `Assets/Tests/EditMode/Process/JobIdTests.cs`
- `Assets/Tests/EditMode/Process/JobPriorityTests.cs`
- Unity `.meta` files for the new C# assets
- `DOCS/sprint-faz-3-atom-map.md`
- `DOCS/sprint-faz-3-job-primitives.md`

## Validation

- `git diff --check` — PASS
- `./tools/validation/run-validation.sh --mode fallback` — PASS
  - Unity editor: BLOCKED (`not_found`)
  - Fallback harness: PASS, 827 passed / 0 failed / 0 skipped
  - TRX: `validation-output/fallback-test-results/fallback.trx`

## Atom-map accounting

Checked off the `job-primitives` bundle:

- `JobId` value handle + tests `[box=PROCESS]`
- `JobKind` seed enum (`None`, `Smith`) `[box=PROCESS]`
- `JobPriority` active/disabled ordering + tests `[box=LIVING]`

Current Faz 3 promotion state: incomplete. The playable acceptance sentence
remains:

`player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

Product-visible PR count for Faz 3 remains `0`; later assignment/event-log atoms
must provide the first visible proof.

## Next increment

Implement the `job-board` bundle: `JobRequest`, `JobBoard.Add`,
`TryPeekNext`, `TryClaim`, terminal removal, and deterministic ordering tests.

# Sprint 4 - Faz 4 promotion

## Goal

Promote Faz 4 after the colony-needs acceptance proof and save round-trip merged
into `main`. This increment is evidence-only: it records final atom count, merged
bundle coverage, product-visible proof, validation result, Debt ledger status,
and next sprint direction.

## Files changed

- `DOCS/sprint-faz-4-atom-map.md`
- `DOCS/sprint-4-faz-4-promotion.md`

## Promotion evidence

- Final implementation atom count: every row in `DOCS/sprint-faz-4-atom-map.md`
  rails 1-6 is checked.
- Bundle count: 6 suggested bundles, 6 merged: `needs-primitives`,
  `mood-evaluator`, `needs-tick-event-log`, `eat-sleep-recovery`, `job-refusal`,
  `needs-save-playable-proof`.
- Product-visible PR count for the sprint: at least one
  `Faz 4: Acceptance replay test - hunger -> refusal -> eat -> reclaim job`
  (PR #132, then PR #133 acceptance replay), plus the save round-trip persistence
  PR (#129, #130).
- Bot-review queue: addressed inline during merge.

## Merged PR coverage (Faz 4 scope)

- `#121` sprint 4: map colony needs atoms
- `#122` sprint 4: add needs primitives
- `#123` sprint 4: add mood evaluator
- `#124` sprint 4: add needs tick event log
- `#126` sprint 4: add eat sleep recovery
- `#127` sprint 4: add job-refusal
- `#128` Faz 4: Job refusal guard + JobRefused event
- `#129` Faz 4: Save/load round-trip for actor schedule and job board
- `#130` Faz 4: Persist actor needs & mood in save + roundtrip test
- `#131` docs: mark job-refusal and save/load rows as completed
- `#132` Faz 4: Acceptance replay test - hunger -> refusal -> eat -> reclaim job
- `#133` Faz 4: Colony needs acceptance replay

## Debt ledger status (after Faz 4)

| ID | Faz origin | Status | Notes |
|---|---|---|---|
| CO-01 | Faz 3 | advanced | `IPathfinder` interface scaffold landed via the Faz 5 foundation bundle (#136 contribution); concrete impl remains in CO-02/03. |
| CO-02 | Faz 3 | open | `GridPathfinder` concrete A* still to land. Targeted for the next bundle. |
| CO-03 | Faz 3 | open | `PathfindingSystem` still to land. Depends on CO-02. |
| CO-04 | Faz 3 | closed (PR #138) | `JobBoard.GetQueueIndex` deterministic per-worksite claim order. |
| CO-05 | Faz 11 spec | closed (PR #137) | `JobStatus` stable-string lifecycle value object + JobBoard migration. |
| CO-06 | Faz 11 spec | open | `WorksiteSlot` value type. Deferred to Faz 5 carry-over. |
| CO-07 | Faz 3 | open | `BakeBread` production data row. Deferred to Faz 5 carry-over. |
| CO-08 | Faz 4 self-correction | closed | `NeedMoodEvaluator.memoryPressure` overload cleanup. |
| CO-09 | Sprint 1 | deferred-to-faz-9 | Narrative iskelet audit booked for Faz 9 kickoff. |

Three open rows (`CO-02`, `CO-03`, `CO-06`, `CO-07`) carry into Faz 5's atom
map debt ledger. Each carry-over row keeps its CO-id stable across sprints so
the audit chain remains traceable.

## Validation

- `git diff --check`: PASS on each merge.
- Local fallback validation: see PR-level summaries for exact counts
  (`./tools/validation/run-validation.sh --mode fallback`).
- GitHub Unity Tests: green on each merge to `main`.

## Acceptance sentence

`player can let an actor go three days without food, see mood fall, see them refuse to work, and recover after a meal`.

Acceptance replay tests in
`Assets/Tests/EditMode/Living/ColonyNeedsAcceptanceReplayTests.cs` exercise the
full sentence end-to-end: hunger raises, mood drops below the refusal
threshold, JobAssignment refuses, `JobRefused` event emits, meal recovery via
`NeedRecoverySystem.EatMeal` restores hunger and re-enables job reclaim.

## Next increment

Start Faz 5 atom map kickoff using `DOCS/sprint-faz-5-atom-map.md` (foundation
already landed via PR #136). Bring forward the three open Faz 4 debt rows
(CO-02 GridPathfinder, CO-03 PathfindingSystem, CO-06 WorksiteSlot, CO-07
BakeBread production data) into the Faz 5 ledger so they remain visible at
every Faz 5 kickoff.

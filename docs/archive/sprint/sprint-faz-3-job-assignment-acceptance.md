# Faz 3 - Job assignment acceptance proof

_Date:_ 2026-05-15
_Branch:_ `agent/sprint-3-job-assignment-acceptance`
_Box:_ `[box=PLAYABLE]`
_Thalamus packet:_ `pkt_20260515160510_f936c3bdc2ce`
_Resolver:_ `sha256:3704ccf03e10f2cb8881b2043e569eb7c666b8a4805ec4cc40c60a6419413e8f`
_Atom map:_ `docs/sprint-faz-3-atom-map.md`

## Acceptance sentence

`player can set 2 actors to "smith" priority 1, watch both queue at the furnace, and produce 4 ingots in a deterministic day`.

## Deterministic replay

This proof is a repo-local replay over the Faz 3 deterministic PROCESS/LIVING
rails, not a Unity video capture. The playable surface is the event log and
save/load state that a debug HUD or scene bridge can display.

1. Two living actors receive enabled `JobKind.Smith` preferences with priority
   `1`.
2. A furnace worksite receives enough ore and fuel for the canonical
   `SmeltIronIngot` recipe fixture.
3. The `JobBoard` receives smithing work requests in deterministic insertion
   order.
4. `JobAssignmentSystem.TryAssignNext` assigns both smiths to furnace work in
   stable actor/job order and emits `WorldEventKind.JobAssigned` rows.
5. `JobAssignmentSystem.TickAssignedJobs` advances recipe work orders until
   completion, then clears actor schedules and emits `WorldEventKind.JobCompleted`
   rows after the recipe completion events.
6. The save/load mapper preserves pending and active job rows, claimed actor ids,
   actor schedule targets, active recipe work orders, and furnace worksite state
   through JSON round-trip.

## Evidence chain

- `JobAssignmentSystemTests.AssignsTwoSmithsDeterministically` proves two smith
  actors claim furnace jobs in a stable order.
- `JobAssignmentCompetitionTests.HigherPrioritySmithWinsFurnace` proves actor
  job priority wins deterministic selection when job lanes compete.
- `JobEventLogTests` proves assignment and completion are visible as world
  events with actor/site/job/worksite reason traces.
- `JobAssignmentRoundTripTests` proves the claimed job and active recipe state
  survive save/load.

## Validation

- `git diff --check` - passed.
- `./tools/validation/run-validation.sh --mode fallback` - passed at
  2026-05-15T16:07Z (fallback harness: 878 passed, 0 failed, 0 skipped;
  Unity editor blocked: not found).

## Promotion state

Faz 3 now has the final playable-proof atom represented in this branch, but the
sprint is not promoted until this PR is merged and validation is green on the
promotion branch.

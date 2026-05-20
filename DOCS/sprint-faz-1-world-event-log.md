# Sprint Faz 1 — WorldEventLog (PROCESS-box chronicle)

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-world-event-log`
_Box tag:_ `[box=PROCESS]`
_Atom-map rows:_ `Assets/Scripts/Domain/World/WorldEventLog.cs`,
`Assets/Tests/EditMode/World/WorldEventLogTests.cs`
(sub-area: WorldEvent log + ReasonTrace).

## Goal

Add the append-only `WorldEventLog` chronicle over `WorldEvent`. With
`WorldEvent` (PR #89) and `ReasonTrace` (PR #90) already landed, the
log is the third and final primitive piece of the PROCESS-box
sub-area. Keeps invariants pinned at append: no null events, no
silent gaps, deterministic insertion order, immutable public view.

## Files changed

- `Assets/Scripts/Domain/World/WorldEventLog.cs` — new
  PROCESS-box append-only chronicle. `Append(WorldEvent)` rejects
  null; `Count`, `IsEmpty`, and `Events` expose log state; `Events`
  is wrapped in `ReadOnlyCollection<WorldEvent>` so callers cannot
  downcast back to a mutable list, mirroring the read-only view
  pattern already in `ReasonTrace.Causes`.
- `Assets/Scripts/Domain/World/WorldEventLog.cs.meta` — Unity meta
  stub matching the repo's existing meta-file shape.
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs` — NUnit tests
  pinning the contract: empty log state, single append, multi-append
  insertion order, decreasing-tick insertion order (chronicle, not
  sorter), null rejection, live view tracking later appends, and
  read-only view immutability.
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs.meta` — Unity
  meta stub.
- `DOCS/sprint-faz-1-atom-map.md` — marks the two WorldEventLog
  atom rows as landed and appends WorldEventLog packet metadata.
- `DOCS/sprint-faz-1-world-event-log.md` — this file.

## Invariants pinned

- `Append(null)` throws `ArgumentNullException`; the log never
  contains silent gaps.
- Events are exposed in deterministic insertion order, even when
  appended ticks decrease. The log is a chronicle, not a sorter.
- `Events` is an `IReadOnlyList<WorldEvent>` backed by a
  `ReadOnlyCollection`; downcasting to `List<WorldEvent>` or
  `WorldEvent[]` is rejected by the type system.
- `Events` is a live view, not a point-in-time snapshot: a previously
  captured reference reflects subsequent `Append` calls, so callers
  MUST NOT cache it as an immutable copy.
- `IsEmpty` matches `Count == 0`.

## Scope limits

- The log stores `WorldEvent` references as supplied; it does not
  validate the event beyond non-null because `WorldEvent` already
  pins its own invariants at construction.
- Reason-trace round-trip is **not** part of this PR.
  `WorldEvent` currently has no `ReasonTrace` field; extending it is
  the very-next Faz 1 atom and lives in a follow-up PR.
- Save/load round-trip for the log is **not** part of this PR; it
  belongs to the TIME-box `SliceSaveMapper` atom.

## Agent-rules-v2 fit

- Rule 1 (product-visible increment): adds a new public domain type
  (`WorldEventLog`); not a test-only PR.
- Rule 2 (no speculative utility): no helpers beyond `Count`,
  `IsEmpty`, `Append`, `Events`. Each member has a concrete consumer
  in this PR's tests; the followup `SliceSaveMapper` PR will be the
  next concrete consumer in production code.
- Rule 3 (data-driven effect): N/A — no new `SpellEffectCode` entry.
- Rule 4 (world-store promotion): adds new world state via a Faz 1
  store-shape primitive, not a `SliceWorldState` field.
- Rule 5 (playable proof): not the fifth PR slot; playable proof
  remains the open `DOCS/sprint-faz-1-acceptance.md` atom and is
  unblocked once save/load round-trip lands.

## Validation

- `./tools/validation/run-validation.sh --mode fallback` — green.
  Test count 745 passing, 0 failing (Unity editor not present on
  this Pi; fallback NUnit harness is the canonical local gate per
  `tools/validation/run-validation.sh` docstring).

## Thalamus packet

- packet_id: `pkt_20260511070129_dd3de05281dd`
- resolver_key: `sha256:8f215f88ffe4d580619c5ef284ca9d66a01b2286a79c642c2c5bd8bc7e4a2826`

## Next increment

Extend `WorldEvent` with an optional `ReasonTrace` field, then add
the round-trip coverage row to `WorldEventLogTests` (event-with-trace
appended → enumerated → trace preserved). Closes the PROCESS-box
sub-area and unblocks the `SliceSaveMapper` round-trip atom.

# Sprint Faz 1 — WorldEvent ReasonTrace attachment

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-world-event-reason-trace`
_Box:_ `[box=PROCESS]`
_Atom-map:_ `docs/sprint-faz-1-atom-map.md`
_Thalamus:_ `pkt_20260511165159_ff7b1d23db09` / `sha256:78d30d0a1413c7305c41d3cc24d827b2fabd484ded4e023472adb5b0296b0355`

## Increment goal

Attach the existing `ReasonTrace` causal-chain record to `WorldEvent` so the
append-only `WorldEventLog` can carry why an event happened, not only what
happened. This closes the small PROCESS-box follow-up called out after the
WorldEventLog PR and keeps the next save/load atom ready to serialize a complete
event payload.

## Files changed

- `Assets/Scripts/Domain/World/WorldEvent.cs` — adds optional `ReasonTrace`
  constructor parameter and read-only property while preserving the existing
  constructor call shape through a default `null` value.
- `Assets/Tests/EditMode/World/WorldEventTests.cs` — pins default-null trace,
  explicit trace storage, and existing constructor invariants.
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs` — pins that append and
  enumeration preserve an event's causal trace reference.
- `docs/sprint-faz-1-atom-map.md` — records this PROCESS-box atom as landed.

## Validation

- `git diff --check`: passed (no whitespace errors).
- `./tools/validation/run-validation.sh --mode fallback`: passed; fallback harness 748/748 tests green; Unity editor blocked because not installed on this host.

## Player-visible note

This is not the final playable-proof PR. It is still product-facing foundation:
future debug HUD / replay output can now show both a `WorldEvent` and its
`ReasonTrace`, making the Faz 1 acceptance proof explainable.

## Next increment

Move to the TIME-box atom: extend the save mapper/tests so stores plus
`WorldEventLog` round-trip deterministically.

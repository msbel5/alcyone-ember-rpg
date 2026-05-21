# Sprint Faz 1 — ReasonTrace pure record

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-reason-trace`
_Atom-map ref:_ `DOCS/sprint-faz-1-atom-map.md` — WorldEvent log + ReasonTrace
sub-area (`[box=PROCESS]`).

## Increment goal

Land the Faz 1 PROCESS-box causal-chain primitive `ReasonTrace`, the second
atom of the WorldEvent log sub-area after `WorldEvent` (in flight on
`agent/sprint-faz-1-world-event`, PR #89). `ReasonTrace` records why an event
happened: an ordered, root-first, immutable sequence of cause labels that
will be attached to a `WorldEvent` (and round-tripped through `WorldEventLog`
in a follow-up PR). The record is pure: no Unity, no I/O, no serialization
concerns. It mirrors the `FactionRecord` / `SiteRecord` defensive-constructor
shape so invariants are pinned at construction.

## Files changed

- `Assets/Scripts/Domain/World/ReasonTrace.cs` — pure record carrying an
  ordered, root-first `IReadOnlyList<string>` of causes (defensive copy in the
  constructor, blank entries rejected, empty chain rejected). Exposes
  `Causes`, `Depth`, `RootCause`, `LeafCause`, and a case-sensitive
  `HasCause(string)` lookup. The `Causes` view is a `ReadOnlyCollection<string>`
  so callers cannot cast back to a mutable array.
- `Assets/Scripts/Domain/World/ReasonTrace.cs.meta` — Unity meta stub matching
  the existing minimal two-line pattern used by `FactionRecord.cs.meta` /
  `SiteRecord.cs.meta`.
- `Assets/Tests/EditMode/World/ReasonTraceTests.cs` — pins constructor
  contract (stores in order, single-cause minimum, null/empty/blank
  rejection, defensive copy detached from source list, view is read-only) and
  `HasCause` behavior (case-sensitive match, rejects blank queries). Nine
  `[Test]` cases, mirroring the `FactionRecordTests` shape.
- `Assets/Tests/EditMode/World/ReasonTraceTests.cs.meta` — Unity meta stub.
- `DOCS/sprint-faz-1-atom-map.md` — checks off the `ReasonTrace` row in the
  WorldEvent log + ReasonTrace sub-area and appends this PR's Thalamus packet.

## Validation

`./tools/validation/run-validation.sh --mode fallback` →
`Passed!  - Failed: 0, Passed: 731, Skipped: 0, Total: 731` against the
`ValidationFallbackHarness` pure C# NUnit harness (Unity Editor unavailable
on the Pi sprint factory host, so the fallback harness is the floor).

## Agent rules v2 status

- Rule 1 (product-visible increment): this PR is a structural atom on the
  PROCESS sub-area, not yet user-visible. PR #89 (WorldEvent) is also
  structural. The next PR on this sub-area should expose a visible capability
  (`WorldEventLog` consuming `ReasonTrace`, or an `EventLog` entry surfaced
  through the existing `EventLog`/HUD), or the sprint widens scope.
- Rule 2 (no speculative utility): the record exposes only the surface
  consumed by `WorldEventLog` in the next PR (`Causes`, `Depth`, `RootCause`,
  `LeafCause`, `HasCause`) plus the defensive-constructor invariants. No
  builder, no batch helper, no fluent API.
- Rule 3 (data-driven effects): N/A — this atom does not touch
  `SpellEffectCode` / `EffectDefinition`.
- Rule 4 (world-store promotion): N/A — `SliceWorldState` is not touched.
- Rule 5 (playable proof): cumulative Faz 1 PR count is well under the
  every-fifth-PR threshold; playable proof remains scheduled for the
  acceptance-proof atom (`DOCS/sprint-faz-1-acceptance.md`).

## Thalamus packet

- packet_id: `pkt_20260511062642_d1b0146ad836`
- resolver_key: `sha256:5aaeab7ba3e5041ca669832ed854c75992c15df6f16d35c96b89d0ea28e30a2f`

## Next increment

`WorldEventLog` (`Assets/Scripts/Domain/World/WorldEventLog.cs`) — append-only
log over `WorldEvent` with deterministic enumeration. Tests follow in
`Assets/Tests/EditMode/World/WorldEventLogTests.cs` pinning append order,
deterministic enumeration, and reason-trace round-trip. Lands after PR #89
(WorldEvent) merges so the log can compile against the typed event payload.

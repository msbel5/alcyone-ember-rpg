# sprint faz-1 — WorldEvent (PROCESS-box payload)

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-world-event`
_Box:_ `[box=PROCESS]`
_Atom-map ref:_ `DOCS/sprint-faz-1-atom-map.md` — WorldEvent log + ReasonTrace sub-area.

## Goal

Land the Faz 1 PROCESS-box atom `WorldEvent` — a pure-Domain typed
event payload (`tick`, `kind`, `actorId`, `siteId`, `reason`) — so the
WorldEvent log sub-area has its primitive in place before
`WorldEventLog` and `ReasonTrace` land. Ships alongside a small
seed enum `WorldEventKind` (`None` / `ActorSpawned` / `ActorTalked` /
`SiteEntered`) covering the Faz 1 acceptance-gate event categories
("player can spawn a guard, talk to it, then walk to a second site"
— `docs/ROADMAP.md`). No new enum entry will be added without a
concrete consumer (agent-rules-v2 rule 2).

## Files changed

- `Assets/Scripts/Domain/World/WorldEventKind.cs` (new) — seed enum
  with `None=0` sentinel + three kinds matching the Faz 1 acceptance
  gate.
- `Assets/Scripts/Domain/World/WorldEvent.cs` (new) — pure record with
  defensive constructor (`None`-kind rejection, "no subject"
  rejection when both `ActorId` and `SiteId` are empty, blank-reason
  rejection). Mirrors `SiteRecord` / `FactionRecord` shape.
- `Assets/Tests/EditMode/World/WorldEventTests.cs` (new) — pins
  field storage, kind/subject/reason invariants, actor-only and
  site-only acceptance paths.
- `DOCS/sprint-faz-1-atom-map.md` — atom-map reconciliation:
  - `ActorStore` + `ActorStoreTests` marked `[x]` against PR #79
    (already merged at `a347efe`).
  - `FactionStore` + `FactionStoreTests` marked `[x]` against PR #88
    (already merged at `6c164eb`).
  - `WorldEvent` atom marked `[x]` for this PR.
  - `Next increment` paragraph repointed at `ReasonTrace` →
    `WorldEventLog`.
  - WorldEvent PR packet_id appended.

## Validation result

`./tools/validation/run-validation.sh --mode fallback` →
`Passed!  - Failed: 0, Passed: 729, Skipped: 0, Total: 729`. New
`WorldEventTests` class adds seven assertions covering field
storage, the `None`-kind sentinel, the "no subject" guard, the
actor-only and site-only acceptance paths, the blank-reason guard,
and the null-reason guard.

## Thalamus packet

- packet_id: `pkt_20260511061314_b24015092ce0`
- resolver_key: `sha256:5d01905609fbe9d722d881387679daccfc861627ad1d18bdfd1590fdf2f395a8`

## agent-rules-v2 compliance

- Rule 1 (product-visible increment): this PR adds a new
  domain primitive (`WorldEvent`) that directly unblocks
  `WorldEventLog`, which is itself the canonical "new EventLog
  line" surface called out in rule 1. The next PR
  (`WorldEventLog`) is the visible-capability increment that
  rule 1's two-test-only cap is pointing at; this PR is the
  predecessor primitive, in the same coherent bundle.
- Rule 2 (no speculative utility): no helper methods on
  `WorldEvent` beyond the constructor — fields are exposed
  read-only and consumed by the log atom in the next PR. The
  `WorldEventKind` seed enum carries only the three kinds the
  Faz 1 acceptance gate names.
- Rule 3 (data-driven effect): not applicable; no change to
  `SpellEffectCode` or magic.
- Rule 4 (world-store promotion): not applicable; no new
  `SliceWorldState` field.
- Rule 5 (playable proof): not applicable for this PR (next
  fifth-PR slot is owned by the WorldEventLog → save/load
  acceptance pair).

## Next increment

`ReasonTrace` (`Assets/Scripts/Domain/World/ReasonTrace.cs`) — pure
causal-chain record attached to a `WorldEvent`. Tests land alongside
following the `WorldEventTests` shape.

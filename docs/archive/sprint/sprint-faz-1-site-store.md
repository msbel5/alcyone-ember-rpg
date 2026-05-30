# Sprint Faz 1 — SiteStore (WORLD-box Core Store)

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-site-store`
_Faz:_ Faz 1 (Core Store reset)
_Mechanic-map box:_ `[box=WORLD]`
_Atom-map sub-area:_ SiteStore (WORLD — primary)

## Increment goal

Land the third Faz 1 Core Store: `SiteStore`, the dictionary-backed
registry over `SiteId -> SiteRecord`. Mirrors `ActorStore`'s contract
exactly so the four Faz 1 stores (Actor / Item / Site / Faction)
share a single regression and consumer shape. Both prerequisites
(`SiteId`, `SiteRecord`) merged earlier in the sprint, so this PR
turns the assembled primitives into a consumable registry.

## Files changed

- `Assets/Scripts/Domain/World/SiteStore.cs` — new pure-Domain
  registry. Add / Get / TryGet / Remove / Contains / Count / Clear /
  Records, default-id rejection, deterministic insertion-order
  enumeration. No Unity, no I/O, no LINQ on the hot path.
- `Assets/Scripts/Domain/World/SiteStore.cs.meta` — Unity asset
  metadata.
- `Assets/Tests/EditMode/World/SiteStoreTests.cs` — NUnit pins the
  same contract `ActorStoreTests` pins (Add/Get/TryGet/Remove/
  Contains/Count/Clear plus insertion-order enumeration and remove-
  preserves-order). Default `SiteId` is rejected on every entry
  point.
- `Assets/Tests/EditMode/World/SiteStoreTests.cs.meta` — Unity asset
  metadata.
- `docs/sprint-faz-1-atom-map.md` — SiteStore + SiteStoreTests rows
  flipped to `[x]`, Thalamus packet appended, next-increment line
  pointed at `ItemRecord`.

## Validation result

`tools/validation/run-validation.sh --mode fallback` executed on
this branch; see PR run output. Pure-Domain change with no Unity
linkage, no asmdef edits, and no consumer migrations, so the
existing fallback validation surface is the relevant gate.

## Agent rules v2 alignment

- Rule 1 (product-visible increment): adds a brand-new Domain
  registry that future Faz 1 PRs will consume; not test-only.
- Rule 2 (no speculative utility): no helpers shipped that lack a
  concrete consumer — `ItemStore` and `FactionStore` will land with
  the same shape and the next-increment line names the first
  consumer atom (`ItemRecord`).
- Rule 3 (data-driven effects): N/A.
- Rule 4 (world-store promotion): adds the third Faz 1 store; no new
  named fields on `SliceWorldState`.
- Rule 5 (playable proof): not this PR — playable proof lands on the
  fifth Faz 1 PR per the sprint rule.

## Thalamus packet

- packet_id: `pkt_20260510225845_f00d001dd7e0`
- resolver_key: `sha256:330b38f56b1931e0946787867ba8f3800d6ccf1425ddcd2eb519cfe958b14e2b`

## Next increment

`ItemRecord` (MATTER-box pure record carrying material + quality +
slot kind) so `ItemStore` can immediately ride the same registry
shape `ActorStore` / `SiteStore` established.

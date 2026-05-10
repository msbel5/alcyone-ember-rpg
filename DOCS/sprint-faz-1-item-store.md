# Sprint Faz 1 — ItemStore registry

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-item-store`
_Atom-map row:_ `Assets/Scripts/Domain/World/ItemStore.cs` + `Assets/Tests/EditMode/World/ItemStoreTests.cs` (MATTER sub-area)
_Box:_ `[box=MATTER]`

## Increment goal

Land the third Faz 1 Core Store — `ItemStore` — so the MATTER sub-area
converges on the same dictionary-backed registry contract that
`ActorStore` (LIVING) and `SiteStore` (WORLD) already share.

## Files changed

- `Assets/Scripts/Domain/World/ItemStore.cs` — dictionary-backed
  registry over `ItemId -> ItemRecord` with Add / Get / TryGet /
  Remove / Contains / Count / Clear / Records, deterministic
  insertion-order enumeration, default-id rejection. Pure Domain —
  no Unity references, no I/O.
- `Assets/Tests/EditMode/World/ItemStoreTests.cs` — pins the
  Add / Get / TryGet / Remove / Contains / Count / Clear / Records
  contract and the default-id rejection paths. Mirrors
  `ActorStoreTests` / `SiteStoreTests` so the three landed stores
  share one regression shape.
- `DOCS/sprint-faz-1-atom-map.md` — atom-map rows for `ItemStore.cs`
  and `ItemStoreTests.cs` checked off; new Thalamus packet recorded;
  "Next increment" advanced to the `FactionId` primitive.

## Validation result

`tools/validation/run-validation.sh --mode fallback`:

```
Passed!  - Failed:     0, Passed:   689, Skipped:     0, Total:   689
PASS fallback_harness
```

## Thalamus packet

- packet_id: `pkt_20260510234148_8fc90621f4a4`
- resolver_key: `sha256:5bf9c0606d5aa98ff18c8bb23bd5faff0e9f2bc81218695467adaaf004fc7b64`

## Agent rules v2 alignment

- Rule 1 (product-visible increment): this PR adds a new domain
  primitive (`ItemStore`) plus its regression coverage. It is a
  Core Store landing rather than a test-only PR — the test file
  exists only because the store is new. Counts against rule 1's
  two-test-only cap as one half: a new primitive PR, not a
  pure-test PR.
- Rule 2 (no speculative utility): no helpers added beyond the
  shape already shared by `ActorStore` / `SiteStore`. No new
  fluent builders, no batch overloads.
- Rule 3 (data-driven effect): not touched. No new
  `SpellEffectKind` entries.
- Rule 4 (world-store promotion): this PR moves Faz 1 forward by
  shipping the MATTER store; `SliceWorldState` is not touched, no
  hard-coded slice fields added.
- Rule 5 (playable proof): handled by the dedicated playable-proof
  PR planned at the end of Faz 1 (`DOCS/sprint-faz-1-acceptance.md`
  atom row).

## Next increment

Begin the SOCIETY-seed sub-area with `FactionId`
(`Assets/Scripts/Domain/World/FactionId.cs`) — readonly value
handle following the `ActorId` / `ItemId` / `SiteId` shape.

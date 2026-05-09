# Sprint Faz 1 — ActorStore (first PR)

_Date:_ 2026-05-09
_Branch:_ `agent/sprint-faz-1-actor-store`
_Phase boxes:_ `[box=LIVING]` (primary).
_Atom map:_ `DOCS/sprint-faz-1-atom-map.md`.

## Increment goal

Land Faz 1's first Core Store: `ActorStore`. Pure Domain, dictionary-
backed, deterministic insertion-order enumeration, default-id rejection.
This is the rail Faz 1's later PRs ride on (deprecated-view shims for
`Player`/`Talker`/...; `SliceWorldState` consumer migration).

This is also Faz 1's canonical decomposition entry point: the atom map
in `DOCS/sprint-faz-1-atom-map.md` decomposes Faz 1 against
`DOCS/mechanic-map-v1.md` (boxes WORLD, LIVING, MATTER, plus a
SOCIETY-seed FactionStore and a PROCESS-primary WorldEventLog).

## Files changed

- `DOCS/sprint-faz-1-atom-map.md` — Faz 1 atom map (canonical decomposition).
- `Assets/Scripts/Domain/World/ActorStore.cs` — new dictionary-backed registry.
- `Assets/Tests/EditMode/World/ActorStoreTests.cs` — 15 tests pinning Add/Get/TryGet/Remove/Contains/Count/Clear/Records contracts and default-id rejection.
- `DOCS/sprint-faz-1-actor-store.md` — this summary.

## Validation result

`./tools/validation/run-validation.sh --mode fallback` — PASS.

```
Passed!  - Failed:     0, Passed:   632, Skipped:     0, Total:   632
```

(Previous baseline on `main` was 617 EditMode tests; the 15-test delta
matches the new `ActorStoreTests` cases.)

## Agent rules v2 check

- Rule 1 (product-visible increment): this is Faz 1's first PR. New
  domain primitive plus its test suite — counts toward foundation, not
  against the two-PR test-only cap.
- Rule 2 (no speculative utility): `ActorStore` has a concrete consumer
  in this PR (`ActorStoreTests`). The next PR will land deprecated-view
  shims and start migrating `SliceWorldState`.
- Rule 3 (data-driven effect rule): not applicable; no `SpellEffectKind` change.
- Rule 4 (world-store promotion rule): this PR is the rail. Future PRs
  may not add hard-coded slice fields; they must land in `ActorStore`,
  `ItemStore`, `SiteStore`, or `FactionStore`.
- Rule 5 (playable proof): not yet — the playable-proof PR is the
  fifth PR of Faz 1 per the atom map.

## Thalamus packet

- packet_id: `pkt_20260509204459_41ce3bbd63a2`
- resolver_key: `sha256:7cc6df815b0d6d4aedfad98eaf53ca7629a69148c393b941bb3517229f8e707c`

## Next increment

Deprecated-view shims so `SliceWorldState.Player`/`Talker`/`Merchant`/
`Guard`/`Enemy` can resolve through `ActorStore`. Then begin the
consumer migration PR-by-PR per rule 4.

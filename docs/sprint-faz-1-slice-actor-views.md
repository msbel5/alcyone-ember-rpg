# Sprint Faz 1 — SliceWorldState store-backed actor views

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-slice-actor-views`
_Box:_ `[box=LIVING]`
_Atom-map:_ `docs/sprint-faz-1-atom-map.md`
_Thalamus:_ `pkt_20260511180152_36cf9ee4aad3` / `sha256:527e964761013636e732d49f4d9979886c83eaee103289da50a3996366a9af6d`

## Increment goal

Turn `SliceWorldState`'s legacy named actor fields into deprecated,
store-backed views over `ActorStore` role lookups. This keeps existing gameplay
code working while Faz 1 moves new world state through the LIVING store instead
of adding or extending hard-coded slice actor fields.

## Files changed

- `Assets/Scripts/Domain/World/SliceWorldState.cs` — adds the canonical
  `Actors` store and makes `Player` / `Talker` / `Merchant` / `Guard` / `Enemy`
  obsolete properties backed by `ActorStore.FirstByRole(...)`.
- `Assets/Tests/EditMode/World/SliceWorldStateActorViewTests.cs` — pins setter
  registration, replacement behavior, role validation, and factory population
  of the store-backed views.
- `Assets/Tests/EditMode/World/ActorStoreTests.cs` — refreshes migration-note
  comments now that `SliceWorldState` consumes the role-view shims.
- `docs/sprint-faz-1-atom-map.md` — checks off the LIVING migration rail and
  records the current packet metadata.

## Validation

- `git diff --check`: passed (no whitespace errors).
- `./tools/validation/run-validation.sh --mode fallback`: passed; fallback harness 753/753 tests green; Unity editor blocked because not installed on this host.
- Compiler note: obsolete-view warnings are expected for legacy slice consumers until the next migration pass replaces them with direct `Actors` lookups.

## Player-visible note

This is product-visible foundation: the playable slice's existing actor access
surface now reads from `ActorStore`, so future guard/player persistence work can
move through the store without new hard-coded `SliceWorldState` actor fields.

## Next increment

Move to the TIME-box save/load atom: serialize the four Faz 1 stores plus
`WorldEventLog`, then pin deterministic round-trip tests.

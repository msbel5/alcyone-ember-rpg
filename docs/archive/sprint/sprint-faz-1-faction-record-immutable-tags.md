## Sprint Faz 1 — FactionRecord.Tags immutable projection

_Date:_ 2026-05-11
_Branch:_ `agent/sprint-faz-1-faction-record` (follow-up commit on PR #87)
_Box:_ `[box=SOCIETY]` (seed, hardening)
_Atom-map ref:_ `docs/sprint-faz-1-atom-map.md` — FactionStore sub-area
_Source:_ AI bot review queue — Copilot + chatgpt-codex-connector P2
on PR #87 ("Return a truly immutable tag snapshot")

## Increment goal

Close the immutability gap on `FactionRecord.Tags`. Before this
change the property returned the backing `string[]` typed as
`IReadOnlyList<string>`, so a caller could cast back to `string[]`
and mutate the internal tag bag. The defensive copy at construction
held, but the projection contract did not.

## Files changed

- `Assets/Scripts/Domain/World/FactionRecord.cs`
  - `using System.Collections.ObjectModel;` added.
  - New `private readonly ReadOnlyCollection<string> _tagsView`
    populated in the constructor.
  - `Tags` now returns `_tagsView` instead of `_tags` directly. The
    wrapper is allocated once per record (no per-access allocation).
- `Assets/Tests/EditMode/World/FactionRecordTests.cs`
  - `Tags_ProjectionIsNotBackingArray` — runtime type of `Tags` must
    not be `string[]`.
  - `Tags_ProjectionCannotBeMutatedViaDowncast` — the `as string[]`
    cast yields `null` and the record stays equal to the original
    insertion-order bag.

## Validation result

`tools/validation/run-validation.sh --mode fallback` — `PASS`,
`Passed: 708, Failed: 0, Skipped: 0` (two new tests on top of the
prior 706).

## Agent-rules v2 compliance

- Rule 1 (product-visible increment): hardening of an existing
  public type so the next Faz 1 increment (`FactionStore`) cannot
  hand callers a mutable internal bag. Not a fresh feature, not a
  test-only PR; addresses a P2 bot-flagged defect on the same PR.
- Rule 2 (no speculative utility): no new helpers, no fluent
  builders. Single internal field plus one-line constructor
  assignment.
- Rules 3-4: not applicable (no `SpellEffectCode`, no
  `SliceWorldState`).
- Rule 5 (playable proof): not the playable-proof slot.

## Bot review queue

- Copilot inline comment on PR #87 — addressed in the same commit.
- chatgpt-codex-connector P2 inline comment on PR #87 — addressed
  in the same commit.
- Replies posted on each comment explaining the
  `ReadOnlyCollection<string>` wrapper choice.

## Thalamus packet

- packet_id: `pkt_20260511002159_c3c153f1251a`
- resolver_key: `sha256:321d708edba89c7c51a29d1e1bab7d603d5835bfbae478cd0c8da3e61a827e49`
- category_filter: `["atoms.code", "atoms.plan", "atoms.memory"]`
- confidence: 0.35 (escalation_reason: "code task requires Builder
  and tests after Thalamus context" — Captain executed inline).

## Next increment

`Assets/Scripts/Domain/World/FactionStore.cs` — dictionary-backed
registry mirroring `ActorStore` / `SiteStore` / `ItemStore`. Atom
map row in `docs/sprint-faz-1-atom-map.md` under FactionStore
sub-area.

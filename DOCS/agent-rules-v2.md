# Agent rules v2 — for Alcyone Captain on alcyone-ember-rpg

These rules supplement the existing `@EMSPR` cron routine in
`/home/msbel/.openclaw/workspace/CRON_CODES.md`. When in doubt these
override the older "exactly one small shippable increment" wording.

## 1. Product-visible increment rule

A sprint may have at most two test-only PRs. The third PR must
unlock or expose a visible capability.

A PR is **visible** if at least one is true:

- it adds a system that, when ticked, produces a new `EventLog` line
  visible in the Unity debug overlay
- it adds a `player can ...` action verifiable on the Unity scene
- it extends a Store (Actor / Item / Site / Faction) with new state
  the rest of the simulation can read and react to
- it ships a new `EffectDefinition` row + handler that produces an
  observable game outcome (not only test fixtures)

Pure regression tests that pin existing behaviour are good and
allowed, but they are subordinate. They do not count toward visible
progress.

## 2. No speculative utility rule

Utility helpers (`MergeMany`, `PartitionMany`, `GroupByMany`, filter
overloads, batch wrappers, fluent builders) may be added only when:

- a concrete consumer is in the same PR, or
- a concrete consumer ships in the very next PR with a backlink

If neither condition holds, do not write the helper. If the helper
already exists and is unused after two sprints, mark it for deletion
in the sprint summary.

## 3. Data-driven effect rule

Do not add new entries to `SpellEffectKind`. The current enum is the
last enum-driven generation of magic.

Before any new effect ships:

1. Promote magic to `EffectDefinition` + `EffectOperation` registry.
2. Re-express the existing 7 enum entries as data rows backed by
   2-3 operation handlers.
3. Land that promotion as one PR.
4. From that point on, new effects ship as data only. C# changes
   only when a new operation kind is needed.

This rule reads as a hard block on a particular kind of micro-PR
that has been dominating the cron loop.

## 4. World-store promotion rule

`SliceWorldState` may not gain new named fields. Existing named
fields (`Player`, `Talker`, `Merchant`, `Guard`, `Enemy`) are
deprecated and will be removed in Faz 1.

New world state must land in:

- `ActorStore : Dictionary<ActorId, ActorRecord>`
- `ItemStore : Dictionary<ItemId, ItemRecord>`
- `SiteStore : Dictionary<SiteId, SiteRecord>`
- `FactionStore : Dictionary<FactionId, FactionRecord>`

A PR that introduces a new actor type or new world fixture must
write through one of the Stores. A PR that adds a hard-coded slice
field will be reverted.

## 5. Playable proof rule

Every fifth PR on the active sprint must include one of:

- a screenshot from the Unity scene with the new capability visible
- a deterministic replay log demonstrating the new behaviour
- a debug HUD dump showing the new state
- a playtest note (one paragraph, what was done, what was observed,
  what failed, what the next move is)

Plus a `player can ...` acceptance sentence written into the sprint
summary. Examples:

```
player can craft an iron ingot from ore and fuel and watch the
   stockpile increase

player can wait until spring, plant wheat, harvest in summer, and
   see food stockpile rise

player can witness an NPC remember a crime committed two days ago
   and refuse to trade
```

If no such sentence is reasonable for the next PR, the sprint scope
needs to widen, not the test surface.

## Enforcement notes for Captain

- Decompose with `atom-of-thoughts` against `DOCS/mechanic-map-v1.md`,
  not against the old enum tree.
- The atom-map (`DOCS/sprint-N-atom-map.md`) tracks which mechanic-map
  box the atom belongs to. Every atom row carries a box tag like
  `[box=PROCESS]`.
- Sprint promotion (the hard rule from `@EMSPR`) still applies, but
  with one addition: the sprint summary must list how many PRs were
  visible per the rule above. A sprint with zero visible PRs is
  failed and is not a candidate for promotion.

## Cheap-model affordance still applies

Local Qwen on Ollama may write a small atom (single pure function,
no I/O, no cross-module touch) when the Builder is delegated. The
Captain still plans, the Inspector still reviews. The Thalamus
packet must be in the Builder spawn context.

## Scope freeze on `ember-rpg`

The Godot/Python repo at `msbel5/ember-rpg` is now read-only
reference for this project. Captain may pull docs from there into
`docs/reference/` of the active repo, but may not edit it.

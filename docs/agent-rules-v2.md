# Agent rules v2 — for Alcyone Captain on alcyone-ember-rpg

> ⚠️ **LEGACY / NOT ACTIVE (E7-023, 2026-05-31).** These old "Alcyone Captain" cron-automation rules
> reference paths that no longer exist (`docs/archive/sprint/`) and hard-fail any touch to
> `Assets/Scripts/Presentation`, which directly conflicts with the current Unity remediation (which MUST
> edit Presentation). **Do not treat these as active PR rules.** The live workflow is
> `docs/CURRENT_STATE.md` + `docs/REMEDIATION_V2_COUNTER.md`. Kept only for historical reference.

These rules supplement the existing `@EMSPR` cron routine in
`/home/msbel/.openclaw/workspace/CRON_CODES.md`. When in doubt these
override the older "exactly one small shippable increment" wording.

## Required reading (Captain consults before every atom kickoff)

In addition to this file, Captain reads the following before every atom-map kickoff doc and before every PR:

- `docs/mechanics/MASTER_MECHANICS_BIBLE.md` — the 8-box living-world model. Every atom row carries exactly one `primary_box` from this list.
- `docs/EMBER_VISION_NOTES_MAMI.md` — operating constraints (Phase fences), 9-point Vision anchors, and Mami's verbatim intent. Captain's kickoff doc cites which anchors the sprint serves and which fences it honors.
- `docs/inspector-audit-checklist.md` — the checklist Inspector applies to every Captain PR. Captain self-checks against this before opening a PR.
- The active sprint atom map (under `docs/archive/sprint/`, once a sprint is in flight) — top-of-file **Debt ledger** is a gate, not a footnote. Before kicking off the next atom, Captain takes one action against the ledger (close / advance / defer) and records it in the kickoff doc.

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

Do not add new hard-coded `SpellEffectCode` branches. Magic has moved
off enum-driven expansion. The remaining legacy
`SpellEffectResolutionService` switch is acknowledged as a migration
adapter for the seven original effect codes; new effects must ship as
`EffectDefinition` + `EffectOperation` rows registered with
`EffectOperationHandlers`. As of the seventh pass,
`DomainSimulationAdapter.TryCastSpell` routes through `SpellExecutionService`
(Cast → Target → Effect → CastRoll), so a live cast mutates the chosen
target's vitals via `SpellEffectResolutionService`. The fully data-driven
`SpellResolver` (zero-C#-branch new effect) is still queued under Faz 8
slice 2.

Before any new effect ships:

1. Promote magic to `EffectDefinition` + `EffectOperation` registry.
2. Re-express the existing 7 legacy effect codes as data rows backed by
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

- Decompose with `atom-of-thoughts` against `docs/mechanics/MASTER_MECHANICS_BIBLE.md`,
  not against the old enum tree.
- The atom-map (`docs/sprint-N-atom-map.md`) tracks which mechanic-map
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

## 6. Hard fail paths (Captain forbidden touch)

A Captain PR is automatically rejected by Inspector if any of the following hold:

- The PR touches `Assets/Scenes/`, `Assets/Art/`, `Assets/Prefabs/`, `Assets/Resources/`, `Assets/Materials/`, `Assets/Textures/`, or any binary file (`.png`, `.jpg`, `.fbx`, `.wav`, `.mp3`, `.psd`, `.blend`, `.tga`, `.exr`).
- The PR touches `docs/screenshots/`, `docs/images/`, or any non-text asset under `docs/`.
- The PR touches `Assets/Scripts/Presentation/` except for pure-C# read-only snapshot row types that contain zero `using UnityEngine` AND name a Mami-side consumer (a pending or merged `mami/*` PR) in the PR body. This exception matches the Faz 11 carve-out in Rule 7 below.
- The PR's acceptance criterion mentions a screenshot, prefab, scene capture, or visual asset that Captain is asked to produce.

The visual layer is Mami territory. Captain may write atom maps and mechanic docs that PRESCRIBE visual outcomes, but Captain does not produce visual artifacts. Fake screenshots (transparent PNGs, placeholder images, empty stubs) are the worst possible outcome of an AI-managed loop and are explicit Rule 6 violations.

## 7. Faz 11 visual layer carve-out

Captain MAY:

- write `docs/archive/sprint/sprint-faz-11-atom-map.md`
- write or extend `docs/mechanics/faz-11-unity-visual-layer.md`
- write pure-C# snapshot row types under `Assets/Scripts/Presentation/VisualLayer/` provided the file contains zero `using UnityEngine` AND has at least one Mami-side consumer pending or merged (cite the consumer in the PR body)

Captain MAY NOT:

- create or modify `.unity` scene files
- create or modify `.prefab` files
- create or modify materials, textures, sprites, models
- generate "evidence" screenshots
- claim Faz 11 promotion proof

Faz 11 promotion is proven by Mami's `mami/*` PR landing a real Unity scene with the targeted acceptance sentence demonstrable in the editor.

## 8. Anti-drift halt rule

If two consecutive Captain PRs satisfy BOTH of the following:

- the PR body's `Visible proof artifact` field reads `none-this-is-foundational`, AND
- the PR body's `Carry-over debt row advanced` field reads `none-ledger-empty` while the active Debt ledger still contains any row whose status is `open` (i.e. neither `closed` nor `deferred-to-faz-N`)

then the active atom map's "Next increment" pointer is treated as stale. Inspector halts the cron loop, opens a `loop-halt` issue tagged with both PR numbers, and pings Mami. Mami rewrites the atom map's "Next increment" before Captain resumes.

A row counted as `closed` by Inspector requires the row's **Exit proof** column to be satisfied by the PR diff, not Captain's self-report. A row counted as `advanced` requires partial-progress evidence in the diff. Captain may not bypass Rule 8 by re-phrasing a partial PR as `closed`.

This rule exists to prevent the Sprint-5 magic micro-loop pattern from recurring in a different costume (struct-primitive matrix, helper proliferation, internal scaffolding without consumer).

## 9. PR body audit fields (mandatory)

Every Captain-authored PR MUST include this block verbatim in its body, with values filled in. Inspector rejects PRs missing the block or any field, per `docs/inspector-audit-checklist.md` checklist A.

```
Primary box: <one of TIME|WORLD|LIVING|MATTER|PROCESS|SOCIETY|CRPG|AI/DM>
Visible proof artifact: <path to test / log / snapshot / event row in the diff, OR "none-this-is-foundational" + CO row ID from the active Debt ledger>
New enum / helper / class added: <yes-with-same-PR-consumer-at-PATH | yes-deferred-to-PR#... | no>
Carry-over debt row advanced: <CO-XX-closed | CO-XX-advanced | CO-XX-deferred-to-faz-N | none-ledger-empty>
Why this is the next bundle: <one sentence tying to ledger + atom map>
Phase fences honored: <yes | called-out-violation-because-...>
```

The block is enforced via `.github/PULL_REQUEST_TEMPLATE.md` so every new PR opens with the template pre-filled. Captain fills the values; Inspector reads the values and checks them against `docs/inspector-audit-checklist.md`.

## Ownership boundaries

| Path | Owner | Captain may write? | Mami writes |
|---|---|---|---|
| `Assets/Scripts/Domain/` | Captain | yes | no |
| `Assets/Scripts/Simulation/` | Captain | yes | no |
| `Assets/Scripts/Data/` | Captain | yes | no |
| `Assets/Tests/EditMode/` | Captain | yes | no |
| `Assets/Scripts/Presentation/VisualLayer/*.cs` (pure-C# rows, zero UnityEngine) | shared | yes, if zero `using UnityEngine` and Mami-side consumer cited | yes |
| `Assets/Scripts/Presentation/` (Unity-bound views) | Mami | no | yes |
| `Assets/Scenes/` | Mami | no — Rule 6 | yes |
| `Assets/Art/`, `Assets/Prefabs/`, materials, textures, models | Mami | no — Rule 6 | yes |
| `docs/` (text only) | shared | yes | yes |
| `docs/screenshots/`, `docs/images/`, binaries under DOCS | Mami | no — Rule 6 | yes |

Branch naming: Captain ships in `agent/*` branches. Mami ships in `mami/*` branches. Captain PRs that branch from `mami/*` are rejected on sight.

## 8-box tag schema (clarification)

Per `docs/mechanics/MASTER_MECHANICS_BIBLE.md` "every gameplay system fits into exactly one box":

- Every atom row carries exactly one `primary_box` from `{ TIME, WORLD, LIVING, MATTER, PROCESS, SOCIETY, CRPG, AI/DM }`.
- Cross-cutting concerns (`infra`, `meta`, `playable`) are NOT boxes. They appear as commentary fields, not in the `primary_box` column.
- Multi-box syntax like `[box=PROCESS][box=LIVING]` is grandfathered in already-merged atom maps but is forbidden in new atom rows. New rows pick the dominant box.

# Alcyone Ember audit — 2026-05-09

State of the repo, why the cron loop has been micro-stuck, and the
realignment plan. Authored after a third-party audit by ChatGPT Pro
(read by msbel) plus repo inspection.

## What is in this repo right now

- Active Unity / C# rewrite. Domain + Simulation + Tests folders are
  populated.
- Sprint 5 magic effect resolution has produced a long string of small
  PRs (#62 to #71) that test the same DirectDamage / RestoreHealth /
  DirectMana / RestoreMana / DirectFatigue / RestoreFatigue code paths
  under clamp, overshoot, zero-magnitude, and bundle conditions.
- `SliceWorldState` still hard-codes Player / Talker / Merchant / Guard
  / Enemy as named fields. There is no `ActorStore`, no `ItemStore`,
  no `SiteStore`, no `FactionStore`.
- The repo has NO copy of the design bible, mechanic map, or PRD set
  from `msbel5/ember-rpg`. Those remain in the Godot/Python repo as
  reference material.
- README and `docs/ROADMAP.md` describe an earlier "Sprint 4 planned"
  state that contradicts the actual PR history.

## What this means

The cron loop is grinding through a high-density test matrix on a
narrow piece of the magic system. Each PR is a real test. But the
overall product is not advancing, and the audit list above shows why:

1. The active repo has no living-world store, so every new PR
   inevitably lands inside the same magic-resolver branch tree.
2. The repo lost the connection to the bible and the PRD set, so
   Captain has no map of "what world state should exist next" and
   defaults to "another regression test on the same enum".
3. The bundling / sprint promotion / atomic decomposition rules added
   to `@EMSPR` recently are correct, but they assume Captain has a
   visible mechanic map to decompose against. There is no map in the
   repo.
4. Test-only PRs do not produce anything a player can see. The repo
   is becoming a regression suite rather than a game.

## Realignment plan (Faz 0)

This audit is part of Faz 0. The five files we are landing in this
branch:

| File | Purpose |
|---|---|
| `docs/alcyone-audit-2026-05-09.md` | This audit |
| `docs/mechanic-map-v1.md` | The 8-box living-world model |
| `docs/agent-rules-v2.md` | New cron rules: visible-progress, no-speculative-utility, data-driven effects, world-store promotion, playable proof |
| `README.md` (rewritten) | Reflects the actual state of the rewrite |
| `docs/ROADMAP.md` (rewritten) | 12-phase roadmap aligned with the mechanic map |

Plus we copy reference material from `msbel5/ember-rpg` into
`docs/reference/` so Captain can decompose against the canonical
mechanic list rather than the bare magic enum.

## Engine + architecture decision (locked)

- Engine: **Unity** (msbel knows C#, repo already configured, fast
  visual iteration).
- Core: **Unity-independent `Ember.Core`** (Domain + Simulation +
  Persistence + Tests). No `using UnityEngine` anywhere in Core.
- Unity layer: only Scene, Input, Camera, UI, ViewModels,
  ActorView/ItemView, DebugVisualizer.
- Determinism: simulation always authoritative. LLM can read views,
  call typed query/roll/mutation tools, but cannot mutate state
  directly.
- Language for content: data-driven where possible. New magic effects
  ship as `EffectDefinition` rows, not new C# enum branches.

## What stops

Effective immediately, by `agent-rules-v2`:

1. No more test-only PRs that do not unlock a visible capability.
   At most two test-only PRs per sprint, third must be visible
   gameplay or world-state extension.
2. No new utility overloads (`MergeMany`, `PartitionMany`,
   `GroupByMany`, etc.) without a same-PR or next-PR consumer.
3. No new `SpellEffectCode` enum entries without first promoting
   to `EffectDefinition` + `EffectOperation` data path.
4. No new world-state field added directly to `SliceWorldState`.
   New world state goes through `ActorStore` / `ItemStore` /
   `SiteStore` / `FactionStore`.
5. Every five PRs must produce one screenshot, playtest note, debug
   UI output, or deterministic replay log, plus a `player can ...`
   acceptance sentence.

## What continues

The existing magic effect tests stay. Their value is regression
coverage. New tests pile up only when paired with a visible-capability
PR per the rules above.

## Decision references

- `docs/EMBER_VISION_BIBLE.md` (already in repo)
- `docs/architecture/` (already in repo)
- `docs/reference/` (newly added, mirrors `msbel5/ember-rpg`
  canonical mechanic and PRD docs)

## Acceptance for Faz 0

- A new contributor opens the repo, reads `README.md`, reads
  `docs/ROADMAP.md`, reads `docs/mechanic-map-v1.md`, and within
  five minutes can answer:
  - what kind of game is this
  - what is in scope right now
  - what is reference / not-yet-built
  - what the next sprint's playable acceptance is

If that test passes, Faz 0 is done. Faz 1 (Core Store reset)
starts next.

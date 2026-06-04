# Codex Senior Review — 2026-06-04

External adversarial code-and-design review (Codex / gpt-5.4, read-only, every
claim cited to file:line). This is the evidence base for `CURRENT_STATE.md`.
Recorded verbatim-condensed; the full tabular version (dimensions A–J with per-
finding file:line) was produced in-session and can be regenerated from
`Reports/codex-senior-review-prompt.md`.

## Executive summary (verbatim)

1. Real progress: boot flow, character creation, runtime world realization,
   save/load, melee/spell interaction, deterministic tick composition, content
   data layer. Not a toy scaffold anymore.
2. Still NOT a cohesive CRPG loop: core progression surfaces are placeholder,
   misleading, or disconnected. Worst: broken HUD attack/search, no live
   quest/journal despite a seeded quest system.
3. Architecture still over-centralizes in `EmberWorldHost` and
   `DomainSimulationAdapter`. Docs claim those god-objects are closed; the code
   does not support that. Partial-class splits improved readability, not ownership.
4. Determinism in Domain/Simulation is materially better than average hobby code.
   No proven `UnityEngine`/`System.Random` leak in reviewed paths. Tick
   registry/composer are among the strongest parts.
5. Biggest "Ember spirit" drift: the world looks alive via presentation tricks
   (ring-spread + idle wander) rather than simulation truth; the worldgen reveal
   fabricates question/NPC-decision content.
6. Heavy reliance on mutable globals + service locators (runtime options, loading
   singleton, domain adapter locator, static worldgen intent handoff).
7. Built-but-unwired: trade, guard interaction, encounter turns, tool use, memory
   writing, visible worldgen reveal, quest presentation.
8. Async/error handling too optimistic: `async void`, fire-and-forget, broad
   exception swallowing around generation/LLM/main-thread apply.
9. UI consistency compromised by runtime-constructed canvases with bare
   `CanvasScaler` defaults — supports the "UI not responsive" complaint.
10. Closer to a solid backend with a partially-honest frontend than to the stated
    living/reproducible-CRPG vision. Next wins are wiring, ownership, and removing
    fakery — not new systems.

## Top 10 highest-leverage fixes

1. Fix the player-facing lies first: HUD `ATK`, `SRCH`, map/inventory/journal copy.
2. Remove or finish the visible worldgen reveal path (half-live + synthetic).
3. Split `EmberWorldHost` by ownership, not partial files.
4. Replace `IDomainSimulationAdapter` + static locator with role interfaces.
5. Immutable simulation config snapshot; stop reading `EmberRuntimeOptionsProvider.Current` from Simulation.
6. Add a minimal quest/journal UI so the seeded quest becomes a loop.
7. Centralize runtime overlay/canvas creation + one scaler/responsiveness policy.
8. Replace broad exception swallowing with structured failure reporting.
9. Stop using generic fallback billboard pools as the NPC identity plan.
10. Delete/archive dead legacy: `EmberLoadingScreen` (done 2026-06-04), legacy
    worldgen UI, stale "current" docs (done 2026-06-04).

## 3-phase roadmap

- **STABILIZE**: real-or-removed verbs; `ToggleMap` means map; quest/journal
  surface; kill/wire dead reveal; logged failures not swallowed.
- **STRENGTHEN**: split `EmberWorldHost` + aggregate adapter; immutable sim config;
  typed UI presenters for char-creation/loading/worldgen; one canvas factory.
- **SCALE-TOWARD-VISION**: actor presence/LOD model; truth-from-sim not illusion;
  region-scale persistence; real generated-NPC identity pipeline.

## Coverage / honesty caveats (from the reviewer)

- Absence ("no callers") claims are production-grep-backed, not exhaustive proof.
- Not playtested live this pass; source-backed only.
- No proven P0 determinism leak found in reviewed Domain/Simulation code.

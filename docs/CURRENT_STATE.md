# Ember — Current State (single source of truth)

_Last updated: 2026-06-04._

This is the ONE status doc. The old interlinked state/remedy/roadmap docs
(`REMEDIATION_V2_COUNTER.md`, `REMEDIATION_V2_GOAL_PROMPT*.md`, `ROADMAP.md`,
`Audit.md`) were **deleted** on 2026-06-04 because they overstated completion
("closed") and contradicted the code, which caused repeated AI drift. Do not
recreate that web. Keep this file adversarial, not celebratory.

The full, evidence-backed assessment is **`docs/CODEX_REVIEW_2026-06-04.md`**
(staff-level external review, every claim cited to file:line). Read that first.

## Honest one-paragraph state

A solid, genuinely-deterministic **backend** with a **partially-honest frontend**.
Boot → main menu → 11-step character creation → runtime-realized generated world →
melee/spell/dialog → save/load all work. It is **not yet a cohesive CRPG loop**:
several player-facing verbs are fake or broken, several systems are built but
unwired, and two god-objects still own too much. The next wins are wiring,
ownership, and removing fakery — not new systems.

## Genuinely good (verified)

- **Determinism**: no proven `UnityEngine`/`System.Random` leak found in
  Domain/Simulation. `noEngineReferences: true` enforced on both. Tick
  registry/composer use a stable total order. (Forge visual-cache `System.Random`
  is a documented, narrow exception — keep it narrow; see `DETERMINISM.md`.)
- Spherical-planet worldgen pipeline + world-history simulation.
- Runtime world realization (terrain + buildings + player rig) from data.
- The new **character-creation visual design** (Claude Design handoff) — see
  `Reports/CC-AllScreens/` + `Reports/codex-senior-review-prompt.md`.

## Open issues that matter (P0/P1 — from the review, verified)

- **God objects, NOT decomposed** (docs previously lied about this):
  `EmberWorldHost` (lifecycle+UI+input+scene) and `DomainSimulationAdapter`
  (save+dialog+combat+worldgen+read-models behind a static locator). Partial-class
  splits reduced file length, not responsibility. → decompose by ownership.
- **Player-facing lies**: HUD `ATK` sends an empty target; `SRCH` sends the
  literal string `"search"`; inventory/map/journal say "not yet available" though
  systems exist; `ToggleMap` opens inventory while the map is hardcoded to `M`.
- **No quest/journal UI** despite a seeded quest system that ticks. The loop has
  no surface for goals/progress.
- **UI not responsive**: fallback canvases use bare `CanvasScaler` defaults →
  clipping/shift across resolutions. (Design handoff + a UI-Toolkit-vs-uGUI
  decision are pending.)
- **Aliveness is partly faked**: generated NPCs use cosmetic ring-spread + idle
  wander + a tiny shared sprite pool instead of authoritative schedule/identity.
- **Async/error hygiene**: `async void`, fire-and-forget, and broad
  catch-and-swallow in LLM/generation/main-thread-apply paths.

## Built but UNWIRED (decision needed: wire, or delete — keeping both is the bug)

0 production callers found for each (grep-verified 2026-06-04):
`MerchantTradeService`, `GuardInteractionService`, `EncounterTurnService`,
`ToolUseService`, `MemoryWriteSystem`. Legacy reveal path
(`EmberWorldGenUI`, `WorldgenViewController`, `WorldgenEventProjector`) is
superseded by the in-char-creation reveal but still present.

## Roadmap (replaces the old ROADMAP.md)

1. **STABILIZE** — make every exposed verb real or remove it; fix `ToggleMap`;
   add a minimal quest/journal surface; delete/wire the dead reveal path; replace
   catch-and-ignore with logged failures.
2. **STRENGTHEN** — decompose `EmberWorldHost` + the aggregate adapter by
   responsibility; freeze an immutable simulation config snapshot; one
   overlay/canvas factory with one scaler policy; typed per-step CharCreation
   presenters; implement the new CharCreation design.
3. **SCALE-TOWARD-VISION** — real actor presence/LOD model; region-scale
   persistence; move truth from presentation illusion into authoritative sim;
   real NPC identity (sprite/schedule/memory) pipeline.

## Rules to stop the drift

- This is the only status doc. Don't add parallel state/remedy/roadmap files.
- No "closed/done" claim without current evidence (build log, proof, or file:line).
- Presentation polish is allowed as polish, never as a substitute for sim truth.

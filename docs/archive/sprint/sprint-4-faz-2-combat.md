# Sprint 4 Faz 2 — Real-Time Combat Foundation

_Date:_ 2026-04-30
_Branch:_ `agent/sprint-4-faz2-rtwp-combat`
_Base:_ `38f4ea3` — Sprint 4 Faz 1 merge

## Scope delivered

Faz 2 adds the first deterministic real-time-with-pause combat substrate while preserving the Sprint 1 bounded encounter loop as legacy slice behavior.

Delivered:
- pure Domain/Simulation action queue for `MeleeSwing`, `Block`, `Dodge`, and `Cast`
- windup / active / recovery timing profiles for each action
- simulation-level pause flag suitable for SPACE-driven RTWP pause
- queue/cancel support while paused; paused ticks do not advance deterministic combat time
- pure weapon-hit abstraction for presentation collider events (`WeaponHitEvent`)
- humanoid body-part hierarchy with parent links, target weights, armor-class modifiers, and damage multipliers
- deterministic damage pipeline using attacker stats, defender armor/AGI/END, active block/dodge intent, body part, AC, and seeded RNG
- thin Sprint 4 Unity presentation adapter that maps mouse/buttons/keys into the pure action scheduler

## Architecture

### Pure layers

New Domain contracts live under `Assets/Scripts/Domain/Combat`:
- `CombatActionKind` — RTWP action verbs
- `QueuedCombatAction` — actor-local scheduled action with windup/active/recovery timestamps
- `CombatActionEvent` / `CombatActionEventKind` — activation/completion milestones
- `WeaponHitEvent` — Unity-free weapon/body collider event payload
- `BodyPartNode` — hierarchy + mechanics metadata for a targetable body part
- `CombatDefenseIntent` — active block/dodge posture for damage resolution
- `RealtimeDamageResult` — deterministic hit/damage evidence record

New Simulation services live under `Assets/Scripts/Simulation/Combat`:
- `RealtimeCombatState` — elapsed time, pause state, and pending queue
- `RealtimeCombatActionScheduler` — queue/cancel/tick and active defense lookup
- `CombatActionTimingProfile` — first-pass action timing values
- `BodyPartHierarchy` — weighted body selection and body node lookup
- `RealtimeDamageService` — AC + body-part + stat + armor damage pipeline

`Domain/` and `Simulation/` remain `UnityEngine`-free. The Unity-facing layer only adapts inputs into the simulation queue.

### Presentation seam

`Assets/Scripts/Presentation/Sprint4/Sprint4CombatInputAdapter.cs` is intentionally thin:
- `Space` toggles the pure combat pause state
- left mouse queues melee swing
- right mouse queues block
- left shift queues dodge
- `1` queues cast
- `Update()` ticks the pure scheduler with `Time.deltaTime`

The Sprint 4 bootstrap attaches this adapter to the placeholder player so the scene has a combat input seam without requiring art, weapons, animation events, or enemy AI yet.

## Test evidence

Local fallback harness:

```text
tools/validation/run-validation.sh --mode fallback
Passed!  - Failed: 0, Passed: 85, Skipped: 0, Total: 85
PASS fallback_harness
```

New tests:
- `RealtimeCombatActionSchedulerTests`
  - actor-local queue ordering
  - activation/completion event ordering
  - paused tick does not advance time
  - queue cancellation/replacement while paused
  - active block/dodge defense lookup
- `RealtimeDamageServiceTests`
  - same seed produces same hit result
  - collider body-part override is honored
  - body hierarchy parent/modifier critical path
  - blocking raises AC and reduces damage

## Limitations / manual gaps

- This is a combat foundation, not a full playable enemy encounter. Enemy AI, target acquisition, animation events, weapon trail/collider authoring, camera shake, hit reactions, SFX, and UI feedback remain future work.
- The adapter defaults to `Space` for RTWP pause, while Faz 1 movement also uses Space for jump. The pure simulation support is correct, but final input mapping needs a design pass before manual play approval.
- Local validation is the pure .NET fallback harness, not a real Unity Editor import/EditMode/PlayMode run.
- No manual feel video was produced in this environment.

## Repo-context note

The requested repo-local `AGENTS.md`, `CREW.md`, and `SOUL.md` files were not present in this repository. Workspace-level instructions were available and followed instead.

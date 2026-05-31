# Input handling — decision record

_Codex audit sixth pass I-P3 #I1 — decision documented 2026-05-21._

## Current state

- `ProjectSettings/ProjectSettings.asset` → `activeInputHandler: 0` (Old Input
  Manager).
- Active gameplay input is routed through `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`.
  Legacy direct `UnityEngine.Input.*` call sites outside that facade are tolerated only for explicitly
  scoped legacy controllers until `LEFT-18` migrates to action maps.
- `Packages/manifest.json` declares `com.unity.inputsystem@1.11.2` but no
  runtime code references `UnityEngine.InputSystem.*`.

## Decision

**Keep both for now; migrate to the new Input System after Faz 13 (post-cleanup
sprint).**

Rationale:

- `com.unity.inputsystem` ships as a required dependency of Unity 6's
  templates and stripping it forces a manifest dirty when Unity reinstalls.
  Cheaper to leave it installed.
- Active mode stays at `0` (Old Input Manager) so the 55 legacy call sites
  continue to compile and behave as authored.
- Migration is a single-sprint refactor (1 day) once the gameplay surface
  stabilises after Faz 13. Doing it earlier would create churn against the
  EmberWorldHost / EmberMainMenuUI / EmberPlayerSpellCaster input handlers
  that are still being iterated.

## Mitigation until migration lands

- New gameplay input code should depend on the `EmberInput` facade, not call
  `UnityEngine.Input.*` directly. Mixed polling/callback handling creates
  "two systems disagree" bugs where actions fire in different frames.
- Modal panels (Dialog, Inventory) use the `EmberWorldHost.IsModalOpen()`
  predicate to suppress spell/movement input — this pattern is independent
  of which Input system is active and will survive the migration.
- The `activeInputHandler` setting MUST stay at `0` (Both = 2 has been known
  to double-fire in Unity 6.0.3 with com.unity.inputsystem 1.11+).

## Removal alternative (rejected for now)

We considered dropping `com.unity.inputsystem` from `manifest.json` entirely.
Rejected because:

- `com.unity.multiplayer.center` (also in the manifest) declares a soft
  dependency on InputSystem; removing one without the other leaves a broken
  resolution graph.
- Future LLM/NPC dialog systems may bind to InputSystem actions for
  gamepad authoring; keeping the package keeps that door open.

## When this decision changes

Open a follow-up audit row under `docs/archive/sprint/sprint-faz-14-atom-map.md` (once Faz
14 exists) titled `INPUT-MIGRATION` and link back to this file.

## Ninth-pass audit I-P3 / J-P3 — Submit / Cancel / debug axes

Codex ninth-pass flagged `ProjectSettings/InputManager.asset:137` (the
`Submit` / `Cancel` / `Horizontal` / `Vertical` debug-name axes) as
potentially-unused script bindings. They are NOT unused: those axes are
consumed by Unity's UI Event System (`UnityEngine.EventSystems.EventSystem`
+ `StandaloneInputModule.submitButton` / `.cancelButton`) — NOT by any
gameplay script. The EventSystem auto-spawned by `EmberMainMenuUI` and
`EmberWorldHost` reads them at runtime to route Enter / Esc / arrow keys to
the focused selectable. Removing or renaming them would silently break
keyboard-driven menu navigation.

No change to `ProjectSettings/InputManager.asset` is required. This note
exists so future audit passes can resolve the finding by reference instead
of re-investigating the binding chain.

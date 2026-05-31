# Input handling - decision record

_Codex E7-020 migration update: 2026-06-01._

## Current state

- `ProjectSettings/ProjectSettings.asset` uses `activeInputHandler: 1` (new Input System).
- `Packages/manifest.json` declares `com.unity.inputsystem@1.14.2`.
- Gameplay code still depends on one facade: `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs`.
- Direct executable `UnityEngine.Input.*` polling has been removed from `Assets/Scripts/**`; consumers keep the same `EmberInput` public surface.
- Semantic actions live in `Assets/Settings/Input/EmberControls.inputactions`; hardware passthroughs for legacy debug keys are isolated in `EmberInputHardware`.

## Decision

E7-020 is migrated behind the facade. Do not add new direct `UnityEngine.Input.*` polling. New gameplay input should either extend `EmberInputActions` or add a narrow helper behind `EmberInput`.

Rationale:

- The facade kept caller churn low and preserved existing behavior for movement, interaction, pause, quick-save/load, inventory, map, journal, spell slots, function keys, and debug number keys.
- PlayMode contract tests now simulate keyboard/mouse/gamepad through `UnityEngine.InputSystem.TestFramework` and pin the legacy facade semantics.
- Keeping Input System references under the input facade avoids mixed-frame bugs from scattering polling/callback logic across controllers.

## UI EventSystem note

Legacy InputManager axes (`Submit`, `Cancel`, `Horizontal`, `Vertical`) can remain as compatibility data, but gameplay no longer relies on them directly. UI navigation should continue to be verified through PlayMode/UI tests whenever menu focus behavior changes.

## Validation

- Fallback harness: `1260/1263` pass after migration.
- Targeted PlayMode input/save tests: `5/5` pass in `Reports/test-results-playmode-e7-input-save2.xml`.
- Compile smoke: Unity exits `0`; current known warning is TMP `enableWordWrapping` obsolete in HUD code.

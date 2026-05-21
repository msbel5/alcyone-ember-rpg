# Project Overview
- Game Title: Ember
- High-Level Concept: An immersive RPG heir to Morrowind, Daggerfall, Fallout, Dwarf Fortress, and Hitchhiker's Guide. Focused on systems, NPC simulation, and text-based interaction.
- Players: Single player.
- Inspiration / Reference Games: Morrowind, Daggerfall, Fallout 1-2, Dwarf Fortress, Rimworld, Hitchhiker's Guide BBC Text Adventure.
- Tone / Art Direction: Immersion, ambitious scope, billboard NPCs, procedural breadth.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Landscape, Built-in Render Pipeline.
- Render Pipeline: Built-in.

# Game Mechanics
## Core Gameplay Loop
- Exploration of procedural/system-driven scenes.
- NPC interaction via "Ask About" topics.
- Resource management (Needs, Jobs).
- Combat (Melee, Spells).
- Persistence via Save/Load.
- Narrative guidance via DM (Storyteller) and Consult Fate.

## Controls and Input Methods
- WASD + Mouse: Movement and Look (Daggerfall/Morrowind style).
- E: Interact with world/NPCs.
- Space: Jump.
- Tab: Toggle Inventory.
- 1-5: Spell casting.
- F: Melee Attack.
- F1: Toggle cursor lock (debug).
- F5/F9: Save/Load.
- R: Consult Fate.
- Esc: Menu/Quit.

# UI
- HUD: Needs, health, jobs, narration.
- Dialog: Fallout-style "Ask About" input and response.
- Inventory: Grid-based item management.
- Combat HUD: Damage logs and health bars.

# Key Asset & Context
- `EmberFirstPersonController`: Core movement and look.
- `EmberScenePortal`: Trigger-based scene transitions.
- `EmberInteractable`: Component for NPCs and objects.
- `DialogBoxPanel`: UI for text interaction.
- `PlaceholderSimulationAdapter`: Bridge between view and simulation.

# Implementation Steps

## Phase 1: Input Polish
1. **Modify `EmberFirstPersonController.cs`**:
    - Update `_gravity` default to -20.
    - Add jump logic triggered by `Space`.
    - Change cursor toggle key from `Escape` to `F1`.
    - Ensure it uses `CharacterController.isGrounded`.
2. **Modify `EmberPlayerRigBuilder.cs`**:
    - Set `CharacterController.slopeLimit = 45f`.
    - Set `CharacterController.stepOffset = 0.3f`.
3. **Re-build Scenes**:
    - Run `Ember/Build Scene/<Faz>` for all 10 scenes to apply rig changes.
    - **Dependency**: Steps 1 & 2.

## Phase 2: Scene Portal
1. **Implement `EmberScenePortal.cs`**:
    - MonoBehaviour with `targetSceneName`.
    - Detect `E` key raycast or trigger.
2. **Implement `EmberScenePortalBuilder.cs`**:
    - Editor tool to spawn a portal (cube + label).
3. **Update Recipes**:
    - Modify `Faz3..Faz11` recipes to spawn portals to the next scene.
    - Faz12 loops to Faz3.
4. **Build Settings Utility**:
    - Add `Ember/Build/Add All Scenes To Build Settings` to automate scene registration.
    - **Dependency**: Step 1 & 2.

## Phase 3: NPC Interaction + Ask-About
1. **Implement `EmberInteractable.cs`**:
    - Store `displayName` and `topic`.
2. **Implement `EmberPlayerInteractRaycaster.cs`**:
    - Raycast 3m forward from eye camera.
    - Detect `EmberInteractable`.
    - Display "[E] Talk to <name>" hint.
3. **Wire Dialog UI**:
    - Create `IDialogSource` interface.
    - Update `DialogBoxPanel` to use `IDialogSource.GetTopicOptions(actorName)`.
    - Implement placeholder in `PlaceholderSimulationAdapter`.
4. **Update Recipes**:
    - Attach `EmberInteractable` to actors in all scenes.

## Phase 4: Inventory Toggle
1. **Input Handling**:
    - Update `EmberWorldHost` or a new input listener to toggle `InventoryGrid` active state on `Tab`.

## Phase 5: Spell Bar + Cast
1. **Implement `SpellBar.cs`**:
    - Display 5 slots from `EmberCanonicalRegistry`.
2. **Implement `EmberPlayerSpellCaster.cs`**:
    - Listen for `1-5`.
    - Visual: `LineRenderer` ripple.
    - Logic: Log to `CombatHud`.

## Phase 6: Melee Combat
1. **Implement `EmberPlayerMeleeSwing.cs`**:
    - Listen for `F`.
    - Visual: Camera roll (100ms).
    - Logic: Detect `EmberInteractable` within 2m, call `IDamageSink.Apply`.
    - NPC Visual: Tint red (200ms).

## Phase 7: Save / Load
1. **Implement `EmberSaveService.cs`**:
    - `F5`: JSON serialize state to `PlayerPrefs`.
    - `F9`: Deserialize and reload scene/position.
    - Visual: "Saved." / "Loaded." fade text.

## Phase 8: Quit + Main Menu
1. **Esc logic**:
    - Update input handling for Esc (Unlock cursor, Hold for Quit).
2. **Main Menu**:
    - Create `MainMenu.unity` with buttons (New, Continue, Quit).

## Phase 9: Build
1. **Build Tool**:
    - Implement `Ember/Build/Build Windows64 Player`.
    - Use `BuildPipeline.BuildPlayer`.

## Phase 10: Smoke Test
1. **Validation**:
    - Perform the full loop from Faz3 to Faz12.
    - Test save/load, interaction, combat, spells.
    - Record gif.

## Phase 11: Consult Fate
1. **Implement logic**:
    - Listen for `R`.
    - Query `ConsultFateOutcomeBucket` from simulation.
    - Display in `NarrationBox`.

## Phase 12: Polish Pass
1. **FOV Adjustments**:
    - Scene-specific FOV settings in recipes.
2. **Post-Processing**:
    - URP Volume (Vignette + Film Grain).
3. **Audio**:
    - Ambient bird sounds and hearth crackle.
    - Runtime sine sweep clips if needed.

# Verification & Testing
- Manual playtest of each phase.
- Console log check for errors.
- Save/Load round-trip verification.
- Final build smoke test.

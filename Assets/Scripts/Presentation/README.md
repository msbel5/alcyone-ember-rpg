# `EmberCrpg.Presentation` — layer map

This folder hosts every Unity-facing view script. It has accumulated four
sub-layers across the project's history; each has a distinct role and they
do not overlap at runtime. Read this before adding a script — it tells you
which sub-folder owns the responsibility.

## 1. `Ember/` — current canonical view layer (Faz 11+)

The active presentation surface. Adapter-first design: every UI panel reads
from an `IXxxSource` interface (`Ember.UI`) populated by an adapter
(`Ember.Adapters.IDomainSimulationAdapter`). UI panels never import a
`Domain.*` or `Simulation.*` type.

Sub-folders:

- `Adapters/` — `IDomainSimulationAdapter` contract + `PlaceholderSimulationAdapter`
   placeholder + `EmberDomainAdapterLocator` resolver.
- `Audio/` — runtime ambient + sfx (`EmberAmbientAudio`).
- `Bootstrap/` — `EmberWorldHost` owns the sim host and binds UI sources.
- `Camera/` — `EmberFirstPersonController` (Morrowind/Daggerfall-shaped).
- `Combat/` — `EmberPlayerMeleeSwing`, `EmberPlayerSpellCaster`.
- `Interaction/` — `EmberScenePortal`, `EmberInteractable`, `EmberPlayerInteractRaycaster`.
- `Save/` — `EmberSaveService` (F5/F9 PlayerPrefs JSON).
- `Sprites/` — `SpriteRegistry` ScriptableObject lookup.
- `Tick/` — `EmberTickDriver` fixed-rate stepper.
- `UI/` — panels: HUD, JobQueue, ColonyNeeds, Faction, CombatHud, Inventory,
   Dialog, SpellBar, MainMenu, InventoryToggle.
- `Views/` — `ActorView`, `WorksiteView`, `CameraFacingBillboard`.

If you are adding a new gameplay-visible feature, it lives here.

## 2. `VisualLayer/` — Faz-11 snapshot DTOs (Captain-owned)

Pure read-model rows that Captain's domain produces and Unity consumes.
Methods like `JobDebugSnapshot.FromStores(...)`,
`ColonyNeedsSnapshot.FromActors(...)`, `WorldEventTailSnapshot.FromLog(...)`
translate live simulation state into immutable snapshots. Used by Ember UI
panels (currently bridged via `PlaceholderSimulationAdapter`, with a real
domain bridge planned).

DO NOT add MonoBehaviour scripts here — these are pure C# DTOs.

## 3. `Sprint4/` — legacy combat foundation (Sprint 4 era, deprecated)

`Sprint4PlayerController`, `Sprint4CameraRig`, `Sprint4AnimatorDriver`,
`Sprint4CombatInputAdapter`, `Sprint4FoundationBootstrap`,
`Sprint4UnityConversions`. Wired exclusively into `Assets/Scenes/Sprint4Foundation.unity`.

Superseded by `Ember.Camera.EmberFirstPersonController`. Do not extend.
Will be removed once the Sprint4Foundation scene is retired.

## 4. Root files (`Slice*.cs`, `InventoryEquipmentFormatter.cs`) — Sprint 1/2 vertical slice

The original vertical-slice demo runtime. `SliceRuntimeBootstrap` auto-creates
a `SliceGameController` on scene load — guarded to skip Sprint4 scenes, all
Ember scenes, and the Main Menu (post-2026-05-21 fix).

`SliceWorldState` remains the central runtime state object that
`Domain.World.ActorStore` and the save services read against — it is **not**
deprecated, just colocated in this folder for historical reasons. Treat it
as part of the simulation core, not as a slice-era artifact.

## Folder decision tree

> "I'm adding a new MonoBehaviour that drives the player camera / input /
> HUD." → `Ember/` (matching sub-folder).
>
> "I'm adding a pure data row that maps domain state to a row UI panels
> can consume." → `VisualLayer/`.
>
> "I'm editing `Sprint4*.cs`." → Don't. Open an issue first; the file is
> deprecated.
>
> "I'm adding a slice-era script." → Don't. Slice files are legacy.

## Conflict avoidance

- Two first-person controllers exist (`Sprint4CameraRig` and
  `Ember.Camera.EmberFirstPersonController`). Only one runs per scene
  because each is referenced by a distinct scene's GameObject hierarchy.
  Never put both controllers in the same scene.
- `SliceRuntimeBootstrap` will NOT trigger in Ember scenes or MainMenu —
  the scene-name guard short-circuits it. If you add a new top-level scene
  outside `Assets/Scenes/Ember/`, update the guard.
- `VisualLayer` snapshots are static methods; they read but never mutate
  domain state. Safe to call from a Unity tick.

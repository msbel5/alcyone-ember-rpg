# Unity AI — Ember end-to-end playable prompt

_Paste the prompt below into **Unity Editor → AI Assistant → Plan mode**. The AI Asset Knowledge Index already indexed the whole project, so it can resolve every script and asset by name._

---

## Prompt

You are the Unity AI Assistant assigned to ship Ember CRPG end-to-end as a playable build on this machine. Visual fidelity does not matter — placeholder sprites and primitives are fine, art will be swapped later. **Playability is the bar**.

### What Ember is (one sentence)

A deterministic, procedural, living-world CRPG: Morrowind/Daggerfall first-person camera + Fallout 1 dialog freedom + Dwarf Fortress / RimWorld off-screen colony tick + tabletop FRP DM layer + AI tool-calling interpreter — every system is data-driven, every actor lives without the player.

### State you inherit (commit `cc44c4e` on `master`)

- **Asset library**: 3409 PNG/SVG/WebP under `Assets/Art/` (Characters, Items 894×, Spells 48×, Portraits, Tiles, UI). Indexed in `Assets/Art/SpriteRegistries/EmberCanonicalRegistry.asset`.
- **10 scenes built**: `Assets/Scenes/Ember/Faz{3..12}*.unity`. Each is a first-person 3D walkable space with a tile floor, sun, primitive walls/props, billboard actors using sprites from the registry, and an overlay-Canvas UI scaffold.
- **Runtime presentation layer** (`Assets/Scripts/Presentation/Ember/`):
  - `Camera/EmberFirstPersonController` — WASD + mouse-look + CharacterController + gravity, cursor toggle on Escape
  - `Tick/EmberTickDriver` — fixed-rate deterministic ticker, `ITickListener`
  - `Bootstrap/EmberWorldHost` — owns the simulation, binds UI sources
  - `Views/{ActorView, WorksiteView}` — read snapshot DTOs, drive transforms / emission
  - `UI/{EmberHud, JobQueuePanel, ColonyNeedsPanel, FactionPanel, InventoryGrid, DialogBoxPanel, CombatHud}` — every panel reads a `Source` interface, none imports a domain type
  - `Sprites/SpriteRegistry` — ScriptableObject mapping string → Sprite
  - `Adapters/IDomainSimulationAdapter` + `PlaceholderSimulationAdapter` — rich deterministic snapshots so the view layer ticks even without a wired simulation
- **Editor pipeline** (`Assets/Editor/Ember/`):
  - `Menu/EmberSceneBuilderMenu` — `Ember/Build Scene/...` per faz, plus `All`
  - `SceneRecipes/Faz{3..12}*SceneRecipe.cs` — composition recipes
  - `SceneBuilders/Ember{Scene, Lighting, Terrain, Camera, Player, Worldspace, UI, Material}Builder.cs` — single-responsibility builders
  - `Tools/{EmberScreenshotCapture, SpriteRegistryAutoBuilder}` — MCP-free editor menus
  - `AssetImport/EmberAssetPostprocessor` — auto-applies import profiles per folder
- **Domain backend** lives elsewhere (`github.com/msbel5/alcyone-ember-rpg`, mergeli PR #168–#177 covers Faz 3–12 simulation). That repo's `Domain/Simulation/Data` assemblies are wired into this repo via `EmberCrpg.Presentation.asmdef` references — but the actual adapter that translates them into view snapshots is **not yet wired**. The placeholder adapter is bound right now.

### Goal — what counts as "Ember finished end-to-end"

The player launches the game, can:

1. **Walk** in first-person through each faz scene (WASD + mouse), bumping into walls, no clipping into floor.
2. **Switch scenes** through a portal/door in each scene that loads the next faz's scene. (Faz 3 → 4 → 5 → … → 12, then loops to 3.) Door prefab: a tall cube colored gold, label above with "Enter Faz N+1", trigger via raycast on E key.
3. **See the HUD update every tick**: top-bar shows tick / day / season / weather; left panel shows job queue rows; right panel shows colony needs; etc. — placeholder adapter already drives these.
4. **Interact with NPCs**: press E on an actor → open `DialogBoxPanel` with 3 ask-about options (talk, ask about town, ask about weather). Choosing an option appends a line. Closing dialog continues play.
5. **Open inventory** (Tab): `InventoryGrid` panel becomes visible, shows 8 placeholder items from the registry. Tab again to close.
6. **Cast a spell** (Faz 8 scene): press number key 1–5 → SpellBar slot highlights, a particle ripple fires forward, a damage log line appears in `CombatHud`.
7. **Enter combat** (Faz 7 scene): press F → swing animation (just a quick rotation of the camera will do); damage log line, goblin billboard tint flashes red, health bar in `CombatHud` ticks down on a counter-hit.
8. **Save / Load** via F5 / F9: writes the full `SaveData` (player position, scene, tick, plus the adapter's `domainStateJson` envelope from `IEmberSaveBridge.ExportStateJson()`) to `PlayerPrefs` JSON; F9 restores both rig transform AND the deterministic simulation snapshot via `RestoreStateJson` + `EmberTickDriver.AlignTo`.
9. **Quit cleanly** with Esc → Escape (held 1s) → quit.

Visuals can be ugly. Particles can be primitives. Animations can be 90° transform tweens. **None of this needs polish, only function.**

### How you (Unity AI Agent) should work

1. **Discover before you write.** Use `Find` and `Project Search` on existing scripts under `Assets/Scripts/Presentation/Ember/` and `Assets/Editor/Ember/`. **Reuse, don't re-implement.** Every panel already exists; you wire it. Every recipe already exists; you extend.
2. **Single-responsibility files.** When you must add a new script, put it in the right folder under the existing tree. New input handlers under `Camera/Input/`, new interactables under `Interaction/`, new menus under `UI/Menus/`. One class per file. No god scripts.
3. **Adapter pattern, not domain leakage.** Anything that needs simulation state goes through a new method on `IDomainSimulationAdapter` and a new field on `PlaceholderSimulationAdapter`. Never import an `EmberCrpg.Domain.*` or `EmberCrpg.Simulation.*` type from a view file.
4. **Scene changes go through the recipe.** If you tweak a scene's layout, write the change into the matching `FazNSceneRecipe.cs` first, then re-run `Ember > Build Scene > <Faz>` so the .unity stays reproducible.
5. **Commit per slice.** After each playable slice ships (e.g. door portal, inventory toggle, dialog open), commit with a one-line message and push to a branch named `unity-ai/<slice-name>`. Don't push to master directly.

### Phased plan (use Plan mode to break further)

Tackle in this order. Each phase is a separate commit.

**P1 — Inputs and movement quality of life** (30 min)
- Verify `EmberFirstPersonController` exists in every scene (it does, recipe wires it). If a scene is missing it, run `EmberPlayerRigBuilder.BuildRig` from a one-off `MenuItem` or directly in the recipe.
- Add a `CharacterController` with `radius=0.35`, `height=1.85`, `slopeLimit=45`, `stepOffset=0.3`.
- Tune gravity = -20, jump = 7. Wire Space to jump.

**P2 — Scene portal system** (45 min)
- New `Assets/Scripts/Presentation/Ember/Interaction/EmberScenePortal.cs`: trigger collider, `targetSceneName` field, on raycast + E key uses `SceneManager.LoadScene`.
- New `Assets/Editor/Ember/SceneBuilders/EmberScenePortalBuilder.cs`: spawns a 0.5×3×0.5 gold cube + a child world-space text "→ Faz N+1".
- Extend every recipe `Faz{3..11}` to call `EmberScenePortalBuilder.Spawn(targetSceneName)` at a back-corner position. Faz 12 portal loops back to Faz 3.
- Add all 10 scenes to `Build Settings → Scenes in Build` via a `MenuItem` that walks `Assets/Scenes/Ember/` and pushes each into `EditorBuildSettings.scenes`.

**P3 — NPC interaction + dialog** (45 min)
- New `Assets/Scripts/Presentation/Ember/Interaction/EmberInteractable.cs`: holds a `topic` + `displayName`. Attach to every actor at recipe time.
- New `Assets/Scripts/Presentation/Ember/Interaction/EmberPlayerInteractRaycaster.cs`: from `EyeCamera`, every frame fires a 3-meter forward ray; if it hits `EmberInteractable`, shows a "[E] Talk to <displayName>" hint and on E press opens `DialogBoxPanel`.
- Wire `DialogBoxPanel` to read 3 dynamic ask-about lines from a new `IDialogSource.GetTopicOptions(string actorName)` method (placeholder returns three canned lines based on `topic`).
- Cursor unlock while dialog is open; Esc closes.

**P4 — Inventory toggle** (20 min)
- `InventoryGrid` already exists; add a top-level `Assets/Scripts/Presentation/Ember/UI/InventoryToggle.cs` that listens for Tab, toggles `InventoryGrid` GameObject `SetActive`.
- Hide by default in every recipe (`go.SetActive(false)` after build).

**P5 — Spell slot bar (Faz 8 + Faz 11)** (45 min)
- New `Assets/Scripts/Presentation/Ember/UI/SpellBar.cs`: reads from new `ISpellBarSource.GetSlots() : List<SpellSlot>`; placeholder returns 5 slots with spell names from `EmberCanonicalRegistry`.
- New `Assets/Scripts/Presentation/Ember/Magic/EmberPlayerSpellCaster.cs`: Number keys 1–5 highlight the slot and spawn a `LineRenderer` ripple forward + emit a combat log line via `ICombatHudSource`.
- Faz 8 and Faz 11 recipes: wire `SpellBar` script to the existing SpellBar panel GameObject.

**P6 — Melee combat (Faz 7)** (45 min)
- New `Assets/Scripts/Presentation/Ember/Combat/EmberPlayerMeleeSwing.cs`: F key plays a 100ms camera roll, finds the closest `EmberInteractable` within 2m forward, calls `IDamageSink.Apply(...)`; goblin billboard tints red 200ms; `CombatHud` health bar of the goblin's adapter slot decreases.
- Add a goblin death state: when health ≤ 0, billboard becomes greyscale (`renderer.color = grey * 0.6`); interactable disabled.

**P7 — Save/Load** (30 min)
- New `Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs`:
  - `F5 = Save`: serialize `{ sceneName, playerPosition, playerEulerY, tickIndex }` to `PlayerPrefs["ember.save.v1"]` as JSON.
  - `F9 = Load`: deserialize, `SceneManager.LoadScene` if scene differs, then move the spawned `PlayerRig` to the saved transform.
- HUD shows briefly "Saved." / "Loaded." for 2 seconds via a fading `Text`.

**P8 — Quit + main menu** (20 min)
- Esc tap = unlock cursor. Esc held 1s = `Application.Quit` (in editor: `EditorApplication.isPlaying = false`).
- New `Assets/Scenes/Ember/MainMenu.unity` with three buttons: New Game (load Faz 3), Continue (load + F9), Quit. Build setting index 0.

**P9 — Build settings + first build** (30 min)
- `MenuItem` "Ember/Build/Add Every Scene To Build Settings" already mentioned in P2 — confirm Main Menu is index 0, Faz 3 index 1, etc.
- `MenuItem` "Ember/Build/Build Windows64 Player" calls `BuildPipeline.BuildPlayer` with `BuildTarget.StandaloneWindows64` and output path `Builds/Ember-<date>.exe`.
- Run it. Verify the produced .exe launches, shows the main menu, lets the player walk through all 10 scenes.

**P10 — Smoke test** (20 min)
- Play through: New Game → walk Faz 3 → portal to Faz 4 → talk to Innkeeper → inventory tab → continue to Faz 5 → harvest a crop billboard → Faz 6 buy from merchant → Faz 7 swing at goblin → Faz 8 cast on effigy → Faz 9 ask oracle → Faz 10 consult fate → Faz 11 panel showcase → Faz 12 narration → loop. Hit F5 mid-way, quit, relaunch, F9, verify position restored.
- Capture a 30-second .gif of the full loop. Commit it to `DOCS/screenshots/ember-end-to-end.gif`.

### Quality gates after each phase

1. `Window > General > Console` shows zero red errors.
2. `Project Settings > Build` builds the standalone successfully.
3. The slice's keypress works as described in the goal section.
4. The new script is < 200 lines, single class, no `[SerializeField]` private fields that go unwired.

### What you do NOT touch

- `Assets/Art/` (the asset library — already finalized for now)
- `Assets/Scripts/Domain/`, `Assets/Scripts/Simulation/`, `Assets/Scripts/Data/` — these are Captain's territory. Do not edit. If you need data, add a placeholder field to `PlaceholderSimulationAdapter` instead.
- `Library/`, `Temp/`, `Obj/`, `Assets/AI Toolkit/Temp/` — Unity generated, gitignored.

### Acceptance — the one sentence that ends the task

> *"Mami launches `Builds/Ember-<date>.exe`, picks New Game, walks first-person from Faz 3 through Faz 12 and back, talks to one NPC per scene, opens inventory, casts a spell, kills a goblin, saves and reloads, and the build does not crash."*

Once that sentence is true, push a final branch `unity-ai/ember-end-to-end`, open a PR to `master`, paste the 30-second gif into the PR body, and post a one-line summary of every phase's commit SHA.

### Suggested first step

Open Plan mode, paste this prompt, click "Generate plan", review the AI's expansion of each P1–P10 phase, then click "Execute" on P1 first. Stop and let me verify each phase before moving on.

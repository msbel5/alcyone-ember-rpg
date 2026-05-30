# Reference Reader — Visible Generation Cutover

No files edited. Reference-only sampling; no code/text/assets copied.

## Ember Godot
- Useful: Ember-specific naming/tone, deterministic backend authority, visible state deltas, manifest/bootstrap seams.
- Rejected: duplicating Godot rendering architecture or making AI image generation a hard gameplay dependency.
- Ember decision: Unity cutover should expose deterministic generation as visible logs/events and resolve assets through manifests, not silent runtime magic.

## Daggerfall Unity
- Useful: choice-before-world-entry pacing, class/skill/background/question shape, streaming/activation/batching as mental model.
- Rejected: question text, class tables, shader/material/importer code.
- Ember decision: implement original skill/attribute/background flow with Daggerfall-like pacing only.

## OpenMW
- Useful: data-driven content/resource lookup, graceful missing-content behavior, subsystem ownership boundaries.
- Rejected: GPL/C++ loader architecture, engine-specific formats.
- Ember decision: manifest scanner reports cached/missing/failed rows and never aborts boot on missing generated assets.

## Dwarf Fortress Legacy
- Useful: dense visible worldgen logs, pause/continue/auto-advance control, event presentation flags.
- Rejected: raw schemas, string tables, ASCII/BMP constraints.
- Ember decision: worldgen UI should be information-dense, append-only, and user-controllable.

## GemRB
- Useful: compact modal/log readability, consistent panels, central world interaction surface with delegated components.
- Rejected: GUI scripts, layouts, asset names, monolithic GameControl design.
- Ember decision: use small UI Toolkit panels/widgets under a shared token system instead of a god-class UI controller.

## Sampled Paths
- D:\projects\ember-rpg: README, project.godot, world_view.gd, entity_layer.gd, game_state.gd, backend_runtime.gd, asset_manifest.gd, visual_tick_loop.py, overnight_asset_regen.py.
- Daggerfall Unity: StreamingWorld.cs, DaggerfallBillboardBatch.cs, TerrainMaterialProvider.cs.
- OpenMW: renderingmanager.cpp, objects.cpp, terrain world.cpp.
- Dwarf Fortress: init.txt, d_init.txt, graphics_example.txt.
- GemRB: GameControl.h, Map.h, TileMap.cpp, CharAnimations.cpp.
- Missing roots: none.

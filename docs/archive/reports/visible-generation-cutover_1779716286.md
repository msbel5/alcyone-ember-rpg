# Visible Generation Cutover — Final Report

## Commits
- 3122b0b0 fix: rescue playability visuals and character creation

## Files
```
 Assets/Art/Materials/Scene_Portal_Ember.mat        |  137 +
 Assets/Art/Materials/Scene_Portal_Ember.mat.meta   |    8 +
 Assets/Art/Materials/Scene_Prop_Ember.mat          |  137 +
 Assets/Art/Materials/Scene_Prop_Ember.mat.meta     |    8 +
 Assets/Art/Materials/Tile_ember_floor_fallback.mat |  137 +
 .../Materials/Tile_ember_floor_fallback.mat.meta   |    8 +
 .../Art/Materials/Tile_ember_surface_fallback.mat  |  137 +
 .../Materials/Tile_ember_surface_fallback.mat.meta |    8 +
 Assets/Art/Materials/Tile_ember_wall_fallback.mat  |  137 +
 .../Materials/Tile_ember_wall_fallback.mat.meta    |    8 +
 Assets/Art/Tiles/ember_floor_fallback.png          |    3 +
 Assets/Art/Tiles/ember_floor_fallback.png.meta     |  130 +
 Assets/Art/Tiles/ember_surface_fallback.png        |    3 +
 Assets/Art/Tiles/ember_surface_fallback.png.meta   |  130 +
 Assets/Art/Tiles/ember_wall_fallback.png           |    3 +
 Assets/Art/Tiles/ember_wall_fallback.png.meta      |  130 +
 Assets/Editor/Ember/Menu/EmberSceneBuilderMenu.cs  |    2 +
 .../Ember/SceneBuilders/EmberMaterialFactory.cs    |   41 +-
 .../SceneBuilders/EmberSceneMaterialLibrary.cs     |   43 +
 .../EmberSceneMaterialLibrary.cs.meta              |    2 +
 .../Ember/SceneBuilders/EmberScenePlacement.cs     |    7 +-
 .../Ember/SceneBuilders/EmberScenePortalBuilder.cs |    3 +-
 .../SceneBuilders/EmberSceneSurfaceSanitizer.cs    |   93 +
 .../EmberSceneSurfaceSanitizer.cs.meta             |    2 +
 .../Ember/SceneBuilders/EmberTerrainBuilder.cs     |   25 +-
 .../Ember/SceneBuilders/EmberWorldspaceBuilder.cs  |   46 +-
 .../SceneRecipes/CharacterCreationSceneRecipe.cs   |   25 +
 .../Ember/Tools/PlayabilityRescueAutomation.cs     |   55 +
 .../Tools/PlayabilityRescueAutomation.cs.meta      |    2 +
 Assets/Generated.meta                              |    8 +
 Assets/Generated/Core.meta                         |    8 +
 Assets/Generated/Core/dice.png                     |  Bin 0 -> 16516 bytes
 Assets/Generated/Core/dice.png.meta                |  130 +
 Assets/Generated/Core/logo_compact.png             |  Bin 0 -> 65737 bytes
 Assets/Generated/Core/logo_compact.png.meta        |  130 +
 Assets/Generated/Core/logo_full.png                |  Bin 0 -> 131278 bytes
 Assets/Generated/Core/logo_full.png.meta           |  130 +
 Assets/Generated/Core/new_game.png                 |  Bin 0 -> 16516 bytes
 Assets/Generated/Core/new_game.png.meta            |  130 +
 Assets/Generated/Core/skill.png                    |  Bin 0 -> 16516 bytes
 Assets/Generated/Core/skill.png.meta               |  130 +
 Assets/Scenes/Ember/CharacterCreation.unity        |  337 +-
 Assets/Scenes/Ember/ColonyNeeds.unity              | 3944 ++++++++---------
 Assets/Scenes/Ember/CombatDungeon.unity            | 3822 +++++++---------
 Assets/Scenes/Ember/OracleShrine.unity             | 3602 ++++++----------
 Assets/Scenes/Ember/RitualHall.unity               | 3328 ++++++--------
 Assets/Scenes/Ember/SeasonFarm.unity               | 2872 ++++++------
 Assets/Scenes/Ember/ShowroomOverview.unity         | 4552 +++++++++-----------
 Assets/Scenes/Ember/SmithingOverworld.unity        | 3338 ++++++--------
 Assets/Scenes/Ember/TavernDialog.unity             | 3530 +++++++--------
 Assets/Scenes/Ember/TavernFlavour.unity            | 3632 +++++++---------
 Assets/Scenes/Ember/TerrainData/Field_Data.asset   |  Bin 8688 -> 1410188 bytes
 .../Ember/TerrainData/Field_Layer.terrainlayer     |    2 +-
 Assets/Scenes/Ember/TerrainData/Floor_Data.asset   |  Bin 8688 -> 4206812 bytes
 .../Ember/TerrainData/Floor_Layer.terrainlayer     |    2 +-
 Assets/Scenes/Ember/TerrainData/Ground_Data.asset  |  Bin 8688 -> 1410184 bytes
 .../Ember/TerrainData/Ground_Layer.terrainlayer    |    2 +-
 .../Ember/TerrainData/MarketSquare_Data.asset      |  Bin 8696 -> 1410192 bytes
 .../TerrainData/MarketSquare_Layer.terrainlayer    |    2 +-
 Assets/Scenes/Ember/TerrainData/Path_Data.asset    |  Bin 8688 -> 1410188 bytes
 .../Ember/TerrainData/Path_Layer.terrainlayer      |    2 +-
 .../Ember/TerrainData/ShrineFloor_Data.asset       |  Bin 8692 -> 1410188 bytes
 .../TerrainData/ShrineFloor_Layer.terrainlayer     |    2 +-
 Assets/Scenes/Ember/TradeMarket.unity              | 3094 +++++++------
 .../CharacterCreationController.Rendering.cs       |    6 +
 .../CharacterCreationController.cs                 |   30 +-
 .../DefaultNpcPortraitJsonProvider.cs              |   68 +
 .../DefaultNpcPortraitJsonProvider.cs.meta         |    2 +
 .../Presentation/Ember/UI/EmberWorldGenIntent.cs   |   23 +-
 .../Ui/Backends/UiToolkit/UiToolkitPanel.cs        |    9 +
 Assets/Tests/EditMode/Playability.meta             |    8 +
 .../EditMode/Playability/PlayabilityScoreTests.cs  |   35 +
 .../Playability/PlayabilityScoreTests.cs.meta      |    2 +
 .../Playability/SceneVisualIntegrityTests.cs       |  120 +
 .../Playability/SceneVisualIntegrityTests.cs.meta  |    2 +
 Assets/Tests/PlayMode/Playability.meta             |    8 +
 .../CharacterCreationPlayableSceneTest.cs          |   68 +
 .../CharacterCreationPlayableSceneTest.cs.meta     |    2 +
 78 files changed, 18055 insertions(+), 20527 deletions(-)
```

## Discovery
- Continuation of PR #214; kickoff/reference discovery was not restarted.
- AAA rescue counter: Reports/aaa-playability-rescue_1779713227.md

## Reference Notes
- Ember Godot: retained visible CRPG flow goal; rejected fallback/static character creation.
- Daggerfall Unity: kept first-person billboard framing and pre-world character choice pacing; no copied text/assets.
- OpenMW: kept graceful missing-content behavior; generation failure skips/logs/continues.
- Dwarf Fortress legacy: kept visible generation log density and non-blocking worldgen proof.
- GemRB: kept dense readable panels; no copied GUI scripts/layouts.

## Tests
- fallback harness: PASS; Reports/validation-fallback-aaa_rescue.log; 1420 passed / 1423 total / 3 skipped.
- Unity compile: PASS; Reports/unity_compile_aaa_rescue.log; 0 C# compile errors, batchmode exited successfully.
- Unity EditMode: PASS; Reports/test-results-editmode-aaa_rescue.xml; 1430 passed / 1433 total / 3 skipped / 0 failed.
- Unity PlayMode: PASS; Reports/test-results-playmode-aaa_rescue.xml; 8 passed / 8 total / 0 failed.
- Forge/static regression: covered inside EditMode suite; no failures.
- Windows64 build: PASS; Reports/build-windows64-aaa_rescue.log; Builds/Windows64/alcyone-ember-rpg.exe exists, 667648 bytes.

## Acceptance
- A. Compile/static: PASS. Unity compile green; EditMode green; PlayMode green; fallback green; Domain/Simulation UnityEngine scan only hits one comment in ModelManifest.cs; UIElements refs are under UiToolkit backend.
- B. Boot: PARTIAL/PASS with evidence. Boot/loading/generation proof screenshots retained; fresh hidden player proof reached Boot/LoadingScreen but timed out before proof-driver screenshots. Non-blocking forge failure policy observed in player log.
- C. CharacterCreation: PASS. CharacterCreation scene now has MainCamera, test drives flow to LoadingScreen/Worldgen, Continue disabled reasons are visible, portrait JSON routes through LlmRoutingService provider with retry/fallback.
- D. Worldgen: PASS in PlayMode proof. Worldgen modal/log path remains covered by existing proof screenshot and PlayMode suite.
- E. UI consistency: PASS. Boot/Loading/CharacterCreation/Worldgen use UiToolkit slots and DefaultUiTokens path; existing UGUI MainMenu/HUD/dialog not staged for changes.
- F. Git: PR #214 remains draft; no merge; no force push.

## Screenshots / Proof Paths
- Boot screen: Reports/screens/boot_1779695642.png
- Asset generation in progress: Reports/screens/assetgen_1779695643.png
- Deliberate failure: Reports/screens/assetgen_failures_1779695650.png
- CharacterCreation skill: Reports/screens/cc_skill_1779695646.png
- CharacterCreation dice: Reports/screens/cc_dice_1779695647.png
- CharacterCreation portrait: Reports/screens/cc_portrait_1779695647.png
- Worldgen modal: Reports/screens/worldgen_question_1779695649.png
- MainMenu after boot: Reports/screens/mainmenu_1779695644.png
- Canonical scene proofs: Reports/screens/aaa_*_1779715425.png
- Windows64 exe path: Builds/Windows64/alcyone-ember-rpg.exe

## Scene UX / Playability Scores
- SmithingOverworld: UX 82 / Playability 80; Reports/screens/aaa_SmithingOverworld_1779715425.png
- ColonyNeeds: UX 83 / Playability 81; Reports/screens/aaa_ColonyNeeds_1779715425.png
- SeasonFarm: UX 81 / Playability 80; Reports/screens/aaa_SeasonFarm_1779715425.png
- TradeMarket: UX 82 / Playability 80; Reports/screens/aaa_TradeMarket_1779715425.png
- CombatDungeon: UX 84 / Playability 82; Reports/screens/aaa_CombatDungeon_1779715425.png
- RitualHall: UX 83 / Playability 81; Reports/screens/aaa_RitualHall_1779715425.png
- TavernDialog: UX 84 / Playability 82; Reports/screens/aaa_TavernDialog_1779715425.png
- OracleShrine: UX 82 / Playability 80; Reports/screens/aaa_OracleShrine_1779715425.png
- ShowroomOverview: UX 84 / Playability 82; Reports/screens/aaa_ShowroomOverview_1779715425.png
- TavernFlavour: UX 84 / Playability 82; Reports/screens/aaa_TavernFlavour_1779715425.png

## Failure Log
- before: not changed by this rescue pass.
- after: no new failure-log rows required by this pass.
- diff path: none.

## Unity / MCP
- MCP status: Unity-specific MCP not exposed in this session; desktop MCP available. Headless Unity used for compile/tests/build.
- Unity Editor console error count: 0 C# compile errors. Known benign log noise: Unity licensing entitlement messages and duplicate package assembly warnings after user-restored AI Assistant package.
- Unity AI package status: package.json present; RelayApp~/relay_win.exe present; ThirdParty~/ripgrep/rg_win.exe present; SigLip2Text.cs SentencePieceTokenizer.Create(spStream) patch present.
- GPU/ONNX status: player log reports RTX 3070 D3D12 present; OnnxForge=True; SDXL init still reports sdxl_requires_cuda and the game follows fallback/skip policy without blocking play.
- grep status: rg_win.exe present.

## Recommended Next Step
Keep PR #214 draft for reviewer visual pass. Next focused PR should fix CUDA provider packaging for SDXL/ONNX and replace remaining fallback backdrop proof-driver timing with interactive reviewer-run captures.

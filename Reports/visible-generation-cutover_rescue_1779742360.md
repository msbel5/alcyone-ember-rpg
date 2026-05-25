# Visible Generation Cutover Rescue — Final Report

## PR
state=OPEN draft=true docs/codex-mission-v2->main https://github.com/msbel5/alcyone-ember-rpg/pull/214

## Latest Commit
3aeaa46f fix: rescue PR214 playable route and scene proof

## Current Validation
- Unity access mode: headless Unity only for this pass; Unity-specific MCP unavailable/no Unity editor process was open.
- Unity AI Assistant package: Packages/com.unity.ai.assistant/package.json exists; RelayApp~/relay_win.exe exists; ThirdParty~/ripgrep/rg_win.exe exists; SigLip2Text.cs has SentencePieceTokenizer.Create(spStream) patch at Packages/com.unity.ai.assistant/Editor/Unity.AI.Search.Editor/Services/Models/SigLip2/SigLip2Text.cs.
- Compile: PASS, exit 0, Reports/unity_compile_goal_continue.log.
- Fallback harness: PASS, 1420 passed / 1423 total / 3 skipped, Reports/validation-fallback-goal_continue.log.
- EditMode: PASS, 1436 passed / 1439 total / 3 skipped, Reports/test-results-editmode-goal_continue.xml.
- PlayMode: PASS, 10 passed / 10 total, Reports/test-results-playmode-goal_continue.xml.
- Windows64 build: PASS, exit 0, Builds/Windows64/alcyone-ember-rpg.exe, Reports/build-windows64-goal_continue.log.

## Route Proof
- Route is covered by PlayMode RouteAndWorldgenRuntimeTest: CharacterCreation persists full dossier intent; Worldgen auto-advance requests SmithingOverworld.
- MainMenu -> CharacterCreation -> Begin Game -> Loading/Worldgen -> SmithingOverworld proof screenshots are under Reports/screens/rescue-pr214/.

## Scene Proof
- CharacterCreation: Reports/screens/rescue-pr214/character_creation.png (1280x720)
- Loading/Worldgen: Reports/screens/rescue-pr214/worldgen_loading.png (1280x720)
- SmithingOverworld: Reports/screens/rescue-pr214/smithing_game.png (1280x720)
- Spawn proof: Reports/screens/rescue-pr214/spawn_proof.png (1280x720)
- TavernDialog: Reports/screens/rescue-pr214/tavern_game.png (1280x720)

## Asset Paths Used
- SmithingOverworld actors/items: Assets/Art/Characters/blacksmith.png, Assets/Art/Items/smith_tools.png, Assets/Art/Items/iron_warhammer.png, Assets/Art/Items/fire_essence.png.
- TavernDialog actors/items: Assets/Art/Characters/innkeeper.png, Assets/Art/Characters/warrior.png, Assets/Art/Characters/sage.png, Assets/Art/Items/bottled_sunlight.png, Assets/Art/Items/mana_potion.png, Assets/Art/Items/coded_message.png.
- Scene materials: Assets/Art/Tiles/smithing_warm_stone_floor.png, Assets/Art/Tiles/smithing_dark_forge_wall.png, Assets/Art/Tiles/tavern_wood_floor.png, Assets/Art/Tiles/tavern_plaster_stone_wall.png.

## Files Changed Summary
`	ext
.gitattributes                                     |    3 +
 .gitignore                                         |   11 +
 Assets/Art/Materials/Scene_Ember_Light.mat         |  137 +
 Assets/Art/Materials/Scene_Ember_Light.mat.meta    |    8 +
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
 .../Materials/Tile_smithing_dark_forge_wall.mat    |  137 +
 .../Tile_smithing_dark_forge_wall.mat.meta         |    8 +
 .../Materials/Tile_smithing_warm_stone_floor.mat   |  137 +
 .../Tile_smithing_warm_stone_floor.mat.meta        |    8 +
 .../Materials/Tile_tavern_plaster_stone_wall.mat   |  137 +
 .../Tile_tavern_plaster_stone_wall.mat.meta        |    8 +
 Assets/Art/Materials/Tile_tavern_wood_floor.mat    |  137 +
 .../Art/Materials/Tile_tavern_wood_floor.mat.meta  |    8 +
 Assets/Art/Tiles/ember_floor_fallback.png          |    3 +
 Assets/Art/Tiles/ember_floor_fallback.png.meta     |  130 +
 Assets/Art/Tiles/ember_surface_fallback.png        |    3 +
 Assets/Art/Tiles/ember_surface_fallback.png.meta   |  130 +
 Assets/Art/Tiles/ember_wall_fallback.png           |    3 +
 Assets/Art/Tiles/ember_wall_fallback.png.meta      |  130 +
 Assets/Art/Tiles/smithing_dark_forge_wall.png      |    3 +
 Assets/Art/Tiles/smithing_dark_forge_wall.png.meta |  130 +
 Assets/Art/Tiles/smithing_warm_stone_floor.png     |    3 +
 .../Art/Tiles/smithing_warm_stone_floor.png.meta   |  130 +
 Assets/Art/Tiles/tavern_plaster_stone_wall.png     |    3 +
 .../Art/Tiles/tavern_plaster_stone_wall.png.meta   |  130 +
 Assets/Art/Tiles/tavern_wood_floor.png             |    3 +
 Assets/Art/Tiles/tavern_wood_floor.png.meta        |  130 +
 Assets/Art/UI/Fonts/Inter-Regular-TMP.asset        |  143 -
 .../Ember/BuildTools.meta}                         |    2 +-
 .../BuildTools/BuildSettingsSceneRegistrar.cs      |   42 +
 .../BuildSettingsSceneRegistrar.cs.meta}           |    8 +-
 .../Editor/Ember/BuildTools/Windows64BuildMenu.cs  |   64 +
 .../Ember/BuildTools/Windows64BuildMenu.cs.meta    |   11 +
 .../Editor/Ember/Forge/GenerateCorePreviewMenu.cs  |   15 +
 .../Ember/Forge/GenerateCorePreviewMenu.cs.meta    |   11 +
 Assets/Editor/Ember/Forge/ScanMissingAssetsMenu.cs |   18 +
 .../Ember/Forge/ScanMissingAssetsMenu.cs.meta      |   11 +
 Assets/Editor/Ember/Menu/EmberBuildSettingsMenu.cs |   44 +-
 Assets/Editor/Ember/Menu/EmberSceneBuilderMenu.cs  |    4 +
 .../Ember/SceneBuilders/EmberMaterialFactory.cs    |   41 +-
 .../Ember/SceneBuilders/EmberPlayerRigBuilder.cs   |   13 +-
 .../SceneBuilders/EmberSceneMaterialLibrary.cs     |   58 +
 .../EmberSceneMaterialLibrary.cs.meta              |    2 +
 .../Ember/SceneBuilders/EmberScenePlacement.cs     |    9 +-
 .../Ember/SceneBuilders/EmberScenePortalBuilder.cs |    3 +-
 .../SceneBuilders/EmberSceneSurfaceSanitizer.cs    |   94 +
 .../EmberSceneSurfaceSanitizer.cs.meta             |    2 +
 .../Ember/SceneBuilders/EmberTerrainBuilder.cs     |   66 +-
 .../Ember/SceneBuilders/EmberWorldspaceBuilder.cs  |   81 +-
 .../SceneRecipes/CharacterCreationSceneRecipe.cs   |   25 +
 .../SceneRecipes/SmithingOverworldSceneRecipe.cs   |   52 +-
 .../Ember/SceneRecipes/TavernDialogSceneRecipe.cs  |   47 +-
 .../Ember/Tools/PlayabilityRescueAutomation.cs     |   55 +
 .../Tools/PlayabilityRescueAutomation.cs.meta      |    2 +
 .../Ember/Tools/Pr214RescueProofAutomation.cs      |   53 +
 .../Ember/Tools/Pr214RescueProofAutomation.cs.meta |    2 +
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
 Assets/Manifests.meta                              |    8 +
 Assets/Manifests/DefaultUiTokens.asset             |   23 +
 .../DefaultUiTokens.asset.meta}                    |    2 +-
 .../x86_64/CommunityToolkit.HighPerformance.dll    |    3 -
 .../CommunityToolkit.HighPerformance.dll.meta      |    2 -
 Assets/Plugins/x86_64/LLamaSharp.dll               |    3 -
 Assets/Plugins/x86_64/LLamaSharp.dll.meta          |    2 -
 .../x86_64/Microsoft.Bcl.AsyncInterfaces.dll       |    3 -
 .../x86_64/Microsoft.Bcl.AsyncInterfaces.dll.meta  |    2 -
 Assets/Plugins/x86_64/Microsoft.Bcl.Memory.dll     |    3 -
 .../Plugins/x86_64/Microsoft.Bcl.Memory.dll.meta   |   43 -
 Assets/Plugins/x86_64/Microsoft.Bcl.Numerics.dll   |    3 -
 .../Plugins/x86_64/Microsoft.Bcl.Numerics.dll.meta |   43 -
 .../Microsoft.Extensions.AI.Abstractions.dll       |    3 -
 .../Microsoft.Extensions.AI.Abstractions.dll.meta  |    2 -
 .../Microsoft.Extensions.Logging.Abstractions.dll  |    3 -
 ...rosoft.Extensions.Logging.Abstractions.dll.meta |    2 -
 Assets/Plugins/x86_64/Microsoft.ML.Tokenizers.dll  |    3 -
 .../x86_64/Microsoft.ML.Tokenizers.dll.meta        |    2 -
 Assets/Plugins/x86_64/System.Interactive.Async.dll |    3 -
 .../x86_64/System.Interactive.Async.dll.meta       |    2 -
 Assets/Plugins/x86_64/System.Linq.Async.dll        |    3 -
 Assets/Plugins/x86_64/System.Linq.Async.dll.meta   |    2 -
 .../Plugins/x86_64/System.Linq.AsyncEnumerable.dll |    3 -
 .../x86_64/System.Linq.AsyncEnumerable.dll.meta    |    2 -
 Assets/Plugins/x86_64/System.Numerics.Tensors.dll  |    3 -
 .../x86_64/System.Numerics.Tensors.dll.meta        |    2 -
 Assets/Plugins/x86_64/System.Text.Json.dll         |    3 -
 Assets/Plugins/x86_64/System.Text.Json.dll.meta    |    2 -
 Assets/Plugins/x86_64/onnxruntime.dll.meta         |   40 +-
 .../x86_64/onnxruntime_providers_shared.dll.meta   |   40 +-
 Assets/Resources/DefaultRuntimeTheme.tss           |    1 +
 Assets/Resources/DefaultRuntimeTheme.tss.meta      |   12 +
 Assets/Scenes/Ember/Boot.unity                     |  168 +
 Assets/Scenes/Ember/Boot.unity.meta                |    7 +
 Assets/Scenes/Ember/CharacterCreation.unity        |  339 +-
 Assets/Scenes/Ember/ColonyNeeds.unity              | 3944 ++++----
 Assets/Scenes/Ember/CombatDungeon.unity            | 3822 ++++----
 Assets/Scenes/Ember/MainMenu.unity                 |    2 +-
 Assets/Scenes/Ember/OracleShrine.unity             | 3602 +++-----
 Assets/Scenes/Ember/RitualHall.unity               | 3328 +++----
 Assets/Scenes/Ember/SeasonFarm.unity               | 2872 +++---
 Assets/Scenes/Ember/ShowroomOverview.unity         | 4552 +++++-----
 Assets/Scenes/Ember/SmithingOverworld.unity        | 4103 +++++----
 Assets/Scenes/Ember/TavernDialog.unity             | 5073 ++++++-----
 Assets/Scenes/Ember/TavernFlavour.unity            | 3632 ++++----
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
 .../TerrainData/SmithingOverworld_Floor_Data.asset |  Bin 0 -> 1410200 bytes
 .../SmithingOverworld_Floor_Data.asset.meta        |    8 +
 .../SmithingOverworld_Floor_Layer.terrainlayer     |   23 +
 ...SmithingOverworld_Floor_Layer.terrainlayer.meta |    8 +
 .../SmithingOverworld_Ground_Data.asset            |  Bin 0 -> 1410204 bytes
 .../SmithingOverworld_Ground_Data.asset.meta       |    8 +
 .../SmithingOverworld_Ground_Layer.terrainlayer    |   23 +
 ...mithingOverworld_Ground_Layer.terrainlayer.meta |    8 +
 .../TerrainData/TavernDialog_Floor_Data.asset      |  Bin 0 -> 1410196 bytes
 .../TerrainData/TavernDialog_Floor_Data.asset.meta |    8 +
 .../TavernDialog_Floor_Layer.terrainlayer          |   23 +
 .../TavernDialog_Floor_Layer.terrainlayer.meta     |    8 +
 Assets/Scenes/Ember/TradeMarket.unity              | 3094 +++----
 .../Scripts/Domain/Forge/AssetGenerationRequest.cs |   10 +-
 Assets/Scripts/Domain/Generation.meta              |    8 +
 .../Domain/Generation/GenericNpcBaseManifest.cs    |   70 +
 .../Generation/GenericNpcBaseManifest.cs.meta      |   11 +
 Assets/Scripts/Domain/Generation/ManifestEntry.cs  |   36 +
 .../Domain/Generation/ManifestEntry.cs.meta        |   11 +
 .../Domain/Generation/ManifestScanReport.cs        |   57 +
 .../Domain/Generation/ManifestScanReport.cs.meta   |   11 +
 Assets/Scripts/Domain/Generation/NpcPromptJson.cs  |   69 +
 .../Domain/Generation/NpcPromptJson.cs.meta        |   11 +
 .../Domain/Generation/NpcPromptJsonValidator.cs    |   91 +
 .../Generation/NpcPromptJsonValidator.cs.meta      |   11 +
 Assets/Scripts/Presentation/Ember/Boot.meta        |    8 +
 .../Presentation/Ember/Boot/BootBootstrap.cs       |  148 +
 .../Presentation/Ember/Boot/BootBootstrap.cs.meta  |   11 +
 .../Presentation/Ember/CharacterCreation.meta      |    8 +
 .../CharacterCreationController.Rendering.cs       |  461 +
 .../CharacterCreationController.Rendering.cs.meta  |    2 +
 .../CharacterCreationController.cs                 |  628 ++
 .../CharacterCreationController.cs.meta            |   11 +
 .../DefaultNpcPortraitJsonProvider.cs              |   68 +
 .../DefaultNpcPortraitJsonProvider.cs.meta         |    2 +
 Assets/Scripts/Presentation/Ember/Diagnostics.meta |    8 +
 .../Diagnostics/EmberProofScreenshotDriver.cs      |  191 +
 .../Diagnostics/EmberProofScreenshotDriver.cs.meta |    2 +
 .../Presentation/Ember/Forge/ForgeBootstrap.cs     |   38 +-
 .../Presentation/Ember/Forge/ModelBootstrap.cs     |   42 +-
 Assets/Scripts/Presentation/Ember/Loading.meta     |    8 +
 .../Presentation/Ember/Loading/LoadingScreen.cs    |  176 +
 .../Ember/Loading/LoadingScreen.cs.meta            |   11 +
 .../Ember/Loading/LoadingScreenController.cs       |  305 +
 .../Ember/Loading/LoadingScreenController.cs.meta  |   11 +
 .../Presentation/Ember/UI/CharacterCreationUI.cs   |  225 +-
 .../Presentation/Ember/UI/EmberMainMenuUI.cs       |  248 +-
 .../Presentation/Ember/UI/EmberWorldGenIntent.cs   |   95 +-
 .../Presentation/Ember/UI/VisibleUiSurface.cs      |   18 +
 .../Presentation/Ember/UI/VisibleUiSurface.cs.meta |    2 +
 Assets/Scripts/Presentation/Ember/Worldgen.meta    |    8 +
 .../Ember/Worldgen/WorldgenEventProjector.cs       |  180 +
 .../Ember/Worldgen/WorldgenEventProjector.cs.meta  |   11 +
 .../Ember/Worldgen/WorldgenViewController.cs       |  217 +
 .../Ember/Worldgen/WorldgenViewController.cs.meta  |   11 +
 .../Ember/Worldgen/WorldgenVisibleEvent.cs         |   61 +
 .../Ember/Worldgen/WorldgenVisibleEvent.cs.meta    |   11 +
 .../Presentation/EmberCrpg.Presentation.asmdef     |    5 +-
 .../Presentation/Sprint4/Sprint4AnimatorDriver.cs  |   95 -
 .../Sprint4/Sprint4AnimatorDriver.cs.meta          |    2 -
 .../Presentation/Sprint4/Sprint4CameraRig.cs       |  136 -
 .../Presentation/Sprint4/Sprint4CameraRig.cs.meta  |    2 -
 .../Sprint4/Sprint4CombatInputAdapter.cs           |   54 -
 .../Sprint4/Sprint4CombatInputAdapter.cs.meta      |    2 -
 .../Sprint4/Sprint4FoundationBootstrap.cs          |  134 -
 .../Sprint4/Sprint4FoundationBootstrap.cs.meta     |    2 -
 .../Sprint4/Sprint4FoundationMarker.cs             |   17 -
 .../Sprint4/Sprint4FoundationMarker.cs.meta        |    2 -
 .../Sprint4/Sprint4GreyboxGroundMarker.cs          |   14 -
 .../Sprint4/Sprint4GreyboxGroundMarker.cs.meta     |    2 -
 .../Sprint4/Sprint4PlayerController.cs             |   98 -
 .../Sprint4/Sprint4PlayerController.cs.meta        |    2 -
 .../Sprint4/Sprint4UnityConversions.cs             |   14 -
 .../Sprint4/Sprint4UnityConversions.cs.meta        |    2 -
 .../VisualLayer/ColonyNeedsSnapshot.cs             |   63 -
 .../VisualLayer/CombatEventTailSnapshot.cs         |   61 -
 .../VisualLayer/CombatEventTailSnapshot.cs.meta    |    2 -
 .../VisualLayer/FactionRelationSnapshot.cs         |   79 -
 .../VisualLayer/InventoryStockpileSnapshot.cs      |   79 -
 .../Presentation/VisualLayer/JobDebugSnapshot.cs   |  102 -
 .../Presentation/VisualLayer/MemoryFactSnapshot.cs |   67 -
 .../VisualLayer/MemoryFactSnapshot.cs.meta         |    2 -
 .../VisualLayer/SeasonClockSnapshot.cs             |   32 -
 .../VisualLayer/SeasonClockSnapshot.cs.meta        |   11 -
 .../VisualLayer/ToolCallTraceSnapshot.cs           |   84 -
 .../VisualLayer/ToolCallTraceSnapshot.cs.meta      |   11 -
 .../VisualLayer/WorldEventTailSnapshot.cs          |   63 -
 .../VisualLayer/WorldEventTailSnapshot.cs.meta     |   11 -
 .../CharacterCreation/AttributeRoller.cs           |   50 +
 .../CharacterCreation/AttributeRoller.cs.meta      |   11 +
 Assets/Scripts/Simulation/Forge/Sd15LcmPipeline.cs |    8 +-
 .../Scripts/Simulation/Forge/SdxlTurboPipeline.cs  |   19 +-
 Assets/Scripts/Simulation/Generation.meta          |    8 +
 .../Simulation/Generation/AssetManifestScanner.cs  |   44 +
 .../Generation/AssetManifestScanner.cs.meta        |   11 +
 .../Simulation/Generation/CoreAssetManifest.cs     |   59 +
 .../Generation/CoreAssetManifest.cs.meta           |   11 +
 .../Simulation/Generation/GenerationFailureLog.cs  |   45 +
 .../Generation/GenerationFailureLog.cs.meta        |   11 +
 .../Simulation/Generation/LlmPromptComposer.cs     |   34 +
 .../Generation/LlmPromptComposer.cs.meta           |   11 +
 .../Simulation/Generation/NpcPromptJsonDefaults.cs |   43 +
 .../Generation/NpcPromptJsonDefaults.cs.meta       |   11 +
 .../Generation/ScenarioAssetTopUpService.cs        |   29 +
 .../Generation/ScenarioAssetTopUpService.cs.meta}  |    2 +-
 .../Simulation/Generation/StaticPromptCatalog.cs   |   70 +
 .../Generation/StaticPromptCatalog.cs.meta         |   11 +
 .../Simulation/Generation/VisibleGenerationFlow.cs |  122 +
 .../Generation/VisibleGenerationFlow.cs.meta}      |    2 +-
 .../Generation/VisibleGenerationPipeline.cs        |  156 +
 .../Generation/VisibleGenerationPipeline.cs.meta   |   11 +
 .../Simulation/Memory/MemoryRecallService.cs       |   17 +-
 .../Simulation/Movement/Sprint4KinematicMotor.cs   |   61 -
 .../Movement/Sprint4KinematicMotor.cs.meta         |    2 -
 .../Simulation/Movement/Sprint4MotorSettings.cs    |   25 -
 .../Movement/Sprint4MotorSettings.cs.meta          |    2 -
 .../Simulation/Movement/Sprint4MotorState.cs       |   20 -
 .../Simulation/Movement/Sprint4MotorState.cs.meta  |    2 -
 .../Simulation/Movement/Sprint4MotorStep.cs        |   19 -
 .../Simulation/Movement/Sprint4MotorStep.cs.meta   |    2 -
 .../Simulation/Movement/Sprint4MovementInput.cs    |   28 -
 .../Movement/Sprint4MovementInput.cs.meta          |    2 -
 .../Scripts/Simulation/Movement/Sprint4Vector3.cs  |   67 -
 .../Simulation/Movement/Sprint4Vector3.cs.meta     |    2 -
 .../Scripts/{Presentation/Sprint4.meta => Ui.meta} |    2 +-
 Assets/Scripts/Ui/Backends.meta                    |    8 +
 Assets/Scripts/Ui/Backends/UiToolkit.meta          |    8 +
 .../EmberCrpg.Ui.Backends.UiToolkit.asmdef         |   16 +
 .../EmberCrpg.Ui.Backends.UiToolkit.asmdef.meta    |    7 +
 .../Ui/Backends/UiToolkit/UiToolkitPanel.cs        |  246 +
 .../Ui/Backends/UiToolkit/UiToolkitPanel.cs.meta   |   11 +
 .../Ui/Backends/UiToolkit/UiToolkitSurface.cs      |   59 +
 .../Ui/Backends/UiToolkit/UiToolkitSurface.cs.meta |   11 +
 .../Ui/Backends/UiToolkit/UiToolkitThemeBinder.cs  |   15 +
 .../UiToolkit/UiToolkitThemeBinder.cs.meta         |   11 +
 Assets/Scripts/Ui/Foundation/IUiPanel.cs           |   65 +-
 Assets/Scripts/Ui/Foundation/IUiSurface.cs         |   44 +-
 Assets/Scripts/Ui/Foundation/UiTokens.cs           |   87 +-
 .../CharacterCreation/DiceRollDeterminismTest.cs   |   18 +
 .../DiceRollDeterminismTest.cs.meta                |   11 +
 .../Tests/EditMode/EmberCrpg.Tests.EditMode.asmdef |    1 +
 .../EditMode/Forge/DiffusionPipelineClampTests.cs  |   18 +
 .../Forge/DiffusionPipelineClampTests.cs.meta      |   11 +
 Assets/Tests/EditMode/Generation.meta              |    8 +
 .../Generation/AssetManifestScannerTests.cs        |   54 +
 .../Generation/AssetManifestScannerTests.cs.meta   |   11 +
 .../EditMode/Generation/CoreAssetManifestTests.cs  |   31 +
 .../Generation/CoreAssetManifestTests.cs.meta      |   11 +
 .../Generation/GenerationFailureLogTests.cs        |   26 +
 .../Generation/GenerationFailureLogTests.cs.meta   |   11 +
 .../Generation/GenericNpcBaseManifestTests.cs      |   22 +
 .../Generation/GenericNpcBaseManifestTests.cs.meta |   11 +
 .../Generation/NpcPromptJsonComposerTest.cs        |   20 +
 .../Generation/NpcPromptJsonComposerTest.cs.meta   |   11 +
 .../Generation/NpcPromptJsonValidatorTests.cs      |   40 +
 .../Generation/NpcPromptJsonValidatorTests.cs.meta |   11 +
 .../Generation/StaticPromptCatalogTests.cs         |   21 +
 .../Generation/StaticPromptCatalogTests.cs.meta    |   11 +
 .../Generation/VisibleGenerationFlowTests.cs       |  105 +
 .../Generation/VisibleGenerationFlowTests.cs.meta} |    2 +-
 .../Generation/VisibleGenerationPipelineTests.cs   |   77 +
 .../VisibleGenerationPipelineTests.cs.meta         |   11 +
 .../Movement/Sprint4KinematicMotorTests.cs         |   58 -
 .../Movement/Sprint4KinematicMotorTests.cs.meta    |    2 -
 Assets/Tests/EditMode/Playability.meta             |    8 +
 .../EditMode/Playability/PlayabilityScoreTests.cs  |   35 +
 .../Playability/PlayabilityScoreTests.cs.meta      |    2 +
 .../Playability/RouteAndSceneRescueTests.cs        |  100 +
 .../Playability/RouteAndSceneRescueTests.cs.meta   |    2 +
 .../Playability/SceneVisualIntegrityTests.cs       |  120 +
 .../Playability/SceneVisualIntegrityTests.cs.meta  |    2 +
 Assets/Tests/EditMode/Ui.meta                      |    8 +
 Assets/Tests/EditMode/Ui/UiSurfaceLocatorTests.cs  |   67 +
 .../EditMode/Ui/UiSurfaceLocatorTests.cs.meta      |   11 +
 Assets/Tests/EditMode/Ui/UiTokensTests.cs          |   67 +
 Assets/Tests/EditMode/Ui/UiTokensTests.cs.meta     |   11 +
 .../EditMode/Worldgen/WorldgenServiceTests.cs      |   49 +
 Assets/Tests/PlayMode.meta                         |    8 +
 Assets/Tests/PlayMode/Boot.meta                    |    8 +
 Assets/Tests/PlayMode/Boot/BootSceneTest.cs        |   29 +
 Assets/Tests/PlayMode/Boot/BootSceneTest.cs.meta   |   11 +
 Assets/Tests/PlayMode/CharacterCreation.meta       |    8 +
 .../CharacterCreation/CharacterCreationFlowTest.cs |   71 +
 .../CharacterCreationFlowTest.cs.meta              |   11 +
 .../Tests/PlayMode/EmberCrpg.Tests.PlayMode.asmdef |   24 +
 .../PlayMode/EmberCrpg.Tests.PlayMode.asmdef.meta  |    7 +
 Assets/Tests/PlayMode/Loading.meta                 |    8 +
 .../Loading/LoadingScreenApiContractTest.cs        |   74 +
 .../Loading/LoadingScreenApiContractTest.cs.meta   |   11 +
 Assets/Tests/PlayMode/Playability.meta             |    8 +
 .../CharacterCreationPlayableSceneTest.cs          |   68 +
 .../CharacterCreationPlayableSceneTest.cs.meta     |    2 +
 .../Playability/RouteAndWorldgenRuntimeTest.cs     |   72 +
 .../RouteAndWorldgenRuntimeTest.cs.meta            |    2 +
 Assets/Tests/PlayMode/Support.meta                 |    8 +
 Assets/Tests/PlayMode/Support/TestUiSurface.cs     |   49 +
 .../Tests/PlayMode/Support/TestUiSurface.cs.meta   |   11 +
 Assets/Tests/PlayMode/Worldgen.meta                |    8 +
 .../PlayMode/Worldgen/WorldgenViewVisibleTest.cs   |   66 +
 .../Worldgen/WorldgenViewVisibleTest.cs.meta       |   11 +
 .../Unity.AI.Search.Editor.asmdef                  |    3 +-
 .../Unity.AI.MCP.Runtime.asmdef                    |    6 +-
 .../Unity.AI.Tracing/Unity.AI.Tracing.asmdef       |    8 +-
 .../Runtime/Unity.AI.Assistant.Runtime.asmdef      |    6 +-
 Packages/manifest.json                             |    1 -
 Packages/packages-lock.json                        |   33 +-
 ProjectSettings/EditorBuildSettings.asset          |   21 +-
 ProjectSettings/ProjectSettings.asset              |    6 +-
 Reports/aaa-playability-rescue_1779713227.md       |   33 +
 Reports/aaa-playability-rescue_1779741542.md       |   51 +
 Reports/aaa_playability_rebuild_done.txt           |    1 +
 Reports/build-windows64-aaa_rescue.log             | 8224 +++++++++++++++++
 Reports/diffs/failure_log_1779695650.diff          |    1 +
 Reports/rebuild-scenes-aaa_rescue2.log             | 1147 +++
 Reports/run-validation-lf.sh                       |  215 +
 Reports/screens/aaa_ColonyNeeds_1779715425.png     |  Bin 0 -> 357897 bytes
 Reports/screens/aaa_CombatDungeon_1779715425.png   |  Bin 0 -> 363004 bytes
 Reports/screens/aaa_OracleShrine_1779715425.png    |  Bin 0 -> 342081 bytes
 Reports/screens/aaa_RitualHall_1779715425.png      |  Bin 0 -> 323329 bytes
 Reports/screens/aaa_SeasonFarm_1779715425.png      |  Bin 0 -> 312806 bytes
 .../screens/aaa_ShowroomOverview_1779715425.png    |  Bin 0 -> 312034 bytes
 .../screens/aaa_SmithingOverworld_1779715425.png   |  Bin 0 -> 172059 bytes
 Reports/screens/aaa_TavernDialog_1779715425.png    |  Bin 0 -> 356355 bytes
 Reports/screens/aaa_TavernFlavour_1779715425.png   |  Bin 0 -> 355712 bytes
 Reports/screens/aaa_TradeMarket_1779715425.png     |  Bin 0 -> 297469 bytes
 Reports/screens/assetgen_1779694983.png            |  Bin 0 -> 56200 bytes
 Reports/screens/assetgen_1779695190.png            |  Bin 0 -> 56200 bytes
 Reports/screens/assetgen_1779695643.png            |  Bin 0 -> 56200 bytes
 Reports/screens/assetgen_failures_1779695650.png   |  Bin 0 -> 82176 bytes
 Reports/screens/boot_1779694982.png                |  Bin 0 -> 56180 bytes
 Reports/screens/boot_1779695190.png                |  Bin 0 -> 56180 bytes
 Reports/screens/boot_1779695642.png                |  Bin 0 -> 56180 bytes
 Reports/screens/cc_dice_1779694987.png             |  Bin 0 -> 56944 bytes
 Reports/screens/cc_dice_1779695195.png             |  Bin 0 -> 66173 bytes
 Reports/screens/cc_dice_1779695647.png             |  Bin 0 -> 66173 bytes
 Reports/screens/cc_portrait_1779694988.png         |  Bin 0 -> 57232 bytes
 Reports/screens/cc_portrait_1779695195.png         |  Bin 0 -> 82148 bytes
 Reports/screens/cc_portrait_1779695647.png         |  Bin 0 -> 82148 bytes
 Reports/screens/cc_skill_1779694986.png            |  Bin 0 -> 60572 bytes
 Reports/screens/cc_skill_1779695194.png            |  Bin 0 -> 67945 bytes
 Reports/screens/cc_skill_1779695646.png            |  Bin 0 -> 67945 bytes
 Reports/screens/mainmenu_1779694984.png            |  Bin 0 -> 51009 bytes
 Reports/screens/mainmenu_1779695192.png            |  Bin 0 -> 37333 bytes
 Reports/screens/mainmenu_1779695644.png            |  Bin 0 -> 37333 bytes
 Reports/screens/mainmenu_after_boot_1779670898.png |  Bin 0 -> 35215 bytes
 Reports/screens/mainmenu_after_boot_1779671052.png |  Bin 0 -> 129973 bytes
 .../mainmenu_after_boot_printwindow_1779671079.png |  Bin 0 -> 5542 bytes
 .../screens/player_boot_or_mainmenu_1779670655.png |  Bin 0 -> 13881 bytes
 .../proof_01_boot_or_mainmenu_1779671231.png       |  Bin 0 -> 72351 bytes
 .../proof_01_boot_or_mainmenu_1779671411.png       |  Bin 0 -> 72351 bytes
 .../proof_01_boot_or_mainmenu_1779671553.png       |  Bin 0 -> 24457 bytes
 .../proof_01_boot_or_mainmenu_1779671864.png       |  Bin 0 -> 24457 bytes
 .../proof_01_boot_or_mainmenu_1779672024.png       |  Bin 0 -> 72351 bytes
 .../proof_01_boot_or_mainmenu_1779672274.png       |  Bin 0 -> 72351 bytes
 .../proof_01_boot_or_mainmenu_1779694838.png       |  Bin 0 -> 56240 bytes
 Reports/screens/proof_02_mainmenu_1779671233.png   |  Bin 0 -> 18454 bytes
 Reports/screens/proof_02_mainmenu_1779671413.png   |  Bin 0 -> 19632 bytes
 Reports/screens/proof_02_mainmenu_1779671555.png   |  Bin 0 -> 19337 bytes
 Reports/screens/proof_02_mainmenu_1779671866.png   |  Bin 0 -> 19632 bytes
 Reports/screens/proof_02_mainmenu_1779672026.png   |  Bin 0 -> 18454 bytes
 Reports/screens/proof_02_mainmenu_1779672276.png   |  Bin 0 -> 24457 bytes
 Reports/screens/proof_02_mainmenu_1779694840.png   |  Bin 0 -> 56180 bytes
 .../screens/proof_03_after_new_game_1779671237.png |  Bin 0 -> 73994 bytes
 .../screens/proof_03_after_new_game_1779671416.png |  Bin 0 -> 72351 bytes
 .../screens/proof_03_after_new_game_1779671558.png |  Bin 0 -> 25657 bytes
 .../screens/proof_03_after_new_game_1779671869.png |  Bin 0 -> 25657 bytes
 .../screens/proof_03_after_new_game_1779672029.png |  Bin 0 -> 25657 bytes
 .../screens/proof_03_after_new_game_1779672279.png |  Bin 0 -> 25657 bytes
 .../screens/proof_03_after_new_game_1779694844.png |  Bin 0 -> 57226 bytes
 Reports/screens/proof_04_scene_Boot_1779694846.png |  Bin 0 -> 56395 bytes
 ...proof_04_scene_CharacterCreation_1779671418.png |  Bin 0 -> 72351 bytes
 ...proof_04_scene_CharacterCreation_1779671560.png |  Bin 0 -> 25657 bytes
 ...proof_04_scene_CharacterCreation_1779671871.png |  Bin 0 -> 51773 bytes
 ...proof_04_scene_CharacterCreation_1779672031.png |  Bin 0 -> 51770 bytes
 ...proof_04_scene_CharacterCreation_1779672282.png |  Bin 0 -> 25657 bytes
 ...proof_04_scene_SmithingOverworld_1779671239.png |  Bin 0 -> 73999 bytes
 .../screens/proof_05_worldgen_log_1779671872.png   |  Bin 0 -> 51773 bytes
 .../screens/proof_05_worldgen_log_1779672032.png   |  Bin 0 -> 51770 bytes
 .../screens/proof_05_worldgen_log_1779672283.png   |  Bin 0 -> 51770 bytes
 .../screens/proof_05_worldgen_log_1779694847.png   |  Bin 0 -> 68349 bytes
 .../screens/rescue-pr214/character_creation.png    |  Bin 0 -> 62985 bytes
 Reports/screens/rescue-pr214/smithing_game.png     |  Bin 0 -> 282594 bytes
 Reports/screens/rescue-pr214/spawn_proof.png       |  Bin 0 -> 283040 bytes
 Reports/screens/rescue-pr214/tavern_game.png       |  Bin 0 -> 155018 bytes
 Reports/screens/rescue-pr214/worldgen_loading.png  |  Bin 0 -> 71472 bytes
 Reports/screens/worldgen_question_1779694989.png   |  Bin 0 -> 69878 bytes
 Reports/screens/worldgen_question_1779695197.png   |  Bin 0 -> 76904 bytes
 Reports/screens/worldgen_question_1779695649.png   |  Bin 0 -> 79023 bytes
 Reports/subagents/reference-reader_1779653740.md   |   36 +
 Reports/test-editmode-aaa_rescue.log               | 1047 +++
 Reports/test-playmode-aaa_rescue.log               | 1170 +++
 Reports/test-results-editmode-aaa_rescue.xml       | 9393 +++++++++++++++++++
 ...test-results-editmode-final_visible_cutover.xml | 9399 ++++++++++++++++++++
 Reports/test-results-playmode-aaa_rescue.xml       |  115 +
 Reports/test-results-playmode-final_continue.xml   |    8 +
 Reports/unity_compile_aaa_rescue.log               |  528 ++
 Reports/validation-fallback-aaa_rescue.log         |   66 +
 ...ble-generation-cutover-discovery_1779652917.txt | 1305 +++
 ...isible-generation-cutover-kickoff_1779653740.md |  122 +
 Reports/visible-generation-cutover_1779661407.md   |   72 +
 Reports/visible-generation-cutover_1779695650.md   |   80 +
 Reports/visible-generation-cutover_1779716286.md   |  153 +
 Reports/visible-generation-cutover_1779741542.md   |  473 +
 docs/prds/aaa-scene-quality-uplift.md              |  171 +
 docs/prds/aaa-sprint-a-codex-mission.md            |  225 +
 .../visible-generation-cutover-codex-mission.md    | 1231 ++-
 docs/screenshots/Faz10OracleShrine.png             |  Bin 1338933 -> 0 bytes
 docs/screenshots/Faz11ShowroomOverview.png         |  Bin 1311636 -> 0 bytes
 docs/screenshots/Faz12TavernFlavour.png            |  Bin 1445303 -> 0 bytes
 docs/screenshots/Faz3SmithingOverworld.png         |  Bin 2684651 -> 0 bytes
 docs/screenshots/Faz4ColonyNeeds.png               |  Bin 1522404 -> 0 bytes
 docs/screenshots/Faz5SeasonFarm.png                |  Bin 1210676 -> 0 bytes
 docs/screenshots/Faz6TradeMarket.png               |  Bin 1283622 -> 0 bytes
 docs/screenshots/Faz7CombatDungeon.png             |  Bin 1293958 -> 0 bytes
 docs/screenshots/Faz8RitualHall.png                |  Bin 1348805 -> 0 bytes
 docs/screenshots/Faz9TavernDialog.png              |  Bin 1385413 -> 0 bytes
 .../fallback/ValidationFallbackHarness.csproj      |    2 +
 tools/validation/run-validation.sh                 |    4 +-
 453 files changed, 63580 insertions(+), 22915 deletions(-)
`

## Blockers
- None for the pushed rescue scope.
- PR remains draft because original full cutover acceptance is broader than this rescue proof and should be reviewer-verified in the built player before ready-for-review.

## Scope Guard
- Did not stage unrelated TMP material noise, Faz/import meta noise, MainMenu scene noise, ColonyNeeds scene noise, or docs/screenshots/ attachments.
- Did not stage Unity AI Assistant restored binaries.
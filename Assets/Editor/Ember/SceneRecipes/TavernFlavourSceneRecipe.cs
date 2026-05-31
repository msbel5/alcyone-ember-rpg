using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Phase 12 acceptance: three NPCs in a tavern exchange flavour lines validated by the
    /// LLM gate; none mutates the world. Reuses the tavern interior and exposes a wide
    /// dialog/narration panel that the LLM client writes to via the same source contract.
    /// </summary>
    public sealed class TavernFlavourSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "TavernFlavour";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/tavern_floor.png", tiling: 8f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 5f);

            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 20f, 20f, 4f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            EmberLightingBuilder.SetAmbientMood(new Color(0.25f, 0.18f, 0.12f), 0.75f);
            EmberPostProcessBuilder.AddVolume("PostProcess_TavernFlavourCandle", EmberMoodPreset.TavernCandle);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.75f, 0.5f),
                intensity: 0.9f,
                eulerAngles: new Vector3(60f, 180f, 0f));

            var ambiance = new GameObject("Ambiance", typeof(Light));
            ambiance.transform.position = new Vector3(0f, 3.5f, 0f);
            var l = ambiance.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 15f;
            l.intensity = 1.0f;
            l.color = new Color(1f, 0.8f, 0.5f);

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Bard",      "bard",       new Vector3(-2f, 0f, 1f), domainActorKey: "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Knight",    "knight",     new Vector3( 0f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",  new Vector3( 2f, 0f, 1f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 3.5f));

            // UI-SINGLE-SOURCE: HUD + dialog come from one place. The recipe used to author a TopBar
            // (EmberHud) and a NarrationBox (DialogBoxPanel) inline, but EmberWorldHost now ensures the
            // standard EmberHud and a single DialogBoxPanel at runtime in every scene. The LLM-gated
            // flavour lines surface through that one host-ensured dialog box (same source contract),
            // so there is no per-scene narration-panel duplicate. Author only the overlay canvas.
            EmberUiBuilder.BuildOverlayCanvas("EmberHUD");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(TavernFlavourSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "SmithingOverworld", "→ Smithing Overworld");
        }
    }
}

using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 12 acceptance: three NPCs in a tavern exchange flavour lines validated by the
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

            EmberTerrainBuilder.BuildRoom(Vector3.zero, 20f, 20f, 4f, floorMat, wallMat);

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

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -6f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Bard",      "bard",       new Vector3(-2f, 0f, 1f), domainActorKey: "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Knight",    "knight",     new Vector3( 0f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",  new Vector3( 2f, 0f, 1f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 3.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var narration = EmberUiBuilder.BuildPanel(canvas, "NarrationBox",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.32f),
                new Color(0f, 0f, 0f, 0.75f));
            EmberUiBuilder.AttachRuntimeScript(narration.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "SmithingOverworld", "→ Faz 3");
        }
    }
}

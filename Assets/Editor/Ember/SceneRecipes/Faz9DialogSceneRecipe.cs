using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 9 acceptance: walk up to an NPC, ask about topics, get answers that consult
    /// memory and faction state. Tavern interior with an innkeeper and a dialog panel
    /// rooted to the lower half of the screen, Fallout 1 / Hitchhiker style.
    /// </summary>
    public sealed class Faz9DialogSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz9TavernDialog";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/tavern_floor.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/wood_floor.png", tiling: 4f);

            EmberTerrainBuilder.BuildRoom(Vector3.zero, 18f, 18f, 3.5f, floorMat, wallMat);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.85f, 0.65f),
                intensity: 1.1f,
                eulerAngles: new Vector3(45f, 135f, 0f));
            
            var interiorLight = new GameObject("InteriorWarmth", typeof(Light));
            interiorLight.transform.position = new Vector3(0f, 3f, 0f);
            var l = interiorLight.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 15f;
            l.intensity = 0.7f;
            l.color = new Color(1f, 0.7f, 0.4f);

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -5f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",    new Vector3(0f, 0f, 3f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Patron",    "warrior",      new Vector3(-3f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Sage",      "sage",         new Vector3( 3f, 0f, 1f), domainActorKey: "Sage Nera");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Hearth", new Vector3(0f, 0.5f, 4f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var dialog = EmberUiBuilder.BuildPanel(canvas, "DialogBox",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.45f),
                new Color(0f, 0f, 0f, 0.7f));
            EmberUiBuilder.AttachRuntimeScript(dialog.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 8f), "Faz10OracleShrine", "→ Faz 10");
        }
    }
}

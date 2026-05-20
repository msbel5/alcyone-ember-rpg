using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 10 acceptance: the player "Consults Fate", the DM tool surface answers via a
    /// deterministic query, the response shows in a HUD card. Scene composition is a
    /// quiet hilltop shrine: a low platform, a DM "Oracle" actor, a wide central card
    /// panel showing tool calls and replies.
    /// </summary>
    public sealed class Faz10DmQuerySceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz10OracleShrine";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/marble.png", tiling: 4f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 24f, floorMat, "ShrineFloor");

            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Podium";
            podium.transform.position = new Vector3(0f, 0.25f, 3f);
            podium.transform.localScale = new Vector3(2.5f, 0.25f, 2.5f);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.85f, 0.9f, 1f),
                intensity: 1.0f,
                eulerAngles: new Vector3(80f, 0f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -7f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Oracle", "fairy", new Vector3(0f, 0.5f, 3f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var card = EmberUiBuilder.BuildPanel(canvas, "DmCard",
                new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.5f),
                new Color(0f, 0f, 0f, 0.7f));
            EmberUiBuilder.AttachRuntimeScript(card.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "Faz11ShowroomOverview", "→ Faz 11");
        }
    }
}

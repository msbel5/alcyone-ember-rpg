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
    public sealed class OracleShrineSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "OracleShrine";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/marble.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 4f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 40f, floorMat, "ShrineFloor");
            
            // Circular boundary wall
            for(int i=0; i<8; i++) {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 15f, 1.5f, Mathf.Sin(angle) * 15f);
                EmberTerrainBuilder.BuildWall(pos, new Vector3(10f, 3f, 0.5f), wallMat, $"Boundary_{i}")
                    .transform.rotation = Quaternion.Euler(0, -i * 45f, 0);
            }

            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Podium";
            podium.transform.position = new Vector3(0f, 0.25f, 3f);
            podium.transform.localScale = new Vector3(2.5f, 0.25f, 2.5f);
            podium.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.9f, 0.95f, 1f),
                intensity: 1.3f,
                eulerAngles: new Vector3(70f, 45f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -7f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Oracle", "fairy", new Vector3(0f, 0.5f, 3f), domainActorKey: "Sage Nera");

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var card = EmberUiBuilder.BuildPanel(canvas, "DmCard",
                new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.5f),
                new Color(0f, 0f, 0f, 0.7f));
            EmberUiBuilder.AttachRuntimeScript(card.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "ShowroomOverview", "→ Faz 11");
        }
    }
}

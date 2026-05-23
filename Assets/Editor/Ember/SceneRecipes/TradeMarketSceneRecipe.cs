using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 6 acceptance: caravan brings goods, merchant prices update, player can trade.
    /// Builds a marketplace exterior with a merchant, a guard, a caravan marker, and the
    /// inventory/trade UI scaffold.
    /// </summary>
    public sealed class TradeMarketSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "TradeMarket";

        public void Build()
        {
            var groundMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/cobblestone.png", tiling: 10f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/brick.png", tiling: 5f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 40f, groundMat, "MarketSquare");
            
            // Add some background walls for context
            EmberTerrainBuilder.BuildWall(new Vector3(0, 2, 20), new Vector3(40, 4, 1), wallMat, "NorthBoundary");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.95f, 0.85f),
                intensity: 1.3f,
                eulerAngles: new Vector3(50f, 180f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -6f),
                spawnRotation: Quaternion.identity);

            var stalls = new GameObject("Stalls").transform;
            EmberWorldspaceBuilder.SpawnActor("Merchant", "merchant", new Vector3(-1.5f, 0f, 2.5f), stalls, "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Guard",    "guard",    new Vector3( 3.5f, 0f, 2.5f), stalls, "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnActor("Trader",   "rogue",    new Vector3( 1.5f, 0f, 4.5f), stalls, "Warden");

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Caravan", new Vector3(-5f, 0.75f, 4f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Stall",   new Vector3(0f, 0.5f, 3.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var inventory = EmberUiBuilder.BuildPanel(canvas, "InventoryGrid",
                new Vector2(0.65f, 0.05f), new Vector2(0.98f, 0.55f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(inventory.gameObject, "EmberCrpg.Presentation.Ember.UI.InventoryGrid");
            inventory.gameObject.SetActive(false);

            var factions = EmberUiBuilder.BuildPanel(canvas, "FactionPanel",
                new Vector2(0.02f, 0.05f), new Vector2(0.38f, 0.55f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(factions.gameObject, "EmberCrpg.Presentation.Ember.UI.FactionPanel");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 10f), "CombatDungeon", "→ Faz 7");
        }
    }
}

using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 8 acceptance: cast a spell, resolve a data-driven effect, see the world react.
    /// Builds a circular ritual hall: stone floor, a mage actor, an effigy target, a wide
    /// HUD area at the bottom for the spellbar and a side panel for spell descriptions.
    /// </summary>
    public sealed class Faz8MagicSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz8RitualHall";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/marble.png", tiling: 4f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 2f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 22f, floorMat, "RitualFloor");

            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f,  11f), new Vector3(22f, 3f, 0.5f), wallMat, "NorthWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f, -11f), new Vector3(22f, 3f, 0.5f), wallMat, "SouthWall");
            EmberTerrainBuilder.BuildWall(new Vector3(-11f, 1.5f, 0f), new Vector3(0.5f, 3f, 22f), wallMat, "WestWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 11f, 1.5f, 0f), new Vector3(0.5f, 3f, 22f), wallMat, "EastWall");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.6f, 0.7f, 1f),
                intensity: 0.7f,
                eulerAngles: new Vector3(75f, 0f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -8f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Mage",      "mage",        new Vector3(-1.5f, 0f, 0f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Apprentice","necromancer", new Vector3( 1.5f, 0f, 0f), domainActorKey: "Ash Rat");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Effigy", new Vector3(0f, 0.75f, 5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var spellBar = EmberUiBuilder.BuildPanel(canvas, "SpellBar",
                new Vector2(0.18f, 0f), new Vector2(0.82f, 0.12f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(spellBar.gameObject, "EmberCrpg.Presentation.Ember.UI.SpellBar");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 12f), "Faz9TavernDialog", "→ Faz 9");
        }
    }
}

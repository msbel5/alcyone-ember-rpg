using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 7 acceptance: melee swing, damage roll, equipped weapon affects outcome.
    /// Builds a dungeon-style interior with stone walls, two enemies, a chest, dim sun.
    /// </summary>
    public sealed class Faz7CombatSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz7CombatDungeon";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/dark_stone.png", tiling: 6f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_wall.png", tiling: 2f);

            EmberTerrainBuilder.BuildGroundPlane(Vector3.zero, 20f, floorMat, "DungeonFloor");

            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f,  10f), new Vector3(20f, 3f, 0.5f), wallMat, "NorthWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 0f, 1.5f, -10f), new Vector3(20f, 3f, 0.5f), wallMat, "SouthWall");
            EmberTerrainBuilder.BuildWall(new Vector3(-10f, 1.5f, 0f), new Vector3(0.5f, 3f, 20f), wallMat, "WestWall");
            EmberTerrainBuilder.BuildWall(new Vector3( 10f, 1.5f, 0f), new Vector3(0.5f, 3f, 20f), wallMat, "EastWall");

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.6f, 0.55f, 0.5f),
                intensity: 0.5f,
                eulerAngles: new Vector3(60f, 0f, 0f));

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -7f),
                spawnRotation: Quaternion.identity,
                fov: 60f);

            var enemies = new GameObject("Enemies").transform;
            EmberWorldspaceBuilder.SpawnActor("Goblin_A", "goblin", new Vector3(-2f, 0f, 4f), enemies, "Ash Rat");
            EmberWorldspaceBuilder.SpawnActor("Goblin_B", "goblin", new Vector3( 2f, 0f, 4f), enemies, "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnActor("Bandit_Lord", "bandit", new Vector3(0f, 0f, 7f), enemies, "Warden");

            EmberWorldspaceBuilder.SpawnWorksiteMarker("Chest", new Vector3(0f, 0.5f, 8.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var combatHud = EmberUiBuilder.BuildPanel(canvas, "CombatHud",
                new Vector2(0f, 0f), new Vector2(1f, 0.18f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(combatHud.gameObject, "EmberCrpg.Presentation.Ember.UI.CombatHud");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 15f), "Faz8RitualHall", "→ Faz 8");
        }
    }
}

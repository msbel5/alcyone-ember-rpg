using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Faz 11 acceptance: every previous faz has a one-screenshot Unity proof.
    /// This recipe builds a "showroom" scene that hosts the JobDebugSnapshot,
    /// ColonyNeedsPanel, InventoryGrid, and DialogBoxPanel side by side so a single
    /// capture demonstrates that the view layer can read deterministic backend snapshots.
    /// </summary>
    public sealed class ShowroomOverviewSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "Faz11ShowroomOverview";

        public void Build()
        {
            var floorMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/stone_floor.png", tiling: 8f);
            var wallMat = EmberMaterialFactory.GetOrCreateTileMaterial(
                $"{EmberAssetPaths.TilesDir}/brick.png", tiling: 4f);

            EmberTerrainBuilder.BuildRoom(Vector3.zero, 30f, 30f, 4f, floorMat, wallMat);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(1f, 0.98f, 0.92f),
                intensity: 1.2f,
                eulerAngles: new Vector3(50f, -30f, 0f));
            
            // Add some point lights for ambiance
            var lightA = new GameObject("AmbianceLightA", typeof(Light));
            lightA.transform.position = new Vector3(-5f, 2f, 5f);
            lightA.GetComponent<Light>().type = LightType.Point;
            lightA.GetComponent<Light>().range = 10f;
            lightA.GetComponent<Light>().intensity = 0.5f;
            lightA.GetComponent<Light>().color = new Color(1f, 0.7f, 0.4f);

            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: new Vector3(0f, 0f, -7f),
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Smith_A",   "blacksmith", new Vector3(-3f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Smith_B",   "blacksmith", new Vector3(-1f, 0f, 1f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",  new Vector3( 1f, 0f, 1f), domainActorKey: "Sage Nera");
            EmberWorldspaceBuilder.SpawnActor("Mage",      "mage",       new Vector3( 3f, 0f, 1f), domainActorKey: "Sentinel Rook");
            EmberWorldspaceBuilder.SpawnWorksiteMarker("Forge", new Vector3(-2f, 0.75f, 3.5f));

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");

            var jobPanel = EmberUiBuilder.BuildPanel(canvas, "JobQueuePanel",
                new Vector2(0f, 0.45f), new Vector2(0.22f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(jobPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.JobQueuePanel");

            var needsPanel = EmberUiBuilder.BuildPanel(canvas, "ColonyNeedsPanel",
                new Vector2(0.78f, 0.45f), new Vector2(1f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(needsPanel.gameObject, "EmberCrpg.Presentation.Ember.UI.ColonyNeedsPanel");

            var factions = EmberUiBuilder.BuildPanel(canvas, "FactionPanel",
                new Vector2(0.24f, 0.45f), new Vector2(0.5f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(factions.gameObject, "EmberCrpg.Presentation.Ember.UI.FactionPanel");

            var combatHud = EmberUiBuilder.BuildPanel(canvas, "CombatHud",
                new Vector2(0.52f, 0.45f), new Vector2(0.76f, 0.94f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(combatHud.gameObject, "EmberCrpg.Presentation.Ember.UI.CombatHud");

            var inventory = EmberUiBuilder.BuildPanel(canvas, "InventoryGrid",
                new Vector2(0f, 0f), new Vector2(0.55f, 0.4f),
                new Color(0f, 0f, 0f, 0.45f));
            EmberUiBuilder.AttachRuntimeScript(inventory.gameObject, "EmberCrpg.Presentation.Ember.UI.InventoryGrid");

            var dialog = EmberUiBuilder.BuildPanel(canvas, "DialogBox",
                new Vector2(0.56f, 0f), new Vector2(1f, 0.4f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(dialog.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            var spellBar = EmberUiBuilder.BuildPanel(canvas, "SpellBar",
                new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.48f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(spellBar.gameObject, "EmberCrpg.Presentation.Ember.UI.SpellBar");

            EmberScenePortalBuilder.BuildPortal(new Vector3(0f, 0f, 15f), "Faz12TavernFlavour", "→ Faz 12");
}
    }
}

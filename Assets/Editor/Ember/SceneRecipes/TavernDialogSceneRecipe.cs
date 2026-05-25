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
    public sealed class TavernDialogSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "TavernDialog";

        public void Build()
        {
            var floorMat = EmberSceneMaterialLibrary.TavernFloor();
            var wallMat = EmberSceneMaterialLibrary.TavernWall();

            var room = EmberTerrainBuilder.BuildRoom(Vector3.zero, 18f, 18f, 3.5f, floorMat, wallMat);
            var roomFloor = EmberScenePlacement.RequireRoomFloor(room);

            EmberLightingBuilder.AddDirectionalSun(
                color: new Color(0.50f, 0.64f, 0.95f),
                intensity: 0.35f,
                eulerAngles: new Vector3(45f, 135f, 0f));

            var interiorLight = new GameObject("InteriorWarmth", typeof(Light));
            interiorLight.transform.position = new Vector3(0f, 3f, 0f);
            var l = interiorLight.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 15f;
            l.intensity = 1.4f;
            l.color = new Color(1f, 0.7f, 0.4f);

            var spawnPosition = EmberScenePlacement.ComputePlayerSpawn(roomFloor);
            EmberPlayerRigBuilder.BuildRig(
                spawnPosition: spawnPosition,
                spawnRotation: Quaternion.identity);

            EmberWorldspaceBuilder.SpawnActor("Innkeeper", "innkeeper",    new Vector3(0f, 0f, 3f), domainActorKey: "Quartermaster Ivo");
            EmberWorldspaceBuilder.SpawnActor("Patron",    "warrior",      new Vector3(-3f, 0f, 1f), domainActorKey: "Warden");
            EmberWorldspaceBuilder.SpawnActor("Sage",      "sage",         new Vector3( 3f, 0f, 1f), domainActorKey: "Sage Nera");

            var focal = new GameObject("FocalContent");
            focal.transform.position = new Vector3(0f, 1.2f, 4f);

            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "Hearth",
                new Vector3(0f, 0.55f, 4f),
                material: EmberSceneMaterialLibrary.EmberLight(),
                scale: new Vector3(2.2f, 0.85f, 0.7f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "BarCounter",
                new Vector3(0f, 0.65f, 5.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(5.2f, 1.0f, 0.8f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "LeftStool",
                new Vector3(-2.0f, 0.45f, 2.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.55f, 0.65f, 0.55f));
            EmberWorldspaceBuilder.SpawnWorksiteMarker(
                "RightStool",
                new Vector3(2.0f, 0.45f, 2.2f),
                material: EmberSceneMaterialLibrary.Prop(),
                scale: new Vector3(0.55f, 0.65f, 0.55f));
            EmberWorldspaceBuilder.SpawnDecorSprite("BottledSunlight", "Assets/Art/Items/bottled_sunlight.png", new Vector3(-1.6f, 1.15f, 5.0f), 0.55f);
            EmberWorldspaceBuilder.SpawnDecorSprite("ManaPotion", "Assets/Art/Items/mana_potion.png", new Vector3(0f, 1.15f, 5.0f), 0.55f);
            EmberWorldspaceBuilder.SpawnDecorSprite("CodedMessage", "Assets/Art/Items/coded_message.png", new Vector3(1.6f, 1.15f, 5.0f), 0.55f);

            var ambience = new GameObject("TavernAmbientPlaceholderLoop", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0.35f;

            var canvas = EmberUiBuilder.BuildOverlayCanvas("EmberHUD");
            var topBar = EmberUiBuilder.BuildPanel(canvas, "TopBar",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.55f));
            EmberUiBuilder.AttachRuntimeScript(topBar.gameObject, "EmberCrpg.Presentation.Ember.UI.EmberHud");
            var dialog = EmberUiBuilder.BuildPanel(canvas, "DialogBox",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.45f),
                new Color(0f, 0f, 0f, 0.7f));
            EmberUiBuilder.AttachRuntimeScript(dialog.gameObject, "EmberCrpg.Presentation.Ember.UI.DialogBoxPanel");

            var portalSpawn = EmberScenePlacement.ComputeEastPortalSpawn(roomFloor);
            EmberScenePlacement.AssertInsideFloorFootprint(roomFloor, portalSpawn, nameof(TavernDialogSceneRecipe));
            EmberScenePortalBuilder.BuildPortal(portalSpawn, "OracleShrine", "→ Oracle Shrine");
        }
    }
}

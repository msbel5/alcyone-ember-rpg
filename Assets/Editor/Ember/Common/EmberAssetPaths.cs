// Single source of truth for project asset paths used by the Ember editor pipeline.
// Pure constants only — no logic, no IO. Anything that resolves a path at runtime
// should consume these strings and add its own composition.
namespace EmberCrpg.Editor.Ember.Common
{
    public static class EmberAssetPaths
    {
        public const string ArtRoot = "Assets/Art";
        public const string CharactersDir = ArtRoot + "/Characters";
        public const string ItemsDir = ArtRoot + "/Items";
        public const string SpellsDir = ArtRoot + "/Spells";
        public const string PortraitsDir = ArtRoot + "/Portraits";
        public const string TilesDir = ArtRoot + "/Tiles";
        public const string UiRoot = ArtRoot + "/UI";
        public const string UiBannersDir = UiRoot + "/Banners";
        public const string UiCombatHudDir = UiRoot + "/CombatHud";
        public const string UiStatusBarsDir = UiRoot + "/StatusBars";
        public const string UiStatusIconsDir = UiRoot + "/StatusIcons";
        public const string UiCommonDir = UiRoot + "/Common";
        public const string BodySilhouettesDir = ArtRoot + "/BodySilhouettes";
        public const string UiPlanDir = ArtRoot + "/UiPlan";

        public const string ScenesRoot = "Assets/Scenes";
        public const string EmberScenesDir = ScenesRoot + "/Ember";

        public const string MaterialsRoot = "Assets/Art/Materials";
        public const string PrefabsRoot = "Assets/Art/Prefabs";

        public const string AssetManifestPath = ArtRoot + "/manifest.json";
    }
}

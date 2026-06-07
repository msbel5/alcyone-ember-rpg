// Single source of truth for project asset paths used by the Ember editor pipeline.
// Pure constants only — no logic, no IO. Anything that resolves a path at runtime
// should consume these strings and add its own composition.
namespace EmberCrpg.Editor.Ember.Common
{
    public static class EmberAssetPaths
    {
        public const string GeneratedRoot = "Assets/Generated";
        public const string CharactersDir = GeneratedRoot + "/Sprites/Characters";
        public const string ItemsDir = GeneratedRoot + "/Sprites/Items";
        public const string SpellsDir = GeneratedRoot + "/Sprites/Spells";
        public const string PortraitsDir = GeneratedRoot + "/Sprites/Portraits";
        public const string TilesDir = GeneratedRoot + "/Textures";
        public const string UiRoot = GeneratedRoot + "/UI";
        public const string UiBannersDir = UiRoot + "/Banners";
        public const string UiCombatHudDir = UiRoot + "/CombatHud";
        public const string UiStatusBarsDir = UiRoot + "/StatusBars";
        public const string UiStatusIconsDir = UiRoot + "/StatusIcons";
        public const string UiCommonDir = UiRoot + "/Common";
        public const string BodySilhouettesDir = GeneratedRoot + "/BodySilhouettes";
        public const string UiPlanDir = GeneratedRoot + "/UiPlan";

        public const string ScenesRoot = "Assets/Scenes";
        public const string EmberScenesDir = ScenesRoot + "/Ember";

        public const string MaterialsRoot = "Assets/Generated/Materials";
        public const string PrefabsRoot = "Assets/Generated/Prefabs";

        public const string AssetManifestPath = GeneratedRoot + "/manifest.json";
    }
}

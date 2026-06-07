using System;
using EmberCrpg.Editor.Ember.Common;

namespace EmberCrpg.Editor.Ember.AssetImport
{
    /// <summary>
    /// Maps an asset import path to an <see cref="AssetCategory"/>.
    /// Pure function over the path string. Folder layout under
    /// <see cref="EmberAssetPaths.GeneratedRoot"/> is the only signal.
    /// </summary>
    public static class EmberAssetCategoryClassifier
    {
        public static AssetCategory Classify(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return AssetCategory.Unknown;

            var p = assetPath.Replace('\\', '/');
            if (p.StartsWith(EmberAssetPaths.CharactersDir, StringComparison.OrdinalIgnoreCase)) return AssetCategory.CharacterSprite;
            if (p.StartsWith(EmberAssetPaths.ItemsDir,      StringComparison.OrdinalIgnoreCase)) return AssetCategory.ItemIcon;
            if (p.StartsWith(EmberAssetPaths.SpellsDir,     StringComparison.OrdinalIgnoreCase)) return AssetCategory.SpellIcon;
            if (p.StartsWith(EmberAssetPaths.PortraitsDir,  StringComparison.OrdinalIgnoreCase)) return AssetCategory.Portrait;
            if (p.StartsWith(EmberAssetPaths.TilesDir,      StringComparison.OrdinalIgnoreCase)) return AssetCategory.Tile;
            if (p.StartsWith(EmberAssetPaths.UiBannersDir,    StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiBanner;
            if (p.StartsWith(EmberAssetPaths.UiCombatHudDir,  StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiCombatHud;
            if (p.StartsWith(EmberAssetPaths.UiStatusBarsDir, StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiStatusBar;
            if (p.StartsWith(EmberAssetPaths.UiStatusIconsDir,StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiStatusIcon;
            if (p.StartsWith(EmberAssetPaths.UiCommonDir,     StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiCommon;
            if (p.StartsWith(EmberAssetPaths.BodySilhouettesDir, StringComparison.OrdinalIgnoreCase)) return AssetCategory.BodySilhouette;
            if (p.StartsWith(EmberAssetPaths.UiPlanDir,       StringComparison.OrdinalIgnoreCase)) return AssetCategory.UiPlan;
            return AssetCategory.Unknown;
        }
    }
}

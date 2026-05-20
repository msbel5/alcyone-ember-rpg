using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.AssetImport
{
    /// <summary>
    /// Declarative sprite import settings for one <see cref="AssetCategory"/>.
    /// Returned by <see cref="EmberSpriteImportRules"/>. Applied by the postprocessor.
    /// </summary>
    public readonly struct EmberSpriteImportProfile
    {
        public readonly TextureImporterType TextureType;
        public readonly SpriteImportMode SpriteMode;
        public readonly int PixelsPerUnit;
        public readonly FilterMode FilterMode;
        public readonly TextureWrapMode WrapMode;
        public readonly bool GenerateMipMaps;
        public readonly int MaxTextureSize;
        public readonly bool AlphaIsTransparency;

        public EmberSpriteImportProfile(
            TextureImporterType textureType,
            SpriteImportMode spriteMode,
            int pixelsPerUnit,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            bool generateMipMaps,
            int maxTextureSize,
            bool alphaIsTransparency)
        {
            TextureType = textureType;
            SpriteMode = spriteMode;
            PixelsPerUnit = pixelsPerUnit;
            FilterMode = filterMode;
            WrapMode = wrapMode;
            GenerateMipMaps = generateMipMaps;
            MaxTextureSize = maxTextureSize;
            AlphaIsTransparency = alphaIsTransparency;
        }
    }

    /// <summary>Per-category sprite profile catalogue. One row per category.</summary>
    public static class EmberSpriteImportRules
    {
        public static EmberSpriteImportProfile? For(AssetCategory category)
        {
            switch (category)
            {
                case AssetCategory.CharacterSprite:
                case AssetCategory.Portrait:
                case AssetCategory.BodySilhouette:
                    return new EmberSpriteImportProfile(TextureImporterType.Sprite, SpriteImportMode.Single, 256, FilterMode.Bilinear, TextureWrapMode.Clamp, false, 2048, true);
                case AssetCategory.ItemIcon:
                case AssetCategory.SpellIcon:
                case AssetCategory.UiStatusIcon:
                    return new EmberSpriteImportProfile(TextureImporterType.Sprite, SpriteImportMode.Single, 256, FilterMode.Bilinear, TextureWrapMode.Clamp, false, 512, true);
                case AssetCategory.UiBanner:
                case AssetCategory.UiCombatHud:
                case AssetCategory.UiStatusBar:
                case AssetCategory.UiCommon:
                case AssetCategory.UiPlan:
                    return new EmberSpriteImportProfile(TextureImporterType.Sprite, SpriteImportMode.Single, 100, FilterMode.Bilinear, TextureWrapMode.Clamp, false, 2048, true);
                default:
                    return null;
            }
        }
    }
}

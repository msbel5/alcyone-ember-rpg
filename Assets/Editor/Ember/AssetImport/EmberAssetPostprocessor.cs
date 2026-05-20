using UnityEditor;

namespace EmberCrpg.Editor.Ember.AssetImport
{
    /// <summary>
    /// Applies declarative import profiles when Unity imports a texture under <c>Assets/Art</c>.
    /// The postprocessor is a thin dispatcher: classify → look up profile → mutate the
    /// <see cref="TextureImporter"/>. All rule data lives in the profile classes.
    /// </summary>
    public sealed class EmberAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null) return;

            var category = EmberAssetCategoryClassifier.Classify(assetPath);
            if (category == AssetCategory.Unknown) return;

            var spriteProfile = EmberSpriteImportRules.For(category);
            if (spriteProfile.HasValue)
            {
                ApplySpriteProfile(importer, spriteProfile.Value);
                return;
            }

            var textureProfile = EmberTextureImportRules.For(category);
            if (textureProfile.HasValue)
                ApplyTextureProfile(importer, textureProfile.Value);
        }

        private static void ApplySpriteProfile(TextureImporter importer, EmberSpriteImportProfile profile)
        {
            importer.textureType = profile.TextureType;
            importer.spriteImportMode = profile.SpriteMode;
            importer.spritePixelsPerUnit = profile.PixelsPerUnit;
            importer.filterMode = profile.FilterMode;
            importer.wrapMode = profile.WrapMode;
            importer.mipmapEnabled = profile.GenerateMipMaps;
            importer.maxTextureSize = profile.MaxTextureSize;
            importer.alphaIsTransparency = profile.AlphaIsTransparency;
            importer.sRGBTexture = true;
        }

        private static void ApplyTextureProfile(TextureImporter importer, EmberTextureImportProfile profile)
        {
            importer.textureType = profile.TextureType;
            importer.textureShape = profile.TextureShape;
            importer.filterMode = profile.FilterMode;
            importer.wrapMode = profile.WrapMode;
            importer.mipmapEnabled = profile.GenerateMipMaps;
            importer.maxTextureSize = profile.MaxTextureSize;
            importer.sRGBTexture = profile.SRGB;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.AssetImport
{
    /// <summary>
    /// Declarative texture (non-sprite) import settings. Used for tile textures applied to
    /// 3D ground/wall materials in the first-person scenes.
    /// </summary>
    public readonly struct EmberTextureImportProfile
    {
        public readonly TextureImporterType TextureType;
        public readonly TextureImporterShape TextureShape;
        public readonly FilterMode FilterMode;
        public readonly TextureWrapMode WrapMode;
        public readonly bool GenerateMipMaps;
        public readonly int MaxTextureSize;
        public readonly bool SRGB;

        public EmberTextureImportProfile(
            TextureImporterType textureType,
            TextureImporterShape textureShape,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            bool generateMipMaps,
            int maxTextureSize,
            bool sRGB)
        {
            TextureType = textureType;
            TextureShape = textureShape;
            FilterMode = filterMode;
            WrapMode = wrapMode;
            GenerateMipMaps = generateMipMaps;
            MaxTextureSize = maxTextureSize;
            SRGB = sRGB;
        }
    }

    public static class EmberTextureImportRules
    {
        /// <summary>Returns a 3D-texture profile for categories that drive 3D materials, otherwise null.</summary>
        public static EmberTextureImportProfile? For(AssetCategory category)
        {
            if (category != AssetCategory.Tile)
                return null;

            return new EmberTextureImportProfile(
                textureType: TextureImporterType.Default,
                textureShape: TextureImporterShape.Texture2D,
                filterMode: FilterMode.Trilinear,
                wrapMode: TextureWrapMode.Repeat,
                generateMipMaps: true,
                maxTextureSize: 1024,
                sRGB: true);
        }
    }
}

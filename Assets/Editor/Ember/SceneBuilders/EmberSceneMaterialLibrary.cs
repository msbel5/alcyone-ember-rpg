using System.IO;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    public static class EmberSceneMaterialLibrary
    {
        public static Material Floor() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("ember_floor_fallback", new Color(0.18f, 0.16f, 0.13f), new Color(0.28f, 0.22f, 0.16f)), 8f);

        public static Material Wall() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("ember_wall_fallback", new Color(0.16f, 0.17f, 0.18f), new Color(0.30f, 0.25f, 0.20f)), 4f);

        public static Material SmithingFloor() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("smithing_warm_stone_floor", new Color(0.31f, 0.24f, 0.17f), new Color(0.42f, 0.31f, 0.20f)), 8f);

        public static Material SmithingWall() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("smithing_dark_forge_wall", new Color(0.10f, 0.09f, 0.08f), new Color(0.25f, 0.15f, 0.08f)), 4f);

        public static Material TavernFloor() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("tavern_wood_floor", new Color(0.30f, 0.18f, 0.09f), new Color(0.47f, 0.29f, 0.14f)), 6f);

        public static Material TavernWall() => EmberMaterialFactory.GetOrCreateTileMaterial(
            EnsureTexture("tavern_plaster_stone_wall", new Color(0.32f, 0.27f, 0.21f), new Color(0.18f, 0.17f, 0.16f)), 4f);

        public static Material EmberLight() => EmberMaterialFactory.GetOrCreateSolidMaterial(
            "Scene_Ember_Light", new Color(1.0f, 0.42f, 0.10f, 1f));

        public static Material Prop() => EmberMaterialFactory.GetOrCreateSolidMaterial(
            "Scene_Prop_Ember", new Color(0.24f, 0.20f, 0.16f, 1f));

        public static Material Portal() => EmberMaterialFactory.GetOrCreateSolidMaterial(
            "Scene_Portal_Ember", new Color(0.85f, 0.55f, 0.16f, 1f));

        public static string EnsureTexture(string name, Color a, Color b)
        {
            var path = EmberAssetPaths.TilesDir + "/" + name + ".png";
            if (File.Exists(path)) return path;

            Directory.CreateDirectory(EmberAssetPaths.TilesDir);
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                bool mortar = x % 16 == 0 || y % 16 == 0;
                texture.SetPixel(x, y, mortar ? b : a);
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }
    }
}

using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Runtime material palette for the procedurally realized location. The runtime twin of the editor
    /// EmberMaterialFactory: it creates throwaway URP/Lit materials (no AssetDatabase persistence) and maps
    /// the deterministic layout's abstract MaterialIndex + the region biome to concrete colours. Keeping the
    /// colour decisions here keeps UnityEngine.Color out of the engine-free layout layer.
    /// </summary>
    public static class RuntimeMaterialPalette
    {
        private static readonly Color[] WallColors =
        {
            new Color(0.42f, 0.34f, 0.26f), // timber brown
            new Color(0.55f, 0.52f, 0.47f), // stone grey
            new Color(0.50f, 0.40f, 0.30f), // warm clay
            new Color(0.38f, 0.36f, 0.40f), // slate
        };

        public static Material Opaque(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        public static Color WallColor(int materialIndex)
        {
            int i = materialIndex % WallColors.Length;
            if (i < 0) i += WallColors.Length;
            return WallColors[i];
        }

        public static Color GroundColor(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Plains:   return new Color(0.34f, 0.40f, 0.22f);
                case BiomeKind.Forest:   return new Color(0.18f, 0.30f, 0.18f);
                case BiomeKind.Mountain: return new Color(0.40f, 0.39f, 0.42f);
                case BiomeKind.Coast:    return new Color(0.46f, 0.42f, 0.30f);
                case BiomeKind.Swamp:    return new Color(0.24f, 0.28f, 0.22f);
                case BiomeKind.Desert:   return new Color(0.62f, 0.55f, 0.36f);
                case BiomeKind.Tundra:   return new Color(0.58f, 0.60f, 0.62f);
                case BiomeKind.Ash:      return new Color(0.26f, 0.22f, 0.22f);
                default:                 return new Color(0.30f, 0.30f, 0.28f);
            }
        }
    }
}

using EmberCrpg.Domain.Overland;
using EmberCrpg.Presentation.Ember.Sprites;
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

        /// <summary>
        /// A textured material from a generated asset (e.g. a biome floor / wall). Falls back to a flat
        /// <paramref name="tint"/> material when the asset has not been generated yet, so the world always
        /// renders. <paramref name="tiling"/> repeats the texture across large surfaces.
        /// </summary>
        public static Material Textured(string generatedAssetId, Color tint, float tiling = 1f)
        {
            var texture = LoadGeneratedTexture(generatedAssetId);
            if (texture == null) return Opaque(tint);

            var mat = Opaque(Color.white);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            mat.mainTexture = texture;
            mat.mainTextureScale = new Vector2(tiling, tiling);
            return mat;
        }

        // Loads only provenance-fresh generated Core textures; stale cache entries fall through to tint.
        public static Texture2D LoadGeneratedTexture(string assetId)
        {
            return GeneratedCoreTextureLoader.TryLoad(assetId, TextureWrapMode.Repeat, FilterMode.Bilinear, true);
        }

        // Best-fit generated floor texture per biome (reuses the env_* floor assets until biome-specific
        // ground textures are generated). Returns the asset id, or null to use a flat colour.
        public static string GroundTextureId(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Plains:   return "env_seasonfarm";
                case BiomeKind.Forest:   return "env_seasonfarm";
                case BiomeKind.Coast:    return "env_trademarket";
                case BiomeKind.Mountain: return "env_combatdungeon";
                case BiomeKind.Swamp:    return "env_ritualhall";
                case BiomeKind.Desert:   return "env_trademarket";
                case BiomeKind.Tundra:   return "env_oracleshrine";
                case BiomeKind.Ash:      return "env_combatdungeon";
                default:                 return "env_colonyneeds";
            }
        }

        private static readonly string[] WallTextureIds =
        {
            "wall_trademarket", "wall_tavernflavour", "wall_colonyneeds", "wall_showroomoverview",
        };

        public static Color WallColor(int materialIndex)
        {
            int i = materialIndex % WallColors.Length;
            if (i < 0) i += WallColors.Length;
            return WallColors[i];
        }

        // Best-fit generated wall texture for an abstract palette slot (reuses the wall_* assets).
        public static string WallTextureId(int materialIndex)
        {
            int i = materialIndex % WallTextureIds.Length;
            if (i < 0) i += WallTextureIds.Length;
            return WallTextureIds[i];
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

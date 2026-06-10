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

        private static Material _water;

        /// <summary>
        /// Shared translucent water surface material (sea/lakes), OpenMW-style flat plane shading. Cached —
        /// every streamed tile that touches the sea reuses the same instance.
        /// </summary>
        public static Material Water()
        {
            if (_water != null) return _water;
            // OPAQUE deep blue on purpose: the runtime URP transparent recipe (keywords + blend state set by
            // hand) rendered as a BLACK plane in the player build (variant stripped / keyword not applied).
            // Opaque cannot fail that way on any pipeline; translucency is polish for later.
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            // Bright readable sea blue: the previous dark navy + smoothness 0.85 read as a BLACK hole in
            // playtests — with no runtime reflection probes the specular term is black, and under grim
            // ambient a dark albedo carries almost no visible light. Brighter base + modest smoothness +
            // a faint emission floor make water unmistakably WATER in every lighting mood.
            var color = new Color(0.18f, 0.45f, 0.65f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            // BULLETPROOF COLOR PATH: building textures provably render in the player, so carry the blue in
            // an actual _BaseMap texture too — whatever variant/keyword the build strips, a textured surface
            // cannot collapse to black the way a colour-only material did in two playtests.
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(46, 115, 166, 255);
            tex.SetPixels32(px);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.04f, 0.12f, 0.20f, 1f));
            }
            // DOUBLE-SIDED: the sheet is a single plane — one-sided it vanishes when the player walks below
            // the waterline (the shore bowl is walkable), and the shadowed bowl under it read as a growing
            // BLACK band from outside. Culling off keeps water visibly water from every side.
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            _water = mat;
            return mat;
        }

        // Solid-colour material with the colour carried in a real 4x4 texture (the bulletproof path the water
        // fix proved: colour-only runtime materials collapsed to black twice in player builds). Cached per
        // colour so interiors across a whole town share a handful of materials.
        private static readonly System.Collections.Generic.Dictionary<uint, Material> SolidCache =
            new System.Collections.Generic.Dictionary<uint, Material>();

        public static Material Solid(Color color)
        {
            var c32 = (Color32)color;
            uint key = ((uint)c32.r << 24) | ((uint)c32.g << 16) | ((uint)c32.b << 8) | c32.a;
            if (SolidCache.TryGetValue(key, out var cached)) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = c32;
            tex.SetPixels32(px);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            SolidCache[key] = mat;
            return mat;
        }

        public static Color GroundColor(BiomeKind biome)
        {
            switch (biome)
            {
                // Saturated, clearly-separated tints ("her yer aynı" fix): the shared crackle textures made
                // every biome read identical when the tints sat this close together.
                case BiomeKind.Plains:   return new Color(0.32f, 0.46f, 0.18f);
                case BiomeKind.Forest:   return new Color(0.12f, 0.33f, 0.14f);
                case BiomeKind.Mountain: return new Color(0.42f, 0.42f, 0.50f);
                case BiomeKind.Coast:    return new Color(0.68f, 0.60f, 0.40f);
                case BiomeKind.Swamp:    return new Color(0.22f, 0.32f, 0.18f);
                case BiomeKind.Desert:   return new Color(0.76f, 0.62f, 0.32f);
                case BiomeKind.Tundra:   return new Color(0.64f, 0.68f, 0.74f);
                case BiomeKind.Ash:      return new Color(0.22f, 0.17f, 0.17f);
                default:                 return new Color(0.30f, 0.30f, 0.28f);
            }
        }
    }
}

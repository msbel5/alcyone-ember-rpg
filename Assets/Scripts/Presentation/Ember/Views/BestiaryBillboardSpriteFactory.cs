using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F29 BESTIARY: deterministic billboard silhouettes for the monster types — generated pixel
    /// masks on the proven unlit sprite path (the star-dome/pose-icon family), so a wolf reads as
    /// a wolf even with the forge OFF. When the forge is ON, the asset library resolves the real
    /// SDXL sprite first and these never show (the spawner tries library → silhouette → neutral).
    /// '#' = body colour, 'o' = accent (eyes/sockets); generated once, cached forever.
    /// </summary>
    public static class BestiaryBillboardSpriteFactory
    {
        private static Sprite s_wolf, s_spider, s_skeleton, s_ghost, s_bandit;

        public static Sprite For(string spriteRole)
        {
            switch (spriteRole)
            {
                case "monster_wolf": return Wolf();
                case "monster_spider": return Spider();
                case "monster_skeleton": return Skeleton();
                case "monster_ghost": return Ghost();
                case "monster_bandit": return Bandit();
                default: return null;
            }
        }

        /// <summary>Per-type billboard height: a wolf stands hip-high, a spider crouches, the
        /// dead stand tall. Unknown roles keep the caller's default.</summary>
        public static float TargetHeightFor(string spriteRole, float defaultHeight)
        {
            switch (spriteRole)
            {
                case "monster_wolf": return 1.2f;
                case "monster_spider": return 0.9f;
                case "monster_skeleton": return 2.0f;
                case "monster_ghost": return 2.2f;
                case "monster_bandit": return 2.0f;
                default: return defaultHeight;
            }
        }

        private static Sprite Wolf()
        {
            if (s_wolf == null)
                s_wolf = FromMask(new[]
                {
                    ".##.............",
                    "#####......##...",
                    "o##############.",
                    ".##############.",
                    ".##############.",
                    "..############..",
                    "..##..##..##..#.",
                    "..##..##..##..#.",
                    "..#...#...#...#.",
                    "................",
                }, new Color(0.45f, 0.40f, 0.34f), new Color(1.0f, 0.35f, 0.2f));
            return s_wolf;
        }

        private static Sprite Spider()
        {
            if (s_spider == null)
                s_spider = FromMask(new[]
                {
                    "#....#....#....#",
                    ".#...#....#...#.",
                    "..############..",
                    ".####o####o####.",
                    "..############..",
                    ".#.##.#.##.#.##.",
                    "#..#..#..#..#..#",
                    "#..#..#..#..#..#",
                    "...#..#..#..#...",
                    "................",
                }, new Color(0.30f, 0.18f, 0.38f), new Color(1.0f, 0.25f, 0.2f));
            return s_spider;
        }

        private static Sprite Skeleton()
        {
            if (s_skeleton == null)
                s_skeleton = FromMask(new[]
                {
                    "....####....",
                    "...######...",
                    "...#o##o#...",
                    "...######...",
                    "....####....",
                    ".....##.....",
                    "..########..",
                    ".#.######.#.",
                    ".#.######.#.",
                    ".#..####..#.",
                    "....####....",
                    "....####....",
                    ".....##.....",
                    "....#..#....",
                    "....#..#....",
                    "....#..#....",
                    "....#..#....",
                    "...##..##...",
                    "............",
                    "............",
                }, new Color(0.88f, 0.86f, 0.78f), new Color(0.08f, 0.08f, 0.10f));
            return s_skeleton;
        }

        private static Sprite Ghost()
        {
            if (s_ghost == null)
                s_ghost = FromMask(new[]
                {
                    "....####....",
                    "..########..",
                    ".##########.",
                    ".##o####o##.",
                    ".##########.",
                    "############",
                    "############",
                    "############",
                    ".##########.",
                    ".##########.",
                    "..########..",
                    "..#######...",
                    "...#####....",
                    "...####.....",
                    "....###.....",
                    ".....##.....",
                    "......#.....",
                    "............",
                }, new Color(0.62f, 0.85f, 0.92f, 0.78f), new Color(0.10f, 0.22f, 0.30f, 0.9f));
            return s_ghost;
        }

        private static Sprite Bandit()
        {
            if (s_bandit == null)
                s_bandit = FromMask(new[]
                {
                    "....####....",
                    "...######...",
                    "..########..",
                    "..##o##o##..",
                    "..########..",
                    "...######...",
                    "..########..",
                    ".##########.",
                    ".##########.",
                    "#####..#####",
                    "#.####.###.#",
                    "#.####.###.#",
                    "..######....",
                    "..######....",
                    "...####.....",
                    "...#..#.....",
                    "...#..#.....",
                    "...#..#.....",
                    "..##..##....",
                    "............",
                }, new Color(0.40f, 0.22f, 0.16f), new Color(0.85f, 0.72f, 0.55f));
            return s_bandit;
        }

        private static Sprite FromMask(string[] rows, Color body, Color accent)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char p = rows[y][x];
                    var c = p == 'o' ? accent : body;
                    tex.SetPixel(x, h - 1 - y, p == '#' || p == 'o' ? c : Color.clear);
                }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit: w);
        }
    }
}

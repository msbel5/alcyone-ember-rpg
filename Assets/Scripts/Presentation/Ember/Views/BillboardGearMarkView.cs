using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// PAPER-DOLL v1 overlay seam: a small pictogram pinned to the billboard's hip line that
    /// echoes REAL role state - guards carry a spear mark, hostiles a blade. When per-actor
    /// equipment lands in the sim, item overlays plug into THIS layer; the forge is never
    /// asked to re-render a whole figure for a gear change.
    /// </summary>
    public static class BillboardGearMarkView
    {
        private static Sprite s_spear, s_blade;

        public static void TryAttach(GameObject root, string spriteRole)
        {
            if (root == null || string.IsNullOrEmpty(spriteRole)) return;
            bool guard = spriteRole.IndexOf("guard", System.StringComparison.OrdinalIgnoreCase) >= 0
                      || spriteRole.IndexOf("knight", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool armed = spriteRole.IndexOf("outlaw", System.StringComparison.OrdinalIgnoreCase) >= 0
                      || spriteRole.IndexOf("bandit", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!guard && !armed) return;

            var go = new GameObject("GearMark");
            go.transform.SetParent(root.transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0.42f, 1.05f, 0f);
            go.transform.localScale = Vector3.one * 0.42f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 11; // one above the billboard sprite
            sr.sprite = guard
                ? (s_spear != null ? s_spear : (s_spear = FromMask(SpearMask, new Color(0.72f, 0.72f, 0.78f))))
                : (s_blade != null ? s_blade : (s_blade = FromMask(BladeMask, new Color(0.62f, 0.58f, 0.52f))));
            go.AddComponent<CameraFacingBillboard>();
        }

        private static readonly string[] SpearMask =
        {
            ".....##.....", "....####....", ".....##.....", ".....##.....",
            ".....##.....", ".....##.....", ".....##.....", ".....##.....",
            ".....##.....", ".....##.....", ".....##.....", ".....##.....",
        };
        private static readonly string[] BladeMask =
        {
            "............", "....##......", "....###.....", ".....###....",
            "......###...", ".......###..", "........##..", "....##..##..",
            ".....####...", "......##....", "............", "............",
        };

        private static Sprite FromMask(string[] rows, Color color)
        {
            int h = rows.Length, w = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool on = x < rows[y].Length && rows[y][x] == '#';
                    tex.SetPixel(x, h - 1 - y, on ? color : Color.clear);
                }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 12f);
        }
    }
}

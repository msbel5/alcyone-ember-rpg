using UnityEngine;

// Design note:
// Presentation-only palette — the ONE home for the six shared Ember UI colors that
// were duplicated across EmberHud, DialogBoxPanel, and five Options sections. Any HUD
// or panel that wants the parchment / gold / brown language pulls from here so a future
// palette shift lives in ONE file. Zero gameplay/digest/save surface touched.
namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>Shared Ember UI colors — parchment, gold, and dark-brown panel language.</summary>
    public static class EmberPalette
    {
        /// <summary>#F2DB9E — the primary paper tone: text on brown, hairline accents at low alpha.</summary>
        public static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);

        /// <summary>#E6D9B3 — dimmed parchment for secondary labels.</summary>
        public static readonly Color ParchmentDim = new Color(0.902f, 0.851f, 0.702f, 1f);

        /// <summary>#FFD94C — gold for highlights and selection accents.</summary>
        public static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);

        /// <summary>#2E2417 at 92% alpha — the deep brown panel/frame fill.</summary>
        public static readonly Color PanelBrown = new Color(0.180f, 0.140f, 0.090f, 0.92f);

        /// <summary>#3A2E1D — hover-lifted brown, one shade brighter than PanelBrown.</summary>
        public static readonly Color PanelBrownHi = new Color(0.227f, 0.180f, 0.114f, 1f);

        /// <summary>Parchment at 30% alpha — hairline dividers on brown fills.</summary>
        public static readonly Color GoldHairline = new Color(0.949f, 0.859f, 0.620f, 0.30f);

        /// <summary>Copy of `c` with `alpha` overriding its alpha channel.</summary>
        public static Color Alpha(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
    }
}

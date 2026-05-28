// Design tokens — single source of truth for the project's visual style.
// Every panel template references this (via the IUiSurface it is mounted on)
// so a theme change ripples through every screen at once.
//
// Authored as a ScriptableObject so designers can tweak in the Editor without
// touching code, and so a future Figma → Code Connect sync can rewrite the
// token asset rather than patching individual templates.

using UnityEngine;

namespace EmberCrpg.Ui.Foundation
{
    /// <summary>
    /// Design tokens used by every panel template. One asset per theme; the
    /// default lives at <c>Assets/Manifests/DefaultUiTokens.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/UI/Ui Tokens", fileName = "UiTokens.asset")]
    public sealed class UiTokens : ScriptableObject
    {
        // Palette — legacy field names kept so existing templates keep compiling;
        // values re-mapped to the Ember Design System (docs/design-system).
        [Header("Palette")]
        public Color Accent = new Color(1f, 0.851f, 0.298f, 1f);       // ember-gold #FFD94C (brand accent / selection)
        public Color AccentMuted = new Color(0.18f, 0.14f, 0.09f, 1f); // panel-brown #2E2417 (button / furniture fill)
        public Color Background = new Color(0.051f, 0.051f, 0.071f, 1f); // void-cool #0D0D12 (menus / boot)
        public Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.5f);  // warm scrim over the world
        public Color Panel = new Color(0.039f, 0.035f, 0.031f, 1f);    // void-warm #0A0908 (in-world panels)
        public Color Text = new Color(0.949f, 0.859f, 0.62f, 1f);      // parchment #F2DB9E (pale-gold labels)
        public Color TextMuted = new Color(0.902f, 0.851f, 0.702f, 1f); // parchment-dim #E6D9B3
        public Color Danger = new Color(0.851f, 0.2f, 0.122f, 1f);     // vital-health #D9331F
        public Color Warning = new Color(0.851f, 0.702f, 0.102f, 1f);  // vital-fatigue #D9B31A
        public Color Success = new Color(0.46f, 0.83f, 0.55f, 1f);     // log success (no design green; kept)

        [Header("Ember Design System — backgrounds & furniture")]
        public Color VoidCool = new Color(0.051f, 0.051f, 0.071f, 1f);   // #0D0D12 menu/boot
        public Color VoidWarm = new Color(0.039f, 0.035f, 0.031f, 1f);   // #0A0908 in-world
        public Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 1f);    // #2E2417 button fill
        public Color PanelBrownHover = new Color(0.227f, 0.18f, 0.114f, 1f); // #3A2E1D hover lift
        public Color InputBrown = new Color(0.122f, 0.102f, 0.078f, 1f); // #1F1A14
        public Color WorldgenBrown = new Color(0.149f, 0.125f, 0.102f, 1f); // #26201A
        public Color ParchmentButton = new Color(0.902f, 0.839f, 0.678f, 1f); // #E6D6AD aged-parchment "white" button
        public Color GoldHairline = new Color(0.949f, 0.859f, 0.62f, 0.22f); // rgba(242,219,158,0.22) faint gold border

        [Header("Ember Design System — golds, inks & vitals")]
        public Color EmberGold = new Color(1f, 0.851f, 0.298f, 1f);      // #FFD94C wordmark
        public Color EmberAmber = new Color(0.945f, 0.769f, 0.059f, 1f); // #F1C40F highlight/selection
        public Color Ink = new Color(0.149f, 0.102f, 0.051f, 1f);        // #261A0D text on parchment
        public Color Bone = new Color(1f, 1f, 1f, 1f);                   // #FFFFFF
        public Color VitalHealth = new Color(0.851f, 0.2f, 0.122f, 1f);  // #D9331F
        public Color VitalFatigue = new Color(0.851f, 0.702f, 0.102f, 1f); // #D9B31A
        public Color VitalMana = new Color(0.2f, 0.451f, 0.949f, 1f);    // #3373F2
        public Color StateSelect = new Color(0.914f, 0.788f, 0.227f, 1f); // #E9C93A active-slot outline

        [Header("Spacing (px at reference DPI)")]
        public float SpacingXs = 4f;
        public float SpacingSm = 8f;
        public float SpacingMd = 16f;
        public float SpacingLg = 24f;
        public float SpacingXl = 40f;

        [Header("Radii (px) — softly rounded, never sharp glass")]
        public float RadiusSm = 6f;   // buttons, inputs, slots
        public float RadiusMd = 12f;  // panels, cards
        public float RadiusLg = 20f;  // modals

        [Header("Typography (px, 1080p reference)")]
        public float FontSizeSmall = 12f;   // micro / bar labels
        public float FontSizeBody = 20f;     // buttons / primary UI
        public float FontSizeHeading = 24f;  // panel headers (h2)
        public float FontSizeTitle = 42f;    // screen title
        public float FsWordmark = 92f;       // EMBER main-menu logo
        public float FsDialog = 18f;         // NPC line / topics / damage log
        public float TrackingWordmark = 0.18f;
        public float TrackingLabel = 0.08f;

        [Header("Motion")]
        public float DurOpenMs = 180f;   // panel open: fade + scale 0.92->1, EaseOutCubic
        public float DurCloseMs = 120f;  // panel close: fade + scale ->0.94, EaseInQuad
        public float TypewriterCps = 45f; // dialog reveal characters/second

        /// <summary>
        /// Resolve a log severity to its display color. Total function — every
        /// enum value maps to a defined token; the default branch falls back to
        /// <see cref="Text"/> for forward-compatibility with new severities.
        /// </summary>
        public Color SeverityColor(UiLogSeverity severity)
        {
            switch (severity)
            {
                case UiLogSeverity.Warning: return Warning;
                case UiLogSeverity.Error:   return Danger;
                case UiLogSeverity.Success: return Success;
                case UiLogSeverity.Info:    return Text;
                default:                    return Text;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Accent == Color.clear || AccentMuted == Color.clear
                || Background == Color.clear || Panel == Color.clear
                || Text == Color.clear || TextMuted == Color.clear
                || Danger == Color.clear || Warning == Color.clear
                || Success == Color.clear)
            {
                throw new System.InvalidOperationException(
                    "UiTokens palette colors cannot be Color.clear.");
            }
        }
#endif
    }
}

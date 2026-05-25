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
        [Header("Palette")]
        public Color Accent = new Color(0.92f, 0.55f, 0.22f, 1f); // ember orange
        public Color AccentMuted = new Color(0.52f, 0.34f, 0.22f, 1f);
        public Color Background = new Color(0.07f, 0.07f, 0.09f, 1f);
        public Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.6f); // modal scrims
        public Color Panel = new Color(0.11f, 0.12f, 0.15f, 1f);
        public Color Text = new Color(0.96f, 0.96f, 0.98f, 1f);
        public Color TextMuted = new Color(0.65f, 0.66f, 0.72f, 1f);
        public Color Danger = new Color(0.94f, 0.39f, 0.39f, 1f);
        public Color Warning = new Color(0.95f, 0.78f, 0.34f, 1f);
        public Color Success = new Color(0.46f, 0.83f, 0.55f, 1f);

        [Header("Spacing (px at reference DPI)")]
        public float SpacingXs = 4f;
        public float SpacingSm = 8f;
        public float SpacingMd = 16f;
        public float SpacingLg = 24f;
        public float SpacingXl = 40f;

        [Header("Typography (px)")]
        public float FontSizeSmall = 12f;
        public float FontSizeBody = 14f;
        public float FontSizeHeading = 22f;
        public float FontSizeTitle = 32f;

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

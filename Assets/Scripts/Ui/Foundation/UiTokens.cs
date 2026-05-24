// PRD: docs/prds/visible-generation-and-consistent-ui.md (Phase 1)
//
// UiTokens is the single source of truth for the project's visual style —
// colors, spacing, typography. Every panel template references it (via the
// IUiSurface it is mounted on) so a theme change ripples through every
// screen at once.
//
// Authored as a ScriptableObject so designers can tweak in the Editor
// without touching code, and so Phase 6's Figma → Code Connect sync can
// rewrite a token asset rather than patching individual templates.

using UnityEngine;

namespace EmberCrpg.Ui.Foundation
{
    /// <summary>
    /// Design tokens used by every panel template. One asset per theme;
    /// the default lives at <c>Assets/Ui/Tokens/DefaultUiTokens.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/UI/Ui Tokens", fileName = "UiTokens.asset")]
    public sealed class UiTokens : ScriptableObject
    {
        [Header("Background colors")]
        public Color BackgroundPrimary = new Color(0.07f, 0.07f, 0.09f, 1f);
        public Color BackgroundSecondary = new Color(0.11f, 0.12f, 0.15f, 1f);
        public Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.6f);

        [Header("Foreground colors")]
        public Color TextPrimary = new Color(0.96f, 0.96f, 0.98f, 1f);
        public Color TextMuted = new Color(0.65f, 0.66f, 0.72f, 1f);
        public Color Accent = new Color(0.92f, 0.55f, 0.22f, 1f); // ember orange

        [Header("Log severity colors")]
        public Color LogInfo = new Color(0.78f, 0.82f, 0.90f, 1f);
        public Color LogWarning = new Color(0.95f, 0.78f, 0.34f, 1f);
        public Color LogError = new Color(0.94f, 0.39f, 0.39f, 1f);
        public Color LogSuccess = new Color(0.46f, 0.83f, 0.55f, 1f);

        [Header("Spacing scale (px at reference DPI)")]
        public float SpaceXS = 4f;
        public float SpaceSM = 8f;
        public float SpaceMD = 16f;
        public float SpaceLG = 24f;
        public float SpaceXL = 40f;

        [Header("Typography")]
        public float FontSizeSmall = 12f;
        public float FontSizeBody = 14f;
        public float FontSizeHeading = 22f;
        public float FontSizeTitle = 32f;

        /// <summary>
        /// Resolve a log severity to its display color.
        /// </summary>
        public Color ColorForSeverity(UiLogSeverity severity)
        {
            switch (severity)
            {
                case UiLogSeverity.Warning: return LogWarning;
                case UiLogSeverity.Error: return LogError;
                case UiLogSeverity.Success: return LogSuccess;
                default: return LogInfo;
            }
        }
    }
}

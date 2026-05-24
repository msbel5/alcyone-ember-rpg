using UnityEngine;

namespace EmberCrpg.Ui.Foundation
{
    [CreateAssetMenu(menuName = "Ember/UI/Ui Tokens", fileName = "UiTokens.asset")]
    public sealed class UiTokens : ScriptableObject
    {
        [Header("Palette")]
        public Color Accent = new Color(0.92f, 0.55f, 0.22f, 1f);
        public Color AccentMuted = new Color(0.52f, 0.34f, 0.22f, 1f);
        public Color Background = new Color(0.07f, 0.07f, 0.09f, 1f);
        public Color Panel = new Color(0.11f, 0.12f, 0.15f, 1f);
        public Color Text = new Color(0.96f, 0.96f, 0.98f, 1f);
        public Color TextMuted = new Color(0.65f, 0.66f, 0.72f, 1f);
        public Color Danger = new Color(0.94f, 0.39f, 0.39f, 1f);
        public Color Warning = new Color(0.95f, 0.78f, 0.34f, 1f);
        public Color Success = new Color(0.46f, 0.83f, 0.55f, 1f);

        [Header("Legacy aliases")]
        public Color BackgroundPrimary = new Color(0.07f, 0.07f, 0.09f, 1f);
        public Color BackgroundSecondary = new Color(0.11f, 0.12f, 0.15f, 1f);
        public Color BackgroundOverlay = new Color(0f, 0f, 0f, 0.6f);
        public Color TextPrimary = new Color(0.96f, 0.96f, 0.98f, 1f);
        public Color LogInfo = new Color(0.78f, 0.82f, 0.90f, 1f);
        public Color LogWarning = new Color(0.95f, 0.78f, 0.34f, 1f);
        public Color LogError = new Color(0.94f, 0.39f, 0.39f, 1f);
        public Color LogSuccess = new Color(0.46f, 0.83f, 0.55f, 1f);

        [Header("Spacing scale")]
        public float SpacingXs = 4f;
        public float SpacingSm = 8f;
        public float SpacingMd = 16f;
        public float SpacingLg = 24f;
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

        public Color SeverityColor(UiLogSeverity severity)
        {
            switch (severity)
            {
                case UiLogSeverity.Warning: return Warning;
                case UiLogSeverity.Error: return Danger;
                case UiLogSeverity.Success: return Success;
                default: return Text;
            }
        }

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Accent == Color.clear || AccentMuted == Color.clear || Background == Color.clear || Panel == Color.clear || Text == Color.clear || TextMuted == Color.clear || Danger == Color.clear || Warning == Color.clear || Success == Color.clear)
                throw new System.InvalidOperationException("UiTokens colors cannot be Color.clear.");
        }
#endif
    }
}

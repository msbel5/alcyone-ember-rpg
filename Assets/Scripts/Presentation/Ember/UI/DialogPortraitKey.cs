using System;
using System.IO;
using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.UI
{
    public static class DialogPortraitKey
    {
        public const string Default = "blacksmith";

        public static string FromSource(IDialogSource source)
        {
            if (source is IDialogSourcePortrait portraitSource)
                return Normalize(portraitSource.GetPortraitName());
            return Default;
        }

        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Default;

            var key = raw.Trim();
            if (LooksLikePath(key))
                key = Path.GetFileNameWithoutExtension(key);

            if (string.Equals(key, "portrait_npc_placeholder", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "portrait_player_placeholder", StringComparison.OrdinalIgnoreCase))
                return Default;

            if (key.StartsWith("portrait_", StringComparison.OrdinalIgnoreCase) &&
                key.EndsWith("_placeholder", StringComparison.OrdinalIgnoreCase) &&
                key.Length > "portrait__placeholder".Length)
            {
                key = key.Substring("portrait_".Length, key.Length - "portrait_".Length - "_placeholder".Length);
            }

            key = NormalizeAlias(key);
            return string.IsNullOrWhiteSpace(key) ? Default : key;
        }

        private static bool LooksLikePath(string key)
        {
            return key.IndexOf('/') >= 0
                || key.IndexOf('\\') >= 0
                || key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAlias(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Default;
            var lower = key.ToLowerInvariant();
            if (lower.Contains("guard") || lower.Contains("warden") || lower.Contains("noble")) return "knight";
            if (lower.Contains("priest") || lower.Contains("scholar") || lower.Contains("elder")) return "sage";
            if (lower.Contains("artisan") || lower.Contains("smith")) return "blacksmith";
            if (lower.Contains("innkeeper") || lower.Contains("farmer")) return "innkeeper";
            if (lower.Contains("merchant")) return "merchant";
            if (lower.Contains("warrior") || lower.Contains("outlaw")) return "warrior";
            return lower;
        }
    }
}

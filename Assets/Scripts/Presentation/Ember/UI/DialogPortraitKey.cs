using System;
using System.IO;
using System.Text;
using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.UI
{
    public static class DialogPortraitKey
    {
        public const string Default = "portrait_npc_placeholder";
        public const string DungeonMaster = "dm_portrait";

        public static string FromSource(IDialogSource source)
        {
            if (source is IDialogSourcePortrait portraitSource)
                return Normalize(portraitSource.GetPortraitName());
            return Default;
        }

        public static bool IsPortraitKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var trimmed = key.Trim();
            return string.Equals(trimmed, DungeonMaster, StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("portrait_", StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Default;

            var key = raw.Trim();
            if (LooksLikePath(key))
                key = Path.GetFileNameWithoutExtension(key);

            key = key.Replace(' ', '_').Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
                return Default;

            if (string.Equals(key, DungeonMaster, StringComparison.OrdinalIgnoreCase))
                return DungeonMaster;

            if (string.Equals(key, Default, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "portrait_player_placeholder", StringComparison.OrdinalIgnoreCase))
                return Default;

            if (key.StartsWith("portrait_", StringComparison.OrdinalIgnoreCase))
                return NormalizePortraitKey(key);

            return "portrait_npc_" + SanitizeFragment(key);
        }

        private static bool LooksLikePath(string key)
        {
            return key.IndexOf('/') >= 0
                || key.IndexOf('\\') >= 0
                || key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePortraitKey(string key)
        {
            if (string.Equals(key, Default, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "portrait_player_placeholder", StringComparison.OrdinalIgnoreCase))
                return Default;
            if (key.StartsWith("portrait_player_", StringComparison.OrdinalIgnoreCase))
                return Default;

            var fragment = key.StartsWith("portrait_npc_", StringComparison.OrdinalIgnoreCase)
                ? key.Substring("portrait_npc_".Length)
                : key.Substring("portrait_".Length);
            if (fragment.EndsWith("_placeholder", StringComparison.OrdinalIgnoreCase))
                fragment = fragment.Substring(0, fragment.Length - "_placeholder".Length);

            var slug = SanitizeFragment(fragment);
            return string.IsNullOrWhiteSpace(slug) ? Default : "portrait_npc_" + slug;
        }

        private static string SanitizeFragment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "placeholder";

            var builder = new StringBuilder(value.Length);
            bool lastWasUnderscore = false;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = char.ToLowerInvariant(value[i]);
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore)
                {
                    builder.Append('_');
                    lastWasUnderscore = true;
                }
            }

            var slug = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(slug) ? "placeholder" : slug;
        }
    }
}

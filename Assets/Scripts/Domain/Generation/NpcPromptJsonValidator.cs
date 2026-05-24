using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmberCrpg.Domain.Generation
{
    public static class NpcPromptJsonValidator
    {
        private static readonly string[] Required =
        {
            "archetype_id", "primary_hue_degrees", "secondary_hue_degrees", "mood_keywords",
            "distinctive_features", "clothing_style", "accessory", "world_style_anchor"
        };

        public static bool TryValidate(string json, GenericNpcBaseManifest manifest, out NpcPromptJson value, out string reason)
        {
            value = null;
            reason = string.Empty;
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(json)) { reason = "invalid_json"; return false; }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(json, "\\\"([^\\\"]+)\\\"\\s*:")) keys.Add(match.Groups[1].Value);
            foreach (var key in keys)
                if (Array.IndexOf(Required, key) < 0) { reason = "unknown_field:" + key; return false; }
            foreach (var key in Required)
                if (!keys.Contains(key)) { reason = "missing_field:" + key; return false; }

            var archetype = GetString(json, "archetype_id");
            if (!ValidateString(archetype, out reason)) return false;
            if (!manifest.Contains(archetype)) { reason = "unknown_archetype"; return false; }

            if (!GetInt(json, "primary_hue_degrees", out var primary) || !GetInt(json, "secondary_hue_degrees", out var secondary)) { reason = "invalid_json"; return false; }
            if (primary < 0 || primary >= 360 || secondary < 0 || secondary >= 360) { reason = "hue_out_of_range"; return false; }

            var moods = GetArray(json, "mood_keywords");
            var features = GetArray(json, "distinctive_features");
            if (moods.Count > 5 || features.Count > 5) { reason = "array_too_long"; return false; }
            foreach (var text in moods) if (!ValidateString(text, out reason)) return false;
            foreach (var text in features) if (!ValidateString(text, out reason)) return false;

            var clothing = GetString(json, "clothing_style");
            var accessory = GetString(json, "accessory");
            var anchor = GetString(json, "world_style_anchor");
            if (!ValidateString(clothing, out reason)) return false;
            if (!ValidateString(accessory, out reason)) return false;
            if (!ValidateString(anchor, out reason)) return false;

            value = new NpcPromptJson(archetype, primary, secondary, moods, features, clothing, accessory, anchor);
            return true;
        }

        private static string GetString(string json, string key)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"])*)\\\"");
            return match.Success ? Unescape(match.Groups[1].Value) : string.Empty;
        }

        private static bool GetInt(string json, string key, out int value)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(-?\\d+)");
            return int.TryParse(match.Success ? match.Groups[1].Value : string.Empty, out value);
        }

        private static List<string> GetArray(string json, string key)
        {
            var list = new List<string>();
            var match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\[(.*?)\\]");
            if (!match.Success) return list;
            foreach (Match item in Regex.Matches(match.Groups[1].Value, "\\\"((?:\\\\.|[^\\\"])*)\\\""))
                list.Add(Unescape(item.Groups[1].Value));
            return list;
        }

        private static bool ValidateString(string value, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(value)) { reason = "empty_string"; return false; }
            if (value.Length > 40) { reason = "string_too_long"; return false; }
            for (int i = 0; i < value.Length; i++)
                if (value[i] > 127) { reason = "non_ascii"; return false; }
            return true;
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf("\\u", StringComparison.OrdinalIgnoreCase) >= 0) return "\u0100";
            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}

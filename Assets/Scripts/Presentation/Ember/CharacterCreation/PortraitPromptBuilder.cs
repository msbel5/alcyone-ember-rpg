using System.Collections.Generic;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public static class PortraitPromptBuilder
    {
        public static string Build(NpcPromptJson json)
        {
            var archetype = Clean(json?.ArchetypeId, "figure");
            var distinctive = Join(cleaned: CleanList(json?.DistinctiveFeatures), fallback: "distinctive features");
            var clothing = Clean(json?.ClothingStyle, "travel-worn attire");
            var accessory = Clean(json?.Accessory, "simple accessory");
            var mood = FirstOrFallback(CleanList(json?.MoodKeywords), "stoic");
            var worldStyle = Clean(json?.WorldStyleAnchor, "ember-warm");
            var primary = json?.PrimaryHueDegrees ?? 28;
            var secondary = json?.SecondaryHueDegrees ?? 215;

            return archetype
                + " with " + distinctive
                + ", wearing " + clothing
                + ", " + accessory
                + ", " + mood + " expression"
                + ", " + worldStyle + " palette (hues " + primary + "/" + secondary + ")";
        }

        private static string Clean(string value, string fallback)
        {
            var source = string.IsNullOrWhiteSpace(value) ? fallback : value;
            var normalized = (source ?? string.Empty).Trim().Replace('_', ' ').Replace('-', ' ');
            return JoinWords(normalized, fallback);
        }

        private static IReadOnlyList<string> CleanList(IReadOnlyList<string> values)
        {
            var cleaned = new List<string>();
            if (values == null) return cleaned;
            for (int i = 0; i < values.Count; i++)
            {
                var token = JoinWords((values[i] ?? string.Empty).Trim().Replace('_', ' ').Replace('-', ' '), string.Empty);
                if (!string.IsNullOrWhiteSpace(token))
                    cleaned.Add(token);
            }

            return cleaned;
        }

        private static string FirstOrFallback(IReadOnlyList<string> values, string fallback)
        {
            if (values != null && values.Count > 0) return values[0];
            return fallback;
        }

        private static string Join(IReadOnlyList<string> cleaned, string fallback)
        {
            if (cleaned == null || cleaned.Count == 0) return fallback;
            return string.Join(", ", cleaned);
        }

        private static string JoinWords(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var pieces = value.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 0) return fallback;
            return string.Join(" ", pieces);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace EmberCrpg.Domain.Generation
{
    public sealed class NpcPromptJson
    {
        public NpcPromptJson(string archetypeId, int primaryHueDegrees, int secondaryHueDegrees, IEnumerable<string> moodKeywords, IEnumerable<string> distinctiveFeatures, string clothingStyle, string accessory, string worldStyleAnchor)
        {
            ArchetypeId = archetypeId ?? string.Empty;
            PrimaryHueDegrees = primaryHueDegrees;
            SecondaryHueDegrees = secondaryHueDegrees;
            MoodKeywords = new ReadOnlyCollection<string>(new List<string>(moodKeywords ?? Array.Empty<string>()));
            DistinctiveFeatures = new ReadOnlyCollection<string>(new List<string>(distinctiveFeatures ?? Array.Empty<string>()));
            ClothingStyle = clothingStyle ?? string.Empty;
            Accessory = accessory ?? string.Empty;
            WorldStyleAnchor = worldStyleAnchor ?? string.Empty;
        }

        public string ArchetypeId { get; }
        public int PrimaryHueDegrees { get; }
        public int SecondaryHueDegrees { get; }
        public IReadOnlyList<string> MoodKeywords { get; }
        public IReadOnlyList<string> DistinctiveFeatures { get; }
        public string ClothingStyle { get; }
        public string Accessory { get; }
        public string WorldStyleAnchor { get; }

        public string ToCanonicalJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            AppendProp(sb, "archetype_id", ArchetypeId); sb.Append(',');
            sb.Append("\"primary_hue_degrees\":").Append(PrimaryHueDegrees.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"secondary_hue_degrees\":").Append(SecondaryHueDegrees.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendArray(sb, "mood_keywords", MoodKeywords); sb.Append(',');
            AppendArray(sb, "distinctive_features", DistinctiveFeatures); sb.Append(',');
            AppendProp(sb, "clothing_style", ClothingStyle); sb.Append(',');
            AppendProp(sb, "accessory", Accessory); sb.Append(',');
            AppendProp(sb, "world_style_anchor", WorldStyleAnchor);
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendProp(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        }

        private static void AppendArray(StringBuilder sb, string key, IReadOnlyList<string> values)
        {
            sb.Append('"').Append(key).Append("\":[");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Escape(values[i])).Append('"');
            }
            sb.Append(']');
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

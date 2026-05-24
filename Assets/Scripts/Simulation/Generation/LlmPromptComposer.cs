using System.Text;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public static class LlmPromptComposer
    {
        public static string Compose(NpcPromptJson json)
        {
            var sb = new StringBuilder();
            sb.Append(StaticPromptCatalog.EmberStyleHeader);
            sb.Append(", archetype ").Append(json.ArchetypeId);
            sb.Append(", primary hue ").Append(json.PrimaryHueDegrees);
            sb.Append(", secondary hue ").Append(json.SecondaryHueDegrees);
            AppendList(sb, ", mood ", json.MoodKeywords);
            AppendList(sb, ", features ", json.DistinctiveFeatures);
            sb.Append(", clothing ").Append(json.ClothingStyle);
            sb.Append(", accessory ").Append(json.Accessory);
            sb.Append(", world style anchor ").Append(json.WorldStyleAnchor);
            sb.Append(", ").Append(StaticPromptCatalog.EmberNegativeFooter);
            return sb.ToString();
        }

        private static void AppendList(StringBuilder sb, string label, System.Collections.Generic.IReadOnlyList<string> values)
        {
            sb.Append(label);
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sb.Append(" and ");
                sb.Append(values[i]);
            }
        }
    }
}

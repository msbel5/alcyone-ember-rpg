using System.Collections.Generic;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// REFORM #3 (typed seam primitives): every pure text rule of the dialog stream lives HERE,
    /// fallback-testable, instead of being buried in Unity-bound adapter partials. The W28
    /// incident (a weak model parroting the FOLLOWUPS instruction into the visible answer)
    /// is pinned as adversarial tests against these exact functions.
    /// </summary>
    public static class DialogStreamText
    {
        public const string FollowupsInstruction =
            " End with one final line exactly like: FOLLOWUPS: first question | second question | third question" +
            " - three short in-character questions the traveller might naturally ask you NEXT.";

        /// <summary>Split "answer ... FOLLOWUPS: q1 | q2 | q3" into (answer, questions). The
        /// marker is honored WHEREVER it appears; an instruction-only reply yields an EMPTY
        /// body (callers keep their deterministic line); parrots are rejected.</summary>
        public static (string Body, List<string> Followups) SplitFollowups(string answer)
        {
            var none = (answer, (List<string>)null);
            if (string.IsNullOrEmpty(answer)) return none;
            int at = answer.IndexOf("FOLLOWUPS", System.StringComparison.OrdinalIgnoreCase);
            if (at < 0) return none;
            var body = answer.Substring(0, at).TrimEnd().TrimEnd(':', '-');
            var tail = answer.Substring(at);
            int colon = tail.IndexOf(':');
            var list = new List<string>();
            if (colon >= 0)
            {
                foreach (var raw in tail.Substring(colon + 1).Split('|'))
                {
                    var q = raw.Trim().TrimStart('-', '*', ' ').Trim();
                    if (IsRealFollowup(q) && list.Count < 3) list.Add(q);
                }
            }
            return (body, list.Count > 0 ? list : null);
        }

        public static bool IsRealFollowup(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 8 || q.Length > 110) return false;
            if (!q.EndsWith("?", System.StringComparison.Ordinal)) return false;
            var lower = q.ToLowerInvariant();
            if (lower.Contains("first question") || lower.Contains("second question")
                || lower.Contains("third question") || lower.Contains("in-character")
                || lower.Contains("traveller might") || lower.Contains("followups"))
                return false;
            return true;
        }

        /// <summary>Menu labels become sentences a person would actually SAY (and hear).</summary>
        public static string NaturalQuestion(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return label;
            var trimmed = label.Trim();
            if (trimmed.StartsWith("Ask about Companion", System.StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("companion_join", System.StringComparison.OrdinalIgnoreCase))
                return "Will you travel with me?";
            if (trimmed.StartsWith("companion_leave", System.StringComparison.OrdinalIgnoreCase))
                return "It is time we parted ways.";
            if (trimmed.StartsWith("Ask about ", System.StringComparison.OrdinalIgnoreCase))
                return "What can you tell me about " + trimmed.Substring(10).TrimEnd('.', '\u2026') + "?";
            return trimmed;
        }
    }
}

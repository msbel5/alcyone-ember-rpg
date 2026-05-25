using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public enum WorldgenVisibleEventKind
    {
        Narration,
        RegionGenerated,
        SettlementSeeded,
        NpcSeeded,
        HistoryProjected,
        DiceRolled,
        QuestionRaised,
        Failure,
        Completed,
    }

    public sealed class WorldgenVisibleEvent
    {
        private WorldgenVisibleEvent(WorldgenVisibleEventKind kind, string id, string message, IReadOnlyList<string> options, string payloadJson)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Message = message ?? string.Empty;
            Options = options ?? new string[0];
            PayloadJson = payloadJson ?? string.Empty;
        }

        public WorldgenVisibleEventKind Kind { get; }
        public string Id { get; }
        public string Message { get; }
        public IReadOnlyList<string> Options { get; }
        public string PayloadJson { get; }

        public static WorldgenVisibleEvent Narration(string id, string message) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.Narration, id, message, null, string.Empty);
        public static WorldgenVisibleEvent Region(string id) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.RegionGenerated, id, "[billboard][region] Generated " + id, null, string.Empty);
        public static WorldgenVisibleEvent Settlement(string id, string region) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.SettlementSeeded, id, "[billboard][settlement] " + region + "/" + id, null, string.Empty);
        public static WorldgenVisibleEvent Npc(string id, string json) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.NpcSeeded, id, "[billboard][npc] " + id + " [decision-json] " + (json ?? "{}"), null, json);
        public static WorldgenVisibleEvent History(int year, string headline, string summary, IReadOnlyList<string> tags)
        {
            var tagLine = tags == null || tags.Count == 0 ? "[history]" : "[" + string.Join(", ", tags) + "]";
            var message = "Year " + year + " - " + (headline ?? "Unrecorded event") + "\n" + (summary ?? string.Empty) + "\n" + tagLine;
            return new WorldgenVisibleEvent(WorldgenVisibleEventKind.HistoryProjected, "history-" + year, message, null, string.Empty);
        }
        public static WorldgenVisibleEvent Dice(string reason, int faces, int value) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.DiceRolled, reason, "[dice] " + reason + ": d" + faces + " = " + value, null, string.Empty);
        public static WorldgenVisibleEvent Question(string id, string message, IReadOnlyList<string> options) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.QuestionRaised, id, message, options, string.Empty);
        public static WorldgenVisibleEvent Failure(string reason, string code = "worldgen.failure", bool continueGeneration = true)
        {
            var safeReason = reason ?? string.Empty;
            var payload = "{\"code\":\"" + EscapeJson(code) + "\",\"reason\":\"" + EscapeJson(safeReason) + "\",\"continue\":" + (continueGeneration ? "true" : "false") + "}";
            return new WorldgenVisibleEvent(WorldgenVisibleEventKind.Failure, "failure", "[error] " + safeReason, null, payload);
        }
        public static WorldgenVisibleEvent Completed(string stats) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.Completed, "completed", "[done] " + stats, null, string.Empty);

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

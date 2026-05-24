using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public enum WorldgenVisibleEventKind
    {
        RegionGenerated,
        SettlementSeeded,
        NpcSeeded,
        DiceRolled,
        QuestionRaised,
        Failure,
        Completed,
    }

    public sealed class WorldgenVisibleEvent
    {
        private WorldgenVisibleEvent(WorldgenVisibleEventKind kind, string id, string message, IReadOnlyList<string> options)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Message = message ?? string.Empty;
            Options = options ?? new string[0];
        }

        public WorldgenVisibleEventKind Kind { get; }
        public string Id { get; }
        public string Message { get; }
        public IReadOnlyList<string> Options { get; }

        public static WorldgenVisibleEvent Region(string id) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.RegionGenerated, id, "[region] Generated " + id, null);
        public static WorldgenVisibleEvent Settlement(string id, string region) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.SettlementSeeded, id, "[settlement] " + region + "/" + id, null);
        public static WorldgenVisibleEvent Npc(string id, string json) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.NpcSeeded, id, "[npc] " + id + " [llm-json] " + json, null);
        public static WorldgenVisibleEvent Dice(string reason, int faces, int value) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.DiceRolled, reason, "[dice] " + reason + ": d" + faces + " = " + value, null);
        public static WorldgenVisibleEvent Question(string id, string message, IReadOnlyList<string> options) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.QuestionRaised, id, message, options);
        public static WorldgenVisibleEvent Failure(string reason) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.Failure, "failure", "[error] " + reason, null);
        public static WorldgenVisibleEvent Completed(string stats) => new WorldgenVisibleEvent(WorldgenVisibleEventKind.Completed, "completed", "[done] " + stats, null);
    }
}

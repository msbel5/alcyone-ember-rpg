using System.Collections.Generic;
using System.Globalization;
using EmberCrpg.Simulation.Worldgen;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public sealed class WorldgenProjectionOptions
    {
        public WorldgenProjectionOptions(
            int maxRegions = 12,
            int maxSettlements = 24,
            int maxNpcs = 32,
            int maxHistoryEvents = 30,
            bool includeQuestionPrompt = true,
            bool includeSyntheticFailure = false)
        {
            MaxRegions = NormalizeLimit(maxRegions);
            MaxSettlements = NormalizeLimit(maxSettlements);
            MaxNpcs = NormalizeLimit(maxNpcs);
            MaxHistoryEvents = NormalizeLimit(maxHistoryEvents);
            IncludeQuestionPrompt = includeQuestionPrompt;
            IncludeSyntheticFailure = includeSyntheticFailure;
        }

        public int MaxRegions { get; }
        public int MaxSettlements { get; }
        public int MaxNpcs { get; }
        public int MaxHistoryEvents { get; }
        public bool IncludeQuestionPrompt { get; }
        public bool IncludeSyntheticFailure { get; }

        private static int NormalizeLimit(int value) => value < 1 ? 1 : value;
    }

    public static class WorldgenEventProjector
    {
        public static List<WorldgenVisibleEvent> Project(GeneratedWorld world, WorldgenProjectionOptions options = null)
        {
            if (world == null) throw new System.ArgumentNullException(nameof(world));

            var projection = options ?? new WorldgenProjectionOptions();
            var events = new List<WorldgenVisibleEvent>();

            ProjectRegions(world, projection, events);
            ProjectSettlements(world, projection, events);
            ProjectNpcDecisions(world, projection, events);
            ProjectHistory(world, projection, events);

            events.Add(WorldgenVisibleEvent.Dice("founding_omen", 20, (int)(world.Seed % 20) + 1));

            if (projection.IncludeQuestionPrompt)
            {
                events.Add(WorldgenVisibleEvent.Question(
                    "worldgen-start-scene",
                    "The genesis ledger is complete. Where should the commander begin?",
                    new[] { "capital gates", "trade road", "wild frontier" }));
            }

            if (projection.IncludeSyntheticFailure)
            {
                events.Add(WorldgenVisibleEvent.Failure("Projection warning: one NPC portrait seed could not be hydrated.", "projection.npc_hydration", continueGeneration: true));
            }

            events.Add(WorldgenVisibleEvent.Completed(
                "World built. Seed: " + world.Seed.ToString(CultureInfo.InvariantCulture) +
                ", Regions: " + world.Regions.Count.ToString(CultureInfo.InvariantCulture) +
                ", Settlements: " + world.Settlements.Count.ToString(CultureInfo.InvariantCulture) +
                ", NPCs: " + world.Npcs.Count.ToString(CultureInfo.InvariantCulture) +
                ", History events: " + world.History.Count.ToString(CultureInfo.InvariantCulture) +
                ", Population: " + world.TotalPopulation.ToString("N0", CultureInfo.InvariantCulture) + "."));

            return events;
        }

        public static List<WorldgenVisibleEvent> CreateMockEvents(int regions, int settlements, int npcs, bool includeQuestion, bool includeFailure)
        {
            var events = new List<WorldgenVisibleEvent>();
            var safeRegions = regions <= 0 ? 1 : regions;
            for (int i = 0; i < safeRegions; i++) events.Add(WorldgenVisibleEvent.Region("region_" + i));
            for (int i = 0; i < settlements; i++) events.Add(WorldgenVisibleEvent.Settlement("settlement_" + i, "region_" + (i % safeRegions)));
            for (int i = 0; i < npcs; i++) events.Add(WorldgenVisibleEvent.Npc("npc_" + i, "{\"npc_id\":" + i.ToString(CultureInfo.InvariantCulture) + ",\"archetype_id\":\"humanoid_male\"}"));
            events.Add(WorldgenVisibleEvent.Dice("start omen", 20, 13));
            if (includeQuestion) events.Add(WorldgenVisibleEvent.Question("q1", "Choose a road", new[] { "left", "right" }));
            if (includeFailure) events.Add(WorldgenVisibleEvent.Failure("mock generation failure", "mock.failure", continueGeneration: true));
            events.Add(WorldgenVisibleEvent.Completed("World built. Regions: " + regions + ", Settlements: " + settlements + ", NPCs: " + npcs + "."));
            return events;
        }

        private static void ProjectRegions(GeneratedWorld world, WorldgenProjectionOptions options, List<WorldgenVisibleEvent> events)
        {
            int count = world.Regions.Count < options.MaxRegions ? world.Regions.Count : options.MaxRegions;
            for (int i = 0; i < count; i++)
            {
                var region = world.Regions[i];
                events.Add(WorldgenVisibleEvent.Region(region.Name));
            }

            int skipped = world.Regions.Count - count;
            if (skipped > 0)
            {
                events.Add(WorldgenVisibleEvent.Narration(
                    "regions-omitted",
                    "[billboard][region] " + skipped.ToString(CultureInfo.InvariantCulture) + " additional regions projected off-screen."));
            }
        }

        private static void ProjectSettlements(GeneratedWorld world, WorldgenProjectionOptions options, List<WorldgenVisibleEvent> events)
        {
            int count = world.Settlements.Count < options.MaxSettlements ? world.Settlements.Count : options.MaxSettlements;
            for (int i = 0; i < count; i++)
            {
                var settlement = world.Settlements[i];
                events.Add(WorldgenVisibleEvent.Settlement(settlement.Name, "region#" + settlement.Region.Value.ToString(CultureInfo.InvariantCulture)));
            }

            int skipped = world.Settlements.Count - count;
            if (skipped > 0)
            {
                events.Add(WorldgenVisibleEvent.Narration(
                    "settlements-omitted",
                    "[billboard][settlement] " + skipped.ToString(CultureInfo.InvariantCulture) + " additional settlements collapsed into distance layers."));
            }
        }

        private static void ProjectNpcDecisions(GeneratedWorld world, WorldgenProjectionOptions options, List<WorldgenVisibleEvent> events)
        {
            int count = world.Npcs.Count < options.MaxNpcs ? world.Npcs.Count : options.MaxNpcs;
            for (int i = 0; i < count; i++)
            {
                var npc = world.Npcs[i];
                var decisionJson =
                    "{\"npc_id\":" + npc.Id.Value.ToString(CultureInfo.InvariantCulture) +
                    ",\"name\":\"" + EscapeJson(npc.Name) + "\"" +
                    ",\"role\":\"" + npc.Role + "\"" +
                    ",\"home_settlement_id\":" + npc.Home.Value.ToString(CultureInfo.InvariantCulture) +
                    ",\"faction_id\":" + npc.Faction.Value.ToString(CultureInfo.InvariantCulture) +
                    ",\"birth_year\":" + npc.BirthYear.ToString(CultureInfo.InvariantCulture) +
                    ",\"prompt_decision\":\"accept\"}";
                events.Add(WorldgenVisibleEvent.Npc("npc_" + npc.Id.Value.ToString(CultureInfo.InvariantCulture), decisionJson));
            }

            int skipped = world.Npcs.Count - count;
            if (skipped > 0)
            {
                events.Add(WorldgenVisibleEvent.Narration(
                    "npcs-omitted",
                    "[billboard][npc] " + skipped.ToString(CultureInfo.InvariantCulture) + " additional NPC prompt decisions batched."));
            }
        }

        private static void ProjectHistory(GeneratedWorld world, WorldgenProjectionOptions options, List<WorldgenVisibleEvent> events)
        {
            int count = world.History.Count < options.MaxHistoryEvents ? world.History.Count : options.MaxHistoryEvents;
            for (int i = 0; i < count; i++)
            {
                var history = world.History[i];
                var tags = new[]
                {
                    "kind:" + history.Kind.ToString().ToLowerInvariant(),
                    "subject:" + history.Subject,
                };
                events.Add(WorldgenVisibleEvent.History(history.Year, history.Kind + " - " + history.Subject, history.Detail, tags));
            }

            int skipped = world.History.Count - count;
            if (skipped > 0)
            {
                events.Add(WorldgenVisibleEvent.Narration(
                    "history-omitted",
                    "[history] " + skipped.ToString(CultureInfo.InvariantCulture) + " additional years archived."));
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

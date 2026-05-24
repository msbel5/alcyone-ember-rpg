using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public static class WorldgenEventProjector
    {
        public static List<WorldgenVisibleEvent> CreateMockEvents(int regions, int settlements, int npcs, bool includeQuestion, bool includeFailure)
        {
            var events = new List<WorldgenVisibleEvent>();
            for (int i = 0; i < regions; i++) events.Add(WorldgenVisibleEvent.Region("region_" + i));
            for (int i = 0; i < settlements; i++) events.Add(WorldgenVisibleEvent.Settlement("settlement_" + i, "region_" + (i % regions)));
            for (int i = 0; i < npcs; i++) events.Add(WorldgenVisibleEvent.Npc("npc_" + i, "{\"archetype_id\":\"humanoid_male\"}"));
            events.Add(WorldgenVisibleEvent.Dice("start omen", 20, 13));
            if (includeQuestion) events.Add(WorldgenVisibleEvent.Question("q1", "Choose a road", new[] { "left", "right" }));
            if (includeFailure) events.Add(WorldgenVisibleEvent.Failure("mock generation failure"));
            events.Add(WorldgenVisibleEvent.Completed("World built. Regions: " + regions + ", Settlements: " + settlements + ", NPCs: " + npcs + "."));
            return events;
        }
    }
}

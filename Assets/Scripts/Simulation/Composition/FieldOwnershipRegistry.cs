using System.Collections.Generic;

namespace EmberCrpg.Simulation.Composition
{
    /// <summary>
    /// REFORM #2 (single-writer-per-field, declared): the OWNERSHIP LEDGER of every mutable
    /// actor/world field and the systems allowed to write it, in cadence:order form. This is
    /// executable documentation: the lint test fails when a writer is added to the tick
    /// registry without declaring itself here - the guard-pursuit class of conflict
    /// (an undeclared second writer at a faster cadence) becomes a CI event.
    /// </summary>
    public static class FieldOwnershipRegistry
    {
        /// <summary>field -> declared writers as "systemId@Cadence:Order".</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Writers =
            new Dictionary<string, string[]>
            {
                ["Actor.Position"] = new[]
                {
                    "living.schedule@PerTick:20",        // routine movement
                    "living.companion_follow@PerTick:21", // heel AFTER schedule, by design
                    "living.predation@Hourly:40",         // hunters step toward prey
                    "living.witness@Hourly:45",           // civilians shy from trouble + guard nudge
                    "living.ambient@Hourly:50",           // critters only (not actors) - listed for audit
                },
                ["Actor.Needs"] = new[]
                {
                    "living.needs@Hourly:30",        // the ramps
                    "living.eatOnArrival@PerTick:22", // arrival meals
                    "living.consumption@Hourly:35",  // metabolism half
                },
                ["Actor.Vitals"] = new[]
                {
                    "living.predation@Hourly:40",
                    "living.witness@Hourly:45",
                    "living.companion_guard@Hourly:42",
                },
                ["World.GuardPursuits"] = new[]
                {
                    "living.witness@Hourly:45",   // arms/refreshes
                    "living.schedule@PerTick:20", // resolves/prunes
                },
                ["World.Stockpiles"] = new[]
                {
                    "world.harvest@Daily:25",
                    "living.eatOnArrival@PerTick:22",
                    "living.consumption@Hourly:35",
                    "living.ambient@Hourly:50",   // vermin theft
                    "econ.trade@Daily:28",
                },
                ["World.Rumors"] = new[] { "living.rumors@Hourly:55" },
                ["World.SiteUnrest"] = new[] { "living.witness@Hourly:45" },
            };
    }
}

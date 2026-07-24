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
                    "living.schedule@PerTick:20",        // NARROWED (W32): actionless actors only
                    "living.action_advance@PerTick:22",  // W32: the active MoveToFood step
                    "living.companion_follow@PerTick:21", // heel AFTER schedule, by design
                    "living.predation@Hourly:40",         // hunters step toward prey
                    "living.witness@Hourly:45",           // civilians shy from trouble + guard nudge
                    "living.ambient@Hourly:50",           // critters only (not actors) - listed for audit
                },
                ["Actor.Needs"] = new[]
                {
                    "living.needs@Hourly:30",           // the ramps
                    "living.action_advance@PerTick:22", // W32: the ConsumeFood commit drops hunger
                    "living.consumption@Hourly:35",     // NARROWED (W32): sleep/fatigue half only
                },
                ["Actor.ActionState"] = new[]
                {
                    "living.decision@PerTick:18",       // W32: intent + action START
                    "living.action_advance@PerTick:22", // W32: phase steps + terminal handover
                },
                ["World.Reservations"] = new[]
                {
                    "living.decision@PerTick:18",       // W32: claim + expiry sweep
                    "living.action_advance@PerTick:22", // W32: consumed/failed release
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
                    "living.action_advance@PerTick:22", // W32: TakeFood decrement + failure return
                    "living.ambient@Hourly:50",   // vermin theft
                    "econ.trade@Daily:28",
                },
                ["World.Rumors"] = new[] { "living.rumors@Hourly:55" },
                ["World.SiteUnrest"] = new[] { "living.witness@Hourly:45" },
            };
    }
}

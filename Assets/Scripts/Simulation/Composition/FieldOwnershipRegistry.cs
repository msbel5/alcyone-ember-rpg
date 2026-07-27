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
                    // W34: living.consumption@Hourly:35 retired — the Sleep recovery ladder now
                    // rides the SAME advance slot the ConsumeFood commit already owned.
                    "living.action_advance@PerTick:22", // W32 ConsumeFood drops hunger; W34 Sleep drops fatigue
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
                    // W36 GUARD+COMBAT: StrikeQuarryAdvancer resolves damage on PerTick under
                    // the action-strip lifecycle. PredationSystem gates on non-None ActionState
                    // to avoid a double-writer race on action-driven hostiles.
                    "living.action_advance@PerTick:22",
                },
                ["World.GuardPursuits"] = new[]
                {
                    "living.witness@Hourly:45",   // arms/refreshes
                    "living.schedule@PerTick:20", // resolves/prunes
                },
                // W36 GUARD+COMBAT (LIVE W39): PursuitRecord's mirror on the enemy side.
                // The two ids below are the ACTUAL registered composer steps (see
                // DefaultTickSystems.DecisionStep + ActionAdvancementStep); the ownership lint
                // resolves each triple against the live registry so a rename here fails CI.
                //   living.decision@PerTick:18       -> ActionLifecycleSystem.Decide:TryDecideHunt
                //                                       arms/refreshes a HuntTargetRecord row.
                //   living.action_advance@PerTick:22 -> StrikeQuarryAdvancer / HuntAdvancer via
                //                                       the ActionAdvancementStep, clears the row
                //                                       on kill/clamp (TTL owns natural expiry).
                ["World.HuntTargets"] = new[]
                {
                    "living.decision@PerTick:18",
                    "living.action_advance@PerTick:22",
                },
                ["World.Stockpiles"] = new[]
                {
                    // W33: world.harvest@Daily:25 retired — stock now lands via HaulCrop deposit.
                    // W34: econ.jobs@Hourly:10 retired — the step no longer touches piles; recipe
                    // input consumption + output mint moved to PerformWork on the advance slot.
                    "living.action_advance@PerTick:22", // W32 TakeFood decrement + failure return; W33 HaulCrop deposit + PlantSeed seed take; W34 PerformWork fund/mint
                    "living.decision@PerTick:18", // W34: orphan work-order refund returns inputs to the site pile (§6.3)
                    "living.ambient@Hourly:50",   // vermin theft
                    "world.caravans@Daily:10", // B03: caravan load/unload (CaravanSystem Remove/Add) — the REAL daily trader; econ.trade was a ghost
                },
                // W34 WORK: the order row's declared writers — World.Reservations' two-slot mirror.
                ["World.WorkOrders"] = new[]
                {
                    "living.decision@PerTick:18",       // orphan sweep + refund (docs/ruh/w34/02 §6.3)
                    "living.action_advance@PerTick:22", // birth / funding / counter / removal (§7.2)
                },
                // W34: World.Jobs' long-standing multi-writer reality finally DECLARED (§9.1).
                ["World.Jobs"] = new[]
                {
                    "econ.jobs@Hourly:10",              // claim / dead-claimant sweep / ghost-cancel
                    "living.action_advance@PerTick:22", // Complete/Cancel — the de-facto writer since W33 PlantSeed
                    "econ.shortage_response@Daily:27",  // posts the shortage cascade's planting jobs
                },
                ["World.Plants"] = new[]
                {
                    "econ.plantgrowth@Daily:20",        // stage advancement
                    "living.action_advance@PerTick:22", // W33: PlantSeed birth + HarvestCrop removal
                },
                ["World.Soils"] = new[]
                {
                    "living.action_advance@PerTick:22", // W33: WithPlant on plant, WithoutPlant on harvest
                },
                ["World.Rumors"] = new[] { "living.rumors@Hourly:55" },
                ["World.SiteUnrest"] = new[] { "living.witness@Hourly:45" },

                // W35 (B04): fields formerly outside ownership. Only writers that resolve
                // to a REAL registered step id go here - the reverse lint refuses the rest.
                // Boot-only + command-driven mutation stays UNDECLARED with a comment; the ledger
                // is a "who writes in the loop" contract, not a where-does-every-byte-live index.
                ["World.Time"] = new[] { "core.time@PerTick:10" },
                // World.Plants declared above (econ.plantgrowth + living.action_advance) — the
                // W35/B04 collection-initializer overwrite would silently shadow the first entry.
                ["Actor.Mood"] = new[]
                {
                    "living.action_advance@PerTick:22", // ConsumeFood/Sleep re-evaluate
                    "living.needs@Hourly:30",
                },
                ["World.NpcMemory"] = new[]
                {
                    "living.witness@Hourly:45",
                    // Command-driven writers (dialog, trade completion, ToolUse) are boundary
                    // writes, not tick systems - lint-inclusion would demand a fake registration.
                },
                ["World.CompanionIds"] = new[] { "living.companion_follow@PerTick:21" },
                ["World.Factions"] = new[] { "politics.faction_decay@Daily:40" },
            };
    }
}

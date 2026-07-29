using System;
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
        // PRD-01 source mutation gate. Identity is repository-relative path + exact line
        // number + trimmed source. This deliberately makes a same-file writer substitution
        // fail instead of being hidden behind an unchanged occurrence count.
        //
        // These are measured debt, not approval of the architecture. Later recovery PRDs
        // remove rows as each direct writer is cut over to the action spine.
        public static readonly IReadOnlyList<string> PositionDebtAllowList = new[]
        {
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Spells.cs", 59, "player.MoveTo(liveCastPosition);"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Spells.cs", 143, "player.MoveTo(CenterOfSite(SettlementSiteId(CurrentSettlementOrStart)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Travel.cs", 57, "player.MoveTo(CenterOfSite(SettlementSiteId(settlement.Id)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 276, "hostile.MoveTo(new GridPosition("),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 295, "hostile.MoveTo(new GridPosition(hostile.Position.X + dx, hostile.Position.Y + dy));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 370, "player.MoveTo(CenterOfSite(SettlementSiteId(here)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 406, "outlaw.MoveTo(new GridPosition(player.Position.X + 1, player.Position.Y)); // the duel is adjacent"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 493, "dummy.MoveTo(new GridPosition(player.Position.X + 1, player.Position.Y));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 665, "host.MoveTo(tavernGrid);"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 682, "player.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(target.Position.X + 1, target.Position.Y));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 1004, "target.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(castFrom.X + 3, castFrom.Y));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 1033, "actor.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(center.X + (posted - 1) * 2, center.Y));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.Npcs.cs", 140, "worker.MoveTo(preferredSmithPosition);"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.Player.cs", 45, "player.MoveTo(CenterOfSite(SettlementSiteId(StartingSettlement)));"),
            Callsite("Assets/Scripts/Simulation/Living/ScheduleSystem.cs", 61, "actor.MoveTo(next);"),
        };

        public static readonly IReadOnlyList<string> PositionActionSpineCallsites = new[]
        {
            Callsite("Assets/Scripts/Simulation/Living/Actions/HaulCropAdvancer.cs", 64, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/FollowPlayerAdvancer.cs", 48, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/HuntAdvancer.cs", 61, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/MoveToBedAdvancer.cs", 50, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/MoveToFoodAdvancer.cs", 46, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/MoveToPlotAdvancer.cs", 79, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/MoveToWorksiteAdvancer.cs", 55, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/OnWatchAdvancer.cs", 57, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/PursueAdvancer.cs", 57, "actor.MoveTo(movement.Position);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/ReportCrimeAdvancer.cs", 76, "actor.MoveTo(movement.Position);"),
        };

        public static readonly IReadOnlyList<string> AllowedMoveToCallsites =
            Merge(PositionDebtAllowList, PositionActionSpineCallsites);

        public static readonly IReadOnlyList<string> AllowedApplyVitalsCallsites = new[]
        {
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.cs", 36, "player.ApplyVitals(player.Vitals.WithHealth(player.Vitals.Health.Damage(amount)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Melee.cs", 170, "target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(rawDamage)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Travel.cs", 99, "player.ApplyVitals(EmberCrpg.Simulation.World.PlayerRestService.RestedVitals(player.Vitals, hoursSlept));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 362, "player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals("),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 503, "dummy.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals("),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 606, "player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals("),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 630, "player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals("),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 991, "player.ApplyVitals(player.Vitals.WithMana(new VitalStat(40, 40)));"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldEncounter.cs", 1005, "target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Restore(target.Vitals.Health.Max)));"),
            Callsite("Assets/Scripts/Simulation/Combat/CombatActionResolver.cs", 61, "attacker.ApplyVitals(attacker.Vitals.WithFatigue(attacker.Vitals.Fatigue.Damage(action.StaminaCost)));"),
            Callsite("Assets/Scripts/Simulation/Combat/CombatActionResolver.cs", 72, "defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(damage)));"),
            Callsite("Assets/Scripts/Simulation/Combat/CombatMathService.cs", 47, "defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(mitigatedDamage)));"),
            Callsite("Assets/Scripts/Simulation/Combat/RealtimeDamageService.cs", 64, "defender.ApplyVitals(defender.Vitals.WithHealth(defender.Vitals.Health.Damage(mitigatedDamage)));"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/CombatOperations.cs", 75, "target.ApplyVitals(new ActorVitals("),
            Callsite("Assets/Scripts/Simulation/Magic/SpellCastingService.cs", 91, "caster.ApplyVitals(caster.Vitals.WithMana(caster.Vitals.Mana.Damage(spell.ManaCost)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 73, "target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 78, "target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Restore(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 84, "target.ApplyVitals(target.Vitals.WithFatigue(target.Vitals.Fatigue.Restore(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 90, "target.ApplyVitals(target.Vitals.WithMana(target.Vitals.Mana.Restore(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 96, "target.ApplyVitals(target.Vitals.WithMana(target.Vitals.Mana.Damage(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs", 102, "target.ApplyVitals(target.Vitals.WithFatigue(target.Vitals.Fatigue.Damage(effect.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellResolver.cs", 112, "context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellResolver.cs", 118, "context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Restore(operation.Magnitude)));"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellResolver.cs", 132, "context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));"),
        };

        public static readonly IReadOnlyList<string> AllowedApplyNeedsCallsites = new[]
        {
            Callsite("Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs", 406, "actor.ApplyNeeds(_needs.TickNeeds(actor.Role, actor.Needs));"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/ConsumeFoodAdvancer.cs", 54, "actor.ApplyNeeds(fed);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/SleepAdvancer.cs", 66, "actor.ApplyNeeds(rested);"),
            Callsite("Assets/Scripts/Simulation/Living/NeedRecoverySystem.cs", 92, "actor.ApplyNeeds(nextNeeds);"),
            Callsite("Assets/Scripts/Simulation/Living/NeedsSystem.cs", 107, "actor.ApplyNeeds(nextNeeds);"),
        };

        public const string ActionStateAuthoritative = "authoritative-transition";
        public const string ActionStateCopyOrRestore = "copy-or-restore";
        public const string ActionStateInitialization = "initialization";
        public const string ActionStateMutatorBody = "mutator-body";

        // Classification is over the complete concrete source inventory: invocation sites
        // and direct assignments. The test derives the one-writer count from these values.
        public static readonly IReadOnlyDictionary<string, string> ActionStateCallsiteClassifications =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Callsite("Assets/Scripts/Data/Save/ActorSaveMapper.cs", 171, "record.ApplyActionState(ActorActionStateSaveReader.Read(save));")] = ActionStateCopyOrRestore,
                [Callsite("Assets/Scripts/Domain/Actors/ActorRecord.cs", 60, "ActionState = actionState;")] = ActionStateInitialization,
                [Callsite("Assets/Scripts/Domain/Actors/ActorRecord.cs", 119, "copy.ApplyActionState(ActionState);")] = ActionStateCopyOrRestore,
                [Callsite("Assets/Scripts/Domain/Actors/ActorRecord.cs", 178, "ActionState = actionState;")] = ActionStateMutatorBody,
                [Callsite("Assets/Scripts/Simulation/Living/Actions/ActionAdvancer.cs", 55, "actor.ApplyActionState(next);")] = ActionStateAuthoritative,
                [Callsite("Assets/Scripts/Simulation/World/PlayerLevelUpService.cs", 152, "copy.ApplyActionState(source.ActionState);")] = ActionStateCopyOrRestore,
            };

        public static readonly IReadOnlyList<string> AllowedStockpileMutationCallsites = new[]
        {
            Callsite("Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.Economy.cs", 72, "stockpile.Add(entry.itemTag, entry.count);"),
            Callsite("Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.Economy.cs", 74, "stockpiles.Add(stockpile);"),
            Callsite("Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.cs", 198, "world.Stockpiles = ToStockpiles(data.stockpiles);"),
            Callsite("Assets/Scripts/Domain/Process/StockpileRecipeInventory.cs", 25, "return _pile.Remove(itemTag, quantity) == quantity;"),
            Callsite("Assets/Scripts/Domain/Process/StockpileRecipeInventory.cs", 33, "_pile.Add(itemTag, quantity);"),
            Callsite("Assets/Scripts/Domain/World/WorldState.cs", 46, "public List<StockpileComponent> Stockpiles = new List<StockpileComponent>();"),
            Callsite("Assets/Scripts/Domain/World/WorldState.cs", 85, "Stockpiles ??= new List<StockpileComponent>();"),
            Callsite("Assets/Scripts/Domain/World/WorldState.cs", 283, "Stockpiles = other.Stockpiles;"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.Hydration.cs", 153, "larder.Add(\"wheat\", 150);"),
            Callsite("Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Worldgen.Hydration.cs", 154, "_world.Stockpiles.Add(larder);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/ActionAdvancer.cs", 99, "FoodOperations.FindPile(world, row.SiteId)?.Add(row.ItemTag, 1);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/ActionAdvancer.cs", 141, "pile.Add(itemTag, state.CarriedUnits);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/ActionLifecycleSystem.cs", 317, "pile.Add(input.ItemTag, input.Quantity);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/FarmOperations.cs", 124, "world.Stockpiles.Add(pile);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/HaulCropAdvancer.cs", 76, "pile.Add(cropTag, state.CarriedUnits);"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/PlantSeedAdvancer.cs", 77, "FarmOperations.PlantIdFor(soil.Id), () => pile.Remove(seedTag, 1) == 1,"),
            Callsite("Assets/Scripts/Simulation/Living/Actions/TakeFoodAdvancer.cs", 42, "pile.Remove(row.ItemTag, 1); // unit is physically in hand now (all-or-nothing)"),
            Callsite("Assets/Scripts/Simulation/Living/AmbientLifeSystem.cs", 52, "var removed = pile.Remove(tag, 1);"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellResolver.cs", 129, "context.TerrainStockpile.Remove(requiredTag, 1);"),
            Callsite("Assets/Scripts/Simulation/Magic/SpellResolver.cs", 130, "context.TerrainStockpile.Add(context.ResultTerrainTag, 1);"),
            Callsite("Assets/Scripts/Simulation/World/CaravanSystem.cs", 50, "var loaded = origin?.Remove(route.ItemTag, route.QuantityPerCaravan) ?? 0;"),
            Callsite("Assets/Scripts/Simulation/World/CaravanSystem.cs", 92, "destination.Add(route.ItemTag, caravan.PayloadRemaining);"),
            Callsite("Assets/Scripts/Simulation/World/RuntimeHistorySystem.cs", 103, "destination.Add(\"wheat\", quantity);"),
            Callsite("Assets/Scripts/Simulation/World/WorldFactory.cs", 129, "furnaceStock.Add(\"iron\", 8);"),
            Callsite("Assets/Scripts/Simulation/World/WorldFactory.cs", 131, "stallStock.Add(\"coin\", 100);"),
            Callsite("Assets/Scripts/Simulation/World/WorldFactory.cs", 135, "stallStock.Add(\"wheat\", 320) /* PLAYTEST maul-survivors eat too: 13 mouths x 5 gate days */;"),
            Callsite("Assets/Scripts/Simulation/World/WorldFactory.cs", 136, "world.Stockpiles.Add(furnaceStock);"),
            Callsite("Assets/Scripts/Simulation/World/WorldFactory.cs", 137, "world.Stockpiles.Add(stallStock);"),
        };

        /// <summary>field -> declared writers as "systemId@Cadence:Order".</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Writers =
            new Dictionary<string, string[]>
            {
                ["Actor.Position"] = new[]
                {
                    "living.schedule@PerTick:20",        // NARROWED (W32): actionless actors only
                    "living.action_advance@PerTick:22",  // W32: the active MoveToFood step
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
                    // W36 GUARD+COMBAT: StrikeQuarryAdvancer resolves damage on PerTick under
                    // the action-strip lifecycle. PredationSystem gates on non-None ActionState
                    // to avoid a double-writer race on action-driven hostiles.
                    "living.action_advance@PerTick:22",
                },
                ["World.GuardPursuits"] = new[]
                {
                    "living.decision@PerTick:18",       // validates/prunes and starts Pursue
                    "living.action_advance@PerTick:22", // ReportCrime arms; pursuit terminal paths clear
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
                    "living.action_advance@PerTick:22",
                    // Command-driven writers (dialog, trade completion, ToolUse) are boundary
                    // writes, not tick systems - lint-inclusion would demand a fake registration.
                },
                ["World.CompanionIds"] = new[] { "living.decision@PerTick:18" },
                ["World.Factions"] = new[] { "politics.faction_decay@Daily:40" },
            };

        private static string Callsite(string path, int line, string source)
            => $"{path}:{line}::{source}";

        private static IReadOnlyList<string> Merge(
            IReadOnlyList<string> first,
            IReadOnlyList<string> second)
        {
            var merged = new List<string>(first.Count + second.Count);
            for (var i = 0; i < first.Count; i++) merged.Add(first[i]);
            for (var i = 0; i < second.Count; i++) merged.Add(second[i]);
            return merged.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Rng;

// Design note:
// WorldgenService is the FOUNDATION's deterministic seed-to-world function.
// Inputs: a uint seed and a WorldgenParameters knob set.
// Outputs: a GeneratedWorld bundle (regions, settlements, factions, faction
// relations, NPCs, 100-year history) shaped to the brief's Daggerfall-style
// targets — ~50 regions, ~200 settlements (1 capital + a few cities + dozens
// of towns + hundreds of villages), ~20 factions, ~750 NPCs, total
// population in [900K, 1.1M], 100-year history of macro events.
//
// Determinism contract: the same (seed, parameters) pair produces a
// byte-identical GeneratedWorld. The implementation uses ONE XorShiftRng
// drawn through a strict call order (regions → settlements → factions →
// relations → npcs → history) so the deterministic-replay test can pin
// the first NPC's name and the history-event count to a fixed expectation.
// No Unity, no I/O, no LINQ in the hot path (HashSet membership only).
//
// Population math: 1 Capital (~150K avg) + 8 Cities (~75K avg × 8 = 600K)
// + 40 Towns (~6K avg × 40 = 240K) + 151 Villages (~575 avg × 151 = ~87K)
// ≈ 1.077M expected total — comfortably inside [900K, 1.1M].
namespace EmberCrpg.Simulation.Worldgen
{
    /// <summary>Pure deterministic worldgen entry point.</summary>
    public static partial class WorldgenService
    {
        // Region name decorators. The brief specifies bag-of-syllables for
        // names; these suffixes turn raw syllable words into region-flavored
        // labels ("Brytharlm Vale", "Vhinaurd Marches") without dragging a
        // full word-list in.
        private static readonly string[] RegionSuffixes =
        {
            "Vale", "Reach", "Marches", "Wilds", "Holds", "Steppe", "Coast", "Lowlands",
        };

        // Settlement name decorators. Capitals/Cities get the bare forged
        // word; towns/villages get a small qualifier so saying "Vhinaurd"
        // (a city) does not look identical to "Vhinaurd" (a village).
        private static readonly string[] SettlementSuffixes =
        {
            "ford", "haven", "vale", "stead", "wick", "hollow", "bridge", "cross",
        };

        // Faction names get a short noun anchor so they read like factions
        // and not like settlements. House X / Order of X / Y Pact / etc.
        private static readonly string[] FactionPrefixes =
        {
            "House", "Order", "Circle", "League", "Pact", "Hand",
        };

        public static GeneratedWorld Generate(uint seed, WorldgenParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var rng = new XorShiftRng(seed);

            // -- 1. Regions ---------------------------------------------------
            var regions = GenerateRegions(rng, parameters);

            // -- 2. Settlements ----------------------------------------------
            var settlements = GenerateSettlements(rng, parameters, regions);

            // -- 3. Factions -------------------------------------------------
            var factions = GenerateFactions(rng, parameters);

            // -- 4. Faction relations ----------------------------------------
            var relations = GenerateFactionRelations(rng, factions);

            // -- 5. NPCs -----------------------------------------------------
            var npcs = GenerateNpcs(rng, parameters, settlements, factions);

            // -- 6. History --------------------------------------------------
            var history = GenerateHistory(rng, parameters, factions, settlements);

            return new GeneratedWorld(seed, regions, settlements, factions, relations, npcs, history);
        }

    }
}

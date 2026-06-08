using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        private static RegionId SelectStartingRegion(
            EmberCrpg.Simulation.Worldgen.GeneratedWorld generated,
            string startLocation)
        {
            if (generated.Regions.Count == 0)
                return default;
            if (!string.IsNullOrWhiteSpace(startLocation))
            {
                for (int i = 0; i < generated.Regions.Count; i++)
                {
                    var r = generated.Regions[i];
                    if (r.Name.IndexOf(startLocation, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return r.Id;
                }
            }
            return generated.Regions[0].Id;
        }

        private static SettlementId SelectStartingSettlement(
            EmberCrpg.Simulation.Worldgen.GeneratedWorld generated,
            SettlementSize preferredSize,
            string startLocation)
        {
            if (!string.IsNullOrWhiteSpace(startLocation))
            {
                foreach (var settlement in generated.Settlements)
                {
                    if (settlement.Name.IndexOf(startLocation, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return settlement.Id;
                }
            }

            foreach (var settlement in generated.Settlements)
            {
                if (settlement.Size == preferredSize)
                    return settlement.Id;
            }
            return generated.Settlements.Count == 0 ? default : generated.Settlements[0].Id;
        }

        private static FactionId SelectStartingFaction(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated, string calling)
        {
            if (generated.Factions.Count == 0) return default;
            string normalized = (calling ?? string.Empty).Trim().ToLowerInvariant();
            for (int i = 0; i < generated.Factions.Count; i++)
            {
                var name = generated.Factions[i].Name.ToLowerInvariant();
                if ((normalized.Contains("mage") || normalized.Contains("scholar")) && (name.Contains("order") || name.Contains("circle")))
                    return generated.Factions[i].Id;
                if ((normalized.Contains("merchant") || normalized.Contains("trader") || normalized.Contains("smith")) && name.Contains("league"))
                    return generated.Factions[i].Id;
                if ((normalized.Contains("war") || normalized.Contains("guard") || normalized.Contains("soldier")) && (name.Contains("house") || name.Contains("pact")))
                    return generated.Factions[i].Id;
            }

            uint hash = FoldSeed(calling, string.Empty, string.Empty);
            return generated.Factions[(int)(hash % (uint)generated.Factions.Count)].Id;
        }

        private static WorldStyle ParseStyle(string mood)
        {
            return WorldGenesisMapper.ToStyle(mood);
        }

        private static WorldGenre ParseGenre(string mood, string calling, string startLocation)
        {
            return WorldGenesisMapper.ToGenre(mood, calling, startLocation);
        }

        private static SettlementSize ParsePreferredSettlementSize(string startLocation)
        {
            return WorldGenesisMapper.ToPreferredSettlementSize(startLocation);
        }

        private static uint FoldSeed(string mood, string calling, string startLocation)
        {
            // FNV-1a-32 over the three strings concatenated with a unit
            // separator so "ab|c" and "a|bc" do not fold to the same seed.
            const uint Prime = 16777619u;
            uint hash = 2166136261u;
            FoldString(ref hash, mood, Prime);
            FoldString(ref hash, "", Prime);
            FoldString(ref hash, calling, Prime);
            FoldString(ref hash, "", Prime);
            FoldString(ref hash, startLocation, Prime);
            // Avoid the XorShiftRng zero-seed reroute by nudging the result
            // when it lands on 0 — preserves determinism (the same inputs
            // still fold to the same seed) without losing entropy.
            if (hash == 0u) hash = 2463534242u;
            return hash;
        }

        private static void FoldString(ref uint hash, string s, uint prime)
        {
            if (s == null) return;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= prime;
            }
        }
    }
}

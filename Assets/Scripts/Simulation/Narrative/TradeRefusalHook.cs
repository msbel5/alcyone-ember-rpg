using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Memory;

namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>
    /// Decides whether a trade should be refused based on remembered crime
    /// facts and current faction relation. Faz 9 Atom 11.
    /// </summary>
    public sealed class TradeRefusalHook
    {
        private readonly MemoryRecallService _recall;

        public TradeRefusalHook(MemoryRecallService recall)
        {
            _recall = recall ?? throw new ArgumentNullException(nameof(recall));
        }

        public bool ShouldRefuse(
            MemoryComponent sellerMemory,
            FactionId sellerFaction,
            FactionId buyerFaction,
            FactionStore factions,
            GameTime now,
            GameTime crimeMemoryHorizon,
            out string reason)
        {
            reason = string.Empty;

            if (factions != null && !sellerFaction.IsEmpty && !buyerFaction.IsEmpty)
            {
                var relation = factions.GetReputation(sellerFaction, buyerFaction).ToRelationKind();
                if (relation.Equals(FactionRelationKind.War))
                {
                    reason = "faction_war";
                    return true;
                }
                if (relation.Equals(FactionRelationKind.Hostile))
                {
                    reason = "faction_hostile";
                    return true;
                }
            }

            // PR#171 bot review fix: bound the recent-crime check to facts at
            // or before `now`. Previously a future-dated crime fact (e.g. from
            // a drifted clock across save/load) would falsely trigger refusal.
            if (sellerMemory != null && _recall.HasRecentFact(sellerMemory, new TopicId("crime"), crimeMemoryHorizon, now))
            {
                reason = "memory_recent_crime";
                return true;
            }

            return false;
        }
    }
}

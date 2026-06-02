using System;
using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    public sealed class FactionReputationDecaySystem
    {
        private readonly IFactionDecayEventPolicy _eventPolicy;

        public FactionReputationDecaySystem()
            : this(new MeaningfulFactionDecayEventPolicy())
        {
        }

        public FactionReputationDecaySystem(IFactionDecayEventPolicy eventPolicy)
        {
            _eventPolicy = eventPolicy ?? throw new ArgumentNullException(nameof(eventPolicy));
        }

        public void Apply(
            FactionStore factions,
            FactionDecayConfig config,
            GameTime stamp,
            WorldEventLog events)
        {
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (config.RatePerStep == 0) return;

            var rows = factions.ReputationRows.ToArray();
            foreach (var row in rows)
            {
                if (Math.Abs(row.Reputation.Value - config.Baseline) <= config.DeadBand)
                    continue;

                var next = row.Reputation.Decay(config.RatePerStep);
                if (next.Equals(row.Reputation))
                    continue;

                factions.WithReputation(row.A, row.B, next);
                if (!_eventPolicy.ShouldEmit(row.Reputation, next, config))
                    continue;

                events.Append(new WorldEvent(
                    stamp,
                    WorldEventKind.FactionReputationChanged,
                    default,
                    new SiteId(row.A.Value),
                    BuildReason(row.A, row.B, row.Reputation.Value, next.Value)));
            }
        }

        private static string BuildReason(FactionId a, FactionId b, int before, int after)
        {
            return $"faction_reputation a:{a} b:{b} from:{before} to:{after} reason:decay";
        }
    }
}

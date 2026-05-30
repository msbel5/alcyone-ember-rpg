using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Applies typed reputation deltas between two factions, persists them
    /// through <see cref="FactionStore"/>, and emits a
    /// <see cref="WorldEventKind.FactionReputationChanged"/> event for replay.
    /// Phase 6 Atom 4.
    /// </summary>
    public sealed class FactionReputationSystem
    {
        public void ApplyDelta(
            FactionStore factions,
            FactionId a,
            FactionId b,
            int delta,
            string reasonCode,
            GameTime now,
            WorldEventLog events)
        {
            if (factions == null) throw new ArgumentNullException(nameof(factions));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (a.IsEmpty || b.IsEmpty || a.Equals(b))
                return;
            if (delta == 0)
                return;

            var before = factions.GetReputation(a, b);
            var after = before.Apply(delta);
            if (after.Equals(before))
                return;

            factions.WithReputation(a, b, after);

            // WorldEvent requires at least one non-empty (actorId, siteId). Reputation
            // shifts are between factions and don't always have an actor or site
            // context; we encode FactionId-A as the SiteId sentinel so the event log
            // shape stays consistent without inventing a fake actor.
            events.Append(new WorldEvent(
                now,
                WorldEventKind.FactionReputationChanged,
                default,
                new SiteId(a.Value),
                BuildReason(a, b, before.Value, after.Value, reasonCode)));
        }

        private static string BuildReason(FactionId a, FactionId b, int before, int after, string reasonCode)
        {
            var code = string.IsNullOrWhiteSpace(reasonCode) ? "unspecified" : reasonCode.Trim().ToLowerInvariant();
            return $"faction_reputation a:{a} b:{b} from:{before} to:{after} reason:{code}";
        }
    }
}

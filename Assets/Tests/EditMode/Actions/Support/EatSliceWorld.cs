using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Tests.EditMode.Actions.Support
{
    /// <summary>
    /// W32 DOC6 §0: the ONE world-building path for the EAT story tests — the EatOnArrival
    /// fixture carried over verbatim: Site(1) spans (0,0)-(10,10) so its centre is (5,5),
    /// the wheat pile sits on that site, and a hungry diner starts at hunger 80 (threshold 55).
    /// </summary>
    internal static class EatSliceWorld
    {
        public static WorldState Build(int wheat = 10)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(60);
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10))); // centre (5,5)
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", wheat);
            world.Stockpiles.Add(pile);
            return world;
        }

        public static ActorRecord Hungry(ulong id, int x, int y)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Diner" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(80)));
            return actor;
        }

        /// <summary>Meals as TERMINAL ACTION OUTCOMES (the strengthened Gate counter), not reason-string grep.</summary>
        public static int Meals(WorldState world) =>
            world.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted);
    }
}

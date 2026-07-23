using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// Review-mandated coverage: the runtime chronicle's determinism and the event→relation
    /// drift were pinned only by the 31-day integration gates; these unit tests pin the exact
    /// per-call contracts cheaply.
    /// </summary>
    public sealed class RuntimeHistorySystemTests
    {
        private static WorldState World(int roomSeed)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.RoomSeed = roomSeed;
            world.Factions.Add(new FactionRecord(new FactionId(1), "Forge", new[] { RuntimeHistorySystem.CraftTag }));
            world.Factions.Add(new FactionRecord(new FactionId(2), "Harbor", new[] { RuntimeHistorySystem.TradeTag }));
            world.Factions.Add(new FactionRecord(new FactionId(3), "Watch", new[] { RuntimeHistorySystem.LawTag }));
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(4, 4)));
            return world;
        }

        private static GameTime Day(int dayIndex) => new GameTime(dayIndex * GameTime.MinutesPerDay);

        [Test]
        public void Tick_SameSeedSameMonthBoundary_WritesIdenticalChronicles()
        {
            var a = World(4242);
            var b = World(4242);
            var system = new RuntimeHistorySystem();

            system.Tick(a, Day(30));
            system.Tick(b, Day(30));

            string ChronicleOf(WorldState w) => string.Join("|", w.Events.Events
                .Where(e => e.Kind == WorldEventKind.ChronicleEvent).Select(e => e.Reason));
            Assert.That(ChronicleOf(a), Is.Not.Empty, "month boundary must write a chronicle entry");
            Assert.That(ChronicleOf(a), Is.EqualTo(ChronicleOf(b)), "same seed, same history — determinism");
        }

        [Test]
        public void Tick_OffMonthBoundary_WritesNoChronicle()
        {
            var world = World(4242);
            new RuntimeHistorySystem().Tick(world, Day(17));

            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ChronicleEvent), Is.False);
        }

        [Test]
        public void Tick_MonthEnd_DepletedSettlementReceivesMigrants()
        {
            // PLAYTEST FIX: towns must REFILL — month's end brings migrants to any settlement
            // whose living civilians fell below the floor.
            var world = World(4242);
            var lone = new EmberCrpg.Simulation.World.WorldActorLoadoutFactory()
                .Create(new EmberCrpg.Domain.Core.ActorId(50), "Last One",
                    EmberCrpg.Domain.Actors.ActorRole.Talker, new GridPosition(2, 2));
            world.Actors.Add(lone);
            int before = world.Actors.Records.Count(a => a != null && a.IsAlive);

            new RuntimeHistorySystem().Tick(world, Day(30));

            int after = world.Actors.Records.Count(a => a != null && a.IsAlive);
            Assert.That(after, Is.GreaterThan(before), "no migrants arrived at a nearly-empty town");
            Assert.That(world.Events.Events.Any(e =>
                e.Kind == WorldEventKind.ActorSpawned && e.Reason.StartsWith("migrant_arrived")), Is.True);
        }

        [Test]
        public void Tick_GuardRespondedYesterday_RaisesTheWatchStanding()
        {
            var world = World(4242);
            var stamp = Day(1);
            world.Events.Append(new WorldEvent(new GameTime(stamp.TotalMinutes - 30),
                WorldEventKind.GuardResponded, default, new SiteId(1), "guard_strikes_hunter target:9"));
            int before = world.Factions.GetReputation(new FactionId(3), new FactionId(1)).Value;

            new RuntimeHistorySystem().Tick(world, stamp);

            Assert.That(world.Factions.GetReputation(new FactionId(3), new FactionId(1)).Value,
                Is.EqualTo(before + RuntimeHistorySystem.GuardRenownDelta),
                "the watch answering trouble must drift law↔craft standing upward");
        }
    }
}

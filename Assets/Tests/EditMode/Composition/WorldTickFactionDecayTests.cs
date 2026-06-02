using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    public sealed class WorldTickFactionDecayTests
    {
        private static int FifteenDays => 15 * WorldTickComposer.TicksPerGameDay;

        [Test]
        public void Advance_DailyTick_DecaysSeededFactionReputationsToNeutral()
        {
            var world = new WorldFactory().Create(roomSeed: 1);
            var pairCount = world.Factions.ReputationRows.Count();
            RunTicks(world, new WorldTickComposer(), FifteenDays);

            Assert.That(world.Factions.ReputationRows.Select(r => r.Reputation.Value), Is.All.EqualTo(0));
            Assert.That(DecayEvents(world).Count(), Is.LessThanOrEqualTo(pairCount));
            Assert.That(DecayEvents(world).Count(), Is.LessThan(pairCount * 15));
        }

        [Test]
        public void Advance_ActiveDeltaBeforeDailyDecay_ComposesDeltaThenDecay()
        {
            var world = new WorldFactory().Create(roomSeed: 1);
            var row = world.Factions.ReputationRows.First();
            new FactionReputationSystem().ApplyDelta(
                world.Factions,
                row.A,
                row.B,
                +30,
                "test_delta",
                world.Time,
                world.Events);

            RunTicks(world, new WorldTickComposer(), WorldTickComposer.TicksPerGameDay);

            Assert.That(world.Factions.GetReputation(row.A, row.B).Value, Is.EqualTo(row.Reputation.Value + 29));
        }

        [Test]
        public void Advance_MultiDayCatchup_EqualsDayByDay()
        {
            var dayByDay = new WorldFactory().Create(roomSeed: 1);
            var composerA = new WorldTickComposer();
            composerA.Advance(dayByDay, 0);
            for (int day = 1; day <= 5; day++)
                composerA.Advance(dayByDay, day * WorldTickComposer.TicksPerGameDay);

            var catchup = new WorldFactory().Create(roomSeed: 1);
            var composerB = new WorldTickComposer();
            composerB.Advance(catchup, 0);
            composerB.Advance(catchup, 5 * WorldTickComposer.TicksPerGameDay);

            Assert.That(ReputationSnapshot(catchup), Is.EqualTo(ReputationSnapshot(dayByDay)));
        }

        [Test]
        public void Advance_SaveLoadReplay_PreservesDecayCadence()
        {
            int saveTick = WorldTickComposer.TicksPerGameDay + 13;
            int finalTick = 5 * WorldTickComposer.TicksPerGameDay;

            var continuous = new WorldFactory().Create(roomSeed: 1);
            RunTicks(continuous, new WorldTickComposer(), finalTick);

            var cold = new WorldFactory().Create(roomSeed: 1);
            var pre = new WorldTickComposer();
            RunTicks(cold, pre, saveTick);
            var service = new JsonSliceSaveService();
            cold = service.LoadFromJson(service.SaveToJson(cold));

            var post = new WorldTickComposer();
            post.RebuildAccumulatorsFrom(cold.Time);
            post.Advance(cold, saveTick);
            for (int tick = saveTick + 1; tick <= finalTick; tick++)
                post.Advance(cold, tick);

            Assert.That(ReputationSnapshot(cold), Is.EqualTo(ReputationSnapshot(continuous)));
            Assert.That(DecayStampSnapshot(cold), Is.EqualTo(DecayStampSnapshot(continuous)));
        }

        private static void RunTicks(WorldState world, WorldTickComposer composer, int finalTick)
        {
            composer.Advance(world, 0);
            for (int tick = 1; tick <= finalTick; tick++)
                composer.Advance(world, tick);
        }

        private static string ReputationSnapshot(WorldState world)
        {
            return string.Join("|", world.Factions.ReputationRows.Select(r => $"{r.A.Value}:{r.B.Value}:{r.Reputation.Value}"));
        }

        private static string DecayStampSnapshot(WorldState world)
        {
            return string.Join("|", DecayEvents(world).Select(e => $"{e.Tick.TotalMinutes}:{e.Reason}"));
        }

        private static System.Collections.Generic.IEnumerable<WorldEvent> DecayEvents(WorldState world)
        {
            return world.Events.Events.Where(e =>
                e.Kind == WorldEventKind.FactionReputationChanged &&
                e.Reason.Contains("reason:decay"));
        }
    }
}

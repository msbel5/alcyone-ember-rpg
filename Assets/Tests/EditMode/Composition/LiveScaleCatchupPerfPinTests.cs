using System.Diagnostics;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>TICKPERF pin at LIVE scale: the factory-world pin (5 actors) never saw the
    /// actors x piles x sites blow-up that froze the marathon (EatOnArrival 152s per replayed
    /// day, tripling daily). This world is ~40 sites / 40 piles / 800 civilians - one replayed
    /// game day must stay comfortably interactive.</summary>
    public sealed class LiveScaleCatchupPerfPinTests
    {
        [Test]
        public void OneReplayedDay_AtLiveScale_StaysUnderThreeSeconds()
        {
            var world = new WorldFactory().Create(roomSeed: 4242);
            ulong nextId = 50000;
            for (int s = 0; s < 40; s++)
            {
                int bx = s * 200;
                world.Sites.Add(new SiteRecord(new SiteId((ulong)(900 + s)), SiteKind.Settlement,
                    "PerfTown" + s, new GridPosition(bx, 0), new GridPosition(bx + 30, 30)));
                var pile = new StockpileComponent(new SiteId((ulong)(900 + s)));
                pile.Add("grain", 500);
                world.Stockpiles.Add(pile);
                for (int a = 0; a < 20; a++)
                {
                    var actor = new ActorRecord(
                        new ActorId(nextId++), "Perf" + s + "_" + a, ActorRole.Talker,
                        new EmberStatBlock(10, 10, 10, 10, 10, 10),
                        new ActorVitals(new VitalStat(30, 30), new VitalStat(10, 10), new VitalStat(10, 10)),
                        new GridPosition(bx + 5 + (a % 20), 5 + (a / 20)),
                        accuracy: 50, dodge: 10, armor: 0, baseDamage: 2);
                    actor.ApplyNeeds(ActorNeeds.Comfortable.WithHunger(new NeedValue(95)));
                    world.Actors.Add(actor);
                }
            }

            var composer = new WorldTickComposer();
            composer.Advance(world, 10); // warm-up

            var watch = Stopwatch.StartNew();
            composer.Advance(world, 10 + 1440);
            watch.Stop();
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(3000),
                "one live-scale replayed day took " + watch.ElapsedMilliseconds
                + " ms - the per-tick food path regressed to actors x piles x sites again");
        }
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// B21 story pin: the RumorMill's seq cursor SURVIVES a WorldEventLog trim without either
    /// re-milling dropped events (the double-talk failure) or skipping the fresh events pushed
    /// after the trim (the guess-labels failure). The path AVOIDS the dropped band.
    /// </summary>
    public sealed class RumorMillCursorTrimTests
    {
        private static WorldState World()
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10)));
            return world;
        }

        /// <summary>
        /// Seed 300 rumorable events (each Distills to a rat line), mill once so the cursor advances
        /// past all of them, trim the log down to 16, append 5 NEW rumorable events, mill again -
        /// only the 5 new rows produce rumors (born counter is exactly 5).
        /// </summary>
        [Test]
        public void RumorMill_Cursor_SurvivesTrim_AndOnlyMillsFreshEvents()
        {
            var world = World();
            var mill = new RumorMillSystem();
            // Seed 300 rumorable events so their birth-order can be replayed one row at a time.
            for (int i = 0; i < 300; i++)
            {
                world.Events.Append(new WorldEvent(new GameTime(60 + i),
                    WorldEventKind.VerminTheft, default, new SiteId(1),
                    "vermin_theft item:wheat critter:" + i));
            }

            // Mill catches up: the cursor lands at TotalAppended=300. ScanCap caps born<=256.
            int firstBorn = mill.Tick(world, new GameTime(500));
            Assert.That(world.RumorEventCursorSeq, Is.EqualTo(300L),
                "cursor persists as seq at TotalAppended after the first tick");
            Assert.That(firstBorn, Is.LessThanOrEqualTo(256),
                "backfill cap is respected on the first pass");

            // Trim to 16 rows. FirstRetainedSeq advances to 300-16 = 284; TotalAppended stays 300.
            int dropped = world.Events.TrimOldest(maxRetained: 16);
            Assert.That(dropped, Is.EqualTo(284));
            Assert.That(world.Events.FirstRetainedSeq, Is.EqualTo(284L));
            Assert.That(world.Events.TotalAppended, Is.EqualTo(300L));

            // Push 5 NEW rumorable events (seqs 300..304 in the retained window).
            for (int i = 0; i < 5; i++)
            {
                world.Events.Append(new WorldEvent(new GameTime(1000 + i),
                    WorldEventKind.VerminTheft, default, new SiteId(1),
                    "vermin_theft item:wheat critter:new" + i));
            }
            Assert.That(world.Events.TotalAppended, Is.EqualTo(305L));

            // Second mill: the seq cursor sits at 300; only 5 fresh rows are unconsumed.
            // (Rumor list is capped at MaxRumors=32 so total .Count is a poor witness — the
            // return value is the born-this-tick counter and IS the trim-tolerance oracle.)
            int secondBorn = mill.Tick(world, new GameTime(1010));
            Assert.That(secondBorn, Is.EqualTo(5),
                "only the 5 post-trim rumorable events are milled - no dropped row is re-milled");
            Assert.That(world.RumorEventCursorSeq, Is.EqualTo(305L),
                "cursor advances to TotalAppended, staying trim-tolerant");
        }
    }
}

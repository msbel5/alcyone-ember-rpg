using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// DET-01: save/load must be replay-equivalent. The tick composer's hourly/daily accumulators are
    /// in-memory and 0 on a fresh process; restoring with <c>ResetAnchor()</c> alone left them at 0,
    /// desyncing the post-load needs/caravan cadence vs a continuous run. The fix restores with
    /// <c>RebuildAccumulatorsFrom(world.Time)</c>, which re-derives the cadence phase from the restored
    /// time. These tests prove the fix reproduces a continuous run AND that the old path did not —
    /// and they double as a regression guard on the absolute-time alignment the rebuild assumes
    /// (the playable world starts at 8:00 = 480 min, an hour-aligned timestamp).
    /// </summary>
    public sealed class WorldTickComposerReplayTests
    {
        private static int SaveTick => WorldTickComposer.TicksPerGameHour + 13;
        private static int FinalTick => 2 * WorldTickComposer.TicksPerGameHour + 5;

        private static WorldState World() => new WorldFactory().Create(roomSeed: 1);

        // Distinct, ordered timestamps at which the composer emitted hourly needs ticks.
        private static long[] NeedStamps(WorldState w) =>
            w.Events.Events
                .Where(e => e.Kind == WorldEventKind.NeedChanged)
                .Select(e => e.Tick.TotalMinutes)
                .Distinct()
                .OrderBy(m => m)
                .ToArray();

        private static void Advance(WorldTickComposer c, WorldState w, int from, int to)
        {
            for (int t = from; t <= to; t++) c.Advance(w, t);
        }

        private static long[] Continuous()
        {
            var w = World();
            var c = new WorldTickComposer();
            c.Advance(w, 0); // anchor at world creation
            Advance(c, w, 1, FinalTick);
            return NeedStamps(w);
        }

        [Test]
        public void ColdLoad_RebuildAccumulators_ReproducesContinuousCadence()
        {
            var expected = Continuous();

            // Run to the save point, then simulate a COLD load: a brand-new composer (accumulators 0)
            // restored via the DET-01 path.
            var w = World();
            var pre = new WorldTickComposer();
            pre.Advance(w, 0);
            Advance(pre, w, 1, SaveTick);

            var restored = new WorldTickComposer();          // fresh process -> accumulators are 0
            restored.RebuildAccumulatorsFrom(w.Time);        // DET-01 fix
            Advance(restored, w, SaveTick, FinalTick);        // first call re-anchors (delta 0)

            Assert.That(NeedStamps(w), Is.EqualTo(expected),
                "a cold-loaded composer must reproduce the continuous needs cadence");
        }

        [Test]
        public void ColdLoad_ResetAnchorOnly_DesyncsCadence_ProvingTheBug()
        {
            var expected = Continuous();

            var w = World();
            var pre = new WorldTickComposer();
            pre.Advance(w, 0);
            Advance(pre, w, 1, SaveTick);

            var restored = new WorldTickComposer();          // fresh process -> accumulators are 0
            restored.ResetAnchor();                           // pre-fix behavior: keeps the 0 accumulators
            Advance(restored, w, SaveTick, FinalTick);

            Assert.That(NeedStamps(w), Is.Not.EqualTo(expected),
                "the pre-fix ResetAnchor path desyncs the post-load cadence (this is the DET-01 bug)");
        }
    }
}

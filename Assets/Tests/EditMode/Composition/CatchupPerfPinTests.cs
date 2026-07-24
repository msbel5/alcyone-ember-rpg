using System.Diagnostics;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>W29 made catch-up O(delta) (per-tick replay for chunking invariance). This pin
    /// keeps that honest: a 14-day catch-up (one legacy travel leg) must stay interactive.
    /// Measured 33 ms at pin time - the bound leaves 150x headroom, so a red run means a
    /// genuinely quadratic regression (an hourly system scanning unbounded history), not noise.</summary>
    public sealed class CatchupPerfPinTests
    {
        [Test]
        public void FourteenDayCatchup_StaysUnderFiveSeconds()
        {
            var world = new WorldFactory().Create(roomSeed: 4242);
            var composer = new WorldTickComposer();
            composer.Advance(world, 10); // warm-up: JIT + first-touch allocations

            var watch = Stopwatch.StartNew();
            composer.Advance(world, 10 + 14 * 1440);
            watch.Stop();
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(5000),
                "14-day catch-up took " + watch.ElapsedMilliseconds + " ms - the replay path regressed");
        }
    }
}

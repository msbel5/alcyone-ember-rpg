using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T7: same seed + any chunking => the IDENTICAL action-phase trace. Narrows the
    /// CadenceChunkingInvarianceTests pin to the new action history: W29 catch-up replays
    /// per-tick, so phase transitions INSIDE a chunk must be boundary-stamped and identical.
    /// </summary>
    public sealed class EatChunkingPhaseTraceTests
    {
        private const int TotalTicks = 2 * 1440;

        private static Domain.World.WorldState Run(int[] chunks)
        {
            var world = new WorldFactory().Create(roomSeed: 4242);
            WorldFactory.SeedVillagers(world);
            world.EnsureInvariants(); // the real cast: many eat episodes over two days
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            int at = 0, i = 0;
            while (at < TotalTicks)
            {
                at = System.Math.Min(TotalTicks, at + chunks[i++ % chunks.Length]);
                composer.Advance(world, at);
            }
            return world;
        }

        [Test]
        public void TickByTick_AndRaggedChunks_WriteTheSameActionHistory()
        {
            var tickByTick = Run(new[] { 1 });
            var ragged = Run(new[] { 1, 7, 13, 1, 40, 3, 61, 5, 127, 2 }); // the sibling test's chunk set

            Assert.That(ActionTrace.Of(ragged), Is.EqualTo(ActionTrace.Of(tickByTick)),
                "ragged advancement wrote a DIFFERENT action history — a phase advances on the wrong clock/stamp");
            Assert.That(ActionTrace.StateDigest(ragged), Is.EqualTo(ActionTrace.StateDigest(tickByTick)),
                "final (intent, action, phase, progress, reservation) rows diverged between chunkings");
        }
    }
}

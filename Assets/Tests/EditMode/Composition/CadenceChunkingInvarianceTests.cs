using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// REFORM #2 enforcement (cadence writer conflicts): the SAME world advanced tick-by-tick
    /// and in ragged chunks must produce the IDENTICAL event log. The catch-up contract
    /// (boundary-stamped crossings) is the load-bearing invariant; any system that writes on
    /// the wrong clock breaks equality HERE instead of at the next playtest.
    /// </summary>
    public sealed class CadenceChunkingInvarianceTests
    {
        [Test]
        public void TickByTick_AndRaggedChunks_ProduceIdenticalEventLogs()
        {
            const int totalTicks = 2 * 1440; // two game days

            var worldA = new WorldFactory().Create(roomSeed: 4242);
            var composerA = new WorldTickComposer();
            for (int tick = 1; tick <= totalTicks; tick++)
                composerA.Advance(worldA, tick);

            var worldB = new WorldFactory().Create(roomSeed: 4242);
            var composerB = new WorldTickComposer();
            int at = 0;
            var chunks = new[] { 1, 7, 13, 1, 40, 3, 61, 5, 127, 2 };
            int chunkIndex = 0;
            while (at < totalTicks)
            {
                at = System.Math.Min(totalTicks, at + chunks[chunkIndex % chunks.Length]);
                chunkIndex++;
                composerB.Advance(worldB, at);
            }

            string LogOf(EmberCrpg.Domain.World.WorldState world) => string.Join("\n",
                world.Events.Events.Select(e =>
                    $"{e.Tick.TotalMinutes}:{e.Kind}:{e.ActorId.Value}:{e.SiteId.Value}:{e.Reason}"));

            Assert.That(LogOf(worldB), Is.EqualTo(LogOf(worldA)),
                "ragged advancement produced a DIFFERENT history - some system writes on the wrong clock");
        }
    }
}

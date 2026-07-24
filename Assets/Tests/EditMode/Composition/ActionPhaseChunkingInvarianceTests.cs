using System.Collections.Generic;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Diagnostics;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// W32 DOC4 §5.a: the phase machine's free determinism proof, the CadenceChunkingInvariance
    /// pattern verbatim. Both runs wear a CAPTURE sink (the EmberLog seam the composer's
    /// ActionLogDebugSink mirrors into), so the FULL transition stream is compared line by
    /// line — the ring may trim at 1024, the capture never does.
    /// </summary>
    public sealed class ActionPhaseChunkingInvarianceTests
    {
        private const int TotalTicks = 2 * 1440; // two game days

        private static List<string> CapturedPhaseStream(int[] chunks)
        {
            var lines = new List<string>();
            var priorSink = EmberLog.Sink;
            var priorEnabled = ActionLogDebugSink.Enabled;
            EmberLog.Sink = line => { if (line.StartsWith("[Action] ")) lines.Add(line); };
            ActionLogDebugSink.Enabled = true;
            try
            {
                var world = new WorldFactory().Create(roomSeed: 4242);
                WorldFactory.SeedVillagers(world); // the real cast: many eat episodes
                world.EnsureInvariants();
                var composer = new WorldTickComposer();
                composer.Advance(world, 0);
                int at = 0, i = 0;
                while (at < TotalTicks)
                {
                    at = System.Math.Min(TotalTicks, at + chunks[i++ % chunks.Length]);
                    composer.Advance(world, at);
                }
            }
            finally
            {
                EmberLog.Sink = priorSink;
                ActionLogDebugSink.Enabled = priorEnabled;
            }
            return lines;
        }

        [Test]
        public void TickByTick_AndRaggedChunks_ProduceIdenticalPhaseStreams()
        {
            var tickByTick = CapturedPhaseStream(new[] { 1 });
            var ragged = CapturedPhaseStream(new[] { 1, 7, 13, 1, 40, 3, 61, 5, 127, 2 });

            Assert.That(tickByTick.Count, Is.GreaterThan(0), "vacuous guard: two days must produce eat episodes");
            Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)),
                "ragged advancement produced a DIFFERENT phase history - some system advances actions on the wrong clock");
        }
    }
}

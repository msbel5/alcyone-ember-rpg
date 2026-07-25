using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Diagnostics;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Simulation.World;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// W32 DOC4 §5.a: the phase machine's free determinism proof, the CadenceChunkingInvariance
    /// pattern verbatim. Both runs wear a CAPTURE sink (the EmberLog seam the composer's
    /// ActionLogDebugSink mirrors into), so the FULL transition stream is compared line by
    /// line — the ring may trim at 1024, the capture never does.
    /// W33 F6: the referee now covers the FARM chain too. The full-cast horizon grows 2 -> 4
    /// game days (wheat ripens in 2, so harvest+haul episodes fit); the SOWING guard lives on
    /// the second, narrow-cast run below — DOC 04 F6's own extension mechanism — because the
    /// factory larder (320 wheat) provably cannot drain to shortage inside 4 days, so no
    /// full-cast horizon that respects the perf pins contains a sowing.
    /// W34 S6: the referee now covers Sleep AND PerformWork too. Sleep is caught in the full
    /// cast (four nights are more than enough for a Rest intent to fire); PerformWork lives
    /// on a THIRD narrow-cast run (§6 of the doc's extension mechanism) — a smelt job on a
    /// WorkSlice fixture. Every clock/decision fork the four-day horizon can hide — the
    /// night boundary (22:00/06:00), the workday boundary (06:00/20:00), the Hourly:10 job
    /// step against the PerTick:18/22 band — is prey for this test.
    /// </summary>
    public sealed class ActionPhaseChunkingInvarianceTests
    {
        private const int TotalTicks = 4 * 1440;     // four game days — full cast
        private const int FarmTotalTicks = 3 * 1440; // three game days — narrow farm cast
        private const int WorkTotalTicks = 1 * 1440; // one game day — narrow work cast (a smelt commit fits easily)
        private static readonly int[] TickByTickChunks = { 1 };
        // The W32 chunk set, VERBATIM — a farm phase writing to the wrong hour inside a chunk
        // makes the two streams diverge here.
        private static readonly int[] RaggedChunks = { 1, 7, 13, 1, 40, 3, 61, 5, 127, 2 };

        private static List<string> Captured(int[] chunks, int totalTicks,
            System.Func<EmberCrpg.Domain.World.WorldState> build)
        {
            var lines = new List<string>();
            var priorSink = EmberLog.Sink;
            var priorEnabled = ActionLogDebugSink.Enabled;
            EmberLog.Sink = line => { if (line.StartsWith("[Action] ")) lines.Add(line); };
            ActionLogDebugSink.Enabled = true;
            try
            {
                var world = build();
                var composer = new WorldTickComposer();
                composer.Advance(world, 0);
                int at = 0, i = 0;
                while (at < totalTicks)
                {
                    at = System.Math.Min(totalTicks, at + chunks[i++ % chunks.Length]);
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

        private static EmberCrpg.Domain.World.WorldState FullCast()
        {
            var world = new WorldFactory().Create(roomSeed: 4242);
            WorldFactory.SeedVillagers(world); // the real cast: many eat episodes
            world.EnsureInvariants();
            return world;
        }

        // The F5 famine world: shortage on day 1, sowing minutes later, reaping on day 2-3 —
        // every farm link fits a 3-day horizon with a cast of one.
        private static EmberCrpg.Domain.World.WorldState FarmCast()
        {
            var world = FarmSliceWorld.Build(seedStock: 5, soilCells: 2);
            FarmSliceWorld.Plant(world, 1, "seed");
            world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
            return world;
        }

        // W34 S6 narrow work cast: one smith, one smelt job, one funded execution — the
        // MoveToWorksite/PerformWork band fits one game day (bench walk + fund + commit).
        private static EmberCrpg.Domain.World.WorldState WorkCast()
        {
            var world = WorkSliceWorld.Build(ore: 2, fuel: 1);
            world.Actors.Add(WorkSliceWorld.Smith(7, 9, 9));
            WorkSliceWorld.PostSmeltJob(world);
            return world;
        }

        [Test]
        public void TickByTick_AndRaggedChunks_ProduceIdenticalPhaseStreams()
        {
            var tickByTick = Captured(TickByTickChunks, TotalTicks, FullCast);
            var ragged = Captured(RaggedChunks, TotalTicks, FullCast);

            Assert.That(tickByTick.Count, Is.GreaterThan(0), "vacuous guard: the horizon must produce eat episodes");
            // W33 F6 vacuous guard: without a haul in the stream this run never exercises the
            // farm chain at all — the horizon, not the assertion, would be lying.
            Assert.That(tickByTick.Any(l => l.Contains("HaulCrop")), Is.True,
                "vacuous guard: a HARVEST+HAUL episode must live inside the horizon");
            // W34 S6 vacuous guard: four nights are more than enough for at least one Rest
            // intent to fire (fatigue accumulates 6/hour * 14 waking hours = 84 by 20:00 of
            // day 1, well past FatigueSleepThreshold==1); a horizon that never Rests would
            // silently prove nothing about the night boundary.
            Assert.That(tickByTick.Any(l => l.Contains("Sleep")), Is.True,
                "vacuous guard: a SLEEP episode must live inside the four-night horizon");
            Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)),
                "ragged advancement produced a DIFFERENT phase history - some system advances actions on the wrong clock");
        }

        // W33 F6, second run (DOC 04's sanctioned narrow-cast extension): the shortage-driven
        // SOWING — job → intent → MoveToPlot → PlantSeed — under the same ragged referee. The
        // Daily:20/25/27 boundary steps against the PerTick:18/22 action band are exactly this
        // test's prey: a farm phase mis-clocked inside a chunk splits the streams.
        [Test]
        public void FarmSlice_TickByTick_AndRaggedChunks_ProduceIdenticalPhaseStreams()
        {
            var tickByTick = Captured(TickByTickChunks, FarmTotalTicks, FarmCast);
            var ragged = Captured(RaggedChunks, FarmTotalTicks, FarmCast);

            Assert.That(tickByTick.Any(l => l.Contains("PlantSeed")), Is.True,
                "vacuous guard: a SOWING episode must live inside the horizon");
            Assert.That(tickByTick.Any(l => l.Contains("HaulCrop")), Is.True,
                "vacuous guard: the reaping's HAUL must live inside the horizon");
            Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)),
                "ragged advancement produced a DIFFERENT farm phase history - a farm system advances on the wrong clock");
        }

        // W34 S6 third run (DOC 04's sanctioned narrow-cast extension): the smelt commit —
        // job → claim → MoveToWorksite → PerformWork → RecipeCompleted — under the same ragged
        // referee. The econ.jobs@Hourly:10 boundary stepping against PerTick:18/22 is prey for
        // this test: an order tick or a fund step mis-clocked inside a chunk splits the streams.
        [Test]
        public void WorkSlice_TickByTick_AndRaggedChunks_ProduceIdenticalPhaseStreams()
        {
            var tickByTick = Captured(TickByTickChunks, WorkTotalTicks, WorkCast);
            var ragged = Captured(RaggedChunks, WorkTotalTicks, WorkCast);

            Assert.That(tickByTick.Any(l => l.Contains("PerformWork")), Is.True,
                "vacuous guard: a PERFORM WORK episode must live inside the horizon");
            Assert.That(tickByTick.Any(l => l.Contains("MoveToWorksite")), Is.True,
                "vacuous guard: the commute to the bench must live inside the horizon");
            Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)),
                "ragged advancement produced a DIFFERENT work phase history - a work system advances on the wrong clock");
        }
    }
}

using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CanSuyu
{
    /// <summary>
    /// CAN SUYU H5 — the NEW gate contract. These gates measure properties a routing script
    /// physically cannot fake: needs must come back DOWN (livability), removing one actor must
    /// change the world's trajectory (perturbation sensitivity), and the world must produce
    /// events nobody scripted (unscripted rate). A fixed-hour screenshot can satisfy none of
    /// them — only a real simulation can.
    /// </summary>
    public sealed class LivingWorldGateTests
    {
        private const int TicksPerDay = 1440;

        private static WorldState BuildWorld(int seed)
        {
            var world = new WorldFactory().Create(seed);
            world.EnsureInvariants();
            return world;
        }

        private static void AdvanceDays(WorldState world, WorldTickComposer composer, int days)
        {
            // Tick-by-tick with a monotone tick INDEX — jumps skip cadence boundaries (banked lesson).
            for (int tick = 1; tick <= days * TicksPerDay; tick++)
                composer.Advance(world, tick);
        }

        [Test]
        public void Gate1_Livability_NeedsComeBackDown_AndStocksFlowBothWays()
        {
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();
            int stockStart = world.Stockpiles.Sum(p => p?.Count ?? 0);

            AdvanceDays(world, composer, 5);

            var civilians = world.Actors.Records
                .Where(a => a != null && a.IsAlive && a.Role != ActorRole.Player && a.Role != ActorRole.Enemy)
                .ToList();
            Assert.That(civilians.Count, Is.GreaterThan(0));
            double avgHunger = civilians.Average(a => a.Needs.Hunger.Value);
            double avgFatigue = civilians.Average(a => a.Needs.Fatigue.Value);

            // The old ratchet drove EVERYONE to 100/100 within two days. Consumption must hold
            // the colony below the job-refusal wall after five full days.
            Assert.That(avgHunger, Is.LessThan(80), $"avg hunger {avgHunger} — the colony is starving; consumption is not closing the loop");
            Assert.That(avgFatigue, Is.LessThan(80), $"avg fatigue {avgFatigue} — nobody sleeps");

            // Stocks must MOVE — production adds, meals subtract. A dead economy sits still.
            int meals = world.Events.Events.Count(e => e.Reason != null && e.Reason.StartsWith("meal_eaten"));
            Assert.That(meals, Is.GreaterThan(0), "no meals were eaten in five days — the circuit is open");
            int stockEnd = world.Stockpiles.Sum(p => p?.Count ?? 0);
            Assert.That(stockEnd, Is.Not.EqualTo(stockStart), "total stock is frozen — no flow");
        }

        [Test]
        public void Gate2_PerturbationSensitivity_RemovingOneWorkerChangesTheTrajectory()
        {
            // Same seed, two worlds; world B loses its FIRST civilian worker at day 0.
            var control = BuildWorld(4242);
            var perturbed = BuildWorld(4242);
            var victim = perturbed.Actors.Records.FirstOrDefault(a =>
                a != null && a.IsAlive && a.Role != ActorRole.Player && a.Role != ActorRole.Enemy);
            Assert.That(victim, Is.Not.Null, "no worker to perturb");
            victim.ApplyVitals(new ActorVitals(
                new VitalStat(0, victim.Vitals.Health.Max), victim.Vitals.Fatigue, victim.Vitals.Mana));

            var composerA = new WorldTickComposer();
            var composerB = new WorldTickComposer();
            AdvanceDays(control, composerA, 3);
            AdvanceDays(perturbed, composerB, 3);

            // The trajectories must DIVERGE: total stock, meals eaten, or job completions differ.
            int stockA = control.Stockpiles.Sum(p => p?.Count ?? 0);
            int stockB = perturbed.Stockpiles.Sum(p => p?.Count ?? 0);
            int mealsA = control.Events.Events.Count(e => e.Reason != null && e.Reason.StartsWith("meal_eaten"));
            int mealsB = perturbed.Events.Events.Count(e => e.Reason != null && e.Reason.StartsWith("meal_eaten"));
            int jobsA = control.Events.Events.Count(e => e.Kind == WorldEventKind.JobCompleted);
            int jobsB = perturbed.Events.Events.Count(e => e.Kind == WorldEventKind.JobCompleted);

            bool diverged = stockA != stockB || mealsA != mealsB || jobsA != jobsB;
            Assert.That(diverged, Is.True,
                $"a dead worker changed NOTHING over 3 days (stock {stockA}={stockB}, meals {mealsA}={mealsB}, " +
                $"jobs {jobsA}={jobsB}) — the world is a diorama, not a simulation");
        }

        [Test]
        public void Gate3_UnscriptedEventRate_TheWorldActsWithoutAPlayer()
        {
            var world = BuildWorld(1717);
            var composer = new WorldTickComposer();
            int before = world.Events.Count;

            AdvanceDays(world, composer, 3);

            // Count SYSTEM-caused events (meals, shortages, restock posts, job lifecycle, plant
            // stages) — none of these involve a player action. The world must act on its own.
            int systemEvents = 0;
            for (int i = before; i < world.Events.Count; i++)
            {
                var e = world.Events.Events[i];
                if (e.Kind == WorldEventKind.NeedChanged
                    || e.Kind == WorldEventKind.ShortageDetected
                    || e.Kind == WorldEventKind.JobAssigned
                    || e.Kind == WorldEventKind.JobCompleted
                    || e.Kind == WorldEventKind.PlantStageAdvanced
                    || e.Kind == WorldEventKind.PlantHarvested
                    || e.Kind == WorldEventKind.PriceChanged)
                    systemEvents++;
            }
            double perDay = systemEvents / 3.0;
            Assert.That(perDay, Is.GreaterThanOrEqualTo(5.0),
                $"only {perDay:0.0} unscripted events/day — the world idles unless a script pokes it");
        }
    }
}

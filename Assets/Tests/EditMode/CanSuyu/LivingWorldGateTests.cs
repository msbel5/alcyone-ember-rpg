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
        public void Gate4_EmergentGathering_TheMiddayCrowdHasNoWindowCode()
        {
            // H2's headline: the lunch WINDOW is deleted. The crowd at the food spot must now
            // EMERGE from the morning hunger ramp. We sample occupancy near the larder at 09:00
            // vs 14:00 — a fed morning town works; a hungry afternoon town converges to eat.
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();
            var spot = EmberCrpg.Simulation.Living.NeedConsumptionSystem.FoodSpot(world);
            Assert.That(spot.HasValue, Is.True, "the world has no larder to gather at");

            int CountNear() => world.Actors.Records.Count(a =>
                a != null && a.IsAlive && a.Role != ActorRole.Player && a.Role != ActorRole.Enemy
                && System.Math.Max(System.Math.Abs(a.Position.X - spot.Value.X),
                                   System.Math.Abs(a.Position.Y - spot.Value.Y)) <= 2);

            // Sample occupancy near the larder EVERY HOUR for two days: a living town produces a
            // WAVE (empty table → meal crowd → empty again); a frozen or window-routed town is
            // flat or a square pulse pinned to authored hours. We assert the wave's amplitude.
            int min = int.MaxValue, max = int.MinValue;
            for (int hour = 1; hour <= 48; hour++)
            {
                for (int tick = (hour - 1) * 60 + 1; tick <= hour * 60; tick++)
                    composer.Advance(world, tick);
                int now = CountNear();
                if (now < min) min = now;
                if (now > max) max = now;
            }

            Assert.That(max - min, Is.GreaterThanOrEqualTo(2),
                $"no gathering wave at the larder over two days (min={min}, max={max}) — " +
                "hunger is not moving anyone to the table and back");
        }

        [Test]
        public void Gate5_EventCascade_AnAttackIsSeenAndRemembered()
        {
            // H3: one event must CAUSE another, player absent. The factory world ships a street
            // hunter (Ash Rat); over two headless days it must attack an NPC (link 1), and nearby
            // civilians must WITNESS it — a WitnessRecorded event plus a REAL ActorMemory entry
            // (link 2; NpcMemory's first runtime writes). Depth-2 chains are the seed of stories.
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();
            AdvanceDays(world, composer, 2);

            var events = world.Events.Events;
            bool npcAttack = events.Any(e =>
                e.Kind == WorldEventKind.CombatResolved
                && world.Actors.TryGet(e.ActorId, out var attacker)
                && attacker != null && attacker.Role == ActorRole.Enemy);
            Assert.That(npcAttack, Is.True, "no NPC-vs-NPC attack in two days — predation still lives on the render pump");

            bool witnessed = events.Any(e => e.Kind == WorldEventKind.WitnessRecorded);
            bool remembered = world.NpcMemory.Memories.Any(m =>
                m.Events.Any(ev => ev.EventType == "witnessed_attack"));
            Assert.That(witnessed && remembered, Is.True,
                $"the attack caused nothing (witnessed={witnessed}, memoryWritten={remembered}) — no cascade, no stories");
        }

        [Test]
        public void Gate6_RuntimeHistory_TheChronicleKeepsBeingWritten_AndDiffersBySeed()
        {
            // H4: worldgen history used to FREEZE at minute zero. Over 31 headless days each
            // world must (a) log at least one chronicle event, (b) actually CHANGE at least one
            // faction relation, and (c) two different seeds must write DIFFERENT histories.
            string ChronicleOf(int seed)
            {
                var world = BuildWorld(seed);
                var composer = new WorldTickComposer();
                var before = world.Factions.ReputationRows
                    .Select(r => $"{r.A.Value}-{r.B.Value}:{r.Reputation.Value}").ToArray();
                AdvanceDays(world, composer, 31);

                var chronicle = world.Events.Events
                    .Where(e => e.Kind == WorldEventKind.ChronicleEvent)
                    .Select(e => e.Reason).ToArray();
                Assert.That(chronicle.Length, Is.GreaterThanOrEqualTo(1),
                    $"seed {seed}: no chronicle event in 31 days — history froze again");

                var after = world.Factions.ReputationRows
                    .Select(r => $"{r.A.Value}-{r.B.Value}:{r.Reputation.Value}").ToArray();
                Assert.That(after, Is.Not.EqualTo(before),
                    $"seed {seed}: 31 days changed NO faction relation — diplomacy is fossilized");

                return string.Join("|", chronicle) + "||" + string.Join("|", after);
            }

            Assert.That(ChronicleOf(4242), Is.Not.EqualTo(ChronicleOf(9999)),
                "two seeds wrote IDENTICAL histories — the chronicle is not seed-driven");
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

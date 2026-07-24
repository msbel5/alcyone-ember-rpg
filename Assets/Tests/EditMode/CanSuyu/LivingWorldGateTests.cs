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
            // DF KALIBRASYONU: gates run against a real village cast (12 civilians, 2 guards,
            // 2 hunters), not the 3-civilian slice — thresholds below are sized for it.
            var world = new WorldFactory().Create(seed);
            WorldFactory.SeedVillagers(world);
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
            int stockStart = world.Stockpiles.Sum(p => p?.Entries.Sum(e => e.Value) ?? 0);

            AdvanceDays(world, composer, 5);

            var civilians = world.Actors.Records
                .Where(a => a != null && a.IsAlive && a.Role != ActorRole.Player && a.Role != ActorRole.Enemy)
                .ToList();
            Assert.That(civilians.Count, Is.GreaterThan(0));
            double avgHunger = civilians.Average(a => a.Needs.Hunger.Value);
            double avgFatigue = civilians.Average(a => a.Needs.Fatigue.Value);

            // The old ratchet drove EVERYONE to 100/100 within two days. Consumption must hold
            // the colony below the job-refusal wall after five full days.
            Assert.That(avgHunger, Is.LessThan(70), $"avg hunger {avgHunger} — the village is starving; consumption is not closing the loop");
            Assert.That(avgFatigue, Is.LessThan(75), $"avg fatigue {avgFatigue} — nobody sleeps");

            // Stocks must MOVE — production adds, meals subtract. A dead economy sits still.
            // W32: meals are counted as TERMINAL ACTION OUTCOMES now, not reason-string grep.
            int meals = world.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted);
            Assert.That(meals, Is.GreaterThanOrEqualTo(civilians.Count * 3),
                $"only {meals} meals for {civilians.Count} villagers over five days — the table is theatre, not sustenance");
            int stockEnd = world.Stockpiles.Sum(p => p?.Entries.Sum(e => e.Value) ?? 0);
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
            int stockA = control.Stockpiles.Sum(p => p?.Entries.Sum(e => e.Value) ?? 0);
            int stockB = perturbed.Stockpiles.Sum(p => p?.Entries.Sum(e => e.Value) ?? 0);
            int mealsA = control.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted);
            int mealsB = perturbed.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted);
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
            // W32 re-pin (arrival != meal now): a walker who reaches the ring still TAKES and
            // CHEWS for ConsumeDurationTicks before leaving, so table-sitting got LONGER and
            // the wave stronger - but the crowd remains a stream, so keep sampling every 10
            // ticks or the hourly snapshot misses the (still emergent, window-free) wave.
            int min = int.MaxValue, max = int.MinValue;
            for (int hour = 1; hour <= 48; hour++)
            {
                for (int tick = (hour - 1) * 60 + 1; tick <= hour * 60; tick++)
                {
                    composer.Advance(world, tick);
                    if (tick % 10 != 0) continue;
                    int now = CountNear();
                    if (now < min) min = now;
                    if (now > max) max = now;
                }
            }

            Assert.That(max - min, Is.GreaterThanOrEqualTo(5),
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

            int witnessed = events.Count(e => e.Kind == WorldEventKind.WitnessRecorded);
            int remembered = world.NpcMemory.Memories.Count(m =>
                m.Events.Any(ev => ev.EventType == "witnessed_attack"));
            Assert.That(witnessed >= 2 && remembered >= 2, Is.True,
                $"a village square attack seen by fewer than two people (events={witnessed}, memories={remembered}) — the crowd is scenery");

            // Depth 3: the watch must ANSWER — attack → witness → guard response, all in the log.
            Assert.That(events.Any(e => e.Kind == WorldEventKind.GuardResponded), Is.True,
                "no GuardResponded in two days — attacks are seen but never answered; the cascade dies at depth 2");
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
        public void Gate7_SeedDivergence_DifferentSeedsLiveDifferentLives()
        {
            // H5: determinism must be per-world, not one canned show replayed under every
            // seed. Three seeds run 31 days; their census vectors (stock, meals, events,
            // reputation matrix, chronicle) must be pairwise DIFFERENT.
            string Census(int seed)
            {
                var world = BuildWorld(seed);
                var composer = new WorldTickComposer();
                AdvanceDays(world, composer, 31);
                int stock = world.Stockpiles.Sum(p => p?.Entries.Sum(e => e.Value) ?? 0);
                int meals = world.Events.Events.Count(e => e.Kind == WorldEventKind.ActionCompleted);
                var reps = string.Join(",", world.Factions.ReputationRows
                    .Select(r => $"{r.A.Value}-{r.B.Value}:{r.Reputation.Value}"));
                var chronicle = string.Join(",", world.Events.Events
                    .Where(e => e.Kind == WorldEventKind.ChronicleEvent).Select(e => e.Reason));
                return $"stock:{stock} meals:{meals} events:{world.Events.Count} reps:{reps} chronicle:{chronicle}";
            }

            var a = Census(4242);
            var b = Census(9999);
            var c = Census(1717);
            Assert.That(a, Is.Not.EqualTo(b), "seeds 4242 and 9999 lived IDENTICAL lives — seed is cosmetic");
            Assert.That(b, Is.Not.EqualTo(c), "seeds 9999 and 1717 lived IDENTICAL lives — seed is cosmetic");
            Assert.That(a, Is.Not.EqualTo(c), "seeds 4242 and 1717 lived IDENTICAL lives — seed is cosmetic");
        }

        [Test]
        public void Gate8_PersonalSpace_TheCrowdGathersWithoutStacking()
        {
            // Visual-truth gate: converging on the food spot must FAN OUT over seats, not pile
            // every actor onto one cell (the user watched billboards walk through each other).
            // Sampled hourly over two days: no cell may ever hold more than two civilians.
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();

            int worstStack = 0;
            for (int hour = 1; hour <= 48; hour++)
            {
                for (int tick = (hour - 1) * 60 + 1; tick <= hour * 60; tick++)
                    composer.Advance(world, tick);
                int stack = world.Actors.Records
                    .Where(a => a != null && a.IsAlive && a.Role != ActorRole.Player && a.Role != ActorRole.Enemy)
                    .GroupBy(a => (a.Position.X, a.Position.Y))
                    .Max(g => g.Count());
                if (stack > worstStack) worstStack = stack;
            }

            Assert.That(worstStack, Is.LessThanOrEqualTo(2),
                $"{worstStack} civilians stood on ONE cell — the crowd is a stack of cardboard, not a gathering");
        }

        [Test]
        public void Gate9_MemoryReachesTheTongue_WitnessedEventsEnterTheDialoguePrompt()
        {
            // The user's core demand: NPCs talk via LLM AND their memories feed the words.
            // After two cascade days, a witness's dialogue context — built by the SAME
            // NpcMemoryLlmEnvelope.RecallLines the live dialog path calls — must contain the
            // witnessed attack. Memory that never reaches the tongue is dead data (the V1 sin).
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();
            AdvanceDays(world, composer, 2);

            var witnessId = world.NpcMemory.Memories
                .Where(m => m.Events.Any(ev => ev.EventType == "witnessed_attack"))
                .Select(m => m.ActorId)
                .FirstOrDefault();
            Assert.That(witnessId.IsEmpty, Is.False, "no witness memory exists to recall — Gate5 should have caught this");

            var recall = EmberCrpg.Simulation.AiDm.NpcMemoryLlmEnvelope.RecallLines(world, witnessId, 8);
            Assert.That(recall.Any(line => line.Contains("witnessed_attack")), Is.True,
                "the witness SAW the attack but their dialogue context is silent about it — memory never reaches the tongue");
        }

        [Test]
        public void Gate10_CompanionLoyalty_ThePartyHoldsThroughAFullDay()
        {
            // V3: a recruited companion must still be AT HEEL after a full simulated day of
            // hunger, schedules, and predation — membership that only survives quiet worlds
            // is theatre.
            var world = BuildWorld(4242);
            var composer = new WorldTickComposer();
            var player = EmberCrpg.Simulation.Living.CompanionService.FindPlayer(world);
            var friend = world.Actors.Records.First(a =>
                a != null && a.IsAlive && a.Role == ActorRole.Talker);
            friend.MoveTo(new GridPosition(player.Position.X + 1, player.Position.Y));
            Assert.That(EmberCrpg.Simulation.Living.CompanionService.TryRecruit(world, friend.Id), Is.True);

            AdvanceDays(world, composer, 1);

            Assert.That(world.CompanionIds, Does.Contain(friend.Id.Value), "the party dissolved overnight");
            int distance = System.Math.Max(
                System.Math.Abs(friend.Position.X - player.Position.X),
                System.Math.Abs(friend.Position.Y - player.Position.Y));
            Assert.That(distance, Is.LessThanOrEqualTo(2),
                $"the companion drifted {distance} cells from the player — follow lost to the schedule");
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
                    || e.Kind == WorldEventKind.PriceChanged
                    || e.Kind == WorldEventKind.CombatResolved
                    || e.Kind == WorldEventKind.GuardResponded
                    || e.Kind == WorldEventKind.WitnessRecorded
                    || e.Kind == WorldEventKind.FactionReputationChanged
                    || e.Kind == WorldEventKind.TradeCompleted
                    || e.Kind == WorldEventKind.CaravanArrived
                    || e.Kind == WorldEventKind.PlantPlanted
                    || e.Kind == WorldEventKind.ChronicleEvent)
                    systemEvents++;
            }
            double perDay = systemEvents / 3.0;
            Assert.That(perDay, Is.GreaterThanOrEqualTo(60.0),
                $"only {perDay:0.0} unscripted events/day — a Dwarf Fortress town HUMS; this one idles");
        }
    }
}

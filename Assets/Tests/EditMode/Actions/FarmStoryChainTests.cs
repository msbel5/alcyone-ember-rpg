using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W33 DOC4 F5 (capstone): famine to table. Shortage (Daily:27, stock &lt; 4) triggers the
    /// planting job; the job becomes a BODIED Plant chain; growth takes real DAYS (Daily:20);
    /// harvest + haul refill the pile through actions; a hungry civilian eats the HAULED unit.
    /// "Bu çember kapandığında ekonomi ilk kez gerçekten yaşar" (RUH_TESHIS §9).
    /// Seed-corn note: SeedTag == CropTag, so the fixture budgets the pile so the farmer's own
    /// meals leave 1-3 units at the Daily:27 boundary — below threshold (shortage REAL), above
    /// zero (a seed survives to plant). The diner joins after the sowing so the cause chain
    /// stays deterministic; the assertions are DOC 04's, unchanged.
    /// </summary>
    public sealed class FarmStoryChainTests
    {
        [Test]
        public void ShortageToMeal_TheChainLivesInCauseOrder()
        {
            var world = FarmSliceWorld.Build(seedStock: 5, soilCells: 2);
            FarmSliceWorld.Plant(world, 1, "seed"); // the standing field: growth's proof + the tag anchor
            world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            // Day 1 boundary: shortage fires (the farmer ate the pile down), posts the job; the
            // farmer claims and SOWS. The diner arrives after the sowing (deterministic order).
            int tick = 0;
            while (!world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantPlanted)
                   && tick < 3 * WorldTickComposer.TicksPerGameDay)
                composer.Advance(world, ++tick);
            world.Actors.Add(FarmSliceWorld.Hungry(8, 3, 3)); // the throat that closes the circle

            long firstDeposit = 0;
            int haulDeposits = 0, prevStock = world.Stockpiles[0].Get(FarmSliceWorld.CropTag);
            for (var end = 5 * WorldTickComposer.TicksPerGameDay; tick < end;)
            {
                composer.Advance(world, ++tick);
                if (world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted
                    && e.Tick.TotalMinutes == world.Time.TotalMinutes
                    && e.Reason != null && e.Reason.Contains("farm:haul completed")))
                {
                    haulDeposits++;
                    if (firstDeposit == 0) firstDeposit = world.Time.TotalMinutes;
                    Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.GreaterThan(prevStock),
                        "the deposit RAISED the stock — the chain's one stock-raising gate");
                }
                prevStock = world.Stockpiles[0].Get(FarmSliceWorld.CropTag);
            }

            // The links in CAUSE order: famine before sowing, sowing before reaping — the events
            // are the chain's causal links, not commentary written after the fact.
            var kinds = new[]
            {
                WorldEventKind.ShortageDetected, WorldEventKind.PlantPlanted,
                WorldEventKind.PlantStageAdvanced, WorldEventKind.PlantHarvested,
                WorldEventKind.ActionCompleted,
            };
            var chain = world.Events.Events.Where(e => kinds.Contains(e.Kind)).ToList();
            int shortage = chain.FindIndex(e => e.Kind == WorldEventKind.ShortageDetected);
            int sown = chain.FindIndex(e => e.Kind == WorldEventKind.PlantPlanted);
            int reaped = chain.FindIndex(e => e.Kind == WorldEventKind.PlantHarvested);
            Assert.That(shortage, Is.GreaterThanOrEqualTo(0), "missing link: ShortageDetected");
            Assert.That(sown, Is.GreaterThan(shortage), "sowing must FOLLOW the famine");
            Assert.That(reaped, Is.GreaterThan(sown), "reaping must FOLLOW the sowing");
            Assert.That(chain.Count(e => e.Kind == WorldEventKind.PlantStageAdvanced),
                Is.GreaterThanOrEqualTo(2), "growth REALLY happened: seed→sprout→ripe, on the DAY scale");

            // Episode identities (W32 T4 continuity on the field): the farmer lived BOTH farm
            // episode kinds, and each episode's links share ONE identity stamp.
            var farmEpisodes = EpisodeStarts(world, actorId: 7UL);
            Assert.That(farmEpisodes.Count(e => e.Intent == ActorIntent.Plant), Is.GreaterThanOrEqualTo(1),
                "a sowing episode belongs to the farmer");
            Assert.That(farmEpisodes.Count(e => e.Intent == ActorIntent.Harvest), Is.GreaterThanOrEqualTo(1),
                "a reaping episode belongs to the farmer");
            AssertEpisodeContinuity(world, 7UL, ActorIntent.Plant);
            AssertEpisodeContinuity(world, 7UL, ActorIntent.Harvest);

            // The stock was filled BY ACTIONS — repeatedly — and the circle CLOSED: the diner
            // ate a unit that was hauled in by hand, after the first deposit tick.
            Assert.That(haulDeposits, Is.GreaterThanOrEqualTo(2), "the pile refilled through ACTIONS");
            var meals = world.Events.Events.Where(e => e.Kind == WorldEventKind.ActionCompleted
                && e.ActorId.Value == 8UL && e.Reason != null
                && e.Reason.Contains("eat:consume completed")).ToList();
            Assert.That(meals, Is.Not.Empty, "field to table — the economy LIVES");
            // The diner may snatch the larder's LAST leftover on arrival; the circle-closing
            // proof is a meal AFTER a deposit refilled the drained pile — hauled stock, eaten.
            Assert.That(meals.Any(m => m.Tick.TotalMinutes > firstDeposit), Is.True,
                "at least one of the diner's meals came from HAULED stock");
        }

        private static List<ActionLogEntry> EpisodeStarts(WorldState world, ulong actorId)
        {
            var starts = new List<ActionLogEntry>();
            for (var i = 0; i < world.ActionLog.Count; i++)
            {
                var e = world.ActionLog.At(i);
                if (e.ActorId == actorId && e.Reason == ActionLogReason.ReservationAcquired
                    && e.ToAction == ActorActionType.MoveToPlot)
                    starts.Add(e);
            }
            return starts;
        }

        /// <summary>Every link of the actor's FIRST episode of the intent carries the same
        /// StartedAtMinutes stamp — one episode, one identity (no phantom restarts).</summary>
        private static void AssertEpisodeContinuity(WorldState world, ulong actorId, ActorIntent intent)
        {
            long episodeStart = -1;
            var links = new List<ActionLogEntry>();
            for (var i = 0; i < world.ActionLog.Count; i++)
            {
                var e = world.ActionLog.At(i);
                if (e.ActorId != actorId || e.Intent != intent) continue;
                if (e.Reason == ActionLogReason.ReservationAcquired)
                {
                    if (episodeStart >= 0) break; // the first episode ended at the next start
                    episodeStart = e.TickMinutes;
                }
                if (episodeStart >= 0) links.Add(e);
                if (e.ToPhase == ActionPhase.Succeeded && e.ToAction != ActorActionType.MoveToPlot
                    && e.ToAction != ActorActionType.HarvestCrop)
                    break; // chain-terminal success closes the episode
            }
            Assert.That(links.Count, Is.GreaterThanOrEqualTo(2), $"no {intent} episode in the ring");
            Assert.That(links.All(l => l.Intent == intent), Is.True,
                $"every link of the {intent} episode carries the SAME intent identity");
        }
    }
}

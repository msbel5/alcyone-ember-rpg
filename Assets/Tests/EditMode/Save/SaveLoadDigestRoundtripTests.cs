using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// F4-DoD: save → load → SAME WORLD, byte-identical digest. The seeded world advances a full game day
    /// (jobs, growth, prices, needs all moved), round-trips through the save mapper, and must digest
    /// identically — any store the mapper drops or distorts fails loudly here.
    /// </summary>
    public sealed class SaveLoadDigestRoundtripTests
    {
        private static readonly SiteId Site = new SiteId(77UL);
        private static readonly GridPosition ForgeCell = new GridPosition(4, 5);
        private static readonly GridPosition FarmCell = new GridPosition(2, 2);
        private static readonly JobId Job = new JobId(701UL);
        private static readonly WorldComponentId PlantId = new WorldComponentId(90UL);
        private static readonly WorldComponentId SoilId = new WorldComponentId(10UL);
        private static readonly ActorId Worker = new ActorId(1UL);

        // F22: world quests joined the digest + the mapper — save→load must keep the journal
        // identical: an OPEN generated contract, a COMPLETED one, and the fixed pair's states.
        [Test]
        public void WorldQuests_SurviveSaveLoadRoundtrip()
        {
            var world = BuildSeededWorld();
            SeedWorldQuests(world);

            var before = WorldStateDigest.Compute(world);
            var data = WorldSaveMapper.ToData(world);
            var loaded = WorldSaveMapper.ToWorld(data, BuildSeededWorld());

            Assert.That(WorldStateDigest.Compute(loaded), Is.EqualTo(before),
                "world quests must survive the roundtrip byte-identically");
            Assert.That(loaded.WorldContracts.Count, Is.EqualTo(2));
            Assert.That(loaded.WorldContracts[0].Title, Is.EqualTo("Bring ale to Maren"));
            Assert.That(loaded.WorldContracts[0].Completed, Is.False);
            Assert.That(loaded.WorldContracts[1].Completed, Is.True, "the closed contract stays closed");
            Assert.That(loaded.WorldQuestStates[9001UL].IsComplete, Is.True, "bounty completion persists");
            Assert.That(loaded.WorldQuestStates[9002UL].IsComplete, Is.False, "open pilgrimage stays open");
        }

        private static void SeedWorldQuests(WorldState world)
        {
            world.WorldContracts.Add(new EmberCrpg.Domain.Quest.WorldQuestRecord
            {
                Id = new EmberCrpg.Domain.Quest.QuestId(9100UL),
                Template = EmberCrpg.Domain.Quest.WorldQuestTemplate.Fetch,
                GiverNpcId = new EmberCrpg.Domain.Worldgen.NpcId(10UL),
                GiverName = "Maren",
                TargetSettlementId = new EmberCrpg.Domain.Worldgen.SettlementId(1UL),
                TargetSettlementName = "Hearthome",
                TargetNpcId = new EmberCrpg.Domain.Worldgen.NpcId(10UL),
                TargetNpcName = "Maren",
                ItemTemplateId = "ale",
                RewardGold = 35,
                DeadlineDay = 6,
                Title = "Bring ale to Maren",
            });
            var closed = new EmberCrpg.Domain.Quest.WorldQuestRecord
            {
                Id = new EmberCrpg.Domain.Quest.QuestId(9101UL),
                Template = EmberCrpg.Domain.Quest.WorldQuestTemplate.Visit,
                GiverNpcId = new EmberCrpg.Domain.Worldgen.NpcId(11UL),
                GiverName = "Olun",
                TargetSettlementId = new EmberCrpg.Domain.Worldgen.SettlementId(2UL),
                TargetSettlementName = "Yonderbrook",
                RewardGold = 42,
                DeadlineDay = 8,
                Completed = true,
                Title = "Visit Yonderbrook",
            };
            world.WorldContracts.Add(closed);

            var bounty = new EmberCrpg.Domain.Quest.QuestState(1, world.Time);
            bounty.MarkTaskTriggered(0);
            bounty.SetCompleted(success: true);
            world.WorldQuestStates[9001UL] = bounty;
            world.WorldQuestStates[9002UL] = new EmberCrpg.Domain.Quest.QuestState(1, world.Time);
        }

        // W32 DOC6 row 16: a mid-flight eat episode must survive save->load. The digest already
        // carries the ActionState + Reservations sections; this pins the (action, phase,
        // progress) triple and the live claim id verbatim — a dropped column half-loads the flight.
        [Test]
        public void MidFlightEatEpisode_SurvivesRoundtrip_TripleIntact()
        {
            var world = BuildSeededWorld();
            world.Stockpiles[0].Add("wheat", 3);
            Assert.That(world.Reservations.TryReserve(Site.Value, "wheat", Worker.Value,
                untilMinutes: 999L, pileCount: 3, out var claim), Is.True);
            world.Actors.Get(Worker).ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat)
                .Start(ActorActionType.ConsumeFood, Site, ItemId.Empty, new ReservationId(claim),
                       startedAtMinutes: 100, ActionInterruptPolicy.Interruptible)
                .Advanced().Advanced()); // ConsumeFood@progress=2 with a live claim

            var before = WorldStateDigest.Compute(world);
            var loaded = WorldSaveMapper.ToWorld(WorldSaveMapper.ToData(world), BuildSeededWorld());

            Assert.That(WorldStateDigest.Compute(loaded), Is.EqualTo(before),
                "a mid-flight episode must roundtrip byte-identically");
            var back = loaded.Actors.Get(Worker).ActionState;
            Assert.That((back.CurrentAction, back.Phase, back.ProgressTicks),
                Is.EqualTo((ActorActionType.ConsumeFood, ActionPhase.Running, 2)),
                "the mid-flight (action, phase, progress) triple must load verbatim");
            Assert.That(back.ReservationId.Value, Is.EqualTo(claim), "the claim follows the actor");
            Assert.That(loaded.Reservations.TryGetByActor(Worker.Value, out var row), Is.True,
                "the ledger's derived indexes are rebuilt after load");
            Assert.That(row.Id, Is.EqualTo(claim));
        }

        [Test]
        public void SaveThenLoad_PreservesWorldDigest()
        {
            var world = BuildSeededWorld();
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            for (var tick = 1; tick <= WorldTickComposer.TicksPerGameDay; tick++)
                composer.Advance(world, tick);

            var before = WorldStateDigest.Compute(world);

            var data = WorldSaveMapper.ToData(world);
            var loaded = WorldSaveMapper.ToWorld(data, BuildSeededWorld());
            var after = WorldStateDigest.Compute(loaded);

            Assert.That(after, Is.EqualTo(before),
                "save→load must reproduce the world byte-identically (a dropped/distorted store fails here)");
        }

        private static WorldState BuildSeededWorld()
        {
            var world = new WorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);

            world.Actors = new ActorStore();
            world.Actors.Add(Worker0());
            world.Actors.Add(PlayerActor());   // the save mapper anchors on the Player- and
            world.Actors.Add(MerchantActor()); // Merchant-role records (authored slice always has both)
            world.Actors.Add(SimpleActor(4UL, "Watch Bren", ActorRole.Guard));
            world.Actors.Add(SimpleActor(5UL, "Gnasher", ActorRole.Enemy)); // the mapper expects the full fixed cast

            world.Worksites.Add(new WorksiteRecord(Site, ForgeCell, WorksiteKind.Furnace, isActive: true));
            world.Jobs.Add(new JobRequest(
                Job,
                new RecipeId(1001UL),
                Site,
                ForgeCell,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(1),
                quantity: 1,
                requesterId: Worker));

            world.Plants.Add(PlantId, new PlantComponent(PlantId, Site, FarmCell, "wheat", new PlantStageId("seed"), 0));
            world.Soils.Add(SoilId, new SoilComponent(SoilId, Site, FarmCell, fertility: 70, moisture: 60, plantId: PlantId));

            var stockpile = new StockpileComponent(Site);
            stockpile.Add("iron", 2);
            world.Stockpiles.Add(stockpile);
            world.Prices.SetPrice(Site, "iron", 10);

            return world;
        }

        private static ActorRecord PlayerActor()
        {
            return new ActorRecord(
                new ActorId(2UL),
                "Vael",
                ActorRole.Player,
                new EmberStatBlock(50, 50, 50, 50, 50, 50),
                new ActorVitals(
                    new VitalStat(40, 40),
                    new VitalStat(40, 40),
                    new VitalStat(25, 25)),
                new GridPosition(1, 1),
                accuracy: 50,
                dodge: 30,
                armor: 4,
                baseDamage: 6);
        }

        private static ActorRecord SimpleActor(ulong id, string name, ActorRole role)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(15, 15)),
                new GridPosition(3, (int)id),
                accuracy: 40,
                dodge: 30,
                armor: 3,
                baseDamage: 4);
        }

        private static ActorRecord MerchantActor()
        {
            return new ActorRecord(
                new ActorId(3UL),
                "Trader Mira",
                ActorRole.Merchant,
                new EmberStatBlock(45, 45, 45, 45, 45, 45),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(20, 20)),
                new GridPosition(2, 1),
                accuracy: 35,
                dodge: 30,
                armor: 2,
                baseDamage: 3);
        }

        private static ActorRecord Worker0()
        {
            var actor = new ActorRecord(
                Worker,
                "Smith Ada",
                ActorRole.Talker,
                new EmberStatBlock(40, 40, 40, 40, 40, 40),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(30, 30),
                    new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 40,
                dodge: 30,
                armor: 4,
                baseDamage: 4);
            actor.ApplyJobPreferences(new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            return actor;
        }
    }
}

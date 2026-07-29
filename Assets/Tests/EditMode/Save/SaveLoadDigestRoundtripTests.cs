using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living.Actions;
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
        // A real pre-HuntTargets JSON fixture: schema v0 and no huntHunterIds/huntTargetIds/
        // huntUntilMinutes properties. Loading it through JsonSliceSaveService exercises the
        // canonical JsonUtility -> WorldSaveData -> WorldSaveMapper seam.
        private const string LegacyMidHuntV0Json =
            "{"
            + "\"schemaVersion\":0,"
            + "\"roomSeed\":1337,"
            + "\"totalMinutes\":480,"
            + "\"actors\":[{"
            + "\"id\":5,"
            + "\"name\":\"Legacy Hunter\","
            + "\"role\":4,"
            + "\"positionX\":3,"
            + "\"positionY\":5,"
            + "\"hasHomeAnchor\":true,"
            + "\"homeX\":3,"
            + "\"homeY\":5,"
            + "\"dayAnchorX\":3,"
            + "\"dayAnchorY\":5,"
            + "\"mig\":10,"
            + "\"agi\":10,"
            + "\"end\":10,"
            + "\"mnd\":10,"
            + "\"ins\":10,"
            + "\"pre\":10,"
            + "\"healthCurrent\":10,"
            + "\"healthMax\":10,"
            + "\"fatigueCurrent\":10,"
            + "\"fatigueMax\":10,"
            + "\"manaCurrent\":10,"
            + "\"manaMax\":10,"
            + "\"hasMood\":true,"
            + "\"mood\":50,"
            + "\"currentIntent\":7,"
            + "\"currentAction\":13,"
            + "\"actionPhase\":1,"
            + "\"actionTargetSiteId\":77,"
            + "\"actionProgressTicks\":1,"
            + "\"actionStartedAtMinutes\":480,"
            + "\"actionInterruptPolicy\":0"
            + "}]}";

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

        // W33 pin migration (DOC4 §2 row 11): the farm twin of the eat pin above — a hands-full
        // HaulCrop flight (CarriedUnits + carry row) PLUS a live plot claim on a second actor.
        // WorldStateDigest carries CarriedUnits and the reservation rows, so a mapper that
        // drops either loses a mid-haul unit or duplicates a plot — and fails HERE.
        [Test]
        public void MidFlightFarmEpisode_SurvivesRoundtrip_HandsAndClaimsIntact()
        {
            var world = BuildSeededWorld();
            world.Stockpiles[0].Add("wheat", 5);
            Assert.That(world.Stockpiles[0].Remove("wheat", 2), Is.EqualTo(2),
                "the two carried units leave the pile before the save");
            Assert.That(world.Reservations.TryReserve(Site.Value, "carry:wheat", Worker.Value,
                untilMinutes: 999L, pileCount: int.MaxValue, out var carryRow), Is.True);
            world.Actors.Get(Worker).ApplyActionState(ActorActionState.ForIntent(ActorIntent.Harvest)
                .Start(ActorActionType.HaulCrop, Site, ItemId.Empty, new ReservationId(carryRow),
                       startedAtMinutes: 100, ActionInterruptPolicy.Interruptible)
                .WithCarriedMatter("wheat", 2).Advanced()); // HaulCrop@progress=1 with 2 units in hand
            // A SECOND actor holds the plot claim ("plot:{soilId}" — FarmOperations' codec):
            // one row per actor, so the harvest-in-waiting belongs to the guard.
            var guard = new ActorId(4UL);
            Assert.That(world.Reservations.TryReserve(Site.Value, "plot:10", guard.Value,
                untilMinutes: 999L, pileCount: 1, out var plotRow), Is.True);

            var matterBefore = world.Stockpiles[0].Get("wheat")
                + world.Actors.Records.Sum(a => a?.ActionState.CarriedUnits ?? 0);
            var before = WorldStateDigest.Compute(world);
            var loaded = WorldSaveMapper.ToWorld(WorldSaveMapper.ToData(world), BuildSeededWorld());

            Assert.That(WorldStateDigest.Compute(loaded), Is.EqualTo(before),
                "a mid-haul flight must roundtrip byte-identically — hands included");
            var back = loaded.Actors.Get(Worker).ActionState;
            Assert.That((back.CurrentAction, back.Phase, back.ProgressTicks, back.CarriedUnits),
                Is.EqualTo((ActorActionType.HaulCrop, ActionPhase.Running, 1, 2)),
                "the (action, phase, progress, hands) quad must load verbatim");
            Assert.That(back.CarriedMatterTag, Is.EqualTo("wheat"));
            Assert.That(back.ReservationId.Value, Is.EqualTo(carryRow), "the carry row follows the hauler");
            Assert.That(loaded.Reservations.TryGetByActor(guard.Value, out var plot), Is.True,
                "the plot claim survives beside the carry row");
            Assert.That((plot.Id, plot.ItemTag), Is.EqualTo((plotRow, "plot:10")),
                "the plot row loads verbatim — the ledger IS the plot exclusivity");
            var matterAfter = loaded.Stockpiles[0].Get("wheat")
                + loaded.Actors.Records.Sum(a => a?.ActionState.CarriedUnits ?? 0);
            Assert.That(matterAfter, Is.EqualTo(matterBefore).And.EqualTo(5),
                "matter is neither lost nor duplicated across the mid-haul save/load boundary");
        }

        // W34 pin (DOC4 §2 row 10): the sleep+work twins of the eat/farm pins above. A mid-night
        // Sleep flight (Rest intent + a live "bed:" row) and a mid-shift PerformWork flight
        // (Work intent, ReservationId.Empty by contract) PLUS a FROZEN WorkOrderLedger row must
        // roundtrip byte-identically — a dropped jobId/completedExecutions column would either
        // orphan the order or replay its funding (the double-consumption wound this store closes).
        [Test]
        public void MidFlightSleepAndWorkEpisodes_SurviveRoundtrip_RowsIntact()
        {
            var world = BuildSeededWorld();
            // Guard 4 sleeps at its Home cell (guards sleep too — W34 DOC1 §4 keeps the fiat's roles).
            var guard = new ActorId(4UL);
            var home = world.Actors.Get(guard).Home;
            Assert.That(world.Reservations.TryReserve(0UL, "bed:" + home.X + ":" + home.Y,
                guard.Value, untilMinutes: 999L, pileCount: 1, out var bedRow), Is.True);
            world.Actors.Get(guard).ApplyActionState(ActorActionState.ForIntent(ActorIntent.Rest)
                .Start(ActorActionType.Sleep, default(SiteId), ItemId.Empty, new ReservationId(bedRow),
                       startedAtMinutes: 1380, ActionInterruptPolicy.Interruptible)
                .Advanced().Advanced().Advanced()); // Sleep@progress=3, mid-night save
            // Worker 1 stands at the bench mid-execution; the order row is the bench truth.
            world.Actors.Get(Worker).ApplyActionState(ActorActionState.ForIntent(ActorIntent.Work)
                .Start(ActorActionType.PerformWork, Site, ItemId.Empty, ReservationId.Empty,
                       startedAtMinutes: 490, ActionInterruptPolicy.Interruptible)
                .Advanced().Advanced()); // PerformWork@progress=2
            Assert.That(world.WorkOrders.Add(new WorkOrderRecord
            {
                JobId = Job.Value,
                RecipeId = 1001UL,
                SiteId = Site.Value,
                PositionX = ForgeCell.X,
                PositionY = ForgeCell.Y,
                StartedByActorId = Worker.Value,
                ProgressTicks = 1,
                CompletedExecutions = 0,
            }), Is.True, "the frozen order row must seed");

            var before = WorldStateDigest.Compute(world);
            var loaded = WorldSaveMapper.ToWorld(WorldSaveMapper.ToData(world), BuildSeededWorld());

            Assert.That(WorldStateDigest.Compute(loaded), Is.EqualTo(before),
                "mid-flight sleep/work episodes must roundtrip byte-identically");
            var sleeper = loaded.Actors.Get(guard).ActionState;
            Assert.That((sleeper.CurrentIntent, sleeper.CurrentAction, sleeper.Phase, sleeper.ProgressTicks),
                Is.EqualTo((ActorIntent.Rest, ActorActionType.Sleep, ActionPhase.Running, 3)),
                "the sleep (intent, action, phase, progress) quad must load verbatim");
            Assert.That(sleeper.ReservationId.Value, Is.EqualTo(bedRow), "the bed row follows the sleeper");
            var busy = loaded.Actors.Get(Worker).ActionState;
            Assert.That((busy.CurrentIntent, busy.CurrentAction, busy.Phase, busy.ProgressTicks),
                Is.EqualTo((ActorIntent.Work, ActorActionType.PerformWork, ActionPhase.Running, 2)),
                "the work (intent, action, phase, progress) quad must load verbatim");
            Assert.That(loaded.WorkOrders.TryGetByJob(Job.Value, out var row), Is.True,
                "the ledger's derived job index is rebuilt after load");
            Assert.That((row.RecipeId, row.ProgressTicks, row.CompletedExecutions, row.StartedByActorId),
                Is.EqualTo((1001UL, 1, 0, Worker.Value)),
                "the frozen order row loads verbatim — funding state included");
        }

        [Test]
        public void MidHunt_SurvivesRoundtrip_AndFirstRestoredTickContinuesDeterministically()
        {
            var world = BuildSeededWorld();
            var hunterId = new ActorId(5UL);
            var hunter = world.Actors.Get(hunterId);
            world.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = hunterId.Value,
                TargetId = Worker.Value,
                UntilMinutes = 999L,
            });
            hunter.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt)
                .Start(ActorActionType.Hunt, Site, ItemId.Empty, ReservationId.Empty,
                    startedAtMinutes: 480L, ActionInterruptPolicy.Interruptible)
                .Advanced());

            var before = MidActionSemanticSnapshot(world);
            var data = WorldSaveMapper.ToData(world);
            var first = WorldSaveMapper.ToWorld(data, BuildSeededWorld());
            var second = WorldSaveMapper.ToWorld(data, BuildSeededWorld());

            Assert.That(MidActionSemanticSnapshot(first), Is.EqualTo(before),
                "actor mind plus hunt target ledger is the semantic save snapshot");
            Assert.That(MidActionSemanticSnapshot(second), Is.EqualTo(before));
            Assert.That(first.HuntTargets.Single().TargetId, Is.EqualTo(Worker.Value),
                "mid-hunt prey identity must not disappear");

            var firstHunter = first.Actors.Get(hunterId);
            var secondHunter = second.Actors.Get(hunterId);
            var start = firstHunter.Position;
            var stamp = new GameTime(540L); // hour boundary: HuntAdvancer must take one route step
            new HuntAdvancer(new ActionLogManager()).Advance(first, firstHunter, stamp);
            new HuntAdvancer(new ActionLogManager()).Advance(second, secondHunter, stamp);

            Assert.That(firstHunter.Position, Is.Not.EqualTo(start),
                "the first post-load tick continues the running hunt");
            Assert.That(firstHunter.Position, Is.EqualTo(secondHunter.Position),
                "two restores choose the same first route step");
            Assert.That(firstHunter.ActionState, Is.EqualTo(secondHunter.ActionState));
            Assert.That((firstHunter.ActionState.Phase, firstHunter.ActionState.ProgressTicks),
                Is.EqualTo((ActionPhase.Running, 2)),
                "movement advances exactly once from the restored progress value");
        }

        [Test]
        public void LegacySaveWithoutHuntFields_LoadsEmpty_ThenRunningHuntFailsDeterministically()
        {
            var world = BuildSeededWorld();
            var hunterId = new ActorId(5UL);
            world.Actors.Get(hunterId).ApplyActionState(ActorActionState.ForIntent(ActorIntent.Hunt)
                .Start(ActorActionType.Hunt, Site, ItemId.Empty, ReservationId.Empty,
                    startedAtMinutes: 480L, ActionInterruptPolicy.Interruptible));
            var legacy = WorldSaveMapper.ToData(world);
            legacy.schemaVersion = 0;
            legacy.huntHunterIds = null;
            legacy.huntTargetIds = null;
            legacy.huntUntilMinutes = null;

            var seed = BuildSeededWorld();
            seed.HuntTargets.Add(new HuntTargetRecord
            {
                HunterId = hunterId.Value,
                TargetId = Worker.Value,
                UntilMinutes = 999L,
            });
            var loaded = WorldSaveMapper.ToWorld(legacy, seed);

            Assert.That(loaded.HuntTargets, Is.Not.Null.And.Empty,
                "missing append-only fields mean empty, never stale seed relationships");
            var hunter = loaded.Actors.Get(hunterId);
            new HuntAdvancer(new ActionLogManager()).Advance(loaded, hunter, new GameTime(541L));
            Assert.That((hunter.ActionState.Phase, hunter.ActionState.FailureReason),
                Is.EqualTo((ActionPhase.Failed, ActionFailureReason.TargetGone)),
                "a rowless restored Hunt terminates deterministically on its first advancement");
        }

        [Test]
        public void LegacyJsonFixtureWithoutHuntFields_CanonicalLoadUsesSafeDeterministicDefault()
        {
            Assert.That(LegacyMidHuntV0Json, Does.Not.Contain("huntHunterIds")
                .And.Not.Contain("huntTargetIds").And.Not.Contain("huntUntilMinutes"),
                "fixture must genuinely omit the append-only HuntTargets fields");

            var loaded = new EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService()
                .LoadFromJson(LegacyMidHuntV0Json);

            Assert.That(loaded.HuntTargets, Is.Not.Null.And.Empty,
                "legacy JSON cannot inherit or invent a hunt relationship");
            var hunter = loaded.Actors.Get(new ActorId(5UL));
            Assert.That((hunter.ActionState.CurrentAction, hunter.ActionState.Phase,
                    hunter.ActionState.ProgressTicks),
                Is.EqualTo((ActorActionType.Hunt, ActionPhase.Running, 1)),
                "the old JSON still restores its action block before validation");

            new HuntAdvancer(new ActionLogManager()).Advance(loaded, hunter, new GameTime(541L));

            Assert.That((hunter.ActionState.Phase, hunter.ActionState.FailureReason,
                    hunter.ActionState.ProgressTicks),
                Is.EqualTo((ActionPhase.Failed, ActionFailureReason.TargetGone, 1)),
                "first post-load advancement fails once, predictably, without fabricating progress");
        }

        [Test]
        public void EnsureInvariants_RepairsEveryCanonicalMutableCollectionRoot()
        {
            var world = new WorldState
            {
                PlayerInventory = null,
                PlayerEquipment = null,
                MerchantInventory = null,
                PlayerKnownSpellIds = null,
                Pickups = null,
                DungeonRoomStates = null,
                DungeonDoorStates = null,
                Topics = null,
                NpcMemory = null,
                CompanionIds = null,
                GuardPursuits = null,
                HuntTargets = null,
                Reservations = null,
                WorkOrders = null,
                ActionLog = null,
                Critters = null,
                Rumors = null,
                SiteUnrest = null,
                PlayerSpellCooldowns = null,
                PlayerShieldBuffs = null,
            };

            world.EnsureInvariants();

            Assert.Multiple(() =>
            {
                Assert.That(world.PlayerInventory, Is.Not.Null);
                Assert.That(world.PlayerInventory.Capacity, Is.EqualTo(10));
                Assert.That(world.PlayerEquipment, Is.Not.Null);
                Assert.That(world.MerchantInventory, Is.Not.Null);
                Assert.That(world.MerchantInventory.Capacity, Is.EqualTo(32));
                Assert.That(world.PlayerKnownSpellIds, Is.Not.Null);
                Assert.That(world.Pickups, Is.Not.Null);
                Assert.That(world.DungeonRoomStates, Is.Not.Null);
                Assert.That(world.DungeonDoorStates, Is.Not.Null);
                Assert.That(world.Topics, Is.Not.Null);
                Assert.That(world.NpcMemory, Is.Not.Null);
                Assert.That(world.CompanionIds, Is.Not.Null);
                Assert.That(world.GuardPursuits, Is.Not.Null);
                Assert.That(world.HuntTargets, Is.Not.Null);
                Assert.That(world.Reservations, Is.Not.Null);
                Assert.That(world.WorkOrders, Is.Not.Null);
                Assert.That(world.ActionLog, Is.Not.Null);
                Assert.That(world.Critters, Is.Not.Null);
                Assert.That(world.Rumors, Is.Not.Null);
                Assert.That(world.SiteUnrest, Is.Not.Null);
                Assert.That(world.PlayerSpellCooldowns, Is.Not.Null);
                Assert.That(world.PlayerShieldBuffs, Is.Not.Null);
            });
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

        private static string MidActionSemanticSnapshot(WorldState world)
        {
            var huntRows = string.Join(";", (world.HuntTargets ?? new System.Collections.Generic.List<HuntTargetRecord>())
                .Select(row => $"{row.HunterId}:{row.TargetId}:{row.UntilMinutes}"));
            return WorldStateDigest.Compute(world) + "\nHUNTTARGETS\n" + huntRows;
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

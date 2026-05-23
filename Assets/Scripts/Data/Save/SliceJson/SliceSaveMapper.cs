// Design note:
// SliceSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: SliceWorldState or SliceSaveData snapshots.
// Outputs: round-trippable save objects with no UnityEngine in Domain/Simulation.
// Bible reference: PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
// Codex audit (seventh pass B-P1 #9): Data.SliceJson no longer references
// EmberCrpg.Simulation. RecipeWorkOrder / SliceWorldFactory rehydration moved
// to EmberCrpg.Simulation.Process.SliceSaveRehydration. SliceWorldState
// construction is the caller's responsibility (overload taking the seed
// world, below).
namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between aggregate world state and JSON DTOs.</summary>
    public static class SliceSaveMapper
    {
        public static SliceSaveData ToData(SliceWorldState world)
        {
            // Codex audit (third pass A-P3): null world used to NRE inside the
            // initializer; throw a typed exception so callers can detect and
            // recover (e.g. surface a "save corrupt" status) rather than
            // crashing the save path.
            if (world == null) throw new ArgumentNullException(nameof(world));
            return new SliceSaveData
            {
                totalMinutes = world.Time.TotalMinutes,
                roomSeed = world.RoomSeed,
                currentRoomId = world.CurrentRoomId,
                dungeonStartRoomId = world.Dungeon?.StartRoomId ?? 0,
                playerRoomId = world.PlayerRoomId,
                talkerRoomId = world.TalkerRoomId,
                merchantRoomId = world.MerchantRoomId,
                guardRoomId = world.GuardRoomId,
                enemyRoomId = world.EnemyRoomId,
                pickupRoomId = world.PickupRoomId,
                dungeonRooms = DungeonSaveMapper.ToRoomData(world.Dungeon),
                dungeonDoors = DungeonSaveMapper.ToDoorData(world.Dungeon),
                dungeonSpawns = DungeonSaveMapper.ToSpawnData(world.Dungeon),
                dungeonRoomStates = DungeonSaveMapper.ToRoomStateData(world.DungeonRoomStates),
                dungeonDoorStates = DungeonSaveMapper.ToDoorStateData(world.DungeonDoorStates),
                player = ActorSaveMapper.ToData(world.Actors.FirstByRole(ActorRole.Player)),
                talker = ActorSaveMapper.ToData(world.Actors.FirstByRole(ActorRole.Talker)),
                merchant = ActorSaveMapper.ToData(world.Actors.FirstByRole(ActorRole.Merchant)),
                guard = ActorSaveMapper.ToData(world.Actors.FirstByRole(ActorRole.Guard)),
                enemy = ActorSaveMapper.ToData(world.Actors.FirstByRole(ActorRole.Enemy)),
                actors = ToActorStoreData(world.Actors),
                itemRecords = ToItemStoreData(world.Items),
                sites = ToSiteStoreData(world.Sites),
                factions = ToFactionStoreData(world.Factions),
                factionReputations = ToFactionReputationData(world.Factions),
                prices = ToPriceLedgerData(world.Prices),
                stockpiles = ToStockpileData(world.Stockpiles),
                tradeRoutes = ToTradeRouteData(world.TradeRoutes),
                caravans = ToCaravanData(world.Caravans),
                worldEvents = ToWorldEventLogData(world.Events),
                toolCallTrace = ToToolCallTraceData(world.ToolCallTrace),
                llmProposalLog = ToLlmProposalLogData(world.LlmProposalLog),
                npcSeeds = ToNpcSeedData(world.NpcSeeds),
                worldProfile = ToWorldProfileData(world.WorldProfile),
                inventory = ToInventoryData(world.PlayerInventory),
                playerEquipment = ToEquipmentData(world.PlayerEquipment),
                merchantInventory = ToInventoryData(world.MerchantInventory),
                pickups = world.Pickups.Select(ItemSaveMapper.ToData).ToArray(),
                topics = world.Topics.Select(topic => new TopicSaveData { id = topic.Id, label = topic.Label, answer = topic.Answer }).ToArray(),
                npcMemories = ToNpcMemoryData(world.NpcMemory),
                playerSpellCooldowns = SpellCooldownSaveMapper.ToData(world.PlayerSpellCooldowns),
                playerShieldBuffs = ShieldBuffSaveMapper.ToData(world.PlayerShieldBuffs),
                doorOpen = world.DoorOpen,
                guardDoorAccessGranted = world.GuardDoorAccessGranted,
                guardWarningCount = world.GuardWarningCount,
                encounterActive = world.EncounterActive,
                lastNarrative = world.LastNarrative,
            };
        }

        public static SliceWorldState ToWorld(SliceSaveData data, SliceWorldState seedWorld)
        {
            var world = seedWorld ?? throw new ArgumentNullException(nameof(seedWorld));
            world.Time = new EmberCrpg.Domain.Core.GameTime(data.totalMinutes);
            if (data.dungeonRooms != null && data.dungeonRooms.Length > 0)
                world.Dungeon = DungeonSaveMapper.ToLayout(data.roomSeed, data.dungeonStartRoomId, data.dungeonRooms, data.dungeonDoors, data.dungeonSpawns);
            world.CurrentRoomId = data.currentRoomId;
            world.PlayerRoomId = data.playerRoomId;
            world.TalkerRoomId = data.talkerRoomId;
            world.MerchantRoomId = data.merchantRoomId;
            world.GuardRoomId = data.guardRoomId;
            world.EnemyRoomId = data.enemyRoomId;
            world.PickupRoomId = data.pickupRoomId;
            if (data.dungeonRoomStates != null && data.dungeonRoomStates.Length > 0)
                world.DungeonRoomStates = DungeonSaveMapper.ToRoomStates(data.dungeonRoomStates);
            if (data.dungeonDoorStates != null && data.dungeonDoorStates.Length > 0)
                world.DungeonDoorStates = DungeonSaveMapper.ToDoorStates(data.dungeonDoorStates);
            if (data.actors != null)
            {
                world.Actors = ToActorStore(data.actors);
            }
            else
            {
                world.ReplaceActorView(ActorRole.Player, ActorSaveMapper.ToActor(data.player));
                world.ReplaceActorView(ActorRole.Talker, ActorSaveMapper.ToActor(data.talker));
                world.ReplaceActorView(ActorRole.Merchant, ActorSaveMapper.ToActor(data.merchant));
                world.ReplaceActorView(ActorRole.Guard, ActorSaveMapper.ToActor(data.guard));
                world.ReplaceActorView(ActorRole.Enemy, ActorSaveMapper.ToActor(data.enemy));
            }
            world.Items = ToItemStore(data.itemRecords);
            world.Sites = ToSiteStore(data.sites);
            world.Factions = ToFactionStore(data.factions);
            ApplyFactionReputations(world.Factions, data.factionReputations);
            world.Prices = ToPriceLedger(data.prices);
            world.Stockpiles = ToStockpiles(data.stockpiles);
            world.TradeRoutes = ToTradeRoutes(data.tradeRoutes);
            world.Caravans = ToCaravans(data.caravans);
            world.Events = ToWorldEventLog(data.worldEvents);
            world.ToolCallTrace = ToToolCallTrace(data.toolCallTrace);
            world.LlmProposalLog = ToLlmProposalLog(data.llmProposalLog);
            world.NpcSeeds = ToNpcSeeds(data.npcSeeds);
            world.WorldProfile = ToWorldProfile(data.worldProfile);
            world.PlayerInventory = ToInventoryState(data.inventory, world.PlayerInventory.Capacity);
            world.PlayerEquipment = ToEquipmentState(data.playerEquipment);
            world.MerchantInventory = ToInventoryState(data.merchantInventory, world.MerchantInventory.Capacity);
            world.Pickups = (data.pickups ?? Array.Empty<PickupSaveData>()).Select(ItemSaveMapper.ToPickup).ToList();
            world.Topics = (data.topics ?? Array.Empty<TopicSaveData>()).Select(topic => new AskAboutTopic(topic.id, topic.label, topic.answer)).ToList();
            world.NpcMemory = ToNpcMemoryStore(data.npcMemories);
            world.PlayerSpellCooldowns = SpellCooldownSaveMapper.ToState(data.playerSpellCooldowns);
            world.PlayerShieldBuffs = ShieldBuffSaveMapper.ToState(data.playerShieldBuffs);
            world.DoorOpen = data.doorOpen;
            world.GuardDoorAccessGranted = data.guardDoorAccessGranted;
            world.GuardWarningCount = data.guardWarningCount;
            world.EncounterActive = data.encounterActive;
            world.LastNarrative = data.lastNarrative;
            return world;
        }


        public static WorksiteSaveData[] ToWorksiteData(WorksiteStore store)
        {
            return (store?.Records ?? Array.Empty<WorksiteRecord>()).Select(ToWorksiteData).ToArray();
        }

        public static WorksiteStore ToWorksiteStore(WorksiteSaveData[] data)
        {
            var store = new WorksiteStore();
            foreach (var record in data ?? Array.Empty<WorksiteSaveData>())
            {
                if (record != null)
                    store.Add(ToWorksiteRecord(record));
            }

            return store;
        }

        // Codex audit (seventh pass B-P1 #9, #10): the previous methods
        // `ToRecipeWorkOrderData(RecipeWorkOrder)` and
        // `ToRecipeWorkOrder(RecipeWorkOrderSaveData, ...)` took/returned a
        // Simulation.Process type, forcing this Data asmdef to reference
        // EmberCrpg.Simulation. Moved to
        // EmberCrpg.Simulation.Process.SliceSaveRehydration so the Data
        // asmdef no longer leaks the Simulation namespace.

        public static JobRequestSaveData[] ToJobBoardData(JobBoard board)
        {
            return (board?.Requests ?? Array.Empty<JobRequest>()).Select(request => ToJobRequestData(request, board)).ToArray();
        }

        public static JobBoard ToJobBoard(JobRequestSaveData[] data)
        {
            var board = new JobBoard();
            // PR#138 bot review fix: add jobs in insertion order first, then restore
            // claims in original claim-sequence order using the explicit accessor so
            // GetQueueIndex returns the same value after a roundtrip.
            var sorted = (data ?? Array.Empty<JobRequestSaveData>())
                .Where(d => d != null)
                .ToArray();

            foreach (var saved in sorted)
            {
                var request = new JobRequest(
                    new JobId(saved.id),
                    new RecipeId(saved.recipeId),
                    new SiteId(saved.siteId),
                    new GridPosition(saved.positionX, saved.positionY),
                    (WorksiteKind)saved.worksiteKind,
                    (JobKind)saved.kind,
                    JobPriority.Active(saved.priority),
                    saved.quantity,
                    new ActorId(saved.requesterId));
                board.Add(request);
            }

            foreach (var saved in sorted.OrderBy(d => d.claimSequence))
            {
                var claimedBy = new ActorId(saved.claimedByActorId);
                if (claimedBy.IsEmpty) continue;

                var restored = saved.claimSequence > 0
                    ? board.TryRestoreClaim(new JobId(saved.id), claimedBy, saved.claimSequence)
                    : board.TryClaim(new JobId(saved.id), claimedBy, out _);
                if (!restored)
                    throw new InvalidOperationException($"JobBoard save data could not restore claim for {saved.id}.");
            }

            return board;
        }

        public static SoilComponentSaveData[] ToSoilComponentData(ComponentStore<SoilComponent> soils)
        {
            return (soils?.Rows ?? Array.Empty<System.Collections.Generic.KeyValuePair<WorldComponentId, SoilComponent>>())
                .Select(row => ToSoilComponentData(row.Value))
                .ToArray();
        }

        public static ComponentStore<SoilComponent> ToSoilComponentStore(SoilComponentSaveData[] data)
        {
            var store = new ComponentStore<SoilComponent>();
            foreach (var soil in data ?? Array.Empty<SoilComponentSaveData>())
            {
                if (soil == null)
                    continue;

                var component = new SoilComponent(
                    new WorldComponentId(soil.id),
                    new SiteId(soil.siteId),
                    new GridPosition(soil.positionX, soil.positionY),
                    soil.fertility,
                    soil.moisture,
                    new WorldComponentId(soil.plantId));
                store.Add(component.Id, component);
            }

            return store;
        }

        public static PlantComponentSaveData[] ToPlantComponentData(ComponentStore<PlantComponent> plants)
        {
            return (plants?.Rows ?? Array.Empty<System.Collections.Generic.KeyValuePair<WorldComponentId, PlantComponent>>())
                .Select(row => ToPlantComponentData(row.Value))
                .ToArray();
        }

        public static ComponentStore<PlantComponent> ToPlantComponentStore(PlantComponentSaveData[] data)
        {
            var store = new ComponentStore<PlantComponent>();
            foreach (var plant in data ?? Array.Empty<PlantComponentSaveData>())
            {
                if (plant == null)
                    continue;

                var component = new PlantComponent(
                    new WorldComponentId(plant.id),
                    new SiteId(plant.siteId),
                    new GridPosition(plant.positionX, plant.positionY),
                    plant.speciesId,
                    new PlantStageId(plant.stageId),
                    plant.daysInStage);
                store.Add(component.Id, component);
            }

            return store;
        }

        private static SoilComponentSaveData ToSoilComponentData(SoilComponent soil)
        {
            return new SoilComponentSaveData
            {
                id = soil.Id.Value,
                siteId = soil.SiteId.Value,
                positionX = soil.Position.X,
                positionY = soil.Position.Y,
                fertility = soil.Fertility,
                moisture = soil.Moisture,
                plantId = soil.PlantId.Value,
            };
        }

        private static PlantComponentSaveData ToPlantComponentData(PlantComponent plant)
        {
            return new PlantComponentSaveData
            {
                id = plant.Id.Value,
                siteId = plant.SiteId.Value,
                positionX = plant.Position.X,
                positionY = plant.Position.Y,
                speciesId = plant.SpeciesId,
                stageId = plant.StageId.Value,
                daysInStage = plant.DaysInStage,
            };
        }

        private static JobRequestSaveData ToJobRequestData(JobRequest request, JobBoard board)
        {
            return new JobRequestSaveData
            {
                id = request.Id.Value,
                recipeId = request.RecipeId.Value,
                siteId = request.SiteId.Value,
                positionX = request.WorksitePosition.X,
                positionY = request.WorksitePosition.Y,
                worksiteKind = (int)request.WorksiteKind,
                kind = (int)request.Kind,
                priority = request.Priority.Value,
                quantity = request.Quantity,
                requesterId = request.RequesterId.Value,
                claimedByActorId = board.GetClaimedBy(request.Id).Value,
                claimSequence = board.GetClaimSequence(request.Id),
            };
        }

        private static ActorSaveData[] ToActorStoreData(ActorStore store)
        {
            return (store?.Records ?? Array.Empty<ActorRecord>()).Select(ActorSaveMapper.ToData).ToArray();
        }

        private static ActorStore ToActorStore(ActorSaveData[] data)
        {
            var store = new ActorStore();
            foreach (var actor in data ?? Array.Empty<ActorSaveData>())
            {
                if (actor != null)
                    store.Add(ActorSaveMapper.ToActor(actor));
            }
            return store;
        }

        private static ItemRecordSaveData[] ToItemStoreData(ItemStore store)
        {
            return (store?.Records ?? Array.Empty<ItemRecord>()).Select(ToItemRecordData).ToArray();
        }

        private static ItemRecordSaveData ToItemRecordData(ItemRecord record)
        {
            return new ItemRecordSaveData
            {
                id = record.Id.Value,
                material = (int)record.Material,
                quality = (int)record.Quality,
                slot = (int)record.Slot,
                slotCode = record.Slot.Code,
            };
        }

        private static ItemStore ToItemStore(ItemRecordSaveData[] data)
        {
            var store = new ItemStore();
            foreach (var record in data ?? Array.Empty<ItemRecordSaveData>())
            {
                if (record != null)
                    store.Add(new ItemRecord(new ItemId(record.id), (ItemMaterial)record.material, (ItemQuality)record.quality, ToEquipmentSlot(record.slotCode, record.slot)));
            }
            return store;
        }

        private static SiteRecordSaveData[] ToSiteStoreData(SiteStore store)
        {
            return (store?.Records ?? Array.Empty<SiteRecord>()).Select(ToSiteRecordData).ToArray();
        }

        private static SiteRecordSaveData ToSiteRecordData(SiteRecord record)
        {
            return new SiteRecordSaveData
            {
                id = record.Id.Value,
                kind = (int)record.Kind,
                name = record.Name,
                minX = record.MinBound.X,
                minY = record.MinBound.Y,
                maxX = record.MaxBound.X,
                maxY = record.MaxBound.Y,
            };
        }

        private static SiteStore ToSiteStore(SiteRecordSaveData[] data)
        {
            var store = new SiteStore();
            foreach (var record in data ?? Array.Empty<SiteRecordSaveData>())
            {
                if (record != null)
                {
                    store.Add(new SiteRecord(
                        new SiteId(record.id),
                        (SiteKind)record.kind,
                        record.name,
                        new GridPosition(record.minX, record.minY),
                        new GridPosition(record.maxX, record.maxY)));
                }
            }
            return store;
        }

        private static WorksiteSaveData ToWorksiteData(WorksiteRecord record)
        {
            return new WorksiteSaveData
            {
                siteId = record.SiteId.Value,
                positionX = record.Position.X,
                positionY = record.Position.Y,
                kind = (int)record.Kind,
                isActive = record.IsActive,
            };
        }

        private static WorksiteRecord ToWorksiteRecord(WorksiteSaveData data)
        {
            return new WorksiteRecord(
                new SiteId(data.siteId),
                new GridPosition(data.positionX, data.positionY),
                (WorksiteKind)data.kind,
                data.isActive);
        }

        private static FactionRecordSaveData[] ToFactionStoreData(FactionStore store)
        {
            return (store?.Records ?? Array.Empty<FactionRecord>()).Select(ToFactionRecordData).ToArray();
        }

        private static FactionRecordSaveData ToFactionRecordData(FactionRecord record)
        {
            return new FactionRecordSaveData
            {
                id = record.Id.Value,
                name = record.Name,
                tags = record.Tags.ToArray(),
            };
        }

        private static FactionStore ToFactionStore(FactionRecordSaveData[] data)
        {
            var store = new FactionStore();
            foreach (var record in data ?? Array.Empty<FactionRecordSaveData>())
            {
                if (record != null)
                    store.Add(new FactionRecord(new FactionId(record.id), record.name, record.tags ?? Array.Empty<string>()));
            }
            return store;
        }

        private static FactionReputationSaveData[] ToFactionReputationData(FactionStore store)
        {
            return (store?.ReputationRows ?? Array.Empty<FactionReputationRow>())
                .Select(row => new FactionReputationSaveData
                {
                    a = row.A.Value,
                    b = row.B.Value,
                    reputation = row.Reputation.Value,
                })
                .ToArray();
        }

        private static void ApplyFactionReputations(FactionStore store, FactionReputationSaveData[] data)
        {
            if (store == null)
                return;

            foreach (var row in data ?? Array.Empty<FactionReputationSaveData>())
            {
                if (row == null || row.a == 0UL || row.b == 0UL || row.a == row.b)
                    continue;
                store.WithReputation(new FactionId(row.a), new FactionId(row.b), new FactionReputation(row.reputation));
            }
        }

        private static PriceLedgerSaveData[] ToPriceLedgerData(PriceLedger ledger)
        {
            return (ledger?.Entries ?? Array.Empty<PriceLedgerEntry>())
                .Select(row => new PriceLedgerSaveData
                {
                    siteId = row.SiteId.Value,
                    itemTag = row.ItemTag,
                    price = row.Price,
                })
                .ToArray();
        }

        private static PriceLedger ToPriceLedger(PriceLedgerSaveData[] data)
        {
            var ledger = new PriceLedger();
            foreach (var row in data ?? Array.Empty<PriceLedgerSaveData>())
            {
                if (row == null || row.siteId == 0UL || string.IsNullOrWhiteSpace(row.itemTag))
                    continue;
                ledger.SetPrice(new SiteId(row.siteId), row.itemTag, row.price);
            }

            return ledger;
        }

        private static StockpileSaveData[] ToStockpileData(IEnumerable<StockpileComponent> stockpiles)
        {
            return (stockpiles ?? Array.Empty<StockpileComponent>())
                .Where(stockpile => stockpile != null)
                .Select(stockpile => new StockpileSaveData
                {
                    siteId = stockpile.SiteId.Value,
                    entries = stockpile.Entries.Select(entry => new StockpileEntrySaveData
                    {
                        itemTag = entry.Key,
                        count = entry.Value,
                    }).ToArray(),
                })
                .ToArray();
        }

        private static List<StockpileComponent> ToStockpiles(StockpileSaveData[] data)
        {
            var stockpiles = new List<StockpileComponent>();
            foreach (var row in data ?? Array.Empty<StockpileSaveData>())
            {
                if (row == null || row.siteId == 0UL)
                    continue;
                var stockpile = new StockpileComponent(new SiteId(row.siteId));
                foreach (var entry in row.entries ?? Array.Empty<StockpileEntrySaveData>())
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.itemTag) || entry.count <= 0)
                        continue;
                    stockpile.Add(entry.itemTag, entry.count);
                }
                stockpiles.Add(stockpile);
            }

            return stockpiles;
        }

        private static TradeRouteSaveData[] ToTradeRouteData(IEnumerable<TradeRouteDef> routes)
        {
            return (routes ?? Array.Empty<TradeRouteDef>())
                .Where(route => route != null)
                .Select(route => new TradeRouteSaveData
                {
                    id = route.Id.Value,
                    originSiteId = route.OriginSiteId.Value,
                    destinationSiteId = route.DestinationSiteId.Value,
                    itemTag = route.ItemTag,
                    quantityPerCaravan = route.QuantityPerCaravan,
                    cadenceDays = route.CadenceDays,
                })
                .ToArray();
        }

        private static List<TradeRouteDef> ToTradeRoutes(TradeRouteSaveData[] data)
        {
            return (data ?? Array.Empty<TradeRouteSaveData>())
                .Where(row => row != null && row.id != 0UL)
                .Select(row => new TradeRouteDef(
                    new TradeRouteId(row.id),
                    new SiteId(row.originSiteId),
                    new SiteId(row.destinationSiteId),
                    row.itemTag,
                    row.quantityPerCaravan,
                    row.cadenceDays))
                .ToList();
        }

        private static CaravanSaveData[] ToCaravanData(IEnumerable<CaravanInstance> caravans)
        {
            return (caravans ?? Array.Empty<CaravanInstance>())
                .Where(caravan => caravan != null)
                .Select(caravan => new CaravanSaveData
                {
                    id = caravan.Id.Value,
                    routeId = caravan.RouteId.Value,
                    currentSiteId = caravan.CurrentSiteId.Value,
                    payloadRemaining = caravan.PayloadRemaining,
                    stepsSinceDeparture = caravan.StepsSinceDeparture,
                    stateCode = caravan.State.Code,
                })
                .ToArray();
        }

        private static List<CaravanInstance> ToCaravans(CaravanSaveData[] data)
        {
            return (data ?? Array.Empty<CaravanSaveData>())
                .Where(row => row != null && row.id != 0UL)
                .Select(row => new CaravanInstance(
                    new CaravanId(row.id),
                    new TradeRouteId(row.routeId),
                    new SiteId(row.currentSiteId),
                    row.payloadRemaining,
                    row.stepsSinceDeparture,
                    CaravanState.FromCode(row.stateCode)))
                .ToList();
        }

        private static WorldEventSaveData[] ToWorldEventLogData(WorldEventLog log)
        {
            return (log?.Events ?? Array.Empty<WorldEvent>()).Select(ToWorldEventData).ToArray();
        }

        private static WorldEventSaveData ToWorldEventData(WorldEvent worldEvent)
        {
            return new WorldEventSaveData
            {
                tickMinutes = worldEvent.Tick.TotalMinutes,
                kind = (int)worldEvent.Kind,
                actorId = worldEvent.ActorId.Value,
                siteId = worldEvent.SiteId.Value,
                reason = worldEvent.Reason,
                reasonTrace = worldEvent.ReasonTrace?.Causes.ToArray(),
            };
        }

        private static WorldEventLog ToWorldEventLog(WorldEventSaveData[] data)
        {
            var log = new WorldEventLog();
            foreach (var worldEvent in data ?? Array.Empty<WorldEventSaveData>())
            {
                if (worldEvent != null)
                {
                    log.Append(new WorldEvent(
                        new GameTime(worldEvent.tickMinutes),
                        (WorldEventKind)worldEvent.kind,
                        new ActorId(worldEvent.actorId),
                        new SiteId(worldEvent.siteId),
                        worldEvent.reason,
                        ToReasonTrace(worldEvent.reasonTrace)));
                }
            }
            return log;
        }

        private static ReasonTrace ToReasonTrace(string[] causes)
        {
            return causes == null || causes.Length == 0 ? null : new ReasonTrace(causes);
        }

        private static ToolCallTraceSaveData[] ToToolCallTraceData(IEnumerable<ToolCallTraceRecord> entries)
        {
            return (entries ?? Array.Empty<ToolCallTraceRecord>())
                .Where(entry => entry != null)
                .Select(entry => ToToolCallTraceData(entry.Tick, entry.SiteId, entry.Request, entry.Result))
                .ToArray();
        }

        private static WorldProfileSaveData ToWorldProfileData(WorldProfile profile)
        {
            if (profile == null) return null;
            return new WorldProfileSaveData
            {
                style = (int)profile.Style,
                genre = (int)profile.Genre,
                seed = profile.Seed,
                targetPopulation = profile.TargetPopulation,
                regionCount = profile.RegionCount,
                factionCount = profile.FactionCount,
                historyYears = profile.HistoryYears,
                moodKeyword = profile.MoodKeyword,
                playerCallingKeyword = profile.PlayerCallingKeyword,
                startLocationKeyword = profile.StartLocationKeyword,
            };
        }

        private static WorldProfile ToWorldProfile(WorldProfileSaveData data)
        {
            if (data == null) return null;
            return new WorldProfile(
                (WorldStyle)data.style,
                (WorldGenre)data.genre,
                data.seed,
                data.targetPopulation,
                data.regionCount,
                data.factionCount,
                data.historyYears,
                data.moodKeyword,
                data.playerCallingKeyword,
                data.startLocationKeyword);
        }

        private static ToolCallTraceSaveData ToToolCallTraceData(GameTime tick, SiteId siteId, ToolCallRequest request, ToolCallResult result)
        {
            return new ToolCallTraceSaveData
            {
                tickMinutes = tick.TotalMinutes,
                siteId = siteId.Value,
                surfaceCode = request?.Surface.Code,
                toolCode = request?.ToolId.Code,
                parameters = ToToolCallParameterData(request?.Parameters),
                accepted = result?.Accepted ?? false,
                payload = result?.Payload,
                rejectionReason = result?.RejectionReason,
            };
        }

        private static ToolCallParameterSaveData[] ToToolCallParameterData(IReadOnlyDictionary<string, string> parameters)
        {
            // Codex audit (second pass A-P3): the dictionary's enumeration order
            // is implementation-defined, so the on-disk parameter list could
            // shift between Save() calls for the same in-memory state. Sort
            // by parameter name (ordinal) for deterministic JSON output.
            return (parameters ?? new Dictionary<string, string>())
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => new ToolCallParameterSaveData { name = parameter.Key, value = parameter.Value })
                .ToArray();
        }

        private static List<ToolCallTraceRecord> ToToolCallTrace(ToolCallTraceSaveData[] data)
        {
            return (data ?? Array.Empty<ToolCallTraceSaveData>())
                .Where(row => row != null)
                .Select(row => new ToolCallTraceRecord(
                    new GameTime(row.tickMinutes < 0 ? 0 : row.tickMinutes),
                    new SiteId(row.siteId),
                    ToToolCallRequest(row),
                    new ToolCallResult(row.accepted, row.payload, row.rejectionReason)))
                .ToList();
        }

        private static ToolCallRequest ToToolCallRequest(ToolCallTraceSaveData row)
        {
            return new ToolCallRequest(
                new ToolId(string.IsNullOrWhiteSpace(row.toolCode) ? "unknown" : row.toolCode),
                ToolSurfaceKind.FromCode(row.surfaceCode),
                ToToolCallParameterDictionary(row.parameters));
        }

        private static Dictionary<string, string> ToToolCallParameterDictionary(ToolCallParameterSaveData[] parameters)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var parameter in parameters ?? Array.Empty<ToolCallParameterSaveData>())
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
                    continue;
                dictionary[parameter.name] = parameter.value ?? string.Empty;
            }

            return dictionary;
        }

        private static LlmProposalLogSaveData[] ToLlmProposalLogData(IEnumerable<LlmProposalLogEntry> entries)
        {
            return (entries ?? Array.Empty<LlmProposalLogEntry>())
                .Where(entry => entry != null)
                .Select(entry => new LlmProposalLogSaveData
                {
                    tickMinutes = entry.Tick.TotalMinutes,
                    providerCode = entry.Provider.Code,
                    conversationId = entry.ConversationId,
                    responseText = entry.ResponseText,
                    acceptedToolCalls = entry.AcceptedToolCalls
                        .Select(call => ToToolCallTraceData(entry.Tick, default, call, ToolCallResult.AcceptedWith("accepted")))
                        .ToArray(),
                    rejectedToolCalls = entry.RejectedToolCalls
                        .Select(rejection => new LlmRejectedToolCallSaveData
                        {
                            request = ToToolCallTraceData(entry.Tick, default, rejection.Request, ToolCallResult.Rejected(rejection.Reason)),
                            reason = rejection.Reason,
                        })
                        .ToArray(),
                })
                .ToArray();
        }

        private static List<LlmProposalLogEntry> ToLlmProposalLog(LlmProposalLogSaveData[] data)
        {
            return (data ?? Array.Empty<LlmProposalLogSaveData>())
                .Where(row => row != null)
                .Select(row => new LlmProposalLogEntry(
                    new GameTime(row.tickMinutes < 0 ? 0 : row.tickMinutes),
                    LlmProviderKind.FromCode(row.providerCode),
                    row.conversationId,
                    row.responseText,
                    (row.acceptedToolCalls ?? Array.Empty<ToolCallTraceSaveData>()).Select(ToToolCallRequest),
                    (row.rejectedToolCalls ?? Array.Empty<LlmRejectedToolCallSaveData>())
                        .Where(rejection => rejection != null && rejection.request != null)
                        .Select(rejection => new ToolCallRejection(ToToolCallRequest(rejection.request), rejection.reason))))
                .ToList();
        }

        private static NpcSeedSaveData[] ToNpcSeedData(IEnumerable<NpcSeedRecord> npcs)
        {
            return (npcs ?? Array.Empty<NpcSeedRecord>())
                .Where(npc => npc != null)
                .OrderBy(npc => npc.Id.Value)
                .Select(npc => new NpcSeedSaveData
                {
                    id = npc.Id.Value,
                    home = npc.Home.Value,
                    faction = npc.Faction.Value,
                    name = npc.Name,
                    birthYear = npc.BirthYear,
                    role = (int)npc.Role,
                    portraitAssetPath = npc.PortraitAssetPath,
                })
                .ToArray();
        }

        private static List<NpcSeedRecord> ToNpcSeeds(NpcSeedSaveData[] data)
        {
            return (data ?? Array.Empty<NpcSeedSaveData>())
                .Where(row => row != null
                    && row.id != 0UL
                    && row.home != 0UL
                    && row.faction != 0UL
                    && !string.IsNullOrWhiteSpace(row.name)
                    && row.role != (int)NpcRole.None)
                .OrderBy(row => row.id)
                .Select(row => new NpcSeedRecord(
                    new NpcId(row.id),
                    new SettlementId(row.home),
                    new FactionId(row.faction),
                    row.name,
                    row.birthYear,
                    (NpcRole)row.role,
                    row.portraitAssetPath))
                .ToList();
        }

        private static EquipmentSaveData ToEquipmentData(EquipmentState equipment)
        {
            // Codex audit (A/P2): the previous version hardcoded
            // `new[] { EquipmentSlot.Weapon }`, so any future slot (armor,
            // shield, ring, etc.) would be silently dropped at save time.
            // Use the new stable enumerator on EquipmentState so every
            // non-empty equipped slot makes it into the DTO, in canonical
            // slot-code order for deterministic JSON output.
            return new EquipmentSaveData
            {
                slots = equipment.EnumerateEquipped()
                    .Select(pair => new EquippedItemSaveData
                    {
                        slot = (int)pair.Key,
                        slotCode = pair.Key.Code,
                        itemId = pair.Value.Value,
                    })
                    .ToArray(),
            };
        }

        private static EquipmentState ToEquipmentState(EquipmentSaveData data)
        {
            var equipment = new EquipmentState();
            foreach (var slot in data?.slots ?? Array.Empty<EquippedItemSaveData>())
                equipment.Equip(ToEquipmentSlot(slot.slotCode, slot.slot), new ItemId(slot.itemId));
            return equipment;
        }

        private static EquipmentSlot ToEquipmentSlot(string code, int legacyValue)
        {
            return string.IsNullOrWhiteSpace(code)
                ? EquipmentSlot.FromLegacyValue(legacyValue)
                : EquipmentSlot.FromCode(code);
        }

        private static NpcMemorySaveData[] ToNpcMemoryData(NpcMemoryStore store)
        {
            return (store ?? new NpcMemoryStore()).GetAllSorted().Select(memory => new NpcMemorySaveData
            {
                actorId = memory.ActorId.Value,
                events = memory.Events.Select(ToInteractionEventData).ToArray(),
                dialogueSeen = memory.DialogueSeen.OrderBy(topicId => topicId).ToArray(),
                transactions = memory.Transactions.Select(ToTransactionData).ToArray(),
            }).ToArray();
        }

        private static NpcMemoryStore ToNpcMemoryStore(NpcMemorySaveData[] data)
        {
            var store = new NpcMemoryStore();
            store.ReplaceAll((data ?? Array.Empty<NpcMemorySaveData>()).Select(ToActorMemory));
            return store;
        }

        private static ActorMemory ToActorMemory(NpcMemorySaveData data)
        {
            var memory = new ActorMemory(new ActorId(data.actorId));
            memory.ReplaceEvents((data.events ?? Array.Empty<InteractionEventSaveData>()).Select(ToInteractionEvent));
            memory.ReplaceDialogueSeen(data.dialogueSeen);
            memory.ReplaceTransactions((data.transactions ?? Array.Empty<TransactionSaveData>()).Select(ToTransaction));
            return memory;
        }

        private static InteractionEventSaveData ToInteractionEventData(InteractionEvent interactionEvent)
        {
            return new InteractionEventSaveData
            {
                timestampMinutes = interactionEvent.Timestamp.TotalMinutes,
                eventType = interactionEvent.EventType,
                actorSeen = interactionEvent.ActorSeen.Value,
                subjectId = interactionEvent.SubjectId,
                itemTemplateId = interactionEvent.ItemTemplateId,
                amount = interactionEvent.Amount,
                locationX = interactionEvent.Location.X,
                locationY = interactionEvent.Location.Y,
            };
        }

        private static InteractionEvent ToInteractionEvent(InteractionEventSaveData data)
        {
            return new InteractionEvent(
                new GameTime(data.timestampMinutes),
                data.eventType,
                new ActorId(data.actorSeen),
                data.subjectId,
                data.itemTemplateId,
                data.amount,
                new GridPosition(data.locationX, data.locationY));
        }

        private static TransactionSaveData ToTransactionData(TransactionRecord transaction)
        {
            return new TransactionSaveData
            {
                timestampMinutes = transaction.Timestamp.TotalMinutes,
                transactionType = transaction.TransactionType,
                itemTemplateId = transaction.ItemTemplateId,
                count = transaction.Count,
                goldDelta = transaction.GoldDelta,
            };
        }

        private static TransactionRecord ToTransaction(TransactionSaveData data)
        {
            return new TransactionRecord(
                new GameTime(data.timestampMinutes),
                data.transactionType,
                data.itemTemplateId,
                data.count,
                data.goldDelta);
        }

        private static InventorySaveData ToInventoryData(InventoryState inventory)
        {
            return new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = inventory.Items.Select(ItemSaveMapper.ToData).ToArray(),
            };
        }

        private static InventoryState ToInventoryState(InventorySaveData inventory, int fallbackCapacity)
        {
            var state = new InventoryState(inventory?.capacity ?? fallbackCapacity);
            foreach (var item in inventory?.items ?? Array.Empty<ItemSaveData>())
                state.TryAdd(ItemSaveMapper.ToItem(item));
            return state;
        }
    }
}

// Design note:
// SliceSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: SliceWorldState or SliceSaveData snapshots.
// Outputs: round-trippable save objects with no UnityEngine in Domain/Simulation.
// Bible reference: PRD Sprint 1 FR-06, Sprint 2 FR-02 through FR-04.
using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between aggregate world state and JSON DTOs.</summary>
    public static class SliceSaveMapper
    {
        public static SliceSaveData ToData(SliceWorldState world)
        {
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
                player = ActorSaveMapper.ToData(world.Player),
                talker = ActorSaveMapper.ToData(world.Talker),
                merchant = ActorSaveMapper.ToData(world.Merchant),
                guard = ActorSaveMapper.ToData(world.Guard),
                enemy = ActorSaveMapper.ToData(world.Enemy),
                actors = ToActorStoreData(world.Actors),
                itemRecords = ToItemStoreData(world.Items),
                sites = ToSiteStoreData(world.Sites),
                factions = ToFactionStoreData(world.Factions),
                worldEvents = ToWorldEventLogData(world.Events),
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

        public static SliceWorldState ToWorld(SliceSaveData data)
        {
            var world = new SliceWorldFactory().Create(data.roomSeed);
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
                world.Player = ActorSaveMapper.ToActor(data.player);
                world.Talker = ActorSaveMapper.ToActor(data.talker);
                world.Merchant = ActorSaveMapper.ToActor(data.merchant);
                world.Guard = ActorSaveMapper.ToActor(data.guard);
                world.Enemy = ActorSaveMapper.ToActor(data.enemy);
            }
            world.Items = ToItemStore(data.itemRecords);
            world.Sites = ToSiteStore(data.sites);
            world.Factions = ToFactionStore(data.factions);
            world.Events = ToWorldEventLog(data.worldEvents);
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

        public static RecipeWorkOrderSaveData ToRecipeWorkOrderData(RecipeWorkOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new RecipeWorkOrderSaveData
            {
                recipeId = order.Recipe.Id.Value,
                siteId = order.SiteId.Value,
                positionX = order.Position.X,
                positionY = order.Position.Y,
                actorId = order.ActorId.Value,
                progressTicks = order.ProgressTicks,
            };
        }

        public static RecipeWorkOrder ToRecipeWorkOrder(RecipeWorkOrderSaveData data, Func<RecipeId, RecipeDef> resolveRecipe)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (resolveRecipe == null)
                throw new ArgumentNullException(nameof(resolveRecipe));

            var recipeId = new RecipeId(data.recipeId);
            var recipe = resolveRecipe(recipeId);
            if (recipe == null)
                throw new InvalidOperationException($"RecipeWorkOrder save data references unknown recipe {recipeId}.");

            return RecipeWorkOrder.Resume(
                recipe,
                new SiteId(data.siteId),
                new GridPosition(data.positionX, data.positionY),
                new ActorId(data.actorId),
                data.progressTicks);
        }

        public static JobRequestSaveData[] ToJobBoardData(JobBoard board)
        {
            return (board?.Requests ?? Array.Empty<JobRequest>()).Select(request => ToJobRequestData(request, board)).ToArray();
        }

        public static JobBoard ToJobBoard(JobRequestSaveData[] data)
        {
            var board = new JobBoard();
            foreach (var saved in data ?? Array.Empty<JobRequestSaveData>())
            {
                if (saved == null)
                    continue;

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

                var claimedBy = new ActorId(saved.claimedByActorId);
                if (!claimedBy.IsEmpty && !board.TryClaim(request.Id, claimedBy, out _))
                    throw new InvalidOperationException($"JobBoard save data could not restore claim for {request.Id}.");
            }

            return board;
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
            };
        }

        private static ItemStore ToItemStore(ItemRecordSaveData[] data)
        {
            var store = new ItemStore();
            foreach (var record in data ?? Array.Empty<ItemRecordSaveData>())
            {
                if (record != null)
                    store.Add(new ItemRecord(new ItemId(record.id), (ItemMaterial)record.material, (ItemQuality)record.quality, (EquipmentSlot)record.slot));
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

        private static EquipmentSaveData ToEquipmentData(EquipmentState equipment)
        {
            return new EquipmentSaveData
            {
                slots = new[] { EquipmentSlot.Weapon }
                    .Select(slot => new EquippedItemSaveData { slot = (int)slot, itemId = equipment.GetEquippedItemId(slot).Value })
                    .Where(slot => slot.itemId != 0UL)
                    .ToArray(),
            };
        }

        private static EquipmentState ToEquipmentState(EquipmentSaveData data)
        {
            var equipment = new EquipmentState();
            foreach (var slot in data?.slots ?? Array.Empty<EquippedItemSaveData>())
                equipment.Equip((EquipmentSlot)slot.slot, new ItemId(slot.itemId));
            return equipment;
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

// Design note:
// WorldSaveMapper translates between pure world state and Unity-serializable DTOs.
// Inputs: WorldState or WorldSaveData snapshots.
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
// EmberCrpg.Simulation. RecipeWorkOrder / WorldFactory rehydration moved
// to EmberCrpg.Simulation.Process.WorldSaveRehydration. WorldState
// construction is the caller's responsibility (overload taking the seed
// world, below).
namespace EmberCrpg.Data.Save
{
    /// <summary>Pure mapping layer between aggregate world state and JSON DTOs.</summary>
    public static partial class WorldSaveMapper
    {
        // EMB-012: current on-disk save schema version. Bump on any incompatible shape change and add
        // a migration branch in ToWorld; WorldSaveData.schemaVersion records the version a save was
        // written with so old saves can be detected and migrated rather than silently misread.
        public const int CurrentSchemaVersion = 1;

        public static WorldSaveData ToData(WorldState world)
        {
            // Codex audit (third pass A-P3): null world used to NRE inside the
            // initializer; throw a typed exception so callers can detect and
            // recover (e.g. surface a "save corrupt" status) rather than
            // crashing the save path.
            if (world == null) throw new ArgumentNullException(nameof(world));
            return new WorldSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                totalMinutes = (long)world.Time.TotalMinutes,
                roomSeed = (long)world.RoomSeed,
                currentRoomId = (long)world.CurrentRoomId,
                dungeonStartRoomId = (long)(world.Dungeon?.StartRoomId ?? 0),
                playerRoomId = (long)world.PlayerRoomId,
                talkerRoomId = (long)world.TalkerRoomId,
                merchantRoomId = (long)world.MerchantRoomId,
                guardRoomId = (long)world.GuardRoomId,
                enemyRoomId = (long)world.EnemyRoomId,
                pickupRoomId = (long)world.PickupRoomId,
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
                // SOUL-01: the production-economy process stores are now read from the world root
                // (not the JsonSliceSaveService side-stores). recipeWorkOrders stays a Simulation
                // type composed by the Presentation save bridge, so it is intentionally absent here.
                worksites = ToWorksiteData(world.Worksites),
                jobs = ToJobBoardData(world.Jobs),
                soils = ToSoilComponentData(world.Soils),
                plants = ToPlantComponentData(world.Plants),
                quests = ToQuestStoreData(world.Quests),
                worldEvents = ToWorldEventLogData(world.Events),
                toolCallTrace = ToToolCallTraceData(world.ToolCallTrace),
                llmProposalLog = ToLlmProposalLogData(world.LlmProposalLog),
                npcSeeds = ToNpcSeedData(world.NpcSeeds),
                worldProfile = ToWorldProfileData(world.WorldProfile),
inventory = ToInventoryData(world.PlayerInventory),
                playerEquipment = ToEquipmentData(world.PlayerEquipment),
                merchantInventory = ToInventoryData(world.MerchantInventory),
                playerLevel = world.PlayerLevel,
                playerXp = world.PlayerXp,
                playerKnownSpellIds = (world.PlayerKnownSpellIds ?? new List<string>()).ToArray(),
                playerGold = world.PlayerGold,
                merchantGold = world.MerchantGold,
                merchantStoreSeeded = world.MerchantStoreSeeded,
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

        public static WorldState ToWorld(WorldSaveData data, WorldState seedWorld)
        {
            var world = seedWorld ?? throw new ArgumentNullException(nameof(seedWorld));

            // EMB-012: detect schema drift. A save written by a NEWER build (version above what this
            // build understands) cannot be safely mapped field-by-field, so refuse it explicitly
            // rather than silently loading a half-mapped world. Legacy saves (schemaVersion 0, written
            // before the field existed) are treated as the v1 baseline and load normally.
            if (data.schemaVersion > CurrentSchemaVersion)
                throw new NotSupportedException(
                    "Save schema v" + data.schemaVersion + " is newer than this build supports (v" +
                    CurrentSchemaVersion + "). Update the game to load this save.");

            world.Time = new EmberCrpg.Domain.Core.GameTime(data.totalMinutes);
            if (data.dungeonRooms != null && data.dungeonRooms.Length > 0)
                world.Dungeon = DungeonSaveMapper.ToLayout((int)data.roomSeed, (int)data.dungeonStartRoomId, data.dungeonRooms, data.dungeonDoors, data.dungeonSpawns);
            world.CurrentRoomId = (int)data.currentRoomId;
            world.PlayerRoomId = (int)data.playerRoomId;
            world.TalkerRoomId = (int)data.talkerRoomId;
            world.MerchantRoomId = (int)data.merchantRoomId;
            world.GuardRoomId = (int)data.guardRoomId;
            world.EnemyRoomId = (int)data.enemyRoomId;
            world.PickupRoomId = (int)data.pickupRoomId;
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
            // SOUL-01: rehydrate the production-economy process stores onto the world root.
            world.Worksites = ToWorksiteStore(data.worksites);
            world.Jobs = ToJobBoard(data.jobs);
            world.Soils = ToSoilComponentStore(data.soils);
            world.Plants = ToPlantComponentStore(data.plants);
            world.Quests = ToQuestStore(data.quests);
            world.Events = ToWorldEventLog(data.worldEvents);
            world.ToolCallTrace = ToToolCallTrace(data.toolCallTrace);
            world.LlmProposalLog = ToLlmProposalLog(data.llmProposalLog);
            world.NpcSeeds = ToNpcSeeds(data.npcSeeds);
            world.WorldProfile = ToWorldProfile(data.worldProfile);
            // Digest-roundtrip finding: a seed world without inventories NREd here — same saver/loader
            // asymmetry as ToInventoryData; the save data's own capacity wins anyway when present.
            world.PlayerInventory = ToInventoryState(data.inventory, world.PlayerInventory?.Capacity ?? 0);
            world.PlayerEquipment = ToEquipmentState(data.playerEquipment);
            world.MerchantInventory = ToInventoryState(data.merchantInventory, world.MerchantInventory?.Capacity ?? 0);
            world.PlayerLevel = data.playerLevel > 0 ? data.playerLevel : Math.Max(1, world.PlayerLevel);
            world.PlayerXp = data.playerXp;
            world.PlayerKnownSpellIds = data.playerKnownSpellIds != null && data.playerKnownSpellIds.Length > 0
                ? new List<string>(data.playerKnownSpellIds)
                : world.PlayerKnownSpellIds ?? new List<string>();
            world.PlayerGold = data.playerGold;
            world.MerchantGold = data.merchantGold;
            world.MerchantStoreSeeded = data.merchantStoreSeeded;
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
    }
}

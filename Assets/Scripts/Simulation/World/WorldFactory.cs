// Design note:
// WorldFactory builds the smallest fully wired world state for the playable slice.
// Inputs: room seed.
// Outputs: deterministic room, actors, inventories, topics, and interaction state.
// Bible reference: PRD Sprint 1 FR-01 through FR-07, Sprint 2 FR-02 through FR-05.
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Magic;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// Creates the initial deterministic world snapshot for the vertical slice.
    ///
    /// EMB3-039 note: <see cref="Create"/> intentionally seeds a FIXED authored actor cast of five
    /// (Warden / Sage Nera / Quartermaster Ivo / Sentinel Rook / Ash Rat). This is by design for the
    /// slice, not a worldgen defect: the deterministic worldgen NPC population (WorldState.Actors /
    /// NpcSeeds) exists and ticks in the simulation, but projecting that full cast into the playable
    /// scenes — a runtime billboard spawner that instantiates one ActorView per generated NPC at its
    /// world->scene position — is tracked separately as SOUL-04 (see
    /// EmberCrpg.Presentation.Ember.Views.ActorView, "spawn-from-worldgen — STILL FLAGGED"). Until
    /// SOUL-04 lands, only this authored cast renders; do not mistake the small fixed cast for a bug.
    /// </summary>
    public sealed class WorldFactory
    {
        private readonly ProceduralRoomGenerator _rooms = new ProceduralRoomGenerator();
        private readonly MultiRoomDungeonGenerator _dungeons = new MultiRoomDungeonGenerator();
        private readonly WorldActorLoadoutFactory _actors = new WorldActorLoadoutFactory();

        public WorldState Create(int roomSeed, bool seedWorldAnchors = true)
        {
            var room = _rooms.Generate(roomSeed);
            var dungeon = _dungeons.Generate(roomSeed);
            var playerSpawn = dungeon.FindSpawn(DungeonSpawnKind.Player);
            var talkerSpawn = dungeon.FindSpawn(DungeonSpawnKind.Talker);
            var merchantSpawn = dungeon.FindSpawn(DungeonSpawnKind.Merchant);
            var guardSpawn = dungeon.FindSpawn(DungeonSpawnKind.Guard);
            var enemySpawn = dungeon.FindSpawn(DungeonSpawnKind.Enemy);
            var pickupSpawn = dungeon.FindSpawn(DungeonSpawnKind.Pickup);
            var talkTopics = new[] { "embers", "gate", "watch" };
            var world = new WorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);
            world.RoomSeed = roomSeed;
            world.Room = room;
            world.Dungeon = dungeon;
            world.CurrentRoomId = dungeon.StartRoomId;
            world.PlayerRoomId = playerSpawn.RoomId;
            world.TalkerRoomId = talkerSpawn.RoomId;
            world.MerchantRoomId = merchantSpawn.RoomId;
            world.GuardRoomId = guardSpawn.RoomId;
            world.EnemyRoomId = enemySpawn.RoomId;
            world.PickupRoomId = pickupSpawn.RoomId;
            world.DungeonRoomStates = dungeon.Rooms.Select(roomNode => new DungeonRoomState(roomNode.Id, roomNode.Id == dungeon.StartRoomId, false)).ToList();
            world.DungeonDoorStates = dungeon.Doors.Select(door => new DungeonDoorState(door.Id, door.StartsOpen)).ToList();
            if (seedWorldAnchors)
                SeedWorldAnchors(world);
            // EMB3-039: fixed authored cast (5 actors). This is the slice's intentional hand-placed cast,
            // NOT the generated worldgen population. Projecting the full generated NPC set into scenes is
            // tracked as SOUL-04 (runtime ActorView spawner); see the class summary above.
            world.ReplaceActorView(ActorRole.Player, _actors.Create(new ActorId(1), "Warden", ActorRole.Player, playerSpawn.Position));
            world.ReplaceActorView(ActorRole.Talker, _actors.Create(new ActorId(2), "Sage Nera", ActorRole.Talker, talkerSpawn.Position, talkTopics));
            world.ReplaceActorView(ActorRole.Merchant, _actors.Create(new ActorId(3), "Quartermaster Ivo", ActorRole.Merchant, merchantSpawn.Position));
            world.ReplaceActorView(ActorRole.Guard, _actors.Create(new ActorId(4), "Sentinel Rook", ActorRole.Guard, guardSpawn.Position));
            world.ReplaceActorView(ActorRole.Enemy, _actors.Create(new ActorId(5), "Ash Rat", ActorRole.Enemy, enemySpawn.Position));
            world.PlayerInventory = new InventoryState(10);
            world.PlayerEquipment = new EquipmentState();
            world.PlayerLevel = 1;
            world.PlayerKnownSpellIds = new List<string>
            {
                WorldSpellCatalog.FlameBoltTemplateId,
                WorldSpellCatalog.MendingTouchTemplateId,
            };
            var startingBlade = WorldItemCatalog.CreateAshTrainingBlade();
            world.PlayerInventory.TryAdd(startingBlade);
            // F16: the starting kit comes EQUIPPED — its +5 acc/+2 dmg finally enter the dice (the
            // blade sat inert in the backpack since Sprint 1). PlayerEquipment persists via the mapper.
            world.PlayerEquipment.Equip(EquipmentSlot.Weapon, startingBlade.Id);
            world.MerchantInventory = new InventoryState(32);
            world.MerchantInventory.TryAdd(WorldItemCatalog.CreateGateWrit());
            world.PlayerGold = 240;
            world.MerchantGold = 1200;
            world.MerchantStoreSeeded = false;
            world.Pickups = new List<RoomPickup>
            {
                new RoomPickup(WorldItemCatalog.CreateEmberShard(), pickupSpawn.Position),
            };
            world.Topics = new List<AskAboutTopic>
            {
                new AskAboutTopic("embers", "Embers", "The embers in this room never fully die; they mark old warding lines."),
                new AskAboutTopic("gate", "Gate", "Quartermaster Ivo still issues writs for the south door, but Sentinel Rook honors only sealed paper."),
                new AskAboutTopic("watch", "Watch", "Sentinel Rook keeps count of every footstep, including yours."),
            };
            world.DoorOpen = false;
            world.GuardDoorAccessGranted = false;
            world.GuardWarningCount = 0;
            world.LastNarrative = "Open inventory with I, equip the Ash Training Blade with Z, then pick up the Ember Shard and work the south door loop.";
            return world;
        }

        private static void SeedWorldAnchors(WorldState world)
        {
            AddSite(world, 1, SiteKind.Settlement, "Furnace", 0, 0, 2, 2);
            AddSite(world, 2, SiteKind.Settlement, "Hearth", 3, 0, 5, 2);
            AddSite(world, 3, SiteKind.Region, "HarvestShed", 6, 0, 8, 2);
            AddSite(world, 4, SiteKind.Settlement, "Caravan", 9, 0, 11, 2);
            AddSite(world, 5, SiteKind.Settlement, "Stall", 12, 0, 14, 2);
            AddSite(world, 6, SiteKind.Dungeon, "Chest", 15, 0, 17, 2);
            AddSite(world, 7, SiteKind.Dungeon, "Effigy", 18, 0, 20, 2);
            AddSite(world, 8, SiteKind.Settlement, "Forge", 21, 0, 23, 2);

            var forge = new FactionId(1);
            var harbor = new FactionId(2);
            var watch = new FactionId(3);
            world.Factions.Add(new FactionRecord(forge, "Forge Guild", new[] { "craft", "smith" }));
            world.Factions.Add(new FactionRecord(harbor, "Harbor Merchants", new[] { "trade", "caravan" }));
            world.Factions.Add(new FactionRecord(watch, "City Watch", new[] { "law", "guard" }));
            world.Factions.WithReputation(forge, harbor, new FactionReputation(12));
            world.Factions.WithReputation(forge, watch, new FactionReputation(4));
            world.Factions.WithReputation(harbor, watch, new FactionReputation(8));

            var furnaceStock = new StockpileComponent(new SiteId(1));
            furnaceStock.Add("iron", 8);
            var stallStock = new StockpileComponent(new SiteId(5));
            stallStock.Add("coin", 100);
            // CAN SUYU H1: the LARDER — a settlement starts with a food store and working fields,
            // or the consumption loop starves before the first harvest. This seeding used to live
            // presentation-side (HydrateSites) — world content belongs to the world factory.
            stallStock.Add("wheat", 150);
            world.Stockpiles.Add(furnaceStock);
            world.Stockpiles.Add(stallStock);
            for (ulong plot = 1; plot <= 3; plot++)
            {
                var plotId = new WorldComponentId(9000 + plot);
                world.Plants.Add(plotId, new PlantComponent(
                    plotId, new SiteId(5),
                    new GridPosition(6 + (int)plot, 4), "wheat",
                    new PlantStageId("seed"), 0));
            }
            world.Prices.SetPrice(new SiteId(1), "iron", 10);

            var route = new TradeRouteDef(new TradeRouteId(1), new SiteId(1), new SiteId(5), "iron", 2, 2);
            world.TradeRoutes.Add(route);
            world.Caravans.Add(new CaravanInstance(new CaravanId(1), route.Id, route.OriginSiteId, 0, 0, CaravanState.EnRoute));
        }

        /// <summary>DF KALIBRASYONU: the authored 5-actor slice cannot carry Dwarf Fortress
        /// gate thresholds (3 civilians = no crowd, no witness pool, no visible economy). This
        /// seeds a deterministic village cast - villagers with homes south of the site row and
        /// day anchors spread across it, a second watchman, and a roaming hunter that commutes
        /// into town (unpinned - predation with witnesses in the plaza, not in a far dungeon).
        /// Gates call this; production worldgen has its own population (SOUL-04).</summary>
        public static void SeedVillagers(WorldState world, int villagers = 9)
        {
            if (world?.Actors == null) return;
            var loadout = new WorldActorLoadoutFactory();
            string[] names =
            {
                "Miller Besk", "Weaver Sanna", "Smith Corvo", "Herbalist Fenn", "Carter Jole",
                "Baker Ede", "Mason Torv", "Fisher Lysa", "Tanner Brik", "Chandler Wex",
                "Cooper Anz", "Scribe Ollo",
            };
            for (int i = 0; i < villagers && i < names.Length; i++)
            {
                var home = new GridPosition(1 + i * 2, 5);
                var anchor = new GridPosition(1 + i * 2, 1); // unique per villager — Gate8 forbids stacked idlers
                world.Actors.Add(loadout
                    .Create(new ActorId(100UL + (ulong)i), names[i], ActorRole.Talker, home)
                    .WithHomeAndAnchor(home, anchor));
            }

            world.Actors.Add(loadout
                .Create(new ActorId(150), "Watchman Serel", ActorRole.Guard, new GridPosition(10, 1))
                .WithHomeAndAnchor(new GridPosition(10, 4), new GridPosition(12, 1)));
            world.Actors.Add(loadout
                .Create(new ActorId(151), "Gaunt Hound", ActorRole.Enemy, new GridPosition(20, 6))
                .WithHomeAndAnchor(new GridPosition(20, 6), new GridPosition(18, 1)));
        }

        private static void AddSite(WorldState world, ulong id, SiteKind kind, string name, int minX, int minY, int maxX, int maxY)
        {
            world.Sites.Add(new SiteRecord(new SiteId(id), kind, name, new GridPosition(minX, minY), new GridPosition(maxX, maxY)));
        }
    }
}

// Design note:
// SliceWorldFactory builds the smallest fully wired world state for Sprint 1 from pure services.
// Inputs: room seed.
// Outputs: deterministic room, actors, topics, inventory, and pickup state.
// Bible reference: PRD FR-01 through FR-07.
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.World
{
    /// <summary>Creates the initial deterministic world snapshot for the vertical slice.</summary>
    public sealed class SliceWorldFactory
    {
        private readonly ProceduralRoomGenerator _rooms = new ProceduralRoomGenerator();

        public SliceWorldState Create(int roomSeed)
        {
            var room = _rooms.Generate(roomSeed);
            var talkTopics = new[] { "embers", "gate", "watch" };
            var world = new SliceWorldState();
            world.Time = new GameTime(8 * GameTime.MinutesPerHour);
            world.RoomSeed = roomSeed;
            world.Room = room;
            world.Player = CreateActor(new ActorId(1), "Warden", ActorRole.Player, room.PlayerSpawn, 62, 18, 12, 2, 7, null);
            world.Talker = CreateActor(new ActorId(2), "Sage Nera", ActorRole.Talker, room.TalkerSpawn, 28, 6, 6, 0, 1, talkTopics);
            world.Merchant = CreateActor(new ActorId(3), "Quartermaster Ivo", ActorRole.Merchant, room.MerchantSpawn, 34, 8, 8, 1, 1, null);
            world.Guard = CreateActor(new ActorId(4), "Sentinel Rook", ActorRole.Guard, room.GuardSpawn, 44, 10, 10, 3, 3, null);
            world.Enemy = CreateActor(new ActorId(5), "Ash Rat", ActorRole.Enemy, room.EnemySpawn, 38, 11, 6, 1, 4, null);
            world.PlayerInventory = new InventoryState(10);
            world.Pickups = new List<RoomPickup>
            {
                new RoomPickup(new InventoryItem(new ItemId(1001), "ember_shard", "Ember Shard", 1), room.PickupSpawn),
            };
            world.Topics = new List<AskAboutTopic>
            {
                new AskAboutTopic("embers", "Embers", "The embers in this room never fully die; they mark old warding lines."),
                new AskAboutTopic("gate", "Gate", "The south wall door was sealed after the last tunnel collapse, but the hinge still moves."),
                new AskAboutTopic("watch", "Watch", "Sentinel Rook keeps count of every footstep, including yours."),
            };
            world.LastNarrative = "Explore the room, ask Sage Nera about a topic, then test the encounter turn loop.";
            return world;
        }

        private static ActorRecord CreateActor(ActorId id, string name, ActorRole role, GridPosition position, int mig, int accuracy, int dodge, int armor, int baseDamage, IEnumerable<string> topics)
        {
            var stats = new EmberStatBlock(mig, 50, 48, 44, 42, 40);
            var vitals = new ActorVitals(new VitalStat(24, 24), new VitalStat(18, 18), new VitalStat(12, 12));
            return new ActorRecord(id, name, role, stats, vitals, position, accuracy, dodge, armor, baseDamage, topics);
        }
    }
}

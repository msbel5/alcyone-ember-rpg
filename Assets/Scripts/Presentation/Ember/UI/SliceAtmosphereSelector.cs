using System.Linq;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Actors;

// Design note:
// SliceAtmosphereSelector maps deterministic dungeon state into audio cue ids for presentation hooks.
// Inputs: generated room template, spawn placement, visited/cleared state, combat, and door state.
// Outputs: ambience/music/SFX cue ids; it performs no playback and mutates no simulation state.
// Bible reference: Sprint 4 Phase 5 audio/atmosphere hooks and deterministic dungeon-state variation.
namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>Pure presentation selector for room/state atmosphere cue ids.</summary>
    public static class SliceAtmosphereSelector
    {
        public static SliceAtmosphereCueSet Select(SliceWorldState world)
        {
            if (world == null || world.Dungeon == null)
                return SliceAtmosphereCueSet.Silent("no-world");

            var room = world.Dungeon.Rooms.FirstOrDefault(candidate => candidate.Id == world.CurrentRoomId);
            if (room == null)
                return SliceAtmosphereCueSet.Silent("missing-room");

            var roomState = world.DungeonRoomStates.FirstOrDefault(candidate => candidate.RoomId == room.Id);
            var ambience = SelectAmbience(room.TemplateId, roomState);
            var music = SelectMusic(world, roomState);
            var sfx = SelectSfx(world, room);
            var reason = $"room:{room.Id} template:{room.TemplateId} visited:{roomState?.Visited ?? false} combat:{IsCombatThreat(world)}";
            return new SliceAtmosphereCueSet(ambience, music, sfx, reason);
        }

        private static string SelectAmbience(string templateId, DungeonRoomState roomState)
        {
            var baseCue = "ambience.ember-hall";
            if (templateId == "ash-cell")
                baseCue = "ambience.ash-cell";
            else if (!string.IsNullOrEmpty(templateId) && templateId.StartsWith("watch-node"))
                baseCue = "ambience.watch-node";

            if (roomState != null && roomState.Cleared)
                return baseCue + ".cleared";
            if (roomState != null && !roomState.Visited)
                return baseCue + ".unvisited";
            return baseCue;
        }

        private static string SelectMusic(SliceWorldState world, DungeonRoomState roomState)
        {
            if (IsCombatThreat(world))
                return "music.combat.low";
            if (!world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.IsDepleted && world.CurrentRoomId == world.EnemyRoomId)
                return "music.tension.enemy-near";
            if (roomState != null && roomState.Cleared)
                return "music.dungeon.resolved";
            if (world.CurrentRoomId == world.MerchantRoomId || world.CurrentRoomId == world.TalkerRoomId)
                return "music.dungeon.quiet-npc";
            return "music.dungeon.explore";
        }

        private static string SelectSfx(SliceWorldState world, DungeonRoom room)
        {
            if (IsCombatThreat(world))
                return "sfx.combat-pulse";
            if (HasClosedDoor(world, room))
                return "sfx.closed-door-pressure";
            if (world.CurrentRoomId == world.PickupRoomId && world.Pickups.Any(pickup => !pickup.IsCollected))
                return "sfx.pickup-ember-hum";
            if (world.CurrentRoomId == world.GuardRoomId && !world.GuardDoorAccessGranted)
                return "sfx.guard-watch";
            return "sfx.none";
        }

        private static bool IsCombatThreat(SliceWorldState world)
        {
            return world.EncounterActive && !world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.IsDepleted && world.CurrentRoomId == world.EnemyRoomId;
        }

        private static bool HasClosedDoor(SliceWorldState world, DungeonRoom room)
        {
            return room.DoorIds.Any(doorId => world.DungeonDoorStates.Any(state => state.DoorId == doorId && !state.Open));
        }
    }
}

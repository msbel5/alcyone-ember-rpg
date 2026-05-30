using System.Linq;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 4 Phase 5's deterministic room/state-to-cue mapping.
// They cover room-template variation, combat/door state cues, and HUD-visible fallback output.
namespace EmberCrpg.Tests.EditMode.Presentation
{
    /// <summary>Verifies atmosphere hooks without requiring Unity audio assets or playback.</summary>
    public sealed class EmberAtmosphereSelectorTests
    {
        [Test]
        public void Select_StartRoom_UsesRoomTemplateAndClosedDoorCues()
        {
            var world = new WorldFactory().Create(1337);
            var room = world.Dungeon.FindRoom(world.CurrentRoomId);

            var cues = EmberAtmosphereSelector.Select(world);

            Assert.That(cues.AmbienceId, Does.StartWith(ExpectedAmbiencePrefix(room.TemplateId)));
            Assert.That(cues.MusicId, Is.EqualTo("music.dungeon.explore"));
            Assert.That(cues.SfxId, Is.EqualTo("sfx.closed-door-pressure"));
            Assert.That(cues.Reason, Does.Contain($"room:{world.CurrentRoomId}"));
        }

        [Test]
        public void Select_CurrentEnemyRoomWithCombatActive_UsesCombatMusicAndSfx()
        {
            var world = new WorldFactory().Create(1337);
            world.CurrentRoomId = world.EnemyRoomId;
            world.EncounterActive = true;

            var cues = EmberAtmosphereSelector.Select(world);

            Assert.That(cues.MusicId, Is.EqualTo("music.combat.low"));
            Assert.That(cues.SfxId, Is.EqualTo("sfx.combat-pulse"));
        }

        [Test]
        public void Select_UnvisitedAndClearedRoomStates_VaryAmbienceDeterministically()
        {
            var world = new WorldFactory().Create(1337);
            var alternateRoom = world.Dungeon.Rooms.First(room => room.Id != world.CurrentRoomId);
            var state = world.DungeonRoomStates.First(candidate => candidate.RoomId == alternateRoom.Id);
            world.CurrentRoomId = alternateRoom.Id;
            state.Visited = false;

            var unvisited = EmberAtmosphereSelector.Select(world);
            state.Visited = true;
            state.Cleared = true;
            var cleared = EmberAtmosphereSelector.Select(world);

            Assert.That(unvisited.AmbienceId, Does.EndWith(".unvisited"));
            Assert.That(cleared.AmbienceId, Does.EndWith(".cleared"));
            Assert.That(cleared.MusicId, Is.EqualTo("music.dungeon.resolved"));
        }

        [Test]
        public void FormatHud_ShowsCurrentAtmosphereForDebugValidation()
        {
            var world = new WorldFactory().Create(1337);
            var cues = EmberAtmosphereSelector.Select(world);

            var hud = EmberHudFormatter.Format(world, "/tmp/save.json", "ready", "none", cues);

            Assert.That(hud, Does.Contain("Atmosphere:"));
            Assert.That(hud, Does.Contain(cues.AmbienceId));
            Assert.That(hud, Does.Contain(cues.MusicId));
            Assert.That(hud, Does.Contain(cues.SfxId));
        }

        private static string ExpectedAmbiencePrefix(string templateId)
        {
            if (templateId == "ash-cell")
                return "ambience.ash-cell";
            if (templateId.StartsWith("watch-node"))
                return "ambience.watch-node";
            return "ambience.ember-hall";
        }
    }
}

using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// This is the Faz 1 PLAYABLE-box deterministic replay proof. It does not add a
// new gameplay system; it stitches the landed store roots, world-event log,
// NPC memory, and JSON save/load rail into the roadmap acceptance sentence.
namespace EmberCrpg.Tests.EditMode.World
{
    public sealed class Faz1AcceptanceReplayTests
    {
        [Test]
        public void Replay_GuardTalkMemoryAndSecondSiteSurviveSaveLoad()
        {
            var world = new SliceWorldFactory().Create(3110);
            var startSiteId = new SiteId(1001);
            var secondSiteId = new SiteId(1002);
            var firstRoomId = world.CurrentRoomId;
            var guardedDoor = world.Dungeon.Doors.First(door => door.RequiresGuardClearance && (door.FromRoomId == firstRoomId || door.ToRoomId == firstRoomId));
            var secondRoomId = guardedDoor.OtherRoom(firstRoomId);

            world.Sites.Add(new SiteRecord(startSiteId, SiteKind.Dungeon, "Ember Gatehouse", new GridPosition(0, 0), new GridPosition(12, 12)));
            world.Sites.Add(new SiteRecord(secondSiteId, SiteKind.Settlement, "Ashford Approach", new GridPosition(13, 0), new GridPosition(24, 12)));
            world.Events.Append(new WorldEvent(
                world.Time,
                WorldEventKind.ActorSpawned,
                world.Guard.Id,
                startSiteId,
                "faz1-acceptance-spawn-guard",
                new ReasonTrace(new[] { "factory-seed-3110", "store-backed-guard" })));

            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            var firstTalk = new GuardInteractionService().Interact(world);
            world.Events.Append(new WorldEvent(
                world.Time,
                WorldEventKind.ActorTalked,
                world.Guard.Id,
                startSiteId,
                "faz1-acceptance-first-guard-talk",
                new ReasonTrace(new[] { "player-command", "guard-memory-passage-request" })));

            var save = new JsonSliceSaveService();
            var loaded = save.LoadFromJson(save.SaveToJson(world));
            var rememberedTalk = new GuardInteractionService().Interact(loaded);
            Assert.That(loaded.PlayerInventory.TryAdd(SliceItemCatalog.CreateGateWrit()), Is.True);
            var clearanceTalk = new GuardInteractionService().Interact(loaded);
            loaded.Player.MoveTo(new GridPosition(loaded.Room.DoorCell.X, 1));
            var doorToggle = new DoorInteractionService().Toggle(loaded);
            var traversal = new DungeonTraversalService().Traverse(loaded, guardedDoor.Id);
            loaded.Events.Append(new WorldEvent(
                loaded.Time.AddMinutes(10),
                WorldEventKind.SiteEntered,
                loaded.Player.Id,
                secondSiteId,
                "faz1-acceptance-enter-second-site",
                new ReasonTrace(new[] { "save-load", "guard-memory-confirmed", "walk-to-second-site" })));

            var final = save.LoadFromJson(save.SaveToJson(loaded));

            Assert.That(firstTalk, Does.Contain("No writ"));
            Assert.That(rememberedTalk, Does.Contain("remembers your first unwrit request"));
            Assert.That(clearanceTalk, Does.Contain("grants clearance"));
            Assert.That(doorToggle, Does.Contain("grinds open"));
            Assert.That(traversal, Does.Contain($"room {secondRoomId}"));
            Assert.That(final.Guard.Id, Is.EqualTo(world.Guard.Id));
            Assert.That(final.NpcMemory.TryGet(final.Guard.Id, out var guardMemory), Is.True);
            Assert.That(guardMemory.CountEvents(ActorMemoryEventTypes.PassageRequested), Is.EqualTo(3));
            Assert.That(final.CurrentRoomId, Is.EqualTo(secondRoomId));
            Assert.That(final.DungeonRoomStates.First(state => state.RoomId == secondRoomId).Visited, Is.True);
            Assert.That(final.Sites.Get(secondSiteId).Name, Is.EqualTo("Ashford Approach"));
            Assert.That(final.Events.Events.Select(worldEvent => worldEvent.Kind), Is.EqualTo(new[]
            {
                WorldEventKind.ActorSpawned,
                WorldEventKind.ActorTalked,
                WorldEventKind.SiteEntered,
            }));
            Assert.That(final.Events.Events.Last().ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "save-load",
                "guard-memory-confirmed",
                "walk-to-second-site",
            }));
        }
    }
}

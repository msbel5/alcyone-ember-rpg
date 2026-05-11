using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// Pins the Faz 1 TIME-box save/load rail for canonical runtime store roots.
// The mapper still writes legacy slice actor fields for migration safety, but
// store arrays are now the preferred source for ActorStore/ItemStore/SiteStore/
// FactionStore/WorldEventLog round-trips.
namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class StoreRoundTripTests
    {
        [Test]
        public void SaveAndLoad_RoundTripsCanonicalStoreRootsAndEventLog()
        {
            var world = new SliceWorldFactory().Create(2112);
            var guard = MakeRecord(44, "Bridge Guard", ActorRole.Guard, new GridPosition(9, 4));
            var siteId = new SiteId(20);
            var itemId = new ItemId(700);
            var factionId = new FactionId(30);

            world.Guard = guard;
            world.Items.Add(new ItemRecord(itemId, ItemMaterial.Iron, ItemQuality.Fine, EquipmentSlot.Weapon));
            world.Sites.Add(new SiteRecord(siteId, SiteKind.Settlement, "Ashford", new GridPosition(5, 2), new GridPosition(12, 8)));
            world.Factions.Add(new FactionRecord(factionId, "Ash Guild", new[] { "guild", "ember" }));
            world.Events.Append(new WorldEvent(
                new GameTime(321),
                WorldEventKind.ActorSpawned,
                guard.Id,
                siteId,
                "faz1-store-roundtrip",
                new ReasonTrace(new[] { "seed", "spawn-guard" })));

            var service = new JsonSliceSaveService();
            var loaded = service.LoadFromJson(service.SaveToJson(world));

            Assert.That(loaded.Actors.Count, Is.EqualTo(world.Actors.Count));
            Assert.That(loaded.Guard.Id, Is.EqualTo(guard.Id));
            Assert.That(loaded.Guard.Name, Is.EqualTo("Bridge Guard"));
            Assert.That(loaded.Guard.Position, Is.EqualTo(new GridPosition(9, 4)));

            Assert.That(loaded.Items.Count, Is.EqualTo(1));
            var item = loaded.Items.Get(itemId);
            Assert.That(item.Material, Is.EqualTo(ItemMaterial.Iron));
            Assert.That(item.Quality, Is.EqualTo(ItemQuality.Fine));
            Assert.That(item.Slot, Is.EqualTo(EquipmentSlot.Weapon));

            Assert.That(loaded.Sites.Count, Is.EqualTo(1));
            var site = loaded.Sites.Get(siteId);
            Assert.That(site.Kind, Is.EqualTo(SiteKind.Settlement));
            Assert.That(site.Name, Is.EqualTo("Ashford"));
            Assert.That(site.Contains(new GridPosition(12, 8)), Is.True);

            Assert.That(loaded.Factions.Count, Is.EqualTo(1));
            var faction = loaded.Factions.Get(factionId);
            Assert.That(faction.Name, Is.EqualTo("Ash Guild"));
            Assert.That(faction.Tags, Is.EqualTo(new[] { "guild", "ember" }));

            Assert.That(loaded.Events.Count, Is.EqualTo(1));
            var worldEvent = loaded.Events.Events.Single();
            Assert.That(worldEvent.Tick.TotalMinutes, Is.EqualTo(321));
            Assert.That(worldEvent.Kind, Is.EqualTo(WorldEventKind.ActorSpawned));
            Assert.That(worldEvent.ActorId, Is.EqualTo(guard.Id));
            Assert.That(worldEvent.SiteId, Is.EqualTo(siteId));
            Assert.That(worldEvent.Reason, Is.EqualTo("faz1-store-roundtrip"));
            Assert.That(worldEvent.ReasonTrace.Causes, Is.EqualTo(new[] { "seed", "spawn-guard" }));
        }

        [Test]
        public void Mapper_PrefersCanonicalActorStoreWhenSaveDataCarriesLegacyAndStoreActors()
        {
            var world = new SliceWorldFactory().Create(17);
            var data = SliceSaveMapper.ToData(world);
            var canonicalGuard = MakeRecord(404, "Canonical Guard", ActorRole.Guard, new GridPosition(3, 7));
            data.actors = data.actors
                .Where(actor => actor.role != (int)ActorRole.Guard)
                .Concat(new[] { ActorSaveMapper.ToData(canonicalGuard) })
                .ToArray();

            var loaded = SliceSaveMapper.ToWorld(data);

            Assert.That(loaded.Guard.Id, Is.EqualTo(canonicalGuard.Id));
            Assert.That(loaded.Guard.Name, Is.EqualTo("Canonical Guard"));
            Assert.That(loaded.Guard.Position, Is.EqualTo(new GridPosition(3, 7)));
        }

        private static ActorRecord MakeRecord(ulong id, string name, ActorRole role, GridPosition position)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(10, 10),
                    new VitalStat(10, 10),
                    new VitalStat(10, 10)),
                position,
                accuracy: 50,
                dodge: 10,
                armor: 0,
                baseDamage: 1);
        }
    }
}

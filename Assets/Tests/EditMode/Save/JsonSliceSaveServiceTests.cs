using EmberCrpg.Data.Save;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin JSON save/load round-trips for the Sprint 1 world snapshot.
// They cover deterministic data preservation, not filesystem IO.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies JSON round-tripping of the vertical slice state.</summary>
    public sealed class JsonSliceSaveServiceTests
    {
        [Test]
        public void SaveAndLoad_RoundTripsInventoryTopicsAndEnemyHealth()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.PlayerInventory.TryAdd(world.Pickups[0].Item);
            world.Pickups[0].Collect();
            new AskAboutService().Ask(world, "embers");
            world.Enemy.ApplyVitals(world.Enemy.Vitals.WithHealth(world.Enemy.Vitals.Health.Damage(5)));

            var service = new JsonSliceSaveService();
            var json = service.SaveToJson(world);
            var loaded = service.LoadFromJson(json);

            Assert.That(loaded.PlayerInventory.Contains("ember_shard"), Is.True);
            Assert.That(loaded.Talker.AskedTopicIds, Does.Contain("embers"));
            Assert.That(loaded.Enemy.Vitals.Health.Current, Is.EqualTo(world.Enemy.Vitals.Health.Current));
        }
    }
}

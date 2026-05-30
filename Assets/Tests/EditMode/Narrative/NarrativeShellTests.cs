using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic Ask About and Think shells (the Ask DM
// shell was removed in ARCH-03; the live DM path is the validated tool router).
// They intentionally avoid live AI dependencies.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies Sprint 1 narrative shell outputs.</summary>
    public sealed class NarrativeShellTests
    {
        [Test]
        public void AskAbout_FirstQuestion_UsesTopicAnswer()
        {
            var world = new WorldFactory().Create(1337);
            var reply = new AskAboutService().Ask(world, "embers");
            Assert.That(reply, Does.Contain("embers").IgnoreCase);
        }

        [Test]
        public void Think_PrefersNearbyPickupWhenInventoryHasSpace()
        {
            var world = new WorldFactory().Create(1337);
            var reply = new ThinkService().Think(world);
            Assert.That(reply, Does.Contain("Ember Shard"));
        }
    }
}

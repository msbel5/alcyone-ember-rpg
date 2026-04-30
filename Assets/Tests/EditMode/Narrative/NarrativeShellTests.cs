using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic Ask About, Ask DM, and Think shells.
// They intentionally avoid live AI dependencies.
namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Verifies Sprint 1 narrative shell outputs.</summary>
    public sealed class NarrativeShellTests
    {
        [Test]
        public void AskAbout_FirstQuestion_UsesTopicAnswer()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new AskAboutService().Ask(world, "embers");
            Assert.That(reply, Does.Contain("embers").IgnoreCase);
        }

        [Test]
        public void AskDm_ReturnsGroundedWorldSummary()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new AskDmService().Ask(world, "What should I do?");
            Assert.That(reply, Does.Contain("room seed 1337"));
        }

        [Test]
        public void Think_PrefersNearbyPickupWhenInventoryHasSpace()
        {
            var world = new SliceWorldFactory().Create(1337);
            var reply = new ThinkService().Think(world);
            Assert.That(reply, Does.Contain("Ember Shard"));
        }
    }
}

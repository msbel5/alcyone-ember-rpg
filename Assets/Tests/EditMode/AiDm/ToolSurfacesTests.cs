using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class ToolSurfacesTests
    {
        [Test]
        public void NpcSurface_HasFourTools_AllNpcSurface()
        {
            var descriptors = NpcAgentToolSurface.Descriptors();
            Assert.That(descriptors.Count, Is.EqualTo(4));
            Assert.That(descriptors.All(d => d.Surface.Equals(ToolSurfaceKind.Npc)), Is.True);
            Assert.That(descriptors.Select(d => d.Id.Code), Does.Contain("ask_about"));
            Assert.That(descriptors.Select(d => d.Id.Code), Does.Contain("escalate_to_dm"));
        }

        [Test]
        public void PartySurface_HasThreeTools_AllPartySurface()
        {
            var descriptors = PartyAgentToolSurface.Descriptors();
            Assert.That(descriptors.Count, Is.EqualTo(3));
            Assert.That(descriptors.All(d => d.Surface.Equals(ToolSurfaceKind.Party)), Is.True);
        }

        [Test]
        public void DmSurface_HasFourTools_AllDmSurface()
        {
            var descriptors = DmAgentToolSurface.Descriptors();
            Assert.That(descriptors.Count, Is.EqualTo(4));
            Assert.That(descriptors.All(d => d.Surface.Equals(ToolSurfaceKind.Dm)), Is.True);
            Assert.That(descriptors.Select(d => d.Id.Code), Does.Contain("consult_fate"));
        }

        [Test]
        public void RegisterAll_PopulatesRegistry()
        {
            var registry = new ToolRegistry();
            NpcAgentToolSurface.RegisterAll(registry);
            PartyAgentToolSurface.RegisterAll(registry);
            DmAgentToolSurface.RegisterAll(registry);
            Assert.That(registry.Count, Is.EqualTo(4 + 3 + 4));
        }
    }
}

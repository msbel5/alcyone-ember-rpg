using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>Pins Phase 10 Atom 5 ToolRegistry: registration, lookup, deterministic enumeration.</summary>
    public sealed class ToolRegistryTests
    {
        private static ToolDescriptor Descriptor(string toolCode, ToolSurfaceKind surface)
        {
            return new ToolDescriptor(
                new ToolId(toolCode),
                surface,
                parameters: null,
                outputSchemaKey: "void",
                sideEffect: ToolSideEffect.Read);
        }

        [Test]
        public void Empty_ReturnsZeroCount_AndFalseLookup()
        {
            var registry = new ToolRegistry();
            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(registry.TryGet(ToolSurfaceKind.Npc, new ToolId("ask"), out var d), Is.False);
            Assert.That(d, Is.Null);
        }

        [Test]
        public void Register_ThenTryGet_ReturnsDescriptor()
        {
            var registry = new ToolRegistry();
            var descriptor = Descriptor("ask_about", ToolSurfaceKind.Npc);
            registry.Register(descriptor);

            Assert.That(registry.TryGet(ToolSurfaceKind.Npc, new ToolId("ask_about"), out var found), Is.True);
            Assert.That(found, Is.SameAs(descriptor));
            Assert.That(registry.Contains(ToolSurfaceKind.Npc, new ToolId("ask_about")), Is.True);
        }

        [Test]
        public void Register_RejectsNull_AndDuplicates()
        {
            var registry = new ToolRegistry();
            Assert.Throws<System.ArgumentNullException>(() => registry.Register(null));

            registry.Register(Descriptor("ask_about", ToolSurfaceKind.Npc));
            Assert.Throws<System.InvalidOperationException>(() =>
                registry.Register(Descriptor("ask_about", ToolSurfaceKind.Npc)));
        }

        [Test]
        public void SameId_DifferentSurface_BothRegister()
        {
            var registry = new ToolRegistry();
            registry.Register(Descriptor("escalate", ToolSurfaceKind.Npc));
            registry.Register(Descriptor("escalate", ToolSurfaceKind.Party));

            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.Contains(ToolSurfaceKind.Npc, new ToolId("escalate")), Is.True);
            Assert.That(registry.Contains(ToolSurfaceKind.Party, new ToolId("escalate")), Is.True);
            Assert.That(registry.Contains(ToolSurfaceKind.Dm, new ToolId("escalate")), Is.False);
        }

        [Test]
        public void DescriptorsFor_FiltersBySurface_InRegistrationOrder()
        {
            var registry = new ToolRegistry();
            registry.Register(Descriptor("ask_about", ToolSurfaceKind.Npc));
            registry.Register(Descriptor("propose_event", ToolSurfaceKind.Dm));
            registry.Register(Descriptor("remember", ToolSurfaceKind.Npc));

            var npcTools = registry.DescriptorsFor(ToolSurfaceKind.Npc).Select(d => d.Id.Code).ToList();

            Assert.That(npcTools, Is.EqualTo(new[] { "ask_about", "remember" }));
        }

        [Test]
        public void TryGet_EmptySurfaceOrId_ReturnsFalse()
        {
            var registry = new ToolRegistry();
            registry.Register(Descriptor("ok", ToolSurfaceKind.Npc));

            Assert.That(registry.TryGet(default, new ToolId("ok"), out _), Is.False);
            Assert.That(registry.TryGet(ToolSurfaceKind.Npc, ToolId.Empty, out _), Is.False);
        }
    }
}

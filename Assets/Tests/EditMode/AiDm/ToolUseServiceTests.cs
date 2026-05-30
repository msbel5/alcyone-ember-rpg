using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class ToolUseServiceTests
    {
        [Test]
        public void Execute_RejectsIllFormedProposal()
        {
            var world = new WorldState();
            var service = new ToolUseService(StarterToolSurfaces.Registry());
            var call = new ToolCallRequest(new ToolId("gift_item"), ToolSurfaceKind.Npc, new Dictionary<string, string> { { "actor", "7" } });

            var result = service.Execute(call, world, new GameTime(10), new SiteId(1));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("missing_required:item"));
            Assert.That(world.ToolCallTrace.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_AcceptsValidProposal_AppendsTraceAndMemory()
        {
            var world = new WorldState();
            var service = new ToolUseService(StarterToolSurfaces.Registry());
            var call = new ToolCallRequest(new ToolId("gift_item"), ToolSurfaceKind.Npc, new Dictionary<string, string> { { "actor", "7" }, { "item", "iron_ring" } });

            var result = service.Execute(call, world, new GameTime(10), new SiteId(1));

            Assert.That(result.Accepted, Is.True);
            Assert.That(world.ToolCallTrace.Count, Is.EqualTo(1));
            Assert.That(world.NpcMemory.TryGet(new ActorId(7), out var memory), Is.True);
            Assert.That(memory.Events.Count, Is.EqualTo(1));
        }
    }
}

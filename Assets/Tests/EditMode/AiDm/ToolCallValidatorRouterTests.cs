using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class ToolCallValidatorRouterTests
    {
        private static ToolDescriptor AskAbout() =>
            new ToolDescriptor(
                new ToolId("ask_about"),
                ToolSurfaceKind.Npc,
                new[] { new ToolParameter("topic", "topic_id", required: true) },
                "dialogue_response",
                ToolSideEffect.Read);

        // ----- Validator -----
        [Test]
        public void Validator_NullRequest_Rejects()
        {
            var result = new ToolCallValidator().Validate(null, new ToolRegistry());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("null_request"));
        }

        [Test]
        public void Validator_UnknownTool_Rejects()
        {
            var registry = new ToolRegistry();
            var request = new ToolCallRequest(new ToolId("ghost"), ToolSurfaceKind.Npc, new Dictionary<string, string>());
            var result = new ToolCallValidator().Validate(request, registry);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("unknown_tool"));
        }

        [Test]
        public void Validator_MissingRequired_Rejects()
        {
            var registry = new ToolRegistry();
            registry.Register(AskAbout());
            var request = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc, new Dictionary<string, string>());
            var result = new ToolCallValidator().Validate(request, registry);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("missing_required:topic"));
        }

        [Test]
        public void Validator_Valid_Accepts()
        {
            var registry = new ToolRegistry();
            registry.Register(AskAbout());
            var request = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc, new Dictionary<string, string> { { "topic", "weather" } });
            var result = new ToolCallValidator().Validate(request, registry);
            Assert.That(result.Accepted, Is.True);
        }

        // ----- Router -----
        [Test]
        public void Router_Invoke_RoutesToHandler_AndEmitsToolInvoked()
        {
            var registry = new ToolRegistry();
            registry.Register(AskAbout());
            var router = new ToolCallRouter(new ToolCallValidator());
            router.RegisterHandler(ToolSurfaceKind.Npc, new ToolId("ask_about"), _ => ToolCallResult.AcceptedWith("answer"));
            var events = new WorldEventLog();

            var request = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc, new Dictionary<string, string> { { "topic", "weather" } });
            var result = router.Invoke(request, registry, default, new SiteId(1UL), events);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Payload, Is.EqualTo("answer"));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.ToolInvoked));
        }

        [Test]
        public void Router_NoHandler_RejectsAndLogs()
        {
            var registry = new ToolRegistry();
            registry.Register(AskAbout());
            var router = new ToolCallRouter(new ToolCallValidator());
            var events = new WorldEventLog();

            var request = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc, new Dictionary<string, string> { { "topic", "x" } });
            var result = router.Invoke(request, registry, default, new SiteId(1UL), events);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("no_handler"));
            Assert.That(events.Count, Is.EqualTo(1));
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class LlmProposalValidatorTests
    {
        private static ToolDescriptor AskAbout() =>
            new ToolDescriptor(
                new ToolId("ask_about"),
                ToolSurfaceKind.Npc,
                new[] { new ToolParameter("topic", "topic_id", required: true) },
                "dialogue_response",
                ToolSideEffect.Read);

        [Test]
        public void Validate_AcceptsValid_RejectsInvalid()
        {
            var registry = new ToolRegistry();
            registry.Register(AskAbout());
            var validator = new LlmProposalValidator(new ToolCallValidator());

            var valid = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc,
                new Dictionary<string, string> { { "topic", "weather" } });
            var invalid = new ToolCallRequest(new ToolId("ghost"), ToolSurfaceKind.Npc,
                new Dictionary<string, string>());

            var response = new LlmResponse("ok", new[] { valid, invalid }, 10);
            var result = validator.Validate(response, registry);

            Assert.That(result.Accepted.Count, Is.EqualTo(1));
            Assert.That(result.Accepted[0], Is.SameAs(valid));
            Assert.That(result.Rejected.Count, Is.EqualTo(1));
            Assert.That(result.Rejected[0].reason, Is.EqualTo("unknown_tool"));
        }

        [Test]
        public void Validate_EmptyProposalList_ProducesEmptyResults()
        {
            var registry = new ToolRegistry();
            var validator = new LlmProposalValidator(new ToolCallValidator());
            var response = new LlmResponse("text only", null, 5);
            var result = validator.Validate(response, registry);
            Assert.That(result.Accepted, Is.Empty);
            Assert.That(result.Rejected, Is.Empty);
        }
    }
}

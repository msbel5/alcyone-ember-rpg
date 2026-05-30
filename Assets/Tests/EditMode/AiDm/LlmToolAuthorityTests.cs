using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>
    /// DET-03: the LLM tool-authority gate is REAL, not cosmetic. A tool call the model PROPOSES is
    /// honoured only after LlmProposalValidator accepts it against the surface's ToolRegistry; a
    /// malicious/unregistered proposal is rejected and never reaches the world through ToolCallRouter.
    /// This mirrors the path DomainSimulationAdapter.Fate.cs now runs for consult_fate.
    /// </summary>
    public sealed class LlmToolAuthorityTests
    {
        private static ToolRegistry FateRegistry()
        {
            var r = new ToolRegistry();
            r.Register(new ToolDescriptor(new ToolId("consult_fate"), ToolSurfaceKind.Dm,
                new[] { new ToolParameter("query", "string", true) }, "string", ToolSideEffect.Read));
            return r;
        }

        private static ToolCallRequest Call(string tool, ToolSurfaceKind surface, params (string, string)[] ps)
        {
            var d = new Dictionary<string, string>();
            foreach (var (k, v) in ps) d[k] = v;
            return new ToolCallRequest(new ToolId(tool), surface, d);
        }

        [Test]
        public void MaliciousProposedToolCall_IsRejected_AndNeverMutatesWorld()
        {
            var registry = FateRegistry();
            var validator = new ToolCallValidator();

            // The model proposes an UNREGISTERED, mutating-sounding tool — exactly what must be refused.
            var response = new LlmResponse("the dice whisper...", new[]
            {
                Call("smite_player", ToolSurfaceKind.Dm, ("amount", "999")),
            }, 0);

            var proposals = new LlmProposalValidator(validator).Validate(response, registry);
            Assert.That(proposals.Accepted, Is.Empty, "an unregistered tool must not be accepted");
            Assert.That(proposals.Rejected, Has.Count.EqualTo(1));
            Assert.That(proposals.Rejected[0].reason, Is.EqualTo("unknown_tool"));

            // Route only the accepted set (none): the world's trace + event log stay untouched.
            var events = new WorldEventLog();
            var tracer = new ToolCallTracer();
            var router = new ToolCallRouter(validator);
            foreach (var accepted in proposals.Accepted)
                router.Invoke(accepted, registry, new GameTime(0), default, events, tracer);

            Assert.That(tracer.Entries, Is.Empty, "no accepted call -> no trace record");
            Assert.That(events.Count, Is.EqualTo(0), "a rejected proposal must not emit a world event");
        }

        [Test]
        public void WrongSurfaceProposal_IsRejected()
        {
            // consult_fate is a Dm tool; an NPC-surface proposal of it must be refused.
            var proposals = new LlmProposalValidator(new ToolCallValidator())
                .Validate(new LlmResponse("...", new[] { Call("consult_fate", ToolSurfaceKind.Npc, ("query", "x")) }, 0),
                          FateRegistry());
            Assert.That(proposals.Accepted, Is.Empty);
            Assert.That(proposals.Rejected, Has.Count.EqualTo(1));
        }

        [Test]
        public void ValidProposedToolCall_IsAccepted_AndRouted()
        {
            var registry = FateRegistry();
            var validator = new ToolCallValidator();
            var response = new LlmResponse("...", new[] { Call("consult_fate", ToolSurfaceKind.Dm, ("query", "oracle")) }, 0);

            var proposals = new LlmProposalValidator(validator).Validate(response, registry);
            Assert.That(proposals.Accepted, Has.Count.EqualTo(1));
            Assert.That(proposals.Rejected, Is.Empty);

            var tracer = new ToolCallTracer();
            var router = new ToolCallRouter(validator);
            router.RegisterHandler(ToolSurfaceKind.Dm, new ToolId("consult_fate"),
                _ => ToolCallResult.AcceptedWith("neutral"));
            var result = router.Invoke(proposals.Accepted[0], registry, new GameTime(0), default, new WorldEventLog(), tracer);

            Assert.That(result.Accepted, Is.True);
            Assert.That(tracer.Entries, Has.Count.EqualTo(1), "the accepted call is traced");
        }
    }
}

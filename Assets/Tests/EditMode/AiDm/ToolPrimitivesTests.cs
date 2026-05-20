using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>Pins Faz 10 tool-calling primitives: ToolId, ToolSurfaceKind, ToolDescriptor, ToolCallRequest/Result.</summary>
    public sealed class ToolPrimitivesTests
    {
        // ----- ToolId -----

        [Test]
        public void ToolId_RejectsBlank()
        {
            Assert.Throws<System.ArgumentException>(() => new ToolId(""));
            Assert.Throws<System.ArgumentException>(() => new ToolId("   "));
        }

        [Test]
        public void ToolId_EqualityByCode()
        {
            var a = new ToolId("ask_about");
            var b = new ToolId("ask_about");
            var c = new ToolId("query_relation");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(ToolId.Empty.IsEmpty, Is.True);
        }

        // ----- ToolSurfaceKind -----

        [Test]
        public void ToolSurfaceKind_ThreeStableCodes()
        {
            Assert.That(ToolSurfaceKind.Npc.Code, Is.EqualTo("npc"));
            Assert.That(ToolSurfaceKind.Party.Code, Is.EqualTo("party"));
            Assert.That(ToolSurfaceKind.Dm.Code, Is.EqualTo("dm"));
            Assert.That(ToolSurfaceKind.Npc, Is.Not.EqualTo(ToolSurfaceKind.Dm));
        }

        // ----- ToolSideEffect -----

        [Test]
        public void ToolSideEffect_TwoStableCodes()
        {
            Assert.That(ToolSideEffect.Read.Code, Is.EqualTo("read"));
            Assert.That(ToolSideEffect.Mutate.Code, Is.EqualTo("mutate"));
        }

        // ----- ToolDescriptor -----

        [Test]
        public void ToolDescriptor_HappyPath()
        {
            var descriptor = new ToolDescriptor(
                new ToolId("ask_about"),
                ToolSurfaceKind.Npc,
                new[] { new ToolParameter("topic", "topic_id", required: true) },
                outputSchemaKey: "dialogue_response",
                sideEffect: ToolSideEffect.Read);

            Assert.That(descriptor.Id.Code, Is.EqualTo("ask_about"));
            Assert.That(descriptor.Surface, Is.EqualTo(ToolSurfaceKind.Npc));
            Assert.That(descriptor.Parameters.Count, Is.EqualTo(1));
            Assert.That(descriptor.Parameters[0].Name, Is.EqualTo("topic"));
            Assert.That(descriptor.Parameters[0].Required, Is.True);
            Assert.That(descriptor.OutputSchemaKey, Is.EqualTo("dialogue_response"));
            Assert.That(descriptor.SideEffect, Is.EqualTo(ToolSideEffect.Read));
        }

        [Test]
        public void ToolDescriptor_NullParameters_BecomeEmpty()
        {
            var descriptor = new ToolDescriptor(
                new ToolId("noop"),
                ToolSurfaceKind.Dm,
                parameters: null,
                outputSchemaKey: "void",
                sideEffect: ToolSideEffect.Read);

            Assert.That(descriptor.Parameters, Is.Empty);
        }

        [Test]
        public void ToolDescriptor_EmptyIdOrSurface_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new ToolDescriptor(
                ToolId.Empty, ToolSurfaceKind.Npc, null, "void", ToolSideEffect.Read));
            Assert.Throws<System.ArgumentException>(() => new ToolDescriptor(
                new ToolId("ok"), default, null, "void", ToolSideEffect.Read));
            Assert.Throws<System.ArgumentException>(() => new ToolDescriptor(
                new ToolId("ok"), ToolSurfaceKind.Npc, null, "", ToolSideEffect.Read));
        }

        // ----- ToolCallRequest -----

        [Test]
        public void ToolCallRequest_StoresParametersImmutably()
        {
            var input = new Dictionary<string, string> { { "topic", "weather" } };
            var request = new ToolCallRequest(new ToolId("ask_about"), ToolSurfaceKind.Npc, input);
            input["topic"] = "mutated_after";

            Assert.That(request.Parameters["topic"], Is.EqualTo("weather"));
            Assert.That(request.TryGetParameter("topic", out var value), Is.True);
            Assert.That(value, Is.EqualTo("weather"));
        }

        // ----- ToolCallResult -----

        [Test]
        public void ToolCallResult_AcceptedAndRejectedFactories()
        {
            var ok = ToolCallResult.AcceptedWith("ok-payload");
            var no = ToolCallResult.Rejected("phase_fence_violation");

            Assert.That(ok.Accepted, Is.True);
            Assert.That(ok.Payload, Is.EqualTo("ok-payload"));
            Assert.That(no.Accepted, Is.False);
            Assert.That(no.RejectionReason, Is.EqualTo("phase_fence_violation"));
        }
    }
}

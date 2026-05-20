using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins ToolCallTraceSnapshot tail behaviour over typed AI/DM envelopes.</summary>
    public sealed class ToolCallTraceSnapshotTests
    {
        private static ToolCallTraceEntry Entry(string toolCode, ToolSurfaceKind surface, bool accepted, string rejection = "")
        {
            var request = new ToolCallRequest(new ToolId(toolCode), surface, new Dictionary<string, string>());
            var result = accepted
                ? ToolCallResult.AcceptedWith("ok")
                : ToolCallResult.Rejected(rejection);
            return new ToolCallTraceEntry(request, result);
        }

        [Test]
        public void NullEntries_ProducesEmptySnapshot()
        {
            var snapshot = ToolCallTraceSnapshot.FromTrace(null, 10);
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void NonPositiveMaxRows_ProducesEmpty()
        {
            var entries = new[] { Entry("ask_about", ToolSurfaceKind.Npc, accepted: true) };
            Assert.That(ToolCallTraceSnapshot.FromTrace(entries, 0).Rows, Is.Empty);
            Assert.That(ToolCallTraceSnapshot.FromTrace(entries, -3).Rows, Is.Empty);
        }

        [Test]
        public void SkipsEntriesWithNullRequestOrResult()
        {
            var entries = new[]
            {
                new ToolCallTraceEntry(null, null),
                Entry("ask_about", ToolSurfaceKind.Npc, accepted: true),
            };
            var snapshot = ToolCallTraceSnapshot.FromTrace(entries, 10);
            Assert.That(snapshot.Rows.Count, Is.EqualTo(1));
            Assert.That(snapshot.Rows[0].ToolCode, Is.EqualTo("ask_about"));
            Assert.That(snapshot.Rows[0].SurfaceCode, Is.EqualTo("npc"));
            Assert.That(snapshot.Rows[0].Accepted, Is.True);
        }

        [Test]
        public void TrimsToLatestTail_WhenLargerThanMax()
        {
            var entries = new[]
            {
                Entry("a", ToolSurfaceKind.Npc, accepted: true),
                Entry("b", ToolSurfaceKind.Party, accepted: false, rejection: "phase_fence"),
                Entry("c", ToolSurfaceKind.Dm, accepted: true),
            };
            var snapshot = ToolCallTraceSnapshot.FromTrace(entries, 2);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(2));
            Assert.That(snapshot.Rows[0].ToolCode, Is.EqualTo("b"));
            Assert.That(snapshot.Rows[0].Accepted, Is.False);
            Assert.That(snapshot.Rows[0].RejectionReason, Is.EqualTo("phase_fence"));
            Assert.That(snapshot.Rows[1].ToolCode, Is.EqualTo("c"));
        }
    }
}

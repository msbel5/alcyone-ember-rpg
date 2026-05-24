using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>
    /// Read-only snapshot of recent AI/DM tool-call requests + results for
    /// Unity debug HUD overlays. Pure C#: no UnityEngine, no mutation.
    /// Faz 11 Atom 7.
    /// </summary>
    public sealed class ToolCallTraceSnapshot
    {
        private readonly IReadOnlyList<ToolCallTraceRow> _rows;

        public ToolCallTraceSnapshot(IReadOnlyList<ToolCallTraceRow> rows)
        {
            _rows = rows ?? new ToolCallTraceRow[0];
        }

        public IReadOnlyList<ToolCallTraceRow> Rows => _rows;

        /// <summary>
        /// Builds a snapshot from in-order (request, result) pairs. The supplied
        /// list is enumerated once; nulls in either slot are skipped. The
        /// final <paramref name="maxRows"/> are returned in chronological order.
        /// </summary>
        public static ToolCallTraceSnapshot FromTrace(IReadOnlyList<ToolCallTraceEntry> entries, int maxRows)
        {
            if (entries == null || maxRows <= 0)
                return new ToolCallTraceSnapshot(new ToolCallTraceRow[0]);

            var filtered = new List<ToolCallTraceRow>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry.Request == null || entry.Result == null)
                    continue;
                filtered.Add(new ToolCallTraceRow(
                    entry.Request.ToolId.Code,
                    entry.Request.Surface.Code,
                    entry.Result.Accepted,
                    entry.Result.RejectionReason));
            }

            var start = filtered.Count > maxRows ? filtered.Count - maxRows : 0;
            if (start == 0)
                return new ToolCallTraceSnapshot(filtered);

            var tail = new List<ToolCallTraceRow>(filtered.Count - start);
            for (var i = start; i < filtered.Count; i++)
                tail.Add(filtered[i]);
            return new ToolCallTraceSnapshot(tail);
        }
    }

    /// <summary>One traced tool-call (request, result) pair for snapshot rendering.</summary>
    public readonly struct ToolCallTraceEntry
    {
        public ToolCallTraceEntry(ToolCallRequest request, ToolCallResult result)
        {
            Request = request;
            Result = result;
        }

        public ToolCallRequest Request { get; }
        public ToolCallResult Result { get; }
    }

    /// <summary>One row in <see cref="ToolCallTraceSnapshot"/>.</summary>
    public readonly struct ToolCallTraceRow
    {
        public ToolCallTraceRow(string toolCode, string surfaceCode, bool accepted, string rejectionReason)
        {
            ToolCode = toolCode ?? string.Empty;
            SurfaceCode = surfaceCode ?? string.Empty;
            Accepted = accepted;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public string ToolCode { get; }
        public string SurfaceCode { get; }
        public bool Accepted { get; }
        public string RejectionReason { get; }
    }
}

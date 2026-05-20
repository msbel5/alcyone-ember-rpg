using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>Append-only trace for deterministic AI/DM tool invocations.</summary>
    public sealed class ToolCallTracer
    {
        private readonly List<ToolCallTraceRecord> _entries = new List<ToolCallTraceRecord>();

        public IReadOnlyList<ToolCallTraceRecord> Entries => _entries;

        public void Record(GameTime tick, SiteId siteId, ToolCallRequest request, ToolCallResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));
            _entries.Add(new ToolCallTraceRecord(tick, siteId, request, result));
        }
    }
}

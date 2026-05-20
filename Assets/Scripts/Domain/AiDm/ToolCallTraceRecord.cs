using System;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>Persistable trace row for one deterministic AI/DM tool call.</summary>
    public sealed class ToolCallTraceRecord
    {
        public ToolCallTraceRecord(GameTime tick, SiteId siteId, ToolCallRequest request, ToolCallResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            Tick = tick;
            SiteId = siteId;
            Request = request;
            Result = result;
        }

        public GameTime Tick { get; }
        public SiteId SiteId { get; }
        public ToolCallRequest Request { get; }
        public ToolCallResult Result { get; }
    }
}

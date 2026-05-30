using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.AiDm
{
    public delegate ToolCallResult ToolHandler(ToolCallRequest request);

    /// <summary>
    /// Routes validated tool calls to registered handlers and emits
    /// ToolInvoked events to the WorldEventLog. Phase 10 Atom 7.
    /// </summary>
    public sealed class ToolCallRouter
    {
        private readonly Dictionary<RouteKey, ToolHandler> _handlers = new Dictionary<RouteKey, ToolHandler>();
        private readonly ToolCallValidator _validator;

        public ToolCallRouter(ToolCallValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public void RegisterHandler(ToolSurfaceKind surface, ToolId id, ToolHandler handler)
        {
            if (surface.IsEmpty || id.IsEmpty) throw new ArgumentException("Empty surface or id.");
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[new RouteKey(surface, id)] = handler;
        }

        public ToolCallResult Invoke(
            ToolCallRequest request,
            ToolRegistry registry,
            GameTime now,
            SiteId siteContext,
            WorldEventLog events,
            ToolCallTracer tracer = null)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var validation = _validator.Validate(request, registry);
            if (!validation.Accepted)
            {
                EmitInvoked(events, now, siteContext, request, accepted: false, reason: validation.RejectionReason);
                tracer?.Record(now, siteContext, request, validation);
                return validation;
            }

            if (!_handlers.TryGetValue(new RouteKey(request.Surface, request.ToolId), out var handler))
            {
                var miss = ToolCallResult.Rejected("no_handler");
                EmitInvoked(events, now, siteContext, request, accepted: false, reason: miss.RejectionReason);
                tracer?.Record(now, siteContext, request, miss);
                return miss;
            }

            var result = handler(request) ?? ToolCallResult.Rejected("handler_returned_null");
            EmitInvoked(events, now, siteContext, request, result.Accepted, result.Accepted ? "ok" : result.RejectionReason);
            tracer?.Record(now, siteContext, request, result);
            return result;
        }

        private static void EmitInvoked(WorldEventLog events, GameTime now, SiteId siteContext, ToolCallRequest request, bool accepted, string reason)
        {
            if (siteContext.IsEmpty)
                return;
            var verdict = accepted ? "accepted" : "rejected:" + (reason ?? "");
            var toolCode = request?.ToolId.Code ?? "(null)";
            var surfaceCode = request?.Surface.Code ?? "(null)";
            events.Append(new WorldEvent(
                now,
                WorldEventKind.ToolInvoked,
                default,
                siteContext,
                $"tool_invoked surface:{surfaceCode} tool:{toolCode} {verdict}"));
        }

        private readonly struct RouteKey : IEquatable<RouteKey>
        {
            public RouteKey(ToolSurfaceKind s, ToolId i) { Surface = s; Id = i; }
            public ToolSurfaceKind Surface { get; }
            public ToolId Id { get; }
            public bool Equals(RouteKey other) => Surface.Equals(other.Surface) && Id.Equals(other.Id);
            public override bool Equals(object obj) => obj is RouteKey k && Equals(k);
            public override int GetHashCode() => unchecked((Surface.GetHashCode() * 397) ^ Id.GetHashCode());
        }
    }
}

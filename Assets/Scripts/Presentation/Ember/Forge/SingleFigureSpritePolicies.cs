using System;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public static class SingleFigureSpritePolicies
    {
        public static bool NpcOnly(AssetGenerationRequest request)
        {
            if (request == null) return false;
            var id = request.RequestId ?? string.Empty;
            // NPC/creature full-body billboards ONLY. Portraits are head-and-shoulders busts (wide/square aspect,
            // no full-body single-figure shape), so the single-figure matte/aspect gate wrongly rejects them and
            // retries until the SD15-CPU 300s timeout (TaskCanceled) — keep portraits OUT of the gate.
            return id.StartsWith("npc_", StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("creature_", StringComparison.OrdinalIgnoreCase);
        }
    }
}

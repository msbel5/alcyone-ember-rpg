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
            return id.StartsWith("npc_", StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("creature_", StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("portrait_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "dm_portrait", StringComparison.OrdinalIgnoreCase);
        }
    }
}

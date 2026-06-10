using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    /// <summary>
    /// One-shot cross-scene hand-off for the LIVE world adapter. Fast travel reloads the GeneratedWorld
    /// scene; during that load the OLD host's OnDestroy clears the adapter locator (by design — it is a
    /// scene-scoped singleton), which previously made the new host bootstrap a FRESH default world: the
    /// played world, its overland and its quests silently vanished (black screen + "overland unavailable").
    /// Travel now carries the adapter across the load through this slot, same pattern as EmberWorldGenIntent.
    /// </summary>
    public static class EmberWorldContinuity
    {
        private static IDomainSimulationAdapter _carried;

        /// <summary>Stash the live adapter right before a deliberate scene reload (fast travel).</summary>
        public static void Carry(IDomainSimulationAdapter adapter) => _carried = adapter;

        /// <summary>Take the carried adapter exactly once; standalone scene loads see null and self-bootstrap.</summary>
        public static IDomainSimulationAdapter Take()
        {
            var adapter = _carried;
            _carried = null;
            return adapter;
        }

        public static void Clear() => _carried = null;
    }
}

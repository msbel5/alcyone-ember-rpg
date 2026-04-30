// Design note:
// ActorMemoryEventTypes centralizes deterministic memory event ids so rules/tests do not depend on prose.
// Inputs: simulation services choosing a mechanical event.
// Outputs: stable string ids persisted in saves and consumed by future DM query layers.
// Bible reference: ARCHITECTURE.md ActorMemory.events and dialogueSeen.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Stable ids for saved NPC memory event types.</summary>
    public static class ActorMemoryEventTypes
    {
        public const string Greeted = "Greeted";
        public const string DialogueTopic = "DialogueTopic";
        public const string TradedWith = "TradedWith";
        public const string PassageRequested = "PassageRequested";
        public const string ClearanceGranted = "ClearanceGranted";
    }
}

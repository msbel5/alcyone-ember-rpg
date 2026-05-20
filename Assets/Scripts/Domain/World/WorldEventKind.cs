// Design note:
// WorldEventKind enumerates the Faz 1 PROCESS-box event categories that the
// WorldEventLog records. The seed set is intentionally small and covers the
// Faz 1 acceptance gate ("player can spawn a guard, talk to it, then walk to
// a second site"): one spawn event, one social event, one location event.
// New kinds land alongside their concrete consumer; speculative additions are
// blocked by agent-rules-v2 rule 2.
// Atom-map ref: DOCS/sprint-faz-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Supported Faz 1 world-event categories. None is reserved as the empty sentinel.</summary>
    public enum WorldEventKind
    {
        None = 0,
        ActorSpawned = 1,
        ActorTalked = 2,
        SiteEntered = 3,
        RecipeCompleted = 4,
        JobAssigned = 5,
        JobCompleted = 6,
        NeedChanged = 7,
        JobRefused = 8,
        DayAdvanced = 9,
        SeasonChanged = 10,
        PlantPlanted = 11,
        PlantStageAdvanced = 12,
        PlantHarvested = 13,
        ActorStepped = 14,
        FactionReputationChanged = 15,
        CaravanArrived = 16,
        PriceChanged = 17,
        TradeCompleted = 18,
        ShortageDetected = 19,
        CombatResolved = 20,
        SpellResolved = 21,
        ToolInvoked = 22,
    }
}

// Pattern: Specification filter — the HUD asks this pure rule whether a world-event kind is worth surfacing.
namespace EmberCrpg.Presentation.Visual
{
    public static class WorldEventInterest
    {
        // Why: suppress deterministic per-tick flood so the HUD highlights meaningful changes by default.
        public static bool IsHudWorthy(string kindCode)
        {
            switch (kindCode)
            {
                case "NeedChanged":
                case "ActorStepped":
                    return false;
                default:
                    return true;
            }
        }
    }
}

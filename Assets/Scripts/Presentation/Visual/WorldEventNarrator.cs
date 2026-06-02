using System.Globalization;

// Pattern: Strategy formatter — deterministic, engine-free mapping from WorldEventRow to one HUD line.
namespace EmberCrpg.Presentation.Visual
{
    public sealed class WorldEventNarrator
    {
        // Why: produce a stable human-readable line for one projected world event row.
        public string ToLine(WorldEventRow row)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[d{0} {1:00}:{2:00}] {3} · {4} · {5}",
                row.Tick.DayOfYear,
                row.Tick.Hour,
                row.Tick.Minute,
                ResolveSubject(row),
                ResolveVerb(row.KindCode),
                row.Reason ?? string.Empty);
        }

        // Why: prefer actor, then site, then world so every row has a deterministic subject.
        private static string ResolveSubject(WorldEventRow row)
        {
            if (!row.ActorId.IsEmpty)
                return row.ActorId.Value.ToString(CultureInfo.InvariantCulture);
            if (!row.SiteId.IsEmpty)
                return row.SiteId.Value.ToString(CultureInfo.InvariantCulture);
            return "world";
        }

        // Why: keep verbs concise and stable while preserving unknown kinds verbatim.
        private static string ResolveVerb(string kindCode)
        {
            switch (kindCode)
            {
                case "RecipeCompleted": return "crafted";
                case "JobAssigned": return "took job";
                case "JobCompleted": return "finished job";
                case "JobRefused": return "refused job";
                case "NeedChanged": return "need";
                case "DayAdvanced": return "new day";
                case "SeasonChanged": return "season";
                case "PlantPlanted": return "planted";
                case "PlantHarvested": return "harvested";
                case "ActorSpawned": return "spawned";
                case "ActorStepped": return "moved";
                case "SiteEntered": return "entered";
                case "PriceChanged": return "price";
                case "TradeCompleted": return "traded";
                case "CombatResolved": return "fought";
                case "SpellResolved": return "cast";
                default: return kindCode ?? string.Empty;
            }
        }
    }
}

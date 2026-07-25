using System.Globalization;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W34-01 §3/§5.1: FarmOperations' mirror for the SLEEP phase machine — the ONE home of the
// bed reservation-key codec plus the night/dawn clock helpers. There is no bed furniture
// entity in this slice: the actor's own Home CELL is the bed (§3.4 keeps the future seam
// here — a furniture bed changes only the key and the capacity source, nothing else).
// CONSTRAINT (namespace disjointness, FarmOperations.cs:8-13 precedent): the "bed:" prefix
// must NEVER leak into a StockpileComponent tag or the FoodPileCache.FoodTags universe — a
// prefixed tag reaching a pile would corrupt effective-stock math. The ledger never parses
// tags; only this class does.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Bed-key codec + night-clock helpers for the SLEEP phase machine.</summary>
    internal static class SleepOperations
    {
        /// <summary>Decision gate: the fiat's "Fatigue.Value &gt; 0" kept verbatim as "&gt;= 1"
        /// (behaviour-preservation picked the simplest rule). A tunable constant, not a table.</summary>
        public const int FatigueSleepThreshold = 1;

        /// <summary>Chebyshev arrival tolerance: the IsAsleepAtHome &lt;= 1 precedent becomes the
        /// real rule — family members share a Home cell, so they settle in the 3x3 "bedroom"
        /// instead of stacking on one tile (plots were 1:1 locked; beds are shared).</summary>
        public const int BedReachCells = 1;

        private const string BedPrefix = "bed:";

        /// <summary>"bed:{x}:{y}" — the Home CELL is the address (SiteId stays 0UL: the bed
        /// joins no site-scoped sweep or count).</summary>
        public static string BedKey(GridPosition home)
            => BedPrefix + home.X.ToString(CultureInfo.InvariantCulture)
             + ":" + home.Y.ToString(CultureInfo.InvariantCulture);

        /// <summary>Failed parse = corrupt row = ReservationLost at the caller (plot-key pattern).</summary>
        public static bool TryParseBedKey(string itemTag, out GridPosition home)
        {
            home = default;
            if (itemTag == null || !itemTag.StartsWith(BedPrefix, System.StringComparison.Ordinal))
                return false;
            var body = itemTag.Substring(BedPrefix.Length);
            var split = body.IndexOf(':');
            if (split < 0)
                return false;
            // AllowLeadingSign: Home coordinates may be negative (plot keys are ulong; cells are not).
            if (!int.TryParse(body.Substring(0, split), NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var x))
                return false;
            if (!int.TryParse(body.Substring(split + 1), NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var y))
                return false;
            home = new GridPosition(x, y);
            return true;
        }

        // CONSTRAINT (W34-01 §11 risk 5): the half-open night window [22,06) lives ONLY here,
        // reading the NeedConsumptionSystem constants — MoveToBed's TimedOut and Sleep's
        // Succeeded MUST share this one predicate or an off-by-one dawn fork is born. The
        // projection's 22/6 literal copy died with this slice.
        public static bool IsNightHour(int hour)
            => hour >= NeedConsumptionSystem.NightStartHour || hour < NeedConsumptionSystem.NightEndHour;

        /// <summary>Minutes from the stamp to the next 06:00 dawn (TTL sizing, 1 tick = 1 minute).</summary>
        public static long MinutesUntilDawn(GameTime stamp)
            => ((NeedConsumptionSystem.NightEndHour - stamp.Hour + 24) % 24) * 60L - stamp.Minute;

        /// <summary>Bed capacity = living actors who call this cell Home — worldgen's house
        /// assignment IS the family definition (W34-01 §3.2 residence rule); no FamilyId model.
        /// A stranger cannot even ask: the decision only ever targets the actor's OWN Home.</summary>
        public static int ResidentCount(WorldState world, GridPosition home)
        {
            var count = 0;
            if (world?.Actors?.Records == null) return 0;
            foreach (var actor in world.Actors.Records)
                if (actor != null && actor.IsAlive && actor.Home.Equals(home))
                    count++;
            return count;
        }
    }
}

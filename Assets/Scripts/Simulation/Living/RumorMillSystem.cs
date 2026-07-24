using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Living
{
    /// <summary>
    /// P1 RumorMill (DFU pattern, reader-only): distills NEW world events into one-line town
    /// talk. Deterministic, no LLM: the mill is a formatter over the event log. 3-day life,
    /// cap 32, cursor persists in the save so loads never re-mill old news.
    /// </summary>
    public sealed class RumorMillSystem
    {
        public const int MaxRumors = 32;
        public const long LifeMinutes = 3L * 24L * 60L;
        private const int ScanCap = 256;

        public int Tick(WorldState world, GameTime stamp)
        {
            var events = world?.Events?.Events;
            if (events == null) return 0;
            world.Rumors ??= new List<RumorEntry>();

            // prune the stale
            world.Rumors.RemoveAll(r => stamp.TotalMinutes - r.BornMinutes > LifeMinutes);

            if (world.RumorEventCursor < 0 || world.RumorEventCursor > events.Count)
                world.RumorEventCursor = events.Count;
            int start = events.Count - world.RumorEventCursor > ScanCap
                ? events.Count - ScanCap
                : world.RumorEventCursor;
            int born = 0;
            for (int i = start; i < events.Count; i++)
            {
                var text = Distill(events[i]);
                if (text == null) continue;
                world.Rumors.Add(new RumorEntry { BornMinutes = stamp.TotalMinutes, SiteId = events[i].SiteId, Text = text });
                born++;
            }
            world.RumorEventCursor = events.Count;
            while (world.Rumors.Count > MaxRumors) world.Rumors.RemoveAt(0);
            return born;
        }

        /// <summary>Event -> one line of talk; null = not worth talking about.</summary>
        public static string Distill(WorldEvent evt)
        {
            var reason = evt.Reason ?? string.Empty;
            switch (evt.Kind)
            {
                case WorldEventKind.GuardResponded:
                    return "The watch crossed blades with a beast prowling the streets.";
                case WorldEventKind.WitnessRecorded:
                    return reason.StartsWith("reported", System.StringComparison.Ordinal)
                        ? "Someone ran to the watch about an attack - they say the guards took up the chase."
                        : "There was an attack in the streets; folk saw it happen.";
                case WorldEventKind.PlantHarvested:
                    return "The fields came in - fresh grain reached the larder.";
                case WorldEventKind.TradeCompleted:
                    return "A trade caravan moved goods between the towns.";
                case WorldEventKind.ChronicleEvent:
                    return "The chroniclers wrote a new page this month, so the elders say.";
                case WorldEventKind.NeedChanged:
                    if (reason.StartsWith("vermin_theft", System.StringComparison.Ordinal))
                        return "Rats got into the larder again - the stores are lighter for it.";
                    if (reason.StartsWith("cat_catch", System.StringComparison.Ordinal))
                        return "The tavern cat caught another rat. Good beast.";
                    if (reason.StartsWith("mauled_survives", System.StringComparison.Ordinal))
                        return "Someone was mauled near the edge of town - alive, but barely.";
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>Deterministic pick for one asker: site-local first, newest bucket, hashed by asker+day.</summary>
        public static string PickFor(WorldState world, ulong askerId, SiteId siteId, GameTime now)
        {
            var rumors = world?.Rumors;
            if (rumors == null || rumors.Count == 0) return null;
            var local = rumors.FindAll(r => r.SiteId.Equals(siteId));
            var pool = local.Count > 0 ? local : rumors;
            ulong hash = (askerId * 2654435761UL) ^ (ulong)(now.TotalMinutes / 1440L) * 40503UL;
            return pool[(int)(hash % (ulong)pool.Count)].Text;
        }
    }
}

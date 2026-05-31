// Why this file is intentionally long: the digest must hand-encode every mutable world-tick store in one canonical order so replay drift is reviewable.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Composition
{
    public static class WorldStateDigest
    {
        private static readonly IFormatProvider Invariant = CultureInfo.InvariantCulture;

        public static string Compute(WorldState world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            var canonical = BuildCanonical(world);
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var hex = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++)
                    hex.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));

                return hex.ToString();
            }
        }

        private static string BuildCanonical(WorldState world)
        {
            var sb = new StringBuilder(4096);
            AppendSectionHeader(sb, "WSDIGEST_v1");

            AppendTime(sb, world);
            AppendActors(sb, world.Actors);
            AppendPlants(sb, world.Plants);
            AppendSoils(sb, world.Soils);
            AppendJobs(sb, world.Jobs);
            AppendPrices(sb, world.Prices);
            AppendStockpiles(sb, world.Stockpiles);
            AppendCaravans(sb, world.Caravans);
            AppendSpellCooldowns(sb, world.PlayerSpellCooldowns);
            AppendShieldBuffs(sb, world.PlayerShieldBuffs);
            AppendEvents(sb, world.Events);

            return sb.ToString();
        }

        private static void AppendTime(StringBuilder sb, WorldState world)
        {
            AppendSectionHeader(sb, "TIME");
            sb.Append("t|");
            AppendLongField(sb, world.Time.TotalMinutes);
            sb.Append('\n');
        }

        private static void AppendActors(StringBuilder sb, ActorStore actors)
        {
            AppendSectionHeader(sb, "ACTORS");
            var rows = new List<ActorRecord>();
            if (actors != null)
            {
                foreach (var actor in actors.Records)
                {
                    if (actor != null)
                        rows.Add(actor);
                }
            }

            rows.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var actor = rows[i];
                sb.Append("a|");
                AppendUlongField(sb, actor.Id.Value);
                sb.Append('|');
                AppendIntField(sb, actor.Position.X);
                sb.Append('|');
                AppendIntField(sb, actor.Position.Y);
                sb.Append('|');
                AppendBoolField(sb, actor.ScheduleState.IsIdle);
                sb.Append('|');
                AppendUlongField(sb, actor.ScheduleState.CurrentJobId.Value);
                sb.Append('|');
                AppendIntField(sb, actor.Needs.Hunger.Value);
                sb.Append('|');
                AppendIntField(sb, actor.Needs.Fatigue.Value);
                sb.Append('|');
                AppendIntField(sb, actor.Needs.Thirst.Value);
                sb.Append('\n');
            }
        }

        private static void AppendPlants(StringBuilder sb, ComponentStore<PlantComponent> plants)
        {
            AppendSectionHeader(sb, "PLANTS");
            var rows = new List<KeyValuePair<WorldComponentId, PlantComponent>>();
            if (plants != null)
            {
                foreach (var row in plants.Rows)
                {
                    if (row.Value != null)
                        rows.Add(row);
                }
            }

            rows.Sort((left, right) => left.Key.Value.CompareTo(right.Key.Value));
            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                sb.Append("p|");
                AppendUlongField(sb, row.Key.Value);
                sb.Append('|');
                AppendStringField(sb, row.Value.StageId.Value);
                sb.Append('|');
                AppendIntField(sb, row.Value.DaysInStage);
                sb.Append('\n');
            }
        }

        private static void AppendSoils(StringBuilder sb, ComponentStore<SoilComponent> soils)
        {
            AppendSectionHeader(sb, "SOILS");
            var rows = new List<KeyValuePair<WorldComponentId, SoilComponent>>();
            if (soils != null)
            {
                foreach (var row in soils.Rows)
                {
                    if (row.Value != null)
                        rows.Add(row);
                }
            }

            rows.Sort((left, right) => left.Key.Value.CompareTo(right.Key.Value));
            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                sb.Append("s|");
                AppendUlongField(sb, row.Key.Value);
                sb.Append('|');
                AppendIntField(sb, row.Value.Fertility);
                sb.Append('|');
                AppendIntField(sb, row.Value.Moisture);
                sb.Append('|');
                AppendUlongField(sb, row.Value.PlantId.Value);
                sb.Append('\n');
            }
        }

        private static void AppendJobs(StringBuilder sb, JobBoard jobs)
        {
            AppendSectionHeader(sb, "JOBS");
            var ids = new List<JobId>();
            if (jobs != null)
            {
                foreach (var request in jobs.Requests)
                {
                    if (request != null)
                        ids.Add(request.Id);
                }
            }

            ids.Sort((left, right) => left.Value.CompareTo(right.Value));
            AppendCount(sb, ids.Count);

            if (jobs == null)
                return;

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                sb.Append("j|");
                AppendUlongField(sb, id.Value);
                sb.Append('|');
                AppendStringField(sb, jobs.GetStatus(id).Code);
                sb.Append('|');
                AppendUlongField(sb, jobs.GetClaimedBy(id).Value);
                sb.Append('\n');
            }
        }

        private static void AppendPrices(StringBuilder sb, PriceLedger prices)
        {
            AppendSectionHeader(sb, "PRICES");
            var entries = new List<PriceLedgerEntry>();
            if (prices != null)
            {
                foreach (var entry in prices.Entries)
                    entries.Add(entry);
            }

            entries.Sort((left, right) =>
            {
                var bySite = left.SiteId.Value.CompareTo(right.SiteId.Value);
                return bySite != 0
                    ? bySite
                    : StringComparer.Ordinal.Compare(left.ItemTag, right.ItemTag);
            });

            AppendCount(sb, entries.Count);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                sb.Append("r|");
                AppendUlongField(sb, entry.SiteId.Value);
                sb.Append('|');
                AppendStringField(sb, entry.ItemTag);
                sb.Append('|');
                AppendIntField(sb, entry.Price);
                sb.Append('\n');
            }
        }

        private static void AppendStockpiles(StringBuilder sb, List<StockpileComponent> stockpiles)
        {
            AppendSectionHeader(sb, "STOCKPILES");
            var rows = new List<StockpileRow>();
            if (stockpiles != null)
            {
                for (var i = 0; i < stockpiles.Count; i++)
                {
                    var stockpile = stockpiles[i];
                    if (stockpile != null)
                        rows.Add(new StockpileRow(i, stockpile));
                }
            }

            rows.Sort((left, right) =>
            {
                var bySite = left.Stockpile.SiteId.Value.CompareTo(right.Stockpile.SiteId.Value);
                return bySite != 0 ? bySite : left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var stockpile = rows[i].Stockpile;
                var entries = new List<KeyValuePair<string, int>>();
                foreach (var entry in stockpile.Entries)
                    entries.Add(entry);

                entries.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

                sb.Append("s|");
                AppendUlongField(sb, stockpile.SiteId.Value);
                sb.Append('|');
                AppendIntField(sb, entries.Count);
                sb.Append('\n');

                for (var e = 0; e < entries.Count; e++)
                {
                    var entry = entries[e];
                    sb.Append("e|");
                    AppendUlongField(sb, stockpile.SiteId.Value);
                    sb.Append('|');
                    AppendStringField(sb, entry.Key);
                    sb.Append('|');
                    AppendIntField(sb, entry.Value);
                    sb.Append('\n');
                }
            }
        }

        private static void AppendCaravans(StringBuilder sb, List<CaravanInstance> caravans)
        {
            AppendSectionHeader(sb, "CARAVANS");
            var rows = new List<CaravanRow>();
            if (caravans != null)
            {
                for (var i = 0; i < caravans.Count; i++)
                {
                    var caravan = caravans[i];
                    if (caravan != null)
                        rows.Add(new CaravanRow(i, caravan));
                }
            }

            rows.Sort((left, right) =>
            {
                var byId = left.Caravan.Id.Value.CompareTo(right.Caravan.Id.Value);
                return byId != 0 ? byId : left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var caravan = rows[i].Caravan;
                sb.Append("c|");
                AppendUlongField(sb, caravan.Id.Value);
                sb.Append('|');
                AppendUlongField(sb, caravan.RouteId.Value);
                sb.Append('|');
                AppendUlongField(sb, caravan.CurrentSiteId.Value);
                sb.Append('|');
                AppendIntField(sb, caravan.PayloadRemaining);
                sb.Append('|');
                AppendIntField(sb, caravan.StepsSinceDeparture);
                sb.Append('|');
                AppendStringField(sb, caravan.State.Code);
                sb.Append('\n');
            }
        }

        private static void AppendSpellCooldowns(StringBuilder sb, SpellCooldownState cooldowns)
        {
            AppendSectionHeader(sb, "SPELL_COOLDOWNS");
            var spellIds = new List<string>();
            if (cooldowns != null)
            {
                foreach (var spellId in cooldowns.GetTrackedSpellTemplateIds())
                {
                    if (!string.IsNullOrEmpty(spellId))
                        spellIds.Add(spellId);
                }
            }

            spellIds.Sort(StringComparer.Ordinal);
            AppendCount(sb, spellIds.Count);

            if (cooldowns == null)
                return;

            for (var i = 0; i < spellIds.Count; i++)
            {
                var spellId = spellIds[i];
                sb.Append("c|");
                AppendStringField(sb, spellId);
                sb.Append('|');
                AppendIntField(sb, cooldowns.GetRemainingTicks(spellId));
                sb.Append('\n');
            }
        }

        private static void AppendShieldBuffs(StringBuilder sb, ShieldBuffState buffs)
        {
            AppendSectionHeader(sb, "SHIELD_BUFFS");
            var spellIds = new List<string>();
            if (buffs != null)
            {
                foreach (var spellId in buffs.GetTrackedSpellTemplateIds())
                {
                    if (!string.IsNullOrEmpty(spellId))
                        spellIds.Add(spellId);
                }
            }

            spellIds.Sort(StringComparer.Ordinal);
            AppendCount(sb, spellIds.Count);

            if (buffs == null)
                return;

            for (var i = 0; i < spellIds.Count; i++)
            {
                var spellId = spellIds[i];
                sb.Append("b|");
                AppendStringField(sb, spellId);
                sb.Append('|');
                AppendIntField(sb, buffs.GetRemainingTicks(spellId));
                sb.Append('\n');
            }
        }

        private static void AppendEvents(StringBuilder sb, WorldEventLog events)
        {
            AppendSectionHeader(sb, "EVENTS");
            if (events == null)
            {
                AppendCount(sb, 0);
                return;
            }

            var rows = events.Events;
            AppendCount(sb, rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                {
                    sb.Append("e|");
                    AppendLongField(sb, 0L);
                    sb.Append('|');
                    AppendIntField(sb, -1);
                    sb.Append('|');
                    AppendStringField(sb, string.Empty);
                    sb.Append('|');
                    AppendStringField(sb, string.Empty);
                    sb.Append('\n');
                    continue;
                }

                sb.Append("e|");
                AppendLongField(sb, row.Tick.TotalMinutes);
                sb.Append('|');
                AppendIntField(sb, (int)row.Kind);
                sb.Append('|');
                AppendStringField(sb, row.Kind.ToString());
                sb.Append('|');
                AppendStringField(sb, row.Reason);
                sb.Append('\n');
            }
        }

        private static void AppendCount(StringBuilder sb, int count)
        {
            sb.Append("n|");
            AppendIntField(sb, count);
            sb.Append('\n');
        }

        private static void AppendSectionHeader(StringBuilder sb, string section)
        {
            sb.Append(section ?? string.Empty);
            sb.Append('\n');
        }

        private static void AppendBoolField(StringBuilder sb, bool value)
        {
            AppendStringField(sb, value ? "1" : "0");
        }

        private static void AppendIntField(StringBuilder sb, int value)
        {
            AppendStringField(sb, value.ToString(Invariant));
        }

        private static void AppendLongField(StringBuilder sb, long value)
        {
            AppendStringField(sb, value.ToString(Invariant));
        }

        private static void AppendUlongField(StringBuilder sb, ulong value)
        {
            AppendStringField(sb, value.ToString(Invariant));
        }

        private static void AppendStringField(StringBuilder sb, string value)
        {
            var text = value ?? string.Empty;
            sb.Append(text.Length.ToString(Invariant));
            sb.Append(':');
            sb.Append(text);
        }

        private sealed class StockpileRow
        {
            public StockpileRow(int originalIndex, StockpileComponent stockpile)
            {
                OriginalIndex = originalIndex;
                Stockpile = stockpile;
            }

            public int OriginalIndex { get; }
            public StockpileComponent Stockpile { get; }
        }

        private sealed class CaravanRow
        {
            public CaravanRow(int originalIndex, CaravanInstance caravan)
            {
                OriginalIndex = originalIndex;
                Caravan = caravan;
            }

            public int OriginalIndex { get; }
            public CaravanInstance Caravan { get; }
        }
    }
}

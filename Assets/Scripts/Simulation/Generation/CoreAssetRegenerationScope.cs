using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public enum CoreAssetRegenerationScope
    {
        DefaultSubset = 0,
        All = 1,
        NpcBillboards = 2,
        EnvironmentSurfaces = 3,
        Items = 4,
        Portraits = 5,
        Icons = 6,
    }

    public static class CoreAssetRegenerationSelector
    {
        private static readonly string[] DefaultSubsetOrder =
        {
            "npc_rogue",
            "npc_sage",
            "npc_guard",
            "wall_tavernflavour",
            "env_tavernflavour",
        };

        private static readonly HashSet<string> DefaultSubsetIds = new HashSet<string>(DefaultSubsetOrder, StringComparer.Ordinal);

        public static IReadOnlyList<ManifestEntry> Select(IReadOnlyList<ManifestEntry> entries, CoreAssetRegenerationScope scope)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (scope == CoreAssetRegenerationScope.DefaultSubset)
                return SelectDefaultSubset(entries);

            var selected = new List<ManifestEntry>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                if (Matches(entry, scope))
                    selected.Add(entry);
            }

            return selected;
        }

        private static IReadOnlyList<ManifestEntry> SelectDefaultSubset(IReadOnlyList<ManifestEntry> entries)
        {
            var selected = new List<ManifestEntry>(DefaultSubsetOrder.Length);
            for (var orderIndex = 0; orderIndex < DefaultSubsetOrder.Length; orderIndex++)
            {
                var id = DefaultSubsetOrder[orderIndex];
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                    {
                        selected.Add(entry);
                        break;
                    }
                }
            }

            return selected;
        }

        private static bool Matches(ManifestEntry entry, CoreAssetRegenerationScope scope)
        {
            switch (scope)
            {
                case CoreAssetRegenerationScope.DefaultSubset:
                    return DefaultSubsetIds.Contains(entry.Id);
                case CoreAssetRegenerationScope.All:
                    return entry.RequiresGeneration;
                case CoreAssetRegenerationScope.NpcBillboards:
                    return string.Equals(entry.Category, "npc", StringComparison.OrdinalIgnoreCase);
                case CoreAssetRegenerationScope.EnvironmentSurfaces:
                    return string.Equals(entry.Category, "environment", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(entry.Category, "wall", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(entry.Category, "roof", StringComparison.OrdinalIgnoreCase);
                case CoreAssetRegenerationScope.Items:
                    return string.Equals(entry.Category, "item", StringComparison.OrdinalIgnoreCase);
                case CoreAssetRegenerationScope.Portraits:
                    return string.Equals(entry.Category, "portrait", StringComparison.OrdinalIgnoreCase);
                case CoreAssetRegenerationScope.Icons:
                    return string.Equals(entry.Category, "ui", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}

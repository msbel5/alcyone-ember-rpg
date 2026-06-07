using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Generation
{
    /// <summary>
    /// Central name-to-generated-core policy for UI sprite lookup. Runtime UI asks for domain item/spell ids;
    /// this maps them to the generated asset ids without reviving the hand-authored Art registry as source of truth.
    /// </summary>
    public static class GeneratedCoreSpriteNameMapper
    {
        private static readonly HashSet<string> CoreIds = BuildCoreIdSet();

        public static bool TryMap(string name, out string coreId)
        {
            coreId = string.Empty;
            var key = Normalize(name);
            if (string.IsNullOrEmpty(key)) return false;

            if (CoreIds.Contains(key))
            {
                coreId = key;
                return true;
            }

            if (TryMapSpell(key, out coreId)) return true;
            if (TryMapItem(key, out coreId)) return true;
            return false;
        }

        private static bool TryMapSpell(string key, out string coreId)
        {
            coreId = string.Empty;
            var candidate = key.StartsWith("spell_", StringComparison.Ordinal) ? key : "spell_" + key;
            if (!CoreIds.Contains(candidate)) return false;
            coreId = candidate;
            return true;
        }

        private static bool TryMapItem(string key, out string coreId)
        {
            coreId = string.Empty;
            if (ContainsAny(key, "sword", "blade", "longsword")) return Set("item_sword", out coreId);
            if (ContainsAny(key, "bow")) return Set("item_bow", out coreId);
            if (ContainsAny(key, "staff", "wand")) return Set("item_staff", out coreId);
            if (ContainsAny(key, "potion", "elixir")) return Set("item_potion", out coreId);
            if (ContainsAny(key, "scroll", "writ")) return Set("item_scroll", out coreId);
            if (ContainsAny(key, "key")) return Set("item_key", out coreId);
            if (ContainsAny(key, "ring")) return Set("item_ring", out coreId);
            if (ContainsAny(key, "helm", "helmet")) return Set("item_helm", out coreId);
            if (ContainsAny(key, "boot")) return Set("item_boots", out coreId);
            if (ContainsAny(key, "shield")) return Set("item_shield", out coreId);
            return Set("inventory", out coreId);
        }

        private static bool Set(string id, out string coreId)
        {
            coreId = CoreIds.Contains(id) ? id : string.Empty;
            return !string.IsNullOrEmpty(coreId);
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            for (var i = 0; i < terms.Length; i++)
            {
                if (value.IndexOf(terms[i], StringComparison.Ordinal) >= 0) return true;
            }

            return false;
        }

        private static string Normalize(string name)
        {
            return (name ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        }

        private static HashSet<string> BuildCoreIdSet()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var entries = CoreAssetManifest.CreateDefault().Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(entries[i].Id))
                    set.Add(Normalize(entries[i].Id));
            }

            return set;
        }
    }
}

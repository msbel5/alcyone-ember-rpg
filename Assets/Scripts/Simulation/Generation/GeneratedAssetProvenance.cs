using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public static class GeneratedAssetProvenance
    {
        public const string Version = "real-images-v1";

        public static bool IsFresh(string assetPath, ManifestEntry entry, StaticPromptCatalog catalog, out string reason)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!entry.RequiresGeneration) { reason = "non_generated"; return true; }

            var stampPath = StampPath(assetPath);
            if (!File.Exists(stampPath)) { reason = "stale_missing_provenance"; return false; }

            var version = string.Empty;
            var promptHash = string.Empty;
            foreach (var line in File.ReadAllLines(stampPath))
            {
                var split = line.IndexOf('=');
                if (split <= 0) continue;
                var key = line.Substring(0, split).Trim();
                var value = line.Substring(split + 1).Trim();
                if (string.Equals(key, "version", StringComparison.Ordinal)) version = value;
                if (string.Equals(key, "promptHash", StringComparison.Ordinal)) promptHash = value;
            }

            if (!string.Equals(version, Version, StringComparison.Ordinal))
            {
                reason = "stale_prompt_version";
                return false;
            }

            if (!string.Equals(promptHash, ComputePromptHash(entry, catalog), StringComparison.Ordinal))
            {
                reason = "stale_prompt_version";
                return false;
            }

            reason = "cached";
            return true;
        }

        public static void Write(string assetPath, ManifestEntry entry, StaticPromptCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) throw new ArgumentException("Asset path is required.", nameof(assetPath));
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var stampPath = StampPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(stampPath));
            File.WriteAllText(
                stampPath,
                "version=" + Version + Environment.NewLine
                + "entryId=" + entry.Id + Environment.NewLine
                + "promptHash=" + ComputePromptHash(entry, catalog) + Environment.NewLine,
                Encoding.UTF8);
        }

        public static bool IsFreshCoreAsset(string entryId, string assetPath)
        {
            var entry = CoreAssetManifest.CreateDefault()
                .Entries
                .FirstOrDefault(e => string.Equals(e.Id, entryId, StringComparison.Ordinal));
            if (entry == null || !entry.RequiresGeneration) return true;
            return IsFresh(assetPath, entry, StaticPromptCatalog.CreateDefault(), out _);
        }

        public static string ComputePromptHash(ManifestEntry entry, StaticPromptCatalog catalog)
        {
            var prompt = ResolvePrompt(entry, catalog);
            var material = Version
                + "|" + entry.Id
                + "|" + entry.Category
                + "|" + entry.Width + "x" + entry.Height
                + "|" + entry.ModelHint
                + "|" + prompt
                + "|" + StaticPromptCatalog.EmberGenerationNegative;
            return Hash(material);
        }

        public static string ResolvePrompt(ManifestEntry entry, StaticPromptCatalog catalog)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!entry.RequiresGeneration) return string.Empty;
            if (catalog.TryGetPrompt(entry.StaticPromptKey, out var prompt)) return prompt;
            return StaticPromptCatalog.EmberStyleHeader + ", missing prompt key " + entry.StaticPromptKey + ", " + StaticPromptCatalog.EmberNegativeFooter;
        }

        private static string StampPath(string assetPath)
        {
            return (assetPath ?? string.Empty) + ".promptmeta";
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var sb = new StringBuilder("sha256:");
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}

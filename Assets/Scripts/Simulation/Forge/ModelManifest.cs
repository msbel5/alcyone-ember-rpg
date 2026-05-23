using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Design note:
// ModelManifest describes the AI model bundle Ember CRPG ships with: every .gguf,
// .onnx and tokenizer JSON the game needs, with a SHA-256 hash and a source HF
// URL. On first launch ModelBootstrap (Presentation) loads the manifest from
// StreamingAssets, verifies hashes, and (if any are missing) downloads them into
// persistentDataPath/Models/ where the runtime expects them.
//
// Layering invariant:
// - Pure C#. Lives in EmberCrpg.Simulation (`noEngineReferences=true`).
// - JSON parsing is hand-rolled (no UnityEngine.JsonUtility, no Newtonsoft) so
//   the harness can exercise it without Unity.
namespace EmberCrpg.Simulation.Forge
{
    public sealed class ModelEntry
    {
        public ModelEntry(string id, string path, long size, string sha256, string url)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            Id = id.Trim();
            Path = path.Trim();
            Size = size;
            Sha256 = (sha256 ?? string.Empty).Trim();
            Url = (url ?? string.Empty).Trim();
        }

        public string Id { get; }
        public string Path { get; }
        public long Size { get; }
        public string Sha256 { get; }
        public string Url { get; }
    }

    public static class ModelManifest
    {
        /// <summary>Parse the manifest JSON. Hand-rolled parser — no third-party deps.</summary>
        public static IReadOnlyList<ModelEntry> LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<ModelEntry>();
            var list = new List<ModelEntry>();

            int i = 0;
            if (!SkipTo(json, ref i, '[')) return list;
            i++; // past '['

            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length) break;
                if (json[i] == ']') break;
                if (json[i] == ',') { i++; continue; }

                if (json[i] != '{')
                {
                    i++;
                    continue;
                }
                int objEnd = FindMatching(json, i, '{', '}');
                if (objEnd < 0) break;
                var obj = json.Substring(i, objEnd - i + 1);

                string id = ReadStringField(obj, "id");
                string path = ReadStringField(obj, "path");
                long size = ReadLongField(obj, "size");
                string sha256 = ReadStringField(obj, "sha256");
                string url = ReadStringField(obj, "url");

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(path))
                    list.Add(new ModelEntry(id, path, size, sha256, url));

                i = objEnd + 1;
            }

            return list;
        }

        /// <summary>
        /// Returns the list of entries whose file is missing on disk OR whose SHA-256
        /// does not match. Entries are considered OK if their hash matches OR if their
        /// declared hash is empty / "TBD" / "PENDING" (used for prerelease builds).
        /// </summary>
        public static IReadOnlyList<ModelEntry> VerifyAllPresent(IReadOnlyList<ModelEntry> entries, string rootDir)
        {
            if (entries == null || entries.Count == 0) return Array.Empty<ModelEntry>();
            if (string.IsNullOrEmpty(rootDir)) throw new ArgumentNullException(nameof(rootDir));

            var missing = new List<ModelEntry>();
            foreach (var e in entries)
            {
                var full = ResolvePath(rootDir, e.Path);
                if (!File.Exists(full))
                {
                    missing.Add(e);
                    continue;
                }

                if (IsHashPlaceholder(e.Sha256)) continue;

                string actual = ComputeSha256(full);
                if (!string.Equals(actual, e.Sha256, StringComparison.OrdinalIgnoreCase))
                    missing.Add(e);
            }
            return missing;
        }

        public static string ResolvePath(string rootDir, string entryPath)
        {
            // Manifest paths are always relative to the root dir.
            return Path.Combine(rootDir, entryPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        }

        public static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(fs);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool IsHashPlaceholder(string sha)
        {
            if (string.IsNullOrWhiteSpace(sha)) return true;
            var s = sha.Trim();
            return string.Equals(s, "TBD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "PENDING", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("placeholder", StringComparison.OrdinalIgnoreCase);
        }

        // ---------- tiny JSON helpers ----------
        private static bool SkipTo(string s, ref int i, char target)
        {
            while (i < s.Length && s[i] != target) i++;
            return i < s.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }

        private static int FindMatching(string s, int start, char open, char close)
        {
            int depth = 0;
            bool inString = false;
            bool escape = false;
            for (int j = start; j < s.Length; j++)
            {
                char c = s[j];
                if (inString)
                {
                    if (escape) { escape = false; continue; }
                    if (c == '\\') { escape = true; continue; }
                    if (c == '"') { inString = false; continue; }
                    continue;
                }
                if (c == '"') { inString = true; continue; }
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0) return j;
                }
            }
            return -1;
        }

        private static string ReadStringField(string obj, string field)
        {
            int idx = FindFieldKey(obj, field);
            if (idx < 0) return string.Empty;
            int colon = obj.IndexOf(':', idx);
            if (colon < 0) return string.Empty;
            int p = colon + 1;
            while (p < obj.Length && (obj[p] == ' ' || obj[p] == '\t')) p++;
            if (p >= obj.Length || obj[p] != '"') return string.Empty;
            p++;
            var sb = new StringBuilder();
            while (p < obj.Length && obj[p] != '"')
            {
                if (obj[p] == '\\' && p + 1 < obj.Length)
                {
                    char esc = obj[p + 1];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(esc); break;
                    }
                    p += 2;
                    continue;
                }
                sb.Append(obj[p]);
                p++;
            }
            return sb.ToString();
        }

        private static long ReadLongField(string obj, string field)
        {
            int idx = FindFieldKey(obj, field);
            if (idx < 0) return 0L;
            int colon = obj.IndexOf(':', idx);
            if (colon < 0) return 0L;
            int p = colon + 1;
            while (p < obj.Length && (obj[p] == ' ' || obj[p] == '\t')) p++;
            int start = p;
            while (p < obj.Length && (char.IsDigit(obj[p]) || obj[p] == '-')) p++;
            if (p == start) return 0L;
            long v;
            return long.TryParse(obj.Substring(start, p - start), out v) ? v : 0L;
        }

        private static int FindFieldKey(string obj, string field)
        {
            var marker = "\"" + field + "\"";
            return obj.IndexOf(marker, StringComparison.Ordinal);
        }
    }
}

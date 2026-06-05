using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedAssetIdUtility
    {
        public static string NormalizeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            var lastDash = false;
            foreach (var c in value.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                    lastDash = false;
                    continue;
                }

                if (char.IsWhiteSpace(c) || c == '-' || c == '_')
                {
                    if (!lastDash && builder.Length > 0)
                    {
                        builder.Append('-');
                        lastDash = true;
                    }
                }
            }

            return builder.ToString().Trim('-');
        }

        public static string BuildStableId(GeneratedAssetKey key)
        {
            var material = BuildIdentityMaterial(key);
            var readable = new List<string>
            {
                NormalizeSegment(key.kind.ToString()),
                NormalizeSegment(key.archetype),
                NormalizeSegment(key.biome),
                NormalizeSegment(key.culture),
                NormalizeSegment(key.faction),
                NormalizeSegment(key.role),
                NormalizeSegment(key.material),
                NormalizeSegment(key.tier),
                NormalizeSegment(key.styleVersion),
            };

            var prefix = Join(readable);
            if (string.IsNullOrEmpty(prefix)) prefix = "generated-asset";
            return prefix + "-" + Hash(material, 12);
        }

        public static string BuildDeterministicToken(string prefix, string material)
        {
            var safePrefix = NormalizeSegment(prefix);
            if (string.IsNullOrEmpty(safePrefix)) safePrefix = "token";
            return safePrefix + "-" + Hash(material ?? string.Empty, 12);
        }

        public static int DeterministicIndex(string material, int candidateCount)
        {
            if (candidateCount <= 0) return -1;
            var hash = Hash(material ?? string.Empty, 8);
            var value = uint.Parse(hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (int)(value % (uint)candidateCount);
        }

        public static string BuildIdentityMaterial(GeneratedAssetKey key)
        {
            return string.Join("|", new[]
            {
                NormalizeSegment(key.kind.ToString()),
                NormalizeSegment(key.archetype),
                NormalizeSegment(key.biome),
                NormalizeSegment(key.culture),
                NormalizeSegment(key.faction),
                NormalizeSegment(key.role),
                NormalizeSegment(key.material),
                NormalizeSegment(key.tier),
                key.variantIndex.ToString(CultureInfo.InvariantCulture),
                NormalizeSegment(key.styleVersion),
                key.seed.ToString(CultureInfo.InvariantCulture),
                NormalizeSegment(key.promptHash),
            });
        }

        private static string Hash(string material, int length)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var full = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++)
                    full.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return full.ToString(0, length);
            }
        }

        private static string Join(IEnumerable<string> segments)
        {
            var builder = new StringBuilder();
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment)) continue;
                if (builder.Length > 0) builder.Append('-');
                builder.Append(segment);
            }

            return builder.ToString();
        }
    }
}

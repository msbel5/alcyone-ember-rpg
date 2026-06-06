using System;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public static class CoreAssetLibraryRecordBuilder
    {
        public static GeneratedAssetRecord Build(ManifestEntry entry, string prompt, string negativePrompt, string promptHash, string importedAtUtc)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var record = new GeneratedAssetRecord
            {
                displayName = entry.Id,
                kind = MapKind(entry.Category),
                sourcePrompt = prompt ?? string.Empty,
                negativePrompt = negativePrompt ?? string.Empty,
                seed = StableSeed(entry.Id),
                modelName = entry.ModelHint ?? string.Empty,
                modelLicense = "See model and toolchain notes.",
                toolchainNotes = "Generated via VisibleGenerationPipeline and SerializedAssetForge.",
                importedAtUtc = importedAtUtc ?? string.Empty,
                relativeAssetPath = entry.ExpectedPath ?? string.Empty,
                previewPath = entry.ExpectedPath ?? string.Empty,
                licenseStatus = GeneratedAssetLicenseStatus.NeedsReview,
                humanApproved = false,
                isTileable = IsTileable(entry.Category),
                deLit = IsTileable(entry.Category),
            };

            ApplyKindSpecificPaths(record, entry);
            PopulateKey(record, entry, promptHash);
            record.SyncIdentity();
            return record;
        }

        private static void ApplyKindSpecificPaths(GeneratedAssetRecord record, ManifestEntry entry)
        {
            switch (record.kind)
            {
                case GeneratedAssetKind.TileableWall:
                case GeneratedAssetKind.TileableFloor:
                case GeneratedAssetKind.TileableCeiling:
                case GeneratedAssetKind.MaterialSet:
                    record.albedoPath = entry.ExpectedPath ?? string.Empty;
                    break;
                default:
                    record.spritePath = entry.ExpectedPath ?? string.Empty;
                    break;
            }
        }

        private static void PopulateKey(GeneratedAssetRecord record, ManifestEntry entry, string promptHash)
        {
            var slug = Suffix(entry.Id, entry.Category);
            record.key.kind = record.kind;
            record.key.promptHash = promptHash ?? string.Empty;
            record.key.styleVersion = GeneratedAssetProvenance.Version;
            record.key.seed = record.seed;
            record.key.variantIndex = 0;

            if (record.kind == GeneratedAssetKind.CharacterBillboard)
            {
                record.key.archetype = "core-npc";
                record.key.role = slug;
                record.tags.Add(entry.Category);
                record.tags.Add(slug);
                return;
            }

            if (record.kind == GeneratedAssetKind.TileableWall
                || record.kind == GeneratedAssetKind.TileableFloor
                || record.kind == GeneratedAssetKind.TileableCeiling)
            {
                record.key.archetype = entry.Category;
                record.key.material = slug;
                record.tags.Add(entry.Category);
                record.tags.Add("tileable");
                return;
            }

            record.key.archetype = slug;
            record.tags.Add(entry.Category);
        }

        private static GeneratedAssetKind MapKind(string category)
        {
            if (string.Equals(category, "npc", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.CharacterBillboard;
            if (string.Equals(category, "portrait", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.CharacterBillboard;
            if (string.Equals(category, "wall", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.TileableWall;
            if (string.Equals(category, "environment", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.TileableFloor;
            if (string.Equals(category, "roof", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.TileableCeiling;
            if (string.Equals(category, "door", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.SmallPropBillboard;
            if (string.Equals(category, "window", StringComparison.OrdinalIgnoreCase)) return GeneratedAssetKind.SmallPropBillboard;
            return GeneratedAssetKind.ItemBillboard;
        }

        private static bool IsTileable(string category)
        {
            return string.Equals(category, "wall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "environment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "roof", StringComparison.OrdinalIgnoreCase);
        }

        private static string Suffix(string entryId, string category)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return string.Empty;
            if (string.Equals(category, "ui", StringComparison.OrdinalIgnoreCase)) return entryId;
            var prefix = category + "_";
            return entryId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? entryId.Substring(prefix.Length)
                : entryId;
        }

        private static int StableSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (var i = 0; i < value.Length; i++) hash = (hash ^ value[i]) * 16777619u;
                hash = hash == 0u ? 1u : hash;
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}

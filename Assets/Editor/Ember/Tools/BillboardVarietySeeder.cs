using System.Collections.Generic;
using System.IO;
using EmberCrpg.Data.GeneratedAssets;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    /// <summary>
    /// GÖRSEL ÇEŞİTLİLİK: the runtime variant machinery (DeterministicIndex over per-actor
    /// seed) has been live since day one, but the library shipped exactly ONE record per role —
    /// so every guard, every villager, every bandit wore the same PNG, and dungeon monsters
    /// fell back to pixel-mask silhouettes ("space" figures). This seeder registers the 25
    /// hand-made character paintings already sitting in Assets/Art as ADDITIONAL billboard
    /// variants (and first-ever monster_* records), and mirrors every billboard PNG into
    /// StreamingAssets so PLAYER builds resolve them without the dev machine's cache.
    /// </summary>
    public static class BillboardVarietySeeder
    {
        private const string DatabasePath = "Assets/Resources/GeneratedAssets/GeneratedAssetDatabase.asset";
        private const string CoreDir = "Assets/Generated/Core";
        private const string StreamingCoreDir = "Assets/StreamingAssets/Generated/Core";

        // role -> art files (Assets/Art/...). Each becomes one extra variant for that role.
        private static readonly (string role, string artPath)[] CivilianVariants =
        {
            ("artisan", "Assets/Art/Characters/blacksmith.png"),
            ("bandit", "Assets/Art/Characters/bandit_fixed.png"),
            ("bandit", "Assets/Art/Characters/goblin_fixed.png"),
            ("bandit", "Assets/Art/Characters/orc.png"),
            ("bard", "Assets/Art/Characters/bard.png"),
            ("beggar", "Assets/Art/Characters/beggar_fixed.png"),
            ("blacksmith", "Assets/Art/Characters/blacksmith.png"),
            ("guard", "Assets/Art/Characters/knight.png"),
            ("healer", "Assets/Art/Characters/healer.png"),
            ("healer", "Assets/Art/Characters/witch.png"),
            ("innkeeper", "Assets/Art/Characters/innkeeper.png"),
            ("knight", "Assets/Art/Characters/knight.png"),
            ("mage", "Assets/Art/Characters/witch.png"),
            ("outlaw", "Assets/Art/Characters/thief.png"),
            ("outlaw", "Assets/Art/Characters/spy.png"),
            ("rogue", "Assets/Art/Characters/thief.png"),
            ("rogue", "Assets/Art/Characters/spy.png"),
            ("sage", "Assets/Art/Characters/sage.png"),
            ("scholar", "Assets/Art/Characters/sage.png"),
        };

        // First-ever records for monster_* roles: real paintings replace runtime silhouettes.
        private static readonly (string role, string artPath)[] MonsterVariants =
        {
            ("monster_spider", "Assets/Art/Characters/spider.png"),
            ("monster_skeleton", "Assets/Art/Characters/skeleton.png"),
            ("monster_skeleton", "Assets/Art/Characters/necromancer.png"),
            ("monster_ghost", "Assets/Art/Characters/ghost.png"),
            ("monster_bandit", "Assets/Art/Characters/bandit_fixed.png"),
            ("monster_bandit", "Assets/Art/Characters/goblin_fixed.png"),
            ("monster_bandit", "Assets/Art/Characters/orc.png"),
            ("monster_wolf", "Assets/Art/BodySilhouettes/beast_quadruped.png"),
        };

        // 32x32 pixel-sprites that slipped into the first pass: blurry half-height billboards.
        private static readonly string[] PurgeArtNames =
            { "art_warrior", "art_mage", "art_merchant", "art_quest_giver", "art_priest", "art_rogue" };

        public static void Seed()
        {
            var database = AssetDatabase.LoadAssetAtPath<GeneratedAssetDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[VarietySeeder] database not found at {DatabasePath}");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(CoreDir);
            Directory.CreateDirectory(StreamingCoreDir);

            // Purge (1) records for art later found to be 32x32 pixel sprites and (2) run-1
            // duplicates whose stableIds Unity canonicalized (characterbillboard-<role>-v1-<hash>)
            // — match on spritePath, the one field canonicalization does not rewrite.
            int purged = database.Records.RemoveAll(r =>
                r != null && (r.spritePath ?? string.Empty).StartsWith("Assets/Generated/Core/art_", System.StringComparison.Ordinal)
                && (System.Array.Exists(PurgeArtNames, n => r.spritePath.EndsWith(n + ".png", System.StringComparison.Ordinal))
                    || !r.stableId.StartsWith("characterbillboard-art-", System.StringComparison.Ordinal)));
            foreach (var name in PurgeArtNames)
            {
                foreach (var dir in new[] { CoreDir, StreamingCoreDir })
                {
                    var f = dir + "/" + name + ".png";
                    if (File.Exists(f)) File.Delete(f);
                    if (File.Exists(f + ".meta")) File.Delete(f + ".meta");
                }
            }

            int added = 0, copied = 0;
            var known = new HashSet<string>();
            foreach (var record in database.Records)
                if (record != null) known.Add(record.stableId);

            var all = new List<(string role, string artPath)>();
            all.AddRange(CivilianVariants);
            all.AddRange(MonsterVariants);

            foreach (var (role, artPath) in all)
            {
                if (!File.Exists(artPath))
                {
                    Debug.LogWarning($"[VarietySeeder] missing art: {artPath}");
                    continue;
                }

                var coreName = "art_" + Path.GetFileNameWithoutExtension(artPath).ToLowerInvariant() + ".png";
                var corePath = CoreDir + "/" + coreName;
                if (!File.Exists(corePath))
                {
                    File.Copy(artPath, corePath);
                    copied++;
                }

                var stableId = $"characterbillboard-art-{role}-{Path.GetFileNameWithoutExtension(artPath).ToLowerInvariant()}";
                if (known.Contains(stableId))
                    continue;

                var record = new GeneratedAssetRecord
                {
                    stableId = stableId,
                    displayName = $"{role} variant ({Path.GetFileNameWithoutExtension(artPath)})",
                    kind = GeneratedAssetKind.CharacterBillboard,
                    spritePath = corePath,
                    relativeAssetPath = corePath,
                    licenseStatus = GeneratedAssetLicenseStatus.Clean,
                    toolchainNotes = "seeded from hand-registered Assets/Art library (BillboardVarietySeeder)",
                };
                record.key.kind = GeneratedAssetKind.CharacterBillboard;
                record.key.role = role;
                database.Records.Add(record);
                known.Add(stableId);
                added++;
            }

            // Mirror EVERY core billboard PNG into StreamingAssets: player builds have no
            // project tree and no forge cache — without this a fresh machine renders grey quads.
            foreach (var png in Directory.GetFiles(CoreDir, "*.png"))
            {
                var dest = Path.Combine(StreamingCoreDir, Path.GetFileName(png));
                if (!File.Exists(dest) || File.GetLastWriteTimeUtc(png) > File.GetLastWriteTimeUtc(dest))
                {
                    File.Copy(png, dest, overwrite: true);
                    copied++;
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VarietySeeder] DONE — records added: {added}, purged: {purged}, files copied: {copied}, total records: {database.Records.Count}");
        }
    }
}

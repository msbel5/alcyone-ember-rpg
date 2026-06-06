using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedAssetDatabaseEditorUtility
    {
        public const string DefaultDatabasePath = "Assets/Resources/GeneratedAssets/GeneratedAssetDatabase.asset";
        public const string LegacyDatabasePath = "Assets/Manifests/GeneratedAssets/GeneratedAssetDatabase.asset";

        public static GeneratedAssetDatabase LoadOrCreate()
        {
            var database = AssetDatabase.LoadAssetAtPath<GeneratedAssetDatabase>(DefaultDatabasePath);
            if (database != null) return database;

            var legacy = AssetDatabase.LoadAssetAtPath<GeneratedAssetDatabase>(LegacyDatabasePath);
            if (legacy != null)
            {
                EmberSceneSavePolicy.EnsureFolderExists("Assets/Resources");
                EmberSceneSavePolicy.EnsureFolderExists("Assets/Resources/GeneratedAssets");
                var moveError = AssetDatabase.MoveAsset(LegacyDatabasePath, DefaultDatabasePath);
                if (!string.IsNullOrWhiteSpace(moveError))
                {
                    UnityEngine.Debug.LogWarning("[GeneratedAssetLibrary] Failed to migrate database to Resources path: " + moveError);
                    return legacy;
                }

                AssetDatabase.SaveAssets();
                return AssetDatabase.LoadAssetAtPath<GeneratedAssetDatabase>(DefaultDatabasePath);
            }

            EmberSceneSavePolicy.EnsureFolderExists("Assets/Resources");
            EmberSceneSavePolicy.EnsureFolderExists("Assets/Resources/GeneratedAssets");

            database = ScriptableObject.CreateInstance<GeneratedAssetDatabase>();
            AssetDatabase.CreateAsset(database, DefaultDatabasePath);
            AssetDatabase.SaveAssets();
            return database;
        }

        public static void UpsertByStableIdOrPath(GeneratedAssetDatabase database, GeneratedAssetRecord incoming)
        {
            if (database == null || incoming == null) return;
            incoming.SyncIdentity();

            for (var i = 0; i < database.Records.Count; i++)
            {
                var existing = database.Records[i];
                if (existing == null) continue;
                existing.SyncIdentity();
                if (existing.stableId == incoming.stableId
                    || (!string.IsNullOrWhiteSpace(existing.relativeAssetPath)
                        && existing.relativeAssetPath == incoming.relativeAssetPath))
                {
                    database.Records[i] = incoming;
                    return;
                }
            }

            database.Records.Add(incoming);
        }
    }
}

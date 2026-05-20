using System.IO;
using EmberCrpg.Editor.Ember.Common;
using EmberCrpg.Presentation.Ember.Sprites;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    /// <summary>
    /// Walks <c>Assets/Art/Items</c>, <c>Assets/Art/Spells</c>, <c>Assets/Art/Characters</c>,
    /// and <c>Assets/Art/Portraits</c>, then writes a <see cref="SpriteRegistry"/> asset
    /// keyed by the file name (without extension). The registry plugs into
    /// <c>InventoryGrid.SpriteLookup</c> and any other view that needs a name-keyed sprite.
    /// </summary>
    public static class SpriteRegistryAutoBuilder
    {
        public const string RegistryAssetPath = "Assets/Art/SpriteRegistries/EmberCanonicalRegistry.asset";

        [MenuItem("Ember/Build/Sprite Registry From Art Folders")]
        public static void Build()
        {
            EmberSceneSavePolicy.EnsureFolderExists("Assets/Art/SpriteRegistries");

            var registry = AssetDatabase.LoadAssetAtPath<SpriteRegistry>(RegistryAssetPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<SpriteRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            }

            var entries = new System.Collections.Generic.List<SpriteRegistry.Entry>();
            CollectFolder(EmberAssetPaths.ItemsDir, entries);
            CollectFolder(EmberAssetPaths.SpellsDir, entries);
            CollectFolder(EmberAssetPaths.CharactersDir, entries);
            CollectFolder(EmberAssetPaths.PortraitsDir, entries);
            CollectFolder(EmberAssetPaths.UiStatusIconsDir, entries);

            var serialized = new SerializedObject(registry);
            var entriesProp = serialized.FindProperty("_entries");
            entriesProp.ClearArray();
            for (int i = 0; i < entries.Count; i++)
            {
                entriesProp.arraySize++;
                var element = entriesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Name").stringValue = entries[i].Name;
                element.FindPropertyRelative("Sprite").objectReferenceValue = entries[i].Sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[EmberRegistry] wrote {entries.Count} sprite mappings into {RegistryAssetPath}");
        }

        private static void CollectFolder(string folder, System.Collections.Generic.List<SpriteRegistry.Entry> sink)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                sink.Add(new SpriteRegistry.Entry
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Sprite = sprite,
                });
            }
        }
    }
}

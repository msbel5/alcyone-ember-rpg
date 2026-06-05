using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedMeshPrefabBuilder
    {
        public static string CreateOrUpdate(GameObject source, GeneratedAssetRecord record)
        {
            EmberSceneSavePolicy.EnsureFolderExists(EmberAssetPaths.PrefabsRoot + "/Generated");
            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) return string.Empty;

            try
            {
                EnsureCollider(instance, record.colliderType);
                var path = GeneratedMeshPathPolicy.ResolvePrefabPath(record);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                AssetDatabase.SaveAssets();
                record.prefabPath = path;
                return path;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureCollider(GameObject root, GeneratedMeshColliderType colliderType)
        {
            if (colliderType == GeneratedMeshColliderType.None || root.GetComponentInChildren<Collider>(true) != null) return;

            switch (colliderType)
            {
                case GeneratedMeshColliderType.Box:
                    root.AddComponent<BoxCollider>();
                    break;
                case GeneratedMeshColliderType.Mesh:
                case GeneratedMeshColliderType.ConvexMesh:
                    var collider = root.AddComponent<MeshCollider>();
                    collider.convex = colliderType == GeneratedMeshColliderType.ConvexMesh;
                    break;
            }
        }
    }
}

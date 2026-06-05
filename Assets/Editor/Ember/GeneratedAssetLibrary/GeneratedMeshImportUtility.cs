using System.Collections.Generic;
using EmberCrpg.Data.GeneratedAssets;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.GeneratedAssets
{
    public static class GeneratedMeshImportUtility
    {
        public static void Analyze(GameObject source, GeneratedAssetRecord record)
        {
            var meshFilters = source.GetComponentsInChildren<MeshFilter>(true);
            var renderers = source.GetComponentsInChildren<Renderer>(true);
            var colliders = source.GetComponentsInChildren<Collider>(true);
            var lodGroup = source.GetComponentInChildren<LODGroup>(true);

            record.generatedMeshPath = AssetDatabase.GetAssetPath(source);
            record.prefabPath = string.IsNullOrWhiteSpace(record.prefabPath) ? record.generatedMeshPath : record.prefabPath;
            record.materialPaths = new List<string>();
            record.texturePaths = new List<string>();
            record.triangleCount = 0;
            record.vertexCount = 0;
            record.subMeshCount = 0;
            record.materialCount = 0;
            record.hasUVs = true;
            record.hasNormals = true;
            record.hasTangents = true;
            record.hasReadableMesh = true;

            foreach (var filter in meshFilters)
            {
                if (filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                record.vertexCount += mesh.vertexCount;
                record.subMeshCount += mesh.subMeshCount;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                    record.triangleCount += mesh.GetTriangles(sub).Length / 3;
                record.hasUVs &= mesh.uv != null && mesh.uv.Length > 0;
                record.hasNormals &= mesh.normals != null && mesh.normals.Length > 0;
                record.hasTangents &= mesh.tangents != null && mesh.tangents.Length > 0;
                record.hasReadableMesh &= mesh.isReadable;
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;
                record.materialCount += renderer.sharedMaterials.Length;
                foreach (var material in renderer.sharedMaterials)
                {
                    var path = material == null ? string.Empty : AssetDatabase.GetAssetPath(material);
                    if (!string.IsNullOrWhiteSpace(path)) record.materialPaths.Add(path);
                    var texture = material != null && material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material == null ? null : material.mainTexture;
                    var texturePath = texture == null ? string.Empty : AssetDatabase.GetAssetPath(texture);
                    if (!string.IsNullOrWhiteSpace(texturePath)) record.texturePaths.Add(texturePath);
                }
            }

            record.hasCollider = colliders.Length > 0;
            record.hasLod = lodGroup != null;
        }
    }
}

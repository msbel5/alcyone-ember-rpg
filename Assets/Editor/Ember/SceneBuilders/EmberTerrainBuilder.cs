using UnityEngine;
using UnityEditor;
using System.IO;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// AAA Polished terrain builder. Uses Unity Terrain system and TerrainLayers.
    /// Supports automatic NavMesh-ready static flags.
    /// </summary>
    public static class EmberTerrainBuilder
    {
        public static GameObject BuildGroundPlane(
            Vector3 center,
            float sizeMeters,
            Material material,
            string name = "Ground")
        {
            string terrainDir = "Assets/Scenes/Ember/TerrainData";
            if (!Directory.Exists(terrainDir)) Directory.CreateDirectory(terrainDir);
            
            string dataPath = Path.Combine(terrainDir, name + "_Data.asset");
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 33;
            terrainData.size = new Vector3(sizeMeters, 10f, sizeMeters);
            
            // Create TerrainLayer from material
            if (material != null && material.mainTexture != null)
            {
                var layer = new TerrainLayer();
                layer.diffuseTexture = (Texture2D)material.mainTexture;
                layer.tileSize = new Vector2(5f, 5f);
                
                string layerPath = Path.Combine(terrainDir, name + "_Layer.terrainlayer");
                AssetDatabase.CreateAsset(layer, layerPath);
                terrainData.terrainLayers = new TerrainLayer[] { layer };
            }

            AssetDatabase.CreateAsset(terrainData, dataPath);

            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = name;
            // Center the terrain
            go.transform.position = center - new Vector3(sizeMeters / 2f, 0f, sizeMeters / 2f);
            
            // Unity 6 navigation: StaticEditorFlags.NavigationStatic is
            // deprecated. The modern NavMeshBuilder.CollectSources picks
            // geometry via NavMeshBuildMarkup (passed at bake time) instead
            // of per-GameObject static flags. Setting no nav flag is a no-op
            // for the new bake path; other static flags stay at scene-recipe
            // defaults.
            
            return go;
        }

        public static GameObject BuildRoom(Vector3 center, float width, float depth, float height, Material floorMat, Material wallMat)
        {
            var root = new GameObject("Room");
            root.transform.position = center;

            // Floor
            BuildGroundPlane(center, width > depth ? width : depth, floorMat, "Floor").transform.SetParent(root.transform);

            // Walls
            float halfW = width / 2f;
            float halfD = depth / 2f;
            float halfH = height / 2f;

            BuildWall(center + new Vector3(0, halfH, halfD), new Vector3(width, height, 0.1f), wallMat, "Wall_North").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(0, halfH, -halfD), new Vector3(width, height, 0.1f), wallMat, "Wall_South").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(halfW, halfH, 0), new Vector3(0.1f, height, depth), wallMat, "Wall_East").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(-halfW, halfH, 0), new Vector3(0.1f, height, depth), wallMat, "Wall_West").transform.SetParent(root.transform);

            return root;
        }

        public static GameObject BuildWall(
            Vector3 center,
            Vector3 sizeMeters,
            Material material,
            string name = "Wall")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = sizeMeters;
            var renderer = go.GetComponent<MeshRenderer>();
            
            if (renderer != null && material != null)
            {
                // Create a material instance so we can tile based on wall size
                var mat = new Material(material);
                float tileX = sizeMeters.x > sizeMeters.z ? sizeMeters.x : sizeMeters.z;
                float tileY = sizeMeters.y;
                
                if (mat.shader.name.Contains("Universal Render Pipeline/Lit"))
                {
                    mat.SetVector("_BaseMap_ST", new Vector4(tileX / 2f, tileY / 2f, 0, 0));
                }
                else
                {
                    mat.mainTextureScale = new Vector2(tileX / 2f, tileY / 2f);
                }
                renderer.sharedMaterial = mat;
            }
            
            // Unity 6 navigation: StaticEditorFlags.NavigationStatic is
            // deprecated. The modern NavMeshBuilder.CollectSources picks
            // geometry via NavMeshBuildMarkup (passed at bake time) instead
            // of per-GameObject static flags. Setting no nav flag is a no-op
            // for the new bake path; other static flags stay at scene-recipe
            // defaults.
            
            return go;
        }

    }
}


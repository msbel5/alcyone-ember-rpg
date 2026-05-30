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
        private static string _sceneKey = "Shared";

        public static void BeginScene(string sceneName)
        {
            _sceneKey = Sanitize(sceneName);
        }

        public static GameObject BuildGroundPlane(
            Vector3 center,
            float sizeMeters,
            Material material,
            string name = "Ground")
        {
            string terrainDir = "Assets/Scenes/Ember/TerrainData";
            if (!Directory.Exists(terrainDir)) Directory.CreateDirectory(terrainDir);

            string dataPath = Path.Combine(terrainDir, _sceneKey + "_" + name + "_Data.asset");
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath) != null)
                AssetDatabase.DeleteAsset(dataPath);
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 33;
            terrainData.size = new Vector3(sizeMeters, 10f, sizeMeters);

            var texture = EmberMaterialFactory.ResolveMainTexture(material);
            if (texture == null)
            {
                material = EmberSceneMaterialLibrary.Floor();
                texture = EmberMaterialFactory.ResolveMainTexture(material);
            }

            // Create TerrainLayer from material
            if (texture != null)
            {
                var layer = new TerrainLayer();
                layer.diffuseTexture = texture;
                layer.tileSize = new Vector2(5f, 5f);

                string layerPath = Path.Combine(terrainDir, _sceneKey + "_" + name + "_Layer.terrainlayer");
                if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath) != null)
                    AssetDatabase.DeleteAsset(layerPath);
                AssetDatabase.CreateAsset(layer, layerPath);
                terrainData.terrainLayers = new TerrainLayer[] { layer };
            }

            AssetDatabase.CreateAsset(terrainData, dataPath);

            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = name;
            // Center the terrain
            go.transform.position = center - new Vector3(sizeMeters / 2f, 0f, sizeMeters / 2f);

            // Codex review (PR #203 P1): legacy NavMeshBuilder.BuildNavMesh
            // bake path still relies on NavigationStatic. Phase 14 sprint
            // migrates to NavMeshBuildMarkup; until then keep the flag and
            // silence CS0618.
#pragma warning disable CS0618
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
#pragma warning restore CS0618

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

            var safeWall = wallMat != null ? wallMat : EmberSceneMaterialLibrary.Wall();
            BuildWall(center + new Vector3(0, halfH, halfD), new Vector3(width, height, 0.1f), safeWall, "Wall_North").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(0, halfH, -halfD), new Vector3(width, height, 0.1f), safeWall, "Wall_South").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(halfW, halfH, 0), new Vector3(0.1f, height, depth), safeWall, "Wall_East").transform.SetParent(root.transform);
            BuildWall(center + new Vector3(-halfW, halfH, 0), new Vector3(0.1f, height, depth), safeWall, "Wall_West").transform.SetParent(root.transform);

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

            if (renderer != null)
            {
                // Create a material instance so we can tile based on wall size
                var mat = new Material(material != null ? material : EmberSceneMaterialLibrary.Wall());
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

            // Codex review (PR #203 P1): legacy NavMeshBuilder.BuildNavMesh
            // bake path still relies on NavigationStatic. Phase 14 sprint
            // migrates to NavMeshBuildMarkup; until then keep the flag and
            // silence CS0618.
#pragma warning disable CS0618
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
#pragma warning restore CS0618

            return go;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Shared";
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }

    }
}

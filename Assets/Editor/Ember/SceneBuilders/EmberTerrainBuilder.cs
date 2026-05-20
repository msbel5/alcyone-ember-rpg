using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builds a flat ground tile (single quad scaled to <see cref="sizeMeters"/>) with the
    /// supplied material. For Morrowind/Daggerfall-style outdoor exteriors a flat plane is
    /// a reasonable starting placeholder until a proper heightmap pipeline lands.
    /// </summary>
    public static class EmberTerrainBuilder
    {
        public static GameObject BuildGroundPlane(
            Vector3 center,
            float sizeMeters,
            Material material,
            string name = "Ground")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.position = center;
            // Built-in Plane is 10x10 units at scale 1, so size/10 gives sizeMeters across.
            go.transform.localScale = new Vector3(sizeMeters / 10f, 1f, sizeMeters / 10f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
            return go;
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
                renderer.sharedMaterial = material;
            return go;
        }
    }
}

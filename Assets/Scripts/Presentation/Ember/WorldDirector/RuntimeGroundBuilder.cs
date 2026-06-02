using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Builds the biome-tinted ground plane the realized settlement stands on — the runtime twin of the
    /// editor EmberTerrainBuilder's ground plane, using a primitive Plane + a runtime material (no
    /// AssetDatabase). The plane carries a MeshCollider so the player's CharacterController has a floor.
    /// </summary>
    public static class RuntimeGroundBuilder
    {
        public static GameObject Build(Transform parent, float radius, BiomeKind biome)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); // Unity Plane = 10m across at scale 1
            ground.name = "GeneratedGround";
            ground.transform.SetParent(parent, worldPositionStays: false);
            ground.transform.localPosition = Vector3.zero;

            float scale = (radius * 2f) / 10f; // make the plane wide enough to hold the layout
            ground.transform.localScale = new Vector3(scale, 1f, scale);

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = RuntimeMaterialPalette.Opaque(RuntimeMaterialPalette.GroundColor(biome));
            return ground;
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Deterministic placement helpers that derive spawn and portal points from authored floor footprint.
    /// </summary>
    public static class EmberScenePlacement
    {
        public static GameObject RequireRoomFloor(GameObject roomRoot)
        {
            if (roomRoot == null)
                throw new System.ArgumentNullException(nameof(roomRoot));

            var floor = roomRoot.transform.Find("Floor");
            if (floor == null)
                throw new System.InvalidOperationException($"Room '{roomRoot.name}' does not contain a Floor child.");

            return floor.gameObject;
        }

        public static Vector3 ComputePlayerSpawn(GameObject floor, float verticalOffset = 0.15f)
        {
            var floorBounds = GetFloorBounds(floor);
            var southThird = floorBounds.center.z - (floorBounds.size.z * 0.36f);
            var footprintPoint = new Vector3(floorBounds.center.x, 0f, southThird);
            var floorY = SampleFloorSurfaceY(floor, footprintPoint);
            return new Vector3(footprintPoint.x, floorY + verticalOffset, footprintPoint.z);
        }

        public static Vector3 ComputeEastPortalSpawn(GameObject floor, float eastInset = 1.0f, float verticalOffset = 0.5f)
        {
            var floorBounds = GetFloorBounds(floor);
            var eastX = Mathf.Max(floorBounds.center.x, floorBounds.max.x - eastInset);
            var footprintPoint = new Vector3(eastX, 0f, floorBounds.center.z);
            var floorY = SampleFloorSurfaceY(floor, footprintPoint);
            return new Vector3(eastX, floorY + verticalOffset, floorBounds.center.z);
        }

        public static void AssertInsideFloorFootprint(GameObject floor, Vector3 worldPosition, string context)
        {
            var floorBounds = GetFloorBounds(floor);
            if (worldPosition.x < floorBounds.min.x || worldPosition.x > floorBounds.max.x ||
                worldPosition.z < floorBounds.min.z || worldPosition.z > floorBounds.max.z)
            {
                throw new System.InvalidOperationException(
                    $"{context} lies outside floor footprint. position={worldPosition}, bounds={floorBounds}");
            }
        }

        private static Bounds GetFloorBounds(GameObject floor)
        {
            if (floor == null)
                throw new System.ArgumentNullException(nameof(floor));

            if (floor.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var size = terrain.terrainData.size;
                var center = terrain.transform.position + (size * 0.5f);
                return new Bounds(center, size);
            }

            if (floor.TryGetComponent<Renderer>(out var renderer))
                return renderer.bounds;

            if (floor.TryGetComponent<Collider>(out var collider))
                return collider.bounds;

            throw new System.InvalidOperationException($"Floor object '{floor.name}' has no Terrain, Renderer, or Collider.");
        }

        private static float SampleFloorSurfaceY(GameObject floor, Vector3 worldPosition)
        {
            if (floor.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
                return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;

            if (floor.TryGetComponent<Renderer>(out var renderer))
                return renderer.bounds.max.y;

            if (floor.TryGetComponent<Collider>(out var collider))
                return collider.bounds.max.y;

            return floor.transform.position.y;
        }
    }
}

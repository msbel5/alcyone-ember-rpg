using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Runtime-only building footprint used by generated NPC billboards to stay reachable until real interiors
    /// and doors exist. It does not affect deterministic simulation placement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingAccessibilityVolume : MonoBehaviour
    {
        private float _halfX;
        private float _halfZ;
        private float _margin;

        public void Configure(float sizeX, float sizeZ, float margin)
        {
            _halfX = Mathf.Max(0.1f, sizeX * 0.5f);
            _halfZ = Mathf.Max(0.1f, sizeZ * 0.5f);
            _margin = Mathf.Max(0.1f, margin);
        }

        public bool TryPushOutside(Vector3 worldPosition, out Vector3 adjusted)
        {
            adjusted = worldPosition;
            var local = transform.InverseTransformPoint(worldPosition);
            float limitX = _halfX + _margin;
            float limitZ = _halfZ + _margin;
            if (Mathf.Abs(local.x) > limitX || Mathf.Abs(local.z) > limitZ)
                return false;

            float right = limitX - local.x;
            float left = local.x + limitX;
            float forward = limitZ - local.z;
            float back = local.z + limitZ;
            float best = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, forward));

            if (best == left) local.x = -limitX;
            else if (best == right) local.x = limitX;
            else if (best == back) local.z = -limitZ;
            else local.z = limitZ;

            adjusted = transform.TransformPoint(local);
            return true;
        }
    }
}

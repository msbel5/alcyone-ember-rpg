using EmberCrpg.Presentation.Ember.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// Keeps generated NPC billboards outside temporary building shells, so quest givers remain clickable
    /// before the project has real doors/interiors. Presentation-only: simulation positions are untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedNpcAccessibilityGuard : MonoBehaviour
    {
        private BuildingAccessibilityVolume[] _volumes = System.Array.Empty<BuildingAccessibilityVolume>();
        private float _nextRefreshTime;

        private void LateUpdate()
        {
            RefreshVolumesIfNeeded();
            if (_volumes.Length == 0) return;

            var pos = transform.position;
            bool moved = false;
            for (int i = 0; i < _volumes.Length; i++)
            {
                var volume = _volumes[i];
                if (volume != null && volume.TryPushOutside(pos, out var adjusted))
                {
                    pos = adjusted;
                    moved = true;
                }
            }

            if (moved)
                transform.position = pos;
        }

        private void RefreshVolumesIfNeeded()
        {
            if (Time.time < _nextRefreshTime && _volumes.Length > 0) return;
            _nextRefreshTime = Time.time + 1f;
            _volumes = FindObjectsByType<BuildingAccessibilityVolume>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }
    }
}

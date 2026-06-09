using EmberCrpg.Domain.Actors;
using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Presentation bridge: feeds the quest compass with the live FPS rig position without mutating the
    /// deterministic domain player actor. The adapter re-bases this local tile through its billboard origin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestGuidancePlayerTracker : MonoBehaviour
    {
        private GridPosition _lastLocalTile;
        private bool _hasLastLocalTile;

        private void Start()
        {
            Push(force: true);
        }

        private void Update()
        {
            Push(force: false);
        }

        private void Push(bool force)
        {
            if (!(EmberDomainAdapterLocator.Current is IQuestGuidanceTracker tracker))
                return;

            var p = transform.position;
            var localTile = new GridPosition(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
            if (!force && _hasLastLocalTile && localTile.Equals(_lastLocalTile))
                return;

            _lastLocalTile = localTile;
            _hasLastLocalTile = true;
            tracker.UpdateQuestGuidancePlayerLocalPosition(localTile);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using EmberCrpg.Presentation.Ember.Save;

namespace EmberCrpg.Presentation.Ember.Interaction
{
    /// <summary>
    /// Implements a scene portal that loads a new scene on trigger or activation.
    /// </summary>
    public sealed class EmberScenePortal : MonoBehaviour
    {
        [SerializeField] private string _targetSceneName;

        public string TargetSceneName => _targetSceneName;

        public void SetTarget(string targetSceneName)
        {
            _targetSceneName = targetSceneName;
        }

        public void Activate()
        {
            if (string.IsNullOrEmpty(_targetSceneName)) return;
            // LEFT-014: a portal serialized with a stale/renamed target would otherwise call
            // SceneManager.LoadScene on a name that isn't in the build and hard-fail. CanStreamedLevelBeLoaded
            // is the runtime-safe build-registry check (works in player builds, not just the Editor).
            if (!Application.CanStreamedLevelBeLoaded(_targetSceneName))
            {
                Debug.LogWarning($"EmberScenePortal: target scene '{_targetSceneName}' is not in the build; ignoring activation.");
                return;
            }

            EmberSaveService.TryAutosaveActiveScene();
            SceneManager.LoadScene(_targetSceneName);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Simple check: does the other object have an EmberFirstPersonController?
            if (other.GetComponentInParent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>() != null)
            {
                Activate();
            }
        }
    }
}

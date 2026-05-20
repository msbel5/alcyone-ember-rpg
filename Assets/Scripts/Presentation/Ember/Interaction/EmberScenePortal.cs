using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (!string.IsNullOrEmpty(_targetSceneName))
            {
                SceneManager.LoadScene(_targetSceneName);
            }
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

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Interaction
{
    /// <summary>
    /// Tagging component for interactable objects and NPCs.
    /// </summary>
    public sealed class EmberInteractable : MonoBehaviour
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _topic;

        public string DisplayName => _displayName;
        public string Topic => _topic;

        public void Setup(string displayName, string topic)
        {
            _displayName = displayName;
            _topic = topic;
        }
    }
}

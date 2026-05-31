using UnityEngine;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Presentation.Ember.Interaction
{
    /// <summary>
    /// Tagging component for interactable objects and NPCs.
    /// </summary>
    public sealed class EmberInteractable : MonoBehaviour
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _topic;

        // DLG-01: optional STABLE actor id this interactable speaks for. Serialized as a string so
        // scenes can leave it blank (legacy authored interactables) — an empty/zero/unparseable value
        // means "no id, resolve by display name". When set, the raycaster prefers the id-keyed
        // GetDialogSource(ActorId) overload, which looks the actor up in WorldState.Actors rather than
        // scanning by name (the bug this fixes). Stored as text because ActorId is a ulong-backed
        // struct that the Unity inspector cannot serialize directly.
        [SerializeField] private string _actorId;

        public string DisplayName => _displayName;
        public string Topic => _topic;

        /// <summary>True when this interactable carries a usable stable actor id (non-blank, parses to a non-zero ulong).</summary>
        public bool HasActorId => TryGetActorId(out _);

        /// <summary>The stable <see cref="ActorId"/> this interactable speaks for, or <see cref="ActorId"/>.Empty when none is authored.</summary>
        public ActorId ActorId => TryGetActorId(out var id) ? id : default;

        private bool TryGetActorId(out ActorId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(_actorId)) return false;
            if (!ulong.TryParse(_actorId.Trim(), out var value) || value == 0UL) return false;
            id = new ActorId(value);
            return true;
        }

        public void Setup(string displayName, string topic)
        {
            _displayName = displayName;
            _topic = topic;
        }

        /// <summary>DLG-01: author both the display name and the stable actor id in one call (used by any runtime spawner).</summary>
        public void Setup(string displayName, string topic, ActorId actorId)
        {
            _displayName = displayName;
            _topic = topic;
            _actorId = actorId.IsEmpty ? string.Empty : actorId.Value.ToString();
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// Drives one actor's transform from an injected snapshot source. The view never
    /// reads the simulation directly: the host pushes a <see cref="ActorViewState"/>
    /// each tick. Visual interpolation between two snapshots stays inside the view
    /// so the simulation can advance at its own deterministic rate.
    /// </summary>
    public interface IDamageSink
    {
        void Apply(int amount);
    }

    [DisallowMultipleComponent]
    public sealed class ActorView : MonoBehaviour, IDamageSink
    {
        [SerializeField] private float _interpolationSpeed = 8f;
        [SerializeField] private Transform _billboard;

        private ActorViewState _target;
        private bool _hasTarget;
        private SpriteRenderer _renderer;
        private float _tintRemaining;

        private void Awake()
        {
            if (_billboard == null)
                _billboard = transform.Find("Billboard");
            
            if (_billboard != null)
                _renderer = _billboard.GetComponent<SpriteRenderer>();
        }

        public void SetTarget(ActorViewState state)
        {
            _target = state;
            _hasTarget = true;
        }

        public void Apply(int amount)
        {
            _tintRemaining = 0.2f;
            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
            if (adapter != null)
            {
                adapter.LogCombat($"{gameObject.name} takes {amount} damage!");
            }
        }

        private void Update()
        {
            if (!_hasTarget) return;
            var t = Mathf.Clamp01(_interpolationSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _target.WorldPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _target.WorldRotation, t);
            if (_billboard != null)
                _billboard.gameObject.SetActive(_target.Visible);

            if (_renderer != null)
            {
                if (_tintRemaining > 0)
                {
                    _tintRemaining -= Time.deltaTime;
                    _renderer.color = Color.red;
                }
                else
                {
                    _renderer.color = Color.white;
                }
            }
        }
    }

    /// <summary>Per-tick visual snapshot, sourced from a domain ActorRecord adapter.</summary>
    public readonly struct ActorViewState
    {
        public readonly Vector3 WorldPosition;
        public readonly Quaternion WorldRotation;
        public readonly bool Visible;
        public ActorViewState(Vector3 worldPosition, Quaternion worldRotation, bool visible)
        {
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
            Visible = visible;
        }
    }
}

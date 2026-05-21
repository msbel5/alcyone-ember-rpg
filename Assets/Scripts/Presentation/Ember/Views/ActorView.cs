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
        [SerializeField] private string _domainActorKey;
        [SerializeField] private float _interpolationSpeed = 8f;
        [SerializeField] private Transform _billboard;

        [Header("Animation")]
        [SerializeField] private float _walkCycleFrequency = 0.4f;
        [SerializeField] private float _idleFloatFrequency = 1.5f;
        [SerializeField] private float _idleFloatAmplitude = 0.05f;
        
        public string DomainActorKey =>
            string.IsNullOrEmpty(_domainActorKey) ? gameObject.name : _domainActorKey;

        private ActorViewState _target;
        private bool _hasTarget;
        private SpriteRenderer _renderer;
        private float _tintRemaining;
        private float _shakeRemaining;
        private Vector3 _billboardBaseLocalPos;
        private float _walkTimer;
        private Vector3 _lastPosition;

        private void Awake()
        {
            if (_billboard == null)
                _billboard = transform.Find("Billboard");
            
            if (_billboard != null)
            {
                _renderer = _billboard.GetComponent<SpriteRenderer>();
                _billboardBaseLocalPos = _billboard.localPosition;
            }
            _lastPosition = transform.position;
        }

        public void SetTarget(ActorViewState state)
        {
            _target = state;
            _hasTarget = true;
        }

        public void Apply(int amount)
        {
            _tintRemaining = 0.2f;
            _shakeRemaining = 0.2f;
            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
            if (adapter != null)
            {
                adapter.LogCombat($"{gameObject.name} takes {amount} damage!");
            }
        }

        private void Update()
        {
            if (!_hasTarget) return;

            // 1. Interpolation
            var t = Mathf.Clamp01(_interpolationSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _target.WorldPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _target.WorldRotation, t);
            
            if (_billboard == null) return;
            _billboard.gameObject.SetActive(_target.Visible);

            // 2. Animation Logic
            float speed = Time.deltaTime > 0 ? (transform.position - _lastPosition).magnitude / Time.deltaTime : 0f;
            _lastPosition = transform.position;

            if (speed > 0.05f)
            {
                // Walk cycle: 2-frame ping-pong via flipX
                _walkTimer += Time.deltaTime;
                if (_walkTimer > _walkCycleFrequency)
                {
                    _walkTimer = 0f;
                    if (_renderer != null) _renderer.flipX = !_renderer.flipX;
                }
                _billboard.localPosition = _billboardBaseLocalPos;
            }
            else
            {
                // Idle float
                float floatOffset = Mathf.Sin(Time.time * _idleFloatFrequency) * _idleFloatAmplitude;
                _billboard.localPosition = _billboardBaseLocalPos + new Vector3(0f, floatOffset, 0f);
                _walkTimer = 0f;
            }

            // 3. Combat Effects
            if (_tintRemaining > 0)
            {
                _tintRemaining -= Time.deltaTime;
                if (_renderer != null) _renderer.color = Color.red;
            }
            else
            {
                if (_renderer != null) _renderer.color = Color.white;
            }

            if (_shakeRemaining > 0)
            {
                _shakeRemaining -= Time.deltaTime;
                float shake = 0.05f;
                _billboard.localPosition += new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f);
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

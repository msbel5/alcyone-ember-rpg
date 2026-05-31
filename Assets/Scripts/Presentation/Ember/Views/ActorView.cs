using UnityEngine;
using EmberCrpg.Domain.Core;

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

    // SOUL-04 (position sync — DONE): EmberWorldHost.PushWorldViews() pushes a fresh ActorViewState
    // into every ActorView each tick. With an authored DomainActorId (preferred) or DomainActorKey
    // (legacy name), the host reads the matching record from WorldState.Actors by STABLE id and
    // projects its GridPosition to world space, so SOUL-03 (ScheduleSystem) movement is now visible on
    // existing billboards. SetTarget below interpolates toward that position. No name uniqueness is
    // required when an id is authored.
    //
    // SOUL-04 (spawn-from-worldgen — STILL FLAGGED, needs an Editor/black-box pass): scenes still author
    // a FIXED cast of ~5 ActorViews. Making the full generated population visible needs a runtime
    // spawner that instantiates one billboard ActorView per WorldState.Actors / NpcSeeds entry at its
    // world->scene position and stamps DomainActorId on each. That is scene/prefab-authoring work
    // (billboard prefab, sprite/material wiring, culling) that must be proven with a Unity Editor
    // screenshot — deliberately NOT done in this headless presentation pass. Until then, generated NPCs
    // exist and move in the simulation but only the authored views render them.
    [DisallowMultipleComponent]
    public sealed class ActorView : MonoBehaviour, IDamageSink
    {
        [SerializeField] private string _domainActorKey;
        // SOUL-04: optional STABLE actor id this billboard tracks. Serialized as a string so legacy
        // scenes can leave it blank (the host then keys position-sync by DomainActorKey/name). When set,
        // the host reads WorldState.Actors by id, so movement stays correct even if two actors share a
        // name. Text-backed because ActorId is a ulong struct the inspector cannot serialize directly.
        [SerializeField] private string _domainActorId;
        [SerializeField] private float _interpolationSpeed = 8f;
        [SerializeField] private Transform _billboard;

        [Header("Animation")]
        [SerializeField] private float _walkCycleFrequency = 0.4f;
        [SerializeField] private float _idleFloatFrequency = 1.5f;
        [SerializeField] private float _idleFloatAmplitude = 0.05f;
        
        public string DomainActorKey =>
            string.IsNullOrEmpty(_domainActorKey) ? gameObject.name : _domainActorKey;

        /// <summary>SOUL-04: true when this view carries a usable stable actor id (non-blank, parses to a non-zero ulong).</summary>
        public bool HasDomainActorId => TryGetDomainActorId(out _);

        /// <summary>SOUL-04: the stable <see cref="ActorId"/> this view tracks, or <see cref="ActorId"/>.Empty when none is authored.</summary>
        public ActorId DomainActorId => TryGetDomainActorId(out var id) ? id : default;

        private bool TryGetDomainActorId(out ActorId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(_domainActorId)) return false;
            if (!ulong.TryParse(_domainActorId.Trim(), out var value) || value == 0UL) return false;
            id = new ActorId(value);
            return true;
        }

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
                // EMB-040: UnityEngine.Random is presentation-only here (cosmetic billboard jitter).
                // It never feeds Domain/Simulation or the save — keep it that way (docs/DETERMINISM.md).
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

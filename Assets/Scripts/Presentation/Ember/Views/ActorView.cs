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
    // SOUL-04 (spawn-from-worldgen — IMPLEMENTED, build-safe; sprite polish still wants a visual pass):
    // scenes still author only a FIXED cast of ~5 ActorViews, so the generated population was invisible.
    // EmberGeneratedActorSpawner (ensured by EmberWorldHost) now instantiates one billboard ActorView per
    // nearby WorldState.Actors record that has no authored view, stamps its stable id via
    // BindDomainActorId, and positions it by the SAME GridPosition->world projection the adapter uses;
    // EmberWorldHost re-scans its ActorView set afterwards so the existing id-keyed PushWorldViews sync
    // drives SOUL-03 (ScheduleSystem) movement on the spawned views too. The spawn is CAPPED to the
    // nearest N (<=12) so a 750-NPC world never floods the scene. The construction (root + "Billboard"
    // child + SpriteRenderer + CameraFacingBillboard + ActorView, mirroring EmberWorldspaceBuilder.
    // SpawnActor) is build-safe and uses the host SpriteRegistry's placeholder sprite; choosing a real
    // per-role sprite/material and confirming billboard facing/scale is the only part that still wants a
    // Unity Editor screenshot (see EmberGeneratedActorSpawner's header for the precise visual-proof TODO).
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

        /// <summary>
        /// SOUL-04 (spawn-from-worldgen): stamp the stable actor id onto a billboard built at
        /// RUNTIME (the spawner can't use the Editor's SerializedObject path the scene builder uses).
        /// Authored scenes still serialize <c>_domainActorId</c> in the asset; this is only for views
        /// instantiated live by <see cref="EmberGeneratedActorSpawner"/>. Stored as the same text form
        /// the inspector uses so the id read path (TryGetDomainActorId) is identical for both origins.
        /// Ignores the empty sentinel so a bad id never silently shadows the name-keyed fallback.
        /// </summary>
        public void BindDomainActorId(ActorId id)
        {
            _domainActorId = id.IsEmpty ? string.Empty : id.Value.ToString();
        }

        private ActorViewState _target;
        private bool _hasTarget;
        private SpriteRenderer _renderer;
        private float _tintRemaining;
        private float _shakeRemaining;
        private Vector3 _billboardBaseLocalPos;
        private float _walkTimer;
        private Vector3 _lastPosition;

        // Cosmetic idle wander (presentation-only; never written back to the sim — see the note at Update's
        // shake block). Generated NPCs were all hydrated onto one settlement tile and the per-tick position
        // sync re-stacked them into a static clump. When enabled, the view strolls within a small radius
        // around its sim anchor so NPCs spread out and look alive; when the sim actually moves the actor
        // (a job target), the anchor moves and the NPC follows, still milling. Cap-bounded by the spawner.
        private bool _wander;
        private float _wanderRadius;
        private float _wanderSpeed = 0.6f;
        private Vector3 _wanderCurrent;
        private Vector3 _wanderGoal;
        private float _wanderRepathTimer;

        // Overworld walkers (set by EmberGeneratedActorSpawner): when >0 the billboard GLIDES toward its sim
        // position at this real m/s instead of the exponential chase, so the per-tick 1-tile colony schedule
        // reads as continuous walking. Combat billboards leave it 0 and keep the snappy chase.
        private float _groundSpeed;

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

        /// <summary>
        /// Turn on cosmetic idle wander for a generated NPC billboard (called by EmberGeneratedActorSpawner).
        /// Uses UnityEngine.Random for the stroll only — purely visual, never feeds Domain/Simulation or the
        /// save (docs/DETERMINISM.md), exactly like the billboard jitter below. Starts at a random in-radius
        /// offset so a settlement's NPCs are spread the instant they spawn rather than overlapping.
        /// </summary>
        public void EnableWander(float radius)
        {
            _wander = true;
            _wanderRadius = Mathf.Max(0.5f, radius);
            Vector2 start = Random.insideUnitCircle * _wanderRadius;
            _wanderCurrent = new Vector3(start.x, 0f, start.y);
            PickWanderGoal();
        }

        private void PickWanderGoal()
        {
            Vector2 g = Random.insideUnitCircle * _wanderRadius;
            _wanderGoal = new Vector3(g.x, 0f, g.y);
            _wanderRepathTimer = 2f + (Random.value * 4f);
        }

        /// <summary>Make this billboard walk toward its sim position at a steady ground speed (m/s) — used for
        /// generated NPCs so the per-tick colony schedule reads as smooth walking, not a stride-then-pause.</summary>
        public void SetGroundSpeed(float metersPerSecond)
        {
            _groundSpeed = Mathf.Max(0f, metersPerSecond);
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

            // 1. Interpolation (toward the sim position, plus the cosmetic wander offset when enabled)
            Vector3 targetPos = _target.WorldPosition;
            if (_wander)
            {
                _wanderRepathTimer -= Time.deltaTime;
                if (_wanderRepathTimer <= 0f) PickWanderGoal();
                _wanderCurrent = Vector3.MoveTowards(_wanderCurrent, _wanderGoal, _wanderSpeed * Time.deltaTime);
                targetPos += _wanderCurrent;
            }
            var t = Mathf.Clamp01(_interpolationSpeed * Time.deltaTime);
            if (_groundSpeed > 0f && (transform.position - targetPos).sqrMagnitude <= 25f)
                // Mid-walk: glide at a constant ground speed so the per-tick 1-tile sim steps look continuous
                // instead of stride-then-pause. Beyond 5 m (spawn/teleport) fall through to the snap below.
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _groundSpeed * Time.deltaTime);
            else
                transform.position = Vector3.Lerp(transform.position, targetPos, t);
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

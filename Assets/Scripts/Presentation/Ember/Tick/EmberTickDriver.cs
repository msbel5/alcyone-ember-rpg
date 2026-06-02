using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Tick
{
    /// <summary>
    /// Deterministic fixed-rate ticker. Accumulates real time and emits an integer tick
    /// count to a single subscriber. The driver is wall-clock independent within a frame;
    /// catch-up is bounded so the editor cannot lock during long pauses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberTickDriver : MonoBehaviour
    {
        public interface ITickListener { void OnTick(int tickIndex); }

        // 0.8333 s/tick: with the coherent 1440 ticks/game-day (EmberRuntimeOptions.Normalize) a full day/night
        // lasts 1440 * 0.8333 ≈ 1200 s = 20 real minutes, and the per-tick schedule mover walks ~1.2 m/s
        // (1 tile/tick). Determinism is unaffected — all sim gating is counted in ticks, not seconds.
        [SerializeField] private float _tickIntervalSeconds = 0.8333f;
        [SerializeField] private int _maxCatchupTicksPerFrame = 8;

        public ITickListener Listener { get; set; }
        public int CurrentTick { get; private set; }

        private float _accumulator;
        private bool _paused;

        public void Pause(bool paused) => _paused = paused;

        /// <summary>
        /// Codex review on PR #185 (P1): align the driver's running counter and
        /// sub-tick accumulator to a restored tick. Without this, a save loaded
        /// at e.g. tick 500 was clobbered on the next frame when the driver
        /// emitted OnTick(1) → adapter.AdvanceTick(1). Call this immediately
        /// after IDomainSimulationAdapter.RestoreStateJson so the timeline
        /// resumes from the saved point.
        /// </summary>
        public void AlignTo(int tickIndex)
        {
            if (tickIndex < 0) tickIndex = 0;
            CurrentTick = tickIndex;
            _accumulator = 0f;
        }

        private void Update()
        {
            if (_paused || Listener == null || _tickIntervalSeconds <= 0f) return;

            _accumulator += Time.deltaTime;
            int budget = _maxCatchupTicksPerFrame;
            while (_accumulator >= _tickIntervalSeconds && budget-- > 0)
            {
                _accumulator -= _tickIntervalSeconds;
                CurrentTick++;
                Listener.OnTick(CurrentTick);
            }
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F3/audio v1 (ship blocker): a fully PROCEDURAL minimum sound set — no asset files, every clip is
    /// synthesized at attach time (deterministic, seeded). Footsteps follow real CharacterController motion,
    /// a wind ambience loops underneath, an encounter sting rides the world-encounter signal, and the UI
    /// clicks. Honest scope: combat per-swing sounds and music are v2.
    /// </summary>
    public static class RuntimeAudioForge
    {
        public static AudioClip Footstep, Wind, Sting, Click;

        public static void EnsureForged()
        {
            if (Footstep != null) return;
            Footstep = Synth("forge_footstep", 0.13f, (t, rng) => Mathf.Pow(1f - t, 3f) * (rng() * 2f - 1f) * 0.8f, smooth: 6);
            Wind = Synth("forge_wind", 3.0f, (t, rng) => (rng() * 2f - 1f) * 0.16f, smooth: 24);
            Sting = Synth("forge_sting", 0.45f, (t, rng) =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(660f, 110f, t) * t) * Mathf.Pow(1f - t, 2f) * 0.6f, smooth: 0);
            Click = Synth("forge_click", 0.05f, (t, rng) =>
                Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Pow(1f - t, 4f) * 0.5f, smooth: 0);
            Debug.Log("[Audio] forged 4 procedural clips (footstep, wind, sting, click).");
        }

        private static AudioClip Synth(string name, float seconds, System.Func<float, System.Func<float>, float> wave, int smooth)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(seconds * rate);
            var data = new float[count];
            uint state = 2463534242u; // fixed seed: same clip every run (determinism contract)
            System.Func<float> rng = () =>
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (state & 0xFFFFFF) / (float)0x1000000;
            };
            for (int i = 0; i < count; i++)
                data[i] = wave(i / (float)count, rng);
            for (int pass = 0; pass < smooth; pass++) // cheap lowpass: noise → soft thud / wind
                for (int i = 1; i < count; i++)
                    data[i] = (data[i] + data[i - 1]) * 0.5f;
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Player-rig audio behaviour: motion-driven footsteps + looping ambience + encounter sting.</summary>
    public sealed class RuntimeAudioDirector : MonoBehaviour
    {
        private CharacterController _controller;
        private AudioSource _oneShot, _ambience;
        private Vector3 _lastPos;
        private float _stepClock;

        public static void Attach(GameObject playerRig)
        {
            if (playerRig == null || playerRig.GetComponent<RuntimeAudioDirector>() != null) return;
            RuntimeAudioForge.EnsureForged();
            playerRig.AddComponent<RuntimeAudioDirector>();
        }

        public static void PlayUiClick()
        {
            RuntimeAudioForge.EnsureForged();
            var rig = GameObject.Find("PlayerRig");
            var self = rig != null ? rig.GetComponent<RuntimeAudioDirector>() : null;
            if (self != null && self._oneShot != null)
                self._oneShot.PlayOneShot(RuntimeAudioForge.Click, 0.7f);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _oneShot = gameObject.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.clip = RuntimeAudioForge.Wind;
            _ambience.loop = true;
            _ambience.volume = 0.35f;
            _ambience.Play();
            _lastPos = transform.position;
            Debug.Log("[Audio] rig attached — ambience playing, footsteps armed.");
        }

        private void Update()
        {
            // Footsteps from REAL horizontal motion (grounded), not key state.
            var pos = transform.position;
            var delta = pos - _lastPos;
            delta.y = 0f;
            _lastPos = pos;
            bool grounded = _controller == null || _controller.isGrounded;
            if (grounded && delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) > 1.2f)
            {
                _stepClock += Time.deltaTime;
                if (_stepClock >= 0.45f)
                {
                    _stepClock = 0f;
                    _oneShot.PlayOneShot(RuntimeAudioForge.Footstep, 0.8f);
                }
            }

            if (EmberCrpg.Presentation.Ember.Adapters.WorldEncounterStingFeed.Consume())
                _oneShot.PlayOneShot(RuntimeAudioForge.Sting, 0.9f);
        }
    }
}

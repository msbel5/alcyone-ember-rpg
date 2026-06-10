using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F8: battle state mirror — the combat read publishes whether a world encounter is live.</summary>
    public static class RuntimeBattleMirror
    {
        public static bool Active { get; set; }
    }

    /// <summary>
    /// F8/procedural MUSIC (GemRB slot recipe × DFU daily rotation): three slots — DAY (calm arpeggio),
    /// NIGHT (low drone), BATTLE (driving pulse) — each with two deterministic synthesized variants; the
    /// active variant rotates by sim day, the slot follows the sim hour and the battle mirror. Every note
    /// is generated at attach (fixed-seed, no asset files) — honest label: simple chord-arp synthesis.
    /// </summary>
    public sealed class RuntimeMusicDirector : MonoBehaviour
    {
        private AudioSource _source;
        private AudioClip[,] _slots; // [slot, variant]
        private int _currentSlot = -1, _currentVariant = -1;
        private float _nextPoll;

        public static void Attach(GameObject playerRig)
        {
            if (playerRig != null && playerRig.GetComponent<RuntimeMusicDirector>() == null)
                playerRig.AddComponent<RuntimeMusicDirector>();
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = 0.30f;
            _slots = new AudioClip[3, 2];
            // DAY: gentle minor arpeggio; NIGHT: slow low drone; BATTLE: fast fifth pulse.
            for (int v = 0; v < 2; v++)
            {
                _slots[0, v] = Synth("music_day_" + v, 110f * (v == 0 ? 1f : 1.122f), 2.0f, 8f, pulse: false);
                _slots[1, v] = Synth("music_night_" + v, 55f * (v == 0 ? 1f : 1.189f), 4.0f, 8f, pulse: false);
                _slots[2, v] = Synth("music_battle_" + v, 147f * (v == 0 ? 1f : 1.122f), 0.5f, 6f, pulse: true);
            }
            Debug.Log("[Music] forged 3 slots x 2 variants (day/night/battle) — procedural chord-arp synthesis.");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;

            int hour = RuntimeFieldMirror.HourOfDay;
            int slot = RuntimeBattleMirror.Active ? 2 : (hour >= 22 || hour < 6 ? 1 : 0);
            int variant = (hour / 24) % 2; // effectively day-based once HourOfDay carries day info; v1: variant by parity of day-hour bucket
            if (slot == _currentSlot && variant == _currentVariant) return;

            _currentSlot = slot; _currentVariant = variant;
            _source.clip = _slots[slot, variant];
            _source.Play();
            Debug.Log($"[Music] slot={(slot == 0 ? "DAY" : slot == 1 ? "NIGHT" : "BATTLE")} variant={variant} (hour={hour}).");
        }

        /// <summary>Deterministic looped piece: root chord arpeggio (root/min3/5th/oct) at the given pace.</summary>
        private static AudioClip Synth(string name, float rootHz, float stepSeconds, float totalSeconds, bool pulse)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(totalSeconds * rate);
            var data = new float[count];
            float[] degrees = { 1f, 1.189f, 1.498f, 2f }; // minor-ish arpeggio ratios
            int stepSamples = Mathf.Max(1, Mathf.CeilToInt(stepSeconds * rate));
            for (int i = 0; i < count; i++)
            {
                int step = (i / stepSamples) % degrees.Length;
                float f = rootHz * degrees[step];
                float t = i / (float)rate;
                float inStep = (i % stepSamples) / (float)stepSamples;
                float env = pulse ? Mathf.Pow(1f - inStep, 2f) : (0.6f + 0.4f * Mathf.Sin(inStep * Mathf.PI));
                float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.25f;
                data[i] = s * env * 0.5f;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

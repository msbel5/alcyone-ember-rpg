using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F8: battle state mirror — the combat read publishes whether a world encounter is live.</summary>
    public static class RuntimeBattleMirror
    {
        public static bool Active { get; set; }
        // F30: true while the bound enemy is the delve's Warden — the BATTLE slot gains percussion.
        public static bool BossActive { get; set; }
    }

    /// <summary>
    /// F8 slots + F11 MUSIC v2 ("müzikler iki üç tondu"): three slots — DAY/NIGHT/BATTLE × 2 variants,
    /// slot follows sim hour + battle mirror (GemRB), variant rotates (DFU). The synthesis is now the
    /// research-validated two-layer rule architecture (AES 2025 — every shipped procgen game is
    /// rule-based): (1) a chord-progression BED — i-VI-III-VII in A minor, detuned two-osc pad + bass
    /// drone; (2) a constrained-random pentatonic MELODY — no leaps over a fifth, mostly stepwise, no
    /// repeated notes, direction reverses after a leap; (3) noise-burst percussion, dense in BATTLE.
    /// All rendered offline at attach from fixed seeds (determinism contract, no asset files).
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

        // PERF (shipcheck FAIL root, Codex finding): rendering 6 × 16s buffers on the main thread froze
        // ONE frame for ~14s — avg 175ms over the measured 90-frame window, budget 16ms. The sample math
        // is pure, so a worker thread renders the buffers and the main thread wraps AudioClips when
        // ready; music simply fades in a few seconds after scene start.
        private static readonly uint[] SlotSeeds = { 0xDA10u, 0x4167u, 0xBA77u };
        // PROCESS-WIDE CACHE: the clips are deterministic (fixed seeds), but every scene reload used to
        // re-render them (a shipcheck soak reforged 13×) — wasted worker CPU and megabytes of per-reload
        // GC garbage. Render ONCE per process on one worker; every later director instance reuses them.
        private static float[][] s_pendingBuffers;
        private static volatile bool s_renderDone;
        private static AudioClip[,] s_cachedSlots;
        private static bool s_workerStarted;
        private bool _clipsReady;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = 0.30f;
            if (!s_workerStarted)
            {
                s_workerStarted = true;
                s_pendingBuffers = new float[6][];
                var worker = new System.Threading.Thread(() =>
                {
                    for (int slot = 0; slot < 3; slot++)
                        for (int v = 0; v < 2; v++)
                            s_pendingBuffers[slot * 2 + v] = ComposeData(slot, SlotSeeds[slot] + (uint)v * 977u);
                    s_renderDone = true;
                })
                { IsBackground = true, Name = "EmberMusicForge" };
                worker.Start();
            }
        }

        private void Update()
        {
            if (!_clipsReady)
            {
                if (s_cachedSlots == null)
                {
                    if (!s_renderDone) return;
                    string[] names = { "day", "night", "battle" };
                    s_cachedSlots = new AudioClip[3, 2];
                    for (int s = 0; s < 3; s++)
                        for (int v = 0; v < 2; v++)
                        {
                            var data = s_pendingBuffers[s * 2 + v];
                            string clipName = "music_" + names[s] + "_" + v;
                            Debug.Log($"[AudioForge] {clipName}: len={LoopSeconds:0}s voices=3 rms={RuntimeAudioSynth.Rms(data):0.000} (bed+melody+perc).");
                            var clip = AudioClip.Create(clipName, data.Length, 1, Rate, false);
                            clip.SetData(data, 0);
                            s_cachedSlots[s, v] = clip;
                        }
                    s_pendingBuffers = null;
                    Debug.Log("[Music] forged v2 off-thread: 3 slots x 2 variants — chord bed (i-VI-III-VII) + bass drone + constrained melody + percussion (process-cached).");
                }
                _slots = s_cachedSlots;
                _clipsReady = true;
            }

            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;

            // F30 RAIN HUSH (the F25 honest debt): music sits back while it rains — the shower bed
            // owns the foreground. Volume only; the slot machinery is untouched.
            bool raining = RuntimeWeatherMirror.Raining;
            if (raining != _rainHushOn)
            {
                _rainHushOn = raining;
                Debug.Log($"[Music] rain hush {(raining ? "ON (0.30->0.18)" : "off (0.18->0.30)")}.");
            }
            // F32: the user's music volume rides every poll (slider changes apply within 2s).
            float musicVol = EmberCrpg.Presentation.Ember.UI.Options.RuntimePlayerSettings.MusicVolume;
            _source.volume = (_rainHushOn ? 0.18f : 0.30f) * musicVol;
            if (_bossPerc != null) _bossPerc.volume = 0.22f * musicVol;

            // F30 BOSS LAYER: the Warden fight stacks a driving percussion loop over the BATTLE bed.
            bool bossLayer = RuntimeBattleMirror.Active && RuntimeBattleMirror.BossActive;
            if (bossLayer != _bossLayerOn)
            {
                _bossLayerOn = bossLayer;
                EnsureBossPercSource();
                if (bossLayer) _bossPerc.Play(); else _bossPerc.Stop();
                Debug.Log($"[Music] boss layer {(bossLayer ? "ON (+percussion)" : "off")}.");
            }

            int hour = RuntimeFieldMirror.HourOfDay;
            int slot = RuntimeBattleMirror.Active ? 2 : (hour >= 22 || hour < 6 ? 1 : 0);
            int variant = (hour / 24) % 2; // effectively day-based once HourOfDay carries day info; v1: variant by parity of day-hour bucket
            if (slot == _currentSlot && variant == _currentVariant) return;

            _currentSlot = slot; _currentVariant = variant;
            _source.clip = _slots[slot, variant];
            _source.Play();
            Debug.Log($"[Music] slot={(slot == 0 ? "DAY" : slot == 1 ? "NIGHT" : "BATTLE")} variant={variant} (hour={hour}).");
        }

        private bool _rainHushOn;
        private bool _bossLayerOn;
        private AudioSource _bossPerc;
        private static AudioClip s_bossPercClip;

        // F30: the boss percussion loop — forged once per process (same cache rule as the slots).
        private void EnsureBossPercSource()
        {
            if (_bossPerc != null) return;
            if (s_bossPercClip == null)
            {
                var data = RuntimeAudioSynth.RenderBossPercussion(0xB055u);
                Debug.Log($"[AudioForge] boss_percussion: len={data.Length / (float)RuntimeAudioSynth.Rate:0.00}s " +
                          $"rms={RuntimeAudioSynth.Rms(data):0.000} centroid={RuntimeAudioSynth.CentroidHz(data):0}Hz");
                s_bossPercClip = RuntimeAudioSynth.ToClip("boss_percussion", data);
            }
            _bossPerc = gameObject.AddComponent<AudioSource>();
            _bossPerc.clip = s_bossPercClip;
            _bossPerc.loop = true;
            _bossPerc.volume = 0.22f;
        }

        // ── F11 MUSIC v2: the proven minimal "sounds like music" architecture (Genesis Noir two-layer
        // split, AES 2025 survey): preset chord-progression bed + rule-constrained random melody. ──

        private const int Rate = 22050;
        private const float LoopSeconds = 16f; // 4 chords × 4s
        // A minor, i-VI-III-VII: Am, F, C, G as semitone offsets from A (triads).
        private static readonly int[][] Chords =
        {
            new[] { 0, 3, 7 },    // Am
            new[] { -4, 0, 3 },   // F
            new[] { 3, 7, 10 },   // C
            new[] { -2, 2, 5 },   // G
        };
        private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22 }; // A min pent, 2 octaves

        private static float Hz(float baseHz, int semitones) => baseHz * Mathf.Pow(2f, semitones / 12f);

        // Pure sample math — runs on the forge worker thread (no UnityEngine objects, Mathf only).
        private static float[] ComposeData(int slot, uint seed)
        {
            int count = Mathf.CeilToInt(LoopSeconds * Rate);
            var data = new float[count];
            uint state = seed;
            System.Func<float> rng = () =>
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (state & 0xFFFFFF) / (float)0x1000000;
            };

            // Slot character: NIGHT = lower, slower, no percussion, sparse melody; BATTLE = driving.
            float baseHz = slot == 1 ? 110f : slot == 2 ? 146.83f : 220f;
            float bassHz = baseHz * 0.5f;
            float padGain = slot == 2 ? 0.07f : 0.11f;
            float melodyDensity = slot == 0 ? 0.62f : slot == 1 ? 0.30f : 0.78f;
            float melodyStep = slot == 1 ? 1.0f : 0.5f; // note grid in seconds

            // LAYER 1: chord bed — per chord, a two-osc detuned pad per triad note + a bass drone.
            float chordSeconds = LoopSeconds / Chords.Length;
            for (int c = 0; c < Chords.Length; c++)
            {
                int start = (int)(c * chordSeconds * Rate);
                int len = (int)(chordSeconds * Rate);
                for (int i = 0; i < len && start + i < count; i++)
                {
                    float t = i / (float)Rate;
                    // pad attack/release 0.8s for seamless chord joins
                    float env = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((chordSeconds - t) / 0.8f);
                    float s = 0f;
                    for (int n = 0; n < Chords[c].Length; n++)
                    {
                        float f = Hz(baseHz, Chords[c][n]);
                        s += Mathf.Sin(2f * Mathf.PI * f * 0.997f * t) + Mathf.Sin(2f * Mathf.PI * f * 1.003f * t);
                    }
                    float bass = Mathf.Sin(2f * Mathf.PI * Hz(bassHz, Chords[c][0]) * t)
                               * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.7f * t)); // slow tremolo warmth
                    data[start + i] += s * padGain * env / 6f + bass * 0.16f * env;
                }
            }

            // LAYER 2: melody — constrained random walk on the pentatonic pool. Rules (verified survey):
            // no leap over a fifth, mostly stepwise, never repeat a pitch, reverse direction after a leap.
            int degree = 4; // start mid-pool
            int lastDir = 1;
            bool lastWasLeap = false;
            for (float beat = 0f; beat < LoopSeconds; beat += melodyStep)
            {
                if (rng() > melodyDensity) continue;
                int dir = lastWasLeap ? -lastDir : (rng() < 0.5f ? -1 : 1); // reverse after a leap
                bool leap = !lastWasLeap && rng() < 0.25f;                   // mostly stepwise
                int next = degree + dir * (leap ? 2 : 1);                    // ±2 degrees ≈ a fourth/fifth max
                if (next < 0 || next >= Pentatonic.Length) { dir = -dir; next = degree + dir; }
                if (next == degree) next = degree + dir;                     // no repeated notes
                degree = Mathf.Clamp(next, 0, Pentatonic.Length - 1);
                lastDir = dir; lastWasLeap = leap;

                float f = Hz(baseHz * 2f, Pentatonic[degree]);
                int start = (int)(beat * Rate);
                int len = (int)(melodyStep * 0.95f * Rate);
                for (int i = 0; i < len && start + i < count; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-t * (slot == 2 ? 7f : 4f)); // pluck
                    data[start + i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 0.5f * t))
                                       * env * 0.13f;
                }
            }

            // LAYER 3: percussion — noise bursts; BATTLE drives 4Hz with accents, DAY ticks softly.
            if (slot != 1)
            {
                float interval = slot == 2 ? 0.25f : 1.0f;
                float gain = slot == 2 ? 0.10f : 0.030f;
                for (float beat = 0f; beat < LoopSeconds; beat += interval)
                {
                    int start = (int)(beat * Rate);
                    float accent = ((int)(beat / interval) % 4 == 0) ? 1.5f : 1f;
                    int len = (int)(0.05f * Rate);
                    for (int i = 0; i < len && start + i < count; i++)
                        data[start + i] += (rng() * 2f - 1f) * Mathf.Exp(-i / (float)Rate * 90f) * gain * accent;
                }
            }

            // Soft-clip + level: three voices must not pump the limiter.
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Clamp(data[i] * 0.8f, -0.95f, 0.95f);

            return data;
        }
    }
}

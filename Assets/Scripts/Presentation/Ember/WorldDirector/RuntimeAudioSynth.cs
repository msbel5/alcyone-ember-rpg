using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F11 SES KALİTESİ — the verified procedural-audio recipes (memory: procedural-audio-recipes.md),
    /// translated from the literature into plain C# sample loops:
    ///  - FOOTSTEPS (Turchet/Serafin): exciter–resonator pair. Exciter = GRF amplitude envelope with TWO
    ///    spline peaks (heel + toe, ~100ms walk). Aggregate ground (dirt) = PhISM (Cook): Poisson-triggered
    ///    micro-impacts through bandpass resonators. Solid ground (stone) = 6-mode modal bank
    ///    h(t)=Σ aᵢ·e^(−bᵢt)·sin(2πfᵢt) — material identity lives in the DECAY TIMES, not the frequencies.
    ///  - Per-step variation (Crackdown 2): pre-rendered VARIANTS (a cascade of random log-spaced dips per
    ///    variant) rotated at runtime + the pool's pitch jitter.
    ///  - FILTER: Cytomic trapezoidal SVF (2 states, stable under sweeps); LP/BP/HP taps implemented
    ///    explicitly (the universal output-mixing claim was refuted in verification).
    ///  - DOOR CREAK: simplified Extended Karplus-Strong — delay-line loop with a one-pole damping filter,
    ///    re-excited in stick-slip bursts (the elastoplastic friction feel) instead of one pluck.
    /// All clips render OFFLINE at attach time from fixed seeds (determinism contract; no asset files).
    /// </summary>
    public static class RuntimeAudioSynth
    {
        public const int Rate = 22050;

        // ── Cytomic trapezoidal SVF (SvfLinearTrapOptimised2): two states, swept-stable. ──
        public struct Svf
        {
            private float _g, _k, _a1, _a2, _a3, _ic1, _ic2;
            private float _lp, _bp, _hp;

            public void Set(float cutoffHz, float q)
            {
                _g = Mathf.Tan(Mathf.PI * Mathf.Clamp(cutoffHz, 10f, Rate * 0.45f) / Rate);
                _k = 1f / Mathf.Max(0.1f, q);
                _a1 = 1f / (1f + _g * (_g + _k));
                _a2 = _g * _a1;
                _a3 = _g * _a2;
            }

            public void Tick(float x)
            {
                float v3 = x - _ic2;
                float v1 = _a1 * _ic1 + _a2 * v3;
                float v2 = _ic2 + _a2 * _ic1 + _a3 * v3;
                _ic1 = 2f * v1 - _ic1;
                _ic2 = 2f * v2 - _ic2;
                _lp = v2;
                _bp = v1;
                _hp = x - _k * v1 - v2;
            }

            public float Lp => _lp;
            public float Bp => _bp;
            public float Hp => _hp;
        }

        private static System.Func<float> Rng(uint seed)
        {
            uint state = seed == 0u ? 2463534242u : seed;
            return () =>
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (state & 0xFFFFFF) / (float)0x1000000;
            };
        }

        // GRF exciter: two raised-cosine bumps — heel strike then toe-off (walk ≈ 100ms total).
        private static float GrfEnvelope(float t, float heelAt, float toeAt, float width)
        {
            float e = 0f;
            float dh = (t - heelAt) / width;
            if (dh > -1f && dh < 1f) e += 0.5f * (1f + Mathf.Cos(Mathf.PI * dh));
            float dt = (t - toeAt) / (width * 1.25f);
            if (dt > -1f && dt < 1f) e += 0.40f * (1f + Mathf.Cos(Mathf.PI * dt));
            return e;
        }

        /// <summary>Aggregate ground (dirt/grass): PhISM micro-impact stream under the GRF envelope —
        /// two parallel bandpass resonators give the grain its body, a lowpassed noise bed the soil.</summary>
        public static float[] RenderFootstepDirt(uint seed)
        {
            const float seconds = 0.16f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            // Band centres sit LOW (dirt is a thud, not gravel): first metric pass read ~1330Hz centroid —
            // brighter than any soil — so the grain bands dropped and a final lowpass caps the spectrum.
            var lowBand = new Svf(); lowBand.Set(220f + rng() * 80f, 1.4f);
            var highBand = new Svf(); highBand.Set(480f + rng() * 140f, 1.8f);
            var soil = new Svf(); soil.Set(300f, 0.7f);
            var cap = new Svf(); cap.Set(900f, 0.707f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float env = GrfEnvelope(t, 0.022f, 0.082f, 0.024f);
                // Poisson micro-impacts: collision probability rides the GRF (more weight → more grains).
                float impacts = rng() < env * 0.55f ? (rng() * 2f - 1f) : 0f;
                lowBand.Tick(impacts);
                highBand.Tick(impacts);
                soil.Tick((rng() * 2f - 1f) * env * 0.4f);
                cap.Tick(lowBand.Bp * 1.0f + highBand.Bp * 0.45f + soil.Lp * 0.9f);
                data[i] = cap.Lp * 0.9f;
            }
            ApplyDipCascade(data, seed ^ 0xD1FFu);
            Normalize(data, 0.55f);
            return data;
        }

        /// <summary>Solid ground (stone): 6-mode modal bank rung at heel + toe. Stone = SHORT decays with
        /// the high modes damping fastest; a brief noise burst supplies the contact scrape.</summary>
        public static float[] RenderFootstepStone(uint seed)
        {
            const float seconds = 0.16f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            float[] freqs = { 185f, 318f, 476f, 742f, 1108f, 1627f };
            float[] decay = { 0.090f, 0.072f, 0.058f, 0.044f, 0.031f, 0.022f }; // material = decay times
            float[] amps = { 1.0f, 0.72f, 0.55f, 0.42f, 0.30f, 0.20f };
            float jitter = 0.94f + rng() * 0.12f;
            var scrape = new Svf(); scrape.Set(1400f, 0.9f);
            float heelT = 0.020f, toeT = 0.080f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int m = 0; m < freqs.Length; m++)
                {
                    float f = freqs[m] * jitter;
                    if (t >= heelT)
                    {
                        float dt = t - heelT;
                        s += amps[m] * Mathf.Exp(-dt / decay[m]) * Mathf.Sin(2f * Mathf.PI * f * dt);
                    }
                    if (t >= toeT)
                    {
                        float dt = t - toeT;
                        s += amps[m] * 0.55f * Mathf.Exp(-dt / (decay[m] * 0.8f)) * Mathf.Sin(2f * Mathf.PI * f * 1.04f * dt);
                    }
                }
                scrape.Tick((rng() * 2f - 1f) * GrfEnvelope(t, heelT + 0.004f, toeT + 0.004f, 0.012f));
                data[i] = s * 0.16f + scrape.Bp * 0.30f;
            }
            ApplyDipCascade(data, seed ^ 0x57014u);
            Normalize(data, 0.5f);
            return data;
        }

        /// <summary>Door creak: Karplus-Strong delay loop (one-pole damping in the feedback path),
        /// re-excited in irregular stick-slip bursts while the hinge swings — not a single pluck.</summary>
        public static float[] RenderDoorCreak(uint seed)
        {
            const float seconds = 1.25f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            // PLAYTEST FIX ("kapi gicirtisi gitar sesi gibi"): the old loop rang ONE stable
            // period at 0.987 sustain - which IS the plucked-string algorithm. A hinge creak is
            // friction: the groan pitch WANDERS on every slip, the resonance dies fast, and dry
            // scraping noise rides on top of the resonant tail.
            int period = (int)(Rate / 86f);
            var line = new float[(int)(Rate / 55f) + 2]; // sized for the lowest wander pitch
            float damp = 0f;
            int next = 0;
            float burstGain = 0f;
            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                if (i >= next) // stick-slip: each slip re-seats the hinge at a NEW groan pitch
                {
                    burstGain = 0.7f + rng() * 0.5f;
                    period = (int)(Rate / (62f + rng() * 74f)); // 62-136 Hz wandering fundamental
                    if (idx >= period) idx = 0;
                    next = i + (int)(Rate * (0.05f + rng() * 0.11f + t * 0.07f));
                }
                float noise = rng() * 2f - 1f;
                float excite = noise * burstGain;
                burstGain *= 0.9985f;
                float read = line[idx];
                damp = 0.55f * read + 0.45f * damp;
                float fed = damp * 0.955f + excite * 0.4f; // short ring: a resonance, not a string
                line[idx] = fed;
                idx = (idx + 1) % period;
                float fade = t < 1.0f ? 1f : 1f - (t - 1.0f) / 0.25f;
                data[i] = (fed * 0.8f + noise * burstGain * 0.25f) * fade; // dry friction on top
            }
            Normalize(data, 0.42f);
            return data;
        }

        /// <summary>Melee swing (buyer feel): a short AIR CUT — noise through a lowpass whose
        /// cutoff sweeps up then back (400→2400→900 Hz) under a hann hump. No tone, no ring.</summary>
        public static float[] RenderSwingWhoosh(uint seed)
        {
            const float seconds = 0.26f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            float lp = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float env = Mathf.Sin(t * Mathf.PI);
                float sweep = t < 0.55f
                    ? Mathf.Lerp(400f, 2400f, t / 0.55f)
                    : Mathf.Lerp(2400f, 900f, (t - 0.55f) / 0.45f);
                float alpha = Mathf.Clamp01(sweep / (sweep + Rate / (2f * Mathf.PI)));
                lp += alpha * ((rng() * 2f - 1f) - lp);
                data[i] = lp * env * env;
            }
            Normalize(data, 0.30f);
            return data;
        }

        /// <summary>Combat hit: a dull modal thud (low modes, fast decays) + lowpassed noise burst —
        /// the F10 flash's audible twin.</summary>
        public static float[] RenderHitImpact(uint seed)
        {
            return RenderHitImpact(seed, pitch: 1f, decayScale: 1f, thumpAmount: 1f);
        }

        // F29 BESTIARY: the same modal bank, re-voiced per struck MATERIAL — pitch scales the mode
        // frequencies (bone/chitin ring high, hide/wail sit low), decayScale stretches or chokes the
        // ring (a wisp lingers, chitin snaps shut), thumpAmount weighs the noise-thud body.
        public static float[] RenderHitImpact(uint seed, float pitch, float decayScale, float thumpAmount)
        {
            const float seconds = 0.22f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            float[] freqs = { 96f, 178f, 322f, 540f, 870f };
            float[] decay = { 0.085f, 0.062f, 0.040f, 0.026f, 0.016f };
            float[] amps = { 1.0f, 0.8f, 0.55f, 0.35f, 0.2f };
            var thump = new Svf(); thump.Set(520f, 0.8f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int m = 0; m < freqs.Length; m++)
                    s += amps[m] * Mathf.Exp(-t / (decay[m] * decayScale)) * Mathf.Sin(2f * Mathf.PI * freqs[m] * pitch * t);
                thump.Tick((rng() * 2f - 1f) * Mathf.Exp(-t * 55f));
                data[i] = s * 0.22f + thump.Lp * 0.9f * thumpAmount;
            }
            Normalize(data, 0.6f);
            return data;
        }

        // ── F30 SES v3: biome ambience layers + two new grounds + the boss percussion layer ──

        /// <summary>Day biome layer: sparse bright chirp pairs over silence — a 6s loop. Chirps are
        /// short downward FM sweeps (2.4-3.6kHz) deterministically scattered through the window.</summary>
        public static float[] RenderBirdChirps(uint seed)
        {
            const float seconds = 6f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            for (int c = 0; c < 9; c++)
            {
                float start = rng() * (seconds - 0.2f);
                float f0 = 2400f + rng() * 1200f;
                float dur = 0.06f + rng() * 0.07f;
                int s0 = (int)(start * Rate), n = (int)(dur * Rate);
                for (int i = 0; i < n && s0 + i < count; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Sin(Mathf.PI * i / (float)n); // smooth in-out
                    float f = f0 * (1f - 0.25f * (i / (float)n));   // slight down-chirp
                    data[s0 + i] += Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
                }
            }
            Normalize(data, 0.35f);
            return data;
        }

        /// <summary>Night biome layer: cricket pulse trains — 4.4kHz ticks gated at ~16Hz inside
        /// chirp windows with silence between. A 6s loop.</summary>
        public static float[] RenderCrickets(uint seed)
        {
            const float seconds = 6f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            for (int w = 0; w < 6; w++)
            {
                float start = rng() * (seconds - 0.8f);
                float dur = 0.5f + rng() * 0.3f;
                int s0 = (int)(start * Rate), n = (int)(dur * Rate);
                for (int i = 0; i < n && s0 + i < count; i++)
                {
                    float t = i / (float)Rate;
                    float gate = Mathf.Sin(2f * Mathf.PI * 16f * t) > 0.6f ? 1f : 0f; // 16Hz pulse train
                    data[s0 + i] += Mathf.Sin(2f * Mathf.PI * 4400f * t) * gate * 0.30f
                        * Mathf.Sin(Mathf.PI * i / (float)n);
                }
            }
            Normalize(data, 0.3f);
            return data;
        }

        /// <summary>Snow ground: a soft compressing crunch — lowpassed noise, squashy two-stage
        /// decay, no modal ring (powder, not stone).</summary>
        public static float[] RenderFootstepSnow(uint seed)
        {
            const float seconds = 0.18f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            var lp = new Svf(); lp.Set(700f + rng() * 200f, 0.9f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 26f) + 0.45f * Mathf.Exp(-(t - 0.07f) * (t - 0.07f) * 2200f);
                lp.Tick((rng() * 2f - 1f) * env);
                data[i] = lp.Lp * 1.2f;
            }
            Normalize(data, 0.5f);
            return data;
        }

        /// <summary>Gravel ground: a rattle of 4-6 micro stone ticks jittered through the step.</summary>
        public static float[] RenderFootstepGravel(uint seed)
        {
            const float seconds = 0.18f;
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            int ticks = 4 + (int)(rng() * 3f);
            for (int k = 0; k < ticks; k++)
            {
                float at = rng() * 0.12f;
                float f = 900f + rng() * 1400f;
                int s0 = (int)(at * Rate);
                for (int i = 0; i < 260 && s0 + i < count; i++)
                {
                    float t = i / (float)Rate;
                    data[s0 + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 240f) * (0.5f + rng() * 0.1f);
                }
            }
            Normalize(data, 0.55f);
            return data;
        }

        /// <summary>F30 boss layer: a 2-bar driving percussion loop (~138bpm) — kick thump (sine
        /// drop 95→55Hz) on the quarters, noise snare on the off-beats. Loops over the BATTLE bed.</summary>
        public static float[] RenderBossPercussion(uint seed)
        {
            const float bpm = 138f;
            float beat = 60f / bpm;
            float seconds = beat * 8f; // 2 bars of 4
            int count = (int)(seconds * Rate);
            var data = new float[count];
            var rng = Rng(seed);
            for (int b = 0; b < 8; b++)
            {
                int s0 = (int)(b * beat * Rate);
                for (int i = 0; i < (int)(0.12f * Rate) && s0 + i < count; i++)
                {
                    float t = i / (float)Rate;
                    float f = Mathf.Lerp(95f, 55f, t / 0.12f);
                    data[s0 + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 30f) * 0.9f;
                }
                if ((b & 1) == 1)
                {
                    int sn = s0 + (int)(beat * 0.5f * Rate);
                    for (int i = 0; i < (int)(0.07f * Rate) && sn + i < count; i++)
                    {
                        float t = i / (float)Rate;
                        data[sn + i] += (rng() * 2f - 1f) * Mathf.Exp(-t * 60f) * 0.5f;
                    }
                }
            }
            Normalize(data, 0.6f);
            return data;
        }

        // Crackdown 2 per-variant colour: ~10 dip (cut) filters at random log-spaced centres. Rendered
        // into each VARIANT offline; runtime rotation + pitch jitter completes the per-step variation.
        private static void ApplyDipCascade(float[] data, uint seed)
        {
            var rng = Rng(seed);
            for (int d = 0; d < 10; d++)
            {
                float f = 120f * Mathf.Pow(2f, rng() * 5.5f); // log-spaced 120Hz..~5.4kHz
                float depth = 0.35f + rng() * 0.45f;
                var band = new Svf();
                band.Set(f, 1.2f + rng() * 2.4f);
                for (int i = 0; i < data.Length; i++)
                {
                    band.Tick(data[i]);
                    data[i] -= band.Bp * depth; // subtract the band = a dip at f
                }
            }
        }

        private static void Normalize(float[] data, float peakTarget)
        {
            float peak = 1e-6f;
            for (int i = 0; i < data.Length; i++)
                if (Mathf.Abs(data[i]) > peak) peak = Mathf.Abs(data[i]);
            float gain = peakTarget / peak;
            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }

        public static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── F11-DoD proof metrics: PNGs are silent, so the honest evidence is numeric — every forged
        // clip logs its RMS + spectral centroid (a zero-crossing estimate: cheap, monotonic with
        // brightness; enough to assert "thud, not bell" without an FFT). ──
        public static float Rms(float[] data)
        {
            double sum = 0;
            for (int i = 0; i < data.Length; i++) sum += data[i] * (double)data[i];
            return Mathf.Sqrt((float)(sum / Mathf.Max(1, data.Length)));
        }

        public static float CentroidHz(float[] data)
        {
            int crossings = 0;
            for (int i = 1; i < data.Length; i++)
                if ((data[i - 1] < 0f) != (data[i] < 0f)) crossings++;
            return crossings * Rate / (2f * Mathf.Max(1, data.Length));
        }
    }
}

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
        public static AudioClip Wind, Sting, Click, DoorCreak, Hit, Rain;
        // F11 footsteps v2: 4 pre-rendered VARIANTS per surface (each coloured by its own random
        // dip-cascade — the Crackdown 2 recipe); the director rotates them + the pool adds pitch jitter.
        public static AudioClip[] FootstepDirt, FootstepStone;

        public static void EnsureForged()
        {
            if (FootstepDirt != null) return;
            FootstepDirt = new AudioClip[4];
            FootstepStone = new AudioClip[4];
            for (uint k = 0; k < 4; k++)
            {
                FootstepDirt[k] = ForgeMetered("footstep_dirt_" + k, RuntimeAudioSynth.RenderFootstepDirt(0xD117u + k * 7919u));
                FootstepStone[k] = ForgeMetered("footstep_stone_" + k, RuntimeAudioSynth.RenderFootstepStone(0x5709u + k * 7919u));
            }
            DoorCreak = ForgeMetered("door_creak", RuntimeAudioSynth.RenderDoorCreak(0xC4EA7u));
            Hit = ForgeMetered("hit_impact", RuntimeAudioSynth.RenderHitImpact(0x1417u));
            Wind = Synth("forge_wind", 3.0f, (t, rng) => (rng() * 2f - 1f) * 0.16f, smooth: 24);
            Rain = ForgeMetered("rain_loop", RenderRain(0xA17EBu)); // F25: hiss bed + droplet ticks
            Sting = Synth("forge_sting", 0.45f, (t, rng) =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(660f, 110f, t) * t) * Mathf.Pow(1f - t, 2f) * 0.6f, smooth: 0);
            Click = Synth("forge_click", 0.05f, (t, rng) =>
                Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Pow(1f - t, 4f) * 0.5f, smooth: 0);
            Debug.Log("[Audio] forged v2 foley set (8 footsteps, creak, hit) + wind/sting/click.");
        }

        // F25: PhISM-flavoured rain — a lowpassed noise bed (the body of the shower) under sparse
        // bright droplet ticks (short decaying sine pops). A 4s loop; the metric log is the DoD proof.
        private static float[] RenderRain(uint seed)
        {
            int count = (int)(RuntimeAudioSynth.Rate * 4f);
            var data = new float[count];
            uint s = seed == 0 ? 1u : seed;
            float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / (float)0x1000000; }

            for (int i = 0; i < count; i++)
                data[i] = (Next() * 2f - 1f) * 0.22f;
            for (int pass = 0; pass < 6; pass++) // soften the noise into a shower bed
                for (int i = 1; i < count; i++)
                    data[i] = (data[i] + data[i - 1]) * 0.5f;

            for (int drop = 0; drop < 260; drop++)
            {
                int at = (int)(Next() * (count - 300));
                float freq = 2000f + Next() * 1400f;
                float amp = (0.10f + Next() * 0.16f) * 0.45f;
                for (int j = 0; j < 240; j++)
                {
                    float tt = j / 240f;
                    data[at + j] += Mathf.Sin(2f * Mathf.PI * freq * (j / (float)RuntimeAudioSynth.Rate))
                                    * amp * Mathf.Pow(1f - tt, 3f);
                }
            }
            return data;
        }

        // F11-DoD: PNG proofs are silent — the honest audible evidence is numeric. Every forged clip
        // logs duration + RMS + a zero-crossing centroid so "thud not bell" is assertable from the log.
        private static AudioClip ForgeMetered(string name, float[] data)
        {
            Debug.Log($"[AudioForge] {name}: len={data.Length / (float)RuntimeAudioSynth.Rate:0.00}s " +
                      $"rms={RuntimeAudioSynth.Rms(data):0.000} centroid={RuntimeAudioSynth.CentroidHz(data):0}Hz");
            return RuntimeAudioSynth.ToClip(name, data);
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

    /// <summary>Player-rig audio behaviour: motion-driven footsteps + looping ambience + encounter sting.
    /// SFX v2 (DOOM s_sound recipe): a fixed CHANNEL POOL with priority eviction, same-origin dedup and
    /// distance attenuation replaces the single one-shot source.</summary>
    public sealed class RuntimeAudioDirector : MonoBehaviour
    {
        private CharacterController _controller;
        private AudioSource _ambience;
        private Vector3 _lastPos;
        private float _stepClock;
        private int _stepVariant;
        private int _hitStampSeen;

        // DOOM channel pool: 8 channels, each remembers its origin id + priority.
        private readonly AudioSource[] _channels = new AudioSource[8];
        private readonly int[] _channelOrigin = new int[8];
        private readonly int[] _channelPriority = new int[8];
        private AudioSource _oneShot; // legacy alias = channel 0 (sting/click callers)

        /// <summary>Play a clip on the pool: same-origin replaces, else free slot, else evict lower priority.</summary>
        public void PlayAt(int originId, AudioClip clip, float volume, int priority, Vector3 worldPos)
        {
            // distance attenuation (DOOM close/clip distances scaled to our town: full <8m, silent >64m)
            float d = Vector3.Distance(transform.position, worldPos);
            if (d > 64f) return;
            float atten = d < 8f ? 1f : 1f - ((d - 8f) / 56f);

            int slot = -1;
            for (int i = 0; i < _channels.Length; i++)
                if (_channelOrigin[i] == originId && _channels[i].isPlaying) { slot = i; break; } // origin dedup
            if (slot < 0)
                for (int i = 0; i < _channels.Length; i++)
                    if (!_channels[i].isPlaying) { slot = i; break; } // free channel
            if (slot < 0)
            {
                int lowest = 0;
                for (int i = 1; i < _channels.Length; i++)
                    if (_channelPriority[i] < _channelPriority[lowest]) lowest = i;
                if (_channelPriority[lowest] >= priority) return; // everything louder matters more
                slot = lowest; // priority eviction
            }

            _channels[slot].Stop();
            _channelOrigin[slot] = originId;
            _channelPriority[slot] = priority;
            _channels[slot].pitch = 0.94f + ((originId * 2654435761u) % 13u) / 100f; // DOOM pitch variation
            _channels[slot].PlayOneShot(clip, volume * atten);
        }

        public static void Attach(GameObject playerRig)
        {
            if (playerRig == null || playerRig.GetComponent<RuntimeAudioDirector>() != null) return;
            RuntimeAudioForge.EnsureForged();
            playerRig.AddComponent<RuntimeAudioDirector>();
        }

        // Built floors are named slabs ("Floor", "CorrFloor", "ChamberFloor"); raw terrain is a Terrain
        // collider. 1.5m is enough to see the ground from the rig's capsule centre.
        private bool IsOnBuiltFloor()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hitInfo, 1.8f))
                return hitInfo.collider != null && hitInfo.collider.name.Contains("Floor");
            return false;
        }

        /// <summary>F11 door v2 ("kapı sesi tiz zil gibiydi" root fix): doors creak — the EKS stick-slip
        /// clip on the channel pool with the door as origin (dedup: a re-trigger replaces, never stacks).
        /// The old wiring literally played the 1320Hz UI click.</summary>
        public static void PlayDoorCreak(Vector3 doorWorldPos)
        {
            RuntimeAudioForge.EnsureForged();
            var rig = GameObject.Find("PlayerRig");
            var self = rig != null ? rig.GetComponent<RuntimeAudioDirector>() : null;
            if (self == null) return;
            int origin = 100 + (Mathf.RoundToInt(doorWorldPos.x) * 73856093 ^ Mathf.RoundToInt(doorWorldPos.z) * 19349663) % 1000;
            self.PlayAt(origin, RuntimeAudioForge.DoorCreak, 0.8f, priority: 2, doorWorldPos);
        }

        private AudioSource _rain;

        /// <summary>F25: the rain hiss loop — on while it rains, off otherwise (lazy looping source).</summary>
        public static void SetRainLoop(bool on)
        {
            RuntimeAudioForge.EnsureForged();
            var rig = GameObject.Find("PlayerRig");
            var self = rig != null ? rig.GetComponent<RuntimeAudioDirector>() : null;
            if (self == null) return;
            if (self._rain == null)
            {
                self._rain = self.gameObject.AddComponent<AudioSource>();
                self._rain.loop = true;
                self._rain.playOnAwake = false;
                self._rain.clip = RuntimeAudioForge.Rain;
                self._rain.volume = 0.55f;
            }
            if (on && !self._rain.isPlaying) { self._rain.Play(); Debug.Log("[Weather] rain loop ON."); }
            else if (!on && self._rain.isPlaying) { self._rain.Stop(); Debug.Log("[Weather] rain loop OFF."); }
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
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = gameObject.AddComponent<AudioSource>();
                _channels[i].playOnAwake = false;
                _channelPriority[i] = int.MinValue;
            }
            _oneShot = _channels[0];
            Debug.Log("[Audio] SFX channel pool ready (8 channels, priority eviction + origin dedup + distance attenuation).");
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
                    // F11 surface pick: a downward ray names the ground — built floors (interiors,
                    // dungeon corridor/chamber) ring the MODAL stone bank, open terrain the PhISM dirt.
                    var pool = IsOnBuiltFloor() ? RuntimeAudioForge.FootstepStone : RuntimeAudioForge.FootstepDirt;
                    _stepVariant = (_stepVariant + 1) % pool.Length;
                    PlayAt(originId: 1, pool[_stepVariant], 0.8f, priority: 1, transform.position);
                }
            }

            if (EmberCrpg.Presentation.Ember.Adapters.WorldEncounterStingFeed.Consume())
                PlayAt(originId: 2, RuntimeAudioForge.Sting, 0.9f, priority: 5, transform.position);

            // F10/F11 hit feel: the modal thud is the red flash's audible twin (stamp feed, see
            // WorldCombatFeedbackFeed — strikes resolve while the combat modal pauses timeScale).
            if (WorldCombatFeedbackFeed.HitStamp != _hitStampSeen)
            {
                _hitStampSeen = WorldCombatFeedbackFeed.HitStamp;
                PlayAt(originId: 5, RuntimeAudioForge.Hit, 0.85f, priority: 4, transform.position);
            }
        }
    }
}

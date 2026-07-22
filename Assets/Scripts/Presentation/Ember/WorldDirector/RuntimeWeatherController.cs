using EmberCrpg.Domain.Overland;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F25: other systems read the CURRENT weather here (music softens in rain, future
    /// sim hooks). Static channel, same pattern as RuntimeFieldMirror.</summary>
    public static class RuntimeWeatherMirror
    {
        public static string Kind = "clear";
        public static bool Raining => Kind == "rain";
        /// <summary>0-1 atmosphere haze the SKY consumes — URP fog shader variants STRIP from player
        /// builds, so the readable fog lives in the sky colour + sun dimming, not RenderSettings.</summary>
        public static float FogFactor;
    }

    /// <summary>
    /// F25 WEATHER: deterministic per-DAY weather from biome + season + world day — rain (particles +
    /// PhISM-style rain hiss loop), fog (exp fog matched to the sky), snow (slow white flakes + cold
    /// fog). Build-safe: particles use a generated white sprite on Sprites/Default (the URP default
    /// particle material strips to magenta in players). The proof driver can FORCE a kind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeWeatherController : MonoBehaviour
    {
        private static string s_proofForce; // proof-only override; null = deterministic pick

        public static void ProofForce(string kind) => s_proofForce = kind;

        private const int PoolSize = 130;

        private BiomeKind _biome;
        private string _applied = "";
        private int _appliedDay = -1;
        private UnityEngine.Camera _camera;
        // PROOF-CAUGHT: ParticleSystem rendered NOTHING in player builds (any mode, any material).
        // The star dome's unlit cubes DID render — precipitation is a manual cube pool now.
        private Transform _pool;
        private Transform[] _drops;
        private float _fallSpeed;
        private bool _precipOn;

        public void Bind(BiomeKind biome) => _biome = biome;

        private void Update()
        {
            int day = RuntimeFieldMirror.WorldDay;
            string kind = s_proofForce ?? Pick(day);
            if (kind != _applied || day != _appliedDay)
            {
                _applied = kind;
                _appliedDay = day;
                Apply(kind, day);
            }
            FollowCamera();
        }

        // Deterministic pick: hash(day, biome) into per-biome, per-season weights.
        private string Pick(int day)
        {
            int season = ((Mathf.Max(1, day) - 1) / 90) % 4; // 0 spring · 1 summer · 2 autumn · 3 winter
            uint h = (uint)(day * 2654435761) ^ ((uint)_biome * 97u);
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            int roll = (int)(h % 100u);

            switch (_biome)
            {
                case BiomeKind.Tundra:
                case BiomeKind.Mountain:
                    if (season == 3 || roll < 35) return "snow";
                    if (roll < 55) return "fog";
                    return "clear";
                case BiomeKind.Swamp:
                    if (roll < 40) return "fog";
                    if (roll < 70) return "rain";
                    return "clear";
                case BiomeKind.Desert:
                case BiomeKind.Ash:
                    return roll < 12 ? "fog" : "clear";
                default: // temperate family
                    if (season == 3 && roll < 30) return "snow";
                    if ((season == 0 || season == 2) && roll < 35) return "rain";
                    if ((season == 0 || season == 2) && roll < 50) return "fog";
                    if (season == 1 && roll < 15) return "rain";
                    return "clear";
            }
        }

        private void Apply(string kind, int day)
        {
            RuntimeWeatherMirror.Kind = kind;
            int season = ((Mathf.Max(1, day) - 1) / 90) % 4;
            Debug.Log($"[Weather] day={day} season={season} biome={_biome} → {kind}.");

            EnsurePool();

            // URP fog shader variants strip from player builds — RenderSettings stays set for the
            // editor, but the READABLE atmosphere is the FogFactor the sky consumes (colour + sun).
            switch (kind)
            {
                case "rain":
                    // SATILABILIRLIK: 2.4m opaque cubes read as glowing BEACONS, not rain. Real rain
                    // streaks are short, thin, and translucent (Sprites/Default is alpha-blended).
                    ConfigurePool(new Vector3(0.025f, 0.55f, 0.025f), new Color(0.62f, 0.70f, 0.85f, 0.45f), 26f);
                    RuntimeWeatherMirror.FogFactor = 0.62f; // a rain sky IS dark — the haze must read
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Exponential;
                    RenderSettings.fogDensity = 0.010f;
                    RenderSettings.fogColor = new Color(0.42f, 0.46f, 0.52f);
                    RuntimeAudioDirector.SetRainLoop(true);
                    break;
                case "snow":
                    ConfigurePool(new Vector3(0.14f, 0.14f, 0.14f), new Color(0.97f, 0.98f, 1f, 0.85f), 1.6f);
                    RuntimeWeatherMirror.FogFactor = 0.55f;
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Exponential;
                    RenderSettings.fogDensity = 0.012f;
                    RenderSettings.fogColor = new Color(0.80f, 0.82f, 0.87f);
                    RuntimeAudioDirector.SetRainLoop(false);
                    break;
                case "fog":
                    SetPoolActive(false);
                    RuntimeWeatherMirror.FogFactor = 0.85f;
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Exponential;
                    RenderSettings.fogDensity = 0.024f;
                    RenderSettings.fogColor = new Color(0.58f, 0.60f, 0.64f);
                    RuntimeAudioDirector.SetRainLoop(false);
                    break;
                default: // clear
                    SetPoolActive(false);
                    RuntimeWeatherMirror.FogFactor = 0f;
                    RenderSettings.fog = false;
                    RuntimeAudioDirector.SetRainLoop(false);
                    break;
            }
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            _pool = new GameObject("WeatherPrecipPool").transform;
            var shader = Shader.Find("Sprites/Default"); // unlit, billboard-guaranteed in builds
            var material = shader != null
                ? new Material(shader) { color = Color.white }
                : RuntimeMaterialPalette.Solid(Color.white);
            _drops = new Transform[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drop.name = $"Precip{i:000}";
                Destroy(drop.GetComponent<Collider>());
                drop.GetComponent<Renderer>().sharedMaterial = material;
                drop.transform.SetParent(_pool, worldPositionStays: false);
                drop.SetActive(false);
                _drops[i] = drop.transform;
            }
        }

        private void ConfigurePool(Vector3 dropScale, Color color, float fallSpeed)
        {
            EnsurePool();
            _fallSpeed = fallSpeed;
            _precipOn = true;
            for (int i = 0; i < PoolSize; i++)
            {
                _drops[i].localScale = dropScale;
                _drops[i].GetComponent<Renderer>().sharedMaterial.color = color;
                _drops[i].gameObject.SetActive(true);
                Rehome(i, scatterHeight: true);
            }
        }

        private void SetPoolActive(bool on)
        {
            _precipOn = on;
            if (_drops == null) return;
            for (int i = 0; i < PoolSize; i++)
                _drops[i].gameObject.SetActive(on);
        }

        // Deterministic per-index scatter inside a 30×30m column around the camera.
        private void Rehome(int i, bool scatterHeight)
        {
            if (_camera == null) return;
            uint h = (uint)(i * 2654435761) ^ (uint)(_appliedDay * 97);
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            float x = ((h % 3000u) / 100f) - 15f;
            float z = (((h >> 8) % 3000u) / 100f) - 15f;
            float y = scatterHeight ? ((h >> 16) % 200u) / 10f : 12f + ((h >> 16) % 30u) / 10f;
            var cam = _camera.transform.position;
            _drops[i].position = new Vector3(cam.x + x, cam.y + y - 6f, cam.z + z);
        }

        private void FollowCamera()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
                if (_camera == null && UnityEngine.Camera.allCamerasCount > 0)
                    _camera = UnityEngine.Camera.allCameras[0];
                return;
            }
            if (!_precipOn || _drops == null) return;
            float dy = _fallSpeed * Time.deltaTime;
            var cam = _camera.transform.position;
            for (int i = 0; i < PoolSize; i++)
            {
                var p = _drops[i].position;
                p.y -= dy;
                // Recycle below the eye line or when the camera outran the column.
                if (p.y < cam.y - 8f || Mathf.Abs(p.x - cam.x) > 20f || Mathf.Abs(p.z - cam.z) > 20f)
                    Rehome(i, scatterHeight: false);
                else
                    _drops[i].position = p;
            }
        }
    }
}

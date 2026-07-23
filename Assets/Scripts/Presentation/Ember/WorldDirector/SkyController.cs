using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F24 SKY v2 — procedural day cycle for the generated world, driven by the SIM's minutes-of-day.
    ///
    /// TIME SOURCE (the clock-jump root fix): v1 re-derived the hour from TickIndex × MinutesPerTick,
    /// which drifts from world.Time after clock jumps (respawn +8h, travel days) — midnight rendered
    /// bright. v2 reads RuntimeFieldMirror.MinutesOfDay, published per tick from world.Time TRUTH.
    ///
    /// RENDERING (build-safe, no skybox shaders — they strip from player builds): the sky is the
    /// camera's clear colour cycling night-navy → dawn-rose → day-blue → dusk-amber; the sun light
    /// rotates and dims with the same curve. NIGHT brings a STAR DOME (tiny unlit cubes on a golden-
    /// angle hemisphere, deterministic) and a MOON (generated circle sprite) that fade in when
    /// daylight drops — both follow the camera so they read as celestial, not parented props.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyController : MonoBehaviour
    {
        private static readonly Color DaySky = new Color(0.40f, 0.62f, 0.86f);
        private static readonly Color NightSky = new Color(0.02f, 0.04f, 0.10f);
        private static readonly Color DuskSky = new Color(0.78f, 0.42f, 0.26f);
        private static readonly Color DawnSky = new Color(0.82f, 0.52f, 0.42f);

        private const int StarCount = 140;
        private const float DomeRadius = 420f;

        private Light _sun;
        private Color _baseAmbient = Color.gray;
        private UnityEngine.Camera _camera;
        private Transform _dome;
        private Renderer[] _starRenderers;
        private Transform _moon;
        private SpriteRenderer _moonRenderer;

        public void Bind(Light sun)
        {
            _sun = sun;
            _baseAmbient = RenderSettings.ambientLight;
            ResolveCamera();
        }

        private void ResolveCamera()
        {
            _camera = UnityEngine.Camera.main;
            if (_camera == null && UnityEngine.Camera.allCamerasCount > 0)
                _camera = UnityEngine.Camera.allCameras[0]; // rig cam is not tagged MainCamera (compass lesson)
            if (_camera != null)
                _camera.clearFlags = CameraClearFlags.SolidColor; // we own the clear colour as the sky
        }

        private void Update()
        {
            float dayFraction = DayFraction();

            // Daylight curve: 0 at midnight, 1 at noon. Twilight peaks near 06:00/18:00; the signed
            // morning/evening split lets dawn blush rose while dusk burns amber.
            float daylight = Mathf.Clamp01((Mathf.Sin((dayFraction * Mathf.PI * 2f) - (Mathf.PI * 0.5f)) * 0.5f) + 0.5f);
            float twilight = 1f - Mathf.Abs(daylight - 0.5f) * 2f;
            bool morning = dayFraction < 0.5f;

            // F25: weather haze — URP fog variants strip from builds, so the READABLE atmosphere is
            // here: the sky greys toward the haze and the sun/ambient dim with it.
            float haze = Mathf.Clamp01(RuntimeWeatherMirror.FogFactor);

            if (_sun != null)
            {
                float sunPitch = (dayFraction * 360f) - 90f;
                _sun.transform.rotation = Quaternion.Euler(sunPitch, 150f, 0f);
                _sun.intensity = Mathf.Lerp(0.04f, 1.15f, daylight) * (1f - 0.55f * haze);
            }

            RenderSettings.ambientLight = _baseAmbient * Mathf.Lerp(0.20f, 1f, daylight) * (1f - 0.22f * haze);

            if (_camera == null) ResolveCamera();
            if (_camera != null)
            {
                Color sky = Color.Lerp(NightSky, DaySky, daylight);
                sky = Color.Lerp(sky, morning ? DawnSky : DuskSky, twilight * (morning ? 0.5f : 0.62f));
                sky = Color.Lerp(sky, new Color(0.45f, 0.48f, 0.54f) * Mathf.Max(0.25f, daylight), haze); // R2: haze no longer bleaches the sky
                _camera.backgroundColor = sky;

                EnsureCelestials();
                float night = Mathf.Clamp01((0.22f - daylight) / 0.22f); // 1 deep night → 0 toward the twilight edges
                UpdateCelestials(dayFraction, night);
            }
        }

        // ----- celestials -------------------------------------------------------------------------

        private void EnsureCelestials()
        {
            if (_dome != null || _camera == null) return;

            var domeGo = new GameObject("SkyDome");
            _dome = domeGo.transform;

            // Star material: Sprites/Default is UNLIT and guaranteed present (every NPC billboard uses
            // it); the URP Lit fallback would render dim at night but never magenta.
            var starShader = Shader.Find("Sprites/Default");
            var starMaterial = starShader != null
                ? new Material(starShader) { color = new Color(0.92f, 0.94f, 1f) }
                : RuntimeMaterialPalette.Solid(new Color(0.92f, 0.94f, 1f));

            _starRenderers = new Renderer[StarCount];
            for (int i = 0; i < StarCount; i++)
            {
                // Deterministic golden-angle spiral over the upper hemisphere — the same sky every night.
                float t = (i + 0.5f) / StarCount;
                float elevation = Mathf.Asin(Mathf.Lerp(0.08f, 0.98f, t));
                float azimuth = i * 2.39996323f; // the golden angle, radians
                var dir = new Vector3(
                    Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                    Mathf.Sin(elevation),
                    Mathf.Cos(elevation) * Mathf.Sin(azimuth));

                var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                star.name = $"Star{i:000}";
                Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(_dome, worldPositionStays: false);
                star.transform.localPosition = dir * DomeRadius;
                float size = 0.7f + ((i * 37) % 10) * 0.13f; // 0.7-1.9m at 420m — pinpricks
                star.transform.localScale = Vector3.one * size;
                _starRenderers[i] = star.GetComponent<Renderer>();
                _starRenderers[i].sharedMaterial = starMaterial;
                _starRenderers[i].enabled = false;
            }

            // The moon: a generated soft-disc sprite that always faces the camera.
            var moonGo = new GameObject("Moon");
            _moon = moonGo.transform;
            _moon.SetParent(_dome, worldPositionStays: false);
            _moonRenderer = moonGo.AddComponent<SpriteRenderer>();
            _moonRenderer.sprite = BuildMoonSprite();
            _moonRenderer.color = new Color(0.93f, 0.93f, 0.88f);
            _moonRenderer.enabled = false;
            _moon.localScale = Vector3.one * 24f; // a 24m disc near 400m reads like a real moon

            Debug.Log($"[Sky] celestials built: {StarCount} stars + moon (dome r={DomeRadius}m, unlit sprite path).");
        }

        private static Sprite BuildMoonSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.82f, 1f, d));
                    // A faint crater shade low-left keeps it from reading as a flat dot.
                    float shade = 1f - 0.18f * Mathf.Clamp01(
                        1f - Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.36f, size * 0.38f)) / (size * 0.28f));
                    tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                }
            }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit: size);
        }

        private void UpdateCelestials(float dayFraction, float night)
        {
            if (_dome == null || _camera == null) return;
            _dome.position = _camera.transform.position; // celestial: follows the eye, parallax-free

            bool starsOn = night > 0.05f;
            if (_starRenderers != null && _starRenderers.Length > 0 && _starRenderers[0] != null
                && _starRenderers[0].enabled != starsOn)
            {
                for (int i = 0; i < _starRenderers.Length; i++)
                    if (_starRenderers[i] != null) _starRenderers[i].enabled = starsOn;
            }

            if (_moonRenderer != null)
            {
                bool moonOn = night > 0.02f;
                _moonRenderer.enabled = moonOn;
                if (moonOn)
                {
                    // The moon rides the sun's azimuth, opposite phase — elevation eased into 8-62°
                    // so a sky glance (and the proof frame) actually finds it instead of the zenith.
                    float moonPitchDeg = Mathf.Repeat(((dayFraction + 0.5f) * 360f) - 90f, 360f);
                    float elevation = Mathf.Clamp(moonPitchDeg > 180f ? 360f - moonPitchDeg : moonPitchDeg, 8f, 62f);
                    var dir = Quaternion.Euler(-elevation, 150f, 0f) * Vector3.forward;
                    _moon.position = _camera.transform.position + dir * (DomeRadius * 0.92f);
                    _moon.rotation = Quaternion.LookRotation(_moon.position - _camera.transform.position);
                    var c = _moonRenderer.color;
                    c.a = Mathf.Clamp01(night * 1.4f);
                    _moonRenderer.color = c;
                }
            }
        }

        // ----- time source ------------------------------------------------------------------------

        private static float DayFraction()
        {
            // F24 root fix: world-time TRUTH via the per-tick mirror (0-1439). The adapter publishes
            // it from world.Time.TotalMinutes, so clock jumps (respawn, travel) cannot desync the sky.
            if (EmberDomainAdapterLocator.Current != null)
                return Mathf.Repeat(RuntimeFieldMirror.MinutesOfDay, 1440f) / 1440f;
            return (Time.time % 120f) / 120f; // adapterless fallback: a 2-minute day
        }
    }
}

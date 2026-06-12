using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// F33: hit SPARKS — a pooled burst of tiny unlit quads that fly out of a struck billboard
    /// and die in a third of a second. Manual pool on the proven Sprites/Default path because
    /// ParticleSystem renders NOTHING in player builds (the F25 build truth). One burst per
    /// landed strike; the pool recycles, never allocates after warm-up.
    /// </summary>
    public sealed class RuntimeHitSparks : MonoBehaviour
    {
        private const int PoolSize = 24;
        private const int SparksPerBurst = 8;
        private const float LifeSeconds = 0.32f;

        private static RuntimeHitSparks s_instance;
        private Transform[] _quads;
        private Vector3[] _velocities;
        private float[] _dieAt;
        private int _next;

        public static void Burst(Vector3 worldPos)
        {
            if (s_instance == null)
            {
                var go = new GameObject("RuntimeHitSparks");
                s_instance = go.AddComponent<RuntimeHitSparks>();
            }
            s_instance.Emit(worldPos);
        }

        private void Awake()
        {
            _quads = new Transform[PoolSize];
            _velocities = new Vector3[PoolSize];
            _dieAt = new float[PoolSize];
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader) { color = new Color(1f, 0.78f, 0.32f, 0.95f) };
            for (int i = 0; i < PoolSize; i++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Spark";
                Destroy(quad.GetComponent<Collider>());
                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                quad.transform.SetParent(transform, false);
                quad.transform.localScale = Vector3.one * 0.07f;
                quad.SetActive(false);
                _quads[i] = quad.transform;
            }
        }

        private void Emit(Vector3 worldPos)
        {
            Debug.Log($"[Sparks] burst at {worldPos} ({SparksPerBurst} quads, {LifeSeconds:0.00}s).");
            // Deterministic-enough scatter: golden-angle fan, no UnityEngine.Random (visual only).
            for (int k = 0; k < SparksPerBurst; k++)
            {
                int i = _next;
                _next = (_next + 1) % PoolSize;
                float angle = (k * 137.5f + Time.frameCount * 31f) * Mathf.Deg2Rad;
                _velocities[i] = new Vector3(Mathf.Cos(angle) * 1.8f, 2.2f + (k % 3) * 0.5f, Mathf.Sin(angle) * 1.8f);
                _dieAt[i] = Time.unscaledTime + LifeSeconds;
                _quads[i].position = worldPos;
                _quads[i].gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            // Unscaled time: strikes resolve while the combat modal pauses timeScale (the F10 rule).
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_quads[i].gameObject.activeSelf) continue;
                if (now >= _dieAt[i]) { _quads[i].gameObject.SetActive(false); continue; }
                _velocities[i] += Vector3.down * (9f * dt); // sparks fall
                _quads[i].position += _velocities[i] * dt;
            }
        }
    }
}

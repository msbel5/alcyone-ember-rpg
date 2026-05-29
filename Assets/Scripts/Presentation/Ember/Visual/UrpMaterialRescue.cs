using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Visual
{
    /// <summary>
    /// Why this exists: several gameplay scenes ship ParticleSystem / Mesh renderers whose
    /// material slot is empty (<c>m_Materials: [{fileID: 0}]</c> in the scene YAML). Under URP an
    /// unassigned material renders with <c>Hidden/InternalErrorShader</c> — the magenta particles
    /// and pink blobs seen in-world. We have no Editor-time access to re-author every scene, so
    /// this runs on every scene load and assigns a build-safe URP fallback to any renderer whose
    /// material is null or the internal-error (magenta) shader.
    ///
    /// It is non-destructive: renderers that already have a valid material are left untouched, so
    /// authored visuals are never overwritten — only the broken (magenta) ones are repaired.
    /// <c>Universal Render Pipeline/Lit</c> is guaranteed to be in every build (the tile materials
    /// reference it), so the Shader.Find fallback chain always resolves to a real shader.
    /// </summary>
    public static class UrpMaterialRescue
    {
        private const string ErrorShaderName = "Hidden/InternalErrorShader";

        private static Material _particleMaterial;
        private static Material _meshMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Subscribe once; idempotent across domain reloads.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RescueLoadedScene();

        /// <summary>Repair every renderer in the active scene set that has a missing/magenta material.</summary>
        public static int RescueLoadedScene()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int repaired = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !NeedsRescue(r.sharedMaterial)) continue;
                r.sharedMaterial = r is ParticleSystemRenderer ? ParticleMaterial() : MeshMaterial();
                repaired++;
            }

            // Legacy 3D TextMesh labels (e.g. the phase-gate "→ Colony Needs" sign) frequently
            // have their MeshRenderer material overridden to a non-font prop material, so the
            // glyphs never render. Restore the font's own material so the text shows under URP.
            var textMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < textMeshes.Length; i++)
            {
                var tm = textMeshes[i];
                if (tm == null || tm.font == null) continue;
                var mr = tm.GetComponent<MeshRenderer>();
                var fontMat = tm.font.material;
                if (mr != null && fontMat != null && mr.sharedMaterial != fontMat)
                {
                    mr.sharedMaterial = fontMat;
                    repaired++;
                }
            }

            if (repaired > 0)
                Debug.Log($"[UrpMaterialRescue] Repaired {repaired} renderer(s) with missing/magenta materials.");
            return repaired;
        }

        private static bool NeedsRescue(Material material)
        {
            if (material == null) return true;
            var shader = material.shader;
            return shader == null || shader.name == ErrorShaderName;
        }

        private static Material ParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
            _particleMaterial = new Material(shader) { name = "UrpRescue_EmberParticle" };
            var ember = new Color(1f, 0.55f, 0.18f, 0.9f); // warm forge ember
            if (_particleMaterial.HasProperty("_BaseColor")) _particleMaterial.SetColor("_BaseColor", ember);
            if (_particleMaterial.HasProperty("_Color")) _particleMaterial.SetColor("_Color", ember);
            return _particleMaterial;
        }

        private static Material MeshMaterial()
        {
            if (_meshMaterial != null) return _meshMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            _meshMaterial = new Material(shader) { name = "UrpRescue_Fallback" };
            var warmStone = new Color(0.18f, 0.14f, 0.09f, 1f); // matches panel-brown furniture
            if (_meshMaterial.HasProperty("_BaseColor")) _meshMaterial.SetColor("_BaseColor", warmStone);
            if (_meshMaterial.HasProperty("_Color")) _meshMaterial.SetColor("_Color", warmStone);
            return _meshMaterial;
        }
    }
}

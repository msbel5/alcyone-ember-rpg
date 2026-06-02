using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Visual
{
    /// <summary>Manager / scene-load coordinator: one-shot post-load cleanup for item billboards, floors, and default props.</summary>
    public static class SceneDressingCoordinator
    {
        private const float MaxItemBillboardHeight = 1.0f;
        private static readonly string[] ProtectedNames = { "forge", "ember", "billboard", "worksite", "anvil", "player" };
        private static Material _floorMaterial;
        private static Material _propMaterial;
        private static Texture2D _floorTexture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Why: subscribe once so the dressing work runs exactly once per scene load.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        // Why: coordinate all three presentation-only repair passes from one scene-load entrypoint.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int billboards = ClampItemBillboards();
            string floor = ResolveFloor(scene.name);
            int props = RematerialProps();
            Debug.Log($"[SceneDressing] clamped {billboards} billboards, floor={floor}, re-materialed {props} props in '{scene.name}'");
        }
        // Why: keep item and decoration billboards clearly smaller than actor billboards.
        private static int ClampItemBillboards()
        {
            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int clamped = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null || sr.sprite == null || sr.GetComponent<CameraFacingBillboard>() == null) continue;
                if (sr.GetComponentInParent<ActorView>() != null) continue;
                float height = sr.bounds.size.y;
                if (height <= MaxItemBillboardHeight + 0.01f) continue;
                sr.transform.localScale *= MaxItemBillboardHeight / height;
                clamped++;
            }
            return clamped;
        }
        // Why: prefer generated env textures, but hide bright placeholders when generation has no floor for this scene.
        private static string ResolveFloor(string sceneName)
        {
            if (SceneEnvironmentDresser.DressScene(sceneName) > 0) return "generated";
            bool changed = ApplyTerrainFallback() | ApplyFloorRendererFallback();
            return changed ? "fallback" : "unchanged";
        }
        // Why: terrains need a dark diffuse fallback when the generated env texture is missing.
        private static bool ApplyTerrainFallback()
        {
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            bool changed = false;
            for (int i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null) continue;
                var layers = terrain.terrainData.terrainLayers;
                if (layers == null || layers.Length == 0 || layers[0] == null || !LooksPlaceholder(layers[0].diffuseTexture)) continue;
                layers[0].diffuseTexture = FloorTexture();
                terrain.terrainData.terrainLayers = layers;
                changed = true;
            }
            return changed;
        }
        // Why: large authored floor meshes need the same dark fallback when they still show placeholder materials.
        private static bool ApplyFloorRendererFallback()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            bool changed = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !IsFloorCandidate(renderer) || IsProtected(renderer.gameObject.name)) continue;
                if (!IsDefaultMaterial(renderer.sharedMaterial) && !LooksPlaceholder(renderer.sharedMaterial)) continue;
                renderer.sharedMaterial = FloorMaterial();
                changed = true;
            }
            return changed;
        }
        // Why: only literal Unity-default prop materials should be re-themed, never authored/emissive content.
        private static int RematerialProps()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int changed = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsFloorCandidate(renderer) || IsProtected(renderer.gameObject.name)) continue;
                if (renderer.GetComponentInParent<WorksiteView>() != null || HasEmission(renderer.sharedMaterial)) continue;
                if (!IsDefaultMaterial(renderer.sharedMaterial)) continue;
                renderer.sharedMaterial = PropMaterial();
                changed++;
            }
            return changed;
        }
        // Why: target only obvious floor surfaces rather than every large mesh in the scene.
        private static bool IsFloorCandidate(Renderer renderer)
        {
            var name = renderer.gameObject.name.ToLowerInvariant();
            var size = renderer.bounds.size;
            return name.Contains("floor") || name.Contains("ground") || name.Contains("field") || name.Contains("path") ||
                   (size.x * size.z >= 25f && size.y <= 2.5f);
        }
        // Why: preserve known-sensitive names from accidental dressing changes.
        private static bool IsProtected(string name)
        {
            var lower = name.ToLowerInvariant();
            for (int i = 0; i < ProtectedNames.Length; i++) if (lower.Contains(ProtectedNames[i])) return true;
            return false;
        }
        // Why: only Unity's literal default materials should be replaced in the conservative prop pass.
        private static bool IsDefaultMaterial(Material material)
        {
            if (material == null) return true;
            var name = material.name;
            return name.StartsWith("Default-Material") || name.StartsWith("Default-Diffuse");
        }
        // Why: placeholder names are the only safe signal that a floor still needs fallback dressing.
        private static bool LooksPlaceholder(Object asset)
        {
            if (asset == null) return true;
            if (asset is Material material && LooksPlaceholder(material.mainTexture)) return true;
            var name = asset.name.ToLowerInvariant();
            return name.Contains("grid") || name.Contains("checker") || name.Contains("default") || name.Contains("placeholder");
        }
        // Why: emissive materials are intentional scene lighting accents and must stay untouched.
        private static bool HasEmission(Material material)
        {
            if (material == null) return false;
            if (material.IsKeywordEnabled("_EMISSION")) return true;
            return material.HasProperty("_EmissionColor") && material.GetColor("_EmissionColor").maxColorComponent > 0.01f;
        }
        // Why: one shared dark floor material avoids per-renderer allocations during the scene-load pass.
        private static Material FloorMaterial()
        {
            if (_floorMaterial != null) return _floorMaterial;
            _floorMaterial = NewMaterial("SceneDressing_Floor", new Color(0.17f, 0.15f, 0.12f));
            _floorMaterial.mainTexture = FloorTexture();
            if (_floorMaterial.HasProperty("_BaseMap")) _floorMaterial.SetTexture("_BaseMap", _floorMaterial.mainTexture);
            return _floorMaterial;
        }
        // Why: one shared dark prop material re-themes literal default cubes without touching authored materials.
        private static Material PropMaterial() => _propMaterial ??= NewMaterial("SceneDressing_Prop", new Color(0.22f, 0.19f, 0.15f));
        // Why: a tiny procedural floor texture hides the bright grid even when no generated env texture exists yet.
        private static Texture2D FloorTexture()
        {
            if (_floorTexture != null) return _floorTexture;
            _floorTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "SceneDressing_FloorTex", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                _floorTexture.SetPixel(x, y, ((x + y) & 1) == 0 ? new Color(0.15f, 0.13f, 0.10f) : new Color(0.11f, 0.10f, 0.08f));
            _floorTexture.Apply();
            return _floorTexture;
        }
        // Why: centralise shader fallback and tint setup so the scene-load pass stays low-LOC.
        private static Material NewMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }
    }
}

using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Visual
{
    /// <summary>
    /// The deterministic "assignment" half of the loading-screen environment-texture pipeline
    /// (see docs/PRD_loading_asset_generation_v1.md). One-shot per scene load: for every Terrain
    /// in the scene, if the forge produced a themed floor texture on disk
    /// (<c>env_&lt;scene&gt;_&lt;terrain&gt;.png</c>) it is painted onto that terrain's primary
    /// <see cref="TerrainLayer.diffuseTexture"/>. Floors in this game are Unity Terrain, NOT tiled
    /// meshes — so we assign the terrain layer's diffuse, not a material <c>_BaseMap</c>.
    ///
    /// If no generated texture exists yet, the scene keeps its authored layer texture (themed or
    /// the neutral fallback) — this never produces magenta and never polls per frame. The matching
    /// generation step (which writes these files during the loading screen) is wired separately;
    /// until then this is a safe no-op that simply leaves floors as authored.
    /// </summary>
    public static class SceneEnvironmentDresser
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // One-shot per scene load (part of the load, not a per-frame poll): subscribe once.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => DressScene(scene.name);

        /// <summary>Paint generated floor textures onto this scene's terrains. Returns count dressed.</summary>
        public static int DressScene(string sceneName)
        {
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            if (terrains == null || terrains.Length == 0) return 0;

            int dressed = 0;
            for (int i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null) continue;
                var layers = terrain.terrainData.terrainLayers;
                if (layers == null || layers.Length == 0 || layers[0] == null) continue;

                // Scene-keyed texture (env_<scene>) that the forge generates and writes; one themed
                // floor per scene, painted onto every terrain in that scene. Matches the manifest ids.
                var key = Sanitize("env_" + sceneName);
                var tex = TryLoadGenerated(key);
                if (tex == null) continue;

                tex.wrapMode = TextureWrapMode.Repeat; // floors tile
                layers[0].diffuseTexture = tex;
                terrain.terrainData.terrainLayers = layers; // reassign so the terrain refreshes
                dressed++;
            }

            if (dressed > 0)
                Debug.Log($"[SceneEnvironmentDresser] Painted {dressed} terrain floor(s) in '{sceneName}' with generated textures.");
            return dressed;
        }

        private static string Sanitize(string s)
        {
            var chars = s.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '_')) chars[i] = '_';
            return new string(chars);
        }

        /// <summary>
        /// Load a generated PNG by key. The forge writes to <c>Assets/Generated/Core</c> in the
        /// Editor, but a built player has no project Assets folder, so we also check the
        /// runtime-writable locations. First existing file wins; missing file → null (safe no-op).
        /// </summary>
        private static Texture2D TryLoadGenerated(string key)
        {
            string fileName = key + ".png";
            string[] candidates =
            {
                // Editor / play mode: <projectRoot>/Assets/Generated/Core
                Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                             "Assets", "Generated", "Core", fileName),
                // Built player (runtime-writable): persistentDataPath/Generated/Core
                Path.Combine(Application.persistentDataPath, "Generated", "Core", fileName),
                // Optional bundled location
                Path.Combine(Application.streamingAssetsPath, "Generated", "Core", fileName),
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    var path = candidates[i];
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    var bytes = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(bytes)) { Object.Destroy(tex); continue; }
                    tex.name = key;
                    return tex;
                }
                catch
                {
                    // Unreadable candidate — try the next location, never throw into scene load.
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using EmberCrpg.Simulation.Forge;

// Design note:
// ModelBootstrap owns first-launch verification + download of the AI model
// bundle. On Awake it:
//   1. Reads StreamingAssets/Models/manifest.json (via UnityWebRequest so it
//      works inside the Android JAR).
//   2. Calls ModelManifest.VerifyAllPresent against persistentDataPath/Models.
//   3. For each missing entry, downloads from HuggingFace into persistentDataPath
//      and re-hashes — entries that fail SHA verification stay missing.
//   4. Hands resolved disk paths to OnnxAssetForge + NativeLlmClient +
//      EmbeddingClient via ForgeLocator. Gameplay proceeds in placeholder mode
//      while the download runs; the locator gets overwritten when ready.
//
// Layering invariant:
// - ModelBootstrap is Presentation-tier (Unity-bound MonoBehaviour). The actual
//   parsing + verification logic lives in EmberCrpg.Simulation.Forge.ModelManifest.
namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ModelBootstrap : MonoBehaviour
    {
        [SerializeField] private string _manifestRelativePath = "Models/manifest.json";
        [SerializeField] private bool _verboseLogging = true;

        public bool ManifestLoaded { get; private set; }
        public bool BootstrapCompleted { get; private set; }
        public float DownloadProgress { get; private set; }
        public string CurrentEntryId { get; private set; } = string.Empty;
        public IReadOnlyList<ModelEntry> ManifestEntries => _manifestEntries ?? (IReadOnlyList<ModelEntry>)Array.Empty<ModelEntry>();

        private IReadOnlyList<ModelEntry> _manifestEntries;
        private string _persistentRoot;

        public string PersistentModelRoot => _persistentRoot;

        private void Awake()
        {
            _persistentRoot = Path.Combine(Application.persistentDataPath, "Models");
            if (!Directory.Exists(_persistentRoot)) Directory.CreateDirectory(_persistentRoot);

            // Run bootstrap on a background task so we don't block the Unity main
            // thread. We use Task.Run for the SHA verification + HttpClient
            // download path; UnityWebRequest portions stay on the main thread via
            // the coroutine in StartCoroutine.
            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            yield return LoadManifestRoutine();
            if (!ManifestLoaded)
            {
                Log("ModelBootstrap: manifest unavailable — running in placeholder mode.");
                BootstrapCompleted = true;
                yield break;
            }

            // Verification is CPU-bound (SHA256 reads). Run on worker.
            var verifyTask = Task.Run(() => ModelManifest.VerifyAllPresent(_manifestEntries, _persistentRoot));
            while (!verifyTask.IsCompleted) yield return null;
            var missing = verifyTask.Result;

            if (missing.Count == 0)
            {
                Log("ModelBootstrap: all models present. " + _manifestEntries.Count + " entries verified.");
                ApplyLocator();
                BootstrapCompleted = true;
                yield break;
            }

            Log("ModelBootstrap: " + missing.Count + " model(s) missing — starting download.");
            foreach (var entry in missing)
            {
                if (string.IsNullOrWhiteSpace(entry.Url))
                {
                    Log("ModelBootstrap: skipping " + entry.Id + " — no URL in manifest.");
                    continue;
                }
                CurrentEntryId = entry.Id;
                yield return DownloadEntryRoutine(entry);
            }
            CurrentEntryId = string.Empty;

            // Final re-verification — entries that failed SHA stay missing.
            var reverifyTask = Task.Run(() => ModelManifest.VerifyAllPresent(_manifestEntries, _persistentRoot));
            while (!reverifyTask.IsCompleted) yield return null;
            var stillMissing = reverifyTask.Result;
            if (stillMissing.Count > 0)
                Log("ModelBootstrap: " + stillMissing.Count + " model(s) still missing after download. Running degraded.");

            ApplyLocator();
            BootstrapCompleted = true;
        }

        private IEnumerator LoadManifestRoutine()
        {
            string url = BuildStreamingAssetsUrl(_manifestRelativePath);
            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log("ModelBootstrap: failed to load manifest from " + url + " — " + req.error);
                    yield break;
                }
                try
                {
                    _manifestEntries = ModelManifest.LoadFromJson(req.downloadHandler.text);
                    ManifestLoaded = _manifestEntries != null && _manifestEntries.Count > 0;
                    Log("ModelBootstrap: loaded manifest with " + (_manifestEntries?.Count ?? 0) + " entries.");
                }
                catch (Exception ex)
                {
                    Log("ModelBootstrap: manifest parse error: " + ex.Message);
                    _manifestEntries = Array.Empty<ModelEntry>();
                }
            }
        }

        private IEnumerator DownloadEntryRoutine(ModelEntry entry)
        {
            var destPath = ModelManifest.ResolvePath(_persistentRoot, entry.Path);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            using (var req = UnityWebRequest.Get(entry.Url))
            {
                var dh = new DownloadHandlerFile(destPath) { removeFileOnAbort = true };
                req.downloadHandler = dh;
                var op = req.SendWebRequest();

                while (!op.isDone)
                {
                    DownloadProgress = req.downloadProgress;
                    yield return null;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log("ModelBootstrap: download failed for " + entry.Id + " — " + req.error);
                    yield break;
                }
            }

            // Verify SHA if a real hash is provided.
            if (!ModelManifest.IsHashPlaceholder(entry.Sha256))
            {
                var verifyTask = Task.Run(() => ModelManifest.ComputeSha256(destPath));
                while (!verifyTask.IsCompleted) yield return null;
                if (!string.Equals(verifyTask.Result, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Log("ModelBootstrap: SHA mismatch for " + entry.Id + " — deleting.");
                    try { File.Delete(destPath); } catch { }
                }
                else
                {
                    Log("ModelBootstrap: downloaded " + entry.Id + " (verified).");
                }
            }
            else
            {
                Log("ModelBootstrap: downloaded " + entry.Id + " (hash placeholder, skipped verify).");
            }
        }

        private string BuildStreamingAssetsUrl(string relative)
        {
            // streamingAssetsPath on Android is jar://... which UnityWebRequest can read directly.
            // On desktop platforms it's a regular path; we still wrap it as file:// for UWR.
            var raw = Path.Combine(Application.streamingAssetsPath, relative);
            if (raw.Contains("://")) return raw;
            return "file://" + raw.Replace("\\", "/");
        }

        private void ApplyLocator()
        {
            // Build resolved disk paths for the three runtime consumers and
            // register them on the locator. ForgeBootstrap may already have run
            // — overwriting is fine, the locator is a simple static container.
            var sdxlDir = Path.Combine(_persistentRoot, "sdxl-turbo");
            var miniLmDir = Path.Combine(_persistentRoot, "minilm-l6-v2");
            var qwenDir = _persistentRoot;

            try
            {
                var onnxPaths = new[]
                {
                    Path.Combine(sdxlDir, "text_encoder.onnx"),
                    Path.Combine(sdxlDir, "unet.onnx"),
                    Path.Combine(sdxlDir, "vae_decoder.onnx"),
                    Path.Combine(sdxlDir, "tokenizer.json"),
                };
                var onnxForge = new OnnxAssetForge(onnxPaths, OnnxDiffusionFlavor.SdxlTurbo);

                // We keep the existing NativeLlmClient + LlmRoutingService from
                // ForgeLocator if already populated, only replacing the asset
                // forge. NPC embedding client (MiniLM) is stored as a separate
                // entry on the locator — accessible to NpcMemoryStore consumers.
                if (onnxForge.IsAvailable())
                    ForgeLocator.SetAssetForge(onnxForge);

                var embeddingClient = new EmberCrpg.Simulation.AiDm.EmbeddingClient(
                    Path.Combine(miniLmDir, "model.onnx"),
                    Path.Combine(miniLmDir, "tokenizer.json"));
                ForgeLocator.SetEmbeddingClient(embeddingClient);

                _ = qwenDir; // qwen path consumed by NativeLlmClient already registered in ForgeBootstrap.
            }
            catch (Exception ex)
            {
                Log("ModelBootstrap: ApplyLocator error: " + ex.Message);
            }
        }

        private void Log(string msg)
        {
            if (_verboseLogging) Debug.Log(msg);
        }
    }
}

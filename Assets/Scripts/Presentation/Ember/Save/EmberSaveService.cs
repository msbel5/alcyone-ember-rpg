using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Save
{
    // Codex audit (fifth pass J-P3): SaveData moved to SaveData.cs so the
    // service file holds only the MonoBehaviour and its private helpers.

    public sealed class EmberSaveService : MonoBehaviour
    {
        private const string SaveKey = "ember.save.v1";          // legacy single-blob (kept for back-compat)
        private const string LastSlotKey = "ember.save.lastslot"; // EMB-011: PlayerPrefs pointer to the last file slot
        private const int DefaultSlot = 0;
        private UnityEngine.UI.Text _statusText;
        private EmberCrpg.Data.Save.FileSaveRepository _repo;     // EMB-011: durable file-based slots

        private void Awake()
        {
            // EMB-011: durable saves live in files under persistentDataPath/saves/slot_N.json; the
            // PlayerPrefs blob is demoted to a legacy fallback so old saves still load.
            _repo = new EmberCrpg.Data.Save.FileSaveRepository(Application.persistentDataPath);

            var canvas = GameObject.Find("EmberHUD") ?? GameObject.FindAnyObjectByType<Canvas>()?.gameObject;
            if (canvas == null) return;

            var go = new GameObject("SaveStatus", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 100);
            rt.anchoredPosition = Vector2.zero;
            
            _statusText = go.GetComponent<UnityEngine.UI.Text>();
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 32;
            _statusText.color = new Color(1f, 1f, 1f, 0f);
        }

        private void Update()
        {
            if (EmberInput.SaveQuick) Save();
            if (EmberInput.LoadQuick) Load();
        }

        public void Save()
        {
            var player = GameObject.Find("PlayerRig");
            if (player == null) return;

            int ticks = 0;
            string domainJson = string.Empty;
            bool domainAvailable = false;
            bool domainFailed = false;
            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
            if (adapter != null)
            {
                domainAvailable = true;
                ticks = adapter.TickIndex;
                // Codex audit Batch 2 / Finding 3: bundle the full deterministic
                // simulation state so F9 / Continue restores more than the player
                // rig transform.
                // Codex audit (second pass A-P1): previously we swallowed the
                // export failure and persisted an empty domainStateJson,
                // showing "Saved." even though the deterministic snapshot was
                // lost. Track the failure and surface it via ShowStatus so the
                // user knows the rig+tick saved but the world state did not.
                try
                {
                    domainJson = adapter.ExportStateJson() ?? string.Empty;
                }
                catch (System.Exception)
                {
                    domainJson = string.Empty;
                    domainFailed = true;
                }
            }

            var data = new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = player.transform.position,
                playerYaw = player.transform.eulerAngles.y,
                tickIndex = ticks,
                domainStateJson = domainJson,
            };

            var jsonStr = JsonUtility.ToJson(data);
            // DET-05: write the durable file slot FIRST; only mirror to the legacy PlayerPrefs blob +
            // last-slot pointer AFTER it succeeds. Previously the blob was written unconditionally and
            // the file slot in a swallowed try/catch — so a file-write failure left the NEW save in
            // PlayerPrefs but the OLD save in the file slot, and Load (which prefers the file slot)
            // silently returned stale state. Now file and blob update together or neither does, so the
            // two stores can never diverge.
            bool durableOk;
            try
            {
                if (_repo == null) throw new System.InvalidOperationException("no save repository");
                _repo.Save(DefaultSlot, jsonStr);
                durableOk = true;
            }
            catch (System.Exception)
            {
                durableOk = false;
            }

            if (durableOk)
            {
                PlayerPrefs.SetInt(LastSlotKey, DefaultSlot);
                PlayerPrefs.SetString(SaveKey, jsonStr); // legacy mirror, consistent with the file slot
                PlayerPrefs.Save();
                ShowStatus(domainAvailable && domainFailed ? "Save partial: domain export failed." : "Saved.");
            }
            else
            {
                // Durable write failed — keep the previous consistent (file + blob) save untouched
                // and tell the player, rather than silently persisting a divergent blob.
                ShowStatus("Save failed: could not write save slot.");
            }
        }

        public void Load()
        {
            // EMB-011: prefer the durable file slot (with corrupt-save quarantine); fall back to the
            // legacy PlayerPrefs blob so saves written before file slots existed still load.
            string json = null;
            if (_repo != null)
            {
                int lastSlot = PlayerPrefs.GetInt(LastSlotKey, DefaultSlot);
                if (_repo.TryLoad(lastSlot, IsLoadableSaveJson, out var fileJson))
                    json = fileJson;
            }
            if (string.IsNullOrEmpty(json))
                json = PlayerPrefs.GetString(SaveKey);   // legacy fallback
            if (string.IsNullOrEmpty(json))
            {
                ShowStatus("No save found.");
                return;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception)
            {
                ShowStatus("Load failed: save corrupt.");
                return;
            }
            if (data == null || string.IsNullOrEmpty(data.sceneName))
            {
                ShowStatus("Load failed: invalid save payload.");
                return;
            }

            if (SceneManager.GetActiveScene().name != data.sceneName)
            {
                // Codex audit (fourth pass F-P3): match the main-menu Continue
                // path — validate against EditorBuildSettings before LoadScene
                // (Editor only; player builds let Unity surface its own
                // "scene not in build" error).
                if (!IsKnownBuildScene(data.sceneName))
                {
                    ShowStatus("Load failed: scene not in build.");
                    return;
                }
                _pendingLoad = data;
                SceneManager.LoadScene(data.sceneName);
            }
            else
            {
                RestorePosition(data);
                // Codex audit (second pass A-P1): only claim "Loaded." when the
                // domain restore actually succeeded. A swallowed RestoreStateJson
                // throw used to still flash "Loaded." while leaving world state
                // at its pre-load baseline.
                var domainResult = ApplyDomainRestore(data);
                // Codex audit (third pass A-P2): NoAdapter used to fall through
                // to "Loaded." even though a non-empty domainStateJson payload
                // was silently dropped. Treat both Failed AND NoAdapter as
                // partial loads when the save carried a payload — only the
                // empty/absent payload path is a full success.
                if (domainResult == DomainRestoreResult.Failed)
                    ShowStatus("Load partial: domain restore failed.");
                else if (domainResult == DomainRestoreResult.NoAdapter)
                    ShowStatus("Load partial: domain restore unavailable.");
                else
                    ShowStatus("Loaded.");
            }
        }

        private static SaveData _pendingLoad;

        /// <summary>
        /// Codex audit (fourth pass F-P3): mirror EmberMainMenuUI's build-
        /// scene validation. Editor build only; player builds trust Unity's
        /// runtime resolution.
        /// </summary>
        /// <summary>EMB-011: the quarantine predicate for file slots. A slot is "loadable" only if it
        /// parses to a SaveData carrying a non-empty sceneName; anything else (truncated write, garbage,
        /// schema drift that drops the scene) is treated as corrupt and moved aside by TryLoad so it can
        /// never crash the loader, with the legacy PlayerPrefs blob as the fallback.</summary>
        private static bool IsLoadableSaveJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            try
            {
                var probe = JsonUtility.FromJson<SaveData>(raw);
                return probe != null && !string.IsNullOrEmpty(probe.sceneName);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool IsKnownBuildScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene == null || string.IsNullOrEmpty(scene.path)) continue;
                var stem = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(stem, sceneName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
#else
            return true;
#endif
        }

        /// <summary>
        /// Codex audit Batch 2 / Finding 4: the main-menu Continue() button used
        /// to LoadScene(name) but had no way to push the deserialized SaveData
        /// into the next-scene EmberSaveService — `_pendingLoad` was private.
        /// Now Continue() calls this hook before LoadScene; the new scene's
        /// EmberSaveService picks the payload up in Start() and restores both
        /// the player rig transform and the domain adapter state.
        /// </summary>
        public static void PreparePendingLoad(SaveData data)
        {
            _pendingLoad = data;
        }

        private void Start()
        {
            if (_pendingLoad != null && _pendingLoad.sceneName == SceneManager.GetActiveScene().name)
            {
                RestorePosition(_pendingLoad);
                var domainResult = ApplyDomainRestore(_pendingLoad);
                _pendingLoad = null;
                // Codex audit (third pass A-P2): NoAdapter used to fall through
                // to "Loaded." even though a non-empty domainStateJson payload
                // was silently dropped. Treat both Failed AND NoAdapter as
                // partial loads when the save carried a payload — only the
                // empty/absent payload path is a full success.
                if (domainResult == DomainRestoreResult.Failed)
                    ShowStatus("Load partial: domain restore failed.");
                else if (domainResult == DomainRestoreResult.NoAdapter)
                    ShowStatus("Load partial: domain restore unavailable.");
                else
                    ShowStatus("Loaded.");
            }
        }

        /// <summary>
        /// Codex audit (second pass A-P1): outcome of <see cref="ApplyDomainRestore"/>.
        /// The previous void return swallowed export/restore exceptions and
        /// allowed the UI to flash "Loaded." even when world state was lost.
        /// </summary>
        public enum DomainRestoreResult
        {
            /// <summary>No payload to restore — vanilla rig+tick load.</summary>
            NoPayload = 0,
            /// <summary>No adapter registered for the scene.</summary>
            NoAdapter = 1,
            /// <summary>RestoreStateJson threw — adapter state unchanged.</summary>
            Failed = 2,
            /// <summary>RestoreStateJson + tick AlignTo both succeeded.</summary>
            Restored = 3,
        }

        private static DomainRestoreResult ApplyDomainRestore(SaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.domainStateJson)) return DomainRestoreResult.NoPayload;
            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
            if (adapter == null) return DomainRestoreResult.NoAdapter;

            // Codex review on PR #188 (P2): gate the tick AlignTo behind a
            // successful RestoreStateJson. If the envelope is malformed (version
            // mismatch, truncated payload, placeholder mismatch) the catch
            // swallowed the exception but the AlignTo still ran, advancing the
            // driver to saved+1 while the adapter remained at its pre-load
            // state — a brand new desync. Skip AlignTo on failure so the driver
            // continues from its current value and the existing snapshot is not
            // clobbered by a mis-aligned OnTick.
            bool restored = false;
            try
            {
                adapter.RestoreStateJson(data.domainStateJson);
                restored = true;
            }
            catch (System.Exception)
            {
                // Codex audit (second pass A-P1): the caller now distinguishes
                // failure from no-payload so the UI can avoid claiming "Loaded."
                return DomainRestoreResult.Failed;
            }

            if (!restored) return DomainRestoreResult.Failed;

            // Codex review on PR #185 (P1): align the scene's EmberTickDriver to
            // the restored tick so the next OnTick(...) does not roll the
            // adapter's just-restored timeline back to 1. Without this sync,
            // any save at tick > 0 is clobbered on the very next frame.
            var driver = UnityEngine.Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.Tick.EmberTickDriver>(
                FindObjectsInactive.Include);
            if (driver != null)
                driver.AlignTo(data.tickIndex);
            return DomainRestoreResult.Restored;
        }

        private void RestorePosition(SaveData data)
        {
            var player = GameObject.Find("PlayerRig");
            if (player != null)
            {
                // Disable character controller while moving to prevent collision fighting
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                player.transform.position = data.playerPosition;
                player.transform.rotation = Quaternion.Euler(0, data.playerYaw, 0);
                
                if (cc != null) cc.enabled = true;

                var fps = player.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fps != null)
                {
                    fps.SyncYaw(data.playerYaw);
                }
            }
        }

        private void ShowStatus(string msg)
        {
            StopAllCoroutines();
            StartCoroutine(FadeStatus(msg));
        }

        private IEnumerator FadeStatus(string msg)
        {
            if (_statusText == null) yield break;
            _statusText.text = msg;
            float elapsed = 0;
            while (elapsed < 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = elapsed < 0.5f ? elapsed / 0.5f : (elapsed > 1.5f ? 1f - (elapsed - 1.5f) / 0.5f : 1f);
                _statusText.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            _statusText.color = new Color(1, 1, 1, 0);
        }
    }
}

// Why this file is intentionally long: EmberSaveService owns the Unity-facing save/load orchestration, status UI, and slot metadata bridge while pure file IO remains in Data.Save.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Save
{
    // Codex audit (fifth pass J-P3): SaveData moved to SaveData.cs so the
    // service file holds only the MonoBehaviour and its private helpers.

    public sealed partial class EmberSaveService : MonoBehaviour
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
            // BUG-SAVE-CRASH: a quick-save (F5) in some scenes closed the game with NO managed
            // exception in Player.log — i.e. it escaped as a hard quit. The inner domain export was
            // already guarded, but anything else in this method (a null deref outside those guards,
            // a scene missing an expected component) would propagate out of Update and take the
            // process down. Wrap the WHOLE body so a quick-save can NEVER crash/close the game:
            // every catchable managed failure is logged and surfaced as a "Save failed" status, and
            // the player keeps playing. (A StackOverflowException is uncatchable by design, but the
            // save path is flat field-mapping with no recursion, so that is not a live risk here.)
            Debug.Log("[EmberSave] quick-save start");
            try
            {
                SaveInternal(SaveSlotId.Quick);
                Debug.Log("[EmberSave] quick-save ok");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[EmberSave] quick-save failed: " + ex);
                ShowStatus("Save failed.");
            }
        }

        public void SaveSlot(SaveSlotId slot)
        {
            Debug.Log("[EmberSave] slot-save start " + slot);
            try
            {
                SaveInternal(slot);
                Debug.Log("[EmberSave] slot-save ok " + slot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[EmberSave] slot-save failed: " + ex);
                ShowStatus("Save failed.");
            }
        }

        public void SaveAuto()
        {
            SaveSlot(SaveSlotId.Auto);
        }

        public void DeleteSlot(SaveSlotId slot)
        {
            Debug.Log("[EmberSave] slot-delete start " + slot);
            try
            {
                if (_repo == null) throw new System.InvalidOperationException("no save repository");
                bool existed = _repo.Delete(slot);
                ShowStatus(existed ? "Save slot deleted." : "Save slot empty.");
                Debug.Log("[EmberSave] slot-delete ok " + slot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[EmberSave] slot-delete failed: " + ex);
                ShowStatus("Delete failed.");
            }
        }

        public static bool TryAutosaveActiveScene()
        {
            try
            {
                var svc = UnityEngine.Object.FindFirstObjectByType<EmberSaveService>(FindObjectsInactive.Include);
                if (svc == null) return false;
                svc.SaveAuto();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[EmberSave] autosave skipped: " + ex.Message);
                return false;
            }
        }

        private void SaveInternal(SaveSlotId slot)
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

            var jsonStr = SaveEnvelopeCodec.Encode(data);
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
                _repo.Save(slot, jsonStr, BuildMetadata(slot, data, domainJson));
                durableOk = true;
            }
            catch (System.Exception)
            {
                durableOk = false;
            }

            if (durableOk)
            {
                PlayerPrefs.SetInt(LastSlotKey, slot.Kind == SaveSlotKind.Manual ? slot.Index : DefaultSlot);
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

        private static SaveSlotMetadata BuildMetadata(SaveSlotId slot, SaveData data, string domainJson)
        {
            return new SaveSlotMetadata
            {
                metadataVersion = 1,
                envelopeVersion = SaveEnvelope.CurrentVersion,
                schemaVersion = ExtractInt(domainJson, "\"schemaVersion\""),
                slotKind = slot.Kind.ToString(),
                slotIndex = slot.Kind == SaveSlotKind.Manual ? slot.Index : 0,
                label = slot.Kind == SaveSlotKind.Manual ? "Manual " + (slot.Index + 1) : slot.Kind.ToString(),
                sceneName = data?.sceneName ?? string.Empty,
                playtimeMinutes = ExtractLong(domainJson, "\"totalMinutes\""),
                savedAtUtcIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                thumbnailPath = string.Empty,
            };
        }

        private static int ExtractInt(string json, string marker)
        {
            long value = ExtractLong(json, marker);
            return value < int.MinValue || value > int.MaxValue ? 0 : (int)value;
        }

        private static long ExtractLong(string json, string marker)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(marker)) return 0L;
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return 0L;
            int colon = json.IndexOf(':', start + marker.Length);
            if (colon < 0) return 0L;
            int cursor = colon + 1;
            while (cursor < json.Length && char.IsWhiteSpace(json[cursor])) cursor++;
            int end = cursor;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            return long.TryParse(json.Substring(cursor, end - cursor), out var parsed) ? parsed : 0L;
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

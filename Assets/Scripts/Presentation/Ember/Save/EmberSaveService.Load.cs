// REF-b (LEFT-019): load/restore split out of EmberSaveService.cs (partial, zero behaviour change).
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Save
{
    public sealed partial class EmberSaveService
    {
        public void Load()
        {
            // BUG-SAVE-CRASH (symmetry with Save): a quick-load (F9) must never crash/close the game
            // either. Wrap the whole body so any catchable managed failure is logged and surfaced as
            // a "Load failed" status rather than escaping out of Update as a hard quit.
            Debug.Log("[EmberSave] quick-load start");
            try
            {
                LoadInternal();
                Debug.Log("[EmberSave] quick-load ok");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[EmberSave] quick-load failed: " + ex);
                ShowStatus("Load failed.");
            }
        }

        private void LoadInternal()
        {
            // EMB-011 / BD-14: resolve the save JSON through the SAME store-precedence helper the
            // main-menu Continue path now uses (durable file slot first, legacy PlayerPrefs blob as
            // fallback) so the menu and in-game load can never diverge.
            string json = ResolveLatestSaveJson(_repo);
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
    }
}

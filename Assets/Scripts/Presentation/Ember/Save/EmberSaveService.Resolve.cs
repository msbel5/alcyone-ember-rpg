// REF-b (LEFT-019): save-resolution + pending-load split out of EmberSaveService.cs (partial, zero behaviour change).
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Runtime;

namespace EmberCrpg.Presentation.Ember.Save
{
    public sealed partial class EmberSaveService
    {
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
            return SaveEnvelopeCodec.TryDecode(raw, out _, out _);
        }

        private static bool IsKnownBuildScene(string sceneName)
        {
            return EmberSceneCatalog.IsKnownBuildScene(sceneName);
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

        /// <summary>
        /// BD-14 (EMB3-019): single source of truth for "where does the latest save live". Mirrors
        /// the EMB-011 precedence the in-game quick-load uses: the durable file slot pointed to by
        /// <see cref="LastSlotKey"/> first (with the same <see cref="IsLoadableSaveJson"/> corrupt-save
        /// quarantine), then the legacy single-blob PlayerPrefs key as a back-compat fallback. The
        /// menu Continue/Load path and <see cref="LoadInternal"/> both call this so they can never
        /// diverge (previously the menu read PlayerPrefs(SaveKey) directly and skipped the file slot).
        /// </summary>
        private static string ResolveLatestSaveJson(EmberCrpg.Data.Save.FileSaveRepository repo)
        {
            string json = null;
            if (repo != null)
            {
                if (repo.TryLoadPayload(SaveSlotId.Quick, IsLoadableSaveJson, out var quickJson))
                    return quickJson;

                int lastSlot = PlayerPrefs.GetInt(LastSlotKey, DefaultSlot);
                if (repo.TryLoad(lastSlot, IsLoadableSaveJson, out var fileJson))
                    json = fileJson;
            }
            if (string.IsNullOrEmpty(json))
                json = PlayerPrefs.GetString(SaveKey);   // legacy fallback
            return json;
        }

        public static bool AuditIsLoadableSaveJson(string raw) => IsLoadableSaveJson(raw);

        public static bool AuditIsKnownBuildScene(string sceneName) => IsKnownBuildScene(sceneName);

        public static string AuditResolveLatestSaveJson(EmberCrpg.Data.Save.FileSaveRepository repo) =>
            ResolveLatestSaveJson(repo);

        public static int AuditDefaultSlot => DefaultSlot;

        public static string AuditSaveKey => SaveKey;

        public static string AuditLastSlotKey => LastSlotKey;

        /// <summary>
        /// BD-14 (EMB3-019): resolves the latest save through the service's durable-file-slot-first
        /// path (PlayerPrefs only as the legacy fallback INSIDE here) and deserializes it. The
        /// main-menu Continue/Load buttons call this instead of reading PlayerPrefs(SaveKey)
        /// themselves, so the menu and in-game load share one path. Returns false when no loadable
        /// save exists or the payload is corrupt/empty.
        /// </summary>
        public static bool TryResolveLatestSave(out SaveData data)
        {
            data = null;
            // The menu has no live service instance, so build a repository over the same root Awake
            // uses (persistentDataPath/saves). Construction is cheap and side-effect-free.
            EmberCrpg.Data.Save.FileSaveRepository repo = null;
            try { repo = new EmberCrpg.Data.Save.FileSaveRepository(Application.persistentDataPath); }
            catch (System.Exception) { /* fall through to PlayerPrefs-only resolution */ }

            var json = ResolveLatestSaveJson(repo);
            if (string.IsNullOrEmpty(json)) return false;

            if (!SaveEnvelopeCodec.TryDecode(json, out data, out _)) return false;
            // E7-008: validate the save's scene against the build registry HERE, at the single resolution
            // point, so NO load entry point (menu Continue/Load, in-game F9) can hand a SaveData that
            // names a renamed/removed scene to SceneManager.LoadScene. A save with an unknown scene is
            // treated as "no loadable save" (callers start a new game / show "no saves").
            if (data == null || string.IsNullOrEmpty(data.sceneName) || !IsKnownBuildScene(data.sceneName))
            {
                data = null;
                return false;
            }
            return true;
        }
    }
}

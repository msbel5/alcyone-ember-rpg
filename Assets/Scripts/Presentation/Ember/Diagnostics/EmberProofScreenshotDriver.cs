using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Presentation.Ember.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Diagnostics
{
    public sealed class EmberProofScreenshotDriver : MonoBehaviour
    {
        private string _outputDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!HasArg("--ember-proof-screenshots")) return;
            var go = new GameObject("EmberProofScreenshotDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<EmberProofScreenshotDriver>();
        }

        private IEnumerator Start()
        {
            _outputDir = ResolveOutputDir();
            Directory.CreateDirectory(_outputDir);
            yield return CaptureAfter(1.5f, "proof_01_boot_or_mainmenu");
            yield return CaptureAfter(2.0f, "proof_02_mainmenu");

            var menu = FindFirstObjectByType<EmberMainMenuUI>();
            if (menu != null) menu.NewGame();
            yield return CaptureAfter(3.0f, "proof_03_after_new_game");
            yield return CaptureAfter(2.0f, "proof_04_scene_" + SceneManager.GetActiveScene().name);
            MountWorldgenProof();
            yield return CaptureAfter(1.0f, "proof_05_worldgen_log");

            if (HasArg("--ember-proof-quit")) Application.Quit();
        }

        private static void MountWorldgenProof()
        {
            var go = new GameObject("WorldgenProofView");
            var view = go.AddComponent<WorldgenViewController>();
            view.Configure("SmithingOverworld");
            view.Play(new List<WorldgenVisibleEvent>
            {
                WorldgenVisibleEvent.Region("ash-coast"),
                WorldgenVisibleEvent.Settlement("cinderwatch", "ash-coast"),
                WorldgenVisibleEvent.Npc("npc-smith-001", "{\"archetype_id\":\"humanoid_male\",\"world_style_anchor\":\"ember-warm\"}"),
                WorldgenVisibleEvent.Dice("first omen", 20, 13),
                WorldgenVisibleEvent.Question("q-start", "Choose the first road.", new[] { "north", "below" }),
                WorldgenVisibleEvent.Failure("sample_generation_failure"),
                WorldgenVisibleEvent.Completed("World built. Regions: 1, Settlements: 1, NPCs: 1. 1 failures."),
            });
            view.AnswerQuestion(0);
        }

        private IEnumerator CaptureAfter(float seconds, string name)
        {
            yield return new WaitForSeconds(seconds);
            var path = Path.Combine(_outputDir, name + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[EmberProofScreenshotDriver] wrote " + path);
            yield return new WaitForSeconds(0.25f);
        }

        private static string ResolveOutputDir()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], "--ember-proof-screenshots", StringComparison.Ordinal))
                    return Path.GetFullPath(args[i + 1]);
            return Path.Combine(Application.persistentDataPath, "proof-screenshots");
        }

        private static bool HasArg(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], arg, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}

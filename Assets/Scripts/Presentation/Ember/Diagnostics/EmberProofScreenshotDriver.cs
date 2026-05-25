using System;
using System.Collections;
using System.IO;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Worldgen;
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
            yield return CaptureAfter(1.5f, "boot");
            yield return CaptureAfter(0.5f, "assetgen");
            LoadingScreen.Dismiss();
            yield return new WaitForSeconds(0.4f);

            SceneManager.LoadScene("MainMenu");
            yield return CaptureAfter(1.0f, "mainmenu");

            SceneManager.LoadScene("CharacterCreation");
            yield return new WaitForSeconds(1.0f);
            var creation = FindFirstObjectByType<CharacterCreationController>();
            if (creation != null)
            {
                creation.AutoLaunchWorldgen = false;
                DriveToBuildSelection(creation);
            }
            yield return CaptureAfter(0.5f, "cc_skill");

            if (creation != null)
            {
                creation.Back();
                yield return null;
            }
            yield return CaptureAfter(0.5f, "cc_dice");

            if (creation != null)
            {
                DriveToDossier(creation);
                yield return null;
            }
            yield return CaptureAfter(0.5f, "cc_portrait");

            if (creation != null)
            {
                Destroy(creation.gameObject);
                yield return null;
            }

            MountWorldgenProof();
            yield return CaptureAfter(1.0f, "worldgen_question");
            var worldgen = FindFirstObjectByType<WorldgenViewController>();
            if (worldgen != null && worldgen.QuestionOpen)
                worldgen.AnswerQuestion(0);
            yield return CaptureAfter(0.5f, "assetgen_failures");

            if (HasArg("--ember-proof-quit")) Application.Quit();
        }

        private static void DriveToBuildSelection(CharacterCreationController creation)
        {
            if (creation.CurrentStep == CharacterCreationController.CreationStep.CommanderIdentity)
                creation.Continue();
            for (int i = 0; i < 10; i++)
                creation.SelectAnswerByIndex(i % 3);
            creation.SkipHistoryReveal();
            creation.Continue();
            creation.RollAllAttributes();
            creation.KeepThisRoll();
            creation.Continue();
            creation.SelectClass("mage");
            creation.SelectAlignment("neutral_good");
            creation.ToggleSkill("insight");
            creation.ToggleSkill("deception");
        }

        private static void DriveToDossier(CharacterCreationController creation)
        {
            if (creation.CurrentStep == CharacterCreationController.CreationStep.StatRolling)
                creation.Continue();
            if (creation.CurrentStep == CharacterCreationController.CreationStep.BuildSelection && creation.CanAdvance)
                creation.Continue();
        }

        private static void MountWorldgenProof()
        {
            var go = new GameObject("WorldgenProofView");
            var view = go.AddComponent<WorldgenViewController>();
            view.Configure("SmithingOverworld");
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            view.PlayFromGeneratedWorld(world, new WorldgenProjectionOptions(
                maxRegions: 2,
                maxSettlements: 3,
                maxNpcs: 5,
                maxHistoryEvents: 5,
                includeQuestionPrompt: true,
                includeSyntheticFailure: true));
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

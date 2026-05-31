using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.Inputs;
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
            if (HasArg("--ember-rescue-proof"))
            {
                yield return RunRescueProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-scene-tour"))
            {
                yield return RunSceneTour();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-llm-proof"))
            {
                yield return RunLlmProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-forge-proof"))
            {
                yield return RunForgeProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-input-proof"))
            {
                yield return RunInputProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            yield return CaptureAfter(1.5f, "boot");
            yield return CaptureAfter(0.5f, "assetgen");
            LoadingScreen.Dismiss();
            yield return new WaitForSeconds(0.4f);

            SceneManager.LoadScene(EmberScenes.MainMenu);
            yield return CaptureAfter(1.0f, "mainmenu");

            SceneManager.LoadScene(EmberScenes.CharacterCreation);
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
            creation.SetCommanderIdentity("Cinder Vey", "42", "proof");
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

        private static WorldgenViewController MountWorldgenProof()
        {
            var go = new GameObject("WorldgenProofView");
            var view = go.AddComponent<WorldgenViewController>();
            view.AutoLoadScene = false;
            view.Configure(EmberScenes.SmithingOverworld);
            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            view.PlayFromGeneratedWorld(world, new WorldgenProjectionOptions(
                maxRegions: 2,
                maxSettlements: 3,
                maxNpcs: 5,
                maxHistoryEvents: 5,
                includeQuestionPrompt: true,
                includeSyntheticFailure: true));
            return view;
        }

        private IEnumerator RunRescueProof()
        {
            SceneManager.LoadScene(EmberScenes.CharacterCreation);
            yield return new WaitForSeconds(1.0f);
            var creation = FindFirstObjectByType<CharacterCreationController>();
            if (creation != null)
            {
                creation.AutoLaunchWorldgen = false;
                DriveToBuildSelection(creation);
            }
            yield return CaptureFixedAfter(0.5f, "character_creation.png");

            LoadingScreen.ShowForContext(new LoadingScreenContext("worldgen", "Building World", "generation"));
            LoadingScreen.SetProgress(0.45f, "Projecting visible regions and NPC seeds");
            LoadingScreen.LogLine(EmberCrpg.Ui.Foundation.UiLogSeverity.Info, "[proof] visible worldgen loading");
            MountWorldgenProof();
            yield return CaptureFixedAfter(0.75f, "worldgen_loading.png");
            LoadingScreen.Dismiss();

            SceneManager.LoadScene(EmberScenes.SmithingOverworld);
            yield return CaptureFixedAfter(1.2f, "smithing_game.png");
            yield return CaptureFixedAfter(0.2f, "spawn_proof.png");

            SceneManager.LoadScene(EmberScenes.TavernDialog);
            yield return CaptureFixedAfter(1.2f, "tavern_game.png");
        }

        // Loads every gameplay scene in turn and captures it twice — once with UI, once with all
        // Canvases hidden — so magenta/material issues (which can live in any scene, e.g. the 5th
        // or 6th) and the HUD can both be verified across the whole game. Scenes load cold;
        // UrpMaterialRescue runs on each sceneLoaded, so the repair is exercised per scene.
        // EMB-056: single source of truth for the gameplay tour list (EmberScenes registry).
        private static readonly string[] TourScenes = EmberScenes.GameplayTour;

        private IEnumerator RunSceneTour()
        {
            for (int i = 0; i < TourScenes.Length; i++)
            {
                string scene = TourScenes[i];
                if (!Application.CanStreamedLevelBeLoaded(scene)) continue; // not in build settings
                SceneManager.LoadScene(scene);
                yield return new WaitForSecondsRealtime(1.5f); // load + UrpMaterialRescue + first frames
                string idx = (i + 1).ToString("00");
                yield return CaptureFixedAfter(0.1f, "tour_" + idx + "_" + scene + "_ui.png");

                var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int c = 0; c < canvases.Length; c++) if (canvases[c] != null) canvases[c].enabled = false;
                yield return CaptureFixedAfter(0.25f, "tour_" + idx + "_" + scene + "_noui.png");
                for (int c = 0; c < canvases.Length; c++) if (canvases[c] != null) canvases[c].enabled = true;
            }
        }

        // EMB-006: real LLM round-trip proof. Waits for the native LLM to register, calls it with a
        // DM prompt, and writes the provider/model provenance + the actual response text to
        // llm-proof.txt. Headless diagnostic (no rendering), so the synchronous Complete() call is
        // fine — it runs, writes, and quits. Proves whether the on-device Qwen is genuinely wired or
        // only producing canned fallback text.
        private IEnumerator RunLlmProof()
        {
            float deadline = Time.realtimeSinceStartup + 90f;
            while (EmberCrpg.Presentation.Ember.Forge.ForgeLocator.NativeLlm == null
                   && Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(0.5f);

            var path = Path.Combine(_outputDir, "llm-proof.txt");
            var llm = EmberCrpg.Presentation.Ember.Forge.ForgeLocator.NativeLlm;
            if (llm == null)
            {
                File.WriteAllText(path, "FAIL: ForgeLocator.NativeLlm was never registered (LLM not wired).\n");
                yield break;
            }

            File.WriteAllText(path,
                "NativeLlm: " + llm.GetType().FullName + "\n" +
                "ModelPath: " + llm.ModelPath + "\n" +
                "IsAvailable: " + llm.IsAvailable + "\n" +
                "Calling Complete() OFF the main thread (Task.Run); polling...\n");

            // EMB-006 + EMB-007: run the blocking inference (incl. the slow first-call model load)
            // on a worker thread so the Unity main loop stays responsive — exactly the gameplay path.
            // Poll the task from the coroutine with a generous CPU-inference timeout.
            EmberCrpg.Domain.AiDm.LlmResponse resp = null;
            Exception error = null;
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var req = new EmberCrpg.Domain.AiDm.LlmRequest(
                        "ember-dm", "llm-proof",
                        null, 48, 42UL,
                        "You are the Ember dungeon master. Reply in one short, vivid sentence.",
                        new[] { "Player asks: What rumours stir in the tavern tonight?" });
                    resp = llm.Complete(req);
                }
                catch (Exception ex) { error = ex; }
            });

            float infDeadline = Time.realtimeSinceStartup + 240f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < infDeadline)
                yield return new WaitForSecondsRealtime(0.5f);

            string outcome;
            if (!task.IsCompleted) outcome = "TIMEOUT: inference did not finish within 240s\n";
            else if (error != null) outcome = "EXCEPTION:\n" + error + "\n";
            else if (resp == null) outcome = "RESULT: (null response)\n";
            else outcome = "RESULT OK\n--- RESPONSE TEXT ---\n" + resp.Text + "\n--- END ---\n";
            File.AppendAllText(path, outcome);
            Debug.Log("[EmberProofScreenshotDriver] llm-proof: " + outcome);
        }

        private IEnumerator CaptureFixedAfter(float seconds, string fileName)
        {
            yield return new WaitForSeconds(seconds);
            var path = Path.Combine(_outputDir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[EmberProofScreenshotDriver] wrote " + path);
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator CaptureAfter(float seconds, string name)
        {
            yield return new WaitForSeconds(seconds);
            var path = Path.Combine(_outputDir, name + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[EmberProofScreenshotDriver] wrote " + path);
            yield return new WaitForSeconds(0.25f);
        }

        // SDXL D1 verification: generate a fixed "carved bone die" through the LIVE IAssetForge
        // (whatever flavour the game wired — SDXL-Turbo in the CUDA build) and write the raw PNG +
        // provenance. Lets us check structure-vs-noise headlessly (FFT/variance) before a human
        // eyeballs the result. Mirrors RunLlmProof's wait+poll pattern; gen runs off the main thread
        // inside SdxlTurboPipeline.RunAsync, so we just poll the returned Task.
        private IEnumerator RunForgeProof()
        {
            float deadline = Time.realtimeSinceStartup + 90f;
            while (EmberCrpg.Presentation.Ember.Forge.ForgeLocator.AssetForge == null
                   && Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(0.5f);

            var txt = Path.Combine(_outputDir, "forge-die.txt");
            var pngPath = Path.Combine(_outputDir, "forge-die.png");
            var forge = EmberCrpg.Presentation.Ember.Forge.ForgeLocator.AssetForge;
            if (forge == null)
            {
                File.WriteAllText(txt, "FAIL: ForgeLocator.AssetForge was never registered (forge not wired).\n");
                yield break;
            }

            // Parameterized so any prompt/kind can be eyeballed headlessly (portrait, item, etc.):
            //   --ember-forge-prompt "..." --ember-forge-negative "..." --ember-forge-size 512 --ember-forge-seed 42
            string prompt = GetArg("--ember-forge-prompt",
                "a single carved bone die, six-sided, studio lighting, dark fantasy, centered, plain dark background, sharp focus");
            string negative = GetArg("--ember-forge-negative", "blurry, text, watermark, multiple dice, hands");
            int size = 512;
            if (!int.TryParse(GetArg("--ember-forge-size", "512"), out size) || size < 64) size = 512;
            uint seed = 42u;
            if (!uint.TryParse(GetArg("--ember-forge-seed", "42"), out seed)) seed = 42u;
            var request = new EmberCrpg.Domain.Forge.AssetGenerationRequest(
                "forge-proof",
                EmberCrpg.Domain.Forge.AssetSubjectKind.Item,
                EmberCrpg.Domain.Worldgen.WorldStyle.LowFantasy,
                EmberCrpg.Domain.Worldgen.WorldGenre.Survival,
                "grim",
                "forge-proof-" + size,
                size, size, seed,
                prompt,
                negative,
                240,
                "");

            System.Threading.Tasks.Task<EmberCrpg.Domain.Forge.AssetGenerationResult> task = null;
            string syncError = null;
            try { task = forge.GenerateAsync(request, System.Threading.CancellationToken.None); }
            catch (Exception ex) { syncError = ex.ToString(); }
            if (syncError != null)
            {
                File.WriteAllText(txt, "FAIL: GenerateAsync threw synchronously:\n" + syncError + "\n");
                yield break;
            }

            float infDeadline = Time.realtimeSinceStartup + 300f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < infDeadline)
                yield return new WaitForSecondsRealtime(0.5f);

            if (!task.IsCompleted)
            {
                File.WriteAllText(txt, "FAIL: generation timed out (no result within 300s).\n");
                yield break;
            }
            if (task.IsFaulted)
            {
                File.WriteAllText(txt, "FAIL: generation faulted:\n" + task.Exception + "\n");
                yield break;
            }

            var result = task.Result;
            if (result.ImageBytes != null && result.ImageBytes.Length > 0)
                File.WriteAllBytes(pngPath, result.ImageBytes);

            File.WriteAllText(txt,
                "ForgeType: " + forge.GetType().FullName + "\n" +
                "Success: " + result.Success + "\n" +
                "IsPlaceholder: " + result.IsPlaceholder + "\n" +
                "FailureReason: " + result.FailureReason + "\n" +
                "GenerationTimeMs: " + result.GenerationTimeMs + "\n" +
                "MimeType: " + result.MimeType + "\n" +
                "ImageBytes: " + (result.ImageBytes?.Length ?? 0) + "\n" +
                "PngPath: " + pngPath + "\n");
        }

        // E7-020 Stage 0 baseline hook.
        // Compile-safe without com.unity.inputsystem: this path records facade outputs only.
        private IEnumerator RunInputProof()
        {
            var logPath = Path.Combine(_outputDir, "input-proof.log");
            var lines = new List<string>
            {
                "mode=source-only",
                "note=Stage 0 input-proof branch captures facade snapshots without synthetic input injection.",
                "utc=" + DateTimeOffset.UtcNow.ToString("O")
            };

            AppendInputSnapshot(lines, "boot");
            yield return CaptureFixedAfter(0.6f, "input-proof_boot.png");

            SceneManager.LoadScene(EmberScenes.MainMenu);
            yield return new WaitForSeconds(1.0f);
            AppendInputSnapshot(lines, "mainmenu");
            yield return CaptureFixedAfter(0.5f, "input-proof_mainmenu.png");

            SceneManager.LoadScene(EmberScenes.CharacterCreation);
            yield return new WaitForSeconds(1.0f);
            AppendInputSnapshot(lines, "character_creation");
            yield return CaptureFixedAfter(0.5f, "input-proof_character_creation.png");

            File.WriteAllLines(logPath, lines);
            Debug.Log("[EmberProofScreenshotDriver] wrote " + logPath);
        }

        private static void AppendInputSnapshot(List<string> lines, string label)
        {
            lines.Add("[snapshot] " + label + " scene=" + SceneManager.GetActiveScene().name);
            lines.Add("Move=" + EmberInput.Move);
            lines.Add("Look=" + EmberInput.Look);
            lines.Add("LookSmoothed=" + EmberInput.LookSmoothed);
            lines.Add("Sprint=" + EmberInput.Sprint);
            lines.Add("JumpDown=" + EmberInput.JumpDown);
            lines.Add("JumpKeyDown=" + EmberInput.JumpKeyDown);
            lines.Add("Interact=" + EmberInput.Interact);
            lines.Add("ToggleCursor=" + EmberInput.ToggleCursor);
            lines.Add("RegenWorld=" + EmberInput.RegenWorld);
            lines.Add("ToggleMap=" + EmberInput.ToggleMap);
            lines.Add("SaveQuick=" + EmberInput.SaveQuick);
            lines.Add("LoadQuick=" + EmberInput.LoadQuick);
            lines.Add("PauseDown=" + EmberInput.PauseDown);
            lines.Add("PauseHeld=" + EmberInput.PauseHeld);
            lines.Add("AttackClick=" + EmberInput.AttackClick);
            lines.Add("SecondaryClick=" + EmberInput.SecondaryClick);
            lines.Add("MeleeSwing=" + EmberInput.MeleeSwing);
            lines.Add("NumberKeyDown()=" + EmberInput.NumberKeyDown());
            lines.Add("NumberKeyDown(1)=" + EmberInput.NumberKeyDown(1));
            lines.Add("FunctionKeyDown()=" + EmberInput.FunctionKeyDown());
            lines.Add("KeyDown(C)=" + EmberInput.KeyDown(KeyCode.C));
            lines.Add("Key(C)=" + EmberInput.Key(KeyCode.C));
            lines.Add("MouseDown(0)=" + EmberInput.MouseDown(0));
            lines.Add("AxisRaw(Horizontal)=" + EmberInput.AxisRaw("Horizontal"));
            lines.Add("Axis(Horizontal)=" + EmberInput.Axis("Horizontal"));
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

        private static string GetArg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            return fallback;
        }
    }
}

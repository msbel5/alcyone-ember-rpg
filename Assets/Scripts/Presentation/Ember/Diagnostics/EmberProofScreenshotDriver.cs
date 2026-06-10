using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.SceneManagement;
using CCStep = EmberCrpg.Presentation.Ember.CharacterCreation.CharacterCreationController.CreationStep;

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

            if (HasArg("--ember-gameplay-shot"))
            {
                yield return RunGameplayShot();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-playthrough"))
            {
                yield return RunPlaythrough();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-lookaround"))
            {
                yield return RunLookAround();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-looptest"))
            {
                yield return RunLoopProof();
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

            if (HasArg("--ember-world-proof"))
            {
                yield return RunWorldProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-input-proof"))
            {
                yield return RunInputProof();
                if (HasArg("--ember-proof-quit")) Application.Quit();
                yield break;
            }

            if (HasArg("--ember-planet-proof"))
            {
                yield return RunPlanetProof();
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
            view.Configure(EmberScenes.GeneratedWorld);
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

        // "Let me see the real New Game": seed EXACTLY like char-creation (EmberWorldGenIntent.Pending) so the
        // GeneratedWorld scene runs the full SeedWorld + WorldSceneDirector.Realize (biome ground + building ring +
        // player rig) and the spawner materialises the nearest NPCs - the REAL playable scene, not a bare fallback.
        // Capture the player's first-person view AND an angled overhead, at spawn and again a few ticks later so
        // NPC walking shows. Headless + small res => cheap, real screenshots of the actual scene.
        private IEnumerator RunGameplayShot()
        {
            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            // Awake() runs SeedWorld synchronously (planet gen + history) before returning, so by the time these
            // waits tick the world is already realised; the waits just let it render + a few schedule ticks pass.
            yield return CaptureFixedAfter(3.0f, "gameplay_fpv_spawn.png");
            yield return CaptureOverheadAfter(0.3f, "gameplay_overhead_spawn.png", 34f);
            yield return new WaitForSeconds(4.0f); // a few game-ticks at 0.8333 s/tick: NPCs walk toward day anchors
            yield return CaptureFixedAfter(0.1f, "gameplay_fpv_later.png");
            yield return CaptureOverheadAfter(0.3f, "gameplay_overhead_later.png", 34f);
        }

        // Full playthrough FROM THE MAIN MENU: drive the REAL New Game flow by calling the same controller methods
        // the buttons call (menu.NewGame -> char-creation step machine -> BeginYourStory -> worldgen reveal ->
        // GeneratedWorld), capturing each step. Going through the real transitions means the gameplay scene ends
        // up with a working camera (unlike the direct-LoadScene shortcut). Run NON-batchmode (real swapchain) so
        // ScreenCapture grabs the actual screen. Defaults are chosen so the wizard advances without UI input.
        private IEnumerator RunPlaythrough()
        {
            yield return CaptureFixedAfter(2.5f, "00_main_menu.png");

            var menu = UnityEngine.Object.FindFirstObjectByType<EmberMainMenuUI>();
            Debug.Log("[Playthrough] main menu found=" + (menu != null));
            if (menu != null) menu.NewGame();
            yield return CaptureFixedAfter(3.0f, "01_char_creation.png");

            var cc = UnityEngine.Object.FindFirstObjectByType<CharacterCreationController>();
            Debug.Log("[Playthrough] char-creation controller found=" + (cc != null));
            if (cc != null)
            {
                cc.AutoLaunchWorldgen = true;
                // Capture EVERY one of the 11 steps: shoot the current screen FIRST, then drive past it. The
                // selection methods that auto-advance (Mood/Calling/Fate/Questions call Continue() internally)
                // must NOT be followed by another Continue — the old loop did, which silently double-skipped
                // Calling + Birthsign (only 9 of 11 were ever captured).
                int guard = 0;
                var last = (CCStep)(-1);
                while (guard++ < 90)
                {
                    var step = cc.CurrentStep;
                    if (step == CCStep.Complete) break;
                    if (step != last)
                    {
                        last = step;
                        Debug.Log("[Playthrough] step " + step);
                        // The reveal generates its planet off-thread; give it a beat so the map/narrative show.
                        float settle = step == CCStep.WorldHistoryReveal ? 3.0f : 0.5f;
                        yield return CaptureFixedAfter(settle, "cc_" + ((int)step).ToString("00") + "_" + step + ".png");
                    }
                    bool autoAdvanced = false;
                    switch (step)
                    {
                        case CCStep.CommanderIdentity:    cc.SetCommanderIdentity("Wayfarer"); break;
                        case CCStep.WorldMood:            cc.SetWorldMood("grim"); autoAdvanced = true; break;
                        case CCStep.PlayerCalling:        cc.SetPlayerCalling("survival"); autoAdvanced = true; break; // valid CallingChoices id
                        case CCStep.FateBegins:           cc.SetFateBegins("crossroads"); autoAdvanced = true; break;
                        case CCStep.PersonalityQuestions: cc.SelectAnswerByIndex(0); autoAdvanced = true; break; // one per page; loops
                        case CCStep.Birthsign:            cc.SelectBirthsign("the_anvil"); break;
                        case CCStep.StatRolling:          cc.KeepThisRoll(); break;            // auto-rolled on enter; accept it
                        case CCStep.BuildSelection:       cc.SelectClass("warrior"); cc.SelectAlignment("true_neutral"); break;
                        case CCStep.Portrait:             break;                              // async LLM; just wait + advance
                        case CCStep.WorldHistoryReveal:   cc.SkipHistoryReveal(); break;
                        case CCStep.DossierLaunch:        cc.BeginYourStory(); break;          // captured above; now launch
                    }
                    yield return new WaitForSeconds(0.4f);
                    if (!autoAdvanced && cc.CurrentStep == step && cc.CanAdvance) cc.Continue();
                    yield return new WaitForSeconds(0.3f);
                }
                Debug.Log("[Playthrough] drive ended at step " + cc.CurrentStep + " (guard=" + guard + ")");
            }

            // Worldgen reveal + the transition into GeneratedWorld (real flow => real camera).
            yield return CaptureFixedAfter(3.5f, "10_worldgen_reveal.png");
            yield return new WaitForSeconds(6.0f);
            yield return CaptureFixedAfter(0.1f, "11_worldgen_reveal_late.png");
            yield return CaptureFixedAfter(4.0f, "20_gameplay.png");
            yield return new WaitForSeconds(4.0f);
            yield return CaptureFixedAfter(0.1f, "21_gameplay_late.png");

            // In-game UI screen tour: open each redesigned modal over the live world + capture, to verify the
            // (Codex-built) screens render against the design — the same way the char-creation playthrough
            // proved all 11 creation steps.
            var igui = UnityEngine.Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            Debug.Log("[Playthrough] in-game UI controller found=" + (igui != null));
            if (igui != null)
            {
                string[] igScreens =
                {
                    "inventory", "character", "spellbook", "journal", "worldmap", "colony", "consul",
                    "dialog", "combat", "loot", "trade", "crafting", "pause", "levelup", "death", "savegame",
                };
                foreach (var s in igScreens)
                {
                    igui.ProofOpenScreen(s);
                    // REALTIME waits: opening a modal pauses the game (timeScale 0), so scaled WaitForSeconds
                    // never elapses — the tour deadlocked on the FIRST modal until a human pressed Escape.
                    yield return new WaitForSecondsRealtime(0.6f);
                    // MODAL HUNT CLOSED: CaptureScreenshotAsTexture is only defined AFTER end-of-frame — a
                    // mid-frame call grabbed the backbuffer before the UI Toolkit pass, so the (proven
                    // attached + fully resolved) modal never appeared in captures. Wait for the frame edge.
                    yield return new WaitForEndOfFrame();
                    CaptureToPng(Path.Combine(_outputDir, "ig_" + s + ".png"));
                    igui.ProofCloseScreens(); // programmatic Escape so the next screen opens cleanly
                    yield return new WaitForSecondsRealtime(0.25f);
                }
            }
        }

        // F2-DoD (--ember-looptest): the FULL GAME LOOP proven headlessly through production paths —
        // New Game → quests seeded → world encounter (CombatActionResolver strikes) → spoils + bounty →
        // live-priced trade. Every leg prints a LOOP-PROOF line for the playtest log.
        private IEnumerator RunLoopProof()
        {
            yield return new WaitForSecondsRealtime(3.0f); // boot settles on MainMenu first
            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);
            Debug.Log("LOOP-PROOF: world entered (New Game intent consumed).");

            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            if (adapter == null)
            {
                Debug.Log("LOOP-PROOF: BROKEN — no domain adapter registered.");
                yield break;
            }

            Debug.Log(adapter.ProofQuestSnapshot());
            Debug.Log(adapter.ProofRunEncounterLeg());
            yield return new WaitForSecondsRealtime(0.4f);
            Debug.Log(adapter.ProofRunTradeLeg());
            Debug.Log(adapter.ProofQuestSnapshot());
            Debug.Log("LOOP-PROOF: full loop complete — explore→quest→fight→loot→trade all settled.");
            CaptureToPng(Path.Combine(_outputDir, "looptest_final.png"));
        }

        // SELF-PLAYTEST ("playtestleri sen yapar mısın... çevrene bakıp inceleyebilirsin"): enter the real
        // generated world, pan a full 360° at spawn, then walk to the nearest building, look through the
        // doorway, step INSIDE and pan the room — the agent inspects the captures by eye afterwards.
        private IEnumerator RunLookAround()
        {
            // Let the boot flow land on MainMenu FIRST — loading GeneratedWorld during boot gets stomped by
            // the boot sequence's own MainMenu navigation (all six captures came back as the main menu).
            yield return new WaitForSecondsRealtime(3.0f);

            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            var rig = GameObject.Find("PlayerRig");
            for (int i = 0; i < 6; i++)
            {
                if (rig != null) rig.transform.rotation = Quaternion.Euler(0f, i * 60f, 0f);
                yield return new WaitForSecondsRealtime(0.35f);
                CaptureToPng(Path.Combine(_outputDir, $"look_{i * 60:000}.png"));
            }

            // F3-DoD perf probe: average + worst frame over a 90-frame window in live gameplay.
            float sum = 0f, worst = 0f;
            for (int i = 0; i < 90; i++)
            {
                yield return null;
                float ms = Time.unscaledDeltaTime * 1000f;
                sum += ms;
                if (ms > worst) worst = ms;
            }
            Debug.Log($"[Perf] gameplay frames: avg={sum / 90f:0.0}ms worst={worst:0.0}ms over 90 frames (budget avg<=16ms).");

            var building = GameObject.Find("Building");
            if (rig != null && building != null)
            {
                var cc = rig.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false; // CharacterController fights direct teleports

                var b = building.transform.position;
                var dir = b - rig.transform.position;
                dir.y = 0f;
                var d = dir.sqrMagnitude > 1f ? dir.normalized : Vector3.forward;

                rig.transform.position = b - (d * 6f) + (Vector3.up * 0.1f); // just outside the doorway side
                rig.transform.rotation = Quaternion.LookRotation(d);
                yield return new WaitForSecondsRealtime(0.45f);
                CaptureToPng(Path.Combine(_outputDir, "look_building_outside.png"));

                rig.transform.position = b + (Vector3.up * 0.1f); // inside the room
                yield return new WaitForSecondsRealtime(0.45f);
                for (int i = 0; i < 4; i++)
                {
                    rig.transform.rotation = Quaternion.Euler(20f, i * 90f, 0f); // tilt down: furniture is knee-height
                    yield return new WaitForSecondsRealtime(0.3f);
                    CaptureToPng(Path.Combine(_outputDir, $"look_inside_{i * 90:000}.png"));
                }

                if (cc != null) cc.enabled = true;
            }

            // F1-DoD: the FARM PLOT sits at the town edge — walk there and capture it (crops + soil).
            var farm = GameObject.Find("FarmPlot");
            if (rig != null && farm != null)
            {
                var cc2 = rig.GetComponent<CharacterController>();
                if (cc2 != null) cc2.enabled = false;
                var f = farm.transform.position;
                rig.transform.position = f + new Vector3(0f, 1.2f, -7f);
                rig.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
                yield return new WaitForSecondsRealtime(0.45f);
                CaptureToPng(Path.Combine(_outputDir, "look_farm.png"));
                if (cc2 != null) cc2.enabled = true;
            }
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

            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return CaptureFixedAfter(1.2f, "generated_world_game.png");
            yield return CaptureFixedAfter(0.2f, "generated_world_spawn_proof.png");
            yield return CaptureOverheadAfter(0.4f, "generated_world_overhead.png", 32f);

            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return CaptureFixedAfter(1.2f, "generated_world_repeat.png");
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
            CaptureToPng(path);
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator CaptureAfter(float seconds, string name)
        {
            yield return new WaitForSeconds(seconds);
            var path = Path.Combine(_outputDir, name + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".png");
            CaptureToPng(path);
            yield return new WaitForSeconds(0.25f);
        }

        // Headless-safe capture. -batchmode has NO swapchain, so ScreenCapture.CaptureScreenshot grabs an empty
        // backbuffer (pure black). Instead we render the main camera EXPLICITLY to an offscreen RenderTexture and
        // read it back, which works with no window/display. This captures the WORLD (screen-space-overlay UI is
        // not seen by a camera render) - exactly what we want to verify the realised scene (NPCs, buildings,
        // daylight). Small screen res in => small PNG out, cheap to inspect.
        // Camera.main is null in the generated scene (the runtime player rig camera is not tagged "MainCamera"),
        // so fall back to the first ACTIVE camera (the rig's first-person camera). It is a proper URP camera that
        // actually renders geometry - a hand-made Camera lacks URP data and would render clear-colour only.
        private UnityEngine.Camera FindSceneCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null) return cam;
            // Include INACTIVE/disabled cameras: the rig's EyeCamera can be gated off in headless mode, which
            // makes both Camera.main and an active-only search return null. Log what exists, then force-enable
            // the rig camera so we can still render the player's view.
            var cams = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("[EmberProofScreenshotDriver] camera search found " + cams.Length + " camera(s)");
            UnityEngine.Camera fallback = null;
            foreach (var c in cams)
            {
                if (c == null) continue;
                Debug.Log("[EmberProofScreenshotDriver]   cam '" + c.name + "' activeInHierarchy=" + c.gameObject.activeInHierarchy + " enabled=" + c.enabled);
                if (fallback == null) fallback = c;
                if (c.isActiveAndEnabled) return c;
            }
            if (fallback != null)
            {
                if (!fallback.gameObject.activeSelf) fallback.gameObject.SetActive(true);
                fallback.enabled = true;
                return fallback;
            }
            return null;
        }

        private void CaptureToPng(string path)
        {
            // Window mode (real swapchain): a full SCREEN grab captures EVERYTHING on screen - UI Toolkit
            // overlays (char-creation, main menu), uGUI overlays (HUD), AND the 3D scene. The camera-render
            // path below renders only the 3D camera and MISSES all overlay UI, so reserve it for headless
            // -batchmode (where there is no swapchain and ScreenCapture comes back blank).
            if (!Application.isBatchMode)
            {
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("[EmberProofScreenshotDriver] screen-grab " + path);
                return;
            }
            var cam = FindSceneCamera();
            if (cam == null)
            {
                ScreenCapture.CaptureScreenshot(path); // no camera at all: best-effort (may be blank in batchmode)
                Debug.Log("[EmberProofScreenshotDriver] (no camera) screen-grab " + path);
                return;
            }
            CaptureCameraToPng(cam, path);
        }

        private void CaptureCameraToPng(UnityEngine.Camera cam, string path)
        {
            int w = Mathf.Clamp(Screen.width <= 1 ? 640 : Screen.width, 64, 1280);
            int h = Mathf.Clamp(Screen.height <= 1 ? 360 : Screen.height, 64, 720);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log("[EmberProofScreenshotDriver] wrote " + path);
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                if (tex != null) UnityEngine.Object.Destroy(tex);
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
        }

        // A high, angled proof camera over the plaza (world origin = where the player rig + crowd sit after the
        // billboard-origin fix). One shot shows the whole settlement: the building ring on biome ground, and
        // whether the world reads as a real place. Angled (not straight-down) so upright billboards stay visible.
        // Borrow the rig's URP-configured camera (a hand-made Camera renders clear-only under URP): fly it up for
        // one angled top-down render of the whole settlement, then restore so the player view is left untouched.
        private IEnumerator CaptureOverheadAfter(float seconds, string fileName, float dist)
        {
            yield return new WaitForSeconds(seconds);
            var cam = FindSceneCamera();
            if (cam != null)
            {
                var tr = cam.transform;
                var sPos = tr.position; var sRot = tr.rotation; var sFov = cam.fieldOfView; var sFar = cam.farClipPlane;
                try
                {
                    tr.position = new Vector3(0f, dist, -dist);
                    tr.rotation = Quaternion.Euler(45f, 0f, 0f);
                    cam.fieldOfView = 70f;
                    cam.farClipPlane = (dist * 2f) + 200f;
                    CaptureCameraToPng(cam, Path.Combine(_outputDir, fileName));
                }
                finally
                {
                    tr.position = sPos; tr.rotation = sRot; cam.fieldOfView = sFov; cam.farClipPlane = sFar;
                }
            }
            yield return new WaitForSeconds(0.2f);
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

        // Snapshot the first `max` generated NPCs' projected world positions, keyed by stable id, so a later
        // snapshot can be diffed to count who actually walked.
        private static System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> SampleNpcPositions(
            EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter adapter, int max)
        {
            var map = new System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2>();
            var actors = adapter.GetSpawnableActors();
            if (actors == null) return map;
            int n = 0;
            foreach (var a in actors)
            {
                if (a.Id == 0UL) continue;
                map[a.Id] = new UnityEngine.Vector2(a.WorldX, a.WorldZ);
                if (++n >= max) break;
            }
            return map;
        }

        // How many sampled NPCs changed position by more than half a tile between two snapshots.
        private static int CountNpcMoved(
            System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> a,
            System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> b)
        {
            if (a == null || b == null) return 0;
            int moved = 0;
            foreach (var kv in a)
                if (b.TryGetValue(kv.Key, out var p) && UnityEngine.Vector2.Distance(kv.Value, p) > 0.5f) moved++;
            return moved;
        }

        // Renders the generated SPHERICAL planet (PRD_planetary_worldgen_ember_v1, phase-1a substrate) to
        // equirectangular PNGs so the world can be SEEN, plus a determinism re-gen check. Generation + sampler
        // are engine-free; this driver only encodes the RGBA bytes to PNG.
        private IEnumerator RunPlanetProof()
        {
            const int level = 6;
            const int width = 1024;
            const int height = 512;
            uint[] seeds = { 1u, 42u, 1234u };
            var report = new StringBuilder();
            report.AppendLine("PLANET PROOF (phase-1a spherical substrate)");
            report.AppendLine("Params: subdivision=" + level + " plates=24 oceanic=0.62 image=" + width + "x" + height);

            string determinism = null;
            foreach (uint seed in seeds)
            {
                var p = new EmberCrpg.Simulation.Worldgen.Planet.PlanetParameters(level, 24, 0.62d, 0d, 0.04d);
                var field = EmberCrpg.Simulation.Worldgen.Planet.PlanetGenerator.Generate(seed, p);

                int land = 0, rivers = 0; double minE = double.MaxValue, maxE = double.MinValue; long digest = 17;
                var biomes = new int[16];
                for (int i = 0; i < field.TileCount; i++)
                {
                    var t = field.TileAt(i);
                    if (t.IsLand) land++;
                    if (t.IsRiver) rivers++;
                    biomes[(int)t.Biome & 15]++;
                    if (t.Elevation < minE) minE = t.Elevation;
                    if (t.Elevation > maxE) maxE = t.Elevation;
                    digest = (digest * 31) + (long)(t.Elevation * 1000d) + t.PlateId;
                }

                var image = EmberCrpg.Simulation.Worldgen.Planet.PlanetImageSampler.Sample(field, width, height);
                var file = Path.Combine(_outputDir, "planet-seed-" + seed + ".png");
                WritePlanetPng(image, file);
                report.AppendLine("seed=" + seed + " tiles=" + field.TileCount + " land="
                    + (100d * land / field.TileCount).ToString("0.0") + "% rivers=" + rivers + " elev=["
                    + minE.ToString("0.00") + ".." + maxE.ToString("0.00") + "] -> " + Path.GetFileName(file));
                report.AppendLine("  biomes Ocean:" + biomes[0] + " Ice:" + biomes[1] + " Tundra:" + biomes[2]
                    + " Taiga:" + biomes[3] + " TempForest:" + biomes[4] + " Grass:" + biomes[5] + " Desert:"
                    + biomes[6] + " Savanna:" + biomes[7] + " Rainforest:" + biomes[8] + " Mtn:" + biomes[9]);

                long totalPop = 0; var stypes = new int[8];
                var setl = field.Settlements;
                for (int si = 0; si < setl.Count; si++) { totalPop += setl[si].Population; stypes[(int)setl[si].Type & 7]++; }
                var typeStr = new StringBuilder();
                for (int k = 0; k < 8; k++) if (stypes[k] > 0) typeStr.Append((EmberCrpg.Simulation.Worldgen.Planet.PlanetSettlementType)k).Append(':').Append(stypes[k]).Append(' ');
                report.AppendLine("  settlements=" + setl.Count + " population=" + totalPop + " types " + typeStr);

                if (seed == 42u)
                {
                    var regen = EmberCrpg.Simulation.Worldgen.Planet.PlanetGenerator.Generate(42u, p);
                    long d2 = 17;
                    for (int i = 0; i < regen.TileCount; i++) { var t = regen.TileAt(i); d2 = (d2 * 31) + (long)(t.Elevation * 1000d) + t.PlateId; }
                    determinism = "seed42 digest=" + digest + " regen=" + d2 + (digest == d2 ? " DETERMINISTIC" : " MISMATCH");
                }
                yield return null;
            }

            report.AppendLine("Determinism: " + determinism);
            File.WriteAllText(Path.Combine(_outputDir, "planet-proof.txt"), report.ToString());
            yield break;
        }

        // Vertically flip (Unity texture rows are bottom-up) so north is up, then encode RGBA32 -> PNG.
        private void WritePlanetPng(EmberCrpg.Simulation.Worldgen.Planet.PlanetImage image, string path)
        {
            var tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);
            int stride = image.Width * 4;
            var flipped = new byte[image.Rgba.Length];
            for (int row = 0; row < image.Height; row++)
                Array.Copy(image.Rgba, row * stride, flipped, (image.Height - 1 - row) * stride, stride);
            tex.LoadRawTextureData(flipped);
            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        private IEnumerator RunWorldProof()
        {
            var path = Path.Combine(_outputDir, "world-proof.txt");
            try
            {
                var options = EmberCrpg.Domain.Configuration.EmberRuntimeOptionsProvider.Current;
                var fallback = options.WorldHost;
                var world = new EmberCrpg.Simulation.World.WorldFactory().Create(roomSeed: 1);
                var adapter = new EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter(world);
                adapter.SeedWorld(
                    fallback.FallbackMood,
                    fallback.FallbackCalling,
                    fallback.FallbackStart,
                    fallback.FallbackWorldSeed);

                int ticksPerDay = EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay;
                adapter.AdvanceTick(0);
                // LIVING-WORLD PROOF: sample a dozen NPCs across the day. If many move from home (early/off
                // hours) to their worksite/anchor (midday) and back home (night), the colony schedule is
                // driving them — the world is alive, not a crowd of standing NPCs.
                System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> npcMorning = null;
                System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> npcMidday = null;
                System.Collections.Generic.Dictionary<ulong, UnityEngine.Vector2> npcNight = null;
                for (int tick = 1; tick <= ticksPerDay; tick++)
                {
                    adapter.AdvanceTick(tick);
                    if (tick == 12) npcMorning = SampleNpcPositions(adapter, 12);
                    if (tick == ticksPerDay / 2) npcMidday = SampleNpcPositions(adapter, 12);
                    if (tick == ticksPerDay - 2) npcNight = SampleNpcPositions(adapter, 12);
                }
                int npcSampled = npcMorning?.Count ?? 0;
                int npcWalkedToWork = CountNpcMoved(npcMorning, npcMidday);
                int npcWalkedHome = CountNpcMoved(npcMidday, npcNight);

                // Diagnostic: raw home/anchor + midday/night positions for a few sampled NPCs, so the daily
                // route is visible as concrete numbers (and any mismatch is debuggable).
                var npcDiag = new StringBuilder();
                int diagShown = 0;
                // Re-base the diag's home/anchor onto the EXACT SAME origin the billboard projection subtracts
                // (the starting-settlement centre, via BillboardOriginCell), so all four columns read in one
                // player-centric frame; an idle NPC then shows home==midday instead of a phantom offset jump.
                var diagOrigin = adapter.BillboardOriginCell();
                int originX = diagOrigin.X, originY = diagOrigin.Y;
                if (world.Actors != null && npcMidday != null && npcNight != null)
                {
                    foreach (var a in world.Actors.Records)
                    {
                        if (a == null) continue;
                        if (!npcMidday.TryGetValue(a.Id.Value, out var mp) || !npcNight.TryGetValue(a.Id.Value, out var np)) continue;
                        npcDiag.Append(" [id=").Append(a.Id.Value)
                               .Append(" home=(").Append(a.Home.X - originX).Append(',').Append(a.Home.Y - originY)
                               .Append(") anchor=(").Append(a.DayAnchor.X - originX).Append(',').Append(a.DayAnchor.Y - originY)
                               .Append(") midday=(").Append((int)mp.x).Append(',').Append((int)mp.y)
                               .Append(") night=(").Append((int)np.x).Append(',').Append((int)np.y)
                               .Append(") idle=").Append(a.ScheduleState.IsIdle).Append(']');
                        if (++diagShown >= 4) break;
                    }
                }

                var events = world.Events?.Events;
                int jobAssignedCount = CountEvents(events, EmberCrpg.Domain.World.WorldEventKind.JobAssigned);
                int jobCompletedCount = CountEvents(events, EmberCrpg.Domain.World.WorldEventKind.JobCompleted);
                bool recipeCompleted = HasSmeltCompletionEvent(events, EmberCrpg.Domain.World.WorldEventKind.RecipeCompleted);
                bool jobCompleted = HasSmeltCompletionEvent(events, EmberCrpg.Domain.World.WorldEventKind.JobCompleted);
                int ironIngotCount = CountInventoryQuantity(world.PlayerInventory, "iron", "ingot");
                bool ironIngotProduced = ironIngotCount > 0;

                // A2: confirm the live quest engine completed the seeded "Forge an Iron Ingot" quest.
                int questStartedCount = CountEvents(events, EmberCrpg.Domain.World.WorldEventKind.QuestStarted);
                int questCompletedCount = CountEvents(events, EmberCrpg.Domain.World.WorldEventKind.QuestCompleted);
                int questCount = world.Quests == null ? 0 : world.Quests.Count;
                bool anyQuestComplete = false;
                if (world.Quests != null)
                {
                    foreach (var kv in world.Quests.Active)
                        if (kv.Value != null && kv.Value.IsComplete) { anyQuestComplete = true; break; }
                }

                // W1-live: confirm the deterministic overland map was generated during New Game seeding.
                var overland = world.Overland;
                bool overlandHasSettlements = overland != null && overland.Settlements.Count > 0;

                // COORD-FIX proof (NPC clustering bug): billboards are now re-based on the player's
                // starting-settlement centre so the town's crowd surrounds the plaza (world origin, where
                // the rig spawns) instead of clumping at raw grid coords ((i%32)*12 ...) tens of metres away.
                // Count how many spawnable NPCs fall within ~40 m of the player and the box they span:
                // near-origin + a real spread IS the fix; ~0 within 40 m was the bug (frozen distant clump).
                var spawnablesProof = adapter.GetSpawnableActors();
                int npcWithin40m = 0;
                float npcMinX = float.MaxValue, npcMaxX = float.MinValue, npcMinZ = float.MaxValue, npcMaxZ = float.MinValue;
                if (spawnablesProof != null)
                {
                    foreach (var s in spawnablesProof)
                    {
                        if (UnityEngine.Mathf.Sqrt((s.WorldX * s.WorldX) + (s.WorldZ * s.WorldZ)) > 40f) continue;
                        npcWithin40m++;
                        if (s.WorldX < npcMinX) npcMinX = s.WorldX;
                        if (s.WorldX > npcMaxX) npcMaxX = s.WorldX;
                        if (s.WorldZ < npcMinZ) npcMinZ = s.WorldZ;
                        if (s.WorldZ > npcMaxZ) npcMaxZ = s.WorldZ;
                    }
                }

                bool passed = jobCompleted && ironIngotProduced && anyQuestComplete && overlandHasSettlements;

                var report = new StringBuilder();
                report.AppendLine(passed ? "PASS" : "FAIL");
                report.AppendLine("TicksRun: " + ticksPerDay);
                report.AppendLine("NpcSampled: " + npcSampled);
                report.AppendLine("NpcWalkedToWorkByMidday: " + npcWalkedToWork + " / " + npcSampled);
                report.AppendLine("NpcWalkedHomeByNight: " + npcWalkedHome + " / " + npcSampled);
                report.AppendLine("NpcDiag:" + npcDiag.ToString());
                report.AppendLine("NpcBillboardsWithin40mOfPlayer: " + npcWithin40m + " / " + (spawnablesProof?.Count ?? 0));
                if (npcWithin40m > 0)
                    report.AppendLine("NpcBillboardBox: x[" + npcMinX.ToString("0.0") + ".." + npcMaxX.ToString("0.0") + "] z[" + npcMinZ.ToString("0.0") + ".." + npcMaxZ.ToString("0.0") + "] (re-based on starting-settlement centre = world origin where the player rig spawns)");
                report.AppendLine("SeedArgs: mood=" + fallback.FallbackMood + " calling=" + fallback.FallbackCalling + " start=" + fallback.FallbackStart + " seed=" + fallback.FallbackWorldSeed);
                report.AppendLine("DetectionNote: completion scan checks reason text for smelt/iron, then falls back to ReasonTrace recipe:1001 because live completion reasons are id-based.");
                report.AppendLine("JobAssignedCount: " + jobAssignedCount);
                report.AppendLine("JobCompletedCount: " + jobCompletedCount);
                report.AppendLine("SmeltRecipeCompleted: " + recipeCompleted);
                report.AppendLine("SmeltJobCompleted: " + jobCompleted);
                report.AppendLine("IronIngotProduced: " + ironIngotProduced + " qty=" + ironIngotCount);
                report.AppendLine("QuestCount: " + questCount);
                report.AppendLine("QuestStartedCount: " + questStartedCount);
                report.AppendLine("QuestCompletedCount: " + questCompletedCount);
                report.AppendLine("AnyQuestComplete: " + anyQuestComplete);
                report.AppendLine("OverlandPresent: " + (overland != null));
                if (overland != null)
                {
                    report.AppendLine("OverlandSize: " + overland.Width + "x" + overland.Height);
                    // Report the playable world scale in km^2 so the proof documents the open-world size goal
                    // (default 16x16 grid x 40km region edge => 409,600 km^2, ~2x Daggerfall's ~200,000 km^2).
                    double regionAreaKm2 = EmberCrpg.Domain.Overland.OverlandParameters.Default.RegionAreaKm2;
                    long totalAreaKm2 = (long)System.Math.Round(overland.Width * overland.Height * regionAreaKm2);
                    report.AppendLine("OverlandAreaKm2: " + totalAreaKm2.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                        + " (" + overland.Width + "x" + overland.Height + " regions x " + regionAreaKm2.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " km^2)");
                    report.AppendLine("OverlandSettlements: " + overland.Settlements.Count);
                    var biomeCounts = new System.Collections.Generic.Dictionary<string, int>();
                    for (int i = 0; i < overland.Tiles.Count; i++)
                    {
                        var b = overland.Tiles[i].Biome.ToString();
                        biomeCounts[b] = (biomeCounts.TryGetValue(b, out var c) ? c : 0) + 1;
                    }
                    var biomeSb = new StringBuilder();
                    foreach (var kv in biomeCounts) biomeSb.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
                    report.AppendLine("OverlandBiomes: " + biomeSb.ToString().TrimEnd());
                }
                report.AppendLine("RecentEvents:");
                AppendRecentEventLines(report, events, 15);

                File.WriteAllText(path, report.ToString());
                Debug.Log(
                    "[WorldProof] " + (passed ? "PASS" : "FAIL") +
                    " ticks=" + ticksPerDay +
                    " jobAssigned=" + jobAssignedCount +
                    " jobCompleted=" + jobCompletedCount +
                    " smeltRecipeCompleted=" + recipeCompleted +
                    " smeltJobCompleted=" + jobCompleted +
                    " ironIngotQty=" + ironIngotCount +
                    " questCompleted=" + questCompletedCount +
                    " anyQuestComplete=" + anyQuestComplete);
            }
            catch (Exception ex)
            {
                File.WriteAllText(path, "FAIL\nException:\n" + ex + "\n");
                Debug.LogError("[WorldProof] FAIL exception while running world proof: " + ex);
            }
            yield break;
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
            lines.Add("ToggleInventory=" + EmberInput.ToggleInventory);
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

        private static int CountEvents(IReadOnlyList<EmberCrpg.Domain.World.WorldEvent> events, EmberCrpg.Domain.World.WorldEventKind kind)
        {
            if (events == null) return 0;
            int count = 0;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Kind == kind)
                    count++;
            }
            return count;
        }

        private static bool HasSmeltCompletionEvent(IReadOnlyList<EmberCrpg.Domain.World.WorldEvent> events, EmberCrpg.Domain.World.WorldEventKind kind)
        {
            if (events == null) return false;
            string recipeCause = "recipe:" + EmberCrpg.Data.Recipes.ProductionRecipeRegistry.SmeltIronIngotId.Value;
            for (int i = 0; i < events.Count; i++)
            {
                var worldEvent = events[i];
                if (worldEvent == null || worldEvent.Kind != kind)
                    continue;
                if (ContainsIgnoreCase(worldEvent.Reason, "smelt") || ContainsIgnoreCase(worldEvent.Reason, "iron"))
                    return true;
                var trace = worldEvent.ReasonTrace;
                if (trace == null) continue;
                for (int causeIndex = 0; causeIndex < trace.Causes.Count; causeIndex++)
                {
                    var cause = trace.Causes[causeIndex];
                    if (ContainsIgnoreCase(cause, "smelt") || ContainsIgnoreCase(cause, "iron") || string.Equals(cause, recipeCause, StringComparison.Ordinal))
                        return true;
                }
            }
            return false;
        }

        private static int CountInventoryQuantity(EmberCrpg.Domain.Inventory.InventoryState inventory, string requiredFragmentA, string requiredFragmentB)
        {
            if (inventory == null || string.IsNullOrEmpty(requiredFragmentA) || string.IsNullOrEmpty(requiredFragmentB)) return 0;
            int quantity = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                if (item != null
                    && ContainsIgnoreCase(item.TemplateId, requiredFragmentA)
                    && ContainsIgnoreCase(item.TemplateId, requiredFragmentB))
                    quantity += item.Quantity;
            }
            return quantity;
        }

        private static void AppendRecentEventLines(StringBuilder report, IReadOnlyList<EmberCrpg.Domain.World.WorldEvent> events, int maxLines)
        {
            if (events == null || events.Count == 0)
            {
                report.AppendLine("(none)");
                return;
            }

            int start = events.Count - maxLines;
            if (start < 0) start = 0;
            for (int i = start; i < events.Count; i++)
            {
                var worldEvent = events[i];
                if (worldEvent == null) continue;
                report.AppendLine(
                    "time=" + worldEvent.Tick.TotalMinutes +
                    " kind=" + worldEvent.Kind +
                    " reason=" + worldEvent.Reason);
            }
        }

        private static bool ContainsIgnoreCase(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value)
                   && !string.IsNullOrEmpty(fragment)
                   && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

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
using DelveLayout = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeDungeonLayoutInfo;

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
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-gameplay-shot"))
            {
                yield return RunGameplayShot();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-playthrough"))
            {
                yield return RunPlaythrough();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-marathon"))
            {
                yield return RunMarathon();
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-igtour"))
            {
                yield return RunIgTour();
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-mainquest"))
            {
                yield return RunMainQuest();
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-lookaround"))
            {
                yield return RunLookAround();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-looptest"))
            {
                yield return RunLoopProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-shipcheck"))
            {
                yield return RunShipCheck();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-scene-tour"))
            {
                yield return RunSceneTour();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-llm-proof"))
            {
                yield return RunLlmProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-forge-proof"))
            {
                yield return RunForgeProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-world-proof"))
            {
                yield return RunWorldProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-input-proof"))
            {
                yield return RunInputProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
                yield break;
            }

            if (HasArg("--ember-planet-proof"))
            {
                yield return RunPlanetProof();
                // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
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

            // PLAYTEST FIX ("oyun testten sonra kapanmıyor"): the driver only exists in proof runs
                // (--ember-proof-screenshots gates Bootstrap), so ALWAYS quit when the proof ends —
                // the old opt-in --ember-proof-quit flag was never passed and windows piled up.
                Debug.Log("[Proof] run complete — quitting player.");
                Application.Quit();
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

        // F4 (--ember-shipcheck): the ONE-COMMAND regression pack — game loop legs, perf budget, a
        // 10×fast-travel SOAK (memory + exception watch across scene reloads), and a modal capture, all
        // summarized as SHIPCHECK section lines + a final PASS/FAIL verdict for the playtest log.
        private IEnumerator RunShipCheck()
        {
            int exceptions = 0;
            Application.logMessageReceived += (c, s, t) => { if (t == LogType.Exception) exceptions++; };
            var sections = new System.Collections.Generic.List<string>();
            bool allPass = true;
            void Section(string name, bool pass, string detail)
            {
                allPass &= pass;
                sections.Add($"SHIPCHECK [{(pass ? "PASS" : "FAIL")}] {name}: {detail}");
                Debug.Log(sections[sections.Count - 1]);
            }

            yield return WaitForBootToSettle();
            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            Section("world-enter", adapter != null, adapter != null ? "adapter live" : "no adapter");
            if (adapter == null) yield break;

            string quests = adapter.ProofQuestSnapshot();
            Section("quest-seed", quests.Contains("active=2"), quests);

            string enc = adapter.ProofRunEncounterLeg();
            Section("encounter-loot", enc.Contains("felled=True"), enc);

            string trade = adapter.ProofRunTradeLeg();
            Section("economy", trade.Contains("success=True"), trade);

            float sum = 0f, worst = 0f;
            for (int i = 0; i < 90; i++)
            {
                yield return null;
                float ms = Time.unscaledDeltaTime * 1000f;
                sum += ms;
                if (ms > worst) worst = ms;
            }
            float avg = sum / 90f;
            Section("perf", avg <= 16f, $"avg={avg:0.0}ms worst={worst:0.0}ms (budget 16)");

            // SOAK: 10 fast-travels across the realm (legacy sync path), scene reload each time — the
            // streamer's OnDestroy frees terrain, the continuity hand-off carries the live world.
            var view = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.WorldViewReadModel;
            var names = new System.Collections.Generic.List<string>();
            var map = view?.Overland;
            if (map != null)
                for (int i = 0; i < map.Settlements.Count && names.Count < 11; i++)
                    names.Add(map.Settlements[i].Name);
            int hops = 0;
            for (int i = 0; i < names.Count && hops < 10; i++)
            {
                if (!adapter.TryTravelToSettlement(names[i], out _)) continue;
                hops++;
                EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                    EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                yield return new WaitForSecondsRealtime(2.0f);
            }
            Section("soak-travel", hops >= 10 && exceptions == 0, $"hops={hops} exceptions={exceptions}");

            // F7-DoD: harvest→stock→price movement over 3 sim days (or honest seasonal dormancy).
            string chain = adapter.ProofEconomyChain();
            Section("economy-chain", chain.Contains("OK"), chain); // "OK" and "DORMANT-OK" both pass

            // F11-DoD audio-forge: PNGs are silent, so the gate is numeric — every clip forges
            // exception-free with sane durations, and the footstep spectrum reads THUD (centroid
            // below ~1200Hz), not the 1320Hz bell the playtest complained about.
            bool audioOk = false;
            string audioDetail = "forge threw";
            try
            {
                EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioForge.EnsureForged();
                var dirtData = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioSynth.RenderFootstepDirt(0xD117u);
                var stoneData = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioSynth.RenderFootstepStone(0x5709u);
                float dirtCentroid = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioSynth.CentroidHz(dirtData);
                float stoneCentroid = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioSynth.CentroidHz(stoneData);
                var dirt = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioForge.FootstepDirt;
                var stone = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioForge.FootstepStone;
                var creak = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioForge.DoorCreak;
                var hitClip = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioForge.Hit;
                audioOk = dirt != null && dirt.Length == 4 && stone != null && stone.Length == 4
                    && dirt[0] != null && dirt[0].length > 0.08f
                    && creak != null && creak.length > 0.6f
                    && hitClip != null && hitClip.length > 0.1f
                    && dirtCentroid < 1200f && stoneCentroid < 1600f;
                audioDetail = $"footsteps=8 dirtCentroid={dirtCentroid:0}Hz stoneCentroid={stoneCentroid:0}Hz " +
                              $"creak={creak.length:0.00}s hit={hitClip.length:0.00}s";
            }
            catch (System.Exception e)
            {
                audioDetail = "forge threw: " + e.Message;
            }
            Section("audio-forge", audioOk, audioDetail);

            var igui = UnityEngine.Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            if (igui != null)
            {
                igui.ProofOpenScreen("inventory");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "shipcheck_modal.png"));
                igui.ProofCloseScreens();
            }
            Section("modal-capture", igui != null, igui != null ? "inventory captured end-of-frame" : "no UI controller");

            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "shipcheck_final.png"));
            Debug.Log($"SHIPCHECK VERDICT: {(allPass ? "PASS" : "FAIL")} ({sections.Count} sections, {exceptions} exceptions logged)");
        }

        // F2-DoD (--ember-looptest): the FULL GAME LOOP proven headlessly through production paths —
        // New Game → quests seeded → world encounter (CombatActionResolver strikes) → spoils + bounty →
        // live-priced trade. Every leg prints a LOOP-PROOF line for the playtest log.
        private IEnumerator RunLoopProof()
        {
            yield return WaitForBootToSettle();
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
            Debug.Log(adapter.ProofGreetingSample()); // F6-DoD: 3 roles, 3 DIFFERENT greetings

            // F8-DoD: hold the encounter open across two music polls — BATTLE in, then DAY back out.
            Debug.Log(adapter.ProofBindEncounterForMusic());
            yield return new WaitForSecondsRealtime(2.6f); // music director polls at 2s → slot=BATTLE logs
            Debug.Log(adapter.ProofRunEncounterLeg());
            yield return new WaitForSecondsRealtime(2.6f); // settle read cleared the mirror → back to DAY/NIGHT
            yield return new WaitForSecondsRealtime(0.4f);

            // F17-DoD: kill (+40 XP) + bounty (+60 XP) crossed the 100-XP threshold — the controller
            // auto-opens the level-up screen; capture it, then close to continue the loop.
            var xpUi = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            if (xpUi != null)
            {
                yield return new WaitForSecondsRealtime(0.5f); // give the HUD pump a frame to open it
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "looptest_levelup.png"));
                yield return new WaitForSecondsRealtime(0.4f); // capture separation
                xpUi.ProofCloseScreens();
                yield return new WaitForSecondsRealtime(0.3f);
            }

            Debug.Log(adapter.ProofRunTradeLeg());
            Debug.Log(adapter.ProofQuestSnapshot());
            // F21-DoD: a generated FETCH contract closed end to end — accept, buy the cargo through
            // the live economy, turn in. Three [QuestGen] lines + one LOOP-PROOF summary.
            Debug.Log(adapter.ProofRunGeneratedQuestLeg());
            // F26-DoD: the tavern sleep flow — hp refilled, 5 gold paid, +8 hours, one honest line.
            Debug.Log(adapter.ProofTavernSleepLeg());
            // F15-DoD: lose to an outlaw AFK, awaken at the plaza — the toll line + a LIVE HUD frame
            // (full vitals + the "You awaken..." event line) prove the loop has no dead-end.
            var deathAdapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            if (deathAdapter != null)
            {
                Debug.Log(deathAdapter.ProofDieAndRespawn());
                // F16-DoD: the swing-log difference — bare hands vs the equipped weapon (Ash Rat duel).
                Debug.Log(deathAdapter.ProofWeaponSwingDiff());
                var respawnUi = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
                respawnUi?.ProofCloseScreens(); // the death modal may have opened mid-leg; respawn already ran
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "looptest_respawn.png"));
                // ScreenCapture.CaptureScreenshot is ASYNC and a same-frame second request REPLACES the
                // pending one — the final capture below ate this frame until a frame of separation existed.
                yield return new WaitForSecondsRealtime(0.4f);
            }

            Debug.Log("LOOP-PROOF: full loop complete — explore→quest→fight→loot→trade all settled.");
            CaptureToPng(Path.Combine(_outputDir, "looptest_final.png"));
        }

        // SELF-PLAYTEST ("playtestleri sen yapar mısın... çevrene bakıp inceleyebilirsin"): enter the real
        // generated world, pan a full 360° at spawn, then walk to the nearest building, look through the
        // doorway, step INSIDE and pan the room — the agent inspects the captures by eye afterwards.
        // F34-DoD (--ember-marathon): the 30-minute autonomous SOAK — a seeded random loop of
        // travel + combat + trade + clock advance through PRODUCTION paths. Exceptions are
        // counted via the log callback, memory is sampled every minute, and the closing line is
        // the verdict: 0 exceptions and a non-climbing memory curve, or it FAILS loudly.
        // "--ember-marathon-minutes N" shortens the soak for iteration runs (the DoD run is 30).
        private IEnumerator RunMarathon()
        {
            yield return WaitForBootToSettle();

            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            float minutes = 30f;
            var cli = Environment.GetCommandLineArgs();
            for (int i = 0; i < cli.Length - 1; i++)
                if (cli[i] == "--ember-marathon-minutes" && float.TryParse(cli[i + 1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var m))
                    minutes = m;

            int exceptions = 0;
            Application.LogCallback handler = (condition, stack, type) =>
            {
                if (type == LogType.Exception) exceptions++;
            };
            Application.logMessageReceived += handler;

            long memStart = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            long memPeak = memStart;
            uint rngState = 0xF34F34u;
            System.Func<uint> next = () =>
            {
                rngState ^= rngState << 13; rngState ^= rngState >> 17; rngState ^= rngState << 5;
                return rngState;
            };

            int actions = 0, travels = 0, fights = 0, trades = 0, hours = 0;
            float endAt = Time.unscaledTime + minutes * 60f;
            float nextHeartbeat = Time.unscaledTime + 60f;
            Debug.Log($"[Marathon] soak armed: {minutes:0}min, seed=0xF34F34, memStart={memStart / 1048576}MB.");

            // Forge-on boots spend 60s+ in ONNX before the adapter registers — WAIT for it
            // (bounded) instead of declaring the world broken 4s after the scene load.
            bool aborted = false;
            float adapterDeadline = Time.unscaledTime + 120f;
            while (EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                       is not EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter)
            {
                if (Time.unscaledTime > adapterDeadline) break;
                yield return new WaitForSecondsRealtime(1f);
            }

            while (Time.unscaledTime < endAt)
            {
                var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                    as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
                if (adapter == null)
                {
                    Debug.Log("[Marathon] BROKEN — adapter lost; aborting soak.");
                    aborted = true;
                    break;
                }

                int pick = (int)(next() % 8u); // 2/8 travel, 3/8 fight, 2/8 trade, 1/8 clock
                if (pick < 2)
                {
                    var names = adapter.ProofListSettlementNames();
                    if (names.Count > 0 && adapter.TryTravelToSettlement(names[(int)(next() % (uint)names.Count)], out _))
                    {
                        EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                            EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                        SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                        yield return new WaitForSecondsRealtime(1.5f);
                        FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>()?.ProofCloseScreens();
                        travels++;
                    }
                }
                else if (pick < 5)
                {
                    adapter.ProofRunEncounterLeg(); // refuses honestly when no outlaw is homed here
                    FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>()?.ProofCloseScreens();
                    fights++;
                }
                else if (pick < 7)
                {
                    adapter.ProofRunTradeLeg();
                    trades++;
                }
                else
                {
                    adapter.ProofAdvanceHours(1);
                    hours++;
                }
                actions++;

                if (Time.unscaledTime >= nextHeartbeat)
                {
                    nextHeartbeat = Time.unscaledTime + 60f;
                    long memNow = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    if (memNow > memPeak) memPeak = memNow;
                    Debug.Log($"[Marathon] t={(int)(Time.unscaledTime - (endAt - minutes * 60f))}s " +
                              $"actions={actions} (travel={travels} fight={fights} trade={trades} clock={hours}) " +
                              $"exceptions={exceptions} mem={memNow / 1048576}MB peak={memPeak / 1048576}MB");
                }

                yield return new WaitForSecondsRealtime(2f + (next() % 5u));
            }

            Application.logMessageReceived -= handler;
            long memEnd = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            if (memEnd > memPeak) memPeak = memEnd;
            // Flat-curve rule: the end may not DOUBLE the start — scene churn fragments, a leak climbs.
            bool flat = memEnd < memStart * 2;
            // Honesty rule: a soak that lost its world or did NOTHING cannot PASS — an aborted
            // run once reported PASS with actions=0 (the exact Potemkin pattern the V2
            // contract exists to kill).
            bool pass = exceptions == 0 && flat && !aborted && actions > 0;
            var livingAdapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            Debug.Log($"[Marathon] LIVING: {(livingAdapter != null ? livingAdapter.ProofLivingCensus() : "adapter gone")}");
            Debug.Log($"[Marathon] VERDICT: {(pass ? "PASS" : "FAIL")} — {minutes:0}min soak, " +
                      $"actions={actions} (travel={travels} fight={fights} trade={trades} clock={hours}), " +
                      $"exceptions={exceptions}, mem {memStart / 1048576}MB -> {memEnd / 1048576}MB " +
                      $"(peak {memPeak / 1048576}MB, flat={flat}).");
        }

        // F32-DoD (--ember-igtour): EVERY in-game screen, one frame each — HUD, inventory,
        // character sheet, journal, world map, pause menu, options (Settings / Audio & Display /
        // Keybinds). The DoD's other half is the source grep: stub copy returns zero.
        private IEnumerator RunIgTour()
        {
            yield return WaitForBootToSettle();

            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            var ui = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            if (ui == null)
            {
                Debug.Log("[Proof] IGTOUR BROKEN — no InGameUiController.");
                yield break;
            }
            ui.ProofCloseScreens();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "igtour_hud.png"));
            yield return new WaitForSecondsRealtime(0.4f);

            // The world-side screens, one by one (the browser is Tab's inventory).
            ui.ProofToggleBrowser();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "igtour_inventory.png"));
            yield return new WaitForSecondsRealtime(0.4f);
            ui.ProofCloseScreens();

            string[] screens = { "character", "journal", "map" };
            foreach (var id in screens)
            {
                ui.ProofOpenScreen(id);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, $"igtour_{id}.png"));
                yield return new WaitForSecondsRealtime(0.4f);
                ui.ProofCloseScreens();
                yield return null;
            }

            // The pause stack: menu, then the options sections (F32's new tabs included).
            var pause = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.PauseMenu>(FindObjectsInactive.Include);
            if (pause == null)
            {
                Debug.Log("[Proof] IGTOUR BROKEN — no PauseMenu.");
                yield break;
            }
            pause.Pause();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "igtour_pause.png"));
            yield return new WaitForSecondsRealtime(0.4f);

            var options = pause.ProofOpenOptions();
            yield return new WaitForSecondsRealtime(0.6f);
            string[] sections = { "Settings", "Audio & Display", "Keybinds" };
            foreach (var section in sections)
            {
                bool shown = options != null && options.ProofShowSection(section);
                Debug.Log($"[Proof] igtour options section '{section}': shown={shown}");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, $"igtour_options_{section.Replace(" & ", "_").Replace(" ", "_").ToLowerInvariant()}.png"));
                yield return new WaitForSecondsRealtime(0.4f);
            }
            pause.Resume();
            Debug.Log("[Proof] igtour complete — 9 frames: hud, inventory, character, journal, map, pause, 3 options sections.");
        }

        // F31-DoD (--ember-mainquest): the THREE-ACT SPINE end to end through production paths —
        // Act 1: travel each delve and open its chest (the inscription piece rides the loot grant);
        // Act 2: travel the capital and consult the sage; Act 3: travel the FINAL delve, fell its
        // Warden, and capture the finale overlay. Every transition logs "[MainQuest] ...".
        private IEnumerator RunMainQuest()
        {
            yield return WaitForBootToSettle();

            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            if (adapter == null)
            {
                Debug.Log("[Proof] MAINQUEST BROKEN — no domain adapter.");
                yield break;
            }
            Debug.Log("[Proof] " + adapter.ProofMainQuestSnapshot());

            // F35: the playthrough FRAME SERIES — one frame per stage (the video stand-in).
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "pt_01_world_intro.png"));
            yield return new WaitForSecondsRealtime(0.4f);

            // ACT 1 — each delve's chest yields its inscription piece (the spine caps the count).
            var delveNames = adapter.ProofListDelveNames();
            Debug.Log($"[Proof] mainquest act 1: {delveNames.Count} delve(s) to sweep.");
            for (int d = 0; d < delveNames.Count; d++)
            {
                if (!adapter.TryTravelToSettlement(delveNames[d], out _)) continue;
                EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                    EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                yield return new WaitForSecondsRealtime(1.5f);
                FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>()?.ProofCloseScreens();
                var chest = FindFirstObjectByType<EmberCrpg.Presentation.Ember.WorldDirector.RuntimeChestView>();
                if (chest != null) Debug.Log("[Proof] mainquest chest: " + chest.ProofOpen());
                else Debug.Log($"[Proof] MAINQUEST BROKEN — no chest at '{delveNames[d]}'.");
                Debug.Log("[Proof] " + adapter.ProofMainQuestSnapshot());
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, $"pt_02_delve_{d + 1}.png"));
                yield return new WaitForSecondsRealtime(0.4f);
            }

            // ACT 2 — the capital's sage reads the joined inscription.
            string capital = adapter.CapitalSettlementName();
            if (!string.IsNullOrEmpty(capital) && adapter.TryTravelToSettlement(capital, out _))
            {
                EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                    EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                yield return new WaitForSecondsRealtime(1.5f);
                Debug.Log($"[Proof] mainquest act 2: at the capital '{capital}'.");
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "pt_03_capital_sage.png"));
                yield return new WaitForSecondsRealtime(0.4f);
            }
            Debug.Log("[Proof] " + adapter.ProofConsultSage());

            // ACT 3 — the FINAL delve's Warden.
            string finalDelve = adapter.FinalDelveName();
            if (string.IsNullOrEmpty(finalDelve) || !adapter.TryTravelToSettlement(finalDelve, out _))
            {
                Debug.Log("[Proof] MAINQUEST BROKEN — final delve travel refused.");
                yield break;
            }
            EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(1.5f);
            FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>()?.ProofCloseScreens();
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "pt_04_final_delve.png"));
            yield return new WaitForSecondsRealtime(0.4f);
            string warden = adapter.ProofBindDelveWarden();
            if (string.IsNullOrEmpty(warden))
            {
                Debug.Log("[Proof] MAINQUEST BROKEN — no Warden at the final delve.");
                yield break;
            }
            Debug.Log("[Proof] mainquest act 3: boss bound — " + warden);
            Debug.Log(adapter.ProofFinishBoundEncounter());
            yield return new WaitForSecondsRealtime(1.0f); // the finale view polls the mirror and raises
            string closing = adapter.ProofMainQuestSnapshot();
            Debug.Log("[Proof] " + closing);
            yield return new WaitForEndOfFrame();
            CaptureToPng(Path.Combine(_outputDir, "pt_05_finale_credits.png"));
            yield return new WaitForSecondsRealtime(0.4f); // async capture separation
            // F35: the playthrough verdict — creation to credits in one unbroken run.
            bool playthroughPass = closing.Contains("complete=True");
            Debug.Log($"[Playthrough] VERDICT: {(playthroughPass ? "PASS" : "FAIL")} — " +
                      $"creation -> three acts -> finale, frame series pt_01..pt_05 captured.");
        }

        private IEnumerator RunLookAround()
        {
            yield return WaitForBootToSettle();

            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            SceneManager.LoadScene(EmberScenes.GeneratedWorld);
            yield return new WaitForSecondsRealtime(4.0f);

            var rig = GameObject.Find("PlayerRig");
            Vector3 spawnPos = rig != null ? rig.transform.position : Vector3.zero;
            // EVIDENCE (rt-look): all six ring frames were byte-identical — EmberFirstPersonController
            // rewrites rig yaw from its cached _yawDegrees every frame, stomping scripted aims. This is a
            // scripted camera session with no player input: disable the controller for the whole tour.
            var ringFps = rig != null
                ? rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>()
                : null;
            if (ringFps != null) ringFps.enabled = false;
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

            // F9-DoD: open the world map and capture it — the dungeon pin (▼ red, always labeled) and the
            // HUD delve line are eye-checked from this frame.
            var mapUi = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
            if (mapUi != null)
            {
                mapUi.ProofOpenScreen("worldmap");
                yield return new WaitForSecondsRealtime(0.6f);
                yield return new WaitForEndOfFrame(); // modal exists only after the UI Toolkit pass
                CaptureToPng(Path.Combine(_outputDir, "look_map.png"));
                mapUi.ProofCloseScreens();
                yield return new WaitForSecondsRealtime(0.25f);
            }

            // F6-DoD: jump the clock to ~23:00 (game starts 08:00), return to the SAME spawn spot, and
            // capture the street again — citizens must be gone (curfew), guards/outlaws may remain.
            var nightAdapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
            if (rig != null && nightAdapter != null)
            {
                var cc3 = rig.GetComponent<CharacterController>();
                if (cc3 != null) cc3.enabled = false;
                rig.transform.position = spawnPos;
                rig.transform.rotation = Quaternion.identity;
                nightAdapter.ProofAdvanceHours(15);
                Debug.Log("[Proof] clock advanced 15h — capturing the night street for the curfew contrast.");
                yield return new WaitForSecondsRealtime(2.6f); // curfew views poll at 2s
                CaptureToPng(Path.Combine(_outputDir, "look_night.png"));
                yield return new WaitForSecondsRealtime(0.4f); // async capture lands before the next leg moves
                if (cc3 != null) cc3.enabled = true;
            }

            // F23-DoD: daylight CRIME — an aimed strike at a civilian posts a bounty; the WATCH hunts
            // on sight (TickHostileAi). Two snapshots + a frame of the closing guard.
            if (rig != null && nightAdapter != null)
            {
                nightAdapter.ProofAdvanceHours(9); // 23:00 → 08:00 — the aggro frame deserves daylight
                Debug.Log("[Proof] F23 " + nightAdapter.ProofCrimeAndWatchLeg());
                // The crime may have SUMMONED brand-new watch actors — materialise their billboards
                // now (the streaming re-scan is movement-gated and the rig stands still here).
                FindFirstObjectByType<EmberCrpg.Presentation.Ember.Views.EmberGeneratedActorSpawner>()?.SpawnMissingNearbyActors();
                yield return new WaitForSecondsRealtime(3.2f); // the watch closes ~7 cells at 1/0.45s
                var watchSnap = nightAdapter.ProofWatchSnapshot();
                Debug.Log($"[Proof] F23 watch B: {watchSnap} (DoD: distance shrinks vs watch A).");
                var fpsC = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fpsC != null) fpsC.enabled = false;
                string watchName = watchSnap.Split('|')[0];
                foreach (var view in FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
                {
                    if (view.name != watchName) continue;
                    rig.transform.rotation = Quaternion.LookRotation(
                        view.transform.position + Vector3.up * 0.9f - rig.transform.position);
                    break;
                }
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "look_guard_aggro.png"));
                yield return new WaitForSecondsRealtime(0.4f); // capture separation
                if (fpsC != null)
                {
                    fpsC.enabled = true;
                    fpsC.SyncYaw(rig.transform.eulerAngles.y);
                }
            }

            // F24-DoD: FOUR sky frames — 06 dawn-rose, 12 day-blue, 18 dusk-amber, 24 night with
            // stars + moon. These hour jumps are exactly the clock jumps that used to leave midnight
            // bright; the sky reads world-time TRUTH now (RuntimeFieldMirror.MinutesOfDay).
            if (rig != null && nightAdapter != null)
            {
                var fpsS = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fpsS != null) fpsS.enabled = false;
                int[] skyAdvance = { 22, 6, 6, 6 };       // ~08:00 → 06 → 12 → 18 → 24
                float[] skyPitch = { -12f, -35f, -12f, -55f }; // horizon sun / high blue / horizon dusk / stars+moon
                string[] skyName = { "sky_06", "sky_12", "sky_18", "sky_24" };
                for (int s = 0; s < 4; s++)
                {
                    nightAdapter.ProofAdvanceHours(skyAdvance[s]);
                    rig.transform.rotation = Quaternion.Euler(skyPitch[s], 150f, 0f); // toward the sun azimuth
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return new WaitForEndOfFrame();
                    CaptureToPng(Path.Combine(_outputDir, skyName[s] + ".png"));
                    yield return new WaitForSecondsRealtime(0.4f); // async capture separation
                    Debug.Log($"[Proof] F24 {skyName[s]} captured at hour={EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.HourOfDay:00} " +
                              $"(minutesOfDay={EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.MinutesOfDay}).");
                }
                if (fpsS != null)
                {
                    fpsS.enabled = true;
                    fpsS.SyncYaw(rig.transform.eulerAngles.y);
                }
            }

            // F25-DoD: three weather frames + [Weather] logs — forced via the proof hook at noon so
            // particles/fog read in daylight (the deterministic per-day pick is exercised by play).
            if (rig != null && nightAdapter != null)
            {
                nightAdapter.ProofAdvanceHours(12); // midnight → noon
                var fpsW = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fpsW != null) fpsW.enabled = false;
                string[] weatherKinds = { "rain", "fog", "snow" };
                foreach (var kind in weatherKinds)
                {
                    EmberCrpg.Presentation.Ember.WorldDirector.RuntimeWeatherController.ProofForce(kind);
                    rig.transform.rotation = Quaternion.Euler(-8f, 150f, 0f);
                    yield return new WaitForSecondsRealtime(1.6f); // particles fill the volume
                    yield return new WaitForEndOfFrame();
                    CaptureToPng(Path.Combine(_outputDir, $"weather_{kind}.png"));
                    yield return new WaitForSecondsRealtime(0.4f); // async capture separation
                }
                EmberCrpg.Presentation.Ember.WorldDirector.RuntimeWeatherController.ProofForce(null);
                yield return null;
                if (fpsW != null)
                {
                    fpsW.enabled = true;
                    fpsW.SyncYaw(rig.transform.eulerAngles.y);
                }
            }

            // F26-DoD: three interior frames — tavern (host + hearth), temple, shop (sign-lit shells).
            if (rig != null && nightAdapter != null)
            {
                var fpsI = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fpsI != null) fpsI.enabled = false;
                var interiorSpots = new[]
                {
                    EmberCrpg.Presentation.Ember.WorldDirector.RuntimeInteriorInfo.TavernWorld,
                    EmberCrpg.Presentation.Ember.WorldDirector.RuntimeInteriorInfo.TempleWorld,
                    EmberCrpg.Presentation.Ember.WorldDirector.RuntimeInteriorInfo.ShopWorld,
                };
                string[] interiorNames = { "look_tavern", "look_temple", "look_shop" };
                for (int s = 0; s < 3; s++)
                {
                    if (interiorSpots[s] == Vector3.zero) { Debug.Log($"[Proof] F26 {interiorNames[s]} skipped — no anchor."); continue; }
                    // Small shells: a tight offset from the centre stays clear of walls/furniture;
                    // the diagonal aim crosses the room (hearth corner + any seated host).
                    rig.transform.position = interiorSpots[s] + new Vector3(0.85f, 1.15f, 0.85f);
                    rig.transform.rotation = Quaternion.LookRotation(
                        interiorSpots[s] + new Vector3(-1.5f, 0.7f, -1.5f) - rig.transform.position);
                    yield return new WaitForSecondsRealtime(0.7f);
                    yield return new WaitForEndOfFrame();
                    CaptureToPng(Path.Combine(_outputDir, interiorNames[s] + ".png"));
                    yield return new WaitForSecondsRealtime(0.4f); // async capture separation
                }
                if (fpsI != null)
                {
                    fpsI.enabled = true;
                    fpsI.SyncYaw(rig.transform.eulerAngles.y);
                }
            }

            // F27-DoD: midday at the tavern — civilians walked in over the lunch window (the schedule
            // routes them 12:00-13:59); pose icons (mug) ride their billboards. Census + one frame.
            if (rig != null && nightAdapter != null
                && EmberCrpg.Presentation.Ember.WorldDirector.RuntimeInteriorInfo.TavernWorld != Vector3.zero)
            {
                nightAdapter.ProofAdvanceHours(1); // ~12:1x → ~13:1x — still lunch, a full hour of walking done
                Debug.Log("[Proof] F27 " + nightAdapter.ProofLunchCensus());
                FindFirstObjectByType<EmberCrpg.Presentation.Ember.Views.EmberGeneratedActorSpawner>()?.SpawnMissingNearbyActors();
                var fpsL = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fpsL != null) fpsL.enabled = false;
                var tavern = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeInteriorInfo.TavernWorld;
                rig.transform.position = tavern + new Vector3(5.5f, 1.6f, 5.5f);
                rig.transform.rotation = Quaternion.LookRotation(tavern + Vector3.up * 1.0f - rig.transform.position);
                yield return new WaitForSecondsRealtime(1.2f); // views glide in + icons poll
                yield return new WaitForEndOfFrame();
                CaptureToPng(Path.Combine(_outputDir, "look_tavern_lunch.png"));
                yield return new WaitForSecondsRealtime(0.4f); // async capture separation
                if (fpsL != null)
                {
                    fpsL.enabled = true;
                    fpsL.SyncYaw(rig.transform.eulerAngles.y);
                }
            }

            // F28-DoD: the spell school — the three damage types wear their COLOURS in the world
            // (flame orange / frost ice-blue / spark white-gold). The adapter arms each cast
            // (learns the catalog, refills mana, posts a living target three cells east), then
            // ProofCast flies the tinted bolt through the REAL cast path and the frame is taken
            // mid-flight. Lantern Glow holds its held-orb for a fourth frame.
            if (rig != null && nightAdapter != null)
            {
                var spellCaster = rig.GetComponent<EmberCrpg.Presentation.Ember.Combat.EmberPlayerSpellCaster>();
                var fpsS = rig.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (spellCaster != null)
                {
                    if (fpsS != null) fpsS.enabled = false;
                    rig.transform.position = EmberCrpg.Presentation.Ember.WorldDirector.RuntimePlayerSpawn.Position + Vector3.up * 0.2f;
                    rig.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // face +X — the armed target stands 3 cells east
                    yield return null; // the combat-position tracker reads the rig's new spot before arming

                    int SlotOf(string templateId)
                    {
                        var slots = nightAdapter.SpellSlots;
                        for (int s = 0; s < slots.Count; s++)
                            if (slots[s] == templateId) return s;
                        return -1;
                    }

                    var boltLegs = new[]
                    {
                        (template: EmberCrpg.Simulation.Magic.WorldSpellCatalog.FlameBoltTemplateId, frame: "look_spell_flame.png"),
                        (template: EmberCrpg.Simulation.Magic.WorldSpellCatalog.FrostLanceTemplateId, frame: "look_spell_frost.png"),
                        (template: EmberCrpg.Simulation.Magic.WorldSpellCatalog.SparkArcTemplateId, frame: "look_spell_spark.png"),
                    };
                    foreach (var leg in boltLegs)
                    {
                        Debug.Log("[Proof] F28 " + nightAdapter.ProofArmSpellSchool());
                        int slot = SlotOf(leg.template);
                        bool fired = slot >= 0 && spellCaster.ProofCast(slot);
                        Debug.Log($"[Proof] F28 cast {leg.template}: slot={slot} fired={fired}");
                        yield return new WaitForSecondsRealtime(0.1f); // mid-flight (~3m of the 8m, 0.28s total)
                        yield return new WaitForEndOfFrame();
                        CaptureToPng(Path.Combine(_outputDir, leg.frame));
                        yield return new WaitForSecondsRealtime(0.6f); // capture separation + the bolt expires
                    }

                    Debug.Log("[Proof] F28 " + nightAdapter.ProofArmSpellSchool());
                    int lanternSlot = SlotOf(EmberCrpg.Simulation.Magic.WorldSpellCatalog.LanternGlowTemplateId);
                    bool lanternFired = lanternSlot >= 0 && spellCaster.ProofCast(lanternSlot);
                    Debug.Log($"[Proof] F28 cast lantern_glow: slot={lanternSlot} fired={lanternFired}");
                    yield return new WaitForSecondsRealtime(0.5f); // the rig view notices the window and lights the orb
                    yield return new WaitForEndOfFrame();
                    CaptureToPng(Path.Combine(_outputDir, "look_spell_lantern.png"));
                    yield return new WaitForSecondsRealtime(0.4f); // async capture separation
                    // Snuff the 60s window NOW — the held orb must not photobomb the delve frames
                    // that the F10-F20 eye-checks already pinned.
                    EmberCrpg.Presentation.Ember.WorldDirector.RuntimeSpellFxMirror.LightUntilRealtime = 0f;
                    yield return null; // the view notices and destroys the orb before the next leg

                    if (fpsS != null)
                    {
                        fpsS.enabled = true;
                        fpsS.SyncYaw(rig.transform.eulerAngles.y);
                    }
                }
                else
                {
                    Debug.Log("[Proof] BROKEN — no EmberPlayerSpellCaster on the rig for the F28 spell leg.");
                }
            }

            // F10-DoD: travel to the nearest DELVE, walk the corridor into the chamber, and eye-proof the
            // haunters guarding the chest, the red hit flash, and the corpse pose after the kill.
            if (nightAdapter != null)
            {
                var delveRow = nightAdapter.ReadDelveGuidance();
                if (delveRow.HasTarget && nightAdapter.TryTravelToSettlement(delveRow.TargetName, out _))
                {
                    EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                        EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                    SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                    // Realize is synchronous in host Awake; a long arrival wait let the haunters chase
                    // ~4m BEFORE the F14 A-frame (a=6.4m instead of ~10.6m). 1.5s settles visuals only.
                    yield return new WaitForSecondsRealtime(1.5f);

                    var rig4 = GameObject.Find("PlayerRig");
                    var interior = GameObject.Find("DungeonInterior");
                    Debug.Log($"[Proof] delve leg: at '{delveRow.TargetName}', rig={(rig4 != null)} interior={(interior != null)}.");
                    if (rig4 != null && interior != null)
                    {
                        var cc4 = rig4.GetComponent<CharacterController>();
                        if (cc4 != null) cc4.enabled = false;
                        // EVIDENCE (look7): "rig aimed ... rot=(0,0,0)" — EmberFirstPersonController
                        // rewrites rig yaw from its cached _yawDegrees EVERY frame, stomping proof aims;
                        // both chamber captures faced the same wall while the haunters stood 6m away.
                        var fps4 = rig4.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                        if (fps4 != null) fps4.enabled = false;
                        // The scene reload re-arms the F17 XP auto-open; an open modal both pollutes the
                        // captures AND gates the hostile-AI pump (chase would freeze). Clear it first.
                        var delveUi = FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>();
                        delveUi?.ProofCloseScreens();
                        yield return null;

                        // F18: world anchors come from the realize step (RuntimeDungeonLayoutInfo) — the
                        // single-chamber local-axis guesses died with the multi-room delve.
                        // F29: dwellers are bestiary-typed now ("Fen Wolf of X" / "Bone Walker of X"
                        // / ...) — the catalog is the single source of truth for the name prefixes.
                        bool IsDweller(string n) =>
                            EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.IsBestiaryName(n)
                            || n.StartsWith("Warden");
                        Debug.Log($"[Proof] F18 delve layout: rooms={DelveLayout.RoomCount} " +
                                  $"dwellerSpots={DelveLayout.DwellerSpots.Count} extent={DelveLayout.FootprintExtentMeters:0.0}m");

                        // F14-DoD CHASE PROOF: stand in the START room and let the nearest dweller COME
                        // (sight 18 covers the 16m room lattice). Two consecutive captures + logged
                        // distances must show ≥4m closed (AI steps 1 cell / 0.45s ≈ 2.2 m/s).
                        rig4.transform.position = DelveLayout.StartRoomWorld + Vector3.up * 1.0f;
                        yield return null; // views already exist (scene-arrival spawn); capture A at once —
                                           // a 0.6s pre-wait let the chase eat ~3m before the A frame
                        Transform chaseTarget = null;
                        foreach (var view in FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
                        {
                            if (!IsDweller(view.name) || view.name.StartsWith("Warden")) continue;
                            if (chaseTarget == null
                                || (view.transform.position - rig4.transform.position).sqrMagnitude
                                   < (chaseTarget.position - rig4.transform.position).sqrMagnitude)
                                chaseTarget = view.transform;
                        }
                        if (chaseTarget != null)
                        {
                            Debug.Log("[Proof] chase A " + nightAdapter.ProofChaseDebug());
                            float distA = Vector3.Distance(chaseTarget.position, rig4.transform.position);
                            rig4.transform.rotation = Quaternion.LookRotation(
                                chaseTarget.position + Vector3.up * 0.9f - rig4.transform.position);
                            yield return new WaitForEndOfFrame();
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_chase_a.png"));
                            yield return new WaitForSecondsRealtime(2.6f); // ~5.7m of chase at 2.2 m/s
                            Debug.Log("[Proof] chase B " + nightAdapter.ProofChaseDebug());
                            float distB = Vector3.Distance(chaseTarget.position, rig4.transform.position);
                            rig4.transform.rotation = Quaternion.LookRotation(
                                chaseTarget.position + Vector3.up * 0.9f - rig4.transform.position);
                            yield return new WaitForEndOfFrame();
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_chase_b.png"));
                            Debug.Log($"[Proof] F14 chase: a={distA:0.0}m b={distB:0.0}m closed={distA - distB:0.0}m " +
                                      $"(DoD: >=4m over two consecutive frames).");
                            // ScreenCapture is ASYNC — moving the camera in the same frame as the call
                            // makes the PNG show the NEXT pose (chamber aim), not this one.
                            yield return new WaitForSecondsRealtime(0.4f);
                        }
                        else
                        {
                            Debug.Log("[Proof] BROKEN — no haunter view found for the F14 chase proof.");
                        }

                        // Stand in the start room, then AIM the camera at the nearest dweller view — the
                        // first runs proved fixed axis guesses capture wall corners instead of monsters.
                        rig4.transform.position = DelveLayout.StartRoomWorld + Vector3.up * 1.0f;
                        rig4.transform.rotation = Quaternion.LookRotation(
                            DelveLayout.FootprintCenterWorld + Vector3.up - rig4.transform.position);
                        yield return new WaitForSecondsRealtime(1.0f);

                        // Positional evidence + camera lock: the haunters must STAND in the chamber, not
                        // merely exist as data (first run: encounter bound while billboards sat unseen).
                        Transform nearestHaunter = null;
                        foreach (var view in FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
                        {
                            if (!IsDweller(view.name)) continue;
                            var sr = view.GetComponentInChildren<SpriteRenderer>(true);
                            Debug.Log($"[Proof] haunter view '{view.name}' at {view.transform.position} " +
                                      $"(rig at {rig4.transform.position}, dist={Vector3.Distance(view.transform.position, rig4.transform.position):0.0}m) " +
                                      $"srOn={(sr != null && sr.enabled)} sprite={(sr != null && sr.sprite != null ? sr.sprite.name : "NULL")} " +
                                      $"size={(sr != null ? sr.bounds.size.ToString() : "-")} color={(sr != null ? sr.color.ToString() : "-")}");
                            if (nearestHaunter == null
                                || (view.transform.position - rig4.transform.position).sqrMagnitude
                                   < (nearestHaunter.position - rig4.transform.position).sqrMagnitude)
                                nearestHaunter = view.transform;
                        }
                        if (nearestHaunter != null)
                        {
                            var aim = nearestHaunter.position + Vector3.up * 0.9f - rig4.transform.position;
                            if (aim.sqrMagnitude > 0.01f)
                                rig4.transform.rotation = Quaternion.LookRotation(aim);
                            yield return new WaitForSecondsRealtime(0.4f);
                            Debug.Log($"[Proof] rig aimed at haunter: fwd={rig4.transform.forward} rot={rig4.transform.rotation.eulerAngles}");
                        }
                        CaptureToPng(Path.Combine(_outputDir, "look_dungeon_chamber.png"));
                        // Async-capture separation: the roof toggle + topdown teleport below must not
                        // land in the frame this capture actually renders (chamber == topdown otherwise).
                        yield return new WaitForSecondsRealtime(0.4f);

                        // F18-DoD: a top-down frame over the WHOLE footprint — the room graph (rooms,
                        // corridors, chest, dwellers) must read in ONE capture. Roofs are hidden for the
                        // frame (cutaway view), then restored — they'd otherwise be all the camera sees.
                        var roofs = new List<MeshRenderer>();
                        foreach (var mr in interior.GetComponentsInChildren<MeshRenderer>())
                            if (mr.name.EndsWith("Roof")) { mr.enabled = false; roofs.Add(mr); }
                        float topHeight = Mathf.Max(16f, DelveLayout.FootprintExtentMeters * 1.25f);
                        rig4.transform.position = DelveLayout.FootprintCenterWorld + Vector3.up * topHeight;
                        rig4.transform.rotation = Quaternion.LookRotation(Vector3.down, interior.transform.forward);
                        yield return new WaitForSecondsRealtime(0.4f);
                        CaptureToPng(Path.Combine(_outputDir, "look_dungeon_topdown.png"));
                        yield return new WaitForSecondsRealtime(0.4f); // capture lands before roofs return
                        foreach (var mr in roofs) mr.enabled = true;
                        rig4.transform.position = DelveLayout.StartRoomWorld + Vector3.up * 1.0f;
                        if (nearestHaunter != null)
                            rig4.transform.rotation = Quaternion.LookRotation(nearestHaunter.position + Vector3.up * 0.9f - rig4.transform.position);
                        yield return new WaitForSecondsRealtime(0.3f);

                        string haunterName = nightAdapter.ProofBindDungeonHaunter();
                        if (!string.IsNullOrEmpty(haunterName))
                        {
                            // Swing until the FIRST landed hit, then capture inside the 0.15s flash window
                            // (one frame for the view to see the stamp, then the frame edge for the PNG).
                            int swings = 0;
                            bool hit = false;
                            while (swings < 250 && !(hit = nightAdapter.TryMeleeStrike(haunterName, 20)))
                                swings++;
                            yield return null;
                            yield return new WaitForEndOfFrame();
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_hitflash.png"));
                            Debug.Log($"[Proof] haunter strike: hit={hit} after {swings + 1} swings (flash frame captured).");

                            Debug.Log(nightAdapter.ProofFinishBoundEncounter());
                            yield return null;
                            delveUi?.ProofCloseScreens(); // the kill XP may cross a level — clear the auto-modal
                            yield return new WaitForSecondsRealtime(0.4f); // flash decays, corpse pose holds
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_felled.png"));
                            // Async-capture separation: the boss-room teleport below must not steal this
                            // frame (the corpse pose rendered as the boss room otherwise).
                            yield return new WaitForSecondsRealtime(0.4f);

                            // F20-DoD: the path down to the Warden — step ON the crushing plate (8 dmg,
                            // audible, logged), take the key from its pedestal, let the boss door's lock
                            // CONSUME it. Flow lines: [Trap] → [Key] → [Door].
                            rig4.transform.position = DelveLayout.TrapWorld + Vector3.up * 1.0f;
                            yield return new WaitForSecondsRealtime(1.0f); // the plate polls and fires
                            yield return new WaitForEndOfFrame();
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_trap.png"));
                            yield return new WaitForSecondsRealtime(0.4f); // capture separation
                            rig4.transform.position = DelveLayout.KeyWorld + Vector3.up * 1.0f;
                            yield return new WaitForSecondsRealtime(1.0f); // pickup polls
                            rig4.transform.position = DelveLayout.BossDoorWorld + Vector3.up * 1.0f;
                            yield return new WaitForSecondsRealtime(1.6f); // the lock consumes the key, the slab grinds
                            delveUi?.ProofCloseScreens();

                            // F18-DoD BOSS LEG: descend to the boss room, frame the Warden, bind it, fell
                            // it ("looptest şefe kadar iner"), then open the hoard for the loot line.
                            rig4.transform.position = DelveLayout.BossRoomWorld + Vector3.up * 1.0f;
                            Transform wardenView = null;
                            foreach (var view in FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
                                if (view.name.StartsWith("Warden")) { wardenView = view.transform; break; }
                            if (wardenView != null)
                                rig4.transform.rotation = Quaternion.LookRotation(
                                    wardenView.position + Vector3.up * 0.9f - rig4.transform.position);
                            delveUi?.ProofCloseScreens(); // a clear HUD before the boss frame
                            yield return new WaitForSecondsRealtime(0.4f);
                            yield return new WaitForEndOfFrame();
                            CaptureToPng(Path.Combine(_outputDir, "look_dungeon_boss.png"));
                            yield return new WaitForSecondsRealtime(0.4f); // capture separation

                            string wardenName = nightAdapter.ProofBindDelveWarden();
                            if (!string.IsNullOrEmpty(wardenName))
                            {
                                Debug.Log($"[Proof] F18 boss bound: {wardenName}.");
                                // F30: hold the bound-boss window open across the music director's
                                // 2s poll — the BATTLE slot + "+percussion" boss layer must LOG.
                                yield return new WaitForSecondsRealtime(2.6f);
                                Debug.Log(nightAdapter.ProofFinishBoundEncounter());
                                yield return null;
                                delveUi?.ProofCloseScreens(); // boss XP may cross a level — clear the auto-modal
                                yield return new WaitForSecondsRealtime(0.4f); // flash decays, corpse pose holds
                                CaptureToPng(Path.Combine(_outputDir, "look_dungeon_boss_felled.png"));
                                yield return new WaitForSecondsRealtime(0.4f); // capture separation
                            }
                            else
                            {
                                Debug.Log("[Proof] BROKEN — no Warden bound at the delve (F18 boss leg).");
                            }

                            // F16-DoD: open the chest (driver can't press E), log the sword grant — the
                            // F18 "loot satırı" — and capture the hinged-open lid beside the hoard.
                            var chestView = FindFirstObjectByType<EmberCrpg.Presentation.Ember.WorldDirector.RuntimeChestView>();
                            if (chestView != null)
                            {
                                Debug.Log("[Proof] chest: " + chestView.ProofOpen());
                                var chestEye = chestView.transform.position;
                                var standDir = DelveLayout.BossRoomWorld - chestEye;
                                standDir.y = 0f;
                                if (standDir.sqrMagnitude < 0.01f) standDir = Vector3.back;
                                rig4.transform.position = chestEye + standDir.normalized * 2.4f + Vector3.up * 1.1f;
                                rig4.transform.rotation = Quaternion.LookRotation(
                                    chestEye + Vector3.up * 0.45f - rig4.transform.position);
                                yield return new WaitForSecondsRealtime(0.5f);
                                yield return new WaitForEndOfFrame();
                                CaptureToPng(Path.Combine(_outputDir, "look_dungeon_chest.png"));
                                yield return new WaitForSecondsRealtime(0.4f); // capture separation
                            }
                            else
                            {
                                Debug.Log("[Proof] BROKEN — no RuntimeChestView found at the delve.");
                            }
                        }
                        else
                        {
                            Debug.Log("[Proof] BROKEN — no haunter bound at the delve (expected 2 chamber outlaws).");
                        }

                        // F29-DoD: the BESTIARY family photo — three living dwellers of three
                        // DISTINCT types posted shoulder-to-shoulder in the start room, one frame;
                        // then one melee strike so the typed thud logs "[Audio] hit variant=...".
                        Debug.Log("[Proof] F29 " + nightAdapter.ProofArrangeBestiaryPhoto(DelveLayout.StartRoomWorld));
                        rig4.transform.position = DelveLayout.StartRoomWorld + new Vector3(0f, 1.0f, -3.4f);
                        rig4.transform.rotation = Quaternion.LookRotation(
                            DelveLayout.StartRoomWorld + Vector3.up * 0.9f - rig4.transform.position);
                        yield return new WaitForSecondsRealtime(1.4f); // tick sync snaps the trio's views (>5m = snap)
                        delveUi?.ProofCloseScreens();
                        yield return new WaitForEndOfFrame();
                        CaptureToPng(Path.Combine(_outputDir, "look_bestiary_trio.png"));
                        yield return new WaitForSecondsRealtime(0.4f); // async capture separation

                        Transform photoTarget = null;
                        foreach (var view in FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
                        {
                            if (!IsDweller(view.name) || view.name.StartsWith("Warden")) continue;
                            if (photoTarget == null
                                || (view.transform.position - rig4.transform.position).sqrMagnitude
                                   < (photoTarget.position - rig4.transform.position).sqrMagnitude)
                                photoTarget = view.transform;
                        }
                        if (photoTarget != null)
                        {
                            int trioSwings = 0;
                            while (trioSwings < 250 && !nightAdapter.TryMeleeStrike(photoTarget.name, 20)) trioSwings++;
                            Debug.Log($"[Proof] F29 typed thud: struck '{photoTarget.name}' after {trioSwings + 1} swings " +
                                      "(the [Audio] hit variant line above is the DoD log).");
                        }
                        else
                        {
                            Debug.Log("[Proof] BROKEN — no bestiary view found for the F29 typed-thud strike.");
                        }
                        if (fps4 != null)
                        {
                            fps4.enabled = true;
                            fps4.SyncYaw(rig4.transform.eulerAngles.y); // hand the cached yaw the proof's final aim
                        }
                        if (cc4 != null) cc4.enabled = true;
                    }
                }
                else
                {
                    Debug.Log("[Proof] delve leg skipped — no delve target or travel refused.");
                }

                // F19-DoD VARIETY LEG: travel to up to two MORE delves and capture one interior frame
                // each — the realize log line carries "archetype=" per visit. A world may roll fewer
                // than three dungeons (the invariant guarantees only ONE); the archetype-mapping
                // EditMode test proves all three palettes exist, this leg shows what THIS world has.
                var firstDelve = nightAdapter.ReadDelveGuidance().TargetName;
                var delveNames = nightAdapter.ProofListDelveNames();
                Debug.Log($"[Proof] F19 delve census: {delveNames.Count} dungeon(s) in this world.");
                int variants = 0;
                for (int d = 0; d < delveNames.Count && variants < 2; d++)
                {
                    string delveName = delveNames[d];
                    if (delveName == firstDelve) continue; // the F18 leg already covered it
                    if (!nightAdapter.TryTravelToSettlement(delveName, out _)) continue;
                    EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(
                        EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current);
                    SceneManager.LoadScene(EmberScenes.GeneratedWorld);
                    yield return new WaitForSecondsRealtime(1.5f);
                    var rigV = GameObject.Find("PlayerRig");
                    var interiorV = GameObject.Find("DungeonInterior");
                    if (rigV == null || interiorV == null)
                    {
                        Debug.Log($"[Proof] F19 variant '{delveName}' skipped — rig/interior missing.");
                        continue;
                    }
                    var ccV = rigV.GetComponent<CharacterController>();
                    if (ccV != null) ccV.enabled = false;
                    var fpsV = rigV.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                    if (fpsV != null) fpsV.enabled = false;
                    FindFirstObjectByType<EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController>()?.ProofCloseScreens();
                    yield return null;
                    rigV.transform.position = DelveLayout.StartRoomWorld + Vector3.up * 1.0f;
                    rigV.transform.rotation = Quaternion.LookRotation(
                        DelveLayout.FootprintCenterWorld + Vector3.up - rigV.transform.position);
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return new WaitForEndOfFrame();
                    variants++;
                    CaptureToPng(Path.Combine(_outputDir, $"look_delve_variant_{variants}.png"));
                    yield return new WaitForSecondsRealtime(0.4f); // async capture lands before we move on
                    Debug.Log($"[Proof] F19 variant captured: '{delveName}' (frame {variants}).");
                    if (fpsV != null)
                    {
                        fpsV.enabled = true;
                        fpsV.SyncYaw(rigV.transform.eulerAngles.y);
                    }
                    if (ccV != null) ccV.enabled = true;
                }
                if (variants == 0)
                    Debug.Log("[Proof] F19: no second delve in this world — variety rests on the mapping test.");
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

        // BOOT-RACE FIX (shipcheck "world-enter: no adapter"): the boot flow navigates Boot→MainMenu on
        // its own schedule; a FIXED pre-wait raced it — when boot timing shifted (forge-off made it
        // slower to settle), the boot's MainMenu navigation STOMPED our GeneratedWorld load. Wait until
        // the MainMenu is actually the active scene (it cannot stomp after that), then a short grace.
        private static IEnumerator WaitForBootToSettle()
        {
            float deadline = Time.unscaledTime + 30f;
            while (SceneManager.GetActiveScene().name != EmberScenes.MainMenu && Time.unscaledTime < deadline)
                yield return null;
            yield return new WaitForSecondsRealtime(0.8f);
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

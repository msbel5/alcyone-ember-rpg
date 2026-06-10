// Why this file is intentionally long: the in-game UI controller is the single presentation orchestrator for HUD refresh, screen routing, and adapter-backed live read-model projection until the remaining screens are split behind narrower controllers.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.UI.InGame.Screens;
using EmberCrpg.Simulation.Magic;
// ICombatHudSource / ISpellBarSource / IEmberHudSource live in the enclosing EmberCrpg.Presentation.Ember.UI
// namespace, so they resolve here without an explicit using.

namespace EmberCrpg.Presentation.Ember.UI.InGame
{
    /// <summary>
    /// Hosts the UI-Toolkit in-game UI (Phase 1: the World HUD; later phases add the modal screens) over the
    /// live gameplay scene. Mounts its own <see cref="UIDocument"/> so it is independent of the menu/char-creation
    /// surface, builds the self-scaling stage + <see cref="WorldHudView"/>, and binds REAL values each frame
    /// (vitals from <see cref="ICombatHudSource"/>, location from the clock + starting settlement, spell slots +
    /// recent world events). It renders ADDITIVELY over the existing uGUI HUD for now — the old HUD is only
    /// retired once the full in-game UI is verified, so the game never breaks mid-migration.
    ///
    /// Gold / level / class are intentionally NOT shown: the simulation does not yet track them, and a fake
    /// number is exactly the kind of player-facing lie we removed elsewhere. They light up once wired.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InGameUiController : MonoBehaviour
    {
        private WorldHudView _hud;
        private InGameStage _stage;
        private object _host;
        private VisualElement _dropdown;
        private VisualElement _activeScreen;   // the overlay the open view added — tracked directly so ANY screen
                                               // (IgModal-named OR a bare Pause/Combat/LevelUp/Death/Dialog/DM/Loot
                                               // VisualElement) can be detected + closed, not just "IgModalOverlay"
        private CharacterView _activeCharacter;
        private string _activePlayerPortraitKey;
        private int _playerPortraitVersion = -1;
        private IDialogSource _activeDialogSource;   // live NPC conversation behind the redesigned DialogView
        private DialogView _activeDialog;
        private string _activeDialogPortrait;        // portrait key, re-resolved each frame until the sprite loads
        private string _tradeMerchantOverrideName;   // dialogue-driven trade reuses settlement stock but labels it with the NPC honestly
        private ConsulFateView _activeOracle;        // the open Oracle screen, polled for its async prophecy
        private TradeView _activeTrade;
        private CraftingView _activeCrafting;
        private SaveLoadView _activeSaveLoad;
        private CombatView _activeCombat;
        private bool _oraclePending;
        private bool _wasOpen;

        /// <summary>True while this controller is active — the legacy EmberWorldHost key handlers (M / Tab / K /
        /// R) yield to it so the redesigned screens own input instead of opening the old uGUI panels.</summary>
        public static bool OwnsInput { get; private set; }

        /// <summary>True while a redesigned screen (a modal or the ☰ browser) is open — folded into
        /// EmberWorldHost.IsModalOpen() so FPS look/move stop, and the controller frees the cursor + pauses.</summary>
        public static bool AnyScreenOpen { get; private set; }

        private void OnEnable() => OwnsInput = true;
        private void OnDisable()
        {
            OwnsInput = false; AnyScreenOpen = false;
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;   // never leave the world paused
        }

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) doc = gameObject.AddComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                doc.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                doc.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DefaultRuntimeTheme");
            }
            doc.panelSettings.sortingOrder = 100;   // over the world
            var root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;   // the HUD must not eat world clicks

            _stage = new InGameStage(root);
            _hud = new WorldHudView(_stage.Canvas)
            {
                OnOpenScreen = OpenScreen,
                OnConsulDm = ConsulDm,
            };
            BuildScreenBrowser(_stage.Canvas);   // ☰ pill: every screen reachable for use + inspection

            // uGUI ScreenSpace-Overlay HUD renders OVER UI-Toolkit panels, so the redesigned HUD cannot sit on
            // top of the legacy EmberHud — Phase 1 retires it and the new HUD owns the screen. The action-bar
            // commands are re-bound onto the new spell bar / buttons in later phases.
            foreach (var legacy in FindObjectsByType<EmberHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                legacy.gameObject.SetActive(false);
            // The always-on top-right event log and the legacy uGUI pause menu are part of the old HUD stack the
            // redesigned UI replaces: retire them too so (a) the old log stops showing over the new HUD and
            // (b) Esc no longer pops the old menu — the controller routes Esc to the redesigned PauseView instead.
            // (EnsureInGameUi is mounted LAST in EmberWorldHost.Awake so both already exist when this runs.)
            foreach (var log in FindObjectsByType<EventLogHudPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                log.gameObject.SetActive(false);
            foreach (var pause in FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                pause.gameObject.SetActive(false);
            Debug.Log("[InGameUI] World HUD mounted; legacy EmberHud + event log + pause menu retired.");
        }

        /// <summary>Wire the data + button host (the EmberWorldHost). Called by EmberWorldHost.EnsureInGameUi.</summary>
        public void Bind(MonoBehaviour host)
        {
            _host = host;
        }

        private void Update()
        {
            if (_hud == null) return;
            _stage?.Fit();
            HandleScreenInput();

            // Free the cursor whenever any redesign-owned screen is open, but only PAUSE for menu-like screens.
            // Live conversation/oracle screens must keep the tick running so the adapter drains its async LLM
            // completions onto the main thread just like the old non-pausing dialog/oracle panels did.
            bool open = IsAnyOpen();
            bool conversationOpen = _activeDialog != null || _activeOracle != null;
            AnyScreenOpen = open;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                // Fully-qualified: this file has both `using UnityEngine` and `using UnityEngine.UIElements`,
                // and UIElements also defines a `Cursor` type (CS0104 ambiguity otherwise).
                UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
                UnityEngine.Cursor.visible = open;
            }
            Time.timeScale = open && !conversationOpen ? 0f : 1f;

            // Stream the NPC's live line into the open DialogView each frame (the off-thread LLM resolves even at
            // timeScale 0; Update still runs), so "{name} thinks…" becomes the real greeting/answer. Also keep
            // re-resolving the portrait until it loads — forge portraits generate asynchronously.
            if (_activeDialog != null && _activeDialogSource != null)
            {
                var line = _activeDialogSource.GetCurrentLine();
                _activeDialog.SetCurrentLine(line);
                if (_activeDialog.HasPendingResponse && !_activeDialogSource.IsThinking)
                    _activeDialog.ResolveLatestResponse(line);
                if (!_activeDialog.HasPortrait && !string.IsNullOrEmpty(_activeDialogPortrait) && _host is ISpriteByName sprites)
                {
                    var sp = sprites.GetSprite(_activeDialogPortrait);
                    if (sp != null) _activeDialog.SetPortrait(sp);
                }
            }

            if (_activeCharacter != null && !_activeCharacter.HasPortrait && !string.IsNullOrEmpty(_activePlayerPortraitKey) && _host is ISpriteByName playerSprites)
            {
                var sp = playerSprites.GetSprite(_activePlayerPortraitKey);
                if (sp != null)
                    _activeCharacter.SetPortrait(sp);
            }
            if (_activeCharacter != null && PlayerPortraitHandoff.Version != _playerPortraitVersion)
            {
                var sp = TryGetPlayerPortraitSprite(forceRefresh: true);
                if (sp != null)
                    _activeCharacter.SetPortrait(sp);
            }

            if (_activeOracle != null && !_activeOracle.HasPortrait && _host is ISpriteByName oracleSprites)
            {
                var sp = oracleSprites.GetSprite(DialogPortraitKey.DungeonMaster);
                if (sp != null)
                    _activeOracle.SetPortrait(sp);
            }

            // Stream the Oracle's async prophecy into the open Consul-Fate screen (same poll pattern as dialog).
            if (_activeOracle != null && _oraclePending)
            {
                var resolved = EmberDomainAdapterLocator.ConsultFateOracle?.TryConsumeResolvedFate();
                if (!string.IsNullOrEmpty(resolved))
                {
                    _activeOracle.SetOracleLine(resolved);
                    _activeOracle.ResolveLatestAnswer(resolved);
                    _oraclePending = false;
                }
            }

            // F2/encounters: the adapter signals "an outlaw drew steel" — open the combat screen on the spot.
            if (EmberCrpg.Presentation.Ember.Adapters.WorldEncounterSignal.Consume())
            {
                OpenScreen("combat");
                Debug.Log("[InGameUI] world encounter signal consumed — combat screen opened.");
            }

            if (_activeCombat != null && _host is ICombatScreenSource combatScreen)
                _activeCombat.Refresh(combatScreen.ReadCombatScreenState());

            var d = new WorldHudData();

            if (_host is ICombatHudSource combat)
            {
                var s = combat.Read();
                d.Hp = s.Health; d.HpMax = s.HealthMax;
                d.Fatigue = s.Stamina; d.FatigueMax = s.StaminaMax;
                d.Mana = s.Mana; d.ManaMax = s.ManaMax;
            }
            if (_host is ISpellBarSource spells)
                d.SpellSlots = spells.GetSlots();
            if (_host is IEmberHudSource hud)
                d.Location = hud.GetHudText();   // the real top-bar string (Tick/Day/mood/pop/settlement)
            var worldOptions = EmberRuntimeOptionsProvider.Current.WorldHost;
            if ((worldOptions.ShowQuestGuidance || worldOptions.ShowQuestCompass) && _host is IQuestGuidanceSource guidance)
            {
                var row = guidance.ReadQuestGuidance();
                if (row.HasTarget)
                {
                    if (worldOptions.ShowQuestGuidance)
                        d.EventLine = row.Line;
                    if (worldOptions.ShowQuestCompass)
                        d.CompassLine = BuildCompassLine(row);
                }
            }

            _hud.Refresh(in d);
        }

        private static string BuildCompassLine(QuestGuidanceRow row)
        {
            if (row.DistanceTiles <= 0)
                return "QUEST " + row.TargetName + " · nearby";
            return "QUEST " + row.TargetName + " · " + row.DistanceTiles + row.Unit + " · " + row.Direction; // "m" local, "tiles" overland
        }

        // Every in-game screen, opened by id. One modal at a time: CloseScreen() drops any open IgModal overlay
        // first. The HUD buttons (I/C/M/J/K/DM) + the ☰ screen browser route here.
        private void OpenScreen(string screenId)
        {
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeAudioDirector.PlayUiClick(); // F3/audio: every screen open clicks
            CloseScreen();
            CloseBrowser();
            RefreshLivePlayer();   // feed the screens the REAL created character (name / stats / vitals)
            var c = _stage.Canvas;
            int before = c.childCount;
            switch (screenId)
            {
                case "inventory":
                    RefreshLiveInventory();
                    new InventoryView(c, CloseScreen, TodoInventoryAction);
                    break;
                case "character":
                    _activeCharacter = new CharacterView(c, CloseScreen, OpenScreen);
                    var playerPortrait = TryGetPlayerPortraitSprite();
                    if (playerPortrait != null)
                        _activeCharacter.SetPortrait(playerPortrait);
                    _activePlayerPortraitKey = ResolvePlayerPortraitKey();
                    if (!_activeCharacter.HasPortrait && !string.IsNullOrEmpty(_activePlayerPortraitKey) && _host is ISpriteByName playerSprites)
                    {
                        var sp = playerSprites.GetSprite(_activePlayerPortraitKey);
                        if (sp != null)
                            _activeCharacter.SetPortrait(sp);
                    }
                    break;
                case "spellbook":
                    RefreshLiveSpells();
                    new SpellbookView(c, CloseScreen, TodoSpellbookAction);
                    break;
                case "journal":
                    RefreshLiveJournal();
                    new JournalView(c, CloseScreen);
                    break;
                case "worldmap":
                    new WorldMapView(c, CloseScreen, FastTravelAction);
                    break;
                case "colony":
                    RefreshLiveColony();
                    new ColonyView(c, CloseScreen, TodoColonyTaskAction);
                    break;
                case "consul":
                    _activeOracle = new ConsulFateView(c, CloseScreen, AskOracle);
                    if (_host is ISpriteByName oracleSprites)
                    {
                        var sp = oracleSprites.GetSprite(DialogPortraitKey.DungeonMaster);
                        if (sp != null)
                            _activeOracle.SetPortrait(sp);
                    }
                    break;
                case "dialog":    new DialogView(c, CloseScreen); break;
                case "combat":
                    RefreshLiveSpells();
                    _activeCombat = new CombatView(c, CloseScreen, ReadCombatScreenState(), TodoCombatAction, TodoCombatFleeAction);
                    break;
                case "loot":
                    RefreshLiveInventory();
                    new LootView(c, CloseScreen, TodoTakeAllLootAction);
                    break;
                case "trade":
                    _tradeMerchantOverrideName = null;
                    RefreshLiveTrade();
                    _activeTrade = new TradeView(c, CloseScreen, TodoTradeAction);
                    break;
                case "crafting":
                    RefreshLiveCrafting();
                    _activeCrafting = new CraftingView(c, CloseScreen, TodoCraftAction);
                    break;
                case "pause":
                    new PauseView(c, CloseScreen, OpenScreen, TodoSettingsAction, TodoMainMenuAction);
                    break;
                case "levelup":
                    RefreshLivePlayer();
                    RefreshLiveSpells();
                    new LevelUpView(c, CloseScreen, ReadLevelUpState(), TodoConfirmLevelUpAction);
                    break;
                case "death":
                    new DeathView(c, CloseScreen, TodoLoadLastSaveAction, TodoMainMenuAction);
                    break;
                case "savegame":
                    RefreshLiveSaveLoad();
                    _activeSaveLoad = new SaveLoadView(c, CloseScreen, TodoSaveSlotAction, TodoLoadSlotAction);
                    break;
            }
            // Every view calls stageCanvas.Add(_overlay) exactly once, so the newly-added last child IS this
            // screen's overlay — whether IgModal-based ("IgModalOverlay") or a bare VisualElement (Pause/Combat/
            // LevelUp/Death/Dialog/DM/Loot). Tracking the element itself is what lets CloseScreen + IsAnyOpen
            // handle ALL screens, so Esc closes them and the cursor/pause toggle fires correctly.
            _activeScreen = c.childCount > before ? c.ElementAt(c.childCount - 1) : null;
            if (_activeScreen != null)
            {
                _activeScreen.schedule.Execute(() =>
                {
                    if (_activeScreen == null) return;
                    _activeScreen.Query<ScrollView>().ForEach(IgDesign.StyleScroll);
                }).StartingIn(0);
            }
        }

        private void CloseScreen()
        {
            // End any live NPC conversation FIRST so a late async reply can't bleed into the next one (bumps the
            // conversation serial) — covers every close path: Farewell, Esc, the X, or opening another screen.
            if (_activeDialogSource != null)
            {
                _activeDialogSource.EndConversation();
                _activeDialogSource = null; _activeDialog = null; _activeDialogPortrait = null;
            }
            _activeCharacter = null; _activePlayerPortraitKey = null;
            _activeOracle = null; _oraclePending = false; _activeTrade = null; _activeCrafting = null; _activeSaveLoad = null; _activeCombat = null;
            if (_activeScreen != null) { _activeScreen.RemoveFromHierarchy(); _activeScreen = null; }
            // Safety net for IgModal-based views in case the tracked element ever desyncs.
            for (var open = _stage.Canvas.Q("IgModalOverlay"); open != null; open = _stage.Canvas.Q("IgModalOverlay"))
                open.RemoveFromHierarchy();
        }

        /// <summary>Open the redesigned NPC dialogue, driven by the REAL <see cref="IDialogSource"/> (greeting +
        /// topics + async LLM replies). The interact raycaster calls this instead of the legacy DialogBoxPanel.</summary>
        public void OpenNpcDialog(IDialogSource src, string npcName, string portrait)
        {
            if (src == null || _stage == null) return;
            CloseScreen();   // ends any prior conversation + drops any open screen
            CloseBrowser();
            var c = _stage.Canvas;
            int before = c.childCount;

            var topics = new List<DialogTopicOption>();
            var ids = src.GetTopics();
            if (ids != null)
                foreach (var id in ids)
                    topics.Add(new DialogTopicOption(id, HumanizeToken(id) ?? id));

            _activeDialogSource = src;
            _activeDialogPortrait = portrait;
            _activeDialog = new DialogView(
                c, CloseScreen,
                string.IsNullOrEmpty(npcName) ? "Stranger" : npcName,
                portrait,
                src.GetCurrentLine(),
                topics,
                id => src.SelectTopic(id),
                question => src.AskFreeText(question),
                () => OpenTradeFromDialog(npcName),
                CloseScreen);
            _activeScreen = c.childCount > before ? c.ElementAt(c.childCount - 1) : null;
            if (!string.IsNullOrEmpty(portrait) && _host is ISpriteByName spriteLookup)
            {
                var sp = spriteLookup.GetSprite(portrait);
                if (sp != null) _activeDialog.SetPortrait(sp);
            }
        }

        private void ConsulDm() => OpenScreen("consul");

        // The Oracle (Consul Fate): each Ask (typed or a suggested chip) fires a REAL fate consult through the
        // adapter — the player's free-text question colours the LLM prophecy — and the immediate "consults…"
        // placeholder shows at once; Update() polls TryConsumeResolvedFate() and streams the prophecy in.
        private void AskOracle(string question)
        {
            var oracle = EmberDomainAdapterLocator.ConsultFateOracle;
            if (oracle == null) return;
            var trimmed = question == null ? string.Empty : question.Trim();
            if (string.IsNullOrEmpty(trimmed) || _oraclePending) return;

            _activeOracle?.BeginQuestion(trimmed);
            _activeOracle?.SetOracleLine(oracle.ConsultFate(trimmed));
            _oraclePending = true;
        }

        // Feed the redesigned screens the REAL created character (name + six attributes + vitals) by overwriting
        // the shared IgMockData.Player snapshot before a screen reads it. Fields the domain does not yet track
        // (level/XP/class/birthsign/skills/gold) keep their mock values. No-op (mock kept) when there is no host
        // or no player actor — proof + EditMode contexts — so the views still render.
        private void RefreshLivePlayer()
        {
            if (!(_host is IPlayerSheetSource sheetSrc)) return;
            var sheet = sheetSrc.ReadPlayerSheet();
            if (!sheet.HasData) return;

            var mock = IgMockData.DefaultPlayer;
            int hp = mock.Hp, hpMax = mock.HpMax, ft = mock.Fatigue, ftMax = mock.FatigueMax, mp = mock.Mana, mpMax = mock.ManaMax;
            if (_host is ICombatHudSource hudSrc)
            {
                var v = hudSrc.Read();
                hp = v.Health; hpMax = v.HealthMax; ft = v.Stamina; ftMax = v.StaminaMax; mp = v.Mana; mpMax = v.ManaMax;
            }

            IgMockData.Player = mock with
            {
                Name = string.IsNullOrWhiteSpace(sheet.Name) ? mock.Name : sheet.Name,
                Hp = hp, HpMax = hpMax, Fatigue = ft, FatigueMax = ftMax, Mana = mp, ManaMax = mpMax,
                Stats = new[]
                {
                    new StatData("MIG", sheet.Mig),
                    new StatData("AGI", sheet.Agi),
                    new StatData("END", sheet.End),
                    new StatData("MND", sheet.Mnd),
                    new StatData("INS", sheet.Ins),
                    new StatData("PRE", sheet.Pre),
                },
            };
        }

        private void RefreshLiveInventory()
        {
            IgMockData.Inventory = Array.Empty<InventoryItemData>();
            IgMockData.EquipSlots = Array.Empty<EquipmentSlotData>();
            if (_host is ITradeSource tradeSrc)
            {
                var trade = tradeSrc.ReadTradeState();
                IgMockData.Player = IgMockData.Player with { Gold = trade.PlayerGold };
            }
            if (!(_host is IInventorySource inventorySrc)) return;

            var slots = inventorySrc.GetSlots();
            if (slots == null) return;

            var live = new InventoryItemData[slots.Count];
            for (int i = 0; i < slots.Count; i++)
                live[i] = MapInventorySlot(slots[i], i);
            IgMockData.Inventory = live;

            // TODO(real-data): IInventorySource exposes only flat slots; no live equipment read-model yet.
        }

        private void RefreshLiveSpells()
        {
            IgMockData.SpellBar = Array.Empty<SpellBarSlotData>();
            IgMockData.SpellSchools = Array.Empty<SpellSchoolData>();
            if (!(_host is ISpellBarSource spellSrc)) return;

            var slots = spellSrc.GetSlots();
            if (slots == null) return;

            int slotCount = Mathf.Max(5, slots.Count);
            int selected = spellSrc.GetSelectedSlot();
            var liveBar = new SpellBarSlotData[slotCount];
            var resolved = new List<SpellDefinition>();
            for (int i = 0; i < slotCount; i++)
            {
                string templateId = i < slots.Count ? slots[i] : null;
                var spell = TryResolveSpell(templateId);
                if (spell != null && !resolved.Contains(spell))
                    resolved.Add(spell);
                liveBar[i] = new SpellBarSlotData(
                    i + 1,
                    spell != null ? spell.DisplayName : HumanizeToken(templateId),
                    i == selected);
            }

            IgMockData.SpellBar = liveBar;
            if (resolved.Count > 0)
                IgMockData.SpellSchools = BuildSpellSchools(resolved);
            // TODO(real-data): no dedicated spellbook host source yet; render only what maps from live spell ids.
        }

        private void RefreshLiveColony()
        {
            IgMockData.ColonyNpcs = Array.Empty<ColonyNpcData>();

            var builders = new Dictionary<string, ColonyNpcProjection>(StringComparer.Ordinal);
            if (_host is IColonyNeedsSource needsSrc)
            {
                var rows = needsSrc.GetRows();
                if (rows != null)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        if (string.IsNullOrWhiteSpace(row.ActorName)) continue;
                        var npc = GetOrCreateColonyNpc(builders, row.ActorName);
                        npc.Needs = new[]
                        {
                            new NeedData("Hunger", row.Hunger),
                            new NeedData("Fatigue", row.Fatigue),
                            new NeedData("Thirst", row.Thirst),
                        };
                        npc.Mood = MoodLabel(row.Mood);
                    }
                }
            }

            if (_host is IJobQueueSource jobsSrc)
            {
                var rows = jobsSrc.GetRows();
                if (rows != null)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        if (string.IsNullOrWhiteSpace(row.ActorName)) continue;
                        var npc = GetOrCreateColonyNpc(builders, row.ActorName);
                        if (!string.IsNullOrWhiteSpace(row.JobTag))
                            npc.Role = HumanizeToken(row.JobTag);
                        npc.Task = string.IsNullOrWhiteSpace(row.StatusCode)
                            ? npc.Role
                            : HumanizeToken(row.StatusCode);
                    }
                }
            }

            // TODO(real-data): FactionRow is world-level, not actor-keyed, so it does not project cleanly onto NPC cards.
            if (_host is IFactionSource)
            {
            }

            if (builders.Count == 0) return;

            var live = new List<ColonyNpcData>(builders.Count);
            foreach (var pair in builders)
            {
                var npc = pair.Value;
                live.Add(new ColonyNpcData(
                    npc.Name,
                    string.IsNullOrWhiteSpace(npc.Role) ? "Colonist" : npc.Role,
                    0,
                    0,
                    npc.Needs ?? Array.Empty<NeedData>(),
                    string.IsNullOrWhiteSpace(npc.Mood) ? "Unknown" : npc.Mood,
                    string.IsNullOrWhiteSpace(npc.Task) ? "Idle" : npc.Task));
            }

            live.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            IgMockData.ColonyNpcs = live.ToArray();
        }

        private void RefreshLiveJournal()
        {
            IgJournalData.Chapters = Array.Empty<JournalChapterData>();
            IgJournalData.CurrentChapter = 0;
            IgJournalData.EmptyTitle = "Journal";
            IgJournalData.EmptyBody = "No active quests — the world has asked nothing of you yet.";
            IgJournalData.EmptyDetail = "No live quest has been accepted.";
            if (EmberRuntimeOptionsProvider.Current.WorldHost.ShowQuestGuidance && _host is IQuestGuidanceSource guidance)
            {
                var row = guidance.ReadQuestGuidance();
                if (row.HasTarget)
                {
                    IgJournalData.EmptyTitle = row.Title;
                    IgJournalData.EmptyBody = row.Line;
                    IgJournalData.EmptyDetail = "This is guidance only; speak with the NPC to accept the quest.";
                }
            }
            if (!(_host is IJournalSource journalSrc)) return;

            var chapters = journalSrc.GetChapters();
            if (chapters == null || chapters.Count == 0) return;

            var live = new JournalChapterData[chapters.Count];
            for (int i = 0; i < chapters.Count; i++)
            {
                var chapter = chapters[i];
                var entries = chapter.Entries ?? Array.Empty<JournalEntryRow>();
                var rows = new JournalEntryData[entries.Count];
                for (int e = 0; e < entries.Count; e++)
                {
                    var entry = entries[e];
                    rows[e] = new JournalEntryData(
                        entry.EntryId,
                        entry.Title,
                        entry.DateLabel,
                        entry.Body,
                        entry.CategoryLabel,
                        JournalStatusLabel(entry.Status),
                        entry.Status);
                }

                live[i] = new JournalChapterData(chapter.ChapterIndex, chapter.Title, rows);
            }

            IgJournalData.Chapters = live;
            IgJournalData.CurrentChapter = Mathf.Clamp(journalSrc.GetCurrentChapter(), 0, live.Length - 1);
        }

        private void RefreshLiveTrade(string statusLine = null)
        {
            IgTradeData.Current = IgTradeData.Default;
            if (!(_host is ITradeSource tradeSrc)) return;

            var state = tradeSrc.ReadTradeState();
            var merchant = new TradeOfferData[state.MerchantItems.Count];
            for (int i = 0; i < state.MerchantItems.Count; i++)
            {
                var row = state.MerchantItems[i];
                merchant[i] = new TradeOfferData(row.TemplateId, row.Name, row.Category, row.Quantity, row.UnitPrice, row.CanAfford, row.Equipped, TradeActionKind.Buy);
            }

            var player = new TradeOfferData[state.PlayerItems.Count];
            for (int i = 0; i < state.PlayerItems.Count; i++)
            {
                var row = state.PlayerItems[i];
                player[i] = new TradeOfferData(row.TemplateId, row.Name, row.Category, row.Quantity, row.UnitPrice, row.CanAfford, row.Equipped, TradeActionKind.Sell);
            }

            var line = statusLine ?? (merchant.Length == 0 ? "No merchant stock is available here yet." : "Choose an item to buy or sell.");
            IgMockData.Player = IgMockData.Player with { Gold = state.PlayerGold };
            IgTradeData.Current = new TradeScreenData(
                string.IsNullOrWhiteSpace(_tradeMerchantOverrideName) ? state.MerchantName : _tradeMerchantOverrideName,
                state.SettlementName,
                state.PlayerGold,
                state.MerchantGold,
                line,
                merchant,
                player);
        }

        private void OpenTradeFromDialog(string npcName)
        {
            if (_stage == null)
                return;

            CloseScreen();
            CloseBrowser();

            _tradeMerchantOverrideName = string.IsNullOrWhiteSpace(npcName) ? null : npcName;
            RefreshLiveTrade();

            var canvas = _stage.Canvas;
            int before = canvas.childCount;
            _activeTrade = new TradeView(canvas, CloseScreen, TodoTradeAction);
            _activeScreen = canvas.childCount > before ? canvas.ElementAt(canvas.childCount - 1) : null;
        }

        private void RefreshLiveCrafting(string statusLine = null)
        {
            IgCraftingData.Current = IgCraftingData.Default;
            if (!(_host is ICraftingSource craftSrc)) return;

            var state = craftSrc.ReadCraftingState();
            var recipes = new CraftingRecipeData[state.Recipes.Count];
            for (int i = 0; i < state.Recipes.Count; i++)
            {
                var row = state.Recipes[i];
                recipes[i] = new CraftingRecipeData(
                    row.RecipeId,
                    row.Name,
                    row.Station,
                    row.IngredientSummary,
                    row.OutputSummary,
                    row.AvailabilityLabel,
                    row.CanCraft);
            }

            var line = statusLine ?? (recipes.Length == 0 ? "No recipes are available here yet." : "Choose a recipe to craft.");
            IgCraftingData.Current = new CraftingScreenData(state.StationName, line, recipes);
        }

        private void RefreshLiveSaveLoad(string statusLine = null)
        {
            IgSaveLoadData.Current = IgSaveLoadData.Default;
            if (!(_host is ISaveLoadSource saveSrc)) return;

            var state = saveSrc.ReadSaveLoadState();
            var lookup = new Dictionary<string, EmberCrpg.Data.Save.SaveSlotMetadata>(StringComparer.Ordinal);
            for (int i = 0; i < state.Slots.Count; i++)
            {
                var meta = state.Slots[i];
                if (meta == null) continue;
                lookup[ToSlotId(meta).FileStem()] = meta;
            }

            var rows = new List<SaveSlotViewData>(state.ManualSlotCap + 2);
            AddSaveSlotRow(rows, lookup, EmberCrpg.Data.Save.SaveSlotId.Quick);
            AddSaveSlotRow(rows, lookup, EmberCrpg.Data.Save.SaveSlotId.Auto);
            for (int i = 0; i < state.ManualSlotCap; i++)
                AddSaveSlotRow(rows, lookup, EmberCrpg.Data.Save.SaveSlotId.Manual(i));

            IgSaveLoadData.Current = new SaveLoadScreenData(
                statusLine ?? "Choose a slot to save or load.",
                rows.ToArray());
        }

        private LevelUpScreenState ReadLevelUpState()
        {
            if (_host is ILevelUpSource levelSrc)
                return levelSrc.ReadLevelUpState();

            var player = IgMockData.Player;
            var stats = new[]
            {
                new LevelUpStatRow("MIG", "MIG", player.Stats[0].Value),
                new LevelUpStatRow("AGI", "AGI", player.Stats[1].Value),
                new LevelUpStatRow("END", "END", player.Stats[2].Value),
                new LevelUpStatRow("MND", "MND", player.Stats[3].Value),
                new LevelUpStatRow("INS", "INS", player.Stats[4].Value),
                new LevelUpStatRow("PRE", "PRE", player.Stats[5].Value),
            };

            var choices = new List<LevelUpSpellRow>();
            foreach (var spell in WorldSpellCatalog.All)
                choices.Add(new LevelUpSpellRow(spell.TemplateId, spell.DisplayName, spell.School.ToString(), spell.ManaCost, DescribeSpellEffect(spell)));
            return new LevelUpScreenState(player.Name, player.Level, 5, stats, choices);
        }

        private CombatScreenState ReadCombatScreenState()
        {
            return _host is ICombatScreenSource combatSource
                ? combatSource.ReadCombatScreenState()
                : new CombatScreenState(false, "Unknown", 0, 0, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, System.Array.Empty<CombatSpellActionRow>());
        }

        private static InventoryItemData MapInventorySlot(InventorySlot slot, int index)
        {
            var fallback = FindDefaultInventoryItem(slot.IconName);
            string name = fallback != null ? fallback.Name : HumanizeToken(slot.IconName);
            string type = fallback != null ? fallback.Type : InferItemType(slot.IconName);
            float weight = fallback != null ? fallback.Weight : 0f;
            int value = fallback != null ? fallback.Value : 0;
            bool equipped = fallback != null && fallback.Equipped;
            int quantity = Mathf.Max(1, slot.Count);
            return new InventoryItemData(index + 1, name, type, weight, value, quantity, equipped);
        }

        private static InventoryItemData FindDefaultInventoryItem(string templateId)
        {
            string key = NormalizeToken(templateId);
            if (string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < IgMockData.DefaultInventory.Length; i++)
            {
                var item = IgMockData.DefaultInventory[i];
                if (NormalizeToken(item.Name) == key)
                    return item;
            }
            return null;
        }

        private static SpellDefinition TryResolveSpell(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId)) return null;
            return WorldSpellCatalog.Find(templateId);
        }

        private static SpellSchoolData[] BuildSpellSchools(List<SpellDefinition> spells)
        {
            var bySchool = new Dictionary<string, List<SpellData>>(StringComparer.Ordinal);
            for (int i = 0; i < spells.Count; i++)
            {
                var spell = spells[i];
                string school = spell.School.ToString();
                if (!bySchool.TryGetValue(school, out var list))
                {
                    list = new List<SpellData>();
                    bySchool.Add(school, list);
                }

                list.Add(new SpellData(
                    spell.DisplayName,
                    spell.ManaCost,
                    DescribeSpellEffect(spell),
                    DescribeSpellRange(spell),
                    DescribeSpellDuration(spell)));
            }

            var live = new List<SpellSchoolData>(bySchool.Count);
            foreach (var pair in bySchool)
                live.Add(new SpellSchoolData(pair.Key, pair.Value.ToArray()));
            live.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return live.ToArray();
        }

        private static string DescribeSpellEffect(SpellDefinition spell)
        {
            if (spell.Effects == null || spell.Effects.Count == 0) return "Unknown effect";
            var effect = spell.Effects[0];
            if (effect.Kind == SpellEffectCode.DirectDamage) return effect.Magnitude + " damage";
            if (effect.Kind == SpellEffectCode.RestoreHealth) return "Restore " + effect.Magnitude + " HP";
            if (effect.Kind == SpellEffectCode.RestoreFatigue) return "Restore " + effect.Magnitude + " FAT";
            if (effect.Kind == SpellEffectCode.RestoreMana) return "Restore " + effect.Magnitude + " MP";
            if (effect.Kind == SpellEffectCode.ShieldBuff) return "Ward +" + effect.Magnitude;
            if (effect.Kind == SpellEffectCode.DirectMana) return effect.Magnitude + " mana drain";
            if (effect.Kind == SpellEffectCode.DirectFatigue) return effect.Magnitude + " fatigue drain";
            return HumanizeToken(effect.Kind.Code);
        }

        private static string DescribeSpellRange(SpellDefinition spell)
        {
            if (spell.TargetKind == SpellTargetKind.CasterSelf) return "Self";
            if (spell.TargetKind == SpellTargetKind.Touch) return "Touch";
            if (spell.RangeInTiles > 0) return spell.RangeInTiles + " tiles";
            return "Single Target";
        }

        private static string DescribeSpellDuration(SpellDefinition spell)
        {
            if (spell.Effects == null || spell.Effects.Count == 0) return "Instant";
            int duration = spell.Effects[0].DurationTicks;
            return duration > 0 ? duration + " ticks" : "Instant";
        }

        private static ColonyNpcProjection GetOrCreateColonyNpc(Dictionary<string, ColonyNpcProjection> builders, string actorName)
        {
            if (!builders.TryGetValue(actorName, out var npc))
            {
                npc = new ColonyNpcProjection { Name = actorName };
                builders.Add(actorName, npc);
            }
            return npc;
        }

        private static string MoodLabel(int mood)
        {
            if (mood >= 75) return "Content";
            if (mood >= 55) return "Steady";
            if (mood >= 35) return "Anxious";
            return "Tired";
        }

        private static string InferItemType(string templateId)
        {
            string key = NormalizeToken(templateId);
            if (key.Contains("sword") || key.Contains("dagger") || key.Contains("blade") || key.Contains("staff"))
                return "Weapon";
            if (key.Contains("armor") || key.Contains("shield") || key.Contains("mail") || key.Contains("jerkin"))
                return "Armor";
            if (key.Contains("potion") || key.Contains("tonic"))
                return "Potion";
            if (key.Contains("scroll"))
                return "Scroll";
            if (key.Contains("bread") || key.Contains("ration") || key.Contains("meat") || key.Contains("water"))
                return "Food";
            if (key.Contains("coin") || key.Contains("gold") || key.Contains("silver"))
                return "Currency";
            return "Tool";
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().Replace(" ", "_").Replace("-", "_").ToLowerInvariant();
        }

        private static string HumanizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var parts = value.Trim().Replace("-", "_").Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return value;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                parts[i] = part.Length == 1
                    ? part.ToUpperInvariant()
                    : char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
            }

            return string.Join(" ", parts);
        }

        private static string JournalStatusLabel(JournalEntryStatus status)
        {
            switch (status)
            {
                case JournalEntryStatus.Completed: return "Completed";
                case JournalEntryStatus.Failed: return "Failed";
                default: return "Active";
            }
        }

        private static void AddSaveSlotRow(List<SaveSlotViewData> rows, Dictionary<string, EmberCrpg.Data.Save.SaveSlotMetadata> lookup, EmberCrpg.Data.Save.SaveSlotId slot)
        {
            lookup.TryGetValue(slot.FileStem(), out var meta);
            rows.Add(new SaveSlotViewData(
                slot,
                EmberCrpg.Presentation.Ember.Save.SaveSlotLabelFormatter.Title(slot),
                DescribeSaveSlot(slot, meta),
                meta != null));
        }

        private static string DescribeSaveSlot(EmberCrpg.Data.Save.SaveSlotId slot, EmberCrpg.Data.Save.SaveSlotMetadata meta)
        {
            if (meta == null)
                return EmberCrpg.Presentation.Ember.Save.SaveSlotLabelFormatter.Title(slot) + " · Empty slot";

            var scene = string.IsNullOrWhiteSpace(meta.sceneName) ? "Unknown Scene" : HumanizeToken(meta.sceneName);
            var minutes = meta.playtimeMinutes < 0 ? 0 : meta.playtimeMinutes;
            var stamp = string.IsNullOrWhiteSpace(meta.savedAtUtcIso) ? "Unknown time" : meta.savedAtUtcIso;
            return scene + " · " + minutes + "m · " + stamp;
        }

        private static EmberCrpg.Data.Save.SaveSlotId ToSlotId(EmberCrpg.Data.Save.SaveSlotMetadata meta)
        {
            if (meta == null || string.IsNullOrWhiteSpace(meta.slotKind))
                return EmberCrpg.Data.Save.SaveSlotId.Manual(0);

            if (string.Equals(meta.slotKind, EmberCrpg.Data.Save.SaveSlotKind.Quick.ToString(), StringComparison.OrdinalIgnoreCase))
                return EmberCrpg.Data.Save.SaveSlotId.Quick;
            if (string.Equals(meta.slotKind, EmberCrpg.Data.Save.SaveSlotKind.Auto.ToString(), StringComparison.OrdinalIgnoreCase))
                return EmberCrpg.Data.Save.SaveSlotId.Auto;
            return EmberCrpg.Data.Save.SaveSlotId.Manual(meta.slotIndex < 0 ? 0 : meta.slotIndex);
        }

        // TODO(host-action): human wires real save/load/quit/combat.
        private void TodoSettingsAction() => LogTodoAndClose("open settings");
        // Real action (Pause + Death "Main Menu"): unpause and load the menu scene.
        private void TodoMainMenuAction()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(EmberScenes.MainMenu);
        }
        private void TodoLoadLastSaveAction()
        {
            if (!(_host is ISaveLoadCommandSink saveSink))
            {
                Debug.Log("[InGameUI] last-save load unavailable.");
                CloseScreen();
                return;
            }

            var result = saveSink.LoadLatestSave();
            if (result.Success)
            {
                CloseScreen();
                return;
            }

            RefreshLiveSaveLoad(result.Message);
            _activeSaveLoad?.Refresh();
        }
        private void TodoCombatFleeAction() => LogTodoAndClose("combat flee");
        private void TodoTakeAllLootAction() => LogTodoAndClose("take all loot");
        private LevelUpActionResult TodoConfirmLevelUpAction(LevelUpSelection selection)
        {
            if (!(_host is ILevelUpCommandSink levelSink))
                return new LevelUpActionResult(false, "Level-up commands are unavailable.");

            var result = levelSink.ApplyLevelUp(selection);
            if (result.Success)
            {
                RefreshLivePlayer();
                RefreshLiveSpells();
            }

            Debug.Log("[InGameUI] level-up: " + result.Message);
            return result;
        }
        private void TodoInventoryAction(string actionId) => LogTodoAndClose("inventory action: " + actionId);
        private void TodoSpellbookAction(string spellName) => LogTodoAndClose("spell action: " + spellName);
        private void TodoTradeAction(TradeActionRequest request)
        {
            if (!(_host is ITradeCommandSink tradeSink))
            {
                RefreshLiveTrade("Trade commands are unavailable in this scene.");
                _activeTrade?.Refresh();
                return;
            }

            var result = tradeSink.ExecuteTrade(request);
            RefreshLiveInventory();
            RefreshLiveTrade(result.Message);
            _activeTrade?.Refresh();
        }
        private void TodoCraftAction(string recipeId)
        {
            if (!(_host is ICraftingCommandSink craftSink))
            {
                RefreshLiveCrafting("Craft commands are unavailable in this scene.");
                _activeCrafting?.Refresh();
                return;
            }

            var result = craftSink.ExecuteCraft(recipeId);
            RefreshLiveInventory();
            RefreshLiveCrafting(result.Message);
            _activeCrafting?.Refresh();
        }
        // REAL fast travel: the adapter moves the domain player actor to the destination settlement, then
        // reloading the world scene re-runs the proven EmberWorldHost.Awake path — the locator-registered
        // adapter survives the load, so WorldSceneDirector realizes the DESTINATION tile (its geography,
        // its layout, its NPCs) with zero stale references. PARTIAL (honest): travel is instant; it does
        // not yet advance world time by the real 40km/tile distance.
        private void FastTravelAction(string settlementName)
        {
            // F3/loading screen: travel is CHUNKED — one sim-day per frame behind a full-screen overlay, so
            // the 14-day cap is gone and a cross-continent hop stays responsive while the world truly lives
            // through every day of the road.
            var adapter = EmberDomainAdapterLocator.Current as DomainSimulationAdapter;
            var hostMono = _host as MonoBehaviour;
            if (adapter == null || hostMono == null)
            {
                LogTodoAndClose("fast travel unavailable: no live travel sink");
                return;
            }

            if (!adapter.TryBeginTravelToSettlement(settlementName, out int days, out var message))
            {
                LogTodoAndClose(message);
                return;
            }

            CloseAll();
            Debug.Log("[Travel] " + message + " (ticking " + days + " days behind the loading screen)");
            hostMono.StartCoroutine(TravelRoutine(adapter, days));
        }

        private System.Collections.IEnumerator TravelRoutine(DomainSimulationAdapter adapter, int days)
        {
            BuildTravelOverlay(out var dayLabel);
            for (int d = 1; d <= days; d++)
            {
                dayLabel.text = $"On the road — day {d} of {days}";
                adapter.AdvanceTravelDay();
                yield return null; // a frame per day keeps the overlay alive and the editor responsive
            }

            Debug.Log($"[Travel] arrival after {days} ticked days — re-realizing the world at the destination.");
            // Carry the LIVE world across the reload: the old host's OnDestroy clears the locator mid-load,
            // and without this hand-off the new host would bootstrap a fresh default world (the bug where
            // travel ended in a black screen + "overland unavailable").
            EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldContinuity.Carry(EmberDomainAdapterLocator.Current);
            UnityEngine.SceneManagement.SceneManager.LoadScene(EmberCrpg.Presentation.Ember.EmberScenes.GeneratedWorld);
        }

        private void BuildTravelOverlay(out Label dayLabel)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.top = 0; overlay.style.right = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0.05f, 0.04f, 0.04f, 0.97f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var title = new Label("Fast Travel");
            title.style.fontSize = 30;
            title.style.color = new Color(0.85f, 0.72f, 0.45f);
            overlay.Add(title);

            dayLabel = new Label("Setting out...");
            dayLabel.style.fontSize = 18;
            dayLabel.style.color = new Color(0.78f, 0.75f, 0.70f);
            dayLabel.style.marginTop = 10;
            overlay.Add(dayLabel);

            _stage.Canvas.Add(overlay); // the scene reload tears it down with the canvas
        }
        private void TodoConsulAskAction(string prompt) => LogTodoAndClose("consult fate: " + prompt);
        private void TodoCombatAction(string actionId)
        {
            var commands = EmberDomainAdapterLocator.PlayerCommandSink;
            if (commands == null)
            {
                Debug.Log("[InGameUI] combat commands unavailable.");
                return;
            }

            bool handled = false;
            if (string.Equals(actionId, "attack", StringComparison.Ordinal))
            {
                handled = commands.TryMeleeStrike(string.Empty, EmberRuntimeOptionsProvider.Current.Combat.MeleeRawDamage);
            }
            else if (!string.IsNullOrEmpty(actionId) && actionId.StartsWith("cast:", StringComparison.Ordinal))
            {
                var indexText = actionId.Substring("cast:".Length);
                if (int.TryParse(indexText, out var slotIndex))
                    handled = commands.TryCastSpell(slotIndex);
            }

            if (!handled && !string.IsNullOrEmpty(actionId))
                commands.LogCombat("Combat action refused: " + actionId);

            _activeCombat?.Refresh(ReadCombatScreenState());
        }
        private void TodoSaveSlotAction(EmberCrpg.Data.Save.SaveSlotId slot)
        {
            if (!(_host is ISaveLoadCommandSink saveSink))
            {
                RefreshLiveSaveLoad("Save service is unavailable.");
                _activeSaveLoad?.Refresh();
                return;
            }

            var result = saveSink.SaveToSlot(slot);
            RefreshLiveSaveLoad(result.Message);
            _activeSaveLoad?.Refresh();
        }

        private void TodoLoadSlotAction(EmberCrpg.Data.Save.SaveSlotId slot)
        {
            if (!(_host is ISaveLoadCommandSink saveSink))
            {
                RefreshLiveSaveLoad("Load service is unavailable.");
                _activeSaveLoad?.Refresh();
                return;
            }

            var result = saveSink.LoadFromSlot(slot);
            if (result.Success)
            {
                CloseScreen();
                return;
            }

            RefreshLiveSaveLoad(result.Message);
            _activeSaveLoad?.Refresh();
        }
        private void TodoColonyTaskAction(string actorName, string taskId) =>
            LogTodoAndClose("colony task: " + actorName + " -> " + taskId);

        private void LogTodoAndClose(string action)
        {
            Debug.Log("[InGameUI] TODO action: " + action);
            CloseScreen();
        }

        private sealed class ColonyNpcProjection
        {
            public string Name;
            public string Role;
            public string Task;
            public string Mood;
            public NeedData[] Needs;
        }

        // ── input: Tab toggles the ☰ browser; the letter keys open a screen; Esc closes. The legacy host
        // handlers yield (OwnsInput), so these REPLACE the old uGUI panels. The cursor is locked in FPS play, so
        // the ☰ pill can't be clicked until a key opens a screen — that is why Tab is the entry point.
        private void HandleScreenInput()
        {
            // While the user is typing in a text field (the Oracle's Ask box, NPC free-text, etc.) the letter
            // hotkeys (R/C/M/…) and Tab must NOT fire — otherwise typing "r" reopens the screen. Esc still closes.
            bool typing = IsTextInputFocused();
            if (EmberInput.KeyDown(KeyCode.Escape)) { if (IsAnyOpen()) CloseAll(); else OpenScreen("pause"); return; }
            if (typing) return;
            if (EmberInput.KeyDown(KeyCode.Tab))    { ToggleBrowser(); return; }
            if (EmberInput.KeyDown(KeyCode.C))      OpenScreen("character");
            else if (EmberInput.KeyDown(KeyCode.I)) OpenScreen("inventory");
            else if (EmberInput.KeyDown(KeyCode.M)) OpenScreen("worldmap");
            else if (EmberInput.KeyDown(KeyCode.J)) OpenScreen("journal");
            else if (EmberInput.KeyDown(KeyCode.K)) OpenScreen("colony");
            else if (EmberInput.KeyDown(KeyCode.R)) OpenScreen("consul");
        }

        // True when a TextField (or its inner text input) holds focus — suppresses screen hotkeys while typing.
        private bool IsTextInputFocused()
        {
            var focused = _stage?.Canvas?.panel?.focusController?.focusedElement as VisualElement;
            if (focused == null) return false;
            return focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null;
        }

        private bool IsAnyOpen() =>
            _activeScreen != null ||
            (_dropdown != null && _dropdown.style.display == DisplayStyle.Flex) ||
            (_stage != null && _stage.Canvas.Q("IgModalOverlay") != null);

        private void ToggleBrowser()
        {
            if (_dropdown == null) return;
            bool show = _dropdown.style.display != DisplayStyle.Flex;
            CloseScreen();   // a modal and the browser are mutually exclusive
            _dropdown.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CloseBrowser() { if (_dropdown != null) _dropdown.style.display = DisplayStyle.None; }
        private void CloseAll() { CloseScreen(); CloseBrowser(); }

        /// <summary>Proof/diagnostic hook: open a screen by id from the screenshot driver (verification tours).</summary>
        public void ProofOpenScreen(string id)
        {
            // Diagnostics for the known-broken proof tour (modals render manually but not in proof captures):
            // these counters tell the next autoplay log whether the overlay was even added to the canvas.
            int before = _stage?.Canvas != null ? _stage.Canvas.childCount : -1;
            OpenScreen(id);
            int after = _stage?.Canvas != null ? _stage.Canvas.childCount : -1;
            UnityEngine.Debug.Log($"[ProofUI] open '{id}': stage={(_stage != null)} canvas {before}->{after} active={(_activeScreen != null)}");
        }

        /// <summary>Proof/diagnostic hook: programmatic Escape — close any open modal/browser between captures.</summary>
        public void ProofCloseScreens() => CloseAll();

        // The player's approved portrait travels from character creation as PNG bytes on the pending
        // worldgen intent (real forge face if it generated, else the deterministic creation swatch).
        // Build it into a Sprite once and cache it so the Character screen shows the real likeness
        // instead of the "C" glyph fallback. Returns null when no portrait was carried.
        private Sprite _playerPortraitSprite;
        private bool _playerPortraitResolved;
        private Sprite TryGetPlayerPortraitSprite(bool forceRefresh = false)
        {
            int version = PlayerPortraitHandoff.Version;
            if (!forceRefresh && _playerPortraitResolved && version == _playerPortraitVersion)
                return _playerPortraitSprite;
            _playerPortraitResolved = true;

            _playerPortraitSprite = PlayerPortraitHandoff.TryCreateSprite();
            _playerPortraitVersion = version;
            return _playerPortraitSprite;
        }

        private string ResolvePlayerPortraitKey()
        {
            // TODO(player-portrait-runtime): character creation currently persists PortraitJson but does not
            // register a stable gameplay sprite key for the player's forged portrait. Keep the Character screen
            // glyph fallback until that handoff exists, instead of guessing a key and showing the wrong sprite.
            return string.Empty;
        }

        private static readonly (string id, string label)[] AllScreens =
        {
            ("inventory", "Inventory"), ("character", "Character"), ("spellbook", "Spellbook"),
            ("journal", "Journal"), ("worldmap", "World Map"), ("colony", "Colony"), ("consul", "Consul · DM"),
            ("dialog", "NPC Dialog"), ("combat", "Combat"), ("loot", "Loot"), ("trade", "Trade"),
            ("crafting", "Crafting"), ("pause", "Pause"), ("levelup", "Level Up"), ("death", "Death"),
            ("savegame", "Save / Load"),
        };

        // A ☰ pill at top-centre (like the design's ScreenBrowser) that drops down every screen — so all 16 are
        // reachable + inspectable while the per-key/per-trigger wiring is migrated off the legacy panels.
        private void BuildScreenBrowser(VisualElement canvas)
        {
            var wrap = new VisualElement();
            // Full-width row, children centred — robustly keeps the pill + dropdown centred under the ☰ (the old
            // left:50% + translate slid the opened dropdown off to the right).
            wrap.style.position = Position.Absolute; wrap.style.top = 8; wrap.style.left = 0; wrap.style.right = 0;
            wrap.style.alignItems = Align.Center;

            _dropdown = new VisualElement();
            var dropdown = _dropdown;
            dropdown.style.display = DisplayStyle.None;
            dropdown.style.flexDirection = FlexDirection.Row; dropdown.style.flexWrap = Wrap.Wrap;
            dropdown.style.maxWidth = 720; dropdown.style.marginTop = 6;
            dropdown.style.backgroundColor = IgDesign.C(8, 6, 4, 0.96f);
            IgDesign.Border(dropdown, IgDesign.PA(0.18f), 1); IgDesign.Radius(dropdown, 12);
            dropdown.style.paddingTop = 12; dropdown.style.paddingBottom = 12;
            dropdown.style.paddingLeft = 14; dropdown.style.paddingRight = 14;
            foreach (var (id, label) in AllScreens)
            {
                var sid = id;
                var b = new Button(() => { OpenScreen(sid); dropdown.style.display = DisplayStyle.None; }) { text = label };
                IgDesign.ResetButton(b);
                b.style.fontSize = 11; b.style.color = IgDesign.ParchDim; IgDesign.ApplyFont(b, IgDesign.Sans);
                b.style.backgroundColor = IgDesign.C(22, 17, 10, 0.65f); IgDesign.Border(b, IgDesign.PA(0.14f), 1);
                IgDesign.Radius(b, 7); b.style.marginRight = 6; b.style.marginTop = 6;
                b.style.paddingLeft = 12; b.style.paddingRight = 12; b.style.paddingTop = 7; b.style.paddingBottom = 7;
                dropdown.Add(b);
            }

            var pill = new Button(() =>
                dropdown.style.display = dropdown.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None)
            { text = "☰ SCREENS" };
            IgDesign.ResetButton(pill);
            pill.style.fontSize = 11; pill.style.letterSpacing = 1.3f; pill.style.color = IgDesign.Gold;
            IgDesign.ApplyFont(pill, IgDesign.Sans); pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            pill.style.backgroundColor = IgDesign.C(6, 5, 3, 0.88f); IgDesign.Border(pill, IgDesign.PA(0.30f), 1);
            IgDesign.Radius(pill, 999); pill.style.paddingLeft = 20; pill.style.paddingRight = 20;
            pill.style.paddingTop = 7; pill.style.paddingBottom = 7;

            wrap.Add(pill); wrap.Add(dropdown);
            canvas.Add(wrap);
        }
    }
}

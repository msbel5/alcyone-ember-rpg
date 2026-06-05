using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Adapters;
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
        private IDialogSource _activeDialogSource;   // live NPC conversation behind the redesigned DialogView
        private DialogView _activeDialog;
        private bool _dialogAsked;                   // poll the async reply only once a topic has been picked
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

            // Free the cursor + pause the world whenever a screen is open; restore FPS capture + the clock when
            // everything closes. (Set on transition so we don't fight the first-person controller every frame.)
            bool open = IsAnyOpen();
            AnyScreenOpen = open;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                // Fully-qualified: this file has both `using UnityEngine` and `using UnityEngine.UIElements`,
                // and UIElements also defines a `Cursor` type (CS0104 ambiguity otherwise).
                UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
                UnityEngine.Cursor.visible = open;
                Time.timeScale = open ? 0f : 1f;
            }

            // Stream the NPC's async reply into the open DialogView (the off-thread LLM resolves even at
            // timeScale 0; Update still runs). Only after a topic is picked, so the greeting isn't overwritten.
            if (_activeDialog != null && _activeDialogSource != null && _dialogAsked)
                _activeDialog.SetResponseLine(_activeDialogSource.GetCurrentLine());

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

            _hud.Refresh(in d);
        }

        // Every in-game screen, opened by id. One modal at a time: CloseScreen() drops any open IgModal overlay
        // first. The HUD buttons (I/C/M/J/K/DM) + the ☰ screen browser route here.
        private void OpenScreen(string screenId)
        {
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
                    new CharacterView(c, CloseScreen, OpenScreen);
                    break;
                case "spellbook":
                    RefreshLiveSpells();
                    new SpellbookView(c, CloseScreen, TodoSpellbookAction);
                    break;
                case "journal":   new JournalView(c, CloseScreen); break;
                case "worldmap":
                    new WorldMapView(c, CloseScreen, TodoFastTravelAction);
                    break;
                case "colony":
                    RefreshLiveColony();
                    new ColonyView(c, CloseScreen, TodoColonyTaskAction);
                    break;
                case "consul":
                    new ConsulFateView(c, CloseScreen, TodoConsulAskAction);
                    break;
                case "dialog":    new DialogView(c, CloseScreen); break;
                case "combat":
                    RefreshLiveSpells();
                    new CombatView(c, CloseScreen, TodoCombatAction, TodoCombatFleeAction);
                    break;
                case "loot":
                    RefreshLiveInventory();
                    new LootView(c, CloseScreen, TodoTakeAllLootAction);
                    break;
                case "trade":
                    RefreshLiveInventory();
                    new TradeView(c, CloseScreen, TodoTradeAction);
                    break;
                case "crafting":
                    new CraftingView(c, CloseScreen, TodoCraftAction);
                    break;
                case "pause":
                    new PauseView(c, CloseScreen, OpenScreen, TodoSettingsAction, TodoMainMenuAction);
                    break;
                case "levelup":
                    RefreshLiveSpells();
                    new LevelUpView(c, CloseScreen, TodoConfirmLevelUpAction);
                    break;
                case "death":
                    new DeathView(c, CloseScreen, TodoLoadLastSaveAction, TodoMainMenuAction);
                    break;
                case "savegame":
                    new SaveLoadView(c, CloseScreen, TodoSaveSlotAction, TodoLoadSlotAction);
                    break;
            }
            // Every view calls stageCanvas.Add(_overlay) exactly once, so the newly-added last child IS this
            // screen's overlay — whether IgModal-based ("IgModalOverlay") or a bare VisualElement (Pause/Combat/
            // LevelUp/Death/Dialog/DM/Loot). Tracking the element itself is what lets CloseScreen + IsAnyOpen
            // handle ALL screens, so Esc closes them and the cursor/pause toggle fires correctly.
            _activeScreen = c.childCount > before ? c.ElementAt(c.childCount - 1) : null;
        }

        private void CloseScreen()
        {
            // End any live NPC conversation FIRST so a late async reply can't bleed into the next one (bumps the
            // conversation serial) — covers every close path: Farewell, Esc, the X, or opening another screen.
            if (_activeDialogSource != null)
            {
                _activeDialogSource.EndConversation();
                _activeDialogSource = null; _activeDialog = null; _dialogAsked = false;
            }
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
            _dialogAsked = false;
            _activeDialog = new DialogView(
                c, CloseScreen,
                string.IsNullOrEmpty(npcName) ? "Stranger" : npcName,
                portrait,
                src.GetCurrentLine(),
                topics,
                id => { _dialogAsked = true; src.SelectTopic(id); },
                CloseScreen);
            _activeScreen = c.childCount > before ? c.ElementAt(c.childCount - 1) : null;
        }

        private void ConsulDm() => OpenScreen("consul");

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
            IgMockData.Inventory = IgMockData.DefaultInventory;
            IgMockData.EquipSlots = IgMockData.DefaultEquipSlots;
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
            IgMockData.SpellBar = IgMockData.DefaultSpellBar;
            IgMockData.SpellSchools = IgMockData.DefaultSpellSchools;
            if (!(_host is ISpellBarSource spellSrc)) return;

            var slots = spellSrc.GetSlots();
            if (slots == null) return;

            int slotCount = Mathf.Max(IgMockData.DefaultSpellBar.Length, slots.Count);
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
            IgMockData.ColonyNpcs = IgMockData.DefaultColonyNpcs;

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
                var fallback = FindDefaultNpc(npc.Name);
                live.Add(new ColonyNpcData(
                    npc.Name,
                    string.IsNullOrWhiteSpace(npc.Role) ? fallback != null ? fallback.Role : "Colonist" : npc.Role,
                    fallback != null ? fallback.Hp : 0,
                    fallback != null ? fallback.HpMax : 0,
                    npc.Needs ?? (fallback != null ? fallback.Needs : Array.Empty<NeedData>()),
                    string.IsNullOrWhiteSpace(npc.Mood) ? fallback != null ? fallback.Mood : "Unknown" : npc.Mood,
                    string.IsNullOrWhiteSpace(npc.Task) ? fallback != null ? fallback.Task : "Idle" : npc.Task));
            }

            live.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            IgMockData.ColonyNpcs = live.ToArray();
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

        private static ColonyNpcData FindDefaultNpc(string actorName)
        {
            for (int i = 0; i < IgMockData.DefaultColonyNpcs.Length; i++)
            {
                var npc = IgMockData.DefaultColonyNpcs[i];
                if (string.Equals(npc.Name, actorName, StringComparison.Ordinal))
                    return npc;
            }
            return null;
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

        // TODO(host-action): human wires real save/load/quit/combat.
        private void TodoSettingsAction() => LogTodoAndClose("open settings");
        private void TodoMainMenuAction() => LogTodoAndClose("return to main menu");
        private void TodoLoadLastSaveAction() => LogTodoAndClose("load last save");
        private void TodoCombatFleeAction() => LogTodoAndClose("combat flee");
        private void TodoTakeAllLootAction() => LogTodoAndClose("take all loot");
        private void TodoConfirmLevelUpAction() => LogTodoAndClose("confirm level up");
        private void TodoInventoryAction(string actionId) => LogTodoAndClose("inventory action: " + actionId);
        private void TodoSpellbookAction(string spellName) => LogTodoAndClose("spell action: " + spellName);
        private void TodoTradeAction(string actionId) => LogTodoAndClose("trade action: " + actionId);
        private void TodoCraftAction(string recipeId) => LogTodoAndClose("craft recipe: " + recipeId);
        private void TodoFastTravelAction(string locationId) => LogTodoAndClose("fast travel: " + locationId);
        private void TodoConsulAskAction(string prompt) => LogTodoAndClose("consult fate: " + prompt);
        private void TodoCombatAction(string actionId) => LogTodoAndClose("combat action: " + actionId);
        private void TodoSaveSlotAction(int slotNumber) => LogTodoAndClose("save slot " + slotNumber);
        private void TodoLoadSlotAction(int slotNumber) => LogTodoAndClose("load slot " + slotNumber);
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
            if (EmberInput.KeyDown(KeyCode.Tab))    { ToggleBrowser(); return; }
            if (EmberInput.KeyDown(KeyCode.Escape)) { if (IsAnyOpen()) CloseAll(); else OpenScreen("pause"); return; }
            if (EmberInput.KeyDown(KeyCode.C))      OpenScreen("character");
            else if (EmberInput.KeyDown(KeyCode.I)) OpenScreen("inventory");
            else if (EmberInput.KeyDown(KeyCode.M)) OpenScreen("worldmap");
            else if (EmberInput.KeyDown(KeyCode.J)) OpenScreen("journal");
            else if (EmberInput.KeyDown(KeyCode.K)) OpenScreen("colony");
            else if (EmberInput.KeyDown(KeyCode.R)) OpenScreen("consul");
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
        public void ProofOpenScreen(string id) => OpenScreen(id);

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

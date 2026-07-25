# 18-ui-input

## HLD - Ne ve Neden (5-10 cumle)

UI + Girdi sistemi, oyunun ekran katmanı (HUD + 16 modal ekran + ☰ browser + Options çadırı) ile klavye/fare girdilerini tek bir sahiplik zinciri altında toplar. Sahiplik zinciri şudur: `EmberInput` (semantik cephe) → `EmberInputActions` (Unity Input System Action Map) → `EmberInputHardware` (KeyCode → InputSystem.Key köprüsü) → `InGameUiController.HandleScreenInput` (in-game ekran hotkey'leri) → `EmberWorldHost.Update` + `IsModalOpen()` (gameplay hareketi/etkileşimi kim yiyor). Ne zaman `InGameUiController.OwnsInput=true` ise, `EmberWorldHost` legacy tuş dallarını (Tab/M/C/I/K/R/RegenWorld/ToggleColony/Escape-hold-quit) es geçer; bu, iki katmanın aynı frame'de aynı tuşa iki kez tepki vermesini engelleyen sözleşme. Modal açıksa ya da bir TextField odaklıysa, `AnyScreenOpen` veya `TypingFocused` static flag'ı `EmberWorldHost.IsModalOpen()` içine kaynar — böylece FPS look/move (`EmberFirstPersonController`), melee/spell (`EmberPlayerMeleeSwing`, `EmberPlayerSpellCaster`) ve interact raycaster hepsi aynı anda susar. W31'de eklenen `TypingFocused` bug'ı kapatmıştır: Oracle'ın "Ask" kutusuna "r" yazınca Oracle ekranı yeniden açılıyordu. W36 B30'da ise Options ekranındaki `KeybindsSection` cheatsheet'i, `HandleScreenInput` içindeki hard-coded switch ile `KeybindsSectionTruthTests` üzerinden pinlendi — kılavuz artık yalanlayamaz, dinamik "Input" tab'ı ise legacy path olarak açıkça etiketlendi (`(legacy — inactive in-game)`). Options ekranı `IOptionsSection` üzerinden yansıma ile keşfedilir (`OptionsSectionRegistry.Discover`) ve üç canlı sekme (`SettingsSection@10`, `AudioDisplaySection@20`, `KeybindsSection@30`) barındırır. Kısacası: bu sistem "kim tuşları duyar, kim ekranı çizer, kim world'ü durdurur" kararının tek doğrusudur; her başka sistem (kamera, savaş, etkileşim, ambient ses, dialog) bu tek doğruya `IsModalOpen()` / `AnyScreenOpen` / `OwnsInput` sorusu ile bağlanır.

## HLD - Akış (numaralı adımlar)

1. **Bootstrap:** `EmberWorldHost.Awake` sona doğru `EnsureInGameUi()` çağırır (`EmberWorldHost.Ui.HudInventory.cs:56`). Bu, sahnede yoksa yeni bir GameObject üzerinde `InGameUiController` bileşenini oluşturur.
2. **Controller açılışı:** `InGameUiController.OnEnable → OwnsInput = true`. `Awake` içinde kendi `UIDocument`'ini mount eder (sortingOrder=100, `pickingMode=Ignore`), `InGameStage` (1920×1080 self-scaling canvas) + `WorldHudView` + `BuildScreenBrowser` (☰ pill) kurar. Legacy `EmberHud`, `EventLogHudPanel`, `PauseMenu` bileşenleri `SetActive(false)` yapılır — yeni HUD onların üstüne render edildiği için birlikte var olamazlar.
3. **Frame başı - state güncelleme (`Update`):** `bool open = IsAnyOpen()` (aktif screen, ☰ dropdown veya IgModalOverlay); `AnyScreenOpen = open`, `TypingFocused = IsTextInputFocused()` (odak zinciri `TextField`'e mi düşüyor). `open != _wasOpen` transition'ında `UnityEngine.Cursor.lockState`/`.visible` toggle edilir; `Time.timeScale = open && !conversationOpen ? 0f : 1f` (dialog/oracle açıkken pause YOK — LLM ana thread'i drain edebilsin diye).
4. **Hotkey girdisi (`HandleScreenInput`):** `EmberInput.KeyDown(KeyCode.Escape)` her koşulda dinlenir — açık ekran varsa `CloseAll` yoksa `OpenScreen("pause")`. `typing` ise başka hiçbir harf tuşu okunmaz (W31 gate). Aksi halde Tab → `ToggleBrowser`, C/I/M/J/K/R/B → `OpenScreen(...)`, T/H → `BeginTimeSkip(rest:false/true)`.
5. **Ekran aç/kapa:** `OpenScreen(screenId)` önce `CloseScreen` + `CloseBrowser`, sonra `RefreshLive*` (Player/Inventory/Spells/Colony/Journal/Trade/Crafting/SaveLoad) ile `IgMockData` snapshot'ını canlı verilerle doldurup ilgili View sınıfını (`InventoryView`, `CharacterView`, …, `DeathView`, `SaveLoadView`) canvas'a ekler. Overlay son child olarak takılır → `_activeScreen` = son child. `CloseScreen` `_activeDialogSource.EndConversation()` çağırır (conversation serial'ı artırır, geç LLM cevabı düşer).
6. **Legacy köprü (`EmberWorldHost.Input.cs`):** Her legacy tuş branch'i `if (…&& !InGameUiController.OwnsInput)` ile korunur. `internal static bool IsModalOpen()` → `WorldHostInputPolicy.IsModalOpen() || InGameUiController.AnyScreenOpen || InGameUiController.TypingFocused` — üçünden biri true ise gameplay girdisi susar.
7. **Consumer'lar:** `EmberFirstPersonController.Update`, `EmberPlayerMeleeSwing.Update`, `EmberPlayerSpellCaster.Update`, `EmberPlayerInteractRaycaster.Update` ve `AmbientVoiceDirector` (sadece `AnyScreenOpen`) hepsi bu gate'i sorar; sorusuz hiç kimse kamera/melee/spell/interact tetiklemez.
8. **Options tent'i:** `PauseMenu.OpenOptions` (uGUI world) `OptionsScreen.Open(this)` çağırır. `BuildOnce` bir kez çalışır, `OptionsSectionRegistry.Discover` yansıma ile `IOptionsSection` implementer'ları tarar (concrete + parametersiz ctor); `Order` + `Title` ile sıralar (`Settings@10 → Audio & Display@20 → Keybinds@30`). Sol raile buton, sağ mount'a section body basılır. Update her frame `EmberInput.PauseDown` ile Esc'ye tepki verir → `Close()`.
9. **Keybind sözleşmesi (W36 B30):** `KeybindsSection.Bindings` `InGameUiController.HandleScreenInput`'in gerçek switch'ini mirror'lar. `SettingsSection.Fields.BuildInput` içindeki üç action-map path'i `(legacy — inactive in-game)` etiketi ile açıkça markalandı — çünkü rebindable yol yalnızca `!InGameUiController.OwnsInput` iken çalışan legacy consumer'lar tarafından okunur.
10. **Proof/diagnostik dışa açılan yüz:** `ProofOpenScreen(id)`, `ProofCloseScreens()`, `ProofToggleBrowser()`, `OptionsScreen.ProofShowSection(title)` — screenshot driver bunları yem olarak kullanır (headless UI capture çünkü cursor kilitli, tıklanamaz).

## LLD - Veri Modeli (file:line)

- `InGameUiController.OwnsInput : static bool` — `Assets/Scripts/Presentation/Ember/UI/InGame/InGameUiController.cs:56`
- `InGameUiController.AnyScreenOpen : static bool` — `InGameUiController.cs:62`
- `InGameUiController.TypingFocused : static bool` — `InGameUiController.cs:63`
- `InGameUiController._hud : WorldHudView` — `InGameUiController.cs:32`
- `InGameUiController._stage : InGameStage` — `InGameUiController.cs:33`
- `InGameUiController._host : object` — `InGameUiController.cs:34` (EmberWorldHost, adapter-source cast'leri için)
- `InGameUiController._dropdown : VisualElement` — `InGameUiController.cs:35` (☰ browser)
- `InGameUiController._activeScreen : VisualElement` — `InGameUiController.cs:36` (son eklenen modal overlay)
- `InGameUiController._activeCharacter/_activeDialog/_activeOracle/_activeTrade/_activeCrafting/_activeSaveLoad/_activeCombat` — `InGameUiController.cs:39-49` (poll/streaming için tutulan view referansları)
- `InGameUiController._activeDialogSource : IDialogSource` — `InGameUiController.cs:41` (streaming LLM cevabı, portrait poll)
- `InGameUiController._activePlayerPortraitKey / _playerPortraitVersion` — `InGameUiController.cs:40,42` (portrait handoff)
- `InGameUiController._wasOpen : bool` — `InGameUiController.cs:52` (cursor toggle edge trigger)
- `InGameUiController._deathScreenShown / _levelUpPrompted : bool` — `InGameUiController.cs:54,55` (bir kez açma gate'leri)
- `InGameUiController._hurtFlash : VisualElement, _hurtFlashUntil : float` — `InGameUiController.cs:1348,1349`
- `InGameUiController._speechCheckDone : bool` — `InGameUiController.cs:1347`
- `InGameUiController._timeSkipRunning : bool` — `TimeSkipRoutine` state (BeginTimeSkip yakınında)
- `InGameUiController.AllScreens : static (string id, string label)[]` — `InGameUiController.cs:1595` (16 kayıt: inventory/character/spellbook/journal/worldmap/colony/consul/dialog/combat/loot/trade/crafting/pause/levelup/death/savegame)
- `InGameStage.Canvas : VisualElement` — `Assets/Scripts/Presentation/Ember/UI/InGame/InGameDesign.cs:311` (1920×1080 tasarım yüzeyi)
- `InGameStage.Fit()` — `InGameDesign.cs:328` (scale + left/top per-frame)
- `IgModal.Build(...) → (VisualElement overlay, VisualElement panel, VisualElement content)` — `Assets/Scripts/Presentation/Ember/UI/InGame/IgModal.cs:15` (16 ekran için ortak çerçeve)
- `WorldHudView.OnOpenScreen : Action<string>, OnConsulDm : Action` — `Assets/Scripts/Presentation/Ember/UI/InGame/WorldHudView.cs:32,33` (HUD butonlarının hotkey ikizleri)
- `WorldHudData` — `WorldHudView.cs:10` (Hp/Fatigue/Mana/SpellSlots/Location/EnemyName/Compass/Delve/EventLine)
- `EmberInput` (static facade) — `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs:10`
- `EmberInput._actions : EmberInputActions` — `EmberInput.cs:12` (SettingsSection.Fields yansıma ile null'lar → ReloadInputs)
- `EmberInputActions` (Input System action map) — `Assets/Scripts/Presentation/Ember/Inputs/EmberInputActions.cs:8` — Move/Look/Jump/Sprint/Interact/ToggleCursor/RegenWorld/ToggleInventory/ToggleColonyPanels/SaveQuick/LoadQuick/Pause/Attack/Secondary/MeleeSwing
- `EmberInputHardware` (KeyCode → InputSystem.Key köprüsü) — `Assets/Scripts/Presentation/Ember/Inputs/EmberInputHardware.cs:8`
- `InputRuntimeOptions` (path'ler + LookSmoothingAlpha + NumberSlots + FunctionSlots) — `Assets/Scripts/Domain/Configuration/EmberRuntimeOptions.cs:44`
- `RuntimePlayerSettings.MusicVolume / SfxVolume / MouseSensitivity : static float` — `Assets/Scripts/Presentation/Ember/UI/Options/RuntimePlayerSettings.cs:16-19` (PlayerPrefs-backed, world-save DEĞİL)
- `OptionsScreen._sections : IReadOnlyList<IOptionsSection>` — `Assets/Scripts/Presentation/Ember/UI/Options/OptionsScreen.cs:16`
- `OptionsScreen._navFills : List<Image>` — `OptionsScreen.cs:15`
- `OptionsScreen._isBuilt : bool` — `OptionsScreen.cs:23`
- `KeybindsSection.Bindings : static (string key, string action)[]` — `Assets/Scripts/Presentation/Ember/UI/Options/KeybindsSection.cs:28` (21 satır, `KeybindsSectionTruthTests` tarafından pinlenmiş)
- `IOptionsSection.Title / Order / Build(Transform)` — `Assets/Scripts/Presentation/Ember/UI/Options/IOptionsSection.cs:6`

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `void InGameUiController.OnEnable()` — `InGameUiController.cs:65` — `OwnsInput = true` bayrağını kaldırır, böylece legacy `EmberWorldHost` tuş dalları yol verir.
- `void InGameUiController.OnDisable()` — `InGameUiController.cs:66` — Üç static flag'i sıfır + `Time.timeScale` 1'e klemp (asla pause'da bırakma).
- `void InGameUiController.Awake()` — `InGameUiController.cs:71` — Kendi `UIDocument`'ini oluşturur, `InGameStage`+`WorldHudView`+ScreenBrowser'ı kurar, legacy HUD/EventLog/PauseMenu bileşenlerini SetActive(false) yapar.
- `void InGameUiController.Bind(MonoBehaviour host)` — `InGameUiController.cs:108` — `EmberWorldHost` referansını `_host` alanına koyar (adapter-source cast'ler için).
- `void InGameUiController.Update()` — `InGameUiController.cs:112` — Speech-check → opening story → HUD fit → `HandleScreenInput` → gate flag'lerini + cursor + timeScale toggle → dialog/oracle/character/combat live-refresh → HP delta hurt-flash → XP/death gate → HUD.Refresh.
- `void InGameUiController.HandleScreenInput()` — `InGameUiController.cs:1498` — Esc her zaman, aksi halde `typing` guard sonrası Tab/C/I/M/J/K/R/B/T/H switch'i — bu switch, `KeybindsSection`'ın truth'udur.
- `bool InGameUiController.IsTextInputFocused()` — `InGameUiController.cs:1518` — Focus zincirinde `TextField` var mı bakar; W31'in "typing yaparken hotkey tetiklenmesin" gate'i.
- `bool InGameUiController.IsAnyOpen()` — `InGameUiController.cs:1525` — `_activeScreen != null || dropdown flex || Q("IgModalOverlay") != null`.
- `void InGameUiController.OpenScreen(string screenId)` — `InGameUiController.cs:379` — 16-ekranlı switch, önce `CloseScreen/CloseBrowser`, sonra `RefreshLive*` + ilgili View constructor'ı; son child'ı `_activeScreen` diye tutar.
- `void InGameUiController.CloseScreen()` — `InGameUiController.cs:481` — Aktif dialog konuşmasını sonlandırır (`EndConversation` + `StopConversationSpeech`), view referanslarını null'lar, overlay'i `RemoveFromHierarchy`; safety net olarak `IgModalOverlay` isimli residual'ları da temizler.
- `void InGameUiController.OpenNpcDialog(IDialogSource src, string npcName, string portrait, Transform speakerAnchor=null)` — `InGameUiController.cs:499` — Interact raycaster'dan çağrılır: prior konuşmayı kapatır, `DialogView` mount eder, portrait/topic seed'ini yapar.
- `void InGameUiController.ToggleBrowser()` / `CloseBrowser()` / `CloseAll()` — `InGameUiController.cs:1533,1541,1542` — ☰ dropdown display toggle + mutually exclusive with modals.
- `void InGameUiController.ProofOpenScreen(string id)` — `InGameUiController.cs:1546` — Proof driver hook: id ile ekran açar + child count / display / opacity / visible değerlerini bir frame sonra loglar (headless capture diagnostik).
- `void InGameUiController.ProofCloseScreens()` — `InGameUiController.cs:1573` — `CloseAll()` alias'ı.
- `void InGameUiController.ProofToggleBrowser()` — `InGameUiController.cs:377` — Ig-tour ☰ toggle'ı için proxy.
- `void InGameUiController.BeginTimeSkip(bool rest)` — `InGameUiController.cs:1385` — T/H tuşları için: TimeSkipRoutine coroutine'ini başlatır (wait 1h / sleep to dawn), aksi halde noop.
- `void InGameUiController.RefreshLive*` (Player/Inventory/Spells/Colony/Journal/Trade/Crafting/SaveLoad) — `InGameUiController.cs:582,616,637,675,745,…` — `_host` üzerinden ilgili `I*Source` adapter'ını sorup `IgMockData.*` snapshot'ına yazar.
- `void InGameUiController.BuildScreenBrowser(VisualElement canvas)` — `InGameUiController.cs:1605` — ☰ pill + wrap-flex dropdown; 16 buton (`AllScreens` tuple listesi).
- `bool EmberWorldHost.IsModalOpen()` — `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.Input.cs:127` — `WorldHostInputPolicy.IsModalOpen() || AnyScreenOpen || TypingFocused` — TEK modal doğrusu; kamera/melee/spell/interact bunu sorar.
- `void EmberWorldHost.Update()` — `EmberWorldHost.Input.cs:13` — RegenWorld / colony toggle / M / ToggleInventory legacy dalları hepsi `!OwnsInput` guard'lı; `IsModalOpen()` sonucu spell-slot number-key hem'ini kontrol eder.
- `void EmberWorldHost.HandleQuitInput()` — `EmberWorldHost.Input.cs:135` — Escape-hold-quit; `OwnsInput` iken atlanır (yeni PauseView Esc'yi sahiplenir).
- `static bool EmberInput.KeyDown(KeyCode key)` — `Assets/Scripts/Presentation/Ember/Inputs/EmberInput.cs:66` — Doğrudan hardware layer'a düşer (`HandleScreenInput` bu ile çalışır).
- `EmberInput.Move/Look/Sprint/JumpDown/Interact/ToggleCursor/RegenWorld/ToggleInventory/ToggleColonyPanels/SaveQuick/LoadQuick/PauseDown/PauseHeld/AttackClick/SecondaryClick/MeleeSwing` — `EmberInput.cs:15-45` — Semantik property'ler, her biri `Actions.X.WasPressedThisFrame()/IsPressed()/ReadValue()`.
- `int EmberInput.NumberKeyDown()` / `bool NumberKeyDown(int oneBased)` — `EmberInput.cs:47,55` — Slot sayısı `InputRuntimeOptions.NumberSlots`; hardware `KeyCode.Alpha1..9` tarar.
- `void EmberInput.ResetForTests() / EnableForTests()` — `EmberInput.cs:88,95` — `#if UNITY_INCLUDE_TESTS`; `PlayMode/Input/EmberInputContractTests` bunları kullanır.
- `EmberInputActions..ctor(...)` — `EmberInputActions.cs:11` — 15 action'ı `InputRuntimeOptions.*Path`'lerden yaratır; `Move` composite'i primary + optional alt yönler.
- `void EmberInputActions.Enable()/Dispose()` — `EmberInputActions.cs:47,52` — Action map lifecycle.
- `static Key EmberInputHardware.ToInputSystemKey(KeyCode)` — `EmberInputHardware.cs:94` — `KeyCode` ailelerini (A-Z, F1-F12, Alpha0-9, Space/Esc/Tab/Shift/Arrow) `Key.*` denklerine eşler.
- `static bool EmberInputHardware.KeyDown/Key(KeyCode)` — `EmberInputHardware.cs:33,35` — `Keyboard.current[key].wasPressedThisFrame/isPressed`.
- `IReadOnlyList<IOptionsSection> OptionsSectionRegistry.Discover()` — `Assets/Scripts/Presentation/Ember/UI/Options/OptionsSectionRegistry.cs:12` — Bütün AppDomain assembly'lerini tarar, concrete + parametersiz ctor'lu IOptionsSection'ları toplar, `Order` + `Title` ile sıralar.
- `void OptionsScreen.Initialize(TMP_FontAsset, Sprite)` — `OptionsScreen.cs:34` — PauseMenu shared asset'lerini enjekte eder.
- `void OptionsScreen.Open(PauseMenu owner)` — `OptionsScreen.cs:41` — İlk açılışta `BuildOnce`, sonra owner PauseMenu görünmez + animate open.
- `void OptionsScreen.Update()` — `OptionsScreen.cs:52` — `EmberInput.PauseDown` ise `Close()` — B15 fix (legacy `Input.GetKeyDown` her frame throw ediyordu).
- `void OptionsScreen.BuildOnce()` — `OptionsScreen.cs:69` — Frame + title + BACK + nav rail + content mount; `LayoutNav` + `BuildSections`.
- `void OptionsScreen.BuildSections()` — `OptionsScreen.cs:100` — Her section için nav butonu ekler, 0. section'ı gösterir.
- `bool OptionsScreen.ProofShowSection(string title)` — `OptionsScreen.cs:118` — İg-tour proof hook; başlığa göre section seçer.
- `void OptionsScreen.ShowSection(int index)` — `OptionsScreen.cs:128` — Content mount'u temizler, seçili nav fill rengini yükseltir, `section.Build(_contentMount)`.
- `void SettingsSection.Build(Transform)` — `Assets/Scripts/Presentation/Ember/UI/Options/SettingsSection.cs:24` — Scroll altında `BuildWorld/BuildInput/BuildTiming` üç grup.
- `void SettingsSection.BuildInput(Transform)` — `SettingsSection.Fields.cs:34` — 14 satır: 8 aktif path + 3 legacy `(legacy — inactive in-game)` etiketli path + LookSmoothingAlpha + NumberSlots + FunctionSlots.
- `static void SettingsSection.ReloadInputs()` — `SettingsSection.Fields.cs:131` — `EmberInput._actions` alanını yansıma ile null'lar → sonraki erişimde yeni path'lerle yeniden inşa.
- `void KeybindsSection.Build(Transform contentMount)` — `KeybindsSection.cs:54` — Statik `Bindings` listesini iki kolon (key, action) label'lara basar.
- `void AudioDisplaySection.Build(Transform)` — `AudioDisplaySection.cs:26` — Music/Sfx/MouseSens slider'ları `RuntimePlayerSettings` static'lerine yazar + `Save()` çağırır.
- `static void RuntimePlayerSettings.Save()` — `RuntimePlayerSettings.cs:22` — Üç PlayerPref key'ini yazar (`ember_music_vol`, `ember_sfx_vol`, `ember_mouse_sens`).

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Not: `FieldOwnershipRegistry` domain simülasyonu için yazılmış single-writer-per-field ledger'ıdır (Actor.Position, World.Reservations, vb.). Presentation'daki UI/Input state'i orada kayıtlı DEĞİL (mimari sınır); yine de bu sistemin declared writer sözleşmesi aşağıdadır:

- `Ui.OwnsInput` (bool, static) — writer: `presentation.ui.ingame@lifecycle` (`InGameUiController.OnEnable/OnDisable`).
- `Ui.AnyScreenOpen` (bool, static) — writer: `presentation.ui.ingame@perframe` (`InGameUiController.Update`, per-frame `IsAnyOpen()`).
- `Ui.TypingFocused` (bool, static) — writer: `presentation.ui.ingame@perframe` (`InGameUiController.Update`, per-frame `IsTextInputFocused()`).
- `Ui.ActiveScreen` (VisualElement, instance) — writer: `presentation.ui.ingame@openclose` (`OpenScreen`/`CloseScreen`).
- `Ui.CursorLockState` + `Ui.CursorVisible` — writer: `presentation.ui.ingame@edge` (yalnızca `open != _wasOpen` transition'ında).
- `Time.timeScale` — writer: `presentation.ui.ingame@perframe` (conversation açıksa 1, diğer screen açıksa 0, hiçbir screen açık değilse 1). NOT: `EmberProofScreenshotDriver` + test harness'ı da timeScale'e dokunabilir — declared multi-writer, ama koşullar ayrık (proof modu ile in-game modu aynı anda çalışmaz).
- `Input.SmoothedLook` (Vector2, static) — writer: `EmberInput` + `EmberInputHardware` (iki nüsha, EmberInput cache'i tercih edilir).
- `PlayerPrefs.ember_music_vol / ember_sfx_vol / ember_mouse_sens` — writer: `presentation.ui.options.audio@usercommit` (`AudioDisplaySection` slider onEndEdit → `RuntimePlayerSettings.Save()`).
- `EmberRuntimeOptionsProvider.Current.Input.*Path` — writer: `presentation.ui.options.settings@usercommit` (`SettingsSection.CommitBinding` → `Apply(...)` → `ReloadInputs()` → `EmberInput._actions=null`).

Okunan alanlar (bu sistem):
- `EmberRuntimeOptionsProvider.Current.Input.*` — action map yeniden inşasında + `NumberSlots`/`FunctionSlots` tarayışında.
- `EmberRuntimeOptionsProvider.Current.WorldHost.*` — `ShowQuestGuidance`, `ShowQuestCompass`, `SpellSlotCount`, `Fate*Seconds`, `EscapeHoldQuitSeconds`.
- `EmberDomainAdapterLocator.Current` (as `DomainSimulationAdapter`) — LevelUpReady, RespawnAfterDeath, WaitHours/ApplyRest, TickHostileAi/TickWorldEncounter, ConsumePendingFateFollowups.
- `WorldDirector.ScreenRequestSignal.Consume()` — dünya prop'ları (shop counter) ekran açma isteği.
- `WorldDirector.RuntimeBattleMirror.Active` — canlı encounter mirror'ının açık olması gate'i (HUD enemy paneli permanent kalmasın diye).
- `WorldEncounterSignal.Consume()` — outlaw draw-steel event; artık pause olmuyor (F13 real-time combat).
- `PlayerPortraitHandoff.Version` — portrait yenileme.

## LLD - Ürettiği/Tükettiği Olaylar

Ürettiği olaylar:
- `WorldDirector.RuntimeAudioDirector.PlayUiClick()` — her `OpenScreen()` çağrısında (F3/audio).
- `SpeechDirector.FeedPartial/FeedFinal(voiceKey, line)` — dialog view açıkken NPC line'ı ses olarak (thinking iken partial, tamamlandığında final).
- `SpeechDirector.StopConversationSpeech()` — `CloseScreen` içinde konuşma bittiğinde.
- `SpeechDirector.SetSpeakerAnchor(voiceKey, transform)` — `OpenNpcDialog` içinde 3D anchor set eder.
- `DomainSimulationAdapter.SpeakPlayerQuestion(text)` — Oracle'a soru sorulunca DM'in oyuncuyu duyması.
- `PlayerCommandSink.TryMeleeStrike/TryCastSpell/LogCombat` — CombatView butonları basıldığında.
- `IDialogSource.EndConversation()` — konuşma bittiğinde (in-flight LLM cevabı düşer).
- Debug.Log: `[InGameUI] World HUD mounted…`, `[InGameUI] world encounter signal consumed…`, `[InGameUI] XP threshold crossed…`, `[ProofUI] open '<id>': stage=… canvas N->M…`, `[Options] discovered N sections…`.

Tükettiği sinyaller/olaylar:
- `UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame` — opening story dismiss (`ActiveOpeningDismiss`).
- `DomainSimulationAdapter.JustCreatedWorld` — opening story tetikleyicisi (tek shot).
- `WorldEncounterSignal.Consume()` — outlaw encounter (LIVE combat; artık modal açmıyor).
- `WorldDirector.ScreenRequestSignal.Consume()` — prop-driven screen request.
- `DomainSimulationAdapter.LevelUpReady` — XP eşiğini geçti sinyali (levelup ekranı bir kez aç).
- HP delta (`_lastHpSeen > 0 && s.Health < _lastHpSeen`) — hurt-flash tetikleyici (F13 buyer feel).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Ui/KeybindsSectionTruthTests.cs` — **W36 B30 pin**: 3 test. (a) `Bindings_CoverEveryScreenHotkeyInHandleScreenInputSwitch` — `HandleScreenInput` içindeki 11 tuşun (Tab/I/M/J/K/C/R/B/T/H/Esc) her biri için `KeybindsSection.Bindings`'te satır zorunlu. (b) `Bindings_DoNotClaimTabOpensInventory` — Tab satırının "inventory" içermediği regresyon guard'ı (eski cheatsheet'in yalanı). (c) `Bindings_HaveNoBlankKeyOrAction` — boş kayıt yok.
- `Assets/Tests/EditMode/Presentation/WorldHostInputPolicyTests.cs` — 4 test: EscapeHold modal iken sıfırlanır, threshold'da quit tetikler; `ResolveSelectedSpellSlot` modal iken slot seçimini korur; `StepFateTimer` süresi dolunca callback fire eder.
- `Assets/Tests/PlayMode/Input/EmberInputContractTests.cs` — Input System-backed contract: `IdleFrame_ReturnsNeutralValues`, `MovementAndLook_ReadDeviceState`, `SemanticButtons_MatchLegacyFacadeContract` gibi 168 satırlık davranış paketi (`InputTestFixture` üzerinden fake Keyboard/Mouse).

W32-W35 pin şu an sadece dolaylı: `InGameUiController`'in kendisi için doğrudan EditMode test yok — davranış hikayesi (Escape her koşulda kapatır, typing hotkey'i yer, screen open cursor free/pause on) proof harness üzerinden video-esque yakalanıyor (`EmberProofScreenshotDriver` legleri). Bilinen borç: `InGameUiController.HandleScreenInput` mantığı için ayrılmış pure-logic testi yok, sadece `KeybindsSection`'ın "sözleşmeye uyduğunu" pinliyor.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W31 - TypingFocused doğdu** (git: `1e9b474b W31: fix(soul) — eight live wounds`): `IsTextInputFocused()` metodu + `TypingFocused` static flag `EmberWorldHost.IsModalOpen()`'e kaynatıldı. Oracle "Ask" kutusuna "r" yazınca Oracle'ın yeniden açılma bug'ı kapandı. Tüm harf hotkey'leri `if (typing) return;` guard'ı arkasına alındı; Escape bu gate'in dışında (her koşulda kapatabilmeli).
- **W31 - Modal gate combat key'lerini örttü**: `IsAnyOpen()` sadece `_activeScreen` değil, `dropdown flex`, `IgModalOverlay` residual'ını da tarar hale geldi. `AnyScreenOpen` bayrağı `EmberFirstPersonController` (kamera look/move) + `EmberPlayerMeleeSwing` + `EmberPlayerSpellCaster` + `EmberPlayerInteractRaycaster`'ın `IsModalOpen()` sorusuna cevap oldu.
- **W31 - Forge screen kısayolu ("B")** eklendi: `HandleScreenInput` içinde `OpenScreen("crafting")`.
- **W31 - Wait/Rest kısayolları ("T"/"H")**: `BeginTimeSkip(rest:false/true)` + coroutine (`TimeSkipRoutine`) → adapter `WaitHours(1)` / `ApplyRest(hours)`; `_timeSkipRunning` gate ile double-fire önlendi.
- **W32-pre - B15 storm fix** (git: `79b707d8 W32-pre: fix(perf)+fix(input) — 'fast travel crash'`): `OptionsScreen.Update` içindeki legacy `Input.GetKeyDown` her frame throw ediyordu (project Input System-only) — `EmberInput.PauseDown` ile değişti.
- **W36 B30 - Keybind unification** (git: `f6c9e2d0 W36: feat(tail)`): `KeybindsSection` satırları `HandleScreenInput`'in gerçek switch'iyle hizalandı (I=Inventory, Tab=Browser — eski cheatsheet Tab=Inventory yalanını taşıyordu; B/T/H eklendi). `SettingsSection.Fields.BuildInput` içindeki 3 legacy path (`RegenWorld`, `ToggleInventory`, `ToggleColony`) `(legacy — inactive in-game)` etiketi aldı — yorumda "gerçek in-game binding'ler `InGameUiController.HandleScreenInput` içinde hard-coded KeyCode branch'leri" not düşüldü. `KeybindsSectionTruthTests` yansımayla `Bindings` field'ını okuyup pinledi.
- **B25 hâlâ açık borç** (git: memory 19724 `Three-Universe Keybind Divergence`): `SettingsSection.BuildInput`'un yazdığı `InputRuntimeOptions.*Path` yolları yalnızca `!InGameUiController.OwnsInput` iken çalışan legacy consumer'lar için gerçek. In-game'de rebindable input INERT — `HandleScreenInput` switch'i hard-coded `KeyCode`. B30 bunu şeffaf yaptı, çözmedi.

## Bilinen Borçlar + Kaçak Kapıları

- **B25 - Üç-evren keybind divergence** (açık): `KeybindsSection` cheatsheet'i (statik string listesi) ↔ `InGameUiController.HandleScreenInput` switch'i (hard-coded `KeyCode.C`/`I`/`M`/…) ↔ `EmberInputActions` action map (`InputRuntimeOptions.*Path`, yalnızca legacy path'te çalışır). Bir tuşu Options'tan yeniden bağlamak in-game davranışı DEĞİŞTİRMEZ; sadece W36-öncesi legacy consumer'ları etkiler (F26 shop counter interact, RegenWorld, ToggleColony, ToggleInventory eski uGUI toggle'ı). Gerçek çözüm: `HandleScreenInput`'i `EmberInputActions` üzerinden semantik action'lara döndürmek (yeni action'lar: `OpenInventory`, `OpenMap`, `OpenJournal`, `OpenColony`, `OpenConsul`, `OpenCrafting`, `OpenCharacter`, `WaitHour`, `SleepDawn`, `ToggleBrowser`) ve `InputRuntimeOptions`'a bunlar için path'ler eklemek. Şimdilik pinlenen tek şey B30 truth-up.
- **`AllScreens` listesi ile OpenScreen switch'i ikizidir** — biri güncellenip diğeri unutulursa ☰ browser ölü butona yol açar. Test yok (kaçak kapı).
- **`Time.timeScale` çift yazımı**: `InGameUiController.Update` her frame yazar (`open && !conversationOpen ? 0f : 1f`). `EmberProofScreenshotDriver` legleri de timeScale'e yazar (`--ember-proof-screenshots` modu). Deklare değil; şimdiye kadar koşullar ayrık ama race bir gün sırıtabilir.
- **Cursor state çift yazımı**: `InGameUiController` edge-triggered olarak yazar (`open != _wasOpen`); `DialogBoxPanel.Render.cs` de `DialogCursorPolicy` üzerinden dialog kapanışında yazar. Legacy dialog kapatıldığında yeni InGameUi cursor'unu resetleyebilir — tespit edilmemiş potansiyel jitter.
- **Legacy uGUI HUD "retired" ama silinmedi**: `EmberHud`, `EventLogHudPanel`, `PauseMenu`, `DialogBoxPanel` sahnede hâlâ oluşuyor, sadece `SetActive(false)` yapılıyor. Kod ölü ama build ağırlığı ve refactor tuzağı olarak duruyor.
- **`InGameUiController` 1648 satır** — dosya başında "intentionally long" notu var; gerçek uzun-vadeli plan her ekranı kendi controller'ına ayırmak. Şu an `_activeCharacter/_activeDialog/_activeOracle/_activeTrade/_activeCrafting/_activeSaveLoad/_activeCombat` tek dev switch içinde çünkü her frame poll edip live-refresh yapıyorlar.
- **`RuntimePlayerSettings` static field-init** race'i: `PlayerPrefs.GetFloat` static ctor'da çalışır — Editor domain reload'da yeniden başlar ama runtime save'den önce Options açılırsa cache'lenen değer eski PlayerPrefs olur. Küçük risk, save() güvenli.
- **Proof hook'ları `public` yüzey**: `ProofOpenScreen`, `ProofCloseScreens`, `ProofToggleBrowser`, `OptionsScreen.ProofShowSection` üretime open API olarak sızabilir. `#if UNITY_INCLUDE_TESTS` altında değil çünkü proof driver runtime; dokümente kaçak kapı.
- **`EmberInputHardware.ToInputSystemKey` eksik map**: `KeyCode.LeftControl`/`RightControl`, `KeyCode.LeftAlt`/`RightAlt`, `KeyCode.Return`, `KeyCode.Backspace`, `KeyCode.Delete` map'lenmemiş → `Key.None` dönüyor. Bu KeyCode'lardan biri `HandleScreenInput`'e eklenirse sessizce ölür.
- **`EmberInput._actions` yansıma ile null'lanır** (`SettingsSection.Fields.ReloadInputs`): action map re-init olurken hız kritik input frame'inde eski action referansı dispose edilirken erişilirse `ObjectDisposedException` teorik olarak fırlayabilir; koruma yok.

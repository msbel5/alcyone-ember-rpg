# 18-ui-input

> Kapsam: UI + girdi — `InGameUiController` (16 ekranli UI-Toolkit orkestratoru), ekranlar
> (envanter / karakter / journal / harita / oracle / ...), keybind haritasi (E/F/M/T/H/C/...),
> input focus/gating (OwnsInput, IsModalOpen, text-focus bastirma), HUD (WorldHudView + legacy
> uGUI kusagi). Her iddia `file:line` ile kanitlidir; emin olunamayanlar "dogrulanmadi" isaretli.

## HLD - Ne ve Neden

UI katmani iki kusakli bir gecis halindedir: eski uGUI kusagi (EmberHud aksiyon-baru,
PauseMenu, EventLogHudPanel, DialogBoxPanel, InventoryGrid, koloni panelleri) ve yeniden
tasarlanan UI-Toolkit "in-game UI" (InGameUiController + 1920x1080 InGameStage + WorldHudView
+ IgModal icinde 16 ekran). Yeni kusak `EmberWorldHost.EnsureInGameUi` tarafindan sahneye EN SON
mount edilir (EmberWorldHost.cs:157; EmberWorldHost.Ui.HudInventory.cs:56-65) ve Awake'te eski
HUD'u, event logunu ve eski pause menusunu SetActive(false) ile emekliye ayirir
(InGameUiController.cs:95-104). Girdi tek semantik cepheden akar: `EmberInput` statik facade'i,
com.unity.inputsystem action'larini calisma zamaninda `InputRuntimeOptions` binding-path
string'lerinden kurar (EmberInputActions.cs:11-29) — yani keybind'ler runtime-remap edilebilir
(testle pinli: EmberInputContractTests.cs:122,143). Oyuncuya gorunen etki: FPS dunyada gezerken
alt barda HP/FAT/MP + 5 buyu slotu + I/C/M/J/K/DM butonlari, ustte gun/saat/yerlesim seridi,
pusula seridi ve F13 canli-dovus dusman paneli; harf tuslari modal ekranlari acar, Esc pause
acar/kapatir. Felsefe: (1) "tek girdi sahibi" — yeni denetleyici aktifken legacy tus
isleyicileri `OwnsInput` bayragina bakip susar (EmberWorldHost.Input.cs:17,19,50,56,65);
(2) "yalan gostermeme" — sim'in takip etmedigi gold/level HUD'da gizlidir (WorldHudView.cs:53-54),
mock alanlar sadece domain veri vermeyince kalir (InGameUiController.cs:542-545);
(3) menu-tipi ekranlar dunyayi durdurur (timeScale=0) ama dialog/oracle ekranlari async LLM
cevabi aksin diye dunyayi DURDURMAZ (InGameUiController.cs:143-157).

## HLD - Akis

### A. Mount ve el degistirme (sahne acilisi, bir kez)
1. `EmberWorldHost.Awake` zincirinin sonunda `EnsureInGameUi()` bir `InGameUi` GameObject'i
   yaratir/bulur ve `Bind(this)` ile host'u verir (EmberWorldHost.Ui.HudInventory.cs:56-65;
   InGameUiController.cs:109-112).
2. `InGameUiController.Awake` kendi UIDocument'ini kurar (sortingOrder=100, root picking Ignore),
   `InGameStage` + `WorldHudView` + ekran browser'ini (Tab ile acilan "SCREENS" pill'i) insa eder ve
   legacy EmberHud/EventLogHudPanel/PauseMenu nesnelerini kapatir (InGameUiController.cs:71-106).
3. `OnEnable` ile `OwnsInput=true` — legacy isleyiciler o andan itibaren yield eder;
   `OnDisable` bayraklari sifirlar ve timeScale 0'da birakilmisligi geri alir
   (InGameUiController.cs:64-69).

### B. Frame dongusu (her Update)
1. Acilis hikayesi: yeni dunya yaratildiysa tam-ekran "THE EMBER WAKES" overlay'i; herhangi bir
   tus veya BEGIN kapatir, cursor kilidi gecici serbest kalir (InGameUiController.cs:129-137,
   1235-1285).
2. `_stage.Fit()` — 1920x1080 stage'i ekrana olcekler (her frame; InGameUiController.cs:140;
   InGameDesign.cs:328-336).
3. `HandleScreenInput()` — asagidaki keybind tablosu (InGameUiController.cs:1463-1479).
4. Cursor + pause politikasi: herhangi bir ekran acik → cursor serbest+gorunur; menu-tipi ekran
   acik ve konusma yok → `Time.timeScale=0`, aksi halde 1 (InGameUiController.cs:146-157).
5. Canli dialog akisi: acik DialogView'a her frame `GetCurrentLine()` yazilir, TTS
   `SpeechDirector.FeedPartial/FeedFinal`'a beslenir, portre sprite'i yuklenene dek yeniden
   cozulur (InGameUiController.cs:162-181).
6. Oracle akisi: `_oraclePending` iken `TryConsumeResolvedFate()` poll edilir, gelen kehanet
   ekrana akitilir (InGameUiController.cs:204-213).
7. HUD verisi: `ICombatHudSource.Read()` vitals, `ISpellBarSource.GetSlots()` buyu adlari,
   `IEmberHudSource.GetHudText()` konum seridi, quest/delve pusula satirlari; HP dususu kirmizi
   kenar vinyetini tetikler (InGameUiController.cs:224-325, 235-238, 1318-1345).
8. Ekran-disi pompalar (ekran KAPALIYKEN): `TickHostileAi` + frame-hizli view projeksiyonu +
   `ScreenRequestSignal.Consume` (dukkan tezgahi ekran isteyebilir) (InGameUiController.cs:241-253);
   canli dusman paneli + `TickWorldEncounter` (257-273); XP esigi asilirsa levelup ekrani bir kez
   acilir (277-291); HP<=0 ise death ekrani bir kez acilir (295-301).

### C. Ekran acma/kapama (tus veya buton tetikler)
1. `OpenScreen(id)`: UI klik sesi → `CloseScreen()+CloseBrowser()` (tek modal kurali) →
   `RefreshLive*` ile IgMockData'ya canli veri yazimi → switch ile 16 ekrandan biri kurulur →
   canvas'a eklenen SON cocuk `_activeScreen` olarak izlenir (InGameUiController.cs:369-467).
2. `CloseScreen()`: once canli konusma sonlandirilir (gec gelen async cevap sonraki konusmaya
   sizmasin), tum aktif-ekran referanslari sifirlanir, overlay hiyerarsiden sokulur; guvenlik agi
   olarak kalan "IgModalOverlay" adlilar da sokulur (InGameUiController.cs:469-484).
3. NPC diyalogu ekran browser'indan degil dunyadan acilir: E tusu →
   `EmberPlayerInteractRaycaster.OpenDialog` → `commands.TryInteract` + `GetDialogSource` →
   `inGameUi.OpenNpcDialog(source, ad, portre)` (EmberPlayerInteractRaycaster.cs:59-92, 123-156;
   InGameUiController.cs:488-520). InGameUiController yoksa legacy DialogBoxPanel'e duser
   (EmberPlayerInteractRaycaster.cs:158-166).

### D. Girdi gating zinciri (her frame, birden cok tuketici)
1. `EmberWorldHost.IsModalOpen()` = legacy DialogBoxPanel gorunur MU
   (WorldHostInputPolicy.IsModalOpen, EmberEventSystemPolicy.cs:44-47) VEYA
   `InGameUiController.AnyScreenOpen` (EmberWorldHost.Input.cs:126-132).
2. Bu predicate'e yield edenler: FPS look/move (EmberFirstPersonController.cs:92), buyu cast'i
   (EmberPlayerSpellCaster.cs:64), etkilesim raycaster'i (EmberPlayerInteractRaycaster.cs:49-53),
   buyu-slot secimi (EmberWorldHost.Input.cs:113-117 → WorldHostInputPolicy.cs:76-91),
   Esc-basili-tut-cikis sayaci (WorldHostInputPolicy.cs:49-74).
3. Yazi odagi: bir TextField odaktayken harf kisayollari ve Tab bastirilir ("r" yazarken oracle
   yeniden acilmasin); Esc calismaya devam eder (InGameUiController.cs:1465-1469, 1482-1487).
4. EventSystem: `EmberEventSystemPolicy.EnsureInputSystemEventSystem` sahnedeki EventSystem'i
   InputSystemUIInputModule'e zorlar, eski StandaloneInputModule'leri yok eder
   (EmberEventSystemPolicy.cs:9-39).

### E. Zaman atlatma ve seyahat (tus/harita tetikler, coroutine kadansi)
1. T = 1 saat bekle, H = safaga kadar uyu: saat-adimli sim ilerletme + tam-ekran overlay;
   dialog acikken calismaz (InGameUiController.cs:1350-1381).
2. Harita ekranindan hizli seyahat: `TryBeginTravelToSettlement` → frame basina 4 sim-gunu ticken
   loading overlay → varista autosave → `EmberWorldContinuity.Carry` ile canli dunya tasinarak
   sahne yeniden yuklenir (InGameUiController.cs:1182-1229).

## Keybind Haritasi (kanitli)

### Dogrudan KeyCode yolu — `HandleScreenInput` (InGameUiController.cs:1468-1478)
| Tus | Etki | Kanit |
|---|---|---|
| Esc | ekran acik → hepsini kapat; degil → pause ekrani | 1468 |
| Tab | "SCREENS" browser toggle | 1470 |
| C | character | 1471 |
| I | inventory | 1472 |
| M | worldmap | 1473 |
| J | journal | 1474 |
| K | colony | 1475 |
| R | consul (Oracle/DM) | 1476 |
| T | 1 saat bekle | 1477 |
| H | safaga kadar uyu | 1478 |

### Action yolu — varsayilan binding path'leri (EmberRuntimeOptions.cs:46-70)
| Action | Varsayilan | Tuketici |
|---|---|---|
| Move | WASD + ok tuslari (cift composite) | EmberFirstPersonController.cs:129-131 |
| Look | `<Mouse>/delta` | EmberFirstPersonController.cs:105-106 |
| Jump / Sprint | Space / LeftShift | EmberInput.cs:27-29 |
| Interact | `<Keyboard>/e` | raycaster (68,76,87), RuntimeChestView.cs:37, RuntimeFunctionalInteriors.cs:98 |
| ToggleCursor | `<Keyboard>/f1` | EmberFirstPersonController.cs:98 |
| RegenWorld | `<Keyboard>/r` | legacy host'ta artik ORACLE tetikler (EmberWorldHost.Input.cs:19-32) — ad stale |
| ToggleInventory | `<Keyboard>/tab` | legacy host (EmberWorldHost.Input.cs:65-106) — sadece !OwnsInput iken |
| ToggleColonyPanels | `<Keyboard>/c` | legacy host (EmberWorldHost.Input.cs:50-51) — sadece !OwnsInput iken |
| SaveQuick / LoadQuick | F5 / F9 | EmberSaveService.cs:48-49 |
| Pause | `<Keyboard>/escape` | quit-hold sayaci (EmberWorldHost.Input.cs:134-145), OptionsScreen.cs:56, PauseMenu |
| Attack | `<Mouse>/leftButton` | CombatInputAdapter.cs:38, DialogBoxPanel.cs:148 (typewriter skip) |
| Secondary | `<Mouse>/rightButton` | dogrulanmadi (arama sadece diagnostics dump'inda gosterdi) |
| MeleeSwing | `<Keyboard>/f` | EmberPlayerMeleeSwing.cs:22 |

### Sayi/Fonksiyon tuslari
- 1-8: buyu cast (donanim 1-9 destekler; EmberPlayerSpellCaster.cs:50-56, EmberInput.cs:55-56);
  modal acikken cast iptal (EmberPlayerSpellCaster.cs:64). Slot SECIMI ayrica legacy host'ta
  1-5 (SpellSlotCount) ile yapilir (EmberWorldHost.Input.cs:113-117).
- F1-F12: legacy EmberHud aksiyon-baru slot tetikleme (EmberHud.ActionBar.cs:77) — ancak yeni
  UI EmberHud'u kapattigi icin canli oyunda olu yol (InGameUiController.cs:95-96).

## LLD - Veri Modeli

| Tip | Alanlar / Not | Kanit |
|---|---|---|
| `WorldHudData` (struct) | Location, EventLine, CompassLine, DelveLine, EnemyName, EnemyHp/Max, Gold, Level, ClassName, Hp/Max, Fatigue/Max, Mana/Max, SpellSlots | WorldHudView.cs:10-22 |
| `InputRuntimeOptions` | 8 move path (ana+alt), Look/Jump/Sprint/Interact/ToggleCursor/RegenWorld/ToggleInventory/ToggleColony/SaveQuick/LoadQuick/Pause/Attack/Secondary/MeleeSwing path'leri; LookSmoothingAlpha=0.5, NumberSlots=9, FunctionSlots=12 | EmberRuntimeOptions.cs:44-70; bos-string onarimi 275-283 |
| `EmberInputActions` | "Gameplay" InputActionMap; 15 action; Move = 2DVector composite x2 | EmberInputActions.cs:9-98 |
| `IgMockData` (static mutable!) | Player, EquipSlots, Inventory, SpellSchools, SpellBar, ColonyNpcs + Default* sabitleri; ekranlar buradan OKUR, RefreshLive* buraya YAZAR | IgMockData.cs:6-156 |
| `IgJournalData` / `IgTradeData` / `IgCraftingData` / `IgSaveLoadData` | ekran-basina static tasima kaplari (ayni desen) | InGameUiController.cs:703-708, 786-816, 835-857, 859-882 |
| `IgModal` overlay | "IgModalOverlay" > "IgModalPanel" > "IgModalContent"; scrim picking=Position (arkayi yer) | IgModal.cs:21-59 |
| `InGameStage` | 1920x1080 design canvas, picking Ignore, Fit() ile uniform olcek | InGameDesign.cs:309-336 |
| `CombatHudState` / `ICombatHudSource` | vitals okuma sozlesmesi | CombatHud.cs:155-162 |
| `PlayerSheetState` / `IPlayerSheetSource` | ad + 6 attribute + class/level/xp; HasData=false → mock kalir | CombatHud.cs:170-189 |
| `IEmberHudSource` | `string GetHudText()` — ust bar seridi | EmberHud.cs:161-164 |
| `IDialogSource(/Portrait)` | canli NPC konusma sozlesmesi | IDialogSource.cs:12,43 |
| Diger kaynak/sink arayuzleri | IInventorySource+ISpriteByName (InventoryGrid.cs:183-188), ITradeSource/Sink (TradeSource.cs:83-88), IJournalSource (JournalSource.cs:46), IQuestGuidanceSource (QuestGuidanceSource.cs:34), ISaveLoadSource/Sink (SaveLoadSource.cs:30-35), ILevelUpSource/Sink (LevelUpSource.cs:95-100), ICraftingSource/Sink (CraftingSource.cs:58-63), ICombatScreenSource (CombatScreenSource.cs:70), IColonyNeedsSource (ColonyNeedsPanel.cs:99), IJobQueueSource (JobQueuePanel.cs:97) |  |
| `AllScreens` | 16 ekran id'si: inventory, character, spellbook, journal, worldmap, colony, consul, dialog, combat, loot, trade, crafting, pause, levelup, death, savegame | InGameUiController.cs:1558-1565 |
| `KeybindsSection.Bindings` | oyuncuya gosterilen SALT-OKUNUR kontrol listesi (13 satir) | KeybindsSection.cs:22-37 |

## LLD - Fonksiyon Haritasi

### Girdi cephesi
- `EmberInput.Move/Look/LookSmoothed/Sprint/JumpDown/Interact/...` — action okuma property'leri
  (EmberInput.cs:15-45); lazy `Actions` kurulum+Enable (106-114).
- `EmberInput.NumberKeyDown(int)` / `FunctionKeyDown()` — donanim Digit/F tus taramasi
  (EmberInput.cs:47-64).
- `EmberInput.KeyDown/Key/MouseDown/AxisRaw/Axis` — KeyCode/legacy-axis kopru katmani
  (EmberInput.cs:66-89).
- `EmberInputActions..ctor(...)` — options path'lerinden 15 action kurar (EmberInputActions.cs:11-29);
  `Move2D` cift composite (72-98).
- `EmberInputHardware.KeyDown(KeyCode)` → `ControlFor` → `ToInputSystemKey` — KeyCode→Key cevirisi;
  desteklenmeyen tus = Key.None = false (EmberInputHardware.cs:34, 88-126).

### Orkestrator
- `InGameUiController.Awake()` — stage+HUD+browser kur, legacy'yi kapat (71-106).
- `Update()` — frame dongusu bolum B'nin tamami (114-326).
- `HandleScreenInput()` — keybind switch'i (1463-1479); `IsTextInputFocused()` (1482-1487);
  `IsAnyOpen()` (1489-1492); `ToggleBrowser/CloseBrowser/CloseAll` (1494-1503).
- `OpenScreen(string)` (369-467); `CloseScreen()` (469-484); `BuildScreenBrowser` (1569-1610).
- `OpenNpcDialog(IDialogSource, string, string)` (488-520); `AskOracle(string)` (527-540).
- `RefreshLivePlayer/Inventory/Spells/Colony/Journal/Trade/Crafting/SaveLoad` — kaynak → IgMockData
  projeksiyonlari (546-882).
- Komut aksiyonlari: `TodoCombatAction` (attack/cast:N → IPlayerCommandSink; 1383-1408),
  `TodoTradeAction` (1149-1162), `TodoCraftAction` (1163-1176), `TodoConfirmLevelUpAction`
  (1132-1146), `TodoSaveSlotAction/TodoLoadSlotAction` (1409-1441), `TodoLoadLastSaveAction`
  (1111-1129), `AwakenAction` — olum sonrasi bedel+respawn+rig teleport (338-354),
  `TodoMainMenuAction` — timeScale=1 + MainMenu sahnesi (1106-1110).
- `FastTravelAction/TravelRoutine` (1182-1229); `BeginTimeSkip/TimeSkipRoutine` (1350-1381).
- `ShowHurtFlash/UpdateHurtFlash` (1318-1345); `ShowOpeningStory` (1235-1285).
- Proof kancalari: `ProofOpenScreen` (1506-1527), `ProofCloseScreens` (1530), `ProofToggleBrowser`
  (367), `ActiveOpeningDismiss` (1233).

### Host + politika
- `EmberWorldHost.Update()` — legacy tus isleyicileri, hepsi `!OwnsInput` kapili
  (EmberWorldHost.Input.cs:13-118).
- `EmberWorldHost.IsModalOpen()` — merkezi modal predicate (126-132).
- `WorldHostInputPolicy.StepEscapeHoldTimer` — Esc-tut-cikis + Esc'e cursor toggle yan etkisi
  (EmberEventSystemPolicy.cs:49-74, 102-108); `ResolveSelectedSpellSlot` (76-91);
  `StepFateTimer` (93-100).
- `EmberEventSystemPolicy.EnsureInputSystemEventSystem` (9-39).

### HUD + etkilesim
- `WorldHudView..ctor` — top bar / pusula seridi / dusman paneli / alt HUD insasi
  (WorldHudView.cs:40-181); `Refresh(in WorldHudData)` (229-253); `RefreshCompass` — yaw→8 yon +
  smoothed frame-ms gostergesi (200-227); `VitalBar.Set` (286-291).
- `EmberPlayerInteractRaycaster.Update` — raycast + E; kacirirsa 60 derece koni icinde en yakin
  interactable'a soft-lock (46-113); `OpenDialog` — yeni UI'ya yonlendir, yoksa legacy panel
  (123-179).
- `EmberPlayerInventoryToggle.Toggle` — Tab'in cift-isleyici carpismasi cozumu; girdi sahipligi
  host'ta (EmberPlayerInventoryToggle.cs:16-53).
- `IgModal.Build/BuildTabbed` — 16 ekranin ortak modal cercevesi (IgModal.cs:19-93).
- `KeybindsSection.Build` — Options icindeki salt-okunur kontrol listesi (KeybindsSection.cs:39-52).

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` sim alanlarini kapsar; bu sistem Presentation'dadir ve registry'de
satiri YOKTUR (dogrulandi: registry Simulation/Composition'dadir, UI yazarlari ilan edilmez).
Fiili sahiplik su sekildedir:

**Yazdigi (dogrudan mutasyon):**
- `Time.timeScale` — InGameUiController.cs:68,157,1108 (menu-pause politikasinin TEK yazari degil;
  TodoMainMenuAction ve OnDisable de dokunur).
- `Cursor.lockState/visible` — InGameUiController.cs:154-155,1270-1277;
  WorldHostInputPolicy.cs:102-108; EmberPlayerInteractRaycaster.cs:168-169;
  EmberPlayerInventoryToggle.cs:39-49; EmberFirstPersonController.cs:84-85;
  EmberWorldHost.Input.cs:86-103 — EN AZ 6 ayri yazar (tek-otorite yok; (a) ailesi).
- `IgMockData.Player/Inventory/SpellBar/SpellSchools/ColonyNpcs`, `IgJournalData.*`,
  `IgTradeData.Current`, `IgCraftingData.Current`, `IgSaveLoadData.Current` — RefreshLive*
  (InGameUiController.cs:560-882). Static mutable — sahneler arasi sizinti riski.
- `InGameUiController.OwnsInput` / `AnyScreenOpen` (static) — 58-69,148.
- Legacy nesnelerin `gameObject.SetActive(false)` durumu (95-104).
- Domain mutasyonu SADECE sink'ler uzerinden: `TryMeleeStrike/TryCastSpell/TryInteract`
  (1395-1401; raycaster 139-143), `ExecuteTrade` (1158), `ExecuteCraft` (1172), `ApplyLevelUp`
  (1137), `SaveToSlot/LoadFromSlot/LoadLatestSave` (1418,1432,1120), `RespawnAfterDeath` (342),
  `AdvanceTravelDay/WaitHours/ApplyRest` (1214,1366,1371), `EndConversation/SelectTopic/AskFreeText`
  (475,510-511).
- `PlayerRig.transform.position` — respawn teleport (344-351).

**Okudugu:**
- `ICombatHudSource.Read()` vitals (226-236), `ISpellBarSource.GetSlots/GetSelectedSlot` (302,608-612),
  `IEmberHudSource.GetHudText` (304-305), `IQuestGuidanceSource.ReadQuestGuidance/ReadDelveGuidance`
  (307-322), `ICombatScreenSource.ReadCombatScreenState` (221-222,257-263), `IPlayerSheetSource`
  (548-549), `IInventorySource.GetSlots` (591), `ITradeSource.ReadTradeState` (586,791),
  `IJournalSource.GetChapters` (723), `ISaveLoadSource.ReadSaveLoadState` (864),
  `ILevelUpSource.ReadLevelUpState` (886-887), `IDialogSource.GetCurrentLine/IsThinking/VoiceKey`
  (164-173), `adapter.LevelUpReady` (280), `adapter.ReadGeneratedQuests` (755),
  `PlayerPortraitHandoff.Version/TryCreateSprite` (189,1540-1547),
  `EmberRuntimeOptionsProvider.Current.Input/WorldHost/Combat` (coklu),
  `Keyboard.current/Mouse.current` donanim durumu (EmberInputHardware.cs:22,40,90).

## LLD - Urettigi/Tukettigi Olaylar

Bu sistem WorldEventKind URETMEZ (dunya olaylarini sim uretir); tuketimi ve log tag'leri:

**Tukettigi sinyaller (consume-once static bayraklar):**
- `WorldEncounterSignal.Consume()` — dunya karsilasmasi basladi bildirimi
  (InGameUiController.cs:218-219; tanim DomainSimulationAdapter.WorldEncounter.cs:12).
- `ScreenRequestSignal.Consume()` — dunya prop'unun ekran istegi (InGameUiController.cs:250-252;
  tanim RuntimeFunctionalInteriors.cs:24).
- `RuntimeBattleMirror.Active` — dusman panelinin gercek-karsilasma kapisi
  (InGameUiController.cs:263; tanim RuntimeMusicDirector.cs:6).
- `DomainSimulationAdapter.JustCreatedWorld/OpeningHook` — acilis hikayesi tetikleyicisi (133-136).
- `WorldEventRow` tail'i — legacy EventLogHudPanel.Render tuketicisiydi; yeni UI bu paneli
  kapattigi icin canli oyunda artik akmaz (EventLogHudPanel.cs:32-62; kapatma
  InGameUiController.cs:101-102). HUD'daki tek-satir event `CombatScreenState.LastEventLine` +
  quest guidance uzerinden gelir (268,313).

**Urettigi yan-olaylar:**
- `SpeechDirector.FeedPartial/FeedFinal` — TTS beslemesi (170-173; ayrica 122-123 speech-check,
  536 oyuncu sorusu).
- `RuntimeAudioDirector.PlayUiClick` — her ekran acilisinda klik sesi (371).
- Log tag'leri: `[InGameUI]` (105,219,285,342,1115,1144,1388,1447), `[Travel]` (1202,1221,1223),
  `[ProofUI]` (1513,1522-1525), `[EmberEventLog]` (EventLogHudPanel.cs:50 — legacy, artik pasif).

## Testler

- `Assets/Tests/PlayMode/Input/EmberInputContractTests.cs` — girdi cephesi sozlesmesi:
  bos frame notr degerler (35), device→Move/Look okumasi (49), semantik butonlar (67),
  mouse/sayi/fonksiyon/passthrough (95), Jump binding'inin runtime options ile remap'i (122),
  ToggleColony remap'i (143).
- `Assets/Tests/EditMode/Presentation/WorldHostInputPolicyTests.cs` — Esc-tut sayaci modal
  acikken sifirlanir (10), esikten sonra quit (28), modal acikken buyu-slot secimi korunur (46),
  fate timer suresi dolunca callback (58).
- `InGameUiController`, `WorldHudView`, `IgModal`, ekran view'lari ve `KeybindsSection` icin
  otomatik test BULUNAMADI (Assets/Tests taramasi bos dondu) — bu yuzeyler yalniz
  `EmberProofScreenshotDriver` proof turlariyla (ProofOpenScreen kancalari,
  InGameUiController.cs:1506-1530) gorsel dogrulanir; regresyon pinli degil.

## Bilinen Borclar + Kacak Kapilari

1. **Legacy Input API sizintisi — CANLI HATA, (e) ailesi:** `OptionsScreen.Update` hem
   `EmberInput.PauseDown` hem `Input.GetKeyDown(KeyCode.Escape)` okur (OptionsScreen.cs:56).
   Proje InputSystem-only'dir (`activeInputHandler: 1`, ProjectSettings.asset:689) ve ayni
   dosyadaki yorum bunu acikca belgeler: "legacy UnityEngine.Input throws here"
   (InGameUiController.cs:126-128). Options ekrani acikken her frame exception atar (short-circuit
   sadece PauseDown true iken kurtarir). Ayni aile daha once 17k exception uretmisti.
2. **KeybindsSection oyuncuya STALE harita gosteriyor:** liste "Tab → Inventory" der
   (KeybindsSection.cs:31) ama yeni UI'da Tab = ekran browser'i, envanter = I
   (InGameUiController.cs:1470-1472). T (bekle), H (uyu), K (koloni), F (melee swing) listede hic
   yok; "F32 no dead buttons" kuralinin kendisi ihlalde. Ayrica bu ekrana giden yol da kopuk
   (madde 3).
3. **Yeni PauseView'un Settings butonu TODO:** `TodoSettingsAction` sadece log yazip kapatir
   (InGameUiController.cs:1104,1445-1449); PauseMenu+OptionsScreen yeni UI tarafindan kapatildigi
   icin (95-104) canli oyunda ayarlara ve keybind listesine ULASILAMAZ.
4. **"RegenWorld" action adi yalan soyluyor:** `<Keyboard>/r` binding'i artik dunya yeniden
   uretmez, legacy yolda Oracle konsultasyonu tetikler (EmberWorldHost.Input.cs:19-32); yeni UI
   yolunda R dogrudan consul ekranidir (1476). Ad/istek uyumsuzlugu remap UI'si yazilirsa yaniltir.
5. **Cift keybind evreni:** ayni tuslar iki ayri mekanizmadan okunur — action-path'li
   `EmberInputActions` (remap edilebilir, testli) ve `HandleScreenInput`'taki SABIT KeyCode
   switch'i (InGameUiController.cs:1468-1478, remap EDILEMEZ). Options'tan Interact'i degistirsen
   bile C/I/M/J/K/R/T/H sabit kalir; (a) tek-otorite ihlali.
6. **Cursor kilidinin 6+ yazari** (yukarida liste): OwnsInput devrindeyken bile legacy yol
   Esc'te cursor toggle eder (WorldHostInputPolicy.cs:61-64 — yalniz !OwnsInput'ta calisir ama
   PauseMenu'lu eski sahnelerde yasar). Tarihsel "cursor control scattered" gozlemiyle uyumlu;
   dialog-kapanis kilidi icin ayri politika sinifi bile var (DialogCursorPolicy,
   DialogBoxPanel.Render.cs:36). (a)/(f) ailesi.
7. **Static mutable ekran verisi:** `IgMockData.Player` vb. static'tir (IgMockData.cs:38);
   RefreshLive* kismi yazar (level/xp/gold mock kalabilir, InGameUiController.cs:542-545,564-566)
   — iki dunya arasi gecinte onceki oyunun degerleri sizabilir (sahne yeniden yuklemede temizleyen
   kod BULUNAMADI — dogrulanmadi).
8. **Tab'in uc sahipli gecmisi:** EmberPlayerInventoryToggle eskiden Tab'i kendisi dinleyip
   host'la carpisiyordu (cift toggle); simdi no-op bilesen + host cagrisi
   (EmberPlayerInventoryToggle.cs:6-14, EmberWorldHost.Input.cs:65-76). OwnsInput devrinde Tab
   browser'a gitti; ayni tusun uc kusagi da kodda duruyor.
9. **`_activeScreen` = "son eklenen cocuk" varsayimi:** OpenScreen canvas'in son cocugunu ekran
   sayar (458); bir view kurulumda canvas'a IKINCI bir eleman eklerse yanlis eleman izlenir.
   "IgModalOverlay" temizligi guvenlik agi olarak itiraf edilmis (481-483). Kirilgan desen,
   (f) ailesi komsusu.
10. **Proof-modal render avi ACIK:** ProofOpenScreen'deki teshis loglari "overlay attach oluyor
    ama capture'da gorunmuyor" hatasinin COZULMEDIGINI soyluyor (InGameUiController.cs:1508-1526).
11. **Kacak kapilari (bilincli):** `ProofOpenScreen/ProofCloseScreens/ProofToggleBrowser` +
    `ActiveOpeningDismiss` static'i — proof suruculeri icin programatik ekran kontrolu
    (1233,1506-1530); `--ember-speech-check` komut satiri kancasi Update icinde (119-124);
    `ResetForTests/EnableForTests` UNITY_INCLUDE_TESTS kapisi (EmberInput.cs:91-104).
12. **TODO(real-data) etiketleri:** envanter ekipman read-model'i yok (599), spellbook host
    kaynagi yok (630), FactionRow NPC kartina projekte edilemiyor (678-681), oyuncu portre
    sprite-key handoff'u eksik (1550-1556). Ekranlar bu alanlarda mock/bos gosterir — bilerek.
13. **Secondary (sag tik) tuketicisi dogrulanamadi:** binding tanimli (EmberRuntimeOptions.cs:66)
    ama diagnostics dump'i disinda okuyan koda rastlanmadi — olu binding olabilir (dogrulanmadi).

Hata ailesi harfleri `docs/SYSTEMS_ATLAS.md:52-59` taksonomisine gore.

# 20-proof-harness

> Kapsam: kanıt zinciri — `EmberProofScreenshotDriver` modları, W30 master-bayrak
> dersi, W32 spatial + label invariantları, W36 `ProofLivingCensus` peaks,
> fallback harness gate (pure-C# NUnit) ve CAN SUYU / dijest golden pin testleri.
> Tarih: 2026-07-26 (W36 sonrası B26 CENSUS storysi kapandı).

## HLD - Ne ve Neden (5-10 cumle)

Kanıt harness'i, Alcyone Ember'in "başımı sallamak yerine ekran görüntüsü göster"
disiplinini işletir: her büyük iddia — sahne yükleniyor, NPC eşit dağılıyor, marathon
memory düz, action label doğru — yalnız data-layer log ile değil, yürüyen player
binary + render katmanında yakalanan PNG/txt kanıtı ile ispatlanır (memory:
`verify-at-render-layer`). Bunun üç katmanı vardır: (1) **oyuncu-binary kanıtı** —
`EmberProofScreenshotDriver`, player build'e gömülen tek `MonoBehaviour`; master
bayrak `--ember-proof-screenshots` yoksa `Bootstrap` erken çıkar (driver hiç
mount olmaz). (2) **saf-C# fallback harness** — Unity binary bulunmadığında
`tools/validation/run-validation.sh` `dotnet test`'i
`ValidationFallbackHarness.csproj` üstünde koşturur (Domain/Simulation/Data +
Assets/Tests/EditMode/** wildcardu). (3) **CAN SUYU + digest golden testleri** —
gate kontratları ve `WorldStateDigest` SHA-256 pini, iki-katmanı da tek imzalı
belirteçle bağlar. W30 dersi tekildir: proof runları arka-plan shell'den açılınca
pencere focus almaz ve pause olan player tüm coroutine'leri dondurur; W36 dersi
"anlık `eating=0` alongside `meals=6195` false-PASS ediyordu"'nun peaks
accumulator ile kapatılmasıdır.

## HLD - Akış (numaralı adımlar)

1. **Bootstrap gate**: `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` çalışır;
   `HasArg("--ember-proof-screenshots")` false ise dönüş — driver hiç mount olmaz
   (`EmberProofScreenshotDriver.cs:23-33`).
2. **runInBackground ON + `DontDestroyOnLoad`**: master bayrak varsa
   `Application.runInBackground = true` set edilir ve yeni GO'ya driver eklenir
   (aynı blok:29-32). W30 dersi ("soak armed then silence"): windowed proof runları
   arka-plan shell'den başlar, focus yoksa pause olan player tüm coroutine'leri
   dondururdu.
3. **Output klasörü**: `ResolveOutputDir()` `--ember-proof-screenshots <path>`
   argümanının bir sonraki tokenini `Path.GetFullPath` ile mutlaklaştırır;
   bulunamazsa `persistentDataPath/proof-screenshots`
   (`EmberProofScreenshotDriver.cs:2545-2551`).
4. **Mod seçici**: `Start()` her mod bayrağını sırayla kontrol edip ilgili
   coroutine'i döndürür ve `Application.Quit()` çağırır (17 mod; :35-209). Master
   bayrak varsa mod bayrağı yoksa fallback rescue-benzeri hızlı capture akışı
   çalışır (:209-260) — mod bayrağı yoksa yine quit eder.
5. **PLAYTEST FIX (W30)**: her mod çıkışı ALWAYS `Application.Quit()` çağırır —
   eski opt-in `--ember-proof-quit` bayrağı hiç geçilmediğinden pencereler
   birikiyordu ("oyun testten sonra kapanmıyor"). Yorum satırı her mod dalında
   verbatim tekrarlanır (:41-47, :53-57, :64-68 … :202-208).
6. **BOOT-RACE FIX (shipcheck)**: `WaitForBootToSettle()` MainMenu aktif sahne
   olana kadar bekler + 0.8s grace; eski sabit `WaitForSeconds` boot navigasyonu
   ile yarıştığından "world-enter: no adapter" hatası veriyordu
   (`EmberProofScreenshotDriver.cs:2528-2539`).
7. **Sahne + adapter yükleme**: gameplay modları `EmberWorldGenIntent.Pending`
   ile intent kurar, `SceneManager.LoadScene(EmberScenes.GeneratedWorld)` çağırır,
   `EmberDomainAdapterLocator.Current is DomainSimulationAdapter` görülene kadar
   bounded polling yapar (marathon örneği: 120s deadline, :941-947).
8. **Peaks reset (W36/B26)**: marathon başlangıcında
   `soakAdapter?.ProofResetLivingPeaks()` çağırılır — peaks adapter üstünde
   Editor session'ları arası kalır; sıfırlanmazsa ilk run'ın peaks'i ikinci run'a
   sızar ve bozuk ikinci run false-PASS eder (:950-952).
9. **W32 DOC5 render-layer invariants (agentcheck)**: her tick sonrası spatial
   drift (billboard sim projeksiyonuna) ve label doğruluğu (`NpcActivityLabelView.
   RenderedText` == `s1.Activity`, `s1==s2` double-read guard'ıyla) `LogError`
   olarak akıtılır; `eatingSeen==0` ise assert VACUOUS damgalanır (:801-853).
10. **PASS gate (marathon)**: `bool pass = exceptions == 0 && flat && !aborted &&
    actions > 0 && censusOk;` — `censusOk = gameHours < 24 || (peaks.sleeping > 0
    && peaks.working > 0 && peaks.eating > 0)` (:1024-1037). Kısa smoke soaks
    peaks'i advisory tutar; 24-saat üstü gerçek marathon peaks'i zorunlu kılar.
11. **Fallback zinciri**: `run-validation.sh` `--mode auto` — Unity binary
    bulunursa `-batchmode -runTests -testPlatform EditMode`, aksi halde
    `dotnet test tools/validation/fallback/ValidationFallbackHarness.csproj`;
    T-CENSUS-3 gibi engine-free testler her iki katmanda da koşar, T-CENSUS-1/2
    `#if UNITY_EDITOR` guard ile Editor-only kalır.

## LLD - Veri Modeli (file:line)

- `EmberProofScreenshotDriver` — proof mode sürücüsü, tek `MonoBehaviour`
  (`Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs:20`),
  2661 satır, 17 mod coroutine'i.
- `_outputDir : string` — mode başlangıcında `ResolveOutputDir()` ile set edilir;
  tüm capture yolları buraya `Path.Combine` edilir (`:22`).
- `EmberProofScreenshotDriver` bayrak dize sabitleri — literal argüman
  formatında; `HasArg`/`GetArg` `Environment.GetCommandLineArgs()` üstünde çalışır
  (`:2555-2569`).
- Peaks accumulator (W36/B26) — `DomainSimulationAdapter.WorldEncounter.cs:43-45`:
  `_livingPeakSleeping, _livingPeakWorking, _livingPeakEating, _livingPeakFarming
  : int`, `_livingPeakSamples : int` (0 = hiç örneklenmedi),
  `_livingPeakSampledAtMinutes : long` (honesty rule için son örnekleme
  game-clock'u).
- `NpcActivityLabelView.RenderedText : string` — TextMesh'ın FIILI text'i, push
  edilen değil (`Assets/Scripts/Presentation/Ember/Views/NpcActivityLabelView.cs
  :38-39`). W32 DOC5 label invariantının render-layer okuma noktası.
- `ActorViewState` — `Views/ActorViewState` (WorldPosition/Activity/ActionKind)
  billboard↔sim resolve sırasında read model olarak taşınır.
- Marathon soak state — `RunMarathon()` içinde local: `exceptions:int`,
  `memStart/memPeak:long`, `rngState:uint` (xorshift), sayaçlar (`actions,
  travels, fights, trades, hours`), `endAt/nextHeartbeat:float`, `aborted:bool`,
  `gameMinStart:long`, `censusOk/flat/pass:bool` (`:920-1037`).

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

Hepsi `EmberProofScreenshotDriver.cs` içinde (aksi yazılmadıkça):

- `static void Bootstrap()` — :24. `--ember-proof-screenshots` yoksa erken çıkar;
  master gate. `runInBackground = true` set eder, driver GO'yu mount eder.
- `IEnumerator Start()` — :36. 17 mod bayrağını sırayla kontrol eder; eşleşen
  coroutine'i döndürüp `Application.Quit()` çağırır (W30 PLAYTEST FIX).
- `IEnumerator RunGameplayShot()` — :311. `EmberWorldGenIntent.Pending` seed +
  `GeneratedWorld` sahnesi + FPV/overhead spawn ve `+4s` frame'leri (`--ember-
  gameplay-shot`).
- `IEnumerator RunPlaythrough()` — :330. `--ember-playthrough`; MainMenu'den 11
  char-creation adımı + worldgen reveal + gameplay + 16 in-game modal.
- `IEnumerator RunShipCheck()` — :424. `--ember-shipcheck` ONE-COMMAND regresyon:
  quest-seed, encounter-loot, economy, perf 90-frame avg/worst, 10x fast-travel
  soak, economy chain, audio-forge centroid, modal capture; `SHIPCHECK VERDICT`
  satırı bastırır.
- `IEnumerator RunLoopProof()` — :541. `--ember-looptest` full loop:
  quests→encounter→loot→trade→respawn (F2/F17/F26 legs), `LOOP-PROOF` satırları.
- `IEnumerator RunTimelapse()` — :623. `--ember-timelapse` KARE-KARE CANLILIK:
  sabit plaza kamerası, 90 frame × 10s = ~18 game saati, 360° pan.
- `IEnumerator RunAgentCheck()` — :674. `--ember-agentcheck` DM oracle + gerçek
  NPC diyaloğu + W32 DOC5 spatial + label invariantları LOUD; `agentcheck_eating_
  label.png` render-layer proof (yalnız `eatingSeen>0` ise).
- `IEnumerator RunMarathon()` — :897. `--ember-marathon`; 30-dk (varsayılan)
  otonom soak, `--ember-marathon-minutes N` ile kısaltılır. `[Marathon] VERDICT
  PASS/FAIL` peaks + censusOk gate ile.
- `IEnumerator RunIgTour()` — :1047. `--ember-igtour` F32-DoD 9 in-game ekranın
  frame turu (HUD/inventory/character/journal/map/pause + 3 options section).
- `IEnumerator RunMainQuest()` — :1121. `--ember-mainquest` F31/F35 three-act
  spine: delve chestleri → capital sage → final Warden.
- `IEnumerator RunLookAround()` — :1214. `--ember-lookaround` self-playtest:
  spawn 360° pan + kapıya yürüyüş + iç oda + F16/F18/F19/F20/F29 legs.
- `IEnumerator RunRescueProof()` — :1884. `--ember-rescue-proof` rescue yolu +
  worldgen loading + generated_world capture.
- `IEnumerator RunSceneTour()` — :1919. `--ember-scene-tour` her gameplay
  sahnesini UI-on + UI-off çift capture (magenta/material rescue proof).
- `IEnumerator RunLlmProof()` — :1942. `--ember-llm-proof` `ForgeLocator.NativeLlm`
  bekle → off-main-thread `Complete()` → provenance + response `llm-proof.txt`.
- `IEnumerator RunForgeProof()` — :2130. `--ember-forge-proof` SDXL D1
  verification; `--ember-forge-prompt/negative/size/seed` parametrize eder.
- `IEnumerator RunPlanetProof()` — :2241. `--ember-planet-proof` PlanetGenerator
  determinism (seed 42 regen) + equirectangular PNG'ler + `planet-proof.txt`.
- `IEnumerator RunWorldProof()` — :2316. `--ember-world-proof` engine-free
  `WorldFactory` + `WorldTickComposer.TicksPerGameDay` gün simülasyonu +
  `world-proof.txt` (JobAssigned/JobCompleted/SmeltCompleted/QuestCompleted).
- `IEnumerator RunInputProof()` — :2488. `--ember-input-proof` E7-020 Stage 0;
  `EmberInput` facade snapshot'ları (Input System yokken derlenebilir kalır).
- `static bool TryResolveViewState(ActorView, IDomainSimulationAdapter, out ActorViewState)`
  — :735. W32 DOC5 id-first, key-fallback view→sim resolve
  (`WorldViewProjector` ile aynı desen).
- `static IEnumerator WaitDialog(IDialogSource, float)` — :755. Async LLM'nin
  `IsThinking` bir frame sonra flip ettiği için 1.5s beat + bounded polling.
- `static IEnumerator WaitForBootToSettle()` — :2528. MainMenu aktif sahne olana
  kadar bekle + 0.8s grace; shipcheck boot-race fix.
- `void CaptureToPng(string)` — :2622. Non-batchmode'da `ScreenCapture.
  CaptureScreenshot` (overlay UI dahil); batchmode'da kamera→RT→PNG.
- `IEnumerator CaptureFixedAfter(float, string)` — :2604. Wait + capture +
  35ms async separation.
- `IEnumerator CaptureOverheadAfter(float, string, float)` — :2662. Rig
  kamerasını yukarı çeker, tek angled top-down alır, geri koyar (URP
  configured cam borrow'u).

Adapter tarafı — `Assets/Scripts/Presentation/Ember/Adapters/
DomainSimulationAdapter.WorldEncounter.cs`:

- `string ProofLivingCensus()` — :676. Event log'dan `meals/witnessed/
  reported/guard/chronicle/shortage` + aktör aksiyonundan `sleepingNow/
  workingNow/eatingNow/farmingNow` sayar; kendi içinde `ProofSampleLivingPeaks()`
  fold eder ve `sleepingPeak/workingPeak/eatingPeak/farmingPeak/peakSamples`
  yazdırır.
- `void ProofSampleLivingPeaks()` — :716. O(alive actors), zero alloc; instant
  sayımı MAX-CONCURRENT peaks'e katar; `_livingPeakSamples++`; `_livingPeakSampledAtMinutes`
  günceller (WorldState'e yazmaz — diagnostic-only).
- `void ProofResetLivingPeaks()` — :743. Marathon her koluna girişte çağırılır;
  peaks + samples + sampledAtMinutes sıfırlanır. B26 sızıntı savunması.
- `(int sleeping, int working, int eating, int farming, int samples) ProofLivingPeaks()`
  — :751. Marathon PASS gate'in okuduğu tuple.
- `long WorldTimeMinutesOrZero()` — :755 civarı. Marathon honesty rule için
  game-clock; world yoksa 0.
- `bool ProofMovePlayerBeside(ulong)` — :670. Reach-gated flowlar için sim
  player'ı hedef actor'ün yanına atlar (proof-only, gerçek oyun yürür).

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Driver **hiçbir** `WorldState` alanına yazmaz — proof surface tam olarak read-only
+ side-effect free adapter komutları üstünden çalışır. Ownership tarafı:

- **Yazar**:
  - `_outputDir` (driver-local) — `ResolveOutputDir()` sonucu.
  - `Application.runInBackground` — global; W30 dersi (window focus yoksa
    coroutine'ler donmasın).
  - `_livingPeakSleeping/Working/Eating/Farming/Samples/SampledAtMinutes`
    (adapter-private) — `ProofSampleLivingPeaks` / `ProofResetLivingPeaks`
    tarafından yazılır; **hiçbir gameplay pathi bunları okumaz**, sadece proof
    çıktısı yazdırır.
  - `PlayerRig.transform.position/rotation` — sadece agent-check + lookaround +
    timelapse gibi self-playtest modlarında; `CharacterController` bir tick
    disable edilip restore edilir. `EmberFirstPersonController.enabled` iki tick
    off/on (aim ownership'i almak için).
  - `Camera.transform` + `farClipPlane` — `CaptureOverheadAfter` içinde ödünç
    alıp restore.
- **Okur** (yalnız):
  - `EmberDomainAdapterLocator.Current`, `.WorldViewReadModel`,
    `.ConsultFateOracle`.
  - `IDomainSimulationAdapter.GetSpawnableActors()`, `TryReadActor(id|key)`,
    `GetDialogSource(ActorId)`, `TickIndex`, `AdvanceTick(index)`,
    `InventorySlots`, `TryTravelToSettlement(name)`.
  - `DomainSimulationAdapter` proof yüzeyleri (yukarıda).
  - `Environment.GetCommandLineArgs()` (bayrak parse), `Time.unscaledTime` (soak
    deadline), `Profiler.GetTotalAllocatedMemoryLong()` (memory flat check).
  - `SceneManager.GetActiveScene()`, `Application.CanStreamedLevelBeLoaded`.

## LLD - Ürettiği/Tükettiği Olaylar

Driver hiçbir `WorldEvent` üretmez (proof gözlem katmanıdır, oyun değil).
Ürettikleri: **log satırları** ve **dosyalar**.

Log prefixleri:

- `[Proof]` — ×67 (rescue, playthrough, mainquest, sceneTour, F16/18/19/20/29
  legs).
- `[AgentCheck]` — ×16 (RunAgentCheck sırasında DM/dialog/inventory
  transcript'i).
- `[Invariant]` — spatial + label (W32 DOC5) LogError'ları; per-tick fail
  sayaçları.
- `[Marathon]` — soak armed, heartbeat (t=Ns, actions, exceptions, mem, peaks),
  LIVING census, VERDICT PASS/FAIL.
- `[Playthrough]` — main menu, char-creation adımları, VERDICT (creation to
  credits).
- `[Timelapse]` — 90 frame sekans özeti.
- `[MainQuest]` — three-act spine geçişleri.
- `LOOP-PROOF` — Encounter/Trade/GeneratedQuest/TavernSleep leg özetleri.
- `SHIPCHECK [PASS|FAIL] <bölüm>` + `SHIPCHECK VERDICT` — kompakt CI mesajı.
- `[EmberProofScreenshotDriver]` — ×7 (camera search, screen-grab, wrote path).

Dosya çıktıları (`_outputDir` altında):

- Ekran görüntüsü PNG'leri: mode-özel prefix'lerle (`cc_*`, `ig_*`, `lapse_*`,
  `look_*`, `pt_*`, `igtour_*`, `shipcheck_*`, `looptest_*`, `gameplay_*`,
  `agentcheck_*`, `planet-seed-*`, `forge-die.png`, `tour_*_ui/noui.png`).
- Metin raporları: `llm-proof.txt`, `forge-die.txt`, `planet-proof.txt`,
  `world-proof.txt`, `input-proof.log`.

Tüketilen: hiçbir `WorldEvent` subscription'ı yok — driver polling + doğrudan
adapter çağrılarıyla çalışır.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Diagnostics/ProofLivingCensusPeaksTests.cs` — B26
  T-CENSUS-1 (`SnapshotZero_EventsMany_ExposesTheWound`) + T-CENSUS-2
  (`PeaksAccumulateAcrossSamples_EvenWhenSnapshotZero`). `#if UNITY_EDITOR`
  guard'lı (adapter Unity-tied). Şu invariantları pin'ler: `eatingNow==0` +
  `eatingPeak>0` peaks sample'ı yaşasın; `peakSamples>=2` her explicit sample
  sayılsın.
- `Assets/Tests/EditMode/Diagnostics/MarathonPassGateCensusTests.cs` — B26
  T-CENSUS-3 pure-boolean gate; `CensusOk(hours,sleep,work,eat)` driver'daki
  clause'un birebir aynası. 6 test: `FullDay_ZeroSleepPeak_Rejects`,
  `FullDay_AllPeaksPositive_Passes`, `ShortSmoke_AllZeroPeaks_Passes_AdvisoryOnly`,
  `FullDay_ZeroWorkPeak_Rejects`, `FullDay_ZeroEatPeak_Rejects`,
  `ExactlyDayBoundary_RequiresPositivePeaks`. Fallback harness'ta koşar (engine-
  free).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — CAN SUYU V2 H1-H5
  gate testleri (stateless + Stamp tick rules, gate contract, marathon benchmark).
- `Assets/Tests/EditMode/CanSuyu/GateContractLintTests.cs` — LivingWorldGate
  test dosyasının varlığını + kontrat sözleşmesini lint eder (silinirse kırılır).
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` —
  `WorldStateDigest` SHA-256 golden pin; herhangi bir schedule/sim değişikliği
  onaylanmamışsa kırar (W34-A: sleep/work state golden seed).
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs` — save
  mapping'in her iki yöne de digest'i koruduğunu pin'ler.
- `tools/validation/fallback/ValidationFallbackHarness.csproj` — 27
  `<Compile Include>` satırı; `Assets/Tests/EditMode/**/*.cs` wildcard'ı ile
  T-CENSUS-3 dahil tüm engine-free testler dotnet test altında yeşile döner.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W30 (proof-harness launch protocol)**: master flag `--ember-proof-
  screenshots` zorunlulaştırıldı; `Application.runInBackground = true` set edildi
  (background shell'den açılan proof runları focus almadığı için "soak armed
  then silence"); her mod çıkışında `Application.Quit()` hardcoded (opt-in
  `--ember-proof-quit` unutuluyordu, pencereler birikiyordu); 5 blind run'dan
  sonra `WaitForBootToSettle` shipcheck'in "world-enter: no adapter" yarışını
  kesti. Memory: `proof-harness-launch-protocol.md`.
- **W31 (soul weeks)**: 8 canlı yara → 20 sistem atlas dokümantasyonu; proof
  harness bunlardan biri olarak ilk kez ayrıştırıldı.
- **W32 DOC5 (render-layer invariants)**: `RunAgentCheck` içine spatial (billboard
  drift > 5.5m fail; window pane wall-face clear fail) + label (`RenderedText`
  == sim.Activity, `s1==s2` double-read guard, `eatingSeen>0` VACUOUS assert,
  2-sim-gün bound) invariantları eklendi; `TryResolveViewState` id-first key-
  fallback pattern'i çıkarıldı (`WorldViewProjector` ile aynı). Memory: `verify-
  at-render-layer.md`.
- **W33 (farm slice)**: F1-F5 farm story testleri; `ProofLivingCensus` içinde
  `PlantSeed/HarvestCrop/HaulCrop` `farming` sayacı olarak eklendi (`DomainSimulationAdapter.WorldEncounter.cs:701-705`).
- **W34 (sleep + work slice)**: `Sleep` → `sleepingNow`, `PerformWork` →
  `workingNow` sayaçları eklendi; W34-A digest golden seed'i non-default
  sleep/work state ile yenilendi (`WorldTickDigestGoldenTests`); marathon
  peaks accumulator için zemin atıldı.
- **W35 SHRINK**: `ProofLivingCensus` LIVE-proof kararı — "marathon
  sleeping=0 while shortages=N Sleep hiç çalışmadı demek, `worked around` değil"
  yorumu ile per-action strip sayacı sertleştirildi (aynı dosya :688-707).
- **W36 (B26 CENSUS)**: peaks accumulator + reset + sample + PASS gate; adapter
  private fields (`_livingPeakSleeping/Working/Eating/Farming/Samples/
  SampledAtMinutes`); `RunMarathon` her iterasyonda `ProofSampleLivingPeaks`
  çağırır (heartbeat-only sampling arası slice başlayıp bitebiliyordu); PASS
  gate `censusOk = gameHours < 24 || (peaks.sleeping > 0 && peaks.working > 0
  && peaks.eating > 0)`; T-CENSUS-1/2 Editor-only, T-CENSUS-3 fallback-safe.
  2026-07-25 kapandı.

## Bilinen Borçlar + Kaçak Kapıları

1. **`--ember-proof-quit` yorum satırı ölü** — 15 mod dalında verbatim çoğaltılan
   PLAYTEST FIX yorumu artık relevant değil (opt-in bayrak yok); consolidation
   `private void QuitProof(string reason)` yardımcısına indirir (`EmberProofScreenshotDriver.
   cs:41-208`). Belge-kod drifti riski.
2. **`RunAgentCheck` W32 invariantları yalnız `--ember-agentcheck` altında
   koşar** — shipcheck bu invariantları koşmaz, marathon sadece census/peaks
   bakar; label/spatial regresyonu ancak nightly agentcheck çalıştırıldığında
   yakalanır. Regresyon fırsatı: shipcheck'e "1 tick + label okuma" hızlı probe.
3. **Farming peak PASS gate'e girmiyor** — `censusOk` sadece sleep/work/eat
   ister; harvest/plant/haul günü yaşamayan bir marathon PASS eder. Farm
   sisteminin gerçekten canlı olduğu iddiası proof-layer'da pin'lenmez (ancak
   `farmingPeak` yazdırılır — göz kontrolü mümkün).
4. **`SnapshotZero_EventsMany_ExposesTheWound`** t-1 testi wound'u belgeliyor
   (rename tuzağı), positive fix testi t-2 farklı; ikisi de aynı adapter
   instance'ında birlikte koşarsa peaks leak'i (t-1 hiç reset çağırmıyor) t-2'yi
   yanıltmaz çünkü t-2 en başta `ProofResetLivingPeaks` çağırıyor — ama testler
   arası shared adapter state fixture'ı gelirse bu kırılabilir.
5. **`CaptureToPng` non-batchmode dalında UI overlay yakalanır ama camera-render
   dalında (batchmode) MISS eder** — headless shipcheck/marathon capture'ları
   3D kameradan aşağıdır; overlay-only asserts (HUD sayaçları) headless
   koşularda görünmez. Belgelenmemiş bir false-negative kaçağı.
6. **`_livingPeakSampledAtMinutes` yazılıyor ama hiçbir yerde okunmuyor** —
   honesty rule için niyet edildi, PASS gate `WorldTimeMinutesOrZero()`'yu
   kullanıyor; alan ya tüketilmeli ya kaldırılmalı (dead field warning riski).
7. **Fallback harness `Assets/Tests/EditMode/**/*.cs` wildcard'ı** — Unity-tied
   test dosyaları `#if UNITY_EDITOR` guard'ıyla korunur; yeni bir Editor-only
   test guard'ı unutursa fallback harness build kırar (bu istenen davranış
   ama regresyon gürültüsü olabilir).
8. **`RunLookAround` FPS controller `enabled=false/true` toggle'ları try/finally
   yok** — proof crash olursa controller kalıcı disabled kalır (in-scene sadece
   proof runlarında olduğu için etkisiz, ama coroutine exception path
   ateşlenirse driver GO destroy edilmiyor). Düşük şiddet, dokümante edilmiş.

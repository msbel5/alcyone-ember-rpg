# 20-proof-harness

> Kapsam: kanıt zinciri — `EmberProofScreenshotDriver` modları, master bayrak
> (`--ember-proof-screenshots`), koşu-içi invariantlar, saf-C# fallback harness
> (`tools/validation/`), CAN SUYU gate testleri ve digest golden pini.
> Kanıt biçimi: `dosya:satır`. Yollar aksi belirtilmedikçe `Assets/` veya repo köklüdür.

## HLD - Ne ve Neden

Kanıt zinciri, "oyun kendini test eder" ilkesinin uygulamasıdır: insan gözünün ve CI'nin
göremediği şeyleri (headless sahne gerçekten kuruluyor mu, NPC'ler gerçekten yürüyor mu,
LLM gerçekten cevap veriyor mu) oyunun kendi çalıştırılabilir dosyası üretir ve PNG +
transcript + PASS/FAIL satırı olarak diske bırakır (README.md:76-81). Zincir dört
katmandır: (1) **oyuncu-binary kanıtı** — `EmberProofScreenshotDriver`, player build'e
`--ember-proof-screenshots <dir>` master bayrağıyla takılan 17-modlu bir MonoBehaviour
sürücüsü (EmberProofScreenshotDriver.cs:23-33); (2) **saf-C# fallback harness** — Unity
editörü olmayan makinede Domain/Simulation kaynak testlerini `dotnet test` ile koşan NUnit
projesi (tools/validation/run-validation.sh:161-203); (3) **gate testleri** — bir yönlendirme
scriptinin FİZİKSEL OLARAK taklit edemeyeceği canlı-dünya özelliklerini (ihtiyaçlar düşer,
tek aktörü silmek tarihi değiştirir, olaylar kimse yazmadan doğar) zaman-serisi üzerinden
ölçen CAN SUYU sözleşmesi (Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs:10-15);
(4) **digest golden** — aynı seed + aynı tick dizisinin bayt-aynı dünya üretmesini SHA-256
pinleyen determinizm çıpası (Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs:26).
Felsefe, V1 kanıt sisteminin çürümesinden çıkan derstir: "saat 12'de 8 kişi tavernada"
tarzı koreografi kanıtları sahtelenebilir; ekran görüntüsü DESTEKLEYİCİ delildir, asla tek
başına kapı değildir — bu kural lint testiyle CI hatasına bağlanmıştır
(GateContractLintTests.cs:8-13, 56-59). Oyuncuya görünen etki dolaylıdır: her sürüm notundaki
"shipcheck 9/9 PASS" satırı bu zincirin çıktısıdır ve sürüm notu kanıttan SONRA yazılır
(Docs/ROADMAP_V1.md:9-12).

## HLD - Akis

1. **Boot kancası:** `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] Bootstrap()` komut
   satırında master bayrak `--ember-proof-screenshots` yoksa hiçbir şey yapmaz; varsa
   `Application.runInBackground = true` (arka plandan başlatılan pencere odak alamayınca
   coroutine'ler donuyordu), `DontDestroyOnLoad` bir GameObject yaratır ve sürücüyü ekler
   (EmberProofScreenshotDriver.cs:23-33).
2. **Çıktı dizini:** `ResolveOutputDir()` master bayrağın HEMEN SONRAKİ argümanını tam yol
   yapar; bulunamazsa `persistentDataPath/proof-screenshots` (EmberProofScreenshotDriver.cs:2460-2467).
3. **Mod seçimi:** `Start()` 17 alt-bayrağı sırayla `HasArg` ile yoklar ve İLK eşleşen modu
   koşturur (EmberProofScreenshotDriver.cs:35-209): `--ember-rescue-proof` (:39),
   `--ember-gameplay-shot` (:50), `--ember-playthrough` (:61), `--ember-marathon` (:72),
   `--ember-igtour` (:80), `--ember-mainquest` (:88), `--ember-timelapse` (:96),
   `--ember-agentcheck` (:104), `--ember-lookaround` (:112), `--ember-looptest` (:123),
   `--ember-shipcheck` (:134), `--ember-scene-tour` (:145), `--ember-llm-proof` (:156),
   `--ember-forge-proof` (:167), `--ember-world-proof` (:178), `--ember-input-proof` (:189),
   `--ember-planet-proof` (:200). Alt-bayrak yoksa varsayılan tur koşar: boot → mainmenu →
   karakter yaratma adımları → sentetik worldgen sorusu + kasıtlı sentetik hata
   (`includeSyntheticFailure: true`, :211-262, :302).
4. **Her mod SONUNDA çıkar:** "oyun testten sonra kapanmıyor" playtest bulgusu üzerine her
   dalın sonunda koşulsuz `Application.Quit()`; eski opt-in `--ember-proof-quit` bayrağı hiç
   geçilmediği için pencereler birikiyordu (EmberProofScreenshotDriver.cs:41-47, 202-208).
5. **Boot yarışı çözümü:** dünya-giren modlar `WaitForBootToSettle()` ile MainMenu aktif sahne
   olana KADAR bekler (sabit ön-bekleme, boot'un kendi MainMenu navigasyonu tarafından
   eziliyordu — "world-enter: no adapter" bulgusu; EmberProofScreenshotDriver.cs:2469-2479).
6. **Dünyaya giriş kalıbı:** modlar `EmberWorldGenIntent.Pending = new EmberWorldGenIntent("grim",
   "wanderer", "crossroads")` ile karakter-yaratmanın AYNISI tohumu bırakıp `GeneratedWorld`
   sahnesini yükler — bare fallback değil, gerçek oynanır sahne (EmberProofScreenshotDriver.cs:322-329,
   438-441).
7. **Yakalama boru hattı:** pencere modunda `ScreenCapture.CaptureScreenshot` (UI dahil tam
   ekran); `-batchmode`'da swapchain OLMADIĞI için ana kamera offscreen RenderTexture'a
   açıkça render edilip PNG'ye okunur — overlay UI bu yolda GÖRÜNMEZ
   (EmberProofScreenshotDriver.cs:1926-1934, 1960-1980, 1982-2014). Modal yakalamalar
   `WaitForEndOfFrame` sınırından sonra yapılmak zorunda (UI Toolkit geçişi frame sonunda;
   :405-411) ve aynı-frame ikinci istek öncekini EZDİĞİ için 0.4 sn ayrımlarla serpiştirilir (:602-605).
8. **Doğrulama zincirinin CI ayağı:** GitHub Actions önce `static-audit.sh`'ı kaynak-only
   checkout'ta koşar (yalancı-yeşilleri yakalar), sonra EditMode testleri —
   "green is NOT runtime/LLM/art/build proof" adıyla, dürüst etiketli
   (.github/workflows/unity-test.yml:79-89, 124-125). Lokal makinede `run-validation.sh`
   Unity varsa gerçek EditMode, yoksa fallback harness koşar (tools/validation/run-validation.sh:205-217).
9. **Kadans:** proof koşuları isteğe bağlıdır (playtest/DoD anında elle veya ajan tarafından);
   CI static-audit + EditMode her push/PR'da, nightly cron yalnızca ağır opsiyonel build
   işini tetikler (.github/workflows/unity-test.yml:26-45).

## LLD - Veri Modeli

Bu sistem kalıcı veri tipi TANIMLAMAZ; ürettiği artefaktlar dosyadır. Sözleşmeleri:

### Sürücü durumu — `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs`
- `_outputDir : string` — tek örnek alan; `ResolveOutputDir()` sonucu (:21, 37).
- `TourScenes : string[]` — sahne turu listesi, tek kaynak `EmberScenes.GameplayTour`
  (EMB-056; :1832-1833).

### Çıktı artefaktları (dosya sözleşmeleri)
- PNG kareler: `shipcheck_modal/final.png` (:528, 534), `looptest_levelup/respawn/final.png`
  (:575, 601, 608), `lapse_000..089.png` (:666), `igtour_*.png` (:980-1025),
  `pt_01..pt_05_*.png` (:1056-1121), `look_*.png` / `ab_*.png` / `sky/weather/interior`
  serileri (:1151-1784), `tour_NN_<scene>_ui/noui.png` (:1842-1849),
  `input-proof_*.png` (:2414-2424), `planet-seed-{1,42,1234}.png` (:2178-2180).
- Metin raporları: `llm-proof.txt` (provider/model + gerçek yanıt; :1868-1910),
  `forge-die.txt` + `forge-die.png` (:2052-2124), `planet-proof.txt` (determinizm satırı
  dahil; :2215-2216), `world-proof.txt` (PASS/FAIL + sayaç dökümü; :2231-2398),
  `input-proof.log` (facade anlık görüntüleri; :2412-2427).
- Log-satırı sözleşmeleri: `SHIPCHECK [PASS|FAIL] <section>: <detail>` + `SHIPCHECK VERDICT:`
  (:433, 535), `[Marathon] VERDICT: PASS|FAIL — ...` (:953-956), `LOOP-PROOF: ...` (:548, 607),
  `[Playthrough] VERDICT:` (:1125).

### Fallback harness — `tools/validation/fallback/`
- `ValidationFallbackHarness.csproj` — net8.0 + NUnit 4.3.2; Domain/Simulation/Infrastructure/Data
  tümü + motor-bağımsız seçme Presentation dosyaları + `Assets/Tests/EditMode/**` derlenir
  (csproj:18-47). `GuardAgainstConflictMarkers` MSBuild hedefi BeforeBuild'de merge-marker
  taraması yapar (csproj:60-74; GuardConflictMarkers.ps1:5-27, guard-conflict-markers.sh:28-47).
- `UnityJsonUtilityStub.cs` — `UnityEngine.Vector3` + `JsonUtility`'nin System.Text.Json
  üstünde küçük taklidi; "Unity serileştirme paritesi İDDİASI DEĞİLDİR" (stub:84-87, 96-123).
- `Directory.Build.props` — obj/bin çıktısını `validation-output/fallback-harness/` altına
  yönlendirir (props:1-6).

### Digest golden — `Assets/Scripts/Simulation/Composition/WorldStateDigest.cs`
- `WorldStateDigest.Compute(WorldState) : string` — `WSDIGEST_v1` başlıklı kanonik metin
  (TIME, ACTORS, PLANTS, SOILS, JOBS, PRICES, STOCKPILES, CARAVANS, büyü bekleme/kalkan,
  EVENTS, QUESTS bölümleri sabit sırada) → SHA-256 hex (:18-54; aktör satırı pozisyon +
  IsIdle + CurrentJobId + Hunger/Fatigue/Thirst içerir, :80-99).
- `WorldTickDigestGoldenTests.BaselineHash` — elle güncellenen altın sabit
  `"e56cb763..."`; her re-baseline gerekçe yorumu bırakır (2026-06-10 olay-log şişmesi,
  2026-06-11 HarvestStep, CAN SUYU H1 tüketim döngüsü, 2026-07-23 M6 hasat-el-gerektirir;
  WorldTickDigestGoldenTests.cs:12-26).

## LLD - Fonksiyon Haritasi

Hepsi `EmberProofScreenshotDriver.cs` içinde (aksi yazılmadıkça):

- `static void Bootstrap()` — :23. Master bayrak kapısı + sürücü montajı.
- `IEnumerator Start()` — :35. Mod dispatcher + varsayılan tur + koşulsuz Quit.
- `IEnumerator RunGameplayShot()` — :311. Gerçek New Game tohumuyla spawn FPS + açılı tepeden kareler.
- `IEnumerator RunPlaythrough()` — :330. 11 karakter-yaratma adımının HER BİRİ + worldgen reveal +
  16 in-game ekran turu (33 kare; auto-advance çift-atlama düzeltmesi :344-349).
- `IEnumerator RunShipCheck()` — :424. F4 tek-komut regresyon paketi; bölüm kapıları aşağıda.
- `IEnumerator RunLoopProof()` — :541. F2 tam oyun döngüsü: quest → encounter → müzik BATTLE/DAY →
  levelup ekranı → trade → üretilmiş fetch görevi → taverna uykusu → ölüm-diriliş → silah farkı.
- `IEnumerator RunTimelapse()` — :623. Plaza kamerasından 90 kare × 10 sn (≈18 oyun saati,
  gece sokağa çıkma yasağı dahil); FPS controller kapatılır (:649-655).
- `IEnumerator RunAgentCheck()` — :674. DM kahini, gerçek NPC diyaloğu (selamlama + konu +
  serbest metin + İKİNCİ karşılaşma tanışıklığı), yoldaş alımı, envanter; REFORM #1 uzamsal
  invariantlar (aşağıda).
- `IEnumerator RunMarathon()` — :836. F34 30-dk otonom soak; `--ember-marathon-minutes N`
  kısaltır (:845-851); xorshift RNG seed 0xF34F34 (:862-867); eylem karışımı 2/8 travel,
  3/8 fight, 2/8 trade, 1/8 saat (:891-925); dakikalık heartbeat + bellek örnekleme (:927-936);
  hüküm `exceptions==0 && memEnd < 2×memStart && !aborted && actions>0` — iptal olmuş/hiçbir şey
  yapmamış koşu PASS diyemez ("Potemkin" dürüstlük kuralı; :941-956).
- `IEnumerator RunIgTour()` — :962. F32: HUD + envanter + karakter/journal/harita + pause +
  3 options sekmesi, 9 kare.
- `IEnumerator RunMainQuest()` — :1036. F31 üç-perde omurga: delve sandıkları → başkent bilgesi →
  final Warden + jenerik; hüküm `complete=True` (:1122-1126).
- `IEnumerator RunLookAround()` — :1129. Öz-playtest: 360° pan, R2 fog/ambient A/B tanısı
  (:1155-1171), perf probu (avg≤16ms; :1173-1183), bina içi/çiftlik/harita/gece/suç/gökyüzü/
  hava/iç mekân/büyü/zindan (takip, tuzak, boss, sandık, bestiary) kare serileri (:1184-1793).
- `IEnumerator RunRescueProof()` — :1799. Karakter yaratma + görünür worldgen yükleme ekranı +
  GeneratedWorld iki kez (kurtarma senaryosu).
- `IEnumerator RunSceneTour()` — :1836. Her gameplay sahnesi UI'li + tüm Canvas'lar kapalı
  ikişer kare (magenta/materyal avı; UrpMaterialRescue sahne başına tetiklenir; :1828-1852).
- `IEnumerator RunLlmProof()` — :1862. EMB-006: `ForgeLocator.NativeLlm` için 90 sn bekler,
  `Complete()`'i worker thread'de 240 sn zaman aşımıyla koşar, provenance + gerçek yanıtı
  `llm-proof.txt`'e yazar.
- `IEnumerator RunForgeProof()` — :2045. Canlı `IAssetForge` üzerinden sabit "carved bone die"
  üretimi; `--ember-forge-prompt/-negative/-size/-seed` ile parametrik (:2062-2070); 300 sn
  zaman aşımı; PNG + provenance.
- `IEnumerator RunPlanetProof()` — :2156. Seed {1, 42, 1234} küresel gezegen üretimi →
  equirectangular PNG; seed 42 için `digest = digest*31 + elev*1000 + plateId` yuvarlanan
  özeti yeniden-üretimle karşılaştırıp `DETERMINISTIC|MISMATCH` yazar (:2172-2207).
- `IEnumerator RunWorldProof()` — :2231. Motor-dışı domain koşusu: `WorldFactory().Create(1)` +
  `SeedWorld` + tam bir oyun günü tick döngüsü; NPC sabah/öğle/gece pozisyon örneklemesi
  (:2255-2270), demir külçe/smelt/quest/overland sayaçları; PASS koşulu
  `jobCompleted && ironIngotProduced && anyQuestComplete && overlandHasSettlements` (:2330).
- `IEnumerator RunInputProof()` — :2407. E7-020 Stage 0: yalnızca `EmberInput` facade çıktısı
  kaydeder, sentetik girdi ENJEKTE ETMEZ (:2412-2414).
- Yakalama yardımcıları: `CaptureToPng` :1960, `CaptureCameraToPng` :1982, `FindSceneCamera`
  :1937 (inaktif rig kamerasını zorla açar), `CaptureFixedAfter` :1910, `CaptureAfter` :1918,
  `CaptureOverheadAfter` :2016, `WritePlanetPng` :2218 (dikey çevirme).
- Argüman/senkron yardımcıları: `HasArg` :2481, `GetArg` :2489, `WaitForBootToSettle` :2473,
  `WaitDialog` :827 (LLM `IsThinking` bir frame SONRA yandığı için 1.5 sn ön-bekleme).
- Sürüş yardımcıları: `DriveToBuildSelection` :263, `DriveToDossier` :281,
  `MountWorldgenProof` :289 (maxRegions 2 / sentetik hata dahil projeksiyon).

`tools/validation/`:
- `run-validation.sh` — mod `auto|unity|fallback` (:17-57); Unity ikilisi env değişkenleri +
  PATH + bilinen Hub yolları sırasıyla aranır (:84-127); unity modu `-batchmode -runTests
  -testPlatform EditMode` (:129-159); fallback `dotnet test ... --logger trx` (:161-203);
  log `validation-output/latest.log`'a kopyalanır (:60-67).
- `static-audit.sh` — 6 bölüm: (1) çift .meta GUID HARD FAIL (:44-56); (2/2b) LFS pointer
  plugin/görsel — bilgi ya da `--require-runtime(-visual)` ile HARD FAIL (:61-96); (3) eksik
  .meta WARN (:103-111); (3b) takipli asset + takipsiz .meta HARD FAIL — temiz klonda GUID
  kırılması (:119-134); (3c) takipli .meta + gitignore'lu asset HARD FAIL — HYG-11 cuDNN/onnx
  boşluğu (:143-159); (4) yetim .meta WARN (:163-178); (5) bilgi grepleri: legacy Input,
  PlayerPrefs, Task.Run, `GetAwaiter().GetResult()` (:183-191); (6) determinizm sınır bekçisi —
  Domain + Data/Save içinde `UnityEngine.Random`/`DateTime.Now|UtcNow` HARD FAIL (:199-208).
- `check-sprint4-branch-hygiene.sh` — HEAD'in sprint-4 taban commit'inden indiğini ve eski
  soy commit'lerinin (52f2e1e/116ae2e) taze aralıkta olmadığını doğrular (:5-36).
- `run-worldgen-character-sample.ps1` + `sample/Program.cs` — worldgen seed 42 + sınıf önerisi
  yazan dotnet konsol dumanı testi (Program.cs:13-27).
- `analyze-generated-image.py` — forge PNG'si için yapı-mı-gürültü-mü hükmü: lag-1
  otokorelasyon, kanal-arası korelasyon, doygunluk, FFT düşük-frekans oranı (:1-14).

## LLD - Yazdigi/Okudugu Alanlar

Kanıt zinciri `FieldOwnershipRegistry`'de yazar olarak KAYITLI DEĞİLDİR — tasarım gereği:
sim alanlarına tick bandları dışında dokunmaz, adapter'ın `Proof*` yüzeyinden geçer (saat
ilerletme bile `ProofAdvanceHours` üzerinden kadans kapısına girer; shipcheck6 dersi,
bkz. 01-time-cadence §7). Yine de sunum katmanında DOĞRUDAN mutasyon yapar:

- **Yazar (sunum-katmanı, koşu-ömürlü):** `Application.runInBackground` (:29);
  `EmberWorldGenIntent.Pending` (:322, 438, 545 vb.); PlayerRig `transform.position/rotation`
  + `CharacterController.enabled` + `EmberFirstPersonController.enabled` (ışınlama/pan için;
  :639-656, 1141-1147, 1190-1207); `RenderSettings.fog/ambientLight` (R2 A/B, geri yüklenir;
  :1162-1171); `Canvas.enabled` (scene-tour noui kareleri; :1846-1849); kamera
  `targetTexture/fieldOfView/farClipPlane` (geri yüklenir; :1988-2012, 2023-2040);
  `Time` üzerinden değil — `adapter.ProofAdvanceHours` ile oyun saati (:921, 1247).
- **Okur:** `EmberDomainAdapterLocator.Current / WorldViewReadModel / ConsultFateOracle`
  (:443, 476, 697); `ForgeLocator.NativeLlm / AssetForge` (:1863, 2054);
  `Profiler.GetTotalAllocatedMemoryLong` (:860, 942); `Time.unscaledDeltaTime` (perf;
  :460-465, 1176-1182); `EmberInput` facade'ının tamamı (input-proof; :2431-2457);
  `WorldState` kökleri (WorldProof: `world.Events/Quests/Overland/PlayerInventory/Actors`;
  :2273-2323).
- **Digest okuru:** `WorldStateDigest.Compute` dünya-tick'in TÜM mutable depolarını salt-okur
  kanonikleştirir (WorldStateDigest.cs:35-54) — sahiplik defterinin "her alanın tek yazarı"
  iddiasının bağımsız denetçisidir: kayıt-dışı bir yazar drift'i digest'i oynatır ve golden
  test kırılır.

## LLD - Urettigi/Tukettigi Olaylar

- **Tükettiği WorldEventKind'lar (WorldProof):** `JobAssigned`, `JobCompleted`,
  `RecipeCompleted`, `QuestStarted`, `QuestCompleted` (EmberProofScreenshotDriver.cs:2273-2289);
  smelt tespiti reason metni + `ReasonTrace` "recipe:1001" fallback'i ile (:2515-2537).
- **Tükettiği WorldEventKind'lar (gate testleri):** `NeedChanged`, `ShortageDetected`,
  `JobAssigned`, `JobCompleted`, `PlantStageAdvanced`, `PlantHarvested`, `PriceChanged`,
  `CombatResolved`, `GuardResponded`, `WitnessRecorded`, `FactionReputationChanged`,
  `TradeCompleted`, `CaravanArrived`, `PlantPlanted`, `ChronicleEvent`
  (LivingWorldGateTests.cs:307-322); ayrıca `meal_eaten` reason öneki (:60, 87).
- **Ürettiği olay:** WorldEvent ÜRETMEZ (Proof* adapter bacakları üretir, o sistemlerin
  atlas sayfalarına aittir). Ürettiği şey log-tag sözleşmeleridir:
  `[Proof]` (×67), `[AgentCheck]` (×16), `[EmberProofScreenshotDriver]` (×7),
  `[Marathon]` (×5), `[Invariant]` (×3), `[Playthrough]`, `[WorldProof]`, `[Perf]`, `[R2]`,
  `[Timelapse]`, `[MainQuest]` ve satır-başı `SHIPCHECK` / `LOOP-PROOF` damgaları
  (dağılım dosya genelinde; örn. :433, 548, 785, 805, 953).
- **Digest golden'ın tükettiği:** olay logunun kendisi digest'e girer (`AppendEvents`,
  WorldStateDigest.cs:50) — olay spam'i golden'ı kırar; 2026-06-10 re-baseline tam bu yüzden
  yapıldı (WorldTickDigestGoldenTests.cs:12-15).

## Testler

- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` — golden pin:
  `Advance_OverTwoGameDays_MatchesCommittedBaselineDigest` (:39, çifte-koşu bayt-aynılık +
  BaselineHash eşitliği), `Advance_SameSeedSameTicks_ProducesSameDigest` (:54),
  `Advance_DifferentTickHorizons_ProduceDifferentDigests` (:62). Tohum dünyası
  `WorldLivesOverNTicksTests` ile hizalı (:80-110).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — Gate1 yaşanabilirlik (açlık<70,
  yorgunluk<75, öğün≥3×nüfus, stok DONMAMIŞ; :39-66), Gate2 pertürbasyon duyarlılığı (:68-97),
  Gate3 senaryosuz olay hızı ≥60/gün (:299-333), Gate4 penceresiz öğle kalabalığı dalgası
  amplitüd≥5 (:99-136), Gate5 olay kaskadı derinlik-3 saldırı→tanık→devriye (:139-166),
  Gate6 runtime tarih + fraksiyon değişimi + seed-farklı kronik (:168-197), Gate7 üç seed
  ikili-farklı yaşamlar (:200-226), Gate8 hücre başına ≤2 sivil (:228-251), Gate9 tanık
  hafızası diyalog prompt'una ulaşır (:253-273), Gate10 yoldaş sadakati (:275-297).
- `Assets/Tests/EditMode/CanSuyu/GateContractLintTests.cs` — sözleşmenin linti: her gate
  gövdesi `AdvanceDays(`/`composer.Advance(` içermek ZORUNDA, gate dosyasında
  `Screenshot|CaptureFrame|IsAtHour(12` yasak (:44-59); `docs/ROADMAP_V2_CAN_SUYU.md` DoD
  satırlarında "sabit saat"/"screenshot proof" vb. yasak (:63-80).
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs` — save/load sonrası digest
  aynılığı (:29-38, :88-100).
- `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` — çift roundtrip
  refleksiyonla alan-aynı (Home/DayAnchor sınıfı düşen-alan hatalarına karşı; :15-21).
- Fallback harness bu testlerin TAMAMINI `Assets/Tests/EditMode/**` wildcard'ı ile derler
  (ValidationFallbackHarness.csproj:45) — yani gate + golden testler Unity'siz makinede de
  koşar; PlayMode koşmaz.
- CI: `static-audit` işi → `editmode-tests` işi ona `needs` ile bağlı
  (.github/workflows/unity-test.yml:79-89, 124-128).

## Bilinen Borclar + Kacak Kapilari

Aile harfleri `docs/SYSTEMS_ATLAS.md:51-60` (a)-(g) sınıflamasına göre.

1. **README ölü bayrak belgeliyor** — README.md:78 hâlâ `--ember-proof-quit` yazar; kod bu
   bayrağın "hiç geçilmediğini" söyleyip koşulsuz Quit'e geçmiş
   (EmberProofScreenshotDriver.cs:203-205). Belge-kod drifti.
2. **Dispatch kopyala-yapıştır — sınıf (f).** 17 if-bloğunun her birine aynı 4 satırlık
   "PLAYTEST FIX" yorumu ve aynı Quit kuyruğu yapıştırılmış (:39-209); yeni mod ekleyen
   birinin Quit'i unutması yapısal olarak mümkün.
3. **ResolveOutputDir argüman yutar** — master bayrağın hemen ardındaki argüman KÖR alınır
   (:2460-2465); `--ember-proof-screenshots --ember-lookaround` yazılırsa çıktı dizini
   `--ember-lookaround` adlı klasör olur. Doğrulama yok.
4. **ShipCheck exception sayacı aboneliği geri alınmaz** — anonim lambda
   `logMessageReceived`'e eklenir, hiç çıkarılmaz (:427); Marathon kendi handler'ını düzgün
   söker (:858, 941). Quit izlediği için zararsız ama kalıp tutarsız.
5. **Batchmode yakalama overlay UI'yi GÖREMEZ** — kamera-render yolu yalnız 3D sahne
   (:1926-1934, 1963-1967); headless igtour/modal kareleri bu yüzden pencere modunda koşulmalı.
   Bu sınır kodda yorumla itiraf edilmiş ama hiçbir yerde makine-okur biçimde işaretli değil.
6. **Async screenshot zamanlama koreografisi — sınıf (f).** Aynı-frame ikinci istek öncekini
   ezer (:602-605); çözüm dosya geneline serpilmiş 0.25-0.4 sn beklemeler. Kare kaybı sessiz
   olur, koşu FAIL olmaz.
7. **Proof koşuları sahneyi mutasyona uğratır** — FPS controller/CharacterController kapatma,
   rig ışınlama, RenderSettings elleme (:649-656, 1141-1147, 1162-1171). Yalnızca "sonunda
   Quit var" varsayımıyla güvenli; mod sonunda geri-alma sözleşmesi yok.
8. **Uzamsal invariant eşikleri elle gömülü — sınıf (d).** Aktör drift eşiği 5.5 m (:780),
   pencere-duvar kabuğu 0.13 (:799) — 3× gömülü-pencere hatasını pinliyorlar ama yalnızca
   `--ember-agentcheck` koşusunda LOUD'lar; shipcheck bu invariantları koşmaz.
9. **`--ember-input-proof` Stage 0** — sentetik girdi enjeksiyonu YOK, yalnız facade anlık
   görüntüsü (:2412-2414). "Input kanıtı" adı vaadinden büyük.
10. **Fallback harness dürüst-KISMÎ** — "pure-C# source tests only; does NOT validate Unity
    compile, scenes, assets, .meta, plugins, or PlayMode" (run-validation.sh:198);
    `UnityJsonUtilityStub` serileştirme paritesi iddia etmez (stub:84-87). Yeşil fallback,
    yeşil oyun DEĞİLDİR — bu boşluğu static-audit + CI etiketi kapatmaya çalışır
    (unity-test.yml:125).
11. **Golden re-baseline el süreci** — `BaselineHash` elle güncellenir; meşru davranış
    değişikliği ile kazara drift diff'te AYNI görünür, ayrım yorum disiplinine emanet
    (WorldTickDigestGoldenTests.cs:12-26). Digest'i üreten koşunun "ikinci koşu aynı mı"
    kontrolü testin içinde (:41-46) ama baseline'ı üreten aracın kendisi repo'da yok.
12. **Marathon/Timelapse duvar-saati bağımlı — sınıf (g) kıyısı.** `WaitForSecondsRealtime`
    tabanlı soak süreleri makine hızına göre farklı sayıda eylem üretir (:930-938); hüküm
    eşiği (`actions>0`, bellek 2×) bunu tolere edecek kadar gevşek, ama koşular arası
    karşılaştırma anlamlı değil.
13. **`analyze-generated-image.py` yetim** — repo içinde onu çağıran script/CI bulunamadı
    (tools/, Docs/, .github taraması boş döndü); elle çağrılan yardımcı. Otomatik forge
    kapısı değil — doğrulanmadı sayılmasın diye not: taramam çağıran satır bulamadı, bu
    "hiç kullanılmıyor" kanıtı değildir.
14. **Sentetik hata varsayılan turda kasıtlı** — mod bayraksız koşuda
    `includeSyntheticFailure: true` (:302); "assetgen_failures" karesi TASARIM gereği hata
    gösterir. Bilmeyen biri bunu gerçek regresyon sanabilir.
15. **PlayMode CI'dan çıkarılmış** — eski PlayMode "testleri" prosedürel gradyan PNG üretiyordu,
    gerçek görsel kanıt değildi; 2026-05-15 denetimiyle söküldü (unity-test.yml:5-16).
    Gerçek render-regresyon kapısı hâlâ açık iş.

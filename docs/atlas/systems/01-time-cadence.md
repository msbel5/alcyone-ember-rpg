# 01-time-cadence

> Kapsam: GameTime, WorldTickComposer (per-tick replay + boundary stamps), WorldTickRegistry,
> DefaultTickSystems, FieldOwnershipRegistry, EmberTickDriver, tick konfigürasyonu.
> Kanıt biçimi: `dosya:satır`. Tüm yollar `Assets/` köklüdür.

## HLD - Ne ve Neden

Zaman-kadans sistemi, oyunun determinizm anayasasının kalbidir: tek bir tamsayı saat
(ember tick) bütün simülasyonu sürer; aynı seed + aynı tick dizisi bayt-aynı WorldState ve
olay logu üretmek ZORUNDADIR (bunu `WorldStateDigest` SHA-256 ile pinler). 1 ember-tick =
1 oyun dakikasıdır (`WorldTickComposer.cs:46-52`); gerçek zamanda bir tick 0.8333 sn sürer,
yani bir oyun günü (1440 tick) ≈ 20 gerçek dakikadır (`EmberTickDriver.cs:15-18`). Oyuncuya
görünen etkisi: gündüz/gece döngüsü, NPC rutinleri, ekin büyümesi, fiyat kayması ve kervanlar
hepsi aynı saatten akar; travel/wait/rest gibi hızlı-ileri sarmalar, aynı süreyi tek tek
oynamakla BİREBİR AYNI tarihi üretir (chunking invariance). Felsefe, Daggerfall Unity'den
alınan "tek tamsayı saat her simülasyonu sürsün" dersinin uygulamasıdır (docs/SYSTEMS_ATLAS.md
§4). Sistem ayrıca (c) sınıfı hata ailesinin ("kadans yazar çatışmaları") yapısal panzehiridir:
Reform #2 ile her mutable alanın yazarı kadans+sıra etiketiyle `FieldOwnershipRegistry`'de ilan
edilir ve catch-up SINIR DAMGALI per-tick replay ile yapılır — 40-tick'lik bir sıçrama ile
kırk adet 1-tick'lik çağrı özdeş tarih üretir (`WorldTickComposer.cs:236-241`).

## HLD - Akis

1. **Gerçek zaman → tick:** `EmberTickDriver` (MonoBehaviour) her frame gerçek zamanı biriktirir;
   `_tickIntervalSeconds = 0.8333f` dolunca `CurrentTick++` ve `Listener.OnTick(CurrentTick)`
   çağrılır; frame başına en çok `_maxCatchupTicksPerFrame = 8` tick (`EmberTickDriver.cs:18-19,
   44-56`). Pause ve save-align (`AlignTo`) driver seviyesindedir (`EmberTickDriver.cs:27, 37-42`).
2. **Host köprüsü:** `EmberWorldHost` `EmberTickDriver.ITickListener`'ı uygular
   (`Presentation/Ember/Bootstrap/EmberWorldHost.cs:21`), boot'ta `_clock.AdvanceTick(0)` ile
   saat çapasını atar (`EmberWorldHost.cs:99`); her tick `OnTick` →
   `_worldViewProjector.ProjectTick(tickIndex)` (`EmberWorldHost.Bindings.cs:44-47`).
3. **Projeksiyon sırası:** `WorldViewProjector.ProjectTick` önce `_clock.AdvanceTick(tickIndex)`
   (sim ilerler), sonra `Project()` (görünümler senkron), sonra HUD olay logu render eder
   (`Presentation/Ember/Views/WorldViewProjector.cs:63-69`).
4. **Adapter:** `DomainSimulationAdapter.AdvanceTick` önce off-thread LLM sonuç kuyruğunu ana
   thread'de boşaltır (DET-02), `_tick`'i günceller, `_tickComposer.Advance(_world, tickIndex)`
   çağırır, olay yankıları + alan aynasını yayınlar
   (`Presentation/Ember/Adapters/DomainSimulationAdapter.Clock.cs:6-13`). Composer örneği
   adapter'da yaşar (`DomainSimulationAdapter.cs:32, 65`).
5. **Composer per-tick replay:** `WorldTickComposer.Advance` delta = yeni tickIndex − son;
   İLK çağrı baseline olarak YUTULUR (delta 0; `WorldTickComposer.cs:229-231` — N günlük
   döngü için N+1 çağrı gerekir). Sonra delta tick TEK TEK replay edilir: her adımda PerTick
   bandı koşar; `_ticksSinceHourly` `TicksPerGameHour`'a ulaşınca Hourly bandı, `_ticksSinceDaily`
   `TicksPerGameDay`'e ulaşınca Daily bandı TAM SINIR ANINDA koşar (`WorldTickComposer.cs:242-280`).
   Sınır anında `world.Time` zaten o tick ilerletilmiş olduğundan sistemlere geçen
   `TickContext.Stamp` sınır damgasının kendisidir (`WorldTickComposer.cs:255, 259`).
6. **Kadans sabitleri tek kaynaktan:** `MinutesPerTick` (varsayılan 1) runtime opsiyonundan gelir;
   `TicksPerHour`/`TicksPerDay` `Normalize()` içinde `GameTime.MinutesPerHour/Day ÷ MinutesPerTick`
   olarak TÜRETİLİR (60/1440) — 240/10 dönemindeki "gün = 4 oyun-saati" desync'ini yapısal olarak
   kapatır (`Domain/Configuration/EmberRuntimeOptions.cs:75-81, 249-255`).
7. **Hızlı ileri sarmalar aynı kapıdan geçer:** `ProofAdvanceHours` saat-adımlı ilerler (tek
   sıçramada hourly/daily sınırları atlanırdı — shipcheck6 bulgusu;
   `DomainSimulationAdapter.WorldEncounter.cs:85-100`); `AdvanceTravelDay` günlük,
   `WaitHours` saatlik sarar (`DomainSimulationAdapter.Travel.cs:80-86`).
8. **Save/load:** restore sonrası `RebuildAccumulatorsFrom(_world.Time)` hourly/daily
   akümülatör fazını mutlak oyun zamanından yeniden türetir (DET-01; soğuk yüklemede bellek
   akümülatörleri 0 olduğundan `ResetAnchor` tek başına yetmiyordu —
   `DomainSimulationAdapter.Save.cs:56-63`); driver tarafında `AlignTo` sayaç çakışmasını önler
   (`EmberTickDriver.cs:29-42`).

## LLD - Veri Modeli

| Tip | Alanlar / içerik | Kanıt |
|---|---|---|
| `GameTime` (readonly struct) | `_totalMinutes: long` tek gerçek; sabitler `MinutesPerHour=60`, `MinutesPerDay=1440`, `MinutesPerMonth=43200`, `MinutesPerYear=518400`, `DaysPerMonth=30`, `MonthsPerYear=12`, `DaysPerYear=360`; türev bileşenler `Minute/Hour/DayOfMonth/Month/Year/DayOfYear`; `Add*` ve karşılaştırma/fark operatörleri | `Domain/Core/GameTime.cs:9-89` |
| `TickCadence` (enum) | `PerTick=0, Hourly=1, Daily=2` | `Simulation/Composition/TickCadence.cs:3-8` |
| `TickContext` (readonly struct) | `World: WorldState`, `Stamp: GameTime` (sınır damgası), `Delta: int` (replay'de hep 1) | `Simulation/Composition/TickContext.cs:6-18` |
| `IWorldTickSystem` | `Id: string`, `Cadence: TickCadence`, `Order: int`, `Run(in TickContext)` | `Simulation/Composition/IWorldTickSystem.cs:3-9` |
| `WorldTickRegistry` | `_ordered/_perTick/_hourly/_daily` dizileri; ctor null/boş-id/çift-id doğrulaması; sıralama kadans → order → ordinal id | `Simulation/Composition/WorldTickRegistry.cs:8-34, 41-50` |
| `WorldTickComposer` (durum) | `_tickRegistry`, `_lastTickIndex` (−1 = çapasız), `_ticksSinceHourly`, `_ticksSinceDaily`; statik profiler alanları `SystemWatch/TickCosts/PerfLog/SlowTickMs=12ms` | `WorldTickComposer.cs:69-73, 218-221` |
| `TickRuntimeOptions` | `MinutesPerTick=1L`; `TicksPerDay=1440` ve `TicksPerHour=60` (Normalize'da TÜRETİLİR); `LowStockThreshold/HighStockThreshold/PriceStep` (fiyat sistemine sızıntı, burada taşınır) | `Domain/Configuration/EmberRuntimeOptions.cs:75-85` |
| `SeasonCalendar` | sıralı, örtüşmesiz `SeasonDefinition` satırları; ctor doğrulaması | `Domain/Time/SeasonCalendar.cs:9-37` |
| `SeasonDefinition` | `Season`, `StartDayOfYear`, `EndDayOfYear` (1-tabanlı, kapsayıcı), `ContainsDay` | `Domain/Time/SeasonDefinition.cs:7-40` |
| `Season` (enum) | `None/Spring/Summer/Autumn/Winter`; davranış enum'a değil veri satırlarına bağlanır | `Domain/Time/Season.cs:4-11` |
| Varsayılan takvim | 90'ar günlük 4 mevsim, Bahar 1. gün | `WorldTickComposer.cs:88-98` |
| `FieldOwnershipRegistry` | `Writers: IReadOnlyDictionary<string, string[]>` — alan → `"systemId@Cadence:Order"` yazar listesi; yürütülebilir dokümantasyon | `Simulation/Composition/FieldOwnershipRegistry.cs:12-53` |
| `WorldStateDigest` | kanonik metin (TIME, ACTORS, PLANTS, SOILS, JOBS, PRICES, STOCKPILES, CARAVANS, SPELL_COOLDOWNS, SHIELD_BUFFS, EVENTS, WORLDQUESTS bölümleri) → SHA-256 hex; WORLDQUESTS bölümü boşken ATLANIR ki eski golden'lar bayt-aynı kalsın | `Simulation/Composition/WorldStateDigest.cs:18-54, 380-390` |

## LLD - Fonksiyon Haritasi

**Çekirdek:**
- `WorldTickComposer.Advance(WorldState world, int tickIndex): void` — `WorldTickComposer.cs:226` — delta'yı per-tick replay eder; idempotent; geri gidiş çapayı yeniler, domain'i geri sarmaz.
- `WorldTickComposer.ResetAnchor(): void` — `WorldTickComposer.cs:309` — `_lastTickIndex = -1`; hourly/daily akümülatörleri KASITLI korunur (sekizinci pas A-P2).
- `WorldTickComposer.RebuildAccumulatorsFrom(GameTime worldTime): void` — `WorldTickComposer.cs:322` — akümülatörleri `TotalMinutes mod TicksPerGameHour/Day`'den türetir (dokuzuncu pas A-P2; DET-01).
- `WorldTickComposer.MinutesPerTick/TicksPerGameDay/TicksPerGameHour` (statik özellik) — `WorldTickComposer.cs:52, 59, 67` — runtime opsiyonlarına delege.
- `DefaultTickSystems.Create(...): WorldTickRegistry` — `DefaultTickSystems.cs:27` — 21 adaptör adımını kaydeder (aşağıdaki bant tablosu).
- `WorldTickRegistry..ctor(IEnumerable<IWorldTickSystem>)` — `WorldTickRegistry.cs:13` — doğrular, sıralar, bantlara böler; `Compare` kadans→order→id — `WorldTickRegistry.cs:41`.
- `GameTimeAdvanceSystem.Advance(GameTime, long minutes): GameTime` — `Simulation/Time/GameTimeAdvanceSystem.cs:22` — negatif dakika fırlatır, saf ekleme.
- `GameTimeAdvanceSystem.Advance(GameTime, long, WorldEventLog, SiteId): GameTime` — `GameTimeAdvanceSystem.cs:30` — geçiş olayları da yazar (runtime'da ÇAĞRILMIYOR, bkz. Borçlar).
- `SeasonCalendar.GetSeason/TryGetSeason/IsSeasonBoundary` — `SeasonCalendar.cs:43, 53, 70`.
- `WorldStateDigest.Compute(WorldState): string` — `WorldStateDigest.cs:18` — determinizm parmak izi.

**Sürücü zinciri:**
- `EmberTickDriver.Update()` — `EmberTickDriver.cs:44` — akümülatör + sınırlı catch-up döngüsü.
- `EmberTickDriver.AlignTo(int)` — `EmberTickDriver.cs:37` — restore sonrası sayaç hizası (PR #185 P1).
- `EmberWorldHost.OnTick(int)` — `EmberWorldHost.Bindings.cs:44` — projektöre delege.
- `WorldViewProjector.ProjectTick(int)` — `WorldViewProjector.cs:64` — advance → sync → HUD sırası.
- `DomainSimulationAdapter.AdvanceTick(int)` — `DomainSimulationAdapter.Clock.cs:6` — kuyruk boşalt + composer + yayınlar.
- `DomainSimulationAdapter.ProofAdvanceHours(int)` — `DomainSimulationAdapter.WorldEncounter.cs:91` — saat-adımlı sarma; hedef `world.Time` dakikası, `hours*4+8` guard'lı döngü.
- `DomainSimulationAdapter.AdvanceTravelDay()` / `WaitHours(int)` — `DomainSimulationAdapter.Travel.cs:81-86`.
- `EmberRuntimeOptionsProvider.Normalize` (tick bölümü) — `EmberRuntimeOptions.cs:249-255` — `TicksPerHour/Day` türetimi.

**Kayıtlı bant tablosu (kanonik sıra; id@Kadans:Order → kanıt satırı = `base(...)` çağrısı, hepsi `DefaultTickSystems.cs`):**

| Bant | Sıra | Id | Satır | Tek cümle |
|---|---|---|---|---|
| PerTick | 10 | `core.time` | :92 | `world.Time += Delta × MinutesPerTick` (`:97-102`) |
| PerTick | 20 | `core.magic` | :110 | oyuncu büyü bekleme/kalkan tick'leri |
| PerTick | 20 | `living.schedule` | :219 | NPC tile-adım yürüyüşü (Hourly'de sürünüyordu, bilinçli PerTick) |
| PerTick | 21 | `living.companion_follow` | :311 | yoldaş heel-follow, schedule'dan SONRA |
| PerTick | 22 | `living.eatOnArrival` | :259 | varış anında yemek (P0 yığılma çözümü) |
| Hourly | 10 | `econ.jobs` | :128 | iş talep/claim + reçete ilerletme, JobAssigned olayı |
| Hourly | 15 | `quest.tick` | :241 | görev sistemi saatlik tick |
| Hourly | 30 | `living.needs` | :361 | ihtiyaç rampaları + mood; saatte TEK özet olay (`:380-398`) |
| Hourly | 35 | `living.consumption` | :296 | metabolizma; saat `Stamp`'ten türetilir (`:301-302`) |
| Hourly | 40 | `living.predation` | :328 | avcılar sim içinde avlanır |
| Hourly | 42 | `living.companion_guard` | :319 | yoldaş gard vuruşu |
| Hourly | 45 | `living.witness` | :337 | tanık belleği + devriye yakınsaması |
| Hourly | 50 | `living.ambient` | :271 | fare/kedi ambient yaşamı, gerçek stok |
| Hourly | 55 | `living.rumors` | :283 | olay → kasaba dedikodusu |
| Daily | 10 | `world.caravans` | :406 | kervan hareketi |
| Daily | 20 | `econ.plantgrowth` | :480 | mevsim-kurallı ekin büyümesi |
| Daily | 25 | `world.harvest` | :424 | olgun bitki → stok +2, yeniden ekim; büyüme(20)→hasat(25)→fiyat(30) aynı-gün zinciri |
| Daily | 27 | `econ.shortage_response` | :348 | kıtlık süpürmesi → ekim işi |
| Daily | 28 | `world.runtime_history` | :514 | günlük olay→ilişki kayması + aylık kronik |
| Daily | 30 | `econ.prices` | :523 | stok eşikli fiyat kayması |
| Daily | 40 | `politics.faction_decay` | :559 | itibar nötrleşmesi; `ShouldApply` gün indeksini damga aritmetiğinden çıkarır (`:574-579`) |

## LLD - Yazdigi/Okudugu Alanlar

Bu sistemin KENDİ yazdığı tek alan: **`World.Time`** — yazar `core.time@PerTick:10`
(`DefaultTickSystems.cs:97-102`). DİKKAT: `FieldOwnershipRegistry`'de `World.Time` satırı YOK —
saat yazarlığı defterde ilan edilmemiş (bkz. Borçlar #3).

Defterin ilan ettiği satırlar (aynen; `FieldOwnershipRegistry.cs:18-52`):
- `Actor.Position` ← `living.schedule@PerTick:20`, `living.companion_follow@PerTick:21`, `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.ambient@Hourly:50`
- `Actor.Needs` ← `living.needs@Hourly:30`, `living.eatOnArrival@PerTick:22`, `living.consumption@Hourly:35`
- `Actor.Vitals` ← `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.companion_guard@Hourly:42`
- `World.GuardPursuits` ← `living.witness@Hourly:45` (kurar/tazeler), `living.schedule@PerTick:20` (çözer/budar)
- `World.Stockpiles` ← `world.harvest@Daily:25`, `living.eatOnArrival@PerTick:22`, `living.consumption@Hourly:35`, `living.ambient@Hourly:50`, `econ.trade@Daily:28` (⚠ hayalet id, bkz. Borçlar #1)
- `World.Rumors` ← `living.rumors@Hourly:55`
- `World.SiteUnrest` ← `living.witness@Hourly:45`

Composer'ın okuduğu kapı alanları: `world.Actors` ve `world.Events` null ise Hourly bandı,
`world.Events` null ise Daily bandı ATLANIR ama sınır sayaçları yine tüketilir
(`WorldTickComposer.cs:252-279`). Kadans sabitleri `EmberRuntimeOptionsProvider.Current.Tick`'ten
okunur (`WorldTickComposer.cs:52-67`).

## LLD - Urettigi/Tukettigi Olaylar

**Üretilen (bu sistemin öz olayları):**
- `WorldEventKind.DayAdvanced` ve `WorldEventKind.SeasonChanged` — yalnız
  `GameTimeAdvanceSystem`'in 4-argümanlı `Advance` overload'ı yazar
  (`GameTimeAdvanceSystem.cs:46-89`). **Runtime'da hiç üretilmez**: `TimeStep.Run` 2-argümanlı
  overload'ı çağırır (`DefaultTickSystems.cs:99-101`); repo grep'inde 4-arg overload'ın tek
  çağrıcısı testlerdir (`Assets/Tests/EditMode/Time/GameTimeAdvanceSystemTests.cs:38, 68, 91`).
  `WorldEventNarrator` bu iki tür için anlatı satırı taşır ama besleyen yok
  (`Presentation/Visual/WorldEventNarrator.cs`).
- Log tag `TickPerf` — 12 ms'i aşan tick'lerde sistem-başına maliyet dökümü `EmberLog.Warn`
  (`WorldTickComposer.cs:218-221, 282-293`). Sink bağlı değilse sessiz.

**Damgası bu sistemce belirlenen (bant adımlarının olayları):** sınırda koşan her adım olay
damgasını `context.Stamp`'ten alır — ör. `JobAssigned` (`DefaultTickSystems.cs:149-162`),
`NeedChanged` saatlik özeti (`:386-398`), `PlantHarvested` (`:460-463`). Kural (CAN SUYU V2,
MultiDayCatchup invariantı): pencere/gün-indeksi/olay damgası HEP `context.Stamp`'ten türetilir,
`world.Time`'dan değil — per-tick replay'de ikisi sınır anında eşittir ama kural stateless'lığı
korur.

**Tüketilen:** composer olay tüketmez; `WorldStateDigest.AppendEvents` tüm `WorldEventLog`'u
parmak izine katar (`WorldStateDigest.cs:442-481`).

## Testler

Hepsi `Assets/Tests/EditMode/` altında:

- `Composition/CadenceChunkingInvarianceTests.cs` — tick-tek-tek vs düzensiz parçalar (1,7,13,…)
  2 oyun günü boyunca ÖZDEŞ olay logu; boundary-stamp kontratının yük taşıyan invariantı
  (`:9-45`). İlk koşusunda gerçek composer bug'ı yakaladı → per-tick replay reformu.
- `Composition/WorldTickComposerReplayTests.cs` — save/load replay-eşdeğerliği:
  `RebuildAccumulatorsFrom` sürekli koşuyu yeniden üretir, eski `ResetAnchor`-tek-başına yolunun
  üretmediği de ayrıca kanıtlanır (`:9-18`).
- `Composition/WorldTickDigestGoldenTests.cs` — golden SHA-256 digest; davranış değişimlerinde
  bilinçli re-baseline geçmişi dosya başında (`:10-22`).
- `Composition/WorldTickRegistryTests.cs` — registry sıralama/çift-id doğrulaması.
- `Composition/FieldOwnershipRegistryTests.cs` — sahiplik lint'i: ilan edilen her yazar bilinen
  id olmalı; çekirdek alanların satırı olmalı (`:11-40`; zaafı için Borçlar #1).
- `Composition/CatchupPerfPinTests.cs` — 14 günlük catch-up interaktif kalmalı (33 ms ölçüm,
  150x pay; `:8-12`).
- `Composition/LiveScaleCatchupPerfPinTests.cs` — canlı ölçek (≈40 site / 800 sivil) 1 replayed
  gün pini; EatOnArrival kuadratik regresyon bekçisi (`:12-16`).
- `Composition/WorldLivesOverNTicksTests.cs` — SOUL-01/02 kabul kapısı: gerçek composer'la dünya
  GÖRÜNÜR ilerler (ekin/iş/fiyat) (`:11-18`).
- `Composition/WorldNpcDailyRhythmTests.cs` — günlük ritim (10:00 iş örneklemesi) (`:23-25`).
- `Composition/WorldTickFactionDecayTests.cs` — günlük itibar nötrleşmesi + MultiDayCatchup
  bağlantılı pinler.
- `Time/GameTimeAdvanceSystemTests.cs` — GameTime ilerletme + DayAdvanced/SeasonChanged geçiş
  satırları (yalnız burada üretilirler).
- `Save/SaveLoadDigestRoundtripTests.cs` — digest üstünden save/load turu.
- `CanSuyu/LivingWorldGateTests.cs` + `CanSuyu/GateContractLintTests.cs` — emergence kapıları
  composer'ı gerçek kadansla sürer; lint sabit-saat/screenshot-kanıt gerilemesini reddeder.

## Bilinen Borclar + Kacak Kapilari

1. **Sahiplik lint'i elle tutulan listeye karşı koşuyor (c-ailesi bekçisinde delik).**
   `FieldOwnershipRegistryTests.knownIds` gerçek registry'den TÜRETİLMİYOR, elle yazılmış
   (`FieldOwnershipRegistryTests.cs:14-22`) ve kayıtlı id'lerle uyumsuz: listede `world.growth`,
   `econ.trade`, `world.shortage`, `world.history`, `econ.caravan`, `faction.decay` var; gerçek
   id'ler `econ.plantgrowth`, `econ.shortage_response`, `world.runtime_history`,
   `world.caravans`, `politics.faction_decay` ve **`econ.trade` diye kayıtlı sistem hiç yok**
   (kayıt listesi `DefaultTickSystems.cs:41-65`). Defterdeki `econ.trade@Daily:28` yazarı hayalet
   (`FieldOwnershipRegistry.cs:49`; gerçek Daily:28 = `world.runtime_history`,
   `DefaultTickSystems.cs:514`). "İlan edilen yazar GERÇEK sistem olmalı" iddiası bu haliyle
   ölü/yeniden-adlanmış id'leri yakalayamaz — lint registry'nin `Ordered` listesinden
   beslenmelidir.
2. **DayAdvanced/SeasonChanged runtime'da hiç doğmuyor.** Olay-yazan overload'ı yalnız testler
   çağırır (yukarıda kanıtlı); canlı loop 2-arg overload kullanır (`DefaultTickSystems.cs:99-101`).
   Gün/mevsim geçişini dinlemek isteyen her tüketici bugün kendi damga aritmetiğini yazmak
   zorunda — (c)/(g) ailesi tekrarına açık kapı.
3. **`World.Time` sahiplik defterinde yok.** Saatin tek yazarı `core.time` fiilen doğru ama ilan
   edilmemiş (`FieldOwnershipRegistry.cs:15-53`'te satır yok); Reform #2'nin "ikinci yazar CI
   olayıdır" vaadi saat alanını kapsamıyor.
4. **Sessiz bant atlama:** `world.Actors`/`world.Events` null iken Hourly (ve Events null iken
   Daily) bandı koşmaz ama sınır sayacı tüketilir (`WorldTickComposer.cs:252-279`) — "sessiz"
   bir dünya hourly tarihini hatasız kaybeder. Test dünyaları hep dolu olduğundan pinlenmemiş.
5. **İlk tick baseline olarak yutulur** (`WorldTickComposer.cs:229-231`): N-gün döngüsü gün
   sınırlarını 1..N−1 damgalar; gün-30 olayı 31 günlük döngü ister (CAN SUYU V2 kuralı).
   Bilerek böyle, ama her yeni test yazarında bir kez tökezleten tuzak.
6. **O(delta) catch-up gerçek saniye öder:** uzun travel/wait per-tick replay ile ilerler
   (`WorldTickComposer.cs:236-241`); perf pinleri (`CatchupPerfPinTests`,
   `LiveScaleCatchupPerfPinTests`) bekçi, ama kadro/dünya büyüdükçe pin bütçesi yeniden ölçülmeli.
7. **`ProofAdvanceHours` tam-saat garantisi zayıf:** `ticksPerHour = TicksPerGameDay/24`
   (`WorldEncounter.cs:92`) `MinutesPerTick` böleni tutmayınca yamulur; döngü hedef-dakika +
   `hours*4+8` guard ile telafi eder (`:96-99`) — 8 saatlik ilerlemenin +7h çıktığı vaka
   yaşandı (gözlem 13178, 2026-06-12).
8. **`Normalize` bölünebilirlik denetlemez:** `TicksPerHour = 60/MinutesPerTick` tamsayı bölmesi
   (`EmberRuntimeOptions.cs:254-255`); `MinutesPerTick=7` gibi bir değer saati 56 dakikaya
   sıkıştırır — geçersiz kombinasyon sessizce kabul edilir.
9. **Statik profiler durumu:** `SystemWatch`/`TickCosts` static (`WorldTickComposer.cs:218-219`);
   iki composer aynı anda Advance ederse (test paralelliği, çok-dünya) maliyet raporu karışır —
   determinizmi bozmaz, teşhisi bozar.
10. **Stateless'lık sözleşmesi lint'siz:** registry adım örnekleri dünyalar arası paylaşılır;
    tick sistemleri stateless olmalı ve damgayı `context.Stamp`'ten türetmeli (CAN SUYU V2
    kuralı) — bunu doğrudan zorlayan analizör/lint yok, yalnız `CadenceChunkingInvarianceTests`
    dolaylı yakalar.
11. **`FactionDecayStep.ShouldApply` damga→gün çevirisini `TicksPerGameDay × MinutesPerTick`
    çarpımıyla yapar (`DefaultTickSystems.cs:574-579`)** — türetilmiş sabitlerle doğru ama gün
    indeksinin üçüncü bir hesaplanış biçimi; #2'deki eksik DayAdvanced olayının dolaylı maliyeti.
12. **Hata ailesi bağları:** bu sistem (c) ailesinin (kadans yazar çatışması) kök çözümüdür
    (Reform #2, docs/SYSTEMS_ATLAS.md §3/§5); #1-#3 borçları o reformun henüz kapanmamış
    kenarlarıdır. (g) ailesine (mod önyargısı/unchecked sayaç) komşu riskler #7-#8'dedir.

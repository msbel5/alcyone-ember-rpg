# 07-history-rumors

Tarih + kronik + dedikodu hattı: `WorldEventLog` (omurga), `RuntimeHistorySystem` (runtime kronik),
`RumorMillSystem` (kasaba dedikodusu), `NpcEventEchoFeed` + `NpcEventEchoView` (olay piktogramları).
Tüm satırlar koddan doğrulandı; doğrulanamayanlar açıkça `dogrulanmadi` etiketiyle işaretli.

## HLD - Ne ve Neden

Worldgen'in HistorySystems'i zengin bir kronik yazar ama dakika sıfırda DONAR — bundan sonra hiçbir
faction bir diğeri hakkında fikir değiştirmezdi (`RuntimeHistorySystem.cs:6-13` tasarım notu).
Bu hat, dünyanın oyun BAŞLADIKTAN sonra da tarih yazmasını sağlar: dünkü simülasyon olayları
faction ilişkilerini kaydırır, her ay sonunda tohumlu bir kronik olayı somut mekanik etkiyle yazılır
("never just a log line"). Omurga `WorldEventLog`: append-only, deterministik, null kabul etmeyen tek
chronicle (`WorldEventLog.cs:5-15`); tarih de dedikodu da NPC üstü ikonlar da hep bu tek log'un
OKUYUCUSUDUR. `RumorMillSystem` LLM'siz, saf formatter: yeni olayları tek satırlık kasaba lafına
damıtır, 3 gün ömür, 32 tavan, cursor save'e yazılır ki load eski haberi yeniden öğütmesin
(`RumorMillSystem.cs:7-11`). Oyuncuya görünen yüzü: "Any news?" diyalog cevabı
(`DomainSimulationAdapter.Dialog.Source.cs:294-303`), selamlaşmalara %35 ihtimalle eklenen olay
anlatısı (`DomainSimulationAdapter.Dialog.Text.cs:85-89`), aylık kronikle değişen faction ilişkileri,
boşalan yerleşimlere gelen göçmenler ve NPC kafası üstünde 3.5 saniyelik olay piktogramları
(göz/ünlem/kılıç/demet/konuşma — `NpcEventEchoView.cs:5-9`). Felsefe: stateless step instance'ları
(H1 dersi, `RuntimeHistorySystem.cs:13`), her şey (RoomSeed, dayIndex)'ten türetilir — iki dünya iki
FARKLI tarih yaşar; catchup çok günlük sıçramalarda birebir aynı replay'i üretir
(`RuntimeHistorySystem.cs:35-36`).

## HLD - Akis

1. **Üretim (sürekli):** Simülasyon sistemleri tick boyunca `world.Events.Append(...)` çağırır —
   cascade (WitnessRecorded/GuardResponded, `CascadeSystems.cs:45,162,183`), hasat, ticaret, fiyat,
   need değişimleri vb. Log append-only, insertion-order (`WorldEventLog.cs:49-55`).
2. **Hourly:55 — `living.rumors` (RumorStep, `DefaultTickSystems.cs:283`):**
   `RumorMillSystem.Tick` önce 3 günden eski dedikoduları budar (`RumorMillSystem.cs:25`), sonra
   `world.RumorEventCursor`'dan log sonuna kadar YENİ olayları tarar (ScanCap 256, `:29-31`),
   `Distill` ile satıra çevirir (null = konuşulmaya değmez), `RumorEntry` doğurur, cursor'ı log
   sonuna alır, listeyi 32'ye kırpar (`:40-41`).
3. **Daily:28 — `world.runtime_history` (RuntimeHistoryStep, `DefaultTickSystems.cs:514`):**
   a. `DriftFromYesterday` (`RuntimeHistorySystem.cs:51-75`): son 8192 olayı tersten tarar, dünün
      penceresindeki (`dayStart < Tick <= stamp`) GuardResponded/ShortageDetected sayar; bekçi cevap
      verdiyse law→craft ve law→trade +1 ("watch_renown"), kıtlık varsa craft→trade −1
      ("grain_tension").
   b. `MonthlyChronicle` (`:77-120`): yalnız ay SONUNDA (dayIndex % 30 == 0) çalışır. Rng tohumu
      `((RoomSeed*2654435761) ^ (dayIndex*40503)) | 1` (`:87`). Üç şubeden biri: festival (+4/+4
      ilişki), caravan_surge (Stockpiles[0]'a 25+intensity buğday, `:99-100`), border_dispute
      (law↔trade −6). Her ay ayrıca −4..+4 diplomatik "chronicle_ripple" (`:110-114`). Sonra
      `ChronicleEvent` append edilir (`:116-117`) ve `ArriveMigrants` koşar.
   c. `ArriveMigrants` (`:132-167`): nüfusu MigrantFloor(4) altına düşen her Settlement'a ayda en
      çok 2 göçmen; deterministik id şeması `400M + site*4096 + (dayIndex%512)*8 + k` (`:151-152`);
      `ActorSpawned` "migrant_arrived" olayı (`:162-164`).
4. **Render pompası (her presentation tick):** `DomainSimulationAdapter.PublishEventEchoes`
   (`DomainSimulationAdapter.Clock.cs:11,41-80`) kendi cursor'ıyla (yine 256 cap) yeni olayları
   tarar; WitnessRecorded→eye/alert, GuardResponded→sword, PlantHarvested→sheaf, ActorTalked→chat
   olarak `NpcEventEchoFeed.Raise` çağırır. Cursor load'da log sonuna atlar — 10k olaylık save
   hiçbir şeyi replay etmez (`Clock.cs:39-40,45`).
5. **Görsel tüketim:** `NpcEventEchoView` her 0.4 sn'de `LatestKindFor(actorId, sinceStamp)` sorar,
   piktogramı 3.5 sn gösterir (`NpcEventEchoView.cs:34-47`). Halka tamponu stamp-not-consume:
   tek cascade tick'i ÇOK olay patlatabilir, tek slot düşürürdü (`NpcEventEchoFeed.cs:6-7`).
6. **Diyalog tüketimi:** "Any news?" konusu `RumorMillSystem.PickFor` ile cevaplanır — anlık,
   deterministik, site-local öncelikli, asla tüketilmez (`Dialog.Source.cs:294-303`). Selamlaşmada
   %35 ihtimalle `ComposeRumor` son 32 olayı hikaye cümlesine çevirir + zindan yol tarifi ekler
   (`Dialog.Text.cs:127-158`, `NarrateEvent :160-195`).
7. **Save/Load:** olay logu TAMAMEN serialize edilir (`WorldSaveMapper.cs:82,188`;
   `WorldSaveMapper.Narrative.cs:19-54`); dedikodular paralel dizilerle + cursor
   (`WorldSaveMapper.cs:104-107`, restore `:223-232`). İki mill cursor'ı da load'da clamp'lenir.

## LLD - Veri Modeli

| Tip | Alanlar | Kaynak |
|---|---|---|
| `WorldEvent` | `Tick:GameTime`, `Kind:WorldEventKind`, `ActorId`, `SiteId`, `Reason:string`, `ReasonTrace` — ctor `None` kind'ı, boş actor+site ikilisini ve boş reason'ı reddeder | `WorldEvent.cs:89-111` |
| `WorldEventLog` | `_events:List<WorldEvent>` + `ReadOnlyCollection` sarmalı; `Count`, `IsEmpty`, `Events` (CANLI view — snapshot DEĞİL, sonraki Append'ler görünür) | `WorldEventLog.cs:25-67` |
| `WorldEventKind` | 32 üye; `None=0` sentinel; `WitnessRecorded=30`, `GuardResponded=31` (H3 cascade), `ChronicleEvent=32` (H4) | `WorldEventKind.cs:135-173` |
| `RumorEntry` | `BornMinutes:long`, `SiteId`, `Text:string` — mutable POCO | `RumorEntry.cs:117-122` |
| `WorldState` alanları | `Events:WorldEventLog` (`:40`), `Rumors:List<RumorEntry>` (`:177`), `RumorEventCursor:int` (`:179`); `CopyFrom` üçünü de aynalar (`:221 civari Events`, `:254-255` Rumors/cursor) | `WorldState.cs` |
| `NpcEventEchoFeed.Echo` | `ActorId:ulong`, `Kind:int` (0=witness..4=talk), `StampAt:int`; `Ring[128]` statik halka + monoton `Stamp` | `NpcEventEchoFeed.cs:11-27` |
| `WorldEventSaveData` | `tickMinutes, kind:int, actorId:long, siteId:long, reason, reasonTrace:string[]` | `WorldSaveMapper.Narrative.cs:24-35` |
| Rumor save şekli | `rumorBornMinutes[]`, `rumorSiteIds[]`, `rumorTexts[]`, `rumorEventCursor` — paralel diziler, restore'da uzunluk savunması | `WorldSaveMapper.cs:104-107, 223-232` |
| `WorldEventRow` (HUD) | log kuyruğunun salt-okunur projeksiyonu | `WorldEventTailSnapshot.cs:14-36` |

Sabitler: `MaxRumors=32`, `LifeMinutes=4320`, `ScanCap=256` (`RumorMillSystem.cs:14-16`);
`GuardRenownDelta=1`, `ShortageTensionDelta=-1`, `FestivalBondDelta=4`, `DisputeDelta=-6`,
`CaravanWheat=25`, `MigrantFloor=4`, `MigrantsPerMonth=2` (`RuntimeHistorySystem.cs:25-29,126-127`);
`LawTag/CraftTag/TradeTag` faction etiket sabitleri (`:21-23`) — presentation hydration bu tag'leri
garanti eder (`DomainSimulationAdapter.Worldgen.Hydration.cs:89-96`).

## LLD - Fonksiyon Haritasi

| İmza | Konum | Ne yapar |
|---|---|---|
| `RuntimeHistorySystem.Tick(WorldState, GameTime)` | `RuntimeHistorySystem.cs:37` | Guard'lar (Factions/Events null, stamp<=0, üç tag'den biri eksik → sessiz çık), sonra drift + kronik. |
| `DriftFromYesterday(world, stamp, law, craft, trade)` | `:51` | 8192-derinlik-kapaklı ters tarama; GuardResponded/ShortageDetected sayımına göre itibar delta'ları. |
| `MonthlyChronicle(...)` | `:77` | `dayIndex % DaysPerMonth == 0` filtresi; seeded rng ile 3 şubeli olay + ripple + `ChronicleEvent` append. |
| `ArriveMigrants(world, stamp, dayIndex, rng)` | `:132` | Yerleşim nüfus sayımı → deterministik id'li göçmen aktörler + `ActorSpawned` olayı. |
| `FindByTag(world, tag)` / `FirstSite(world)` | `:169 / :176` | Tag'li ilk faction / ilk site; site yoksa `SiteId(1)` fallback. |
| `RumorMillSystem.Tick(WorldState, GameTime) : int` | `RumorMillSystem.cs:18` | Budama + cursor taraması + damıtma; doğan dedikodu sayısını döner. |
| `RumorMillSystem.Distill(WorldEvent) : string` | `:46` | Kind→cümle eşlemesi; `NeedChanged` reason önekine bakar (vermin_theft/cat_catch/mauled_survives); null = sessiz. |
| `RumorMillSystem.PickFor(world, askerId, siteId, now) : string` | `:77` | `(asker*2654435761) ^ (gun*40503)` hash'i ile havuzdan seçim; site-local havuz doluysa önce o. |
| `FactionReputationSystem.ApplyDelta(factions, a, b, delta, reasonCode, now, events)` | `FactionReputationSystem.cs:15-22` | İtibarı persist eder + `FactionReputationChanged` olayı yazar; FactionId-A, SiteId sentinel'i olarak kodlanır (`:38-47`). |
| `WorldEventLog.Append(WorldEvent)` | `WorldEventLog.cs:49` | Null'da throw; sessiz boşluk yok. |
| `NpcEventEchoFeed.Raise(ulong actorId, int kind)` | `NpcEventEchoFeed.cs:29` | Halkaya yaz, `Stamp`'i artır. |
| `NpcEventEchoFeed.LatestKindFor(ulong actorId, int sinceStamp) : int` | `:36` | Aktörün sinceStamp'ten yeni en taze echo'su; -1 = yok. |
| `DomainSimulationAdapter.PublishEventEchoes()` | `DomainSimulationAdapter.Clock.cs:41` | Presentation cursor'ı ile 4 kind'ı echo'ya çevirir; `WitnessRecorded` reason'ı "reported" ile başlıyorsa alert, değilse eye (`:53-58`). |
| `NpcEventEchoView.Bind(ulong)` / `Update()` | `NpcEventEchoView.cs:21 / :34` | Spawner bağlar (`EmberGeneratedActorSpawner.cs:195`); 0.4 sn poll, 3.5 sn gösterim. |
| `WorldEventTailSnapshot.FromLog(log, maxRows[, predicate])` | `WorldEventTailSnapshot.cs:25, :39` | HUD için kuyruk projeksiyonu, mutasyonsuz. |
| `WorldSaveMapper.ToWorldEventLogData / ToWorldEventLog` | `WorldSaveMapper.Narrative.cs:19 / :37` | Logun tam gidiş-dönüşü; ReasonTrace dahil. |
| `DomainSimulationAdapter.ComposeRumor(uint h)` / `NarrateEvent(WorldEvent)` | `Dialog.Text.cs:127 / :160` | Selamlaşma dedikodusu: son 32 olaydan hikaye cümlesi + zindan reveal. |

Kayıt noktaları: `RumorStep` → `living.rumors@Hourly:55` (`DefaultTickSystems.cs:283`, listede `:53`);
`RuntimeHistoryStep` → `world.runtime_history@Daily:28` (`:514`, listede `:61`).

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry diliyle (`FieldOwnershipRegistry.cs`):

**Yazdıkları:**
- `World.Rumors` ← `living.rumors@Hourly:55` — ledger'da DEKLARE (`FieldOwnershipRegistry.cs:51`).
  `World.RumorEventCursor` aynı yazıcıya ait ama ayrı ledger satırı yok.
- `World.Events` (append) ← her iki sistem + tüm üreticiler. Ledger'da satırı YOK — append-only
  olduğu için çok-yazarlı çatışma sınıfı dışında sayılmış görünüyor (gerekçe dokümante değil,
  dogrulanmadi).
- Faction itibar matrisi (`FactionStore.WithReputation`) ← `world.runtime_history@Daily:28`
  (`RuntimeHistorySystem.cs:70-74,94-114` üzerinden `FactionReputationSystem.cs:36`) — ledger'da
  DEKLARE DEĞİL. `politics.faction_decay@Daily:40` da aynı matrise yazar; ikisi de sicilsiz.
- `World.Stockpiles[0]` ("wheat", caravan_surge) ← `world.runtime_history@Daily:28`
  (`RuntimeHistorySystem.cs:99-100`) — ledger'ın `World.Stockpiles` satırında YOK; satırda
  var olmayan bir `econ.trade@Daily:28` id'si duruyor (aşağıda borçlar).
- `World.Actors` (göçmen `Add`, `RuntimeHistorySystem.cs:159-161`) — Actors üyeliği için ledger
  satırı hiç yok.
- `NpcEventEchoFeed.Ring/Stamp` (statik) ← yalnız presentation (`Clock.cs`); sim alanı değil,
  ledger kapsamı dışı.

**Okudukları:** `World.Events.Events` (iki sistem de derinlik-kapaklı: 8192 tarih / 256 mill+echo),
`World.Factions` (tag araması + `GetReputation`), `World.Sites.Records` (yerleşim sayımı, FirstSite),
`World.Actors.Records` (nüfus sayımı), `World.Stockpiles`, `World.Time`, `World.RoomSeed` (rng
tohumu), `World.Rumors` (PickFor).

## LLD - Urettigi/Tukettigi Olaylar

**Üretilen (WorldEventKind + reason etiketi):**
- `ChronicleEvent` — `"chronicle:{festival|caravan_surge|border_dispute} intensity:N day:D"`
  (`RuntimeHistorySystem.cs:116-117`)
- `ActorSpawned` — `"migrant_arrived name:... site:..."` (`:162-164`) — ActorSpawned'ın ilk runtime
  emitter'ı (`:125`)
- `FactionReputationChanged` — `"faction_reputation a:.. b:.. from:.. to:.. reason:{watch_renown|grain_tension|festival|border_dispute|chronicle_ripple}"`
  (`FactionReputationSystem.cs:42-47,50-54`)

**Tüketilen:**
- RuntimeHistory drift: `GuardResponded`, `ShortageDetected` (`RuntimeHistorySystem.cs:63-64`)
- RumorMill.Distill: `GuardResponded`, `WitnessRecorded` (reason "reported" ayrımı),
  `PlantHarvested`, `TradeCompleted`, `ChronicleEvent`, `NeedChanged`
  (vermin_theft/cat_catch/mauled_survives önekleri) (`RumorMillSystem.cs:49-73`)
- NpcEventEchoFeed (Clock adapter): `WitnessRecorded`, `GuardResponded`, `PlantHarvested`,
  `ActorTalked` (`Clock.cs:51-76`)
- Dialog.NarrateEvent: `NeedChanged(meal_eaten)`, `WitnessRecorded`, `GuardResponded`,
  `ShortageDetected`, `CaravanArrived`, `PriceChanged`, `FactionReputationChanged`,
  `CombatResolved`, `PlantHarvested`, `ChronicleEvent` (`Dialog.Text.cs:160-193`)

## Testler

- `Assets/Tests/EditMode/World/RuntimeHistorySystemTests.cs` — aynı seed aynı kronik (`:33-46`),
  ay dışı gün kronik yazmaz (`:49-55`), ay sonu göçmen (`:58-75`), GuardResponded→renown (`:78-91`).
- `Assets/Tests/EditMode/Living/RumorMillSystemTests.cs` — bir olay bir dedikodu + cursor asla
  yeniden öğütmez (`:22-32`), 3 günlük budama (`:35-41`), PickFor determinizm + site-local
  (`:44-54`).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — Gate6: 31 günde en az bir kronik, en az
  bir ilişki değişimi, iki seed iki farklı tarih (`:168-198`); Gate7 census'ünde chronicle vektörü
  (`:200-227`); "town hums" kapısında ChronicleEvent sayımı (`:300-334`).
- `Assets/Tests/EditMode/World/WorldEventTests.cs` — WorldEvent ctor sözleşmesinin tamamı
  (`:31-146`).
- `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs` — WorldEventLog save round-trip (`:34-70`).
- `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` — Rumors + RumorEventCursor
  round-trip (`:31-33`).
- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — ledger lint (borçlara bak).
- `Assets/Tests/EditMode/Visual/WorldEventInterestTests.cs` — log kuyruğunun görsel projeksiyonu
  (kapsam detayı dogrulanmadi; dosya bu sistemin tüketicisini pinliyor).
- NpcEventEchoFeed/View için TEST YOK (Assets/Tests grep'inde `NpcEventEcho` sıfır eşleşme) —
  presentation halka tamponu tamamen pinsiz.

## Bilinen Borclar + Kacak Kapilari

1. **Ledger yalanı — `econ.trade@Daily:28` diye bir sistem yok.** `FieldOwnershipRegistry.cs:49`
   `World.Stockpiles` yazarı olarak `econ.trade@Daily:28` deklare ediyor; `DefaultTickSystems.cs`
   içindeki gerçek step id'leri arasında `econ.trade` YOK (tam liste: `core.time, core.magic,
   econ.jobs, living.schedule, quest.tick, living.eatOnArrival, living.ambient, living.rumors,
   living.consumption, living.companion_follow, living.companion_guard, living.predation,
   living.witness, econ.shortage_response, living.needs, world.caravans, world.harvest,
   econ.plantgrowth, world.runtime_history, econ.prices, politics.faction_decay`). Gerçek Daily:28
   Stockpiles yazarı `world.runtime_history`'nin caravan_surge şubesi
   (`RuntimeHistorySystem.cs:99-100`) — yani ledger'daki satır hayalet, gerçek yazar sicilsiz.
   Bu tam olarak ledger'ın yakalamak için var olduğu "deklare edilmemiş ikinci yazar" ailesi
   (`FieldOwnershipRegistry.cs:5-11` kendi ifadesi). ((a)-(g) aile harfi eşlemesi repo içinde
   bulunamadı — dogrulanmadi.)
2. **Lint testinin knownIds listesi bayat.** `FieldOwnershipRegistryTests.cs:14-22` şu id'leri
   "gerçek kayıtlı sistem" sayıyor: `world.growth`, `econ.trade`, `world.shortage`,
   `world.history`, `econ.caravan`, `faction.decay` — HİÇBİRİ tick registry'de yok (gerçekleri:
   `econ.plantgrowth`, `econ.shortage_response`, `world.runtime_history`, `world.caravans`,
   `politics.faction_decay`). Test registry'den okumak yerine elle kopyalanmış liste kullandığı
   için "declared writer gerçek sistem mi" iddiası boş güvence: hayalet `econ.trade` linti geçiyor.
3. **RuntimeHistory'nin Faction matrisi ve Actors yazıları sicilsiz.** İtibar matrisine iki Daily
   yazar var (`world.runtime_history@Daily:28`, `politics.faction_decay@Daily:40`) ama ledger'da
   `World.Factions` satırı hiç yok; göçmen `Actors.Add` için de yok. Çatışma bugün yok (sıra
   28<40 deterministik) ama sınıf, guard-pursuit vakasıyla aynı.
4. **WorldEventLog sınırsız büyür ve save'e TAMAMEN yazılır.** Budama/rotasyon yok
   (`WorldEventLog.cs:25`, `WorldSaveMapper.Narrative.cs:19-22`); `PathfindingSystem.cs:57` her
   adım için `ActorStepped` bile basıyor. Okuyucular kendilerini kapaklıyor (8192/256/32) ama save
   boyutu ve `ToWorldEventLogData`'nın O(n) LINQ kopyası uzun oturumda büyümeye devam eder. Uzun
   marathon save'lerinde ölçülmüş üst sınır: dogrulanmadi.
5. **ScanCap kaçak kapısı — sessiz haber kaybı.** Hem mill (`RumorMillSystem.cs:29-31`) hem echo
   (`Clock.cs:45`) cursor gerisinde 256'dan fazla olay birikirse arayı ATLAR (cursor yine sona
   çekilir). Catchup patlamalarında dedikodu/ikon üretilmeden kaybolur — kasıtlı (O(1) tick) ama
   log'a düşmeyen bir veri kaybı.
6. **`PickFor` doc-drift.** Yorum "newest bucket" vaat ediyor (`RumorMillSystem.cs:76`) ama
   implementasyon tazelik ağırlığı uygulamıyor — havuzun tamamından hash'le seçiyor (`:81-84`).
   Davranış deterministik ve testli, sadece yorum yanlış.
7. **Rumor doğum damgası olay zamanı değil, mill zamanı.** `BornMinutes = stamp.TotalMinutes`
   (`RumorMillSystem.cs:37`) — 3 gün önce olmuş bir olay catchup'ta bugün öğütülürse "taze" doğar
   ve 3 gün daha yaşar. Küçük ama gerçek bir zaman kayması.
8. **`NpcEventEchoFeed` statik ve asla sıfırlanmaz.** `Ring`/`Stamp`/`_writeIndex` statik
   (`NpcEventEchoFeed.cs:24-27`), Reset API'si yok: aynı editor/oyun oturumunda dünya değişse de
   eski aktör id'lerinin echo'ları halkada kalır. `Bind` anındaki `Stamp` snapshot'ı
   (`NpcEventEchoView.cs:31`) pratikte replay'i engeller; id çakışması teorik risk. Domain-reload
   kapalı editor ayarında davranış: dogrulanmadi.
9. **Kronik dedikodusu jenerik.** `Distill`'in `ChronicleEvent` satırı hangi olayın yazıldığını
   söylemez ("The chroniclers wrote a new page...", `RumorMillSystem.cs:61-62`) — reason'daki
   festival/caravan_surge/border_dispute bilgisi kullanılmıyor; ayrıntılı anlatım yalnız
   selamlaşma yolundaki `NarrateEvent`'te var (`Dialog.Text.cs:182-190`). İki damıtıcı (Distill +
   NarrateEvent) aynı işi iki ayrı kopyada yapıyor — birleşik tek formatter borcu.
10. **Migrant id şeması 512 ay sonra sarar.** `(dayIndex % 512)` (`RuntimeHistorySystem.cs:152`)
    ~42 oyun yılı sonrası id çakışmasında `Contains` kontrolü göçmeni sessizce atlar (`:153`) —
    kasıtlı kaçak kapısı, kayıp loglanmaz.
11. **Drift eşik-temelli, hacim-körü.** `DriftFromYesterday` gün içinde 1 de olsa 50 de olsa
    GuardResponded için aynı +1'i uygular (`RuntimeHistorySystem.cs:68-74`) — tasarım mı borç mu
    dokümante değil (dogrulanmadi).

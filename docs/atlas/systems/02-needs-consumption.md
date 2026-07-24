# 02-needs-consumption

> Kapsam: ihtiyac basinclari (ActorNeeds), saatlik metabolizma (NeedsSystem), tuketim yarisi
> (NeedConsumptionSystem: hourly yemek/uyku + per-tick varis yemekleri + pile cache) ve mood
> turetimi (NeedMoodEvaluator). Tum satir numaralari 2026-07-24 calisma kopyasindan dogrulandi.

## HLD - Ne ve Neden

Bu sistem, yasayan-dunya dongusunun "kapali devre" yarisidir. `NeedsSystem` yalnizca basinc
YUKSELTIR (aclik/yorgunluk/susuzluk rampalari); `NeedConsumptionSystem` ise CAN SUYU H1 ile
eklenen eksik yariyi kapatir: ac aktorler gercek stockpile'lardan yer, yorgun aktorler gece
uyur (`NeedConsumptionSystem.cs:6-10`). Bunun oyuncuya gorunen etkisi uc katmanlidir:
(1) "yuru-ye-don" ritmi — yemek masada yenir, aktor larder'a kadar yurur
(`NeedConsumptionSystem.cs:17,173-175`); (2) stok gercekten duser, fiyatlar talebi gorur,
kitlik gercek olur (`NeedConsumptionSystem.cs:8-10`); (3) aclik >= 80 veya mood <= 25 olan
aktor is REDDEDER (`JobAssignmentSystem.Tick.cs:358-375`), yani beslenme ekonomiyi tasir.
Felsefe: deterministik, olay-yayan, saf Simulation katmani (Unity tipi yok); olaylar
catch-up sirasinda kadans SINIRINDA damgalanir ki gun-gun ve coklu-gun sicramalari ayni
logu yazsin (`NeedConsumptionSystem.cs:29-31`). Kim neyi yazar sorusu
`FieldOwnershipRegistry` ile bildirilmis ve lint testiyle CI olayina baglanmistir
(`FieldOwnershipRegistry.cs:5-11`).

## HLD - Akis

Kadans/siralar `DefaultTickSystems.Create` kayit listesinden (`DefaultTickSystems.cs:41-64`):

1. **`living.needs` @Hourly:30 (NeedsStep)** — her canli aktor icin `TickNeeds` calisir:
   aclik +8, yorgunluk +6, susuzluk +5 / oyun-saati (`NeedsSystem.cs:21-28`,
   `DefaultTickSystems.cs:356-377`). Rol filtresi YOK: Player, Guard, Enemy dahil herkes
   rampalanir (`DefaultTickSystems.cs:371-377`). Saatte BIR ozet olayi yazilir
   (aktor-basina spam GC krizine yol acmisti — `DefaultTickSystems.cs:380-397`).
2. **`living.schedule` @PerTick:20 (ScheduleStep)** — `NeedConsumptionSystem.FoodSpots` ile
   tum yemekli pile merkezleri toplanir, `ScheduleSystem.Advance`'e beslenir
   (`DefaultTickSystems.cs:224-233`). `ChooseTarget` icinde aclik yalnizca
   `HungerEatThreshold` (55) uzerindeyse yemek teklifi verir; esik alti aclik masaya
   cekMEZ ("herkes town merkezinde" playtest fixi, `ScheduleSystem.cs:129-135`).
3. **`living.eatOnArrival` @PerTick:22 (EatOnArrivalStep)** — `TickArrivals`: larder'a
   VARMIS ac sivil ayni tick yer; saatlik adimi beklerken masada dikilme kalabaligi
   cozulur (`NeedConsumptionSystem.cs:59-84`, `DefaultTickSystems.cs:252-263`). Per-tick
   maliyet, TICKPERF fixiyle pile cache'e indirildi ("EatOnArrival 152s/day and tripling",
   `NeedConsumptionSystem.cs:66-72`).
4. **`living.consumption` @Hourly:35 (ConsumptionStep)** — metabolizma yarisi: ac ve
   erisimdeki aktor yer; 22:00-06:00 arasi yorgunluk saatte 40 duser (uyku)
   (`NeedConsumptionSystem.cs:32-57`, `DefaultTickSystems.cs:289-304`). Sira 35, NeedsStep
   (30) yukselttikten hemen sonra (`DefaultTickSystems.cs:290`).
5. Her ihtiyac yazimindan sonra mood yeniden turetilir: `mood = 50 - (H+F+T)/3`
   (`NeedMoodEvaluator.cs:123-130`). `JobAssignmentSystem.IsRefusing` bunu tuketir
   (aclik >= 80 veya `Mood.IsLow`) (`JobAssignmentSystem.Tick.cs:358-375`).
6. Yenen her ogun `StockpileComponent.Remove` ile stok dusurur
   (`NeedConsumptionSystem.cs:157,177`); fiyat ve kitlik sistemleri (PriceStepSystem
   @30, ShortageResponseStep @Daily:27) bu stogu okur — bu dosyanin kapsami disi
   (`DefaultTickSystems.cs:60-62`).

## LLD - Veri Modeli

| Tip | Alanlar / Sozlesme | Kanit |
|---|---|---|
| `NeedValue` (readonly struct) | 0-100 clamp'li basinc skaleri; `Min=0, Max=100`, `Comfortable=default`, `Critical=100`; `Increase` headroom-clamp'li, `Decrease` negatif-guvenli | `NeedValue.cs:11-53,100-107` |
| `NeedKind` (enum) | `None=0, Hunger=1, Fatigue=2, Thirst=3` | `NeedKind.cs:115-121` |
| `ActorNeeds` (readonly struct) | `Hunger/Fatigue/Thirst : NeedValue`; immutable, `With/WithHunger/WithFatigue/WithThirst` kopya-uretir; `Comfortable=default` | `ActorNeeds.cs:11-72` |
| `ActorMood` (readonly struct) | 0-100, dusuk = isteksiz; `NeutralValue=50`, `LowMoodThreshold=25`, `IsLow => Value <= 25`; ic saklama `+1` kaydirmali ki `default` Neutral cozulsun | `ActorMood.cs:121-133,48`; tasarim notu `ActorMood.cs:112-117` |
| `ActorRecord` (ilgili uyeler) | `Needs { get; private set; }` (83), `Mood { get; private set; }` (84); tek yazim kapilari `ApplyNeeds(167)`, `ApplyMood(172)` | `ActorRecord.cs:83-84,167-175` |
| `StockpileComponent` | SiteId-kapsamli tag→adet sozlugu; `Get` (38, yoksa 0), `Add` (45-58, long'a terfi + Int32 clamp — unchecked tasma fixi), `Remove` (64-75, gercekte silinen adedi doner, asla eksiye dusmez), enumeration kanonik tag sirali (77-85) | `StockpileComponent.cs:12-85` |
| `FoodPileEntry` (private readonly struct) | TickArrivals cache satiri: `Pile, CentreX, CentreY, HasSite` | `NeedConsumptionSystem.cs:86-94` |
| Sabitler (tuketim) | `HungerEatThreshold=55` (H2 utility crossover'la hizali — `WorkScore=55`, `ScheduleSystem.cs:26`), `EatReachCells=2`, `MealHungerFloor=5`, `MealThirstRecovery=40` ("su simulasyonu yok, icecek ogunun icinde"), `NightSleepFatigueRecovery=40`, gece 22-06 | `NeedConsumptionSystem.cs:16-22` |
| Sabitler (rampa) | `HungerIncreasePerTick=8, FatigueIncreasePerTick=6, ThirstIncreasePerTick=5` (24 saatlik yasam olcegi re-balансi) | `NeedsSystem.cs:17-28` |
| Save/load | `hunger/fatigue/thirst/mood` 0-100 int + `hasMood` bayragi; eski save'lerde `mood>0` fallback, degilse Neutral | `ActorSaveMapper.cs:63-70,92-99` |

## LLD - Fonksiyon Haritasi

**NeedsSystem** (`Assets/Scripts/Simulation/Living/NeedsSystem.cs`)
- `ActorNeeds TickNeeds(ActorNeeds needs, int ticks = 1)` — 42-51; uc basinci rampa oraniyla yukseltir, `ScaleRate` long-tasma korumali (102-106).
- `ActorMood RecomputeMood(ActorRecord actor)` — 53-61; evaluator'i cagirir ve `ApplyMood` yazar.
- `bool TickActorNeeds(actor, eventLog, now, ticks)` — 63-100; aktor-basina `NeedChanged` olayi yazan iz surumu. Uretim cagiricisi YOK (yalnizca testler; `DefaultTickSystems.cs:384` yorumu bunu bilerek saklar).

**NeedMoodEvaluator** (`NeedMoodEvaluator.cs`)
- `ActorMood Evaluate(ActorNeeds)` — 123-130; `50 - toplamBasinc/3`. Saf, mutasyonsuz.

**NeedConsumptionSystem** (`NeedConsumptionSystem.cs`)
- `int Tick(WorldState, int hourOfDay, GameTime stamp)` — 32-57; saatlik yemek+uyku; Player/Enemy haric (41); donen deger = yenen ogun sayisi (kanit metrigi).
- `int TickArrivals(WorldState, GameTime stamp)` — 62-84; per-tick varis yemekleri; species listesi + pile cache tick basina BIR kez kurulur, secim matematigi degismedi (66-75).
- `static List<FoodPileEntry> BuildFoodPileCache(world, species)` — 96-121; yemekli pile'larin site merkezlerini stockpile sirasiyla cache'ler.
- `bool TryEatCached(world, actor, stamp, species, cache)` — 123-167; Chebyshev en-yakin secim, sitesiz pile dist=0 ile ONE sortlanir, strict `<` first-wins tie-break; ayni tick erken boşalan pile `Get` ile yeniden dogrulanir (126-140).
- `bool TryEat(world, actor, stamp)` — 169-187; saatlik yolun cache'siz esdegeri.
- `static bool TryGetSiteCentre(world, siteId, out centre)` — 190-203; "tek merkez-otoritesi" iddiali yardimci — AMA cagiricisi yok (asagida borc #1).
- `static bool WithinReach(world, actor, pile)` — 205-218; site merkezine Chebyshev <= 2; sitesiz dunya/pile PERMISSIVE (true) doner.
- `static List<GridPosition> FoodSpots(world)` — 223-245; tum yemekli pile merkezleri; ScheduleStep'in beslemesi.
- `static GridPosition? FoodSpot(world)` — 247-257; TEKIL eski API, uretim cagiricisi yok (yalnizca `LivingWorldGateTests.cs:106`).
- `static StockpileComponent FindNearestFoodPile(world, from, out foodTag)` — 261-291; saatlik yolun en-yakin secicisi (cache'siz, pile x site tarama).
- `static StockpileComponent FindFoodPile(world, out foodTag)` — 293-309; ilk-yemekli-pile (FoodSpot icin).
- `static List<string> FoodTags(world)` — 311-320; "wheat" sabit cekirdek + `world.Plants.Rows` icindeki HER `SpeciesId`.

**Tuketiciler / kompozisyon**
- `ScheduleSystem.ChooseTarget` yemek teklifi — `ScheduleSystem.cs:120-148` (esik kapisi 133-135; tie sirasi eat > rest > work > idle, 140-147).
- `JobAssignmentSystem.IsRefusing` — `JobAssignmentSystem.Tick.cs:358-375` (aclik >= 80 sabiti; `Mood.IsLow`); `CanActorWorkJob` bunu zincire ekler (`JobAssignmentSystem.cs:160-165`).
- Step adaptörleri: `NeedsStep` (`DefaultTickSystems.cs:356-398`), `EatOnArrivalStep` (254-263), `ConsumptionStep` (289-304; saat `(TotalMinutes/60)%24` ile hesaplanir, 301), `ScheduleStep` (211-234).

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` diliyle (`FieldOwnershipRegistry.cs:26-31,43-50`):

**Yazar oldugu bildirilen alanlar**
- `Actor.Needs` ← `living.needs@Hourly:30` (rampalar), `living.eatOnArrival@PerTick:22`
  (varis yemekleri), `living.consumption@Hourly:35` (metabolizma yarisi).
- `World.Stockpiles` ← `living.eatOnArrival@PerTick:22`, `living.consumption@Hourly:35`
  (digger yazarlar: harvest, ambient vermin, trade).

**Fiilen yazilan ama defterde OLMAYAN alan**
- `Actor.Mood` — uc adim da `ApplyMood` cagirir (`NeedConsumptionSystem.cs:53,162,182`;
  `NeedsSystem.cs:59` uzerinden NeedsStep) fakat registry'de `Actor.Mood` anahtari yok
  (`FieldOwnershipRegistry.cs:15-53` tam liste). Bkz. borc #5.

**Okudugu alanlar** (registry disi, koddan): `Actor.Position` (mesafe, `NeedConsumptionSystem.cs:142-143,152-153`),
`Actor.Role`/`IsAlive` (40-41,78-79), `World.Sites.Records` sinir kutulari (109-117,208-216),
`World.Plants.Rows[].SpeciesId` (316-318), `World.Time` (27), `World.Events` (163,183).

## LLD - Urettigi/Tukettigi Olaylar

**Urettigi** — hepsi `WorldEventKind.NeedChanged = 7` (`WorldEventKind.cs:21`):
- `"meal_eaten item:{tag} hunger:{n}"` — aktor + pile.SiteId ile; hem saatlik yol
  (`NeedConsumptionSystem.cs:183-185`) hem varis yolu (163-165).
- `"needs_tick_summary"` — saatte BIR, ReasonTrace: `needs_tick / actors:N / time:T`;
  temsilci aktor = ilk canli aktor (deterministik anchor)
  (`DefaultTickSystems.cs:369-397`).
- `"need_changed:{actorId}"` + tam oncesi→sonrasi ReasonTrace — yalnizca test-yolu
  `TickActorNeeds` (`NeedsSystem.cs:81-97`).
- **Uyku bilerek LOGSUZ** — gece saati basina aktor-basina olay logu sisirirdi; etkiyi
  Gate1 pinler (`NeedConsumptionSystem.cs:46-47`).

**Dolayli tukettigi/tetikledigi**: `JobRefused` olaylari bu sistemin yazdigi Needs/Mood
uzerinden JobAssignment'ta dogar (`JobAssignmentSystem.cs:79-80`); stok dususu fiyat ve
kitlik adimlarina girdi olur (kapsam disi).

## Testler

| Test dosyasi | Pinledigi sey |
|---|---|
| `Assets/Tests/EditMode/Living/NeedConsumptionSystemTests.cs` | Saatlik yemek: doygunluga kadar ye + stok dus (44), erisim disi yiyemez (58), iki larder'dan yakini secilir (68), `FoodSpots` pile-basina bir merkez (79) |
| `Assets/Tests/EditMode/Living/EatOnArrivalTests.cs` | Varan ac sivil AYNI tick yer (15); ac degil / orada degil → ogun yok (41) |
| `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` | Gate1: 5 gunde ihtiyaclar GERI DUSER + stok cift yonlu akar (39-51); Gate4: oglen kalabaligi pencere-kodsuz olusur, `FoodSpot` ile (99,106) |
| `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` | Aclik esigi karar tablosu: esik ustu food-spot'a gider (109), esik alti ASLA gitmez (197) |
| `Assets/Tests/EditMode/Composition/LiveScaleCatchupPerfPinTests.cs` | Canli olcekte bir replay gunu < 3 sn (19) — TICKPERF/EatOnArrival regresyon pini |
| `Assets/Tests/EditMode/Composition/CadenceChunkingInvarianceTests.cs`, `WorldTickDigestGoldenTests.cs` | TICKPERF yorumunun atifta bulundugu bit-ozdes tarih goldenlari (`NeedConsumptionSystem.cs:71-72`) |
| `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` | Sahiplik lint'i: bildirilmemis yazar CI'da patlar |
| `Assets/Tests/EditMode/Save/ActorNeedsRoundTripTests.cs` | Needs+Mood save round-trip (14) |
| `Assets/Tests/EditMode/Living/NeedsEventLogTests.cs`, `NeedsSystemMoodTests.cs`, `ColonyNeedsAcceptanceReplayTests.cs` | `TickActorNeeds` olay sozlesmesi, mood turetimi, koloni kabul replay'i |

## Bilinen Borclar + Kacak Kapilari

Hata ailesi kodlari `docs/SYSTEMS_ATLAS.md:52-60`'taki (a)-(g) siniflamasina gore.

1. **`TryGetSiteCentre` olu kod / yarim refactor** — dokstring "tek merkez-otoritesi, dort
   kopya birlestirildi" der (`NeedConsumptionSystem.cs:189-190`) ama repo genelinde SIFIR
   cagirici var; `(Min+Max)/2` merkez aritmetigi hala 5+ yerde inline
   (113-114, 152-153, 211-212, 238-240, 253-255, 282-283). **Aile (b)/(d)**: koordinat
   otoritesi coğaltilmis, review fixi tamamlanmamis.
2. **Susuzlugun bagimsiz kaynagi yok** — tek geri kazanim ogundeki icecek
   (`MealThirstRecovery=40`, "no water sim yet", `NeedConsumptionSystem.cs:19`). Yemek
   biterse aclik VE susuzluk birlikte 100'e kilitlenir (bagli cokme). Ayrica
   `NeedsSystem.cs:26-27` yorumu "hunger (20) / fatigue (15)" der ama sabitler 8/6 —
   H2 re-balансindan kalma BAYAT yorum (**aile (f)** benzeri: guncellenmeyen defter).
3. **Player ve Enemy rampalanir ama beslenmez** — NeedsStep rol filtresiz herkesi yukseltir
   (`DefaultTickSystems.cs:371-377`); tuketim iki rolu de atlar
   (`NeedConsumptionSystem.cs:41,79`). Player/Enemy acligi 100'e, mood'u tabana oturur.
   Player icin ayri bir yeme yolu bu taramada BULUNAMADI — *dogrulanmadi* (baska sistemde
   olabilir); Enemy icin bilerek olabilir ama hicbir yerde belgelenmemis.
4. **Muhafizlar teoride yer, pratikte masaya gidemez** — tuketim Guard'i DAHIL eder (filtre
   yalnizca Player/Enemy, 41) ama `ScheduleSystem.ChooseTarget` Guard/Enemy'yi
   `ClassicTarget`'a yollar, food-spot'a asla rotalamaz (`ScheduleSystem.cs:124-127`;
   "guards eat off-shift — honest simplification, logged in ROADMAP_V2"). Cikarim: posta/ev
   merkezi larder'a Chebyshev <= 2 degilse muhafiz hic yiyemez → aclik 100, mood dusuk.
   Etki zinciri kod-kanitli, oyun-ici sonucu *dogrulanmadi* (mood muhafiz davranisini su an
   ne kadar etkiliyor, ayri inceleme ister).
5. **`Actor.Mood` sahiplik defterinde yok** — uc adim `ApplyMood` yazar ama
   `FieldOwnershipRegistry`'de `Actor.Mood` anahtari yok (15-53). Lint bu alani koruMUYOR;
   ileride dorduncu bir mood yazari sessizce girebilir. **Aile (c)** on-kosulu.
6. **Her bitki turu yemektir** — `FoodTags` "wheat"i sabit koyar ve `Plants.Rows`'taki HER
   `SpeciesId`'yi menuye ekler (`NeedConsumptionSystem.cs:311-320`). Yenmez tur (lif, sus)
   eklendigi gun aktorler onu da yer. Su an yenmez tur var mi — *dogrulanmadi*.
7. **Ikiz secim mantigi elle senkron** — `TryEatCached` ile `FindNearestFoodPile` ayni
   secim kuralinin iki kopyasi; yorum bunu acikca kabul eder (126-128). Sitesiz-pile
   dist=0 + strict `<` tie-break sozlesmesi ince ve yalnizca goldenlarla pinli. Kurallardan
   biri degisirse per-tick ve hourly yollar sessizce ayrisir. **Aile (b)/(f)**.
8. **`FoodSpot` (tekil) eski API duruyor** — cok-yerlesimli dunyada herkesi ilk pile'a
   yuruten tek-nokta tasarimin kalintisi (**aile (a)**, `SYSTEMS_ATLAS.md:52`); uretimde
   cagirici yok, Gate4 testi kullaniyor (`LivingWorldGateTests.cs:106`). Silinemiyor cunku
   test API'si haline gelmis.
9. **Sitesiz pile = evrensel mutfak (kacak kapisi)** — `WithinReach` site kaydi yoksa
   `true` doner (207,217) ve sitesiz pile mesafe 0 sayilip HERKESIN "en yakini" olur
   (277). Ciplak test dunyalari icin bilincli permissiflik; uretimde site'siz bir pile
   olusursa tum harita oradan beslenir. Uretimde boyle pile olusuyor mu — *dogrulanmadi*.
10. **Saatlik yol hala cache'siz** — TICKPERF fixi yalnizca `TickArrivals`'i cache'ledi;
    `Tick` → `TryEat` → `FindNearestFoodPile` her ac aktor icin pile x site taramasini
    saatte bir tekrar eder (169-187, 261-291). Saatlik kadans maliyeti bugun tasiyor;
    aktor/pile sayisi buyudugunde ayni **aile (c)** perf deseninin ikinci gelisi olur.
    Ayrica `TickArrivals` cache'i, ac aktor hic olmasa da yemekli pile varsa her tick
    yeniden kurulur (73-75, ac filtresi 80'de cache'ten SONRA).
11. **Uyku bir durum degil, sayi dususu** — gece penceresinde yorgunluk dusurulur ama
    "uyuyor" durumu yok; aktor yururken de "uyur" (`NeedConsumptionSystem.cs:48-54`).
    Yatak/ev kosulu yok; pencere 22-06 sabit (21-22). Uyku ayrica bilerek logsuz (46-47) —
    denetim izi yalnizca Gate1'in etki pini.
12. **Ogun tip-agnostik anlik doyum** — 1 adet herhangi bir yemek acligi dogrudan 5'e
    ceker (`MealHungerFloor`, 158-160); besin degeri/porsiyon farki yok. `MealThirstRecovery`
    susuzluk 0 iken de uygulanir, `NeedValue` clamp'i sessizce yutar (`NeedValue.cs:100-107`).
13. **Saat hesabi kopyasi** — ConsumptionStep saati `(Stamp.TotalMinutes/60)%24` ile elde
    eder (`DefaultTickSystems.cs:301`); `GameTime.Hour` benzeri tek otorite yerine inline
    aritmetik (**aile (b)** mikro-ornegi; `ScheduleSystem.IsWorkHour` `time.Hour` kullanir,
    `ScheduleSystem.cs:109-112`).

# 02-needs-consumption

> Kapsam: `ActorNeeds` component'i + `NeedsSystem` saatlik ihtiyaç rampaları + `NeedConsumptionSystem`
> (yalnız sabitler + site/food-spot doğrusu — W32'den sonra Tick'i öldü) + `ConsumeFoodAdvancer`
> (W32 EAT dilimi: instant-eat yerine 3-tick meal) + `SleepAdvancer` (W34 SLEEP dilimi:
> pozisyonsuz gece fiyatının yerine yatakta ladder). `NeedRecoverySystem` command/dialog
> köprüsüdür (tick bandında değil), Borçlar bölümüne düşer.
> Kanıt biçimi: `dosya:satır`. Tüm yollar `Assets/` köklüdür.

## HLD - Ne ve Neden (5-10 cümle)

İhtiyaç sistemi Phase 4'ün "deneyimlenen zaman" katmanıdır: aktörün üç bastıran hissi
(hunger/fatigue/thirst) `NeedValue` ile 0-100 aralığında (yüksek = kötü) tutulur ve `NeedsSystem`
her oyun saati sabit oranda tırmandırır (Hourly:30 bandı). 0-100'lük ölçek ve "artış otomatik
clamp'lenir" kuralı `NeedValue.Increase/Decrease` içindedir (`Domain/Actors/NeedValue.cs:35-52`),
oranlar (H=8, F=6, T=5 per oyun-saati; `NeedsSystem.cs:19-29`) CAN SUYU H2'nin 24 saatlik
yaşam kalibrasyonudur — eski +20/+15/+10 ratchet çağı öğle vakti herkesi tüketiyordu. W32 EAT
dilimi bir yıkıcı sadeleşme getirdi: **hunger düşüşü artık bir yerdedir** — `ConsumeFoodAdvancer`
tabakta 3 tick çiğnendikten sonra atomik commit ile hunger'ı `MealHungerFloor=5`'e, thirst'ü
`-MealThirstRecovery=40`'e indirir ve verbatim `meal_eaten` olayını yazar
(`ConsumeFoodAdvancer.cs:44-60`). "İnstant-eat" (varış anında ye) B09 ailesinin kök nedeniydi —
gate-Living'de aktörler "W32 eat slice"inde ölüyordu — o yol öldü, `NeedConsumptionSystem.Tick`
tamamen silindi (kalanı sabitler + food-spot lookup, `NeedConsumptionSystem.cs:14-59`). W34 SLEEP
dilimi ise ikinci yıkıcı sadeleşmedir: pozisyonsuz `NightSleepFatigueRecovery(40)/saat` fiat'ı
öldü, yerine `SleepAdvancer` **yalnızca Running fazında ve Home hücresinde** her 3. tick 2 puan
fatigue düşürür (`SleepAdvancer.cs:19-22, 61-68`) — yürüyen adam uyumaz, uzakta uyumaz.
Böylece "aktörlerde kimlik var, fakat devam eden irade yok" pini kırıldı: bir gün YAŞANIR,
anlatılmaz. `FieldOwnershipRegistry` "Actor.Needs" satırında iki yazar ilan eder:
`living.needs@Hourly:30` (rampalar) ve `living.action_advance@PerTick:22` (ConsumeFood + Sleep
commit'leri) — `living.consumption@Hourly:35` W34'te retire edildi
(`FieldOwnershipRegistry.cs:27-34`; `DefaultTickSystems.cs:63`).

## HLD - Akış (numaralı adımlar)

1. **Hourly rampa (living.needs@30):** `NeedsStep.Run` `world.Actors.Records`'ı gezer, her canlı
   aktör için `actor.ApplyNeeds(_needs.TickNeeds(actor.Needs))` + `RecomputeMood(actor)` çağırır;
   `TickNeeds` üç need'i sırayla `+8/+6/+5 × ticks` `Increase` eder ve clamp 100'de kilitlenir
   (`DefaultTickSystems.cs:414-422`; `NeedsSystem.cs:44-51`).
2. **Saatte TEK özet olay:** `NeedsStep` per-actor `NeedChanged` yerine sabit bir "needs_tick_summary"
   olayı yazar (`actors:N`, `time:TotalMinutes`); per-actor spam'i öldürmenin nedeni ~900
   entry/gün-saati ve gün-90 gen2 GC (`DefaultTickSystems.cs:426-446`). `TickActorNeeds` per-actor
   overload'ı hâlâ vardır (`NeedsSystem.cs:63-97`) ama bant onu çağırmaz — testler + `CascadeSystems`
   gibi çağrı köprüleri kullanır.
3. **Karar (decision katmanı):** `ActionLifecycleSystem.Decide` `PerTick:18` bandında koşar; boş
   aktör için önce `actor.Needs.Hunger >= HungerEatThreshold=55` kapısı, sonra `TryDecideEat`
   (rezervasyon + `MoveToFood → TakeFood → MoveToPile → ConsumeFood` zinciri;
   `ActionLifecycleSystem.cs:77-88`).
4. **Yeme (W32):** `ConsumeFoodAdvancer.Step` her tick önce ReservationLost + WithinEatReach
   probe'ları çalıştırır (uzakta çiğneme yok, T1 witness-nudge sınıfı), 3 tick tamamlandığında
   ATOMİK commit: `WithHunger(new NeedValue(5))` + `WithThirst(mevcut - 40)`, mood re-evaluate,
   `meal_eaten` `NeedChanged` olayı (SiteId dolu — RumorMill/Gate bunu okur), reservation release
   (`ConsumeFoodAdvancer.cs:26-59`).
5. **Karar (uyku):** aynı `Decide` fonksiyonunda kod sırası öncelik sırasıdır: eat kararı üretmedi
   ise `SleepOperations.IsNightHour(stamp.Hour) && Fatigue.Value >= FatigueSleepThreshold=1`
   kapısı `TryDecideRest`'i tetikler → `MoveToBed → Sleep` zinciri
   (`ActionLifecycleSystem.cs:91-97`).
6. **Uyku (W34):** `SleepAdvancer.Step` her tick ÜÇ kapıdan geçer — (a) reservation var + benim +
   BedKey home'umla eşleşiyor mu (`SleepOperations.TryParseBedKey`), (b) Chebyshev(pos, Home) ≤ 1
   (bed reach — 3x3 "yatak odası"), (c) hâlâ `IsNightHour(Stamp.Hour)` mı — dawn'da reservation
   release + Succeeded (`SleepAdvancer.cs:29-56`).
7. **Recovery ladder:** `ProgressTicks++`; `progressed.ProgressTicks % TicksPerRecoveryStep(3) == 0`
   ise `WithFatigue(new NeedValue(mevcut - 2))` + mood re-evaluate (`SleepAdvancer.cs:59-67`).
   Fatigue 0 GECEYİ BİTİRMEZ — aktör dawn'a kadar yatar. `TransitionTo` faz sınırlarını yazar,
   in-phase tick'ler log'suz geçer (B21 grammar).
8. **Terminal:** Sleep TransitionTo(Succeeded) → `ActionLifecycleSystem.NextLink` yeni intent'i
   düşünür; MoveToBed TimedOut olduysa (dawn geldi ama yatağa varılamadı) failure — kimse gece
   ratchet'ini almaz, sürünmüş bir gün mühürlenir.

## LLD - Veri Modeli (file:line)

| Tip | Alanlar / içerik | Kanıt |
|---|---|---|
| `NeedValue` (readonly struct) | tek alan `Value: int`, sabitler `Min=0/Max=100`; ctor Clamp; `Increase(a)` headroom check + Critical shortcut; `Decrease(a)` `Math.Max(0,a)` sonra clamp | `Domain/Actors/NeedValue.cs:12-56` |
| `NeedKind` (enum) | `None=0, Hunger=1, Fatigue=2, Thirst=3` | `Domain/Actors/NeedKind.cs:9-15` |
| `ActorNeeds` (readonly struct) | `Hunger/Fatigue/Thirst: NeedValue`; `Comfortable` = default; `Get/With(NeedKind, NeedValue)` selector + `WithHunger/WithFatigue/WithThirst` overload'ları; `NeedKind.None` fırlatır | `Domain/Actors/ActorNeeds.cs:11-73` |
| `ActorMood` (readonly struct) | `NeutralValue` çapa; NeedMoodEvaluator türetir | `Domain/Actors/ActorMood.cs` (referans: `NeedMoodEvaluator.cs:18`) |
| `ActorRecord` (Needs bölümü) | `Needs: ActorNeeds` (immutable snapshot), `ApplyNeeds(next)` re-atar; `ApplyMood(mood)` mood alanını yazar | `Domain/Actors/ActorRecord*.cs` (kullanım: `NeedsSystem.cs:77`, `ConsumeFoodAdvancer.cs:54-55`, `SleepAdvancer.cs:64-65`) |
| `NeedsSystem` sabitleri | `HungerIncreasePerTick=8`, `FatigueIncreasePerTick=6`, `ThirstIncreasePerTick=5` (per oyun-saati; H2 kalibrasyonu) | `Simulation/Living/NeedsSystem.cs:22-27` |
| `NeedConsumptionSystem` sabitleri | `HungerEatThreshold=55` (H2 utility crossover), `EatReachCells=2` (tabaka kadar yürü), `MealHungerFloor=5` (doyuma kadar ye), `MealThirstRecovery=40` (yemek içeriği içer), `NightStartHour=22`, `NightEndHour=6` (yalnız `SleepOperations.IsNightHour` okur) | `Simulation/Living/NeedConsumptionSystem.cs:15-23` |
| `ConsumeFoodAdvancer.ConsumeDurationTicks` | `= 3` — "yemek 3 tick sürer" sabitinin tek evi | `Simulation/Living/Actions/ConsumeFoodAdvancer.cs:15` |
| `SleepAdvancer.RecoveryPerStep/TicksPerRecoveryStep` | `= 2 / = 3` — retired fiat 40/saat verbatim: 40/60 = 2/3 → her 3. Running tick'te 2 puan; determinizm sadece integer | `Simulation/Living/Actions/SleepAdvancer.cs:18-22` |
| `SleepOperations` sabitleri | `FatigueSleepThreshold=1` (fiat'ın `Fatigue > 0` gate'i verbatim), `BedReachCells=1` (3x3 yatak odası; aile üyeleri aynı Home'u paylaşır), `BedPrefix="bed:"` | `Simulation/Living/Actions/SleepOperations.cs:22-31` |
| `FoodPileCache.Entry` (readonly struct) | `Pile: StockpileComponent`, `CentreX/CentreY: int`, `HasSite: bool`; per-tick snapshot | `Simulation/Living/FoodPileCache.cs:16-24` |

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

**NeedValue / ActorNeeds (Domain, saf):**
- `NeedValue.Increase(int amount): NeedValue` — `Domain/Actors/NeedValue.cs:38` — amount ≤ 0 no-op; headroom taşması Critical'e clamp; determinizm anayasası (float yok).
- `NeedValue.Decrease(int amount): NeedValue` — `Domain/Actors/NeedValue.cs:50` — negatif amount 0'a kırpılır, sonra Clamp.
- `NeedValue.IsAtLeast(NeedValue threshold): bool` — `Domain/Actors/NeedValue.cs:33` — decision katmanının eşik testleri (HungerEatThreshold vs).
- `ActorNeeds.Get(NeedKind): NeedValue` / `With(NeedKind, NeedValue): ActorNeeds` — `Domain/Actors/ActorNeeds.cs:28, 44` — kind switch, `None` fırlatır; three `WithXxx` overload'ı record-copy döner.

**NeedsSystem (Hourly:30):**
- `NeedsSystem.TickNeeds(ActorNeeds needs, int ticks=1): ActorNeeds` — `Simulation/Living/NeedsSystem.cs:44` — üç Increase zinciri (`WithHunger/WithFatigue/WithThirst`); `ScaleRate(rate,ticks)` long taşmayı `int.MaxValue`'ya clamp'ler; ticks ≤ 0 no-op.
- `NeedsSystem.RecomputeMood(ActorRecord): ActorMood` — `Simulation/Living/NeedsSystem.cs:54` — `_moodEvaluator.Evaluate(actor)` + `actor.ApplyMood(mood)`; null aktör fırlatır.
- `NeedsSystem.TickActorNeeds(ActorRecord, WorldEventLog, GameTime, int ticks=1): bool` — `Simulation/Living/NeedsSystem.cs:63` — per-actor overload: previousNeeds → nextNeeds → ApplyNeeds → RecomputeMood + 7-string ReasonTrace `NeedChanged` olayı (`needs_tick, actor:X, ticks:N, time:T, hunger:a->b, fatigue:a->b, thirst:a->b, mood:m`). Bant bunu ÇAĞIRMAZ; testler + CascadeSystems okur.
- `NeedsStep.Run(in TickContext)` — `Simulation/Composition/DefaultTickSystems.cs:414` — canlı aktörler için `ApplyNeeds(TickNeeds(Needs))` + `RecomputeMood`; SADECE bir özet olay yazar (yukarıda 2. adım).
- `NeedMoodEvaluator.Evaluate(ActorNeeds): ActorMood` — `Simulation/Living/NeedMoodEvaluator.cs:15` — `totalPressure / 3` penalty, `NeutralValue - penalty`; mutasyon yok, stateless.

**NeedConsumptionSystem (yalnız sabitler + site truth):**
- `NeedConsumptionSystem.TryGetSiteCentre(WorldState, SiteId, out GridPosition): bool` — `Simulation/Living/NeedConsumptionSystem.cs:27` — 4 kez duplike edilen site-center lookup'ın tek evi (review fix).
- `NeedConsumptionSystem.FoodSpots(WorldState): List<GridPosition>` — `Simulation/Living/NeedConsumptionSystem.cs:44` — `FoodPileCache.FoodTags` → `Build` → HasSite olan her entry için centre; multi-settlement dünyalarda MANY larders (gate wave sampler'ı için).
- `NeedConsumptionSystem.FoodSpot(WorldState): GridPosition?` — `Simulation/Living/NeedConsumptionSystem.cs:56` — ilki (deterministik: stockpile sırası).
- `FoodPileCache.FoodTags(WorldState): List<string>` — `Simulation/Living/FoodPileCache.cs:24` — `"wheat"` staple + live plant species; O(plants).
- `FoodPileCache.Build(WorldState, List<string>): List<Entry>` — `Simulation/Living/FoodPileCache.cs:36` — TICKPERF hoist: "EatOnArrival 152s/day" species×piles×sites re-build'inden çıktı, per-tick tek build.

**ConsumeFoodAdvancer (W32; PerTick:22):**
- `ConsumeFoodAdvancer.Step(WorldState, ActorRecord, GameTime)` — `Simulation/Living/Actions/ConsumeFoodAdvancer.cs:25` — reservation triple (var + benim + ID eşleşir) → `FoodOperations.WithinEatReach` (T1 uzakta çiğneme reddi) → ProgressTicks < 3 ise `ActionLogReason.ProgressTicked`; == 3'te ATOMİK commit: `WithHunger(NeedValue(5))` + `WithThirst(mevcut - 40)`, mood, `meal_eaten` NeedChanged (SiteId dolu), release, `Succeeded` handover. `TryEatCached`'in verbatim math bloğu (retired NeedConsumptionSystem.cs:180-188).

**SleepAdvancer (W34; PerTick:22):**
- `SleepAdvancer.Step(WorldState, ActorRecord, GameTime)` — `Simulation/Living/Actions/SleepAdvancer.cs:30` — reservation quad (var + benim + BedKey parse + Home ile eşleşir) → Chebyshev(pos, Home) > 1 ise Unreachable fail (witness-nudge sınıfı) → `!IsNightHour(Stamp.Hour)` ise release + Succeeded (dawn) → progressed.ProgressTicks % 3 == 0 ise `WithFatigue(mevcut - 2)` + mood → in-phase `TransitionTo(ProgressTicked)`.
- `SleepOperations.BedKey(GridPosition): string` — `Simulation/Living/Actions/SleepOperations.cs:34` — `"bed:X:Y"` codec; SiteId 0UL (bed hiçbir site-scoped süpürmeye katılmaz).
- `SleepOperations.TryParseBedKey(string, out GridPosition): bool` — `Simulation/Living/Actions/SleepOperations.cs:39` — plot-key pattern; başarısızlık = ReservationLost. `AllowLeadingSign` (Home negatif olabilir).
- `SleepOperations.IsNightHour(int hour): bool` — `Simulation/Living/Actions/SleepOperations.cs:60` — `hour >= 22 || hour < 6`; NeedConsumptionSystem sabitlerini okur — TEK predicate (§11 risk 5, MoveToBed TimedOut ile Sleep Succeeded aynı testi kullanmalı yoksa dawn'da off-by-one).
- `SleepOperations.MinutesUntilDawn(GameTime): long` — `Simulation/Living/Actions/SleepOperations.cs:63` — TTL sizing (1 tick = 1 dakika).
- `SleepOperations.ResidentCount(WorldState, GridPosition): int` — `Simulation/Living/Actions/SleepOperations.cs:69` — bed kapasitesi = bu cell'i Home diyen canlı aktörler; worldgen'in ev ataması aile tanımıdır.

**Decision katmanı (PerTick:18, bu sistemin dışsal gate'i):**
- `ActionLifecycleSystem.TryDecideEat(WorldState, ActorRecord, List<string>, List<FoodPileCache.Entry>, GameTime)` — `Simulation/Living/Actions/ActionLifecycleSystem.cs:277` — HungerEatThreshold gate'inden geçen aktör için larder seçimi + rezervasyon zinciri.
- `ActionLifecycleSystem.TryDecideRest(WorldState, ActorRecord, GameTime)` — `Simulation/Living/Actions/ActionLifecycleSystem.cs:391` — TryDecideEat/Plant mould'u; IsNightHour + Fatigue ≥ 1 kapısından geçen aktör için MoveToBed rezervasyonu.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Bu sistemin defterdeki yazar kayıtları (`FieldOwnershipRegistry.cs`):

- **`Actor.Needs`** ← `living.needs@Hourly:30` (rampalar), `living.action_advance@PerTick:22` (W32 ConsumeFood hunger drop; W34 Sleep fatigue drop). W34 öncesi burada `living.consumption@Hourly:35` da vardı — retire edildi çünkü Sleep recovery zaten ConsumeFood'un sahip olduğu advance slot'una taşındı (yorum: `FieldOwnershipRegistry.cs:28-31`).
- **`Actor.Mood`** ← `living.action_advance@PerTick:22` (ConsumeFood/Sleep re-evaluate), `living.needs@Hourly:30` (NeedsStep RecomputeMood) (`FieldOwnershipRegistry.cs:105-109`).
- **`World.Reservations`** ← `living.decision@PerTick:18` (claim + expiry sweep), `living.action_advance@PerTick:22` (consumed/failed release) — bu sistem BedKey/food-key row'larını burada tutar (`FieldOwnershipRegistry.cs:40-44`).
- **`World.Stockpiles`** ← `living.action_advance@PerTick:22` (W32 TakeFood decrement + failure return; sıralı satır `FieldOwnershipRegistry.cs:63`) — yeme geometrisi burayı okur ama bu sistemin öz yazarlığı DEĞİL (TakeFoodAdvancer yazar).

**Okuduğu alanlar:**
- `world.Actors.Records` — `NeedsStep` ve `SleepAdvancer.ResidentCount` gezer.
- `world.Sites.Records` — site centre lookup (`NeedConsumptionSystem.TryGetSiteCentre`).
- `world.Stockpiles` + `world.Plants.Rows` — `FoodPileCache` build inputs (`FoodPileCache.cs:29, 40`).
- `world.Reservations` — hem ConsumeFood hem Sleep advancer per-tick validation triple/quad okur.
- `world.Events` — null ise özet olayı yazılmaz ama bant koşar; SleepAdvancer hiç olay yazmaz.
- `actor.Position`, `actor.Home` — Chebyshev bed reach + WithinEatReach; `actor.Role` (Guard) — decision katmanının pursuit carve-out'u.

## LLD - Ürettiği/Tükettiği Olaylar

**Üretilen:**
- `WorldEventKind.NeedChanged` — **iki farklı yayınlayıcı:**
  - **Hourly özet (bant):** `NeedsStep.Run` `anchor` aktör + reason `"needs_tick_summary"` + ReasonTrace `["needs_tick", "actors:N", "time:T"]` (`DefaultTickSystems.cs:434-445`). Saatte 1, aktör sayısından bağımsız.
  - **Per-actor detay (per-actor overload):** `NeedsSystem.TickActorNeeds` reason `"need_changed:{actorId}"` + 7-string trace (yukarıda). Bant çağırmaz; testler + CascadeSystems + eski demo scriptler kullanır.
- `WorldEventKind.NeedChanged` (meal_eaten satırı) — `ConsumeFoodAdvancer.Step` atomic commit tick'inde: reason `"meal_eaten item:{ItemTag} hunger:{Value}"`, **SiteId dolu** (larder site) (`ConsumeFoodAdvancer.cs:57-58`). RumorMill/Gate meal counter'ı bu prefix'i okur (`DomainSimulationAdapter.WorldEncounter.cs:683`).
- **Sleep hiç WorldEvent yazmaz** — B21 grammar: Started/Completed action log satırları `TransitionTo` üzerinden yayılır (log tag `ProgressTicked/Completed`), ayrı bir WorldEvent yok. Nedeni: "sleep sayacı okuyan yok, en az LOC" (yorum: `SleepAdvancer.cs:70-72`).

**Tüketilen:** bu sistem WorldEvent tüketmez; RumorMill/CascadeSystems/AmbientLife kendi kanallarında `NeedChanged`'i okur.

**Action log satırları:** `ActionLogManager.Started/Completed/ProgressTicked` her ConsumeFood + Sleep faz sınırında (`ActionAdvancer.TransitionTo` üzerinden). B21 grammar: bir night başına tek Started + tek Completed; in-phase tick'ler log'suz.

## Testler (bu sistemi pinleyen test dosyaları — W32-W36 hikâye-testleri dahil)

- `Actors/ActorNeedsTests.cs` — `Comfortable`, ctor, `Get/With(NeedKind)` selector, `WithHunger/Fatigue/Thirst` — üç need bağımsız (thirst'i sessizce silme regresyonuna karşı A-P1 pin).
- `Actors/NeedValueTests.cs` — 0-100 clamp, Increase headroom, Decrease negatif kırpma.
- `Actors/NeedKindTests.cs` — enum ordinal + isim pin.
- `Actors/ActorRecordNeedsTests.cs` — `ApplyNeeds` immutability + copy.
- `Actors/ActorNeedsRoundTripTests.cs` — save/load turu (üç need + mood).
- `Living/NeedsSystemTests.cs` — rate pin (H=8, F=6, T=5); repeated ticks clamp; ticks ≤ 0 no-op.
- `Living/NeedsSystemMoodTests.cs` — rampa sonrası mood re-derive.
- `Living/NeedsEventLogTests.cs` — `TickActorNeeds` 7-string ReasonTrace pin (per-actor overload; hourly özet farklı test).
- `Living/NeedMoodEvaluatorTests.cs` — `totalPressure/3` penalty formülü.
- `Living/NeedConsumptionSystemTests.cs` — W32 sonrası KALAN: yalnız `FoodSpots` geometri (Tick pinleri fiat ile birlikte öldü; comment `NeedConsumptionSystemTests.cs:14-16, 33-35`).
- `Living/NeedRecoverySystemEatTests.cs` + `NeedRecoverySystemSleepTests.cs` — command/dialog köprüsü olan `NeedRecoverySystem` için; tick sistemi değil (Borçlar #2).
- `Living/ColonyNeedsAcceptanceReplayTests.cs` — replay determinism (needs branch).
- `Living/EatActionStoryTests.cs` — W32 tarihinden gelen story pinleri.
- `Actions/EatHungerAtCompletionTests.cs` — W32 EAT dilimi T-serisi: hunger düşüşünün SADECE 3. tick'te olduğunu pinler.
- `Actions/EatInterruptionConservationTests.cs` — W32 T-serisi: yarım-yenmiş meal iptal edilirse rezerve edilen unit ve hunger değişmez (matter conservation).
- `Actions/EatAtDistanceTests.cs` — T1: uzakta çiğneme (witness-nudge diner) reddi.
- `Actions/EatActionContinuityTests.cs` — chunking invariance pini (tick-tek-tek vs. parça).
- `Actions/EatStoryChainTests.cs` — decide → walk → take → eat zinciri.
- `Actions/GuardEatStoryTests.cs` — W33-C: guards-eat pursuit carve-out (chase outranks lunch).
- `Actions/SleepRecoveryAuthorshipTests.cs` — **W34 DOC4 S1** (canonical): fatigue yalnızca Sleep+Running+at-Home iken düşer; MoveToBed sıfır recovery; iki günlük horizon'da gece 22 → dawn tam bracket ve day2 sunrise < day1 bedtime (sustainability pin).
- `Actions/SleepInterruptionTests.cs` — **W34 DOC4 S2**: hunted sleeper WAKES → Failed(Interrupted); reservation release; banked recovery korunur (refund değil).
- `Actions/SleepWorkStoryChainTests.cs` — **W34 DOC4 S5** capstone: work → walk home → sleep → wake → work; 24 saat bodied chain (RUH_TESHIS "kimlik var, süregelen irade yok" kırılması).
- `Actions/Support/EatSliceWorld.cs` + `SleepSliceWorld.cs` — support fixture (Tired/Hungry aktör builder'ları, larder, bed odası).
- `Composition/WorldTickDigestGoldenTests.cs` — SHA-256 golden; needs branch değişince re-baseline geçmişi.
- `Composition/LiveScaleCatchupPerfPinTests.cs` — canlı ölçek pini; EatOnArrival regresyon bekçisi (TICKPERF hoist'un koruyucusu).
- `Composition/WorldTickComposerReplayTests.cs` — save/load replay eşdeğerliği (needs dahil).
- `Save/SaveLoadDigestRoundtripTests.cs` — needs alanlarının kanonikleşmiş digest turu.
- `CanSuyu/LivingWorldGateTests.cs` — H1 gate'i needs saatlik rampasını gerçek composer ile sürer.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W32 EAT slice (DOC3):** `ConsumeFoodAdvancer` doğdu (`ConsumeFoodAdvancer.cs`); `NeedConsumptionSystem.Tick` (retired) — instant-eat/`TryEatCached` bloğu SİLİNDİ, verbatim math ConsumeFoodAdvancer'a taşındı. `Actor.Needs` yazarı olarak `living.action_advance@PerTick:22` defterye eklendi (`FieldOwnershipRegistry.cs:33`). B09 "aktörler W32 eat slice'inda ölüyor" ailesinin kök çözümü: yeme artık 3-tick action, hunger commit atomik, uzakta çiğneme reddedilir. `NeedConsumptionSystemTests` iki eski Tick pini silindi (yorum `NeedConsumptionSystemTests.cs:33-35`).
- **W33-C:** `GuardEatStoryTests` + `HasLivePursuit(Guard)` carve-out — nöbetçiler chase varken yemek kararı vermez (`ActionLifecycleSystem.cs:71-76`); `Actor.Needs` yazarları değişmedi.
- **W34-B SLEEP slice (DOC4):** `SleepAdvancer` + `MoveToBedAdvancer` + `SleepOperations` doğdu; `NightSleepFatigueRecovery(40)/saat` fiat'ı `NeedConsumptionSystem.Tick`'ten SİLİNDİ (kalan sabitler `NightStartHour/EndHour` yalnız `SleepOperations.IsNightHour` üzerinden okunur). `living.consumption@Hourly:35` bant kaydı retire edildi (`DefaultTickSystems.cs:63`; `FieldOwnershipRegistry.cs:28-31`). SleepRecoveryAuthorshipTests + SleepInterruptionTests + SleepWorkStoryChainTests eklendi. Projection'ın 22/6 literal kopyası da öldü (§11 risk 5 karşılığı, tek predicate).
- **W34-C WORK:** ihtiyaç sisteminde doğrudan yazma yok; ama `Actor.Needs` writer listesi commentle güncellendi (`FieldOwnershipRegistry.cs:31`) — decision katmanı priority order artık "eat > sleep > work" (kod sırası = öncelik doktrini, `ActionLifecycleSystem.cs:88-97`).
- **W35 (B04):** `FieldOwnershipRegistry` boot-only writer pratiğine düzenlendi; `Actor.Mood` bu sistemin ikinci yazar seti olarak açıkça eklendi (`FieldOwnershipRegistry.cs:105-109`). NeedsSystem/Mood-derive ikili yazarlığı defter satırıyla ilan edildi.
- **W36:** B17/B18/B06 fix'leri push edildi (f6c9e2d0); needs sistemi bu batch'te doğrudan cerrahiye girmedi ama Actor.Mood ownership row + boot-mutation kuralı ihtiyaç modeline dokunuyor.

## Bilinen Borçlar + Kaçak Kapıları

1. **`NeedRecoverySystem` boot/dialog/command köprüsü, tick bandında değil.** `EatMealAction="eat_meal"`, `SleepAction="sleep"` string action'ları `NeedRecoveryRecipe` üstünden envanterden yer ve bir NeedChanged olayı yazar (`NeedRecoverySystem.cs:19-98`); `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs` içinde HİÇ Step olarak kayıtlı DEĞİL. Yani `NeedRecoverySystem.EatMeal` çağrıldığında `Actor.Needs`'e yazan bir kod var ama defterde `<hangi-cadence>:` yazarı yok — Reform #2'nin CI vaadi bu boot/dialog patikasını KAPSAMIYOR. Testleri (`NeedRecoverySystemEatTests`, `NeedRecoverySystemSleepTests`) geçiyor çünkü direkt fonksiyon çağırıyorlar.
2. **Per-actor NeedChanged olayı iki köprüden geliyor.** Bant sadece özeti yayınlarken (`DefaultTickSystems.cs:434`), `NeedsSystem.TickActorNeeds` overload'ı hâlâ 7-string trace ve reason `need_changed:{id}` yazıyor (`NeedsSystem.cs:82-95`). Aramalar (`Grep TickActorNeeds`) `CascadeSystems`'de + `RumorMillCursorTrimTests`'de kullanım gösteriyor; iki farklı reason prefix (`needs_tick_summary` vs `need_changed:`) tüketicilerin ayrı switch-case'ler yazmasına yol açıyor (`RumorMillSystem.cs:69`, `AmbientLifeSystem.cs:51-70`, `NeedRecoverySystem.cs:98`). RumorMill/Narrator/Interest tekilleştirilmemiş.
3. **Sleep hiç WorldEvent yazmaz.** ConsumeFood `meal_eaten` yazar, Sleep sadece action log ile idare eder (`SleepAdvancer.cs:70-72`). RumorMill'in "geçen gece iyi uyudu" satırı yok; UI'da uyku bir sayaç okuyamıyor. Kasten LOC minimize, ama "sleep_completed" olayı istenirse burası yeniden dönülecek.
4. **`FoodPileCache` per-tick build.** `FoodPileCache.Build` decision katmanının her hungry aktörü için tekrar çağrılabilir; `ActionLifecycleSystem.Decide` cache'i lazy build ediyor (`ActionLifecycleSystem.cs:81-84`) ama bu cache tick-scope'lu — persistent hoist yok, yalnız aynı Decide çağrısı içinde reuse edilir. Canlı ölçek pini bekçi, ama daha büyük dünyada tekrar bakılmalı.
5. **`SleepOperations.ResidentCount` O(N) actor scan.** Her sleep karar denemesinde canlı aktör listesi taranır (`SleepOperations.cs:74-79`); Home indeksi yok. Aile 2-4 kişi için ucuz ama 800 sivilde her IsNightHour+Fatigue≥1 aktör için lineer.
6. **Thirst tüketimi çok yumuşak.** `MealThirstRecovery=40` her meal'de düşerken `ThirstIncreasePerTick=5/saat` yalnız rampa; yemek olmayan bir dünyada thirst kritiğe gider ama ayrı bir "iç" action yok. `WithThirst`'in eklendiği W28-öncesi pin (`NeedsSystemTests` explicitly asserted thirst unchanged; obs 5886) tarihsel dip.
7. **`HungerEatThreshold=55` sihirli sabit.** Comment "H2 utility crossover" der (`NeedConsumptionSystem.cs:15`) ama başka bir sisteme referans linki yok — WorkScore/priority tablosu dokümante değil; değişirse decision katmanı sessiz kaymaya başlar.
8. **`MealHungerFloor=5` her yemekte kesin 5'e döner.** Yarı yiyen bir aktör bile 3-tick chew tamamlanırsa 5'e clamp'lenir — `.WithHunger(new NeedValue(5))` (`ConsumeFoodAdvancer.cs:52`). "Az yeyen az doyar" modeli yok; küçük ısırık ekleyeceksek bu satır dallanır.
9. **`SleepAdvancer.RecoveryPerStep=2` fiat parity yorumu güvenilmez ölçekleme.** 40/60 = 2/3 hesabı gerçek fiat'ı `MinutesPerTick=1` altında verbatim eder. `TickRuntimeOptions.MinutesPerTick` değişirse (7 dakika/tick gibi) `TicksPerRecoveryStep=3` fiat rate'i bozar — `%3` PROGRESS ticks'i sayıyor, tick uzunluğunu değil.
10. **BedKey format brittle.** `"bed:X:Y"` codec sadece Home cell'i kodlar; iki aktör aynı Home'a paylaşımlı yatak vermek istenirse ekstra alan gerek. Şu an `SleepOperations.ResidentCount` bunu "aile" sayıyor ama reservation ledger'ı per-actor row açıyor; kapasite kontrolü yok — 5 kişilik aile aynı BedKey için 5 ayrı row rezerv edebilir.
11. **`ConsumeFoodAdvancer.EatReachCells=2` ile `NeedConsumptionSystem.EatReachCells=2` iki yerde sabit.** İkinci yer NeedConsumption'ın "site truth"unda; FoodOperations.WithinEatReach reach'i başka bir yerden okur (Grep bulmadı, LOAD-BEARING değişiklik gerekirse üç noktayı hizalamak lazım).
12. **B09 ailesi: instant-eat retired, ama yamalar kalıntısı var.** `NeedConsumptionSystem.cs` yorumları hâlâ "H1, narrowed twice" açıklıyor (line 5-9); dosya adı değişmedi, testler `NeedConsumptionSystemTests` adında ama yalnız FoodSpots pinliyor — okuyan bir yeni geliştirici için isim yanıltıcı, `FoodSpotRegistry` gibi bir refactor ismi geç kalmış.
13. **`NeedsStep` özet olayı `anchor` aktör kullanır (deterministik ilk canlı aktör).** Aktör silinirse anchor kayar ve olay ActorId'si değişir — `WorldStateDigest`'e giriyorsa golden re-baseline gerekir (aktör death'ler zaten bunu tetikliyor ama sebep zinciri örtük).
14. **Hourly cadence olay logu spam'i öldürüldü, ama TickActorNeeds spam'i öldürülmedi.** CascadeSystems saldırı sırasında `NeedChanged` yazar (`CascadeSystems.cs:89`), AmbientLife de yer (`AmbientLifeSystem.cs:51, 70`) — özet ile per-actor tüketicilerin karışması (#2) gen2 GC'yi tekrar açabilir; ölçüm yok.

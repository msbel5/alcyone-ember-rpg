# W33 / Doc 02 — Jobs → Decision köprüsü: kıtlık kaskadı GERÇEK çiftçiye bağlanır (B05'in kökü kapanır, B06 ölür)

> Teşhis referansı: `docs/RUH_TESHIS.md` §9 ikinci dikey dilim ("seed item → plot rezervasyonu →
> çiftçinin yürüyüşü → Plant action → gerçek plant instance → growth → Harvest action → stockpile").
> Bug referansı: `docs/ruh/w32/00-bug-triage.md` B05 (5101 ghost-cancel spot fix'i geçicidir) ve
> B06 (köy üretimi oyuncunun çantasında pişiyor).
>
> Kapsam: SADECE job→action köprüsü + recipe IO köprüsü. Growth/soil biyolojisi W33'ün diğer
> dokümanlarının işi; Haul action ve çok-türlü tohum kataloğu İLERİYE bırakılır (§12).
>
> Desen sözleşmesi: W32 EAT dilimi (commit `5049d445`) YENİDEN KULLANILIR, yeniden icat edilmez:
> `ActorActionState` + append-only enum'lar, `ActionLifecycleSystem` tek-yazar decide+advance,
> `IActionAdvancer` stratejileri + `ActionAdvancerRegistry`, `ActionAdvancer.TransitionTo` →
> `ActionLogManager` tek log dikişi, `ReservationLedger` adet-claim'i,
> `Assets/Tests/EditMode/Actions/` hikâye testleri. Chunking invariance HAKEMDİR.

---

## 1. Mevcut durumun kanıtı (okunan gerçek kod)

Kaskadın bugünkü ölü ucu ve iki hayalet:

- **Kıtlık job'u POSTALANIYOR ama kimse gerçekten çalışmıyor.**
  `Assets/Scripts/Simulation/World/ShortageResponseSystem.cs:27-62` — günlük tarama, stok < 4
  (`ShortageThreshold` :18) olan her (pile, tag) için `FarmingJobRequestFactory.CreatePlantingJob`
  (recipe **5101**, `FarmingJobRequestFactory.cs:17`) ile job basar; `HasPendingPlanting` (:74-81)
  aynı site için ikinci job'u bastırır. Stateless + boundary-stamp sözleşmesi (CAN SUYU) doğru.
- **B05 spot fix = hayalet-iptal.** `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs:178-210`
  (`JobAssignmentStep.Run`): claim edilmiş her request için `ProductionRecipeRegistry.Resolve`
  denenir; 5101/5102 kayıtlı DEĞİL (`ProductionRecipeRegistry.cs:49-54` yalnız 1001/1002), yani
  `KeyNotFoundException` → `job_dropped recipe:5101 unregistered` event'i (:192-195) → `Cancel`
  (:207-210). Sonuç: çiftçi claim eder, job aynı saat İPTAL olur, ertesi gün kaskad yeniden basar.
  Döngü "donmuyor" (B05 spot fix) ama SAHTE: tarla hiç ekilmiyor.
- **B06 canlı.** `DefaultTickSystems.cs:170,203,217` — `StartRecipeForClaim` ve `TickAssignedJobs`
  köy üretimini `world.PlayerInventory` üzerinden yürütür; `:212` `NextInventoryItemId(world.PlayerInventory)`
  çıktı item id'lerini oyuncunun çantasından türetir. Demirci cevheri oyuncunun cebinden yer,
  külçeyi oyuncunun cebine koyar.
- **HarvestStep = mesafeli ışınlama.** `DefaultTickSystems.cs:455-501` — günlük tarama: `ripe`
  bitki + `HarvestHandsService.FindHarvester` (`HarvestHandsService.cs:20-40`, Chebyshev ≤ 2'de
  HERHANGİ canlı sivil) → `pile.Add(p.SpeciesId, 2)` + bitki `seed`'e sıfırlanır (:496-498,
  "self-replant" — B05'in ekonomiyi ayakta tutan koltuk değneği). Aktör hiçbir eylem yaşamaz;
  yürüyüş yok, niyet yok, log yok — yakından geçen biri varsa hasat "oluverir".
- **İş ilerlemesi de ışınlı.** `JobAssignmentSystem.Tick.cs:88-93` — `TickAssignedJobs` aktif
  order'ları aktörün POZİSYONUNA hiç bakmadan ilerletir. Claim `ActorScheduleState.Assigned`
  yazar (`JobAssignmentSystem.cs` `TryClaimCandidate`), `ScheduleSystem.ChooseTarget`
  (`ScheduleSystem.cs:100-122`) iş saatinde aktörü worksite'a YÜRÜTÜR ama varış hiçbir şeye
  bağlı değildir: yürüyüş dekor, iş uzaktan biter.
- **Gerçek ekim/hasat atomları YETİM.** `PlantingSystem.TryPlant`
  (`Assets/Scripts/Simulation/Process/PlantingSystem.cs:15-75`) ve `HarvestSystem.TryHarvest`
  (`HarvestSystem.cs:17-`) üretim çağıranı sıfır; ikisi de `InventoryState` imzalı (tip uyuşmazlığının
  öbür yüzü).
- **Çiftçi kimliği zaten var.** `NpcRole.Farmer` (`WorldgenEnums.cs:55`) →
  `NpcRoleJobMapper.ToJobKind` → `JobKind.Farmer` (`NpcRoleJobMapper.cs:6-19`);
  `DomainSimulationAdapter.Worldgen.Npcs.cs:60-62` doğan her rollü NPC'ye
  `ActorJobPreference(kind, Active(1))` basar. `JobAssignmentSystem.TryAssignNext` +
  `TryGetActivePreference` + `IsRefusing` (açlık ≥ 80 / düşük mood reddi) deterministik eşleşmeyi
  ZATEN yapıyor — claim makinesi sağlam, claim SONRASI boş.
- **W32 dikişleri hazır.** `living.decision@PerTick:18` → `living.schedule@PerTick:20` →
  `living.action_advance@PerTick:22` (`DefaultTickSystems.cs:252-281`); `ScheduleSystem.Advance`
  action'lı aktöre dokunmaz (:50-52); `ActionAdvancer.TransitionTo` tek yazar dikişi
  (`ActionAdvancer.cs:50-63`); `ReservationLedger` aktör başına 1 satır + TTL sweep
  (`ReservationLedger.cs:33-59,86-100`).

Kadans gerçeği (`WorldTickComposer.cs:242-281`): her tick PerTick bandı → saat sınırında Hourly
bandı → gün sınırında Daily bandı, AYNI tick içinde bu sırayla. Chunked replay tick-tek-tek aynı
interleave'i oynar (REFORM #2) — yeni sistemler bu sözleşmeye otomatik girer.

---

## 2. Hedef akış

```text
Daily:27  kıtlık → planting job 5101 (free-soil şartıyla, §7.3)
Hourly:10 econ.jobs: ölü-claimant süpürme → TryAssignNext → idle çiftçi CLAIM (Assigned yazılır)
PerTick:18 decision: claim'li + actionsız + iş saati → tohum REZERVE → FarmIntent + MoveToField
PerTick:20 schedule: action'lı aktöre dokunmaz (mevcut kapı)
PerTick:22 advance: MoveToField tarlaya tek-tek yürür → varış → PlantSeed (3 tick)
          → COMMIT: pile'dan 1 tohum düşer + gerçek PlantComponent doğar + Jobs.Complete
          → JobCompleted event + schedule Idle → zincir biter, aktör serbest
Daily:20  growth: seed → sprout → ripe
Daily:22  (YENİ) ripe bitki → harvest job 5102   [Daily:25 HarvestStep SİLİNİR]
Hourly:10 claim → PerTick: MoveToField → HarvestCrop (3 tick)
          → COMMIT: pile += 2 "wheat" + 1 "wheat_seed"; bitki silinir, soil boşalır; Jobs.Complete
Daily:27  stok hâlâ < 4 ise kaskad boş soil'e yeni 5101 basar — çember teşhis §9'daki gibi kapanır
```

"İş ancak eylem zinciri biterse biter": job'un `Complete` çağrısı YALNIZCA zincirin son
advancer'ının atomik commit'inde yaşar. `job_dropped` hayaleti 5101/5102 için bir daha ateşlenemez.

---

## 3. Karar 1 — Farm işleri recipe şeridinden ÇIKAR, action şeridine GİRER

**Ayırıcı: `request.Kind == JobKind.Farmer`** (recipe id listesi değil — factory her farm job'una
zaten `JobKind.Farmer` + `WorksiteKind.Field` basıyor, `FarmingJobRequestFactory.cs:58-59`).

`DefaultTickSystems.JobAssignmentStep.Run` (:173-210) değişimi:

```csharp
foreach (var request in world.Jobs.Requests)
{
    if (!world.Jobs.IsClaimed(request.Id)) continue;
    // W33: Farmer işleri BEDENLİ çalışır — recipe şeridi (uzaktan-ilerleyen RecipeSystem)
    // değil, action şeridi (decide@18 + advance@22) yürütür; Complete'i advancer basar.
    if (request.Kind == JobKind.Farmer) continue;
    ... // Resolve + ghost-cancel + StartRecipeForClaim aynen kalır (gerçek bilinmeyen id'ler için ağ)
}
```

- `TryAssignNext` claim makinesi (öncelik/refusal/worksite eşleşmesi) OLDUĞU GİBİ yeniden
  kullanılır — `TryGetActiveMatchingWorksite` Field worksite'ı doğrular (worldgen `farmPos`'a
  `WorksiteKind.Field` basıyor, `DomainSimulationAdapter.Worldgen.Production.cs:47`).
- `job_dropped` yolu (:178-195) SİLİNMEZ: 5101/5102 artık ona ulaşmadığından gerçekten kayıtsız
  recipe'ler için güvenlik ağı olarak kalır. `ProductionRecipeRegistry`'ye 5101/5102 KAYDEDİLMEZ —
  registry, RecipeSystem şeridinin (sabit tezgâh craft'ları) kataloğudur; ekim bir craft değildir.
- **Ölü-claimant süpürmesi (B05'in hortlamaması için ŞART).** Zincir başarısızlığı claim'i canlı
  aktörde bırakır (yeniden dener), ama claimant ÖLÜRSE job sonsuza dek claim'li kalır ve
  `HasPendingPlanting` o sitenin kaskadını yine dondurur — B05'in birebir yeniden doğuşu.
  `JobBoard`'a tek yeni API:

```csharp
/// <summary>Claim'i geri bırakır (entry pending'e döner). W33: ölü claimant süpürmesi.</summary>
public bool ReleaseClaim(JobId id)   // entry.ClaimedBy = default; ClaimSequence = 0
```

  `JobAssignmentStep.Run` başında deterministik süpürme (Requests sırası): claim'li request'in
  claimant'ı `!TryGet || !IsAlive` ise `ReleaseClaim` + `ChronicleEvent "job_claim_released
  reason:claimant_dead"`. Job pending'e döner, bir sonraki saat başka çiftçi alır.

**Neden recipe şeridi değil:** (a) `RecipeSystem` girişleri START'ta yer (`RecipeSystem.cs:53`),
zincir yarıda kesilirse madde iadesi yok — EAT'in commit-anında-tüket dersi tersine dönerdi;
(b) `_activeOrders` sistem-örneği durumudur (`JobAssignmentSystem.cs:23-24`, save'i
`WorldSaveRehydration` ayrıca taşır), action şeridinde ilerleme `ActorActionState`'te yaşar ve
aktörle birlikte OTOMATİK kaydolur; (c) iş konum şartı recipe şeridine yamanamazdı (ışınlı ilerleme
B06 ile aynı sınıf koku).

---

## 4. Karar 2 — Enum genişlemesi (save sözleşmesi: append-only, all-zero = Idle)

`Assets/Scripts/Domain/Actors/ActorActionState.cs`:

```csharp
public enum ActorIntent      { None = 0, Eat = 1, Farm = 2 }                       // append
public enum ActorActionType  { None = 0, MoveToFood = 1, TakeFood = 2, ConsumeFood = 3,
                               MoveToField = 4, PlantSeed = 5, HarvestCrop = 6 }   // append
public enum ActionFailureReason { ..., SourceDrained = 6, JobLost = 7, PlantGone = 8 } // append
```

- **Yeni ALAN yok.** Tarla hedefi `actor.ScheduleState.TargetWorksitePosition`'dan türetilir
  (claim yazıyor, save zaten taşıyor); site `TargetSiteId`'ye, tohum claim'i `ReservationId`'ye
  biner; `TargetItemId` farm zincirinde Empty kalır. `default(ActorActionState) == Idle` ve
  all-zero-extends-Idle sözleşmeleri kendiliğinden korunur — W32 öncesi VE W32 save'leri
  değişmeden yüklenir.
- **Sınır güncellemeleri (üçü de aynı PR'da, biri unutulursa test kırmızı):**
  1. `ActorActionState.TryRestore` (:168-196): `action > ActorActionType.ConsumeFood` →
     `> HarvestCrop`; `intent > Eat` → `> Farm`; `failureReason > SourceDrained` → `> PlantGone`.
     (`ActorSaveMapper`/`ActorActionStateSaveReader` TryRestore'a delege ettiğinden save yolu
     başka değişiklik istemez.)
  2. `ActionAdvancerRegistry.cs:15`: dizi boyu `(int)ActorActionType.ConsumeFood + 1` →
     `(int)ActorActionType.HarvestCrop + 1`.
  3. `ActionAdvancer.ToLogReason` (:82-90): `JobLost → InterruptPreempted`,
     `PlantGone → TargetGone` (ActionLogReason enum'una üye EKLENMEZ — mevcut gramer yeter).
- Projeksiyon: `ActionVerbTable.cs:19-31`'e üç satır (`MoveToField → "walking to field"`,
  `PlantSeed → "planting"`, `HarvestCrop → "harvesting"`); UI verbatim okumaya devam eder
  (`DomainSimulationAdapter.WorldProjection.cs:117`).

---

## 5. Karar 3 — Decide fazı: farm kuralı (EAT kuralının ARKASINA)

`ActionLifecycleSystem.Decide` (:36-56) mevcut kapı sırası korunur; `TryDecideEat` denendikten
sonra (karar çıkmadıysa) farm kuralı çalışır. Açlık > iş: yemek zinciri biter
(`MealHungerFloor = 5` < 55), ertesi tick farm kuralı ateşlenir — kilitlenme yok. Kural sırası
kod sırasıyla SABİT (deterministik öncelik, W32-02 §3.3 stratejisiyle aynı ilke).

Farm kapıları (ucuzdan pahalıya, hepsi alan okuması + O(1) lookup):

```csharp
// 1) actor.ScheduleState.CurrentJobId boş → geç (claim yok)
// 2) !ScheduleSystem.IsWorkHour(stamp) → geç (gece tarla yok; schedule eve yürütür)
// 3) world.Jobs.TryGet(jobId, out req) değil VEYA GetClaimedBy != actor.Id → geç
//    (savunma: süpürme/iptal yarışı — bir sonraki saat düzelir)
// 4) req.Kind != JobKind.Farmer → geç (recipe şeridinin işi, ona karışmayız)
```

**5101 (ekim) dalı:** türün tohum tag'i katalogdan gelir — `ActionLifecycleSystem` ctor'u
composer'ın `BuildDefaultPlantSpecies` listesini alır (`WorldTickComposer.cs:104-122`; bu dilimde
tek tür: "wheat" / tohum "wheat_seed"). Sonra EAT'in rezervasyon deseni birebir:

```csharp
var pile = FoodOperations.FindPile(world, req.SiteId.Value);      // yoksa: karar yok, bekler
long walk = Chebyshev(actor.Position, req.WorksitePosition);
long until = stamp.TotalMinutes + walk + PlantSeedAdvancer.PlantDurationTicks + 60; // W32-02 §4.3 TTL
if (!world.Reservations.TryReserve(req.SiteId.Value, seedTag, actor.Id.Value,
        until, pile.Get(seedTag), out var resId))
    return; // tohum yok/hepsi claim'li: job PENDİNG DEĞİL CLAİM'Lİ bekler, aktör schedule'a düşer
var start = ActorActionState.ForIntent(ActorIntent.Farm).Start(
    ActorActionType.MoveToField, req.SiteId, ItemId.Empty,
    new ReservationId(resId), stamp.TotalMinutes, ActionInterruptPolicy.Interruptible);
_registry.For(ActorActionType.MoveToField).TransitionTo(world, actor, start,
    ActionLogReason.ReservationAcquired, stamp);
```

- Tohumsuz site DONMAZ: job claim'li bekler (kaskad `HasPendingPlanting` ile doğru şekilde
  susar), tohum pile'a düşünce (hasat iadesi §6.3 / worldgen başlangıç stoğu §7.4) zincir başlar.
  Tohum SAYISI korunumludur (§6.3) — kalıcı açlık ancak tohum sızıntısıyla mümkün olur, o da
  T5 testinin konusudur.
- `ReservationLedger`'ın aktör-başına-1-satır kuralı YAPISAL olarak tutar: decide aktör başına tek
  action verir, eat ve farm aynı anda başlayamaz.
- **5102 (hasat) dalı:** rezervasyon YOK — job claim'i zaten bitkinin tekil kilididir (JobBoard
  aktör başına 1 claim, `TryClaim` :104-105). Decide yalnız hedef doğrular: `req.WorksitePosition`
  hücresinde `ripe` bitki hâlâ var mı (`world.Plants.Rows` taraması; dilim ölçeğinde ucuz, dizin
  §12). Yoksa: `world.Jobs.Cancel(jobId)` + `ChronicleEvent "job_dropped reason:plant_gone"` +
  `actor.ApplyScheduleState(Idle)` — hayalet yürüyüş baştan kesilir. Varsa aynı `Start` bloğu,
  `ReservationId.Empty` ile.

**Zincir türetimi:** `NextLink` (:95-100) saf switch kalamaz — `MoveToField`'ın devamı job'un
recipe'sine bağlı. Advance'in handover koluna dünya-bağlamlı overload:

```csharp
// Zincir SAVE EDİLMEZ (W32-01 §8): intent + JobBoard'daki claim'den türetilir.
private static ActorActionType NextLink(WorldState world, ActorRecord actor, ActorActionType current)
{
    if (current == ActorActionType.MoveToField)
    {
        var jobId = actor.ScheduleState.CurrentJobId;
        if (!jobId.IsEmpty && world.Jobs.TryGet(jobId, out var req))
            return req.RecipeId.Equals(FarmingJobRequestFactory.PlantCropRecipeId)
                ? ActorActionType.PlantSeed : ActorActionType.HarvestCrop;
        return ActorActionType.None; // job handover anında yoksa zincir sessizce Idle'a düşer
    }
    return current switch { MoveToFood => TakeFood, TakeFood => ConsumeFood, _ => None };
}
```

---

## 6. Karar 4 — Advancer'lar (Template Method'a üç yeni strateji)

Üçü de `ActionAdvancer` tabanından türer: pursuit interruption probu, `TransitionTo` dikişi,
`Fail` kapısı BEDAVA gelir. Paylaşılan lookup'lar `FarmOperations` (FoodOperations'ın aynası,
`internal static`): `FindPlantAt(world, site, pos)`, `FindSoilAt(world, site, pos)`,
`FindFreeSoil(world, site)`, `SpeciesOf(catalog, speciesId)`.

### 6.1 `MoveToFieldAdvancer` (`Handles => MoveToField`)

MoveToFoodAdvancer'ın iskeleti, hedef farklı:

- Doğrulama: `world.Jobs.TryGet(jobId)` + `GetClaimedBy == actor.Id` değilse
  `Fail(JobLost)` (claim süpürüldü/iptal edildi). 5101 için ek: rezervasyon satırı hâlâ aktörün mü
  (`TryGetByActor` + id eşitliği), pile'da tohum duruyor mu — değilse `Fail(ReservationLost /
  SourceDrained)` (MoveToFood :22-37 ile birebir).
- Adım: `MovementService.StepToward(actor.Position, sched.TargetWorksitePosition)`; varış =
  hücrenin ÜSTÜNDE durmak (plot tek hücre; seat halkası yok). Varışta `Succeeded()`
  (`ActionLogReason.Arrived`), değilse `Advanced()`.

### 6.2 `PlantSeedAdvancer` (`Handles => PlantSeed`, `PlantDurationTicks = 3`)

ConsumeFoodAdvancer'ın faz iskeleti: her step doğrula → süre dolana dek `Advanced()` → son tick
ATOMİK COMMIT. Doğrulamalar: job claim (yoksa `Fail(JobLost)`), rezervasyon + pile'da tohum
(yoksa `Fail(ReservationLost/SourceDrained)`), plot'a mesafe ≤ 1 (itilmişse `Fail(Unreachable)`).

Commit sırası (tek step içinde, tek yazar):

```csharp
var soil = FarmOperations.FindSoilAt(world, req.SiteId, req.WorksitePosition)
        ?? FarmOperations.FindFreeSoil(world, req.SiteId);       // advisory pozisyon doluysa
if (soil == null || soil.HasPlant)
{   // plot kalıcı geçersiz: action DA job DA ölür — sonsuz yeniden-karar döngüsü yasak
    world.Jobs.Cancel(jobId); actor.ApplyScheduleState(Idle);
    Fail(world, actor, ActionFailureReason.PlantGone, stamp); return;
}
pile.Remove(seedTag, 1);                                          // tohum ANCAK commit'te düşer
world.Reservations.Release(row.Id);
var plantId = new WorldComponentId(PlantIdBand + soil.Id.Value);  // §6.4 deterministik kimlik
world.Plants.Add(plantId, new PlantComponent(plantId, soil.SiteId, soil.Position,
    species.SpeciesId, species.FirstStage.Id, 0));
world.Soils.Replace(soil.Id, soil.WithPlant(plantId));
world.Events.Append(... PlantPlanted, "plant_planted:..." ...);   // PlantingSystem.cs:58-73 satırı VERBATIM
world.Jobs.Complete(jobId);                                       // iş, EYLEMLE biter — dilimin cümlesi
world.Events.Append(... JobCompleted, $"job_completed:{jobId}" ...); // Tick.cs:129-143 formatı VERBATIM
actor.ApplyScheduleState(ActorScheduleState.Idle);
TransitionTo(world, actor, progressed.Succeeded(), ActionLogReason.Completed, stamp);
```

Event satırları `PlantingSystem.TryPlant` ve `TickAssignedJobs`'tan kelimesi kelimesine alınır
(ConsumeFood'un `meal_eaten` dersi: chronicle/proof tüketicileri kırılmaz). `PlantingSystem` /
`HarvestSystem` InventoryState imzalı atomlar olarak kalır (oyuncu şeridi; sim çağıranı yok) —
emekliliği §12.

### 6.3 `HarvestCropAdvancer` (`Handles => HarvestCrop`, `HarvestDurationTicks = 3`)

Doğrulama: job claim + plot'ta `ripe` bitki (`Fail(PlantGone)` değilse) + mesafe ≤ 1. Commit:

```csharp
pile.Add(plant.SpeciesId, HarvestYieldUnits);      // 2 — HarvestStep'in verimi, tag DÂHİL aynı
pile.Add(species.SeedItemTag, SeedReturnUnits);    // 1 — tohum İADESİ: sayım korunur, döngü kapanır
world.Plants.Remove(plant.Id);
world.Soils.Replace(soil.Id, soil.WithoutPlant()); // self-replant koltuk değneği ÖLDÜ: yeniden
                                                   // ekim artık kaskadın (5101) gerçek işi
world.Events.Append(... PlantHarvested, $"harvested species:{...} qty:2 by:{actor.Id.Value}");
world.Jobs.Complete(jobId); + JobCompleted event + schedule Idle + Succeeded  // §6.2 ile aynı blok
```

- **Tag kararı:** verim tag'i `plant.SpeciesId` ("wheat") KALIR — `wheat_grain`
  (`PlantSpeciesDef.HarvestItemTag`) bugün hiçbir tüketicinin tanımadığı bir tag'dir; EAT cache'i
  ve kıtlık dedektörü tag'leri SpeciesId'den türetir (`ShortageResponseSystem.FoodTags` :93-101).
  "wheat"/"wheat_grain" şizmi §12'de adlandırılmış borç. `wheat_seed` SpeciesId olmadığı için
  yemek sayılmaz — tohumlar YENMEZ (bedava doğruluk).
- **Madde korunumu:** tohum yalnız commit'te düşer, elde taşınan birim YOK — dolayısıyla farm
  zincirinin TÜM başarısızlık yolları release-only'dir (EAT'in ConsumeFood iade dansı burada
  gerekmez; `Fail` tabanındaki ConsumeFood dalı farm action'larına dokunmaz).

### 6.4 Deterministik kimlikler

- `PlantIdBand = 5_000_000_000UL`; `plantId = PlantIdBand + soilId`. 1 soil ↔ en çok 1 bitki
  olduğundan saf fonksiyon yeter: sayaç yok, save alanı yok, `ComponentStore.Add` çakışamaz
  (hasat önce Remove etti). Worldgen'in küçük id'leriyle (`base*10+2`,
  `Worldgen.Production.cs:51-56`) bant ayrışması.
- Reddedilen alternatif: `ReservationLedger.NextId` tarzı persist sayaç — yeni save alanı +
  rehydration yükü, sıfır kazanç.

---

## 7. Karar 5 — HarvestCrop tetiği: `RipeCropJobStep` (Daily:22), `HarvestStep` SİLİNİR

### 7.1 Yeni adım

`DefaultTickSystems`'e `RipeCropJobStep : StepBase("econ.harvest_jobs", TickCadence.Daily, 22)` —
growth(20) ripe yapar, 22 job basar, kıtlık(27) ve fiyat(30) aynı günün gerçeğini okur. Gövde
(ShortageResponseSystem deseninin aynası, stateless):

```csharp
foreach ripe plant:                                  // Plants.Rows sırası — deterministik
    jobId = new JobId(HarvestJobIdBase + plant.Id.Value);   // 8_800_000_000_000UL bandı
    if (world.Jobs.Contains(jobId)) continue;        // pending/claimed dedup — id bitkiye SABİT
    world.Jobs.Add(FarmingJobRequestFactory.CreateHarvestJob(
        jobId, plant.SiteId, plant.Position, FirstCivilianId(world), JobPriority.Active(1), 1));
    event: "harvest_job_posted reason:ripe"
```

Bitki ancak hasatla `ripe`'tan çıkar (`daysToNextStage: 0` terminal) → job tamamlanınca bitki
silinir, aynı soil'in GELECEK bitkisi YENİ plant id almaz (soil-sabit id, §6.4) ama eski job
`Complete` ile tahtadan düştüğü için `Contains` yeniden basıma izin verir. Çiftçisiz sitede job
pending bekler — M6 semantiği ("plot ripe BEKLER") korunur, ama artık ışınsız.

### 7.2 `HarvestStep`'in ölümü

`DefaultTickSystems.cs:455-501` komple silinir (`world.harvest@Daily:25` slotu boşalır);
`HarvestHandsService` üretim çağıransız kalır → silinir; `ReachCells = 2` sabitini kullanan tek
yer `WorldProjection.cs:144` — sabit `FarmOperations.FieldReachCells`'e taşınır. Goldenlarda
`PlantHarvested` satırlarının zamanı kayar (gün-sınırından zincir-commit tick'ine): re-baseline
BEKLENEN, chunking hakemi değişmeden yeşil kalmak ZORUNDA.

### 7.3 `ShortageResponseSystem` düzeltmeleri (aynı PR)

- `FieldPositionFor` (:83-91) bugün MEVCUT bitkinin pozisyonunu verir — dolu plot'a ekim job'u!
  Değişim: sitenin BOŞ soil'inin pozisyonu (`FarmOperations.FindFreeSoil`, Rows sırası ilk);
  boş soil yoksa o (pile, tag) için JOB BASILMAZ (post-kapısı) — §6.2'nin plot-invalid iptali
  hiç doğmadan önlenir, günlük post→iptal salınımı imkânsızlaşır.
- `HasPendingPlanting` (:74-81) yalnız 5101'e bakar — hasat job'ları ekimi bastırmaz (doğru);
  değişiklik gerekmez, satır testle pinlenir.

### 7.4 Worldgen başlangıç stoğu

`DomainSimulationAdapter.Worldgen.Production.cs` site pile'ına `wheat_seed × 2` ekler (pile yoksa
HarvestStep'teki oluştur-deseni). Tohum korunumu (§6.3) sayesinde bu 2 birim koloninin ebedi
döner sermayesidir.

---

## 8. Karar 6 — B06: `IRecipeInventory` köprüsü (tag-count yeter)

Tip uyuşmazlığının teşhisi: `RecipeSystem` yalnız TAG-COUNT işlemleri yapar — `HasInputs` sayar
(:141-157), `ConsumeInputs` `TryRemoveStackable` (:159-166), çıktı `TryAdd` (:88-95). Item
KİMLİĞİ (`ItemId`, `Func<RecipeOutput, InventoryItem>` mint fabrikası, `NextInventoryItemId`
hack'i `DefaultTickSystems.cs:212`) yalnız `InventoryState`'in iç ihtiyacıdır. Köprü bu gözlemin
kendisidir:

```csharp
// Assets/Scripts/Domain/Process/IRecipeInventory.cs — Domain, saf, Unity/IO yok
public interface IRecipeInventory
{
    int CountOf(string itemTag);                  // ekipman-dışı birim sayısı
    bool TryConsume(string itemTag, int quantity); // all-or-nothing (kısmî tüketim YASAK)
    bool TryAccept(string itemTag, int quantity);  // kapasite reddi hakkı (stockpile: hep true)
    IRecipeInventory CloneForPreflight();          // CanStartRequestedQuantity klon-provası için
}
```

İki adaptör:

- `StockpileRecipeInventory(StockpileComponent pile)`: `CountOf → pile.Get`;
  `TryConsume → Get >= q && Remove(tag, q) == q` (Remove up-to davranışına karşı ön-kontrol —
  atomiklik adaptörün sorumluluğu); `TryAccept → pile.Add(tag, q); true`; `CloneForPreflight` →
  tag sayımlarının bağımsız kopyası (pile'a DOKUNMAYAN sözlük-iç kopya).
- `InventoryRecipeAdapter(InventoryState inv, Func<string, InventoryItem> mint)`: mevcut
  davranış birebir — `TryAccept` kapasite dolunca false, mint fabrikası ADAPTÖRÜN içine göçer
  (unique ItemId üretimi artık çağıranın değil, kimliğe ihtiyacı olan tarafın derdi).

**Çağıran envanteri (görev şartı — tam liste):**

| Çağıran | Konum | W33 sonrası |
|---|---|---|
| `JobAssignmentStep.Run` → `StartRecipeForClaim` | `DefaultTickSystems.cs:198-205` | `new StockpileRecipeInventory(world.FindStockpile(request.SiteId) ?? oluştur)` — **B06 ölür** |
| `JobAssignmentStep.Run` → `TickAssignedJobs` | `DefaultTickSystems.cs:212-224` | site-başına IO çözücü (aşağıda); `NextInventoryItemId` hack'i SİLİNİR |
| `JobEventLogTests` | `Assets/Tests/EditMode/Process/JobEventLogTests.cs:66-69` | dokunulmaz (InventoryState sarmalayıcı overload'lar kalır) |
| `JobAssignmentSystemTests` | `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs` (30 kullanım, iki dosya toplamı) | dokunulmaz |
| (dolaylı) `CanActorWorkJob(recipe, inventory)` / `CanStartRequestedQuantity` | `JobAssignmentSystem.cs:170-193, 561-586` | parametre `IRecipeInventory`'ye döner; InventoryState overload'ı adaptörle delege |
| (dolaylı) `RecipeSystem.TryStart/Tick` | `RecipeSystem.cs:28-110` | `IRecipeInventory` overload'ları eklenir; eski imzalar delege |

**Kritik imza dalgası — tek gerçek kırılım:** `TickAssignedJobs` TEK inventory alır ama
`_activeOrders` FARKLI sitelerin order'larını karıştırır (`Tick.cs:88-93`). Site-doğru IO için
ana overload `Func<SiteId, IRecipeInventory> ioForSite` alır; order başına `pair.Value.SiteId`
ile çözülür. Eski InventoryState overload'ları `_ => adapter` sabitiyle delege eder (testler ve
davranışları değişmez). `world.PlayerInventory` SADECE oyuncu-başlatmalı craft şeridinde kalır
(W31 smith commission zaten doğru).

Köy üretim çıktısı böylece tag-count olur: külçe `ItemId`'siz, sitenin pile'ında — `PriceStepSystem`
(`stockpile.Entries` okur) ve kıtlık dedektörü ilk kez NPC üretimini GERÇEKTEN görür.

---

## 9. Tick sırası — bir günün zaman çizelgesi

```text
gün d, tick T (gün sınırı): PerTick(18 decide, 20 schedule, 22 advance)
  → Hourly(econ.jobs@10: süpürme+claim+recipe şeridi; needs@30)
  → Daily(growth@20 → harvest_jobs@22 → shortage@27 → prices@30)   [harvest@25 YOK artık]
gün d, saat s+1 sınırı: econ.jobs yeni 5101/5102'yi claim eder (Assigned yazılır)
sonraki tick: decide@18 rezervasyon+MoveToField → advance@22 ilk adım AYNI tick (W32-03 §4 kuralı)
varış+3 tick: commit — pile/Plants/Soils/JobBoard tek step'te, boundary-stamp ile
```

Sıra sabitleri: decide(18) < schedule(20) → karar alan aktörü router AYNI tick atlar (W32
sözleşmesi, değişmez). Yeni Daily 22, 20 ile 25 arası boş slottu; kayıt sırası değil ORDER alanı
belirleyici olduğundan registry çakışması yok.

---

## 10. Test planı (`Assets/Tests/EditMode/Actions/` + `Process/`)

W32-06 hikâye-testi deseni (kurulum-anlatı-tek iddia bloğu):

- **T1 — çember kapanır:** kıt stok → job → claim → yürüyüş (pozisyon adım adım) → PlantSeed →
  `Jobs.Contains == false` ANCAK commit tick'inde; PlantComponent seed'de; pile tohum -1;
  JobCompleted + PlantPlanted event'leri var. Job'un ERKEN tamamlanmadığı ayrıca pinlenir
  (yürüyüş ortasında `Contains == true`).
- **T2 — tohumsuz site donmaz:** tohumsuz pile → job claim'li bekler, action doğmaz, kaskad
  yeni job basmaz (`HasPendingPlanting` pin); pile'a tohum eklenince zincir başlar.
- **T3 — ışınlama öldü:** ripe bitki + UZAKTA çiftçi → eski dünyada hasat oluyordu; şimdi commit
  ancak aktör plot'a ≤ 1 hücredeyken; verim `wheat × 2 + wheat_seed × 1`; bitki silinmiş, soil boş.
- **T4 — hakem:** farm zincirli marathon, chunked vs tick-tek-tek replay bayt-eş
  (`ActionPhaseChunkingInvarianceTests`'e farm senaryosu eklenir).
- **T5 — madde korunumu:** yürüyüş ortasında pursuit interrupt → rezervasyon bırakılmış, pile
  tohum sayısı DEĞİŞMEMİŞ, claim çiftçide durur, sonraki tick yeniden dener. Tohum toplamı
  (pile + ekili bitki sayısı) zincir boyunca sabit.
- **T6 — ölü çiftçi:** zincir ortasında ölüm → econ.jobs süpürmesi claim'i bırakır → ikinci
  çiftçi işi bitirir; B05-hortlaması yok (kaskad donmaz).
- **T7 — B06 idam kanıtı:** demirci job'u site pile'ından cevher tüketir, külçeyi site pile'ına
  yazar; `world.PlayerInventory` öncesi/sonrası BAYT-EŞ.
- **T8 — save ortası zincir:** PlantSeed Running + rezervasyonlu kayıt → yükle → aynı tick'te
  aynı commit (golden). W32-öncesi save yükleme regresyonu (all-zero → Idle) yeniden koşulur.

---

## 11. Dosya manifesti + LOC bütçesi

| Dosya | İş | ~LOC |
|---|---|---|
| `Domain/Actors/ActorActionState.cs` | 3 enum üyesi + TryRestore sınırları | +10 |
| `Domain/Process/IRecipeInventory.cs` | YENİ arayüz | 25 |
| `Domain/Process/StockpileRecipeInventory.cs` | YENİ adaptör | 45 |
| `Domain/Inventory/InventoryRecipeAdapter.cs` | YENİ adaptör (mint içeride) | 50 |
| `Domain/Process/JobBoard.cs` | `ReleaseClaim` | +12 |
| `Simulation/Living/Actions/FarmOperations.cs` | YENİ ortak lookup | 45 |
| `Simulation/Living/Actions/MoveToFieldAdvancer.cs` | YENİ | 45 |
| `Simulation/Living/Actions/PlantSeedAdvancer.cs` | YENİ | 70 |
| `Simulation/Living/Actions/HarvestCropAdvancer.cs` | YENİ | 65 |
| `Simulation/Living/Actions/ActionLifecycleSystem.cs` | farm kuralı + NextLink overload + ctor kataloğu | +55 |
| `Simulation/Living/Actions/ActionAdvancer.cs` | ToLogReason 2 satır | +2 |
| `Simulation/Living/Actions/ActionAdvancerRegistry.cs` | dizi sınırı | ±1 |
| `Simulation/Process/RecipeSystem.cs` | IRecipeInventory overload'ları | +40 |
| `Simulation/Process/JobAssignmentSystem(.Tick).cs` | IO parametre göçü + ioForSite | +35 |
| `Simulation/World/ShortageResponseSystem.cs` | FieldPositionFor free-soil + post-kapısı | +15 |
| `Simulation/Composition/DefaultTickSystems.cs` | Farmer-skip, süpürme, stockpile IO, RipeCropJobStep; HarvestStep −47 | net +30 |
| `Simulation/Process/HarvestHandsService.cs` | SİL (ReachCells → FarmOperations) | −40 |
| `Presentation/.../ActionVerbTable.cs` | 3 fiil | +6 |
| `Presentation/.../Worldgen.Production.cs` | başlangıç tohumu | +6 |
| Testler (T1-T8 + mevcut pin güncellemeleri) | | ~450 |

Üretim kodu net ~+520: dilim bütçesi içinde; en büyük kalemler adaptörler (B06) ve üç advancer
(dilimin kendisi).

---

## 12. Bilinçli sınırlar / ileriye bırakılanlar

1. **Haul action yok:** hasat verimi doğrudan site pile'ına yazılır (teşhis §9'un "crop item
   aktörün elinde → Haul → stockpile" adımı sıkıştırıldı). Elde-taşıma, EAT'in TakeFood desenine
   sahip ayrı bir W3x dilimi.
2. **Tek tür:** "wheat" hard-coded değil ama katalog tek elemanlı; `JobRequest`'e species tag
   alanı eklemek yerine katalogdan çözülüyor. Çok tür geldiğinde job'a tag alanı gerekir.
3. **"wheat" vs "wheat_grain" şizmi:** verim tag'i SpeciesId kalıyor; `HarvestItemTag`/`SeedItemTag`
   ayrımı tam anlamını Haul/mutfak dilimlerinde bulacak. Adlandırılmış borç.
4. **Plants/Soils pozisyon dizini yok:** decide + advancer taramaları Rows-lineer; koloni ölçeği
   büyüyünce (site, pos) dizini.
5. **`PlantingSystem`/`HarvestSystem` emekliliği:** InventoryState imzalı iki atom üretim
   çağıransız kalıyor; event satırları advancer'lara verbatim taşındıktan sonra ayrı bir temizlik
   PR'ında silinmeli (bu PR'da değil — davranış değişimiyle silme karışmaz).
6. **Recipe şeridinde konum şartı hâlâ yok:** demirci/fırıncı işleri uzaktan ilerlemeye devam
   ediyor (B06 çözüldü, "ışınlı emek" çözülmedi). Farm deseni kanıtlanınca aynı köprüyle taşınır.

# W34 / Doc 02 — WORK dilimi: son tiyatro ölür — üretim, tezgâh başındaki BEDENE bağlanır

> Teşhis referansı: `docs/RUH_TESHIS.md` §2.6 ("İş yapmak ile iş yerine yürümek bağlı değil"),
> §8 madde 5 ("İş ilerlemesi ancak aktör worksiteda ve `PerformWork` aşamasındaysa olur") ve
> §10 kabul satırları ("Aktör worksite dışında işi ilerletemez", "Input olmadan output oluşmaz").
> Borç referansı: `docs/ruh/w33/02-jobs-decision.md` §12.6 — "Recipe şeridinde konum şartı hâlâ
> yok: demirci/fırıncı işleri uzaktan ilerlemeye devam ediyor (B06 çözüldü, 'ışınlı emek'
> çözülmedi). Farm deseni kanıtlanınca aynı köprüyle taşınır." Bu doküman o taşımadır.
>
> Kapsam: SADECE farm-dışı claim'li işlerin (smelt **1001**, bake **1002**) action şeridine
> taşınması + order ilerlemesinin bedene kilitlenmesi + order durumunun WorldState'e göçü.
> Yeni recipe içeriği, worksite kapasitesi, çok-execution paralelliği YOK (§13).
>
> Desen sözleşmesi: W32 EAT (`5049d445`) + W33 FARM (`61e340f3`) makinesi YENİDEN KULLANILIR,
> yeniden icat edilmez: `ActorActionState` append-only enum'lar (`Domain/Actors/ActorActionState.cs`),
> `ActionLifecycleSystem` tek-yazar decide+advance (`Simulation/Living/Actions/`), `ActionAdvancer`
> tabanı + `ActionAdvancerRegistry` tip-anahtarlı tablo, `TransitionTo` → `ActionLogManager` tek
> log dikişi, `ReservationLedger` anahtar-kodlu claim (FARM'ın `plot:{id}` emsali), W33 B06
> `IRecipeInventory` site-pile köprüsü (`StockpileRecipeInventory`), `Assets/Tests/EditMode/Actions/`
> hikâye testleri. Determinizm anayasası + all-zero-extends-Idle save sözleşmesi geçerli;
> chunking invariance HAKEMDİR.

---

## 1. Mevcut durumun kanıtı (okunan gerçek kod)

Farm dilimi bedenlendi; farm-DIŞI üretim hâlâ üç ayrı tiyatro oynuyor:

- **RecipeSystem konum bilmiyor.** `Assets/Scripts/Simulation/Process/RecipeSystem.cs:27-57`
  (`TryStart`, InventoryState) ve `:64-98` (W33 `IRecipeInventory` overload'ı): gate merdiveni
  worksite varlığı + kind eşleşmesi + girdi sayımı — **aktör pozisyonu parametre olarak bile
  yok**. `Tick` (`:105-144` io, `:152-197` InventoryState) aynı: order + envanter + event log,
  beden yok. (Teşhis §2.6'nın birebir cümlesi: "aktör konumu parametre olarak bile verilmez".)
- **TickAssignedJobs her çağrıda HER order'ı ilerletir.** `JobAssignmentSystem.Tick.cs:86-90`
  (InventoryState şeridi) ve `:189-193` (W33 tag-count şeridi): `foreach (_activeOrders)` →
  `recipeSystem.Tick(...)` — koşulsuz, sorgusuz. Çağıran `DefaultTickSystems.cs:242-248`
  (`econ.jobs@Hourly:10`): claim'li demirci evinde uyurken external saat sayaç işletir.
  İlerleme emeğin değil, TAKVİMİN fonksiyonudur — "free-running ticks".
- **Başlangıç da ışınlı.** `JobAssignmentSystem.cs:199-258` (`StartRecipeForClaim`): aktör canlı
  mı (`:229`), schedule'ı bu job'da mı (`:231`) bakar; **worksite'a ulaştı mı HİÇ bakmaz**
  (teşhis §2.6, :199-257 atfı). io-overload'ı (`:265-324`) W33'te girdileri site pile'ına
  bağladı (B06 öldü) ama TÜKETİM ANI hâlâ claim-sonrası ilk saat başıdır: cevher, demirci daha
  yoldayken tezgâha ışınlanır.
- **Yürüyüş dekor.** Claim `ActorScheduleState.Assigned` yazar (`JobAssignmentSystem.cs:346-363`),
  `ScheduleSystem.ChooseTarget` iş saatinde aktörü `TargetWorksitePosition`'a yürütür
  (`ScheduleSystem.cs:115-126`) — ama varış hiçbir şeyi tetiklemez. Ekrandaki demirci yürür;
  sayaç başka bir bantta, başka bir kadansta, başka bir sebepten döner. Paralel koreografi.
- **Order durumu sistem-örneği hafızasında ve save yolu KOPUK.**
  `JobAssignmentSystem.cs:23-24`: `_activeOrders` + `_completedExecutionCounts` iki private
  Dictionary. DTO var (`WorldSaveData.cs:61` `recipeWorkOrders`,
  `WorldSaveData.WorldProcess.cs:39-47` — dikkat: satırda **jobId alanı yok**), mapper var
  (`WorldSaveRehydration.cs:28-64`), ama yükleme yolu order'ları
  `JsonSliceSaveService._recipeWorkOrders`'a park eder (`JsonSliceSaveService.cs:134`) ve
  üretim kodunda `RecipeWorkOrders`/`ReplaceRecipeWorkOrders` OKUYAN TEK ÇAĞIRAN YOKTUR.
  Sonuç — canlı yara: save'de claim'li job + taze (boş) `_activeOrders` → bir sonraki saat
  `StartRecipeForClaim` AYNI execution için girdileri İKİNCİ KEZ tüketir. Madde korunumu
  save/load sınırında delik.
- **RecipeCompleted fosil damgalı.** `RecipeSystem.cs:131-133`: event zamanı
  `new GameTime(order.ProgressTicks)` — dünya saati değil, order'ın kendi sayacı. W32/W33
  boundary-stamp disipliniyle çelişen bir fosil.
- **Claim makinesi ise SAĞLAM (yeniden kullanılacak).** `TryAssignNext` öncelik/refusal/worksite
  eşleşmesi (`JobAssignmentSystem.cs:32-143`), ölü-claimant süpürmesi
  (`DefaultTickSystems.cs:149-166`), hayalet-iptal ağı (`:196-240`), `JobKind.Farmer` atlaması
  (`:206`). W33'ün kanıtladığı gibi: claim SAĞLAM, claim-SONRASI yanlış bantta.
- **W34'ün gireceği dikişler hazır.** `living.decision@PerTick:18` →
  `living.schedule@PerTick:20` (action'lı aktöre dokunmaz, `ScheduleSystem.cs:50-52`) →
  `living.action_advance@PerTick:22`; iş-saati kapısı decide'da (`ActionLifecycleSystem.cs:79`,
  `IsWorkHour` = 06-20, `ScheduleSystem.cs:18-21,95-98`); claim'li aktörün farm dalı
  `ActionLifecycleSystem.cs:80-83` — farm-dışı kind'lar bugün orada SESSİZCE düşüyor
  (`TryDecidePlant :213` `Kind != Farmer → return`). Delik tam WorkIntent'in gireceği yerde.

---

## 2. Hedef akış

```text
worldgen/econ: smelt 1001 job POSTALANIR (JobKind.Smith; Worldgen.Production.cs:64-77 forge tohumu)
Hourly:10  econ.jobs: ölü-claimant süpürme → TryAssignNext → idle demirci CLAIM (Assigned yazılır)
           — adım BAŞKA HİÇBİR ŞEY YAPMAZ (§8: order başlatmaz, order İLERLETMEZ)
PerTick:18 decide: claim'li + actionsız + iş saati + worksite aktif + (order satırı VAR ya da
           site pile 1 execution'ı fonluyor) → WorkIntent + MoveToWorksite (rezervasyonsuz, §3)
PerTick:20 schedule: action'lı aktöre dokunmaz (mevcut kapı)
PerTick:22 advance: MoveToWorksite tezgâha tek-tek yürür → varış (Chebyshev ≤ 1) → PerformWork
           → progress==0 step'i: girdiler site pile'ından ANCAK ŞİMDİ düşer (tezgâh başında
             tüketim; W33'ün commit-anında-tüket dersi başlangıca uygulanır)
           → her step: claim + worksite + saat + mesafe doğrula → order sayacı +1
             (sayaç YALNIZCA burada oynar — emek beden gerektirir)
           → execution biter: çıktı StockpileRecipeInventory ile SİTE pile'ına, RecipeCompleted
             (gerçek boundary stamp — fosil yeni şeritte ölür)
           → quantity biter: Jobs.Complete + JobCompleted (gramer VERBATIM) + schedule Idle
             + Succeeded → NextLink None → aktör serbest
kesinti (pursuit / paydos / itilme / pile kuruması): ACTION ölür, ORDER SATIRI KALIR
           (progress durur ama KAYBOLMAZ), claim canlı aktörde kalır → sonraki decide zinciri
           yeniden kurar → PerformWork KALDIĞI sayaçtan devam eder
```

"Pause" ayrı bir durum DEĞİLDİR: üç kalıcı gerçeğin (claim JobBoard'da + order satırı
WorldState'te + `ActionState == Idle`) bileşkesidir. Yeni faz, yeni alan, yeni enum gerekmez —
W33'ün "zincir başarısızlığı claim'i canlı aktörde bırakır, yeniden dener" kuralının order'lı
genellemesi.

---

## 3. Karar 1 — WORK zinciri action şeridine girer; kilit CLAIM'dir, rezervasyon YOK

**Ayırıcı:** `request.Kind != JobKind.Farmer` olan claim'li işler (bugün `Smith = 1`,
`Baker = 2`; `JobKind.cs:13-25`). Farm dalları (`TryDecidePlant`/`TryDecideHarvest`) aynen
kalır; decide'ın `!ScheduleState.IsIdle` kolu kind'a göre YÖNLENDİRİR (§6).

**Rezervasyon satırı açılmaz; `ReservationId.Empty` taşınır.** Gerekçe üçlü:

1. Münhasırlık zaten claim'de: JobBoard aktör başına 1 claim, job başına 1 claimant — W33
   doc02 §5 hasat dalının aynı argümanı ("job claim'i zaten kilidin kendisi").
2. `ReservationLedger` YAPISAL olarak aktör başına 1 satırdır (`ReservationLedger.cs:37`);
   smelt 2 girdi tag'i ister (`iron_ore` + `fuel`, `ProductionRecipeRegistry.cs:27`) — iki tag
   tek satıra sığmaz. Ledger şemasını büyütmek bu dilimin işi değil (§13.3).
3. Girdi çekişmesi tüketim-anında dürüstçe çözülür: tezgâha varınca pile kurumuşsa
   `Fail(SourceDrained)` → claim kalır → yeniden dener (MoveToPlot'un tohum-drain deseni,
   `MoveToPlotAdvancer.cs:48-56`).

`ActionAdvancer.Fail`'in rezervasyon-iade kolları (`ActionAdvancer.cs:65-90`) WORK zincirine
DOKUNMAZ: satır yok, `CarriedUnits` hep 0 — failure yolları bedava temiz.

**Neden recipe şeridine konum yaması değil:** W33 doc02 §3'ün üç gerekçesi aynen geçerli;
W34 dördüncüyü ekler: uzak şeridin batch devamı mid-drain'de **exception atar**
(`JobAssignmentSystem.Tick.cs:119,222` — "Cannot start next execution") çünkü uzaktan-ilerleyen
bir sayacın "bekle" diyebileceği bir beden yoktur. Action şeridinde aynı durum `SourceDrained`
duraklamasıdır: kervan cevher getirince iş KALDIĞI yerden sürer (§7.2). Işınlı emekte imkânsız
olan hikâye, bedenli emekte kendiliğinden doğar.

---

## 4. Karar 2 — Enum genişlemesi (append-only; all-zero = Idle; YENİ ALAN YOK)

`Assets/Scripts/Domain/Actors/ActorActionState.cs`:

```csharp
public enum ActorIntent      { None = 0, Eat = 1, Plant = 2, Harvest = 3, Work = 4 }          // append
public enum ActorActionType  { ..., HaulCrop = 7, MoveToWorksite = 8, PerformWork = 9 }        // append
public enum ActionFailureReason { ..., CropGone = 8, JobLost = 9, WorksiteGone = 10 }          // append
```

`Domain/Actors/Actions/ActionLogEntry.cs` (W33'ün "iki farklı cümle asla katlanmaz" ilkesi,
`:20-23`): `ActionLogReason`'a `JobLost = 12`, `WorksiteGone = 13` eklenir — "işim iptal oldu"
ile "ocak söndü" ayrı hikâye hammaddesidir.

- **ActorActionState'e yeni ALAN yok.** Site `TargetSiteId`'ye biner; tezgâh hücresi
  `actor.ScheduleState.TargetWorksitePosition`'dan türetilir (claim yazdı, save zaten taşıyor —
  `ActorScheduleState.cs:43-52`); `TargetItemId` Empty, `ReservationId` Empty (§3),
  `CarriedUnits` 0 kalır (TryRestore'un Harvest/Haul-dışı `carriedUnits > 0` reddi `:228`
  DEĞİŞMEZ — PerformWork eli boş çalışır). `default(ActorActionState) == Idle` ve
  all-zero-extends-Idle kendiliğinden korunur; W32-öncesi, W32 ve W33 save'leri değişmeden yüklenir.
- **Sınır güncellemeleri (hepsi aynı PR, biri unutulursa test kırmızı):**
  1. `TryRestore` (`:204-234`): `action > HaulCrop` → `> PerformWork`; `intent > Harvest` →
     `> Work`; `failureReason > CropGone` → `> WorksiteGone`.
  2. `ActionAdvancerRegistry.cs:129`: dizi sınırı `(int)ActorActionType.HaulCrop + 1` →
     `(int)ActorActionType.PerformWork + 1`.
  3. `ActionAdvancer.ToLogReason` (`:92-102`): `JobLost → ActionLogReason.JobLost`,
     `WorksiteGone → ActionLogReason.WorksiteGone` (1:1; katlama yok).
  4. `ActionVerbTable.cs` (`Verb :149-161`, `KindName :164-174`): `MoveToWorksite → "to work"`,
     `PerformWork → "working"` + iki KindName satırı. Projeksiyon verbatim okumaya devam eder —
     tezgâh fiili İLK KEZ tahmin değil gerçek olur (teşhis §2.9'un son kalesi düşer).

Zincir türetimi (`ActionLifecycleSystem.NextLink :127-136`, save edilmez — W32-01 §8):

```csharp
(ActorIntent.Work, ActorActionType.MoveToWorksite) => ActorActionType.PerformWork,
(ActorIntent.Work, ActorActionType.PerformWork)    => ActorActionType.None,
```

---

## 5. Karar 3 — Sayaç nerede yaşıyor: ŞİMDİ vs SONRA (dilimin kalbi)

### 5.1 Şimdi: free-running, sistem-örneği, save'i kopuk

`RecipeWorkOrder.ProgressTicks` (`RecipeSystem.cs:296`) →
`JobAssignmentSystem._activeOrders` private sözlüğünde (`JobAssignmentSystem.cs:23`) →
`econ.jobs@Hourly:10` her koşusunda koşulsuz +1 (`Tick.cs:86-90/:189-193` üzerinden
`DefaultTickSystems.cs:242-248`). Aktör pozisyonu okunmaz; save yolu tek yönlü kopuk (§1) —
yükleme çift-tüketim yarasıyla sonuçlanır. Batch sayacı `_completedExecutionCounts` ikinci
private sözlük; DTO'da jobId dahi yok, yani rebind KAĞIT ÜZERİNDE de imkânsız.

### 5.2 Sonra: order = tezgâhtaki İŞ PARÇASI, WorldState'te; sayacı oynatma HAKKI = beden

Sayaç `ProgressTicks` olarak KALIR ama evi değişir — yeni saf-Domain store:

```csharp
// Assets/Scripts/Domain/Process/WorkOrderLedger.cs — ReservationLedger'ın aynası:
// Rows saved truth (ekleme sırası = save sırası), jobId indexi DERIVED, RebuildIndexes load'da.
public sealed class WorkOrderRecord
{
    public ulong JobId;                // rebind anahtarı (DTO'ya EKLENİR — bugünkü eksik)
    public ulong RecipeId;
    public ulong SiteId;
    public int PositionX, PositionY;   // tezgâh hücresi
    public ulong StartedByActorId;     // atıf; devralan değiştirmez (§13.4)
    public int ProgressTicks;          // SAYAÇ — tek yazar living.action_advance@PerTick:22
    public int CompletedExecutions;    // _completedExecutionCounts'un göçtüğü yer
}
public sealed class WorkOrderLedger   // WorldState.WorkOrders; EnsureInvariants → RebuildIndexes
{ Rows, TryGetByJob(jobId), Add, Remove, RebuildIndexes }
```

- **Neden Domain store (üç zorunluluk):** (a) "pause" order'ın action'dan VE claimant'tan uzun
  yaşamasını ister — aktör-üstü kalıcı durum WorldState'in işidir; (b) sistem-örneği sözlüğü
  rehydrate EDİLEMİYOR (kanıt §1) — mapper'ın doğal yazacağı yer world store'dur (Jobs/Worksites
  emsali, `JsonSliceSaveService.cs:130-135`); (c) `FieldOwnershipRegistry` satırı ancak WorldState
  alanına yazılabilir — tek-yazar beyanı sözlüğe değil store'a yapılır. `RecipeWorkOrder`
  Simulation tipidir (`RecipeSystem.cs:267`), WorldState'e KONAMAZ (Domain→Simulation bağımlılık
  ihlali — `WorldSaveMapper.cs:19` notunun sebebi); saf veri satırı bu yüzden şart.
- **Fonlama invariantı (madde korunumunun anahtarı):** `ProgressTicks == 0` ⇔ mevcut
  execution'ın girdileri HENÜZ tüketilmedi; `> 0` ⇔ tüketildi. Girdi düşüşü ve ilk sayaç vuruşu
  AYNI PerformWork step'inde yaşar (§7.2) — satır hiçbir zaman "girdiyi yedi mi yemedi mi"
  belirsizliğinde saklanamaz. Orphan süpürmesi (§6.3) iade kararını bu invarianttan okur.
- **`RecipeSystem`'e satır overload'ları (+~35 LOC):**
  `TryFund(recipe, io)` — `:88-94`'ün CountOf-sonra-TryConsume-yoksa-throw gramerini satır için
  yeniden kullanır; `Tick(WorkOrderRecord row, RecipeDef recipe, IRecipeInventory io,
  WorldEventLog log, GameTime stamp)` — `:105-144` gövdesinin aynısı ama (a) Domain satırında,
  (b) `RecipeCompleted` GERÇEK boundary stamp ile (fosil `:131` yalnız yeni şeritte ölür; eski
  overload'lar ve goldenları DEĞİŞMEZ).
- **Save diffi (append-only):** `RecipeWorkOrderSaveData`'ya `jobId` + `completedExecutions`
  eklenir (JsonUtility eksik alanı 0 okur). `WorldSaveMapper.ToData/ToWorld` alanı
  `world.WorkOrders`'tan doldurur/geri kurar; `jobId == 0` satırlar yüklemede DÜŞÜRÜLÜR — eski
  save'lerin fiilî davranışı (hiç geri yüklenmiyorlardı) statüko olarak korunur, regresyon yok.
  `JsonSliceSaveService`'in park listesi (`_recipeWorkOrders`) ve `ReplaceRecipeWorkOrders`
  emekliye ayrılır. Çift-tüketim yarası YAPISAL kapanır: yüklenen dünya order satırını taşır,
  `ProgressTicks > 0` ⇒ decide fonlama istemez, PerformWork kaldığı vuruştan sürer.
- `ActorActionState.ProgressTicks` action'ın KENDİ adım sayacı olarak kalır (log/UI grameri
  uniform) — recipe gerçeği satırdadır; kesintide action sayacı sıfırlanır, satırınki KALIR.
  İki sayacın ayrılığı tam da "pause" semantiğinin kendisidir.

---

## 6. Karar 4 — Decide fazı: WORK kuralı (EAT ve farm kapılarının ARKASINA)

`ActionLifecycleSystem.Decide` mevcut kapı sırası korunur (`:52-62` canlılık/rol/action/pursuit,
`:62-74` eat önceliği, `:77-79` katalog-guard-iş saati). Tek yapısal değişiklik `:80-83`:

```csharp
if (!actor.ScheduleState.IsIdle)             // claim'li aktör: kind yönlendirir
{
    if (JobKindOf(world, actor) == JobKind.Farmer) TryDecidePlant(world, actor, stamp);
    else TryDecideWork(world, actor, stamp); // W34: farm-dışı claim BEDENLENİR
}
else TryDecideHarvest(world, actor, stamp);
```

`TryDecideWork` kapıları (ucuzdan pahalıya; TryDecidePlant `:208-233` deseni):

```csharp
// 1) world.Jobs.TryGet(jobId) && GetClaimedBy == actor.Id değilse → geç (süpürme/iptal yarışı)
// 2) req.Kind == JobKind.Farmer → geç (savunma; yönlendirme zaten ayırdı)
// 3) _resolveRecipe(req.RecipeId) null → geç (bilinmeyen id econ.jobs hayalet ağının işi, §8)
// 4) worksites.TryGet(req.SiteId, req.WorksitePosition) + IsActive + Kind == req.WorksiteKind
//    değilse → geç (job claim'li BEKLER — tezgâhsız site donmaz, tohumsuz-site emsali)
// 5) world.WorkOrders.TryGetByJob(jobId, out var row) → DEVAM zinciri: fonlama SORULMAZ
//    (girdi ya satırda gömülü ya progress==0 ile tezgâhta sorulacak)
// 6) satır yoksa: 1 execution fonlaması — her input için io.CountOf(tag) >= qty (klonsuz,
//    salt okuma); eksikse → geç (job claim'li bekler; kervan getirince zincir başlar)
var start = ActorActionState.ForIntent(ActorIntent.Work).Start(
    ActorActionType.MoveToWorksite, req.SiteId, ItemId.Empty,
    ReservationId.Empty, stamp.TotalMinutes, ActionInterruptPolicy.Interruptible);
_registry.For(ActorActionType.MoveToWorksite).TransitionTo(world, actor, start,
    ActionLogReason.TargetSelected, stamp);   // rezervasyon yok → ReservationAcquired YANLIŞ olur
```

- **Recipe çözücü enjeksiyonu:** `ActionLifecycleSystem` ctor'una `Func<RecipeId, RecipeDef>`
  eklenir (composer `ProductionRecipeRegistry.Resolve`'ün try/catch sargısını verir —
  `DomainSimulationAdapter.cs:65` delege emsali). Null çözücü WORK kurallarını kapatır (çıplak
  EAT/FARM test dünyaları) — `_plantSpecies` null sözleşmesinin aynası (`:20-22`).
- **İş saati:** `:79`'daki `IsWorkHour` kapısı WORK kuralını da örter — gece karar doğmaz;
  schedule claim'li aktörü eve yürütür, order satırı bekler. Paydos ORTASI kesinti advancer'da (§7).
- **Açlık > iş:** eat kuralı önce koşar (`:62-73`); 55 eşiğini aşan demirci önce yemek zinciri
  yaşar, doyunca (action biter, claim durur) ertesi tick işine döner. Claim-anı `IsRefusing`
  açlık-80 kapısı (`JobAssignmentSystem.cs:487-504`) değişmez.
- **6.3 Orphan order süpürmesi (decide başı, `:46` rezervasyon süpürmesinin yanına):**
  `world.WorkOrders.Rows` içinde `!world.Jobs.Contains(row.JobId)` satırlar (iptal edilmiş /
  dışarıdan silinmiş job): `ProgressTicks > 0` ise mevcut execution'ın girdileri site pile'ına
  İADE edilir (recipe çözülür, `recipe.Inputs` geri `Add` — madde korunumu; `Fail`'in
  ConsumeFood-iadesi sınıfı), satır düşer, `ChronicleEvent "work_order_refunded"` yazılır.
  Çözülemeyen id iadesiz düşer (pratikte ulaşılamaz: satır ancak çözülür id ile doğar).
  Tamamlanan job satırı zaten commit step'inde silmiştir (§7.2) — süpürme istisnai yol.

---

## 7. Karar 5 — Advancer'lar (Template Method'a iki yeni strateji)

İkisi de `ActionAdvancer` tabanından türer: pursuit interruption probu, `TransitionTo` dikişi,
`Fail` kapısı bedava. Ortak doğrulama merdiveni yeni `WorkOperations` (FoodOperations /
FarmOperations kardeşi, `internal static`): `WorkReachCells = 1`, `ClaimOf(world, actor)`
(job + claimed-by-me doğrulaması), `WorksiteOf(world, req)` (TryGet + IsActive + Kind),
`SiteIo(world, siteId)` (= `FarmOperations.FindOrCreatePile` + `StockpileRecipeInventory`),
`CompleteJob(world, actor, stamp)` — `PlantSeedAdvancer.CompleteJob`'un (`:185-205`) buraya
KALDIRILMIŞ hali; PlantSeed delege eder, JobCompleted grameri tek evde kalır
(`Tick.cs:127-142` VERBATIM — chronicle/proof/quest tüketicileri kırılmaz;
`QuestCatalog.cs:29` `recipe_completed:1001` cause pinini yalnız ZAMANLAMA kaydırır).

### 7.1 `MoveToWorksiteAdvancer` (`Handles => MoveToWorksite`)

MoveToPlot iskeleti (`MoveToPlotAdvancer.cs:29-77`), hedef tezgâh:

```csharp
// 1) ClaimOf değilse → Fail(JobLost)          (claim süpürüldü / job iptal / başkasına geçti)
// 2) WorksiteOf değilse → Fail(WorksiteGone)  (ocak söndü / kayıt silindi)
// 3) !IsWorkHour(stamp) → Fail(Interrupted)   (paydos yürüyüşü yarıda keser; satır zaten yok
//                                              ya da bekliyor — sabah decide yeniden kurar)
// 4) hedef = ScheduleState.TargetWorksitePosition; Chebyshev > WorkReachCells ise
//    MoveTo(StepToward(...)) — living.action_advance zaten Actor.Position yazarı
// 5) Chebyshev <= WorkReachCells → Succeeded (Arrived); değilse Advanced (ProgressTicked)
```

Varış "hücrenin üstü" değil "erişim" (≤ 1): tezgâh hücresi dolu bir demirbaştır;
PlantSeed'in `Chebyshev ≤ 1` çalışma kapısı (`PlantSeedAdvancer.cs:145`) emsaldir.

### 7.2 `PerformWorkAdvancer` (`Handles => PerformWork`) — sayacın TEK oynatıcısı

```csharp
// 1-3) MoveToWorksite'ın aynı üç kapısı (JobLost / WorksiteGone / Interrupted-paydos)
// 4) Chebyshev(actor, tezgâh) > WorkReachCells → Fail(Unreachable)   (itilme = pause)
var io = WorkOperations.SiteIo(world, req.SiteId);
var row = world.WorkOrders.TryGetByJob(jobId) ?? world.WorkOrders.Add(NewRow(req, actor));
if (row.ProgressTicks == 0)                       // fonlanmamış execution (invariant §5.2)
{
    if (!RecipeSystem.TryFund(recipe, io))        // CountOf hepsi ≥ → TryConsume hepsi
    { Fail(world, actor, ActionFailureReason.SourceDrained, stamp); return; }
    // girdiler TEZGÂH BAŞINDA düştü — teşhis §5 "bedel": madde, beden oradayken hareket eder
}
if (_recipes.Tick(row, recipe, io, world.Events, stamp))   // sayaç +1; süre dolduysa commit:
{                                                          // çıktılar io.TryAccept → SİTE pile
    row.CompletedExecutions++; row.ProgressTicks = 0;      // sonraki execution fonsuz başlar
    if (row.CompletedExecutions >= req.Quantity)
    {
        world.WorkOrders.Remove(jobId);                    // iş parçası tezgâhtan kalkar
        WorkOperations.CompleteJob(world, actor, stamp);   // Complete + JobCompleted + Idle
        TransitionTo(world, actor, state.Advanced().Succeeded(), ActionLogReason.Completed, stamp);
        return;
    }
}
TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
```

- **"İş ancak eylem zinciri biterse biter"** (W33'ün dilim cümlesi): `Jobs.Complete` YALNIZCA
  bu commit'te yaşar. `job_dropped` hayaleti 1001/1002 için ateşlenemez (id'ler kayıtlı);
  gerçekten kayıtsız id'ler için ağ §8'de kalır.
- **Pause yolları tek tek:** pursuit → taban probu `Fail(Interrupted)`; paydos → kapı 3;
  itilme → kapı 4 `Unreachable`; pile kuruması → `SourceDrained`; job iptali → `JobLost`;
  ocak sönmesi → `WorksiteGone`. HEPSİNDE satır (varsa progress'iyle) kalır, claim canlı
  aktörde kalır (JobLost hariç — orada zaten job gitti, satırı §6.3 süpürür/iade eder).
  Ölü claimant'ı `econ.jobs` süpürmesi (§8) pending'e çevirir; İKİNCİ demirci decide'da satırı
  bulur (§6 kapı 5) ve KALDIĞI vuruştan sürdürür — "yarım külçe tezgâhta bekler" hikâyesi.
- **Batch drain = duraklama, exception değil:** kalan execution fonlanamazsa `SourceDrained`
  ile durur, `CompletedExecutions` korunur; uzak şeridin `Tick.cs:119` "Cannot start next
  execution" ölümü bu şeritte tanımsız hâle gelir (§3 gerekçe 4).
- **Süre anlamı kayar (bilinçli):** `DurationTicks: 2/3` (`ProductionRecipeRegistry.cs:26,37`)
  bugün 2-3 SAAT (Hourly bant); yarın 2-3 tezgâh VURUŞU (PerTick, 1 tick = 1 dakika) + yürüyüş.
  Emek beden-vuruşu sayar; pacing istenirse data satırı retune edilir (§13.2). Goldenlarda
  RecipeCompleted/JobCompleted zamanları kayar: re-baseline BEKLENEN; chunking hakemi
  değişmeden yeşil kalmak ZORUNDA.

---

## 8. Karar 6 — `JobAssignmentStep` diyeti: adım artık HİÇBİR ŞEYİ ilerletmez

`DefaultTickSystems.JobAssignmentStep.Run` (`:143-248`) sonrası görev listesi:

| Görev | Satır (bugün) | W34 sonrası |
|---|---|---|
| Ölü-claimant süpürmesi | `:149-166` | **KALIR** (W33 davranışı aynen; pending'e dönen job'ın order satırı bekler — devralma §7.2) |
| `TryAssignNext` claim döngüsü + JobAssigned event | `:168-190` | **KALIR** (claim makinesi dilimin dokunmadığı çekirdek) |
| `JobKind.Farmer` atlaması | `:206` | **KALIR** (farm action şeridinde) |
| Hayalet-iptal ağı (çözülemeyen recipe id → `job_dropped` + Cancel) | `:196-226, 238-240` | **KALIR** — bilinmeyen id'li CLAIM'Lİ job claimant'ı sonsuza dek dondurur; ağ decide kapı 3'ün (§6) tamamlayıcısıdır. Düşen job'ın olası order satırını §6.3 iade-süpürmesi toplar |
| `StartRecipeForClaim` çağrısı | `:228-236` | **SİLİNİR** — order doğumu ve girdi tüketimi PerformWork'ün progress==0 step'ine taşındı (§7.2) |
| `TickAssignedJobs` çağrısı | `:242-248` | **SİLİNİR** — üretimdeki TEK çağıran buydu; free-running sayaç ölür |
| `SiteRecipeInventory` yardımcı | `:253-259` | **SİLİNİR** — `WorkOperations.SiteIo` tek ev (FarmOperations.FindOrCreatePile'ı zaten paylaşır) |

Metodların kaderi: `TickAssignedJobs` / `StartRecipeForClaim` / `_activeOrders` /
`_completedExecutionCounts` ve InventoryState overload'ları **üretim şeridinden emekli, test
sözleşmesi olarak kalır** (W33 emsali: `JobEventLogTests` / `JobAssignmentSystemTests`
dokunulmaz, doc02 §8 tablosu). Ayrı bir temizlik PR'ında gövdeleri sökülür (davranış değişimiyle
silme karışmaz — W33 §12.5 ilkesi). `world.PlayerInventory` yalnız oyuncu-craft şeridinde
(`DomainSimulationAdapter.Crafting.cs`) — W33'ün bıraktığı yerde, dokunulmaz.

---

## 9. Registry / sahiplik diffleri (hepsi aynı PR; lint testi hakem)

1. **`FieldOwnershipRegistry.cs`** (`:54-61` Stockpiles bloğu):
   - `World.Stockpiles`: `"econ.jobs@Hourly:10"` satırı **EMEKLİ** (adım artık pile'a dokunmaz);
     `living.action_advance@PerTick:22` yorumu genişler: "W34 PerformWork girdi tüketimi +
     çıktı basımı".
   - **YENİ** `["World.WorkOrders"]`: `living.decision@PerTick:18` (orphan süpürme + iade,
     §6.3), `living.action_advance@PerTick:22` (doğum/fonlama/sayaç/silme, §7.2) —
     `World.Reservations`'ın iki-slot beyanının aynası (`:38-42`).
   - **YENİ** `["World.Jobs"]` (mevcut çok-yazarlığın nihayet BEYANI): `econ.jobs@Hourly:10`
     (claim/sweep/ghost-cancel), `living.action_advance@PerTick:22` (Complete/Cancel — W33
     PlantSeed'den beri fiilî yazar), `world.shortage@Daily:27` (post). Teşhis §2.3'ün
     "görünür kıl" ilkesi.
2. **`ActionAdvancerRegistry`**: dizi sınırı §4; `ActionLifecycleSystem` ctor'una iki kayıt
   (`MoveToWorksiteAdvancer(log)`, `PerformWorkAdvancer(log, resolveRecipe)`) + çözücü parametresi.
3. **`DefaultTickRegistry`/`DefaultTickSystems.Create`**: YENİ ADIM YOK — decide@18 /
   schedule@20 / advance@22 / econ.jobs@Hourly:10 slotları değişmez; composer yalnız
   `ActionLifecycleSystem`'e çözücü delegesi geçirir (`:46-49` ctor bloğu). W34 diffi organ
   değil, perhizdir.
4. **`WorldState`**: `public WorkOrderLedger WorkOrders = new WorkOrderLedger();` +
   `EnsureInvariants`'ta `RebuildIndexes` (`:96-98` Reservations emsali) + `CopyFrom` satırı.
5. **`WorldStateDigest`**: `AppendWorkOrders` bölümü (`:50/:467` AppendReservations emsali) —
   chunking hakemi order sayacını İLK KEZ görür; sayaç kaçağı bayt farkı olarak yakalanır.
6. **Save**: DTO append + mapper göçü + park listesi emekliliği (§5.2).

---

## 10. Tick sırası — bir vardiyanın zaman çizelgesi

```text
saat s sınırı  : econ.jobs@Hourly:10 — süpürme + claim (Assigned yazılır); order'a DOKUNMAZ
tick T         : decide@18 WorkIntent + MoveToWorksite → schedule@20 atlar → advance@22 ilk adım
                 AYNI tick (W32-03 §4 kuralı: handover'lı link ilk vuruşunu aynı tick alır)
tick T+walk    : varış (≤1) → Succeeded(Arrived) → ertesi tick PerformWork başlar
tick T+walk+1  : progress==0 → TryFund (girdiler pile'dan düşer) + sayaç 1
tick T+walk+2  : sayaç 2 == DurationTicks(1001) → commit: külçe pile'a, RecipeCompleted(stamp)
                 → quantity==1 → Complete + JobCompleted + Idle + Succeeded
gece 20:00     : (uzun batch'te) Interrupted — satır progress'iyle DONAR, aktör eve yürür
sabah 06:00+   : decide satırı bulur (kapı 5) → walk → kaldığı vuruştan devam
```

Sıra sabitleri değişmez: decide(18) < schedule(20) → karar alan aktörü router AYNI tick atlar;
advance(22) Needs/Stockpiles yazım noktasını korur. Hourly bandın PerTick bandıyla interleave'i
(`WorldTickComposer` kadans sözleşmesi) aynen; chunked replay tick-tek-tek aynı sırayı oynar.

---

## 11. Test planı (`Assets/Tests/EditMode/Actions/` — W32-06/W33-04 hikâye deseni)

- **W1 — çember kapanır:** worldgen forge job (1001) → claim → yürüyüş (pozisyon adım adım
  pinlenir) → varışta fonlama: `iron_ore -2, fuel -1` ANCAK varış tick'inde (claim tick'inde
  pile BAYT-EŞ) → 2 vuruş → `iron_ingot +1` SİTE pile'ında; `Jobs.Contains == false` yalnız
  commit tick'inde; JobCompleted + RecipeCompleted (gerçek stamp) event'leri; UI etiketi
  `"working"` == `CurrentAction` (teşhis §10).
- **W2 — uzaktan emek öldü:** claim'li demirci uzakta TUTULUR → saatler geçer →
  `WorkOrders` satırı ya yok ya `ProgressTicks` sabit; pile değişmez. (Eski dünyada iki saatte
  külçe "oluverirdi" — dilimin idam kanıtı.)
- **W3 — pause/resume, madde sabit:** PerformWork ortasında pursuit interrupt → action Idle,
  satır progress'iyle duruyor, pile'a İKİNCİ dokunuş yok, claim duruyor → tehdit geçer →
  zincir yeniden kurulur → toplam girdi tüketimi TAM 1 execution'lık; çıktı 1.
- **W4 — paydos:** 19:5x'te başlayan iş 20:00'de `Interrupted` → aktör eve, satır donuk →
  06:00 sonrası devam → çıktı doğru. Gece boyunca sayaç bayt-sabit.
- **W5 — ölü demirci, devralan çırak:** zincir ortası ölüm → econ.jobs süpürmesi claim'i
  bırakır → ikinci Smith-tercihli aktör claim eder → decide satırı bulur, fonlama SORMAZ →
  kaldığı vuruştan bitirir; girdiler bir kez tüketilmiş, çıktı 1 (B05-hortlaması yok).
- **W6 — cevhersiz site donmaz:** boş pile → decide kapı 6 geçmez, job claim'li bekler,
  hayalet-iptal ATEŞLENMEZ (id kayıtlı) → pile'a cevher eklenir → zincir başlar. Batch
  varyantı: 2. execution öncesi drain → `SourceDrained` duraklaması → restokta devam.
- **W7 — hakem:** work-zincirli marathon chunked vs tick-tek-tek BAYT-EŞ
  (`ActionPhaseChunkingInvarianceTests`'e work senaryosu; digest artık WorkOrders içerir §9.5).
- **W8 — save ortası tezgâh:** PerformWork Running + satır progress k'de kaydet → yükle →
  fonlama tekrarı YOK (çift-tüketim yarası kapandı — pile bayt-eş), aynı tick sayısında commit
  (golden). Legacy save (jobId'siz DTO satırları) yüklemede düşer, dünya yine de yüklenir.
- **W9 — sahiplik linti:** FieldOwnershipRegistry yeni satırları ↔ tick registry yazarları
  tutarlı; `econ.jobs`'un Stockpiles yazarlığının kalktığı pinlenir.

---

## 12. Dosya manifesti + LOC bütçesi

| Dosya | İş | ~LOC |
|---|---|---|
| `Domain/Actors/ActorActionState.cs` | 4 enum üyesi + TryRestore sınırları | +8 |
| `Domain/Actors/Actions/ActionLogEntry.cs` | 2 log reason | +3 |
| `Domain/Process/WorkOrderLedger.cs` | YENİ store + record | 80 |
| `Domain/World/WorldState.cs` | alan + EnsureInvariants + CopyFrom | +6 |
| `Simulation/Living/Actions/WorkOperations.cs` | YENİ ortak merdiven + CompleteJob evi | 70 |
| `Simulation/Living/Actions/MoveToWorksiteAdvancer.cs` | YENİ | 55 |
| `Simulation/Living/Actions/PerformWorkAdvancer.cs` | YENİ | 85 |
| `Simulation/Living/Actions/PlantSeedAdvancer.cs` | CompleteJob → WorkOperations delegesi | −15 |
| `Simulation/Living/Actions/ActionLifecycleSystem.cs` | TryDecideWork + yönlendirme + NextLink + ctor çözücü + orphan süpürme | +75 |
| `Simulation/Living/Actions/ActionAdvancer.cs` | ToLogReason 2 satır | +2 |
| `Simulation/Living/Actions/ActionAdvancerRegistry.cs` | dizi sınırı | ±1 |
| `Simulation/Process/RecipeSystem.cs` | TryFund + satır-Tick (gerçek stamp) | +35 |
| `Simulation/Composition/DefaultTickSystems.cs` | Start/Tick çağrıları −, çözücü geçişi | net −25 |
| `Simulation/Composition/FieldOwnershipRegistry.cs` | 2 yeni satır + 1 emekli | +10 |
| `Simulation/Composition/WorldStateDigest.cs` | AppendWorkOrders | +20 |
| `Data/Save/WorldSaveData.WorldProcess.cs` | DTO 2 alan | +2 |
| `Data/Save/SliceJson/WorldSaveMapper(.Process).cs` | WorkOrders map | +30 |
| `Presentation/Ember/Save/JsonSliceSaveService.cs` | park listesi emekli | −20 |
| `Presentation/.../ActionVerbTable.cs` | 2 fiil + 2 kind | +6 |
| Testler (W1-W9 + pin göçleri) | | ~420 |

Üretim kodu net ~+430 — dilim bütçesinde; en büyük kalemler ledger (save yarasının bedeli) ve
iki advancer (dilimin kendisi).

---

## 13. Bilinçli sınırlar / ileriye bırakılanlar

1. **Tek-execution fonlaması:** decide/tezgâh kapıları 1 execution sorar; `Quantity`'nin tamamı
   için ön-prova (`CanStartRequestedQuantity`'nin klon provası) istenmez — batch drain artık
   exception değil duraklama olduğundan prova zorunluluğu ortadan kalktı. Pacing sorun olursa
   decide kapısı kalan-quantity'ye sıkılaştırılır (tek satır).
2. **`DurationTicks` anlam kayması:** 2 saat → 2 dakika-vuruşu. Değerler data satırıdır
   (`ProductionRecipeRegistry.cs:26,37`); oyun hissi isterse retune ayrı, davranışsız bir PR.
3. **Girdi rezervasyonu yok:** ledger aktör başına 1 satır; çok-tag'li iş rezervasyonu şema
   işi. "grain" tag'i bugün hiçbir EAT/kıtlık tüketicisiyle çakışmıyor ("wheat" şizmi, W33
   §12.3) — çakışma doğduğunda bu sınır yeniden açılır.
4. **`StartedByActorId` atıftır:** devralan çırak satırın atıf alanını değiştirmez;
   RecipeCompleted aktörü işi BİTİREN olur (advancer stamp'i aktörden geçer). Adlandırılmış
   kozmetik borç.
5. **Worksite kapasitesi yok:** aynı tezgâha iki AYRI job basılırsa iki aktör aynı hücre
   komşuluğunda çalışabilir. Job-başına-tezgâh münhasırlığı (worksite claim'i) ayrı dilim.
6. **Recipe şeridinin gövdeleri:** `TickAssignedJobs`/`StartRecipeForClaim`/`_activeOrders`
   test sözleşmesi olarak yaşar; söküm ayrı temizlik PR'ı (W33 §12.5 ilkesi).
7. **Fosil stamp yalnız yeni şeritte ölür:** eski io/InventoryState `Tick` overload'larının
   `GameTime(ProgressTicks)` damgası test goldenlarını korumak için dokunulmaz kalır; üretim
   şeridi artık oradan geçmez.

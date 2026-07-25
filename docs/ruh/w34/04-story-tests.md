# W34 / DOC 4 — SLEEP + WORK Dilimi Hikâye Testleri (RUH_TESHIS §8–§10 → Somut EditMode Testleri)

> Kaynak teşhis: `docs/RUH_TESHIS.md` §8 madde 4–5 ("uyku ve iş typed action olur; iş ilerlemesi ancak
> aktör worksiteda ve `PerformWork` aşamasındaysa olur"), §10 ("Uyku toparlanması yalnızca aktif Sleep
> action ve uygun yatakta olur", "Aktör worksite dışında işi ilerletemez", "Input olmadan output oluşmaz"un
> craft yarısı), §2.6 ("iş yapmak ile iş yerine yürümek bağlı değil").
> Kapsam: **YALNIZCA SLEEP + WORK dilim çifti** (dilim 3 + dilim 4). Guard/combat aksiyonları, yatak/mobilya
> konsepti ve crop→meal pişirme bilinçli KAPSAM DIŞI — son bölümde devir listesi var. Desen sahibi W32 EAT
> (`docs/ruh/w32/06-story-tests.md` T1–T8) ve W33 FARM (`docs/ruh/w33/04-story-tests.md` F1–F7): şablonlar
> burada ÜÇÜNCÜ kez görünür ve YENİDEN KULLANILIR, yeniden icat edilmez.

İki yara, iki dilim, tek doküman — çünkü ikisi aynı geceyi paylaşır (S5 kapak taşı ikisini tek zincirde bağlar):

- **Uyurgezer yara:** `NeedConsumptionSystem.cs:29-45` gece saatinde HER canlı sivilin fatigue'ünü düşürür —
  aktör nerede olursa olsun, ne yapıyor olursa olsun. Yürürken uyur, tarlada uyur, kovalanırken uyur.
- **Hayalet işçi yara:** `JobAssignmentSystem.Tick.cs:86-92` (+ tag-count ikizi `:189-196`) `_activeOrders`
  içindeki HER RecipeWorkOrder'ı işçinin POZİSYONUNU HİÇ OKUMADAN ilerletir (`econ.jobs@Hourly:10`,
  `DefaultTickSystems.cs` çağrısı). Demirci eve yürürken ocak kendi kendine demir döker.

---

## 0. Test Sözleşmesi — Bu Testlerin Varsaydığı API Yüzeyi

Adlandırma sahibi kardeş dokümanlardır (durum/enum DOC 01, karar + claim köprüsü DOC 02, ilerletme +
operasyonlar DOC 03). İsim değişirse test metinleri mekanik güncellenir, **iddialar değişmez**.

```csharp
// Domain/Actors — W32/W33 enumları APPEND-ONLY büyür (save'e int yazılır; yeniden numaralama YASAK;
// default(ActorActionState) == Idle "all-zero" kuralı aynen korunur — eski save'ler Idle yüklenir):
ActorIntent      { None=0, Eat=1, Plant=2, Harvest=3, Sleep=4, Work=5 }
ActorActionType  { None=0, MoveToFood=1, TakeFood=2, ConsumeFood=3,
                   MoveToPlot=4, PlantSeed=5, HarvestCrop=6, HaulCrop=7,
                   MoveHome=8, Sleep=9, MoveToWorksite=10, PerformWork=11 }
// Zincirler ActionLifecycleSystem.NextLink kalıbıyla intent'ten türetilen SABİT boru hatlarıdır (save'e yazılmaz):
//   Sleep : MoveHome → Sleep
//   Work  : MoveToWorksite → PerformWork
// ActorActionState.TryRestore üst sınırları yeni uçlara genişler (intent<=Work, action<=PerformWork,
// yeni FailureReason uçları) — eski save'ler etkilenmez (append-only). Yeni saved ALAN YOK: uyku
// ilerlemesi ProgressTicks'te, iş ilerlemesi RecipeWorkOrder.ProgressTicks'te yaşar (ikisi de zaten
// roundtrip'li — WorldSaveRehydration.ToRecipeWorkOrderData).
// ActionFailureReason / ActionLogReason yeni üyeleri (JobGone, WorksiteLost, ...) DOC 01'in malı — append-only.

// Simulation/Living/Actions — FoodOperations/FarmOperations'ın kardeşleri; her operasyon MESAFEYİ
// KENDİSİ doğrular (sistem sırası ne olursa olsun uzaktan uyumak/çalışmak FİZİKSEL reddedilir — W32 T1 kalıbı):
SleepOps.TryRecoverTick(world, actor)    // yalnız home hücresinde (slice kuralı: Chebyshev == 0'a
                                         // yürünür — yatak/bina konsepti sonraki dilimin) ve yalnız
                                         // Sleep/Running fazında fatigue düşürür; oran DOC 03'ün malı
SleepOps.Interrupt(world, actor, reason) // uyandırma kapısı: bankalanan toparlanma GERİ ALINMAZ
WorkOps.TryStartOrder(world, actor)      // claim'li job + reach → RecipeSystem.TryStart (input consume);
                                         // consume-at-start sözleşmesi ve zamanı DOC 03'ün malı
WorkOps.TryWorkTick(world, actor)        // reach doğrular; RecipeSystem.Tick'in TEK CANLI ÇAĞIRANI olur
WorkOps.Interrupt(world, actor, reason)  // düşen iş order'ı korunumlu bırakır (yarım progress SAKLANIR)
// Reach sabitleri (WorkReachCells; EatReachCells=2 emsali) DOC 03'ün malı; testler sembolik okur.

// Composer — YENİ STEP ID YOK: uyku+iş kararı living.decision@PerTick:18 içinde, ilerletme
// living.action_advance@PerTick:22 içinde yaşar (tek-yazar eleştirisi yeni örnek KAZANMAZ).
// econ.jobs@Hourly:10 poster/atayıcı/claim-süpürücü olarak YAŞAR; TickAssignedJobs'un
// pozisyonsuz ilerletme gövdesi ÖLÜR (bkz. S3/S4 + envanter #7/#11).
// living.consumption@Hourly:35 fatigue yazarı olmaktan çıkar — adım tamamen ölür mü, boş mu kalır
// DOC 03'ün kararı; her iki halde "Hourly:35 fatigue düşürür" satırı ölür (envanter #1/#7/#8).

// Olay grameri — WorldEventKind append-only; YENİ kind yok. ActionLogManager.IsChainTerminal
// Sleep ve PerformWork'ü kapsar (intent-başına-terminal-halka genellemesi); RecipeCompleted artık
// yalnız PerformWork commit'inde doğar. DİKKAT: ActionCompleted sayan her pin "hangi zincir?"
// sorusunu sormak ZORUNDA kalır — reason öneki "sleep:/work:" ile ayrışır (envanter #3).
```

Kanıt yüzeyi DEĞİŞMEZ: `Support/ActionTrace.Of` (ActionLog ring + terminal olaylar) ve
`ActionTrace.StateDigest` aynen yeniden kullanılır — render/diagnostik log ASLA kanıt değildir.
Bölüm kimliği W32 dokümanındaki varsayımsal `ActionId` değil, gemideki gerçeğidir:
`(actorId, StartedAtMinutes)` çifti + iz satırları.

Ortak kurulum (EatSliceWorld/FarmSliceWorld kalıbı — story testlerin TEK dünya-kurma yolu):

```csharp
// Assets/Tests/EditMode/Actions/Support/SleepWorkSliceWorld.cs
// Site(1) (0,0)-(10,10); home hücreleri kuşakta, furnace worksite + SmeltIron job
// (JobAssignmentSystemTests'in RecipeFixtureCatalog.SmeltIronIngot deseni) + ore stoklu pile.
static WorldState Build(int oreStock = 4)           // site + worksite + job + pile + EnsureInvariants
static ActorRecord Worker(ulong id, int x, int y)   // tok, dinç; Smith tercihi — gündüz işe gider
static ActorRecord Tired(ulong id, int x, int y)    // fatigue=80, diğer needs rahat: uyku kararı yemekle yarışmaz
static int TotalIron(WorldState w)                  // input + output + order-içi — madde muhasebesi sayacı
```

Bütün testler **EditMode**, saf Domain/Simulation (Unity API yok, IO yok, RNG yalnız seed'li) —
determinizm anayasası + chunking hakemi aynen geçerli.

---

## 1. Hikâye Testleri (S1–S7)

### S1 — Fatigue YALNIZCA home hücresindeki Running Sleep'te düşer: uyurgezerlik ÖLDÜ

**Dosya:** `Assets/Tests/EditMode/Actions/SleepRecoveryAuthorshipTests.cs`

Bugün toparlanma bir battaniyedir: `NeedConsumptionSystem.Tick` gece saatinde canlı her sivilin
fatigue'ünü -40/saat düşürür — pozisyon parametresi İMZADA BİLE YOK (`NeedConsumptionSystem.cs:29-45`).
Yeni hikâye: toparlanmanın TEK ebeveyni, home hücresinde ilerleyen bir Sleep aksiyonunun tick'leridir.

**Kurulum:**
```csharp
var world = SleepWorkSliceWorld.Build();
world.Actors.Add(SleepWorkSliceWorld.Tired(7, 9, 9));   // home (2,2)'den uzak: MoveHome uzun yaşar
var composer = new WorldTickComposer();
var trace = new List<(int tick, int fatigue, ActorActionType action, ActionPhase phase, GridPosition pos)>();
for (int t = 1; t <= 2 * 1440; t++)                     // iki tam gün: en az bir gece bölümü
{ composer.Advance(world, t); trace.Add(Sample(A())); }
```

**Kesin iddia (pozitif — yazarlık taraması):**
```csharp
// Fatigue'ün DÜŞTÜĞÜ her tick'te aktör Sleep/Running fazındaydı VE home hücresindeydi:
foreach (var (prev, cur) in Pairwise(trace).Where(p => p.cur.fatigue < p.prev.fatigue))
{
    Assert.That(cur.action, Is.EqualTo(ActorActionType.Sleep), "toparlanmanın tek ebeveyni Sleep");
    Assert.That(cur.phase, Is.EqualTo(ActionPhase.Running), "terminal/handover tick'i toparlamaz");
    Assert.That(cur.pos, Is.EqualTo(A().Home), "uyku EVDE olur — yürürken uyunmaz");
}
Assert.That(trace.Any(s => s.action == ActorActionType.Sleep), Is.True,
    "vacuous guard: iki günde hiç uyku bölümü yaşanmadıysa test hiçbir şey sınamıyor");
// Gece boyu MoveHome/Running'te geçen tick'lerde fatigue YALNIZ YÜKSELEBİLİR (living.needs):
Assert.That(Pairwise(trace).Where(p => p.cur.action == ActorActionType.MoveHome)
    .All(p => p.cur.fatigue >= p.prev.fatigue), "yürüyen adam uyumaz — uyurgezer battaniye öldü");
```

**Kesin iddia (negatif — saldırgan kurulum, W32 T1 kalıbı):**
```csharp
// Fazı zorla Sleep/Running'e kur, aktörü evden 40 hücre öteye koy:
far.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Sleep).Start(
    ActorActionType.Sleep, SleepWorkSliceWorld.HomeSite, ItemId.Empty,
    ReservationId.Empty, stamp.TotalMinutes, ActionInterruptPolicy.Interruptible));
Assert.That(SleepOps.TryRecoverTick(world, far), Is.False, "uzaktan uyuma REDDEDİLİR");
Assert.That(far.Needs.Fatigue.Value, Is.EqualTo(80), "fatigue kımıldamadı");
```

Toparlanma ORANI bu testin malı değildir (DOC 03 seçer); sürdürülebilirlik bekçisi Gate1'dir:
gece bütçesi gündüz yükselişini (`NeedsSystem.FatigueIncreasePerTick=6`/saat) yenmek zorunda,
yenemiyorsa yanlış olan eşik değil orandır (W32 Gate1 dersi — envanter #3).

---

### S2 — Kovalanan/saldırıya uğrayan uyuyan UYANIR: Interrupted + etiket anında döner

**Dosya:** `Assets/Tests/EditMode/Actions/SleepInterruptionTests.cs`

Bugün gece kovalamacası uyuyana dokunamaz — battaniye herkese iner. Yeni hikâye: tehdit uykudan
büyüktür. Mevcut dikiş YENİDEN KULLANILIR: `ActionAdvancer.Advance` pursuit-quarry probe'u zaten her
Running aksiyonu `Interrupted` ile düşürür (`ActionAdvancer.cs:36-40`, guards-eat W33-C emsali) —
Sleep advancer'ı bu tabandan türediği anda uyanma BEDAVA gelir; test bunu pinler, yeniden kurmaz.

**Kurulum:**
```csharp
var world = SleepWorkSliceWorld.Build();
world.Actors.Add(SleepWorkSliceWorld.Tired(9, 2, 2));            // evinde, uykuya dalacak
var composer = new WorldTickComposer();
AdvanceUntil(composer, world, () => A().ActionState.CurrentAction == ActorActionType.Sleep);
int fatigueAtSleepStart = 80, ranTicks = A().ActionState.ProgressTicks;
// Gece yarısı baskın (GuardEatStoryTests fixture deseni — avcı değil AV taraf):
world.GuardPursuits.Add(new PursuitRecord { GuardId = 7, TargetId = 9, UntilMinutes = 100000 });
composer.Advance(world, NextTick());                             // bir advancement bandı
```

**Kesin iddia:**
```csharp
Assert.That(ActionTrace.Of(world), Does.Contain("Sleep/Running->Sleep/Failed")
    .And.Contain("InterruptPreempted"), "uyuyan UYANDI — sebep hikâyeye taşınır");
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionFailed
    && e.ActorId.Value == 9), "terminal başarısızlık olay oldu");
// Bankalanan toparlanma GERİ ALINMAZ (korunum — W32 T5'in uyku yüzü):
Assert.That(A().Needs.Fatigue.Value, Is.LessThanOrEqualTo(fatigueAtSleepStart),
    "yarım uyku yarım dinlendirir; uyanmak cezası fatigue iadesi DEĞİLDİR");
Assert.That(A().Needs.Fatigue.Value, Is.GreaterThan(SleepFloorOf(fatigueAtSleepStart, ranTicks + 999)),
    "ama kesilen uyku TAM gecenin faydasını da veremez");
// ETİKET TAKİP EDER (RUH_TESHIS §10: activity == CurrentAction — uyanma tick'inde bile):
Assert.That(ActionVerbTable.Verb(ActorActionType.Sleep), Is.EqualTo("sleeping"));
Assert.That(A().ActionState.IsIdle, Is.True, "handover sonrası Idle — projeksiyon artık 'sleeping' ÜRETEMEZ");
// (davranış pini S7'de: Idle aktör için etiket null — saat 03:00 olsa bile.)
```

İkinci vaka (saldırı): pursuit yerine doğrudan `SleepOps.Interrupt(world, actor, Interrupted)` —
vitals hasarının uyandırma kapısına HANGİ sistemden bağlanacağı (witness/predation) DOC 03'ün malı;
bu test yalnız kapının sonucunu pinler: aynı iz, aynı korunum, aynı etiket dönüşü.

---

### S3 — Reach dışına çıkan işçinin tarif ilerlemesi O AN donar; dönünce KALDIĞI YERDEN sürer

**Dosya:** `Assets/Tests/EditMode/Actions/WorkReachFreezeTests.cs`

Yaranın kalbi: `RecipeSystem.Tick` sayaç makinesi aktör pozisyonunu HİÇ OKUMAZ ve
`JobAssignmentSystem.Tick.cs:86-92` onu işçiye sormadan her saat çağırır. Yeni hikâye: ilerletmenin
tek yolu `WorkOps.TryWorkTick` olur ve o, mesafeyi HER TICK doğrular — sistem sırası değil fizik karar verir.

**Kurulum:**
```csharp
var world = SleepWorkSliceWorld.Build(oreStock: 4);
world.Actors.Add(SleepWorkSliceWorld.Worker(7, 3, 3));
var composer = new WorldTickComposer();
AdvanceUntil(composer, world, () => OrderProgress(world) >= 2);   // PerformWork gerçekten başladı
int frozenAt = OrderProgress(world);
Shove(A(), cells: 10);                                            // baskın/itilme: reach dışına
                                                                  // (living.witness'in shove emsali —
                                                                  // test doğrudan MoveTo ile kurar)
for (int i = 0; i < 120; i++) { composer.Advance(world, NextTick()); PinPosition(A()); }
```

**Kesin iddia:**
```csharp
Assert.That(OrderProgress(world), Is.EqualTo(frozenAt),
    "işçi ocakta değil → tarif TEK TICK ilerlemedi (eski dünyada 2 saat = 2 adım BEDAVAYDI)");
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.RecipeCompleted), Is.False);
UnpinAndWalkBack(composer, world);                                // işçi döner
AdvanceUntil(composer, world, () => OrderProgress(world) > frozenAt);
Assert.That(OrderProgress(world), Is.EqualTo(frozenAt + 1),
    "dönüş SIÇRATMAZ: catch-up yok, reset yok — kaldığı yerden TEK adım (chunking hakemi kuralı)");
// Korunum: donma boyunca input ne iade edildi ne çoğaldı (consume-at-start sözleşmesi bozulmadı):
Assert.That(SleepWorkSliceWorld.TotalIron(world), Is.EqualTo(ironBefore), "MADDE KORUNUMU");
```

Ek vaka (kesilme): donma sırasında `WorkOps.Interrupt` → aksiyon düşer, `RecipeWorkOrder`
yarım progress'iyle YAŞAR ve aynı işçi (ya da sıradaki claimant) yeniden bağlanınca kaldığı
yerden süren aynı order'dır — order'ın kesilmede yaşama/iptal kuralı DOC 03'ün malı, test yalnız
"progress asla sessizce sıfırlanmaz/atlanmaz" invariantını pinler. Uçuş-ortası donmuş order'ın
save/load'u envanter #10'un iddiasıdır.

---

### S4 — Tamamlanmış PerformWork zinciri olmadan OUTPUT yok: hayalet işçi ÖLDÜ

**Dosya:** `Assets/Tests/EditMode/Actions/WorkOutputAuthorshipTests.cs` (F2'nin ocak ikizi)

**Kurulum (negatif):**
```csharp
var world = SleepWorkSliceWorld.Build(oreStock: 4);   // claim'li SmeltIron job + dolu ore pile
world.Actors.Add(SleepWorkSliceWorld.Worker(7, 3, 3));
var composer = new WorldTickComposer();
AdvanceUntil(composer, world, () => world.Jobs.GetClaimedBy(Job).Value == 7UL); // claim OLDU
PinFarAway(A());                                      // işçi hiçbir zaman reach'e giremez
AdvanceDays(composer, world, 3);                      // üç gün boyunca her econ.jobs sınırı geçilir
```

**Kesin iddia:**
```csharp
Assert.That(world.Stockpiles[0].Get("iron_ingot"), Is.Zero,
    "kimse ocakta TER DÖKMEDİ → output YOK (eski dünyada 3 gün = bedava ingot yağmuruydu)");
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.RecipeCompleted), Is.False);
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.JobCompleted), Is.False);
// Madde muhasebesi kapanır: input ya el değmemiş durur ya order'ın içindedir — ASLA yarı-yolda kaybolmaz:
Assert.That(SleepWorkSliceWorld.TotalIron(world), Is.EqualTo(ironStart), "input buharlaşmadı");
```

**İkinci vaka (yazarlık taraması — entegrasyon):** serbest işçiyle 3 gün koşulur; output stoğunun
ARTTIĞI her tick için izde aynı tick'te bir `PerformWork/Running->PerformWork/Succeeded` geçişi
vardır ve o tick'te işçi worksite hücresine `<= WorkReachCells` mesafesindedir. Artışın TEK kapısı
PerformWork commit'idir; `RecipeCompleted` olayı da yalnız o tick'te doğar. `RecipeSystem`'ın
consume-at-start / preflight / batch sözleşmeleri AYNEN yaşar — değişen tek şey ÇAĞIRANIN kim olduğu
(envanter #11).

---

### S5 (kapak taşı) — Gece+gündüz tam çevrim: iş günü → eve yürüyüş → uyku → uyanış → iş

**Dosya:** `Assets/Tests/EditMode/Actions/SleepWorkStoryChainTests.cs`

T8 ve F5'in takvim ikizi. RUH_TESHIS'in "aktörlerde kimlik var, fakat devam eden irade yok" cümlesinin
kapanış kanıtı: bir işçinin YİRMİ DÖRT SAATİ artık kesintisiz, sebepli bir aksiyon tarihidir — saat
tahmininden değil, fiillerin birbirini doğurmasından.

**Kurulum:**
```csharp
var world = SleepWorkSliceWorld.Build(oreStock: 8);   // iki günlük iş var
world.Actors.Add(SleepWorkSliceWorld.Worker(7, 2, 2)); // evinde, sabah 08:00
var composer = new WorldTickComposer();
AdvanceDays(composer, world, 2);                       // iki tam gün
var episodes = EpisodesOf(world, actor: 7);            // izden: (startedAt, intent) sıralı listesi
```

**Kesin iddia:**
```csharp
// Halkalar SEBEP sırasında: gündüz Work bölümü/bölümleri → akşam Sleep bölümü → ertesi gün YİNE Work.
Assert.That(episodes.Select(e => e.intent),
    Has.Some.EqualTo(ActorIntent.Work).And.Some.EqualTo(ActorIntent.Sleep));
Assert.That(FirstIndexOrder(episodes, ActorIntent.Work, ActorIntent.Sleep, ActorIntent.Work), Is.Ordered,
    "iş uykudan, uyku ertesi günün işinden ÖNCE — takvim yaşandı, anlatılmadı");
// Uyku bölümü MoveHome → Sleep zinciridir ve GECE bandında yaşar (dakika damgaları izden):
Assert.That(SleepEpisode(episodes).Chain, Is.EqualTo(new[] {
    ActorActionType.MoveHome, ActorActionType.Sleep }), "eve yürümeden yatak yok");
// Fatigue testere dişi: gündüz yalnız yükselir, yalnız Sleep bölümünde düşer (S1 taraması ufka yayılır);
// sabah fatigue'ü akşamkinden düşüktür — gece GERÇEKTEN dinlendirdi:
Assert.That(FatigueAt(trace, day2MorningTick), Is.LessThan(FatigueAt(trace, day1EveningTick)));
// Çevrim üretti: en az bir RecipeCompleted VE stok arttı — iş günü tiyatro değil:
Assert.That(world.Events.Events.Count(e => e.Kind == WorldEventKind.RecipeCompleted),
    Is.GreaterThanOrEqualTo(1), "tam çevrimin kanıtı üretimdir");
// Hiçbir tick'te aktör "eylemsiz ve kayıp" değildi: Sleep/Work bölümleri arasında schedule
// yönlendirmesi yaşar (W32 T4 süreklilik dersi — boşluk ayıbı yalnız bölüm İÇİNDE aranır).
```

---

### S6 — Faz-izi chunking invaryansı Sleep/PerformWork'ü de kapsar

**Dosya:** `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` (GENİŞLER — yeni dosya yok)

Hakem değişmez: tick-tick ile ragged chunk koşumu (`{1,7,13,1,40,3,61,5,127,2}` seti HARF HARF aynı)
BİREBİR aynı faz akışını yazmalıdır. Uyku bu testin EN İYİ avıdır: gece sınırı (22:00/06:00) chunk
İÇİNE düştüğünde Sleep kararı ya da toparlanma tick'i yanlış saate yazılırsa iki akış anında ayrışır.
İş için aynısı `econ.jobs@Hourly:10` sınırı ile `PerTick:18/22` bandının etkileşiminde yaşar.

**Kesin iddia (mevcut iddialara EK — F6'nın vacuous-guard kalıbı):**
```csharp
// FullCast koşumu (4 gün) kadroda uyku bölümlerini kendiliğinden içerir:
Assert.That(tickByTick.Any(l => l.Contains("Sleep")), Is.True,
    "vacuous guard: dört gecede hiç uyku yaşanmadıysa test SLEEP'i hiç sınamıyor demektir");
// PerformWork tam kadroda organik doğmuyorsa fixture'a SmeltIron job'ı eklenir ya da
// SleepWorkSliceWorld tabanlı üçüncü dar-kadrolu koşum açılır (FarmCast emsali — karar DOC 03 inerken):
Assert.That(tickByTick.Any(l => l.Contains("PerformWork")), Is.True,
    "vacuous guard: İŞ bölümü de akışta olmalı");
Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)));   // değişmedi
```

---

### S7 — Etiket gerçeği: dört yeni fiil + gece/iş tahmin dallarının ÖLÜMÜ

**Dosyalar:** `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` (satır EKLENİR)
+ `NpcPoseIconView.cs` lint kapsamı

`ActionVerbTable` tek-girdili imza garantisi (yalnız `ActorActionType`; Hour/Position/Needs lint'i)
zaten yapısal; yeni satırlar — sözcükler tahmin dallarından MİRAS alınır, tahmin ÖLÜR:

```csharp
Assert.That(ActionVerbTable.Verb(ActorActionType.MoveHome), Is.EqualTo("heading home"));
Assert.That(ActionVerbTable.Verb(ActorActionType.Sleep), Is.EqualTo("sleeping"));
Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToWorksite), Is.EqualTo("to work"));
Assert.That(ActionVerbTable.Verb(ActorActionType.PerformWork), Is.EqualTo("working"));
```

Lint yarısı (mevcut `Lint_ProjectionReadsTheTable...` genişler): bugün
`DomainSimulationAdapter.WorldProjection.cs:132-139` saatten "sleeping/heading home/winding down/working"
UYDURUR ve `:93` `sleeping:` view bayrağını `IsAsleepAtHome` tahmininden doldurur — §2.9 hastalığının
hayatta kalan gece+iş dalları. Banned listesine eklenir: `"\"sleeping\""`, `"\"heading home\""`,
`"\"winding down\""`, `"\"working\""`, `"IsAsleepAtHome"`, `"GUESS(SLEEP"`, `"GUESS(WORK"`.
`sleeping:` bayrağının yeni kaynağı `ActionState.CurrentAction == Sleep` olur (dosyadaki
`GUESS(SLEEP slice)` emeklilik yorumunun kendi vasiyeti). `NpcPoseIconView.cs:43`'ün saat-poll dalı da
`GUESS(WORK` yasağıyla ölür. Hayatta kalan tahminler yalnız `GUESS(GUARD` ("on watch") ve
`GUESS(COMBAT` ("hunting") etiketleriyle yaşar — `Does.Contain("GUESS(")` pini onlarla sürer
(W32 sözleşmesi: dilim indiğinde o dilimin tahmini KALAMAZ).

---

## 2. DEĞİŞMESİ GEREKEN Mevcut Testler (pin envanteri + yeni hikâyeleri)

Grep tabanı: `NightSleepFatigueRecovery | TickAssignedJobs | _activeOrders | SmeltIron | sleeping |
heading home | GUESS( | BaselineHash | econ.jobs | living.consumption | rest`.

| # | Dosya | Bugünkü pin | Yeni beklenen hikâye |
|---|---|---|---|
| 1 | `Assets/Tests/EditMode/Living/NeedConsumptionSystemTests.cs` | `Tick_NightHour_TiredCivilianSleepsAndMoodFollows`: 23:00'te KONUMSUZ -40 fatigue; `Tick_DayHour_...`: gündüz toparlamaz | **Gece pini ters döner:** saatlik adım artık HİÇ toparlamaz — toparlanma S1'in malı. "Gündüz toparlamaz + asla yedirmez" pinleri ruhen yaşar (izin negatifi). `FoodSpots` geometri testleri AYNEN kalır; dosya küçülür, sabitler (`HungerEatThreshold`, `EatReachCells`) yaşadıkça dosya silinmez. `ConsumptionStep`'in kaderi DOC 03'ün (bkz. #7). |
| 2 | `Assets/Tests/EditMode/Living/NeedRecoverySystemSleepTests.cs` | `Sleep(recipe)` anlık toparlanma + `need_recovery/action:sleep` ReasonTrace zinciri | W32 satır-3 emsali harfiyen: `Sleep`, Sleep adımının İÇ uygulayıcısı olur (tek yazar); ReasonTrace'e aksiyon halkası eklenir. "Dinç aktöre olay yok" pini aynen kalır. "Anlıklık" artık yalnız tick-commit'inin uygulanışıdır, kararı değil. |
| 3 | `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` | Gate1: `avgFatigue < 75` ("nobody sleeps") + `meals = Count(ActionCompleted) >= 3×villager` | **Eşikler KORUNUR** — Gate1 artık cansız battaniyenin değil, gövdeli uykunun kanıtıdır; kırmızıysa eşik değil karar saati/oran yanlıştır (W32 Gate1 dersi). **Zorunlu daraltma:** `meals` sayacı TÜM `ActionCompleted`'ı sayıyor — W33'ten beri farm terminalleriyle şişikti, gecelik Sleep terminalleri (5 gün × villager) pini anlamsızlaştırır. Lambda `e.Reason.StartsWith("eat:")` olur (reason öneki `ActionLogManager.Chain`'in malı). Gate4/5/8 dokunulmaz. |
| 4 | `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` | Rest yarısı: `ChooseTarget` fatigue + `NightRestBonus` ile aktörü Home'a BİZZAT yürütür | W32 satır-4'ün gece yüzü: sivil uyku yönlendirmesi karar katmanına göçer (`Sleep` intent + `MoveHome`); `ChooseTarget`'ın rest satırı ya ölür ya yalnız "aksiyonsuz kalan" artıklar için yaşar — biçim DOC 02'nin malı. Guard/Enemy `ClassicTarget` curfew'u ve pursuit çözümü bu dilimde DEĞİŞMEZ. Work-hour routing (`TargetWorksitePosition`) `MoveToWorksite` aksiyonuna devreder — testler decision-table pinine dönüşür. |
| 5 | `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs` | 2 günde: job claim edildi + işçi non-idle + iron fiyatı YÜKSELDİ (stok akışı serbest-koşan üretimden gelirdi) | Claim/non-idle pinleri AYNEN yaşar (köprü değişmedi). Üretim artık GÖVDELİ: tek işçi geceleri uyuyacağı için tamamlanma zamanlaması kayar — iddialar "2 gün içinde işçi ocağa YÜRÜDÜ ve en az bir order ilerledi" düzeyinde gözden geçirilir; fiyat-drift iddiası stok gerçekten aksiyonla değişince yaşar. W33 satır-7 emsali: yorum bloğuna tarih notu düşülür. Determinizm ikizi (`RunAndSnapshot`) metin değişmeden yaşar. |
| 6 | `Assets/Tests/EditMode/Composition/WorldNpcDailyRhythmTests.cs` | 22:00 örneklemesinde HERKES `actor.Home`'da; gündüz/gece pozisyonları ayrışır | Pin YAŞAR ve güçlenir: eve varış artık MoveHome bölümünün kanıtıdır. İNCELEME işaretli: 22:00'de hâlâ yürüyen varsa yanlış olan örnekleme saati değil KARAR saatidir (karar bandı gün batımından önce MoveHome'u başlatmalı — W32 satır-13 dersi aynen). |
| 7 | `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` | Kanonik liste `Hourly:35:living.consumption` + `Hourly:10:econ.jobs` içerir | `econ.jobs` satırı YAŞAR ama sözleşmesi daralır: poster/atayıcı/claim-süpürücü — "Hourly:10 stok/progress yazarı" kimliği ölür (gövde S3/S4). `living.consumption` satırı ya SİLİNİR ya boş-gövde kalır — karar DOC 03'ün; hangi biçimde olursa olsun "Hourly:35 fatigue düşürür" ölür. `PerTick:18/22` id'leri DEĞİŞMEZ (uyku+iş aynı iki banda biner — W33 kuralı). |
| 8 | `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` (+ `FieldOwnershipRegistry.cs:27-32,54-61`) | `Actor.Needs` yazarları: needs ramp + ConsumeFood commit + `living.consumption@Hourly:35` (sleep yarısı); `World.Stockpiles` yazarları arasında `econ.jobs@Hourly:10` | `Actor.Needs`'te consumption satırı SİLİNİR; fatigue-düşüş yazarı `living.action_advance@PerTick:22` (Sleep tick-commit'i) olur. `World.Stockpiles`'ta `econ.jobs` satırı ya silinir ya "yalnız order start'ta input consume" dipnotuna daralır — nihai biçim DOC 03'ün; test azalan/yeni satırları pinler, çok-yazarlılık lint edilen gerileme kalır. |
| 9 | `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` | `BaselineHash = "a2489e8b3514..."` (`:50`, W33-B damgalı) | **Zorunlu re-baseline** — meşru tarih değişimi: gece toparlanması saatlik battaniyeden Sleep tick'lerine, üretim `econ.jobs` saat sınırından PerformWork commit'lerine kayar; yeni terminal olaylar loglanır. Prosedür dosya geleneği: ÖNCE aynı-seed çift koşum birebirken yakala, SONRA hash'i tarih+sebep yorumuyla değiştir ("W34 SLEEP+WORK: uyurgezerlik ve hayalet işçi öldü"). **EN SON adım** — tüm davranış otururken BİR kez (W32/W33 sıra kuralı). |
| 10 | `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` + `SaveLoadDigestRoundtripTests.cs` + `ActorActionState.TryRestore` aralık pinleri | Temsilî dünyada uçuş-ortası EAT+FARM durumları; `TryRestore` `intent > Harvest` ve `action > HaulCrop`'u REDDEDER | Üst sınırlar yeni uçlara genişler (`Work`/`PerformWork`); `carriedUnits` kapısı DEĞİŞMEZ (el yalnız Harvest/Haul'da dolu). Temsilî dünyaya İKİ uçuş-ortası durum eklenir: `Sleep@progress` (gece yarısı save) ve `PerformWork@progress` + **donmuş** RecipeWorkOrder (S3'ün save yüzü: reach dışında kaydet → yükle → progress AYNI). `RecipeWorkOrderSaveData` zaten roundtrip'li (`WorldSaveRehydration`) — mapper bir alanı düşürürse reflection farkı burada patlar. |
| 11 | `Assets/Tests/EditMode/Process/JobAssignmentSystemTests.cs` + `JobEventLogTests.cs` | `TickAssignedJobs`: işçi NEREDE olursa olsun saat başına bir progress; batch continuation; `JobCompleted` emisyonu | Serbest-koşan pinler ÖLÜR; `RecipeSystem`'ın order makinesi (consume-at-start, preflight, batch, tek `RecipeCompleted`) PerformWork commit'inin İÇ uygulayıcısı olarak AYNEN yaşar — dosyalar "order makinesi birim testi"ne daralır, çağıran-katman iddiaları S3/S4'e göçer. `StartRecipeForClaim` kapıları (aktif worksite, kind eşleşmesi) yaşar. `JobAssignmentCompetitionTests` DEĞİŞMEZ: claim yarışı karar köprüsünün girdisi kalır (W33 satır-4 emsali). |
| 12 | `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` | 4 gün FullCast + 3 gün FarmCast; iki vacuous guard | S6'nın kendisi: Sleep + PerformWork guard'ları eklenir; chunk seti ve eşitlik iddiası HARF HARF aynı kalır. Koşum süresi büyürse çare ufku kısaltmak DEĞİL, dar-kadrolu koşum eklemektir (F6 kuralı). |
| 13 | `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` (+ `NpcPoseIconView.cs` lint kapsamı, + `ActorSaveMapperTests`'in `sleeping` alanı) | 7 fiil satırı + `GUESS(` emeklilik sözleşmesi; `sleeping:` bayrağı `IsAsleepAtHome` tahmininden | S7'nin kendisi: 4 yeni satır; banned listesi büyür; `GUESS(SLEEP`/`GUESS(WORK` etiketleri SİLİNMİŞ olmalı; `sleeping:` kaynağı `CurrentAction == Sleep`. `ActorSaveMapperTests` yalnız GÖZDEN GEÇİRME (alan taşınıyorsa kaynak değişir, save şekli değişmez). |
| 14 | `Assets/Tests/EditMode/Composition/CatchupPerfPinTests.cs` + `LiveScaleCatchupPerfPinTests.cs` | 14 gün < 5 sn; 800 sivil 1 gün < 3 sn | Eşik ve metin AYNEN KALIR (W30e dersi). Sıcak döngüye gecelik Sleep kararı + reach kontrolü eklenir: ikisi de O(aktör) ve alansız (cache'siz tek Chebyshev) kalmak ZORUNDA — bu iki test onun bekçisidir; yorum blokları yeni sıcak yola göre güncellenir. |

**Değişmeyen bekçiler (bilinçli):** `Actions/Eat*` T1–T8 ailesi, `Actions/Farm*` F1–F5 ailesi,
`GuardEatStoryTests` (pursuit-üstünlüğü dikişi S2'nin TEMELİdir — bozulursa önce o kırmızıya döner),
`PlantGrowthSystemTests`, `ProductionRecipeRegistryTests` (SmeltIron id/malzeme DATA pinleri — tarif
verisi bu dilimde DOKUNULMAZ, yalnız kimin ilerlettiği değişir), `SettlementCraftingServiceTests`
(OYUNCU şeridinin anlık craft'ı — köylü üretimi değil; bilinçli kapsam dışı), `Faz1AcceptanceReplayTests`,
`CadenceChunkingInvarianceTests` (metin değişmez, pin kendiliğinden güçlenir), Gate4/5/8 ve
`GateContractLintTests`. Fallback/proof harness (`--ember-proof-screenshots`) sözleşmesi değişmez;
Gate'ler yeşilken harness da yeşildir.

---

## 3. Çalıştırma Sırası (uygulama haftası için)

1. S1–S7 önce KIRMIZI yazılır (derleme için gereken `SleepWorkSliceWorld` + Ops kancaları DOC 01–03'e sipariştir).
2. Dilim kodu indikçe sıra: S1 (uyku yazarlığı) → S2 (uyandırma) → S4 (hayalet işçinin ölümü) →
   S3 (donma/devam) → S7 (etiket) → S6 (chunking) → S5 (kapak taşı — takvim otururken).
3. Bölüm-2 tablosu tek PR'da: #1–2/#4/#11 yeniden yazım, #3 lambda daraltması, #7–8 liste düzeltmesi,
   #9 re-baseline **EN SON** (tüm davranış otururken bir kez).
4. Yeşil tanımı: tüm suite + S1–S7 + re-baseline'lı golden, aynı-seed çift koşum birebirken.

## 4. Sonraki Dilime Devreden Maddeler (bilinçli kapsam dışı)

- "Uygun YATAKTA olur"un mobilya yarısı → FURNITURE dilimi: bu dilimde yatak = home hücresi;
  yatak bir Thing/affordance olduğunda `SleepOps` hedef seçimi rezervasyonlu yatağa döner
  (ReservationLedger'ın key-encoded emsali hazır: `bed:{componentId}`, `plot:`/`carry:` kalıbı).
- Guard gece vardiyası + "on watch" etiketi → GUARD dilimi (`GUESS(GUARD` o zaman ölür);
  "hunting" → COMBAT dilimi. `Does.Contain("GUESS(")` pini son tahminle birlikte topluca kaldırılır.
- Crop→meal pişirme (mutfak PerformWork'ü) → CRAFT dilimi; WORK altyapısı hazır, tarif satırı eklenir.
- Uyandırma kapısına vitals-hasar bağlantısı (witness/predation → `SleepOps.Interrupt`) DOC 03'te
  yalnız kapı olarak açılır; kaskad hikâyesi (gece baskını → köy uyanır) sonraki dilimin gate'idir.
- Genelleşen kalıplar: S1 (tek-ebeveyn yazarlık taraması), S3 (donma/devam) ve S6 (vacuous-guard'lı
  chunking izi) şablonları dördüncü kez göründüklerinde parametrikleştirilir — `ActorActionType`
  büyüdükçe test şablonu sabit kalır (W32'den beri süren sözleşme).

# W33 / DOC 4 — FARM Dilimi Hikâye Testleri (RUH_TESHIS §2.8 + §9 dilim 2 + §10 → Somut EditMode Testleri)

> Kaynak teşhis: `docs/RUH_TESHIS.md` §2.8 ("tarım zinciri hem kopuk hem teleportlu"), §9 ikinci dikey dilim
> (`seed → plot rezervasyonu → yürüyüş → Plant → growth → Harvest → elde crop → Haul → stockpile`), §10
> ("Plant output doğrudan stockpile'a teleport olmaz", "Input olmadan output oluşmaz"un tarım yarısı).
> Kapsam: **YALNIZCA FARM dilimi** (plant + harvest + haul). Uyku, genel worksite/iş ve craft dilimleri
> bilinçli KAPSAM DIŞI — son bölümde devir listesi var. Desen sahibi W32 EAT dilimidir
> (`docs/ruh/w32/06-story-tests.md`): T1–T8 şablonları burada YENİDEN KULLANILIR, yeniden icat edilmez.

---

## 0. Test Sözleşmesi — Bu Testlerin Varsaydığı API Yüzeyi

Adlandırma sahibi kardeş dokümanlardır (durum/enum DOC 01, karar+plot rezervasyonu DOC 02, ilerletme+
operasyonlar DOC 03). İsim değişirse test metinleri mekanik güncellenir, **iddialar değişmez**.

```csharp
// Domain/Actors — W32 enumları APPEND-ONLY büyür (save'e int yazılır; yeniden numaralama YASAK;
// default(ActorActionState) == Idle "all-zero" kuralı aynen korunur — eski save'ler Idle yüklenir):
ActorIntent      { None=0, Eat=1, Plant=2, Harvest=3 }
ActorActionType  { None=0, MoveToFood=1, TakeFood=2, ConsumeFood=3,
                   MoveToPlot=4, PlantSeed=5, HarvestCrop=6, HaulToStockpile=7 }
// Zincirler ActionLifecycleSystem.NextLink kalıbıyla intent'ten türetilen SABİT boru hatlarıdır (save'e yazılmaz):
//   Plant   : MoveToPlot → PlantSeed
//   Harvest : MoveToPlot → HarvestCrop → HaulToStockpile
// ActionFailureReason yeni üyeleri (PlotLost, NotRipe, ...) DOC 01'in malı — append-only.

// Domain/Process — FoodOperations'ın kardeşi; her operasyon MESAFEYİ ve rezervasyonu KENDİSİ doğrular
// (sistem sırası ne olursa olsun uzaktan plant/harvest/deposit FİZİKSEL reddedilir — W32 T1 kalıbı):
FarmOps.TryReservePlot(world, actor, out reservationId)  // boş soil hücresi (Plant) / ripe plant (Harvest)
FarmOps.TryPlantAt(world, actor)      // rezervasyonlu seed birimi düşer + GERÇEK PlantComponent doğar + PlantPlanted
FarmOps.TryHarvestAt(world, actor)    // ripe doğrulanır; crop birimi AKTÖRÜN ELİNE geçer + PlantHarvested
FarmOps.TryDepositCarried(world, actor) // el → pile; zincirin stok ARTIRAN TEK operasyonu
FarmOps.Interrupt(world, actor, reason) // faza göre korunumlu iade/serbest bırakma (W32 T5 kalıbı)
// Plot rezervasyonunun defter biçimi (ReservationLedger genişler mi, kardeş PlotLedger mı) DOC 02'nin
// malı; testler yalnız İNVARYANTI pinler: bir plot'ta EN FAZLA bir aktif claim.

// Composer — YENİ STEP ID YOK: farm kararı living.decision@PerTick:18 içinde, ilerletme
// living.action_advance@PerTick:22 içinde yaşar (tek-yazar eleştirisi yeni örnek KAZANMAZ).
// world.harvest@Daily:25 teleport gövdesi ÖLÜR (bkz. F2 + envanter #5).

// Olay grameri — WorldEventKind append-only; YENİ kind eklenmez, mevcutlar SEBEP olaya döner:
// PlantPlanted(11) / PlantStageAdvanced(12) / PlantHarvested(13) artık tamamlanan aksiyonların
// commit'inde doğar; terminal başarı/başarısızlık ActionCompleted(33)/ActionFailed(34).
// ActionLogManager'daki "ToAction == ConsumeFood" özel durumu intent-başına-terminal-halka
// genellemesine döner (PlantSeed ve HaulToStockpile de ActionCompleted yazar) — DOC 03'ün malı.
```

Kanıt yüzeyi DEĞİŞMEZ: `Support/ActionTrace.Of` (ActionLog ring + terminal olaylar) ve
`ActionTrace.StateDigest` aynen yeniden kullanılır — render/diagnostik log ASLA kanıt değildir.

Ortak kurulum (EatSliceWorld kalıbı — story testlerin TEK dünya-kurma yolu):

```csharp
// Assets/Tests/EditMode/Actions/Support/FarmSliceWorld.cs
// Site(1) (0,0)-(10,10); soil hücreleri kuşakta; WorldTickComposer'ın wheat türü kullanılır:
// seed(1 gün) → sprout(1 gün) → ripe(harvestable) — WorldTickComposer.cs:106-114.
static WorldState Build(int seedStock = 4, int soilCells = 2) // pile + soil + EnsureInvariants
static ActorRecord Farmer(ulong id, int x, int y)             // tok, dinç sivil: farm kararı yemekle yarışmaz
static ActorRecord Hungry(ulong id, int x, int y)             // F5'in çemberi kapatan yiyicisi
const string SeedTag;  const string CropTag;                  // tag adları DOC 01'in malı — testler sembolik okur
static int TotalCrop(WorldState w)                            // pile + eldeki + (ripe plot başına 1) — korunum sayacı
```

Bütün testler **EditMode**, saf Domain/Simulation (Unity API yok, IO yok, RNG yalnız seed'li) —
determinizm anayasası + chunking hakemi aynen geçerli.

---

## 1. Hikâye Testleri (F1–F7)

### F1 — PlantSeed tamamlanmadan plot'ta bitki OLUŞAMAZ

**Dosya:** `Assets/Tests/EditMode/Actions/FarmPlantAuthorshipTests.cs`

Bugün bitki iki yoldan doğar: worldgen (`WorldFactory.cs:141-144`) ve hiç doğmaz — çünkü planting job'ın
`5101` tarifi kayıtsızdır ve B05 düzeltmesi job'ı chronicle iziyle düşürür (`DefaultTickSystems.cs:175-195`).
Yeni hikâye: canlı akışta bitkinin TEK ebeveyni tamamlanmış bir `PlantSeed` aksiyonudur.

**Kurulum:**
```csharp
var world = FarmSliceWorld.Build(seedStock: 2, soilCells: 1);
world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
var composer = new WorldTickComposer();
int plantsBefore = world.Plants.Rows.Count;
```

**Kesin iddia (pozitif — yazarlık):**
```csharp
RunUntil(composer, world, () => world.Plants.Rows.Count > plantsBefore, maxTicks: 2000);
var planted = world.Events.Events.Single(e => e.Kind == WorldEventKind.PlantPlanted);
// İz: aynı aktör, aynı tick'te PlantSeed Running->Succeeded geçişi YAŞADI (ActionTrace satırı).
Assert.That(ActionTrace.Of(world), Does.Contain($"{planted.Tick.TotalMinutes}:7:")
    .And.Contain("PlantSeed/Running->PlantSeed/Succeeded"), "bitkinin ebeveyni tamamlanmış PlantSeed'dir");
// Fiziksellik: commit anında aktör plot'a erişim mesafesindeydi (olay pozisyon/plot damgalı).
// Madde: seed stoku TAM 1 düştü — input olmadan output yok, output olmadan input gitmez.
Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.SeedTag), Is.EqualTo(1));
```

**Kesin iddia (negatif — saldırgan kurulum):**
```csharp
// Yürüyüş ortasında kes: bitki YOK, seed stoku DEĞİŞMEDİ, plot claim'i serbest.
FarmOps.Interrupt(world, farmer, ActionFailureReason.Interrupted);
Assert.That(world.Plants.Rows.Count, Is.EqualTo(plantsBefore), "yarım niyet bitki DOĞURMAZ");
// Uzaktan zorlama: faz ConsumeFood benzeri Debug_Set ile PlantSeed'e kurulur, aktör 40 hücre ötede:
Assert.That(FarmOps.TryPlantAt(world, far), Is.False, "uzaktan ekim REDDEDİLİR (W32 T1 kalıbı)");
```

---

### F2 — HarvestCrop tamamlanmadan YIELD yok: teleport ÖLDÜ

**Dosya:** `Assets/Tests/EditMode/Actions/FarmHarvestTeleportDeathTests.cs`

Bugünkü yara TAM ŞURADA: `DefaultTickSystems.cs` `HarvestStep` (`"world.harvest", Daily, 25`;
sınıf 455) yakında el bulursa `pile.Add(p.SpeciesId, 2)` yazar (satır 490) ve bitkiyi seed'e geri sarar —
ürün yerde/elde/asla yolda olmaz; sayaçlar arası TELEPORT (teşhis §2.8). `HarvestHandsService` "yakınlık
büyüsü" de bu adımla birlikte ölür: hasatçı artık bulunmaz, hasatçı AKSİYONUN SAHİBİDİR.

**Kurulum:**
```csharp
var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
PlantRipe(world);                                   // ripe bitki hazır
// HİÇ sivil eklenmez — eski kod el bulamayınca beklerdi ama el BULUNCA tek tick'te +2 yazardı.
var composer = new WorldTickComposer();
AdvanceDays(composer, world, 3);                    // üç Daily:25 sınırı geçer
```

**Kesin iddia:**
```csharp
Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.Zero,
    "kimse hasat AKSİYONU yaşamadı → stok KIMILDAMAZ (eski dünyada +6 olurdu)");
Assert.That(world.Plants.Rows.Single().Value.StageId.Value, Is.EqualTo("ripe"),
    "bitki olgun BEKLER — hayalet eller onu seed'e geri saramaz");
Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.PlantHarvested), Is.False);
```

**İkinci vaka (yazarlık taraması — entegrasyon):** kalabalık dünyada 5 gün koşulur; crop stoğunun
ARTTIĞI her tick için izde aynı tick'te bir `HaulToStockpile/Running->HaulToStockpile/Succeeded`
geçişi vardır ve aktör pile sitesine erişim mesafesindedir. Artışın TEK kapısı `TryDepositCarried`dır:
`PlantHarvested` olayının kendisi stok DEĞİŞTİRMEZ (el değiştirir) — delta taraması bunu ayrıca pinler.

---

### F3 — Aynı plot İKİ KEZ rezerve edilemez

**Dosya:** `Assets/Tests/EditMode/Actions/FarmPlotReservationConflictTests.cs`

Bugün plot kavramı yok; iki job aynı hücreye yazılabilir. Yeni hikâye W32 T2'nin toprağa inişi:
"iki çiftçi aynı boş hücreyi ister; kazanan DETERMİNİSTİK, kaybeden BUNU BİLİR".

**Kurulum:**
```csharp
var world = FarmSliceWorld.Build(seedStock: 4, soilCells: 1);   // TEK boş hücre
world.Actors.Add(FarmSliceWorld.Farmer(1, 4, 4));               // store sırasında ÖNCE
world.Actors.Add(FarmSliceWorld.Farmer(2, 4, 5));
var composer = new WorldTickComposer();
composer.Advance(world, 1);                                     // decision bandı
```

**Kesin iddia:**
```csharp
Assert.That(PlotClaims(world), Is.EqualTo(1), "tek hücre → tek claim");
Assert.That(PlotClaimOwner(world).Value, Is.EqualTo(1UL),
    "kazanan store insertion sırası — seed değil, SIRA kırar (W32 T2 kuralı)");
Assert.That(ActorOf(2).ActionState.CurrentAction,
    Is.Not.EqualTo(ActorActionType.MoveToPlot), "kaybeden PLANA BAĞLANMAZ; replan sonraki tick");
// Doğrudan çift claim: false döner, iz'de ikinci ReservationAcquired YOKTUR.
Assert.That(FarmOps.TryReservePlot(world, ActorOf(2), out _), Is.False);
// Serbest bırakma kanıtı (W32 T5 kalıbı): kazanan kesilince hücre TEKRAR dünyanın malı.
FarmOps.Interrupt(world, ActorOf(1), ActionFailureReason.Interrupted);
Assert.That(FarmOps.TryReservePlot(world, ActorOf(2), out _), Is.True);
```

Ek vaka (Harvest tarafı): tek ripe bitkiye iki hasatçı → tek claim; bitki bir kez `PlantHarvested`
yaşar, yield toplamı bir hasadın yield'ini AŞMAZ.

---

### F4 — Haul madde korunumu: stockpile deltası == el deltası

**Dosya:** `Assets/Tests/EditMode/Actions/FarmHaulConservationTests.cs`

Taşıma dilimin YENİ fiziğidir: birim plot'tan ele, elden pile'a YOLCULUK eder. Her kesilme noktasında
toplam madde sabittir; birimin kesilmede NEREYE düştüğü (iade kuralı) DOC 03'ün malı — bu test yalnız
KORUNUMU pinler, adresi değil (W32 T5'in "yarım yeme yok" basitleştirme dersi).

**Kurulum (parametrik):**
```csharp
[TestCase(ActorActionType.MoveToPlot)] [TestCase(ActorActionType.HarvestCrop)] [TestCase(ActorActionType.HaulToStockpile)]
public void Interrupt_AtLink_ConservesCropAndFreesClaims(ActorActionType at)
{
    var world = FarmSliceWorld.Build(seedStock: 0, soilCells: 1);
    PlantRipe(world); world.Actors.Add(FarmSliceWorld.Farmer(7, 12, 12));
    var composer = new WorldTickComposer();
    AdvanceUntilAction(composer, world, at);          // halka başına deterministik koşum
    int before = FarmSliceWorld.TotalCrop(world);     // plot + el + pile
    FarmOps.Interrupt(world, A(), ActionFailureReason.Interrupted);
    composer.Advance(world, NextTick());
```

**Kesin iddia:**
```csharp
    Assert.That(FarmSliceWorld.TotalCrop(world), Is.EqualTo(before), "MADDE KORUNUMU: dup yok, kayıp yok");
    Assert.That(PlotClaims(world), Is.Zero, "claim serbest");
    Assert.That(A().ActionState.IsIdle, Is.True, "eylem düştü, replan sonraki karar bandında");
}
// Mutlu yol ikizi: kesintisiz koşumda pile deltası == elden düşen delta (+1); PlantHarvested ile
// deposit arasındaki HER tick'te birim TAM BİR yerdedir (elde) — asla iki yerde, asla sıfır yerde.
```

---

### F5 (kapak taşı) — Kıtlıktan sofraya: shortage → plant → growth → harvest → haul → stockpile → meal

**Dosya:** `Assets/Tests/EditMode/Actions/FarmStoryChainTests.cs`

W32 T8'in tarla ikizi ve RUH_TESHIS §9'un cümlesi: "Bu çember kapandığında ekonomi ilk kez gerçekten
yaşar." Kıtlık (`ShortageResponseSystem.ShortageThreshold = 4` altı, Daily:27) planting'i tetikler;
büyüme Daily:20'de 2 gün sürer; hasat+taşıma stoku doldurur; aç sivil o stoktan yer.

**Kurulum:**
```csharp
var world = FarmSliceWorld.Build(seedStock: 4, soilCells: 2);
world.Stockpiles[0].Add(FarmSliceWorld.CropTag, 1);           // eşik ALTI: kıtlık gerçek
world.Actors.Add(FarmSliceWorld.Farmer(7, 9, 9));
world.Actors.Add(FarmSliceWorld.Hungry(8, 3, 3));             // çemberi kapatacak boğaz
var composer = new WorldTickComposer();
AdvanceDays(composer, world, 5);                              // shortage(D1) → ekim → 2 gün büyüme → hasat+haul → yemek
```

**Kesin iddia:**
```csharp
var kinds = new[] { ShortageDetected, PlantPlanted, PlantStageAdvanced, PlantHarvested, ActionCompleted };
var chain = world.Events.Events.Where(e => kinds.Contains(e.Kind)).ToList();
Assert.That(FirstIndexOrder(chain, ShortageDetected, PlantPlanted, PlantHarvested), Is.Ordered,
    "halkalar SEBEP sırasında: kıtlık ekimden, ekim hasattan ÖNCE — sonradan yazılmış yorum değil");
Assert.That(chain.Count(e => e.Kind == PlantStageAdvanced), Is.GreaterThanOrEqualTo(2),
    "büyüme GERÇEKTEN yaşandı: seed→sprout→ripe, tick değil GÜN ölçeğinde");
// Bölüm kimlikleri: Plant bölümü TEK ActionId, Harvest bölümü TEK ActionId (W32 T4 sürekliliği tarlada).
Assert.That(EpisodeIds(world, actor: 7), Has.Count.EqualTo(2), "iki bölüm: ekim ve hasat+haul");
// Madde muhasebesi gün gün kapanır: pile + eller + tarladaki potansiyel, seed girişiyle açıklanır.
Assert.That(world.Stockpiles[0].Get(FarmSliceWorld.CropTag), Is.GreaterThan(1), "stok AKSİYONLA doldu");
// Çemberin kapanışı: aç sivil HAULLANMIŞ birimi yedi (EAT zinciri değişmeden yeniden kullanılır).
Assert.That(MealsOf(world, actor: 8), Is.GreaterThanOrEqualTo(1), "tarladan sofraya — ekonomi YAŞIYOR");
```

---

### F6 — Faz-izi chunking invaryansı YENİ aksiyonları da kapsar

**Dosya:** `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` (GENİŞLER — yeni dosya yok)

Hakem değişmez: tick-tick ile ragged chunk koşumu BİREBİR aynı faz akışını yazmalıdır. Mevcut test
2 gün koşar; wheat 2 günde ripe olduğundan hasat bölümleri ufka SIĞMAZ — ufuk 4 güne çıkar
(`TotalTicks = 4 * 1440`; chunk seti `{1,7,13,1,40,3,61,5,127,2}` AYNEN kalır). Daily:20/25/27
sınır adımlarıyla PerTick:18/22 aksiyon bandının etkileşimi tam da bu testin avıdır: bir farm fazı
chunk İÇİNDE yanlış saate yazılırsa iki akış ayrışır.

**Kesin iddia (mevcut iddiaya EK):**
```csharp
Assert.That(tickByTick.Any(l => l.Contains("PlantSeed")), Is.True,
    "vacuous guard: ufukta EKİM bölümü yaşanmadıysa test farm'ı hiç sınamıyor demektir");
Assert.That(tickByTick.Any(l => l.Contains("HaulToStockpile")), Is.True,
    "vacuous guard: HASAT+TAŞIMA bölümü de akışta olmalı");
Assert.That(string.Join("\n", ragged), Is.EqualTo(string.Join("\n", tickByTick)));   // değişmedi
```

Koşum süresi ~2×: perf pinlerine dokunulmaz; test süresi sorun olursa çare ufku kısaltmak DEĞİL,
`FarmSliceWorld` tabanlı ikinci bir dar-kadrolu invaryans koşumu eklemektir (karar DOC 03 uygulanırken).

---

### F7 — Etiket gerçeği: üç yeni fiil + tarla tahmin dallarının ÖLÜMÜ

**Dosya:** `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` (satır EKLENİR)

`ActionVerbTable` tek-girdili imza garantisi (yalnız `ActorActionType`) zaten yapısal; yeni satırlar:

```csharp
Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToPlot), Is.EqualTo("to the field"));
Assert.That(ActionVerbTable.Verb(ActorActionType.PlantSeed), Is.EqualTo("planting"));
Assert.That(ActionVerbTable.Verb(ActorActionType.HarvestCrop), Is.EqualTo("harvesting"));
Assert.That(ActionVerbTable.Verb(ActorActionType.HaulToStockpile), Is.EqualTo("hauling"));
```

Lint yarısı (mevcut `Lint_ProjectionReadsTheTable...` genişler): bugün
`DomainSimulationAdapter.WorldProjection.cs:134-145` olgun bitkiye YAKINLIKTAN "harvesting",
kuşakta durmaktan "tending the field" UYDURUR — §2.9 hastalığının hayatta kalan tarla dalı.
Banned listesine `"tending the field"` ve `"harvesting\""` eklenir; `GUESS(farm)` emeklilik
etiketleri SİLİNMİŞ olmalıdır (W32'nin "surviving guess must carry GUESS(<slice>)" sözleşmesi:
farm dilimi indiğinde farm etiketi kalamaz; başka dilim tahminleri kalıyorsa `GUESS(` pini onlarla yaşar).

---

## 2. DEĞİŞMESİ GEREKEN Mevcut Testler (pin envanteri + yeni hikâyeleri)

Grep tabanı: `world.harvest | HarvestStep | HarvestHands | TryPlant | TryHarvest | FarmingJob |
PlantHarvested | shortage | BaselineHash | GUESS(`.

| # | Dosya | Bugünkü pin | Yeni beklenen hikâye |
|---|---|---|---|
| 1 | `Assets/Tests/EditMode/Process/HarvestSystemTests.cs` | `TryHarvest` ripe bitkiyi `InventoryState` stok + `createHarvestItem` fabrikasıyla DOĞRUDAN stoğa çevirir, soil temizler | Operasyon `FarmOps.TryHarvestAt`in iç uygulayıcısı olur: çıktı artık PILE değil AKTÖRÜN ELİdir; "unripe reddi + mutasyonsuzluk" pinleri AYNEN yaşar (F2'nin temelidir), yalnız çıktı adresi değişir. Stockpile-kapasite reddi deposit'e (Haul commit) taşınır. |
| 2 | `Assets/Tests/EditMode/Process/HarvestHandsServiceTests.cs` | "yakındaki köylü ELDİR / kimse yoksa plot bekler / düşman asla hasat etmez" yakınlık büyüsü | **Dosya ve servis ÖLÜR** (`HarvestHandsService.cs` silinir): hasatçı bulunmaz, hasatçı hasat AKSİYONUNUN sahibidir. "plot bekler" hikâyesi F2'ye, "en yakın kazanır" determinizmi decision'ın plot seçimine göçer; "düşman hasat etmez" decision'ın rol kapısında zaten yaşar (Player/Enemy elenir). |
| 3 | `Assets/Tests/EditMode/Process/PlantingSystemTests.cs` | `TryPlant` seed'i `InventoryState`ten (OYUNCU envanteri — teşhis §2.7) düşer, soil doldurur, event yazar | Operasyon `FarmOps.TryPlantAt`in içi olur: seed kaynağı REZERVE EDİLMİŞ stok birimidir, oyuncu envanteri değil. "seed yoksa/soil doluysa mutasyonsuz false" pinleri aynen kalır; `PlantPlanted` olayına aktör kimliği eklenir (F1 yazarlık izinin temeli). |
| 4 | `Assets/Tests/EditMode/Process/FarmingJobIntegrationTests.cs` | 5101/5102 tarifli job'lar worksite'a atanır / worksite yoksa bekler | Hayalet tarifler (`ProductionRecipeRegistry`de kayıtsız 5101/5102 — B05 yarası) yol OLMAKTAN çıkar: planting job'ı decision'ın Plant intent'ine dönüşür. Dosya "job → intent köprüsü" testine döner: post edilmiş planting job'lı dünyada farmer'ın `CurrentIntent == Plant` olduğu pinlenir; kesin köprü biçimi DOC 02'nin malı. |
| 5 | `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` | Kanonik liste (`:66-87`) `Daily:25:world.harvest` içerir | Satır ya SİLİNİR ya `world.harvest_jobs` (yalnız job/intent POSTER, stok yazarı DEĞİL) olur — karar DOC 03'ün; liste hangi biçimde olursa olsun "Daily:25 doğrudan stok yazarı" satırı ölür. `PerTick:18/22` id'leri DEĞİŞMEZ (farm aynı iki banda biner). |
| 6 | `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` | Gate1 "stok DONMASIN + meals≥3/villager"; Gate2 sapma üçlüsü; Gate3 olay karması `PlantPlanted/PlantHarvested/ShortageDetected` sayar | İddia metinleri AYNEN yaşar — ama Gate1 artık ZİNCİRE bağlıdır: stok akışı Daily:25 hediyesi değil, ekim+hasat+taşıma bölümlerinin toplamıdır. Kırmızıya dönerse eşik DEĞİL cadence yanlıştır (W32 Gate1 dersi). Gate3 olay sayısı artar (aksiyon terminalleri eklenir) — alt sınır pini kendiliğinden GÜÇLENİR. |
| 7 | `Assets/Tests/EditMode/Composition/WorldLivesOverNTicksTests.cs` | Yorum+iddia: "HarvestStep replants ripe crops at seed the same day" — seed'e dönüş canlılık kanıtı sayılır | Ters döner: ripe bitki HASAT EDİLENE KADAR ripe kalır; yeniden ekim kıtlık kaskadının YENİ bir Plant bölümüdür. Aşama-izleme iddiası "stage ilerledi VEYA hasat edildi (PlantHarvested izi)" biçimine güncellenir; W30 başlık yorumuna tarih notu düşülür. |
| 8 | `Assets/Tests/EditMode/Composition/WorldNpcDailyRhythmTests.cs` | Fixture `FarmingJobRequestFactory.CreatePlantingJob` ile iş kurar (`:124`) | Fixture yeni kuruluma göçer (soil hücresi + Plant intent'li farmer). Ritim eşikleri İNCELEME işaretli: tarla yürüyüşleri artık MoveToPlot fazından gelir — kırmızıysa örnekleme saati değil faz cadence'ı yanlıştır (W32 satır 13 dersi aynen). |
| 9 | `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` | `BaselineHash = "7ed20befd1bf5e68..."` (`:40`) | **Zorunlu re-baseline** — meşru tarih değişimi: hasat zamanlaması Daily:25 sınırından bölüm tamamlanma tick'lerine kayar, yeni olaylar loglanır. Prosedür dosya geleneği: ÖNCE aynı-seed çift koşum birebirken yakala, SONRA hash'i tarih+sebep yorumuyla değiştir ("W33 FARM: teleport hasat öldü; yield aksiyon commit'inde"). EN SON adım — tüm davranış otururken BİR kez (W32 sıra kuralı). |
| 10 | `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` (+ `FieldOwnershipRegistry.cs:54-60`) | `World.Stockpiles` yazarları: `world.harvest@Daily:25`, `living.action_advance@PerTick:22`, ambient, trade | `world.harvest@Daily:25` satırı SİLİNİR (stok yazarı olmaktan çıkar); `World.Plants` ve `World.Soils` YENİ kayıt satırları alır: `living.action_advance@PerTick:22` (plant/harvest commit) + `econ.plantgrowth@Daily:20` (büyüme). Test azalan/yeni yazarları pinler — çok-yazarlılık lint edilen gerileme kalır. |
| 11 | `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` + `SaveLoadDigestRoundtripTests.cs` + `ActorActionState.TryRestore` aralık pinleri | Temsilî dünyada uçuş-ortası EAT durumu; `TryRestore` `action > ConsumeFood` ve `intent > Eat`i REDDEDER | `TryRestore` üst sınırları yeni enum uçlarına genişler (eski save'ler etkilenmez — append-only). Temsilî dünyaya uçuş-ortası FARM durumu eklenir: `HaulToStockpile@progress` + eldeki crop + canlı plot claim'i. `WorldStateDigest.Compute` sözleşmesi eldeki birimleri ve plot claim'lerini de içermek ZORUNDA — yoksa digest roundtrip yarım-taşıma kaybını GÖREMEZ. |
| 12 | `Assets/Tests/EditMode/Composition/ActionPhaseChunkingInvarianceTests.cs` | 2 gün ufuk, yalnız EAT bölümleri | F6'nın kendisi: ufuk 4 gün + iki vacuous guard. Chunk seti ve eşitlik iddiası HARF HARF aynı kalır. |
| 13 | `Assets/Tests/EditMode/Presentation/VisualLayer/ActivityLabelTruthTests.cs` | 3 EAT fiili + `GUESS(` emeklilik-etiket sözleşmesi | F7'nin kendisi: 4 yeni satır; banned listesi büyür; `GUESS(farm)` etiketleri silinmiş olmalı. `ColonyNeedsSnapshotTests` yalnız GÖZDEN GEÇİRME (aktivite alanı taşıyorsa kaynak zaten tablo). |
| 14 | `Assets/Tests/EditMode/Composition/CatchupPerfPinTests.cs` + `LiveScaleCatchupPerfPinTests.cs` | 14 gün < 5 sn; 800 sivil 1 gün < 3 sn | Eşik ve metin AYNEN KALIR (W30e dersi). Sıcak döngüye farm kararı eklenir: plot/soil taraması FoodPileCache deseniyle O(aktör) tutulmak ZORUNDA — bu iki test onun bekçisidir; yorum blokları yeni sıcak yola göre güncellenir. |

**Değişmeyen bekçiler (bilinçli):** `Actions/Eat*` T1–T8 ailesi, `Living/EatActionStoryTests`,
`PlantGrowthSystemTests` (büyüme kuralları bu dilimde DOKUNULMAZ — yalnız çağıran değişir),
`PlantDefinitionTests`, `PlantSeasonRoundTripTests`, `CadenceChunkingInvarianceTests` (metin değişmez,
pin kendiliğinden güçlenir), Gate4/5/8 ve `GateContractLintTests`. Fallback/proof harness
(`--ember-proof-screenshots`) sözleşmesi değişmez; Gate'ler yeşilken harness da yeşildir.

---

## 3. Çalıştırma Sırası (uygulama haftası için)

1. F1–F7 önce KIRMIZI yazılır (derleme için gereken `Debug_Set`/`FarmSliceWorld` kancaları DOC 01–03'e sipariştir).
2. Dilim kodu indikçe sıra: F3 (plot claim) → F1 (ekim yazarlığı) → F2 (teleportun ölümü) → F4 (haul korunumu)
   → F7 (etiket) → F6 (chunking) → F5 (kapak taşı — zincirin tamamı otururken).
3. Bölüm-2 tablosu tek PR'da: #1–4 yeniden yazım, #5/#10 liste düzeltmesi, #9 re-baseline EN SON.
4. Yeşil tanımı: tüm suite + F1–F7 + re-baseline'lı golden, aynı-seed çift koşum birebirken.

## 4. Sonraki Dilime Devreden Maddeler (bilinçli kapsam dışı)

- "Aktör worksite dışında işi ilerletemez" + craft input/output → WORK/CRAFT dilimi (5101/5102'nin
  tarif kaydı sorunu da orada kökten çözülür ya da tarifler tamamen aksiyona göçer).
- Uyku toparlanması → SLEEP dilimi (NeedConsumptionSystem gece yarısı yine dokunulmadan kaldı).
- Crop→meal dönüşümü (öğün pişirme) → CRAFT dilimi; bu dilimde stok crop'u doğrudan yenilebilir sayılır.
- Genelleşen kalıplar: F4 (korunum), F3 (claim çakışması) ve F6 (vacuous-guard'lı chunking izi) şablonları
  üçüncü kez göründüklerinde parametrikleştirilir — `ActorActionType` büyüdükçe test şablonu sabit kalır.

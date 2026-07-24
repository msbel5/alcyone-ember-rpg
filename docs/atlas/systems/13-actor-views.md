# 13-actor-views

> Kapsam: aktor gorselleri — `EmberGeneratedActorSpawner`, `WorldViewProjector`, `ActorView`
> (glide/snap/wander), `BillboardGroundingView`, `GeneratedNpcAccessibilityGuard`, paper-doll
> tint/gear katmani ve billboard uydu-bilesenleri.
> Ana dizin: `Assets/Scripts/Presentation/Ember/Views/` (14 dosya).
> Tum satir referanslari 2026-07-24 calisma kopyasina gore dogrulandi.
> UYARI: bu taramada calisma kopyasinda VE HEAD'de derlemeyi kirmasi gereken bir govde bulundu
> (`WorldViewProjector.ReplaceActorViews` — borclar #1). Editorun canli derleme durumu bu
> oturumda dogrulanmadi; asagidaki akis anlatimi kodun NIYETINI anlatir.

## HLD - Ne ve Neden

Bu sistem, yasayan dunyanin GOZLE GORULEN yuzudur: worldgen `WorldState.Actors`'a ~750 NPC
hidrate eder ama sceneler yalnizca ~5 el-yapimi `ActorView` yazar — geri kalani gorunmezdi
(`EmberGeneratedActorSpawner.cs:14-16`). Cozum uc katmandir: (1) **spawner** oyuncuya en yakin
N adayin her biri icin calisma zamaninda bir billboard `ActorView` insa eder ve uzerine stable
actor id'yi damgalar (`EmberGeneratedActorSpawner.cs:17-21`); (2) **projector** her tick sim
kaydini `ActorViewState`'e cevirip view'a iter (`WorldViewProjector.cs:41-53`); (3) **ActorView**
o hedefe dogru interpolasyon yapar — yuruyuscu NPC'ler sabit m/s ile "glide" eder, 5 m'den buyuk
sicramalar "snap"tir, siviller ayrica kozmetik "wander" ile cevrelerinde dolanir
(`ActorView.cs:196-206`, `:188-194`). Gorsel dil bilinclidir: Daggerfall tarzi, kameraya donen
2D billboard'lar (`CameraFacingBillboard.cs:5-8`). Oyuncuya gorunen etki: kasaba sokaklari
yuruyup calisan, ogle yemegine oturan, gece yatan, vurulunca kirmizi yanip dusen bir kalabalik.
Felsefe kati bir tek-yon kuraldir: bu katman sim'e ASLA geri yazmaz — wander/jitter icin
`UnityEngine.Random` kullanimi bile "presentation-only, never feeds Domain/Simulation or the
save" diye isaretlidir (`ActorView.cs:91-95`, `:262-264`; `docs/DETERMINISM.md` referansiyla).
Paper-doll v1 de ayni ucuzluk felsefesindedir: ayni base sprite, aktor-id'den deterministik
kumas tintiyle farkli "cast" giyer; rol-gercek gear isaretleri ayri overlay katmanindadir ve
forge'a asla tam figur re-render ettirilmez (`NpcVariantTintService.cs:3-7`,
`BillboardGearMarkView.cs:5-10`).

## HLD - Akis

1. **Boot (host Start yolu)**: `EmberWorldHost` once sahnedeki el-yapimi `ActorView`/`WorksiteView`
   setini toplar (`EmberWorldHost.cs:159-160`), sonra `EnsureGeneratedActorSpawner().SpawnMissingNearbyActors()`
   cagirir; yeni billboard dogduysa view setini YENIDEN tarar (`EmberWorldHost.cs:168-169`).
   Spawner tekil olarak host GameObject'ine eklenir (`EmberWorldHost.Ui.Overlay.cs:123-126`).
2. **Projector kurulumu**: `_worldViewProjector = new WorldViewProjector(_clock, _worldView,
   actorViews, worksiteViews, eventLogHud)` (`EmberWorldHost.cs:171`), ardindan ilk `Project()`
   (`EmberWorldHost.cs:174`).
3. **Spawn tek-cagri govdesi** (`EmberGeneratedActorSpawner.cs:76-145`): read model'den aday
   listesi (`:78-82`), settlement degistiyse bayat billboard supurmesi (travel sonrasi eski
   sehrin adamlari ufka yurumesin — `:84-106`), sahnede zaten id tasiyan view'larin toplanmasi
   (`:110`), oyuncu ankraji (`PlayerRig` → `Camera.main` → origin, `:309-324`), filtre + kare-mesafe
   sirala + `RuntimeNpcDensity.CapOrDefault(_maxSpawnCount)` ile kirp (`:117-133`;
   `RuntimeNpcDensity.cs:12` — Inn az, City kalabalik), her aday icin `SpawnOne` (`:136-140`),
   en az bir dogum olduysa `EmberWorldHost.RescanActorViews()` (`:141-143`).
4. **SpawnOne insa sirasi** (`EmberGeneratedActorSpawner.cs:147-251`): root GameObject +
   ring-scatter offset (`:158-161`, offset matematigi `:338-357`); `NightCurfewView` + rol-string'den
   hostile tespiti (`outlaw|bandit|monster_*` — `:164-171`); "Billboard" cocugu (ad SOZLESMEDIR,
   `ActorView.Awake` bu adla baglar — `:173-177`, `ActorView.cs:110-111`); sprite cozumu
   library → bestiary siluet → notr gri quad (`:180`, `:371-385`); rol-bazli boy
   (`BestiaryBillboardSpriteFactory.TargetHeightFor`, kurt kalca boyu — `:183-184`);
   **paper-doll tint** feedback bind'dan ONCE (`:187-190`); `ActorCombatFeedbackView.Bind`
   (`:193`); `BillboardGearMarkView.TryAttach` (`:194`); `NpcEventEchoView` (`:195`);
   `BillboardWalkAnimView` (`:197`); hostile'a kirmizi elmas marker (`:199-202`, `:256-271`);
   `ActorView` + `BindDomainActorId` + `SetGroundSpeed(hostile ? 3.4 : 1.3)` (`:207-212`);
   YALNIZ sivillere `EnableWander(0.8)` + `NpcPoseIconView` + `NpcActivityLabelView`
   (`:219-229`; F18: canavar amacla kovalar, milleme sarhos gosteriyordu — `:217-218`);
   `GeneratedNpcAccessibilityGuard` (`:230`); `BillboardGroundingView` (`:233`);
   `EmberInteractable.Setup(name, "General", id)` + kisi-boyu BoxCollider (E-etkilesimi icin —
   `:238-246`).
5. **Streaming respawn**: spawner `Update`'i 2.5 s'de bir oyuncu 40 m'den fazla yer degistirdiyse
   `SpawnMissingNearbyActors`'i yeniden kosar — "quest 250m diyor ama orasi bos" fix'i,
   Daggerfall tarzi yuruyerek belirme (`EmberGeneratedActorSpawner.cs:290-307`).
6. **Tick kadansi**: `EmberWorldHost.OnTick` → `ProjectTick(tickIndex)`
   (`EmberWorldHost.Bindings.cs:44-47`) → sirasi SABIT: clock ilerlet, `Project()`, event-log
   HUD'a son 64 `WorldEvent`'i render et (`WorldViewProjector.cs:64-69`).
7. **Project dongusu** (`WorldViewProjector.cs:41-61`): her `ActorView` icin id-anahtarli
   (`TryReadActor(ActorId)`) ya da legacy ad-anahtarli (`TryReadActor(string)`) okuma → `SetTarget`
   (`:47-52`); null (site degisiminde yok edilmis) view atlanir (`:46`). Worksite'lar AD ile
   okunur (`:55-60`).
8. **Tick-arasi tazeleme**: gercek-zamanli kovalama adimlari (1 hucre/0.45 s) tick BEKLEMEDEN
   `ProjectWorldViewsNow()` ile view hedeflerini tazeler — clock ilerletmeden `Project()`
   (`EmberWorldHost.Bindings.cs:49-52`; cagiran: `InGameUiController.cs:248`).
9. **Kare kadansi (Update/LateUpdate yigini)**: `ActorView.Update` pozisyon interpolasyonu +
   yürüme bob/lean + idle float + combat tint/shake (`ActorView.cs:182-266`). Sonra LateUpdate
   katmani: `CameraFacingBillboard` yaw-donusu (`CameraFacingBillboard.cs:16-32`),
   `BillboardGroundingView` 4 Hz raycast ile y-zemine oturtma (`BillboardGroundingView.cs:18-51`),
   `GeneratedNpcAccessibilityGuard` bina kabuklarindan disari itme (`GeneratedNpcAccessibilityGuard.cs:16-35`).

## LLD - Veri Modeli

**SpawnableActor** (`IDomainSimulationAdapter.cs:106-124`) — sunum-yalitimli duz DTO:
`Id: ulong` (stable actor id'nin ham hali), `Name: string`, `SpriteRole: string`,
`WorldX/WorldZ: float` (adapter tarafinda ONCEDEN projekte edilmis — spawner Domain matematigi
yapmaz), `Seed: int`. Uretici: `DomainSimulationAdapter.GetSpawnableActors`
(`DomainSimulationAdapter.WorldProjection.cs:150-178`) — Player haric, olu haric ("olu ayakta
respawn olmaz" — `:164-165`), YALNIZ mevcut settlement sakinleri ("baska sehrin adami burada
belirdi" fix'i — `:154-159`).

**ActorViewState** (`ActorView.cs:270-288`) — tick basi gorsel anlik goruntusu:
`WorldPosition: Vector3`, `WorldRotation: Quaternion`, `Visible: bool`,
`Activity: string` (playtest: "ne yaptigi anlasilmiyor" — `:275-276`),
`Sleeping: bool` (eve VARINCA true; yatma pozu commute'u bekler — `:277-279`).

**WorksiteViewState** (`WorksiteView.cs:45-54`): `IsActive: bool`, `QueueDepth: int`
(QueueDepth'i hicbir gorsel kullanmiyor — `ApplyEmission` yalniz IsActive okur, `WorksiteView.cs:31-42`).

**EmberGeneratedActorSpawner durumu** (`EmberGeneratedActorSpawner.cs`):
- `_maxSpawnCount = 24` (`:51`), `_billboardTargetHeight = 2.1f` (`:56`), `_spawnSpacing = 1.5f` (`:62`).
- `_spawnedIds: HashSet<ulong>`, `_spawnedRoots: Dictionary<ulong, GameObject>` (`:66-67`),
  `_spawnedForSettlement: ulong` (travel supurme anahtari — `:68`).
- `LoggedSpriteResolutions: static HashSet<string>` (rol|kaynak|yol bazli tek-seferlik log — `:64`).
- Streaming sabitleri: `ScanIntervalSeconds = 2.5f`, `ScanMoveThresholdMeters = 40f` (`:296-297`).

**ActorView durumu** (`ActorView.cs`):
- Serialized: `_domainActorKey` (`:37`), `_domainActorId` — ActorId ulong'u inspector
  serialize edemedigi icin STRING tasinir (`:38-42`), `_interpolationSpeed = 8` (`:43`),
  `_billboard` (`:44`), yurume/idle parametreleri (`:46-49`).
- Runtime: `_target/_hasTarget` (`:82-83`), wander alanlari (`_wanderRadius`,
  `_wanderSpeed = 0.6f`, hedef/repath sayaci — `:96-101`), `_groundSpeed` (0 = ussel chase,
  >0 = sabit m/s glide — `:103-106`), `ExternalPoseOverride` (uyku/olum pozu transformu
  sahiplenir — `:121-122`).

**Paper-doll tinti** (`NpcVariantTintService.cs`): `MinChannel = 0.80f` (`:11`);
`TintFor(ulong)` splitmix64-tarzi hash'ten R/G/B'yi 0.80..1.00 araligina indirger (`:13-24`) —
sanat okunur kalir, yalniz "cast" degisir. Simulation.AiDm namespace'inde ama saf fonksiyondur,
sim durumuna dokunmaz.

**Feed'ler (stamp-not-consume deseni)**:
- `WorldCombatFeedbackFeed` (`WorldCombatFeedbackFeed.cs:9-41`): `HitStamp/HitTargetId/HitMaterial`,
  `FelledStamp/FelledTargetId`, `EnemyStrikeStamp/EnemyStrikeId` — cok view ayni feed'i okur,
  kimse "tuketmez", herkes son gordugu stamp'i hatirlar (`:3-8`).
- `NpcEventEchoFeed` (`NpcEventEchoFeed.cs:9-49`): 128'lik ring buffer + artan `Stamp`;
  kind sabitleri Witness=0, Report=1, Guard=2, Harvest=3, Talk=4 (`:11-15`).
- `RuntimeFieldMirror.HourOfDay` (`RuntimeFieldBuilder.cs:14`) — poz ikonlarinin saat kaynagi.
- `RuntimeNpcDensity.Cap` (`RuntimeNpcDensity.cs:10-12`) — director realize'da yazar, spawner okur.

**BestiaryBillboardSpriteFactory** (`BestiaryBillboardSpriteFactory.cs`): 5 canavar tipi icin
piksel-maske siluet (`:16-27`) + tip-bazli boy tablosu (kurt 1.2, orumcek 0.9, iskelet 2.0,
hayalet 2.2, haydut 2.0 — `:31-42`); sprite'lar static alanlarda sonsuza dek cache'lenir (`:14`).

**BuildingAccessibilityVolume** (`BuildingAccessibilityVolume.cs:10-46`): yari-boyut + marj
(`Configure`, `:16-21`); `TryPushOutside` icerideki noktayi EN YAKIN kenara iter (`:23-45`).

## LLD - Fonksiyon Haritasi

Spawner (`EmberGeneratedActorSpawner.cs`):
- `int SpawnMissingNearbyActors()` — `:76-145` — idempotent, capli, settlement-supurmeli tek dogum kapisi; donen sayi > 0 ise host re-scan yapar.
- `bool SpawnOne(SpawnableActor, int spawnIndex)` — `:147-251` — bir billboard'in tum bilesen zinciriyle insasi (HLD adim 4).
- `static void AddHostileMarker(Transform)` — `:256-271` — kafa ustune kirmizi elmas quad; collider'i YOK EDILIR ki interact raycast'ini yemesin.
- `static HashSet<ulong> CollectExistingViewIds()` — `:275-285` — sahnedeki tum ActorView id'leri (cift-dogum kilidi).
- `static Vector2 ResolvePlayerAnchorXZ()` — `:309-324` — PlayerRig → Camera.main → origin fallback zinciri.
- `Vector2 SpawnOffset(int index)` — `:338-357` — es-merkezli kare halkalarda (8r slot) deterministik sacilim.
- `static void FitBillboardToPlayableHeight(Transform, SpriteRenderer, float)` — `:361-368` — hedef boy / sprite boyu, clamp 0.02..3.
- `Sprite ResolvePlaceholderSprite(SpawnableActor)` — `:371-385` — library → bestiary siluet → notr fallback siralamasi.
- `static Sprite ResolveGeneratedSprite(SpawnableActor)` — `:387-407` — `GeneratedNpcBillboardResolver.TryResolveRecord` + `GeneratedCoreSpriteLoader` yollari (`GeneratedNpcBillboardResolver.cs:5,23`; `GeneratedCoreSpriteLoader.cs:26,42`).
- `Sprite GetOrCreateFallbackSprite()` — `:421-441` — 64px, 64 PPU notr gri (1x1 dev magenta'nin panzehiri).

Projector (`WorldViewProjector.cs`):
- ctor `WorldViewProjector(IEmberSimulationClock, IWorldViewReadModel, ActorView[], WorksiteView[], EventLogHudPanel)` — `:17-27` — DIKKAT: govde yalniz `_clock/_worldView/_actorViews` atar; `_worksiteViews`/`_eventLogHud` atamasi YANLIS metodda (borclar #1).
- `void ReplaceActorViews(ActorView[])` — `:33-38` — gec dogan view'lari sync setine alir ("drifted 21m" invariant fix'i); su anki govdesi derlenemez durumda (borclar #1).
- `void Project()` — `:41-61` — id/ad-anahtarli aktor okumasi + `SetTarget`; ad-anahtarli worksite okumasi + `SetState`.
- `void ProjectTick(int)` — `:64-69` — advance → Project → event log render (eski tick sirasi AYNEN korunur).

ActorView (`ActorView.cs`):
- `bool TryGetDomainActorId(out ActorId)` — `:60-67` — string alanin ulong parse'i; 0 ve bos gecersiz.
- `void BindDomainActorId(ActorId)` — `:77-80` — runtime dogumlarin SerializedObject muadili.
- `void SetTarget(ActorViewState)` — `:124-136` — hedefi yazar + `Activity`'yi label'a, `Sleeping`'i curfew'a iletir (lazy component probe `:128-133`).
- `void EnableWander(float radius)` — `:148-155` — kozmetik milleme; rastgele ic-nokta baslangici.
- `void SetGroundSpeed(float)` — `:166-169` — >0 iken sabit m/s glide modu.
- `void Apply(int)` (IDamageSink) — `:171-180` — legacy hasar geri bildirimi: 0.2 s tint+shake + adapter.LogCombat.
- `void Update()` — `:182-266` — (1) glide ≤5 m / snap >5 m / ussel Lerp uclu interpolasyonu (`:196-206`), (2) hiz-esikli yurume bob/lean vs idle float, `ExternalPoseOverride` bekcisiyle (`:216-245`), (3) combat tint/shake (`:247-265`).

Uydu bilesenler:
- `CameraFacingBillboard.LateUpdate` — `CameraFacingBillboard.cs:16-32` — yaw-only kameraya donus.
- `BillboardGroundingView.LateUpdate` — `BillboardGroundingView.cs:18-51` — +60 m'den 160 m asagi RaycastAll, kendi hiyerarsisi/aktorler/Roof-Canopy-mobilya haric EN YUKSEK yuzeye snap; 4 Hz.
- `GeneratedNpcAccessibilityGuard.LateUpdate` — `GeneratedNpcAccessibilityGuard.cs:16-35` — tum volume'lara karsi `TryPushOutside`; volume listesi 1 s'de bir `FindObjectsByType` ile tazelenir (`:37-44`).
- `BillboardWalkAnimView.Bind/Update` — `BillboardWalkAnimView.cs:22-56` — hareket varken 0.28 s kadansta flipX ayna-frame + %5 squash.
- `static BillboardGearMarkView.TryAttach(GameObject, string)` — `BillboardGearMarkView.cs:15-34` — guard/knight'a mizrak, outlaw/bandit'e kilic pictogrami (12px maske, sortingOrder 11).
- `NpcPoseIconView.Bind/Update` — `NpcPoseIconView.cs:20-44` — `HourOfDay`'den cekic (is) / kupa (ogle) ikonu; 1.1 s poll.
- `NpcActivityLabelView.Bind/SetActivity/Update` — `NpcActivityLabelView.cs:19-55` — TextMesh fiil etiketi; CameraFacingBillboard KULLANMAZ (ayna yazi "gnilbi" bugi — `:33-36`), 22 m cull.
- `NpcEventEchoView.Bind/Update` — `NpcEventEchoView.cs:21-47` — echo feed'den aktore ozel son olay pictogrami, 3.5 s gosterim, 0.4 s poll.
- `ActorCombatFeedbackView.Bind/Update/Fall` — `ActorCombatFeedbackView.cs:23-93` — hit flash (unscaled: combat modali timeScale'i durdurur — `:8-10`), enemy-strike lunge (`:56-79`), olum pozu `Fall` (`:83-93`, `ExternalPoseOverride = true`).
- `NpcVariantTintService.TintFor(ulong)` — `NpcVariantTintService.cs:13-24`.

Host baglantilari:
- `EmberWorldHost.OnTick` — `EmberWorldHost.Bindings.cs:44-47`; `ProjectWorldViewsNow` — `:52`; `RescanActorViews` — `:55-57` (FindObjectsByType ile tum ActorView'lar → `ReplaceActorViews`).
- `EmberWorldHost.EnsureGeneratedActorSpawner` — `EmberWorldHost.Ui.Overlay.cs:123-126`.
- Adapter okuma yuzeyi: `TryReadActor(string)` — `DomainSimulationAdapter.WorldProjection.cs:23`; `TryReadActor(ActorId)` — `:40`; `GetSpawnableActors` — `:150-178`; `CurrentSettlementKey` — `DomainSimulationAdapter.Travel.cs:29`; bos adapter muadilleri `UnavailableSimulationAdapter.cs:46-50`.

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` dilinde: bu sistemin `WorldState` uzerinde HICBIR yazimi yoktur ve
registry'de kaydi yoktur — tamamen Presentation. `Actor.Position`'in beyanli yazarlari sim
sistemleridir (bkz. 03-schedule-movement); bu katman o alanin yalnizca PROJEKSIYONUNU okur.

**Yazdigi (Unity sahne durumu — registry disi):**
- View root `Transform.position` — UC ayri yazar, kare icinde zincir halinde:
  `ActorView.Update` (sim hedefine interpolasyon — `ActorView.cs:196-206`),
  `BillboardGroundingView.LateUpdate` (y — `BillboardGroundingView.cs:49-50`),
  `GeneratedNpcAccessibilityGuard.LateUpdate` (x/z disari itme — `GeneratedNpcAccessibilityGuard.cs:33-34`).
  Bu uclunun sozlesmesi ORTULUDUR: siralama Update-vs-LateUpdate'e ve bilesen sirasina dayanir,
  hicbir registry/lint korumaz (borclar #6).
- Billboard cocugunun `localPosition/localRotation/flipX/color/scale` — yazarlar:
  `ActorView.Update` (bob/lean/idle + tint restore — `:222-256`), `BillboardWalkAnimView` (flipX+squash —
  `BillboardWalkAnimView.cs:49-55`), `ActorCombatFeedbackView` (flash/lunge/olum pozu —
  `ActorCombatFeedbackView.cs:46-47`, `:64-79`, `:88-92`), `NightCurfewView` (yatma pozu —
  `NightCurfewView.cs`). Tek hakem `ExternalPoseOverride` bayragi (`ActorView.cs:122`, `:216-221`)
  ve o da yalniz ActorView'un kendi yazicilarini susturur (borclar #2-#3).
- `ActorView._domainActorId` — `BindDomainActorId` (`ActorView.cs:77-80`), yazan: spawner `:208`.
- `RuntimeNpcDensity.Cap` — bu sistem OKUR (`EmberGeneratedActorSpawner.cs:132`); yazari
  WorldSceneDirector'dur (`RuntimeNpcDensity.cs:4-6`).

**Okudugu:**
- `WorldState.Actors` — dolayli, `IWorldViewReadModel.TryReadActor` ve `GetSpawnableActors`
  uzerinden (`WorldViewProjector.cs:48-50`; `EmberGeneratedActorSpawner.cs:78-81`).
- `CurrentSettlementKey` — travel supurme anahtari (`EmberGeneratedActorSpawner.cs:89-105`).
- `WorldCombatFeedbackFeed.*Stamp` ve `NpcEventEchoFeed.Stamp/ring` — asagida.
- `RuntimeFieldMirror.HourOfDay` (`NpcPoseIconView.cs:37`).
- Sahne fizigi (`Physics.RaycastAll` — `BillboardGroundingView.cs:27`) ve `Camera.main`
  (`CameraFacingBillboard.cs:20`, `NpcActivityLabelView.cs:46`, `ActorCombatFeedbackView.cs:66`).

## LLD - Urettigi/Tukettigi Olaylar

**Urettigi:**
- `WorldEventKind` URETMEZ — sim'e geri olay yazan tek yol yoktur.
- Log tag `[NpcBillboardResolve]`: her rol|kaynak|yol kombinasyonu icin bir kez, sprite'in
  library/core/siluet/notr hangi kaynaktan geldigini soyler (`EmberGeneratedActorSpawner.cs:409-414`).
- `adapter.LogCombat("{name} takes {n} damage!")` — legacy `IDamageSink.Apply` yolundan
  (`ActorView.cs:171-180`).

**Tukettigi:**
- `WorldCombatFeedbackFeed`: `HitStamp/HitTargetId` (flash + `RuntimeHitSparks.Burst` —
  `ActorCombatFeedbackView.cs:35-44`), `FelledStamp/FelledTargetId` (olum pozu — `:49-54`),
  `EnemyStrikeStamp/EnemyStrikeId` (lunge tell — `:58-63`).
- `NpcEventEchoFeed.LatestKindFor(actorId, sinceStamp)` — goz/unlem/kilic/demet/konusma
  pictogramlari (`NpcEventEchoView.cs:41-46`).
- `IWorldViewReadModel.RecentWorldEvents(64)` — projector her tick event-log HUD'ina verir
  (`WorldViewProjector.cs:68`); anlatici `WorldEventNarrator` (`:15`).
- `ActorViewState.Activity/Sleeping` — `SetTarget` icinden label ve curfew'a dagitilir
  (`ActorView.cs:134-135`).

## Testler

- `Assets/Tests/EditMode/AiDm/NpcVariantTintServiceTests.cs` — paper-doll tint pinleri:
  ayni id ayni tint (kararlilik), kanallar 0.80..1.00 (okunurluk), 40 koyluden ≥30 farkli tint
  (cesitlilik).
- `Assets/Tests/EditMode/GeneratedAssetLibrary/GeneratedNpcBillboardResolverTests.cs` —
  spawner'in sprite cozum zincirinin kayit-cozumleme yarisini pinler (komsu sistem).
- DIKKAT — ad cakismasi: `Assets/Tests/EditMode/World/WorldStateActorViewTests.cs` BU SISTEMI
  test ETMEZ; Domain'deki `WorldState.ReplaceActorView(ActorRole, ...)` rol-kayit API'sini test
  eder. "ActorView" adi iki ayri kavram icin kullaniliyor (aile (a) kokusu, ad-tek-otorite yok).
- **Bosluk**: `EmberGeneratedActorSpawner`, `WorldViewProjector`, `ActorView` interpolasyonu,
  `BillboardGroundingView`, `GeneratedNpcAccessibilityGuard`, curfew/feedback/echo/pose/label
  bilesenlerinin HICBIRINI pinleyen test yok (hepsi MonoBehaviour; EditMode kapsami sifir).
  Projector'daki derleme-kiran govdenin (borclar #1) yakalanmamis olmasi bu boslugun dogrudan
  sonucudur.

## Bilinen Borclar + Kacak Kapilari

Aile harfleri `docs/SYSTEMS_ATLAS.md:52-60`'taki (a)-(g) siniflamasina gore.

1. **KRITIK — `WorldViewProjector.ReplaceActorViews` derlenemez govde tasiyor** [aile (f):
   edit/durum-sifirlama kazasi]. Kurucu yalniz `_clock/_worldView/_actorViews` atar
   (`WorldViewProjector.cs:24-26`); `_worksiteViews = worksiteViews ?? ...` ve
   `_eventLogHud = eventLogHud` satirlari `ReplaceActorViews`'un ICINDEDIR (`:36-37`) — orada
   `worksiteViews`/`eventLogHud` diye parametre YOK (CS0103) ve iki alan `readonly`'dir
   (`:13-14`, kurucu disinda atama CS0191). Gorunen o ki kurucunun iki satiri metoda
   tasinmis/yapistirilmis. HEAD'deki icerik de ayni (commit `23367666` ile karsilastirildi) —
   yani bu bir calisma-kopyasi yariminligi degil, COMMIT EDILMIS kirik. Unity editorunun su anki
   canli derleme durumu bu oturumda dogrulanmadi. Naif duzeltme tuzagi: iki satiri sadece
   silmek derler ama `_worksiteViews` null kalir ve `Project` `:55`'te NRE atar — dogru
   duzeltme satirlari kurucuya GERI tasimaktir.
2. **Paper-doll tinti icin cift renk-yazari, sira tanimsiz** [aile (c) analogu, view katmaninda].
   `ActorView.Update` her kare kosulsuz `_renderer.color = Color.white` restore eder
   (`ActorView.cs:253-256`); `ActorCombatFeedbackView.Update` ayni SpriteRenderer'a her kare
   `_baseColor` (spawn'da tintlenmis renk) yazar (`ActorCombatFeedbackView.cs:46-47`).
   Karenin son rengi hangi Update'in sonra kostuguna baglidir; Script Execution Order
   tanimlanmamistir. Feedback view sonra kosuyorsa tint yasar, once kosuyorsa spawner'in
   `:189-190`'da uyguladigi kumas tinti her kare BEYAZA ezilir. Hangi siranin gercekte
   kostugu dogrulanmadi (canli olcum yok) — ama sozlesme yorumla bile korunmuyor.
3. **Cift gait-yazari: flipX'e iki animator birden dokunuyor**. `ActorView.Update` yurume
   dongusunde 0.4 s kadansla `_renderer.flipX` toggle'lar (`ActorView.cs:224-230`);
   `BillboardWalkAnimView` AYNI renderer'da 0.28 s kadansla toggle'lar
   (`BillboardWalkAnimView.cs:49-52`) — spawner IKISINI de takar (`EmberGeneratedActorSpawner.cs:197`,
   `:207`). Iki bagimsiz metronom ayni bayragi surdugunde efektif adim ritmi ikisinin
   girisimidir; F33'un "0.28 s adim" niyeti sessizce bozulur. (Gorsel etkisi playtest'te
   raporlanmadi — dogrulanmadi.)
4. **Poz ikonlari saat TAHMINI, sim karari degil** [aile (a); 03-schedule-movement borc #3'un
   ayna yuzu]. `NpcPoseIconView` "12:00-13:59 lunch window, matching ScheduleSystem" iddiasiyla
   `HourOfDay`'den ikon secer (`NpcPoseIconView.cs:6-9`, `:37-43`) ama H2 utility selector'da
   artik sabit ogle penceresi yok — aclik erken/gec kazandiginda kupa ikonu yalan soyler.
   `NpcActivityLabelView` ise sim'in gercek fiilini kullanir (dogru yol).
5. **Rol-string ayristirma anlamsal veri tasiyor** [aile (a)/(g); `docs/SYSTEMS_ATLAS.md`'nin
   kendi 5 no'lu tespiti]. Hostillik `spriteRole.IndexOf("outlaw"/"bandit")` + `monster_*`
   onekiyle (`EmberGeneratedActorSpawner.cs:167-171`), iscilik `farmer|blacksmith|artisan`
   substringiyle (`:224-226`), gear marki `guard|knight` substringiyle
   (`BillboardGearMarkView.cs:18-21`) belirlenir. Tip bilgisi kaynak-id string'inde kacak
   yasiyor; yeni bir rol adi eklendiginde uc ayri dosyada uc ayri liste sessizce eksik kalir.
6. **View-root transformunda uc beyansiz yazar** [aile (c) on-kosulu]. Sync (Update),
   grounding (LateUpdate, y) ve accessibility guard (LateUpdate, x/z) ayni `transform.position`'i
   surer; guard bir NPC'yi bina disina iterken per-tick sync hedefi hala bina icindeyse aktor
   kapida titrer (iki yazar her kare zit yonde) — bu, "gecici kabuk, kapi/ic mekan gelene
   kadar" diye beyan edilmis bilinçli bir kacak kapisidir (`GeneratedNpcAccessibilityGuard.cs:6-9`,
   `BuildingAccessibilityVolume.cs:5-8`) ama titreme senaryosu olculmedi (dogrulanmadi).
7. **Zemin tespiti isim-string filtresiyle** [aile (d)/(g)]. Grounding raycast'i "Roof",
   "Canopy", "Table", "Bench", "Trestle" ISIMLERINI dislar (`BillboardGroundingView.cs:37-39`);
   yeni bir cati prefab'i farkli adla gelirse NPC catiya oturur. Ayrica komsu-aktor filtresi
   `GetComponent<EmberInteractable>` cagrisiyla her hit icin yapilir (`:36`) — 4 Hz'de kabul
   edilebilir, ama katman maskesi yerine tip/isim sorgusu iki kez kirilmis bir desenin devami
   ("iki NPC cakisinca biri digerinin ustune cikti" fix'i ayni bloktadir, `:33-35`).
8. **Bayat sayilar dokumantasyonda** [aile (f): guncellenmeyen defter]. Spawner ust yorumu
   "nearest `_maxSpawnCount` (6 by default)" der (`EmberGeneratedActorSpawner.cs:27`), alan 24'tur
   (`:51`); `ActorView` basligi "CAPPED to the nearest N (<=12)" der (`ActorView.cs:30-31`).
   Iki yorum da iki nesil eski.
9. **Worksite senkronu ad-anahtarli ve null-korumasiz**. Projector aktor dongusunde null view
   atlar (`WorldViewProjector.cs:46`) ama worksite dongusunde atlamaz ve `worksite.name` okur
   (`:55-58`) — site degisiminde yok edilen bir WorksiteView MissingReferenceException uretir
   (senaryo canli gorulmedi — dogrulanmadi). Aktorler id'ye tasinmisken worksite'larin hala
   sahne adiyla eslesmesi aile (a) tohumudur (`TryReadWorksite(worksite.name)`).
10. **Statik cache'ler ve domain-reload varsayimlari** [aile (g)]. `LoggedSpriteResolutions`
    (`EmberGeneratedActorSpawner.cs:64`), bestiary/gear/poz/echo sprite'lari
    (`BestiaryBillboardSpriteFactory.cs:14`, `BillboardGearMarkView.cs:13`, `NpcPoseIconView.cs:13-14`,
    `NpcEventEchoView.cs:13`) ve feed stamp'leri hepsi static — sahneler arasi yasarlar
    (istenen davranis) ama editor domain-reload kapaliyken oturumlar arasi da tasinirlar;
    feed stamp'lerinin yeni oyunda sifirlanmamasi "eski stamp'i hatirlayan view" sinifinda
    tek-seferlik hayalet efekt uretebilir (dogrulanmadi).
11. **`GameObject.Find("PlayerRig")` + `FindObjectsByType` taramalari**. Ankraj cozumu her
    2.5 s'lik scan'de isimle arama yapar (`EmberGeneratedActorSpawner.cs:311`), guard her
    saniye volume taramasi (`GeneratedNpcAccessibilityGuard.cs:41-43`), rescan tum sahneyi
    tarar (`EmberWorldHost.Bindings.cs:56-57`). Hepsi bilincli olarak dusuk-frekansli — ama
    hicbiri profillenmis bir butceye bagli degil; NPC sayisi buyudugunde LateUpdate'te
    aktor-basina-1 Hz FindObjectsByType (guard) ilk supheli olacaktir.
12. **`WorksiteViewState.QueueDepth` olu veri**. Sim uretir, projector tasir, gorsel katman
    hic okumaz (`WorksiteView.cs:31-42`) — kucuk ama "tasiyip kullanmama" aile (f) kokusu.

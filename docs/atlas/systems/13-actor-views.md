# 13-actor-views

## HLD - Ne ve Neden (5-10 cumle)

Aktör görselleri Domain'in `WorldState.Actors` kaydı ile sahnedeki 2D-billboard'lar arasındaki senkron katmanıdır (Daggerfall stili flat sprite in 3D). Katman iki iş yapar: (1) sim tarafından yürütülen konumu her tick billboard'a projekte etmek, (2) sahnede yazarlar tarafından authored edilmemiş worldgen NPC'lerini runtime'da doğurmak. Sahneler yalnızca ~5 authored `ActorView` içerir; `SOUL-01`'in hidrate ettiği ~750 worldgen NPC'si için görsel yoksa `EmberGeneratedActorSpawner` runtime'da billboard üretir ve stable `ActorId`'yi (`BindDomainActorId`) view'a damgalar. Bir kez damgalandıktan sonra `WorldViewProjector` id-keyed `TryReadActor` yolundan konumu okuyup `ActorView.SetTarget`'e verir. Presentation-only detaylar (wander, glide, tint, flash, lunge, fall, activity label, pose icon, event echo, gear mark, hostile marker, grounding, accessibility) burada yaşar; hiçbiri sim'e geri yazmaz; current authority `docs/recovery/CURRENT_STATE.md` altindadir. W30 iki büyük yara kapadı: (a) şehir değişince önceki settlement'ın billboard'ları sahnede kalıp "kilometrelerce" yürüyordu → `CurrentSettlementKey` eşiğinde stale billboard'lar despawn ediliyor; (b) host boot'unda tek seferlik cache'lenmiş `ActorView[]` streaming spawner'ları içermiyordu → `WorldViewProjector.ReplaceActorViews` + `EmberWorldHost.RescanActorViews` ile late-join sağlandı. W36 açık borç: B24 çoklu-yazar sahnesi VARIANT B color arbiter ile kısmen kapatıldı ama story-test yok (`BUG_REPORT_SCORECARD` "SHIPPED-NO-TEST"). Runtime `UnityEngine.Random` kullanan tek yer wander/jitter — sim'e sızmadığı EMB-040 notu ile pinlenmiş.

## HLD - Akış (numaralı adımlar)

1. `EmberWorldHost` boot'ta authored `ActorView`'ları FindObjectsByType ile toplayıp `WorldViewProjector`'a verir (ctor).
2. Host `EmberGeneratedActorSpawner.SpawnMissingNearbyActors()`'ı çağırır. Spawner adapter'dan `GetSpawnableActors()` alır (pre-projected `SpawnableActor` DTO'ları).
3. `CurrentSettlementKey` değiştiyse: `_spawnedRoots` içindeki candidate-listesinde olmayan tüm billboard'ları `Destroy` eder (W30 cross-city fix); `_spawnedForSettlement` güncellenir.
4. Halihazırda authored/spawned id'ler filtrelenir; kalanlar `PlayerRig`/`Camera.main`/origin anchor'ına göre XZ mesafesine sıralanır; `RuntimeNpcDensity.CapOrDefault(_maxSpawnCount)` cap'i uygulanır.
5. Her `SpawnOne` root + "Billboard" child + `SpriteRenderer` (Generated/Core → bestiary silhouette → neutral fallback) + `ActorView` + `NightCurfewView` + `ActorCombatFeedbackView` + `BillboardWalkAnimView` + `BillboardGroundingView` + `GeneratedNpcAccessibilityGuard` + `EmberInteractable` + `BoxCollider` inşa eder; hostile role ise `HostileMarker` diamond ekler; civilian ise `NpcPoseIconView` + `NpcActivityLabelView` + wander(0.8m) ekler; herkese `BillboardGearMarkView.TryAttach` + `NpcEventEchoView.Bind` + deterministic `NpcVariantTintService` cloth-tint uygulanır; `SetGroundSpeed(hostile ? 3.4 : 1.3)` yazılır.
6. `SpawnOne` başarısı sonrası spawner `EmberWorldHost.RescanActorViews()` çağırır → host `Object.FindObjectsByType<ActorView>` ile yeni set'i toplayıp `WorldViewProjector.ReplaceActorViews`'e geçirir (W30 late-join fix).
7. Her tick `WorldViewProjector.ProjectTick(tickIndex)`: `_clock.AdvanceTick` → `Project()` (id/name-keyed `TryReadActor` her `ActorView`'a `SetTarget(state)` push'lar; Unity-null olan destroyed view'lar atlanır) → `EventLogHudPanel.Render`.
8. `ActorView.Update` interpole eder: `_wander` XZ ofsetini ekler; sim mesafesi ≤5m ise `_groundSpeed`-glide, >5m ise SNAP (spawn/teleport/time-skip), `_groundSpeed=0` ise exponential Lerp. `ExternalPoseOverride=true` ise pose sahibi başka biri (sleep/fall) — bob/lean/shake/tint blokları es geçilir. Aksi halde `flipX` yok (retired), `strideBob`+lean writer'ları çalışır. `_tintRemaining>0` iken `_renderer.color = red`, `_shakeRemaining>0` iken localPosition jitter'ı.
9. `ActorCombatFeedbackView.Update` (DefaultExecutionOrder=50, ActorView'dan sonra çalışır): `HitStamp/FelledStamp/EnemyStrikeStamp` polling; hit → 0.15s flash + sparks; felled → `Fall()` (`ExternalPoseOverride=true`, facing off, board 90° flat, grey); enemy strike → 0.2s lunge ofseti ADDITIVELY billboard localPosition'a ekleniyor. B24 color arbiter: flash aktifken red-orange, değilse `ActorView.DamageTinting` true iken base'e dönmüyor (Apply'ın kırmızısını korur), else `_baseColor`.
10. `Update()` içindeki streaming sweep: 2.5s throttle + player 40m'den fazla hareket ettiyse `SpawnMissingNearbyActors` re-entrant çağrılır (Daggerfall-style lazy world).

## LLD - Veri Modeli (file:line)

- `ActorView` (`Assets/Scripts/Presentation/Ember/Views/ActorView.cs:33`) — `[SerializeField] _domainActorKey` (`:34`), `_domainActorId` (`:39`, string olarak — ulong inspector'da serialize edilmez), `_interpolationSpeed=8f` (`:40`), `_billboard` (`:41`), `_walkCycleFrequency=0.4f`/`_idleFloatFrequency=1.5f`/`_idleFloatAmplitude=0.05f` (`:44-46`), runtime state: `_target` (`ActorViewState`, `:82`), `_hasTarget`, `_renderer`, `_tintRemaining`, `_shakeRemaining` (`:83-86`), `_billboardBaseLocalPos` (`:87`), `_walkTimer`, `_lastPosition` (`:88-89`), wander alanları `_wander/_wanderRadius/_wanderSpeed=0.6f/_wanderCurrent/_wanderGoal/_wanderRepathTimer` (`:96-101`), `_groundSpeed` (`:106`), `ExternalPoseOverride` (public bool, `:122`), `DamageTinting` (`:127`, B24 sözleşmesi).
- `ActorViewState` (`ActorView.cs:288`) readonly struct — `WorldPosition`, `WorldRotation`, `Visible`, `Activity` (string), `Sleeping` (bool), `ActionKind` (string, W32 DOC5).
- `WorldViewProjector` (`WorldViewProjector.cs:8`) — `_clock` (IEmberSimulationClock), `_worldView` (IWorldViewReadModel), `_actorViews` (MUTABLE `ActorView[]`, W30 açıklaması `:12`), `_worksiteViews` (readonly), `_eventLogHud`, `_eventNarrator`.
- `EmberGeneratedActorSpawner` (`EmberGeneratedActorSpawner.cs:41`) — serialize alanlar `_maxSpawnCount=24` (`:47`), `_billboardTargetHeight=2.1f` (`:52`), `_spawnSpacing=1.5f` (`:58`). Runtime state: `LoggedSpriteResolutions` (static HashSet), `_fallbackSprite`, `_spawnedIds` (HashSet<ulong>), `_spawnedRoots` (Dictionary<ulong,GameObject>), `_spawnedForSettlement` (ulong — W30 despawn eşiği), streaming: `_lastScanAnchor`, `_nextScanTime`, `ScanIntervalSeconds=2.5f`, `ScanMoveThresholdMeters=40f` (`:281-286`).
- `SpawnableActor` DTO (`Assets/Scripts/Presentation/Ember/Adapters/IDomainSimulationAdapter.cs:106`) — id, name, spriteRole, worldX, worldZ, seed. Domain'den flat DTO, presentation Domain math görmez.
- `ActorCombatFeedbackView` (`ActorCombatFeedbackView.cs:17`, `[DefaultExecutionOrder(50)]`) — `_actorId`, `_sprite`, `_billboardFacing`, `_actorView` (B24 arbiter için, `:23`), `_hitSeen/_felledSeen/_strikeSeen` (stamp cursors), `_flashUntil`, `_lungeUntil`, `_fallen`, `_baseColor`, `_lungeOffset` (`:31`).
- `BillboardWalkAnimView` (`BillboardWalkAnimView.cs:11`) — `StepSeconds=0.28f`, `SquashScale=0.95f`, `_sprite`, `_baseScale`, `_lastPos`, `_nextStep`, `_frameB`.
- `NpcPoseIconView` (`NpcPoseIconView.cs:11`) — static `s_hammer`/`s_mug`, `_icon`, `_worker`, `_nextPoll`, `_actionKind` (W32 DOC5: kind push'lanır, view hour derivmez).
- `NpcActivityLabelView` (`NpcActivityLabelView.cs:12`) — TextMesh legacy path, `VisibleMeters=22f` cull.
- `NpcEventEchoView` (`NpcEventEchoView.cs:11`) — static sprite havuzu (eye/alert/sword/sheaf/chat), `NpcEventEchoFeed.Stamp` polling.
- `BillboardGearMarkView` (`BillboardGearMarkView.cs:11`) — static `s_spear`/`s_blade`, static-only class.
- `BillboardGroundingView` (`BillboardGroundingView.cs:12`) — `_nextProbe`, `_groundY`, `_hasGround`; LateUpdate ray + hariç tut (self, actor collider, Roof/Canopy/Table/Bench/Trestle).
- `CameraFacingBillboard` (`CameraFacingBillboard.cs:11`) — `_yawOnly=true`, `_cameraTransform` cache.
- `GeneratedNpcAccessibilityGuard` (`GeneratedNpcAccessibilityGuard.cs:11`) — `_volumes` (BuildingAccessibilityVolume[]), `_nextRefreshTime`.
- `BestiaryBillboardSpriteFactory` (`BestiaryBillboardSpriteFactory.cs:11`) — static wolf/spider/skeleton/ghost/bandit sprite'ları + `TargetHeightFor`.

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `ActorView.HasDomainActorId => TryGetDomainActorId(out _)` (`ActorView.cs:57`) — id string non-empty & ulong parse & !=0.
- `ActorView.DomainActorId => ActorId` (`:60`) — parse edilmiş stable id, yoksa `default`.
- `ActorView.BindDomainActorId(ActorId id)` (`:71`) — SOUL-04 runtime damga; spawner'ın kullandığı text-form.
- `ActorView.SetTarget(ActorViewState state)` (`:130`) — sim push; ilk çağrıda `NpcActivityLabelView/NpcPoseIconView/NightCurfewView` cache'lenir; her state pushunda label/pose/curfew güncellenir.
- `ActorView.EnableWander(float radius)` (`:151`) — cosmetic idle wander; presentation-only (`UnityEngine.Random`).
- `ActorView.SetGroundSpeed(float mps)` (`:166`) — overworld walker m/s; combat ise 0 tutulur (snap chase).
- `ActorView.Apply(int amount)` (`:170`) — IDamageSink; 0.2s tint + 0.2s shake + combat log.
- `ActorView.Update()` (`:181`) — interpole (wander/glide/snap/Lerp arbiter'ı `:200-210`), billboard bob/lean writer'ları (ExternalPoseOverride guard'lı), tint+shake writer'ları (B24 guard).
- `WorldViewProjector.ctor(clock, worldView, actorViews[], worksiteViews[], eventLogHud)` (`WorldViewProjector.cs:17`) — actorViews null → boş array.
- `WorldViewProjector.ReplaceActorViews(ActorView[])` (`:36`) — W30 INVARIANT FIX: streaming spawner'ları/post-travel refill'leri sync setine sokar.
- `WorldViewProjector.Project()` (`:41`) — her ActorView için id/name-keyed `TryReadActor`; Unity-null view'lar atlanır (destroyed on site change); her WorksiteView için `TryReadWorksite`.
- `WorldViewProjector.ProjectTick(int tickIndex)` (`:62`) — clock advance → project → event log render (eski tick sırası).
- `EmberGeneratedActorSpawner.SpawnMissingNearbyActors() : int` (`EmberGeneratedActorSpawner.cs:76`) — reentrant one-shot; W30 despawn sweep + nearest-N + RescanActorViews.
- `EmberGeneratedActorSpawner.SpawnOne(SpawnableActor, int spawnIndex) : bool` (`:147`) — root+billboard hierarchy inşası; hostile/civilian dallanma; BindDomainActorId + SetGroundSpeed + EnableWander(0.8f) civilian için.
- `EmberGeneratedActorSpawner.AddHostileMarker(Transform root)` (`:246`) — F10 kırmızı elmas quad; collider destroy edilir; unlit-solid material.
- `EmberGeneratedActorSpawner.CollectExistingViewIds() : HashSet<ulong>` (`:265`) — sahnedeki authored+spawned view id'lerini toplar.
- `EmberGeneratedActorSpawner.Update()` (`:288`) — streaming rescan (2.5s throttle + 40m player hareketi).
- `EmberGeneratedActorSpawner.ResolvePlayerAnchorXZ() : Vector2` (`:297`) — PlayerRig → Camera.main → origin.
- `EmberGeneratedActorSpawner.SpawnOffset(int index) : Vector2` (`:340`) — deterministic square-ring scatter, `_spawnSpacing` step'li.
- `EmberGeneratedActorSpawner.ResolvePlaceholderSprite(SpawnableActor)` (`:371`) — library → bestiary silhouette → neutral fallback.
- `ActorCombatFeedbackView.Bind(ulong actorId, SpriteRenderer, Behaviour facing)` (`ActorCombatFeedbackView.cs:33`) — spawner tarafından çağrılır; stamp cursor'ları offset'lenir.
- `ActorCombatFeedbackView.Update()` (`:42`) — hit/felled/strike stamps + color arbiter (B24) + lunge additive offset.
- `ActorCombatFeedbackView.Fall()` (`:114`) — corpse pose; `ActorView.ExternalPoseOverride=true`, facing off, board 90° flat, grey.
- `BillboardWalkAnimView.Bind(SpriteRenderer)` (`BillboardWalkAnimView.cs:23`) — base scale/last pos.
- `BillboardWalkAnimView.Update()` (`:31`) — B24 SINGLE flipX yazarı; hareket varsa 0.28s cadence'te mirror + 0.95 squash.
- `BillboardGroundingView.LateUpdate()` (`BillboardGroundingView.cs:18`) — 4Hz throttle + RaycastAll, actor collider/roof/canopy/furniture hariç, en yüksek yüzey.
- `CameraFacingBillboard.LateUpdate()` (`CameraFacingBillboard.cs:15`) — yaw-only LookRotation.
- `NpcActivityLabelView.Bind()` (`NpcActivityLabelView.cs:19`) — TextMesh (LegacyRuntime font) child inşa eder; kendisi camera-facing (CameraFacingBillboard mirror problemi).
- `NpcActivityLabelView.SetActivity(string)` (`:41`) — ActorView.SetTarget'ten push'lanır.
- `NpcActivityLabelView.Update()` (`:47`) — readable-facing (glyph mirror değil) + 22m cull.
- `NpcPoseIconView.Bind(bool workerRole)` (`NpcPoseIconView.cs:22`) — icon child + CameraFacingBillboard.
- `NpcPoseIconView.SetActionKind(string kind)` (`:34`) — W32 DOC5: kind push'lanır, view derivmez.
- `NpcPoseIconView.Update()` (`:36`) — 1.1s poll; `_actionKind=="ConsumeFood"` → Mug; worker+work-hours (GUESS, PerformWork ile emekliye ayrılacak) → Hammer.
- `NpcEventEchoView.Bind(ulong actorId)` (`NpcEventEchoView.cs:22`) — event echo child.
- `NpcEventEchoView.Update()` (`:33`) — 0.4s poll; kind → sprite; 3.5s hide.
- `BillboardGearMarkView.TryAttach(GameObject root, string spriteRole)` (`BillboardGearMarkView.cs:16`) — guard/knight → spear, outlaw/bandit → blade.
- `GeneratedNpcAccessibilityGuard.LateUpdate()` (`GeneratedNpcAccessibilityGuard.cs:14`) — bina volume'unun içindeyse dışarı it.
- `BestiaryBillboardSpriteFactory.For(string spriteRole) : Sprite` (`:14`) — monster_wolf/spider/skeleton/ghost/bandit dispatcher.
- `BestiaryBillboardSpriteFactory.TargetHeightFor(...)` (`:27`) — per-tür billboard yüksekliği.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Bu sistem `WorldState` yazmaz (presentation-only). `FieldOwnershipRegistry` alanlarına yazan yok; okuyan alanlar:

- **Okur**: `Actor.GridPosition` (adapter'ın id-keyed `TryReadActor`'ı üzerinden — `ActorView.SetTarget`'e projected `WorldPosition`), `Actor.Vitals` (dolaylı — `WorldCombatFeedbackFeed` stamp'ları felled kararının ardında), `WorldEventFeed` (RecentWorldEvents), `NpcEventEchoFeed` (per-actor son event kind).
- **Okur**: `IWorldViewReadModel.CurrentSettlementKey` (`EmberGeneratedActorSpawner` despawn eşiği) ve `GetSpawnableActors()` (nearest-N candidate havuzu).

Presentation-layer yazarları (registry dışı, doküman notu):
- `SpriteRenderer.color` yazarları: `ActorView` (`_tintRemaining>0` iken red; `!ExternalPoseOverride` guard'lı), `ActorCombatFeedbackView` (flash red-orange + base restore + Fall grey; B24 arbiter: `ActorView.DamageTinting` true iken base restore yasaklı), spawner ctor'da `NpcVariantTintService.TintFor` (cloth-tint, bir kez).
- `SpriteRenderer.flipX` yazarı: **YALNIZ** `BillboardWalkAnimView` (B24 VARIANT B step 1a — ActorView'ın flipX yazımı retired, sadece walkTimer no-op gate kaldı).
- `_billboard.localPosition` yazarları: `ActorView` (stride bob + idle float + shake; guard'lı), `ActorCombatFeedbackView` (lunge ADDITIVE offset; guard'lı; DefaultExecutionOrder=50 ile ActorView'dan sonra), `ActorCombatFeedbackView.Fall` (corpse pin y=0.15).
- `_billboard.localRotation` yazarları: `ActorView` (walk lean sin + idle identity; guard'lı), `ActorCombatFeedbackView.Fall` (90° flat).
- `transform.position` yazarı: `ActorView.Update` interpolasyonu (root); `BillboardGroundingView.LateUpdate` (Y snap); `GeneratedNpcAccessibilityGuard.LateUpdate` (bina dışına it) — sıra: ActorView@Update < ActorCombatFeedbackView@Update(50) < GroundingView@LateUpdate < AccessibilityGuard@LateUpdate < CameraFacingBillboard@LateUpdate.
- `ExternalPoseOverride` yazarı: `ActorCombatFeedbackView.Fall`, `NightCurfewView` (sleep pose sahibi).

## LLD - Ürettiği/Tükettiği Olaylar

**Üretmez** (presentation-only, sim geri yazımı yok).

**Tüketir**:
- `IWorldViewReadModel.TryReadActor(ActorId/string) → ActorViewState` (per tick, `Project()`).
- `IWorldViewReadModel.TryReadWorksite(string) → WorksiteViewState` (per tick).
- `IWorldViewReadModel.RecentWorldEvents(64)` (event log render).
- `IWorldViewReadModel.GetSpawnableActors()` + `CurrentSettlementKey` (spawner).
- `WorldCombatFeedbackFeed.HitStamp/HitTargetId`, `FelledStamp/FelledTargetId`, `EnemyStrikeStamp/EnemyStrikeId` (ActorCombatFeedbackView polling).
- `NpcEventEchoFeed.Stamp` + `LatestKindFor(id, seen)` (NpcEventEchoView).
- `RuntimeFieldMirror.HourOfDay` (NpcPoseIconView — GUESS, PerformWork ile emekliye ayrılacak).
- `RuntimeNpcDensity.CapOrDefault(default)` (spawner cap, director'dan).
- `RuntimeMaterialPalette.Solid(color)` (hostile marker material).
- `NpcVariantTintService.TintFor(id)` (paper-doll v1 cloth tint).
- `EmberInteractable.Setup(name, "General", id)` — dialog id-keyed path'i tetikler.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/World/WorldStateActorViewTests.cs` — **Domain-side** `WorldState.ReplaceActorView(ActorRole, ...)` sözleşmesini pinler (isim ambiguity, replace-by-role); Presentation `ActorView` MonoBehaviour'ı pinlemiyor.
- `Assets/Tests/EditMode/GeneratedAssetLibrary/GeneratedNpcBillboardResolverTests.cs` — spawner'ın sprite çözünürlük yolunu (library → core fallback) dolaylı pinler (`ResolveGeneratedSprite`).
- `Assets/Tests/EditMode/GeneratedAssetLibrary/GeneratedAssetSpritePipelineTests.cs` / `GeneratedAssetDatabaseTests.cs` / `CoreAssetLibraryRecordBuilderTests.cs` / `CoreAssetRegenerationScopeTests.cs` — GeneratedCore sprite kaynağı; spawner'ın consume ettiği katman.
- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — `BillboardOriginCell` adapter'ı çalıştırır; view'ın konum kaynağını dolaylı pinler.

**Doğrudan story-test EKSİK**: `EmberGeneratedActorSpawner` cross-city despawn (W30), `WorldViewProjector.ReplaceActorViews` late-join, `ActorView` glide/snap eşiği (5m), B24 çoklu-yazar arbiter (`ActorView.DamageTinting` sözleşmesi). `BUG_REPORT_SCORECARD.md:33` B24 "SHIPPED-NO-TEST" olarak markalanmış. Presentation MonoBehaviour test harness'ı yok — B24/W30 için PlayMode fixture veya headless adapter-fake story-test'i açık borç.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W30 — Cross-city despawn** (`EmberGeneratedActorSpawner.cs:83-107`): "npc ler koyden uzaklasmaya basliyorlar" LIVE bug. `readModel.CurrentSettlementKey != _spawnedForSettlement` iken `_spawnedRoots` içindeki candidate-listesinde olmayan tüm billboard'lar destroy edilir. F10 aynı-site corpse durumu bozulmasın diye anahtar-gated.
- **W30 — Late-join view sync** (`WorldViewProjector.cs:36`): "drifted 21m, deterministic" INVARIANT FIX. Host boot'unda tek seferlik cache'lenmiş `_actorViews` array'i mutable oldu, `ReplaceActorViews` API'si açıldı, `EmberWorldHost.RescanActorViews` streaming spawner çağrısı sonrası array'i yenilior.
- **W30 — Glide/snap eşiği** (`ActorView.cs:207-210`): "drifted 21m off its sim projection" INVARIANT FIX. `_groundSpeed>0` ise ≤5m mesafede MoveTowards glide, >5m ise SNAP (spawn/teleport/time-skip). Eski Lerp saniyelerce şehir üzerinden kayıyordu.
- **W30 — Streaming rescan** (`EmberGeneratedActorSpawner.cs:279-295`): "quest 250m diyor ama orası boş" fix. `Update()` 2.5s throttle + 40m player hareketi eşiğiyle `SpawnMissingNearbyActors`'ı re-entrant çağırıyor — Daggerfall-style lazy world.
- **W32 DOC5 — Action-kind push** (`ActorView.cs:143`, `NpcPoseIconView.cs:34`, `ActorViewState.ActionKind`): mug icon eskiden hour+lunch-window'dan derivyordu (`DomainSimulationAdapter.WorldProjection.cs:109-117` hala mirror comment'ı taşıyor — B22 tetiği). W32 DOC5: mug artık sim'in `CurrentAction.Kind=="ConsumeFood"` push'undan; view derivmiyor.
- **W34 — Sleep sözleşmesi** (`EmberGeneratedActorSpawner.cs:157-162`): Prowler sprite-name guess silindi; `NightCurfewView` yalnızca `ActorViewState.Sleeping` gerçek sim Sleep action ile true olduğunda lying pose'a geçiyor.
- **W36 tail — B24 VARIANT B color arbiter** (`ActorCombatFeedbackView.cs:12,23,29,58,88`, `ActorView.cs:124,237,259`): DefaultExecutionOrder=50 ile ActorView'dan sonra çalışıyor; `ActorView.DamageTinting` sözleşmesi eklendi; flash > tint > base color öncelik zinciri; `BillboardWalkAnimView` tek flipX yazarı, ActorView.flipX retired; lunge SETTEN ADDITIVE OFFSET'e çevrildi (`_lungeOffset`), shake x/z bileşenini kırpmıyor artık. **Story test yok** (`BUG_REPORT_SCORECARD` "SHIPPED-NO-TEST").
- **F27/F29/F33 vitrini** (spawner ctor): `NpcPoseIconView` (worker/eater pictogram), `BillboardGearMarkView` (guard spear/bandit blade), `BestiaryBillboardSpriteFactory` (wolf/spider/skeleton/ghost/bandit silhouette fallback), `RuntimeHitSparks` (F33 landed-strike sparks), `NpcEventEchoView` (M6 event echo icons), `BillboardWalkAnimView` (F33 two-frame mirror gait) hepsi son beş haftada spawner'a wire edildi.

## Bilinen Borçlar + Kaçak Kapıları

- **B24 SHIPPED-NO-TEST** (`BUG_REPORT_SCORECARD.md:33`): VARIANT B arbiter'ı sözleşmeleştirdi ama story-test yok. `DamageTinting` sözleşmesini yakalayan bir headless test (fake stamp feed + assert color chain) açık iş.
- **B22 mug hour guess** (`DomainSimulationAdapter.WorldProjection.cs:109-117` — MUST-match yorumu): mug artık action-kind push'undan geliyor ama `NpcPoseIconView.cs:47` hala `hour>=8 && hour<18` guess'i içeriyor (worker branch); PerformWorkAction landing ile emekliye ayrılması gerek.
- **Sim sızması korkusu**: `ActorView.EnableWander` + shake bloğu `UnityEngine.Random` kullanıyor (EMB-040 pin'li). Sim'e sızmadığı tarihsel olarak varsayilmis; current authority `docs/recovery/CURRENT_STATE.md`. Save/digest'e girmemesi projeksiyonel — regression testi yok.
- **B25 mutable domain leak**: `IWorldViewReadModel` metotları Domain tipleri (`OverlandMap`) dönüyor; view teorik olarak `TryReadActor` yolundan sim state'ini mutate edebilir. B25 later-slice.
- **Streaming spawner boot rescan race**: `RescanActorViews` boot'ta projector kurulmadan çağrılırsa no-op; comment satırı var (`EmberGeneratedActorSpawner.cs:140`) ama boot'ta ilk spawn sonrası host'un elle rescan etmesi bekleniyor. Order kırılırsa sessiz kayıp — invariant assert yok.
- **`CollectExistingViewIds` her spawn'da O(scene) `FindObjectsByType`**: 750 NPC + streaming scan 2.5s cadence'te maliyet birikir; small-N cap sayesinde şu an sorun değil ama kaçak kapı.
- **Grounding raycast dışlama listesi hardcoded string** (`BillboardGroundingView.cs:36-39`): Roof/Canopy/Table/Bench/Trestle isim eşleşmesi; yeni ground-emici mesh isimleri sessiz düşer.
- **NpcActivityLabelView TextMesh legacy path**: TMP Essentials'sız çalışsın diye deliberately legacy; TMP'ye geçilirse ayrı mirror-fix gerekecek (CameraFacingBillboard'ın glyph mirror'ı).
- **NpcEventEchoView `ChatMask` ilk-satır uzunluğu tutarsız** (`NpcEventEchoView.cs:96` — bir satır 10 char, geri kalanlar 12): `FromMask` `x < rows[y].Length` guard'lı, silent-safe ama görsel kırık.
- **`_maxSpawnCount=24` cap'i realize-derived değil**: `RuntimeNpcDensity.CapOrDefault` bir override sağlıyor ama baked scene'lerde default kalıyor. Kalabalık şehir sahnesi hala 24 ile sınırlı.
- **`ActorCombatFeedbackView.Fall` bir kere çalışıyor**: `_fallen=true` sonrası revive yolu yok; save/load sonrası "yeniden dirilme" için özel yol lazım (şu anda sim state'i drives etmiyor).

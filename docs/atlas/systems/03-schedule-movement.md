# 03-schedule-movement

## HLD - Ne ve Neden (5-10 cümle)

`ScheduleSystem` (SOUL-03 → CAN SUYU H2), aksiyonsuz aktörleri kendi ihtiyaçlarına göre `rest / work / idle` üçlüsünden birine yönlendiren utility-selector'dır; V1'in "12:00-13:59 arası koreografik öğle" gibi hardcoded pencerelerinin yerini aldı ve W32 EAT sonrası "yemek yeme" tercihi de aksiyon katmanına (`ActionLifecycleSystem`) taşındı, dolayısıyla bu sistem artık civilian için sadece `Idle-only steering` yapıyor. Muhafızlar için pursuit çözümlemesini de bu sistem koşturuyor: `WitnessResponseSystem` bir `PursuitRecord` yazınca aynı `PerTick` kadansındaki bu sistem chase'i her tikte bir hücre kapatıyor, expire olan/kayıp/ölü hedefler in-place prune ediliyor. Fiziksel hareketin tek uygulayıcısı `MovementService.StepToward` — Chebyshev 8-yön, tik başına eksen başına bir hücre, monoton yakınsama, hedefi asla aşmaz. W36 (B10 §A) sonrası bu adım opsiyonel bir `IWorldNavigability` view'ıyla çalışıyor: view null ise legacy wall-blind primitif; view varken diagonal duvar-köşesi kesimini reddediyor ve sabit sırayla (X-önce, sonra Y) axial fallback deniyor, her ikisi de kapalıysa bir tik donuyor (`from` döner). Blocker seti `WorldState.Blocked` (`BlockedCellSet`, packed `HashSet<long>`) türetilmiş bir durumdur — hiç serialize edilmez, `HydrateBlockedCells` ile bina footprint'lerinden yeniden inşa edilir. Böylece civilian yol arama basit bir StepToward + probe kalırken, dungeon slice `RoomMovementService`'te kendi kural setini korur (odayı `WorldState.NavView`'e katlamak Gate1/Gate8'te köylüleri dondururdu). Tüm sistem saf Domain/Simulation'dir: Unity yok, I/O yok, RNG yok, `PerTick@20` prioritesi.

## HLD - Akış (numaralı adımlar)

1. `WorldTickComposer` (living.schedule step, `PerTick` cadence, priority 20) her tikte `_schedule.Advance(actors, stamp, world.GuardPursuits, world)` çağırır.
2. `Advance` `ActorStore.Records` üzerinde döner; ölü aktör atlanır, `ActorActionState.CurrentAction != None` olan aktör atlanır (aksiyon katmanının bacaklarını almış).
3. F18 filtresi: `Role == Enemy && Home.Equals(DayAnchor)` olan lair guard'lar atlanır (rubber-band önleme).
4. Muhafızsa `TryResolvePursuit` çalışır: kendi ID'sine ait aktif kaydı bulur, expire/dead-quarry/>40 hücre uzaklaşmışsa listeden siler ve `false` döner; canlıysa `target = quarry.Position`.
5. Aksi halde `ChooseTarget(actor, time)` çağrılır: Guard/Enemy için `ClassicTarget` (work-hour içinde worksite ya da anchor; dışında Home). Civilian için utility tablosu — `rest = Fatigue.Value + (workHour ? 0 : 25)`, `work = workHour && !Idle ? 55 : 0`, `idle = workHour ? 35 : 0`; deterministic tie order `rest > work > idle`.
6. `MovementService.StepToward(actor.Position, target, world?.NavView)` bir sonraki hücreyi hesaplar.
7. `StepToward` iç akışı: `dx = Sign(to.X - from.X)`, `dy = Sign(to.Y - from.Y)`, `candidate = (from.X+dx, from.Y+dy)`; nav null ise anında `candidate` döner.
8. Nav varsa: `diagonal` ve `nav.BlocksDiagonal(from, candidate)` ise diagonal reddedilir, `AxialFallback` çalışır; değilse `nav.IsWalkable(candidate)` doğruysa `candidate` döner, aksi halde yine `AxialFallback`.
9. `AxialFallback` sabit sırayla X-axial (`from.X+dx, from.Y`) sonra Y-axial (`from.X, from.Y+dy`) dener; her ikisi de kapalıysa `from` döner (bir tik freeze — action'ın arrival predicate'i sonraki tikte tekrar dener).
10. Sonuç `actor.Position`'a eşit değilse `actor.MoveTo(next)` çağrılır (Position rows yazılır, W28 SoA).

## LLD - Veri Modeli (file:line)

- `ScheduleSystem` (sealed class) — `Assets/Scripts/Simulation/Living/ScheduleSystem.cs:14`
  - `WorkStartHour = 6` (:17), `WorkEndHour = 20` (:20)
  - `WorkScore = 55`, `IdleScore = 35`, `NightRestBonus = 25` (:25-27)
- `MovementService` (static) — `Assets/Scripts/Domain/Core/MovementService.cs:11`
  - `StepToward(from, to, nav = null)` (:17)
  - `AxialFallback(from, dx, dy, nav)` (:36)
- `IWorldNavigability` (interface) — `Assets/Scripts/Domain/Core/IWorldNavigability.cs:11`
  - `bool IsWalkable(GridPosition cell)` (:14)
  - `bool BlocksDiagonal(GridPosition from, GridPosition to)` (:18)
- `BlockedCellSet` — `Assets/Scripts/Domain/World/BlockedCellSet.cs:13`
  - `PackStride = 1_000_000L` (:18), `HashSet<long> _cells` (:20), `long _revision` (:21)
  - `Revision`, `Count`, `Contains`, `Add`, `Clear`, `PackedCells`, `Pack(x,y)` (:23-44)
- `PursuitRecord` — `Assets/Scripts/Domain/World/PursuitRecord.cs:8`
  - `ulong GuardId`, `ulong TargetId`, `long UntilMinutes`
- `WorldState : IWorldNavigability` — `Assets/Scripts/Domain/World/WorldState.cs:23`
  - `NavView => this` (:111), açık interface impl `IsWalkable` (:117) ve `BlocksDiagonal` (:124)
  - `BlockedCellSet Blocked = new BlockedCellSet()` (:240)
  - `List<PursuitRecord> GuardPursuits` (aynı dosya; W28 SoA blokunda)

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `void Advance(ActorStore actors, GameTime time)` — `ScheduleSystem.cs:29` — Nav'sız legacy overload, `Advance(actors, time, null, null)`'a devreder.
- `void Advance(ActorStore, GameTime, List<PursuitRecord>)` — `ScheduleSystem.cs:35` — P0 pursuit overload, world hâlâ null (test yolları).
- `void Advance(ActorStore, GameTime, List<PursuitRecord>, WorldState)` — `ScheduleSystem.cs:44` — B10 §A5 nav-aware ana giriş; composer bunu çağırır.
- `bool TryResolvePursuit(pursuits, actors, guard, time, out target)` — `ScheduleSystem.cs:82` — Aktif chase'i quarry'nin canlı hücresine çözer; expire/dead/lost kayıtları prune eder.
- `bool IsWorkHour(GameTime time)` — `ScheduleSystem.cs:105` — `hour in [6, 20)` kontrolü; ChooseTarget ve testler için public.
- `GridPosition ChooseTarget(ActorRecord actor, GameTime time)` — `ScheduleSystem.cs:114` — Utility core; guard/enemy için ClassicTarget, civilian için rest/work/idle skor tablosu.
- `GridPosition ClassicTarget(ActorRecord actor, bool workHour)` — `ScheduleSystem.cs:135` — Yasal muhafız/düşman routing: work-hour ⇒ worksite/anchor, dışı ⇒ Home.
- `GridPosition StepToward(GridPosition from, GridPosition to, IWorldNavigability nav = null)` — `MovementService.cs:17` — Nav-opsiyonel Chebyshev bir tik; W28 pathfinding seam'i olacak yer.
- `GridPosition AxialFallback(from, dx, dy, nav)` — `MovementService.cs:36` — X-önce Y-sonra sabit sırayla axial dener, her ikisi kapalı ⇒ `from` döner (freeze).
- `IWorldNavigability WorldState.NavView` — `WorldState.cs:111` — `this` döner; allocation-free per-tick probe.
- `bool WorldState.IsWalkable(cell)` — `WorldState.cs:117` — `Blocked == null || !Blocked.Contains(cell)` (odalar bu view'e dahil değil).
- `bool WorldState.BlocksDiagonal(from, to)` — `WorldState.cs:124` — İki ortogonal komşusu da blocked ise diagonal reddedilir.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Kayıtlı sahiplik satırları (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs`):

- **`Actor.Position`** yazar (:20): `living.schedule@PerTick:20` — NARROWED (W32): sadece actionless aktörler; `living.action_advance@PerTick:22` aktif MoveTo* için ayrı satır tutar.
- **`World.GuardPursuits`** yazar (:53): `living.schedule@PerTick:20` — resolve/prune; `living.witness@Hourly:45` arm/refresh.
- Okur (sahiplenmez): `Actor.ActionState.CurrentAction` (skip filtresi), `Actor.Role`, `Actor.Home`, `Actor.DayAnchor`, `Actor.ScheduleState.{IsIdle,TargetWorksitePosition}`, `Actor.Needs.Fatigue`, `GameTime.Hour`.
- `MovementService.StepToward` okur: `IWorldNavigability.IsWalkable`, `IWorldNavigability.BlocksDiagonal` → altında `WorldState.Blocked` (hiçbir alan yazmaz).
- `WorldState.Blocked` derived state — `HydrateBlockedCells` (Presentation adapter) tarafından yazılır, tik döngüsü sadece okur.

## LLD - Ürettiği/Tükettiği Olaylar

- **Üretmez** — hareket sessizdir; `ActionLog`'a satır düşmez, `World.Events`'e olay basmaz. Tik izleri sadece `Actor.Position` diff'i ve `GuardPursuits`'un prune sonrası boyutu.
- **Tüketir**: `WorldEventKind.CombatResolved` üzerinden dolaylı — `WitnessResponseSystem` olayı okuyup `GuardPursuits`'a yeni kayıt yazar; schedule sonra bu listeden okur.
- **Yan-tetik**: Bir sonraki tikte `living.action_advance@PerTick:22` `Actor.Position`'ı okur; `living.companion_follow@PerTick:21` de schedule'dan SONRA heel yapar (tasarım gereği).

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` — 10 test: work-hour worksite yakınsaması, gece/night no-move, actionless-action skip (W32), utility tablosu, F18 pinli-enemy hold, curfew commute enemy, idle→anchor.
- `Assets/Tests/EditMode/Living/GuardPursuitTests.cs` — 3 test: witness→pursuit arm, chase her tikte kapatır, expire→home dönüşü.
- `Assets/Tests/EditMode/Movement/MovementServiceBlockerTests.cs` — 7 test (W36/B10 §A4): null-nav legacy primitif, açık diagonal, blocked-diagonal X-önce fallback, X-blocked Y fallback, hepsi kapalı freeze, corner-cut refused, düz-axial etkilenmez.
- `Assets/Tests/EditMode/Actions/MoveAvoidsBlockedCellsTests.cs` — 2 test (W36 story): `MoveToFood` blocked candidate'a girmez ve axial slide yapar; blocker yokken diagonal candidate hâlâ alınır (regresyon guard'ı).
- `Assets/Tests/EditMode/World/BlockedCellSetTests.cs` — 6 test (W36/B10 §A2): boş, add + revision bump, idempotent add, clear + bump, empty-clear no-op, large-coord pack collision-free.
- `Assets/Tests/EditMode/World/RoomMovementServiceTests.cs` — dungeon slice'ın kendi `IsWalkable`'ını consult ettiği ayrık path (bu sistemin `NavView`'i odayı **kapsamaz**).

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W32 EAT** — Hunger routing bu sistemden çıktı; `ActionLifecycleSystem.Decide` `EatIntent` verip `EatAction`'ı çakıyor. ScheduleSystem artık `CurrentAction != None` olan aktörü atlıyor. `ChooseTarget` utility tablosundaki `eat` slot'u kaldırıldı; utility artık sadece rest/work/idle. Ayrıca `MovementService.StepToward` `ScheduleSystem`'den koparılıp `Domain.Core`'a taşındı (W32-03 §5).
- **W33 farm** — Bu sistemi doğrudan değiştirmedi ama `FieldOwnershipRegistry`'e yeni `Actor.Position` yazıcıları eklendi (farm advancer'ları); schedule'ın `PerTick:20` prioritesi ve NARROWED açıklaması korundu.
- **W34 work & sleep** — `MoveToWorksiteAdvancer` / `MoveToBedAdvancer` yeni consumer'lar olarak `MovementService.StepToward`'u kullanır (schedule değil). Guard'ın peckish davranışı ActionLifecycle'da eat-eligibility'ye taşındı; schedule'da guard `ClassicTarget` yolu sağlamlaştı (yorum satırı :117-121'de netleştirildi).
- **W35 Idle-only steering pin** — Yorumlar ve test isimleri "actionless routing" terimini benimsedi; schedule'ın civilian action-owner ile çakışmayacağı `Advance_ActorWithActiveAction_IsNotMoved` testi ile pinlendi.
- **W36 (B10 §A) IWorldNavigability** — `IWorldNavigability` interface + `BlockedCellSet` + `WorldState.NavView` üçlüsü eklendi. `MovementService.StepToward` opsiyonel `nav` parametresi aldı; corner-cut refuse + X-önce axial fallback + freeze kuralları geldi. `ScheduleSystem.Advance` 4'lü overload (`world` parametreli) eklendi; composer bu yeni çağrıya yönlendirildi. `HydrateBlockedCells` presentation adapter'ında bina footprint'lerini `Blocked`'a projekte ediyor. 15 yeni test (`MovementServiceBlockerTests` + `MoveAvoidsBlockedCellsTests` + `BlockedCellSetTests`) eklendi.

## Bilinen Borçlar + Kaçak Kapıları

- **Path yok, tek tik freeze var**: `StepToward` üç adayı da kapalı bulursa `from` dönüyor. Uzun U-şeklinde engellerde aktör donabilir; Stage B'de A*/JPS `MovementService` seam'inin arkasına takılacak (yorumda "pathfinding plugs in behind this seam later" — `MovementService.cs:6`).
- **NavView civilian-only**: `WorldState.IsWalkable` sadece `Blocked`'ı okur, oda duvarları GİRMEZ. Dungeon slice hâlâ `RoomMovementService`'in kendi kural setini kullanmalı; birleştirme denemesi Gate1/Gate8 crowd freeze'ini geri getirir (yorum :113-116).
- **PursuitRecord `long UntilMinutes` linear scan**: `TryResolvePursuit` her guard için tüm listeyi tarıyor (`for i`). Çok sayıda muhafız + chase durumunda O(g·p). Şu anki köy ölçeği için sorun değil, ama caravan/battle ölçeğinde index gerekecek.
- **F18 pinned-enemy kısayolu `Home.Equals(DayAnchor)` heuristiğine bağlı**: Yeni bir "sedanter civilian" role'ü aynı home==anchor invariant'ını taşırsa yanlış filtrelenir; F10 dungeon-dweller kontratını Role-based bir bayrağa çevirmek gerekebilir.
- **Blocked derived + never serialized**: Save/load sonrası `HydrateBlockedCells` çağrılmazsa `Blocked` boş kalır ve civilian duvardan geçer. Hydration seam `EnsureInvariants`'la aynı yolda ama presentation-side; headless test'lerde manuel `world.Blocked.Add(...)` gerekiyor (MoveAvoidsBlockedCellsTests bunu yapıyor).
- **Utility ağırlıkları hardcoded (55/35/25)**: `ChooseTarget` sabit skorlar kullanıyor; kişilik/kültür modülatörleri yok. CAN SUYU H3'te "trait skew" için buraya bir tablo enjekte edilecek.
- **`Advance` 3-overload zinciri boilerplate**: Legacy `(actors, time)` ve pursuit-only `(actors, time, pursuits)` overload'ları test yolları için tutuluyor; composer sadece nav-aware olanı çağırıyor. Kaçak kapı: yeni bir çağrı eski overload'ı yanlışlıkla seçebilir ve `NavView` bypass olur — pin (`MovementServiceBlockerTests.NullNav_PreservesLegacyChebyshevPrimitive`) bu davranışın kasıtlı olduğunu belgeliyor ama grep-öncesi refaktörde tehlikelidir.
- **`companion_follow@PerTick:21` schedule'dan SONRA çalışıyor** (yorum :22): Heel bir tik geri kalıyor — kabul edilmiş minor lag, ama companion ile pursuit hedefi çakışırsa companion "chase'in gerisinde" görünür.

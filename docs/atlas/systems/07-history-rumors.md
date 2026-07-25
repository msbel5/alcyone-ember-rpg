# 07-history-rumors

## HLD — Ne ve Neden (5-10 cümle)

Bu sistem, oyunun **tarih + kronik + dedikodu** eksenidir: worldgen'in yazdığı sabit geçmişten SONRA dünyanın konuşmaya devam etmesini sağlar. Üç katmandan oluşur: (1) `WorldEventLog` — bütün simülasyonun append-only kroniği; her tick ne olduysa buraya yazılır. (2) `RuntimeHistorySystem` (Daily:28) — dünkü sim olaylarını faction ilişkilerine sürer (watch renown, shortage gerginliği) ve ay sonunda `(RoomSeed, day)`-tohumlu bir kronik olay üretir (festival / caravan_surge / border_dispute) + bled-out settlement'ları migrant'la takviye eder. (3) `RumorMillSystem` (Hourly:55) — YENİ event'leri deterministik olarak bir satır kasaba diline damıtır ve `Rumors` listesinde 3 gün / cap 32 tutar; `PickFor` ile diyalogda ve `AmbientVoiceDirector` ile sokakta konuşturur. Sunum katmanında `NpcEventEchoFeed` (ring buffer[128]) tekil aktörlerin başına 12×12 pictogram (göz / uyarı / kılıç / demet / sohbet) astırır. **W36 ayrışması**: log artık `TrimOldest(maxRetained)` + seq-based kursor kontratı ile SINIRLI olmaya HAZIR (B21) — RumorMill kursörü `RumorEventCursorSeq` (long) olarak seq'e taşındı, trim'i tolere ediyor; ancak trim'i tetikleyen bir tick step'i HENÜZ takılmadı, bu yüzden runtime'da log hâlâ unbounded büyüyor (aşağıda "Bilinen Borçlar" — B21 yarım açık).

## HLD — Akış (numaralı adımlar)

1. **Her tick** — Herhangi bir sim step'i (Cascade, Recipe, Job, Plant, Faction, Ambient, Trade, Combat...) bir olayı `world.Events.Append(new WorldEvent(...))` ile log'un sonuna yapıştırır. `WorldEvent` ctor `Kind ≠ None`, `ActorId ∨ SiteId ≠ empty`, `Reason ≠ blank` invariant'larını dayatır; `TotalAppended++`.
2. **PerTick** (Presentation) — `DomainSimulationAdapter.AdvanceTick` içinde `PublishEventEchoes()` `_echoCursor`'dan itibaren log kuyruğunda 256 taneyle sınırlı yürüyor; `WitnessRecorded`, `GuardResponded`, `PlantHarvested`, `ActorTalked` gördükçe `NpcEventEchoFeed.Raise(actorId, kind)` çağırır ve `ActorTalked` için ayrıca `AmbientVoiceDirector.Offer(subject, RumorMillSystem.PickFor(...))` ile spatial mırıltıyı bırakır.
3. **Hourly:55** — `RumorStep` çalışır → `RumorMillSystem.Tick(world, stamp)`: stale rumor'ları (>3 gün) siler, `RumorEventCursorSeq`'i `FirstRetainedSeq..TotalAppended` aralığına clamp'ler, `ScanCap=256` ile backfill'i keser, `log.TryIndexForSeq(cursor, out start)` ile mevcut retained pencerede index bulur, [start..events.Count) döner, her event için `Distill(evt)` non-null döndürürse yeni `RumorEntry { BornMinutes, SiteId, Text }` ekler; sonda `Rumors.Count > 32` iken en eskiden kırpar; kursör `TotalAppended`'a atlar (asla re-mill etmez).
4. **Daily:28** — `RuntimeHistoryStep` çalışır → `RuntimeHistorySystem.Tick(world, stamp)`. Önce `DriftFromYesterday`: son 8192 event'i sondan tarayıp `[dayStart..stamp]` penceresinde `GuardResponded` ve `ShortageDetected` sayar; guard cevapları `law→craft` ve `law→trade`'e +1 renown, shortage `craft↔trade`'e −1 tension olarak `FactionReputationSystem.ApplyDelta` ile yazılır.
5. **Ay sonunda** (day % `DaysPerMonth == 0`) — `MonthlyChronicle`: `XorShiftRng((RoomSeed * 2654435761) ^ (day * 40503) | 1)` ile intensity (1..20) ve dal seçilir → festival (`law↔craft/craft↔trade` +4), caravan_surge (`Stockpiles[0].Add("wheat", 25+intensity)`), veya border_dispute (`law↔trade` −6). Aynı rng ile diplomatik ripple (−4..+4) rastgele bir çifte uygulanır. `ChronicleEvent` log'a düşer + `ArriveMigrants` bled-out (`<MigrantFloor=4` civil) settlement'lara `MigrantsPerMonth=2` yeni Talker doğurur ve her biri için `ActorSpawned` event'i emit eder.
6. **Save/Load** — `WorldSaveMapper` diskke `worldEvents[]` + `worldEventFirstRetainedSeq` (**tek** long, `TotalAppended = firstRetainedSeq + worldEvents.Length` invariant'ıyla türetilir) + `rumorBornMinutes[]/rumorSiteIds[]/rumorTexts[]` + `rumorEventCursorSeq` yazar; load `new WorldEventLog(firstRetainedSeq)` ctor'unu kullanır → kursorlar seq-uzayında aynı yerde uyanır, trim'lenmiş event'ler re-mill'lenmez.
7. **Diyalog** — Oyuncu bir NPC'ye "Any news?" derse `DomainSimulationAdapter.Dialog.Source` `RumorMillSystem.PickFor(world, askerId, siteId, now)` çağırır; site-local pool varsa oradan, yoksa global; hash `askerId * 2654435761 ^ (day * 40503)` deterministik pick — aynı asker aynı gün aynı hikâyeyi alır.

## LLD — Veri Modeli (file:line)

- **`WorldEventLog`** — `Assets/Scripts/Domain/World/WorldEventLog.cs:24-126`
  - `_events: List<WorldEvent>` (L26) + `_eventsView: ReadOnlyCollection<WorldEvent>` (L27)
  - `_firstRetainedSeq: long` (L32), `_totalAppended: long` (L33) — B21 seq accounting
  - Ctor `WorldEventLog()` (L35), restore ctor `WorldEventLog(long firstRetainedSeq)` (L46)
  - Invariant: `TotalAppended == FirstRetainedSeq + _events.Count`
- **`WorldEvent`** — `Assets/Scripts/Domain/World/WorldEvent.cs:18-43`
  - `Tick: GameTime`, `Kind: WorldEventKind`, `ActorId`, `SiteId`, `Reason: string`, `ReasonTrace?`
  - Ctor pins invariants (L21-31): `Kind ≠ None`, `actorId ∨ siteId ≠ empty`, `reason ≠ blank`
- **`WorldEventKind`** enum — `Assets/Scripts/Domain/World/WorldEventKind.cs:12-55`
  - 35 rakam kind: `None=0..ActorFailed=34`; H3 `WitnessRecorded=30`/`GuardResponded=31`; H4 `ChronicleEvent=32`; W32 `ActionCompleted=33`/`ActionFailed=34` (per-step spam SILINDİ — B21 sınıfını yeniden doğurmasın)
- **`RumorEntry`** — `Assets/Scripts/Domain/World/RumorEntry.cs:5-9`
  - `BornMinutes: long`, `SiteId: SiteId`, `Text: string` (plain fields, no ctor invariants — sadece mill yazar)
- **`WorldState.Events`** — `Assets/Scripts/Domain/World/WorldState.cs:40, 82`
- **`WorldState.Rumors`** — `Assets/Scripts/Domain/World/WorldState.cs:249` (`List<RumorEntry>`)
- **`WorldState.RumorEventCursorSeq`** — `Assets/Scripts/Domain/World/WorldState.cs:253` (**long**, B21'de int index'ten seq'e taşındı; `CopyFrom(other)` L333'te aynen taşınır)
- **`WorldEventSaveData[]`** — `Assets/Scripts/Data/Save/WorldSaveData.cs` + `worldEventFirstRetainedSeq: long` (L60), rumor alanları (L110-116)
- **`NpcEventEchoFeed.Echo`** struct — `Assets/Scripts/Presentation/Ember/WorldDirector/NpcEventEchoFeed.cs:18-23` — `ActorId: ulong`, `Kind: int`, `StampAt: int`; `Ring[128]` static (L25)

## LLD — Fonksiyon Haritası (imza + file:line + 1 cümle)

**Domain — WorldEventLog (`Assets/Scripts/Domain/World/WorldEventLog.cs`)**
- `void Append(WorldEvent worldEvent)` (L77) — Null reddeder, `_events.Add`, `_totalAppended++`.
- `int TrimOldest(int maxRetained)` (L90) — B21: `_events.RemoveRange(0, drop)` + `_firstRetainedSeq += drop`; drop sayısını döner (`Count ≤ maxRetained` iken 0). O(N) memmove — max 16384'te ~64KB/gün.
- `bool TryIndexForSeq(long seq, out int index)` (L110) — Absolute seq → mevcut `Events` index'i; `seq ≤ FirstRetainedSeq` iken 0, `seq ≥ TotalAppended` iken `Events.Count`; her zaman `true`.
- `IReadOnlyList<WorldEvent> Events { get; }` (L123) — Canlı read-only view; sonraki Append'ler görünür (snapshot değil).

**Simulation — RumorMillSystem (`Assets/Scripts/Simulation/Living/RumorMillSystem.cs`)**
- `int Tick(WorldState world, GameTime stamp)` (L18) — Stale prune → kursor clamp/ScanCap kes → `TryIndexForSeq` ile start bul → walk, her rumorable event için `RumorEntry` ekle → kursor `TotalAppended`'a, list `MaxRumors=32`'ye kırp; born-this-tick sayısını döner.
- `static string Distill(WorldEvent evt)` (L50) — Kind + reason'a göre bir cümle üretir (`GuardResponded`, `WitnessRecorded/reported`, `PlantHarvested`, `TradeCompleted`, `ChronicleEvent`, `NeedChanged/vermin_theft|cat_catch|mauled_survives`); tanınmayan → `null` (talk'a değmez).
- `static string PickFor(WorldState world, ulong askerId, SiteId siteId, GameTime now)` (L83) — Site-local varsa oradan, yoksa global pool; `hash = askerId * 2654435761 ^ (day * 40503) * 40503` → deterministik pick.

**Simulation — RuntimeHistorySystem (`Assets/Scripts/Simulation/World/RuntimeHistorySystem.cs`)**
- `void Tick(WorldState world)` (L29) — `Tick(world, world.Time)` overload.
- `void Tick(WorldState world, GameTime stamp)` (L33) — Guard'lar (factions/events var mı, stamp>0, law/craft/trade tag'li 3 faction var mı) → `DriftFromYesterday` + `MonthlyChronicle`.
- `private void DriftFromYesterday(...)` (L48) — Son 8192 event'i sondan tarar, `[dayStart..stamp]` penceresinde `GuardResponded` / `ShortageDetected` say, `_reputation.ApplyDelta` ile law/craft/trade'e sür.
- `private void MonthlyChronicle(...)` (L74) — `day % DaysPerMonth == 0` gate; `XorShiftRng`'den festival/caravan_surge/border_dispute + monthly ripple; `ChronicleEvent` append; `ArriveMigrants` çağır.
- `private static void ArriveMigrants(WorldState world, GameTime stamp, long dayIndex, XorShiftRng rng)` (L118) — Her Settlement site için living-civilian sayar; `< MigrantFloor` ise `MigrantsPerMonth` kadar deterministik id/home ile Talker doğurur; her biri için `ActorSpawned` event'i emit eder ("migrant_arrived name:{...}").

**Presentation — NpcEventEchoFeed (`Assets/Scripts/Presentation/Ember/WorldDirector/NpcEventEchoFeed.cs`)**
- `static void Raise(ulong actorId, int kind)` (L29) — Ring'e yazar, `Stamp++` (monotone), `_writeIndex++`; hiçbir zaman consume etmez (bir cascade'te aynı aktör birden fazla event'i doğurabilir).
- `static int LatestKindFor(ulong actorId, int sinceStamp)` (L36) — Ring'i tamamen tarar, `sinceStamp`'ten yeni + aktör eşleşen en yeni echo'nun kind'ini (yoksa −1) döner.

**Presentation — NpcEventEchoView (`Assets/Scripts/Presentation/Ember/Views/NpcEventEchoView.cs`)**
- `void Bind(ulong actorId)` (L21) — Child GO oluşturur, `SpriteRenderer` + `CameraFacingBillboard` ekler, `_seenStamp`'i mevcut `Stamp`'a kilitler (geriye replay yok).
- `void Update()` (L33) — 0.4s poll → `LatestKindFor` çağır, kind ≥ 0 ise `SpriteFor(kind)` ile pictogram bas, 3.5s sonra gizle. Sprite'lar tembel cache (`s_eye/s_alert/s_sword/s_sheaf/s_chat`), her biri 12×12 mask'ten `Texture2D` üretir.

**Presentation — DomainSimulationAdapter.Clock (`Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Clock.cs`)**
- `void AdvanceTick(int tickIndex)` (L6) — DrainMainThreadApply → `_tickComposer.Advance` → `PublishEventEchoes()` → `PublishFieldMirror()`.
- `private void PublishEventEchoes()` (L41) — `_echoCursor` tail scan (max 256), `WitnessRecorded/GuardResponded/PlantHarvested/ActorTalked` gördükçe `NpcEventEchoFeed.Raise` + `ActorTalked` için `AmbientVoiceDirector.Offer(PickFor(...))`.

## LLD — Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

- **`World.Events`** (WorldEventLog) — **yazar**: her append yapan sim/adapter (16 çağrı, `CascadeSystems`, `CaravanSystem`, `RecipeSystem`, `PlantingSystem`, `PlantGrowthSystem`, `HarvestCropAdvancer`, `ConsumeFoodAdvancer`, `JobAssignmentSystem`, `NeedsSystem`, `NeedRecoverySystem`, `CombatActionResolver`, `SpellResolver`, `RuntimeHistorySystem` monthly + migrant, `FactionReputationSystem/DecaySystem`, `TradeService`, `ToolCallRouter`, `DmAgentEscalationService/NarrationServices`). Ownership registry'de **UNDECLARED** (multi-writer + boundary-write karışımı). **okur**: `RumorMillSystem`, `RuntimeHistorySystem.DriftFromYesterday`, `DomainSimulationAdapter.PublishEventEchoes`, `WorldEventTailSnapshot.FromLog`, `CombatEventTailSnapshot.FromLog`, `NarrationServices`, `NeedsSystemMoodTests` ve `Faz1AcceptanceReplayTests` benzeri golden'lar.
- **`World.Rumors`** — **yazar**: `living.rumors@Hourly:55` (**tek writer**, `FieldOwnershipRegistry.cs:87`). **okur**: `RumorMillSystem.PickFor` (dialog + ambient voice), `WorldSaveMapper` (save/load).
- **`World.RumorEventCursorSeq`** — **yazar**: `living.rumors@Hourly:55` (mill her tick sonunda `TotalAppended`'a atar). Ownership registry'de ayrı satır YOK (rumor pool ile aynı writer, deklare edilmiş sayılır). **okur**: sadece `RumorMillSystem.Tick` ve save mapper.
- **`World.Factions` (delta)** — `politics.faction_decay@Daily:40` + **`world.runtime_history@Daily:28`** (drift + chronicle) + boundary trade/dialog. Runtime history'nin delta yazması `_reputation.ApplyDelta` üzerinden, event'i de aynı çağrı yazar.
- **`World.Stockpiles[0]`** — caravan_surge branch buğday ekler (boundary-benzeri, month-end).
- **`World.Actors`** — `ArriveMigrants` yazar (`Add(loadout.Create(...))`). Bu, sim'in ilk runtime `ActorSpawned` emitter'ı (worldgen dışında).
- **Runtime-only (save DIŞI)**: `NpcEventEchoFeed.Ring[128]` + `Stamp` + `_writeIndex` — hepsi static, restart'ta sıfırlanır; loadta echo replay etmez (view `Bind`'de `_seenStamp = Stamp` çekiyor).

## LLD — Ürettiği/Tükettiği Olaylar

**Ürettiği** (`world.Events.Append`):
- `RuntimeHistorySystem.MonthlyChronicle` → `ChronicleEvent` (reason `"chronicle:{festival|caravan_surge|border_dispute} intensity:{n} day:{d}"`)
- `RuntimeHistorySystem.ArriveMigrants` → `ActorSpawned` (reason `"migrant_arrived name:{...} site:{...}"`)
- `_reputation.ApplyDelta` (bu sistemden çağrılınca) → `FactionReputationChanged` reason `"watch_renown" / "grain_tension" / "festival" / "border_dispute" / "chronicle_ripple"`

**Tükettiği**:
- `RumorMillSystem.Distill` — `GuardResponded`, `WitnessRecorded`, `PlantHarvested`, `TradeCompleted`, `ChronicleEvent`, `NeedChanged (vermin_theft / cat_catch / mauled_survives)`
- `RuntimeHistorySystem.DriftFromYesterday` — `GuardResponded`, `ShortageDetected`
- `DomainSimulationAdapter.PublishEventEchoes` — `WitnessRecorded`, `GuardResponded`, `PlantHarvested`, `ActorTalked`

**Emit-side effects (save/HUD)**:
- `NpcEventEchoFeed.Raise` — HUD ring (in-memory only, non-save)
- `AmbientVoiceDirector.Offer` — PiperTTS spatial line (18m earshot, 30s cooldown, 1 concurrent)

## Testler (bu sistemi pinleyen test dosyaları — W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Living/RumorMillSystemTests.cs` — 3 test: `Tick_DistillsEventsOnce_AndCursorNeverReMills`, `Tick_StaleRumors_ArePruned`, `PickFor_IsDeterministic_AndPrefersLocalTalk`
- `Assets/Tests/EditMode/Living/RumorMillCursorTrimTests.cs` — **B21 story pin (W35)**: `RumorMill_Cursor_SurvivesTrim_AndOnlyMillsFreshEvents` — 300 event seed → mill → trim 16 → 5 fresh → mill; `born == 5` ve kursor `TotalAppended`'a düşmüş.
- `Assets/Tests/EditMode/World/WorldEventLogTests.cs` — 201 satır: append invariant, empty view, `TrimOldest_UnderCap_IsNoop`, `TrimOldest_OverCap_DropsOldestAndAdvancesSeqBaseline`, `TryIndexForSeq` clamp senaryoları, restore ctor + TotalAppended replay
- `Assets/Tests/EditMode/World/WorldEventTests.cs` — 156 satır: ctor invariant testleri (None kind, empty ids, blank reason)
- `Assets/Tests/EditMode/World/RuntimeHistorySystemTests.cs` — 93 satır: `DriftFromYesterday` guard renown / grain tension, `MonthlyChronicle` day-30 gate, chronicle rng determinism, migrant arrival trigger
- `Assets/Tests/EditMode/World/FactionReputationDecaySystemTests.cs` — decay + runtime history etkileşimi
- `Assets/Tests/EditMode/Composition/WorldTickComposerReplayTests.cs` — replay determinism (event log tail + rumor list snapshot)
- `Assets/Tests/EditMode/Living/ColonyNeedsAcceptanceReplayTests.cs` — acceptance golden'ı: rumor'lar dahil narrative slice
- `Assets/Tests/EditMode/Visual/WorldEventTailSnapshotTests.cs`, `WorldEventInterestTests.cs`, `WorldEventNarratorTests.cs` — HUD projection consumer'ları
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — H3/H4 gate (event chain + chronicle mekanik etki)

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W32 — Action olayları çöp yığın olmaktan çıktı**: `WorldEventKind` sadece `ActionCompleted=33` + `ActionFailed=34`'ü ekledi; phase-step olayları KASITEN log'dan silindi. Yorum (`WorldEventKind.cs:47-49`): "phase steps live in `WorldState.ActionLog` (bounded ring); writing steps here would resurrect the B21 per-step spam class (~1GB log by day 90)".
- **W35 — B21 tasarımı formalleşti**: unbounded log gerçekliği inceleme masasına yatırıldı; MaxRetained=16384, seq-based cursor, Daily:99 trim step, altı-dosyalı yüzey planlandı.
- **W36 — B21 uygulama katmanı indi (kısmi)**:
  - `WorldEventLog` ctor + `TrimOldest` + `FirstRetainedSeq/TotalAppended/TryIndexForSeq` (`WorldEventLog.cs:24-126`).
  - `WorldState.RumorEventCursorSeq` — int index'ten **long seq**'e rename; `CopyFrom` L333 taşır.
  - `RumorMillSystem.Tick` — seq-clamp + `TryIndexForSeq` + `ScanCap=256` backfill guard (`RumorMillSystem.cs:26-40`).
  - `WorldSaveMapper` — `worldEventFirstRetainedSeq` diske girer (**tek** long), load `new WorldEventLog(firstRetainedSeq)` restore ctor'unu kullanır (`WorldSaveMapper.cs:87, 206`; `WorldSaveMapper.Narrative.cs:37-45`).
  - `RumorMillCursorTrimTests` — 300→trim(16)→5→mill senaryosu pin edildi.
- **W36 (aynı batch)** — B19/B21 fixleri f6c9e2d0 olarak `main`'e taşındı; observation 19836 "B21 Story Complete: WorldEventLog Seq-Based Trim + RumorMill Cursor Migration Across 12 Files" ve 19848 "W36 batch pushed to main as f6c9e2d0".
- **AÇIK KALAN**: Daily:99 **`WorldEventTrimStep`** composer'a takılmadı — `TrimOldest` sadece testlerde çağrılıyor (`grep .TrimOldest` = 4 hit, hepsi test). Kod yazıldı, koşan bir yerde çağrılmıyor. Bu, aşağıdaki bilinen borçların #1'i.

## Bilinen Borçlar + Kaçak Kapıları

1. **B21 yarım — trim çağıran YOK** (KRİTİK). API + seq contract + test pin var, ama `DefaultTickSystems.cs`'de `Daily,99` slotunda hiç step yok (`grep "Daily," DefaultTickSystems.cs` = 6 satır, hiçbiri trim). Runtime log hâlâ unbounded büyür — Jun 10 observation 12299 "WorldEventLog Is Unbounded Append-Only List Growing Since Tick 1" hâlâ ayakta. Fix: `WorldEventLogTrimStep` sınıfı + `Daily,99` cadence + `context.World.Events.TrimOldest(16384)`.
2. **`RumorMillSystem.ScanCap = 256` sessiz kayıp**: bir save 300+ unmilled event'le açılırsa (mill 3 gün çalışmamış vs.), en eski (Total − 256) event'in altı silinip kursor oraya çekilir → o event'lerden rumor doğmaz. Ne log'lanır ne diagnoz'lanır. En azından "skipped N older events" telemetrisi eklenmeli.
3. **`Distill` switch'i hard-coded string tablosu**: yeni bir `WorldEventKind` eklendiğinde otomatik olarak `null` döner (talk'a değmez). "Yeni event beklerken kimse konuşmuyor" bug'ı için bir kırmızı bayrak yok. Kind ↔ Distiller kayıt registrasyon defter tutulabilir.
4. **`RuntimeHistorySystem.DriftFromYesterday` scan floor = 8192**: log 8192'yi geçtikten sonra sondan eski günlere dönüşü keser — B21 trim'i (16384) devreye girdiğinde bile bu 8192 çakışmaz ama iki cadence çakışırsa (catchup burst içinde önce trim sonra history) drift eksik sayabilir. History cadence bugün trim'in ÖNCE mi SONRA mı olduğunu bilmediği için `Daily,28` vs `Daily,99` sıralama kritik.
5. **`MigrantNames` 8-name döngüsü** (`RuntimeHistorySystem.cs:139`): büyük dünya + uzun oyun → "Rill of the Road" x N tekrarı görülebilir. Deterministik ama zayıf.
6. **`NpcEventEchoFeed.LatestKindFor` O(128) her poll**: 500 aktör × 0.4s poll = 500 × 2.5Hz = 1250 tam-ring tarama/s = ~160k karşılaştırma/s. Bugün ihmal edilebilir ama kabalık limiti — actor→lastEcho map'i O(1)'e indirebilir.
7. **`NpcEventEchoView` sprite cache static + `PiperTTS`/`AmbientVoiceDirector.s_host` static**: Editor domain reload sırasında NullRef riski; play-mode enter'da ilk poll'de `s_eye != null` false görülebilir. Zararsız (tembel yaratılıyor) ama edge case.
8. **`WorldEventLog.Events` canlı view — snapshot değil**: caller iterating iken bir sim step Append yaparsa `IReadOnlyList` boyutu değişir. Presentation'da `PublishEventEchoes` bunu tek thread aynı tick içinde yaptığı için sorun yok; ama LLM/agent async tarafında bir gün UI thread'inde iterate + sim thread'inde Append riski var. Save mapper `Events` üzerinde `.Select` çağırıyor — büyük log'da tek allocation.
9. **`RumorEntry` alanları public field + no ctor**: hiçbir invariant yok (BornMinutes negatif, Text null geçebilir). Mill her zaman doğru yazıyor ama save mapper (`WorldSaveMapper.cs:266`) null-tolerant değil — data corrupt olursa NRE.
10. **`ChronicleEvent` reason string'i grep-lenebilir ama structured değil**: quest/UI kod `reason.Contains("festival")` gibi string-match yapıyor. Enum/tag alanı yok; kaçak kapı olarak future refactor'da `ReasonTrace` ile ayrılabilir.
11. **`AmbientVoiceDirector`'ın `RumorMillSystem.PickFor` boş dönüşü** (`_currentDialogLine = rumor ?? "Quiet lately..."` — dialog'da fallback var, ambient voice tarafında **`Offer(null)` → sessizce reddediliyor** ama tetikleyen event kayboluyor). Yani rumor havuzu boşken NPC ağzını tamamen kapatır — bir "no news" seed rumor'u atılmıyor.

# 04-cascades-crime

> Kapsam: predation + tanik + guard pursuit + site huzursuzluk supurmesi + yoldas topuk-takibi/koruma — kisacasi kasabanin GORULEN, HATIRLANAN, YANITLANAN suc zinciri.
> Ana dosyalar: `Assets/Scripts/Simulation/Living/CascadeSystems.cs` (299 satir) + `Assets/Scripts/Simulation/Living/CompanionSystem.cs` (153 satir).
> Yardimci: `Assets/Scripts/Domain/World/PursuitRecord.cs` (13 satir), `Assets/Scripts/Domain/World/SiteUnrestRecord.cs` (11 satir), `ScheduleSystem.TryResolvePursuit` (`ScheduleSystem.cs:80-102`).
> Tum satir referanslari 2026-07-26 calisma kopyasi (main @ `f6c9e2d0`) uzerinden dogrulandi.

## HLD - Ne ve Neden

CAN SUYU H3 bu sisteme ruh verdi: o gune kadar tepkisel davranis SADECE render pump'inda oturuyor ve YALNIZCA oyuncuyu takip ediyordu — NPC-vs-NPC imkansizdi, hicbir olay bir baskasini tetiklemiyordu. `PredationSystem` + `WitnessResponseSystem` predation'i simulasyona tasidi ve dunyanin ilk gercek zincirini kurdu: bir avci sivili PATAKLAR (`CombatResolved`) → yakindaki siviller GORUR (`WitnessRecorded` + `NpcMemory`'nin ilk runtime yazicisi) → tanik nobetciye kadar KOSAR ve rapor eder (dedup'lu ikinci `WitnessRecorded`) → nobet CHASE ederek yakinlasip vurur (`GuardResponded`). P0-P2 iyilestirmeleri bu zincirin uc gedigini kapatti: (P0) kovalama artik `PerTick`'te; (P1) siviller pataklanip 1 HP'de HAYATTA kalir — 58 gunluk marathon 3 kasabayi bosaltiyordu; (P2) kasabanin bir "huzursuzluk defteri" var ve esigi asinca TUM nobet supurmesi tetiklenir. V3 YOLDAŞ ayni felsefeyi partiye tasidi: yoldas yeni bir rol degil, aktarilmis kimlikli (adi + sprite'i + hafizasi kalir) bir sivildir — `CompanionIds` uyeliktir, davranis PerTick heel-follow + Hourly guard-strike'a bolunur. Butun kod stateless step instance (H1 dersi), tamamen Simulation katmani, Unity yok, RNG boundary stamp + iki ID'den turer (ayni dunya, ayni isirik). W35-W36'da PLANLANAN "guard+combat action slice" (RUH_TESHIS §2.4'un son iki puppet path'i) hala TASARIM asamasinda — `docs/ruh/w36/00-guard-combat-design.md` cizildi, kod yok; predation/witness dogrudan `CombatActionResolver.Resolve` cagirmaya devam ediyor (action lifecycle sistemi bunlari GORMEZ).

## HLD - Akış

1. **Tetik zamanlamasi** (`DefaultTickSystems.cs:374-386`):
   - `PredationStep` — `"living.predation"`, `Hourly:40`.
   - `CompanionGuardStep` — `"living.companion_guard"`, `Hourly:42`.
   - `WitnessStep` — `"living.witness"`, `Hourly:45`.
   - `CompanionFollowStep` — `"living.companion_follow"`, `PerTick:21` (order kasitli olarak `living.schedule@PerTick:20`'den SONRA — lagging yoldas'in karosunu follow step sahiplenir, jitter yok).
   - `ScheduleStep` — `"living.schedule"`, `PerTick:20`, `world.GuardPursuits`'i `Advance`'e verir (`DefaultTickSystems.cs:277`).
2. **Predation tick** (`CascadeSystems.cs:22-64`): `world.Actors.Records` uzerinde deterministik iterasyon; her hostile icin ONCE `StrikeReach=2` icindeki guard tetiklenir (`:39-49` — cascade'in ucuncu halkasi ONCE calisir; predation-once-guard-response sirasi kasitli, testle pinlenmis), sonra `HuntRadius=6` icindeki en yakin sivil av secilir (`:51-53`). `Chebyshev(hunter,prey) <= 2` ise `Strike`, aksi halde tek karoluk yaklasim adimi (`:56-63`).
3. **Deterministik zar** (`CascadeSystems.cs:67-77`): RNG seed `(TotalMinutes*2654435761) ^ (attackerId*97) ^ (targetId*193) | 1u` — ayni dunya + ayni saat + ayni cift = ayni zar. `CombatActionResolver.Resolve` `CombatResolved` olayini yazar, hasar bandi genisligi `max(1, BaseDamage/2)`.
4. **Mauled-survives contract** (`CascadeSystems.cs:82-90`): dusman VEYA nobet olmayan bir hedef 0 HP'ye duserse HP 1'de klanplenir, `NeedChanged` olayi `mauled_survives by:{id}` etiketiyle yazilir. 58 marathon gunu boyunca predation kasabalari bosaltmasin diye — playtest fix'i ("vardigimda kimse yoktu"); nobet + avci hala birbirini oldurur, ust populasyon kendini yine sinirlar.
5. **Witness tick** (`CascadeSystems.cs:148-215`): son `stamp - 60 dk` icindeki olay logu tarama, `scanFloor = max(0, events.Count - 4096)` derinlik kepi (marathon'da 50k+ olay birikince O(history) buyume kesildi; per-hour ~500 olay).
6. Her `CombatResolved` icin `WitnessRadius=8` icindeki HER sivil/nobetci (`attacker.Id`'ye esit olmayan, non-Enemy non-Player, `:161-166`):
   - `NpcMemory.GetOrCreate(witness.Id).RecordEvent("witnessed_attack", …)` — `NpcMemory`'nin ilk runtime yazma noktasi (`:168-171`).
   - `WitnessRecorded` olayi yazilir (`:172-173`).
7. **Depth-4 rapor** (`CascadeSystems.cs:177-198`): tanik en yakin `radius=16` guard'i arar; guard'a Chebyshev <=2 ise `reported_attack` bir kez memory'e yazilir (dedup: ayni attacker + ayni tanik = tek rapor — `:180-192`), yoksa tanik guard'a dogru bir karo yurur (mill in shock DEGIL). Bu adim iki-gunluk gate testinden once "gate kirilinca dedup yok" review-notu ile pinlendi.
8. **Watch converges** (`CascadeSystems.cs:201-215`): `ResponseRadius=12` icindeki HER guard icin `RegisterPursuit` (pursuit kaydi armlanir) + `RaiseUnrest(siteId, +2, …)` ; ayrica `Chebyshev>1` guard bir karo yaklasik yurur (kayit takim hemen hazir olmasa da guard visibly hareket eder).
9. **Pursuit resolve** (`ScheduleSystem.cs:69-77`): PerTick'te guard'in aktif pursuit'i varsa hedef = avin CANLI hucresi, `MovementService.StepToward` ile tam-tick hizinda; onceden 60:1 kaybediyordu — return-to-post yazicisiyla ayni kadansa cikinca kovalama gercekten kapatiyor (marathon: 1.8m closed / 2.6s vs eski ~5.7m).
10. **Pursuit budama** (`ScheduleSystem.cs:80-102`): `time > UntilMinutes` (varsayilan 120 dk), av olmus/yok, veya `>40 hucre` kacmis kayitlar `pursuits.RemoveAt(i)` — guard poste doner.
11. **Site sweep** (`CascadeSystems.cs:223-277`): `RaiseUnrest` her cagirimda `today > LastDecayDay` ise `-1/gun` ile eritilir, sonra `+amount`; `Unrest >= SweepThreshold(6)` VE `stamp >= SweepCooldownUntilMinutes` ise: (a) `SweepCooldownUntilMinutes = stamp + 1440 dk`, (b) `Unrest = 2` (defter havayi temizler, hafizayi silmez), (c) site sinirlarinin +4 karo cevresindeki HER guard'a `RegisterPursuit` — TOPLU sefer, (d) `ChronicleEvent` `watch_sweep guards:{n} target:{id}` yazilir. Cooldown icinde tekrar esige varilirsa `Unrest = SweepThreshold - 1`'e klanplenir — site "primed" kalir ama bir oyun-gununden fazla marsi tekrarlamaz (W30 wound-4: 5510 marathon supurme satiri fix'i).
12. **Companion follow** (`CompanionSystem.cs:72-108`): oncelikle olu yoldaslari `CompanionIds`'ten ters yonde tarayarak dusurur ve `companion_fell` yazar (ölum bir hikaye ani — M2). Sonra her uye icin `gap = Chebyshev(companion, player)`; `gap <= HeelCells(1)` ise dokunmaz; `gap > 2` ise CIFT ADIM (P0 re-pin — schedule/meal detour'lari heel'i asamasin), diger halde tek adim.
13. **Companion guard** (`CompanionSystem.cs:113-135`): her yoldas icin `NearestHostile(player, companion)` `GuardReachCells=2` icinde ise predation ile ayni deterministik RNG (`stamp * 2654435761 ^ compId*97 ^ threatId*193 | 1u`) ile `CombatActionResolver.Resolve` cagirilir — ayni motor, ayni zar; site fallback `PredationSystem.FallbackSite(world, threat.Position)`.
14. **Recruit/dismiss** (`CompanionSystem.cs:19-45`): `CompanionService.TryRecruit` `RecruitReachCells=3` ve `MaxCompanions=2` gate'ler; player veya enemy olan REDDEDILIR; basari `ActorTalked` + `companion_joined name:{name}`. `TryDismiss` cikarir + `companion_left name:{name}`.
15. **Save/load** (`WorldSaveMapper.cs:100-103, 120-123, 221-236`): `CompanionIds` bir dizi; `GuardPursuits` uc paralel dizi (GuardId/TargetId/UntilMinutes); `SiteUnrest` dort paralel dizi (SiteId/Unrest/LastDecayDay/SweepCooldownUntilMinutes — sonuncusu W30 sonrasi golden 777 ile seed'lendi).

## LLD - Veri Modeli

**Sabitler** — `Assets/Scripts/Simulation/Living/CascadeSystems.cs`:
- `PredationSystem.HuntRadius = 6` (`:19`) — avci sivili bu Chebyshev mesafeye kadar arar.
- `PredationSystem.StrikeReach = 2` (`:20`) — bu mesafede guard avciya vurur / avci ava vurur.
- `WitnessResponseSystem.WitnessRadius = 8` (`:145`).
- `WitnessResponseSystem.ResponseRadius = 12` (`:146`).
- `WitnessResponseSystem.SweepThreshold = 6` (`:221`).
- `WitnessResponseSystem.SweepCooldownMinutes = 1440` (`:222`) — bir oyun gunu.
- `WitnessResponseSystem.PursuitMinutes = 120` (`:281`) — guard pursuit'inin varsayilan gecerlilik penceresi.
- Witness tarama pencere kepi `scanFloor = max(0, events.Count - 4096)` (`:157`) — marathon O(history) borcu.

**Sabitler** — `Assets/Scripts/Simulation/Living/CompanionSystem.cs`:
- `CompanionService.MaxCompanions = 2` (`:18`).
- `CompanionService.RecruitReachCells = 3` (`:19`).
- `CompanionSystem.HeelCells = 1` (`:66`) — bu mesafede yoldas rahat durur.
- `CompanionSystem.GuardReachCells = 2` (`:67`) — hostile bu mesafedeyse yoldas vurur.

**PursuitRecord** (`Assets/Scripts/Domain/World/PursuitRecord.cs:8-13`) — `GuardId: ulong`, `TargetId: ulong`, `UntilMinutes: long`. Depo: `WorldState.GuardPursuits: List<PursuitRecord>` (`WorldState.cs:234`); kopyada referans paylasimi (`:326`).

**SiteUnrestRecord** (`Assets/Scripts/Domain/World/SiteUnrestRecord.cs:5-11`) — `SiteId: SiteId`, `Unrest: int`, `LastDecayDay: long`, `SweepCooldownUntilMinutes: long` (W30 alani). Depo: `WorldState.SiteUnrest: List<SiteUnrestRecord>` (`WorldState.cs:255`; kopyada `:334`).

**CompanionIds** — `WorldState.CompanionIds: List<ulong>` (`WorldState.cs:232`; kopyada `:325`).

**Uretilen olay etiketleri** (`WorldEvent.Detail` icinde):
- `guard_strikes_hunter target:{id}` — `CascadeSystems.cs:46`.
- `mauled_survives by:{id}` — `CascadeSystems.cs:90`.
- `witnessed attacker:{id}` — `CascadeSystems.cs:173`.
- `reported attacker:{id} guard:{id}` — `CascadeSystems.cs:192`.
- `watch_sweep guards:{n} target:{id}` — `CascadeSystems.cs:275`.
- `companion_joined name:{name}` — `CompanionSystem.cs:33`.
- `companion_left name:{name}` — `CompanionSystem.cs:42`.
- `companion_fell name:{name}` — `CompanionSystem.cs:88`.

## LLD - Fonksiyon Haritasi

**PredationSystem** (`Assets/Scripts/Simulation/Living/CascadeSystems.cs`):
- `int Tick(WorldState)` — `:22` — `world.Time`'i boundary stamp olarak overload'a devreder.
- `int Tick(WorldState, GameTime stamp)` — `:25-64` — hostile dongusu; guard-first strike sirasi, sonra av arama + yaklas/vur; sayaç geriye doner (dogrulama icin).
- `static void Strike(WorldState, resolver, action, attacker, target, GameTime stamp)` — `:66-91` — deterministik RNG ile `CombatActionResolver.Resolve`; sivil olumu klanplenir.
- `internal static ActorRecord Nearest(WorldState, GridPosition from, int radius, Func<ActorRecord,bool>)` — `:93-104` — Chebyshev'e gore en yakin filtre-uyan aktor (WitnessResponse ve CompanionSystem tarafindan da kullanilir).
- `internal static int Chebyshev(GridPosition, GridPosition)` — `:106-107`.
- `internal static SiteId FallbackSite(WorldState, GridPosition position)` — `:109-115` — pozisyonu iceren siteyi arar (`site.Contains`); yoksa `FallbackSite(world)`. B22'nin GERCEK cozumu — onceden ilk site sabit yaziliyordu.
- `internal static SiteId FallbackSite(WorldState)` — `:117-123` — herhangi bir site yoksa `SiteId(1)`.

**WitnessResponseSystem** (`Assets/Scripts/Simulation/Living/CascadeSystems.cs`):
- `int Tick(WorldState)` — `:148`.
- `int Tick(WorldState, GameTime stamp)` — `:151-217` — govde: log window scan (4096 depth cap), witness dongusu, rapor dedup, watch converge + `RegisterPursuit` + `RaiseUnrest`.
- `void RaiseUnrest(WorldState, SiteId, int amount, GameTime stamp, ulong attackerId)` — `:223-277` — decay + accumulate + threshold + cooldown clamping + sweep (site-scoped guard sec, +4 karo tolerans) + chronicle event.
- `void RegisterPursuit(WorldState, ulong guardId, ulong targetId, GameTime stamp)` — `:281-297` — guard basina en yeni bela kazanir; varsa `UntilMinutes` uzatir, yoksa yeni kayit.

**CompanionService** (statik — `Assets/Scripts/Simulation/Living/CompanionSystem.cs:17-60`):
- `bool TryRecruit(WorldState, ActorId)` — `:21-36` — cap/reach/role/canlilik gate; basari `ActorTalked` event.
- `bool TryDismiss(WorldState, ActorId)` — `:38-45`.
- `bool IsCompanion(WorldState, ActorId)` — `:47-48`.
- `ActorRecord FindPlayer(WorldState)` — `:50-56` — presentation'in proof yuzeyi de kullaniyor (public bilincli).
- `internal static int Chebyshev(GridPosition, GridPosition)` — `:58-59`.

**CompanionSystem** (`Assets/Scripts/Simulation/Living/CompanionSystem.cs:63-152`):
- `int TickFollow(WorldState)` — `:72-108` — olu-yoldas dusurme (reverse walk) + heel-follow; gap>2 ise cift-adim (P0 re-pin); sayac.
- `int TickGuard(WorldState, GameTime stamp)` — `:113-135` — her yoldas icin en yakin hostile bul + deterministik RNG ile `Resolve`.
- `static ActorRecord NearestHostile(WorldState, GridPosition player, GridPosition companion)` — `:137-150` — min(player-mesafe, companion-mesafe) `GuardReachCells` icinde en yakin Enemy.

**ScheduleSystem hosted karsiliklari** (`Assets/Scripts/Simulation/Living/ScheduleSystem.cs`):
- `void Advance(ActorStore, GameTime, List<PursuitRecord>, WorldState)` — `:44-77` — guard ise `TryResolvePursuit`, aksi halde `ChooseTarget`; nav-farkindali `StepToward`.
- `static bool TryResolvePursuit(List<PursuitRecord>, ActorStore, ActorRecord guard, GameTime, out GridPosition)` — `:80-102` — expiry/dead-quarry/>40 cell budama, canli hedef donusu.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Registry: `Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs`.

**Yazicilar** (declared, W35 sonrasi):
- `Actor.Vitals` (`:44-49`) → `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.companion_guard@Hourly:42` (uc yazici da `CombatActionResolver.Resolve` uzerinden).
- `World.GuardPursuits` (`:50-54`) → `living.witness@Hourly:45` (arm/refresh — `RegisterPursuit` iki callsite: witness converge + sweep) VE `living.schedule@PerTick:20` (resolve/prune — `TryResolvePursuit` in-place remove).
- `World.SiteUnrest` (`:88`) → `living.witness@Hourly:45` (tek yazici — `RaiseUnrest`).
- `World.CompanionIds` (`:111`) → `living.companion_follow@PerTick:21` (tek DECLARED yazici — olu-dusurme). NOT: `CompanionService.TryRecruit/TryDismiss` boot/dialog komut yolu — ledger "tick loop'ta kim yazar" kontratidir, bu carve-out `FieldOwnershipRegistry.cs:107-110` yorumunda kayitli.
- `World.NpcMemory` (`:106-110`) → `living.witness@Hourly:45` (tek DECLARED — `RecordEvent`); dialog/trade/ToolUse boundary yazilari kasitli UNDECLARED.
- `Actor.Mood` (`:101-105`) → witness dolayli olarak degistirmez; Kabuluk gerginlikte degil.

**Okuyucular** (kod yolu — registry'de olmayan `World.Events`, `World.Sites`, `World.Actors` genel okumalar):
- `World.Events` — witness log scan (`CascadeSystems.cs:154-179`); ambient/rumors da okur ama farkli sistemler.
- `World.Sites.Records` — `FallbackSite` (`:109-114`), sweep site-scope filtresi (`:262-269`).
- `World.Actors.Records` — hem predation, hem witness converge, hem companion tick.

## LLD - Ürettiği/Tükettiği Olaylar

**Uretilenler** (`WorldEventKind` — `Assets/Scripts/Domain/World/WorldEventKind.cs`):
- `CombatResolved` — `Resolve` cagrilari uzerinden (predation, witness'in dolayli tetikledigi guard-strike, companion guard). BU dosyada dogrudan `Events.Append` YOK — resolver yazar.
- `GuardResponded` — `CascadeSystems.cs:45` — guard avciya vurdugunda ("cascade'in ucuncu halkasi").
- `NeedChanged` — `:88` — mauled-survives etiketi (borç: NeedChanged bu semantik icin ideal degil ama enum sozlestirme yerine yeniden kullanildi).
- `WitnessRecorded` — `:171` (witnessed) ve `:190` (reported).
- `ChronicleEvent` — `:274` — sweep chronicle line, `RuntimeHistorySystem` bunu chronicle'a yigar.
- `ActorTalked` — `CompanionSystem.cs:33, 42, 87` — companion_joined/left/fell (yeni enum yerine mevcut kanal).

**Tuketilenler**:
- `CombatResolved` — `WitnessResponseSystem` son bir saatin log'undan tarar (`:154, :156`); attacker rol filtresi Enemy'e sinirlar (`:161` — player brawl'lari bounty sisteminin isi).

## Testler

- `Assets/Tests/EditMode/Living/CascadeSystemsTests.cs` (96 satir): `WitnessTick_SameAttackerTwice_FilesExactlyOneReport`, `PredationTick_CivilianCanNeverDieOfPredation_OnlyMauled`, `PredationTick_GuardInReach_StrikesTheHunterFirst` — review-mandated per-tick pins.
- `Assets/Tests/EditMode/Living/GuardPursuitTests.cs` (93 satir): `WitnessReport_ArmsAPursuit_ForGuardsInEarshot`, `Advance_PursuingGuard_ClosesEveryTick_InsteadOfRubberBanding`, `Advance_ExpiredPursuit_IsPruned_AndTheWatchGoesHome` — P0 (ARCHITECTURE_GAPS #2) pinleri.
- `Assets/Tests/EditMode/Living/SiteUnrestTests.cs` (101 satir): `RepeatedReports_CrossTheThreshold_AndTheWholeWatchSweeps`, `ContinuousTrouble_SweepsOncePerDay_AndReArmsAfterTheCooldown`, `Unrest_DecaysWithTheDays` — P2 defter + W30 cooldown pinleri.
- `Assets/Tests/EditMode/Living/CompanionSystemTests.cs` (138 satir, 7 test): `TryRecruit_NearbyCivilian_JoinsAndEmitsEvent`, `TryRecruit_BeyondReachOrOverCap_IsRefused`, `TickFollow_CompanionLagsBehind_StepsTowardThePlayer`, `TickFollow_CompanionAtHeel_HoldsPosition`, `TickGuard_EnemyBesideThePlayer_CompanionStrikesIt`, `TickFollow_CompanionDied_LeavesThePartyWithAFallenEvent`, `TryDismiss_Companion_LeavesAndEmitsEvent` — V3 TDD-first.
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` (337 satir, 10 gate): `Gate5_EventCascade_AnAttackIsSeenAndRemembered`, `Gate9_MemoryReachesTheTongue_WitnessedEventsEnterTheDialoguePrompt`, `Gate10_CompanionLoyalty_ThePartyHoldsThroughAFullDay` — integration gates (2-day sim).
- `Assets/Tests/EditMode/Actions/WorkOutputAuthorshipTests.cs` — companion pursuit carve-out'un `guards-eat` decision path'te bozulmadigini pinler.
- `Assets/Tests/EditMode/Memory/MemoryWriteSystemTests.cs` — NpcMemory yazma tarafi (witness tarafindan tetiklenen).
- `Assets/Tests/EditMode/World/RuntimeHistorySystemTests.cs` — `watch_sweep` chronicle'a girdisinin yazilmasi.

W32-W36 sirasinda bu sistem icin YENI story-test slice acilmadi (EAT/SLEEP/WORK/FARM story-test slice'lari ayri sistemlere aitti). Kaskadin sınav yeri hala iki-gunluk integration gate + yukaridaki 4 unit-test dosyasi.

## W32-W36 Değişiklikleri

- **W30 (`8c16b572`)** — Wound-4: `SweepCooldownUntilMinutes` alani `SiteUnrestRecord`'a eklendi, cooldown mantigi `RaiseUnrest`'e kondu (`CascadeSystems.cs:243-253`), save iki yone mapper'landi (`WorldSaveMapper.cs:123, 233`), golden 777 seed edildi. 5510 marathon `watch_sweep` satirinden kurtulundu — 12 saat sikinti = 1 sweep, ertesi gun tekrar armlanir.
- **W32 (`5049d445`)** — EAT slice: `ScheduleSystem` %50 kuculdu, `Idle`-only guard geldi (`:59-60` — `CurrentAction != None` ise atla); pursuit resolve/prune path'i etkilenmedi ama `Advance` imzasi `world` alarak nav-farkindali oldu (`ScheduleSystem.cs:44-46`) ve `GuardPursuitTests.cs:5` bu overload'a gore migrate edildi. Companion follow-order 21 (`schedule@20`+1) burada sabitlendi.
- **W33 (`61e340f3`)** — FARM slice: bu sistem dogrudan degismedi. `world.Stockpiles` yazicilarina `living.action_advance@PerTick:22` eklendi (`FieldOwnershipRegistry.cs:60`), cascade'in `NeedChanged` etiketleri farm-scoping'de yeniden kullanilmadi. NOT: `guards-eat` carve-out (W33-C task #11) `ActionLifecycleSystem.Decide`'e kondu — nobet AKTIF chase'de yemege GITMEZ (`ActionLifecycleSystem.cs:71` yorum).
- **W34 (`3aa87cf6`)** — SLEEP + WORK slice: puppet path'lerin retiring'i cascade'i dolayli etkiledi — projection'un GUESS(WORK) fallback'i kaldirildi (`W35` acilinda 2. sirada bitti); pursuit ve companion path'lerine dokunulmadi.
- **W35 (`20a3b899`)** — `FieldOwnershipRegistry`'nin 6 yeni satiri: `World.Time`, `World.Plants`, `Actor.Mood`, `World.NpcMemory`, `World.CompanionIds`, `World.Factions` DECLARED oldu (`:88-111`). Her declared yazici W33-C reverse lint'ini GECIYOR — bogus row build'i patlatir. Command-driven yazilar (recruit/dismiss) UNDECLARED birakildi + yorum. `ProofLivingCensus` yoldas + guard sayaclarini yigmayi ogrendi (soak: meals=6195, shortages=5296, guard-chase gorunur).
- **W36 (`f6c9e2d0`)** — RUH_TESHIS post-arch tail: `CascadeSystems.cs`'e 14 satir eklendi — B22 fix'i `FallbackSite(world, GridPosition)` overload'i (`:109-115`). Onceden `guard_strikes_hunter` ve `mauled_survives` `SiteId(1)`'e sabit yaziliyordu — artik pozisyonu iceren siteye. Test yok (SHIPPED-NO-TEST — `BUG_REPORT_SCORECARD.md:31`). Guard+combat action slice `docs/ruh/w36/00-guard-combat-design.md` cizildi ama kod YOK — implementation "content depth, next wave" olarak ertelendi. Ayrica `CompanionSystem.cs`'de 2 satir tail dokunusu (event narrator wire-up), `ScheduleSystem.cs`'de 12 satir (nav overload B10 tamamlamasi).

## Bilinen Borçlar + Kaçak Kapıları

1. **W36 guard+combat vertical slice: MACHINERY LANDED, COMPOSER DARK** (2026-07-26). Domain: `ActorIntent.Watch/Hunt`, `ActorActionType.OnWatch/Hunt/StrikeQuarry`, `HuntTargetRecord` + `WorldState.HuntTargets` ledger, `ActionKindDescriptors` 3 new rows. Simulation: `OnWatchAdvancer` (guard beat + pursuit interrupt), `HuntAdvancer` (60-min cadence, prey scan), `StrikeQuarryAdvancer` (60-min swing cadence, mercy clamp, cyclic NextLink), `CombatOperations` (Nearest/Chebyshev/ResolveStrike/MaybeMaulClamp), `ActionLifecycleSystem` `enableGuardAndCombat` ctor flag + `TryDecideWatch`/`TryDecideHunt` + Hunt↔StrikeQuarry NextLink loop. Composition: `FieldOwnershipRegistry` `World.HuntTargets` + `Actor.Vitals@PerTick:22` writers; `PredationSystem` gates hunter loop on non-None ActionState (guard-first-strike still runs). Presentation: `WorldProjection.DescribeScheduleWord` Guard/Enemy GUESS branches DELETED (verbs from `ActionVerbTable` verbatim now). Tests: `GuardOnWatchStoryTests` (3), `EnemyHuntStoryTests` (3). BUT — `DefaultTickSystems` keeps `enableGuardAndCombat: false` this commit; the flip needs a `LivingWorldGate Gate8` + `ProofLivingCensus` soak drift measurement + dated golden re-baseline first (see the commit's comment for the conditions). Chunking-invariance: UNCHANGED green.
2. **B22 fix'i test-sizdir** (SHIPPED-NO-TEST — `BUG_REPORT_SCORECARD.md:31`): `FallbackSite` overload'i `site.Contains(position)` ile calisir ama bunu pinleyen bir story testi yok. Test yazilana kadar regresyona acik.
3. **B24 SHIPPED-NO-TEST**: cascade'in yazdigi `Actor.Vitals` render tarafinda `ActorCombatFeedbackView` VARIANT B "renk arbiter" ile birden fazla yaziciyi tolere ediyor — story test yok, cascade tarafinda direct etki degil ama presentation kirsa cascade zincirinin gorunumu bozulur.
4. **`Actor.Vitals` uc yazici**: predation + witness + companion_guard AYNI Hourly'de yaziyor (`FieldOwnershipRegistry.cs:44-49`). Sirali (40/42/45) ama ayni tick'te bir aktoru birden fazla yazici HP dusurebilir; deterministik ama "kim vurdu?" sorusu ChronicleEvent'te kayipsizdir sadece `mauled_survives by:` bakisiyla. Race yok cunku hepsi single-thread.
5. **Witness scan cap 4096** (`CascadeSystems.cs:157`): tick basina en fazla 4096 son olay taranir; catchup + gerceklestirme yolunda hourly-then-daily interleaving cok yogun ise (nadir) dogru saati asan olaylar taranmadan gecebilir. Production ~500/hour, pratik olarak 4096 rahat kapsar. Borç: cap gelecekte configurable veya per-hour-index olabilir.
6. **Pursuit yumusak esikleri**: `>40 cell` kayip esigi ve `120 dk` gecerlilik penceresi sabitler — cok genis dungeon'larda quarry teleport'lu (F18 lair leash) bir hedefe kilitlenmis guard `>40` uzerinden hemen budanir. Bu istenen davranis ama tuning notu.
7. **`NeedChanged` overload'u**: `mauled_survives` semantigi icin uygun bir enum yok, `NeedChanged` yeniden kullanildi. Konsolide etme borç.
8. **Companion cift-adim heel**: `gap > HeelCells+1` ise iki adim (`CompanionSystem.cs:100`) — kalabalık kanal darligında P0 re-pin sırasında yoldas geriden gelen bir NPC'nin uzerinden gecebilir; nav-blocker'i takip etmez cunku `MoveTo` dogrudan cagirilir (StepToward yok).
9. **Watch converge move-step yalnizca `d>1`**: `d==1`'de guard yerinde durur (`CascadeSystems.cs:213`) — `d==0` gate'i yok ama zaten kovalayan `ResponseRadius=12` icindeki her guard hem move-step hem `RegisterPursuit` alir; PerTick pursuit resolve zaten one gecer, bu Hourly step'in `MoveTo`'su neredeyse gozardi edilebilir bir dokunustur — kaldirilirsa churn azalir.
10. **Save round-trip sirasi**: `WorldSaveMapper.cs:100-103` companion + pursuit + unrest icin paralel-diziler kullaniyor — `Count` uyusmazligi asserted DEGIL, sadece `Length`'lerden dolar. Bir array'in kirilmasi silent olarak fasit sonuca yol acar; `WorldSaveMapper` load path'inde `Math.Min(len1,len2,len3)` savunmasi olmali (W33-A'da benzer koruma isJobs icin geldi, buraya gelmedi).

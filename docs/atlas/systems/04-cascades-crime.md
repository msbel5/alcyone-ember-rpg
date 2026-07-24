# 04-cascades-crime

Kapsam: PredationSystem, WitnessResponseSystem, GuardPursuits, SiteUnrest (+ sweep + cooldown), CompanionSystem.
Ana kaynaklar: `Assets/Scripts/Simulation/Living/CascadeSystems.cs`, `Assets/Scripts/Simulation/Living/CompanionSystem.cs`, `Assets/Scripts/Simulation/Living/ScheduleSystem.cs` (pursuit tuketicisi), `Assets/Scripts/Simulation/Composition/DefaultTickSystems.cs` (kayit/kadans).

## HLD - Ne ve Neden

CAN SUYU H3'ten once tek reaktif davranis Presentation adaptorunde yasiyordu: render pompasinda kosuyor ve SADECE oyuncuyu avliyordu — NPC-vs-NPC imkansizdi ve hicbir olay baska bir olayi dogurmuyordu (`CascadeSystems.cs:8-14`). Bu dosyadaki sistemler dunyanin ilk gercek olay zincirini simulasyona tasir: bir avci (Enemy) bir sivili parcalar (CombatResolved) → yakindaki siviller bunu GORUR (WitnessRecorded + gercek bir ActorMemory kaydi — NpcMemory'nin ilk calisma-zamani yazari) → tanik muhafiza KOSAR ve rapor eder → rapor bir pursuit (kovalamaca) kurar ve kasabanin unrest defterine islenir → esik asilirsa TUM devriye sweep yapar (ChronicleEvent). Oyuncuya gorunen etkisi: kasaba meydaninda bir saldiri artik "sahne dekoru" degil; kalabalik kacar, muhafizlar gercekten kovalar ve NPC diyalogu yasananlari hatirlar (Gate9: bellek dile ulasir). CompanionSystem ayni felsefenin parti tarafi: yoldaslar yeni bir rol DEGIL, ise alinmis sivillerdir — kimliklerini, sprite'larini ve ActorMemory'lerini korurlar; oyuncunun yaninda dusmanlara predation'in ayni deterministik zarlariyla vururlar (`CompanionSystem.cs:8-13`). Tum sistemler H1 dersine uyar: stateless step instance'lari, deterministik zar (boundary stamp + aktor id'leri), saf Simulation katmani — render pompasina sifir bagimlilik.

## HLD - Akis

Kayit sirasi ve kadanslar (`DefaultTickSystems.cs:47-56` yorumlu liste; kesin kayitlar asagida):

| Adim | Kadans:Order | Kayit |
|---|---|---|
| living.schedule | PerTick:20 | `DefaultTickSystems.cs:219` |
| living.companion_follow | PerTick:21 | `DefaultTickSystems.cs:311` |
| living.predation | Hourly:40 | `DefaultTickSystems.cs:328` |
| living.companion_guard | Hourly:42 | `DefaultTickSystems.cs:319` |
| living.witness | Hourly:45 | `DefaultTickSystems.cs:337` |

Zincir, bir saatlik dongu icinde soyle akar:

1. **Predation (Hourly:40, tetikleyen: tick registry).** Her canli Enemy icin: once StrikeReach(2) icindeki EN YAKIN muhafiz avciya vurur (GuardResponded + CombatResolved; zincirin ucuncu halkasi one alinmis — kasabanin bosalmasini engelleyen fren, `CascadeSystems.cs:38-49`). Avci hayattaysa HuntRadius(6) icindeki en yakin sivili secer; StrikeReach icindeyse vurur, degilse 1 Chebyshev hucre yaklasir (`CascadeSystems.cs:51-65`).
2. **Mauled-survives kurali.** Predation sivili OLDURMEZ: 0 HP'ye dusen sivil 1 HP'de hayatta kalir ve `mauled_survives` (NeedChanged) damgasi alir; NeedRecovery iyilestirir. Avcilar ve muhafizlar birbirini oldurmeye devam eder — yirtici nufusu kendini sinirlar (`CascadeSystems.cs:81-91`, playtest fix "vardigimda kimse yoktu").
3. **CompanionGuard (Hourly:42).** Her yoldas, oyuncunun VEYA kendisinin GuardReachCells(2) icindeki en yakin dusmana predation'in ayni zar semasıyla vurur (`CompanionSystem.cs:110-136`).
4. **Witness (Hourly:45, tetikleyen: tick registry).** Son BIR SAATIN CombatResolved olaylari taranir (pencere `(stamp-60, stamp]`, derinlik tavani 4096 olay, `CascadeSystems.cs:135-148`). Saldirgan Enemy degilse atlanir (oyuncu kavgalari bounty sisteminin isi, `CascadeSystems.cs:150`). WitnessRadius(8) icindeki her canli, Enemy/Player olmayan aktor: `witnessed_attack` ActorMemory kaydi + WitnessRecorded olayi alir (`CascadeSystems.cs:159-164`).
5. **Rapor (Depth 4).** Tanik 16 hucre icindeki en yakin muhafiza KOSAR; muhafizin 2 hucresi icindeyse saldirgan basina bir kez `reported_attack` kaydi + ikinci bir WitnessRecorded uretir; uzaktaysa muhafiza dogru 1 hucre yurur (`CascadeSystems.cs:166-193`).
6. **Konverjans + pursuit + unrest.** ResponseRadius(12) icindeki her muhafiz: (a) `RegisterPursuit` ile 120 dakikalik bir kovalamaca kurar/tazeler (muhafiz basina TEK aktif pursuit, en yeni bela kazanir, `CascadeSystems.cs:271-289`); (b) `RaiseUnrest(+2)` ile sitenin suc defterini kabartir; (c) saldirgana dogru 1 hucre yurur (`CascadeSystems.cs:196-211`).
7. **Pursuit kosusu (PerTick:20, tetikleyen: ScheduleSystem).** Kovalamacayi WitnessResponse DEGIL, PerTick calisan ScheduleSystem kosar: aktif pursuit'i olan muhafizin hedefi avin CANLI hucresi olur ve posta-donus rotasini gecersiz kilar — saatlik itis tek basina posta-donus yazarina 60:1 kaybediyordu (ARCHITECTURE_GAPS #2 "pursuit is arithmetically erased"; `ScheduleSystem.cs:74-75`, `PursuitRecord.cs:15-19`). Suresi dolan / avi olen / 40+ hucre kacan pursuit yerinde budanir (`ScheduleSystem.cs:86-107`).
8. **Sweep (esik olayi).** Unrest >= SweepThreshold(6) olursa ve site cooldown'da degilse: o yerlesimin sinir+4 icindeki TUM canli muhafizlari ayni saldirgana pursuit kurar, bir ChronicleEvent (`watch_sweep guards:N target:X`) dusulur, unrest 2'ye iner ("sweep havayi temizler, hafizayi degil") ve site 1440 dakikalik (1 oyun gunu) sweep cooldown'una girer. Cooldown suresince unrest esigin hemen altinda (5) tutulur — kasaba "tetikte" kalir ama devriye gunde en fazla bir kez yuruyus yapar (`CascadeSystems.cs:216-268`, tuning: "sweep spam", 5510 marathon satiri).
9. **CompanionFollow (PerTick:21, schedule'dan SONRA).** Geride kalan yoldas oyuncuya dogru 1 (cok gerideyse 2) Chebyshev adim atar; boylece follow adimi o tick icin hucrenin son sahibi olur — schedule/follow titremesi yok (`CompanionSystem.cs:69-106`, `DefaultTickSystems.cs:308-312`). Olen yoldas rostadan SESLI dusurulur: `companion_fell` olayi (`CompanionSystem.cs:75-87`).
10. **Recruit/dismiss (tetikleyen: oyuncu etkilesimi, tick disi).** `CompanionService.TryRecruit/TryDismiss` — 3 hucre mesafe, en fazla 2 yoldas, Player/Enemy alinamaz (`CompanionSystem.cs:22-45`).

Catchup sozlesmesi: her iki kaskad sistemi de `Tick(world, stamp)` imzasiyla boundary stamp alir; zarlar ve olay damgalari bu stamp'ten turer. Witness taramasi erken KIRAMAZ — cok gunluk catchup'ta log stamp-monoton degildir (saatlik gecisler, gunluk gecislerin geri-doldurmasindan once eklenir) (`CascadeSystems.cs:25-26, 129-131`).

## LLD - Veri Modeli

**PursuitRecord** (`Assets/Scripts/Domain/World/PursuitRecord.cs:20-25`) — aktif bir muhafiz kovalamacasi:
- `ulong GuardId` — kovalayan muhafiz (muhafiz basina tek satir).
- `ulong TargetId` — av.
- `long UntilMinutes` — son kullanma ani (kurulusta stamp + 120 dk, `CascadeSystems.cs:272, 287`).

**SiteUnrestRecord** (`Assets/Scripts/Domain/World/SiteUnrestRecord.cs:5-11`) — yerlesimin suc basinci (DFU LegalRep-lite):
- `SiteId SiteId`
- `int Unrest` — rapor basina +2; esik 6.
- `long LastDecayDay` — tembel gunluk cozunme cizelgesi (gun basina -1, yalniz yazim aninda islenir, `CascadeSystems.cs:233-238`).
- `long SweepCooldownUntilMinutes` — bir sonraki sweep'e izin ani (stamp + 1440 dk).

**WorldState alanlari** (`Assets/Scripts/Domain/World/WorldState.cs`):
- `NpcMemoryStore NpcMemory` (:167) — tanik bellegi buraya yazilir.
- `List<ulong> CompanionIds` (:171) — parti uyeligi (rol degisimi yok).
- `List<PursuitRecord> GuardPursuits` (:173).
- `List<SiteUnrestRecord> SiteUnrest` (:181).

**ActorMemory / InteractionEvent** (`Assets/Scripts/Domain/Memory/`):
- `ActorMemory` (:13): 64 olaylik ring buffer (`MaxEvents = 64`, tasarsa en eski silinir, `ActorMemory.cs:16, 32-37`).
- `InteractionEvent` (readonly struct, `InteractionEvent.cs:13-40`): `Timestamp, EventType, ActorSeen, SubjectId, ItemTemplateId, Amount, Location`. Kaskadlarin yazdigi EventType'lar: `"witnessed_attack"` (SubjectId `"predation"`), `"reported_attack"` (SubjectId `"watch_report"`).
- `NpcMemoryStore.GetOrCreate(ActorId)` (`NpcMemoryStore.cs:19`).

**Sabitler:**
- PredationSystem: `HuntRadius=6`, `StrikeReach=2` (`CascadeSystems.cs:20-21`).
- WitnessResponseSystem: `WitnessRadius=8`, `ResponseRadius=12` (:124-125), rapor kosu yaricapi 16 / rapor mesafesi 2 (koda gomulu, :169, :173), `SweepThreshold=6` (:219), `SweepCooldownMinutes=1440` (:220), `PursuitMinutes=120` (:272), tarama derinlik tavani 4096 (:143).
- CompanionService/System: `MaxCompanions=2`, `RecruitReachCells=3` (`CompanionSystem.cs:19-20`), `HeelCells=1`, `GuardReachCells=2` (:65-66).
- ScheduleSystem pursuit: kayip esigi 40 hucre (`ScheduleSystem.cs:102`).

**Kalicilik** (`Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.cs`): CompanionIds (:95), pursuit paralel dizileri guardIds/targetIds/untilMinutes (:96-98), unrest paralel dizileri siteIds/values/lastDecayDays/sweepCooldowns (:108-111); geri yukleme :203-246. Golden roundtrip testi pinler (asagida).

## LLD - Fonksiyon Haritasi

- `PredationSystem.Tick(WorldState, GameTime) : int` — `CascadeSystems.cs:26` — saatlik av dongusu; vurulan darbe sayisini dondurur.
- `PredationSystem.Strike(world, resolver, action, attacker, target, stamp)` (private static) — `CascadeSystems.cs:70` — deterministik XorShiftRng (stamp*2654435761 ^ attackerId*97 ^ targetId*193 | 1) ile `CombatActionResolver.Resolve` cagirir; mauled-survives kuralini uygular.
- `PredationSystem.Nearest(world, from, radius, filter) : ActorRecord` (internal static) — `CascadeSystems.cs:94` — Chebyshev en-yakin canli aktor; KENDINI haric tutmaz (asagida borclara bkz).
- `PredationSystem.Chebyshev(a, b) : int` (internal static) — `CascadeSystems.cs:108`.
- `PredationSystem.FallbackSite(world) : SiteId` (internal static) — `CascadeSystems.cs:111` — site store'daki ILK siteyi dondurur; bos ise SiteId(1).
- `WitnessResponseSystem.Tick(WorldState, GameTime) : int` — `CascadeSystems.cs:132` — son saatin CombatResolved'larini tarar; yazilan tanik kaydi sayisini dondurur.
- `WitnessResponseSystem.RaiseUnrest(world, siteId, amount, stamp, attackerId)` (private static) — `CascadeSystems.cs:221` — tembel cozunme + esik + cooldown + sweep; sweep'te site sinir+4 filtresiyle tum devriyeye `RegisterPursuit`.
- `WitnessResponseSystem.RegisterPursuit(world, guardId, targetId, stamp)` (private static) — `CascadeSystems.cs:273` — muhafiz basina tek satir; var olani hedef+sure ile tazeler.
- `ScheduleSystem.Advance(actors, time, foodSpots, List<PursuitRecord> pursuits)` — `ScheduleSystem.cs:52-53` — PerTick rota secimi; pursuit'i olan muhafizin hedefi avin canli hucresi (:74-75). F18: pinli Enemy (home==dayAnchor) schedule'dan muaf (:64-68).
- `ScheduleSystem.TryResolvePursuit(pursuits, actors, guard, time, out target) : bool` (private static) — `ScheduleSystem.cs:86` — suresi dolmus / avi olmus / 40+ hucre kacmis satirlari yerinde budar.
- `CompanionService.TryRecruit(world, actorId) : bool` — `CompanionSystem.cs:22` — mesafe/kota/rol kapilari; `companion_joined` olayi.
- `CompanionService.TryDismiss(world, actorId) : bool` — `CompanionSystem.cs:38` — `companion_left` olayi.
- `CompanionService.IsCompanion(world, actorId) : bool` — `CompanionSystem.cs:47`.
- `CompanionService.FindPlayer(world) : ActorRecord` — `CompanionSystem.cs:50` — public: Presentation'daki proof yuzeyi de kullanir.
- `CompanionSystem.TickFollow(world) : int` — `CompanionSystem.cs:69` — olu yoldas temizligi (`companion_fell`) + heel-follow (gerekirse cift adim, :96-102).
- `CompanionSystem.TickGuard(world, stamp) : int` — `CompanionSystem.cs:110` — yoldas basina en fazla bir vurus; predation zar semasi.
- `CompanionSystem.NearestHostile(world, playerPos, companionPos) : ActorRecord` (private static) — `CompanionSystem.cs:138` — min(oyuncuya, yoldasa) Chebyshev <= 2 olan en yakin Enemy.
- `CombatActionResolver.Resolve(...) : CombatActionOutcome` — `Assets/Scripts/Simulation/Combat/CombatActionResolver.cs:25` — stamina kapisi → hit roll → damage roll → `defender.ApplyVitals` → CombatResolved olayi (:76-81). Kaskadlarin vitals'a dokundugu tek yol (mauled-survives istisnasi haric).

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry dili (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs`):

| Alan | Beyanli yazarlar | Registry satiri |
|---|---|---|
| `Actor.Position` | `living.schedule@PerTick:20`, `living.companion_follow@PerTick:21`, `living.predation@Hourly:40` (avci adimi), `living.witness@Hourly:45` (tanik kosusu + muhafiz konverjansi) | :18-25 |
| `Actor.Vitals` | `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.companion_guard@Hourly:42` | :32-37 |
| `World.GuardPursuits` | `living.witness@Hourly:45` (kurar/tazeler), `living.schedule@PerTick:20` (cozer/budar) | :38-42 |
| `World.SiteUnrest` | `living.witness@Hourly:45` | :52 |

Beyan DISI kalan gercek yazimlar (asagida borclara da islendi):
- `World.NpcMemory` — witness yazar (`CascadeSystems.cs:159-161, 181-182`); registry'de alan yok.
- `World.CompanionIds` — CompanionService.TryRecruit/TryDismiss (`CompanionSystem.cs:32, 40`) ve CompanionSystem.TickFollow olum temizligi (:83) yazar; registry'de alan yok.
- `Actor.Vitals` icin beyanli `living.witness@Hourly:45`: mevcut kodda WitnessResponseSystem vitals'a HIC yazmiyor (dosyada Resolve/ApplyVitals cagrisi yok) — fazla-beyan (stale gorunumlu; zararsiz ama defteri yaniltir).
- Okumalar: her iki kaskad `World.Events` logunu okur (witness tarama :142-148), `World.Actors.Records`, `World.Sites.Records` (FallbackSite :111-117, sweep siniri :254-256); ScheduleSystem `World.GuardPursuits` okur/budar.

## LLD - Urettigi/Tukettigi Olaylar

WorldEventKind kaynagi: `Assets/Scripts/Domain/World/WorldEventKind.cs`.

| Olay / log tag | Kind (satir) | Ureten | Tuketen |
|---|---|---|---|
| `combat_resolved action:predation ...` | CombatResolved=20 (:34) | PredationSystem → resolver | WitnessResponseSystem (tek kaskad tetigi, :148-150); RumorMill; sunum |
| `combat_resolved action:companion_guard ...` | CombatResolved=20 | CompanionSystem.TickGuard | Witness taramasi bunu ATLAR (saldirgan Enemy degil, :150) |
| `guard_strikes_hunter target:X` | GuardResponded=31 (:47) | PredationSystem (:45-46) | Gate5 log kaniti; sunum |
| `mauled_survives by:X` | NeedChanged=7 (:21) | PredationSystem (:89-90) | NeedRecovery anlatisi; log |
| `witnessed attacker:X` | WitnessRecorded=30 (:46) | WitnessResponseSystem (:162-163) | Gate5; diyalog boru hatti (Gate9) |
| `reported attacker:X guard:Y` | WitnessRecorded=30 | WitnessResponseSystem (:183-184) | log/chronicle |
| `watch_sweep guards:N target:X` | ChronicleEvent=32 (:49) | RaiseUnrest (:267-268) | chronicle; SiteUnrestTests |
| `companion_joined/left/fell name:X` | ActorTalked=2 (:16) | CompanionService/System (:33, :42, :85) | log; sunum |
| ActorMemory: `witnessed_attack`, `reported_attack` | (InteractionEvent, olay logu degil) | WitnessResponseSystem | NpcMemoryLlmEnvelope.RecallLines → LLM diyalog baglami (Gate9) |

Tuketim tarafi: WitnessResponseSystem YALNIZ `CombatResolved` + saldirgan rolu Enemy olanlari tuketir. WitnessRecorded olaylari bir sonraki saatte pencere disina duser (pencere alt siniri exclusive, :147) — kendi kendini tetikleyen dongu yok.

## Testler

- `Assets/Tests/EditMode/Living/CascadeSystemsTests.cs` — tek-rapor idempotensi (:35), mauled-survives (:61), muhafiz once vurur (:83).
- `Assets/Tests/EditMode/Living/GuardPursuitTests.cs` — rapor pursuit kurar (:35), kovalayan muhafiz her tick yaklasir / rubber-band yok (:52), suresi dolan pursuit budanir ve devriye posta doner (:75).
- `Assets/Tests/EditMode/Living/SiteUnrestTests.cs` — esik + toplu sweep (:41), gunde bir sweep + cooldown sonrasi yeniden kurulum (:61), gunlerle cozunme (:84).
- `Assets/Tests/EditMode/Living/CompanionSystemTests.cs` — recruit/kota/mesafe (:40, :54), heel-follow (:71, :86), guard strike (:97), olum-rosta cikisi (:112), dismiss (:128).
- `Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs` — Gate5 kaskad ucgeni: saldiri→tanik→muhafiz yaniti, en az 2 tanik + 2 bellek kaydi (:139-166); Gate9 bellek dile ulasir (:253); Gate10 yoldas sadakati tam gun (:275).
- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — her beyanli yazar tick registry'de var (:12), cekirdek mutable alanlarin sahipligi beyanli (:34) — "guard-pursuit sinifi catisma" (beyansiz hizli-kadans ikinci yazar) CI olayi olur.
- `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` — CompanionIds / GuardPursuits / SiteUnrest kaliciligini pinler.

## Bilinen Borclar + Kacak Kapilari

Hata ailesi harfleri `docs/SYSTEMS_ATLAS.md:52-59` taksonomisine gore.

1. **FallbackSite = tek-site atfi — sinif (a).** Predation'in tum CombatResolved/GuardResponded olaylari `FallbackSite(world)` yani store'daki ILK siteye yazilir (`CascadeSystems.cs:79, 111-117`); RaiseUnrest de `evt.SiteId` uzerinden ayni siteye isler (:206). Sonuc: cok-siteli dunyada TUM predation kaynakli unrest cografyadan bagimsiz ilk sitede birikir; sweep de o sitenin sinirlarıyla filtrelenir. Kirsalda islenen suc baskentin devriyesini yurutebilir. (CompanionGuard da ayni FallbackSite'i kullanir, `CompanionSystem.cs:132`.)
2. **Witness tarama derinlik tavani 4096 — bilincli kacak kapisi.** O(history) tam-tarama buyumesi (canli 6 dk kosuda ~50k olay) 4096-olay tavaniyla degistirildi (`CascadeSystems.cs:139-143`). Bir saat + catchup serpistirmesi 4096'yi asarsa penceredeki eski saldirilar SESSIZCE taniksiz kalir. Uretim saatlik hacmi ~500 — pay genis ama sinir izlenmiyor (tasma sayaci yok) — sinif (g).
3. **`Nearest` kendini haric tutmaz → muhafiz-tanik kendine rapor verir.** Muhafizlar tanik olabilir (filtre yalniz Enemy/Player'i eler, :155); tanik-muhafiz `Nearest(..., Role==Guard)` cagrisinda mesafe 0 ile KENDINI bulur ve kendi bellegine `reported_attack` yazar (:169-185). Zararsiz gorunuyor (rapor idempotent) ama "muhafiza kosma" sahnesinin anlamini bosaltir; dogrulanmadi: tasarim mi yan etki mi.
4. **Olu muhafizin pursuit satiri sizintisi.** `TryResolvePursuit` budamayi yalniz o muhafiz Advance dongusunde CANLIYKEN yapar (`ScheduleSystem.cs:60-62` olu aktor atlanir); muhafiz olurse PursuitRecord'u sonsuza dek `World.GuardPursuits`'ta kalir ve save'e yazilir. Kucuk ama birikimli — sinif (g).
5. **Fazla-beyan: `Actor.Vitals` ← `living.witness@Hourly:45`.** Registry beyan ediyor (`FieldOwnershipRegistry.cs:35`) ama mevcut WitnessResponseSystem vitals'a yazmiyor. Lint testi yalniz "beyanli yazar registry'de var mi" yonunu denetler (:12) — ters yon (beyan var, yazim yok) yakalanmaz. Defter okuyucusunu yaniltir.
6. **Beyansiz alanlar: `World.NpcMemory` ve `World.CompanionIds`.** Ikisi de gercek calisma-zamani yazimina sahip (witness :159-161; CompanionService :32/:40; TickFollow :83) ama FieldOwnershipRegistry'de alan olarak yok. Registry'nin varlik nedeni "beyansiz ikinci yazar" sinifini CI olayina cevirmekti (`FieldOwnershipRegistry.cs:5-10`) — bu iki alan korumasiz. Sinif (c) riskinin acik kapisi.
7. **Unrest cozunmesi tembel (yalniz yazimda).** `LastDecayDay` yalniz RaiseUnrest cagrildiginda islenir (:233-238); belasi biten site esik alti bir degerde donar. Esik altinda etkisiz ama save'de "hayalet gerginlik" olarak yasar — sinif (g), kozmetik.
8. **Olay-kind geri donusumu.** `mauled_survives` NeedChanged=7 uzerinde, yoldas uyelik olaylari ActorTalked=2 uzerinde tasinir — kind'a gore filtreleyen tuketiciler icin anlamsal gurultu; ozel kind yok. Sinif (g).
9. **CompanionGuard "uzaktan vurus" gorunumu.** Tehdit OYUNCUNUN 2 hucresi icindeyse yoldas mesafesine bakilmaksizin vurur (`CompanionSystem.cs:145-148` min(player, companion) semantigi — yorum :66 bunu acikca soyluyor, yani kasitli). Yoldas tehdide YURUMEZ; gorsel olarak vurusun kaynagi belirsiz kalabilir. Sunum tarafinda karsiligi dogrulanmadi.
10. **Muhafizlar postada yemek yer — belgeli basitlestirme.** Pursuit disinda muhafiz rotasi klasiktir ve "guards eat off-shift" ROADMAP_V2'ye loglanmis durumda (`ScheduleSystem.cs:126-129`).
11. **Cift `Chebyshev` kopyasi.** `PredationSystem.Chebyshev` (:108) ve `CompanionService.Chebyshev` (`CompanionSystem.cs:58`) ayni fonksiyonun iki kopyasi; ScheduleSystem.TryResolvePursuit uçuncu bir inline kopya tasir (:99-101). Davranis riski yok, bakim borcu.
12. **Predation oyuncuyu avlamaz.** Av filtresi Player'i disarida tutar (:52) — oyuncu tehdidi baska sistemde (bounty/presentation). Sinir bilincli ama bu dosyadan gorunmez; capraz referans dogrulanmadi.

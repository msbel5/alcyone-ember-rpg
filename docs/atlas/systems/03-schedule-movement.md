# 03-schedule-movement

> Kapsam: gunluk ritim + hareket — `ScheduleSystem`, day anchors, seat ring, `StepToward`, pursuit resolution.
> Ana dosya: `Assets/Scripts/Simulation/Living/ScheduleSystem.cs` (198 satir).
> Tum satir referanslari 2026-07-24 calisma kopyasina gore dogrulandi.

## HLD - Ne ve Neden

`ScheduleSystem` yasayan dunyanin "yuruyucusu"dur: her tick'te hayattaki her NPC'yi, o an ihtiyaclarinin sectigi davranisin hedefine dogru TEK karo yurutur (`ScheduleSystem.cs:14-15`). V1 saat-tabanli bir yonlendiriciydi ve 12:00-13:59 arasi HARDCODED bir ogle penceresi tasiyordu — denetimin "koreografi, davranis degil" dedigi kanonik ornek; CAN SUYU H2 bunu bir UTILITY SELECTOR ile degistirdi: her sivil kendi acligi/yorgunlugu ve saatten eat/rest/work/idle skorlarini uretir, kazanan davranisin hedefine yurur (`ScheduleSystem.cs:5-11`). Oyuncuya gorunen etkisi: sabah ise gidis, ogle vakti meydan masasina KENDILIGINDEN olusan kalabalik (aclik sabah boyunca birikip isi gecince), aksam eve donus, gece uyku — ve bir suc islendiginde nobetcilerin tam hizda kovalamasi. Felsefe: saf Domain/Simulation kodu — deterministik, Unity yok, I/O yok (`ScheduleSystem.cs:11`); kalabalik davranisi yazilmaz, IHTIYACLARDAN dogar. Hareket modeli kasitli olarak ilkel tutulmustur: yol bulma yok, tek eksende en fazla 1 adimlik Chebyshev yuruyusu (`ScheduleSystem.cs:188-196`).

## HLD - Akis

1. **Tetik**: `WorldTickComposer` tick'i → `ScheduleStep` (`"living.schedule"`, `TickCadence.PerTick`, order 20) `DefaultTickSystems.cs:211-222`. PerTick olmasi bilinclidir: `Advance` cagri basina bir karo yurutur; Hourly'de NPC'ler saatte bir karo "surunuyordu" ve ise/eve hic varamiyordu (`DefaultTickSystems.cs:216-218`).
2. `ScheduleStep.Run` yemek noktalarini toplar (`NeedConsumptionSystem.FoodSpots(world)`) ve `world.GuardPursuits` listesiyle birlikte `Advance`'e verir (`DefaultTickSystems.cs:224-233`).
3. **Aktor dongusu** (`ScheduleSystem.cs:51-82`): `actors.Records` uzerinde deterministik EKLENME sirasiyla iterasyon (`ActorStore.cs:96-99`). Olu aktorler atlanir (`:61-62`).
4. **F18 lair-guard muafiyeti**: `Home == DayAnchor` olan bir Enemy "pinned"dir (zindan sakini sozlesmesi) — gunluk ritmi yoktur, tek hareket ettiricisi dusman-kovalama AI'sidir; her tick eve geri adimlatmak aktif kovalamayi lastik gibi geri cekiyordu (`ScheduleSystem.cs:64-68`). Eve donusunu `DomainSimulationAdapter.WorldEncounter.cs:246-262` (lair leash) yapar.
5. **Pursuit cozumu**: aktor Guard ise ve `TryResolvePursuit` aktif bir kovalama bulursa hedef = avin CANLI hucresi, tam tick hizinda (`ScheduleSystem.cs:74-75`). Cozum sirasinda suresi dolmus / avi olmus / >40 hucre kacmis kayitlar listeden yerinde budanir (`:84-106`).
6. **Utility secimi**: diger herkes icin `ChooseTarget` (`ScheduleSystem.cs:117-148`). Guard ve Enemy klasik yonlendirmede kalir (`:126-127`, `:150-156`); siviller icin eat/rest/work/idle skorlanir, beraberlik sirasi deterministik: eat > rest > work > idle (`:140-147`).
7. **Seat ring**: kazanan davranis "eat" ise hedef, food spot'un kendisi degil, cevresindeki Chebyshev ring-2'nin 16 koltugundan `seatOrdinal`'a dusen hucredir (`:141-142`, `:171-186`). `seatOrdinal` dongudeki sivil sirasindan uretilir (`:58`, `:77`) — ortak hedefler tek karoya yigilmak yerine dagilir ("birbirlerinin uzerinden yuruyorlar" düzeltmesi, `:70-72`).
8. **Adim**: `StepToward` ile tek karoluk 8-yonlu adim; pozisyon degistiyse `actor.MoveTo(next)` (`:78-80`, `:191-196`).
9. **Ayni tick'in devami**: order 21 `living.companion_follow` (yoldas topuk takibi, kasitli olarak schedule'dan SONRA — `DefaultTickSystems.cs:306-313`), order 22 `living.eatOnArrival` (masaya VARAN ac sivil ayni tick yemek yer; saatlik adimi beklerken masada dikilme yigilmasinin cozumu — `DefaultTickSystems.cs:252-263`, `NeedConsumptionSystem.cs:59-83`).
10. **Pursuit'un silahlanmasi (yukari akis, Hourly)**: `WitnessResponseSystem` (`"living.witness"`, Hourly, order 45 — `DefaultTickSystems.cs:333-337`) son bir saatin `CombatResolved` olaylarini tarar; menzildeki (ResponseRadius 12) her guard icin `RegisterPursuit` cagirir (`CascadeSystems.cs:204`), ayrica site huzursuzluk esigi asilirsa TUM nobet supurmesi de her guard'a pursuit takar (`CascadeSystems.cs:264`). Kayit 120 dakika gecerlidir, guard basina tek kayit, en yeni bela kazanir (`CascadeSystems.cs:272-288`).
11. **Anchor'larin dogumu (worldgen, tek seferlik)**: her NPC'ye site sinirlari icinde hash ile dagitilmis bir `Home` hucresi ve FARKLI hash'le dagitilmis bir `DayAnchor` verilir — gunduz herkes kendi noktasina dagilir, merkezde donmus tek kume olusmaz (`DomainSimulationAdapter.Worldgen.Npcs.cs:33-40`, `:70-83` HomeCellFor, `:84-100` DayAnchorFor).

## LLD - Veri Modeli

**ScheduleSystem sabitleri** (`Assets/Scripts/Simulation/Living/ScheduleSystem.cs`):
- `WorkStartHour = 6` (dahil) — `:18`; `WorkEndHour = 20` (haric) — `:21`.
- `WorkScore = 55`, `IdleScore = 35`, `NightRestBonus = 25` — `:26-28`. Denge notu: aclik +20/saat ratchet'i tokken ogleye dogru 55'i gecer, aksam fatigue + gece bonusu her seyi gecer (`:23-25`).
- `SeatOffsets`: food spot etrafindaki Chebyshev ring-2'nin 16 hucresi; ic 3x3 KASITLI olarak bos birakilir cunku meydan masasi + banklar oraya render edilir ("masanin uzerine cikip eating" canli buginin cozumu); her koltuk merkeze tam `EatReachCells` (2) mesafede, yemek her koltuktan basarili — `:171-180`.

**PursuitRecord** (`Assets/Scripts/Domain/World/PursuitRecord.cs:8-13`): `GuardId: ulong`, `TargetId: ulong`, `UntilMinutes: long`. Amac dokumantasyonu: "witness report yazar; PerTick schedule okur — kovalama, poste-donus yazicisiyla AYNI kadansta kosar, 60:1 kaybetmez" (`:3-7`).
- Depo: `WorldState.GuardPursuits: List<PursuitRecord>` (`WorldState.cs:173`); kopya kurucuda referans paylasimi (`WorldState.cs:252` — dosyadaki tum liste alanlariyla ayni stil).
- Kalicilik: `WorldSaveMapper.cs:96-98` (save: uc paralel dizi) / `:203-210` (load).

**ActorScheduleState** (`Assets/Scripts/Domain/Actors/ActorScheduleState.cs`): immutable struct — `CurrentJobId` (`:43`), `TargetSiteId` (`:46`), `TargetWorksitePosition` (`:49`), `IsIdle => CurrentJobId.IsEmpty` (`:52`). Yazari bu sistem DEGIL, `JobAssignmentSystem`'dir (`:8-9` tasarim notu); ScheduleSystem sadece okur.

**ActorRecord ilgili alanlar** (`Assets/Scripts/Domain/Actors/ActorRecord.cs`): `Position` (`:70`), `Home` (`:71`), `DayAnchor` (`:72`), `IsAlive => !Vitals.IsDead` (`:78`), `ScheduleState` (`:82`), `Needs` (`:83`), `MoveTo(GridPosition)` (`:87-90`). Kurucuda `home`/`dayAnchor` verilmezse spawn pozisyonuna duser (`:48-49`) — `Home == DayAnchor` esitligi F18 "pinned" sozlesmesinin ta kendisidir.

**GameTime** (`Assets/Scripts/Domain/Core/GameTime.cs`): `TotalMinutes` (`:37`), `Hour = (TotalMinutes/60) % 24` (`:41`).

**Komsu sabitler** (`Assets/Scripts/Simulation/Living/NeedConsumptionSystem.cs`): `HungerEatThreshold = 55` — kasitli olarak `WorkScore` ile hizali (`:16`); `EatReachCells = 2` (`:17`).

## LLD - Fonksiyon Haritasi

Hepsi `ScheduleSystem.cs` icinde, aksi belirtilmedikce:

- `void Advance(ActorStore, GameTime)` — `:30-33` — food spot'suz geri-uyum sarmalayicisi.
- `void Advance(ActorStore, GameTime, GridPosition? foodSpot)` — `:37-42` — H2 tek-larder sarmalayicisi; pencere yok, acligin kendisi karar verir.
- `void Advance(ActorStore, GameTime, IReadOnlyList<GridPosition> foodSpots)` — `:46-47` — coklu-larder sarmalayicisi (herkes EN YAKIN noktaya; bir kasabanin ogle kalabaligi baska kasabanin masasina yurumez).
- `void Advance(ActorStore, GameTime, IReadOnlyList<GridPosition>, List<PursuitRecord>)` — `:51-82` — gercek govde: aktor dongusu, pinned-enemy muafiyeti, pursuit oncelik dallanmasi, seat ordinal sayaci, adim + `MoveTo`.
- `static bool TryResolvePursuit(List<PursuitRecord>, ActorStore, ActorRecord guard, GameTime, out GridPosition)` — `:86-106` — guard'in aktif kovalamasini avin canli hucresine cozer; suresi dolan (`:95`), avi olmus/yok (`:96-97`), >40 hucre kacan (`:101`) kayitlari yerinde siler (silme sonrasi `false` doner → guard o tick poste doner).
- `static bool IsWorkHour(GameTime)` — `:109-113` — `hour >= 6 && hour < 20`.
- `static GridPosition ChooseTarget(ActorRecord, GameTime, GridPosition? foodSpot)` — `:117-118` — testlerin hareket simule etmeden karar tablosunu pinlemesi icin public sarmalayici (seatOrdinal=0).
- `static GridPosition ChooseTarget(ActorRecord, GameTime, GridPosition?, int seatOrdinal)` — `:120-148` — H2 utility cekirdegi. Onemli detay: `HungerEatThreshold` altindaki aclik icin eat skoru -1'e sabitlenir, cunku `TryEat` o aclikta zaten REDDEDIYORDU ama esik-alti aclik yine de idle/rest'i gecip butun kasabayi masada ac-ama-yeterince-ac-degil bekletiyordu ("herkes town merkezinde" playtest fix'i — `:129-135`).
- `static GridPosition ClassicTarget(ActorRecord, bool workHour)` — `:150-156` — Guard/Enemy: mesai disinda `Home`; mesaide idle ise `DayAnchor`, degilse `TargetWorksitePosition`.
- `static GridPosition? NearestSpot(IReadOnlyList<GridPosition>, GridPosition)` — `:158-169` — Chebyshev mesafeyle en yakin larder.
- `static GridPosition Seat(GridPosition table, int seatOrdinal)` — `:182-186` — `seatOrdinal mod 16` ile ring koltugu.
- `static GridPosition StepToward(GridPosition from, GridPosition to)` — `:191-196` — eksen basina `Math.Sign` ile tek karoluk, asla hedefi asmayan (monoton yakinsayan) 8-yon adimi. Engel/zemin kontrolu YOKTUR.

Yukari/asagi akis komsulari:
- `WitnessResponseSystem.Tick(WorldState, GameTime)` — `CascadeSystems.cs:132-214` — pursuit'lari silahlar (`:204` yakin-menzil, `:264` sweep).
- `static void RegisterPursuit(WorldState, ulong guardId, ulong targetId, GameTime)` — `CascadeSystems.cs:273-289` — guard basina tek kayit; var olani tazeler (`PursuitMinutes = 120`, `:272`).
- `static List<GridPosition> NeedConsumptionSystem.FoodSpots(WorldState)` — `NeedConsumptionSystem.cs:223-244` — yemek tutan tum stok yiginlarinin site merkezleri.
- `NeedConsumptionSystem.TickArrivals(WorldState, GameTime)` — `NeedConsumptionSystem.cs:62-83` — varista yemek (PerTick:22, schedule'in hemen ardindan).

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` dilinde (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs`):

**Yazdigi:**
- `Actor.Position` ← `living.schedule@PerTick:20` ("routine movement", `FieldOwnershipRegistry.cs:20`). Ayni alanin diger beyan edilmis yazarlari: `living.companion_follow@PerTick:21` (kasitli olarak SONRA), `living.predation@Hourly:40`, `living.witness@Hourly:45`, `living.ambient@Hourly:50` (`:21-24`).
- `World.GuardPursuits` ← `living.schedule@PerTick:20` rolu "resolves/prunes"; `living.witness@Hourly:45` rolu "arms/refreshes" (`:38-42`). Guard-pursuit sinifi catisma (farkli kadansta beyan edilmemis ikinci yazar) artik lint testiyle CI olayidir (`:6-11`).

**Okudugu (registry disi, koddan dogrulandi):**
- `World.Time` — `context.Stamp` uzerinden (`DefaultTickSystems.cs:231`); `GameTime.Hour` ve `TotalMinutes`.
- `Actor.Needs.Hunger / Fatigue` — `ScheduleSystem.cs:133-136`.
- `Actor.ScheduleState` (`IsIdle`, `TargetWorksitePosition`) — `:137`, `:146`, `:154-155`.
- `Actor.Home`, `Actor.DayAnchor`, `Actor.Role`, `Actor.IsAlive`, `Actor.Position` — `:61-77`, `:144-147`.
- `World.Stockpiles` + `World.Sites` — dolayli, `FoodSpots` araciligiyla (`NeedConsumptionSystem.cs:223-244`).
- `World.GuardPursuits` — okuma + budama (`ScheduleSystem.cs:86-106`).

## LLD - Urettigi/Tukettigi Olaylar

- **ScheduleSystem'in kendisi HICBIR WorldEvent uretmez** — 198 satirlik dosyada `Events` erisimi yok (tam dosya okumasiyla dogrulandi). Hareket sessizdir; gorunurluk Presentation projeksiyonundan gelir.
- **Tukettigi (dolayli)**: pursuit zinciri `WorldEventKind.CombatResolved` olaylarindan dogar — ama onlari okuyan ScheduleSystem degil, Hourly `WitnessResponseSystem`'dir (`CascadeSystems.cs:148`).
- **Komsu uretimler** (bu ritmin gorunen izleri): `WorldEventKind.WitnessRecorded` (`CascadeSystems.cs:162`, `:183`), `WorldEventKind.ChronicleEvent` log tag `watch_sweep` (`CascadeSystems.cs:267-268`).
- **Presentation tuketicileri**: `ActorView` GridPosition'i dunya uzayina projekte eder (`ActorView.cs:20`), `NpcActivityLabelView` aktivite fiilini gosterir (`NpcActivityLabelView.cs:8`), `DomainSimulationAdapter.WorldProjection.DescribeActivity` saat pencerelerinden fiil uretir (`WorldProjection.cs:108-127`).

## Testler

- `Assets/Tests/EditMode/Living/ScheduleSystemTests.cs` — 13 test, sistemi en genis pinleyen dosya: mesai ici worksite'a tek karo (`:25`), asimsiz yakinsama (`:38`), mesai disi hareketsizlik (`:55`), idle aktorun DayAnchor'a yuruyusu (`:79`), gece eve donus (`:91`), "aclik saati degil saat acligi" — pencere olmadan food spot'a gidis (`:109`, H2 notu `:104`), utility karar tablosu (`:144`, `:142`), pinned lair-guard yerinde kalir (`:166`), commuting enemy gece eve yurur (`:181`), esik-alti aclik ASLA food spot'u hedeflemez (`:197`), seat ordinal 0-15 → 16 FARKLI ring koltugu, asla masa ustu (`:212`), iki larderden en yakinina yuruyus (`:240`).
- `Assets/Tests/EditMode/Living/GuardPursuitTests.cs` — P0 pursuit pinleri: witness raporu menzildeki guard'lara pursuit takar (`:35`), kovalayan guard lastiklenmek yerine HER tick yaklasir (`:52`), suresi dolan pursuit budanir ve nobet eve doner (`:75`).
- `Assets/Tests/EditMode/Composition/FieldOwnershipRegistryTests.cs` — sahiplik lint'i: registry'deki her yazar tick kayitlarinda var mi / her Position-yazari beyan edilmis mi (`:23`, `:38`).
- `Assets/Tests/EditMode/Composition/WorldTickRegistryTests.cs` — kanonik kadans/sira dizilimi (`:44`; `living.schedule@PerTick:20` bu dizilimin parcasi).
- `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` — `GuardPursuits` save/load altin gidis-donusu (mapper alanlari `WorldSaveMapper.cs:96-98`, `:203-210`).

## Bilinen Borclar + Kacak Kapilari

1. **Yol bulma yok — duvarlarin icinden yurume** [aile (b)/(d) bitisigi]: `StepToward` saf isaret-adimidir (`ScheduleSystem.cs:191-196`); zemin, bina, su, baska aktor kontrolu yoktur. Sim tarafinda carpismasiz grid varsayimi bilincli, ama Presentation'da bina/engel var — NPC'lerin katı geometriden gecmesi bu bosluktan dogar.
2. **Guard'lar hic yemek yemez** [beyan edilmis basitlestirme]: "the watch holds its post (guards eat off-shift — an honest simplification, logged in ROADMAP_V2)" (`ScheduleSystem.cs:124-127`). Guard acligi utility'ye hic girmez; `NeedConsumptionSystem` de Enemy'yi atlar ama Guard'i atlamaz — Hourly adimda masadan uzaktaysa `EatReachCells` nedeniyle yemek yine basarisiz olabilir (dogrulanmadi: guard acliginin uzun vadede ne yaptigi kosturulup olculmedi).
3. **Olu ogle penceresinin hayaletleri Presentation'da yasiyor** [aile (a) tek-dogru ihlali]: H2 pencereyi oldurdu (`ScheduleSystem.cs:6-11`) ama `WorldProjection.DescribeActivity` hala hardcoded 12-14 penceresi + kopyalanmis `hunger >= 55` sabitiyle fiil uretiyor (`DomainSimulationAdapter.WorldProjection.cs:108-124`; ustelik yorum "Windows MUST match ScheduleSystem (work 6-20, lunch 12-14)" diyor — artik eslesecek pencere yok). `NpcPoseIconView` de "12:00-13:59, matching ScheduleSystem's lunch window" iddiasindadir (`NpcPoseIconView.cs:6-9`). Fiil/ikon, simin gercek kararindan degil saat tahmininden geliyor — aclik erken/gec kazandiginda soz yalan soyler.
4. **Seat ordinal kimlik degil dongu-pozisyonu** [aile (a) bitisigi]: `seatOrdinal` her `Advance`'te canli aktorler uzerinde sayilarak uretilir (`ScheduleSystem.cs:58`, `:77`) — deterministik ama bir aktor OLDUGUNDE ondan sonraki herkesin koltugu kayar; yemek ortasinda toplu koltuk degisimi mumkun. Ayrica pursuit'e giren guard'lar sayaci ilerletmez, o da alt-siradakilerin ordinalini oynatir. (Gorsel etkisi playtest'te raporlanmadi — dogrulanmadi.)
5. **16 koltuk siniri**: 16'dan fazla es-zamanli ac sivilde `seatOrdinal % 16` sarmalar (`ScheduleSystem.cs:184`) — koltuk paylasimi ve yigilma geri doner. Rezervasyon/doluluk kavrami yok.
6. **Pursuit sabitleri gomulu**: kayip esigi 40 hucre (`ScheduleSystem.cs:101`), sure 120 dk (`CascadeSystems.cs:272`), sweep siniri site sinir+4 (`CascadeSystems.cs:260-263`) — hicbiri config'te degil.
7. **`TryResolvePursuit` tek-kayit sozlesmesine ortulu bagimli**: budama sonrasi `return false` yapar, taramaya devam etmez (`ScheduleSystem.cs:95-97`, `:101`). `RegisterPursuit` guard basina tek kayit garantiler (`CascadeSystems.cs:276-282`) — ama bu garanti baska bir dosyada yasar; ikinci bir yazar cift kayit eklerse ikincisi sessizce golgede kalir. Kacak kapisi: sozlesme testle degil yorumla korunuyor.
8. **`WorldState` kopya kurucusu `GuardPursuits` referansini paylasir** (`WorldState.cs:252`) — dosyadaki tum liste alanlariyla tutarli stil, ama PerTick budama yapan bir listenin kopyalar arasi paylasimi, kopyayi kullanan herhangi bir gelecek tuketici icin aliasing tuzagidir (bugun canli bir hata uretmiyor — dogrulanmadi).
9. **Cadence-catismasi tarihi (cozuldu, aile (c) flagship)**: pursuit bir zamanlar Hourly nudge ile yurutuluyor ve PerTick poste-donus yazicisina 60:1 kaybediyordu ("pursuit is arithmetically erased", ARCHITECTURE_GAPS #2). Cozum `PursuitRecord` + ayni kadansta cozum (`PursuitRecord.cs:3-7`) + `FieldOwnershipRegistry` lint'i (`FieldOwnershipRegistry.cs:6-11`). Ayni ailenin ikinci uyesi (PerTick yurume / Hourly yemek) `EatOnArrivalStep` ile kapatildi (`DefaultTickSystems.cs:252-259`).
10. **`IsWorkHour` mantigi Presentation'da elle kopyalanmis**: `WorldProjection` saat hesabini `GameTime.Hour` yerine `(TotalMinutes/60)%24` olarak yeniden yazar (`WorldProjection.cs:101`, `:112`) — kucuk ama aile (a) tohumu.
11. **Hedefe varan aktor icin "isleme" kavrami yok**: worksite'a varan aktor orada sadece durur; uretimi `JobAssignmentSystem`/uretim sistemleri ayri yurutur. `ChooseTarget` idle aktoru mesai boyunca `DayAnchor`'da bekletir (`ScheduleSystem.cs:147`) — meydan kalabaliginin "donuk" gorunmesinin sim-tarafi nedeni.

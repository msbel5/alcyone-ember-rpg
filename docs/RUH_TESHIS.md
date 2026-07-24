# Ember — “Dünyanın Ruhu Neden Yok?” Mimari Teşhis

## Karar

Sorun merkezî bir `WorldTickComposer` bulunması değildir. Deterministik bir simülasyonda merkezî saat ve faz sıralaması yararlıdır. Sorun, bu merkezin metronom olmakla kalmayıp aktörlerin niyetini, hareketini ve sonuçlarını ayrı ayrı dışarıdan değiştiren sistemlerin kuklacısına dönüşmesidir.

Mevcut yapı klasik zengin OOP değildir; fakat temiz bir ECS/data-oriented tasarım da değildir. En doğru tanım:

> **Anemik aktör kayıtları + dev ortak mutable dünya + aynı alanlara yazan geniş sistemler + kalıcı eylem durumunun yokluğu.**

Bu nedenle oyun “Ayşe yemek yedi” olayını simüle etmez. Şu sayaç değişikliklerini aynı tick dizisinde yapar:

1. Açlık yükselir.
2. Başka bir sistem hedef seçer.
3. Başka bir sistem konumu değiştirir.
4. Başka bir sistem stoktan bir sayı siler.
5. Aynı sistem açlık sayısını düşürür.
6. Olay günlüğüne sonradan `meal_eaten` yazılır.
7. Görsel katman konum ve saate bakarak “eating” etiketi tahmin eder.

Ortada kalıcı bir `EatAction`, ayrılmış bir ekmek, eylem aşaması, süre, başarısızlık veya kesilme yoktur.

---

## 1. Merkezî manager neden tek başına suçlu değil?

### Ember

`Assets/Scripts/Simulation/Composition/WorldTickComposer.cs:18-34` merkezî saati ve sıralı cadence bantlarını yönetir. Bu rol doğrudur:

- zamanı ilerletmek,
- deterministik sırayı korumak,
- saatlik/günlük fazları çağırmak,
- replay/save-load davranışını sabit tutmak.

Bu sınıf **metronom** olarak kalmalıdır.

### GemRB

GemRB’de de merkezî döngü vardır:

- `gemrb/core/Interface.cpp:1897-1916` — `GameLoop()` oyun güncellemesini çağırır.
- `gemrb/core/Map.cpp:774-827` — map aktörleri, kapıları ve container’ları günceller.

Fakat her `Scriptable` kendi devam eden davranışını taşır:

- `gemrb/core/Scriptable/Scriptable.h:272-287`
  - `actionQueue`
  - `CurrentAction`
  - `CurrentActionState`
  - `CurrentActionTarget`
  - interruptible durumu
  - action tick sayısı
- `Scriptable.cpp:178-200` kendi script ve action’larını ilerletir.

Merkez “şimdi güncellen” der; aktör “hangi eylemde kaldım?” bilgisini taşır.

### Daggerfall Unity

DFU’da gerçek bir `GameManager` singleton vardır:

- `Assets/Scripts/Game/GameManager.cs:35-99`

Fakat düşmanın yerel davranış durumu kendi bileşenlerinde yaşar:

- `DaggerfallEntityBehaviour.cs:62-82` güncellemeyi entity’ye iletir.
- `EnemySenses.cs` hedef, görüş, işitme, son bilinen konum ve zamanlayıcıları tutar.
- `EnemyMotor.cs` yerel hareket/aksiyon durumunu ilerletir.

Yani büyük manager’ın varlığı tek başına ruhu öldürmez.

### Dwarf Fortress arşivi

Verilen `dwarf-fortress-legacy` arşivinde `.cpp`, `.h`, `.cs` gibi kaynak dosyası yoktur. Arşiv executable, DLL ve raw verilerinden oluşur. Bu nedenle Dwarf Fortress’ın iç sınıf/scheduler mimarisi bu paket üzerinden doğrulanamaz. Raw’lar veri odaklı içerik tasarımını gösterir; davranış sahipliğini göstermez.

---

## 2. Ember’daki gerçek yapısal sorunlar

### 2.1 ActorRecord bir “kişi” değil, durum torbası

`Assets/Scripts/Domain/Actors/ActorRecord.cs:8-16` sınıfı kendisini açıkça “deterministic actor state bag” olarak tanımlar.

Taşıdığı şeyler:

- kimlik,
- rol,
- statlar,
- ihtiyaçlar,
- mood,
- pozisyon,
- iş hedefi,
- hafıza.

Davranış metotları esasen setter sarmalayıcılarıdır:

- `MoveTo` — `87-90`
- `ApplyScheduleState` — `162-165`
- `ApplyNeeds` — `167-170`
- `ApplyMood` — `172-175`

Olmayan şeyler:

- `CurrentGoal`
- `CurrentIntent`
- `CurrentAction`
- `ActionPhase`
- `ActionQueue/Plan`
- `TargetThingId`
- `ReservedResource`
- `Progress`
- `InterruptPolicy`
- `FailureReason`

Ayşe’nin “yemek yiyorum” diye bir hali yoktur.

### 2.2 WorldState dev ve herkese açık mutable depo

`Assets/Scripts/Domain/World/WorldState.cs:23-62` zaman, aktörler, item’lar, siteler, event’ler, fiyatlar, stoklar, bitkiler, topraklar, işler ve worksiteleri tek açık aggregate üzerinde taşır.

Alanların çoğu public mutable durumdadır. Bir sisteme `WorldState` verildiğinde dünya üzerinde geniş yazma yetkisi elde eder.

Bu save/replay için merkezî store olarak kullanılabilir; ancak mutation yüzeyi sınırlandırılmalıdır.

### 2.3 “Single writer” belgesi aslında çok yazarlı alanları gösteriyor

`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:18-50`:

- `Actor.Position` için birden fazla sistem,
- `Actor.Needs` için üç sistem,
- `Actor.Vitals` için üç sistem,
- `World.Stockpiles` için beş sistem

yazar olarak listelenmiştir.

Bu registry çatışmayı görünür kılıyor ama gerçek tek-yazar sahipliği sağlamıyor. Sonuç büyük ölçüde sistem sırasına bağlıdır: son yazan kazanır veya önce yazan bir sonraki sisteme zemin hazırlar.

### 2.4 ScheduleSystem karar değil, her tick yeniden hesaplanan yönlendirme

`Assets/Scripts/Simulation/Living/ScheduleSystem.cs:58-80` her aktörü tarar, hedefi hesaplar ve `actor.MoveTo()` çağırır.

`115-147` her çağrıda eat/rest/work/idle skorlarını yeniden hesaplar. Kalıcı karar yoktur.

Bu nedenle:

- aktör bir hedefe bağlanmaz,
- neden karar verdiğini taşımaz,
- kesilen işi devam ettiremez,
- başarısızlıktan öğrenemez,
- “son ekmeği almaya gidiyordum fakat Mehmet aldı” gibi bir hikâye yaşayamaz.

Dosyanın kendi yorumu eski sistemi “choreography, not behavior” diye adlandırır. Utility selector eklemek sabit öğle rotasını iyileştirmiştir; fakat bir action/commitment modeli yaratmamıştır.

### 2.5 Yemek bir eylem değil, aynı fonksiyonda iki sayaç mutasyonu

`NeedConsumptionSystem.cs:123-166`:

- yakın stok bulunur,
- `best.Remove(tag, 1)` yapılır,
- açlık doğrudan tabana çekilir,
- mood yeniden hesaplanır,
- sonradan bir event yazılır.

Olmayanlar:

- gerçek food item instance,
- rezervasyon,
- item’ın stoktan ele/çantaya geçmesi,
- yemek süresi,
- oturma/işlem aşaması,
- kesilme,
- başarısızlık,
- başka aktörün aynı ekmeği istemesine tepki.

Ayrıca `NeedConsumptionSystem.cs:46-54` gece olduğu için fatigue azaltır. Aktörün yatakta veya evde bulunması domain kuralı değildir. Görsel katman yalnızca evdeyse uyuyor pozu gösterir. Böylece aktör sokakta yürürken domain bakımından dinlenebilir.

### 2.6 İş yapmak ile iş yerine yürümek bağlı değil

`JobAssignmentSystem.cs:199-257` bir işi başlatırken aktörün hayatta ve uygun olduğunu kontrol eder; aktörün worksite konumuna ulaştığını kontrol etmez.

`RecipeSystem.cs:27-56` worksite ve inventory’yi doğrular; aktör konumu parametre olarak bile verilmez.

`JobAssignmentSystem.Tick.cs:80-90` aktif order’ları her tick ilerletir. Aktör uzakta olsa da ilerleme sürebilir.

Bu nedenle ekranda işe yürümek, üretimin gerçek sebebi değildir. Yürüyüş ile sayaç ilerlemesi paralel koreografidir.

### 2.7 NPC üretimi oyuncu envanterini kullanıyor

`DefaultTickSystems.cs:164-200` claimed NPC işlerini başlatmak ve ilerletmek için `world.PlayerInventory` geçirir.

Bu, dünya sahipliği ve madde korunumu açısından kritik bir hatadır. Köylü işi köy stokundan veya worksitenin input container’ından değil, oyuncunun çantasından beslenebilir.

### 2.8 Tarım zinciri hem kopuk hem teleportlu

`FarmingJobRequestFactory.cs:16-17` tarım için `5101` ve `5102` recipe ID’lerini kullanır.

`Data/Recipes/ProductionRecipeRegistry.cs:17-53` yalnızca `1001` ve `1002` tariflerini kaydeder. Bilinmeyen tarif exception üretir.

`DefaultTickSystems.cs:172-182` exception’ı sessizce yakalar ve işi bekletir. Böylece shortage → planting job zinciri claim edilmiş halde takılabilir.

Hasat tarafında `DefaultTickSystems.cs:437-465` yakın bir aktör bulunursa:

- stok doğrudan `+2` yapılır,
- event yazılır,
- bitki doğrudan seed aşamasına döner.

Ürün:

- yerde item olmaz,
- aktörün eline geçmez,
- taşınmaz,
- depoya bırakılmaz.

Madde dünyada yolculuk yapmaz; sayaçlar arasında teleport olur.

### 2.9 Görsel katman gerçek eylemi okumuyor, eylem tahmin ediyor

`DomainSimulationAdapter.WorldProjection.cs:108-142` aktivite metnini şu ipuçlarından türetir:

- saat,
- rol,
- konum,
- açlık,
- bitkiye yakınlık,
- bitkinin olgunluğu.

Örneğin aktör olgun bitkiye yakınsa `harvesting` yazar. Fakat aktörün `HarvestAction` durumu yoktur.

Bu çok önemli bir “ruh” problemidir:

> Oyun önce gerçek fiili simüle edip görüntülemiyor; görüntü katmanı duruma bakıp fiil uyduruyor.

### 2.10 Pathfinding sistemi var ama canlı akışta kullanılmıyor

`PathfindingSystem.cs` için production kullanım bulunmamaktadır. ScheduleSystem engelleri dikkate almadan X/Y ekseninde birer kare yaklaşır.

Ayrıca dormant pathfinding kodunda actor ID `0` hard-code edilmiştir ve event için “from” konumu hareketten sonra okunur; böylece event başlangıç/bitiş bilgisini yanlış kaydedebilir.

---

## 3. Tasarım belgesi başka bir oyun tarif etmiş

`docs/mechanics/ARCHITECTURE.md` şunları hedefler:

- unified Actor + Item,
- `ActorClass` üzerinden davranış/polimorfizm,
- aktörde `aiSequence`,
- her aktöre `Tick(dt)`,
- AI package step ve devam eden davranış.

`docs/EMBER_VISION_BIBLE.md:126-159` her NPC için:

- kalıcı yerel zihin,
- reaction tree,
- memory-driven behavior,
- gerekirse `move_to`, `give_item`, `attack`, `flee` gibi aksiyonlar

tarif eder.

Gerçek `NpcAgentToolSurface.cs:11-43` ise yalnızca:

- `ask_about`,
- `remember`,
- `query_relation`,
- `escalate_to_dm`

tanımlar. Hareket/etkileşim action yüzeyi yoktur.

`docs/PRD_living_world_soul_v1.md:10-13,78-82` kabul kriterini şunlara indirger:

- plant stage değişti,
- job claim edildi,
- price değişti,
- position hedefe yaklaştı.

Bu testler “dünya sayıları değişiyor mu?” sorusunu kanıtlar. Şunları kanıtlamaz:

- aktör gerçekten iş yaptı mı,
- iş sırasında orada mıydı,
- gerçek malzemeyi kullandı mı,
- eylem kesilebilir miydi,
- başka aktör olayı gördü mü,
- sonuç yeni bir karara yol açtı mı.

Kod hedeflenen mimariden test edilen davranışa doğru sürüklenmiştir.

---

## 4. Bu OOP’nin tersi mi?

Tam olarak değil.

### Zengin OOP

State ve davranış aynı aggregate çevresinde kapsüllenir. Aktör mesaj alır, kendi geçerli durum geçişini yönetir. Polimorfizm/capability kullanılır.

### ECS / data-oriented

Entity çoğu zaman yalnızca ID’dir; component’ler veridir; davranışı sistemler uygular. Bu büyük simülasyonlarda gayet doğru olabilir.

Ancak temiz bir ECS’de de şunlar gerekir:

- açık component sahipliği,
- tek yazarlı veya kontrollü mutation,
- `CurrentAction/Intent/Reservation` gibi davranış component’leri,
- sistemler arası açık command/event kontratları,
- fiziksel invariantlar.

Ember şu anda iki dünyanın kötü taraflarını birleştiriyor:

- OOP benzeri sınıflar var ama davranışları setter,
- ECS benzeri sistemler var ama action component yok,
- WorldState var ama mutation sınırı yok,
- event log var ama event çoğunlukla değişiklikten sonra yazılan yorum,
- unified Actor hedefi var ama `ActorRole` dalları davranışı belirliyor.

Doğru teşhis: **anemik domain + kuklacı sistemler**.

---

## 5. “Ruh” teknik olarak nedir?

Bir simülasyon karakterinin canlı hissedilmesi için en az şu zincir gerekir:

1. **Kimlik** — aynı kişi zaman içinde sürer.
2. **Niyet** — ne istiyor ve neden istiyor?
3. **Devamlılık** — şu an hangi eylemin hangi aşamasında?
4. **Yerleşiklik** — gerçekten nerede, neyi görüyor, neye ulaşabiliyor?
5. **Bedel/madde korunumu** — hangi nesne nereden nereye geçti?
6. **Çatışma** — aynı kaynak için başka aktörle çarpışınca ne olur?
7. **Gözlem** — kim olayı gördü veya duydu?
8. **Hafıza** — olay gelecekteki kararı değiştirdi mi?
9. **Okunabilirlik** — görüntü gerçek action state’i mi gösteriyor?

Ember’da kimlik, saat, ihtiyaçlar ve kısmen hafıza vardır. En büyük eksik niyet + action devamlılığı + fiziksel madde akışıdır.

Emergence, çok sayıda sayacın değişmesi değildir. Emergence:

> **Birbirine bağlanmış, devam eden ve kaynak kullanan aktör kararlarının çarpışmasıdır.**

Örnek hikâye ancak şu durumda doğar:

- Ayşe son ekmeği rezerve eder.
- Mehmet ekmek bulamaz.
- Mehmet yeni plan yapar veya çalar.
- Bir tanık bunu görür.
- Muhafız tepki verir.
- Ayşe bunu hatırlar.
- Kıtlık fiyatı ve çiftçinin işini değiştirir.

Mevcut yapıda ilk aç aktör stok sayısını azaltır; ikinci aktör yalnızca başarısız bir tarama yaşar. Hiçbiri “ne oldu?” bilgisini taşımaz.

---

## 6. Hedef mimari: aktör-merkezli, sistem-yürütmeli, dünya-doğrulamalı

Her şeyi `Actor` sınıfına doldurmak da doğru değildir. Önerilen sorumluluk:

### Saat / composer

Yalnızca fazları çağırır:

1. zaman ve çevre sinyalleri,
2. perception,
3. decision,
4. reservation,
5. action advancement,
6. validated transaction commit,
7. structured events,
8. projection.

### Aktör

Aktör kendi sürekliliğini taşır:

```text
ActorMindState
- CurrentGoal
- CurrentIntent
- CurrentActionId
- ActionPhase
- TargetThingId / TargetActorId
- Plan / ActionQueue
- Progress
- StartedAt
- InterruptPolicy
- FailureReason
- PerceivedFacts
```

Aktör “dünyayı keyfî değiştirmez”; bir niyet/command üretir.

### Thing / item / material

Cansız nesne düşünmek zorunda değildir. Affordance ve fiziksel özellik sunar:

- Food → Take, Carry, Consume
- Bed → Reserve, Sleep
- Plant → Tend, Harvest (ripe ise)
- Worksite → Craft (tarif ve input uygunsa)
- Door → Open, Close, Lock, Pick, Break
- Material → hardness, density, ignition temperature, value modifiers

Davranış, aktör + thing + ortam etkileşiminden çıkar.

### Sistemler

Sistemler dar bir aşamayı ilerletir:

- `NeedsSystem` yalnızca basıncı artırır.
- `DecisionSystem` yalnızca idle/interrupted aktör için intent seçer.
- `ReservationSystem` kaynağı ayırır.
- `MovementSystem` yalnızca aktif Move action’ı ilerletir.
- `InteractionSystem` actor/thing precondition’larını doğrular.
- `InventoryTransferSystem` fiziksel item konumunu değiştirir.
- `ActionResolutionSystem` sonuçları uygular.
- `EventReactionSystem` gerçek sonuçlara tepki üretir.

### Dünya mutation’ı

Public setter yerine dar işlemler:

- `TryReserve(itemId, actorId)`
- `TryMoveItem(itemId, fromContainer, toContainer)`
- `TryBeginConsume(actorId, itemId)`
- `TryCompleteHarvest(actorId, plantId, toolId)`
- `TryApplyDamage(source, target, amount, cause)`

Her işlem invariant doğrular; başarılıysa structured result/event üretir.

---

## 7. Ayşe’nin yemek zinciri nasıl olmalı?

### Şimdiki akış

```text
Hunger >= 55
→ ScheduleSystem hedefi yemek noktasına çevirir
→ MoveTo ile bir kare yürütür
→ yakınsa stockpile -1
→ hunger = 5
→ “meal_eaten” yaz
```

### Hedef akış

```text
NeedsSystem: Hunger 54 → 56
→ NeedThresholdCrossed(actor=Ayşe, hunger)
→ Ayşe idle/interruptible ise EatGoal seçer
→ algıladığı/bildiği erişilebilir FoodThing aranır
→ bread#418 Ayşe adına rezerve edilir
→ plan oluşur:
   1. MoveTo(stockpile)
   2. Take(bread#418)
   3. MoveTo(seat)
   4. Consume(bread#418, duration=...)
→ her tick CurrentAction ilerler
→ kapı kapanırsa Move başarısız olur ve replan yapılır
→ ekmek çalınırsa reservation failure oluşur
→ Consume tamamlanınca item dünyadan çıkar
→ ancak o anda Hunger düşer
→ MealConsumed structured event’i çıkar
→ tanıklar/ilişkiler/fiyat/stock gerçek sonucu görür
→ UI doğrudan CurrentAction=Consume okur
```

Bu zincirde olay kesildiğinde hikâye çıkar. Ruh burada oluşur.

---

## 8. Tam yeniden yazım gerekmiyor

### Korunacaklar

- `WorldTickComposer` ve deterministik cadence,
- seeded RNG,
- world stores ve save/load,
- temel Actor/Item kimlikleri,
- needs, memory, jobs, plants, worksites veri tanımları,
- event log fikri.

### Dönüştürülecekler

1. `ActorRecord`a kalıcı mind/action state eklenir.
2. `ScheduleSystem` hedef seçip hareket ettiren sistem olmaktan çıkar; decision/plan üretir.
3. Hareket yalnızca aktif Move action üzerinden yapılır.
4. Yemek, uyku, iş, hasat typed action olur.
5. İş ilerlemesi ancak aktör worksiteda ve `PerformWork` aşamasındaysa olur.
6. NPC üretimi player inventory yerine worksitenin/stockpile’ın gerçek container’ını kullanır.
7. Tarım recipe ID’leri ve gerçek planting action bağlanır.
8. Hasat output’u önce item/stack olarak aktörün eline veya yere çıkar; sonra haul edilir.
9. Projection, activity’yi tahmin etmez; `CurrentAction`ı gösterir.
10. Alan başına mutation sahibi daraltılır; diğer sistemler command/event üretir.

---

## 9. İlk dikey dilim

Bütün dünyayı aynı anda çevirmeyin. Önce tek bir sivilin yemek yemesini baştan sona gerçek yapın:

1. `ActorActionState` ekle.
2. Needs yalnızca açlığı yükseltsin.
3. Decision `EatIntent` oluştursun.
4. Gerçek bir food item/stack rezerve edilsin.
5. Move action ile gidilsin.
6. Item stoktan aktör container’ına taşınsın.
7. Consume action birkaç tick sürsün.
8. Tamamlanınca item yok olsun ve açlık düşsün.
9. Structured event üret.
10. UI sadece action state’i göstersin.

Bu dilim canlı hissettirmeden farming, rumor, history veya LLM sistemi eklemeyin.

İkinci dikey dilim:

```text
seed item
→ plot rezervasyonu
→ çiftçinin yürüyüşü
→ Plant action
→ gerçek plant instance
→ growth
→ Harvest action
→ crop item aktörün elinde/yerde
→ Haul action
→ stockpile
→ meal
```

Bu çember kapandığında ekonomi ilk kez gerçekten yaşar.

---

## 10. Yeni kabul testleri

Sayaç testlerine ek olarak hikâye/invariant testleri gereklidir:

- Aktör uzaktan yemek yiyemez.
- Aynı food unit iki aktör tarafından rezerve edilemez.
- Hunger, Consume tamamlanmadan düşmez.
- Eylem tickler arasında aynı ID ve faz ile sürer.
- Kesilen eylem item’ı kaybetmez/çoğaltmaz.
- Aktör worksite dışında işi ilerletemez.
- Input olmadan output oluşmaz.
- Plant output doğrudan stockpile’a teleport olmaz.
- Uyku toparlanması yalnızca aktif Sleep action ve uygun yatakta olur.
- Activity etiketi `CurrentAction` ile bire bir aynıdır.
- Aynı seed + input trace aynı action/event trace’i üretir.
- `NeedThresholdCrossed → IntentChosen → ResourceReserved → Arrived → ItemTransferred → MealConsumed` zinciri eksiksizdir.

---

## Sonuç

Ember’ın ruhsuz olmasının sebebi sistem sayısının azlığı değildir. Tam tersine sistem çoktur; fakat fiiller birinci sınıf dünya nesnesi değildir.

- Aktörlerde kimlik var, fakat devam eden irade yok.
- Nesnelerde veri var, fakat fiziksel etkileşim zinciri yok.
- Event var, fakat çoğu olayın sebebi değil, sonradan yazılan açıklaması.
- Görselde fiil var, fakat gerçek action’dan değil, konum/saat tahmininden geliyor.
- Dünya değişiyor, fakat karakterler dünyayı yaşayarak değiştirmiyor.

Doğru yön:

> **Manager metronom olsun. Aktör niyet ve mevcut eylemi taşısın. Thing/material affordance ve fiziksel özellik sunsun. Sistemler eylemi adım adım ilerletsin. Dünya yalnızca doğrulanmış işlemlerle değişsin.**

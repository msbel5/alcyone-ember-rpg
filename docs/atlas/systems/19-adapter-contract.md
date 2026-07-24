# 19-adapter-contract

> Kapsam: IDomainSimulationAdapter rol arayüzleri, DomainSimulationAdapter'in 40 partial dosyası,
> UnavailableSimulationAdapter, EmberDomainAdapterLocator, statik mirror kanalları (kaçak kapı
> envanteri) ve DTO sınırları.
> Kanıt biçimi: `dosya:satır`. Aksi belirtilmedikçe tüm yollar
> `Assets/Scripts/Presentation/Ember/Adapters/` köklüdür; diğer yollar `Assets/` köklü yazılır.
> Not: görev tanımı "24 partial" der — gerçek sayı **40**'tır (40 dosyanın hepsi
> `partial class DomainSimulationAdapter` bildirimi içerir; grep ile doğrulandı). 24 sayısı
> muhtemelen eski bir snapshot'tır.

## HLD - Ne ve Neden

Adapter kontratı, deterministik simülasyon dünyası (`Domain` + `Simulation` asmdef'leri) ile
Unity sahne katmanı (`Presentation` asmdef) arasındaki TEK dikişin sözleşmesidir: runtime host
her tick `IDomainSimulationAdapter` üzerinden simülasyonu ilerletir ve görünüm başına düz DTO
satırları okur (`IDomainSimulationAdapter.cs:252-268`). Oyuncuya görünen etkisi dolaylı ama
total: HUD üst barı, colony/faction/job panelleri, envanter, dialog, combat ekranı, dünya
haritası, save/load ve fal (fate) ribbonu — hepsi bu tek kulptan beslenir. Felsefe iki
katmanlıdır: (1) tek şişman arayüz yerine BEŞ rol arayüzü (clock, HUD read-model, world-view
read-model, player command sink, fate oracle) + save köprüsü; agregat sadece "tek
implementasyon her şeyi çalıştırır" düzenini derleme-uyumlu tutmak için yaşar
(`IDomainSimulationAdapter.cs:10-15, 260-267`). (2) Gerçek adapter boot edemezse oyun YALAN
söylemez: `UnavailableSimulationAdapter` sahte gameplay satırı uydurmak yerine dürüst
"unavailable" durumları gösterir (`UnavailableSimulationAdapter.cs:10-16`). Adapter'in sahneye
dokunamaması bilinçli bir kısıttır; sahnenin adapter'dan ANLIK sinyal alması gerektiğinde
kontrat dışına çıkan statik mirror kanalları devreye girer — bunlar bu haritanın "kaçak kapı"
envanteridir (örn. `DomainSimulationAdapter.WorldEncounter.cs:8-11` kendini açıkça "adapter
UI açamaz, one-shot statik sinyal" diye gerekçelendirir). Arayüz dosyasının kendisi bile
"temporary aggregate ... while role interfaces are extracted" der
(`IDomainSimulationAdapter.cs:1`) — kontrat bitmiş değil, göç halindedir.

## HLD - Akis

1. **Boot (sahne Awake):** `EmberWorldHost.Awake` adapteri üç aşamalı fallback ile seçer:
   `EmberDomainAdapterLocator.Current ?? EmberWorldContinuity.Take() ?? TryCreateDomainAdapter()`,
   hepsi null ise `CreateFallbackAdapter()` → `UnavailableSimulationAdapter`
   (`Presentation/Ember/Bootstrap/EmberWorldHost.cs:72-75`,
   `Presentation/Ember/Bootstrap/EmberWorldHost.AdapterBootstrap.cs:9-13, 24-63`).
   `EmberWorldHostAdapterBinding` agregatı beş rol koluna ayırır — host cast etmez
   (`Presentation/Ember/Bootstrap/EmberWorldHostAdapterBinding.cs:12-27`). Sonra
   `EmberDomainAdapterLocator.Register(_adapter)` (`EmberWorldHost.cs:81`).
2. **Worldgen intent tüketimi:** MainMenu sihirbazının bıraktığı `EmberWorldGenIntent.Pending`
   ilk tick'ten ÖNCE `_commands.SeedWorld(...)` + `ApplyCharacterCreation(...)` ile dünyaya
   uygulanır (`EmberWorldHost.cs:83-97`; `DomainSimulationAdapter.Worldgen.cs:26-90, 92-127`).
3. **Tick kadansı:** `EmberTickDriver.Update` → `Listener.OnTick` → projektör →
   `AdvanceTick(tickIndex)`: önce off-thread LLM sonuç kuyruğu ana thread'de boşaltılır
   (DET-02), `_tickComposer.Advance(_world, tickIndex)` simülasyonu ilerletir, sonra
   `PublishEventEchoes()` + `PublishFieldMirror()` statik kanallara yayın yapar
   (`DomainSimulationAdapter.Clock.cs:6-13`; driver `Presentation/Ember/Tick/EmberTickDriver.cs:44-56`).
4. **Frame kadansı (okuma):** UI panelleri her frame rol arayüzlerinden DTO okur; `HudText`
   tick başına cache'lenir ("oyun her tickte kasıyor" fix,
   `DomainSimulationAdapter.Hud.cs:18-23, 59-60`). Satır DTO'ları her okumada yeniden inşa
   edilir (`DomainSimulationAdapter.WorldRows.cs:14-51`).
5. **Oyuncu komutları (olay kadansı):** E tuşu → `TryInteract` → aktör bulunursa
   `GetDialogSource(id)` (sohbet) ya da düşman rolündeyse world-encounter bağlama + one-shot
   sinyal (`DomainSimulationAdapter.Combat.Interaction.cs:21-54`,
   `DomainSimulationAdapter.WorldEncounter.cs:52-68`). Melee/spell/trade/craft/levelup/travel
   verb'leri kendi partial'larında domain servislerine yönlenir.
6. **Async LLM (fate/dialog):** `ConsultFate` anında placeholder döner, `Task.Run` ile LLM'i
   off-thread çağırır, sonucu `_mainThreadApply` kuyruğuna koyar; kuyruk bir SONRAKİ
   `AdvanceTick` başında ana thread'de uygulanır ve host `TryConsumeResolvedFate()` ile tam bir
   kez tüketir (`DomainSimulationAdapter.Fate.cs:28-46, 87-128`; kuyruk
   `DomainSimulationAdapter.Clock.cs:15-30`).
7. **Save/load:** `ExportStateJson` → `JsonSliceSaveService.SaveToJson`; `RestoreStateJson` →
   `LoadFromJson` + `WorldState.CopyFrom` + `EnsureInvariants` + composer akümülatör yeniden
   türetimi (DET-01) (`DomainSimulationAdapter.Save.cs:24-64`); driver tarafında `AlignTo`
   sayaç hizalar (`EmberTickDriver.cs:37-42`).
8. **Fast travel (sahne reload):** canlı adapter `EmberWorldContinuity.Carry` ile statik slota
   konur; yeni host `Take()` ile tam bir kez alır — yoksa oynanan dünya sessizce default
   dünyayla değişirdi (`Presentation/Ember/Bootstrap/EmberWorldContinuity.cs:5-28`,
   `EmberWorldHost.cs:69-74`).

## LLD - Veri Modeli

### Rol arayüzleri (hepsi `IDomainSimulationAdapter.cs`)

| Tip | Üyeler | Satır |
|---|---|---|
| `IEmberClockSink` | `AdvanceTick(int)` | 23-26 |
| `IEmberClockSource` | `TickIndex` | 29-32 |
| `IEmberSimulationClock` | sink + source kompozisyonu | 35-37 |
| `IEmberHudReadModel` | `HudText`, `CombatHud`, `PlayerSheet` | 40-45 |
| `IWorldViewReadModel` | `JobQueueRows`, `ColonyNeedsRows`, `FactionRows`, `InventorySlots`, `SpellSlots`, `Overland`, `PlayerOverlandTile`, `StartingSettlementName`, `RecentWorldEvents(int)`, `TryReadActor(string/ActorId)`, `TryReadWorksite`, `GetSpawnableActors()`, `CurrentSettlementKey` | 48-97 |
| `IPlayerCommandSink` | `LogCombat`, `TakePlayerDamage`, `TryCastSpell`, `TryMeleeStrike`, `TryInteract(string/ActorId)`, `GetDialogSource(string/ActorId)`, `SeedWorld(mood, calling, startLocation, uint? worldSeed)` | 127-203 |
| `IConsultFateOracle` | `ConsultFate()`, `ConsultFate(string)`, `TryConsumeResolvedFate()` | 213-228 |
| `IEmberSaveBridge` | `ExportStateJson()`, `RestoreStateJson(string)` | 238-250 |
| `IDomainSimulationAdapter` | yukarıdaki altısının agregatı | 260-268 |
| `EmberDomainAdapterLocator` | `Current` + rol-özel accessor'lar (`ClockSource`..`SaveBridge`), `Register`, `Clear` | 279-305 |

Notlar: `IPlayerCommandSink`'te default-method shim'leri bilinçli KALDIRILDI — eksik verb
görünmez kalamaz, her implementasyon açıkça yazmak zorunda
(`IDomainSimulationAdapter.cs:132-141`). `GetDialogSource(ActorId)` çözülemeyen id'de null
DEĞİL, kasıtlı boş source döndürmek zorundadır (`IDomainSimulationAdapter.cs:180-193`).

### İkincil rol arayüzleri (agregat DIŞINDA, ekran başına)

Adapter bunları partial bildirimlerinde ek olarak uygular; `UnavailableSimulationAdapter` da
hepsini uygular (`UnavailableSimulationAdapter.cs:14-16`):

- `ICombatScreenSource` — `Presentation/Ember/UI/CombatScreenSource.cs:70`; adapter:
  `DomainSimulationAdapter.CombatScreen.cs:8`
- `ICraftingSource` / `ICraftingCommandSink` — `Presentation/Ember/UI/CraftingSource.cs:58,63`;
  adapter: `DomainSimulationAdapter.Crafting.cs:14`
- `IJournalSource` — `Presentation/Ember/UI/JournalSource.cs:46`; adapter:
  `DomainSimulationAdapter.Journal.cs:9`
- `ILevelUpSource` / `ILevelUpCommandSink` — `Presentation/Ember/UI/LevelUpSource.cs:95,100`;
  adapter: `DomainSimulationAdapter.LevelUp.cs:8`
- `ITradeSource` / `ITradeCommandSink` — `Presentation/Ember/UI/TradeSource.cs:83,88`; adapter:
  `DomainSimulationAdapter.Trade.cs:10`
- `IQuestGuidanceSource` / `IQuestGuidanceTracker` —
  `Presentation/Ember/UI/QuestGuidanceSource.cs:34,43`; adapter:
  `DomainSimulationAdapter.QuestGuidance.cs:11`
- `IWorldTravelSink` — adapter dosyasının İÇİNDE tanımlı
  (`DomainSimulationAdapter.Travel.cs:7-10`), adapter: `Travel.cs:12`
- `IDialogSource` / `IDialogSourcePortrait` — `IDialogSource.cs:12-36, 43-46`
  (`VoiceKey`, `GetCurrentLine`, `GetTopics`, `SelectTopic`, `AskFreeText`, `EndConversation`,
  `IsThinking` default'lu); adapter kök partial'da: `DomainSimulationAdapter.cs:28`

### DTO sınırı — readonly struct satırları

Kontratın vaadi: "wires Captain's domain stores into the Ember view layer **without leaking
domain types** into the presentation assembly" (`IDomainSimulationAdapter.cs:253-256`). DTO'lar:

- `SpawnableActor` — `Id:ulong, Name, SpriteRole, WorldX, WorldZ, Seed`; world pozisyonu
  ÖNCEDEN projekte edilmiş, spawner Domain matematiği yapmaz
  (`IDomainSimulationAdapter.cs:99-124`)
- `CombatHudState` — `Presentation/Ember/UI/CombatHud.cs:144`; `PlayerSheetState` — aynı
  dosya `:170`
- `JobQueueRow` — `Presentation/Ember/UI/JobQueuePanel.cs:82`
- `ColonyNeedsRow` — `Presentation/Ember/UI/ColonyNeedsPanel.cs:82`. DİKKAT: aynı basit adla
  İKİNCİ bir `ColonyNeedsRow` `Presentation/Visual/ColonyNeedsSnapshot.cs:44`'te yaşar; kök
  partial bu çakışma yüzünden `using` yerine tip alias'ı kullanmak zorunda
  (`DomainSimulationAdapter.cs:17-21`)
- `FactionRow` — `Presentation/Ember/UI/FactionPanel.cs:83`; `InventorySlot` —
  `Presentation/Ember/UI/InventoryGrid.cs:176`; `TradeItemRow` —
  `Presentation/Ember/UI/TradeSource.cs:35`
- `ActorViewState` — `Presentation/Ember/Views/ActorView.cs:270`; `WorksiteViewState` —
  `Presentation/Ember/Views/WorksiteView.cs:45`
- `WorldEventRow` — `Presentation/Visual/WorldEventTailSnapshot.cs:73`
- `CombatScreenState` + `CombatSpellActionRow` — `Presentation/Ember/UI/CombatScreenSource.cs`

**Sınır ihlalleri (kontratın kendi metninde):** `IWorldViewReadModel` Domain tiplerini DOĞRUDAN
sızdırır — `EmberCrpg.Domain.Overland.OverlandMap Overland` (`IDomainSimulationAdapter.cs:57`),
`EmberCrpg.Domain.Actors.GridPosition PlayerOverlandTile` (`:60`) ve `ActorId` parametreleri
(`:77, 167, 193`). `UnavailableSimulationAdapter` bile bu yüzden `Domain.Overland` import etmek
zorunda (`UnavailableSimulationAdapter.cs:38-39`). Tek asmdef `EmberCrpg.Presentation` olduğu
için derleyici bunu yakalamaz — sınır sözleşmeyle, derlemeyle değil (asmdef listesi:
`Assets/Scripts/*/*.asmdef`; Presentation tek parçadır).

### Kök partial durumu (`DomainSimulationAdapter.cs:30-58`)

`_world` (WorldState, ctor'da null-check `:60-62`), `_saveService` (JsonSliceSaveService,
`BindWorld` ile canlı dünyaya bağlanır — bağlanmazsa seed'lenen worksiteler tick'lenmezdi,
`:63-71`), `_tickComposer`, `_tick`, `_lastCombatLine`, dialog durumu
(`_activeDialogActor/-Id`, `_conversation`, `_currentPortrait`, `_streamingPartialLine`,
`_suppressGlobalTopicFallback`), fate durumu (`_pendingFate`, `_isFateThinking`),
`_topicAskCounts` (tekrar soruları yeniden ifade ettiren sayaç, `:45-48`), id-uzayı offsetleri
(`RegionSiteOffset=100_000`, `SettlementSiteOffset=200_000`, `GeneratedNpcActorOffset=10_000`,
`:56-58`). `World` property'si canlı WorldState'i DIŞARI verir (`:93`) — DTO sınırının en
büyük deliği (bkz. Borçlar).

### 40 partial dosyanın haritası

| Partial | Satır sayısı | Sorumluluk |
|---|---|---|
| `DomainSimulationAdapter.cs` | 98 | paylaşılan durum + ctor + `World` |
| `.Clock.cs` | 150 | `AdvanceTick`, DET-02 apply kuyruğu, event echo + field mirror yayını |
| `.Hud.cs` | 99 | `HudText` (tick-cache), `CombatHud`, `PlayerSheet` |
| `.WorldRows.cs` | 119 | job/needs/faction/inventory/spell satır projeksiyonları |
| `.WorldProjection.cs` | 269 | `TryReadActor/Worksite`, `GetSpawnableActors`, billboard origin |
| `.Overland.cs` | 60 | `Overland`, `PlayerOverlandTile`, `StartingSettlementName` |
| `.Combat.cs` | 41 | `LogCombat`, `TakePlayerDamage` (gerçek vitals mutasyonu) |
| `.Combat.Interaction.cs` | 57 | `TryInteract` (string→id köprüsü, id→dialog/encounter yönlendirme) |
| `.Combat.Melee.cs` | 198 | `TryEquip`, melee vuruş çözümü, crime tetiği, hit-feed yayını |
| `.Combat.Spells.cs` | 140 | `TryCastSpell`, `SpellResolved` olayı, spell-fx mirror |
| `.Combat.Helpers.cs` | 173 | `CenterOf`, `WorksiteKindFor`, `HitMaterialFor` statik yardımcıları |
| `.CombatScreen.cs` | 67 | `ICombatScreenSource` + battle mirror yayını |
| `.WorldEncounter.cs` | 994 | encounter bağla/çöz, loot/XP/respawn, kapı/tavern/temple etkileşimi, Proof* kancaları |
| `.Crafting.cs` | 159 | `ICraftingSource/Sink` |
| `.Trade.cs` | 193 | `ITradeSource/Sink`, canlı pazar fiyatı (statik baz fiyatı ezer, `:71`) |
| `.Journal.cs` | 108 | `IJournalSource` — quest→journal projeksiyonu |
| `.LevelUp.cs` | 66 | `ILevelUpSource/Sink` |
| `.QuestGuidance.cs` | 239 | pusula/yönlendirme satırları |
| `.QuestInteraction.cs` | 62 | quest hedefi etkileşimi |
| `.QuestProgress.cs` | 17 | anlık `QuestSystem.Tick` (tick dışı yeniden değerlendirme) |
| `.WorldQuests.cs` | 157 | bounty/pilgrimage quest tohumları (`QuestId 9001/9002`, `:9-10`), rep yazımı |
| `.MainQuest.cs` | 99 | üç-perde omurga + `JustCreatedWorld`/`OpeningHook` STATİK bayrakları (`:9-10`) |
| `.Travel.cs` | 135 | `IWorldTravelSink`, wait/rest; `_currentSettlement` KASITLI save-dışı (`:15-17`) |
| `.Haunters.cs` | 182 | sentetik zindan sakinleri + watch (id bantları 9M/9.5M, `:9-14`) |
| `.Fate.cs` | 132 | async LLM fal + DET-03 governed tool gate |
| `.Save.cs` | 66 | `IEmberSaveBridge` round-trip |
| `.Dialog.cs` | 7 | boş indeks partial'ı (yorum: "concrete responsibilities live in siblings") |
| `.Dialog.Binding.cs` | 215 | konuşma bağlama, portre çözümü, NPC memory kaydı |
| `.Dialog.Source.cs` | 394 | `IDialogSource` implementasyonu, topic akışı, followup ayrıştırma |
| `.Dialog.Text.cs` | 285 | greeting matrisi, `NarrateEvent`, `SanitizeNpcLine`, `CompleteLlmOrEmpty` |
| `.Dialog.Topics.cs` | 130 | topic listesi üretimi |
| `.Dialog.Greetings.cs` | 125 | selamlama seçimi |
| `.Worldgen.cs` | 127 | `SeedWorld` + `ApplyCharacterCreation` |
| `.Worldgen.State.cs` | 37 | `GeneratedWorld`, `StartingRegion/Settlement/Faction` property'leri |
| `.Worldgen.Hydration.cs` | 121 | site/faction/NPC/history hidrasyon sırası (`:25-33`) |
| `.Worldgen.Npcs.cs` | 166 | NPC seed → ActorRecord |
| `.Worldgen.NpcStats.cs` | 74 | rol→stat/vitals tabloları, `ToRuntimeEventKind` eşlemesi |
| `.Worldgen.Player.cs` | 69 | oyuncu yerleştirme, history→event yazımı, site-id türetme (`:65-66`) |
| `.Worldgen.Production.cs` | 84 | üretim tohumları |
| `.Worldgen.Selection.cs` | 127 | başlangıç bölge/yerleşim/faction seçimi, `FoldSeed` (`:99-121`) |

## LLD - Fonksiyon Haritasi

- `void AdvanceTick(int tickIndex)` — `Clock.cs:6-13` — kuyruk boşalt → composer ilerlet →
  statik kanallara yayınla; kontratın tek yazma-kadansı girişi.
- `DomainSimulationAdapter(WorldState world)` — `DomainSimulationAdapter.cs:60-91` — save
  service'i canlı dünyaya bağlar, site→worksite tohumlar.
- `void SeedWorld(string mood, string calling, string startLocation, uint? worldSeed = null)`
  — `Worldgen.cs:26-90` — FNV-1a fold ya da verilen seed ile deterministik dünya; profil +
  hidrasyon + overland projeksiyonu + `ConfigureMainQuest()`.
- `void ApplyCharacterCreation(string playerName, string classId, string birthsignId)` —
  `Worldgen.cs:92-127` — oyuncu ActorRecord'unu sınıf/burç statlarıyla değiştirir.
- `bool TryInteract(ActorId actorId)` — `Combat.Interaction.cs:38-54` — aktör lookup →
  `GetDialogSource(id)`; hostile rol `TryBeginWorldEncounter`'a sapar
  (`WorldEncounter.cs:52-68`).
- `bool TryBeginWorldEncounter(ActorRecord, NpcSeedRecord)` — `WorldEncounter.cs:52-68` —
  outlaw VEYA bounty'li guard'ı gerçek combat rakibi olarak bağlar, iki one-shot sinyal atar.
- `CombatScreenState ReadCombatScreenState()` — `CombatScreen.cs:10-65` — world-encounter
  önceliği + battle mirror yazımı + spell aksiyon satırları.
- `void TakePlayerDamage(int amount)` — `Combat.cs:27-38` — GERÇEK `ActorRecord.Vitals`
  mutasyonu (eski geçici sayaç kaldırıldı).
- `string ConsultFate(string question)` / `string TryConsumeResolvedFate()` —
  `Fate.cs:30-46` — anında placeholder + tek-seferlik çözülmüş kehanet; async gövde
  `ConsultFateAsync` `Fate.cs:48-129` (deterministik d100 bucket + LLM süsleme + DET-03
  governed tool-call yolu).
- `string ExportStateJson()` / `void RestoreStateJson(string json)` — `Save.cs:24-64` —
  tam deterministik snapshot round-trip; restore sonrası `EnsureInvariants` +
  `RebuildAccumulatorsFrom`.
- `bool TryBeginTravelToSettlement(string, out int travelDays, out string message)` —
  `Travel.cs:31-60+` — Chebyshev tile mesafesi = gün; oyuncu aktörünün pozisyonu tek gerçek
  kaynak olarak taşınır.
- `IReadOnlyList<SpawnableActor> GetSpawnableActors()` — `WorldProjection.cs` (bildirim
  arayüzde `IDomainSimulationAdapter.cs:80-90`) — ön-cull YASAK, deterministik sıra şart.
- `int EnsureWatchOfficers()` — `Haunters.cs:21-40+` — ilk suçta plazaya iki sentetik watch;
  idempotent deterministik id'ler.
- `EmberWorldHostAdapterBinding.Create(candidate, fallbackFactory)` —
  `Bootstrap/EmberWorldHostAdapterBinding.cs:34-46` — null adayda fallback fabrikası.
- `EmberDomainAdapterLocator.Register/Clear` — `IDomainSimulationAdapter.cs:296-304` —
  uyarısız overwrite; testler `[TearDown]`'da `Register(null)` çağırır (`:273-277`).

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` yalnızca TICK SİSTEMLERİNİN yazarlarını deklare eder
(`Simulation/Composition/FieldOwnershipRegistry.cs:12-56`); **adapter'in hiçbir yazımı bu
deftere kayıtlı değildir**. Adapter'in yazımları oyuncu-komutu kadansındadır (tick bantları
dışında, ana thread'de) ve lint testi onları göremez — bkz. Borçlar #5.

Yazdıkları (ilgili registry alanı parantezde):

- `Actor.Vitals` (registry'de living.predation/witness/companion_guard'a aittir) —
  `TakePlayerDamage` `Combat.cs:36`; melee/spell/encounter çözümleri
  (`Combat.Melee.cs`, `Combat.Spells.cs`, `WorldEncounter.cs`).
- `Actor.Position` (registry'de living.schedule vd.) — travel `player.MoveTo`
  `Travel.cs:56-57`; hydration `MovePlayerToStartingSettlement` (`Worldgen.Hydration.cs:32`).
- `Actor` kayıtlarının kendisi — `ApplyCharacterCreation` oyuncuyu değiştirir
  (`Worldgen.cs:123-125`); Haunters/Watch sentetik aktör ekler (`Haunters.cs:21+`);
  worldgen hidrasyonu tüm NPC'leri ekler (`Worldgen.Npcs.cs`).
- `World.Events` — `SpellResolved` append (`Combat.Spells.cs:90-95`), `ActorTalked` append
  (`Dialog.Source.cs:332-335`), history→event hidrasyonu (`Worldgen.Player.cs:32-35`).
- `World.WorldProfile`, `World.RoomSeed`, `World.Overland` — `Worldgen.cs:34-35, 55-65, 76-79`.
- `World.MainQuest` (Configure), `World.LastNarrative`, statik `OpeningHook` —
  `MainQuest.cs:29-35`.
- `World.PlayerReputation` / `World.PlayerBountyGold` — quest ödülleri / suç
  (`WorldQuests.cs:117, 152`; `Combat.Melee.cs:114` civarı [Crime] yolu).
- `World.ToolCallTrace` — fate governed-gate trace kayıtları (`Fate.cs:121-122`).
- `World.PlayerEquipment` / `World.PlayerInventory` — `TryEquip` (`Combat.Melee.cs:36-54`),
  trade/craft/loot yolları.
- NPC `Memory` — `RecordEvent(InteractionEvent)` (`Dialog.Binding.cs:114`,
  `Dialog.Source.cs:110, 185`).
- TÜM WorldState — restore'da `_world.CopyFrom(restored)` (`Save.cs:47`).

Okudukları: pratikte WorldState'in tamamı (Actors, Sites, Plants, Caravans, Events, Time,
NpcSeeds, Quests, Stockpiles, CompanionIds, MainQuest, Overland...) — read-model
projeksiyonlarının doğası gereği (`WorldRows.cs`, `Hud.cs:14-55`, `Clock.cs:41-147`,
`Overland.cs:14-58`).

## LLD - Urettigi/Tukettigi Olaylar

**Ürettiği WorldEventKind'lar** (`_world.Events.Append` ile):

- `SpellResolved` — `Combat.Spells.cs:90-95` (reason: `slice_spell_cast id:... mana:...`)
- `ActorTalked` — `Dialog.Source.cs:332-335`
- Worldgen history eşlemesi: `FactionReputationChanged`, `TradeCompleted`,
  `ShortageDetected`, `StorytellerCheckpoint` — `Worldgen.NpcStats.cs:57-69` →
  `Worldgen.Player.cs:32-35`

**Tükettiği WorldEventKind'lar:**

- Echo yayını için: `WitnessRecorded` (reason "reported…" ise KindReport), `GuardResponded`,
  `PlantHarvested`, `ActorTalked` — `Clock.cs:51-76`
- Dialog anlatısı (`NarrateEvent`) için: `NeedChanged`, `WitnessRecorded`, `GuardResponded`,
  `ShortageDetected`, `CaravanArrived`, `PriceChanged`, `FactionReputationChanged`,
  `CombatResolved`, `PlantHarvested`, `ChronicleEvent` — `Dialog.Text.cs:157-182`
- Encounter/tavern dedikodu filtresi — `WorldEncounter.cs:675-679`

**Log tag'leri** (Debug.Log + LogCombat): `[Encounter]`, `[Crime]`, `[Spell]`, `[MainQuest]`,
`[Quest]`, `[QuestGen]`, `[Rep]`, `[XP]`, `[Loot]`, `[Key]`, `[Door]`, `[Tavern]`, `[Temple]`,
`[Lunch]`, `[Bestiary]`, `[Respawn]`, `[Rest]`, `[Trade]`, `[NpcGreeting]`, `[fate]`
(dağılım: yukarıdaki partial'lar; örn. `WorldEncounter.cs:66-990`, `Fate.cs:120`).

## Statik Mirror Kanallari — Kacak Kapi Envanteri

Adapter sahneye/UI'a dokunamaz; bu yüzden kontrat DIŞINDA statik global kanallar kullanılır.
Desen ailesi üç varyanttır: **mirror** (sürekli değer, poll edilir), **stamp feed** (artan
sayaç, çoklu tüketici — "stamp-not-consume"), **one-shot signal** (tek tüketici consume eder).

### Adapter'in YAZDIĞI kanallar

| Kanal | Tanım | Adapter yazım noktası | Tüketici / desen |
|---|---|---|---|
| `RuntimeFieldMirror` | `Presentation/Ember/WorldDirector/RuntimeFieldBuilder.cs:11-45` — HourOfDay, MinutesOfDay, WorldDay, PlantCount, StageIndex, `Plants[]`+`PlantsStamp` | `Clock.cs:116-133, 122` | ekin sapları, gökyüzü, gece sokağı, hava durumu seçimi (mirror; hash değişince yayın `Clock.cs:118-123`) |
| `RuntimeCaravanMirror` | `RuntimeCaravanView.cs:10-15` — AtSiteCount | `Clock.cs:147` | plaza ticaret arabası görünürlüğü (mirror, 2 sn poll) |
| `NpcEventEchoFeed` | `NpcEventEchoFeed.cs:10-50` — 128'lik ring, 5 kind sabiti, `Stamp` | `Clock.cs:56-74` | NPC üstü ikon yankıları (stamp-not-consume ring; tek slot patlamaları düşürürdü) |
| `WorldCombatFeedbackFeed` | `WorldCombatFeedbackFeed.cs:10-42` — HitStamp/HitTargetId/HitMaterial, FelledStamp, EnemyStrikeStamp | `Combat.Melee.cs:173,192`; `Combat.Spells.cs:100`; `WorldEncounter.cs:327, 978` | billboard kırmızı flash / yere yatma / düşman hamlesi (stamp; ÇOK tüketici) |
| `RuntimeSpellFxMirror` | `RuntimeSpellFx.cs:7-13` — LastCastTemplate, LightUntilRealtime, HasteUntilRealtime, RecallRequested | `Combat.Spells.cs:118-132` | bolt vfx + fener orb + recall snap (mirror + RecallRequested one-shot'ı VIEW sıfırlar `RuntimeSpellFx.cs:55-57`) |
| `RuntimeBattleMirror` | `RuntimeMusicDirector.cs:6-11` — Active, BossActive | `CombatScreen.cs:18-24`; `WorldEncounter.cs:348` | müzik direktörünün BATTLE slotu (mirror) |
| `RuntimeMainQuestMirror` | `RuntimeMainQuestFinale.cs:8-11` — FinaleRequested | `WorldEncounter.cs:989` | finale overlay (one-shot; view sıfırlar) |
| `WorldEncounterSignal` | adapter dosyasının İÇİNDE: `WorldEncounter.cs:12-24` | `WorldEncounter.cs:64` | `UI/InGame/InGameUiController.cs:218` combat ekranını açar (one-shot consume) |
| `WorldEncounterStingFeed` | `WorldEncounter.cs:26-32` | `WorldEncounter.cs:65` | audio sting (ayrı one-shot — "one flag, one consumer" kuralı) |
| `DomainSimulationAdapter.JustCreatedWorld` / `.OpeningHook` | adapter SINIFININ statik alanları `MainQuest.cs:9-10` | `MainQuest.cs:30, 35` | intro hikaye overlay'i bir kez tüketir |

### Adapter'in OKUDUĞU / yaşam döngüsünü taşıyan statikler

| Kanal | Tanım | Rol |
|---|---|---|
| `EmberDomainAdapterLocator` | `IDomainSimulationAdapter.cs:279-305` | sahne-scoped singleton; `Register` uyarısız ezer |
| `EmberWorldContinuity` | `Bootstrap/EmberWorldContinuity.cs:13-28` | fast-travel sahne reload'unda canlı adapter'i taşıyan tek-seferlik slot |
| `EmberWorldGenIntent.Pending` | `UI/EmberWorldGenIntent.cs:15` | MainMenu sihirbazı → host `SeedWorld` handoff'u (`EmberWorldHost.cs:83-97`) |
| `ForgeLocator.LlmRouter/NativeLlm/AssetForge/Embedding` | `Forge/ForgeLocator.cs:8-20` | fate/dialog LLM yolunun servis locator'ı (`Fate.cs:50`) |

### Ailenin adapter-DIŞI üyeleri (tam envanter için; adapter yazmaz/okumaz)

`RuntimeWeatherMirror` (`RuntimeWeatherController.cs:8-15`; weather controller yazar, müzik/sky
okur — adapter yalnızca `WorldDay`'i besleyerek dolaylı sürer), `ScreenRequestSignal`
(`RuntimeFunctionalInteriors.cs:24`; interior → `InGameUiController.cs:250`),
`NpcEventEchoFeed` benzeri diğer WorldDirector feed'leri ve `Infrastructure/AiDm/SyncTaskBridge.cs:6`.

## Testler

- `Assets/Tests/EditMode/Presentation/PlayableLoopCraftQuestTests.cs` — adapteri gerçek
  WorldState ile 12+ kez kurar (`:34, 77, 100, ...`); craft/quest/trade komut yüzeyini pinler.
- `Assets/Tests/EditMode/Presentation/JournalSourceTests.cs:29,44` — `IJournalSource`
  projeksiyonu.
- `Assets/Tests/EditMode/AiDm/LlmToolAuthorityTests.cs` — fate'in DET-03 governed tool-gate
  yolu.
- `Assets/Tests/EditMode/Audit/AuditFourthPassTailCoverageTests.cs`,
  `AuditSixthPassCoverageTests.cs`, `AuditSeventhPassCoverageTests.cs`,
  `SelectSpellTargetTests.cs` — audit bulgularının regresyon pinleri (adapter referanslı).
- `Assets/Tests/EditMode/Audit/EmberWorldGenIntentHandoffTests.cs` — `SeedWorld` handoff'u.
- `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` — `SeedWorld`
  kabul yolu.
- `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs` + `EditMode/Worldgen/NpcSeedSaveRoundTripTests.cs`
  + `WorldProfileSaveRoundTripTests.cs` — `IEmberSaveBridge`'in dayandığı round-trip sadakati
  (arayüz yorumu da bunu işaret eder, `IDomainSimulationAdapter.cs:234-236`).
- **Doğrulanmadı/bulunamadı:** rol-arayüz AYRIŞMASININ kendisini (örn. bir tüketicinin yalnız
  `IEmberClockSource` alabildiğini) pinleyen özel bir test taramada bulunamadı; audit test
  dosyalarında `IEmberClockSource`/`IPlayerCommandSink` adlarına rastlanmadı. Statik mirror
  kanallarını pinleyen EditMode testi de bulunamadı.

## Bilinen Borclar + Kacak Kapilari

1. **Rol ayrışması yarım.** Arayüz dosyası kendini "temporary aggregate ... while role
   interfaces are extracted" diye etiketler (`IDomainSimulationAdapter.cs:1, 10-15`); pratikte
   tüm tüketiciler hâlâ tek agregat implementasyonun üstünden geçer, binding yalnız görünüşte
   daraltır (`EmberWorldHostAdapterBinding.cs:15-19` — aynı nesnenin altı görünümü).
2. **DTO sınırı delik.** (a) `IWorldViewReadModel` Domain tipleri sızdırır (`OverlandMap:57`,
   `GridPosition:60`, `ActorId:77`); (b) `DomainSimulationAdapter.World` canlı `WorldState`'i
   public verir (`DomainSimulationAdapter.cs:93`) — herhangi bir Presentation kodu sim'i
   doğrudan mutasyona açabilir; (c) tek Presentation asmdef olduğu için ihlal derlemede
   yakalanmaz.
3. **Statik kanal ailesi = global mutable state.** 10+ statik kanal (yukarıdaki envanter)
   domain-reload/scene-reload yaşam döngüsüne ve "tek tüketici" disiplinine güvenir; hiçbir
   test pinlemez (bkz. Testler). `RuntimeSpellFxMirror.RecallRequested` ve
   `RuntimeMainQuestMirror.FinaleRequested` consume'u VIEW'in Update'inde yapılır — ikinci bir
   tüketici eklenirse sessiz yarış. `JustCreatedWorld`/`OpeningHook` süreç ömürlü statiktir;
   aynı süreçte save YÜKLEYINCE sıfırlanmaz (yalnız yeni SeedWorld set eder,
   `MainQuest.cs:30`) — load-sonrası intro davranışı **doğrulanmadı**.
4. **Locator uyarısız ezer.** `EmberDomainAdapterLocator.Register` bilinçli overwrite
   (`IDomainSimulationAdapter.cs:273-277, 296-299`) — additive sahne yüklerinde çift kayıt
   maskelenir; yanlış sahne sırasında hangi adapter'in kazandığı görünmezdir.
5. **Adapter yazımları FieldOwnershipRegistry dışı.** Registry yalnız tick sistemlerini
   listeler (`FieldOwnershipRegistry.cs:14-17`); adapter'in `Actor.Vitals`/`Actor.Position`/
   `World.Stockpiles`-komşusu yazımları deftere ve lint testine görünmez. Bu, 01-time-cadence
   haritasının (c) sınıfı ("kadans yazar çatışmaları") hata ailesinin adapter tarafında AÇIK
   kalan kanadıdır: oyuncu-komutu yazımı ile hourly sistem yazımı aynı alana çakışırsa CI
   yakalamaz. ((a)-(g) taksonomisinin tanım dosyası repo'da bulunamadı — 10-save-load.md:158
   de aynı notu düşer; burada yalnız (c) ile bağ kurulabildi.)
6. **Adapter-yerel durum save'e girmez.** `_topicAskCounts`, `_echoCursor`, `_lastPlantsHash`,
   `_worldEncounterId`, `_currentSettlement` (kasıtlı — `Travel.cs:15-17`), `_conversation`
   adapter alanlarıdır; load sonrası sıfırlanır. Çoğu için zararsız/kasıtlı, ama aktif
   world-encounter save edilip yüklenirse bağın kopması beklenir — **doğrulanmadı**.
7. **Proof/looptest yüzeyi üretim sınıfının içinde.** `Proof*` metotları (örn.
   `ProofQuestSnapshot` `WorldEncounter.cs:73+`, `ProofConsultSage` `MainQuest.cs:67-77`,
   `ProofListSettlementNames` `MainQuest.cs:80-88`) teşhis içindir ama public API olarak
   agregat sınıfta yaşar; kontrat arayüzlerinde değildir — çağıran (diagnostics driver)
   somut tipe cast etmek zorunda kalır (host'un `ApplyCharacterCreation` cast'i de aynı
   desendedir, `EmberWorldHost.cs:93-94`).
8. **`WorldEncounter.cs` 994 satır.** En büyük partial; encounter + loot + respawn + kapı/
   tavern/temple + proof kancaları tek dosyada. `Combat.cs:1` başlığı "until command handlers
   are extracted" der — çıkarma yapılmadı.
9. **Dialog partial'ı boş iskelet.** `Dialog.cs` 7 satırlık indekstir; dialog sorumluluğu beş
   kardeş dosyaya yayılmıştır — partial sayısını şişiren ama zarar vermeyen bir kalıntı.
10. **Ad çakışması tuzağı.** İki `ColonyNeedsRow` (UI + Visual) — kök partial alias'la yaşar
    (`DomainSimulationAdapter.cs:17-21`); yeni partial'a dikkatsiz `using
    EmberCrpg.Presentation.Visual;` eklemek derleme hatası/yanlış tip bağlama üretir.

# 10-save-load

> Kapsam: `WorldState` <-> `WorldSaveData` DTO cift yonlu esleme; slot dosya IO (`FileSaveRepository`); Unity uzeri kayit orkestrasyonu (`EmberSaveService` + `SaveEnvelopeCodec`); W32 EAT reservation/action-log paralel-dizi persist; W34 WORK park-list emeklilik; W33 golden roundtrip (reflection-diff).
> Kanit disiplini: her iddia `file:line` ile. Emin olunmayan yerler "dogrulanmadi" olarak isaretli.

## HLD - Ne ve Neden

Kaydet, "**paralel dizi mezarligi**": `WorldSaveData` (`Assets/Scripts/Data/Save/WorldSaveData.cs:22-140`) tek bir `[Serializable]` DTO kok. Icinde 25+ arka arkaya dizilmis `ulong[] / int[] / string[]` sutunlari var (`pursuitGuardIds` + `pursuitTargetIds` + `pursuitUntilMinutes`; W32'de gelen 9-sutun `actionLog*` grubu; 5-sutun `reservation*` grubu; kritter, rumor, unrest, mainQuest, companion, spell dizileri). Bunu tek satirlik `struct` per-entry yerine paralel dizi olarak tutmanin gerekcesi: `UnityEngine.JsonUtility` **yalniz** `[Serializable]` diz ve DTO'lari serilestirir; `Dictionary` yok, `Tuple` yok, `List<T>` yok. Depo tarafinda tuttugumuz zengin domain kayitlari (`ReservationRecord`, `ActionLogEntry`, `PursuitRecord`) burada duz sutunlara acilir, load'da tekrar toplanir.

Kaydet iki katmanli: (1) **pure mapper** — `EmberCrpg.Data.SliceJson` asmdef'i (`noEngineReferences=true`, README `Assets/Scripts/Data/Save/SliceJson/README.md`), `WorldSaveMapper` parcali (`*.cs`, `.Process.cs`, `.Economy.cs`, `.Narrative.cs`, `.Quest.cs`, `.World.cs`, `.ActorDetail.cs`, `.ActionLog.cs`). (2) **Unity JSON kopru** — `JsonSliceSaveService` (`Assets/Scripts/Presentation/Ember/Save/JsonSliceSaveService.cs:18`) `JsonUtility.ToJson/FromJson<WorldSaveData>` cagirir; `EmberSaveService` (`Assets/Scripts/Presentation/Ember/Save/EmberSaveService.cs:14`) F5/F9 girdisini ve zarf (envelope) sarmalayicisini sever.

W32 EAT dilimi iki yeni yuk getirdi: `ReservationLedger` (5 sutun + `reservationNextId` sayaci) ve `ActionLogRing` (9 sutun + `actionLogTotalPushed` sayaci) — ikisi de golden roundtrip'in reflection-diff'inde alan-alan denetleniyor (`Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs:23-63`). W33 golden roundtrip zaten tum yeni koleksiyonlari (`GuardPursuits`, `Critters`, `Rumors`, `SiteUnrest`, `CompanionIds`, `MainQuest`) besleyip iki-kere save-load dongusunun **field-identical** olmasini talep ediyor; birakilan tek bir alan sonsuza kadar burada patlar.

W34-C WORK dilimi eski `JsonSliceSaveService._recipeWorkOrders` park listesini **emekli etti** (`JsonSliceSaveService.cs:21-27` yorumu). Eskiden `SaveToJson` bu listeyi `data.recipeWorkOrders`'a klobber ediyor, `LoadFromJson` ise geri okurken hicbir yere yazmiyordu — tek yonlu bir cadde, yani B19'da bulunan **cift-tuketim yarasi**: yuklenen bir talep bir sonraki saatte inputlarini tekrar tuketiyordu. W34-C, `recipeWorkOrders` DTO alanini artik pure-Domain `WorldSaveMapper.ToWorkOrderLedger` (`WorldSaveMapper.Process.cs:47-89`) uzerinden `world.WorkOrders` root'una okuyor; `jobId == 0` satirlar (legacy park-list satirlari) dusuruluyor. Bu, save/load bariyerini geri kazandi.

Schema surumleme: `WorldSaveData.schemaVersion` (`WorldSaveData.cs:26`), `WorldSaveMapper.CurrentSchemaVersion = 1` (`WorldSaveMapper.cs:30`). Pre-alan kayitlarda `JsonUtility` alan yoksa 0 okur → v1 taban kabul. Ileri surum (`schemaVersion > 1`) yuklenmez, `NotSupportedException` firlatilir (`WorldSaveMapper.cs:180-183`).

## HLD - Akis

### A. Kaydet (F5 quick / manual slot)

1. **Girdi:** `EmberInput.SaveQuick` → `EmberSaveService.Save()` → `SaveInternal(SaveSlotId.Quick)` (`EmberSaveService.cs:65,80,128`). Manual: `SaveSlot(SaveSlotId.Manual(n))` (`EmberSaveService.cs:79`).
2. **Domain export:** `SaveInternal` adapter'i bulur, `adapter.ExportStateJson()` ile `WorldState` → JSON. Bu cagri **ic**te `JsonSliceSaveService.SaveToJson(world)` cagiriyor (dogrulanmadi — adapter dosyasinda `ExportStateJson` grep'lenmedi ama zincir belli).
3. **Mapping:** `WorldSaveMapper.ToData(world)` (`WorldSaveMapper.cs:33-158`) her stor icin partial dosyalara delege eder:
   - `.Process.cs`: worksites, jobs, soils, plants, **workOrders (W34)**.
   - `.Economy.cs`: prices, stockpiles, tradeRoutes, caravans.
   - `.Narrative.cs`: events, toolCallTrace, llmProposalLog, topics, npcMemories.
   - `.Quest.cs`: quests, worldQuestStates, worldContracts.
   - `.World.cs`: actors, items, sites, factions, npcSeeds, worldProfile.
   - `.ActorDetail.cs`: player/talker/merchant/guard/enemy legacy role slotlari + envanterler.
   - `.ActionLog.cs`: `ToActionLogData(ring, data)` 9 paralel sutun + `TotalPushed`.
   - Inline (kok mapper): reservation 5 sutun + `NextId`; pursuit 3 sutun; critter 5; rumor 3 + `rumorEventCursorSeq` (B21); unrest 4; companion 1; mainQuest 4; spellIds 1.
4. **Zarf:** `JsonUtility.ToJson(data, pretty=true)` → domain JSON string (`JsonSliceSaveService.cs:100`). Sonra `EmberSaveService.SaveInternal` bunu `SaveData.domainStateJson` alanina koyar (`EmberSaveService.cs:167`), `SaveEnvelopeCodec.Encode(data)` (`SaveEnvelopeCodec.cs:9-29`) `SaveEnvelope { envelopeVersion=1, payload }` sarar.
5. **Diske:** `FileSaveRepository.Save(id, payloadJson, meta)` (`FileSaveRepository.cs:55-63`). Atomik: once `.tmp` yazar, sonra `File.Move`. `.meta.json` yan dosyasi best-effort — patlarsa payload commit'i olmus sayilir. Cikti: `<persistentDataPath>/saves/{quick|auto|manual_N}.json` + `.meta.json`.
6. **PlayerPrefs:** `LastSlotKey = "ember.save.lastslot"` (`EmberSaveService.cs:17`) yalnizca "son slot" pointer'i olarak yasar, kayitlar dosyada.

### B. Yukle (F9 quick / slot secimi)

1. **Girdi:** `EmberInput.LoadQuick` → `Load()` (`EmberSaveService.cs:48`) → `EmberSaveService.Load.cs:41` `LoadJson(json, slot)`.
2. **Zarf coz:** `SaveEnvelopeCodec.TryDecode(raw, out data, out migratedFromLegacy)` (`EmberSaveService.Load.cs:68`) — modern zarfi denerse, sonra eski payload-only formatini dener; ikisinde de patlarsa quarantine yolu.
3. **Legacy migrate:** `migratedFromLegacy == true` ise `_repo.Save(slot, migratedJson, meta)` ile yeni zarfa yazip payload'i **konumunda modernlestirir** (`EmberSaveService.Load.cs:87`) — bir daha eski format okumaz.
4. **Sahne bekle:** `_pendingLoad = data` (`Load.cs:107`); scene load'dan sonra `EmberSaveService.cs:206-210` sahne adi eslesince `RestorePosition` + `ApplyDomainRestore` calisir.
5. **Domain restore:** `ApplyDomainRestore` `domainStateJson`'u adapter'a verir (`Load.cs:166` `adapter.RestoreStateJson(...)`). Ic zincir: `JsonSliceSaveService.LoadFromJson(json)` (`JsonSliceSaveService.cs:104-131`):
   - Bos/null JSON → `ArgumentException` (Codex A/P3 kararı: sessiz `NewGame`'e dusme).
   - `JsonUtility.FromJson<WorldSaveData>(json)` → DTO.
   - `WorldSaveRehydration.CreateSeedWorld((int)data.roomSeed)` (`WorldSaveRehydration.cs:78-81`) tohum `WorldState`'i insa eder — bu Simulation layer'da (bakinci Codex 7. gecis B-P1 #10 not: Data asmdef Simulation'a bakmasin).
   - `WorldSaveMapper.ToWorld(data, seedWorld)` (`WorldSaveMapper.cs:175-315`) alan-alan hidrasyon.
6. **Schema kontrolu:** `data.schemaVersion > CurrentSchemaVersion` ise `NotSupportedException` (`WorldSaveMapper.cs:180-183`).
7. **Store hidrasyonu:** her partial'in `ToXStore(data.xArr)` yolu; paralel diziler `for (i = 0; i < min(len...); i++)` ile `record` tekrar toplanir (bkz. `WorldSaveMapper.cs:263-315` reservation/pursuit/critter/rumor/unrest bloklari).
8. **Ledger reset:** `world.Reservations.RebuildIndexes()` (`WorldSaveMapper.cs:279`), `WorkOrderLedger.RebuildIndexes()` (`WorldSaveMapper.Process.cs:87`), `world.ActionLog = ToActionLogRing(data)` (`WorldSaveMapper.cs:281`, `WorldSaveMapper.ActionLog.cs:42-76`) → `ring.Restore(entries, TotalPushed)`.
9. **Bridge mirror:** `JsonSliceSaveService._bridge` alani `world.Worksites/Jobs/Soils/Plants` referanslarini kopyalar (`JsonSliceSaveService.cs:123-128`) — round-trip testlerinde ve pre-BindWorld caller'larda bu servisin property'lerini okuyanlar yuklenmis dunyayi gorur.

### C. W34-C park-list emeklilik zinciri (B19 double-consumption kapanisi)

- **Eski hal:** `JsonSliceSaveService._recipeWorkOrders` List. `SaveToJson` bu listeden `data.recipeWorkOrders`'i yazardi. `LoadFromJson` ise `data.recipeWorkOrders`'u park listeye geri koymaz, `world` uzerine de yansitmazdi. Sonuc: kayittan yuklenen bir talep sanki hic baslamamis gibi bir sonraki saatte tekrar hammadde tuketirdi (B19).
- **Yeni hal (W34-C, `docs/ruh/w34/02 §5.2`):** park listesi ve `ReplaceRecipeWorkOrders()` API'si silindi (yorumla anitlastirildi: `JsonSliceSaveService.cs:21-27`). `WorldSaveMapper.ToData` `world.WorkOrders`'u DIREKT `data.recipeWorkOrders` DTO'suna `ToRecipeWorkOrderData(ledger)` (`WorldSaveMapper.Process.cs:51-67`) ile yazar. `ToWorld` `ToWorkOrderLedger(data.recipeWorkOrders)` (`WorldSaveMapper.Process.cs:69-89`) ile `world.WorkOrders`'a rehidre eder + `RebuildIndexes`.
- **Legacy satir dusme:** `jobId == 0L` satirlar (eski park-list zamanindan kalan; `RecipeWorkOrderSaveData.jobId` W34'te eklenen alan, `JsonUtility` pre-W34 dosyalarda 0 okur) yuklemede atlanir (`WorldSaveMapper.Process.cs:75`). Not: bu satirlar zaten pre-W34'te de world'e geri koyulmuyordu — "keep dropping" statu-quo devam.
- **Save-bridge klobber kaldirildi:** `SaveToJson` icinde `data.recipeWorkOrders` override calismasi silindi (`JsonSliceSaveService.cs:94-96` yorumu: "the retired park-list override used to CLOBBER it with an empty array here").

### D. W32 EAT reservation persist

- Yazma: `WorldSaveMapper.cs:105-111` — `reservationIds/SiteIds/ItemTags/ActorIds/UntilMinutes` = `world.Reservations.Rows.ConvertAll(...)`; `reservationNextId = world.Reservations.NextId ?? 1UL`.
- Okuma: `WorldSaveMapper.cs:262-279` — pre-W32 kayitta diziler null → bos ledger, `NextId = data.reservationNextId != 0 ? ... : 1UL`; sonra `RebuildIndexes()` cagrilir (indexler derived).
- Neden `NextId` diskte: yeniden 1'den baslarsa yeni rezervasyonlar eski id'lerle carpisir → determinism kirilir. Bu, dokta ozellikle vurgulanmis (`WorldSaveData.cs:81`).

### E. W32 all-zero-extends-Idle

- `WorldSaveData.cs:96-104` action-log 9 sutun `int[] actionLog{Intents,FromActions,FromPhases,ToActions,ToPhases,Reasons}` + 3 `long/ulong` sutun + `long actionLogTotalPushed`.
- Pre-W32 kayitta hepsi null → `ToActionLogRing` early-return ile bos ring (`ActionLog.cs:47`).
- Bir sutun eksik/yeni gelirse `JsonUtility` 0 okur; `(ActorActionType)0 == Idle`, `(ActionPhase)0 == Idle`, `(ActorIntent)0 == None`, `(ActionLogReason)0 == None` — hepsi Idle'a genisler (0-degeri enum guvenligi). Bu "all-zero-extends-Idle" invaryanti, unseasoned rows'un anlamli olmasini saglar; test edilen field: reflection golden diff (asagi).

### F. W33 golden roundtrip (reflection-diff)

`Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs:23`:
1. `WorldFactory.Create(7)` seed.
2. Her **yeni koleksiyon** (companions, pursuits, critters, rumors, unrest, W32 reservation + W32 action-state + W33 hands-full HaulCrop actor) doldurulur.
3. `WorldSaveMapper.ToData(world)` → `ToWorld` → `ToData` (**iki-kere** save-load).
4. `Assert.That(secondData, Is.EqualTo(firstData))` — reflection-diff. Bir mapper alani dusurdugunde HER ZAMAN buradan patlar (dokta "the Home/DayAnchor class of bug"). Kimse ozel test yazmayi hatirlamasa da bu tek test yeni alanlari yakalar.

## LLD - Veri Modeli (file:line)

| Tip | Alanlar | Yer |
|---|---|---|
| `WorldSaveData` | 25+ paralel-dizi + skaler alan; `schemaVersion, totalMinutes, currentRoomId, actors[], itemRecords[], sites[], factions[], factionReputations[], prices[], stockpiles[], tradeRoutes[], caravans[], worldEvents[], worldEventFirstRetainedSeq (B21), toolCallTrace[], llmProposalLog[], npcSeeds[], worldProfile, worksites[], recipeWorkOrders[], jobs[], soils[], plants[], inventory, playerEquipment, merchantInventory, companionIds[], pursuit(3), reservation(5)+NextId (W32), actionLog(9)+TotalPushed (W32), critters(5), rumors(3)+rumorEventCursorSeq (B21), unrest(4), mainQuest(4), playerKnownSpellIds[], playerGold/merchantGold, pickups[], topics[], npcMemories[], playerSpellCooldowns, playerShieldBuffs, doorOpen, ...` | `Data/Save/WorldSaveData.cs:22-140` |
| `WorldSaveData` partial'lari | `.WorldProcess.cs`: ItemRecord/SiteRecord/Worksite/**RecipeWorkOrder(jobId+completedExecutions W34)**/Soil/Plant DTO; `.Economy.cs`, `.Narrative.cs`, `.Quest.cs`, `.ActorDungeon.cs` | `Data/Save/WorldSaveData.*.cs` |
| `RecipeWorkOrderSaveData` | `recipeId, siteId, positionX/Y, actorId, progressTicks, jobId (W34), completedExecutions (W34)` | `Data/Save/WorldSaveData.WorldProcess.cs:37-51` |
| `WorldSaveMapper` (kok) | `const int CurrentSchemaVersion = 1`; `static WorldSaveData ToData(WorldState)`; `static WorldState ToWorld(WorldSaveData, WorldState seedWorld)` | `Data/Save/SliceJson/WorldSaveMapper.cs:27-315` |
| `SaveEnvelope` / `SaveEnvelopePayload` | `envelopeVersion=1, payload{sceneName, playerPosXYZ, playerYaw, tickIndex, domainStateJson}` | `Data/Save/SaveEnvelope.cs:5-25` |
| `SaveSlotId` | `struct { SaveSlotKind Kind, int Index }`; `FileStem() = "manual_N" | "auto" | "quick"` | `Data/Save/SaveSlotId.cs:5-61` |
| `SaveSlotKind` | `Manual | Auto | Quick` enum | `Data/Save/SaveSlotKind.cs` |
| `SaveSlotMetadata` | `metadataVersion, envelopeVersion, schemaVersion, slotKind, slotIndex, label, sceneName, playtimeMinutes, savedAtUtcIso, thumbnailPath` | `Data/Save/SaveSlotMetadata.cs:5-19` |
| `FileSaveRepository` | `_savesDir = <root>/saves`; atomic .tmp+Move; `.meta.json` sidecar best-effort; `.corrupt[.N]` quarantine (DET-06 forensics: en yeniyi ezmez, "corrupt.N" ile numaralar) | `Data/Save/FileSaveRepository.cs:15-140` |
| `JsonSliceSaveService` (Presentation) | `WorldState _bridge`; `BindWorld(world)`; `SaveToJson(world)`; `LoadFromJson(json)`; W34: `_recipeWorkOrders` EMEKLI; ctor `Func<RecipeId, RecipeDef>` yalniz caller uyumu icin duruyor | `Presentation/Ember/Save/JsonSliceSaveService.cs:18-131` |
| `SaveEnvelopeCodec` (Presentation) | `Encode(SaveData) → SaveEnvelope JSON`; `TryDecode(raw, out data, out migratedFromLegacy)` — modern zarf → eski payload-only → false | `Presentation/Ember/Save/SaveEnvelopeCodec.cs:1-50` |
| `EmberSaveService` (Presentation) | `MonoBehaviour, partial (.cs/.Load/.Resolve/.Ui)`; F5/F9 kisayolu; `SaveInternal(slot)`; `LoadJson(json, slot?)`; `_pendingLoad` scene load bekleyicisi; `WorldFactory` seed'i `WorldSaveRehydration.CreateSeedWorld` uzerinden | `Presentation/Ember/Save/EmberSaveService*.cs` |
| `WorldSaveRehydration` (Simulation) | `CreateSeedWorld(roomSeed)` = `new WorldFactory().Create(roomSeed)`; RecipeWorkOrder <-> DTO iki yon (LEGACY: bu iki metod Simulation.Process.RecipeWorkOrder cindi surer, W34-C sonrasi cagirilan yer YOK — dogrulanmadi grep aktif callsite icin) | `Simulation/Process/WorldSaveRehydration.cs:12-82` |

## LLD - Fonksiyon Haritasi

| Imza | Yer | Ne yapar |
|---|---|---|
| `static WorldSaveData WorldSaveMapper.ToData(WorldState world)` | `Data/Save/SliceJson/WorldSaveMapper.cs:33-158` | Domain → DTO. `world == null` → `ArgumentNullException`. Her stor icin partial'a delege eder; reservation/pursuit/critter/rumor/unrest/mainQuest inline paralel-dizi doldurur; sonda `ToActionLogData(world.ActionLog, data)`. |
| `static WorldState WorldSaveMapper.ToWorld(WorldSaveData data, WorldState seedWorld)` | `Data/Save/SliceJson/WorldSaveMapper.cs:175-315` | DTO → Domain. `seedWorld == null` → `ArgumentNullException`. `schemaVersion > 1` → `NotSupportedException`. Aktor legacy role fallback (pre-actors[] kayitlar). `RebuildIndexes()` reservation ve workOrders'ta. |
| `static RecipeWorkOrderSaveData[] WorldSaveMapper.ToRecipeWorkOrderData(WorkOrderLedger ledger)` | `Data/Save/SliceJson/WorldSaveMapper.Process.cs:51-67` | W34: pure-Domain `WorkOrderRecord` satirlarini DTO'ya cevirir (`jobId + completedExecutions` dahil). |
| `static WorkOrderLedger WorldSaveMapper.ToWorkOrderLedger(RecipeWorkOrderSaveData[] data)` | `Data/Save/SliceJson/WorldSaveMapper.Process.cs:69-89` | W34: DTO → `WorkOrderLedger`. **`jobId == 0` legacy satirlari duser**; sonra `RebuildIndexes()`. |
| `static JobBoard WorldSaveMapper.ToJobBoard(JobRequestSaveData[] data)` | `Data/Save/SliceJson/WorldSaveMapper.Process.cs:98-130` | Once insertion sirasinda `Add`, sonra `claimSequence` sirasinda `TryRestoreClaim` — PR#138 bot review fix'i (queue index roundtrip stabil). |
| `private static void WorldSaveMapper.ToActionLogData(ActionLogRing ring, WorldSaveData data)` | `Data/Save/SliceJson/WorldSaveMapper.ActionLog.cs:14-40` | Ring → 9 paralel `int[]/ulong[]/long[]` sutun + `TotalPushed`. |
| `private static ActionLogRing WorldSaveMapper.ToActionLogRing(WorldSaveData data)` | `Data/Save/SliceJson/WorldSaveMapper.ActionLog.cs:42-76` | Herhangi bir sutun null → bos ring (pre-W32). `count = Math.Min(...)` her sutunu klipler → yalniz tam-genislikli satirlari toplar; `ring.Restore(entries, TotalPushed)`. |
| `string JsonSliceSaveService.SaveToJson(WorldState world)` | `Presentation/Ember/Save/JsonSliceSaveService.cs:79-101` | `WorldSaveMapper.ToData` + `_bridge` override'lari (yalniz `Count > 0` ise klobber yok) + `JsonUtility.ToJson(pretty=true)`. |
| `WorldState JsonSliceSaveService.LoadFromJson(string json)` | `Presentation/Ember/Save/JsonSliceSaveService.cs:104-131` | Bos JSON → `ArgumentException`. `FromJson<WorldSaveData>` → `WorldSaveRehydration.CreateSeedWorld((int)data.roomSeed)` → `WorldSaveMapper.ToWorld` → `_bridge`'e store mirror. |
| `WorldState JsonSliceSaveService.BindWorld(WorldState world)` | `Presentation/Ember/Save/JsonSliceSaveService.cs:43-48` | Bridge'i canli dunyaya baglar, `EnsureInvariants`; adapter kurulumunda cagrilir. |
| `string SaveEnvelopeCodec.Encode(SaveData data)` | `Presentation/Ember/Save/SaveEnvelopeCodec.cs:9-29` | `SaveData` → `SaveEnvelope(envelopeVersion=1)` → `JsonUtility.ToJson` (compact). |
| `bool SaveEnvelopeCodec.TryDecode(string rawJson, out SaveData data, out bool migratedFromLegacy)` | `Presentation/Ember/Save/SaveEnvelopeCodec.cs:31-49` | Modern zarf → eski payload-only fallback → false; `migratedFromLegacy` flag'i in-place modernizasyon icin. |
| `void EmberSaveService.Save()` / `SaveSlot(SaveSlotId)` | `Presentation/Ember/Save/EmberSaveService.cs:52-83` | BUG-SAVE-CRASH: **butun** govde `try/catch` sarilmis; F5 hicbir kosulda oyunu kapamaz. |
| `void EmberSaveService.SaveInternal(SaveSlotId slot)` | `Presentation/Ember/Save/EmberSaveService.cs:128-190` | Adapter'dan `ExportStateJson()`, `SaveData` doldur, `SaveEnvelopeCodec.Encode`, `_repo.Save(slot, json, meta)`. |
| `void EmberSaveService.LoadJson(string json, SaveSlotId? migratedSlot)` | `Presentation/Ember/Save/EmberSaveService.Load.cs:66-107` | `TryDecode` → legacy ise in-place migrate (`_repo.Save(slot, migratedJson, meta)`) → `_pendingLoad = data` + sahne bekle. |
| `DomainRestoreResult EmberSaveService.ApplyDomainRestore(SaveData data)` | `Presentation/Ember/Save/EmberSaveService.Load.cs:140-170` | Bos `domainStateJson` → `NoPayload`; degilse `adapter.RestoreStateJson(data.domainStateJson)`. |
| `string EmberSaveService.ResolveLatestSaveJson(FileSaveRepository)` | `Presentation/Ember/Save/EmberSaveService.Resolve.cs:57-77` | Continue akisi icin en yeni gecerli slotu bulur. |
| `void FileSaveRepository.Save(SaveSlotId id, string payloadJson, SaveSlotMetadata meta)` | `Data/Save/FileSaveRepository.cs:55-63` | Atomik `.tmp`+Move payload, best-effort `.meta.json` sidecar. |
| `bool FileSaveRepository.TryLoadPayload(SaveSlotId id, Func<string,bool> isValid, out string payloadJson)` | `Data/Save/FileSaveRepository.cs:75` | Bozuk payload ise `Quarantine` → `.corrupt[.N]` (DET-06). |
| `static WorldState WorldSaveRehydration.CreateSeedWorld(int roomSeed)` | `Simulation/Process/WorldSaveRehydration.cs:78-81` | `new WorldFactory().Create(roomSeed)` — Simulation-side seed insaci; Data asmdef Simulation'a bakmasin diye buraya tasindi (Codex 7. gecis B-P1 #10). |

## LLD - Yazdigi/Okudugu Alanlar (FieldOwnershipRegistry dilinde)

**Yazar (Save):** `WorldSaveMapper.ToData` `WorldState`'i **okur**; herhangi bir dunya alanini yazmaz. Bu bir "read-through export."

**Yazar (Load):** `WorldSaveMapper.ToWorld` `seedWorld` uzerine **direkt** yazar (assignment). Dokunulan alanlarin ozeti (registry'ye "world.load@LoadFromJson" ekleyecek olsaydik):
- Aktorler: `world.Actors` (roll actors[] veya legacy 5 slot).
- Zamanlar: `world.Time`, `world.CurrentRoomId, PlayerRoomId, TalkerRoomId, MerchantRoomId, GuardRoomId, EnemyRoomId, PickupRoomId`.
- Zindanlar: `world.Dungeon, DungeonRoomStates, DungeonDoorStates` (varsa).
- Item/site/faction: `world.Items, Sites, Factions` + faction reputations uygulanmis.
- Ekonomi: `world.Prices, Stockpiles, TradeRoutes, Caravans`.
- Proses (SOUL-01 sonrasi world root'ta): `world.Worksites, Jobs, WorkOrders (W34), Soils, Plants`.
- Quest: `world.Quests, WorldQuestStates, WorldContracts`.
- Olay/log: `world.Events (+ FirstRetainedSeq B21), ToolCallTrace, LlmProposalLog, NpcSeeds, WorldProfile`.
- Envanterler: `world.PlayerInventory, PlayerEquipment, MerchantInventory` (kapasite: DTO'da varsa DTO kazanir, degilse seed).
- Oyuncu: `PlayerLevel, PlayerXp, PlayerClassName, PlayerReputation, PlayerBountyGold, PlayerGold, MerchantGold, MerchantStoreSeeded, PlayerKnownSpellIds` (B20: uzunluk-guard boş DTO icin in-memory listeyi ezmez), `PlayerSpellCooldowns, PlayerShieldBuffs`.
- W32: `world.Reservations` (Rows + NextId + RebuildIndexes), `world.ActionLog` (Restore + TotalPushed).
- P0-P2: `world.GuardPursuits, Critters, Rumors, RumorEventCursorSeq (B21), SiteUnrest`.
- V3: `world.CompanionIds`.
- F31: `world.MainQuest` (act clamp, RequiredInscriptions default 3, ClaimedDelveIds).
- Dialog: `world.Pickups, Topics, NpcMemory`.
- Gate flags: `DoorOpen, GuardDoorAccessGranted, GuardWarningCount, EncounterActive, LastNarrative`.

**Registry mevcut kayit:** `Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs` icinde grep'te "save" bulunmadi — save yolu registry'ye kayitli DEGIL (dogrulanmadi ama arama negatif). Kayit sistemi butun dunyayi yaziyor; ownership registry per-tick sistem yazarlarina odaklanmis. Save-load yasi butun registry alanlarini "hepsi bir anda yazilir" olarak gorulmeli.

## LLD - Urettigi/Tukettigi Olaylar

- **Uretmez:** kayit/yukle yolu `WorldEventLog`'a olay eklemez (`WorldSaveMapper.ToWorld` sadece diskteki `worldEvents[]` dizisini `world.Events` icine kopyalar, yeni satir uretmez).
- **Tuketir:** dogrudan yok. Ancak `WorldEventLog.FirstRetainedSeq` disk-persist edildigi icin (B21, `WorldSaveData.cs:60`; mapper `WorldSaveMapper.cs:88-90`), yuklemede `world.Events` cursor'lari (`RumorEventCursorSeq` + `FirstRetainedSeq`) seq-based olarak dogru offset'e dusurulur — cursor'lar olay-yayilim tuketicileri (rumor mill vb.) tarafindan sonraki tick'te tukenir.
- **Unity taraf:** `EmberSaveService.SaveInternal` `Debug.Log("[EmberSave] quick-save ok")`, `LoadJson` `ShowStatus(...)` UI olayi (BUG-SAVE-CRASH koruma sarmali).

## Testler

- `Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs` — **W33 golden roundtrip** (reflection-diff, iki-kere roundtrip). W32-05 hikayesi: eat action-state, live reservation + bumped NextId; W33-01 §9.3 hikayesi: hands-full HaulCrop actor + carry: rezervasyon (`WorldSaveMapperGoldenRoundtripTests.cs:23-63`).
- `Assets/Tests/EditMode/Save/SaveLoadDigestRoundtripTests.cs` — F4-DoD: seed → **tam gunluk tick** (jobs/growth/prices/needs) → save → load → `WorldStateDigest` byte-identical (`SaveLoadDigestRoundtripTests.cs:1-337`, ozel test: `WorldQuests_SurviveSaveLoadRoundtrip`, F22 slice).
- `Assets/Tests/EditMode/Save/JsonSliceSaveServiceTests.cs` — servis-level roundtrip; bridge klobber davranisi (W33 farm slice fix).
- `Assets/Tests/EditMode/Save/RecipeWorksiteRoundTripTests.cs` — W34-C oncesi park-list yolu; W34-C sonrasi W34 DoD altinda `world.WorkOrders` uzerinden yeniden yasar.
- `Assets/Tests/EditMode/Save/JobAssignmentRoundTripTests.cs` — W34-C `WorkOrderLedger` + `JobBoard.TryRestoreClaim` sirasi.
- `Assets/Tests/EditMode/Save/StoreRoundTripTests.cs` — ItemStore/SiteStore/FactionStore duz mapper testleri.
- `Assets/Tests/EditMode/Save/ActorSaveMapperTests.cs`, `ActorNeedsRoundTripTests.cs`, `ShieldBuffSaveMapperTests.cs`, `SpellCooldownSaveMapperTests.cs` — per-mapper unit.
- `Assets/Tests/EditMode/Save/SaveSchemaVersionTests.cs` — schema drift red senaryosu (`schemaVersion > 1` → `NotSupportedException`); pre-alan 0'in v1 taban kabulu.
- `Assets/Tests/EditMode/Save/SaveEnvelopeCodecTests.cs` — modern zarf enc/dec + legacy payload-only migrasyonu.
- `Assets/Tests/EditMode/Save/FileSaveRepositoryTests.cs` — atomik `.tmp`+Move, `.corrupt[.N]` quarantine, sidecar best-effort.
- `Assets/Tests/EditMode/Save/SaveSlotRepositoryTests.cs`, `SaveSlotBrowserStateTests.cs` — slot listeleme/browser state.
- `Assets/Tests/EditMode/Save/EmberSaveServiceResolutionTests.cs` — `ResolveLatestSaveJson` continue-akisi.
- `Assets/Tests/EditMode/Save/WorldSaveMapperTradeFieldsTests.cs`, `PlantSeasonRoundTripTests.cs` — dilim-spesifik alanlar.
- `Assets/Tests/EditMode/Worldgen/WorldProfileSaveRoundTripTests.cs`, `NpcSeedSaveRoundTripTests.cs` — worldgen tarafi DTO roundtrip.
- `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs` — save patenti degil ama digest golden'i save-load sonrasi tick determinizminin (F4-DoD) altyapisi.

## W32-W36 Degisiklikleri (bu sistemin son 5 haftadaki buyuk hareketleri)

**W32 (EAT / action phase-machine):**
- Reservation ledger persist: `WorldSaveData.cs:84-92` 5 sutun + `reservationNextId`. Mapper: `WorldSaveMapper.cs:105-111` (write) + `WorldSaveMapper.cs:262-279` (read + `RebuildIndexes`).
- Action-log ring persist: `WorldSaveData.cs:96-104` 9 sutun + `actionLogTotalPushed`. Mapper: yeni partial `WorldSaveMapper.ActionLog.cs`. "All-zero-extends-Idle" invaryanti sayesinde eksik sutun eklendiginde eski kayitlar bozulmaz.
- Golden roundtrip'e W32 hikayesi: `WorldSaveMapperGoldenRoundtripTests.cs:35-55` (eat action-state + non-zero progress + live reservation).

**W33 (FARM):**
- Bridge klobber fix: `JsonSliceSaveService.SaveToJson` `_bridge.Worksites/Jobs/Soils/Plants` override'lari **yalniz** `Count > 0` ise uygulanir (`JsonSliceSaveService.cs:82-100` yorumu). Onceki hal: bagli olmayan (unbound) bir servis, dis dunyanin store'larini bos bridge ile klobberliyordu → soil/plant/job kayboluyordu (faction-decay save-replay tesaduf golden'i turuyordu).
- Golden roundtrip'e W33 hikayesi: hands-full HaulCrop actor + `carry:` rezervasyonu (`WorldSaveMapperGoldenRoundtripTests.cs:44-63`).

**W34 (WORK):**
- Park-list emeklilik: `_recipeWorkOrders` List<> ve `ReplaceRecipeWorkOrders` API'si `JsonSliceSaveService`'den kaldirildi (`JsonSliceSaveService.cs:21-27,94-96` yorumlari). B19 double-consumption save wound structurally closed.
- `WorkOrderLedger` Domain store'u world root'a bindi (`world.WorkOrders`); mapper direkt yazip okuyor (`WorldSaveMapper.Process.cs:51-89`).
- `RecipeWorkOrderSaveData` append-only 2 alan: `jobId` (rebind anahtari) + `completedExecutions` (`WorldSaveData.WorldProcess.cs:37-51`). `jobId == 0` legacy satirlari load'da dusuyor.
- Save-mapping her iki yon + digest satirlari + golden seeds "non-default sleep/work state" ile beslendi (W34-A DoD).

**W35-W36:** save-load sisteminde HLD-degistiren buyuk hareket bulunmadi (grep negatif `docs/atlas` altinda W35/W36 save baglantisi icin — dogrulanmadi ama kayit patologisi tarihcesi W34-C ile kapali gorunuyor).

**B21 (jul 25-26 kayak):** WorldEventLog seq-based trim + `worldEventFirstRetainedSeq` + `rumorEventCursorSeq` migrasyon (`WorldSaveData.cs:58-62,110`). Mapper `WorldSaveMapper.cs:88-90` (write) + `world.Events = ToWorldEventLog(data.worldEvents, data.worldEventFirstRetainedSeq)` (read). Pre-fix `int rumorEventCursor` alani JsonUtility tarafinda missing-field-drops-to-0 semantigiyle dusuyor; sicak save gecisinde kayip.

**B20 (jul 26):** `PlayerKnownSpellIds` restore uzunluk-guard'i (`WorldSaveMapper.cs:293-295`) — bos DTO in-memory listeyi ezmez.

## Bilinen Borclar + Kacak Kapilari

- **Legacy role slotlari:** `player/talker/merchant/guard/enemy` + `playerRoomId..enemyRoomId` alanlari `actors[]`'e migre olmus dunyalarla birlikte hala yaziliyor (`WorldSaveData.cs:31-49` yorumu + `WorldSaveMapper.cs:61-65,185-197`). Phase 13 cleanup'a kadar duruyor — yeni kod bunlara yazmamali, `actors[]` / world stores'a yazmali. Kacak kapisi: mapper hala her save'de bu 5 role icin `FirstByRole(...)` cagirir; buyuk dunyalarda `O(N * 5)` linear scan.
- **`WorldSaveRehydration.RecipeWorkOrder <-> DTO` iki metodu:** W34-C sonrasi cagiran YOK gorunuyor (`Simulation/Process/WorldSaveRehydration.cs:29-73`) — grep aktif callsite icin negatif kalmis (dogrulanmadi, geniş grep gerekir). Emekli olmasi lazim.
- **Adapter'daki `ExportStateJson()` / `RestoreStateJson()`:** bu doktan iz surmedim (`DomainSimulationAdapter` altinda grep aktif metod adlariyla). Load zinciri `EmberSaveService.Load.cs:166` `adapter.RestoreStateJson(...)` cagriyor — implementasyonun icinde `JsonSliceSaveService.LoadFromJson` cagrildigi varsayimi mantikli ama **kesin dogrulanmadi**. Bir sonraki atlas revizyonunda bu koprunun tam dosyasi kanitlanmali.
- **Schema versiyon 1'de takili:** hicbir yeni field bump gerektirmedi (append-only + `JsonUtility` missing-field-0 semantigi). Bir sey **inkompatibl** degistiginde `CurrentSchemaVersion++` + `ToWorld` icine migration branch eklenmeli (`WorldSaveMapper.cs:29-31` protokolu).
- **JsonUtility 0-degeri enum guvenligi:** `ActorActionType.Idle == 0`, `ActionPhase.Idle == 0`, `ActorIntent.None == 0`, `ActionLogReason.None == 0`. Bir gun bu enum'lardan biri 0 slotuna baska anlam koyarsa "all-zero-extends-Idle" invaryanti sessizce kirilir. Bu kural yorumu yalniz `WorldSaveMapper.ActionLog.cs`'de gecmiyor — dogrulanmadi, "0-guard" testine ihtiyac var.
- **`.corrupt.N` sonsuz birikimi:** `FileSaveRepository.Quarantine` her bozuk kayidi forensik olarak saklar (DET-06); temizlik yok. Uzun sureli oynayan bir kullanicida disk sisebilir. Kaçak: `saves/` altinda `.corrupt` sayaci monotonik.
- **`SaveEnvelopeCodec` legacy fallback yolu:** payload-only eski format hala kabul ediliyor + in-place modernize ediliyor. Bu koruma sonsuza kadar acik kalirsa gecmis-yuk borcu birikir; bir tarihte "hard cut" gerekebilir.
- **BUG-SAVE-CRASH sarma:** `Save()` govdesi butunuyle `try/catch` — bu, F5'in `Update` icinde patlayip prosesi indirmesini engelledi (`EmberSaveService.cs:52-71` yorumu). Ancak `StackOverflowException` gibi catchable-degil hatalar da unutulmamali; save yolu flat field-mapping, o yuzden risk yok (dokta soylenmis).
- **Rumor cursor migrasyon:** B21 pre-fix `int rumorEventCursor` **kaybolur** (JsonUtility missing-field → 0). Bu sicak save gecisinde bir kez agri verir; production bir kayit gozetim ihtiyaci varsa bir sonraki release'de warn-toast yardimci olur.

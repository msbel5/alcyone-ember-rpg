# 10-save-load

## HLD - Ne ve Neden

Kayit/yukleme sistemi, deterministik `WorldState`'in tamamini JSON'a dondurup diskteki dosya slotlarina yazan ve geri okuyan katmanli boru hattidir. Oyuncuya gorunen yuzu: F5 quicksave / F9 quickload (`EmberSaveService.Update`, EmberSaveService.cs:46-50), 5 dakikada bir otomatik kayit (`RuntimeAutosaveView`, RuntimeAutosaveView.cs:13,25-27), sahne gecisinde autosave (EmberScenePortal.cs:33) ve ana menudeki Continue/slot tarayicisi. Felsefe uc kurala dayanir: (1) saf esleme — `WorldSaveMapper` motor-serbest (`noEngineReferences=true`) bir asmdef icinde yasar (EmberCrpg.Data.SliceJson.asmdef:16), Unity'ye tek temas noktasi Presentation'daki `JsonSliceSaveService`'in `JsonUtility` cagrisidir; (2) asla oyunu dusurme — kayit/yukleme govdeleri bastan sona try/catch ile sarilidir (EmberSaveService.cs:63-72, EmberSaveService.Load.cs:18-27), bozuk dosyalar `.corrupt[.N]` olarak karantinaya alinir (FileSaveRepository.cs:106-119); (3) yuvarlanabilirlik kanitla — golden roundtrip testi her DTO alanini yansima ile karsilastirir, digest testi bir gunluk simulasyondan sonra bayt-ozdesligi ister. Ic ice iki zarf vardir: dis `SaveEnvelope` (sahne adi + rig pozisyonu + tick + `domainStateJson` stringi, SaveEnvelope.cs:445-463) ve onun icindeki `WorldSaveData` domain JSON'u. Sema versiyonu (`schemaVersion`, su an 1) ileri-uyumsuz kayitlari acikca reddeder (WorldSaveMapper.cs:31,141-144).

## HLD - Akis

### Kayit (F5 / slot / autosave)
1. **Tetik**: `EmberInput.SaveQuick` (F5, EmberSaveService.cs:48) -> `Save()`; slot UI -> `SaveSlot(SaveSlotId)` (EmberSaveService.cs:75); 300 sn unscaled kadansla `RuntimeAutosaveView.Update` -> `TryAutosaveActiveScene()` (RuntimeAutosaveView.cs:25-27, EmberSaveService.cs:112-126); sahne portali gecisinde de autosave (EmberScenePortal.cs:33).
2. `SaveInternal(slot)` (EmberSaveService.cs:128-202): "PlayerRig" GameObject'i bulunamazsa **sessizce cikar** (satir 130-131). Adapter varsa `adapter.ExportStateJson()` cagrilir; hata firlatirsa `domainFailed` isaretlenir ve "Save partial: domain export failed." gosterilir (satir 150-158, 194).
3. `ExportStateJson` -> `JsonSliceSaveService.SaveToJson(world)` (DomainSimulationAdapter.Save.cs:24-29, JsonSliceSaveService.cs:84-97): `WorldSaveMapper.ToData(world)` + bridge uzerinden `worksites/recipeWorkOrders/jobs/soils/plants` alanlarinin override'i -> `JsonUtility.ToJson(data, true)`.
4. `SaveEnvelopeCodec.Encode` dis zarfi uretir (SaveEnvelopeCodec.cs:8-28).
5. `FileSaveRepository.Save(id, payload, meta)` (FileSaveRepository.cs:50-59): payload `.tmp + File.Replace` ile atomik yayinlanir (DET-07, satir 179-188); metadata sidecar'i best-effort'tur, hatasi payload'i asla dusurmez (satir 57-58).
6. Dosya yazimi basariliysa PlayerPrefs mirror'i + son-slot isaretcisi guncellenir (DET-05: ikisi birlikte ya da hicbiri, EmberSaveService.cs:171-201).

### Yukleme (F9 / slot / menu Continue)
1. **Tetik**: `EmberInput.LoadQuick` (F9) -> `Load()` (EmberSaveService.cs:49, EmberSaveService.Load.cs:12-28); slot UI -> `LoadSlot` (Load.cs:30-49); menu Continue -> `TryResolveLatestSave` + `PreparePendingLoad` (EmberSaveService.Resolve.cs:94-117,44-47).
2. `ResolveLatestSaveJson` oncelik sirasi: Quick slot dosyasi -> son kullanilan sayili slot dosyasi -> legacy PlayerPrefs blob (Resolve.cs:57-72). Her dosya okuma `IsLoadableSaveJson` karantina yuklemi ile gecer (Resolve.cs:25-29); yuklemi gecemeyen dosya `.corrupt[.N]`'e tasinir (FileSaveRepository.cs:160-177,106-119).
3. `SaveEnvelopeCodec.TryDecode` once guncel zarfi, olmuyorsa legacy ciplak `SaveData`'yi dener (SaveEnvelopeCodec.cs:30-49); legacy'den yuklenen slot hemen guncel zarf formatina geri yazilir (E7-007, Load.cs:82-94).
4. Sahne farkliysa `_pendingLoad`'a koyulup `SceneManager.LoadScene`; yeni sahnenin `Start()`'i payload'i devralir (Load.cs:96-109, EmberSaveService.cs:204-223). Sahne EditorBuildSettings'te yoksa yukleme reddedilir (Load.cs:102-106).
5. `ApplyDomainRestore` (Load.cs:149-187): `adapter.RestoreStateJson(domainStateJson)` basarirsa `EmberTickDriver.AlignTo(tickIndex)` — basarisizsa AlignTo atlanir (desync onlemi, PR #188 notu satir 155-162). Sonuc `NoPayload/NoAdapter/Failed/Restored` olarak UI statusune yansir.
6. `RestoreStateJson` (DomainSimulationAdapter.Save.cs:31-65): `JsonSliceSaveService.LoadFromJson` -> `schemaVersion` kapisi (WorldSaveMapper.cs:141-144) -> `WorldSaveRehydration.CreateSeedWorld((int)data.roomSeed)` (JsonSliceSaveService.cs:116, WorldSaveRehydration.cs:78-80) -> `WorldSaveMapper.ToWorld(data, seedWorld)` -> canli dunyaya `_world.CopyFrom(restored)`; canli oturumun `Overland`'i korunur (satir 44-49), `EnsureInvariants()` null store'lari onarir (satir 51-55), `_tickComposer.RebuildAccumulatorsFrom(_world.Time)` kadans akumulatorlerini mutlak oyun zamanina yeniden baglar (DET-01, satir 57-64).

## LLD - Veri Modeli

### WorldSaveData (kok DTO, partial sinif)
- Govde: Assets/Scripts/Data/Save/WorldSaveData.cs:20-113. `schemaVersion` (satir 25), zaman/oda alanlari (26-35), dungeon dizileri (36-40), **legacy adli aktorler** `player/talker/merchant/guard/enemy` (41-45, geriye-uyumluluk icin cift yazilir — dosya basi tasarim notu satir 9-16), kanonik `actors[]` (46), store dizileri `itemRecords/sites/factions/factionReputations/prices/stockpiles/tradeRoutes/caravans` (47-54), `worldEvents/toolCallTrace/llmProposalLog/npcSeeds/worldProfile` (55-59), surec store'lari `worksites/recipeWorkOrders/jobs/soils/plants` (60-64), envanter/ekipman (65-67), oyuncu ilerlemesi (68-73), ana gorev omurgasi F31 (95-98), altin/pickup/topic/npc hafizasi/cooldown/shield (100-107), kapi-karsilasma bayraklari (108-112).
- **Parallel arrays** (WorldSaveData.cs:74-93): pursuit `pursuitGuardIds/pursuitTargetIds/pursuitUntilMinutes` (75-77); critter `critterIds/critterSiteIds/critterXs/critterYs/critterKinds` (79-83); rumor `rumorBornMinutes/rumorSiteIds/rumorTexts` + `rumorEventCursor` (85-88); unrest `unrestSiteIds/unrestValues/unrestLastDecayDays/unrestSweepCooldownUntilMinutes` (90-93). Yazma tarafi `List.ConvertAll` ile ayni kaynak listeden uretildigi icin uzunluklar esittir (WorldSaveMapper.cs:96-111); okuma tarafi min-uzunluk zip'i ile korunur (WorldSaveMapper.cs:204-245).
- Quest partial'i: `quests/worldQuestStates/worldContracts` (WorldSaveData.Quest.cs:124-131), `WorldContractSaveData` (133-150), `QuestStateSaveData` (152-160).
- Aktor/dungeon DTO'lari: `ActorSaveData` (WorldSaveData.ActorDungeon.cs:70-123; `hasHomeAnchor` satir 81 ve `hasMood` satir 122 — "0 mu, alan yok mu" ayirt etme bayraklari), `JobRequestSaveData.claimSequence` (156-160, kuyruk sirasinin korunmasi), envanter/item/pickup/topic/npc-hafiza/cooldown/shield DTO'lari (163-257).
- Ekonomi DTO'lari: WorldSaveData.Economy.cs:6-64. Anlati/AiDm DTO'lari: WorldSaveData.Narrative.cs:71-118. Surec DTO'lari: WorldSaveData.WorldProcess.cs:167-232. `NpcSeedSaveData/WorldProfileSaveData`: WorldSaveData.cs:115-140.

### Zarf ve slot katmani
- `SaveEnvelope` (`envelopeVersion`, `payload`) + `SaveEnvelopePayload` (`sceneName`, rig pozisyon/yaw, `tickIndex`, `domainStateJson` — domain JSON'u **string alan olarak gomulu**): SaveEnvelope.cs:444-463.
- `SaveSlotId` (Kind+Index degeri esitlikli struct; dosya govdesi `manual_N|auto|quick`): SaveSlotId.cs:469-524. `SaveSlotKind` enum: SaveSlotKind.cs:528-533.
- `SaveSlotMetadata` (metadataVersion, envelopeVersion, schemaVersion, slotKind/Index, label, sceneName, playtimeMinutes, savedAtUtcIso, thumbnailPath): SaveSlotMetadata.cs:539-552. Sidecar dosyasi `<stem>.meta.json` (FileSaveRepository.cs:38).

## LLD - Fonksiyon Haritasi

- `WorldSaveMapper.ToData(WorldState world) -> WorldSaveData` — WorldSaveMapper.cs:33-131; dunyanin tamamini tek nesne baslaticisiyla DTO'ya dokerek null dunyada tipli istisna firlatir (satir 39). `recipeWorkOrders`'i **bilerek yazmaz** (satir 72-74).
- `WorldSaveMapper.ToWorld(WorldSaveData data, WorldState seedWorld) -> WorldState` — WorldSaveMapper.cs:133-276; sema kapisi (141-144), store'larin geri kurulumu, parallel-array zip'leri, F31 varsayilanlari (250-258).
- `WorldSaveMapper.CurrentSchemaVersion = 1` — WorldSaveMapper.cs:31.
- `ActorSaveMapper.ToSave/FromSave` — ActorSaveMapper.cs:21-72,74-152; `hasMood` yoksa `mood>0` sezgiseli, `priority<=0 -> JobPriority.Disabled` sentineli (109-113).
- `DungeonSaveMapper.ToRoomData/ToDoorData/ToSpawnData/ToLayout/ToRoomStates/ToDoorStates` — DungeonSaveMapper.cs:181-258.
- `ItemSaveMapper.ToData/ToItem/ToPickup` — ItemSaveMapper.cs:308-348; slot cozumu `slotCode` oncelikli, legacy int fallback (325-327).
- `SpellCooldownSaveMapper.ToData/ToState` — SpellCooldownSaveMapper.cs:365-401; ordinal siralama ile deterministik JSON (371).
- `ShieldBuffSaveMapper.ToData/ToState` — ShieldBuffSaveMapper.cs:418-457.
- `WorldSaveMapper.ToJobBoard(JobRequestSaveData[]) -> JobBoard` — WorldSaveMapper.Process.cs:221-259; once insertion-order ekleme, sonra `claimSequence` sirasiyla `TryRestoreClaim`; kurtarilamayan claim `InvalidOperationException` firlatir (255).
- `WorldSaveMapper.ToWorldProfile` — WorldSaveMapper.Narrative.cs:227-241; `targetPopulation<=0` ise null doner (229).
- `JsonSliceSaveService.SaveToJson/LoadFromJson/BindWorld` — JsonSliceSaveService.cs:84-97,99-127,39-44; tek Unity JSON temas noktasi.
- `WorldSaveRehydration.ToRecipeWorkOrderData/ToRecipeWorkOrder/CreateSeedWorld` — WorldSaveRehydration.cs:28-42,49-70,78-80; Simulation-bagimli yeniden-kurulum Data asmdef'inden buraya tasindi (dosya basi not, 10-20).
- `FileSaveRepository.Save/TryLoad/TryLoadPayload/TryLoadMetadata/Delete/ListAll` — FileSaveRepository.cs:42-59,69-90,94-104,131-140; `WriteAtomically` (179-188), `Quarantine` (106-119), regex tabanli metadata parse/yeniden-insa (214-236,302-365).
- `SaveEnvelopeCodec.Encode/TryDecode` — SaveEnvelopeCodec.cs:8-28,30-49; legacy ciplak `SaveData` cozumu (79-103).
- `EmberSaveService.Save/SaveSlot/SaveAuto/TryAutosaveActiveScene/SaveInternal` — EmberSaveService.cs:52-73,75-88,90-93,112-126,128-202.
- `EmberSaveService.Load/LoadSlot/LoadJson/ApplyDomainRestore/RestorePosition` — EmberSaveService.Load.cs:12-28,30-49,66-130,149-187,189-209.
- `EmberSaveService.ResolveLatestSaveJson/TryResolveLatestSave/PreparePendingLoad` — EmberSaveService.Resolve.cs:57-72,94-117,44-47.
- `DomainSimulationAdapter.ExportStateJson/RestoreStateJson` — DomainSimulationAdapter.Save.cs:24-29,31-65.

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` yalnizca tick-zamani yazicilari bildirir (FieldOwnershipRegistry.cs:12-52); kayit/yukleme sistemi orada **bildirilmemis, bant-disi bir butun-dunya yazicisidir** — bu bilincli bir bosluktur ama registry'nin "tek-yazici" iddiasinin kapsam disi kapisidir.

- **Okur (kayit aninda)**: `WorldState`'in serilestirilen tum alanlari — `Actor.*` (Position, Needs, Vitals, Mood, ScheduleState, Memory...), `World.GuardPursuits`, `World.Stockpiles`, `World.Rumors`, `World.SiteUnrest`, `World.Critters`, tum store'lar (Actors/Items/Sites/Factions/Prices/TradeRoutes/Caravans/Worksites/Jobs/Soils/Plants/Quests/Events/ToolCallTrace/LlmProposalLog/NpcSeeds), oyuncu ilerleme alanlari (WorldSaveMapper.cs:40-130). Ayrica PlayerRig transform'u ve `EmberTickDriver.TickIndex` (EmberSaveService.cs:130,141).
- **Yazar (yukleme aninda)**: yukaridaki alanlarin TAMAMI — `WorldSaveMapper.ToWorld` seed dunyaya alan alan (WorldSaveMapper.cs:146-274), ardindan `WorldState.CopyFrom` canli dunyaya toptan (DomainSimulationAdapter.Save.cs:47). Yani registry'deki `Actor.Position`, `Actor.Needs`, `Actor.Vitals`, `World.GuardPursuits`, `World.Stockpiles`, `World.Rumors`, `World.SiteUnrest` dahil her mutable alan load'da yeniden yazilir.
- **Disk/PlayerPrefs**: `<persistentDataPath>/saves/{quick|auto|manual_N}.json` + `.meta.json` + `.corrupt[.N]` (FileSaveRepository.cs:34-38); PlayerPrefs `ember.save.v1` (legacy mirror) ve `ember.save.lastslot` (EmberSaveService.cs:16-17,191-193).
- **Yazmadigi**: `WorldState.Overland` — kayida hic girmez; load'da canli oturumdakiyle korunur (DomainSimulationAdapter.Save.cs:41-49).

## LLD - Urettigi/Tukettigi Olaylar

- **WorldEventKind uretmez** — sistem `WorldEventLog`'u yalnizca serilestirir/geri kurar (WorldSaveMapper.Narrative.cs:159-194); kayit-yukleme eylemi icin log'a olay eklenmez.
- **Log taglari**: `[EmberSave]` (quick-save/load, slot islemleri: EmberSaveService.cs:62,66,70,77,81,85,97,103,107,123; EmberSaveService.Load.cs:17,21,25,32,42,46), `[Autosave]` (5 dk kadans: RuntimeAutosaveView.cs:28-30).
- **UI statusleri** (fade text): "Saved.", "Save partial: domain export failed.", "Save failed: could not write save slot.", "Load failed: save corrupt.", "Load partial: domain restore failed.", "Load partial: domain restore unavailable.", "Loaded.", "No save found.", "Load failed: scene not in build." (EmberSaveService.cs:194-200, EmberSaveService.Load.cs:37,59,70-75,104,123-128, EmberSaveService.cs:216-221).

## Testler

Hepsi Assets/Tests/EditMode/Save/ altinda:
- `WorldSaveMapperGoldenRoundtripTests.cs` — **golden roundtrip**: temsili dunya `ToData -> ToWorld -> ToData`, iki DTO yansima ile ALAN ALAN karsilastirilir; alan dusuren mapper burada sonsuza dek yakalanir (satir 12-50).
- `SaveLoadDigestRoundtripTests.cs` — **F4-DoD**: bir tam oyun gunu ilerlemis dunya roundtrip sonrasi `WorldStateDigest` ile bayt-ozdes olmali (satir 11-16); F22 dunya-gorevleri de digest'e dahil (28-46).
- `SaveSchemaVersionTests.cs` — sema kontrati: ToData guncel versiyonu damgalar, v0 legacy=v1 kabul, gelecek versiyon reddedilir (satir 8-13).
- `StoreRoundTripTests.cs` — kanonik store koklerinin (Actor/Item/Site/Faction/EventLog) roundtrip'i.
- `FileSaveRepositoryTests.cs` — atomik yazim, karantina, metadata sidecar davranislari.
- `SaveEnvelopeCodecTests.cs` — zarf encode/decode + legacy gecisi.
- `JsonSliceSaveServiceTests.cs` — Unity JSON koprusu; `EmberSaveServiceResolutionTests.cs` — slot cozum oncelik sirasi.
- Alan-ozel roundtrip'ler: `ActorSaveMapperTests.cs`, `ActorNeedsRoundTripTests.cs`, `JobAssignmentRoundTripTests.cs` (claim/queue), `PlantSeasonRoundTripTests.cs`, `RecipeWorksiteRoundTripTests.cs`, `SpellCooldownSaveMapperTests.cs`, `ShieldBuffSaveMapperTests.cs`, `WorldSaveMapperTradeFieldsTests.cs`, `SaveSlotBrowserStateTests.cs`, `SaveSlotRepositoryTests.cs`.

## Bilinen Borclar + Kacak Kapilari

1. **Overland persist edilmiyor** — kayit overland haritasini hic tasimiyor; ayni oturumda load canli overland'i koruyarak kurtariyor (DomainSimulationAdapter.Save.cs:41-49) ama **soguk yuklemede** (process yeniden basladiktan sonra Continue) overland seed fabrikasindan gelir; ayni seed'le deterministik olarak ayni haritayi uretip uretmedigi bu dosyalardan **dogrulanmadi**.
2. **`recipeWorkOrders` asimetrisi** — `WorldSaveMapper.ToData` bu alani bilerek yazmaz (WorldSaveMapper.cs:72-74); yalnizca `JsonSliceSaveService.SaveToJson` doldurur (JsonSliceSaveService.cs:92). `ToData`'yi dogrudan cagiran her yol aktif is emirlerini kaybeder; golden test iki tarafta da null gordugu icin bunu YAKALAMAZ.
3. **Legacy adli-rol yuzeyi hala cift yaziliyor** — `player/talker/...` + `*RoomId` alanlari deprecated WorldState gorunumlerini aynalar; Phase 13 temizligine kadar tasinacak (WorldSaveData.cs:9-16, WorldSaveMapper.cs:47-52,58-62; load fallback'i WorldSaveMapper.cs:160-171).
4. **Elle JSON kazima** — `FileSaveRepository` metadata yeniden-insasi regex ile payload'dan alan ceker (FileSaveRepository.cs:214-236,334-365; sablon regex'ler 22-23 derlenmis ama `ToString()+Format` ile yeniden yorumlaniyor, derleme faydasi bosa gidiyor); `EmberSaveService.ExtractLong` `IndexOf` taramasi yapar (EmberSaveService.cs:248-260). JSON bicimi degisirse sessizce 0/bos doner — (a)-(g) hata ailesi eslemesi **dogrulanmadi** (taksonomi tanimina repo icinde rastlanmadi).
5. **Lossy-by-design normalizasyon** — load filtreleri bazi satirlari sessizce dusurur: `siteId==0` veya bos tag fiyatlar (WorldSaveMapper.Economy.cs:36-37), `count<=0` stockpile girisleri (70-71), `a==b`/0 faction-rep satirlari (WorldSaveMapper.World.cs:166-167), id/home/faction=0 npcSeed'ler (WorldSaveMapper.Narrative.cs:363-369), `remainingTicks<=0` cooldown/shield girisleri (SpellCooldownSaveMapper.cs:394-395, ShieldBuffSaveMapper.cs:448-451), `questId<=0` görev satirlari (WorldSaveMapper.Quest.cs:40-41,81). Boyle satir iceren bir kayit bayt-ozdes roundtrip yapamaz; digest testleri bu bolgeye girmiyor.
6. **`PlayerKnownSpellIds` bos-liste tuzagi** — load'da bos dizi seed dunyanin listesine geri duser (WorldSaveMapper.cs:259-261); mesru olarak sifir buyu bilen bir oyuncu, seed varsayilanlarini geri kazanabilir (hasMood/hasHomeAnchor'daki gibi bir "presence" bayragi yok).
7. **JobBoard claim restore tek satirla tum yuklemeyi dusurur** — `TryRestoreClaim` basarisizsa `InvalidOperationException` (WorldSaveMapper.Process.cs:254-255); ust katman yakalayip "Load failed." gosterir ama kismi kurtarma yolu yok.
8. **PlayerRig yoksa sessiz no-op kayit** — `SaveInternal` erken doner, status bile gostermez (EmberSaveService.cs:130-131); rig'siz bir sahnede F5/autosave hicbir sey yapmaz.
9. **Statik global durum** — `_pendingLoad` (EmberSaveService.Resolve.cs:14) ve autosave cabasindaki `s_nextSaveAt` (RuntimeAutosaveView.cs:17) statiktir; test izolasyonu ve domain-reload davranisi acisindan kacak kapisi.
10. **Best-effort catch-all'lar** — sidecar yazimi (FileSaveRepository.cs:57-58), karantina (117-118), silme (190-197,199-212) ve legacy gecis geri-yazimi (EmberSaveService.Load.cs:90-93) istisnalari sessizce yutar; tasarim geregi ama teshis izini de yutar.
11. **`int` daralmalari** — `roomSeed/currentRoomId/dungeonStartRoomId` DTO'da `long` tutulup load'da `(int)` cast edilir (WorldSaveMapper.cs:148-149); 32-bit tasan deger sessizce sarmalanir (pratikte seed int uretiliyor, teorik borc).
12. **Cift kayit yolu mirasi** — `Save(int slot,...)` legacy yolu sidecar yazmaz (FileSaveRepository.cs:42-46); `ListAll` bu slotlar icin metadata'yi regex ile payload'dan yeniden-insa etmek zorunda kalir (142-158,214-236). PlayerPrefs `ember.save.v1` mirror'i hala her kayitta guncellenir (EmberSaveService.cs:192) — EMB-011 "dosya tek gercek kaynak" hedefinin tamamlanmamis kuyrugu.

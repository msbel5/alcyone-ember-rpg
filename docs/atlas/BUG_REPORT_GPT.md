# Atlas mantiksal hata ve bug ozeti

Bu rapor, atlas dokumanlarindaki borc/kacak-kapi kisimlari ve systems.json alanlarindan sentezlendi.

## En kritik ilk 8

### B01 - WorldViewProjector.ReplaceActorViews derlemeyi kiriyor (critical)
- Belirti: Commit edilmis govdede tanimsiz worksiteViews/eventLogHud ve readonly alan atamasi var; Unity compile kirmasi beklenir.
- Neden onemli: Gorsel katman ayaga kalkmazsa diger sim hatalarini gorme sansi da yok.
- Fix yonu: Atama satirlarini kurucuya geri tasi; sadece silme, cunku _worksiteViews null kalip Project icinde NRE uretir.
- Kanit: 13-actor-views debt #1: CS0103/CS0191 riski ve dogru duzeltme notu.

### B02 - Soguk load sonrasi overland haritasi geri gelmiyor (critical)
- Belirti: Save Overland yazmiyor; ayni oturum load canli map i koruyor ama taze process load da _world.Overland null kalabilir.
- Neden onemli: Travel, M-map, HUD tile ve GeneratedWorld fallback sessizce bozuluyor.
- Fix yonu: WorldProfile.Seed ile load sonrasi overland ve GeneratedWorld uretimini tek yoldan yeniden kur; save restore testini taze process senaryosuyla pinle.
- Kanit: 10-save-load debt #1 ve 11-worldgen-overland debt #1-2.

### B03 - FieldOwnershipRegistry ve lint listesi bayat; guvenlik hissi sahte (critical)
- Belirti: econ.trade/world.history/world.shortage gibi hayalet id ler lintten geciyor; gercek id ler world.caravans/econ.shortage_response/world.runtime_history.
- Neden onemli: Tek-yazar sozlesmesi sistemin ana emniyet kemeri; kemer sahte listeye bagli.
- Fix yonu: knownIds listesini elle tutma; DefaultTickSystems.Ordered ve gercek registry den turet. Ters lint de ekle: beyan var ama gercek writer yoksa fail.
- Kanit: 01 debt #1, 05 debt #1-2, 06 debt #1, 07 debt #1-2.

### B04 - Kritik state alanlari ownership disinda kaliyor (high)
- Belirti: World.Time, Actor.Mood, World.Plants, World.NpcMemory, World.CompanionIds, World.Factions ve adapter yazimlari registry tarafindan korunmuyor.
- Neden onemli: Birden fazla sistem ayni alana yazdiginda CI yakalamayacak; buglar nondeterministic gorunebilir.
- Fix yonu: Registry kapsamini tick + adapter + save/load boundary olarak genislet; en azindan bypass yazimlari whitelistli raporla.
- Kanit: 01 debt #3, 02 debt #5, 04 debt #6, 06 debt #1, 07 debt #3, 09 debt #4, 14 debt #12, 19 debt #5.

### B05 - Kitlik -> ekim -> hasat dongusu kapanmiyor (high)
- Belirti: Planting job ilan ediliyor ama RecipeId 5101 kayitsiz; PlantingSystem.TryPlant uretimde cagrilmiyor.
- Neden onemli: Ciftci tarlaya yuruyup is bitmis sayilabilir ama yeni plant dogmaz; ekonomi HarvestStep self-replant ile yasiyor.
- Fix yonu: Recipe 5101 kaydini ekle veya planting job completion i PlantingSystem.TryPlant e bagla; sessiz KeyNotFound catch ini log/telemetry yap.
- Kanit: 05 debt #4 ve 06 debt #2.

### B06 - NPC uretimi oyuncu envanterini kullaniyor (high)
- Belirti: econ.jobs recipe girdilerini world.PlayerInventory den alip ciktiyi yine oyuncuya yaziyor.
- Neden onemli: Koy ekonomisi ile oyuncu cantasi birbirine karisiyor; stockpile fiyat/kitlik sinyali anlamsizlasiyor.
- Fix yonu: Job recipe IO sunu site stockpile/worksite store a tasi; player inventory sadece player action tarafinda kalsin.
- Kanit: 05-economy debt #5.

### B07 - Kervanlar tekrar eden rota degil tek atimlik teslimat (high)
- Belirti: Unload payload i sifirliyor ve Idle kervan yeniden yola cikmiyor; CadenceDays dokstringi fiilen yol suresi.
- Neden onemli: Site ekonomileri arasindaki mal akisi bir turdan sonra duruyor.
- Fix yonu: Idle -> Loading -> EnRoute state transition i ve cadenced departure reset i ekle; TradeRouteDef anlamini testle pinle.
- Kanit: 05-economy debt #3.

### B08 - Stok sifira dustugunde fiyat sinyali donar (medium)
- Belirti: PriceStepSystem sadece Entries uzerinde donuyor; zero count entries filtreleniyor.
- Neden onemli: Kitlik fiyat etkisi en cok gerekli anda kaybolur.
- Fix yonu: Known items set i veya last-known item ledger i uzerinden zero stock itemlari da fiyatla.
- Kanit: 05-economy debt #6.

## Tum bulgular
- **B01 [critical] WorldViewProjector.ReplaceActorViews derlemeyi kiriyor** - Commit edilmis govdede tanimsiz worksiteViews/eventLogHud ve readonly alan atamasi var; Unity compile kirmasi beklenir.
- **B02 [critical] Soguk load sonrasi overland haritasi geri gelmiyor** - Save Overland yazmiyor; ayni oturum load canli map i koruyor ama taze process load da _world.Overland null kalabilir.
- **B03 [critical] FieldOwnershipRegistry ve lint listesi bayat; guvenlik hissi sahte** - econ.trade/world.history/world.shortage gibi hayalet id ler lintten geciyor; gercek id ler world.caravans/econ.shortage_response/world.runtime_history.
- **B04 [high] Kritik state alanlari ownership disinda kaliyor** - World.Time, Actor.Mood, World.Plants, World.NpcMemory, World.CompanionIds, World.Factions ve adapter yazimlari registry tarafindan korunmuyor.
- **B05 [high] Kitlik -> ekim -> hasat dongusu kapanmiyor** - Planting job ilan ediliyor ama RecipeId 5101 kayitsiz; PlantingSystem.TryPlant uretimde cagrilmiyor.
- **B06 [high] NPC uretimi oyuncu envanterini kullaniyor** - econ.jobs recipe girdilerini world.PlayerInventory den alip ciktiyi yine oyuncuya yaziyor.
- **B07 [high] Kervanlar tekrar eden rota degil tek atimlik teslimat** - Unload payload i sifirliyor ve Idle kervan yeniden yola cikmiyor; CadenceDays dokstringi fiilen yol suresi.
- **B08 [medium] Stok sifira dustugunde fiyat sinyali donar** - PriceStepSystem sadece Entries uzerinde donuyor; zero count entries filtreleniyor.
- **B09 [high] Aclik/susuzluk herkes icin artiyor ama herkes beslenmiyor** - Player/Enemy ihtiyaçlari rampalaniyor ama consumption disinda; Guard consumption a dahil ama schedule food spot a goturmuyor.
- **B10 [medium] Schedule movement pathfinding degil isaret-adimi** - NPC tek Chebyshev adimi atiyor; zemin, bina, su, carpismalar yok.
- **B11 [high] Generated contract sistemi oyuncuya bagli degil** - Accept/TurnIn yalniz proof driver tarafindan cagriliyor; journal sadece gosteriyor.
- **B12 [high] Magic tasarimi ile canli hat ayrismis** - SpellResolver ve roll/fizzle sistemi test-only; live cast legacy SpellEffectCode ve %100 success.
- **B13 [medium] Editor Play Mode da yerel LLM fiilen kapali, fallback null** - USE_LLAMASHARP editor define yok; production setup fallback null; LLM cagrilari bos cevapla donebilir.
- **B14 [high] Dialog state machine yeni UI da canli refresh olmuyor** - Topic panel ctor da bir kez kuruluyor; consumed/followup balonlari konusma kapanip acilmadan degismiyor.
- **B15 [critical] OptionsScreen legacy Input API ile InputSystem-only projede exception uretiyor** - OptionsScreen.Update Input.GetKeyDown okuyor; proje activeInputHandler=InputSystem-only.
- **B16 [medium] TTS state machine statik ve testsiz; backend tek hatada kalici kapanir** - SpeechDirector offset/purge yamalari semptom bazli; Piper/SAPI _dead bayragi oturum boyunca geri acilmaz.
- **B17 [high] Placeholder asset taze cache olarak damgalanabiliyor** - Model yokken 8x8 gri placeholder Success=true yazilip promptmeta ile fresh kabul ediliyor.
- **B18 [medium] Forge cache anahtari boyut/negatif/steps bilgisini disliyor** - Prompt|Style|Seed hashleniyor; W/H/negative/steps degisimi cache i gecersiz kilmiyor.
- **B19 [high] recipeWorkOrders ToData yolunda yazilmiyor** - WorldSaveMapper.ToData alanı bilerek yazmiyor; JsonSliceSaveService.SaveToJson dolduruyor.
- **B20 [medium] Bos PlayerKnownSpellIds save i seed defaultlarina geri donduruyor** - Load bos dizi gorunce seed dunyanin bilinen buyu listesine fallback yapiyor.
- **B21 [medium] WorldEventLog sinirsiz buyuyor; okuyucular sessiz cap ile veri atliyor** - Log save e tamamen yaziliyor; Rumor/Echo 256 cap ten fazlasini sessizce sona atliyor.
- **B22 [high] Predation/crime unrest ilk siteye yaziliyor** - FallbackSite(world) store daki ilk siteyi kullanir; cok sitede tum predation unrest oraya akar.
- **B23 [medium] Presentation sim kararini tahmin ediyor, simden okumuyor** - Lunch icon/activity logic hardcoded 12-14 ve hunger sabitleriyle yasiyor; schedule ise utility selector.
- **B24 [medium] Actor view renk/flip/transform icin birden fazla yazar var** - ActorView ve feedback/walk anim ayni renderer/transform u farkli cadence larda suruyor.
- **B25 [high] Adapter DTO siniri delik; public WorldState sim mutasyonuna acik** - IWorldViewReadModel Domain tipleri sizdiriyor; DomainSimulationAdapter.World public.
- **B26 [medium] Fallback harness yesil olsa da Unity compile/scenes/assets dogrulanmiyor** - Pure-C# fallback Unity PlayMode/asset/meta/plugin gercekligini validate etmiyor.
- **B27 [medium] Hava sim e bagli degil; kar yagarken ekin buyuyebilir** - Plant growth isSnowing:false hardcode ile calisiyor; presentation weather ayri.
- **B28 [medium] Ayni seed icin iki Overland Generate overload u farkli harita uretiyor** - Generate(uint, params) duz WorldgenService yoluna gidiyor; gezegen pipeline dan farkli.
- **B29 [high] Birden cok sistemde canli hat ile test-only/uyuyan hat ayrismis** - ShortageDetector, TradeService, PlantingSystem, HarvestSystem, SpellResolver, dialog_defs, generated quest accept/turn-in ve narration services test-only veya dormant.
- **B30 [medium] Iki keybind evreni var: remap edilebilir action ve sabit KeyCode switch** - C/I/M/J/K/R/T/H gibi ekran tuslari HandleScreenInput ta sabit; Options haritasi stale.

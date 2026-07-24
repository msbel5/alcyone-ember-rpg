# 17-forge-assets

> Kapsam: gorsel forge — ONNX tabanli SDXL-Turbo / SD1.5-LCM difuzyon boru hatlari,
> portre/sprite/doku uretimi, cache-anahtar semalari, tek-figur kapisi (single-figure gate)
> ve cift-figur/"twin" sorununun dogum yerleri.
> Her iddia `file:line` ile kanitlidir; emin olunamayanlar "dogrulanmadi" olarak isaretlendi.

## HLD - Ne ve Neden

Forge, oyunun TUM 2D sanatini (NPC billboard'lari, portreler, esya/buyu/UI ikonlari,
zemin/duvar/cati dokulari, kapi/pencere fikstürleri, logo, splash) yerel makinede, bulut
olmadan, ONNX Runtime uzerinden uretir. Iki difuzyon cekirdegi vardir: CUDA varsa
SDXL-Turbo, yoksa CPU'da SD1.5-LCM; ikisi de yoksa deterministik 8x8 gri "placeholder"
moduna duser ve oyun ASLA sert-hata vermez (OnnxAssetForge.cs:119-131, EmberForgeFactory.cs:71-74).
Oyuncuya gorunen etki: her dunya tohumu kendi gorsel kimligini uretir — ayni seed ayni
portreyi verir (OnnxAssetForgeTests.cs:53), forge kapaliyken bile bir kurt "kurt gibi okunur"
cunku calisma-zamani piksel-maske siluetleri devrededir (BestiaryBillboardSpriteFactory.cs:5-11).
Felsefe uc katmandir: (1) uretim deterministik ve seed'li, (2) her uretilen dosya
`.promptmeta` provenans damgasiyla cache'lenir ve prompt/versiyon degisince bayatlar
(GeneratedAssetProvenance.cs:15-52), (3) GPU'yu korumak icin TUM uretimler tek-isci
kuyrugundan gecer — asla iki agir uretim ayni anda kosamaz (GenerationManager.cs:21-27).
Sistemin en cok kanayan yarasi "twin" sorunudur: difuzyon modeli tek kisi istenince iki
kisi/karakter-sayfasi uretebilir; savunma dort katmanlidir (prompt sozlugu, CFG negatif
yonlendirme, U2Net matte + bagli-bilesen kapisi, yeniden-tohumlu 8 deneme) ve asagida
tek tek haritalanmistir.

## HLD - Akis

### A. Boot kablolamasi (sahne acilisi, bir kez)
1. `ForgeBootstrap.Awake` model kokunu cozer: `persistentDataPath/Models` varsa o, yoksa
   `streamingAssetsPath/Models` (ForgeBootstrap.cs:78-83). `--ember-forge-off` bayragi
   model kokunu var-olmayan klasore yonlendirip forge'u cache-only moda dusurur
   (ForgeBootstrap.cs:35-46 — proof-run izolasyon kacak kapisi).
2. `EmberForgeFactory.BuildForge` secimi yapar: CUDA dll'leri (`onnxruntime.dll` +
   `onnxruntime_providers_cuda.dll` + `onnxruntime_providers_shared.dll`) bulunursa PATH'e
   klasor eklenir ve SDXL-Turbo CUDA ile warmup denenir; basarisizsa SD1.5-LCM CPU'ya
   duser; o da yoksa SD15 ornegi yine de dondurulur (placeholder degrade)
   (EmberForgeFactory.cs:26-77, 112-128).
3. Secilen forge iki dekorator ile sarilir: `SerializedAssetForge` (tek-isci kuyrugu + RAM
   guard) ve `SingleFigureRefiningAssetForge` (NPC-only tek-figur kapisi); sonuc
   `ForgeLocator.Register` ile global locator'a yazilir (ForgeBootstrap.cs:50-63).
4. Paralel olarak `ModelBootstrap.BootstrapRoutine` StreamingAssets'teki
   `Models/manifest.json`'u okur, `persistentDataPath/Models` altinda SHA256 dogrulamasi
   yapar, eksikleri HuggingFace'ten indirir, SHA tutmayani siler ve locator'daki forge'u
   AYNI sarma zinciriyle yeniden baglar (ModelBootstrap.cs:57-102, 129-172, 184-217).
   Indirme surerken oyun placeholder modda ilerler (ModelBootstrap.cs:12-22 tasarim notu).

### B. Tek uretim istegi (calisma zamani, istek basina)
1. Cagiran `ForgeLocator.AssetForge.GenerateAsync(request)` der. Istek once
   `SingleFigureRefiningAssetForge`'a gelir; `SingleFigureSpritePolicies.NpcOnly` yalniz
   `npc_`/`creature_` on-ekli RequestId'leri kapiya sokar — portreler BILEREK disarida
   (SingleFigureSpritePolicies.cs:8-17, gerekce: bust oranlari kapiyi yanlis tetikleyip
   300s timeout'a suruklüyordu, ayni dosya 10-14 yorumu).
2. Kapi-disi istek dogrudan `SerializedAssetForge` → `GenerationManager.GenerateAsync`'e
   iner: istek oncelik kuyruguna girer (backpressure 4096), TEK worker dongusu sirayla
   ceker (GenerationManager.cs:27-35, 88-129). Worker, VRAM tahmini yetersizse >512
   istekleri sessizce 512x512'ye kucultur (RAM guard, GenerationManager.cs:224-250).
3. `OnnxAssetForge.GenerateAsync`: lazy init → model dosyalari yoksa placeholder PNG
   (Success=true, IsPlaceholder=true), sert hata varsa yapisal Failure, degilse secili
   pipeline'a `RunAsync` (OnnxAssetForge.cs:99-158, 170-197).
4. `SdxlTurboPipeline.Run`: CLIP BPE tokenizasyon → iki text-encoder'in SONDAN-IKINCI
   hidden state'i (`hidden_states.11`/`hidden_states.31` — son katman verilirse "rainbow"
   cop cikar, SdxlTurboPipeline.cs:29-36) → `Subject==Npc` ise CFG guidance 3 acilir ve
   anti-twin negatif ("two heads, multiple people, ...") gercekten kosullamaya girer;
   diger kindlar hizli guidance-0 yolunda kalir (SdxlTurboPipeline.cs:111-133) → Euler
   cizelgesi (istek `Steps`; CFG varsa min 4) → UNet dongusu → VAE decode → PNG
   (SdxlTurboPipeline.cs:132-159). Boyutlar 64..1024 arasina klampe edilir ve 8'e
   yuvarlanir (SdxlTurboPipeline.cs:339-344).
5. Kapi-ici (npc_/creature_) istekte `SingleFigureSpriteRefiner.GenerateAsync` dongusu:
   uret → PNG coz → `OnnxImageMatteService` (U2Net) ile alfa matte →
   `ConnectedComponentSingleFigureGate.Evaluate` → kabulse kirp+alfa uygula+dondur;
   redse seed+attempt ile YENIDEN uret, en fazla `MaxAttempts=8`
   (SingleFigureSpriteRefiner.cs:31-76, 138-155; RuntimeSingleFigureForgeFactory.cs:12-18).
   Hicbiri gecmezse: `AllowBestEffortFallback` false oldugundan calisma zamaninda
   `single_figure_gate_rejected_all_attempts` hatasi doner (SingleFigureSpriteRefiner.cs:66-75,
   RuntimeSingleFigureForgeFactory.cs:32-33).

### C. Cekirdek asset uretimi (loading ekrani + editor menusu)
1. Calisma zamani: `BootBootstrap` locator'daki forge'u bekler (~3s), `CoreAssetManifest.CreateDefault`
   girdilerini `VisibleGenerationFlow`'a verir (BootBootstrap.cs:27-67 — akisin ic detayi
   bu taramada okunmadi, dogrulanmadi).
2. Editor: `Ember/Forge/Regenerate Core Assets` menuleri `CoreAssetRegenerationRunner.Start/RunBlocking`
   cagirir (RegenerateCoreAssetsMenu.cs:8-37). Akis: bayat girdileri `_backup_<stamp>`'e tasi
   (provenans-tazelik kontrolu ile, CoreAssetRegenerationRunner.cs:225-241) → tara →
   `VisibleGenerationPipeline.RunAsync` ile uret → sprite/texture import ayarlari uygula →
   `GeneratedAssetDatabase`'e kayit upsert + `RebuildStableIds` (CoreAssetRegenerationRunner.cs:89-130).
3. `VisibleGenerationPipeline` her girdi icin: prompt'u `StaticPromptCatalog`'dan coz,
   promptHash hesapla, girdi-basina timeout'lu uret, basariliysa PNG'yi `ExpectedPath`'e yaz
   ve `.promptmeta` damgala (VisibleGenerationPipeline.cs:53-108, 136-142).
4. Batchmode girisleri: `RegenerateNpcBillboardsBatch` (forceRebuild=true) vb.
   (RegenerateCoreAssetsMenu.cs:32-36); batchmode'da async continuation tuzagina karsi
   `SyncTaskBridge.Run` kullanilir (CoreAssetRegenerationRunner.cs:152-157).

### D. Tuketim (frame kadansi, sprite cozunumu)
1. `EmberGeneratedActorSpawner` billboard sprite'ini uc adimda cozer: (1) `GeneratedAssetRuntimeDatabase`
   kaydindan `GeneratedCoreSpriteLoader.TryLoadRelativeSprite` ("library"), (2) bestiary
   rolleri icin `BestiaryBillboardSpriteFactory.For` piksel-maske silueti, (3) notr gri
   placeholder (EmberGeneratedActorSpawner.cs:373-400). Boy, ture gore olceklenir
   (EmberGeneratedActorSpawner.cs:181-184; BestiaryBillboardSpriteFactory.cs:31-42).
2. UI panelleri `EmberWorldHost.GetSprite` → `GeneratedCoreSpriteLoader.TryLoadByName/TryLoadPortrait`
   ile ayni PNG'leri timestamp-cache'li okur (EmberWorldHost.Sprites.cs:8-18,
   GeneratedCoreSpriteLoader.cs:21-63).

### E. Karakter yaratma portresi (oyuncu etkilesimi basina)
1. `GeneratePortrait` once deterministik swatch'i ANINDA basar (bos kutu asla yok), sonra
   LLM JSON'unu arka planda yukseltir (CharacterCreationController.Portrait.cs:52-90).
2. `StartPortraitForgeUpgrade` forge hazirsa `AssetKind.Portrait` sablonundan
   `DefaultImageGenSpecFactory.Create` ile spec kurar (512x512, 1 step, guidance 0 —
   IImageGenSpecFactory.cs:17-33, ImageGenKindTemplate.cs:26-33), istegi
   `cc_portrait_<generation>_<seed>` id'siyle atar ve sonucu hem panele hem
   `PlayerPortraitHandoff`'a yayinlar (CharacterCreationController.Portrait.cs:181-230,
   256-290, 232-254). Reroll'lar generation serisiyle yarislari keser
   (CharacterCreationController.Portrait.cs:55-56, 300-302).

## LLD - Veri Modeli

- **AssetGenerationRequest** (AssetGenerationRequest.cs:96-153): `RequestId` (ayni zamanda
  policy anahtari: "npc_*" on-eki kapiyi acar), `Subject:AssetSubjectKind` (Npc/Item/Region/Splash,
  6-12; Npc CFG'yi tetikler), `Style/Genre/MoodKeyword`, `PromptHash`, `Width/Height`,
  `Seed:uint`, `Prompt/NegativePrompt`, `TimeoutSeconds` (varsayilan 300), `ModelHint`
  (tasinir ama uretimde OKUNMAZ — bkz. Borclar #1), `Steps` (varsayilan 1, kind-config,
  68-70).
- **AssetGenerationResult** (AssetGenerationResult.cs:5-47): `ImageBytes` (savunmaci clone,
  20), `Success`, `FailureReason`, `IsPlaceholder` (EMB-042 provenans bayragi, 35-41 —
  Success=true + placeholder olabilir).
- **ImageGenSpec / ImageGenKindTemplate / AssetKind** (ImageGenSpec.cs:5-44,
  ImageGenKindTemplate.cs:8-118, AssetKind.cs:3-12): kind-basina W/H/steps/guidance/
  scaffold/negatif. NpcBillboard 512x768, Portrait/Item/vd. 512x512, hepsi TurboSteps=1 +
  guidance 0 (ImageGenKindTemplate.cs:10-11, 18-25). `AssetKind.ToSubjectKind` Portrait'i
  da Npc'ye esler → portreler de pipeline'da CFG alir (AssetKind.cs:16-22).
- **OnnxModelBundle** (OnnxAssetForge.cs:220-319): text_encoder(/2)/unet/vae/tokenizer yol
  demeti; eksik dizilimlerde kardes-klasor turetme ve Unity 6.3 Mono `Path` istisna
  toleransi (295-318).
- **MatteResult / SingleFigureGateResult / PixelBounds** (MatteResult.cs, SingleFigureGateResult.cs,
  PixelBounds.cs — alan detayi bu taramada acilmadi, dogrulanmadi): matte alfa maskesi ve
  kapi karari (bilesen sayisi, ust-govde sayisi, kenar temasi, ana-bilesen maskesi;
  kullanim: ConnectedComponentSingleFigureGate.cs:49-57).
- **SingleFigureRefinementOptions** (SingleFigureRefinementOptions.cs:5-27): MaxAttempts,
  AlphaThreshold, CropPadding, MinimumLargeComponentPixels, DominantComponentRatio,
  AllowBestEffortFallback, RejectFireArtifacts, FireArtifactMinPixels.
- **ManifestEntry** (ManifestEntry.cs:5-35): Id, Category, ExpectedPath, StaticPromptKey,
  W/H, RequiresGeneration, TimeoutSeconds, ModelHint. `CoreAssetManifest.CreateDefault`
  tum katalogu kurar: splash 768x512 sd15-lcm (CoreAssetManifest.cs:29), NPC sprite'lari
  896x1344 / 1200s timeout / sd15-lcm (11-13, 88-106), ikon/kapi/pencere sdxl-turbo 512
  (30-34 yorumu: Turbo 512-NATIF, 1024'te tek zar 40'lik grid'e donusuyordu).
- **StaticPromptCatalog** (StaticPromptCatalog.cs:8-186): anahtar→prompt sozlugu.
  Kritik sozlesme: NPC sprite POZITIFI tamamen "kurucu" dildedir — "no X" ifadeleri
  guidance-0 Turbo'da pozitif token olarak ENJEKTE edilip coklu-figur sayfalari uretiyordu;
  bastirma sozlugu `EmberGenerationNegative`'e tasindi (12-15, 20, 167-174). Bestiary
  prompt gövdeleri `WorldBestiaryCatalog.All`'dan gelir (161-165).
- **GeneratedAssetRecord anahtari** (CoreAssetLibraryRecordBuilder.cs:54-85): kind +
  promptHash + styleVersion(`real-images-v4`) + seed + variantIndex (+ npc icin
  archetype="core-npc", role=slug). Kayit `GeneratedAssetDatabase.asset`'e upsert edilir
  (CoreAssetRegenerationRunner.cs:276-295).
- **Cache anahtarlari — UC AYRI SEMA:**
  1. Editor NPC-portre cache'i: `Sha256(Prompt + "|" + Style + "|" + Seed)` →
     `persistentDataPath/forge-cache/<hash>.png` (PromptComposers.cs:47-51,
     AssetForgeCache.cs:14-19). W/H/negatif/steps anahtarda YOK.
  2. Cekirdek asset provenansi: `sha256(Version|Id|Category|WxH|ModelHint|prompt|EmberGenerationNegative)`
     `.promptmeta` dosyasinda `version=` satiriyla birlikte (GeneratedAssetProvenance.cs:79-90,
     54-68); seed ayrica girmez cunku `StableSeed(entry.Id)` deterministiktir
     (VisibleGenerationPipeline.cs:144-151).
  3. CC portresi: `Sha256(prompt|negative|WxH|seed)` yalniz `request.PromptHash` alanina
     konur, diskte cache'lenmez (CharacterCreationController.Portrait.cs:261-270).

## LLD - Fonksiyon Haritasi

- `EmberForgeFactory.BuildForge(modelRoot, out onnx, out failureReason): IAssetForge`
  (EmberForgeFactory.cs:26) — CUDA-probe→SDXL→SD15 seciminin TEK otoritesi (EMB-041
  split-brain düzeltmesi, 9-14 yorumu).
- `OnnxAssetForge.GenerateAsync(request, ct): Task<AssetGenerationResult>`
  (OnnxAssetForge.cs:99) — placeholder/sert-hata/pipeline uc kollu dagitim.
- `OnnxAssetForge.TryWarmup(out error): bool` (OnnxAssetForge.cs:89) — init'i zorlar,
  placeholder olmayan calisirlik dondurur.
- `SdxlTurboPipeline.Run(request, ct): byte[]` (SdxlTurboPipeline.cs:89) — tokenize→encode→
  (CFG)→Euler→VAE→PNG; `BuildEulerSchedule` (240) diffusers EulerDiscrete esdegeri.
- `Sd15LcmPipeline.RunAsync` (Sd15LcmPipeline.cs:52) — CPU fallback boru hatti (ic detay
  bu taramada okunmadi; negatif-prompt tuketip tuketmedigi dogrulanmadi).
- `GenerationManager.GenerateAsync(request, priority, ct)` (GenerationManager.cs:38) —
  kuyruk + tek worker (88) + `ApplyResourceGuard` VRAM kucultmesi (224).
- `SingleFigureSpriteRefiner.GenerateAsync` (SingleFigureSpriteRefiner.cs:31) — deneme
  dongusu; `RefineAttempt` (78) matte+kapi+kirpma; `HasFireArtifact` (97) turuncu-alev
  bileseni avi; `ChooseBest` (118) en-buyuk-ana-bilesen fallback secimi.
- `ConnectedComponentSingleFigureGate.Evaluate(matte): SingleFigureGateResult`
  (ConnectedComponentSingleFigureGate.cs:35) — kabul kosulu: buyuk-bilesen<=1 VE
  ust-govde-bileseni<=1 VE kafa-serit-kosusu<=1 VE dominans>=0.7 VE ikincil-oran<0.12 VE
  aspect<=0.78 (43-48; sabitler 17-24); `CountTopBandRuns` (60) matte'nin BIRLESTIRDIGI
  yan-yana ciftleri kafa bandindaki kosu sayisiyla yakalar; `CountUpperBodyLargeComponents` (97).
- `OnnxImageMatteService.Matte(rgba, w, h): MatteResult` (OnnxImageMatteService.cs:47) —
  U2Net 320x320 infer + min-max normalize + bilinear geri-olcek; `EnsureModelOnDisk` (114)
  u2net.onnx'i (176MB) GitHub'dan lazily indirir.
- `RuntimeSingleFigureForgeFactory.WrapNpcBillboards(serializedForge, modelRoot, log)`
  (RuntimeSingleFigureForgeFactory.cs:20) — calisma-zamani kapi sabitlerinin tek yeri (12-18).
- `PromptComposers.NpcPortrait/RegionEstablishingShot/ItemIcon(...)` (PromptComposers.cs:16,26,36)
  — dunya-kayitlarindan istek kurar; `MixSeed` (59) profil-seed⊕id karisimi; `CacheKey` (47).
- `GeneratedAssetProvenance.IsFresh/Write/ComputePromptHash` (GeneratedAssetProvenance.cs:17,54,79)
  — `.promptmeta` damga sozlesmesi; `Version="real-images-v4"` (15) topyekun gecersizlestirme dugmesi.
- `VisibleGenerationPipeline.RunAsync(entries, ct)` (VisibleGenerationPipeline.cs:53) —
  girdi-basina timeout'lu uretim + yaz + damgala; `ToRequest` (120) sabit
  Style=DarkFantasyGrim/Genre=Survival/mood="ember" kullanir.
- `CoreAssetRegenerationRunner.RunAsync/RunBlockingCore` (CoreAssetRegenerationRunner.cs:89,132)
  — yedekle→tara→uret→import→DB-upsert orkestrasyonu; `CreateForge` (340) editor kapisini
  `GeneratedAssetPipelineSettings`'ten kurar.
- `CharacterCreationController.BuildPortraitRequest(kind, spec, generation)`
  (CharacterCreationController.Portrait.cs:256) — cc_portrait istek fabrikasi.
- `BestiaryBillboardSpriteFactory.For(spriteRole)/TargetHeightFor` (BestiaryBillboardSpriteFactory.cs:16,31)
  — forge-OFF siluet ailesi, statik sprite cache'li (14).
- `GeneratedCoreSpriteLoader.TryLoadByName/TryLoadPortrait/TryLoadRelativeSprite`
  (GeneratedCoreSpriteLoader.cs:21,26,42) — PNG→Sprite + LastWriteTime cache (50-63).
- `ForgeMenu.GenerateWorldAssets` (ForgeMenu.cs:18) — editor menusunden canli dunyanin
  NPC portrelerini toplu uretip cache'ler.

## LLD - Yazdigi/Okudugu Alanlar

Bu sistem Simulation cekirdegine yazmaz; sahiplik dili soyle:

**Yazar:**
- `ForgeLocator.AssetForge/NativeLlm/LlmRouter/Embedding` (statik servis kayitlari;
  yazarlar: ForgeBootstrap.cs:63, ModelBootstrap.cs:202, ForgeLocator.cs:25-39).
- Disk: `persistentDataPath/Models/**` (ModelBootstrap indirme, ModelBootstrap.cs:129-172),
  `persistentDataPath/Models/matte/u2net.onnx(+manifest)` (OnnxImageMatteService.cs:114-138),
  `persistentDataPath/forge-cache/<sha>.png` (AssetForgeCache.cs:37-44),
  `Assets/Generated/Core/*.png` + `*.png.promptmeta` (VisibleGenerationPipeline.cs:136-142),
  `Assets/Generated/_backup_<stamp>/**` (CoreAssetRegenerationRunner.cs:100-102, 243-255),
  `Logs/generation-failures.json` (CoreAssetRegenerationRunner.cs:186),
  `Assets/Resources/GeneratedAssets/GeneratedAssetDatabase.asset` kayitlari
  (CoreAssetRegenerationRunner.cs:110-118).
- Surec ortami: `PATH` env degiskenine CUDA klasoru ON-EKLENIR (global yan etki,
  EmberForgeFactory.cs:121-128).
- `WorldState.NpcSeeds` — `ForgeMenu.GenerateWorldAssets` canli dunyada
  `NpcSeedRecord.PortraitAssetPath`'i cache anahtariyla degistirip listeyi GERI YAZAR
  (ForgeMenu.cs:41-42, 68-72, 132-142). Editor menusunden sim-alan mutasyonu: sahiplik
  ihlali olarak isaretli (bkz. Borclar #8).
- `PlayerPortraitHandoff` statik handoff'una Publish/PublishPng
  (CharacterCreationController.Portrait.cs:148, 247).

**Okur:**
- `EmberRuntimeOptionsProvider.Current.CharacterCreation.PortraitForgeWaitFrames/PortraitForgeTimeoutSeconds`
  (CharacterCreationController.Portrait.cs:184-186, 213).
- `WorldBestiaryCatalog.All` (StaticPromptCatalog.cs:161-165, CoreAssetManifest.cs:101-106),
  `NpcRole` enum'u (CoreAssetManifest.cs:90-97), `NpcSeedRecord`/`RegionRecord`/`ItemRecord`/
  `WorldProfile` (PromptComposers.cs:16-44).
- `SystemInfo.graphicsMemorySize/systemMemorySize` (UnityResourceProbe.cs:8-15,
  CoreAssetRegenerationRunner.cs:346), `Application.persistentDataPath/streamingAssetsPath/dataPath`,
  komut satiri arg'lari (ForgeBootstrap.cs:40).
- `GeneratedAssetPipelineSettings.asset` (editor kapi/import ayarlari,
  CoreAssetRegenerationRunner.cs:23, 334-338; alanlar GeneratedAssetPipelineSettings.cs:9-52).

## LLD - Urettigi/Tukettigi Olaylar

- **WorldEventKind: YOK.** Forge hicbir sim-olayi uretmez/tuketmez (grep dogrulamasi:
  `WorldEventKind.*(Forge|Asset|Portrait)` sifir sonuc).
- C# olaylari: `VisibleGenerationPipeline.EntryStarted/EntryProgress/EntryThumbnail/
  EntrySucceeded/EntryFailed/Completed` (VisibleGenerationPipeline.cs:46-51) — loading
  ekrani ve regen runner'in ilerleme goruntusu bunlara abone olur
  (CoreAssetRegenerationRunner.cs:193-213).
- Log etiketleri (bu sistemin gozlem dili):
  - `[Forge]` — forge-off bildirimi (ForgeBootstrap.cs:44); `Forge Connectivity:` durum
    satiri (ForgeBootstrap.cs:94-96).
  - `ModelBootstrap:` — manifest/indirme/SHA/rebind akisi (ModelBootstrap.cs:62-172, 199-206).
  - `[SingleFigureGate]` — deneme-basina components/upper/fire/accepted dokumu, fallback ve
    toplu-red satirlari (SingleFigureSpriteRefiner.cs:54, 59, 69, 74).
  - `[CoreAssetRegen]` — secim/yedek/kuyruk/ozet + girdi-basina started/succeeded/failed
    (CoreAssetRegenerationRunner.cs:107, 120-126, 374-377).
  - `[portrait]` — CC portre yasam dongusu (CharacterCreationController.Portrait.cs:77, 125-127,
    197-198, 228, 306-314).
  - `[start]` — boot uretim satiri, model=hint dahil (BootBootstrap.cs:89).
  - Sprite cozunum kaynagi: "library" / "bestiary-silhouette" / "missing" satirlari
    (EmberGeneratedActorSpawner.cs:380, 383, 397 — `LogSpriteResolution`).
- Kalici hata defteri: `GenerationFailureLog` → `Logs/generation-failures.json`
  (VisibleGenerationPipeline.cs:108-113, CoreAssetRegenerationRunner.cs:186).

## Testler

- `Assets/Tests/EditMode/Forge/SingleFigureSpriteRefiningAssetForgeTests.cs` — kapinin
  twin-sozlesmesini pinler: iki buyuk blob reddi (:29), tek dominant kabul (:43), ilk
  kabulde durma (:58), best-effort'un ANCAK acikca istenince calismasi (:102), alev
  artefakti reddi (:123), ust-govdede bitisik cift-figur reddi (:172), tek govdeyi
  paylasan iki kafa reddi (:191), policy'nin doku/ikon haric tutmasi (:163).
- `Assets/Tests/EditMode/Forge/OnnxAssetForgeTests.cs` — placeholder dusumu (:15),
  seed determinizmi (:53, :70), gercek modelde boyut eslesmesi (:89).
- `Assets/Tests/EditMode/Forge/SdxlPipelineCompileTests.cs` — uretilen PNG basligi (:13).
- `Assets/Tests/EditMode/Forge/DiffusionPipelineClampTests.cs` — boyut klampe sozlesmesi.
- `Assets/Tests/EditMode/Forge/GenerationManagerTests.cs` + `GenerationManagerResourceTests.cs`
  + `AssetForgeQueueTests.cs` + `SerializedAssetForgeTests.cs` — tek-isci/oncelik/RAM-guard.
- `Assets/Tests/EditMode/Forge/AssetForgeCacheTests.cs` — ayni istek hit / farkli seed miss (:14).
- `Assets/Tests/EditMode/Forge/PromptComposerTests.cs` — prompt+hash determinizmi (:13, :27, :40).
- `Assets/Tests/EditMode/Forge/TypedImageGenContractTests.cs` — her AssetKind icin sablon (:9),
  portre scaffold'inin tek-merkezli olmasi (:35).
- `Assets/Tests/EditMode/Forge/ModelManifestTests.cs`, `GeometricPromptTests.cs`,
  `EmbeddingClientTests.cs`.
- `Assets/Tests/EditMode/Generation/StaticPromptCatalogTests.cs` — stil zarfi (:11), ikonlarin
  Turbo-512 kalmasi (:51), NPC portre promptlarinin "tam bir kisi" zorlamasi (:66), her
  NpcRole'un prompt'u olmasi (:92).
- `Assets/Tests/EditMode/Generation/GeneratedAssetProvenanceTests.cs` — damga yok/eslesen/
  degisen-prompt durumlari (:14, :33, :53).
- `Assets/Tests/EditMode/Generation/CoreAssetManifestTests.cs`, `CoreAssetRegenerationScopeTests.cs`,
  `AssetManifestScannerTests.cs`, `VisibleGenerationPipelineTests.cs`, `VisibleGenerationFlowTests.cs`,
  `GenerationFailureLogTests.cs`.
- `Assets/Tests/EditMode/Bestiary/BestiaryCatalogTests.cs` — siluet/prompt rol butunlugu.
- Portre yolu: `Assets/Tests/EditMode/CharacterCreation/PortraitPromptBuilderTests.cs`,
  `DefaultNpcPortraitJsonProviderTests.cs`, `Assets/Tests/EditMode/Presentation/PlayerPortraitHandoffTests.cs`,
  `Assets/Tests/EditMode/Ui/DialogPortraitKeyTests.cs`; PlayMode:
  `Assets/Tests/PlayMode/CharacterCreation/CharacterCreationFlowTest.cs`,
  `Assets/Tests/PlayMode/Playability/CharacterCreationPlayableSceneTest.cs`.

## Bilinen Borclar + Kacak Kapilari

Hata ailesi harfleri `docs/SYSTEMS_ATLAS.md:52-60` (a)-(g) siniflamasina gore.

1. **ModelHint olu uctur — sinif (a).** Manifest her girdiye model yazar ("sd15-lcm" /
   "sdxl-turbo", CoreAssetManifest.cs:29-73), istek tasir (AssetGenerationRequest.cs:50),
   ama HICBIR tuketici hint'e gore pipeline secmez — secim boot'ta SURECE-GLOBAL tek
   forge'dur (EmberForgeFactory.cs:26-77; grep: ModelHint yalniz pass-through/log/provenans-hash).
   CUDA'li makinede "sd15-lcm" isteyen splash/env girdileri de SDXL-Turbo'da uretilir;
   provenans hash'i hint'i ICERDIGI icin (GeneratedAssetProvenance.cs:86) damga "sd15
   uretti" diye yalan soyleyebilir.
2. **CUDA-probe split-brain'i hala yasiyor — sinif (f).** EMB-041 iki bootstrap'i
   birlestirdi (EmberForgeFactory.cs:9-14) ama `ForgeMenu` kendi kopyasini tutuyor ve
   FARKLI kural kosuyor: sabit `Plugins/x86_64/cuda` klasoru + fazladan
   `onnxruntime_providers_tensorrt.dll` sarti (ForgeMenu.cs:123-129 vs
   EmberForgeFactory.cs:112-118, 130-148). Ayni makinede menu CPU'ya duserken oyun
   CUDA'da kosabilir.
3. **Placeholder "fresh" damgalanir — sinif (g).** `VisibleGenerationPipeline` Success=true
   olan HER sonucu diske yazar ve damgalar; IsPlaceholder yalniz SAYILIR
   (VisibleGenerationPipeline.cs:75-83, 136-142). Model dosyalari eksikken kosan bir regen,
   8x8 gri placeholder'i `Assets/Generated/Core/npc_guard.png` olarak yazar, `.promptmeta`
   damgasi taze oldugundan sonraki kosular "cached" sayar (GeneratedAssetProvenance.cs:44-51)
   — versiyon-bump ya da forceRebuild'e kadar oyun gri kare tasir.
4. **RAM-guard sessiz kucultmesi provenansa yansimaz — sinif (b) benzeri.** VRAM dusukse
   896x1344 NPC istegi 512x512'ye indirgenir (GenerationManager.cs:224-250) ama promptHash
   ve `.promptmeta` manifestin ILAN EDILEN boyutuyla hesaplanir (GeneratedAssetProvenance.cs:85)
   — dusuk-VRAM makinede uretilen kucuk sprite, tam-boy sanilarak sonsuza dek cache'de kalir.
5. **forge-cache anahtari eksik-boyutlu — sinif (a).** `PromptComposers.CacheKey` yalniz
   Prompt|Style|Seed hash'ler (PromptComposers.cs:47-51); W/H/negatif/steps degisimi cache'i
   GECERSIZLESTIRMEZ. Uc ayri anahtar semasi (forge-cache, promptmeta, cc_portrait) ayni
   kavrami uc dilde konusur — tek otorite yok.
6. **UNet oturumu her adimda diskten yeniden yuklenir — verim borcu.** `RunUnet` her
   cagrida `CreateSession(_models.Unet)` acar-kapar (SdxlTurboPipeline.cs:190-204; ayni
   desen encoder/VAE icin 164-218). CFG'li NPC uretimi: >=4 adim x 2 UNet kosusu = 8 oturum
   acilisi; kapi 8 deneme yaparsa 64 UNet yuklemesi. Oturum cache'i yok.
7. **U2Net calisma zamaninda senkron 176MB indirme — sinif (e)/guvenlik.** Ilk matte
   cagrisi worker icinde `HttpClient.GetByteArrayAsync(...).GetAwaiter().GetResult()` ile
   GitHub'dan model ceker; dogrulama SADECE bayt-uzunlugu, SHA yok
   (OnnxImageMatteService.cs:114-128, 17-19). Cevrimdisi makinede ilk npc_ uretimi
   exception'la `matte_refine_failed` yoluna duser (SingleFigureSpriteRefiner.cs:48-56 —
   o durumda refined edilmemis HAM goruntu doner: twin filtresi devre disi).
8. **Editor menusu canli sim'i mutasyonlar — sahiplik ihlali.** `ForgeMenu.GenerateWorldAssets`
   `world.NpcSeeds`'i yeniden yazar (ForgeMenu.cs:72) ve log'u var olmayan
   `docs/forge-samples/sample.png` dosyasina isaret eder — klasor yaratilir ama PNG hicbir
   yerde yazilmaz (ForgeMenu.cs:74-77; dosya yazimi yok, dogrulandi).
9. **Kapi tum denemeleri reddedince calisma zamani ELI BOS kalir.** Runtime sarici
   `allowBestEffortFallback:false` kurar (RuntimeSingleFigureForgeFactory.cs:32-33) →
   8 tam difuzyon sonrasi `single_figure_gate_rejected_all_attempts` (SingleFigureSpriteRefiner.cs:74-75).
   SD15-CPU'da bu, dakikalarca is yakip yine siluete dusmek demek. Portreler bu yuzden
   kapidan tamamen cikarildi (SingleFigureSpritePolicies.cs:10-14) — yani portre twin'leri
   bugun SADECE prompt sozlugu + CFG ile tutuluyor, piksel dogrulamasi yok.
10. **Kapi esikleri iki yerde — sinif (f).** Ayni tuning sabitleri hem
    `RuntimeSingleFigureForgeFactory` (12-18) hem `GeneratedAssetPipelineSettings`
    varsayilanlari (GeneratedAssetPipelineSettings.cs:28-36) olarak yasar; editor ile
    runtime kapisi sessizce ayrisabilir (bugun degerler esit: 160/1024/0.7/0.42/400/8).
11. **`IsCudaProviderFailure` asiri genis.** Mesajinda "provider" GECEN her istisna CUDA
    hatasi sayilir (OnnxSessionFactory.cs:137-144) → alakasiz hatalar "sdxl_requires_cuda"
    etiketiyle maskelenebilir.
12. **Kacak kapilari (bilincli):** `--ember-forge-off` (ForgeBootstrap.cs:35-46, proof
    kosulari GPU'yu forge'dan izole eder); `GeneratedAssetProvenance.Version` string'i
    (=`real-images-v4`) topyekun cache-purge dugmesi (GeneratedAssetProvenance.cs:12-15);
    `RegenerateNpcBillboardsBatch(forceRebuild:true)` damgalari yok sayar
    (RegenerateCoreAssetsMenu.cs:34, CoreAssetRegenerationRunner.cs:145);
    `USE_ONNX_RUNTIME` define'i yokken tum pipeline placeholder'a katlanir
    (SdxlTurboPipeline.cs:50-52, OnnxAssetForge.cs:186-190).
13. **Twin sorununun dogum haritasi (ozet):** (1) SDXL egitim-verisi "character sheet /
    turnaround" korelasyonlari + guidance-0'da negatif kosullamanin OLMAMASI
    (StaticPromptCatalog.cs:12-15); (2) tarihsel bug: "no twin/no second person" bastirma
    sozcuklerinin POZITIF prompt'a gomulmesi — tokenler enjekte olup twin URETIYORDU
    (StaticPromptCatalog.cs:169-174, duzeltildi); (3) natif-512 modelde super-natif kanvas
    (1024 ikonlarda grid/tile, CoreAssetManifest.cs:30-34; 896x1344 NPC kanvasi ayni riski
    tasir, 11-12); (4) matte iki yan-yana figuru TEK blob'a birlestirince bilesen sayaci
    kor kalir — aspect-orani ve kafa-serit sayaci bunun icin eklendi
    (ConnectedComponentSingleFigureGate.cs:19-24, 60-95). Savunma bugun: CFG guidance 3
    (yalniz Subject=Npc, SdxlTurboPipeline.cs:111-133) + kapi + 8 reseed denemesi; kapi
    disindaki kindlar (ikon, doku, portre) yalnizca prompt disiplinine guvenir.

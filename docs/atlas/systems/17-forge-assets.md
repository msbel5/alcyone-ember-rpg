# 17-forge-assets

## HLD - Ne ve Neden

Görsel forge, dünyayı boyayan üretim boru hattıdır: NPC portresi, canavar billboard'u, item ikonu, bölge estetiği - hepsi bu sistemden geçer. Neden: oyun ilk açılışta 100+ el-boyalı asset ile gelmesin; SDXL/SD1.5 modelleri kendi assetlerini üretsin, disk cache'e yazsın, sonraki oturumlarda saniyeler içinde yüklensin. Neden native ONNX: Unity Sentis kaldırıldı (W23), pure C# / `Microsoft.ML.OnnxRuntime` + `LlamaSharp` stack'i bir seçim değil, tek geçerli yol - CUDA varsa SDXL-Turbo (GPU), yoksa SD1.5-LCM (CPU), o da yoksa deterministic 8x8 gri placeholder. Neden decorator zinciri (`Serialized -> SingleFigureRefining -> Onnx`): eşzamanlı üretim GPU'yu tek başına OOM eder, iki başlı NPC ürerse gate reject edip yeniden dener, cache hit varsa hiç modele gitmez. W33-C'de iki eski yara kapatıldı: **B17** - placeholder'lar canonical stamp almıyor (üretici model gelince otomatik retry); **B18** - cache key W/H/negative/steps içeriyor artık, "v2\|" schema prefix ile eski hit'ler temiz miss'e dönüyor.

## HLD - Akış

1. `ForgeBootstrap.Awake()` (Presentation) - komut satırında `--ember-forge-off` varsa modelRoot'u geçersize çevirir (proof-run izolasyonu); değilse `Application.persistentDataPath/Models`.
2. `EmberForgeFactory.BuildForge(modelRoot, out onnx, out failureReason)` - CUDA artefaktları varsa PATH'e ekle + SDXL-Turbo dene; warmup fail ise SD1.5-LCM'ye düş; o da fail olursa yine SD15 instance'ını tut (deterministic placeholder içeride).
3. Decorator sarımları (bootstrap katmanında):
   - `SerializedAssetForge(realForge, UnityResourceProbe)` - `GenerationManager` içinden tek worker döngüsü + RAM guard.
   - `RuntimeSingleFigureForgeFactory.WrapNpcBillboards(serialized, modelRoot)` - `SingleFigureRefiningAssetForge` NPC/creature request'lerini `SingleFigureSpriteRefiner` ile 8 denemeye kadar matte + connected-component gate'inden geçirir; portreler (bust) `SingleFigureSpritePolicies.NpcOnly` ile GATE DIŞINDA kalır (yoksa 300s SD15-CPU timeout).
4. `ForgeLocator.Register(...)` - static locator; `ModelBootstrap` manifest download bitince `SetAssetForge` ile REBIND eder (LLM router'ı ezmez).
5. Bir istek geldiğinde (`VisibleGenerationPipeline` veya runtime NPC spawner): `PromptComposers.CacheKey(req)` -> `AssetForgeCache.PathFor(req)` -> disk hit varsa direkt PNG; miss ise `SingleFigureRefiningAssetForge.GenerateAsync` -> gate reject ederse Reseed edip retry.
6. Real path: `OnnxAssetForge` -> flavor'a göre `SdxlTurboPipeline.Run` (CLIP tokenize -> iki text encoder penultimate hidden state -> Euler multi-step UNet, NPC'ler için CFG=3 + neg-prompt, diğerleri CFG=0 -> VAE decode -> `OnnxPngEncoder.EncodeRgba`) veya `Sd15LcmPipeline.Run`.
7. Sonuç `AssetGenerationResult`: **`IsPlaceholder=true`** ise `VisibleGenerationPipeline.Write(entry, bytes, isPlaceholder)` PNG'yi yazar ama `.promptmeta` STAMP ATMAZ - scanner bir sonraki taramada "stale_missing_provenance" görüp gerçek üretime yeniden gönderir.
8. Fallback: BestiaryBillboardSpriteFactory - forge KAPALI/asset yoksa deterministik 12-20 satırlık char mask'ten wolf/spider/skeleton/ghost/bandit sprite'ı üretir; spawner önce library, sonra silhouette, sonra neutral dener.

## LLD - Veri Modeli

- `IAssetForge` - kontrat, `Task<AssetGenerationResult> GenerateAsync(...)` + `bool IsAvailable()` - `Assets/Scripts/Domain/Forge/IAssetForge.cs:6`.
- `AssetGenerationRequest` - subject, style, prompt, W/H, seed, negative, steps, modelHint, timeoutSeconds - `Assets/Scripts/Domain/Forge/AssetGenerationRequest.cs:14`.
- `AssetGenerationResult` - `RequestId`, `ImageBytes`, `MimeType`, `GenerationTimeMs`, `Success`, `FailureReason`, **`IsPlaceholder`** (EMB-042) - `Assets/Scripts/Domain/Forge/AssetGenerationResult.cs:5`.
- `AssetKind` enum + `AssetKindExtensions.ToSubjectKind` (NpcBillboard, Portrait, Item, Furniture, Logo, InventoryIcon, EnvironmentProp) - `Assets/Scripts/Domain/Forge/AssetKind.cs:3`.
- `ImageGenKindTemplate` - kind bazlı W/H/Steps/Guidance/Prompt scaffold sözlüğü, `TurboSteps=1`, `TurboGuidance=0` - `Assets/Scripts/Domain/Forge/ImageGenKindTemplate.cs:6`.
- `SingleFigureRefinementOptions` - MaxAttempts, AlphaThreshold, CropPadding, MinimumLargeComponentPixels, DominantComponentRatio, AllowBestEffortFallback, RejectFireArtifacts - `Assets/Scripts/Domain/Forge/SingleFigureRefinementOptions.cs:5`.
- `SingleFigureGateResult` - IsSingleFigure, PixelBounds, ComponentCount, UpperBodyComponentCount, TouchesFrameEdge, MainComponentMask - `Assets/Scripts/Domain/Forge/SingleFigureGateResult.cs:3`.
- `OnnxModelBundle` - TextEncoder/TextEncoder2/Unet/VaeDecoder/TokenizerVocab/Merges/Config path'leri, `RequiredFilesExist(flavor)` - `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs:210`.
- `PipelineResult` - Total/Succeeded/Failed/**Placeholders** (EMB-042 provenance) - `Assets/Scripts/Simulation/Generation/VisibleGenerationPipeline.cs:11`.

## LLD - Fonksiyon Haritası

- `EmberForgeFactory.BuildForge(string modelRoot, out OnnxAssetForge selectedOnnx, out string failureReason) : IAssetForge` - CUDA -> SDXL -> SD15 seçim sırası - `Assets/Scripts/Presentation/Ember/Forge/EmberForgeFactory.cs:61`.
- `EmberForgeFactory.BuildSdxlForge(modelRoot, providerPreference) : OnnxAssetForge` - HuggingFace subdir layout ile 7 dosya path'i - `Assets/Scripts/Presentation/Ember/Forge/EmberForgeFactory.cs:114`.
- `EmberForgeFactory.HasCudaRuntimeArtifacts() : bool` - onnxruntime.dll + CUDA provider DLL'lerini `Assets/Plugins/x86_64/[cuda/]` altında arar - `Assets/Scripts/Presentation/Ember/Forge/EmberForgeFactory.cs:147`.
- `OnnxAssetForge.GenerateAsync(request, ct) : Task<AssetGenerationResult>` - lazy init, placeholder / hard-failure / real pipeline dallanması - `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs:98`.
- `OnnxAssetForge.PlaceholderPng(request) : byte[]` - deterministic 8x8 gri (seed'in low byte'ı) - `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs:161`.
- `SdxlTurboPipeline.Run(request, ct) : byte[]` - CLIP tokenize -> penultimate hidden states (encoder1 `hidden_states.11`, encoder2 `hidden_states.31`) -> Euler CFG döngüsü -> VAE decode -> RGBA PNG - `Assets/Scripts/Simulation/Forge/SdxlTurboPipeline.cs:89`.
- `SdxlTurboPipeline.BuildEulerSchedule(steps, out timesteps, out sigmas) : void` - SDXL scaled_linear beta schedule, `steps=1` tek-sigma_max eşdeğeri (backward safe) - `Assets/Scripts/Simulation/Forge/SdxlTurboPipeline.cs:244`.
- `SdxlTurboPipeline.ProbeAvailability(out error) : bool` - tokenizer + 4 model session probe'u, CUDA gerekli - `Assets/Scripts/Simulation/Forge/SdxlTurboPipeline.cs:55`.
- `PromptComposers.CacheKey(request) : string` - **B18 v2** SHA256(`v2|prompt|style|seed|WxH|negative|steps`) - `Assets/Scripts/Simulation/Forge/PromptComposers.cs:53`.
- `PromptComposers.NpcPortrait/RegionEstablishingShot/ItemIcon(...) : AssetGenerationRequest` - Brom/Torment scaffold + PortraitNegative - `Assets/Scripts/Simulation/Forge/PromptComposers.cs:17`.
- `AssetForgeCache.PathFor(request) : string` - `<persistent>/forge-cache/<sha>.png` - `Assets/Scripts/Simulation/Forge/AssetForgeCache.cs:20`.
- `AssetForgeCache.TryRead / Write(...)` - disk cache IO, sadece `Success && bytes.Length>0` yazar - `Assets/Scripts/Simulation/Forge/AssetForgeCache.cs:25`.
- `SingleFigureRefiningAssetForge.GenerateAsync(...) : Task<AssetGenerationResult>` - Refiner decorator'una delegate - `Assets/Scripts/Presentation/Ember/Forge/SingleFigureRefiningAssetForge.cs:24`.
- `SingleFigureSpriteRefiner.GenerateAsync(...)` - `_shouldRefine` false ise passthrough; true ise MaxAttempts (=8) turda Reseed + matte + gate kontrolü - `Assets/Scripts/Simulation/Forge/SingleFigureSpriteRefiner.cs:32`.
- `SingleFigureSpritePolicies.NpcOnly(request) : bool` - `id.StartsWith("npc_"|"creature_")` - portreler (bust) DIŞARIDA - `Assets/Scripts/Presentation/Ember/Forge/SingleFigureSpritePolicies.cs:8`.
- `RuntimeSingleFigureForgeFactory.WrapNpcBillboards(serializedForge, modelRoot, log) : SingleFigureRefiningAssetForge` - `AlphaThreshold=160`, `MinimumLargeComponentPixels=1024`, `DominantComponentRatio=0.7f`, `UpperBodyFraction=0.42f`, `MaxAttempts=8` - `Assets/Scripts/Presentation/Ember/Forge/RuntimeSingleFigureForgeFactory.cs:15`.
- `ForgeBootstrap.Awake()` - LlmClient + `EmberForgeFactory.BuildForge` + `SerializedAssetForge` + refiner sarımı + locator register - `Assets/Scripts/Presentation/Ember/Forge/ForgeBootstrap.cs:32`.
- `ModelBootstrap.ApplyLocator()` - manifest download bitince yeniden `EmberForgeFactory.BuildForge` + `SerializedAssetForge` sarımı + `ForgeLocator.SetAssetForge` - `Assets/Scripts/Presentation/Ember/Forge/ModelBootstrap.cs:145`.
- `ForgeLocator.SetAssetForge(forge) : void` - eski forge'u Dispose eder, LLM/router'ı ezmez - `Assets/Scripts/Presentation/Ember/Forge/ForgeLocator.cs:19`.
- `OnnxImageMatteService.Matte(rgba, w, h) : MatteResult` - U2-Net 320x320 (HuggingFace/rembg release), mean/std normalize, mask sample bilinear - `Assets/Scripts/Simulation/Forge/OnnxImageMatteService.cs:52`.
- `OnnxImageMatteService.EnsureModelOnDisk() : string` - lazy download + expected-size check + manifest yazımı - `Assets/Scripts/Simulation/Forge/OnnxImageMatteService.cs:118`.
- `OnnxPngEncoder.EncodeRgba/EncodeRgb/EncodeGrayscale(...) : byte[]` - store-block zlib PNG (dış PNG dependency yok) - `Assets/Scripts/Simulation/Forge/OnnxPngEncoder.cs:19`.
- `OnnxPngDecoder.Decode(bytes) : SpriteImageFrame` - 8-bit, colorType 0/2/6, filter 0 destekli minimal decoder - `Assets/Scripts/Simulation/Forge/OnnxPngDecoder.cs:11`.
- `GenerationManager.GenerateAsync(request, priority, ct)` - tek worker döngüsü + priority queue, aynı request için ikinci workItem'ı de-dupe eder - `Assets/Scripts/Simulation/Forge/GenerationManager.cs:38`.
- `VisibleGenerationPipeline.Write(entry, bytes, isPlaceholder) : void` - **B17**: `RequiresGeneration && !isPlaceholder` olduğunda `.promptmeta` yazar - `Assets/Scripts/Simulation/Generation/VisibleGenerationPipeline.cs:136`.
- `BestiaryBillboardSpriteFactory.For(spriteRole) : Sprite` - `monster_wolf|spider|skeleton|ghost|bandit` char-mask fallback sprite - `Assets/Scripts/Presentation/Ember/Views/BestiaryBillboardSpriteFactory.cs:15`.
- `BestiaryBillboardSpriteFactory.TargetHeightFor(role, def) : float` - wolf=1.2, spider=0.9, skeleton=2.0, ghost=2.2, bandit=2.0 - `Assets/Scripts/Presentation/Ember/Views/BestiaryBillboardSpriteFactory.cs:31`.

## LLD - Yazdığı/Okuduğu Alanlar

Forge alt-sistemi WorldState mutation'ı yapmaz (FieldOwnershipRegistry tabanındaki actor/tile alanlarına yazmaz). IO alanı disk + static locator + Unity engine:

- **Yazar**: `<persistentDataPath>/forge-cache/<sha>.png` (`AssetForgeCache.Write`), `<persistentDataPath>/Models/**/*.onnx` (`ModelBootstrap.DownloadEntryRoutine`), `<persistentDataPath>/Models/matte/u2net.onnx` + `u2net.manifest.json` (`OnnxImageMatteService.EnsureModelOnDisk/WriteManifest`), manifest entry çıktısı `<projectRoot>/<entry.ExpectedPath>.png` (+ `.promptmeta` sadece REAL üretim için, `VisibleGenerationPipeline.Write`).
- **Okur**: `<streamingAssetsPath>/Models/manifest.json` (UWR), `AssetForgeCache.TryRead` disk hit'i, `Application.dataPath/Plugins/x86_64[/cuda]/onnxruntime*.dll`.
- **Static locator alanları**: `ForgeLocator.AssetForge`, `NativeLlm`, `LlmRouter`, `Embedding` - üç setter (`Register`, `SetAssetForge`, `SetEmbeddingClient`) + `Clear`.
- **Environment**: `EmberForgeFactory.AddCudaProviderDirectoryToPath` `PATH` başına CUDA klasörünü ekler (idempotent).

## LLD - Ürettiği/Tükettiği Olaylar

- **Ürettiği (`VisibleGenerationPipeline` C# event'leri)**: `EntryStarted`, `EntryProgress`, `EntryThumbnail`, `EntrySucceeded`, `EntryFailed`, `Completed(PipelineResult)`.
- **Ürettiği log satırları**: `[Forge] ONNX generation DISABLED for this run (--ember-forge-off)`; `Forge Connectivity: ComfyUI=..., Ollama=..., NativeLLM=..., OnnxForge=..., Failure='...'`; `ModelBootstrap: asset forge rebound -> {Flavor} (cuda={UsesCuda})`.
- **Tükettiği**: `AssetGenerationRequest` (VisibleGenerationPipeline `ManifestEntry` -> `ToRequest`, karakter yaratımdan `PortraitPromptBuilder`, worldgen NPC seeder'lardan `PromptComposers.NpcPortrait`), `IResourceProbe` sinyalleri (RAM guard).
- **CancellationToken zinciri**: caller CT + `VisibleGenerationPipeline` içindeki linked `CancelAfter(entry.TimeoutSeconds)` timeout kaynağı.

## Testler

- `Assets/Tests/EditMode/Forge/PromptComposerTests.cs` - **B18** `CacheKey_CoversEveryPixelChangingField_AndStaysDeterministic` (W/H/negative/steps her biri key'i değiştirmeli, `ItemAndRegion_ComposersProduceStableCacheKeys` 64-hex SHA sabitliği).
- `Assets/Tests/EditMode/Forge/OnnxAssetForgeTests.cs` - `OnnxAssetForge_NoModels_FallsBackToPlaceholder`, deterministic-seed placeholder pin.
- `Assets/Tests/EditMode/Forge/SdxlPipelineCompileTests.cs` - pipeline construction contract.
- `Assets/Tests/EditMode/Forge/DiffusionPipelineClampTests.cs` - `ClampDimension` 64-1024 8-align invariant'ı.
- `Assets/Tests/EditMode/Forge/AssetForgeCacheTests.cs` - disk cache round-trip, sadece success yazımı.
- `Assets/Tests/EditMode/Forge/AssetForgeQueueTests.cs` - priority preemption + capacity backpressure.
- `Assets/Tests/EditMode/Forge/GenerationManagerTests.cs` + `GenerationManagerResourceTests.cs` - tek-worker invariant'ı, RAM-guard defer, aynı request de-dupe.
- `Assets/Tests/EditMode/Forge/SerializedAssetForgeTests.cs` - decorator tek-akış pin'i.
- `Assets/Tests/EditMode/Forge/SingleFigureSpriteRefiningAssetForgeTests.cs` - passthrough vs refine dallanması, Reseed + gate retry.
- `Assets/Tests/EditMode/Forge/ModelManifestTests.cs` - SHA verify + placeholder-hash detect.
- `Assets/Tests/EditMode/Forge/GeometricPromptTests.cs` + `TypedImageGenContractTests.cs` - kind-scaffold katalog stability.
- `Assets/Tests/EditMode/Generation/VisibleGenerationPipelineTests.cs` - **B17** `PlaceholderSuccess_IsNeverStamped_SoTheScannerRetriesIt` (PNG yazılır, `.promptmeta` YAZILMAZ), kontrol pin'i: gerçek üretim hâlâ stamp atar.
- `Assets/Tests/EditMode/Generation/VisibleGenerationFlowTests.cs` - end-to-end event akışı, `PipelineResult.Placeholders` sayacı.
- `Assets/Tests/EditMode/Generation/GeneratedAssetProvenanceTests.cs` - `.promptmeta` yazımı ve freshness sınıflandırması.

## W32-W36 Değişiklikleri

- **W33-C (B17)** - `VisibleGenerationPipeline.Write` artık `isPlaceholder` parametresi alıyor; placeholder success PNG'yi yazar ama `.promptmeta` stamp atmaz. `AssetGenerationResult.IsPlaceholder` opsiyonel ctor parametresi (default false), `OnnxAssetForge.GenerateAsync` placeholder branch'ında `isPlaceholder: true` ile geri döner. Sonuç: model gelmeden üretilen 8x8 gri canonical hâline gelmiyor, scanner "stale_missing_provenance" görüp otomatik retry ediyor.
- **W33-C (B18)** - `PromptComposers.CacheKey` schema'sı `"v2|"` prefix + `Width`, `Height`, `NegativePrompt`, `Steps` alanlarını içeriyor. Eski v1 (`prompt|style|seed`) hashler CACHE MISS düşüyor; her v1 entry temiz miss oluyor (asla yanlış hit yok). `ModelHint` kasıtlı dışarıda (model switching v3 için ayrılmış), `TimeoutSeconds` piksel değiştirmediğinden yine dışarıda.
- **W35 (LLM router taraflı)** - forge bu dönemde structural değişim almadı, `ForgeLocator.SetAssetForge` invariant'ı korundu; LLM tarafı yeniden şekillenirken locator tekilliği doğrulandı.
- **W36 (post-arch tail)** - forge dosyalarına git ağacında değişiklik yok, spot-fix hattı diğer sistemlere düştü.
- **W32 öncesi ama görülen zemin** - `EMB-041` (2026-05-30) forge-bootstrap split-brain'i kapatan `EmberForgeFactory` tek geçit; `EMB-042` (2026-05-30) `IsPlaceholder` provenance alanı ilk kez tanıtıldı; `F29 BESTIARY` (2026-06-12) `BestiaryBillboardSpriteFactory` altı-canavarlık fallback silhouette ailesini ekledi; **--ember-forge-off** proof-run izolasyon bayrağı `ForgeBootstrap.Awake`'de (proof capture'larında GPU'yu paylaşan SDXL 16ms bütçesini 537ms worst'a çıkarıyordu).

## Bilinen Borçlar + Kaçak Kapıları

- `SdxlTurboPipeline.RunUnet` her step'te `_sessionFactory.CreateSession(_models.Unet)` çağırıyor - 4-step CFG'de UNet session 8 kez kuruluyor. Aynı yorum `EncodeText/EncodeText2/DecodeLatents` için de geçerli. Session cache'i yok; her generation tam soğuk. Multi-step CFG NPC path'inde performans deltası ölçülmedi.
- `OnnxPngDecoder.InflateStoreBlocks` SADECE store-block (deflate=00) destekliyor. `SdxlTurboPipeline`'ın output'u `OnnxPngEncoder.ZlibStore` (kendi encoder'ımız) olduğu için çalışıyor; harici PNG (huggingface cover, dışardan gelen asset) inflate atmıyor, `"Only store-block zlib payloads are supported"` throw.
- `OnnxPngDecoder.ExpandToRgba` filter=0 dışını reddediyor (`"Unsupported PNG row filter"`). Aynı gerekçe: kendi PNG'lerimiz uyumlu, dış PNG değil.
- `OnnxAssetForge.PlaceholderPng` 8x8, `request?.Seed & 0xFF` seed'ini kullanıyor - portre/npc/creature ayırt etmiyor; kaçak kapı: hangi asset başarısız oldu bilgisi 8x8 gri karede saklı değil, sadece log satırında.
- **`ForgeLocator` static** - test parallelizmi için tehlike; tek proses/tek Editor session assumption'u üzerine kurulu. `Clear()` var ama teardown yolu manuel.
- `SingleFigureSpritePolicies.NpcOnly` id-prefix bazlı (`"npc_"|"creature_"`); id şeması değişirse gate silent-off. Portre bust'ları explicit dışlanıyor; gerçek bir Portrait AssetKind'ı gate'e girmesini istersek ayrı policy şart.
- `OnnxImageMatteService` U2-Net'i HTTPS'ten indiriyor (`github.com/danielgatis/rembg/releases/download/v0.0.0/u2net.onnx`), 175 997 641 byte kontrol var ama SHA yok - manifest'te de sadece URL + expectedBytes yazılıyor. `ModelBootstrap` manifest hash placeholder'sa doğrulamayı atlıyor.
- `ComfyUiAssetForge` hâlâ derleniyor ve `IAssetForge` implementiyor ama `ForgeBootstrap.DetectAsync` `ComfyUiAvailable = false` sabit yazıyor - koddaki bridge şu an DEAD path, aktif kullanılmıyor.
- `ForgeBootstrap.ExplicitFailureAssetForge` ve `ModelBootstrap.ModelBootstrapFailureAssetForge` iki farklı katmanda aynı "başarısızlık forge'u" pattern'i - küçük duplication, `EmberForgeFactory` tek geçit olsa da her iki bootstrap kendi private failure class'ını taşıyor.
- **B18 gapleri**: `ModelHint` kasıtlı dışarıda (yorum: "v3 için"); şu an model switch = eski cache'in yanlış-doğru hit vermesi mümkün (aynı prompt/steps/negatif iki farklı UNet ile ürerse SHA aynı). `TimeoutSeconds` çıktıyı değiştirmediği için dışarıda ama iptal davranışı değişir.
- **B17 gapi**: `IsPlaceholder` flag'i sadece `OnnxAssetForge` kendi placeholder branch'ında yazılıyor. `Sd15LcmPipeline` başarılı ama düşük kalite çıktı verirse "gerçek üretim" olarak stamp atılır - placeholder değil, sadece istenmeyen sonuç. Kalite gate'i (B18'in tersi) yok.
- CUDA runtime tespit için `Application.dataPath/Plugins/x86_64` altı üç aday deneniyor; Editor vs. Player build'de dataPath farklı olabilir - Player'da streamed folder farkı henüz doğrulanmış değil.

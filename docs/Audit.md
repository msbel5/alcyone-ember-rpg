> Historical audit input (reference). Active implementation tracker: docs/REMEDIATION_V2_COUNTER.md.
# Ember 5. deneme kontrolü — kapananlar / kalanlar / Codex 5.3 promptu

Beşinci zip’i statik olarak kontrol ettim: `/mnt/data/alcyone-ember-rpg-main (5).zip`.

Benim ortamımda Unity Editor ve `dotnet` yok, bu yüzden **Unity import, PlayMode, Win64 build, gerçek LFS runtime, gerçek LLM çalışması** doğrulanmadı. Statik script ve dosya taraması yaptım. Bu önemli: `docs/CURRENT_STATE.md` içinde “Win64 build geçti / fallback harness 1226 geçti / model bytes real here” yazıyor; bu **yerel LFS-resolved çalışma ağacında doğru olabilir**, ama **yüklenen zip içinde runtime asset’leri hâlâ Git LFS pointer**. O yüzden Codex’e “kanıt üret, source-only yeşili runtime kanıt sanma” diye vermek gerekiyor.

---

## 1. Beş denemenin kısa delta tablosu

| Alan                                      |                                          Deneme 1 | Deneme 2 | Deneme 3 |        Deneme 4 | Deneme 5 sonucu                                                  |
| ----------------------------------------- | ------------------------------------------------: | -------: | -------: | --------------: | ---------------------------------------------------------------- |
| `docs/EMBER_GOAL.md`                      |                                             vardı |      yok |      yok |             yok | yok                                                              |
| `docs/CURRENT_STATE.md`                   |                                               yok |    vardı |      yok |             yok | **var**                                                          |
| `docs/AUDIT_COUNTER.md`                   |                                               yok |    vardı |    vardı |           vardı | **silinmiş**                                                     |
| `Reports/`                                |                                             vardı |      yok |      yok |             yok | yok                                                              |
| `GeneratedAssets/`                        |                                             vardı |      yok |      yok |             yok | yok                                                              |
| `Assets/Generated/Core/`                  |                                               yok |      yok |      yok |           vardı | **var**                                                          |
| TMP Examples                              |                                             vardı |      yok |      yok |             yok | yok                                                              |
| `Assets/Plans`, `Assets/pold`             |                                             vardı |      yok |      yok |             yok | yok                                                              |
| duplicate `.meta` GUID                    |                                             vardı |      yok |      yok |             yok | **yok**                                                          |
| missing `.meta`                           |                                                 3 |        0 |        0 |               0 | **0**                                                            |
| orphan `.meta`                            |                                                15 |       13 |       13 |              12 | **0**                                                            |
| broad LFS pointer count under `Assets`    |                                               908 |      908 |      908 |             908 | **908**                                                          |
| runtime plugin/model LFS pointer count    |                                                25 |       25 |       25 |              25 | **25**                                                           |
| `Simulation → Data.SliceJson` asmdef edge |                                               var |      var |      var |             var | **var**                                                          |
| `SliceSaveMapper.cs` monolith             |                                             vardı |      yok |      yok |             yok | **yok**                                                          |
| `DomainSimulationAdapter.cs` LOC          |                                              1173 |    split |      557 |             718 | **365 main, ama partial toplamı hâlâ büyük**                     |
| >1000 LOC source/test                     | `SpellEffectResolutionServiceTests.cs` 1033 vardı |    vardı |    vardı |           vardı | **artık yok**                                                    |
| package test-framework skew               |                                     vardı/karışık |    vardı |    vardı |           vardı | **manifest 1.6.0, lock 1.6.0 root — kapanmış**                   |
| generated actor visibility                |                                         yok/eksik |    eksik |    eksik | spawner başladı | **spawner var, runtime proof eksik**                             |
| native GGUF pointer readiness             |                                             zayıf |    zayıf |    zayıf |           zayıf | **kod/test düzeyinde düzelmiş**                                  |
| main-menu Continue save path              |                             PlayerPrefs ağırlıklı |  partial |  partial |         partial | **file repository üzerinden çözülüyor**                          |
| living-world tick                         |                                             zayıf |      dar |      dar |        daha iyi | **time/needs/jobs/schedules/plants/prices/caravans/magic bağlı** |

Net: **5. deneme önceki dört denemeye göre ciddi cleanup yaptı.** En büyük asset-hygiene borçları kapandı. Kalanlar artık daha çok **runtime proof, scene proof, save mimarisi, actor identity, asmdef boundary, büyük sınıf refactorları**.

---

## 2. Kapanan / artık açık sayılmaması gerekenler

Bunları Codex’e tekrar “sıfırdan düzelt” diye vermemelisin.

| Eski bulgu                                                                          | Durum                      | Kanıt                                                                                                                                                   |
| ----------------------------------------------------------------------------------- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Unity/URP doğrulaması                                                               | Kapandı / doğrulandı       | `ProjectSettings/ProjectVersion.txt`: `6000.3.13f1`; `Packages/manifest.json`: URP `17.3.0`.                                                            |
| Duplicate `.meta` GUID                                                              | Kapandı                    | `tools/validation/static-audit.sh`: `PASS: no duplicate GUIDs.`                                                                                         |
| Missing `.meta`                                                                     | Kapandı                    | `PASS: every non-hidden Asset file has a .meta on disk.`                                                                                                |
| Orphan `.meta`                                                                      | Kapandı                    | `PASS: no orphan .meta`. Deneme 4’te 12 idi, deneme 5’te 0.                                                                                             |
| `Assets/Generated/Core` policy                                                      | Büyük ölçüde kapandı       | `Assets/Generated/Core/.gitkeep` var; `Assets/Generated/Core.meta` artık orphan değil.                                                                  |
| `Reports/`, `GeneratedAssets/`, `Assets/Plans`, `Assets/pold`, TMP Examples clutter | Kapandı                    | Bu klasörler deneme 5’te yok.                                                                                                                           |
| Root non-build old scenes                                                           | Kapandı                    | Build settings 13 sahneyi sadece `Assets/Scenes/Ember/**` altında listeliyor.                                                                           |
| `SliceSaveMapper.cs` monolith                                                       | Kapandı                    | Dosya yok; mapper işi `WorldSaveMapper*` / `WorldSaveData.cs` tarafına taşınmış.                                                                        |
| Save restore reflection-copy                                                        | Kapandı                    | `WorldState.CopyFrom(WorldState other)` var; reflection restore paterni eskiye göre düzelmiş.                                                           |
| Process/farming stores Presentation side-state                                      | Büyük ölçüde kapandı       | `WorldState` artık `Plants`, `Soils`, `Jobs`, `Worksites` taşıyor.                                                                                      |
| Main tick frozen / dar sim loop                                                     | Kapandı core düzeyde       | `WorldTickComposer` time, needs, magic, caravans, plant growth, job assignment, price update, schedule bağlıyor.                                        |
| LLM provider Simulation içinde                                                      | Kapandı                    | `NativeLlmClient`, `LlmHttpClientCore`, `LocalQwenClient` artık `Assets/Scripts/Infrastructure/AiDm/**`.                                                |
| MiniLM manifest path mismatch                                                       | Kapandı                    | Manifest `all-minilm-l6-v2/model.onnx`; `ModelBootstrap` da `all-minilm-l6-v2` kullanıyor.                                                              |
| Manifest `TBD` hashleri                                                             | Kapandı                    | `Assets/StreamingAssets/Models/manifest.json` artık 64-char sha256 içeriyor.                                                                            |
| Native LLM `File.Exists` readiness bug                                              | Kod/test düzeyinde kapandı | `NativeLlmClient.IsUsableModelFile()` size + `GGUF` magic kontrol ediyor; `NativeLlmModelReadinessTests.cs` pointer/truncated/wrong-magic testleri var. |
| Main menu PlayerPrefs-only Continue                                                 | Kapandı                    | `EmberMainMenuUI.Flow.cs` artık `EmberSaveService.TryResolveLatestSave()` çağırıyor.                                                                    |
| Player-build scene validation tamamen açık                                          | Büyük ölçüde kapandı       | `Application.CanStreamedLevelBeLoaded` hem save hem portal tarafında kullanılıyor.                                                                      |
| Direct legacy Input scattered                                                       | Büyük ölçüde kapandı       | Direct `Input.` çağrıları `EmberInput.cs` içinde merkezileşmiş.                                                                                         |
| Generated NPC visual projection hiç yok                                             | Partial kapandı            | `EmberGeneratedActorSpawner.cs` var; generated actor billboard spawn ediyor ve `ActorId` bağlıyor.                                                      |
| DialogBoxPanel scene eksikliği                                                      | Partial kapandı            | `EmberWorldHost.Ui.cs` runtime’da `EnsureDialogBoxPanel()` yapıyor.                                                                                     |
| Package test-framework mismatch                                                     | Kapandı                    | Manifest `com.unity.test-framework: 1.6.0`; lock root da `1.6.0`.                                                                                       |
| >1000 LOC magic test                                                                | Kapandı                    | Deneme 5 LOC taramasında artık >1000 dosya yok.                                                                                                         |

---

## 3. Hâlâ açık kalanlar

Bunlar Codex 5.3’e verilecek gerçek kalan işler.

| ID      | Öncelik | Durum              | Path(s)                                                                                                                    | Kanıt                                                                                                                                                                   | Neden önemli                                                                           | Fix yönü                                                                                                                                   |
| ------- | ------- | ------------------ | -------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| LEFT-01 | P0      | Açık               | `docs/EMBER_GOAL.md`, `docs/CURRENT_STATE.md`, `README.md`, `docs/REMEDIATION_V2_COUNTER.md`                               | `EMBER_GOAL.md` hâlâ yok; `CURRENT_STATE.md` var ama README hâlâ uzun historical block taşıyor; remediation dosyasında eski audit gömülü içerik var.                    | Codex yanlış doc’u izlerse düzeltilmiş işleri bozabilir.                               | `EMBER_GOAL.md` için kısa redirect/stub veya read-order düzeltmesi; README historical bölümü archive’a; `CURRENT_STATE.md` tek kısa truth. |
| LEFT-02 | P0      | Açık               | `Assets/Plugins/**`, `Assets/StreamingAssets/Models/**`, `Assets/Art/**`                                                   | `static-audit --require-runtime` 25 runtime pointer ile fail; broad scan: 883 PNG + 13 DLL + 10 ONNX + 1 GGUF + 1 PB pointer.                                           | Source-only yeşil sonuç gerçek oyun/LLM/art/build kanıtı değil.                        | Runtime-LFS proof mode ve docs/CI ayrımı.                                                                                                  |
| LEFT-03 | P0/P1   | Açık               | `tools/validation/static-audit.sh`                                                                                         | Script runtime pointer check’i sadece `Assets/Plugins` + `Assets/StreamingAssets` için yapıyor; `Assets/Art` pointer PNG’leri runtime visual proof modunda yakalamıyor. | Billboard görselleri pointer iken screenshot/build “geçti” sanılabilir.                | `--require-runtime` altında art PNG pointer scan opsiyonu eklenmeli.                                                                       |
| LEFT-04 | P1      | Açık / kanıt eksik | `docs/proofs/**`, `.github/workflows/unity-test.yml`, `docs/CURRENT_STATE.md`                                              | CI hâlâ `lfs:false`; `CURRENT_STATE.md` yerel build/LLM başarıları söylüyor ama yüklenen zip source-only pointer.                                                       | Proof reproducible değilse Codex gerçek runtime varsayar.                              | Proof’ları `source-only`, `LFS-runtime`, `Unity-PlayMode`, `manual` olarak etiketle.                                                       |
| LEFT-05 | P1      | Açık               | `.github/workflows/unity-test.yml`                                                                                         | PlayMode/build jobs hâlâ `lfs:false`; comments dürüst ama runtime-LFS job yok.                                                                                          | CI hâlâ gerçek LLM/art/build kanıtlamıyor.                                             | Opsiyonel/manual `runtime-lfs-validation` job veya dokümante local gate.                                                                   |
| LEFT-06 | P1      | Açık               | `Assets/Scripts/Presentation/Ember/Save/EmberSaveService*.cs`, `SaveData.cs`                                               | Default slot `0`, PlayerPrefs mirror/fallback, static `_pendingLoad`, nested `domainStateJson`.                                                                         | Ember uzun yaşayan dünya; tek slot/prototype envelope yeterli değil.                   | Multi-slot UI + typed save envelope + migration/golden fixtures.                                                                           |
| LEFT-07 | P1/P2   | Açık               | `Assets/Scenes/Ember/*.unity`, `EmberInteractable.cs`, `ActorView.cs`                                                      | Scene YAML’de `_actorId:` ve `_domainActorId:` alanları çoğunlukla boş; `_displayName` dolu.                                                                            | Actor memory/faction/schedule/Ask About display-name ile güvenli değil.                | Authored actors/interactables stable IDs ile migrate edilmeli. Unity Editor gerekir.                                                       |
| LEFT-08 | P2      | Açık               | `ConversationState.cs`, `DomainSimulationAdapter.Dialog.cs`                                                                | `ConversationState` hâlâ `ActorName` string tutuyor; `SelectTopic()` `_activeDialogActor` name ile actor/npc arıyor.                                                    | Aynı isimli NPC’lerde memory/topic/faction karışır.                                    | Conversation state’e `ActorId` / `NpcId` ekle; display name sadece UI text olsun.                                                          |
| LEFT-09 | P1/P2   | Açık               | `Assets/Scenes/Ember/**`, `Assets/Tests/PlayMode/**`                                                                       | PlayMode testleri Boot/CharacterCreation/Worldgen route kapsıyor; 13 sahnenin full movement/dialog/portal/save screenshot turu yok.                                     | Ember oynanabilir CRPG route olmalı, sadece compile değil.                             | Scene-tour validation + screenshot proof. Unity Editor/PlayMode gerekir.                                                                   |
| LEFT-10 | P2      | Açık               | `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef`                                                                    | Simulation hâlâ `EmberCrpg.Data.SliceJson` referanslıyor.                                                                                                               | Deterministic simulation concrete JSON persistence’a bağlı kalıyor.                    | Ya dependency kaldır, ya bilinçli architectural exception olarak docs’a yaz.                                                               |
| LEFT-11 | P2      | Açık               | `DomainSimulationAdapter*.cs`, `EmberWorldHost*.cs`                                                                        | Aggregate partial sorumluluk hâlâ büyük: adapter main 365 + Dialog 448 + Worldgen 472 + Combat 310 + Save/Fate; `EmberWorldHost.cs` 530 + UI 271.                       | Codex feature work burada regression üretir.                                           | No-behaviour-change extraction PR’ları.                                                                                                    |
| LEFT-12 | P2/P3   | Açık               | `CharacterCreationController*.cs`, `EmberHud.cs`, `DialogBoxPanel.cs`, `LoadingScreenController.cs`, `EmberMainMenuUI*.cs` | CharacterCreation 669 + Rendering 580; HUD 538; Dialog 369; Loading 369; menu partials.                                                                                 | Player flow/UI procedural ve overcoupled.                                              | State/presenter/view split; screenshot regression.                                                                                         |
| LEFT-13 | P2      | Açık               | `Assets/Scripts/Infrastructure/AiDm/LlmHttpClientCore.cs`, `LocalQwenClient.cs`, `NativeLlmClient.cs`                      | `GetAwaiter().GetResult()` hâlâ 6 site.                                                                                                                                 | Blocking provider calls loading/dialog freeze veya cancellation sorunları yaratabilir. | Async/cancellable provider ya da bounded worker.                                                                                           |
| LEFT-14 | P2      | Açık               | `NativeLlmClient.cs`, `docs/AI_STACK.md`                                                                                   | `EnsureModelReady()` model indirebiliyor; docs explicit opt-in diyor. Kodda opt-in config net değil.                                                                    | Multi-GB download loading sırasında sürpriz olmamalı.                                  | Silent download kapalı; explicit opt-in/progress/cancel.                                                                                   |
| LEFT-15 | P2      | Açık               | `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs`, `Sd15LcmPipeline.cs`, `SdxlTurboPipeline.cs`                          | ONNX provider implementation Simulation altında.                                                                                                                        | Simulation deterministic/headless kalmalı; provider infrastructure/runtime’dır.        | Provider impl’i Infrastructure/Presentation runtime assembly’ye taşıma planı.                                                              |
| LEFT-16 | P3      | Açık / proof eksik | `EmberGeneratedActorSpawner.cs`, scenes, docs/proofs                                                                       | Spawner var, ama bu statik auditte gerçek Unity screenshot/interaction kanıtı yok.                                                                                      | Generated actors canonical world’ün görünen aktörleri olmalı.                          | Same-seed visible NPC screenshot + interaction proof.                                                                                      |
| LEFT-17 | P3      | Açık               | `WorldTickComposer.cs`                                                                                                     | Hardcoded wheat catalog, price constants, cadence constants.                                                                                                            | Living world scale için data-driven olmalı.                                            | Digest tests sonrası catalogs/rules data definitions’a alınmalı.                                                                           |
| LEFT-18 | P3      | Açık               | `EmberInput.cs`                                                                                                            | Legacy `UnityEngine.Input` merkezi ama hâlâ legacy.                                                                                                                     | Rebinding/accessibility yok.                                                           | Input System action maps facade arkasına migrate.                                                                                          |
| LEFT-19 | P4      | Açık               | `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`, `Reference/PRDs/**`, `docs/PRD_GOVERNANCE.md`                               | Governance daha iyi; matrix hâlâ eski backend/Godot path’lerini active gibi gösterebiliyor.                                                                             | Codex Godot-era PRD’yi aktif Unity işi sanabilir.                                      | Matrix’i active/reference/deprecated olarak regenerate.                                                                                    |
| LEFT-20 | P4      | Açık               | `docs/REPO_HYGIENE.md`, `README.md`, `docs/proofs/**`, `Assets/Scripts/Data/Save/SliceJson/README.md`                      | Bazı docs artık silinmiş TMP Examples / old audit / old paths / `SliceSaveMapper` gibi şeylere referans veriyor.                                                        | Docs düzelmiş işi tekrar açtırıyor.                                                    | Stale docs cleanup.                                                                                                                        |
| LEFT-21 | P5      | Açık               | `Assets/Resources/**`, UI/loading/theme code                                                                               | `Resources.Load` kullanımı devam ediyor.                                                                                                                                | Hidden dependencies / build bloat / asset ownership belirsiz.                          | Küçük global asset dışını explicit references/registry’ye taşıma.                                                                          |

---

## 4. Codex 5.3’e verilecek en doğru prompt

Aşağıdaki prompt’u **doğrudan Codex 5.3’e** verebilirsin. Kapsamı özellikle cleanup/proof tarafında tuttum. Yeni gameplay feature istemiyor; önce kalan gerçek açıkları kapatıyor.

```text
You are working on the Unity/C# repository **Alcyone Ember RPG / Ember**.

This is a cleanup/proof pass after five audit/remediation attempts. Do not rewrite the project, do not add new gameplay features, and do not turn Ember into a generic action RPG. Preserve Ember’s identity: deterministic living-world CRPG, simulation-first authority, 3D Daggerfall-like world with 2D billboard actors, local LLM only for flavour, and LLM world mutation only through validated tools.

Use the current repo state as the source of truth. The prior broad audit has been mostly addressed. Do NOT re-fix closed issues.

## Current known addressed items

Do not spend time on these unless validation shows they regressed:

- Unity version is `6000.3.13f1`; URP is `17.3.0`.
- Duplicate `.meta` GUIDs are fixed.
- Missing `.meta` files under non-hidden `Assets` are fixed.
- Orphan `.meta` files are fixed in attempt 5.
- Root clutter is removed: no top-level `Reports/`, no top-level `GeneratedAssets/`, no `Assets/Plans`, no `Assets/pold`, no TMP Examples & Extras.
- `Assets/Generated/Core/` exists with `.gitkeep`.
- Old root non-build scenes are gone; build scenes are under `Assets/Scenes/Ember`.
- `SliceSaveMapper.cs` is gone; save mapping is split.
- `WorldState.CopyFrom` replaced old reflection-style restore.
- `WorldState` now owns plants/soils/jobs/worksites.
- `WorldTickComposer` now wires time, magic, needs, caravans, plant growth, job assignment, price updates, and schedules.
- LLM provider implementation moved to `Assets/Scripts/Infrastructure/AiDm`.
- Model manifest paths and MiniLM path are fixed.
- Model manifest hashes are no longer `TBD`.
- `NativeLlmClient.IsUsableModelFile()` rejects LFS-pointer/truncated/wrong-magic GGUF files.
- Main menu Continue/Load now resolves through `EmberSaveService.TryResolveLatestSave`.
- Direct legacy `Input.` calls are centralized in `EmberInput.cs`.
- `EmberGeneratedActorSpawner.cs` exists and creates runtime billboard actors with stable `ActorId`.
- `EmberWorldHost` creates missing HUD/dialog UI at runtime.
- `Packages/manifest.json` and `packages-lock.json` now agree on root `com.unity.test-framework` 1.6.0.

## Current open problems you should address

Work in small safe batches. Prefer one commit per batch. Do not edit scene YAML by hand unless there is no Unity-safe alternative.

### Batch 1 — docs truth and stale-doc cleanup

Goal:
Make the docs match the current attempt-5 repository state and stop future agents from following stale audit text.

Files likely touched:
- `README.md`
- `docs/CURRENT_STATE.md`
- `docs/REMEDIATION_V2_COUNTER.md`
- `docs/REPO_HYGIENE.md`
- `docs/AI_STACK.md`
- `docs/proofs/README.md`
- `docs/proofs/**`
- `docs/PRD_GOVERNANCE.md`
- `docs/reference/PRD_IMPLEMENTATION_MATRIX.md`
- `Assets/Scripts/Data/Save/SliceJson/README.md`

Required fixes:
1. `docs/EMBER_GOAL.md` is still absent while the original audit prompt expected it. Do not recreate a huge stale goal file. Either:
   - add a tiny redirect stub `docs/EMBER_GOAL.md` pointing to `docs/CURRENT_STATE.md`, `docs/REMEDIATION_V2_COUNTER.md`, and the vision bible, OR
   - update every current doc/read-order reference so `docs/CURRENT_STATE.md` is explicitly the replacement.
2. Keep `docs/CURRENT_STATE.md` concise and truthful. It may say local LFS-resolved builds/proofs were performed only if the proof is reproducible and clearly mode-tagged.
3. Mark proof docs as one of: `source-only`, `LFS-runtime`, `Unity PlayMode`, `manual screenshot`, or `historical`.
4. Remove or archive stale claims about already-fixed items:
   - missing orphan metas,
   - missing `Assets/Generated/Core`,
   - TMP Examples,
   - old root scenes,
   - old `SliceSaveMapper.cs`,
   - old `AUDIT_COUNTER.md`,
   - old `Reports/` clutter.
5. Fix `Assets/Scripts/Data/Save/SliceJson/README.md` if it still references removed/moved files such as `SliceSaveMapper.cs` or wrong `JsonSliceSaveService` placement.
6. Update PRD matrix/governance so active Unity PRDs, old Godot/backend PRDs, and reference-only PRDs are unambiguous.

Acceptance criteria:
- `grep -RIn "EMBER_GOAL\\|AUDIT_COUNTER\\|SliceSaveMapper\\|Reports/\\|GeneratedAssets/\\|Examples & Extras\\|Assets/pold\\|Assets/Plans" README.md docs Assets/Scripts/Data/Save/SliceJson` shows no misleading active-status references.
- `docs/CURRENT_STATE.md` is one page or close to one page and distinguishes source-only proof from LFS-runtime proof.
- `docs/PRD_GOVERNANCE.md` and `docs/reference/PRD_IMPLEMENTATION_MATRIX.md` do not imply old Godot/backend PRDs are active Unity implementation requirements.
- No code or Unity assets are changed in this batch except docs.

Validation:
- Run `bash tools/validation/static-audit.sh`.
- No Unity Editor required for this docs-only batch.

### Batch 2 — runtime asset / LFS validation honesty

Goal:
Prevent source-only green tests from being mistaken for real runtime LLM/art/build proof.

Files likely touched:
- `tools/validation/static-audit.sh`
- `.github/workflows/unity-test.yml`
- `docs/validation.md`
- `docs/AI_STACK.md`
- `docs/REPO_HYGIENE.md`

Known evidence:
- `bash tools/validation/static-audit.sh` passes in source-only mode.
- `bash tools/validation/static-audit.sh --require-runtime` fails on 25 runtime plugin/model pointer files.
- A broad scan under `Assets` finds 908 LFS pointer files:
  - 883 `.png`
  - 13 `.dll`
  - 10 `.onnx`
  - 1 `.gguf`
  - 1 `.pb`

Required fixes:
1. Keep source-only mode fast and non-failing for LFS pointer assets.
2. Add a strict runtime/visual proof mode that also detects LFS pointer art files under `Assets/Art` and any other runtime visual asset paths used by scenes/billboards.
3. Make the script output explicit:
   - source-only mode validates code/static structure only;
   - runtime mode is required for LLM, ONNX, native plugin, art, screenshot, PlayMode, and build proof.
4. CI may stay `lfs:false`, but the workflow must expose or document a separate runtime-LFS proof path. It can be manual if LFS budget is a constraint.
5. Do not modify or replace binary/model/art files.

Acceptance criteria:
- Existing `bash tools/validation/static-audit.sh` still passes on source-only checkout.
- New strict runtime command fails on pointer `.png`, `.dll`, `.onnx`, `.gguf`, `.pb` files with clear paths.
- Docs explain exactly which command is source-only and which is runtime-proof.
- CI labels source-only jobs honestly and points to runtime-LFS proof.

Suggested validation:
- `bash tools/validation/static-audit.sh`
- `bash tools/validation/static-audit.sh --require-runtime`
- Add and run the new art-inclusive runtime option if you implement it.

Unity Editor required:
- No for the script/docs.
- Yes later for actual runtime proof, not in this batch.

### Batch 3 — save/load characterization, not full rewrite

Goal:
Lock down current save/load behaviour before deeper persistence refactors.

Files likely touched:
- `Assets/Tests/EditMode/Save/**`
- `Assets/Scripts/Presentation/Ember/Save/EmberSaveService*.cs`
- `Assets/Scripts/Presentation/Ember/UI/EmberMainMenuUI*.cs`
- `Assets/Scripts/Data/Save/FileSaveRepository.cs`
- `Assets/Scripts/Presentation/Ember/Save/SaveData.cs`

Do not do:
- Do not implement full multi-slot UI yet.
- Do not rewrite the save schema wholesale.
- Do not remove PlayerPrefs legacy fallback in this batch.

Known current state:
- File repository exists.
- Corrupt-save quarantine exists.
- Main menu Continue now uses `EmberSaveService.TryResolveLatestSave`.
- Still open: default slot `0`, PlayerPrefs mirror/fallback, static `_pendingLoad`, nested `domainStateJson`, full schema migration proof.

Required fixes:
1. Add tests for:
   - file slot load wins over legacy PlayerPrefs when valid;
   - PlayerPrefs legacy fallback still works when file slot missing;
   - corrupt file slot is quarantined and falls back safely;
   - `TryResolveLatestSave` rejects malformed/empty payloads;
   - invalid scene names are rejected through runtime-safe validation;
   - menu Continue path uses the same save resolution service and does not parse PlayerPrefs directly.
2. If any small bug is exposed by these tests, fix it narrowly.
3. Document current limitations: single default slot, future multi-slot UI, nested `domainStateJson`.

Acceptance criteria:
- New save tests are deterministic and do not require Unity PlayMode unless absolutely necessary.
- `grep -RIn "PlayerPrefs.GetString(.*ember.save.v1" Assets/Scripts/Presentation/Ember/UI Assets/Scripts/Presentation/Ember/Save` shows no menu-side direct canonical save parsing outside `EmberSaveService` internals.
- Invalid scene save is rejected before `SceneManager.LoadScene`.

Validation:
- Run EditMode/fallback if available.
- Run `bash tools/validation/static-audit.sh`.

Unity Editor required:
- No for pure tests if kept EditMode/pure.
- Yes later for manual save/load scene proof.

### Batch 4 — actor identity validation before scene migration

Goal:
Stop future work from relying on display names for actor identity, but do not blindly mass-edit scene YAML.

Files likely touched:
- `Assets/Scripts/Presentation/Ember/Interaction/EmberInteractable.cs`
- `Assets/Scripts/Presentation/Ember/Views/ActorView.cs`
- `Assets/Scripts/Presentation/Ember/Interaction/EmberPlayerInteractRaycaster.cs`
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Dialog.cs`
- `Assets/Scripts/Domain/Narrative/ConversationState.cs`
- tests under `Assets/Tests/**`
- editor validation tooling if appropriate

Known current state:
- Runtime generated actors carry stable `ActorId`.
- Authored scene actors/interactables mostly have `_actorId:` / `_domainActorId:` fields empty in scene YAML.
- `ConversationState` still stores `ActorName`, not `ActorId`.
- `SelectTopic()` still has name-based lookup seams via `_activeDialogActor`.

Required fixes:
1. Add validation/test that reports every authored `ActorView` / `EmberInteractable` lacking a stable actor id, but do not mass-edit scenes in this batch.
2. Change conversation state to carry stable `ActorId` / optional `NpcId` where available. Display name remains only display text.
3. Ensure the `GetDialogSource(ActorId)` path remains primary.
4. Keep legacy display-name fallback only for clearly labelled legacy/ad-hoc interactables.
5. Add duplicate-display-name test: two actors with the same name but different IDs must resolve to different conversations/topics/events.

Acceptance criteria:
- Tests prove duplicate display names do not collapse conversation state.
- Scene validation reports missing IDs without changing scenes yet.
- No mass scene YAML edits.
- Generated actor spawner still compiles and uses `ActorId`.

Validation:
- EditMode tests.
- `bash tools/validation/static-audit.sh`.

Unity Editor required:
- No for tests/validation.
- Yes later for authored-scene ID migration.

### Batch 5 — PlayMode/manual proof planning, not visual polish

Goal:
Prepare the actual scene-tour proof for all 13 build scenes.

Files likely touched:
- `Assets/Tests/PlayMode/**`
- `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs`
- `docs/proofs/README.md`
- maybe editor validation scripts

Do not do:
- Do not polish visuals yet.
- Do not edit scenes unless the validation harness exposes a tiny safe issue and you can prove the fix.
- Do not treat headless/source-only tests as visual proof.

Required proof checklist:
For each build scene:
- `Boot`
- `MainMenu`
- `CharacterCreation`
- `SmithingOverworld`
- `ColonyNeeds`
- `SeasonFarm`
- `TradeMarket`
- `CombatDungeon`
- `RitualHall`
- `TavernDialog`
- `OracleShrine`
- `ShowroomOverview`
- `TavernFlavour`

Verify or report:
- scene loads;
- camera/player/EventSystem expected state;
- HUD visible where relevant;
- dialog path works where NPC/interactable exists;
- portal targets validate;
- save/load path is present in gameplay scenes;
- generated actors are visible and not giant/magenta;
- screenshots are captured for human review.

Acceptance criteria:
- PlayMode/editor validation can list pass/fail per scene.
- Failure report is precise and does not silently pass scenes with missing interaction.
- Screenshot proof output is mode-labelled: source-only / LFS-runtime / manual / PlayMode.

Unity Editor required:
- Yes.

### Batch 6 — architecture tail, one no-behaviour-change refactor at a time

Do this only after the validation and proof baselines above are in place.

Remaining refactor targets:
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter*.cs`
- `Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost*.cs`
- `Assets/Scripts/Presentation/Ember/CharacterCreation/CharacterCreationController*.cs`
- `Assets/Scripts/Presentation/Ember/UI/EmberHud.cs`
- `Assets/Scripts/Presentation/Ember/UI/DialogBoxPanel.cs`
- `Assets/Scripts/Presentation/Ember/Loading/LoadingScreenController.cs`
- `Assets/Scripts/Infrastructure/AiDm/LlmHttpClientCore.cs`
- `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs`
- `Assets/Scripts/Simulation/EmberCrpg.Simulation.asmdef`

Rules:
- One responsibility per PR.
- No scene YAML changes.
- No gameplay feature changes.
- Characterization tests first.
- Preserve public MonoBehaviour names and serialized fields unless a Unity reference scan proves it is safe.

Preferred order:
1. Remove or explicitly document `EmberCrpg.Simulation -> EmberCrpg.Data.SliceJson` dependency.
2. Extract read-model projection from `DomainSimulationAdapter`.
3. Extract dialog/fate bridge from `DomainSimulationAdapter`.
4. Extract save bridge from `DomainSimulationAdapter`.
5. Split `EmberWorldHost` lifecycle/composition/UI binding.
6. Split character creation state from rendering.
7. Split LLM HTTP/local/cloud provider classes.
8. Move or isolate ONNX forge provider implementation out of deterministic Simulation boundary if feasible.

Validation:
- Existing tests green.
- Static audit green.
- Scene smoke after Presentation changes.
- Do not proceed to the next refactor if validation regresses.

## General guardrails

- Do not rewrite the project.
- Do not add new gameplay features.
- Do not move Unity assets without `.meta`.
- Do not rename MonoBehaviour classes without scene/prefab reference checks.
- Do not hand-edit scene YAML unless tiny, audited, and verified.
- Do not delete docs before marking canonical/reference/archive.
- Do not let LLM mutate world state except through validated tools.
- Do not make fallback/canned AI look like real local Qwen.
- Do not enable cloud/network providers by default.
- Do not silently download large models during normal play.
- Do not treat LFS pointer files as runtime assets.
- Do not add another manager/helper/god class.
- Keep Ember strange, systemic, grounded, simulation-first, and CRPG-readable.
```

---

## 5. Codex’e ilk verilecek en küçük parça

Eğer tek bir ilk PR istiyorsan, yukarıdaki prompt’tan sadece şunu ver:

```text
Start with Batch 1 only: docs truth and stale-doc cleanup.

Do not touch code, scenes, assets, packages, or binary/model files.

Goal:
Make the docs match the current attempt-5 repository state. Create or restore a tiny `docs/EMBER_GOAL.md` redirect/stub or update all current docs so `docs/CURRENT_STATE.md` is explicitly the replacement. Keep `docs/CURRENT_STATE.md` concise. Mark old audit/proof docs as historical/reference. Remove stale active-status references to already-fixed issues: orphan metas, missing `Assets/Generated/Core`, TMP Examples, old root scenes, old `SliceSaveMapper.cs`, old `AUDIT_COUNTER.md`, `Reports/`, `GeneratedAssets/`, `Assets/pold`, `Assets/Plans`.

Acceptance:
- `grep -RIn "EMBER_GOAL\\|AUDIT_COUNTER\\|SliceSaveMapper\\|Reports/\\|GeneratedAssets/\\|Examples & Extras\\|Assets/pold\\|Assets/Plans" README.md docs Assets/Scripts/Data/Save/SliceJson` has no misleading active-status references.
- `docs/CURRENT_STATE.md` states source-only vs LFS-runtime vs Unity/PlayMode/manual proof clearly.
- No code/assets changed.
- Run `bash tools/validation/static-audit.sh`.
```

Bu ilk PR güvenli; Unity Editor gerekmez. Ondan sonra runtime/LFS validation PR’ına geçmek en mantıklısı.

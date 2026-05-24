# Codex Mission — Ember CRPG: Visible Generation & Consistent UI Cutover

> Copy-paste this whole file into Codex Desktop as the initial prompt.
> The full spec is `docs/prds/visible-generation-cutover.md` (same branch).
> Approved by @msbel5: single-mission, single-branch, single-PR cutover.

---

Codex, görev: Ember CRPG'ye **görünür asset generation + tutarlı UI cutover**'ı gerçekleştir.

Bu tek sprint değil, **cutover projesi**. Yarım bırakma: oku → planla → testleri yaz → kodu yaz → test et → Windows64 build üret → kullanıcı için kanıt rapor yaz → GitHub'a pushla.

Çıktın **tek bir PR** olsun (PR #210, zaten draft, bu branch'te) ve `Reports/visible-generation-cutover_<unix>.md` kanıt raporu olsun.

================================================================================
0. ÇALIŞMA KLASÖRÜ / REPO / BRANCH
================================================================================

```
Windows working directory:
C:\Users\msbel\projects\alcyone-ember-rpg

Canonical GitHub repo:
msbel5/alcyone-ember-rpg

Base branch when you start:
main      (after PR #212 "codex/sdxl → main reconcile" merges)

Your work branch:
feat/visible-generation-cutover

Active PR for your work:
#210 (draft)

Unity Editor version (project-pinned):
6000.3.13f1  — see ProjectSettings/ProjectVersion.txt

Unity Editor install location on this developer's machine:
E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe
```

**Pre-flight check before you start writing any code:**

```bash
# These two MUST be true. If not, stop and post a PR #210 comment "BLOCKED: <reason>".
git fetch origin
git log -1 --format=%H origin/main                # must equal a merged PR #212 head
gh pr view 212 --json state --jq '.state'         # must be "MERGED"

# Then rebase your branch onto main (Codex's PR base):
git checkout feat/visible-generation-cutover
git rebase origin/main                            # resolve conflicts using PRD §5 file map
git push --force-with-lease origin feat/visible-generation-cutover
# Switch PR #210's base branch from codex/sdxl-pipeline-and-naming-refactor → main:
gh pr edit 210 --base main
```

================================================================================
1. VİZYON / KARAR
================================================================================

Bkz `docs/prds/visible-generation-cutover.md` §1. Özet:

- Asset generation şu an sessiz. Kullanıcı 800 asset'in hangi 750'sinin bittiğini, hangi 50'sinin neden patladığını bilmiyor.
- Worldgen "soru sorduğunu" söylüyor ama hiç soru ekrana çıkmıyor.
- CharacterCreation "bombos" — kullanıcı skill seçemiyor, zar atamıyor, dünya log'unu okuyamıyor.

**Cutover:** `start.exe` → Boot → AssetGen (visible) → MainMenu → New Game → CharacterCreation (visible) → LoadingScreen → Worldgen (visible) → game scene. **Tek branch, tek PR.**

Phase'lara bölme YOK. Aynı PR'da hepsi.

================================================================================
2. NEYIN ZATEN DOĞRU OLDUĞU (don't re-discover)
================================================================================

| | |
|---|---|
| Unity 6.3.13f1 compile geçiyor (0 error, 5 known benign warnings) | ✅ |
| `Packages/com.unity.ai.assistant/` embedded + `SigLip2Text.cs:59` patched | ✅ PR #207 |
| `OnnxAssetForge.cs` PR #207 Path.GetDirectoryName try/catch (line 302/305/315) | ✅ |
| `ForgeBootstrap.BuildForge` SD15 instance preserved when TryWarmup fails | ✅ PR #212 |
| `ClampDimension` snaps to multiple of 8 in both SDXL + SD1.5 pipelines | ✅ PR #212 |
| CI workflow checkout uses `lfs: true` (DLLs resolve correctly) | ✅ PR #212 |
| `USE_ONNX_RUNTIME` scripting define present in ProjectSettings.asset | ✅ |
| 6 base silhouettes on disk: `Assets/Art/BodySilhouettes/{humanoid_male,humanoid_female,beast_quadruped,undead_humanoid,construct,aberration}.png` | ✅ |
| Forge domain (`OnnxModelBundle`, `IDiffusionPipeline`, `SdxlTurboPipeline`, `Sd15LcmPipeline`, `OnnxSessionFactory`, `OnnxPngEncoder`, `PromptComposers`) | ✅ |
| `ForgeBootstrap` MonoBehaviour + `ForgeLocator` runtime DI | ✅ |
| `Assets/Scripts/Ui/Foundation/{IUiSurface,IUiPanel,UiTokens,UiSurfaceLocator}` scaffolding (uncommitted on this branch — use as-is or rewrite to equivalent quality) | 📝 |

You **do not** need to re-prove any of the above. Build on it.

================================================================================
3. ZORUNLU READ-ONLY KEŞİF (run first, paste outputs into the kickoff report)
================================================================================

```bash
# 3.1 Branch + base state
git status
git log --oneline origin/main..HEAD                  # commits unique to this branch
git log --oneline -10                                 # recent local commits

# 3.2 Ember editor menu surface (the user does not know what is in there)
grep -rn "MenuItem.*Ember" Assets/Editor/ | sort

# 3.3 Existing scenes (you must NOT migrate MainMenu/HUD/dialog UGUI in v1)
find Assets/Scenes -name "*.unity" | sort
grep -l "MainMenuCanvas\|UIDocument" Assets/Scenes/Ember/*.unity 2>/dev/null

# 3.4 ForgeLocator registration order (where to hook generation triggers)
grep -rn "ForgeLocator\.Register\|ForgeLocator\.Current" Assets/Scripts/

# 3.5 PromptComposers static surface (what already exists for NPC portraits)
grep -A10 "public static.*PromptComposers" Assets/Scripts/Simulation/Forge/PromptComposers.cs

# 3.6 Worldgen domain events you must surface in the UI
grep -rn "event \|EventHandler\|public.*Action<" Assets/Scripts/Simulation/Worldgen/ | head -30

# 3.7 NativeLlmClient surface (for NpcPromptJson — §9)
grep -n "public" Assets/Scripts/Simulation/AiDm/NativeLlmClient.cs | head -20

# 3.8 Confirm Unity-MCP grep regression
test -f Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe \
  && echo "rg present" \
  || echo "rg ABSENT — §14.4 fix needed"
```

**No file changes** until those 8 outputs are in your kickoff report
(`Reports/visible-generation-cutover-kickoff_<unix>.md`).

================================================================================
4. HEDEF AKIŞ (the user-visible end state)
================================================================================

`start.exe` → `Boot.unity` (Build Settings index 0) → `BootBootstrap.Awake` mounts the new UI surface and runs `AssetManifestScanner` → if everything cached, 1 second "Ready" log → `MainMenu.unity`.

If anything missing: **AssetGenerationScreen** plays sequentially through every missing entry. Per entry: current label, current prompt, thumbnail-as-it-bakes, progress 0..1, per-step log line. Failures: red icon, `[error]` log line, append row to `Logs/generation-failures.json`, **continue**. End-of-loop screen says "Generation complete: X/Y succeeded. Z failed (see Logs/...)." with `Continue to Main Menu` button.

`New Game` button → second AssetGenerationScreen pass scoped to the chosen scenario's manifest entries → `CharacterCreation.unity` with a new overlay canvas. Steps:

1. **SkillPickStep** — modal, 12 skills, pick 3, log `[choice] Picked: stealth, smithing, lore.`
2. **AttributeRollStep** — for each of 6 attributes, **DiceRollWidget** rolls 4d6-drop-lowest. User sees 4 dice spin, lowest grayed out, sum displayed. Log `[roll] STR = 4+5+6+(2) = 15.`
3. **BackgroundChooseStep** — modal, 8 backgrounds, one-line flavor, pick one. Log `[choice] Background: smuggler.`
4. **PortraitPreviewStep** — composes `NpcPromptJson` from picks + world style, requests LLM via NativeLlmClient, validates JSON strict-mode, renders portrait (chosen archetype silhouette + RGB recolor + optional ONNX refinement). User can re-roll ≤3 times then locks.

`Begin Game` → **LoadingScreen** with **WorldgenView** overlay: every domain event becomes a log line. RegionGenerated, SettlementSeeded, NpcSeeded (with the LLM JSON inline pretty-printed), DiceRolled (reason + face + value), QuestionRaised (modal pause; user picks; log the choice), Failure. Auto-advance toggle + manual `Continue`. WorldgenView log slot is a virtualized scroll list (10k+ lines).

When `WorldgenService` completes → "World ready — entering {startScene}" → 1s → `SceneManager.LoadSceneAsync(startScene)`.

**Same UI tokens, same prefab library, same theme on every screen.**

================================================================================
5. MİMARİ KURALLAR
================================================================================

SOLID. Her dosya bir iş yapar. 800 satırlık tek dosya YOK. 200 satırı geçen bir dosya varsa neden olduğunu inline yorum olarak yaz.

PRD §5'teki dosya struktur tablosu **birebir**. Sapacaksan PR yorumunda gerekçeli yaz.

Composition root invariants:

- Game code **asla** `new UiToolkitSurface(...)` veya `new OnnxAssetForge(...)` etmez. Her zaman:
  - `UiSurfaceLocator.Current`
  - `ForgeLocator.Current`
  - `LlmRoutingService` (zaten var)
- **Static asset prompts designer-authored** (`StaticPromptCatalog`). LLM static prompt yazmıyor. Bu kural sert: bir LLM çağrısı static prompt üretiyorsa kod review reddeder.
- LLM **yalnızca** `NpcPromptJson` üretir (PRD §9). `NpcPromptJsonValidator` strict mode reddederse retry 1×, sonra deterministic default JSON (`DeriveDefaultJsonFromSeed(npcSeed)`).
- **Failure policy: skip + log + continue.** `Logs/generation-failures.json` JSON-lines, append-only. Pipeline **asla throw etmez**. Throwing aborts the boot — that is the bug, not the feature.
- Long IO: `CancellationToken` + timeout zorunlu. Forge default 5 dk/asset (`AssetGenerationRequest.TimeoutSeconds` field ekle, OnnxAssetForge respect etsin).
- Module-level mutable global yok. Locator'lar register-once + clear-on-dispose.

================================================================================
6. UI BACKEND STRATEJİSİ
================================================================================

Default backend: **UI Toolkit** (Unity 6 native, USS, UXML). Boot + Loading + Worldgen + CharacterCreation overlay buradan inşa edilir.

**Mevcut UGUI canvas'ları (MainMenuCanvas, in-scene HUD, dialog) DOKUNULMAZ** v1'de. Sadece `Boot.unity` yeni; `CharacterCreation.unity` yeni bir overlay GameObject alır (mevcut canvas üstte; overlay daha yüksek sorting layer'da).

**Abstraction katmanı zorunlu** (`IUiSurface` / `IUiPanel` / `UiTokens`) tek backend yollamış olsak bile. Gelecek Unity LTS UI Toolkit'i kırarsa, ikinci bir backend (UGUI veya Web/CSS via Noesis/ReactUnity) eklenir; **hiçbir screen rewrite olmaz**. Maliyet ~70 satır, kazanç teknik bağımsızlık.

Tokens: `Assets/Manifests/DefaultUiTokens.asset` (UiTokens ScriptableObject instance). Dark default. Bir extra theme = duplicate asset.

================================================================================
7. ASSET MANIFEST
================================================================================

İki manifest, ikisi de hand-authored ScriptableObject, ikisi de git'te.

### 7.1 CoreAssetManifest.asset

~50 entry, oyunun açılması için gerekli. PRD §7.1 kategori dağılımı:

| Kategori | ~Adet | Örnekler |
|---|---|---|
| UI ikonları | 15 | new_game, settings, dice, skill, attack, defend, equip, drop |
| Fontlar | 3 | body, heading, monospace (referans, generate edilmez) |
| Generic silhouettes | 6 | on-disk PNG'ler (referans, generate edilmez) |
| Item icon örnekleri | 10 | sword, bow, staff, potion, scroll, key, ring, helm, boots, shield |
| Spell icon örnekleri | 6 | sleep, heal, fire, ice, shield_spell, lightning |
| Core sesler | 5 | ui_click, ui_hover, dice_roll, level_up, error |
| Logo/splash | 3 | logo_full, logo_compact, splash_background |

Entry alanları: `id`, `category`, `expectedPath` (Assets/-relative), `staticPromptKey`, `dimensions` (W,H), `requiresGeneration` (bool).

### 7.2 GenericNpcBaseManifest.asset

Per-archetype entry: `archetypeId`, `silhouettePath`, `huePaletteMin/Max`, `saturationRange`, `lightnessRange`, `notes`.

**Sadece on-disk olan 6 silhouette** valid `silhouettePath` ile gelir. fairy/dragon/elemental gibi PRD §2'de listelenmemiş archetype'lar `silhouettePath = ""` + `requiresGeneration = true`. (Codex PR #208 yorumu — atla.)

### 7.3 Scanner

`AssetManifestScanner.ScanAsync(core, npcBase, AssetForgeCache, ct) → ManifestScanReport`. Idempotent. PRD §7.3 record şeması.

================================================================================
8. STATIC PROMPT KALİTESİ
================================================================================

PRD §8. Her static prompt:

- `EmberStyleHeader` const ile başlar. Önerilen:
  > `"dark-fantasy ember-warm palette, painterly low-saturation, 1024x1024, transparent background, single subject centered"`
- Konkret nouns/adjectives. **İYİ**: `"a wrought-iron longsword with rune-etched fuller, hilt wrapped in oxblood leather"`. **KÖTÜ**: `"a sword"`.
- Negative constraints: `"no text, no watermark, no border, no UI elements"`.
- Reproducible: `(prompt, seed, dimensions, model)` → same image.

Kalite gate: 3 sample render üret (1 UI icon + 1 silhouette + 1 item), `Reports/screens/sample_{id}.png` paste, @msbel5 subjective review eder.

================================================================================
9. LLM JSON CONTRACT (dynamic NPC portraits)
================================================================================

PRD §9. Schema:

```json
{
  "archetype_id": "humanoid_male",
  "primary_hue_degrees": 28,
  "secondary_hue_degrees": 215,
  "mood_keywords": ["wary", "soot-stained"],
  "distinctive_features": ["scar across left eye", "iron earring"],
  "clothing_style": "leather jerkin",
  "accessory": "talisman pendant",
  "world_style_anchor": "ember-warm"
}
```

`NpcPromptJsonValidator` strict:

- `archetype_id` ∈ `GenericNpcBaseManifest`
- Hue ∈ [0, 360), integer
- String arrays max 5 entries × max 40 chars each, ASCII only
- Unknown fields → reject
- Validation fail → retry 1× with `"the previous response was invalid because <reason>; respond ONLY with valid JSON"`
- Second fail → `DeriveDefaultJsonFromSeed(npcSeed)` (deterministic)

`LlmPromptComposer.Compose(NpcPromptJson) → string`. **Deterministic**. JSON'dan sonra LLM çağrısı YOK.

================================================================================
10. BOOT FLOW
================================================================================

PRD §10. Yeni scene `Assets/Scenes/Ember/Boot.unity` Build Settings index 0.

`BootBootstrap.Awake` sırası:

1. `UiSurfaceLocator.Current` null ise default `UiToolkitSurface` instantiate + register.
2. `UiSurfaceLocator.Current.Mount("BootScreen")` → panel handle.
3. `AssetManifestScanner.ScanAsync(...)` → live slots: `total`, `cached`, `missing`.
4. `Missing == 0 && RequiresGeneration == 0` → 1s `[ok] Ready` → `SceneManager.LoadSceneAsync("MainMenu")`.
5. Aksi → `VisibleGenerationPipeline.RunAsync(missingEntries, ct)`:
   - `OnEntryStart(entry)` → log `[gen] {entryId} — {prompt[0..80]}…`
   - `OnEntryProgress(entry, t)` → progress slot
   - Bytes geldikçe decode et + `current_thumbnail` slot'a push et (kullanıcı resmi görür yaratılırken)
   - `OnEntrySuccess(entry, elapsed)` → log `[ok] {entryId} ({elapsedMs}ms)` + small thumbnail grid'e ekle
   - `OnEntryFailure(entry, reason)` → log `[error] {entryId} — {reason}` + JSON-lines satırı + **continue**
6. Loop bitti → "Generation complete: {ok}/{total}. {failed} failed (see Logs/generation-failures.json)." + `Continue` button → MainMenu.

================================================================================
11. LOADINGSCREEN
================================================================================

PRD §11. Static façade:

```csharp
public static class LoadingScreen
{
    public static void Show(string title, string subtitle);
    public static void SetProgress(float normalized, string currentLabel);
    public static void LogLine(UiLogSeverity severity, string line);
    public static void ShowThumbnail(Texture2D texture, string caption);
    public static void Hide();
}
```

Backed by `LoadingScreenController` (MonoBehaviour, `DontDestroyOnLoad`, locator-registered). Boot'un "Loading MainMenu…" beat'inde + New Game worldgen narrasyonunda + her scene transition'da kullanılır. **Tek implementation, tek API.**

================================================================================
12. NEW GAME + CHARACTERCREATION
================================================================================

PRD §12. Mevcut `New Game` button click handler değiştirilmez — yeni bir subscriber eklenir.

CharacterCreation.unity'ye yeni GameObject `UiCanvasOverlay` (yüksek sorting layer). `CharacterCreationController` step'leri sırayla yönetir (PRD §12 listesi). Her step **kendi modal panelini** mount eder (`UiSurfaceLocator.Current.Mount(...)`), kullanıcı seçim yapar, controller advance eder. Log slot her seçimi/zarı yansıtır.

`Begin Game` → `LoadingScreen.Show("Building world…", "")` → `WorldgenViewController.Mount(loadingScreen)` → `WorldgenService.RunAsync(...)`.

================================================================================
13. WORLDGEN VISIBLE
================================================================================

PRD §13. `WorldgenService` (domain) **değişmez**. Sadece event subscriber eklenir.

Event-to-UI mapping (PRD §13'te tam liste):

- `RegionGenerated` → `[region] Generated {regionId}` + small map sketch slot update
- `SettlementSeeded` → `[settlement] {regionId}/{settlementId}`
- `NpcSeeded` → `[npc] {npcSeed.Name} — {archetype}` + JSON inline pretty (`[llm-json] {...}`)
- `DiceRolled` → `[dice] {reason}: d{faces} = {value}`
- `QuestionRaised` → modal pause; user picks option; log `[choice] {question.Id}: {answer}`. Auto-advance ON ise 1.5s preview sonrası ilk seçeneği tıklar.
- `Failure` → red log line + JSON-lines row

`WorldgenLogPanel` virtualized scroll (10k+ lines). Pause auto-scroll button.

Worldgen complete → "World ready — entering {startScene}" → 1s → `SceneManager.LoadSceneAsync(startScene)`.

================================================================================
14. EDITOR MENUS + MAINTENANCE
================================================================================

### 14.1 New editor menu entries

- `Ember/Forge/Scan Missing Assets` — Editor'de `AssetManifestScanner` çalıştırır, `ManifestScanReport`'u Console'a tablo olarak basar.
- `Ember/Forge/Generate Core (Editor preview)` — §10 boot flow'unu Editor Game view'inde oynatır (restart gerekmez).

### 14.2 Existing menus (untouched)

`Ember/Build Scene/*`, `Ember/Build/*`, `Ember/Capture/*`, `Ember/Forge/Generate world assets`, `Ember/Forge/Generate Fresh World Assets` — none modified.

### 14.3 Build Settings

`Boot.unity` index 0. `Ember/Build/Add All Scenes To Build Settings` re-run sonrası Boot 0'da kalmalı (idempotent).

### 14.4 Unity-MCP rg regression fix

`Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe` PR #207 sırasında `~` folder trimi ile silindi (114 MB hard limit). İki seçenekten biri:

**A. ZIP + InitializeOnLoad unzip** (önerilen): `Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe.gz` (~2 MB) commit; `Assets/Editor/Ember/Patches/RestoreRipgrep.cs` `[InitializeOnLoad]` Editor compile'ında unzip eder hedefe.

**B. Stub `rg.cmd`** sistem PATH'inden çağırır (`where rg`); rg sistemde yoksa graceful fallback.

Implementation review'da hangi daha basit kazanır, gerekçeli karar.

================================================================================
15. TESTLER (BEFORE CODE, EVERY TIME)
================================================================================

PRD §15. EditMode + PlayMode.

### EditMode (must pass)

```
UiTokensTests                       — every color non-default, severity→color map covers enum
UiSurfaceLocatorTests               — Register/Clear, double-register throws, Current returns last
CoreAssetManifestTests              — loads, every entry non-empty staticPromptKey, no duplicate id
GenericNpcBaseManifestTests         — on-disk silhouettes have entries; requiresGeneration entries empty path
AssetManifestScannerTests           — empty cache → all Missing; full cache → all Cached; idempotent
StaticPromptCatalogTests            — every key resolves; every prompt starts with EmberStyleHeader; non-empty
NpcPromptJsonValidatorTests         — valid accepts; unknown field rejects; out-of-range hue rejects; oversize rejects; non-ASCII rejects
GenerationFailureLogTests           — append grows file by 1 valid JSON line; reopen preserves
VisibleGenerationPipelineTests      — fake forge + 3 entries → events fire in order; one failure does not stop loop
```

### PlayMode (must pass)

```
BootSceneTest                       — load Boot.unity with fake IAssetForge → mount, 3 fake entries, 3 success lines, transition to MainMenu
LoadingScreenApiContractTest        — Show → SetProgress → LogLine → ShowThumbnail → Hide round-trip; DontDestroyOnLoad survives a LoadScene
CharacterCreationFlowTest           — load CharacterCreation.unity, drive controller.Advance() through 4 steps; log slot has 1 line per choice + 1 per attribute roll
WorldgenViewVisibleTest             — mock worldgen with 2 regions, 3 settlements, 5 NPCs, 1 question; log line counts correct; modal opens; advances on Answer(1)
```

### Forge regression

PR #207 + PR #212 fix'leri (OnnxAssetForge_DeterministicSeed_ProducesSameOutput, OnnxAssetForge_DifferentSeed_ProducesDifferentOutput, +SDXL/SD15 ClampDimension snap) yeşil kalmalı.

================================================================================
16. ACCEPTANCE (BLOCKING — PR #210 ready-for-review öncesi her madde ✅)
================================================================================

**A. Compile / static**

- [ ] Unity 6.3.13f1: 0 compile error, max 5 önceden bilinen warning
- [ ] EditMode + PlayMode tests yeşil (§15)
- [ ] Unity-MCP `grep` tool çalışıyor (§14.4 fix verify)

**B. Boot flow (visible)**

- [ ] `Builds/Windows64/alcyone-ember-rpg.exe` çift tıkla → Boot scene first
- [ ] `Logs/generation-failures.json` silinmiş + cache boş iken: boot screen 50 core entry'yi visible işler (prompt + thumbnail + log + status icon)
- [ ] Cache full iken: `cached = X/X`, ≤1s, `Ready`, MainMenu
- [ ] Forge'a deliberate exception inject → entry red + JSON-lines + **loop continues**

**C. New Game flow (visible)**

- [ ] `New Game` → (scenario gen if needed) → CharacterCreation modal sequence
- [ ] Skill pick → 6 attribute roll (dice animation + drop-lowest grayed + sum) → background pick → portrait preview with LLM JSON visible in log
- [ ] `Begin Game` → LoadingScreen worldgen log scrolling with regions + settlements + NPCs + at least 1 question modal

**D. UI consistency**

- [ ] Boot / Loading / CharacterCreation overlay / Worldgen overlay — hepsi `UiTokens`'tan stil alır
- [ ] Bir token (örn. `Accent`) değiştirildiğinde 4 screen aynı anda değişir (manuel verify)
- [ ] `Assets/Scripts/Ui/Backends/` dışında UI Toolkit veya UGUI types'a direct referans YOK

**E. Failure semantics**

- [ ] 3 silhouette PNG'i geçici rename → 3 red log + 3 JSON-lines satır + boot devam ediyor (halt yok)

**F. Git**

- [ ] `feat/visible-generation-cutover` üzerinde küçük focused commit'ler (örn: "manifest scaffold", "boot scene + asset gen", "worldgen overlay", "char creation steps", "tests")
- [ ] PR #210 draft → Ready for review (yalnızca acceptance §16 hepsi ✅ olunca)
- [ ] **DO NOT MERGE** — @msbel5 review eder
- [ ] Hiç `git push --force` çalıştırılmadı (rebase ihtiyacı varsa: önce @msbel5 yorum, sonra `--force-with-lease`)

**G. Report**

- [ ] `Reports/visible-generation-cutover_<unix>.md` yazıldı + commit'lendi + içerikte §17 gerekleri

================================================================================
17. ÇALIŞMA STİLİ + REPORT
================================================================================

### Hard rules

- Önce kısa plan (≤20 satır), sonra §3 read-only discovery, sonra kickoff report, sonra test, sonra kod, sonra commit. **Bu sırayı bozma.**
- 200 satırı geçen .cs dosyasının başına `// Why this file is intentionally long: <reason>` yorum.
- Static asset prompts: hand-authored. LLM yazmıyor. Bu sert sınır.
- LLM yalnızca `NpcPromptJson` schema (§9). Free-text LLM çağrısı yok.
- Failure: skip + log + continue. **Throw aborts the boot.** Throwing = bug.
- `CancellationToken` + timeout her async IO için.
- Existing UGUI canvases (MainMenu, HUD, dialog) DOKUNULMAZ. Sadece `Boot.unity` yeni; `CharacterCreation.unity` overlay alır.
- `git push --force` YOK (rebase için → @msbel5 onayı → `--force-with-lease`).
- `.gitignore Packages/*/` rule + ai.assistant exception DOKUNULMAZ.
- Trading bot, OpenClaw, external systems, embedded ai.assistant DLL'leri: DOKUNULMAZ.
- "Yaptım" deme. Komut + çıktı + screenshot path + test sayısı koy. **Her iddianın kanıtı reportta var.**
- BLOCKED isen: PR #210 yorumu olarak `BLOCKED: <one-line question>` yaz, fallback varsa uygula, yoksa @msbel5 mention.

### Final report template (`Reports/visible-generation-cutover_<unix>.md`)

```markdown
# Visible Generation Cutover — Final Report

## Commits (ordered)
<list of SHAs + one-line subjects>

## Files (count + path tree)
<output of: git diff --stat origin/main..HEAD | tail>

## §3 Discovery output
<paste of every command from §3 + its output>

## Tests
- EditMode: <pass>/<total>, <fail count> failing, output paste below
- PlayMode: <pass>/<total>, <fail count> failing, output paste below
- Forge regression suite: <pass count>/24 (target: 24/24)

## §16 Acceptance (every item ✅ or ❌ with evidence path)
A. Compile/static ...
B. Boot flow ...
C. New Game flow ...
D. UI consistency ...
E. Failure semantics ...
F. Git ...
G. Report ...

## Screenshots (paths)
- Boot screen: Reports/screens/boot_{ts}.png
- AssetGen (3 deliberate failures): Reports/screens/assetgen_failures_{ts}.png
- CharacterCreation skill pick: Reports/screens/cc_skill_{ts}.png
- CharacterCreation dice roll: Reports/screens/cc_dice_{ts}.png
- CharacterCreation portrait preview: Reports/screens/cc_portrait_{ts}.png
- Worldgen question modal: Reports/screens/worldgen_question_{ts}.png

## Failure log diff
- before/after deliberate failure injection: Reports/diffs/failure_log_{ts}.diff

## Recommended next step
<one paragraph: what should @msbel5 do after merging this PR>
```

### Tokens / secrets

Asla yazma. `QA_FIGMA_TOKEN`, Unity license, GitHub PAT, environment values → loglanmaz; report'ta `<set>` veya hash redact.

---

**Eğer PRD'de cevaplanmamış bir nokta varsa: PR #210 yorumu olarak `BLOCKED: <question>` yaz; @msbel5 yanıtlasın; bu PR'da resolve et — yarım iş bırakma.**

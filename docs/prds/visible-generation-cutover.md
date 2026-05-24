# PRD-V2 — Visible Generation & Consistent UI Cutover (Ember CRPG)

**Status:** Authored 2026-05-24, awaiting @msbel5 approval before Codex hand-off
**Branch:** `feat/visible-generation-cutover`
**Supersedes:** `docs/prds/visible-generation-and-consistent-ui.md` (the 6-phase split was a mistake; this PRD is one branch, one PR, one cutover)
**Owner of execution:** decided in §17 — Codex single-mission **OR** Claude + msbel together
**Branch policy:** never merge before @msbel5 explicit review

---

## 0. ÇALIŞMA KLASÖRÜ / REPO / BRANCH

```
Windows working directory:
C:\Users\msbel\projects\alcyone-ember-rpg

Canonical GitHub repo:
msbel5/alcyone-ember-rpg

Active base branch (acts as "main" until rename):
codex/sdxl-pipeline-and-naming-refactor

This PRD's branch:
feat/visible-generation-cutover

Unity Editor version (project-pinned):
6000.3.13f1 — see ProjectSettings/ProjectVersion.txt

Unity Editor install location (per-machine):
E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe
```

---

## 1. VİZYON / KARAR

Ember CRPG'nin asset üretimi ve worldgen şu an **görünmez**:

- Toplu batch generation ekrana hiçbir şey basmıyor — kullanıcı 800 asset'in hangi 750'sinin bittiğini, hangi 50'sinin neden patladığını bilmiyor.
- Worldgen "soru sorduğunu" söylüyor ama hiç soru ekrana çıkmıyor: skill seçimi yok, zar atma görünmüyor, dünya history log'u akmıyor.
- Character Creation "bombos" — oyuncunun adım adım geçeceği akış yok.
- UI sahneden sahneye tutarsız; ortak design token / prefab kütüphanesi yok.

**Bu PRD'nin tek hedefi:** kullanıcı `start.exe` yaptığında, sonra `New Game` bastığında, sonra `Character Creation` adımlarını geçtiğinde, **her aşamada ne olduğunu canlı olarak gören** bir oyun teslim etmek. Tek branch, tek PR, hepsi birlikte.

Phase'lara bölmek YOK. Her şey `feat/visible-generation-cutover` üzerinde gelişir, tek bir PR'da review edilir, tek seferde merge olur.

---

## 2. MEVCUT REPO DURUMU (already discovered)

| | |
|---|---|
| Unity Editor | 6000.3.13f1 (Beta), DX12 backend, projeyi açıyor |
| `Packages/com.unity.ai.assistant/` | Embedded, SigLip2Text.cs:59 patched (PR #207, merged) |
| `Assets/Scripts/Simulation/Forge/` | Domain: OnnxAssetForge, AssetForgeCache, AssetForgeQueue, SdxlTurboPipeline, Sd15LcmPipeline, OnnxModelBundle, ClipBpeTokenizer, OnnxSessionFactory, OnnxPngEncoder, PromptComposers |
| `Assets/Scripts/Presentation/Ember/Forge/` | Runtime: ForgeBootstrap (MonoBehaviour, Awake'de ForgeLocator.Register), ForgeLocator, ModelBootstrap, ComfyUiAssetForge |
| `Assets/Editor/Ember/Menu/ForgeMenu.cs` | `Ember/Forge/Generate world assets` — current world's NPC portraits, SDXL→SD1.5 fallback, disk cache |
| `Assets/Editor/Ember/Forge/WorldgenSmokeTest.cs` | `Ember/Forge/Generate Fresh World Assets` — generates a fresh world first, then assets |
| `Assets/Editor/Ember/Menu/` | 20 menu entries: 12 scene builders, 3 build pipeline, 2 capture, 2 forge |
| `Assets/Art/BodySilhouettes/` | 6 PNGs: humanoid_male, humanoid_female, beast_quadruped, undead_humanoid, construct, aberration (verified on disk) |
| `Assets/Scripts/Simulation/Worldgen/` | Pure-Domain WorldgenService, RegionId/SettlementId/NpcId, deterministic |
| `Assets/Scripts/Simulation/AiDm/` | NativeLlmClient (LLamaSharp), EmbeddingClient |
| `Assets/Scripts/Ui/Foundation/` (this branch only, not yet committed) | IUiSurface, IUiPanel, UiTokens, UiSurfaceLocator, asmdef |
| Failing tests prior to PR #207 | OnnxAssetForgeTests.OnnxAssetForge_DeterministicSeed_ProducesSameOutput, OnnxAssetForgeTests.OnnxAssetForge_DifferentSeed_ProducesDifferentOutput — fixed |
| Console warnings (post-PR #207) | 5 total, all benign: 1 Input Manager deprecation, 1 admin Unity, 1 RenderTexture cleanup, 2 ai.assistant TMP CanvasRenderer |
| Unity-MCP grep regression | `Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe` removed when we trimmed `~` folders; Unity-MCP grep tool can't find rg. Fix scope: §3 step 6. |

---

## 3. READ-ONLY KEŞİF KOMUTLARI (Codex / Claude must run before changing anything)

```bash
# 3.1 Repo state
git status
git branch --show-current
git log --oneline origin/codex/sdxl-pipeline-and-naming-refactor -10

# 3.2 Forge editor menu surface
grep -rn "MenuItem.*Ember" Assets/Editor/

# 3.3 Existing UI canvases (so we know what NOT to refactor in v1)
find Assets/Scenes -name "*.unity" | while read f; do
  printf "%s: " "$f"
  grep -c "m_Component:" "$f" 2>/dev/null
done

# 3.4 Existing prefabs that ship UI
find Assets -name "*.prefab" | xargs grep -l "Canvas\|UIDocument" 2>/dev/null | head -20

# 3.5 ForgeBootstrap registration order
grep -rn "ForgeLocator\.Register\|ForgeLocator\.Current" Assets/Scripts/

# 3.6 Unity-MCP rg dependency check — does our trim actually break grep?
find Packages -name "rg_win.exe" -o -name "rg" 2>/dev/null
# If empty: that confirms the regression. Restore policy in §14.4.

# 3.7 PromptComposers static surface
grep -A5 "public static.*PromptComposers" Assets/Scripts/Simulation/Forge/PromptComposers.cs
```

Output of every command goes into the kickoff report (see §16). Do not modify any file until the report is written.

---

## 4. HEDEF PRD

When `start.exe` runs, this is what the user sees, end to end:

1. **Boot scene loads first** (added to Build Settings position 0). Plays the project's design system from frame one.
2. **AssetGenerationScreen** opens. The scanner checks `CoreAssetManifest` against disk. If everything is cached → 1s "Ready" → Main Menu. If anything is missing → live grid of every missing asset, each tile transitions queued → generating → cached/failed in real time, with a thumbnail of the produced image as it bakes, and a scrolling log on the side.
3. **Main Menu** loads. `New Game` button is enabled.
4. **`New Game`** → if scenario-specific assets are missing, AssetGenerationScreen plays again for just those (lazy). Then **Character Creation** opens.
5. **Character Creation** — every step visible: skill picks, attribute rolls (dice animation + final value + history line in the log), background choice, portrait preview (synthesized live from generic silhouette + LLM JSON for traits + RGB recolor).
6. **`Begin Game`** → **LoadingScreen** with worldgen narrative log: each region generated, each settlement seeded, each NPC seeded, each dice roll, each LLM JSON received. Auto-advance toggle + manual `Continue`.
7. Same UI system used at every step. Same tokens, same prefabs, same theme.

When a generation step fails the UI shows a red icon, appends an `[error]` line to the log, writes a row to `Logs/generation-failures.json`, and continues. No silent failures, no halts.

---

## 5. MİMARİ KURALLAR

SOLID. Each file does one thing. No 800-line files.

Required structure on this branch (final, after merge):

```
Assets/
  Scripts/
    Ui/
      Foundation/                       (this branch — already drafted)
        IUiSurface.cs                   abstraction — every screen uses this
        IUiPanel.cs                     panel handle abstraction
        UiTokens.cs                     ScriptableObject design tokens
        UiSurfaceLocator.cs             DI registry (ForgeLocator-style)
        EmberCrpg.Ui.Foundation.asmdef
      Backends/UiToolkit/               UI Toolkit concrete backend
        UiToolkitSurface.cs
        UiToolkitPanel.cs
        UiToolkitPanelBindings.cs       slot→element resolver
        EmberCrpg.Ui.Backends.UiToolkit.asmdef
      Screens/
        Boot/
          BootBootstrap.cs              MonoBehaviour on the Boot scene root
          AssetGenerationScreenController.cs
        Loading/
          LoadingScreen.cs              static façade Show/Hide/Progress/Log/Thumbnail
          LoadingScreenController.cs
        Worldgen/
          WorldgenViewController.cs     modal overlay
          WorldgenQuestionModal.cs
          DiceRollWidget.cs
          WorldgenLogPanel.cs
        CharacterCreation/
          CharacterCreationController.cs
          SkillPickStep.cs
          AttributeRollStep.cs
          BackgroundChooseStep.cs
          PortraitPreviewStep.cs
        EmberCrpg.Ui.Screens.asmdef
    Generation/
      Manifest/
        CoreAssetEntry.cs               record type
        CoreAssetManifest.cs            ScriptableObject (list of CoreAssetEntry)
        GenericNpcArchetypeEntry.cs
        GenericNpcBaseManifest.cs       ScriptableObject (archetype list)
        AssetManifestScanner.cs         diff manifest vs cache
        ManifestScanReport.cs
        EmberCrpg.Generation.Manifest.asmdef
      Prompts/
        StaticPromptCatalog.cs          hand-authored prompts per CoreAssetEntry
        LlmPromptComposer.cs            consumes NPC seed + world style → JSON request → composed string prompt
        NpcPromptJson.cs                strict JSON DTO (see §9)
        NpcPromptJsonValidator.cs
        EmberCrpg.Generation.Prompts.asmdef
      Pipeline/
        VisibleGenerationPipeline.cs    orchestrator: takes a list of entries, drives Forge sequentially, raises UI events
        GenerationFailureLog.cs         appends to Logs/generation-failures.json
        EmberCrpg.Generation.Pipeline.asmdef
    Presentation/Ember/Forge/
      ForgeRuntimeTrigger.cs            new — runtime entry point that BootBootstrap calls
                                        (existing ForgeBootstrap/Locator unchanged)
  Tests/
    EditMode/
      Ui/                                tokens validator, locator register/clear
      Generation/                        manifest scan idempotency, failure log shape, prompt catalog completeness, JSON validator
      Pipeline/                          orchestrator drives Forge in sequence (with fake forge)
    PlayMode/
      Boot/                              Boot scene → generation runs → continue button → Main Menu (mocked forge)
      LoadingScreen/                     public API contract test
      Worldgen/                          full worldgen run with overlay UI (mocked services)
      CharacterCreation/                 each step renders, user can advance
  Manifests/
    CoreAssetManifest.asset              hand-authored — ~50 entries
    GenericNpcBaseManifest.asset         hand-authored — 6 archetypes (real silhouettes only)
  Scenes/
    Ember/
      Boot.unity                          NEW — Build Settings index 0
      MainMenu.unity                      (existing, untouched)
      CharacterCreation.unity             (existing, gains UiCanvasOverlay component)
      …other gameplay scenes unchanged
Assets/Editor/Ember/Menu/
  ForgeMenu.cs                            (existing, untouched)
  ScanManifestMenu.cs                     NEW — Ember/Forge/Scan Missing Assets
  GenerateCoreMenu.cs                     NEW — Ember/Forge/Generate Core (Editor preview of boot flow)
```

Composition root rules:

- **Game code never new()'s a backend.** Surface obtained via `UiSurfaceLocator.Current`; Forge via `ForgeLocator.Current`; LLM via `LlmRoutingService` (already exists).
- **Static asset prompts are designer-authored**, stored in `StaticPromptCatalog`. The LLM never composes prompts for static assets. The LLM only authors **NpcPromptJson** (a strict, schema-validated JSON DTO — see §9) for dynamic NPC portraits, which `LlmPromptComposer` then folds into a final prompt template.
- **Failure policy: skip + log + continue.** `GenerationFailureLog` appends one JSON row per failure; pipeline never throws. UI shows red icon, log line `[error]`.
- **No module-level mutable global state.** Locators hold one reference, registered once, cleared on dispose.
- **Network/long IO requires a CancellationToken and a timeout.** Default Forge timeout: 5 minutes per asset (`AssetGenerationRequest.TimeoutSeconds`, add field).
- **Logs are append-only.** `Logs/generation-failures.json` is a JSON-lines file, never overwritten.
- **No `~/.openclaw/...`-style hidden state paths.** Failures and reports live under the project's `Logs/` and `Reports/` directories (both already in `.gitignore`).

---

## 6. UI BACKEND STRATEJİSİ

**Default backend: UI Toolkit** (Unity 6 native, USS for tokens, UXML for layout). All Boot / Loading / Worldgen / Character Creation screens use it.

**Existing UGUI canvases stay UGUI in v1.** `MainMenuCanvas`, in-scene HUD, and dialog overlays are not refactored here. A separate later PR ports them once the new system is proven.

**Abstraction layer** (`IUiSurface` / `IUiPanel`) is mandatory even though we ship a single backend. Reason: when a future Unity LTS breaks UI Toolkit (precedent: every UI Toolkit minor has shipped breaking changes), we add a second backend without rewriting any screen. The abstraction is ~70 lines total; the cost is negligible.

**Stretch backend** (out of scope here, planned for a later PR): a Web/CSS backend via Noesis GUI or a packaged WebView, so the same screens can run in a browser-style devtools window.

Tokens live in `Assets/Manifests/DefaultUiTokens.asset` (ScriptableObject instance of `UiTokens`). Default theme is dark; one extra theme asset can be added by duplicating the file.

---

## 7. ASSET MANIFEST

Two manifests, both hand-authored ScriptableObjects, both checked into git.

### 7.1 CoreAssetManifest.asset

~50 entries the game cannot start without. Categories and rough counts:

| Category | Count | Examples |
|---|---|---|
| UI icons | ~15 | new_game, settings, dice, skill, attack, defend, equip, drop |
| Fonts | ~3 | body, heading, monospace (referenced, not generated) |
| Generic silhouettes | 6 | the verified set under `Assets/Art/BodySilhouettes/` |
| Sample item icons | ~10 | sword, bow, staff, potion, scroll, key, ring, helm, boots, shield |
| Sample spell icons | ~6 | sleep, heal, fire, ice, shield_spell, lightning |
| Core sounds | ~5 | ui_click, ui_hover, dice_roll, level_up, error |
| Logo / splash | ~3 | logo_full, logo_compact, splash_background |

Each entry has: `id`, `category`, `expectedPath` (relative to `Assets/`), `staticPromptKey` (looks up in `StaticPromptCatalog`), `dimensions` (W,H), `requiresGeneration` (bool — true for items that have no on-disk asset yet).

### 7.2 GenericNpcBaseManifest.asset

One entry per archetype. Each entry: `archetypeId`, `silhouettePath` (under `Assets/Art/BodySilhouettes/`), `huePaletteMin`, `huePaletteMax`, `saturationRange`, `lightnessRange`, `notes`. The six on-disk silhouettes (humanoid_male, humanoid_female, beast_quadruped, undead_humanoid, construct, aberration) are populated; others (fairy, dragon, elemental) get an entry with `silhouettePath = ""` and `requiresGeneration = true` so the scanner knows to queue them.

### 7.3 Scanner

`AssetManifestScanner.ScanAsync(CoreAssetManifest, GenericNpcBaseManifest, AssetForgeCache, CancellationToken)` returns `ManifestScanReport`:

```csharp
public sealed record ManifestScanReport(
    IReadOnlyList<EntryStatus> Entries,
    int Total,
    int Cached,
    int Missing,
    int RequiresGeneration,
    int FailedSinceLastScan);

public sealed record EntryStatus(
    string EntryId,
    string Category,
    EntryStatusKind Kind,    // Cached, Missing, RequiresGeneration, Failed
    string Reason);
```

Idempotent. Two scans without disk changes return identical reports.

---

## 8. STATIC PROMPT KALİTESİ

Static prompts are the hardest creative work and **must be hand-authored**. The LLM does not write them. They live in `StaticPromptCatalog` as a `Dictionary<string, string>`, keyed by `staticPromptKey` on the manifest entry.

Each static prompt must:

- be **explicit about style** (Ember CRPG's house style — define once in a header constant `EmberStyleHeader` and prefix every prompt with it; example `EmberStyleHeader = "dark-fantasy ember-warm palette, painterly low-saturation, 1024x1024, transparent background, single subject centered"`).
- describe the subject in **concrete nouns and adjectives**, never vague (`"a wrought-iron longsword with rune-etched fuller, hilt wrapped in oxblood leather"` not `"a sword"`).
- specify **negative constraints** when relevant (`"no text, no watermark, no border, no UI elements"`).
- be reproducible: same `(prompt, seed, dimensions, model)` → same image.

Acceptance for §8: the catalog is filled (every CoreAssetEntry has a non-empty static prompt), and a sample render of three distinct entries produces visibly Ember-style images (subjective check by @msbel5).

---

## 9. LLM JSON CONTRACT (dynamic NPC portraits)

For unique NPC portraits per playthrough, the LLM is asked to **produce JSON only**, never free-text prompts. The composer turns JSON into a prompt deterministically. This isolates the unreliable surface (LLM) from the reliable surface (prompt grammar).

Schema:

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

Constraints enforced by `NpcPromptJsonValidator`:

- `archetype_id` must match an entry in `GenericNpcBaseManifest`.
- Hue degrees must be integers in `[0, 360)`.
- Each string array max 5 entries, each entry max 40 chars, ASCII only (no LLM emoji injection).
- Unknown fields → reject (strict mode).
- Validation failure → request retried once with a "the previous response was invalid because <reason>; respond ONLY with valid JSON" follow-up. Second failure → fall back to a deterministic default JSON derived from the NPC seed.

`LlmPromptComposer.Compose(NpcPromptJson)` then produces a static prompt by string interpolation; no LLM call after this point.

---

## 10. BOOT FLOW

New scene `Assets/Scenes/Ember/Boot.unity`, placed at **Build Settings index 0**.

Single root GameObject `BootRoot` with `BootBootstrap` MonoBehaviour. On `Awake`:

1. Resolve dependencies via locators (Forge, Llm, UiSurface). If `UiSurfaceLocator.Current == null`, instantiate the default `UiToolkitSurface` first.
2. Mount panel `kind: "BootScreen"`.
3. Run `AssetManifestScanner.ScanAsync`. Update panel slots `total`, `cached`, `missing` live.
4. If `Missing == 0 && RequiresGeneration == 0` → wait 1s, append "Ready" log line, `SceneManager.LoadSceneAsync("MainMenu")`.
5. Else → for each entry in `Missing + RequiresGeneration` order, in sequence:
   a. Append log `[gen] {entryId} — {prompt[0..80]}…`
   b. Update panel slot `current_label = entryId`, `current_thumbnail = null`
   c. `await VisibleGenerationPipeline.GenerateOne(entry, ct)` — this calls Forge, updates the progress slot, and as bytes arrive decodes the texture and pushes it to `current_thumbnail` so the user watches it appear.
   d. On success: log `[ok] {entryId} ({elapsedMs}ms)`, append small thumbnail to the grid.
   e. On failure: log `[error] {entryId} — {reason}`, write `Logs/generation-failures.json` row, continue.
6. After loop: "Generation complete: {ok}/{total}. {failed} failed (see Logs/generation-failures.json)." + `Continue` button → `MainMenu`.

User-facing acceptance: the user can read every prompt, see every thumbnail, watch the count climb, and never wonder what is happening.

---

## 11. LOADING SCREEN LIBRARY

`LoadingScreen` is a static façade over `IUiSurface.Mount("LoadingScreen")`:

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

Backed by `LoadingScreenController` MonoBehaviour with `DontDestroyOnLoad`, registered to the `UiSurfaceLocator`. Reused by Boot (for the trailing "Loading Main Menu…" beat), by New Game (for worldgen narration), and by any future scene transition. One implementation, one API.

---

## 12. NEW GAME FLOW

In `MainMenu`, the existing `New Game` button now:

1. Computes the scenario-asset list via `AssetManifestScanner` filtered to category `scenario:{scenarioId}`.
2. If anything is missing → `AssetGenerationScreen` flow from §10 but scoped to this list.
3. Then load `CharacterCreation.unity`.

The `CharacterCreation` scene gains a `UiCanvasOverlay` GameObject that mounts the new UI on top of the existing scene (the existing UGUI canvas stays — the overlay is on a higher sorting layer). `CharacterCreationController` walks the steps in order:

1. `SkillPickStep` — modal lists 12 skills, user picks 3, log line `[choice] Picked: stealth, smithing, lore.`
2. `AttributeRollStep` — for each of 6 attributes, `DiceRollWidget` rolls 4d6-drop-lowest. Visible: 4 dice spin, drop animation removes lowest, sum displayed. Log line `[roll] STR = 4+5+6+(2) = 15.`
3. `BackgroundChooseStep` — modal lists 8 backgrounds with one-line flavor, user picks one. Log line `[choice] Background: smuggler.`
4. `PortraitPreviewStep` — composes `NpcPromptJson` from picks + world style, requests LLM, validates JSON, renders portrait using the chosen archetype silhouette + RGB recolor + (if LLM call succeeded) ONNX refinement. Shows the result, lets the user re-roll up to 3 times, then locks.
5. `Begin Game` button → `LoadingScreen` with the worldgen flow from §13.

---

## 13. WORLDGEN VISIBLE FLOW

`WorldgenViewController` mounts a panel `kind: "WorldgenView"` on top of the loading screen. As `WorldgenService` runs (no domain change), an event subscriber translates each domain event to a UI update:

- `RegionGenerated(regionId)` → `[region] Generated {regionId}` + small map sketch slot updates.
- `SettlementSeeded(settlementId, regionId)` → `[settlement] {regionId}/{settlementId}`
- `NpcSeeded(npcSeed)` → `[npc] {npcSeed.Name} — {archetype}` and if portrait JSON requested, log the JSON inline (`[llm-json] {pretty-printed}`).
- `DiceRolled(reason, faces, value)` → `[dice] {reason}: d{faces} = {value}`
- `QuestionRaised(question)` → modal pause, user picks an option, log `[choice] {question.Id}: {answer}`. The auto-advance toggle, when on, just clicks the first option after a 1.5s preview.
- `Failure(reason)` → red log line + `Logs/generation-failures.json` row.

`WorldgenLogPanel` is a virtualized scroll list (handles 10k+ lines). User can pause auto-scroll to read.

When `WorldgenService` completes, the screen shows "World ready — entering {startScene}" + 1s delay → `SceneManager.LoadSceneAsync(startScene)`.

---

## 14. EDITOR MENUS & MAINTENANCE

### 14.1 New menu entries (Editor only)

- `Ember/Forge/Scan Missing Assets` — runs `AssetManifestScanner` in Editor, dumps `ManifestScanReport` to the console as a table.
- `Ember/Forge/Generate Core (Editor preview)` — runs the §10 boot flow inside the Editor's Game view, useful for testing without restarting.

### 14.2 Existing entries (untouched)

`Ember/Build Scene/*`, `Ember/Build/*`, `Ember/Capture/*`, `Ember/Forge/Generate world assets`, `Ember/Forge/Generate Fresh World Assets` — none modified.

### 14.3 Build Settings

`Boot.unity` inserted at index 0 (auto-bootstrap on `start.exe`). `Ember/Build/Add All Scenes To Build Settings` updated to keep Boot at 0 even when re-run.

### 14.4 Unity-MCP grep regression fix

`Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe` is restored to a 7-Zip-compressed sibling and a `RestoreRipgrep.cs` `[InitializeOnLoad]` editor script unzips it into `ThirdParty~/ripgrep/` on first compile. Storing the zip (one file, ~2 MB) instead of the raw exe (~4 MB across three platforms) keeps git history thin and dodges the 100 MB GitHub limit that bit us last time. (Alternative: drop a stub `rg.cmd` in the same path that shells out to `where rg` — accepted if simpler in implementation review.)

---

## 15. TESTS

### 15.1 EditMode (must pass before any commit)

- `UiTokensTests` — every color non-default, severity→color mapping covers all enum values.
- `UiSurfaceLocatorTests` — Register/Clear, double-register throws, Current returns last registered.
- `CoreAssetManifestTests` — loads from disk, every entry has non-empty `staticPromptKey`, no duplicate ids.
- `GenericNpcBaseManifestTests` — every on-disk silhouette has an entry; entries marked `requiresGeneration` have empty `silhouettePath`.
- `AssetManifestScannerTests` — diff with empty cache → all `Missing`/`RequiresGeneration`; diff with full cache → all `Cached`; idempotent (two scans → identical reports).
- `StaticPromptCatalogTests` — every manifest `staticPromptKey` resolves; every prompt starts with `EmberStyleHeader`; every prompt non-empty.
- `NpcPromptJsonValidatorTests` — valid JSON accepts; unknown field rejects; out-of-range hue rejects; oversize string rejects; non-ASCII rejects.
- `GenerationFailureLogTests` — append → file grows by one valid JSON line; re-open → previous lines preserved.
- `VisibleGenerationPipelineTests` — given a fake forge and a 3-entry list, fires `OnEntryStart`/`OnEntryProgress`/`OnEntrySuccess`/`OnEntryFailure` in order; one failure does not stop the loop.

### 15.2 PlayMode (must pass before any commit)

- `BootSceneTest` — load `Boot.unity` with a fake `IAssetForge` returning placeholder bytes; assert `BootBootstrap` mounts the screen, runs through 3 fake entries, logs 3 success lines, and transitions to `MainMenu`.
- `LoadingScreenApiContractTest` — `Show → SetProgress → LogLine → ShowThumbnail → Hide` round-trip without exceptions; controller `DontDestroyOnLoad` survives a `SceneManager.LoadScene` call.
- `CharacterCreationFlowTest` — load `CharacterCreation.unity`, drive `SkillPickStep → AttributeRollStep → BackgroundChooseStep → PortraitPreviewStep` via `controller.Advance()`; assert the log slot has one line per choice and one per attribute roll.
- `WorldgenViewVisibleTest` — run a mocked worldgen with 2 regions, 3 settlements, 5 NPCs, 1 question; assert log slot has correct line counts, modal opens for the question, advances on `Answer(1)`.

### 15.3 Forge tests (regression)

All existing Forge tests must still pass. The two that were failing before PR #207 must remain green.

### 15.4 Manual acceptance (§16)

Listed under acceptance criteria.

---

## 16. ACCEPTANCE CRITERIA

A. **Compile / static**

- [ ] Unity 6.3.13f1 compiles with 0 errors and only the previously-known 5 benign warnings.
- [ ] All EditMode + PlayMode tests in §15 pass.
- [ ] Unity-MCP `grep` tool works again (regression fix §14.4 verified by listing one match).

B. **Boot flow (visible)**

- [ ] `start.exe` (Windows64 build) opens the Boot scene first, not the Main Menu.
- [ ] When `Logs/generation-failures.json` is deleted and the cache is empty, the boot screen plays through every CoreAssetEntry visibly, with prompts, thumbnails, and a scrolling log. Final count shows N/N succeeded **or** lists the failures.
- [ ] When the cache is full, the boot screen shows `total = X, cached = X` for ≤1s, logs `Ready`, and transitions to Main Menu.
- [ ] Killing the Forge mid-stream (set a fake exception) does not crash the boot; the entry is marked failed, the log line is red, the loop continues.

C. **New Game flow (visible)**

- [ ] `New Game` either flashes the generation screen (if scenario assets missing) or goes straight to Character Creation.
- [ ] Character Creation walks: skill pick modal → 6 attribute rolls (dice animation, sum) → background choose → portrait preview (LLM JSON shown in the log).
- [ ] Begin Game shows worldgen log scrolling with regions/settlements/NPCs and at least one question modal.

D. **UI consistency**

- [ ] Boot, Loading, Character Creation overlay, Worldgen overlay all use `UiTokens` colors / fonts / spacing — verifiable by changing one token and seeing all four screens follow.
- [ ] No screen reaches for UI Toolkit / UGUI types directly outside `Assets/Scripts/Ui/Backends/`.

E. **Failure semantics**

- [ ] Inducing 3 deliberate prompt failures (rename 3 silhouette files) results in 3 red log lines and 3 rows in `Logs/generation-failures.json`. Boot does not halt.

F. **Git**

- [ ] One commit (or a small focused series) on `feat/visible-generation-cutover`.
- [ ] PR opened against `codex/sdxl-pipeline-and-naming-refactor`. **Do not merge** before @msbel5 review.
- [ ] Commit message references this PRD path.

G. **Report**

- [ ] `Reports/visible-generation-cutover_<unix>.md` written with §16 contents and the working-style §17 evidence.

---

## 17. ÇALIŞMA STİLİ + REPORT (rules for whoever executes)

- Read this PRD end to end before writing any code.
- Run every §3 command and paste output into the kickoff report before changing any file.
- Tests first, then code, then tests pass, then commit.
- No 800-line files. One class per file unless trivially co-located DTOs.
- Failure policy is skip + log + continue. Throwing aborts the boot — that is the bug, not the behavior.
- Static asset prompts are designer-authored. Never call the LLM to write one.
- LLM is only used in dynamic NPC flow, and only via the strict JSON schema in §9.
- Trading bot / OpenClaw / external systems: do not touch.
- Existing scene canvases stay UGUI. Only Boot scene is new; CharacterCreation gets an overlay; existing canvases are untouched.
- Do not run `git push --force` without explicit @msbel5 ask.
- Do not modify `.gitignore`'s `Packages/*/` rule beyond the one ai.assistant exception (lands in PR #207 already).
- "Yaptım" deme. Komut + çıktı + test sonucu + screenshot path koy. Every claim has evidence in the report.

### Decision: Codex single-mission OR Claude + msbel together

Both viable. Trade-offs:

| | Codex single-mission | Claude + msbel together |
|---|---|---|
| Speed | 1 round trip, multi-day in background | iterative, faster feedback per file |
| Visibility | Final PR only | Live commits, you watch each piece land |
| Quality control | Codex review at end | review every commit |
| msbel cognitive load | low | medium |
| Best when | scope locked + you want a finished deliverable | you want to course-correct as it lands |

Recommendation: **Codex single-mission** for this one. Scope is locked (this PRD), §3 read-only discovery is fully scripted, acceptance is enumerated, working style is explicit. You will not need to baby-sit. If Codex blocks on something this PRD does not answer, the blocker becomes a review comment and we resolve in this same PR — not by halting.

The Codex prompt to hand off is appended below this PRD when @msbel5 approves §17's decision.

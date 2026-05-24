# Codex Mission — Ember CRPG: Visible Generation Cutover

> Copy this whole file into Codex Desktop as the initial prompt.
> Full PRD lives at `docs/prds/visible-generation-cutover.md` (same branch).
> Approved by @msbel5: single-mission, single-PR cutover against `main`.

---

Codex, görev: Ember CRPG için **VISIBLE GENERATION CUTOVER** projesini production-review seviyesinde tamamla.

Bu tek sprint değil, **cutover projesi**. Yarım bırakma:
**oku → referansları incele → planla → kickoff raporu yaz → testleri önce yaz → kodu yaz → test et → Unity build üret → görsel/komut kanıtı topla → final rapor yaz → GitHub'a pushla.**

Çıktı: **tek PR**, `main`'e karşı, `@msbel5` review edecek. **Do NOT merge yourself.**

---

## TARGETS

| Field | Value |
|---|---|
| Repo | `msbel5/alcyone-ember-rpg` |
| Working dir (Windows) | `C:\Users\msbel\projects\alcyone-ember-rpg` |
| Default branch | `main` (canonical — all prior work was reconciled in PR #212; PRD + mission prompt + UI Foundation landed via PR #213) |
| **Your work branch** | **Open a fresh branch: `feat/visible-generation-cutover` (or another `feat/visible-generation-*` name if the old one is still cached anywhere)** |
| Active PR for this mission | **none yet — you will open it against `main` after the kickoff report commit** |
| Unity Editor (project-pinned) | `6000.3.13f1` |
| Unity Editor install (this dev) | `E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe` |
| Reports root | `Reports/` |
| Kickoff report path | `Reports/visible-generation-cutover-kickoff_<unix>.md` |
| Final report path | `Reports/visible-generation-cutover_<unix>.md` |

**Branch history note for Codex:**
PR #210 (`feat/visible-generation-cutover` → `main`) was **CLOSED, not merged**. Its content (PRD V2 + this mission prompt + the `Assets/Scripts/Ui/Foundation/*` scaffold) was rescued onto `main` via PR #213. The closed branch was deleted from origin. Do not try to push to a branch named `feat/visible-generation-cutover` expecting to update PR #210 — **open a new branch and a new PR**.

================================================================================
## 0. HARD RULES
================================================================================

- **Token harcama:** kısa status, kanıt-odaklı rapor. "Yaptım" deme; komut + çıktı + screenshot path + test sayısı koy.
- **Önce read-only keşif ve kickoff report. Kod değişikliği yok** kickoff committed olana kadar.
- **Test önce, kod sonra.** Her functional commit'ten önce ilgili test dosyası mevcut + kırmızı.
- **SOLID.** Küçük dosya, küçük sınıf, küçük test.
- **200 satırı geçen `.cs` dosyasında en üste:** `// Why this file is intentionally long: <reason>`
- **800 satırlık "manager god class" yasak.**
- **Existing UGUI MainMenu / HUD / dialog canvas'larına dokunma.** Sadece yeni Boot scene + CharacterCreation overlay + Worldgen overlay UI Toolkit'le yapılır.
- Yeni Boot, Loading, Worldgen, CharacterCreation overlay **UI Toolkit + UI abstraction** ile yapılır.
- **Domain ve Simulation `UnityEngine` import etmez.** Pure C#. (`UnityEngine.Mathf` bile yok — `System.Math` kullan.)
- Game code concrete UI/Forge **new'lemez**. Her zaman:
  - `UiSurfaceLocator.Current`
  - `ForgeLocator.Current`
  - `LlmRoutingService` / `NativeLlmClient` surface
- **Static asset prompt'ları designer-authored.** LLM static prompt yazmaz. Bu kural sert: bir LLM çağrısı static prompt üretiyorsa code review reddeder.
- **LLM sadece `NpcPromptJson` üretir** (PRD §9 schema). Free-text image prompt LLM çağrısı yok.
- LLM JSON invalid ise **retry 1×**, sonra **deterministic fallback** (`NpcPromptJsonDefaults.FromSeed(npcSeed)`).
- **Asset generation failure policy: skip + log + continue.** Boot abort etmek bug'dır. Throw = bug.
- **Long IO: `CancellationToken` + timeout zorunlu** (Forge default 5 dk/asset).
- `Logs/generation-failures.json` **JSON-lines append-only**.
- **Secrets / tokens loglanmaz.** `QA_FIGMA_TOKEN`, Unity license, GitHub PAT, env values → `<set>` veya hash redact.
- **No direct code/art/text copying from reference repos.** Reference-only, pattern extraction.
- **`git push --force` yok.** Rebase gerekirse: önce PR yorumu, sonra `--force-with-lease`.
- Trading bot, OpenClaw, unrelated systems, embedded DLL'ler: **dokunma**.
- `.gitignore`'daki `Packages/*/` rule + ai.assistant exception: **dokunma**.
- **Unity-MCP** varsa kullan. MCP revoked / disconnected ise **headless Unity** path ile test et ve rapora "MCP revoked; headless Unity used" yaz.

================================================================================
## 1. PRE-FLIGHT GATE
================================================================================

Önce bunları çalıştır. Şartlar sağlanmazsa kod yazma; yeni branch'in **ilk commit'i** olarak `Reports/visible-generation-cutover-kickoff_<unix>.md` içine `BLOCKED: <one-line reason>` yaz, kullanıcıya bildir, **bekle**.

**PowerShell (Windows host):**

```powershell
Set-Location C:\Users\msbel\projects\alcyone-ember-rpg
git fetch origin --prune
git status --short --branch
git log -1 --format=%H origin/main
git log --oneline origin/main -5

gh pr view 212 --json state,mergeCommit --jq "{state: .state, mergeCommit: .mergeCommit.oid}"
gh pr view 213 --json state,mergeCommit --jq "{state: .state, mergeCommit: .mergeCommit.oid}"
gh pr list --state open --limit 20 --json number,title,headRefName,baseRefName
```

**Expected state (must all be true):**

- `PR #212` → `MERGED` (codex/sdxl → main reconcile)
- `PR #213` → `MERGED` (PRD V2 + mission prompt + UI Foundation rescue)
- No open PRs targeting `main` from any `feat/visible-generation-*` branch
- `main` HEAD already contains:
  - `docs/prds/visible-generation-cutover.md`
  - `docs/prds/visible-generation-cutover-codex-mission.md` (this file)
  - `Assets/Scripts/Ui/Foundation/{IUiSurface,IUiPanel,UiTokens,UiSurfaceLocator}.cs` + asmdef
  - `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs` with `catch (ArgumentException)` defensive guards in `DeriveSiblingModel` / `DeriveTokenizerSibling`
  - `Assets/Scripts/Simulation/Forge/{Sdxl,Sd15}*.cs` with `ClampDimension` snapping to multiple of 8
  - `Assets/Scripts/Presentation/Ember/Forge/ForgeBootstrap.cs` keeping the SD15 instance instead of returning `ExplicitFailureAssetForge` on TryWarmup failure
  - `Packages/com.unity.ai.assistant/` embedded with `SigLip2Text.cs:59` patched to `SentencePieceTokenizer.Create(spStream)`
  - `.github/workflows/unity-test.yml` with `lfs: true` on all checkout steps

**Then open a fresh work branch:**

```powershell
git checkout main
git pull --ff-only origin main
git checkout -b feat/visible-generation-cutover
git push -u origin feat/visible-generation-cutover
```

If GitHub rejects the branch name as already in use (cache delay), pick `feat/visible-generation-cutover-2026-05-25` or similar — branch name is incidental, just pick one and stick to it for the whole mission.

After the kickoff report commit lands on this branch, **immediately open the PR** so reviewers can watch it grow:

```powershell
gh pr create --base main --draft --title "Visible Generation Cutover (single-mission)" --body "<short link to kickoff report + a one-paragraph summary of the plan>"
```

Then convert it from draft to ready-for-review **only** when §18 acceptance is fully green.

================================================================================
## 2. EMBER VISION
================================================================================

Ember is **not** a generic 2D tile game.

Target soul:

- **Morrowind / Daggerfall–like first-person CRPG mood.**
- **Deterministic simulation backend.** Same seed → same world → same NPCs → same dice.
- **Visible world systems:** generation, dice, history, NPC seeds, failures — all on screen.
- **Local-first AI generation:** the player sees what is being made, never a silent black box.
- **Dense CRPG UI**, not marketing UI. Information density over whitespace.
- **Low-saturation dark fantasy, ember-warm palette, readable logs.**
- The UI must make the game feel **alive** before combat exists.

Cutover flow the player experiences after this PR merges:

```
start.exe
   ↓
Boot.unity                                  ← new, Build Settings index 0
   ↓
visible core asset scan/generation          ← AssetGenerationScreen, per-entry log + thumbnail
   ↓
MainMenu.unity                              ← existing, UGUI, untouched
   ↓ [New Game]
visible scenario asset top-up               ← AssetGenerationScreen again, scoped
   ↓
CharacterCreation.unity + overlay           ← existing scene + new overlay
   • SkillPickStep        (12 skills → pick 3, log)
   • AttributeRollStep    (6 attrs × 4d6-drop-lowest, dice spin, log)
   • BackgroundChooseStep (8 backgrounds, one-line flavor, log)
   • PortraitPreviewStep  (NpcPromptJson via LLM, validate, silhouette + RGB, reroll ≤3)
   ↓ [Begin Game]
LoadingScreen + WorldgenView                ← visible event log
   • RegionGenerated / SettlementSeeded / NpcSeeded (JSON inline) / DiceRolled / QuestionRaised / Failure / Completed
   ↓
first game scene
```

The player **never** stares at a silent blank screen while generation happens.

================================================================================
## 3. REFERENCE READING — REQUIRED, READ-ONLY
================================================================================

Before implementation, inspect these as **reference only** (license-clean clean-room reimplementation):

**Primary Ember/Godot source (single source of truth for Ember-specific naming, tone, content):**

```
D:\projects\ember-rpg
```

**Reference repos (idiomatic CRPG patterns, never copy):**

```
D:\projects\examples\daggerfall-unity-master
D:\projects\examples\openmw-master
D:\projects\examples\dwarf-fortress-legacy
D:\projects\examples\gemrb-master
```

**Rules:**

- Do **not** copy code, text, assets, UI art, or data tables directly. Even file headers.
- Extract patterns only. Architecture shape, control flow, naming intuition.
- If copying feels unavoidable, stop and write in kickoff report:
  - file path,
  - reason,
  - estimated LOC,
  - license risk (Daggerfall Unity = MIT, OpenMW = GPLv3, dwarf-fortress-legacy = various, GemRB = GPLv2; **GPL contamination is the major risk — assume default = do not copy**).
- Default is **clean reimplementation** inside Unity project.

**What to learn:**

| Source | Look for | Do not lift |
|---|---|---|
| `D:\projects\ember-rpg` | Ember terms, character creation ideas, asset generation prompt vocabulary, UI flow intent, visual tone | Verbatim text, recipe data, prompt strings |
| Daggerfall Unity | Question-driven char creation shape, skill/class/background/dice flow, CRPG pacing (choices before world entry) | Question text, class tables, image assets |
| OpenMW | Data-driven content loading, graceful missing-content behavior, config/manifest style | Engine code, asset paths, ESM/ESP parsing |
| dwarf-fortress-legacy | Visible worldgen log density, history presentation, pause/continue/auto-advance UX | History event strings, legends-mode output |
| GemRB | Infinity Engine modal/log/inventory readability, compact UI density, panel consistency | GUI scripts, layout XML, IE asset names |

In the kickoff report, include a short **Reference Extraction** subsection:

```markdown
### Reference Extraction
- Ember Godot: <what was useful, what was rejected, Ember-specific decision>
- Daggerfall Unity: ...
- OpenMW: ...
- Dwarf Fortress: ...
- GemRB: ...
```

================================================================================
## 4. READ-ONLY DISCOVERY COMMANDS
================================================================================

**No code changes** until these outputs are captured in `Reports/visible-generation-cutover-kickoff_<unix>.md`.

```powershell
# 4.1 Branch state
git status --short --branch
git log --oneline origin/main..HEAD
git log --oneline -10
git diff --stat origin/main..HEAD

# 4.2 Editor menus
rg -n "MenuItem\(.*Ember|MenuItem.*Ember" Assets/Editor

# 4.3 Scenes + UI surfaces
rg --files Assets/Scenes | Sort-Object
rg -l "MainMenuCanvas|UIDocument|Canvas|CharacterCreation" Assets/Scenes Assets/Scripts/Presentation Assets/Editor

# 4.4 Forge surface
rg -n "ForgeLocator\.Register|ForgeLocator\.Current|ForgeBootstrap|OnnxAssetForge|Sdxl|Sd15|IDiffusionPipeline" Assets/Scripts Assets/Editor

# 4.5 Prompt composers + cache keys
rg -n "class PromptComposers|public static .*Prompt|NpcPortrait|CacheKey" Assets/Scripts/Simulation/Forge Assets/Scripts/Domain/Forge

# 4.6 Worldgen domain
rg -n "WorldgenService|GeneratedWorld|WorldHistoryEvent|RegionRecord|SettlementRecord|NpcSeedRecord|WorldProfile" Assets/Scripts/Domain Assets/Scripts/Simulation Assets/Scripts/Presentation

# 4.7 LLM surface
rg -n "NativeLlmClient|LocalQwenClient|LlmRoutingService|NpcPromptJson|ToolUseService" Assets/Scripts

# 4.8 Existing character creation
rg -n "CharacterCreation|Birthsign|CreationQuestion|CharacterClass|Dice|Attribute|Skill" Assets/Scripts Assets/Tests Assets/Scenes

# 4.9 MCP rg presence (regression sanity)
Test-Path "Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe"
Test-Path "Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe.gz"

# 4.10 Reference scan (limit output — these dirs are big)
rg -n "character|creation|class|birth|skill|attribute|dice|question|background|portrait|prompt|worldgen|generation" `
   D:\projects\ember-rpg -g "*.gd" -g "*.py" -g "*.json" -g "*.md" -g "*.tscn" | Select-Object -First 200
rg -n "class.*creation|question|skill|attribute|background|birthsign|dice" `
   D:\projects\examples\daggerfall-unity-master -g "*.cs" | Select-Object -First 200
rg -n "content|fallback|manifest|resource|load" `
   D:\projects\examples\openmw-master -g "*.cpp" -g "*.hpp" -g "*.cs" -g "*.md" | Select-Object -First 80
rg -n "worldgen|history|event|civilization|region|site" `
   D:\projects\examples\dwarf-fortress-legacy | Select-Object -First 80
rg -n "GUI|Window|Button|TextArea|Log|Dialog|Inventory|Portrait" `
   D:\projects\examples\gemrb-master -g "*.cpp" -g "*.h" -g "*.py" | Select-Object -First 80

# 4.11 Unity compile smoke (before any code change)
& "E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -projectPath "C:\Users\msbel\projects\alcyone-ember-rpg" `
  -quit -logFile "Reports/unity_compile_preflight.log"

# 4.12 If Unity-MCP available
#   - mcp Unity_GetProjectData
#   - mcp Unity_GetConsoleLogs(Error)
# If revoked, note in kickoff report: "MCP revoked; headless Unity log used."
```

Paste every output (or a tail + relevant grep) into the kickoff report under §4.

================================================================================
## 5. SUBAGENT ORCHESTRATION
================================================================================

If subagents are available, use them. Keep each one small and scoped.

**Lead Codex owns:**

- final architecture decisions
- integration order between subagents
- staging / commits
- validation runs
- PR / final report

**Suggested subagents (open in parallel where dependencies allow):**

| # | Name | Scope | Tests it owns | Touches game flow? |
|---|---|---|---|---|
| 1 | reference-reader | Read `D:\projects\ember-rpg` + examples, write `Reports/subagents/reference-reader_<unix>.md` | none | no |
| 2 | ui-foundation-agent | `UiTokens`, `IUiSurface`, `IUiPanel`, `UiSurfaceLocator`, UI Toolkit backend, base widgets | `UiTokensTests`, `UiSurfaceLocatorTests` | no |
| 3 | manifest-generation-agent | `CoreAssetManifest`, `GenericNpcBaseManifest`, `StaticPromptCatalog`, `AssetManifestScanner`, `GenerationFailureLog`, `VisibleGenerationPipeline` | manifest/scanner/static-prompt/failure-log/pipeline tests | no |
| 4 | boot-loading-agent | `Boot.unity`, `BootBootstrap`, `LoadingScreen` API, `LoadingScreenController`, Build Settings index 0 management | `BootSceneTest`, `LoadingScreenApiContractTest` | yes (Boot) |
| 5 | character-creation-agent | CharacterCreation overlay controller + four step panels, reuses existing Domain types when present | `CharacterCreationFlowTest`, dice/widget deterministic tests | yes |
| 6 | worldgen-visible-agent | `WorldgenViewController`, `WorldgenLogPanel`, event DTOs / adapter from existing `WorldgenService` (projection, not rewrite) | `WorldgenViewVisibleTest`, projector tests | yes |
| 7 | unity-proof-agent | Editor menus, Windows64 build, screenshot capture, report evidence — MCP or headless | none of its own; collects evidence | no (read/build only) |

Each subagent returns:

```markdown
- files read
- files changed (path + LOC delta)
- tests added
- risks / blockers
- commands run + output excerpts
```

**Lead Codex reviews each subagent return before committing.**

If subagents are not available, simulate the same sequence manually with small commits (one logical batch per subagent scope).

================================================================================
## 6. ARCHITECTURE TARGET
================================================================================

New / expected structure. Follow `docs/prds/visible-generation-cutover.md` §5 where it exists. If deviating, write the reason in the kickoff report.

```
Assets/Scripts/Ui/
  Foundation/                       (already on main from PR #213)
    IUiSurface.cs
    IUiPanel.cs
    UiTokens.cs
    UiSurfaceLocator.cs
    UiLogSeverity.cs                (extract from IUiPanel.cs if currently inline)
    EmberCrpg.Ui.Foundation.asmdef
  Backends/UiToolkit/               NEW
    UiToolkitSurface.cs
    UiToolkitPanel.cs
    UiToolkitThemeBinder.cs         (subscribes to UiTokens, mutates USS vars)
    Templates/*.uxml + Styles/*.uss
    EmberCrpg.Ui.Backends.UiToolkit.asmdef (references Foundation + UIElements)
  Screens/                          NEW
    Boot/             (UXML + USS for BootScreen)
    Loading/          (UXML + USS for LoadingScreen)
    CharacterCreation/(UXML + USS per step)
    Worldgen/         (UXML + USS for WorldgenView)
  Widgets/                          NEW
    LogList/          (virtualized scroll, 10k+ lines)
    Progress/         (deterministic progress bar)
    Dice/             (4d6 visual + drop-lowest highlight)
    Thumbnail/        (Texture2D → UI Toolkit Image bridge)
    Modal/            (QuestionRaised modal pause)

Assets/Scripts/Domain/Generation/   NEW (pure C#, no UnityEngine)
  ManifestEntry.cs
  ManifestScanReport.cs
  NpcPromptJson.cs
  NpcPromptJsonValidator.cs

Assets/Scripts/Simulation/Generation/   NEW (pure C#, no UnityEngine)
  StaticPromptCatalog.cs
  AssetManifestScanner.cs
  VisibleGenerationPipeline.cs
  GenerationFailureLog.cs
  NpcPromptJsonDefaults.cs
  LlmPromptComposer.cs

Assets/Scripts/Presentation/Ember/  NEW
  Boot/
    BootBootstrap.cs                MonoBehaviour on Boot scene root
  Loading/
    LoadingScreen.cs                static façade
    LoadingScreenController.cs      MonoBehaviour, DontDestroyOnLoad
  CharacterCreation/
    CharacterCreationController.cs
    SkillPickStep.cs
    AttributeRollStep.cs
    BackgroundChooseStep.cs
    PortraitPreviewStep.cs
  Worldgen/
    WorldgenViewController.cs
    WorldgenEventProjector.cs       takes existing GeneratedWorld + emits events

Assets/Editor/Ember/                NEW (Editor-only)
  Forge/
    ScanMissingAssetsMenu.cs        "Ember/Forge/Scan Missing Assets"
    GenerateCorePreviewMenu.cs      "Ember/Forge/Generate Core (Editor preview)"
  Build/
    BuildSettingsSceneRegistrar.cs  keeps Boot.unity at index 0 idempotently
    Windows64BuildMenu.cs           "Ember/Build/Windows64"
  Patches/
    RestoreRipgrep.cs               (only if §16-A path chosen)

Assets/Manifests/                   NEW (ScriptableObject instances)
  DefaultUiTokens.asset
  CoreAssetManifest.asset
  GenericNpcBaseManifest.asset

Assets/Tests/
  EditMode/
    Ui/                             (new)
    Generation/                     (new)
    CharacterCreation/              (new)
    Worldgen/                       (extend if existing)
    Forge/                          (DO NOT touch unless adding regression coverage)
  PlayMode/
    Boot/                           (new)
    Loading/                        (new)
    CharacterCreation/              (new)
    Worldgen/                       (new)

Reports/                            NEW
  visible-generation-cutover-kickoff_<unix>.md
  visible-generation-cutover_<unix>.md
  screens/                          (PNG evidence)
  subagents/                        (per-subagent returns)
```

**Composition invariants (enforced by code review):**

- Runtime flow depends on **abstractions** (IUiSurface, IAssetForge, ILlmClient).
- `Assets/Scripts/Presentation/**` may reference `UnityEngine` / `UnityEngine.UIElements`.
- `Assets/Scripts/Domain/**` and `Assets/Scripts/Simulation/**` must stay pure (no `UnityEngine`, no `UnityEditor`).
- UI Toolkit direct references **only** under `Assets/Scripts/Ui/Backends/UiToolkit/**` and screen / controller Presentation layer.
- Existing UGUI MainMenu / HUD / dialog **left untouched**.
- `Assets/Scripts/Ui/Foundation/EmberCrpg.Ui.Foundation.asmdef` is **autoReferenced**; backend + screen asmdefs are **not** autoReferenced and reference Foundation explicitly.

================================================================================
## 7. UI FOUNDATION
================================================================================

Default backend: **UI Toolkit**.

**Required API surface (already present in `IUiSurface` / `IUiPanel`; extend if needed, do not break):**

```csharp
public interface IUiSurface
{
    IUiPanel Mount(string panelId);
    void Unmount(IUiPanel panel);
    UiTokens Tokens { get; }
}

public interface IUiPanel
{
    string Id { get; }
    void SetText(string slot, string text);
    void SetProgress(string slot, float normalized);          // clamped [0,1]
    void LogLine(string slot, UiLogSeverity severity, string line);
    void SetThumbnail(string slot, Texture2D texture);         // null clears
    void SetVisible(string slot, bool visible);
    void SetButtonHandler(string slot, Action onClick);        // null removes
}
```

`UiTokens` (ScriptableObject — `Assets/Manifests/DefaultUiTokens.asset`):

- Dark default
- Color fields: `Accent`, `AccentMuted`, `Background`, `Panel`, `Text`, `TextMuted`, `Danger`, `Warning`, `Success`
- Spacing fields: `SpacingXs`, `SpacingSm`, `SpacingMd`, `SpacingLg` (px)
- Font fields: `FontBody`, `FontHeading`, `FontMono` (TMP_FontAsset references; loaded by GUID, not name)
- `Color SeverityColor(UiLogSeverity severity)` — total function, every enum value mapped
- Validate on `OnValidate`: no color is `Color.clear`; throws in Editor if so

**Tests (EditMode):**

- `UiTokensTests`:
  - every color non-default (not `Color.clear`)
  - `SeverityColor` covers every `UiLogSeverity` enum value (no `default` fall-through)
  - serialization roundtrip preserves values
- `UiSurfaceLocatorTests`:
  - `Register` then `Current` returns the registered instance
  - `Register` twice without `Clear` throws `InvalidOperationException`
  - `Clear` sets `Current` to null
  - thread-safety: 100-thread `Register/Clear` race does not throw

================================================================================
## 8. ASSET MANIFEST + STATIC PROMPTS
================================================================================

### 8.1 `CoreAssetManifest.asset` (~50 entries)

| Category | Entry IDs | `requiresGeneration` | Notes |
|---|---|---|---|
| UI icons | `new_game`, `settings`, `dice`, `skill`, `attack`, `defend`, `equip`, `drop`, `inventory`, `map`, `journal`, `magic`, `rest`, `continue`, `error` | `true` | 64×64, square |
| Fonts | `font_body`, `font_heading`, `font_mono` | `false` | reference existing TMP_FontAssets |
| Silhouettes | `silhouette_humanoid_male`, `silhouette_humanoid_female`, `silhouette_beast_quadruped`, `silhouette_undead_humanoid`, `silhouette_construct`, `silhouette_aberration` | `false` | point at on-disk `Assets/Art/BodySilhouettes/*.png` |
| Item icons | `item_sword`, `item_bow`, `item_staff`, `item_potion`, `item_scroll`, `item_key`, `item_ring`, `item_helm`, `item_boots`, `item_shield` | `true` | 128×128 |
| Spell icons | `spell_sleep`, `spell_heal`, `spell_fire`, `spell_ice`, `spell_shield`, `spell_lightning` | `true` | 96×96 |
| Sounds | `sfx_ui_click`, `sfx_ui_hover`, `sfx_dice_roll`, `sfx_level_up`, `sfx_error` | `false` unless pipeline supports audio | reference placeholders for now |
| Logo / splash | `logo_full`, `logo_compact`, `splash_background` | `true` | logo 256×128 / 128×128, splash 1920×1080 |

**Entry fields:**

```csharp
public record ManifestEntry(
    string Id,
    string Category,
    string ExpectedPath,        // Assets-relative
    string StaticPromptKey,     // resolves in StaticPromptCatalog (empty if requiresGeneration=false)
    int Width,
    int Height,
    bool RequiresGeneration,
    int TimeoutSeconds,         // default 300
    string ModelHint            // optional: "sdxl-turbo" | "sd15-lcm" | ""
);
```

### 8.2 `GenericNpcBaseManifest.asset`

Use **exactly** the six on-disk silhouettes — verified paths:

```
Assets/Art/BodySilhouettes/humanoid_male.png
Assets/Art/BodySilhouettes/humanoid_female.png
Assets/Art/BodySilhouettes/beast_quadruped.png
Assets/Art/BodySilhouettes/undead_humanoid.png
Assets/Art/BodySilhouettes/construct.png
Assets/Art/BodySilhouettes/aberration.png
```

`ArchetypeEntry` fields: `ArchetypeId`, `SilhouettePath`, `HuePaletteMin`, `HuePaletteMax`, `SaturationRange`, `LightnessRange`, `Notes`, `RequiresGeneration`.

Other future archetypes (fairy, dragon, elemental) **may** exist with `SilhouettePath = ""` and `RequiresGeneration = true`. **Do not invent many in v1** — focus on the six.

### 8.3 `StaticPromptCatalog`

```csharp
public const string EmberStyleHeader =
    "dark-fantasy ember-warm palette, painterly low-saturation, transparent background, single subject centered";

public const string EmberNegativeFooter =
    "no text, no watermark, no border, no UI elements, no signature, no logo";
```

Every static prompt template:

- **starts with** `EmberStyleHeader`
- **ends with** `EmberNegativeFooter`
- concrete nouns, not generic fantasy. **Good:** `"a wrought-iron longsword with rune-etched fuller, hilt wrapped in oxblood leather"`. **Bad:** `"a sword"`.
- reproducible: `(prompt, seed, dimensions, model)` → same image.

**Quality gate:** Render 3 sample static assets (1 UI icon + 1 item + 1 spell) and paste paths under `Reports/screens/sample_static_*.png` for @msbel5 subjective review.

### 8.4 Tests

- all manifest IDs unique
- entries with `RequiresGeneration = true` have non-empty `StaticPromptKey`
- entries with `RequiresGeneration = false` may have empty `StaticPromptKey`
- every non-empty `StaticPromptKey` resolves in `StaticPromptCatalog`
- every prompt starts with `EmberStyleHeader`
- every prompt ends with `EmberNegativeFooter`
- scanner: empty cache → all entries `Missing`
- scanner: full cache → all entries `Cached`
- scanner: idempotent (two scans without disk change → identical report)

================================================================================
## 9. VISIBLE GENERATION PIPELINE
================================================================================

**`AssetManifestScanner.ScanAsync(core, npcBase, cache, ct) → Task<ManifestScanReport>`:**

```csharp
public sealed record ManifestScanReport(
    IReadOnlyList<EntryRow> Entries,
    int Total,
    int Cached,
    int Missing,
    int RequiresGeneration,
    int FailedSinceLastScan
);

public sealed record EntryRow(
    string EntryId,
    string Category,
    string Path,
    EntryState State,        // Cached | Missing | RequiresGeneration | Failed
    string Reason
);
```

Idempotent. No side effects beyond reading the cache.

**`VisibleGenerationPipeline.RunAsync(IReadOnlyList<ManifestEntry> entries, CancellationToken ct) → Task<PipelineResult>`:**

- Never aborts the full loop on one failed entry.
- Uses `ForgeLocator.Current`.
- Per-entry timeout from `entry.TimeoutSeconds` → `AssetGenerationRequest.TimeoutSeconds`.
- Emits events (or invokes callbacks) in this order per entry:
  1. `EntryStarted(entry)`
  2. `EntryProgress(entry, normalized)` (0..1, may fire multiple times)
  3. `EntryThumbnail(entry, partialTexture)` (optional; if forge can stream)
  4. `EntrySucceeded(entry, finalTexture, elapsedMs)` **OR** `EntryFailed(entry, reason, exceptionType)`
- After all entries: `Completed(total, succeeded, failed)`.
- Failures append one JSON-line row to `Logs/generation-failures.json`:

```json
{"ts":"2026-05-24T22:43:11Z","entryId":"item_sword","category":"item","reason":"onnx_inference_failed:OnnxRuntimeException","exceptionType":"OnnxRuntimeException","promptHash":"sha256:abc...","elapsedMs":42813}
```

- Never throws to caller (the boot loop must continue).

**`GenerationFailureLog`:**

- Append-only.
- One valid JSON object per line (newline-terminated).
- No secrets.
- Includes: `ts` (ISO-8601 UTC), `entryId`, `category`, `reason` (one-line), `exceptionType` (optional), `promptHash` (sha256 of `prompt + seed + dims`, never raw prompt), `elapsedMs`.
- If file does not exist: create with `Directory.CreateDirectory` for `Logs/`.

**Boot behavior (PRD §10):**

- All cached → log `[ok] Ready (X/X cached)` → 1s pause → load MainMenu.
- Anything missing → AssetGenerationScreen plays through every missing entry visibly.
- Each failure → red log row + JSON-lines append + **continue** to next entry.
- End of loop → `Generation complete: X/Y succeeded, Z failed (see Logs/generation-failures.json)` + `Continue to Main Menu` button.

**Tests:**

- Fake `IAssetForge` returning placeholder bytes; 3 entries, one deliberately fails → events fire in order, loop completes, `EntryFailed` fires exactly once, JSON-lines has exactly one new row.
- `CancellationToken` mid-loop → `OperationCanceledException` to caller, JSON-lines not corrupted.
- Idempotent re-run on same cache → 0 new failures, all `Cached`.

================================================================================
## 10. LLM NPC JSON CONTRACT
================================================================================

The LLM produces **only** this JSON. Free-text prompts are not allowed.

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

**`NpcPromptJsonValidator` (strict mode):**

- `archetype_id` ∈ `GenericNpcBaseManifest` IDs → else reject (`unknown_archetype`)
- `primary_hue_degrees` and `secondary_hue_degrees`: integer ∈ `[0, 360)` → else reject (`hue_out_of_range`)
- `mood_keywords`, `distinctive_features`: array, max 5 entries → else reject (`array_too_long`)
- Each string: max 40 chars, ASCII only (no emoji, no extended Unicode) → else reject (`string_too_long` / `non_ascii`)
- Unknown top-level fields → reject (`unknown_field:<name>`)
- Missing required field → reject (`missing_field:<name>`)
- Whitespace-only string → reject (`empty_string`)

**On failure:**

1. Retry **once** with correction prompt: `"The previous response was invalid because <reason>. Respond ONLY with valid JSON matching the schema."`
2. Second failure → `NpcPromptJsonDefaults.FromSeed(npcSeed)` — deterministic, never throws.

**`LlmPromptComposer.Compose(NpcPromptJson) → string`:** deterministic. **No LLM call after JSON.** Builds the final image-generation prompt by template interpolation only.

**Tests:**

- Valid JSON accepts.
- Each rejection reason has its own test case (`unknown_archetype`, `hue_out_of_range`, `array_too_long`, `string_too_long`, `non_ascii`, `unknown_field:foo`, `missing_field:archetype_id`, `empty_string`).
- Fallback deterministic: same seed → identical JSON.

================================================================================
## 11. BOOT SCENE
================================================================================

**Create:** `Assets/Scenes/Ember/Boot.unity`

**Build Settings:**

- `Boot.unity` index 0
- `MainMenu.unity` retained (existing position bumps by 1)
- `CharacterCreation.unity` retained
- Existing scenes retained
- `Ember/Build/Add All Scenes To Build Settings` updated to **keep Boot at index 0 idempotently** (re-running the menu must not reorder if Boot is already at 0)

**`BootBootstrap.Awake`:**

1. If `UiSurfaceLocator.Current` is null → instantiate `UiToolkitSurface` (using DefaultUiTokens from `Assets/Manifests/DefaultUiTokens.asset`) and register.
2. Mount `BootScreen` panel.
3. Load `CoreAssetManifest` + `GenericNpcBaseManifest` from `Assets/Manifests/`.
4. Run `AssetManifestScanner.ScanAsync(...)`. Update panel slots `total`, `cached`, `missing` live as scan progresses (scanner can yield).
5. If `Missing == 0 && RequiresGeneration == 0` → log `[ok] Ready (X/X cached)` → 1s pause → `SceneManager.LoadSceneAsync("MainMenu")`.
6. Else → subscribe `VisibleGenerationPipeline` events to panel updates, then `await pipeline.RunAsync(missingEntries, ct)`.
7. After loop → final screen: `Generation complete: X/Y succeeded, Z failed (see Logs/generation-failures.json)` + `Continue to Main Menu` button (wired via `SetButtonHandler`).
8. Continue → `SceneManager.LoadSceneAsync("MainMenu")`.

**Do not block the main thread with long IO.** All Forge calls go through `Task.Run` (already true in `OnnxAssetForge.GenerateAsync`).

**Tests (PlayMode):**

- `BootSceneTest`:
  - Load `Boot.unity` with a fake `IAssetForge` registered via `ForgeLocator.Register(fakeForge)`.
  - Fake manifest with 3 entries (2 succeed, 1 fail).
  - Assert: 3 `EntryStarted`, 2 `EntrySucceeded`, 1 `EntryFailed`, 1 `Completed`.
  - Assert: panel log slot has lines containing `[ok]` × 2 + `[error]` × 1.
  - Assert: scene transition request is `MainMenu`.
- Cached-only fast-path: empty manifest → 1s pause → transition to MainMenu.

================================================================================
## 12. LOADING SCREEN
================================================================================

**Static façade (PRD §11):**

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

**Implementation:**

- `LoadingScreenController` MonoBehaviour, `DontDestroyOnLoad`.
- Uses `UiSurfaceLocator.Current.Mount("LoadingScreen")` on first `Show`.
- Single instance — repeated `Show` updates existing panel, does not create a duplicate.
- `Hide` unmounts the panel but does not destroy the controller (so the next `Show` is instant).

**Tests (PlayMode):**

- `LoadingScreenApiContractTest`:
  - `Show("Loading", "World") → SetProgress(0.3, "Step A") → LogLine(Info, "x") → ShowThumbnail(tex, "y") → Hide()` round-trip without exceptions.
  - After `SceneManager.LoadScene("MainMenu")`, controller still exists, second `Show` reuses it.
  - Two consecutive `Show` calls → only one `LoadingScreenController` in the scene.

================================================================================
## 13. CHARACTER CREATION CUTOVER
================================================================================

**Do not make CharacterCreation empty.** This is the worst current player experience and the highest user priority.

Use existing Domain CharacterCreation types if present (discover via §4.8). Likely candidates:

- `CharacterClass`
- `Birthsign`
- `CreationQuestion`
- `CharacterCreationService`

If current implementation is thin, **extend cleanly** without introducing UnityEngine into Domain.

**Reference spirit (no verbatim lift):**

- Daggerfall-style questions and class suggestion (`D:\projects\examples\daggerfall-unity-master`).
- Godot Ember implementation for tone (`D:\projects\ember-rpg`).

**`CharacterCreation.unity` overlay:**

- Add new GameObject `CharacterCreationOverlay` with high sorting layer (above existing UGUI canvas).
- Attach `CharacterCreationController` MonoBehaviour.
- Controller mounts panels via `UiSurfaceLocator.Current.Mount(stepId)`.

**Step sequence:**

### 1. `SkillPickStep`

- 12 skills (list from Domain if present, else PRD-canonical list: stealth, smithing, lore, archery, alchemy, persuasion, athletics, magic, melee, lockpicking, perception, restoration).
- User picks exactly 3 (modal disables Continue until 3 selected).
- Log: `[choice] Picked: stealth, smithing, lore.`

### 2. `AttributeRollStep`

- 6 attributes (STR, DEX, CON, INT, WIS, CHA).
- For each: `DiceRollWidget` shows 4 dice; rolls 4d6 (seeded); drops lowest (grays it out); sum displayed.
- Animation: dice spin for 800ms then settle (skip-animation toggle in settings).
- Log per attribute: `[roll] STR = 4+5+6+(2) = 15.`

### 3. `BackgroundChooseStep`

- 8 backgrounds (PRD-canonical: smuggler, scholar, pilgrim, mercenary, exile, artisan, hunter, healer).
- One-line flavor each.
- User picks one.
- Log: `[choice] Background: smuggler.`

### 4. `PortraitPreviewStep`

- Compose `NpcPromptJson` from picks + `WorldProfile` (style anchor, palette hints).
- Request via `LlmRoutingService` → `NativeLlmClient` (LLamaSharp).
- Strict-validate; on failure → retry once → fallback to `NpcPromptJsonDefaults`.
- Render: chosen silhouette (RGB-recolored using JSON hue fields) → optional ONNX SDXL refinement via `ForgeLocator.Current` if available.
- Display JSON inline below preview (collapsible).
- Reroll button: max 3 uses, decrements counter.
- Lock button: writes `CharacterCreationResult` into the existing handoff (`EmberWorldGenIntent` or whatever exists).

**Begin Game:**

- Persist `CharacterCreationResult`.
- `LoadingScreen.Show("Building world…", "")`.
- `WorldgenViewController.Mount(loadingScreenPanel)`.
- Kick off worldgen flow (§14).

**Tests (PlayMode + EditMode):**

- `CharacterCreationFlowTest` (PlayMode):
  - Drive controller through all 4 steps via `controller.Advance(...)`.
  - Assert: exactly 3 skills required to advance from step 1.
  - Each attribute roll logs exactly one line.
  - Portrait fallback deterministic when LLM returns invalid JSON.
  - Reroll button disables after 3 uses.
- `DiceRollDeterminismTest` (EditMode):
  - Seeded RNG; 4d6-drop-lowest with seed `42` → `[6,5,4,(1)] = 15` (or whatever the deterministic output is — write the test to lock the actual output).
  - Re-roll with same seed → identical output.
- `NpcPromptJsonComposerTest` (EditMode):
  - Picks + world profile → JSON request matches expected shape.
  - Same input → same composed prompt (deterministic).

================================================================================
## 14. WORLDGEN VISIBLE
================================================================================

**Do not rewrite `WorldgenService` unless required.** Prefer projection/wrapper.

If `WorldgenService` currently returns only `GeneratedWorld`:

- Add `WorldgenEventProjector` (in `Presentation`) that takes `GeneratedWorld` and emits an ordered event stream deterministically (so the UI can replay the build live).
- Keep Domain/Simulation pure.
- UI subscribes in `WorldgenViewController`.

**Events to surface (one log line each):**

| Event | Log format |
|---|---|
| `RegionGenerated(regionId)` | `[region] Generated {regionId}` + map sketch slot update |
| `SettlementSeeded(settlementId, regionId)` | `[settlement] {regionId}/{settlementId}` |
| `NpcSeeded(npcSeed)` | `[npc] {npcSeed.Name} — {archetype}` + JSON inline `[llm-json] {pretty}` |
| `DiceRolled(reason, faces, value)` | `[dice] {reason}: d{faces} = {value}` |
| `QuestionRaised(question)` | modal pause; user picks; log `[choice] {question.Id}: {answer}` |
| `Failure(reason)` | red log line + JSON-lines row in `Logs/generation-failures.json` |
| `Completed(stats)` | `[done] World built. Regions: X, Settlements: Y, NPCs: Z. {failed} failures.` |

**`WorldgenView` (UI Toolkit panel):**

- Virtualized scroll list (10k+ lines without frame drop).
- Auto-scroll toggle (default ON; pause to read).
- Auto-advance toggle (default OFF; when ON, picks first option after 1.5s preview).
- Modal overlay for `QuestionRaised` — answer buttons + log preview.
- Pretty-printed JSON inline for NPC portrait decisions.

**On `Completed`:**

```
"World ready — entering {startScene}"
wait 1s
SceneManager.LoadSceneAsync(startScene)
```

**Tests (PlayMode):**

- `WorldgenViewVisibleTest`:
  - Mock worldgen with 2 regions, 3 settlements, 5 NPCs, 1 question.
  - Assert log line counts: 2 region + 3 settlement + 5 NPC + 1 dice (any) + 1 choice + 1 done = ≥ 13 lines.
  - Modal opens when `QuestionRaised` fires.
  - `Answer(1)` advances; controller calls back to projector.
  - Auto-advance ON → first option selected after 1.5s.
  - `Failure` event → red log + JSON-lines row added.

================================================================================
## 15. EDITOR MENUS
================================================================================

### Add

- `Ember/Forge/Scan Missing Assets` → runs `AssetManifestScanner` in Editor, dumps `ManifestScanReport` to the Console as an aligned table.
- `Ember/Forge/Generate Core (Editor preview)` → runs the §11 boot flow inside the Editor Game view (no restart) for visual verification.
- `Ember/Build/Windows64` (if not already present) → builds the Windows64 player to `Builds/Windows64/alcyone-ember-rpg.exe`.

### Do not modify existing menus

- `Ember/Build Scene/*`
- `Ember/Build/*` (except adding Windows64 if missing)
- `Ember/Capture/*`
- `Ember/Forge/Generate world assets`
- `Ember/Forge/Generate Fresh World Assets`

### Build Settings idempotency

`Ember/Build/Add All Scenes To Build Settings` (or its replacement `BuildSettingsSceneRegistrar`) must:

- Insert `Boot.unity` at index 0 if missing.
- If `Boot.unity` is already at index 0 → no-op.
- Never reorder existing scenes if Boot is already first.

================================================================================
## 16. UNITY-MCP RIPGREP REGRESSION
================================================================================

**Verify presence first:**

```powershell
Test-Path "Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe"
```

If present → no action; report "rg present" in §19 final report and skip this section.

If absent (the `~`-folder trim during the ai.assistant embed dropped it):

### Option A — Preferred: gzipped binary + restore script

- Commit `Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg_win.exe.gz` (~2 MB; well under GitHub's 100 MB hard limit).
- Add `Assets/Editor/Ember/Patches/RestoreRipgrep.cs` `[InitializeOnLoad]`:
  - On Editor compile: if `rg_win.exe` missing and `rg_win.exe.gz` present → unzip in place.
  - Idempotent: skip if `rg_win.exe` already exists and is non-empty.
  - Logs to Editor Console once: `[RestoreRipgrep] Unpacked rg_win.exe (X bytes)` or `[RestoreRipgrep] rg_win.exe already present`.
- Verify: MCP grep tool returns results from `Assets/Scripts/**/*.cs`.

### Option B — Simpler: stub `rg.cmd`

- Add `Packages/com.unity.ai.assistant/ThirdParty~/ripgrep/rg.cmd` (text, ~10 lines): shells out to `where rg` and invokes the first hit, passing arguments through.
- If no system rg → exit 0 with `echo "[rg stub] system rg not found; grep tool degraded"` and let MCP fall back.

**Pick the simplest robust fix.** Document the decision in the final report under §19 → `## Unity / MCP`.

================================================================================
## 17. TESTS FIRST
================================================================================

**Before code**, add or adjust the tests below. Tests should fail (red) until the implementation lands.

### EditMode (required)

- `UiTokensTests` (§7)
- `UiSurfaceLocatorTests` (§7)
- `CoreAssetManifestTests` (§8.4)
- `GenericNpcBaseManifestTests` (§8.4)
- `AssetManifestScannerTests` (§9)
- `StaticPromptCatalogTests` (§8.4)
- `NpcPromptJsonValidatorTests` (§10)
- `GenerationFailureLogTests` (§9)
- `VisibleGenerationPipelineTests` (§9)
- `DiceRollDeterminismTest` (§13)
- `NpcPromptJsonComposerTest` (§13)
- `WorldgenEventProjectorTests` (§14)
- `BuildSettingsSceneRegistrarTests` (§15 idempotency)

### PlayMode (required)

- `BootSceneTest` (§11)
- `LoadingScreenApiContractTest` (§12)
- `CharacterCreationFlowTest` (§13)
- `WorldgenViewVisibleTest` (§14)

### Forge regressions (must stay green — do not modify)

- `OnnxAssetForge_DeterministicSeed_ProducesSameOutput`
- `OnnxAssetForge_DifferentSeed_ProducesDifferentOutput`
- `OnnxAssetForge_NoModels_FallsBackToPlaceholder`
- `OnnxAssetForge_RejectsInvalidConstructorArgs`
- New: `SdxlPipeline_ClampDimension_SnapsToMultipleOfEight`
- New: `Sd15Pipeline_ClampDimension_SnapsToMultipleOfEight`

### Validation commands (run after each functional batch)

```powershell
# Fallback harness (fast, headless)
bash tools/validation/run-validation.sh --mode fallback

# Unity EditMode (full)
& "E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -projectPath "C:\Users\msbel\projects\alcyone-ember-rpg" `
  -runTests -testPlatform EditMode `
  -testResults "Reports/test-results-editmode.xml" `
  -logFile "Reports/test-editmode.log"

# Unity PlayMode (full)
& "E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -projectPath "C:\Users\msbel\projects\alcyone-ember-rpg" `
  -runTests -testPlatform PlayMode `
  -testResults "Reports/test-results-playmode.xml" `
  -logFile "Reports/test-playmode.log"

# Windows64 build
& "E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -projectPath "C:\Users\msbel\projects\alcyone-ember-rpg" `
  -quit -executeMethod EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build `
  -logFile "Reports/build-windows64.log"
```

================================================================================
## 18. ACCEPTANCE — BLOCKING
================================================================================

**A. Compile / static**

- [ ] Unity compile: 0 errors
- [ ] Max known benign warnings documented in final report
- [ ] EditMode tests green (pass count ≥ previous)
- [ ] PlayMode tests green
- [ ] `bash tools/validation/run-validation.sh --mode fallback` green
- [ ] No `UnityEngine` references in `Assets/Scripts/Domain/**` or `Assets/Scripts/Simulation/**` (assert via `rg` in final report)
- [ ] UI Toolkit direct refs contained to `Assets/Scripts/Ui/Backends/UiToolkit/**` and Presentation screen layer
- [ ] Unity-MCP grep tool works or §16 fallback documented

**B. Boot (Windows64 build verified)**

- [ ] `Builds/Windows64/alcyone-ember-rpg.exe` exists
- [ ] Double-click → Boot scene first (not MainMenu)
- [ ] Empty cache (`Logs/generation-failures.json` deleted + cache cleared): visible core generation screen, every entry shows prompt + progress + thumbnail + status icon, final count matches manifest
- [ ] Deliberate failure (rename 3 silhouette PNGs to break their loader): red rows + 3 new JSON-lines in `Logs/generation-failures.json` + loop continues + final count reflects failures
- [ ] Full cache: `cached X/X`, `Ready` log within 1s, transitions to MainMenu

**C. CharacterCreation**

- [ ] `New Game` reaches CharacterCreation (with scenario-asset top-up screen if applicable)
- [ ] 3-skill pick gate works (cannot advance with 2 or 4 selected)
- [ ] 6 attribute rolls each show 4 dice + dropped lowest grayed + sum
- [ ] Background pick: 8 options, one selected, log line
- [ ] Portrait preview shows JSON inline; reroll counter decrements; locks after 3
- [ ] `Begin Game` starts `LoadingScreen` + `WorldgenView`

**D. Worldgen**

- [ ] Region/settlement/NPC/dice/event log visible and scrolling
- [ ] At least one question modal path exercised
- [ ] Auto-scroll toggle works
- [ ] Auto-advance toggle picks first option after 1.5s
- [ ] `Completed` loads start scene

**E. UI consistency**

- [ ] Boot / Loading / CharacterCreation overlay / Worldgen overlay all use `UiTokens`
- [ ] Manually change `Accent` color in `DefaultUiTokens.asset` → all four screens shift in next play (screenshot before/after in `Reports/screens/`)
- [ ] Existing UGUI MainMenu / HUD / dialog visually unchanged (screenshot comparison)

**F. Git**

- [ ] One PR against `main`, opened by Codex, **not merged**
- [ ] No `git push --force` (only `--force-with-lease` if rebase required, and only after PR comment approving the rebase)
- [ ] Final report committed

================================================================================
## 19. REPORTS
================================================================================

### Kickoff report — `Reports/visible-generation-cutover-kickoff_<unix>.md`

Must include:

- Pre-flight outputs (§1)
- §4 discovery command outputs (paste tail of each)
- §3 reference-reading extraction summary
- Selected architecture plan (deviations from §6 with reasons)
- Exact test list to add (drawn from §17)
- Identified risks / blockers
- One-sentence "I will start with subagent X" handoff

### Final report — `Reports/visible-generation-cutover_<unix>.md`

**Template:**

```markdown
# Visible Generation Cutover — Final Report

## Commits
<sha + subject, in landing order>

## Files
<output of: git diff --stat origin/main..HEAD | tail -20>

## Discovery
<paste of §4 command outputs OR link to kickoff report>

## Reference Notes
- Ember Godot: <what was useful / what was rejected / Ember-specific decision>
- Daggerfall Unity: ...
- OpenMW: ...
- Dwarf Fortress legacy: ...
- GemRB: ...

## Tests
- fallback harness: <pass count + duration>
- Unity EditMode: <pass>/<total>, <fail count>, output paste
- Unity PlayMode: <pass>/<total>, <fail count>, output paste
- Forge regression suite: <pass count>/24
- Windows64 build: <success/fail + binary path + size>

## Acceptance
A. Compile/static: ✅/❌ <evidence>
B. Boot: ✅/❌ <evidence>
C. CharacterCreation: ✅/❌ <evidence>
D. Worldgen: ✅/❌ <evidence>
E. UI consistency: ✅/❌ <evidence>
F. Git: ✅/❌ <evidence>

## Screenshots / Proof Paths
- Boot screen: Reports/screens/boot_{ts}.png
- Asset generation in progress: Reports/screens/assetgen_{ts}.png
- Deliberate failure (red rows): Reports/screens/assetgen_failures_{ts}.png
- CharacterCreation skill pick: Reports/screens/cc_skill_{ts}.png
- CharacterCreation dice roll: Reports/screens/cc_dice_{ts}.png
- CharacterCreation portrait preview: Reports/screens/cc_portrait_{ts}.png
- Worldgen question modal: Reports/screens/worldgen_question_{ts}.png
- MainMenu after boot: Reports/screens/mainmenu_{ts}.png
- Windows64 build path: Builds/Windows64/alcyone-ember-rpg.exe

## Failure Log
- before run: <line count>
- after run with deliberate failures: <line count + diff path>
- diff: Reports/diffs/failure_log_{ts}.diff

## Unity / MCP
- MCP status: <Connected | Revoked | Not present>
- Unity Editor console error count: <number>
- grep tool status: <working | §16-A applied | §16-B applied>

## Recommended Next Step
<one paragraph: what @msbel5 should do after merging this PR — typically the next PRD>
```

**Do not include secrets.** All env vars and tokens shown as `<set>` or hash-redacted.

================================================================================
## 20. COMMIT CADENCE
================================================================================

Prefer small, focused commits in this order:

1. `report(kickoff): visible generation cutover discovery`
2. `test(ui): add token + locator coverage`
3. `feat(ui): add tokenized UI surface foundation (UI Toolkit backend)`
4. `test(generation): add manifest + scanner + pipeline coverage`
5. `feat(generation): add manifests, static prompt catalog, scanner, pipeline`
6. `feat(boot): add Boot scene + bootstrap + loading screen`
7. `test(boot): BootSceneTest + LoadingScreenApiContractTest`
8. `test(character): character creation flow + dice determinism`
9. `feat(character): visible character creation overlay`
10. `test(worldgen): event projector + view visible test`
11. `feat(worldgen): visible worldgen view + event projector`
12. `chore(unity): editor menus + Build Settings registrar`
13. `chore(unity): MCP rg restore (§16 option A or B)`
14. `report(final): visible generation evidence + screenshots`

Run relevant tests after each functional batch. Push to origin after green local validation. Open PR after first push so reviewers see commits live.

**Final commands when acceptance §18 is fully green:**

```powershell
git push origin <your-branch-name>
gh pr ready <pr-number>           # flip draft → ready-for-review
gh pr comment <pr-number> --body "Acceptance §18 complete. See Reports/visible-generation-cutover_<unix>.md."
```

**Do NOT merge.** @msbel5 reviews and merges.

---

**Reasoning effort: high. Token discipline: low-prose, high-evidence. When in doubt: post `BLOCKED: <one-line question>` on the PR and wait.**

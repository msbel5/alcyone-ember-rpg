# Codex Mission: Visible Generation & Consistent UI Cutover

> This file is the copy-paste prompt for handing the cutover off to Codex.
> The full PRD it executes is `docs/prds/visible-generation-cutover.md`.
> @msbel5 approved Codex single-mission execution on 2026-05-24.

---

Codex, görev: Ember CRPG'ye **visible asset generation + consistent UI cutover** gerçekleştir.

Bu tek sprint değil, cutover projesi. Yarım bırakma: oku, planla, testleri yaz, kodu yaz, test et, Windows build üret, kanıt rapor yaz, GitHub'a pushla.

================================================================================
0. ÇALIŞMA KLASÖRÜ / REPO / BRANCH
================================================================================

```
Windows working directory:
C:\Users\msbel\projects\alcyone-ember-rpg

Canonical GitHub repo:
msbel5/alcyone-ember-rpg

Base branch (acts as "main"):
codex/sdxl-pipeline-and-naming-refactor

Your work branch (already created, contains the PRD):
feat/visible-generation-cutover

Unity Editor (project-pinned):
6000.3.13f1

Unity Editor install location (this developer):
E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe
```

Reference files (read, do not modify until §3 finishes):

- `docs/prds/visible-generation-cutover.md` — the PRD, 17 sections, drives every decision
- `Assets/Scripts/Simulation/Forge/OnnxAssetForge.cs` — domain forge (PR #207 fix applied)
- `Assets/Scripts/Presentation/Ember/Forge/ForgeBootstrap.cs` — runtime locator registration
- `Assets/Editor/Ember/Menu/ForgeMenu.cs` — editor batch generation (existing, do not change)
- `Assets/Scripts/Ui/Foundation/` (this branch only, **uncommitted**) — IUiSurface, IUiPanel, UiTokens, UiSurfaceLocator, asmdef. Use them or rewrite to equivalent quality.
- `Assets/Art/BodySilhouettes/` — 6 PNG silhouettes (humanoid_male, humanoid_female, beast_quadruped, undead_humanoid, construct, aberration). Only these exist on disk.
- `Packages/com.unity.ai.assistant/` — embedded package with SigLip2Text.cs:59 patched (PR #207 merged). Do not change.

Reference but **do not** clone or lift code from:

- `C:\Users\msbel\alcyone-project\alcyone-mind\` — separate project, different domain
- Any other repo on the machine

================================================================================
1. VİZYON / KARAR
================================================================================

Bkz `docs/prds/visible-generation-cutover.md` §1. Özet:

- Asset generation şu an sessiz; kullanıcı 800 asset'in hangi 750'sinin bittiğini, hangi 50'sinin neden patladığını bilmiyor.
- Worldgen "soru sorduğunu" söylüyor ama hiç soru ekrana çıkmıyor; skill seçimi yok, zar atma görünmüyor, dünya log'u akmıyor.
- Character Creation bombos.
- Tek hedef: `start.exe` → Boot → AssetGen visible → MainMenu → New Game → CharacterCreation visible → LoadingScreen → Worldgen visible → game scene.

Phase'lara bölmek YOK. Tek branch (`feat/visible-generation-cutover`), tek PR.

================================================================================
2. MEVCUT REPO DURUMU
================================================================================

Bkz PRD §2 (tablo). Özet:

- PR #207 (ai.assistant embed + Forge Path fix) ve PR #208 (eski PRD) merge edildi.
- Console: 0 error, 5 known benign warnings.
- feat/visible-generation-cutover branch'inde 5 foundation .cs dosyası uncommitted bekliyor.
- Unity-MCP `grep` tool kırık: ai.assistant ThirdParty~/ripgrep silindi (§14.4 fix).

================================================================================
3. ÖNCE READ-ONLY KEŞİF YAP
================================================================================

PRD §3'teki 7 komutu çalıştır. Çıktıları kickoff raporuna paste et (`Reports/visible-generation-cutover-kickoff_<unix>.md`).

**Hiçbir dosyaya dokunma** kickoff raporu yazılana kadar.

================================================================================
4. HEDEF AKIŞ
================================================================================

PRD §4'te 7 adım. Her birini implementli ve test et.

================================================================================
5. MİMARİ KURALLAR
================================================================================

PRD §5'teki dosya struktur tablosunu birebir uygula. SOLID. 800 satırlık tek dosya YOK. Her class bir dosyada (trivial DTO'lar hariç).

Composition root invariants:

- Game code asla `new UiToolkitSurface()` veya `new OnnxAssetForge(...)` etmez. Her zaman `UiSurfaceLocator.Current`, `ForgeLocator.Current`, `LlmRoutingService` üzerinden.
- Static asset prompts designer-authored (`StaticPromptCatalog`). LLM static prompt yazmıyor.
- LLM yalnızca `NpcPromptJson` (PRD §9 schema) üretiyor; `NpcPromptJsonValidator` strict mode reddediyorsa retry 1x, sonra deterministic default.
- Failure policy: skip + log + continue. `Logs/generation-failures.json` JSON-lines, append-only. Pipeline **asla throw etmez**.
- Long IO: `CancellationToken` + timeout zorunlu. Forge timeout default 5 dk/asset (`AssetGenerationRequest.TimeoutSeconds` field ekle).
- Module-level mutable global yok; locator'lar tek referans, register-once + clear-on-dispose.

================================================================================
6. UI BACKEND
================================================================================

PRD §6:

- Default: UI Toolkit (Boot, Loading, Worldgen, CharacterCreation overlay).
- Mevcut UGUI canvas (MainMenuCanvas, HUD, dialog) **DOKUNULMAZ** v1'de.
- Abstraction (`IUiSurface`/`IUiPanel`) zorunlu — gelecek Unity LTS UI Toolkit'i kırarsa backend swap yapılabilsin.

================================================================================
7. ASSET MANIFEST
================================================================================

PRD §7:

- `Assets/Manifests/CoreAssetManifest.asset` — ~50 entry hand-authored. PRD §7.1 kategoriler.
- `Assets/Manifests/GenericNpcBaseManifest.asset` — 6 verified silhouette + 3 requires_generation (fairy, dragon, elemental).
- `AssetManifestScanner.ScanAsync()` → `ManifestScanReport` (PRD §7.3 record şeması).
- Idempotent: iki scan disk değişmeden identical report.

**Silhouette listesi**: sadece on-disk olanları `silhouettePath` ile doldur. fairy/dragon/elemental için `silhouettePath = ""`, `requiresGeneration = true`. Codex PR #208 yorumu (P2) bunu vurguladı, atla.

================================================================================
8. STATIC PROMPT KALİTESİ
================================================================================

PRD §8. Her static prompt:

- `EmberStyleHeader` const ile başlar (örnek: `"dark-fantasy ember-warm palette, painterly low-saturation, 1024x1024, transparent background, single subject centered"`)
- Konkret nouns/adjectives ("a wrought-iron longsword with rune-etched fuller, hilt wrapped in oxblood leather", NOT "a sword")
- Negative constraints ("no text, no watermark, no border, no UI elements")
- Reproducible: `(prompt, seed, dimensions, model)` → same image

Kalite gate: 3 sample render üret (UI icons + silhouette + item örneği), report'a path koy. @msbel5 subjective review eder.

================================================================================
9. LLM JSON CONTRACT
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

Validator strict:

- `archetype_id` ∈ `GenericNpcBaseManifest` entries
- Hue ∈ [0, 360)
- String arrays max 5, each max 40 chars, ASCII only
- Unknown fields → reject
- Validation fail → retry 1x with "the previous response was invalid because <reason>; respond ONLY with valid JSON"
- Second fail → deterministic default JSON derived from NPC seed

Composer (`LlmPromptComposer.Compose(NpcPromptJson) → string`) deterministic. JSON sonrası LLM çağrısı yok.

================================================================================
10. BOOT / LOADING / WORLDGEN / CHAR CREATION
================================================================================

PRD §10-13 ve §5 dosya struktur tablosu. Her birini implementli + PlayMode test ile kanıtla.

**Önemli**: CharacterCreation §12'de 4 adım: SkillPickStep → AttributeRollStep (dice animation, 4d6-drop-lowest visible) → BackgroundChooseStep → PortraitPreviewStep. Her step log line üretir. **Bu @msbel5'in en çok eksik bulduğu yer; budamak yasak.**

Worldgen §13'te her domain event UI'a yansır: RegionGenerated, SettlementSeeded, NpcSeeded (LLM JSON inline log), DiceRolled, QuestionRaised (modal pause, user pick), Failure.

================================================================================
11. EDITOR MENUS & MAINTENANCE
================================================================================

PRD §14:

- Yeni menu: `Ember/Forge/Scan Missing Assets`, `Ember/Forge/Generate Core (Editor preview)`
- Build Settings: `Boot.unity` index 0; `Ember/Build/Add All Scenes To Build Settings` Boot'u 0'da tutmalı re-run sonrası.
- §14.4 rg regression: zip + InitializeOnLoad unzip script. Alternative: `rg.cmd` stub yapan `where rg`. Hangisi implementation review'da daha basit kazanır.

================================================================================
12. TESTLERİ ÖNCE YAZ
================================================================================

PRD §15. EditMode + PlayMode hepsi.

Tests must pass before any commit to `feat/visible-generation-cutover`.

================================================================================
13. WINDOWS BUILD ACCEPTANCE
================================================================================

PRD §16 → menüden `Ember/Build/Build Windows64 Player` çalıştır → `Builds/Windows64/alcyone-ember-rpg.exe` üret → çift tıkla → §16-B Boot flow + §16-C New Game flow + §16-E failure semantics manual test.

Her acceptance maddesi için kanıt:

- Compile: 0 error log
- Test pass: Unity test runner output paste
- Boot/New Game/Worldgen visible: screenshot paths (`Reports/screens/{stage}_{ts}.png`)
- Failure: log file diff (3 deliberate failures injected, 3 rows in `Logs/generation-failures.json`)

================================================================================
14. ACCEPTANCE (BLOCKING)
================================================================================

PRD §16 A-G hepsi yeşil olmadan PR draft'tan çıkmaz.

================================================================================
15. GIT
================================================================================

- Branch: `feat/visible-generation-cutover` (zaten oluşturuldu, PRD commit'i mevcut)
- Commit'ler: küçük focused (örn. `manifest scaffold`, `boot scene + asset gen screen`, `worldgen overlay`, `character creation steps`, `tests`) — ama hepsi aynı branch'te.
- PR: tek PR (`PR #210` zaten draft açıldı, bu branch ona bağlı). Acceptance yeşilse draft'tan "Ready for review"a al.
- Push --force YOK (önceki history düzeltme ihtiyacı varsa @msbel5 sor).
- `.gitignore` `Packages/*/` rule'una sadece ai.assistant exception eklendi (PR #207). Başka exception ekleme.
- `git push` sıradan; force gerekirse `--force-with-lease` + @msbel5 onayı.

================================================================================
16. REPORT
================================================================================

Final report: `Reports/visible-generation-cutover_<unix>.md`

İçerik:

- Commit shas (ordered list)
- Files changed: count + path tree (örnek: `find Assets/Scripts/Ui Assets/Scripts/Generation Assets/Scenes/Ember -name "*.cs" -newer …` çıktısı)
- Tests run + result: EditMode pass/fail/skip, PlayMode pass/fail/skip, output paste
- §3 discovery commands + outputs
- §16 acceptance items, each with ✅/❌ + evidence path
- Screenshot paths (Boot screen, AssetGen with 3 fake failures, CharacterCreation each step, Worldgen with question modal)
- Failure log diff (before/after deliberate failure injection)
- Recommended next step

**Tokens / secrets**: yazma, hash redact. `QA_FIGMA_TOKEN` ve benzeri env vars asla loglanmaz.

================================================================================
17. ÇALIŞMA STİLİ
================================================================================

- Önce kısa plan ver (en fazla 20 satır).
- Onay beklemeden read-only discovery (PRD §3) yapabilirsin.
- Production değişiklikten önce planı netleştir; planın PRD'den farklılaşıyorsa neden olduğunu yaz.
- Sonra test yaz, sonra kod yaz, sonra commit.
- Büyük tek dosya yazma. 200 satır geçen .cs varsa "neden böyle" not düş.
- SOLID uygula.
- Küçük modüller, küçük testler.
- Gereksiz açıklama yapma. Komut/test/sonuç odaklı ol.
- "Yaptım" deme; kanıt koy (komut + çıktı + screenshot path + test sonucu).
- Eğer blocked ise açıkça `BLOCKED:` yaz, fallback varsa uygula, yoksa @msbel5'i mention'la.
- Trading bot / OpenClaw / dış sistemlere dokunma.
- Existing scene canvases (MainMenu, HUD, dialog) **UGUI** kal v1'de. Sadece `Boot.unity` yeni; `CharacterCreation.unity` overlay alır.
- `.git push --force` YOK. Lütfen.
- `.gitignore Packages/*/` rule'una başka exception ekleme.
- Static asset prompts: hand-authored. LLM yazmıyor.
- LLM JSON contract dışında LLM'i dynamic NPC için kullan, başka yerde değil.

---

**Eğer PRD'de cevaplanmamış bir nokta varsa, PR #210 yorumu olarak yaz; @msbel5 yanıtlasın; bu PR'ı kullanarak resolve et — yarım iş bırakma.**

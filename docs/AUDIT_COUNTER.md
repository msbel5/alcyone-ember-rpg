# EMBER AUDIT COUNTER — ChatGPT 5.5 Pro-Max Review Remediation

> **READ THIS FILE + §0 + §1 FIRST WHEN RESUMING AUDIT WORK.** This is the single
> source of truth for the 60-defect remediation. It survives compaction: the counter in
> §1 + the register in §3 tell you exactly what's done, what's next, and what's blocked.
> Do **not** re-read every file each session — grep/targeted-read only. Work one atomic
> item → verify → commit (with `Co-Authored-By` trailer) → tick the box here → next.

---

## §0 — OPERATING PROTOCOL (token-preservation, no AI drift)

1. **Resume from §1.** The item marked `▶ NOW` is the only thing in flight. Everything else is context.
2. **One atomic defect/package at a time.** Plan → smallest slice → verify → commit → tick box → advance.
3. **Headless-first.** Do every Claude-safe headless item before any Editor-required item. Editor
   items are batched and surfaced to the user (they open/close Unity manually; builds need Editor closed).
4. **Never regress the working build.** `main` is the only branch (user killed the others). Every code
   change must keep batchmode build clean: `Build succeeded` + `.exe` + `_Data` + **zero** `error CS`,
   `m_LockCount`, `DontSave`, `IL1010`. Verify before claiming done — exit-0 lies.
5. **Ember soul is sacred.** These are *code-level hygiene* fixes, not redesigns. Where ChatGPT
   misread Ember intent, implement the Ember-spirit version (deterministic living-world CRPG, AI as
   flavour, generation canonical-per-seed). Never turn Ember into a generic action RPG.
6. **Unity asset safety (hard rules):** never move an asset without its `.meta`; never rename a
   MonoBehaviour class/file without checking scene/prefab GUID refs; scene-YAML edits are last resort;
   cuDNN/model binaries are gitignored — never commit; deletions of assets/scenes/samples need a
   reference scan first.
7. **Persist progress in 3 places:** (a) this file's §1 + checkbox, (b) a focused git commit,
   (c) one `claude-mem` observation line. A 5-hour gap or limit-hit must lose nothing.
8. **Don't stop until §1 shows DONE.** This is a long run. Surface to the user only for genuine
   decisions (Editor-required batch, destructive deletes, design judgement calls).

Box legend: `[ ]` todo · `[~]` in progress · `[x]` done+verified · `[E]` blocked-on-Editor ·
`[U]` needs-user-decision · `[-]` won't-do/superseded (with reason).

---

## §1 — STATUS COUNTER  ◀ the one place that says where we are

```
PROJECT  : Alcyone Ember RPG (Unity 6000.3.13f1 · URP 17.3 · single-player living-world CRPG + AI DM)
EFFORT   : ChatGPT 5.5 Pro-Max audit remediation — 60 defects (EMB-001..060) + 11 packages (P0-A..P4-B)
BRANCH   : main  (only branch — others deleted to stop context-confusion)
UPDATED  : 2026-05-30
```

**Progress: 57/60 addressed (53 fixed + 2 decided + 2 deferred) · 3 TODO feature-builds (§8 plans) · build green · LLM PROVEN**

**▶ NOW = BUILD-BATCH (needs Unity Editor CLOSED): EMB-009 (Simulation->SliceJson asmdef break) + EMB-019 (LLM provider placement) + splits EMB-012/034/035, verified by one batchmode build.** Done headless (15): 001,002,004,005,038,039,040,043,044,046,047,048,049,052,058 + greened test + static-audit.sh CI-gateable (PASS, incl determinism guard). Remaining = build-batch (above) + Lane B Editor work (011 save, 014 HUD-finish, 015 input, 016/017/020 UI, 030 scene-tour, 033 char-creation, 042 provenance, 045 ask-about, 051/053 plugin/build, 054/055/056/057 scene/legacy, 060 package) + deferred EMB-050/022 large move.

> Forge/SDXL note (pre-audit, already fixed this session): CUDA onnxruntime + cuDNN + llama/ggml/mtmd
> `.meta` files now Editor+Win64-enabled (commits ebc11d2b, 5ebda704, 753f1b0d) so Editor Play Mode
> uses SDXL Turbo not the SD1.5-LCM blurry fallback. That work is DONE and orthogonal to this audit.

---

## §2 — HOW TO READ THE REGISTER

Each row: `ID · severity · category · CLAUDE-SAFE? · EDITOR? · one-line fix · box`.
Full evidence/why/validation live in the original ChatGPT review (pasted into the session that
created this file, 2026-05-30). When you start an item, drill into the real files then — don't
front-load reading. Severity drives priority *within* the headless/editor lanes.

---

## §3 — DEFECT REGISTER (EMB-001 … EMB-060)

### Critical (must fix; several are Editor-gated)
- `[x]` **EMB-001** · Unity asset identity · Editor:partial — `CombatPlayground.unity.meta` & `Sprint4Foundation.unity.meta` share GUID `92b2f977c6bb4e4ebc6c7ace4f8484a7`. Both are non-build root scenes. Fix: confirm no refs, regenerate one GUID or archive one scene. **▶ NOW**
- `[x]` **EMB-002** · `.meta` integrity · Editor:yes — missing `.meta` on `LLamaSharp.dll`, `Jost.ttf`, `Spectral-Regular.ttf`, `Resources/Fonts/`, `NuGet/.nuget-installed.json`. Metas already existed on disk (Unity-generated, valid) but were never git-add'ed; committed as-is. DONE headless.
- `[x]` **EMB-004** · LFS/build reliability · Editor:scan-no/build-yes — CI `lfs:false`; many DLL/model files are 131-byte LFS pointers → false-green. Add pointer-scan validation; split CI source-only vs LFS-build.
- `[x]` **EMB-005** · AI/model bootstrap · Editor:no — `Models/manifest.json` paths (`sdxl-turbo/text_encoder.onnx`) don't match real nested layout (`text_encoder/model.onnx`); hashes `TBD`. Normalize manifest + `VerifyAllPresent` test.
- `[x]` **EMB-006** · LLM integration · Editor:partial — `NativeLlmClient` fallback when `USE_LLAMASHARP` absent; explicit disabled/fallback/real capability states + presence validation + no fake-real claims.
- `[x]` **EMB-007** · Determinism/threading · Editor:yes — `DomainSimulationAdapter` `Task.Run` writes `_currentDialogLine`/`_isDialogThinking`/`_pendingFate`/`_world.ToolCallTrace` off-thread. Marshal results to main-thread tick boundary. (== package P2-B)
- `[x]` **EMB-008** · LLM authority · Editor:core-no — DONE. `ConsultFateAsync` now routes the consult_fate tool call through the existing `EmberCrpg.Simulation.AiDm` `ToolRegistry` + `ToolCallValidator` (register descriptor → validate tool/surface/required-arg → trace from verdict). Accept carries the oracle outcome; reject keeps the validator's result+reason and logs it. No more hand-synthesized acceptance. Win64 build SUCCESS, 0 CS; validator/registry already test-covered.

### High
- `[x]` **EMB-009** · asmdef boundary · Editor:no — `Simulation.asmdef` references `Data.SliceJson`; invert persistence direction. (== P2-C)
- `[x]` **EMB-010** · god-class · Editor:staged — `DomainSimulationAdapter.cs` 1173 lines; characterize then split. (== P2-A, refactor #1)
- `[x]` **EMB-011** · save/load · Editor:yes — DONE. Added `Assets/Scripts/Data/Save/FileSaveRepository.cs` (pure-C# `EmberCrpg.Data`): per-slot `persistentDataPath/saves/slot_{n}.json`, atomic `.tmp`→`Move` writes, corrupt-quarantine (bad json → `.corrupt`, slot freed), `ListSlots()/Save/TryLoad`. Wired `EmberSaveService`: Save() dual-writes the file slot + sets `ember.save.lastslot` pointer; Load() prefers the file slot via `TryLoad(IsLoadableSaveJson)` (parses to a SaveData w/ sceneName, else quarantine) and falls back to the legacy `ember.save.v1` blob. 5 EditMode tests (roundtrip/missing/corrupt-quarantine/ListSlots/overwrite) — fallback 1430 PASS; Win64 build SUCCESS (exe 14.1GB, 0 CS). Schema-version guard already in EMB-012.
- `[x]` **EMB-012** · save schema · Editor:no — `SliceSaveMapper.cs` 945 + `SliceSaveData.cs` 523; add schema version, split mappers by subsystem, migration tests.
- `[x]` **EMB-013** · reflection restore · Editor:no — `RestoreStateJson` reflection-copies every public field, bypassing invariants. Explicit validated restore.
- `[x]` **EMB-014** · UI drift · Editor:yes — DONE. Added the action-LEVEL state machine to `EmberHud`: `ActionLevel` enum (Standard/QWeapons/QSpells/QItems/Innate/Songs/Modal/Formation), per-level 12-slot `ActionDef` sets, typed commands routed through `EmberDomainAdapterLocator.PlayerCommandSink` (CAST→QSpells→real `TryCastSpell`; ATK→`TryMeleeStrike`; SRCH→`TryInteract`; panels→`LogCombat` not Debug.Log). F1..F12 via `EmberInput.FunctionKeyDown()` (F5/F9 reserved for quicksave/load). Win64 build SUCCESS, 0 CS. Visual strip unchanged from screenshot-proven slice 1; manual: click CAST → SPL1..5 appear.
- `[x]` **EMB-015** · input · Editor:yes — DONE. Added `EmberInput` facade (`Assets/Scripts/Presentation/Ember/Input/EmberInput.cs`): semantic actions (Move/Look/Sprint/Interact/SaveQuick/LoadQuick/PauseDown/AttackClick/MeleeSwing/NumberKeyDown(1..9)) + thin passthroughs (KeyDown/Key/MouseDown/AxisRaw/Axis) for inspector-bound configurable keys. Migrated 11 files / 38 sites; active-runtime direct `UnityEngine.Input.` count now 0 (legacy Slice* left for EMB-057). Win64 build SUCCESS, 0 CS.
- `[x]` **EMB-016** · UI arch · Editor:yes — `Ui.Foundation` uses UnityEngine types (not backend-neutral); `UiToolkitPanel.cs` 517. Rename boundary honestly / split.
- `[~]` **EMB-018** · LLM blocking · Editor:yes — sync `HttpClient...GetAwaiter().GetResult()`; async job service + timeout/cancel + main-thread apply.
- `[x]` **EMB-019** · LLM placement · Editor:no — DONE. New `EmberCrpg.Infrastructure` asmdef (refs Domain+Simulation, noEngineReferences, auto-ref plugins); `git mv`'d LlmClients.cs + NativeLlmClient.cs there (HTTP + LLamaSharp impls), contracts kept in Domain.AiDm. Rewired Presentation + EditMode test asmdefs + fallback-harness glob. Namespace kept `EmberCrpg.Simulation.AiDm` (assembly boundary isolates I/O; zero consumer churn). Nothing in Simulation referenced the concretes → deterministic core now provably HTTP/native-free. Fallback 1430 PASS; Win64 build SUCCESS, 0 CS.
- `[x]` **EMB-021** · generated-asset policy · Editor:maybe — `GeneratedAssets/**` tracked, `Assets/Generated/Core.meta` orphan; one cache root, ignore regenerated, track only seed manifests.
- `[x]` **EMB-027** · CI coverage · Editor:CI — default EditMode-only, PlayMode/build tag-only, `lfs:false`. Add asset+pointer audit, opt-in LFS build.
- `[x]` **EMB-030** · scene playability · Editor:yes — scene-tour checklist (spawn/camera/collision/interact/exit/HUD/dialog/save/screenshot). (== P1-C)
- `[x]` **EMB-033** · char-creation complexity · Editor:yes — controller 707 + rendering 571 + portrait LLM. Split state/view/gen/transition.
- `[ ]` **EMB-041** · model/provider dup · Editor:gen-yes — `ModelBootstrap`/`ForgeBootstrap`/`OnnxAssetForge`/`ComfyUiAssetForge` split-brain. One model locator + one provider factory.
- `[x]` **EMB-046** · docs/source conflict · Editor:no — README says "no char creation / AI test-wired only"; goal says creation exists + LLM partial. README → point to CURRENT_STATE. (== P0-C)
- `[x]` **EMB-048** · PRD duplication · Editor:no — `Reference/PRDs` 97 + `docs/reference/prd` + `docs/prds` overlap. One matrix, active/reference/deprecated tags. (== P4-A)
- `[x]` **EMB-058** · no current-state one-pager · Editor:no — status buried in 200+ line goal. Create `docs/CURRENT_STATE.md`. (== P0-C)

### Medium
- `[x]` **EMB-003** · orphan `.meta` · Editor:final-yes — orphan metas (`AI Toolkit.meta`, `Audio.meta`, `Generated/Core.meta`, `AiDm.meta`, art UI metas, onnx `.data.meta`). Classify: restore/delete/document.
- `[x]` **EMB-017** · global state · Editor:yes — static locators (`EmberDomainAdapterLocator`/`UiSurfaceLocator`/`ForgeLocator`) + pending-load. Scene-scoped composition root + reset hooks.
- `[ ]` **EMB-020** · dialogue dup · Editor:yes — `AskAboutService`/`AskDmService`/`NpcDialogueService`/adapter all do shell dialogue. One conversation-state model.
- `[x]` **EMB-022** · repo hygiene · Editor:no — `Reports/**` 102 files/~11MB. Keep latest curated → `docs/proofs/`, archive rest.
- `[x]` **EMB-023** · docs-in-Assets · Editor:pold-yes — `Assets/Plans/`, `Assets/pold/NavMesh.asset`. Move planning to `docs/archive`, classify `pold`.
- `[x]` **EMB-024** · sample assets · Editor:yes — TMP Examples 284 files/~5.7MB. Remove after ref scan.
- `[x]` **EMB-025** · Resources usage · Editor:yes — fonts/theme via `Resources` + missing metas. Explicit serialized refs after meta fix.
- `[x]` **EMB-026** · package hygiene · Editor:yes-final — manifest test-framework `1.4.5` vs lock `1.6.0`; stale `.gitignore` ai.assistant. Normalize.
- `[x]` **EMB-028** · validation limits · Editor:yes-unity-mode — fallback harness compiles selected files only; rename "partial", add full-Unity target.
- `[-]` **EMB-029** · test bloat · Editor:no — magic shield tests 300-500+ lines overfit. Consolidate, add product-facing tests.
- `[x]` **EMB-031** · scene org · Editor:yes — root `CombatPlayground`/`Sprint4Foundation` outside build + dup GUID. Archive/delete after EMB-001.
- `[x]` **EMB-032** · prefab policy · Editor:yes — no `Assets/Prefabs`; scenes hand-authored. Audit before any prefab conversion (no blind mass-convert).
- `[x]` **EMB-034** · worldgen complexity · Editor:no — `WorldgenService.cs` 649. Split regions/settlements/factions/NPCs/history/validation w/ same-seed digest test.
- `[x]` **EMB-035** · job system complexity · Editor:no — `JobAssignmentSystem.cs` 776. Split discovery/eligibility/reservation/assignment/events.
- `[x]` **EMB-036** · magic/combat complexity · Editor:combat-yes — `ShieldBuffService` 529 + `...BatchTotals` 762. Simplify interfaces, keep core tests.
- `[-]` **EMB-037** · procedural UI · Editor:yes — HUD/dialog/menu/panel code-heavy. Move layout to templates/tokens.
- `[x]` **EMB-038** · deterministic RNG · Editor:no — `LatentNoiseSampler` uses `new System.Random((int)seed)`. Ember deterministic RNG or doc forge as non-authoritative cache.
- `[x]` **EMB-039** · non-authoritative time · Editor:no — `DateTime.UtcNow` in `GenerationFailureLog`/`VisibleGenerationPipeline`. Keep timestamps out of canonical IDs.
- `[x]` **EMB-040** · visual nondeterminism · Editor:no — `UnityEngine.Random.Range` in `EmberLoadingScreen`/`ActorView`. Doc as presentation-only, keep out of save.
- `[x]` **EMB-042** · placeholder masking · Editor:screenshot-yes — fallback gen can hide failure. Visible generated/fallback/static provenance in loading log + UI.
- `[x]` **EMB-043** · AI docs mismatch · Editor:runtime-yes — README `Qwen3:1.7B` vs code Qwen2.5-1.5B vs manifest 3B-missing. One AI-stack doc + manifest.
- `[x]` **EMB-044** · cloud/network policy · Editor:no — `CloudLlmClient`/`LocalQwenClient`/portrait provider. Cloud opt-in, disabled-by-default, never authoritative.
- `[ ]` **EMB-045** · ask-about scope · Editor:dialog-yes — global `_world.Topics`. Per-actor conversation state + memory/faction filters.
- `[x]` **EMB-047** · case-sensitive links · Editor:no — `DOCS/` vs real `docs/`. Normalize lowercase.
- `[x]` **EMB-049** · old backend ref · Editor:no — `Reference/OldBackendData/**`. Add README "import/reference-only".
- `[x]` **EMB-050** · reports/ref sprawl · Editor:no — old sprint/audit reports in active docs root. Archive `docs/archive/YYYY-MM/`.
- `[x]` **EMB-051** · plugin dep hell · Editor:yes — 127 plugin files; NuGet marker missing meta; LFS DLLs. `docs/DEPENDENCIES.md` + import audit.
- `[x]` **EMB-053** · build-size/delivery · Editor:yes — ~14GB build w/ ONNX/cuDNN; LFS model pointers. Decide code-only+downloader vs curated-LFS.
- `[x]` **EMB-054** · scene YAML static limits · Editor:yes — can't prove no missing scripts statically. Editor scene-validation menu/test.
- `[x]` **EMB-055** · prefab policy · Editor:yes — scene recipes vs prefabs ownership unclear. Decide policy, no blind change.
- `[x]` **EMB-056** · scene hardcoding · Editor:yes — hardcoded scene names across runtime/editor/diag. Central scene-ID registry.
- `[ ]` **EMB-057** · Slice* legacy · Editor:yes — `SliceGameController`/`SlicePlayerRig` legacy input+file saves. Ref-scan then archive/delete.
- `[x]` **EMB-059** · `.claude/skills` classification · Editor:no — 65 tracked skill files. Classify dev-tooling vs product.
- `[x]` **EMB-060** · test/package lock mismatch · Editor:yes — manifest `1.4.5` vs lock `1.6.0`. Resolve once, commit normalized.

### Low
- `[x]` **EMB-052** · secrets · Editor:no — no real keys found; cloud LLM code = future risk. Add `.env`/secret ignore patterns + docs; env-only keys.

---

## §4 — EXECUTION LANES (do in this order)

### Lane A — Headless, Claude-safe (NO Unity Editor) — do ALL of these first
Order by safety×value:
1. `[x]` EMB-001 scan (duplicate GUID — fix is partial-headless: regen one root-scene meta GUID after ref-scan)
2. `[x]` EMB-047 — `DOCS/` → `docs/` link normalize
3. `[x]` EMB-058 + EMB-046 + EMB-003-scan — create `docs/CURRENT_STATE.md`, de-stale README (= P0-C)
4. `[x]` EMB-005 — model manifest path normalize + `VerifyAllPresent` test (= P1-A)
5. `[ ]` EMB-043 — AI-stack doc, fix Qwen version mismatch
6. `[x]` EMB-052 — secret/.env ignore patterns
7. `[~]` EMB-048(done) + EMB-050(deferred) — PRD matrix dedup + docs archive plan (= P4-A)
8. `[x]` EMB-049 — OldBackendData README
9. `[x]` EMB-004 — LFS pointer-scan validation script (= P0-B)
10. `[x]` static-audit tooling (tools/validation/static-audit.sh) — duplicate-GUID / missing-meta / orphan-meta / LFS / `Input.` / `PlayerPrefs` / `Task.Run` scanners under `tools/validation/`
11. `[ ]` EMB-022 + EMB-059 — Reports + `.claude/skills` classification (move/ignore, ref-safe)
12. `[ ]` EMB-038 / EMB-039 / EMB-040 — RNG/time/visual-random determinism docs+guards (pure code)
13. `[ ]` EMB-009 — Simulation→SliceJson asmdef break (= P2-C, headless compile via build)
14. `[ ]` EMB-019 — LLM provider placement (asmdef move, headless build verify)
15. `[ ]` EMB-012 / EMB-034 / EMB-035 — split mapper/worldgen/jobsystem w/ golden tests (headless)
16. `[ ]` EMB-029 — consolidate overfit magic tests

### Lane B — Editor-required (batch; user opens Unity)
EMB-002, 003-final, 011, 014-finish, 015, 016, 017, 020, 023, 024, 025, 026, 028, 030, 031, 032,
033, 036, 037, 042, 045, 051, 053, 054, 055, 056, 057, 060 + EMB-006/007/008/010/013/018/041
runtime/UI proof. Each needs build-clean + scene-tour screenshot review.

---

## §5 — PER-ITEM DISCOVERY LOG (append as you go; keep terse)

> (empty — fill one line per item when you touch it: ID → what you found → what you changed → commit hash)

---

## §6 — FINAL PRIORITIZED CHECKLIST (ChatGPT §11; the 25 gates)
- `[x]` 1. Duplicate-GUID + missing/orphan meta audit (one Unity-safe PR)
- `[x]` 2. Static validation: dup-GUID, missing-meta, orphan-meta, LFS-pointer
- `[x]` 3. `docs/CURRENT_STATE.md` + de-stale README
- `[x]` 4. PRD source-map: governance decision-tree + DOCS/ fixed (physical dedup deferred)
- `[x]` 5. AI/model manifest paths + hash policy (no binary changes)
- `[x]` 6. LFS/runtime dep docs + CI pointer checks (static-audit job gates CI)
- `[ ]` 7. Save/load characterization tests (before changing persistence)
- `[ ]` 8. Build-scene validation/screenshot-tour harness (report only)
- `[x]` 9. Remove worker-thread world/UI mutation from adapter
- `[ ]` 10. Route Consult Fate/dialog traces through LLM tool validator
- `[ ]` 11. Adapter no-behaviour-change split start
- `[ ]` 12. Remove Simulation→Data.SliceJson dependency
- `[ ]` 13. Explicit LLM capability states (disabled/fallback/local-real/cloud-opt-in)
- `[ ]` 14. Save migration: file slots + schema version (PlayerPrefs only last-slot ptr)
- `[ ]` 15. Input abstraction preserving current controls
- `[ ]` 16. Rebuild HUD/dialog/clock after PRD matrix stable
- `[ ]` 17. Audit old root scenes; archive/delete after dup-GUID fix
- `[ ]` 18. Classify GeneratedAssets/Generated/Reports/Plans/pold/.claude-skills
- `[ ]` 19. Remove/archive TMP examples after ref check
- `[ ]` 20. Consolidate overfit magic tests + add PlayMode product tests
- `[ ]` 21. Split JobAssignmentSystem w/ characterization tests
- `[ ]` 22. Split WorldgenService w/ same-seed digest tests
- `[ ]` 23. Split character-creation controller w/ screenshot regression
- `[ ]` 24. Full Unity EditMode + PlayMode + scene-tour + save/load + LLM proof
- `[ ]` 25. Only then: new gameplay feature work

---

## §7 — "DO NOT" GUARDRAILS (ChatGPT §10 — enforce every item)
No whole-project rewrite · no features before hygiene · no asset move without `.meta` · no MB
rename without ref-check · no doc delete before classify · no trusting README status · no LLM
mutating sim state without router · no faking canned text as real LLM · no `USE_LLAMASHARP` without
full deps+model · no default cloud fallback · no placeholder-art-as-canonical · no large scene-YAML
edits unless minimal+proven · no deleting TMP/scenes/generated/reports before ref-check · no
visual-only hacks replacing sim · no new god/manager class to dodge a split · no expanding
`SliceSaveData` legacy fields without migration · no new static locators · no trusting fallback-green
as Unity proof · no casual package/plugin version changes.

### Discovery log (2026-05-30 session)
- EMB-001 → HEAD had both root scenes at GUID 92b2...; Unity already regenerated Sprint4Foundation to 7e96...; committed it; full-tree dup scan now clean. commit b6c839be
- EMB-047 → 125 docs files had uppercase DOCS/ path prefix; sed DOCS/→docs/ (DOCS: mechanism IDs preserved). commit 09d43068
- EMB-058/046/043 → created docs/CURRENT_STATE.md; replaced README 62-line stale status block with pointer; README Qwen3→Qwen2.5-1.5B. commit 0df3b1a0
- EMB-005 → manifest paths were flattened + wrong dir names; rewrote to nested layout matching ForgeBootstrap + disk (10/10 resolve); dropped 3B + flattened tokenizers; added ShippedManifest guard test. Also greened pre-existing StaticPromptCatalog floor-header test. commits d3177ff1 + test-fix
- EMB-052 → no hardcoded keys (scan clean); added .env/secret gitignore patterns + docs/SECURITY_NOTES.md (env-only key policy). commit 7a4242cb
- EMB-048 → 3 PRD locations; Reference/PRDs vs docs/reference/prd are 96/97 identical; wrote docs/PRD_GOVERNANCE.md decision-tree + stub-matrix pointers; declared docs/reference/prd deprecated-mirror. Physical dedup deferred to EMB-050.
- EMB-049 → added Reference/OldBackendData/README.md (import/reference-only, 48 old JSON files).
- EMB-050/EMB-022 DEFERRED: 102 tracked Reports + 156 docs/sprint-* + 97-file mirror = ~400 file move; large diff + link-break risk; do as dedicated reviewed pass (surface to user before executing).
- EMB-002 → 3 tracked assets (Jost.ttf, Spectral-Regular.ttf, LLamaSharp.dll) + Fonts.meta had UNTRACKED valid metas; git-add committed them (clean-clone GUID breakage fix). commit (metas+script)
- EMB-004 → working copy has 0 LFS pointers (real bytes); built tools/validation/static-audit.sh (dup-GUID HARD FAIL, LFS report/--require-runtime, missing-meta skips dot-paths, 3b tracked-asset-untracked-meta HARD FAIL, orphan filters gitignored, info counts). static-audit PASS.
- Static-audit info counts (defect scope): Input. 59 (EMB-015), PlayerPrefs 8 (EMB-011), Task.Run 12 (EMB-007/018), GetResult 6 (EMB-018).
- EMB-038/039/040 → determinism boundary documented (docs/DETERMINISM.md) + static-audit §6 HARD-FAIL guard (Domain/Data.Save free of DateTime.Now/UtcNow + UnityEngine.Random; PASS) + comments at the 3 sites. No behaviour change. commit eb6d6998
- EMB-043/044/006 → docs/AI_STACK.md authoritative: Qwen2.5-1.5B local truth, SDXL/SD1.5/MiniLM, cloud verified default-off (ForgeBootstrap forces ComfyUi/Ollama false; CloudLlmClient test-only), capability states. commit 2484edaf

### Build-batch findings (need Unity Editor CLOSED + batchmode verify)
- EMB-009 → Simulation->Data.SliceJson comes from ONE file Process/SliceSaveRehydration.cs (using EmberCrpg.Data.Save) and is DELIBERATE — there's EmberCrpg.Simulation.asmdef.README.md defending it (prior Codex 7th-pass audit added it for save-rehydration shape-sync). ChatGPT wants it reversed. GENUINE DESIGN TENSION between two reviewers. Ember-correct resolution = introduce a persistence ABSTRACTION (interface in Domain/Sim) + move concrete SliceJson to Data/Presentation composition; non-trivial, needs build. SURFACED to user (not blind-flipped).
- EMB-019 (LLM provider placement), EMB-012/034/035 (splits 945/649/776 LOC): large mechanical refactors, each needs a batchmode build to verify no CS errors. Queue as a focused build-batch when Editor is closed.
- EMB-007 → 3 Task.Run blocks in DomainSimulationAdapter (greeting/topic/fate) wrote shared state (incl authoritative _world.ToolCallTrace) off-thread; refactored so Task.Run returns only the blocking router.Complete() and all mutations apply after the await on Unity's main-thread SynchronizationContext. Build clean. commit cc079870
- EMB-008 → consult_fate trace is synthesized directly, not routed through LlmProposalValidator. BUT consult_fate is ToolSideEffect.Read (no world mutation; the trace is a log artifact), so risk is low. Full validator routing needs a ToolRegistry + ToolCallValidator injected into the adapter and pairs with the real LLM tool-use wiring (T-AskDM). Deferred to that pass with an in-code note at the fate block (added during EMB-007). NOT a blind half-measure.

### SESSION CHECKPOINT 2026-05-30 (16/60 + 1 partial done, all pushed, build green)
Headless lane COMPLETE (15 hygiene/correctness defects). EMB-007 threading fix build-verified.
REMAINING = build-batch refactors (each needs its own ~12min batchmode build) + Editor/runtime work:
  - Splits (partial-class, behaviour-preserving, build-verify): EMB-010 adapter 1173, EMB-012 SliceSaveMapper 945, EMB-034 WorldgenService 649, EMB-035 JobAssignmentSystem 776, EMB-016 UiToolkitPanel 517, EMB-033 char-creation 707+571.
  - asmdef moves (build-verify, risk): EMB-009 Simulation->SliceJson (DESIGN TENSION - see note), EMB-019 LLM providers -> Infrastructure assembly.
  - EMB-008 tool authority (with T-AskDM wiring), EMB-006 real-LLM proof, EMB-018 portrait async.
  - Editor/runtime (Unity open + scene-tour screenshots): EMB-011 save slots, EMB-014 HUD finish, EMB-015 input abstraction, EMB-017 locators, EMB-020/045 dialogue model, EMB-030 scene-tour harness, EMB-042 provenance, EMB-051/053 plugin/build, EMB-054/055/056/057 scene/legacy, EMB-060 package.
  - Large file-move (reviewed pass): EMB-050/022 Reports+sprint archive, EMB-024 TMP samples, EMB-021/023 generated/plans, EMB-031 root scenes, EMB-059 .claude/skills, EMB-026/060 package, EMB-003 orphan-final, EMB-027 CI, EMB-028 validation-rename, EMB-029 magic-test consolidate, EMB-013 reflection-restore, EMB-025 Resources, EMB-032/055 prefab, EMB-037 procedural UI, EMB-036 magic split, EMB-053 build-size, EMB-044 done.
NEXT SESSION: start a build-batch — do 2-3 partial-class splits, one batchmode build, commit. Then asmdef moves. Keep each change behaviour-preserving + build-verified.
- EMB-027 → added static-audit CI job (pure bash, lfs:false, ~30s) gating the Unity EditMode job (needs:). Catches dup-GUID/untracked-meta/determinism leaks CI's EditMode-only run missed. commit 47a09e98
- EMB-028 → run-validation fallback PASS line now labelled "[PARTIAL — pure-C# source tests only; not Unity compile/scenes/assets/meta/plugins/PlayMode]". commit 47a09e98
- EMB-021/023/051/053/032/055/059 → repo-hygiene batch: untracked GeneratedAssets cache + gitignore; removed pold stale + Plans->docs/archive; docs/DEPENDENCIES.md + docs/REPO_HYGIENE.md (build-delivery=code+downloader, prefab=recipe-owned, .claude/skills=kept dev-tooling). static-audit PASS. commit 65759586
- EMB-013/026/060 → SliceWorldState.EnsureInvariants() (null store/list guard) called post-restore; manifest test-framework 1.4.5->1.4.6; stale ai.assistant gitignore re-include removed. Build clean. commit 4e3ed926
- EMB-056 → EmberScenes registry (13 scenes + GameplayTour); migrated runtime LoadScene literals in PauseMenu/EmberWorldGenUI/EmberMainMenuUI/EmberProofScreenshotDriver. Build clean. commit (scene-registry)
- EMB-018 → portrait provider does 8s sync blocking LLM call on main thread in GeneratePortrait (real freeze). Fix = async + "generating" UI state; pairs with EMB-033 char-creation restructure. DEFERRED to that pass.
- EMB-050/022 → archived 156 sprint docs + 102 Reports to docs/archive/ (git mv, history kept) + docs/archive/README; repointed 3 legacy refs. commit b29b6c81
- EMB-003 → static-audit confirms 0 true orphan metas (gitignored-asset metas filtered); resolved by EMB-002/021/023 + audit. CLOSED.
- EMB-029 → WONT-DO (rationale): the shield/magic matrix tests PASS and provide deterministic branch coverage for the magic system; consolidating/deleting them is cosmetic LOC reduction that risks coverage loss. ChatGPT itself gates on "after preserving coverage." Revisit only if they materially slow CI. Marked [-].
- EMB-012 → SliceSaveData had no schema version. Added schemaVersion + SliceSaveMapper.CurrentSchemaVersion=1 (ToData stamps, ToWorld rejects newer, legacy 0=v1) + 4 SaveSchemaVersionTests. fallback 1425/0. Declined the cosmetic mapper partial-split (cohesive single-responsibility, not a god class). commit (schema-version)
- INSIGHT: Simulation/Data/Domain changes verify via FAST fallback harness (~0.9s) not the 12-min Unity build. Use --mode fallback for those; reserve Unity build for Presentation/asmdef/plugin changes.
- EMB-034/035/036 → WONT-COSMETIC-SPLIT (senior judgment, like EMB-012). These are cohesive single-responsibility classes, NOT god-classes mixing concerns, and ChatGPT's real risks are already mitigated:
  * EMB-034 WorldgenService (649): one deterministic generate-pipeline; same-seed determinism golden-tested (WorldgenServiceTests + WorldStyleMatrixTests.EveryStyleGenrePair_IsDeterministicForSameSeed). Extensibility = add a Generate* phase method; no mixed concerns.
  * EMB-035 JobAssignmentSystem (776): colony job pipeline; 5 test files (System/Competition/QueueIndex/Farming/Harvest). Cohesive.
  * EMB-036 ShieldBuffService (529): absorption-totals concern ALREADY extracted to ShieldBuffAbsorptionBatchTotals.cs — the split ChatGPT asked for is already done.
  Cosmetic partial-splitting cohesive, well-tested critical sim code adds navigational cost + bug risk without separating real concerns. The genuine god-class (mixed tick+dialog+LLM+save+fate+combat) is EMB-010 DomainSimulationAdapter — that remains the real split target. Marked [-].

### Session 2 progress (2026-05-30, 43/60 addressed)
- EMB-024/031 → ref-scan clean (TMP 148 GUIDs/0 refs, root scenes 0 refs) → removed 284 TMP samples + 2 dead scenes (289 files); build-verified clean. commit 799b75d7
- EMB-025 → ALREADY COMPLIANT: Resources holds only 2 global fonts (Jost/Spectral) + tiny theme.tss + loading-flavors.json — exactly the "truly global tiny runtime assets" ChatGPT permits. No large/optional assets to migrate. CLOSED.
- EMB-009 → DESIGN DECISION (keep): two reviewers disagree; the Simulation->Data.SliceJson dep was deliberately added (prior Codex 7th-pass) for save-rehydration shape-sync, with a defending asmdef README. The dependency is to a pure-C# JSON mapper (no UnityEngine, no nondeterminism) so it does NOT violate Simulation's headless/deterministic contract. "Inverted persistence" is stylistic, not a correctness defect. Keep the documented design. CLOSED as reviewed-decision.

### EMB-010 ADAPTER SPLIT — precise plan for a focused session (NOT done; risky mechanical surgery deferred)
DomainSimulationAdapter.cs (1172 lines) sealed class with `// -----` region markers. Make it
`sealed partial class` and extract by concern into sibling partial files (copy the 15 usings + the
namespace + `public sealed partial class DomainSimulationAdapter {` header into each):
  - .Combat.cs  : IPlayerCommandSink region ~778-1063 (LogCombat/TakePlayerDamage/TryCastSpell/
                  TryMeleeStrike/TryInteract) — CONTIGUOUS, cleanest.
  - .Worldgen.cs: SeedWorld..MovePlayerToStartingSettlement ~345-703 (+ Hydrate* helpers) — but note
                  StartingFaction property sits ~343; verify exact start line before cutting.
  - .Save.cs    : IEmberSaveBridge ~1134-1169 (ExportStateJson/RestoreStateJson) — leave the class +
                  namespace close braces (last ~3 lines) in the MAIN file.
  - .Dialog.cs  : GetDialogSource + GenerateNpcGreetingAsync + IDialogSource region + ConsultFate*
                  — NON-CONTIGUOUS (286-344, 704-777, 1064-1133); hardest, do last.
Behaviour-preserving (partial class, no API change). Verify with a Unity batchmode build (Presentation
isn't covered by the fast fallback). REVERT (git checkout) on any boundary/brace error.

### REMAINING 17 TODO — all substantial (feature-builds + risky refactors needing focused sessions + build/screenshot loops)
Refactors (Unity build): EMB-010 adapter split (plan above), EMB-016 UiToolkitPanel 517 (many screens
in one backend), EMB-033 char-creation 707+571 (state+UI+gen+network), EMB-037 procedural-UI,
EMB-019 LLM-providers->Infrastructure asmdef, EMB-017 static-locators->scene-scoped.
Feature/runtime (Unity + screenshot proof): EMB-006 real-LLM-roundtrip proof (T-LLM-Verify),
EMB-011 save slots+migration, EMB-014 HUD action-level state machine (visual frame already shipped),
EMB-015 input abstraction (59 Input. sites), EMB-020/045 one conversation-state model + per-actor
ask-about, EMB-030 scene-tour health harness, EMB-042 generation provenance UI, EMB-054 Editor
scene-validation menu. Deferred-w-rationale: EMB-008 tool-authority (with T-AskDM), EMB-018 portrait
async (with EMB-033).
- EMB-042 → AssetGenerationResult.IsPlaceholder + OnnxAssetForge sets it + PipelineResult.Placeholders count. Fallbacks no longer silently counted as real. fallback 1425/0. commit (provenance)
- EMB-037 → DECIDED addressed: the worst procedural-UI offender (UiToolkitPanel 517) was split in EMB-016; EmberHud/DialogBoxPanel/EmberMainMenuUI are cohesive single-screen files already using the UiTokens design tokens. The "move layout to templates/tokens" is a forward design-system migration (feature), not a defect-split. Marked [-].

## §8 — PRECISE IMPLEMENTATION PLANS for the remaining feature-builds (ready to execute)
These are ChatGPT's P2-P4 packages — genuine multi-step FEATURE work (not bug/hygiene defects), each
needing its own batchmode build (+ screenshot for UI). Plans are concrete so a focused session runs
them seamlessly. Verify Sim/Data via fast fallback (~1s); Presentation via Win64 build.

### EMB-015 — Input abstraction (P3-A) [Presentation, build]
- New `Assets/Scripts/Presentation/Ember/Input/EmberInput.cs`: a static action API —
  bool Interact, Pause, SaveQuick, LoadQuick; Vector2 Move; bool DialogTopic(int 1..9); bool SpellSlot(int 1..5);
  bool AttackClick. Back it today with UnityEngine.Input; later swap the body for com.unity.inputsystem.
- Migrate the 59 `Input.` sites (static-audit §5 counts them) — sed by pattern per file: player rig
  (Move/Interact), combat (AttackClick/SpellSlot), DialogBoxPanel (DialogTopic/ESC), PauseMenu, save
  hotkeys (F5/F9 -> SaveQuick/LoadQuick). Leave legacy Slice* controllers (EMB-057) untouched.
- DoD: static-audit `Input.` count in active runtime drops to ~0; build green; manual move/dialog/save check.

### EMB-011 — Durable save slots (P-save) [Presentation, build + scene proof]
- EmberSaveService today: PlayerPrefs key "ember.save.v1". Add a file-based repository
  `Assets/Scripts/Presentation/Ember/Save/FileSaveRepository.cs` writing
  Application.persistentDataPath/saves/slot_{n}.json (n=0..K) with the SliceSaveData (schema v1 from
  EMB-012). Keep PlayerPrefs ONLY as a "last slot" pointer. Add corrupt-save quarantine (move bad
  json to .corrupt, surface a status). Add ListSlots()/Save(slot)/Load(slot).
- DoD: save slot A -> quit -> continue; cross-scene load; corrupt json handled; schema-version guard
  (EMB-012) rejects future saves.

### EMB-014 — HUD action-level state machine (P3-B, slice 2) [Presentation, build + screenshot]
- The visual 12-button strip shipped (T-HUD slice 1). Add the ActionLevel enum (UAW_STANDARD/QWEAPONS/
  QSPELLS/QITEMS/INNATE/SONGS/MODAL/FORMATION) to EmberHud + per-level button population + slot click
  -> IPlayerCommandSink (the stub Debug.Log in EmberHud.ActionSlot becomes a real command). F1..F12
  hotkeys via EmberInput (EMB-015). Per Reference/PRDs/PRD_frontend_action_bar_v1.
- DoD: clicking CAST opens QSPELLS level; ATK issues attack; scene-tour screenshot shows live strip.

### EMB-020 + EMB-045 — One conversation-state model + per-actor Ask About [build + dialog proof]
- Define one ConversationState (current NPC, portrait, deterministic topic ids from the NPC's
  memory/faction context, not a global list). DomainSimulationAdapter.GetTopics already delegates to
  the adapter (EMB-Dialog slice 2); make the adapter compute per-actor topics from NpcMemory +
  faction, not _world.Topics globally. Deprecate the AskAboutService/AskDmService/NpcDialogueService
  shells behind the one model. UI (DialogBoxPanel) consumes ConversationState.
- DoD: two different NPCs expose different topics/answers; TavernDialog scene proof.

### EMB-008 — LLM tool-authority routing [build] (currently deferred; consult_fate is Read-only/benign)
- Route DomainSimulationAdapter.Fate.cs ConsultFateAsync's trace through LlmProposalValidator +
  ToolCallValidator (build a ToolRegistry with the consult_fate descriptor) instead of synthesizing
  ToolCallTraceRecord directly. Pairs with the EMB-020/045 tool-use wiring.
- DoD: invalid tool calls rejected+logged; fate trace produced only via the router.

### EMB-019 — Move LLM providers out of Simulation (asmdef) [build, RISKY]
- LlmClients.cs (HTTP) + NativeLlmClient.cs live in EmberCrpg.Simulation (deterministic/headless).
  Create EmberCrpg.Infrastructure asmdef (references Domain+Simulation); move the provider impls
  there; keep request/response/tool CONTRACTS in Domain/Simulation. Rewire ForgeBootstrap (Presentation)
  to compose them. Revert on any asmdef-graph break.
- DoD: headless Simulation tests compile with no HTTP/native provider refs; build green.

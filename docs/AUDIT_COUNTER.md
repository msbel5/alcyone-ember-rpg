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

**Progress: 4 / 60 defects done (+1 partial) · 0 / 11 packages · 4 / 25 final-checklist items**

**▶ NOW = EMB-005 (model manifest path normalize + VerifyAllPresent test).** Done so far (headless): EMB-001 dup-GUID, EMB-047 DOCS/ links, EMB-058 CURRENT_STATE, EMB-046 README de-stale, EMB-043 README-Qwen (partial; manifest pending here). Next headless: 005, 052, 048/050, 049, 004-scan, static-audit tooling.

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
- `[ ]` **EMB-002** · `.meta` integrity · Editor:yes — missing `.meta` on `LLamaSharp.dll`, `Jost.ttf`, `Spectral-Regular.ttf`, `Resources/Fonts/`, `NuGet/.nuget-installed.json`. Import via Unity (don't hand-fake importer settings). [E]
- `[ ]` **EMB-004** · LFS/build reliability · Editor:scan-no/build-yes — CI `lfs:false`; many DLL/model files are 131-byte LFS pointers → false-green. Add pointer-scan validation; split CI source-only vs LFS-build.
- `[ ]` **EMB-005** · AI/model bootstrap · Editor:no — `Models/manifest.json` paths (`sdxl-turbo/text_encoder.onnx`) don't match real nested layout (`text_encoder/model.onnx`); hashes `TBD`. Normalize manifest + `VerifyAllPresent` test.
- `[ ]` **EMB-006** · LLM integration · Editor:partial — `NativeLlmClient` fallback when `USE_LLAMASHARP` absent; explicit disabled/fallback/real capability states + presence validation + no fake-real claims.
- `[ ]` **EMB-007** · Determinism/threading · Editor:yes — `DomainSimulationAdapter` `Task.Run` writes `_currentDialogLine`/`_isDialogThinking`/`_pendingFate`/`_world.ToolCallTrace` off-thread. Marshal results to main-thread tick boundary. (== package P2-B)
- `[ ]` **EMB-008** · LLM authority · Editor:core-no — `ConsultFateAsync` synthesizes `ToolCallTraceRecord` directly, bypassing validator/router. Route fate/dialog effects through tool router. (== P2-D)

### High
- `[ ]` **EMB-009** · asmdef boundary · Editor:no — `Simulation.asmdef` references `Data.SliceJson`; invert persistence direction. (== P2-C)
- `[ ]` **EMB-010** · god-class · Editor:staged — `DomainSimulationAdapter.cs` 1173 lines; characterize then split. (== P2-A, refactor #1)
- `[ ]` **EMB-011** · save/load · Editor:yes — `EmberSaveService` uses `PlayerPrefs ember.save.v1` + static `_pendingLoad` + scene names. Move to file slots + schema version + corrupt-quarantine; PlayerPrefs only for "last slot" pointer.
- `[ ]` **EMB-012** · save schema · Editor:no — `SliceSaveMapper.cs` 945 + `SliceSaveData.cs` 523; add schema version, split mappers by subsystem, migration tests.
- `[ ]` **EMB-013** · reflection restore · Editor:no — `RestoreStateJson` reflection-copies every public field, bypassing invariants. Explicit validated restore.
- `[ ]` **EMB-014** · UI drift · Editor:yes — HUD direction (PRD = bottom bars + 12-btn action bar). Already largely shipped this session (T-HUD slice 1); finish + screenshot proof. (== P3-B)
- `[ ]` **EMB-015** · input · Editor:yes — legacy `UnityEngine.Input` widespread. Input abstraction first, then InputSystem. (== P3-A)
- `[ ]` **EMB-016** · UI arch · Editor:yes — `Ui.Foundation` uses UnityEngine types (not backend-neutral); `UiToolkitPanel.cs` 517. Rename boundary honestly / split.
- `[ ]` **EMB-018** · LLM blocking · Editor:yes — sync `HttpClient...GetAwaiter().GetResult()`; async job service + timeout/cancel + main-thread apply.
- `[ ]` **EMB-019** · LLM placement · Editor:no — HTTP/native/model clients in `Simulation`; move providers to Infrastructure/Presentation, keep contracts in core.
- `[ ]` **EMB-021** · generated-asset policy · Editor:maybe — `GeneratedAssets/**` tracked, `Assets/Generated/Core.meta` orphan; one cache root, ignore regenerated, track only seed manifests.
- `[ ]` **EMB-027** · CI coverage · Editor:CI — default EditMode-only, PlayMode/build tag-only, `lfs:false`. Add asset+pointer audit, opt-in LFS build.
- `[ ]` **EMB-030** · scene playability · Editor:yes — scene-tour checklist (spawn/camera/collision/interact/exit/HUD/dialog/save/screenshot). (== P1-C)
- `[ ]` **EMB-033** · char-creation complexity · Editor:yes — controller 707 + rendering 571 + portrait LLM. Split state/view/gen/transition.
- `[ ]` **EMB-041** · model/provider dup · Editor:gen-yes — `ModelBootstrap`/`ForgeBootstrap`/`OnnxAssetForge`/`ComfyUiAssetForge` split-brain. One model locator + one provider factory.
- `[x]` **EMB-046** · docs/source conflict · Editor:no — README says "no char creation / AI test-wired only"; goal says creation exists + LLM partial. README → point to CURRENT_STATE. (== P0-C)
- `[ ]` **EMB-048** · PRD duplication · Editor:no — `Reference/PRDs` 97 + `docs/reference/prd` + `docs/prds` overlap. One matrix, active/reference/deprecated tags. (== P4-A)
- `[x]` **EMB-058** · no current-state one-pager · Editor:no — status buried in 200+ line goal. Create `docs/CURRENT_STATE.md`. (== P0-C)

### Medium
- `[ ]` **EMB-003** · orphan `.meta` · Editor:final-yes — orphan metas (`AI Toolkit.meta`, `Audio.meta`, `Generated/Core.meta`, `AiDm.meta`, art UI metas, onnx `.data.meta`). Classify: restore/delete/document.
- `[ ]` **EMB-017** · global state · Editor:yes — static locators (`EmberDomainAdapterLocator`/`UiSurfaceLocator`/`ForgeLocator`) + pending-load. Scene-scoped composition root + reset hooks.
- `[ ]` **EMB-020** · dialogue dup · Editor:yes — `AskAboutService`/`AskDmService`/`NpcDialogueService`/adapter all do shell dialogue. One conversation-state model.
- `[ ]` **EMB-022** · repo hygiene · Editor:no — `Reports/**` 102 files/~11MB. Keep latest curated → `docs/proofs/`, archive rest.
- `[ ]` **EMB-023** · docs-in-Assets · Editor:pold-yes — `Assets/Plans/`, `Assets/pold/NavMesh.asset`. Move planning to `docs/archive`, classify `pold`.
- `[ ]` **EMB-024** · sample assets · Editor:yes — TMP Examples 284 files/~5.7MB. Remove after ref scan.
- `[ ]` **EMB-025** · Resources usage · Editor:yes — fonts/theme via `Resources` + missing metas. Explicit serialized refs after meta fix.
- `[ ]` **EMB-026** · package hygiene · Editor:yes-final — manifest test-framework `1.4.5` vs lock `1.6.0`; stale `.gitignore` ai.assistant. Normalize.
- `[ ]` **EMB-028** · validation limits · Editor:yes-unity-mode — fallback harness compiles selected files only; rename "partial", add full-Unity target.
- `[ ]` **EMB-029** · test bloat · Editor:no — magic shield tests 300-500+ lines overfit. Consolidate, add product-facing tests.
- `[ ]` **EMB-031** · scene org · Editor:yes — root `CombatPlayground`/`Sprint4Foundation` outside build + dup GUID. Archive/delete after EMB-001.
- `[ ]` **EMB-032** · prefab policy · Editor:yes — no `Assets/Prefabs`; scenes hand-authored. Audit before any prefab conversion (no blind mass-convert).
- `[ ]` **EMB-034** · worldgen complexity · Editor:no — `WorldgenService.cs` 649. Split regions/settlements/factions/NPCs/history/validation w/ same-seed digest test.
- `[ ]` **EMB-035** · job system complexity · Editor:no — `JobAssignmentSystem.cs` 776. Split discovery/eligibility/reservation/assignment/events.
- `[ ]` **EMB-036** · magic/combat complexity · Editor:combat-yes — `ShieldBuffService` 529 + `...BatchTotals` 762. Simplify interfaces, keep core tests.
- `[ ]` **EMB-037** · procedural UI · Editor:yes — HUD/dialog/menu/panel code-heavy. Move layout to templates/tokens.
- `[ ]` **EMB-038** · deterministic RNG · Editor:no — `LatentNoiseSampler` uses `new System.Random((int)seed)`. Ember deterministic RNG or doc forge as non-authoritative cache.
- `[ ]` **EMB-039** · non-authoritative time · Editor:no — `DateTime.UtcNow` in `GenerationFailureLog`/`VisibleGenerationPipeline`. Keep timestamps out of canonical IDs.
- `[ ]` **EMB-040** · visual nondeterminism · Editor:no — `UnityEngine.Random.Range` in `EmberLoadingScreen`/`ActorView`. Doc as presentation-only, keep out of save.
- `[ ]` **EMB-042** · placeholder masking · Editor:screenshot-yes — fallback gen can hide failure. Visible generated/fallback/static provenance in loading log + UI.
- `[~]` **EMB-043** · AI docs mismatch · Editor:runtime-yes — README `Qwen3:1.7B` vs code Qwen2.5-1.5B vs manifest 3B-missing. One AI-stack doc + manifest.
- `[ ]` **EMB-044** · cloud/network policy · Editor:no — `CloudLlmClient`/`LocalQwenClient`/portrait provider. Cloud opt-in, disabled-by-default, never authoritative.
- `[ ]` **EMB-045** · ask-about scope · Editor:dialog-yes — global `_world.Topics`. Per-actor conversation state + memory/faction filters.
- `[x]` **EMB-047** · case-sensitive links · Editor:no — `DOCS/` vs real `docs/`. Normalize lowercase.
- `[ ]` **EMB-049** · old backend ref · Editor:no — `Reference/OldBackendData/**`. Add README "import/reference-only".
- `[ ]` **EMB-050** · reports/ref sprawl · Editor:no — old sprint/audit reports in active docs root. Archive `docs/archive/YYYY-MM/`.
- `[ ]` **EMB-051** · plugin dep hell · Editor:yes — 127 plugin files; NuGet marker missing meta; LFS DLLs. `docs/DEPENDENCIES.md` + import audit.
- `[ ]` **EMB-053** · build-size/delivery · Editor:yes — ~14GB build w/ ONNX/cuDNN; LFS model pointers. Decide code-only+downloader vs curated-LFS.
- `[ ]` **EMB-054** · scene YAML static limits · Editor:yes — can't prove no missing scripts statically. Editor scene-validation menu/test.
- `[ ]` **EMB-055** · prefab policy · Editor:yes — scene recipes vs prefabs ownership unclear. Decide policy, no blind change.
- `[ ]` **EMB-056** · scene hardcoding · Editor:yes — hardcoded scene names across runtime/editor/diag. Central scene-ID registry.
- `[ ]` **EMB-057** · Slice* legacy · Editor:yes — `SliceGameController`/`SlicePlayerRig` legacy input+file saves. Ref-scan then archive/delete.
- `[ ]` **EMB-059** · `.claude/skills` classification · Editor:no — 65 tracked skill files. Classify dev-tooling vs product.
- `[ ]` **EMB-060** · test/package lock mismatch · Editor:yes — manifest `1.4.5` vs lock `1.6.0`. Resolve once, commit normalized.

### Low
- `[ ]` **EMB-052** · secrets · Editor:no — no real keys found; cloud LLM code = future risk. Add `.env`/secret ignore patterns + docs; env-only keys.

---

## §4 — EXECUTION LANES (do in this order)

### Lane A — Headless, Claude-safe (NO Unity Editor) — do ALL of these first
Order by safety×value:
1. `[x]` EMB-001 scan (duplicate GUID — fix is partial-headless: regen one root-scene meta GUID after ref-scan)
2. `[x]` EMB-047 — `DOCS/` → `docs/` link normalize
3. `[x]` EMB-058 + EMB-046 + EMB-003-scan — create `docs/CURRENT_STATE.md`, de-stale README (= P0-C)
4. `[ ]` EMB-005 — model manifest path normalize + `VerifyAllPresent` test (= P1-A)
5. `[ ]` EMB-043 — AI-stack doc, fix Qwen version mismatch
6. `[ ]` EMB-052 — secret/.env ignore patterns
7. `[ ]` EMB-048 + EMB-050 — PRD matrix dedup + docs archive plan (= P4-A)
8. `[ ]` EMB-049 — OldBackendData README
9. `[ ]` EMB-004 — LFS pointer-scan validation script (= P0-B)
10. `[ ]` static-audit tooling — duplicate-GUID / missing-meta / orphan-meta / LFS / `Input.` / `PlayerPrefs` / `Task.Run` scanners under `tools/validation/`
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
- `[ ]` 1. Duplicate-GUID + missing/orphan meta audit (one Unity-safe PR)
- `[ ]` 2. Static validation: dup-GUID, missing-meta, orphan-meta, LFS-pointer
- `[x]` 3. `docs/CURRENT_STATE.md` + de-stale README
- `[ ]` 4. PRD source-map: one matrix, active/reference/deprecated, fix `DOCS/`
- `[ ]` 5. AI/model manifest paths + hash policy (no binary changes)
- `[ ]` 6. LFS/runtime dep docs + CI pointer checks
- `[ ]` 7. Save/load characterization tests (before changing persistence)
- `[ ]` 8. Build-scene validation/screenshot-tour harness (report only)
- `[ ]` 9. Remove worker-thread world/UI mutation from adapter
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

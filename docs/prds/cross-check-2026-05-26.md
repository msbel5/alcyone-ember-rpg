# Cross-Check: docs/ Unity-Canonical vs Current Implementation (2026-05-26)

**Status:** Draft (Mami audit, complements `prd-audit-2026-05-26.md`)
**Branch:** `docs/codex-mission-v2` (PR #214, do not merge)
**Author:** Mami (visual track agent)
**Scope:** maps the Unity-canonical vision (`docs/EMBER_VISION_BIBLE.md`, `docs/agent-rules-v2.md`, `docs/ROADMAP.md`, `docs/mechanics/faz-11-unity-visual-layer.md`, `docs/ember-soul-acceptance-2026-05-23.md`) against the live Unity code, scenes, and prior 97-PRD Godot audit. Identifies goal drift. Outputs the immediate Mami AAA queue.

---

## 1. Why this document exists

`prd-audit-2026-05-26.md` (committed `0ce0f004`) classified the 97 Godot-era PRDs in `Reference/PRDs/` and `docs/reference/prd/` into 30 KEEP / 57 ADAPT / 10 DEPRECATE. That audit answered "what to do with the **old** specs". It did **not** read the Unity-canonical vision content that lives in `docs/` root and `docs/mechanics/`, so it could not check whether the current Unity code obeys the canon. This document closes that gap.

The user explicitly corrected this in their last instruction:

> "oyunun yeni prd leri burda `docs` cross check edebilirsin burdakiler godot implementasyonun prdleri `Reference`"

So: **`docs/` = Unity-canonical (this is what the project is)**, **`Reference/` = Godot-historical (read-only inheritance)**.

---

## 2. The Unity-canonical stack (what the project actually is)

Reading order for any new contributor or agent:

| # | File | Role | LOC | Status |
|---|------|------|----:|--------|
| 1 | `docs/EMBER_VISION_BIBLE.md` | Vision canon (10 sections) | ~440 | Canonical, last updated 2026-05-02 |
| 2 | `docs/ROADMAP.md` | 12-phase roadmap (Faz 0→12), supersedes Sprint 0 roadmap | ~360 | Active, last updated 2026-05-09 |
| 3 | `docs/agent-rules-v2.md` | Five enforcement rules + Faz 11 carve-out (Rule 7) | ~220 | Mandatory for every PR |
| 4 | `docs/mechanics/MASTER_MECHANICS_BIBLE.md` | Mechanics canon | (large) | Canonical |
| 5 | `docs/mechanics/faz-11-unity-visual-layer.md` | **The Mami territory spec** | ~600 | Active spec |
| 6 | `docs/mechanics/faz-3..12-*.md` | Per-faz mechanic specs | varies | Per-phase |
| 7 | `docs/sprint-faz-1..12-atom-map.md` | Captain decomposition per faz | varies | Per-phase |
| 8 | `docs/ember-soul-acceptance-2026-05-23.md` | Soul acceptance gate (LLM + NPC + Forge proven) | ~40 | **Already passed (READY FOR MAIN)** |
| 9 | `docs/PRD_SPRINT_1_TINY_VERTICAL_SLICE.md` | Sprint 1 (already shipped, April 2026) | ~200 | Historical reference |
| 10 | `docs/prds/visible-generation-cutover.md` | PR #214 cutover plan | (large) | Active (in PR) |
| 11 | `docs/prds/aaa-scene-quality-uplift.md` | 5-sprint AAA plan, target ≥90 UX / ≥88 Playability across 10 scenes | 171 | Draft, awaiting @msbel5 approval |
| 12 | `docs/prds/PRD_visual_architecture_3d_billboard_v1.md` | 3D billboard architecture (Daggerfall-Unity-style) | 199 | Draft (this session) |

Vision Bible §11 is decisive: **Daggerfall-Unity is the closest twin** to our codebase. That validates PRD-3D-Billboard's premise — we are not inventing a new visual architecture, we are formalizing what `CameraFacingBillboard.cs` already implements and what `references/daggerfall-unity-master/` already demonstrates.

---

## 3. The 5-layer vision (Vision Bible §3) vs current code

| Layer | Vision Bible says | Current Unity state | Drift? |
|---|---|---|---|
| **L1 Pure Domain** | Actor, Stats (MIG/AGI/END/MND/INS/PRE), Health/Fatigue/Mana, Item, Inventory, Memory. Unity-free. | `Assets/Scripts/Domain/` exists with `Actors/`, `Items/`, `Core/`, `Process/`, `World/`. `noEngineReferences=true` per soul-acceptance. | **No drift.** Canon honored. |
| **L2 Deterministic Sim Core** | Procedural rooms, NPC schedules, day/night, weather, save/load, off-world tick. | `Assets/Scripts/Simulation/` has Living/, Process/, World/. `WorldgenService` produces 932 NPCs for Seed 42 (soul-acceptance Phase 3). | **No drift.** Sim core operational. |
| **L3 Gameplay Mechanics** | Combat (RTWP, hit chance, body parts), Inventory/Equipment/Stats, Quest engine, Magic (school+components), Crafting. | `Faz 3 Recipe/Worksite` shipped, `Faz 4 Job assignment` in flight via Captain debt ledger (CO-02/03/04/05/08), Magic = `SpellExecutionService` routes via `SpellEffectResolutionService` for legacy 7 codes, data-driven `EffectDefinition` promotion **queued under Faz 8 slice 2** (per agent-rules-v2 §3). | **Partial drift.** Magic still has hard-coded `SpellEffectCode` branches awaiting promotion per Rule 3. Combat is one-vs-one (Sprint 1 deviation budget honored) — RTWP not yet wired. |
| **L4 Player Interaction Facade** | Ask About / Think / DM buttons. AI agent → deterministic shell → fallback chain. Skill-gated branches. | "Ask About", "Ask DM", "Think" thin shells shipped in Sprint 1 per PRD §FR-07. Native LLM client wired (`NativeLlmClient.cs`, qwen2.5-1.5b-instruct-q4_k_m) per soul-acceptance Phase 2. | **No structural drift.** L4 surface exists; depth (skill-gated branches) lives in Faz 9 dialog work. |
| **L5 AI Agent Layer** | Per-NPC persistent agent (1 instance/NPC), DM agent, tool-call surface only, fallback path. **Should ship LAST.** | Native LLM already invoked for NPC first dialog line + R-key ConsultFate oracle (soul-acceptance Phase 4). | **Early arrival but contained.** L5 is online but used as flavor router per Vision Bible Rule 1 ("Ship deterministic first, AI last"), not as the primary loop. Acceptable. |

**Verdict:** the 5-layer vision is intact. The architecture is honored. The drift is small and scoped (Faz 8 magic promotion + Faz 7 RTWP both already queued in roadmap).

---

## 4. The Faz 11 carve-out (agent-rules-v2 §7) and this branch

agent-rules-v2 §6/§7 is the controlling rule for **this session**:

> **Captain MAY NOT:** create or modify `.unity` scene files, `.prefab` files, materials, textures, sprites, models, "evidence" screenshots, or claim Faz 11 promotion proof.
>
> Faz 11 promotion is proven by **Mami's `mami/*` PR landing a real Unity scene with the targeted acceptance sentence demonstrable in the editor.**

This means PR #214 (`docs/codex-mission-v2`) is — by Rule 6/7 — a **Mami-territory PR**, because it touches:

- `Assets/Scenes/Ember/*.unity` (10+ scenes, currently dirty in working tree)
- `Assets/Art/Materials/*.mat` (6 dirty)
- `Assets/Editor/Ember/SceneRecipes/*.cs` (12 recipes, both un-prefixed and `Faz3..Faz12` prefixed pairs)
- `docs/screenshots/*.png` (10 AAA baseline shots, untracked)

The branch is **correctly named Mami-side work** (the `docs/codex-mission-v2` name is misleading — it carries Mami visual work, not pure Captain docs). The Codex mission prompts in this PR (`aaa-sprint-a-codex-mission.md`) brief Codex to act as **Mami delegate**, with Mami reviewing and committing.

**No drift here either.** The PR is operating correctly under Rule 7.

---

## 5. The two parallel scene name systems

Discovered while scanning `Assets/Scenes/Ember/` and `Assets/Editor/Ember/SceneRecipes/`:

| Un-prefixed (AAA targets) | Faz-prefixed (older) | Both real? |
|---|---|---|
| `SmithingOverworld.unity` (93KB) | `Faz3SmithingOverworld.unity` (93KB) | **Yes — both ~93KB** |
| `ColonyNeeds.unity` | `Faz4ColonyNeeds.unity` (100KB) | Both real |
| `SeasonFarm.unity` | `Faz5SeasonFarm.unity` (95KB) | Both real |
| `TradeMarket.unity` | `Faz6TradeMarket.unity` (84KB) | Both real |
| `CombatDungeon.unity` | `Faz7CombatDungeon.unity` (104KB) | Both real |
| `RitualHall.unity` | `Faz8RitualHall.unity` (91KB) | Both real |
| `TavernDialog.unity` | `Faz9TavernDialog.unity` (99KB) | Both real |
| `OracleShrine.unity` | `Faz10OracleShrine.unity` (105KB) | Both real |
| `ShowroomOverview.unity` | `Faz11ShowroomOverview.unity` (125KB) | Both real |
| `TavernFlavour.unity` | `Faz12TavernFlavour.unity` (99KB) | Both real |

**Correction (later inspection):** the Faz-prefixed scenes are NOT orphan `.meta` ghosts as initially feared — they are real `.unity` files of comparable size to the un-prefixed copies, with the same mtime (May 24, 2026). This is the **rename cutover footprint** from commit `9d8c5b8b "Commit 4: Scene file renames + path rewires"`. Both copies coexist during transition.

**Implications:**
1. The 10 AAA scene names in `aaa-scene-quality-uplift.md` §1 score table match the **un-prefixed** column — these are the AAA targets.
2. The Faz-prefixed copies are likely the **pre-cutover state** kept as safety nets.
3. **Action:** before AAA Sprint A starts, confirm with @msbel5 which set is authoritative. If un-prefixed is canonical, the Faz-prefixed set should be removed in a single janitor commit (saving ~1MB of duplicate scene data + reducing build scene confusion). Do NOT delete unilaterally — these are .unity files, not throwaway artifacts.

## 5.1. Reference library presence check

Vision Bible §11 promises a `references/` directory (gitignored) containing:
- `references/daggerfall-unity-master/` (C# Daggerfall port — **closest twin to our codebase**)
- `references/openmw-master/` (C++ Morrowind reimplementation)
- `references/gemrb-master/` (BG/IWD/PST engine)
- `references/dwarf-fortress-legacy/` (DF C++ snapshot)
- `references/ember-rpg/` (our previous Godot attempt — read as anti-pattern)

**Verified state on this dev machine:** `references/` **does not exist**. None of the engine references are present locally. This is a **real gap** for Faz 11 Mami work — billboard rendering, mood lighting, dungeon prop placement, and combat hit-chance math are all supposed to be informed by reading these references first per the **clean-room rule** in Vision Bible §11.

**Action:** before Sprint A starts on SmithingOverworld, decide:
- (a) clone `references/daggerfall-unity-master/` into the gitignored path so Mami can read the forge/anvil/billboard patterns, OR
- (b) accept that the AAA uplift will be done from screenshots + general Daggerfall-Unity public-knowledge memory, NOT from reading the actual C# port, OR
- (c) defer Sprint A until references are present.

Recommendation: (a) is the canonical path per Vision Bible. (b) is the pragmatic path. (c) blocks work.

`Reference/` (capital R) exists with `OldBackendData/` + `PRDs/` only — that's the Godot-era PRD library, not engine references.

---

## 6. Connection to the 97-Godot-PRD audit

`prd-audit-2026-05-26.md` produced these directly Faz-11-blocking ADAPT candidates (top from the 57 ADAPT list):

| Godot PRD path (under `Reference/PRDs/`) | Why this matters for Mami AAA work | Status |
|---|---|---|
| `Reference/PRDs/PRD_architecture_sprite_layers.md` | 2D multi-layer sprite spec — **needs ADAPT to 3D billboard** per `PRD_visual_architecture_3d_billboard_v1.md` | Covered by new PRD; mark Godot one DEPRECATED |
| `Reference/PRDs/PRD_PLAYABILITY_RESCUE.md` | F1/BG1 vertical slice gate — **already overtaken** by `aaa-scene-quality-uplift.md` (90+ target) | Effectively superseded |
| `Reference/PRDs/PRD_architecture_lighting.md` | Lighting setup specs — **direct ADAPT** to per-scene Light + post-process volume in AAA uplift §2 | Pull lighting tables into AAA Sprint plan |
| `Reference/PRDs/PRD_architecture_particles.md` | Particle systems — **direct ADAPT** to forge sparks/dungeon fog/shrine incense in AAA uplift §2 | Pull particle prefab specs |
| `Reference/PRDs/PRD_architecture_audio.md` | Audio bed — **direct ADAPT** to forge clang/market chatter/etc in AAA uplift §2 | Pull AudioMixer routing pattern |
| `Reference/PRDs/PRD_architecture_camera.md` | Camera framing — **direct ADAPT** to CinemachineVirtualCamera per scene | Pull thirds/dolly conventions |
| `Reference/PRDs/PRD_architecture_npc_placement.md` | NPC routine placement — **direct ADAPT** to smith-at-anvil/merchant-at-stall pattern | Pull placement conventions |
| 4 more lighting/material PRDs | Direct ADAPT | Cherry-pick into AAA sprints |

**The bridge work:** when authoring each AAA Sprint atom map (Sprint A through E in `aaa-scene-quality-uplift.md` §5), cite the Godot PRD being adapted, then apply the 3D-billboard substitutions documented in `PRD_visual_architecture_3d_billboard_v1.md` §FR-01..FR-12.

---

## 7. The current vs the canonical pipeline (one diagram)

```
┌─────────────────────────────────────────────────────────────────┐
│ Vision Bible (docs/EMBER_VISION_BIBLE.md)                       │
│   ↓                                                              │
│ Roadmap (docs/ROADMAP.md) — 12 phases                            │
│   ↓                                                              │
│ Per-phase mechanic spec (docs/mechanics/faz-N-*.md)              │
│   ↓                                                              │
│ Captain atom map (docs/sprint-faz-N-atom-map.md)  ← Captain      │
│   ↓                                                              │
│ Debt ledger + PR audit fields (agent-rules-v2 §9)               │
│   ↓                                                              │
│ Captain PR (Domain + Simulation only, Rule 6 hard-failed         │
│             from Assets/Scenes/, Assets/Art/, Assets/Prefabs/)   │
│                                                                  │
│ ─── Faz 11 boundary (Rule 7) ──────────────────────────────────  │
│                                                                  │
│ Mami PR  ← THIS BRANCH (docs/codex-mission-v2 / PR #214)         │
│   - reads Captain stores (ActorStore, JobBoard, etc.)            │
│   - lights, props, particles, camera, audio, NPC placement       │
│   - SceneRecipe.cs (Editor-time codegen)                         │
│   - .unity + .prefab + .mat + .png commits allowed               │
│   - Reports/screens/aaa_*.png as evidence                        │
│   ↓                                                              │
│ Player playtests the scene; acceptance sentence demonstrable     │
│ in Unity editor → Faz 11 promotion proven                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. Goal-drift findings (top issues, prioritized)

| # | Finding | Severity | Owner | Suggested fix |
|---|---|---:|---|---|
| 1 | `aaa-scene-quality-uplift.md` is **Draft (awaiting @msbel5 approval)** for over 24 hours; Sprint A Codex mission is queued behind it. | 🟡 Moderate | @msbel5 | Approve PRD or send specific edit list so Sprint A can start. |
| 2 | `PRD_visual_architecture_3d_billboard_v1.md` has **4 open questions** in §10 awaiting Mami answers (atlas authoring, rim-light shader, establishing-shot space skip, Boot exemption). | 🟡 Moderate | @msbel5 | Answer the 4 questions → PRD goes Approved. |
| 3 | Duplicate scene files in `Assets/Scenes/Ember/` (un-prefixed AAA targets AND Faz-prefixed pre-cutover copies, both ~100KB each). See §5. | 🟡 Moderate | @msbel5 | Confirm un-prefixed set is authoritative; then janitor commit removing `Faz*.unity` + `Faz*.unity.meta` (~1MB cleanup). Do NOT delete unilaterally. |
| 4 | 10 AAA baseline screenshots in `docs/screenshots/` are **untracked**. These are the 82-84 reference shots that `aaa-scene-quality-uplift.md` §1 promised to replace. | 🟡 Moderate | Mami | Commit them under `Reports/screens/aaa_<scene>_baseline_<unix>.png` per AAA uplift §6 evidence convention, NOT in `docs/screenshots/`. |
| 5 | Working tree has uncommitted material + scene mutations from prior Editor opens (Scene_Ember_Light.mat, 10 scene files, TerrainData/*). | 🟡 Moderate | Mami | Triage: keep intentional changes (terrain data, materials), discard accidental Unity auto-touches. Do NOT bulk-commit. |
| 6 | Magic system still routes through legacy `SpellEffectCode` enum branches; agent-rules-v2 §3 requires `EffectDefinition` promotion **before** any new effect ships. Soul-acceptance lists "SpellResolver fully data-driven still queued under Faz 8 slice 2." | 🟡 Moderate | Captain | Already on Captain debt ledger; no Mami action. |
| 7 | Mami has not yet authored a `docs/sprint-faz-11-atom-map.md` per agent-rules-v2 §7 carve-out. Captain CAN write it, but it must be Mami-consumer-cited per Rule 7. | 🟡 Moderate | Captain (with Mami consumer) | Defer until Sprint A lands one real scene proof; then write atom map referencing the proven pattern. |
| 8 | `prd-audit-2026-05-26.md` scanned `Reference/PRDs/` only; missing the docs/ root cross-check (this document closes that gap). | 🟢 Minor | Mami | This file. ✅ |
| 9 | No `mami/*` branch exists per Rule 7's wording ("Mami's `mami/*` PR landing a real Unity scene"). Current Mami work is on `docs/codex-mission-v2`. | 🟢 Minor | Convention | Either rename future Mami branches to `mami/aaa-sprint-a-*` or amend Rule 7 wording to allow shared cutover branches. Defer to @msbel5 preference. |
| 10 | **CONFIRMED MISSING:** `references/` directory does not exist on dev machine. None of Vision Bible §11's engine references (`daggerfall-unity-master/`, `openmw-master/`, `gemrb-master/`, `dwarf-fortress-legacy/`, `ember-rpg/`) are locally present. Blocks the clean-room workflow Vision Bible §11 prescribes. See §5.1. | 🔴 Critical | @msbel5 | Decide between (a) clone `daggerfall-unity-master` locally, (b) proceed without references, or (c) defer Sprint A. |

**No catastrophic architecture drift.** Layers are clean, separation of concerns is enforced. The blockers are:
- 2 PRD approvals (3D billboard + AAA uplift) awaiting @msbel5 input,
- 1 reference-library decision (clone Daggerfall-Unity, proceed without, or defer),
- 1 scene-duplication confirmation (un-prefixed vs Faz-prefixed authoritative).

Once those 4 user inputs land, Sprint A can proceed.

---

## 9. Mami AAA queue (ordered, executable)

This replaces the looser task list in the prior session. Each item has an explicit gate, owner, and exit proof.

| Order | Action | Gate | Exit proof |
|---:|---|---|---|
| **1** | @msbel5 answers 4 open questions in `PRD_visual_architecture_3d_billboard_v1.md` §10. | User input. | PRD §1 Status → Approved. |
| **2** | @msbel5 approves `aaa-scene-quality-uplift.md` (or sends edits). | User input. | PRD §1 Status → Approved. |
| **3** | Mami commits this cross-check report. | None. | `git log` shows the commit. |
| **4** | Mami janitor commit: delete orphaned `Faz*.unity.meta` ghosts; move untracked `docs/screenshots/*.png` to `Reports/screens/aaa_<scene>_baseline_<unix>.png` per AAA §6 convention. | None. | Working tree clean; baseline shots in Reports/screens/. |
| **5** | @msbel5 decides reference library path: (a) clone `references/daggerfall-unity-master/` locally, (b) proceed without, or (c) defer. | User input. | Decision recorded in PRD §10 or follow-up commit. |
| **5.5** | @msbel5 confirms which scene set is authoritative (un-prefixed vs Faz-prefixed). | User input. | Decision recorded; janitor commit follows if duplicates to be removed. |
| **6** | Mami starts **AAA Sprint A: SmithingOverworld** per `aaa-sprint-a-codex-mission.md`. | Items 1+2 approved. | One scene at UX ≥90, Playability ≥88, `Reports/screens/aaa_SmithingOverworld_<unix>.png` committed. |
| **7** | Mami continues Sprint A: **TavernDialog**. | Item 6 done. | Same exit proof. |
| **8** | Sprint A PR opens for @msbel5 review. | Items 6+7 done. | PR opened with both scenes' before/after evidence table. |

Items 9+ (Sprints B/C/D/E) follow the same shape.

---

## 10. What this document does NOT do

- Does NOT approve any PRD on @msbel5's behalf. Items 1+2 in §9 still need user input.
- Does NOT commit binaries, scenes, or screenshots. That happens in Item 4.
- Does NOT modify any of the 97 Godot PRDs. `prd-audit-2026-05-26.md` already classified them; the bridge work to ADAPT them happens **per AAA sprint**, not in this cross-check.
- Does NOT rewrite the Vision Bible. The canon stands.
- Does NOT claim Faz 11 promotion. That requires real scene work landing (Item 6 onward).

---

## 11. Acceptance for this cross-check

- [x] Read EMBER_VISION_BIBLE (§§1-11), ROADMAP (Faz 0-12), agent-rules-v2 (Rules 1-9), faz-11-unity-visual-layer (sistem haritası + veri modeli), ember-soul-acceptance.
- [x] Identified Daggerfall-Unity as Vision Bible's canonical billboard twin → confirms `PRD_visual_architecture_3d_billboard_v1.md` is on canon.
- [x] Verified 5-layer architecture (L1→L5) is intact in current `Assets/Scripts/`.
- [x] Identified Faz 11 carve-out (agent-rules-v2 §6+§7) governs **this branch's** work.
- [x] Bridged `prd-audit-2026-05-26.md` (Godot PRDs) → AAA uplift PRD via 8 direct ADAPT mappings.
- [x] Produced Mami AAA queue with explicit gates.
- [ ] @msbel5 reviews this cross-check and answers PRD §10 questions in `PRD_visual_architecture_3d_billboard_v1.md` + approves `aaa-scene-quality-uplift.md`.

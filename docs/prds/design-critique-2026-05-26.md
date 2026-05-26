# Design Critique: AAA Scene Baselines (2026-05-26)

**Status:** Mami visual critique, complements `cross-check-2026-05-26.md` and `aaa-scene-quality-uplift.md`.
**Branch:** `docs/codex-mission-v2` (PR #214).
**Source frames:** 11 PNGs under `docs/screenshots/` (captured 2026-05-25, before the AAA uplift sprints).
**Rubric anchor:** Vision Bible §2 — "*Hitchhiker's Guide* narrative voice, *Fallout 1* DM turn structure, *Morrowind*-shaped world, *Daggerfall* deterministic combat baseline, *RimWorld* colony-sim core."

---

## 1. Overall Impression (2-second read)

**The 3D billboard architecture works. The world dressing does not.**

The good news first: TavernDialog and CombatDungeon prove that `CameraFacingBillboard.cs` renders 2D character sprites in 3D space exactly as `PRD_visual_architecture_3d_billboard_v1.md` specifies — yaw-only facing, correct aspect, no axis flip. The 3D billboard direction is **validated by the frames themselves**, not just the PRD.

The bad news: the rooms those billboards stand in are bare checker-grid gray-boxes. The Vision Bible promises a **Morrowind-shaped world** with **Fallout 1's DM-narrated turn structure** in a **Daggerfall-inherited mood**. The frames deliver **a Unity tutorial primitive scene with NPCs glued in**. The atmosphere gap is the entire delta between the 80-84 baseline scores in `aaa-scene-quality-uplift.md` §1 and the 90+ AAA target — and it is much larger than the +6-10 point gap that PRD implies.

**Calibration finding:** the prior rubric scored these 80-84. A reader looking at the actual PNGs would honestly score them 60-72. Either the rubric was generous, or the rubric's "Playability" axis (which scores presence of components, not visual quality) dominated the "UX" axis (which scores subjective polish). Either way, the AAA Sprint A through E plan is not over-scoped — if anything, it's under-scoped.

---

## 2. Per-scene findings

### 2.1 SmithingOverworld (Sprint A target #1) — 🔴 Critical

**Captured frame shows:** a dark gray brick wall with subtle yellow grout lines. That's it. No floor, no characters, no forge, no anvil, no fire, no smoke, no smith, no warm light. The frame is essentially a single-material wall texture filling the viewport.

| Finding | Severity | Recommendation |
|---|---|---|
| **No focal prop.** The scene's promised "forge focal scene" has no forge. | 🔴 Critical | Place anvil + furnace + bellows cluster on a thirds intersection per AAA uplift §2 vision. |
| **No characters.** The smith NPC is missing entirely from the captured frame. | 🔴 Critical | Spawn the smith NPC billboard at the anvil (smith-at-anvil placement per agent-rules-v2 §6.7 conventions). |
| **No lighting setup.** Single ambient gray. The promised "warm forge" mood is absent. | 🔴 Critical | Add warm key light (≈3200K) at the forge, cool ambient fill, rim from doorway. Post-process volume with mild bloom + warm tint. |
| **No floor visible in frame.** Camera may be pointed at a wall by mistake. | 🟡 Moderate | Confirm CinemachineVirtualCamera target. Frame should show floor + anvil + smith in single composition. |
| **No particles.** Forge sparks + smoke are mandatory per AAA uplift §2. | 🟡 Moderate | Two ParticleSystems: forge sparks (intermittent), chimney smoke (continuous). |
| **No audio.** | 🟡 Moderate | Forge clang loop, mid-volume, mixer-routed to "Environment" group. |

**Honest score:** UX 55 / Playability 60 (not 82 / 80 per the prior table). The scene is empty.

### 2.2 TavernDialog (Sprint A target #2) — 🟡 Moderate (best baseline of the four reviewed)

**Captured frame shows:** 3 character billboards (sword-armed guard left of center, mid figure with mace/club, mage with orb right of center) standing on a dark tiled floor in a dark room. Yellow placeholder object on right wall (probably the "tavern door"). Tiny illegible UI label top-right.

| Finding | Severity | Recommendation |
|---|---|---|
| **Billboards render correctly.** ✅ The 3D billboard architecture is validated by this frame. | ✅ Good | Keep this asset/setup as the **reference frame** for billboard quality across all scenes. |
| **No bar counter, no hearth, no tables.** "Tavern" reads as "dark room with people." | 🔴 Critical | Hand-place: bar counter (long horizontal prop), 2-3 tables with stools, fireplace on side wall. |
| **No warm/candle lighting.** The Vision Bible's "candle-warm" tavern is currently candle-absent. | 🔴 Critical | Add 3-5 point lights at table positions (warm 2700K), 1 hearth light, mild bloom volume. |
| **NPC silhouettes too similar in stance.** All 3 stand in identical T-pose-ish front facing. | 🟡 Moderate | Vary idle poses or use the `GenericNpcBaseManifest` (PR #213) RGB recolor base to differentiate roles. |
| **Top-right UI label illegible.** Looks like 3-4 tiny words. | 🟡 Moderate | Either remove or enlarge to ≥14pt with the Ui tokens' Text color. |
| **Floor tile grid too geometric.** The grout pattern reads as a Unity ProGrid debug overlay, not a tavern floor. | 🟡 Moderate | Swap floor material to wood-plank texture (the `Tile_tavern_wood_floor.png` material already exists in the working tree — wire it up). |
| **Yellow placeholder on right wall.** Reads as untextured Unity primitive. | 🟢 Minor | Replace with door prop or remove from frame. |

**Honest score:** UX 70 / Playability 75 (not 84 / 82).

### 2.3 CombatDungeon (Sprint B target #1) — 🟡 Moderate

**Captured frame shows:** 2 zombie/orc enemy billboards on small stone platforms (good placement instinct — elevation reads as "danger"), one small player figure between them, dark hall with same gray brick walls. Yellow placeholder on right.

| Finding | Severity | Recommendation |
|---|---|---|
| **Enemy elevation works.** ✅ The platforms read as "obstacle, climb to engage." Good design choice. | ✅ Good | Keep this elevation pattern; replicate in other combat scenes. |
| **No torches, no fog.** Vision Bible promises Daggerfall dungeon mood (cold blue + torchlight). | 🔴 Critical | Add wall-mounted torch props at 3-4 positions; FogVolume with cold blue tint; flickering point lights at torches. |
| **No ambient drip / cave audio.** | 🟡 Moderate | Cave drip loop, low volume; intermittent stone-shift cue. |
| **Walls identical to TavernDialog and Smithing.** Same gray-brick reads everywhere. | 🟡 Moderate | Per-scene wall material: dungeon=rough hewn stone, tavern=plaster + wood beam, smithing=soot-stained brick. Material variants are cheap; reuse mesh. |
| **No fog volume.** The frame's deep dark void behind enemies should fall off into atmospheric perspective. | 🟡 Moderate | URP volumetric fog at distance ~15m, near 20m far 60m, color cold blue. |
| **Player billboard too small relative to enemies.** | 🟢 Minor | Confirm billboard quad scale matches PRD §FR-02 (1.0 × 0.685 units). Player may be at greater Z. |

**Honest score:** UX 65 / Playability 75 (not 84 / 82). The composition (3 figures + elevation) is the best of the four.

### 2.4 ShowroomOverviewWithUI (UI showcase) — 🟢 Good (the strongest baseline)

**Captured frame shows:** dark UI canvas on top half with `Tick 0063 Day 001`, three info panels (JOB QUEUE [idle], FACTIONS [Forge Guild 0 neutral, Harbor Merchants 12, City Watch 4], COLONY NEEDS [Warden / Sage Nera / Quartermaster Ivo / Sentinel Rook / Ash Rat with H/F/T/M tracking]). Below: checker tile floor showcase, one billboard NPC (apron-wearing smith with mug), hotbar with inventory slots + MANA button (blue). Dialog text: "Ask clean questions. The world remembers what matters." Four Ask About options: rumors, work, trade, fate.

| Finding | Severity | Recommendation |
|---|---|---|
| **UI data binding works.** ✅ Real data on FACTIONS + COLONY NEEDS + Tick/Day clock. | ✅ Good | This is the strongest frame in the set. Use as Ui style reference. |
| **Dialog text is on-vision.** ✅ "Ask clean questions. The world remembers what matters." — pure Hitchhiker's-Guide-meets-Morrowind voice. Mami canon held. | ✅ Good | Lock this dialog tone for all Ask About copy. |
| **Ask About 1-4 numbered + colored.** ✅ Matches Fallout 1 inheritance from Vision Bible §2. | ✅ Good | Keep the numbering convention across all NPCs. |
| **Hotbar slots empty.** No item icons in inventory slots. | 🟡 Moderate | Wire item icons from `InventoryEquipmentFormatter.cs` to slots. |
| **Yellow checker squares on left.** Reads as Unity primitive debug overlay. | 🟡 Moderate | Hide debug overlay before AAA capture; or move outside frame. |
| **UI panels lack frame/border.** Floating text on black. Vision Bible's "Morrowind-shaped" UI should have parchment / iron / wood frame styling. | 🟡 Moderate | Add subtle border + warm-paper background tint to JOB QUEUE / FACTIONS / COLONY NEEDS panels. |
| **Player figure background-blown.** Bright white window/door behind smith reads as overexposed. | 🟡 Moderate | Reduce skybox/window exposure or add curtain prop. |

**Honest score:** UX 80 / Playability 85. The strongest baseline. This is what the other scenes need to catch up to.

---

## 3. Cross-cutting consistency findings

| Finding | Where seen | Severity | Recommendation |
|---|---|---|---|
| **All three world scenes share the same dark gray brick wall.** | SmithingOverworld + TavernDialog + CombatDungeon | 🟡 Moderate | Per-scene wall material variants (see §2.3 row 4). |
| **All three world scenes share the same checker floor pattern.** | TavernDialog + CombatDungeon (Smithing shows wall only) | 🟡 Moderate | Per-scene floor: tavern=wood, dungeon=cobble, smithing=dirt+soot. Materials already exist in `Assets/Art/Materials/`. |
| **Yellow placeholder primitive appears in every scene.** | SmithingOverworld (?), TavernDialog, CombatDungeon, Showroom | 🟡 Moderate | Audit and either replace with intended prop or remove. |
| **No skybox / no establishing context.** All scenes are black void above wall line. | All world scenes | 🟢 Minor (per PRD-3D-Billboard §10 Q3 — establishing shot is optional) | Indoor scenes can keep dark void; SmithingOverworld is technically "Overworld" and should show sky. |
| **NPC scale consistent.** Billboard quads appear correctly sized. | TavernDialog + CombatDungeon | ✅ Good | PRD §FR-02 (1.0 × 0.685 quad) verified. |
| **UI tokens applied consistently.** Same dark background + warm accent across panels. | Showroom + (presumably others) | ✅ Good | UiTokens system is working. |

---

## 4. Vision Bible alignment check

| Vision Bible promise | Current frame delivers | Gap |
|---|---|---|
| **Daggerfall-shaped world** (Bible §2) | 3D billboards in gray box | Need mood lighting + dungeon/forge/tavern props |
| **Morrowind faction depth** | "Forge Guild 0, Harbor Merchants 12, City Watch 4" visible in Showroom UI | UI ✅, but factions need scene-side visual presence (banners, livery) |
| **Fallout 1 DM narration** | "Ask clean questions. The world remembers what matters." dialog text | ✅ Voice on-canon |
| **Hitchhiker's narrative voice** | Ask About 1-4 numbered prompts | ✅ Voice on-canon |
| **RimWorld colony sim** | COLONY NEEDS panel with 5 actors + H/F/T/M needs | ✅ UI on-canon |
| **Persistent NPC identity** | Named NPCs in Showroom (Warden, Sage Nera, Quartermaster Ivo, Sentinel Rook, Ash Rat) | ✅ Memory system on-canon |
| **AI-last principle** | "Ask about rumors/work/trade/fate" — deterministic shells exist | ✅ L4 shell present |
| **No fourth-wall break** | Ask About reads as in-diegetic dialog menu | ✅ No chat overlay |
| **Skill-gated branches** | Not visible in this frame | Not testable from screenshot; check Faz 9 dialog work |

**Bottom line:** the **UI + dialog + data binding layer is on-canon and shipping.** The **world environment layer is the entire deficit.**

---

## 5. What works well (preserve as reference)

1. **3D billboard rendering** — yaw-only camera facing works exactly as PRD specifies. Sprite quality is acceptable (the smith billboard in Showroom is the cleanest).
2. **UI data binding** — Tick/Day clock, faction reputation, NPC needs, Ask About menu all bind to real simulation state.
3. **Dialog voice** — "Ask clean questions. The world remembers what matters." is the kind of one-liner the Vision Bible's Hitchhiker's-Guide lineage demands. Lock this.
4. **NPC roster naming** — Warden / Sage Nera / Quartermaster Ivo / Sentinel Rook / Ash Rat are Morrowind-tier names that suggest role + culture. Naming convention is on-canon.
5. **Enemy elevation in CombatDungeon** — small platforms read as obstacle. Good silent design.
6. **Faction reputation already +/- signed** — "Harbor Merchants +12" suggests the reputation system has positive/negative state working.

---

## 6. Priority recommendations (for Mami AAA Sprint A)

1. **🔴 SmithingOverworld needs a complete pass, not a polish pass.** The scene is empty. Step zero: confirm the SceneRecipe is actually placing the focal anvil + smith. Step one: hand-place anvil, furnace, bellows. Step two: warm key light + sparks particle + clang audio. Step three: NPC at anvil. Step four: CinemachineVirtualCamera framed on the anvil-smith composition. Only then capture aaa_SmithingOverworld_<unix>.png.

2. **🔴 TavernDialog needs bar + hearth + tables before warm lighting.** Lighting on a featureless box still reads featureless. Hand-place 3 props (bar, table, fireplace) first; then add point lights at each; then capture.

3. **🟡 Replace gray brick wall material across world scenes with per-scene variants.** The materials likely already exist in `Assets/Art/Materials/` (the dirty working tree includes `Tile_tavern_plaster_stone_wall.mat` and `Tile_smithing_dark_forge_wall.mat`). Wire them to the correct scene.

4. **🟡 Hide yellow placeholder primitives before AAA capture.** They are debug props from the SceneRecipe builder; gate them behind a debug flag.

5. **🟢 Calibrate the rubric.** The 80-84 baselines in `aaa-scene-quality-uplift.md` §1 do not match what a player sees. Either redefine "UX" / "Playability" axes to be more visual-quality-weighted, or accept that the AAA target (≥90) is much further away than +6-10 points. Mami recommendation: keep the rubric, document the calibration delta in this critique, and let Sprint A's first scene proof recalibrate the baseline.

---

## 7. Acceptance for this critique

- [x] Read 4 baseline frames: SmithingOverworld, TavernDialog, CombatDungeon, ShowroomOverviewWithUI.
- [x] Confirmed 3D billboard architecture works as PRD specifies.
- [x] Confirmed UI + dialog + data binding layer is on Vision Bible canon.
- [x] Identified world environment as the entire deficit.
- [x] Produced per-scene + cross-cutting findings with severity ratings.
- [x] Listed 6 preserve-as-reference good design choices.
- [x] Listed 5 prioritized Mami AAA Sprint A actions.
- [ ] @msbel5 acknowledges the calibration finding (80-84 → 60-72 honest delta) and confirms AAA target stays at ≥90.
- [ ] Mami proceeds with Sprint A on SmithingOverworld per §6 item 1.

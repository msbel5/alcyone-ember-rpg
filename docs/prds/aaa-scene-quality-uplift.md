# PRD: AAA Scene Quality Uplift (post-cutover)

**Status:** Draft (awaiting @msbel5 approval, scheduled after PR #214 merges)
**Branch policy:** small focused PRs per sprint; never merge before user review
**Trigger:** the Visible Generation Cutover (PR #214) shipped with playability rescue producing scene scores in the **80-84 / 100** range. AAA target is **90+**. This PRD plans the work to close that gap.

---

## 1. Problem statement

The cutover landed visible UI, visible worldgen, character creation, and a Windows64 build that runs. But on a side-by-side comparison with the Morrowind/Daggerfall reference soul described in the cutover PRD §2, the in-Editor scene shots feel like a **gray-box first pass**, not the polished frame the player should see:

| Sahne (Reports/screens/aaa_*.png) | UX | Playability | Gap notes |
|---|---:|---:|---|
| SmithingOverworld | 82 | 80 | Forge glow missing, anvil prop placeholder, no smoke particles |
| ColonyNeeds | 83 | 81 | UI panels readable but worldspace lighting flat |
| SeasonFarm | 81 | 80 | No seasonal palette swap, terrain texture tiling visible |
| TradeMarket | 82 | 80 | Stall props identical, no merchant idle anim, ambient missing |
| CombatDungeon | 84 | 82 | Best of the set; dungeon torch flicker absent, no fog volume |
| RitualHall | 83 | 81 | Altar geometry passable, no glow, candles static |
| TavernDialog | 84 | 82 | UI readable; bar geometry placeholder, no NPC clusters |
| OracleShrine | 82 | 80 | Shrine focal point ok, no godrays, no incense particles |
| ShowroomOverview | 84 | 82 | Gallery layout works; no spotlight setup |
| TavernFlavour | 84 | 82 | Flavour text good; visuals match TavernDialog placeholder |

**Average: 82.9 UX / 81 Playability. AAA threshold: 90+.**

The cutover did the right thing (visible flow > visual polish in v1). This PRD takes the next step.

---

## 2. Vision

After this PRD ships, a player who opens any scene from `Ember/Build Scene/*` sees:

- **Mood-correct lighting** per scene (forge=warm, dungeon=cold blue, shrine=soft amber, tavern=candle-warm).
- **Per-scene focal prop** with a distinct silhouette readable at a 1-second glance (anvil, altar, market stall cluster, bar counter).
- **At least one localised dynamic effect** (forge sparks, dungeon torch flicker, shrine incense, tavern fireplace, farm seasonal palette).
- **Camera framing** that puts the focal prop on a thirds intersection, not dead-center.
- **Audio bed** appropriate to scene (forge clang, market chatter, shrine drone, tavern crowd hum, dungeon drip).
- **NPC placement** that suggests routine, not random scatter (smith at anvil, merchant at stall, oracle at shrine).

Score target: **≥ 90 UX / ≥ 88 Playability** in every scene, measured by the same rubric Codex used during the rescue.

---

## 3. Non-goals (v1 of this PRD)

- Character animation rigging beyond idle.
- Real-time global illumination (URP forward only).
- Procedural prop generation (props are hand-picked from existing kits or AI-generated through the Visible Generation pipeline established in PR #214).
- Multiplayer.
- New combat encounters.

These belong to later PRDs once the AAA visual baseline lands.

---

## 4. Scope split: hand-authored vs Visible Generation

Following the cutover decision (D2 + §8 of `docs/prds/visible-generation-cutover.md`):

| Asset class | Source | Rationale |
|---|---|---|
| Lighting setup (Light components, post-process volumes) | Hand-authored in scene | Spatial; can't be generated |
| Prop placement (transform, scale, rotation) | Hand-authored in scene recipes (`Assets/Editor/Ember/SceneRecipes/*.cs`) | Spatial; ScriptableObject-driven |
| Prop meshes (anvil, altar, stalls) | **Visible Generation** when missing from CoreAssetManifest | Reuses the pipeline shipped by PR #214 |
| Material textures (diffuse/normal/roughness) | **Visible Generation** for placeholder fillers; hand-authored for hero materials | Same pipeline |
| Particle systems (sparks, fog, incense) | Hand-authored prefabs under `Assets/Art/Particles/` | Designer-tunable |
| Audio loops | Hand-authored or licensed packs under `Assets/Audio/` | One-off purchases vs generation |
| Camera framing | Hand-authored CinemachineVirtualCamera per scene | Spatial |
| NPC silhouettes (RGB recolor base) | Already in `GenericNpcBaseManifest` (PR #213) | No new work |
| NPC unique portraits | **Visible Generation** via LLM JSON contract (PRD §9) | Already shipped |

In short: this PRD adds **lighting + prop + particle + audio + camera + NPC placement** authoring. The Visible Generation pipeline from PR #214 produces any missing **mesh / texture / portrait** the recipes reference.

---

## 5. Per-scene work plan (sprint estimate ~ 2 days each, 10 scenes)

For each scene, the deliverable is:

1. New / refactored `<Scene>SceneRecipe.cs` under `Assets/Editor/Ember/SceneRecipes/`
2. Added prop manifest entries in `Assets/Manifests/CoreAssetManifest.asset` (any new meshes go through Visible Generation)
3. Lighting setup (key + fill + rim, post-process volume, optional fog)
4. At least one particle system prefab
5. Camera framing (CinemachineVirtualCamera per scene)
6. Audio bed (single AudioSource looping, mixer-routed)
7. Updated `SceneVisualIntegrityTests.cs` assertions for the new components
8. New `Reports/screens/aaa_<Scene>_<unix>.png` evidence + score table update

### Order (highest impact first):

| # | Scene | Reason for order |
|---|---|---|
| 1 | **SmithingOverworld** | Player tutorial entry; sets the production-quality bar |
| 2 | **TavernDialog** | Hub for narrative beats; visible in every playthrough |
| 3 | **CombatDungeon** | Combat reads first here; pacing depends on mood |
| 4 | **RitualHall** | Magic system entry; needs hero lighting |
| 5 | **OracleShrine** | High narrative weight per visit |
| 6 | **TradeMarket** | NPC density driver |
| 7 | **ColonyNeeds** | Strategy UI overlay clarity |
| 8 | **SeasonFarm** | Seasonal palette swap unique to this scene |
| 9 | **ShowroomOverview** | Catalog view; lower per-visit weight |
| 10 | **TavernFlavour** | Cousin of TavernDialog; mostly reuses kit |

### Sprint shape

- **Sprint A (week 1):** Scenes 1-2 land in one PR (`feat/aaa-uplift-tutorial-hub`). Score rubric calibrated against real frames.
- **Sprint B (week 2):** Scenes 3-4 land in one PR (`feat/aaa-uplift-combat-magic`).
- **Sprint C (week 3):** Scenes 5-6 (`feat/aaa-uplift-narrative-trade`).
- **Sprint D (week 4):** Scenes 7-8 (`feat/aaa-uplift-strategy-seasonal`).
- **Sprint E (week 5):** Scenes 9-10 + cross-scene polish PR (`feat/aaa-uplift-catalog-finale`).

5 PRs total, each independently reviewable, each unlocking visible playtest improvements.

---

## 6. Quality gates (per PR in this PRD)

| Gate | Tool | Acceptance |
|---|---|---|
| Compile | Unity 6.3.13f1 batchmode | 0 C# errors |
| EditMode tests | Unity Test Runner | All previously passing tests stay green; new `SceneVisualIntegrityTests` entries added per scene pass |
| Score rubric | `PlayabilityScoreTests` (added in PR #214) | UX ≥ 90, Playability ≥ 88 per touched scene |
| Visual proof | Capture via `Ember/Capture/Active Scene Screenshot` | `Reports/screens/aaa_<scene>_<unix>.png` committed |
| Frame budget | Unity Profiler in PlayMode | ≤ 4 ms CPU / ≤ 8 ms GPU at 1080p on the dev machine |
| Memory budget | Same | No leaks across 5 scene loads (Profiler RAM stable) |

Failures halt the PR; no acceptance check is downgraded "partial pass" without a PR comment from @msbel5.

---

## 7. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Visible Generation produces inconsistent prop styles | All prop prompts go through `StaticPromptCatalog` with `EmberStyleHeader` (PR #214) so palette stays coherent |
| Particle systems tank frame budget on Pi target | All particles capped at 64 instances; Profiler check per PR |
| Lighting setup overrides existing UGUI panel readability | Post-process volume only inside scene worldspace, UI canvas on Overlay sorting layer (PR #214) — already isolated |
| Score rubric is subjective | Tests assert presence of mandatory components (Light, AudioSource, CinemachineVirtualCamera, ParticleSystem, focal prop tag); subjective grade is a comment, not a gate |
| Cross-scene state leak (lighting probes baked once across scenes) | Per-scene reflection probe + per-scene fog settings; no global lightmap dependency |

---

## 8. Out of scope (later PRDs)

- Quest design (PRD-Q, separate doc)
- Skill tree balance (PRD-S, separate doc)
- Save/load polish (already shipped in earlier PRs)
- Localization (English only for v1)
- Mobile / VR

---

## 9. Acceptance

- [ ] All 10 scenes score UX ≥ 90 and Playability ≥ 88 in `PlayabilityScoreTests`
- [ ] All 10 scenes have a visible particle system, lighting setup, CinemachineVirtualCamera, AudioSource
- [ ] `Reports/screens/aaa_<Scene>_<unix>.png` committed for each scene (replacing the 80-84 reference shots in `Reports/aaa-playability-rescue_<old>.md`)
- [ ] No regression: PR #214's Acceptance §18 stays green
- [ ] Frame budget honored on the dev machine
- [ ] Final report `Reports/aaa-uplift_<unix>.md` written, with before/after score table, screenshots, and Profiler captures

---

## 10. Handoff

When @msbel5 approves this PRD: open a GitHub issue per Sprint A–E, attach the Codex mission prompt (Alcyone-Mind cutover style), and assign Codex. Each sprint PR follows the same "tests first, commit cadence numbered, evidence-only report" rules from PRD §17 of the cutover PRD.

The Visible Generation pipeline from PR #214 is the **dependency** here, not a deliverable — every "missing prop / texture" the scene recipe declares queues through `VisibleGenerationPipeline` instead of being hand-painted.

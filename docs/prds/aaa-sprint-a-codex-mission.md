# Codex Mission — AAA Uplift Sprint A (SmithingOverworld + TavernDialog)

> Copy this whole file into Codex Desktop as the initial prompt.
> Full PRD: `docs/prds/aaa-scene-quality-uplift.md`.
> This sprint targets the first two scenes (highest player impact). Same branch as PR #214 — **do NOT open a new branch**.

---

Codex, görev: **Ember CRPG'nin SmithingOverworld + TavernDialog sahnelerini AAA kalitesine çıkar.**

Bu Sprint A — tek mission, tek PR, mevcut PR #214 branch'i (`docs/codex-mission-v2`) üstüne commit'le. Yarım bırakma:
**oku → planla → testleri yaz → kodu yaz → test et → ekran kanıtı topla → final rapor yaz → push.**

@msbel5 direktifi: AAA olmadan merge yok. Mevcut Game view screenshot'ı (grid+sarı şerit+kahverengi zemin) **placeholder seviyesi** — bu sprint'in çıktısı bu iki sahnenin **UX≥90, Playability≥88 / 100** seviyesine taşınmasıdır.

================================================================================
0. TARGETS
================================================================================

| Field | Value |
|---|---|
| Repo | `msbel5/alcyone-ember-rpg` |
| Working dir | `C:\Users\msbel\projects\alcyone-ember-rpg` |
| **Branch** | **`docs/codex-mission-v2`** — same as PR #214, do NOT branch off |
| Active PR | **#214** (draft) — your commits stack on top |
| Unity | `E:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe` |
| Sprint scope | 2 scenes only: `SmithingOverworld`, `TavernDialog` |
| Time estimate | ~2 days each, ~4 day total sprint |
| Final report | `Reports/aaa-uplift-sprint-a_<unix>.md` |

**Pre-flight gate:**

```powershell
Set-Location C:\Users\msbel\projects\alcyone-ember-rpg
git fetch origin
git checkout docs/codex-mission-v2
git pull --ff-only origin docs/codex-mission-v2
git log -1 --format=%H              # must match origin/docs/codex-mission-v2 head
gh pr view 214 --json state --jq '.state'   # must be OPEN (draft)
```

If anything diverges: post `BLOCKED: <reason>` on PR #214 and stop.

================================================================================
1. PLAYER-VISIBLE TARGET
================================================================================

When this sprint merges, a player opening `Ember/Build Scene/3. Smithing Overworld` or `Ember/Build Scene/9. Tavern Dialog` sees:

**SmithingOverworld:**
- A glowing forge with **visible orange-hot embers and sparks** drifting upward
- An **anvil prop** centered on a thirds intersection of the camera frame
- **Warm key light** from the forge plus **cool fill** from outside (sky/window)
- Two smiths positioned in working stance, not random spawn
- A **smoke particle column** rising from the forge chimney
- Forge clang **audio loop** + ambient outdoor wind
- **Bloom + warm color grading** post-process

**TavernDialog:**
- A **lit hearth with flickering fire particles** at one end
- A **bar counter prop** with stools and bottle silhouettes
- **Candle-warm key light** from hearth + **cool moonlight** through a window
- Three NPCs in distinct postures (innkeeper at bar, patron at stool, sage at table)
- **Crowd murmur + fire crackle** audio loop
- **Bloom + warm grading** post-process

Both: **CinemachineVirtualCamera** placed for hero framing, no straight-on flat angles.

================================================================================
2. SCOPE CONTRACT
================================================================================

**In scope (this sprint):**

- New helper classes under `Assets/Editor/Ember/SceneBuilders/`:
  - `EmberParticleBuilder.cs` — forge sparks, smoke, hearth fire, hearth embers, ambient dust
  - `EmberCinemachineBuilder.cs` — CinemachineVirtualCamera per scene (Cinemachine 3.1.6 already in Packages/manifest.json)
  - `EmberAudioBuilder.cs` — AudioSource loop with AudioMixerGroup routing (UI / Ambient / SFX)
  - `EmberPostProcessBuilder.cs` — URP Volume + VolumeProfile with Bloom + ColorAdjustments
  - `EmberPropBuilder.cs` — primitive-based focal props (anvil = cylinder+cube combo with metal material; bar counter = stretched cube with wood material; bottle silhouettes = small cylinders)
- Updates to `SmithingOverworldSceneRecipe.cs` and `TavernDialogSceneRecipe.cs` to call the new helpers
- New `Assets/Manifests/CoreAssetManifest.asset` entries (if any new texture / icon needed) — route through Visible Generation pipeline (PRD §7)
- New EditMode tests: `SmithingOverworldVisualIntegrityTest.cs`, `TavernDialogVisualIntegrityTest.cs` — assert presence of Light/ParticleSystem/CinemachineVirtualCamera/AudioSource/post-process Volume + focal prop named anvil/bar
- New PlayMode test: `SmithingOverworldPlayableSceneTest.cs`, `TavernDialogPlayableSceneTest.cs` — load scene, run 2 frames, capture screenshot, assert frame budget ≤ 4 ms CPU / 8 ms GPU
- Updated `Reports/aaa-uplift-sprint-a_<unix>.md` with before/after screenshots + score table

**Out of scope (other sprints):**

- The other 8 scenes (Sprint B-E will handle them)
- Real PBR materials with normal/roughness maps (use URP/Lit basic colors for now; AI-generated textures route through Visible Generation in a later sprint)
- Character rigging beyond what's already in `EmberWorldspaceBuilder.SpawnActor`
- New UI screens (Boot, Loading, CharacterCreation, Worldgen already shipped in PR #214)
- Real audio assets — use Unity AudioSource with placeholder built-in test tones; mark `// TODO: replace with licensed audio in audio-pack sprint` so the next sprint picks it up

================================================================================
3. ARCHITECTURE
================================================================================

Each scene recipe stays single-method `Build()` but calls into the new builders. Example for SmithingOverworld:

```csharp
public void Build()
{
    // existing terrain + smiths + furnace setup (do not delete)
    BuildTerrain();
    BuildSmiths();
    BuildFurnaceMarker();

    // NEW — AAA additions
    EmberPropBuilder.BuildAnvil(parent: smithingRoot, position: new Vector3(0f, 0f, 2f));
    EmberLightingBuilder.AddForgeGlow(position: new Vector3(0f, 1.2f, 3f), color: new Color(1f, 0.45f, 0.12f), intensity: 2.5f, range: 8f);
    EmberLightingBuilder.AddRimLight(target: smithingRoot, color: new Color(0.6f, 0.7f, 1f));
    EmberParticleBuilder.SpawnForgeSparks(parent: forgeMarker, intensity: 12f);
    EmberParticleBuilder.SpawnSmokeColumn(position: new Vector3(0f, 4f, 3f), color: new Color(0.4f, 0.4f, 0.4f, 0.6f));
    EmberCinemachineBuilder.BuildHeroCam(name: "SmithingHeroCam", lookAt: anvilProp.transform, followOffset: new Vector3(0f, 1.7f, -4f));
    EmberAudioBuilder.PlayLoop(parent: forgeMarker, mixerGroup: "SFX", clipId: "sfx_forge_clang", volume: 0.35f);
    EmberAudioBuilder.PlayLoop(parent: smithingRoot, mixerGroup: "Ambient", clipId: "amb_wind_outdoor", volume: 0.20f);
    EmberPostProcessBuilder.AddVolume(parent: smithingRoot, preset: "SmithingWarmGlow");
}
```

Each helper is its own file, <100 LOC, has its own EditMode unit test for the "doesn't throw + returns the right kind of GameObject" contract.

Recipe-level integration test (`SmithingOverworldVisualIntegrityTest`) asserts the scene after `Build()`:

```csharp
[Test]
public void SmithingOverworld_HasAaaComponents()
{
    var scene = new Scene();
    new SmithingOverworldSceneRecipe().Build();
    Assert.That(GameObject.Find("AnvilProp"), Is.Not.Null, "focal prop");
    Assert.That(Object.FindObjectsOfType<Light>().Length, Is.GreaterThanOrEqualTo(3), "key + fill + rim");
    Assert.That(Object.FindObjectsOfType<ParticleSystem>().Length, Is.GreaterThanOrEqualTo(2), "sparks + smoke");
    Assert.That(Object.FindObjectOfType<Unity.Cinemachine.CinemachineVirtualCameraBase>(), Is.Not.Null, "hero cam");
    Assert.That(Object.FindObjectsOfType<AudioSource>().Length, Is.GreaterThanOrEqualTo(2), "sfx + ambient");
    Assert.That(Object.FindObjectOfType<UnityEngine.Rendering.Volume>(), Is.Not.Null, "post-process");
}
```

================================================================================
4. PLAYABILITY SCORE RUBRIC
================================================================================

Extend existing `Assets/Tests/EditMode/Playability/PlayabilityScoreTests.cs` so each scene scores:

| Component | Weight | Pass criterion |
|---|---:|---|
| Focal prop present | 15 | named prop visible to camera, on thirds |
| Lighting (key + fill + rim) | 20 | ≥ 3 Light components, color mood matches scene tag |
| Particles (≥ 1 dynamic effect) | 15 | ≥ 1 active ParticleSystem with emission > 0 |
| Cinemachine framing | 15 | CinemachineVirtualCamera exists, framing offset non-zero |
| Audio bed | 10 | ≥ 1 looping AudioSource active |
| Post-process | 10 | URP Volume with Bloom OR ColorAdjustments enabled |
| NPC placement (non-random) | 10 | each Actor position not at origin, distance > 0.5m between actors |
| Frame budget | 5 | profiler sample ≤ 4 ms CPU / 8 ms GPU |

Total = 100. Sprint A acceptance: **≥ 90** for both SmithingOverworld and TavernDialog.

================================================================================
5. EVIDENCE (mandatory)
================================================================================

Capture and commit:

```
Reports/screens/aaa-sprint-a/
  smithing_before_<unix>.png            (placeholder Game view from current state)
  smithing_after_<unix>.png             (post-cutover Game view)
  smithing_thirds_overlay_<unix>.png    (annotated thirds grid showing anvil placement)
  tavern_before_<unix>.png
  tavern_after_<unix>.png
  tavern_thirds_overlay_<unix>.png
  profiler_smithing_<unix>.png          (Unity Profiler frame breakdown)
  profiler_tavern_<unix>.png
```

Capture via `Ember/Capture/Active Scene Screenshot` (existing menu) or `Unity_Camera_Capture` MCP tool.

================================================================================
6. COMMIT CADENCE
================================================================================

Keep small focused commits on `docs/codex-mission-v2`:

1. `report(sprint-a kickoff): pre-flight + before screenshots`
2. `feat(scene-builders): add EmberParticleBuilder + tests`
3. `feat(scene-builders): add EmberPropBuilder + tests`
4. `feat(scene-builders): add EmberCinemachineBuilder + tests`
5. `feat(scene-builders): add EmberAudioBuilder + tests`
6. `feat(scene-builders): add EmberPostProcessBuilder + tests`
7. `feat(scenes): AAA uplift SmithingOverworld + integrity test`
8. `feat(scenes): AAA uplift TavernDialog + integrity test`
9. `feat(playmode): playable scene profiler tests`
10. `report(sprint-a final): evidence + after screenshots + score table`

Push after each green local validation. **Do NOT merge.** @msbel5 reviews.

================================================================================
7. ACCEPTANCE — BLOCKING
================================================================================

- [ ] Unity 6.3.13f1: 0 compile errors
- [ ] EditMode tests green (all previous + 7 new helpers + 2 integrity)
- [ ] PlayMode tests green (2 new scene tests)
- [ ] `SmithingOverworld` PlayabilityScore ≥ 90
- [ ] `TavernDialog` PlayabilityScore ≥ 90
- [ ] Frame budget ≤ 4 ms CPU / 8 ms GPU at 1080p for both scenes
- [ ] All 8 screenshots in `Reports/screens/aaa-sprint-a/` committed
- [ ] `Reports/aaa-uplift-sprint-a_<unix>.md` final report committed
- [ ] No regression to PR #214 acceptance §18

================================================================================
8. WORKING STYLE
================================================================================

- Same rules as the cutover mission prompt: SOLID, no 800-line files, tests first, fail-soft.
- Static asset prompts hand-authored if any new texture comes through Visible Generation.
- Existing UGUI MainMenu / HUD / dialog **DO NOT TOUCH** — only worldspace scene content.
- `git push --force` YOK; rebase needed → ask in PR #214 comment first, then `--force-with-lease`.
- "Yaptım" deme; komut + screenshot path + test sayısı + frame ms koy.

---

**Eğer PRD yeterince netleşmediyse: PR #214 yorumu olarak `BLOCKED: <one-line question>` yaz, @msbel5 yanıtlasın, sonra devam.**

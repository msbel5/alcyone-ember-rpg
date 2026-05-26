# PRD: Visual Architecture — 3D World with Camera-Facing Billboard Actors

**Project:** Ember RPG (Unity 6 / URP)
**Phase:** 1
**Author:** Alcyone (CAPTAIN), authored from `CameraFacingBillboard.cs` normative reference
**Date:** 2026-05-26
**Status:** Approved (2026-05-26 by @msbel5)

> **Supersedes:**
> - `Reference/PRDs/PRD_architecture_sprite_layers_v1.md` (Godot Texture2D paperdoll compositor)
> - `Reference/PRDs/PRD_architecture_actor_animation_v1.md` (Godot 10-state × 8-facing 2D sprite FSM)
> - `Reference/PRDs/PRD_architecture_area_ambient_v1.md` (Godot z-order layer sprite cycles)
> - `Reference/PRDs/PRD_godot_client.md` (Godot autoload/scene contract)
> - `Reference/PRDs/PRD_godot_ux_accessibility_v1.md` (Godot 1600×900 baseline)
> - `Reference/PRDs/PRD_godot_campaign_shell_contract_v1.md` (Godot shell)
> - `Reference/PRDs/PRD_PLAYABILITY_RESCUE.md` § "Kill criterion: Unity rejected (2026-04-10)" — overridden by Unity pivot (2026-05-XX)

---

## 1. Purpose

Ember RPG's visual layer is a **3D URP world** populated by **camera-facing billboard actors** in the Daggerfall / Arena / Ultima Underworld lineage. This PRD is the single source of truth for how every actor, prop, and effect renders inside a scene, so that frontend PRDs (`PRD_character_creation_v2`, `PRD_frontend_inventory_v1`, the eight `PRD_frontend_creation_*_v1` and per-scene recipes) can stop carrying Godot-era assumptions (sprite z-layers, Node2D positioning, palette tint shaders, fixed `zoom_level=16` viewport scale).

The decision to drop the Godot 2D sprite-layer paradigm was implicit in the Unity pivot (commit history May 2026) and lives only as `Assets/Scripts/Presentation/Ember/Views/CameraFacingBillboard.cs` (28 LOC, yaw-only `LateUpdate` lookat). This PRD makes that decision normative and gives downstream code one place to look.

## 2. Scope

### 2.1 In scope

- Scene-space (worldspace) rendering of: NPCs, player avatar, monsters, worksite props that need facing (furnace, anvil, altar), and projectile / hit-VFX billboards.
- Camera framework (URP camera stack + Cinemachine 3.1.6, already in `Packages/manifest.json`).
- Per-scene focal-prop contract (anvil / bar counter / hearth / altar / market stall — must sit on a thirds intersection for the hero shot).
- Sprite atlas resolution, axis policy, and shadow casting policy for billboard meshes.
- LOD / distance fade for billboards.
- Integration with existing `Assets/Scripts/Presentation/Ember/Views/{ActorView,CameraFacingBillboard,WorksiteView}.cs`.

### 2.2 Out of scope

- 3D character mesh + skeletal animation (rejected — billboard is cheaper, matches Daggerfall mood, no rigging pipeline needed in v1).
- Real-time global illumination (URP forward only; baked lighting where appropriate).
- VR / stereo render.
- World-space UI panels (CharacterCreation overlays etc. stay UI Toolkit / UGUI, see `PRD_visible_generation_cutover.md`).
- Pathfinding / collision (covered by `PRD_architecture_pathfinding_v1` ADAPTed).

## 3. Functional Requirements (FR)

- **FR-01** Every actor (NPC, player avatar, monster) **MUST** render as a single quad mesh with a `CameraFacingBillboard` component.
- **FR-02** The quad's local rotation **MUST** track the active camera every `LateUpdate`, applying `Quaternion.LookRotation(toCamera, Vector3.up)` after zeroing the Y component of the direction vector (yaw-only). Full-Y rotation is allowed only on top-down debug cameras (config flag `_yawOnly = false`, must not ship enabled).
- **FR-03** The quad **MUST** be 1 unit tall × 0.685 units wide at scale 1 (matches the 832×1216 portrait aspect from `PRD_asset_category_expansion_v1.md`'s `body_silhouettes` category). Larger actors scale uniformly via `Transform.localScale`.
- **FR-04** Billboard quads **MUST** cast and receive shadows by default (`MeshRenderer.shadowCastingMode = ShadowCastingMode.On`, `receiveShadows = true`). A `_disableShadows` flag exists for UI-only billboards (compass icons, etc.) but must default off.
- **FR-05** Billboard materials **MUST** use `URP/Lit` with `_Surface = Transparent` and `_AlphaClip = 1.0` (alpha cutout at 0.5) so silhouettes read cleanly against any background and shadows respect the cutout.
- **FR-06** Sprite atlas resolution **MUST** be 1024×1024 or 1024×1536 (chosen per actor archetype in `GenericNpcBaseManifest.asset`, see `PRD_visible_generation_cutover.md` §7.2). Eight-direction atlases (NN, NE, EE, SE, SS, SW, WW, NW) are stored as horizontal strips; the selected facing is set via `MaterialPropertyBlock` UV offset.
- **FR-07** When the camera is within `_lodNearDistance` (default 6 m) the high-detail atlas is bound; between `_lodNearDistance` and `_lodFarDistance` (default 18 m) the low-detail atlas (half resolution); beyond `_lodFarDistance` the billboard renders with `_simplifiedShader = URP/Unlit/Cutout` and one static facing.
- **FR-08** Every gameplay scene **MUST** declare exactly one `CinemachineVirtualCamera` tagged `PrimaryHero` as the hero shot, with a focal prop on a rule-of-thirds intersection. (The focal prop is named per scene: `Anvil`, `Hearth`, `Altar`, `MarketStall`, `Throne`, etc.)
- **FR-09** Every gameplay scene **MUST** have an `EmberPostProcessVolume` tagged `WorldspaceMain`, providing Bloom + ColorAdjustments + Tonemapping. Scene-specific tone (warm forge, cool dungeon) lives in `Volume.profile`.
- **FR-10** When the player enters a scene, the camera **MUST** start on `PrimaryHero` for a configurable beat (default 1.2 s) before passing control to the player camera. This is the "establishing shot" beat.
- **FR-11** Billboards **MUST NOT** flip their `Transform.localScale.x` to mirror facings (older sprite engines did this) — atlas selection handles facing; mirroring breaks shadow direction and normal sampling.
- **FR-12** A `BillboardDebugOverlay` `[InitializeOnLoad]` editor tool, gated by a settings toggle, **MUST** draw the billboard's quad bounds and forward vector in the Scene view for placement debugging.

## 4. Data Structures

```csharp
namespace EmberCrpg.Presentation.Ember.Views
{
    // Authored as ScriptableObject; one asset per archetype under Assets/Manifests/BillboardArchetypes/
    [CreateAssetMenu(menuName = "Ember/Visual/Billboard Archetype", fileName = "BillboardArchetype.asset")]
    public sealed class BillboardArchetype : ScriptableObject
    {
        public string ArchetypeId;            // matches GenericNpcBaseManifest.ArchetypeId
        public Texture2D HighDetailAtlas;     // 8-facing strip, 1024×n
        public Texture2D LowDetailAtlas;      // 8-facing strip, 512×n (half)
        public Vector2Int AtlasFacingCount;   // typically (8, 1) — 8 horizontal facings
        public Vector2 QuadSize = new(0.685f, 1f);
        public float LodNearDistance = 6f;
        public float LodFarDistance = 18f;
        public bool CastsShadows = true;
        public bool DisableShadows = false;
        public Material BaseMaterial;          // URP/Lit cutout
    }

    public enum BillboardFacing : byte
    {
        North = 0, NorthEast = 1, East = 2, SouthEast = 3,
        South = 4, SouthWest = 5, West = 6, NorthWest = 7,
    }
}
```

`CameraFacingBillboard` (the existing 28-LOC component) is the runtime driver — no changes required to ship FR-01 / FR-02. A new sibling component `BillboardFacingResolver` (Phase 1 implementation) reads `transform.forward` versus camera and writes the correct facing into `MaterialPropertyBlock`.

## 5. Public API

### 5.1 `CameraFacingBillboard` (existing — reference only)

```csharp
public sealed class CameraFacingBillboard : MonoBehaviour
{
    public bool YawOnly { get; set; } = true;  // FR-02
    private void LateUpdate();                 // rotates toward Camera.main
}
```

### 5.2 `BillboardFacingResolver` (Phase 1 to add)

```csharp
public sealed class BillboardFacingResolver : MonoBehaviour
{
    public BillboardArchetype Archetype { get; set; }
    public BillboardFacing Current { get; private set; }
    public void ApplyFacing(BillboardFacing f);    // sets MaterialPropertyBlock UV offset
    private void LateUpdate();                      // recomputes facing from camera angle
}
```

### 5.3 `EmberCinemachineBuilder` (Phase 2 to add, referenced by `aaa-sprint-a-codex-mission.md`)

```csharp
public static class EmberCinemachineBuilder
{
    public static GameObject BuildHeroCam(
        string name,
        Transform lookAt,
        Vector3 followOffset,
        float dampingY = 0.5f);                   // FR-08
}
```

### 5.4 `EmberPostProcessBuilder` (Phase 2 to add)

```csharp
public static class EmberPostProcessBuilder
{
    public static GameObject AddVolume(
        Transform parent,
        string presetName);                       // FR-09; loads VolumeProfile from Assets/Manifests/PostProcessPresets/
}
```

## 6. Acceptance Criteria (AC)

- **AC-01** Loading any gameplay scene and walking around with the player camera, every NPC visible to the camera stays upright and faces the camera on its yaw axis. **Test:** PlayMode `BillboardYawOnlyTest` instantiates a NPC at origin, places camera at four cardinal positions, asserts `Mathf.Abs(npc.transform.eulerAngles.x) < 0.01f` and `npc.transform.eulerAngles.y` matches expected yaw per camera position (±0.5°).
- **AC-02** A 1.0-unit-tall billboard quad casts a shadow of expected length on a horizontal plane lit by a directional light at 45° elevation. **Test:** EditMode `BillboardShadowCastTest` builds the rig, advances `Camera.Render`, samples a shadow map slice, asserts non-zero shadow occlusion within the expected pixel band.
- **AC-03** Setting `BillboardArchetype.AtlasFacingCount = (8,1)` and rotating the camera 360° around the actor cycles through 8 distinct atlas UV offsets, each exactly `1f/8f = 0.125f` wide on U. **Test:** EditMode `BillboardFacingResolverCyclesEightUvSlicesTest`.
- **AC-04** Crossing the `_lodNearDistance` and `_lodFarDistance` thresholds swaps to the low-detail and unlit-cutout materials respectively. **Test:** PlayMode `BillboardLodSwapTest` translates camera away from actor across both thresholds, asserts material binding at each side.
- **AC-05** Every scene in `Build Settings` (Boot, MainMenu, CharacterCreation, SmithingOverworld, ColonyNeeds, SeasonFarm, TradeMarket, CombatDungeon, RitualHall, TavernDialog, OracleShrine, ShowroomOverview, TavernFlavour) opens with a `CinemachineVirtualCamera` named `PrimaryHero_<Scene>` and an `EmberPostProcessVolume` named `WorldspaceMain_<Scene>`. **Test:** EditMode `EveryScene_HasHeroCamAndPostProcess_Test` iterates `EditorBuildSettings.scenes`, opens each, asserts both objects exist with the expected name pattern.
- **AC-06** The PlayMode entry into any non-Menu scene plays the `PrimaryHero` establishing shot for the configured beat before handing control to the player camera (default 1.2 s ± 0.1 s tolerance). **Test:** PlayMode `EstablishingShotPlaysOnSceneEnterTest`.
- **AC-07** Billboards never flip on `localScale.x` regardless of facing (asserts `localScale.x > 0` after 360° camera circle). **Test:** PlayMode `BillboardNeverMirrorsOnScaleTest`.
- **AC-08** `BillboardDebugOverlay` toggled on draws Scene-view gizmos that match the billboard's quad bounds within 1 pixel at 1080p. **Test:** EditMode visual snapshot (read screenshot bytes, compare against `Reports/screens/billboard_debug_gizmo_expected.png` with ≤2% pixel-diff tolerance).

## 7. Performance Budget

- Each billboard draw call ≤ 0.05 ms CPU on the dev machine (i7-equivalent, RTX 2060).
- Per-scene billboard count ≤ 40 visible at once (LOD culling).
- Atlas memory budget ≤ 64 MB per scene (8 high-detail atlases × 4 MB + 16 low-detail × 1 MB).
- `LateUpdate` for `CameraFacingBillboard` ≤ 0.002 ms per instance (40 actors → ≤ 0.08 ms frame budget contribution).

## 8. Error Handling

- `Camera.main == null` → `CameraFacingBillboard.LateUpdate` early-returns; no rotation. Logged once per session.
- `BillboardArchetype.HighDetailAtlas == null` → fall back to `LowDetailAtlas`; if both null, render `Resources/Billboards/missing_pink.png` (magenta debug texture) so missing assets are visible in-game.
- `BillboardFacingResolver` invalid facing index → wraps modulo 8.

## 9. Integration Points

- `Assets/Scripts/Presentation/Ember/Views/CameraFacingBillboard.cs` (existing, ships unchanged)
- `Assets/Scripts/Presentation/Ember/Views/ActorView.cs` (consumes `BillboardArchetype` via `BillboardFacingResolver` — Phase 1 wire-up)
- `Assets/Scripts/Presentation/Ember/Views/WorksiteView.cs` (focal-prop FR-08 hook)
- `Assets/Editor/Ember/SceneBuilders/EmberCinemachineBuilder.cs` (Phase 2 — `aaa-sprint-a-codex-mission.md`)
- `Assets/Editor/Ember/SceneBuilders/EmberPostProcessBuilder.cs` (Phase 2)
- `Assets/Manifests/BillboardArchetypes/*.asset` (Phase 1 — 6 entries matching `GenericNpcBaseManifest.asset`)
- `Assets/Manifests/PostProcessPresets/*.asset` (Phase 2 — `SmithingWarmGlow`, `TavernCandle`, `DungeonCold`, `ShrineAmber`, etc.)
- `docs/prds/visible-generation-cutover.md` §10 (Boot scene uses this PRD's hero-cam contract)
- `docs/prds/aaa-sprint-a-codex-mission.md` §2.2 (calls `EmberPostProcessBuilder.AddVolume` per FR-09)

## 10. Migration / Deprecation

Reference PRDs to mark **Superseded by this PRD** in their header front-matter:

| Old PRD | Why superseded |
|---|---|
| `PRD_architecture_sprite_layers_v1.md` | Godot Texture2D paperdoll compositor; this PRD ships single-quad billboards with per-archetype atlas instead |
| `PRD_architecture_actor_animation_v1.md` | Godot 2D sprite FSM; this PRD defines facing via `BillboardFacingResolver` and atlas UV offset |
| `PRD_architecture_area_ambient_v1.md` | Godot z-order layer sprite cycles; this PRD's `EmberParticleBuilder` (Sprint A) covers torch/flag ambient |
| `PRD_godot_client.md` | Godot autoload/scene contract; this PRD plus `PRD_visible_generation_cutover.md` form the Unity equivalent |
| `PRD_godot_ux_accessibility_v1.md` | Godot-specific 1600×900 baseline; ADAPT'ed sections fold into `PRD_unity_client_v1.md` (Phase 2) |
| `PRD_PLAYABILITY_RESCUE.md` § kill-criterion | Unity reject decision is **overridden** by the Unity pivot; this PRD lives on `main` and replaces that section |

The old PRDs **stay in `Reference/PRDs/`** as historical record but each will receive a `> **Superseded by:** docs/prds/PRD_visual_architecture_3d_billboard_v1.md (2026-05-26)` banner during the `prd-audit-2026-05-26.md` pass.

## 11. Resolved Questions (answered 2026-05-26 by @msbel5)

1. **Atlas authoring tool** — Visible Generation Pipeline generates at the **exact target size we need** via AI prompt. FR-03 quad ratio (1.0 × 0.685) stays as the canonical billboard aspect; when the pipeline needs to fill a non-portrait slot, the prompt targets that resolution directly (no pad-to-1024 step). Authoring tool is the existing `OnnxAssetForge` pipeline (SDXL Turbo + SD 1.5 fallback). If a particular generation path creates too much work, regenerate cleanly rather than retrofit.
2. **Per-archetype dust / silhouette outline shader** — **Keep flat URP/Lit cutout for v1.** Rim-light is a Phase 2+ enhancement; revisit when an art pass clearly demands it. Standing rule per user: "do it right, but if too much work, delete and regenerate properly."
3. **`PrimaryHero` establishing shot Space-skip** — **Yes.** The 1.2 s establishing beat is skippable on `Space` press, gated by a `PlayerPrefs` accessibility flag (default: skippable enabled).
4. **Boot scene exception** — **No `PrimaryHero` cam in Boot.** FR-05 / FR-08 / FR-10 apply only to scenes containing `EmberWorldspaceRoot`. Boot follows best-practice for menu scenes (single full-screen Canvas at Overlay sorting layer, no worldspace camera framing).

> **Approval banner:** approved by @msbel5 on 2026-05-26 in the design-critique session. The 8 Reference PRDs in §10 still need their `> **Superseded by:** ...` banners — that's a follow-up doc-cleanup commit and is not gating Sprint A.

## 12. Reference Library (Vision Bible §11 clean-room rule)

User confirmed reference engines copied to `Reference/library/`:

- `Reference/library/daggerfall-unity-master/` — **primary reference** for billboard rendering, dungeon prop placement, hit-chance math. Read before any Sprint A scene work.
- `Reference/library/openmw-master/` — secondary reference for NPC schedule + faction reputation.
- `Reference/library/gemrb-master/` — RTWP combat reference (Phase 7+).
- `Reference/library/dwarf-fortress-legacy/` — off-world simulation tick reference.

**Clean-room rule:** read, then write our own. No copy-paste from these repos (license isolation).

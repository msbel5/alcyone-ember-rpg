# PRD: Overland Map — Daggerfall-style Procedural Open World v1

**Project:** Ember RPG (Unity 6 / URP)
**Phase:** 2 (Post-Sprint-E AAA-uplift)
**Author:** Mami (visual track, via Claude session 2026-05-26)
**Date:** 2026-05-26
**Status:** Draft (companion to aaa-scene-quality-uplift.md §12 amendment)
**Reference:** `Reference/library/daggerfall-unity-master/` (clean-room rule per Vision Bible §11)

> **Premise:** `aaa-scene-quality-uplift.md` §12 declared the 10 named scenes are biome / settlement / encounter **templates**, not standalone rooms. This PRD specifies the system that composes templates into a procedurally generated Daggerfall-shaped overland map, the system the player traverses between template instances.

---

## 1. Purpose

Player needs a continuous Daggerfall-style world: a procedurally generated overland map of regions, biomes, settlements, and dungeons, traversable on foot and by fast-travel, that **instantiates the 10 AAA scene templates** as the player enters specific tagged locations.

This PRD is the contract for the open-world layer. It is **architecture only** — the actual content (region table, biome list, settlement roster) lives in `Assets/Manifests/` data rows.

---

## 2. Scope

In scope:

- Region grid (16×16 tiles default, configurable per WorldgenService seed).
- Biome assignment per tile (Plains, Forest, Mountain, Coast, Swamp, Desert, Tundra, Ash — 8 biomes matching Daggerfall climate count).
- Settlement placement (City, Town, Village, Hamlet, Inn, Shrine, Dungeon — 7 types).
- Travel UI (top-down map view with player marker + fast-travel selection).
- Scene template instantiation (when player enters a tagged settlement, load matching template + seeded prop variation).
- Persistence (settlement state, faction reputation, off-world tick per Vision Bible §4).

Out of scope (later PRDs):

- Real-time weather propagation.
- Caravan AI routing (Faz 6 already specs).
- Procedural dungeon interior generation (Daggerfall's RDB system — separate PRD).
- Quest beacon placement (DM module, Faz 12).

---

## 3. Functional Requirements

### FR-01 Region grid
`OverlandMap` is a 16×16 grid of `RegionTile` records. Each tile carries: `RegionId`, `BiomeKind`, list of `SettlementId`, list of `EncounterId`, `Climate` enum, deterministic prop-variation seed.

### FR-02 Deterministic generation
World seed (uint, set in CharacterCreation) → `OverlandWorldgen.Generate(seed)` → `OverlandMap`. Same seed = same map across saves and across machines. No `UnityEngine.Random` inside generation.

### FR-03 Biome distribution
Voronoi-style seeded biome assignment with neighbor-coherence pass (no single-tile biome islands unless intended). Reference: `Reference/library/daggerfall-unity-master/Assets/Scripts/Game/UserInterfaceWindows/TravelMapWindow.cs` for the climate map rendering pattern.

### FR-04 Settlement placement
Per biome, settlement density table determines count. Settlements are placed on tiles with road-network connectivity (Steiner-tree approximation) so no orphan settlements.

### FR-05 Settlement → template mapping
Each settlement has a `TemplatePack` referencing one or more scene templates:

| SettlementKind | Templates instantiated when entered |
|---|---|
| City | TradeMarket + SmithingOverworld + TavernDialog + RitualHall + ColonyNeeds (multi-scene complex) |
| Town | SmithingOverworld + TavernDialog + ColonyNeeds |
| Village | SmithingOverworld + TavernDialog |
| Hamlet | TavernDialog (small inn only) |
| Inn (isolated) | TavernDialog or TavernFlavour |
| Shrine | OracleShrine or RitualHall (based on shrine alignment) |
| Dungeon | CombatDungeon (with procedural depth) |

### FR-06 Travel UI
Top-down map view, scrollable, zoomable. Player marker at current tile. Click settlement → fast-travel confirmation. Reference Daggerfall-Unity `TravelMapWindow`.

### FR-07 Scene instantiation contract
When player initiates entry into a settlement:
1. `OverlandTravelService` requests `SceneInstantiationRequest(settlementId, seed)`.
2. `SceneTemplateLoader` invokes the matching SceneRecipe Build() with a per-settlement prop-variation seed (so two villages of the same template look subtly different).
3. Player spawned at the template's `EmberScenePlacement.ComputePlayerSpawn` position.
4. Settlement state (NPC moods, faction relations, completed quests) restored from `SettlementMemoryStore`.

### FR-08 Persistence
`SettlementMemoryStore` (Domain) carries the per-settlement state across save/load and across multiple visits. Persistent across save slots. Off-world tick (Faz 5+ per ROADMAP) updates settlement state even when player is elsewhere.

### FR-09 Fast-travel
Selecting a known settlement on the map performs `OverlandTravelService.FastTravelTo(settlementId)`:
- Advances `GameTime` by computed travel duration (settlement distance / player travel speed).
- Triggers ambush encounters per random-roll (Daggerfall pattern).
- Final tick advances NPC routines for elapsed time.

### FR-10 First-person traversal (Phase 2.5)
Initial v1 ships **only fast-travel** (Daggerfall-style click-to-travel). First-person on-foot overland traversal is a Phase 2.5 deliverable that requires terrain streaming.

### FR-11 Discovery
Settlements are hidden on the map until the player "discovers" them (enters within proximity). Discovered settlements are persisted in `PlayerKnowledgeStore`.

### FR-12 Determinism gate
`OverlandWorldgenTests` (EditMode) confirms seed=42 produces a fixed map: same biome at tile (8,5), same settlement count, same encounter count. Same gate Worldgen already uses (per soul-acceptance Seed 42 → 932 NPCs).

---

## 4. Data Structures (pure Domain)

```csharp
// Assets/Scripts/Domain/Overland/RegionId.cs
public readonly struct RegionId : IEquatable<RegionId> { ... }

// Assets/Scripts/Domain/Overland/RegionTile.cs
public sealed class RegionTile
{
    public RegionId Id { get; }
    public BiomeKind Biome { get; }
    public IReadOnlyList<SettlementId> Settlements { get; }
    public IReadOnlyList<EncounterId> Encounters { get; }
    public ClimateKind Climate { get; }
    public uint PropVariationSeed { get; }
}

// Assets/Scripts/Domain/Overland/OverlandMap.cs
public sealed class OverlandMap
{
    public int Width { get; }
    public int Height { get; }
    public RegionTile this[int x, int y] { get; }
    public IReadOnlyList<RegionTile> Tiles { get; }
    public bool TryGetTile(RegionId id, out RegionTile tile);
}

// Assets/Scripts/Domain/Overland/BiomeKind.cs
public enum BiomeKind { Plains, Forest, Mountain, Coast, Swamp, Desert, Tundra, Ash }

// Assets/Scripts/Domain/Overland/SettlementKind.cs
public enum SettlementKind { City, Town, Village, Hamlet, Inn, Shrine, Dungeon }

// Assets/Scripts/Domain/Overland/TemplatePack.cs
public sealed class TemplatePack
{
    public SettlementKind Kind { get; }
    public IReadOnlyList<string> TemplateNames { get; }  // matches SceneRecipe.SceneName
}
```

## 5. Public API (Simulation)

```csharp
// Assets/Scripts/Simulation/Overland/OverlandWorldgen.cs
public static class OverlandWorldgen
{
    public static OverlandMap Generate(uint seed, int width = 16, int height = 16);
}

// Assets/Scripts/Simulation/Overland/OverlandTravelService.cs
public sealed class OverlandTravelService
{
    public TravelOutcome FastTravelTo(SettlementId target, OverlandMap map, ActorState player, GameTime now);
    public IReadOnlyList<EncounterRoll> RollAmbushes(SettlementId from, SettlementId to, uint seed);
}

// Assets/Scripts/Simulation/Overland/SettlementMemoryStore.cs
public sealed class SettlementMemoryStore
{
    public bool TryGet(SettlementId id, out SettlementMemory memory);
    public void Record(SettlementId id, SettlementMemory memory);
    public void TickOffWorld(GameTime advanced, OverlandMap map);
}
```

## 6. Public API (Presentation)

```csharp
// Assets/Scripts/Presentation/Ember/Overland/OverlandMapView.cs (MonoBehaviour)
public sealed class OverlandMapView : MonoBehaviour
{
    public void DisplayMap(OverlandMap map, IReadOnlyCollection<SettlementId> discoveredSettlements);
    public event Action<SettlementId> SettlementSelected;
}

// Assets/Scripts/Presentation/Ember/Overland/SceneTemplateLoader.cs (MonoBehaviour)
public sealed class SceneTemplateLoader : MonoBehaviour
{
    public IEnumerator LoadTemplateAsync(SettlementId settlementId, uint propVariationSeed);
}
```

## 7. Acceptance Criteria

- [ ] `OverlandWorldgenTests.Generate_Seed42_Determinism_PassesGoldenSnapshot` (EditMode).
- [ ] `OverlandMap` for seed=42 has 8 biomes represented, ≥50 settlements, ≥15 dungeons.
- [ ] Travel UI: click "City of Vaelheim" on map → confirmation modal → player loads into TradeMarket+SmithingOverworld+TavernDialog complex with seed-shifted prop variation.
- [ ] `SettlementMemoryStore` round-trips through save/load (JsonUtility-compatible).
- [ ] Off-world tick advances NPC mood + faction reputation at remote settlements per Vision Bible §4.
- [ ] Player can discover, travel, and remember at least 5 settlements across one play session.
- [ ] Frame budget: map view ≤2 ms CPU / ≤4 ms GPU at 1080p on dev machine.

## 8. Performance Budget

| Layer | Budget |
|---|---|
| `OverlandWorldgen.Generate(seed=42)` | ≤80 ms one-time at world start |
| Map view render | ≤2 ms CPU, ≤4 ms GPU |
| Fast-travel transition (full game-state save + scene load + memory restore) | ≤4 s on dev machine |
| Per-tile prop variation generation | ≤500 µs per template instantiation |
| Off-world tick per remote settlement | ≤20 µs per tick |

## 9. Error Handling

- `OverlandWorldgen.Generate` with invalid seed (0) → falls back to seed=42, logs warning.
- `OverlandTravelService.FastTravelTo` with unknown settlement → returns `TravelOutcome.Failed("unknown_settlement")`.
- `SceneTemplateLoader` with missing template → falls back to a generic "ColonyNeeds" template and surfaces the error to LoadingScreen.
- `SettlementMemoryStore` corrupted save → drops settlement memory, regenerates from seed.

## 10. Integration Points

- `Assets/Scripts/Domain/Generation/WorldgenService.cs` (existing, produces NPCs — extend to write into `OverlandMap.SettlementMemoryStore`).
- `Assets/Scripts/Presentation/Ember/UI/EmberWorldGenUI.cs` (existing, worldgen visible flow — extend to show overland-map intro reveal).
- `Assets/Editor/Ember/SceneRecipes/*SceneRecipe.cs` (existing 10 templates — `SceneTemplateLoader` invokes `Build()` per-recipe with prop-variation seed).
- `Assets/Scripts/Simulation/AiDm/NarrationServices.cs` (DM module gets `OverlandMap` read access so it can spawn DM-narrated overland encounters).
- `docs/EMBER_VISION_BIBLE.md` §4 (off-world tick).
- `docs/mechanics/faz-5-plant-growth.md` (off-world tick infrastructure).

## 11. Phase plan

| Phase | Deliverable | When |
|---|---|---|
| **Phase 2.0** (Sprint F) | `OverlandWorldgen` + `OverlandMap` + Travel UI map view + fast-travel + scene template loader | After Sprint E (AAA scene templates all shipped) |
| **Phase 2.5** | First-person on-foot overland traversal with terrain streaming | Phase 2.0 + 1 sprint |
| **Phase 3.0** | Procedural dungeon interior generation (Daggerfall RDB equivalent) | Separate PRD |

## 12. Open Questions

1. Map projection: rectangular grid (Daggerfall) or hex grid (DwarfFortress)? **Default: rectangular** per Vision Bible's Daggerfall lineage; revisit if hex offers gameplay value.
2. Region grid size: 16×16 (256 tiles) seems large enough for v1; larger (32×32) feels like Phase 2.5 effort. **Default: 16×16.**
3. Travel ambush UI: full-scene combat encounter (load CombatDungeon template) or quick-resolve modal? **Default: full-scene** for visual consistency.
4. Save format: JSON (existing pattern) or binary (Daggerfall's SAVEVARS)? **Default: JSON** for debuggability.

These all default to safe choices and can be revised after Phase 2.0 ships.

---

**Approval:** This PRD is the architecture spec. Implementation begins after Sprint E (Sprint F = open world composition). No code lands against this PRD until @msbel5 explicitly opens Sprint F.

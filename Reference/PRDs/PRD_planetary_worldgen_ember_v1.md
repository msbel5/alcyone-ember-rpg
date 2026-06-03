# PRD — Planetary Worldgen ("from the Ember"): deterministic spherical planet pipeline

Status: DRAFT for approval · Date: 2026-06-03 · Owner: Claude + Codex (engine-free) / Claude (Presentation)

## 0. Intent

Replace the flat 1000×1000 plate model with a **round planet** generated from its molten
**ember** forward through real, mathematically-grounded geophysics, run as **deterministic,
streamable stages** so the player watches the world form (Option 1 chunking + Option 2
background-task/event-queue, combined). Geography → resources → settlements → population all
**emerge** from the physics; nothing is hand-placed. The same pipeline scales to ~200 planets via
hierarchical LOD. The colony sim (already live on a coherent clock) consumes the emergent
resources so production has real raw materials.

This is engine-free (Domain/Simulation, no UnityEngine), seeded-deterministic (XorShiftRng, fixed
iteration order), and validated by the fallback harness + the world-proof.

## 1. What the research confirms (each stage has a real, citable method)

- **Spherical representation** — subdivide an **icosahedron** into a geodesic tile grid (icosphere);
  define plates by **random flood-fill from N seed tiles**, give each plate a drift+spin, derive
  boundaries from conflicting motion, then set elevation by a **priority-queue distance field**
  (distance to boundary/center + boundary elevation + tectonic activity). Canonical references:
  Andy Gainey / *Experilous* "Procedural Planet Generation"; Red Blob Games "Procedural map
  generation on a sphere." Deterministic.
- **Plate tectonics** — `platec` (Mindwerks/**plate-tectonics**, behind **WorldEngine**, MIT) is the
  portable reference algorithm (simplified plate model → heightmap: orogeny at convergent, ridges/
  rifts at divergent, trenches, hotspots). **PyTectonics** (3D, "scientifically defensible") is
  CC-NC — *learn the model, do not copy code*. We clean-room **port the algorithm**, not the source.
- **Hydrology & erosion** — **priority-flood** depression filling → **flow accumulation** (D8 /
  flow_t per triangle, flow_s per edge à la Red Blob mapgen4) → rivers + lakes; DF-style "test
  rivers carve channels downhill, grow lakes where they can't reach the sea." Erosion: **stream-power
  law** (hydraulic), **talus/thermal**, **aeolian** (wind).
- **Climate** — axial tilt → insolation by latitude; **Hadley/Ferrel/Polar cells** → wind belts;
  deserts at the **horse latitudes (15–30°)**; **orographic rainfall + rain shadow**; **Whittaker
  diagram** (temperature × precipitation → biome) as a discretized lookup; seasons from tilt.
  References: WorldEngine (rain shadow + Holdridge life zones), Joe Duffy "Climate Simulation for
  Procedural World Generation," Worldbuilding Pasta "Apple Pie from Scratch (Climate)."
- **Sea level & ice** — water budget fills basins to a sea level; an explicit "ice caps melt →
  sea level rises" sub-step is a defensible, dramatic stage for the reveal.
- **Resource genesis (scientifically grounded)** — the **late-veneer hypothesis is real**:
  siderophile metals (gold, platinum, iridium) sank to the core during formation; the crust's
  current siderophile budget was delivered by **late meteoritic bombardment** (~3.8–4.1 Bya). So
  **seeding metal ore at simulated impact sites, weighted by meteorite metal abundances, is
  defensible** — combined with hydrothermal enrichment at plate boundaries. **Coal** forms in
  ancient swamp/forest biomes; **oil/gas** in former marine basins (organic-rich source rock).
- **Dwarf Fortress pipeline** (reproducible, PRNG-seeded): scalar fields (elevation, rainfall,
  temperature, drainage, volcanism, savagery) → erosion + river carving → **biomes from field
  overlap** → mineral/ore layer → **history sim**. We mirror this layered approach on the sphere.

Sources are listed at the bottom.

## 2. Determinism strategy (hard invariant)

- One `XorShiftRng`, seeded from the planet seed, **threaded** (sub-streams per stage by mixing a
  stage constant) so stages are independently reproducible.
- **Fixed iteration order everywhere**: iterate tiles by ascending index; no `HashSet`/`Dictionary`
  enumeration order in logic; no parallelism inside a stage (or parallel + deterministic merge).
- Doubles are fine on the single Win64 target with fixed op order (the existing
  `WorldTickDigestGoldenTests` proves seed→byte-identical is achievable here). No GPU.
- Clean-room implementations of published algorithms (ideas, not code) to avoid license issues.

## 3. The Ember pipeline (stages — each yields events to the live reveal)

| # | Stage | Method | Emergent output |
|---|-------|--------|-----------------|
| 0 | **Ember** | seed → planet params (radius, axial tilt, sea fraction, age, plate count) | the seed of everything |
| 1 | **Icosphere** | subdivide icosahedron to level L → tiles + adjacency | the globe grid |
| 2 | **Plates** | flood-fill N seeds; per-plate drift+spin | plate map + boundaries |
| 3 | **Tectonics** | boundary interaction → elevation (orogeny/ridge/rift/trench), hotspots → volcano chains | heightfield, mountains, volcanoes |
| 4 | **Sea & ice** | water budget → sea level; ice caps melt → level rises | oceans, coasts, lakes basins |
| 5 | **Climate** | tilt→insolation; wind cells; orographic rain + rain shadow; temperature | temp + moisture per tile, wind |
| 6 | **Hydrology** | priority-flood → flow accumulation → rivers/lakes; stream-power + talus + aeolian erosion | rivers, watersheds, eroded terrain |
| 7 | **Biomes** | Whittaker(temp, moisture) + seasons | biome per tile (forest/steppe/desert/…) |
| 8 | **Resources** | impact-seeded metal ore (late-veneer-weighted) + hydrothermal at boundaries; coal in paleo-swamp biomes; oil/gas in marine basins; stone/clay/wood from biome+geology | per-tile resource ledger |
| 9 | **Civilization** | settlement **suitability** = f(fresh water, arable soil, resource access, coast/crossroads); **carrying capacity** → population; local resources → production profile | settlements emerge with size + economy |
| 10 | **History** | existing DF-style history sim runs **on this substrate** | migrations, wars, foundings → present world |

Stages 0–8 are pure geography/geology; 9–10 produce the populated world the colony sim inhabits.

## 4. Resources → emergent settlements → population (the colony-sim feed)

Every tile ends with a ledger: `elevation, biome, soilFertility, freshWater, ore{metal…}, coal,
oilGas, stone, clay, woodDensity`. From that:
- **Where towns appear**: suitability score (water + arable + resource + coast/crossroads). High
  scores seed settlements; mining towns at ore, farm villages on fertile river plains, ports on
  coasts, crossroads hubs on trade lines.
- **How big**: carrying capacity = f(local farmland + water + trade access) → population. **Size and
  population are not authored — they fall out of geography.** (This is the user's "organic settlement
  enlargement.")
- **What they hold/produce**: local resources define each settlement's production + trade goods, so
  the colony sim's jobs/worksites have real raw materials (ore→smith, fields→farmer, forest→wood).

## 5. Scale: ~200 planets × ~3M people — hierarchical LOD (the honest architecture)

Full simulation of 200×3M individually-AI'd agents is not runnable by any machine, and DF does not
do it either. We deliver the *experience* with three tiers + manager classes:

- **Tier 1 — per seed, once**: run the pipeline + history → a compact **WorldSummary** per planet
  (regions, settlements, populations, resource ledgers, factions, notable figures). Cheap to store;
  fully regenerable from seed.
- **Tier 2 — active region**: only where the player is, run the **full colony sim** with
  individually-AI'd, memory-bearing NPCs (the system already live on the coherent clock).
- **Tier 3 — promotion/demotion**: as the player approaches a settlement, **materialize** its NPCs
  deterministically from `(seed, settlementId, personIndex)` with AI + memory; **demote** to
  aggregates on leaving. The 3M are a statistical population that becomes concrete on demand.
- **Managers** (SOLID, one responsibility each): `PlanetManager` (the 200), `WorldSummaryStore`,
  `RegionActivationManager` (load/unload active region), `PopulationManager` (promote/demote +
  deterministic person genesis), `ResourceManager`, all orchestrated by the existing **World
  Director**. The Director's job gets *easier*: it asks managers "who/what is here," it doesn't hold
  600M objects.

## 6. Phased build plan (each phase independently testable + playable) + Codex split

- **Phase 1 — Spherical vertical slice**: icosphere + plates + elevation + sea level + coarse
  biomes + **the live streaming reveal** (combined chunking + background-task/queue). Provable: a
  determinism test (seed→identical globe digest) + the reveal renders the globe forming.
  - *Codex (engine-free)*: stages 0–4 + 7-coarse in Domain/Simulation, deterministic, unit-tested.
  - *Claude (Presentation)*: streaming reveal + sphere→overland projection + integration + proof.
- **Phase 2 — Climate + hydrology + erosion** (stages 5–6): wind cells, rain shadow, rivers,
  watersheds, erosion. Test: rivers reach sea, rain shadows exist.
- **Phase 3 — Resources + emergent settlements** (stages 8–9): meteor-ore + coal/oil + carrying-
  capacity settlements/population. Test: ore at boundaries/impacts, towns on water/resources.
- **Phase 4 — History on the substrate + LOD managers** (stage 10 + §5): promote/demote, World
  Director wiring. Test: summary↔active-region round-trip; one planet end-to-end.
- **Phase 5 — Multi-planet**: the 200, per-planet streaming + save, planet selection.

Determinism gate (fallback harness) + Win64 build + world-proof after every phase. Claude reviews
every Codex diff for drift before committing. Commit + push to main per phase; user pulls + playtests.

## 7. Decisions to confirm

1. **Mesh resolution / planet scale** — recommend icosphere **level 6–7 (~40k–160k tiles)** per
   planet: enough for continents/regions, cheap to run once. (Tunable.)
2. **Replace vs layer** — recommend **replace** the flat `WorldGeography` with the spherical pipeline
   while **keeping the `GeneratedWorld` output shape** so map/history/colony-sim downstream keep
   working (two worldgens = drift). The overland map becomes a projection of the globe.
3. **Phase 1 scope** — confirm the vertical slice (globe forms + streams) lands first, before the
   deep physics, so you can *see* it working early.

## 8. Sources

- Red Blob Games — Procedural map generation on a sphere: https://www.redblobgames.com/x/1843-planet-generation/
- Red Blob Games — Mapgen4: https://www.redblobgames.com/maps/mapgen4/
- Andy Gainey / Experilous — Procedural Planet Generation (via): https://enki2.tumblr.com/post/104104415794/
- Mindwerks/plate-tectonics (platec, MIT via WorldEngine): https://github.com/Mindwerks/plate-tectonics
- WorldEngine: https://github.com/Mindwerks/worldengine
- PyTectonics (CC-NC — learn only): https://github.com/seanth/PyTectonics
- Late veneer / heavy metals from bombardment: https://www.sciencedaily.com/releases/2008/05/080501093513.htm ; https://pmc.ncbi.nlm.nih.gov/articles/PMC8793101/
- Dwarf Fortress advanced worldgen: https://dwarffortresswiki.org/index.php/DF2014:Advanced_world_generation
- Climate sim for worldgen (Joe Duffy): https://www.joeduffy.games/climate-simulation-for-procedural-world-generation
- Worldbuilding Pasta — Climate (biomes & zones): https://worldbuildingpasta.blogspot.com/2020/05/an-apple-pie-from-scratch-part-vib.html

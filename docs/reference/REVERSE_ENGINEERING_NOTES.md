# Reverse-Engineering Notes — Reference Repos → Ember Implementation

Extracted 2026-06-10 from the local reference checkouts (personal, non-commercial use; clean-room concept
extraction — algorithms re-implemented, no code copied verbatim). Each finding lists WHERE it now lives in
this repo, so this is an audit trail, not a wishlist.

## 1. Daggerfall Unity (`reference-data/daggerfall-unity-master`)

### 1.1 ONE map truth — the MapsFile pattern (THE key lesson)
- `Assets/Scripts/API/MapsFile.cs`: ONE 1000×500 map-pixel grid keys EVERYTHING — climate
  (`GetClimate(px,py)`), politics/region (`GetPoliticIndex`), heightmap (WoodsFile by pixel), and LOCATIONS
  (`HasLocation(px,py)`). Reversible transforms in all directions: `MapPixelToWorldCoord`,
  `WorldCoordToMapPixel`, `MapPixelToLongitudeLatitude`, `GetMapPixelID`. The travel map UI
  (`DaggerfallTravelMapWindow.cs:673-730`) draws dots from the SAME grid the world streams from — drift is
  structurally impossible.
- **Ember now:** the planet icosphere is the single source (see §4 audit table). The same equirect formula
  appears in exactly three places and is asserted identical: `PlanetImageSampler.cs:55-60` (atlas pixels),
  `PlanetToWorldMapper.cs:388-392` (geography cells), `WorldGeoSampler.SurfaceAt` (3D terrain). Settlement
  pins anchor to the icosphere tile their cell maps to (`PlanetAtlas.TryGetTileAnchorPercent`) — a dot can
  no longer sit on a neighbouring ocean tile's pixels. Scale is Domain-owned
  (`WorldSpaceProjection.MetersPerTile` = `OverlandParameters.DefaultRegionEdgeKm` × 1000 = 40 km/tile).

### 1.2 Streaming terrain (`StreamingWorld.cs`, `DefaultTerrainSampler.cs`)
- Pool of (2×TerrainDistance+1)² live terrain tiles around the player; floating origin compensation;
  per-tile 129×129 heightmaps = bicubic interpolation of the COARSE world height data + multi-octave noise
  (baseScale 8, noiseScale 4, extra 10); ocean CLAMPED at `oceanElevation`, beach band above it.
- **Ember now:** `TerrainStreamer` (bubble of 256m tiles, per-tile `TerrainData` freed on unload),
  `RuntimeTerrainBuilder` 129×129 heightfields, `WorldGeoSampler` = coarse-data + deterministic value-noise
  detail, beach sand band; settlement pad flattening = DFU's location levelling
  (`TerrainHelper.BlendLocationTerrainJob` recipe: flatten + radial blend).

### 1.3 Travel takes days
- DFU computes journey time from map-pixel distance; the clock advances.
- **Ember now:** `TryTravelToSettlement` advances the REAL world clock 1 day / 40km tile
  (`AdvanceTick(_tick + days × WorldTickComposer.TicksPerGameDay)`, capped 14d until ticking is chunked
  behind a loading screen — cap is an explicit PARTIAL).

## 2. OpenMW (`reference-data/openmw-master`)
- `components/esm3/loadland.hpp`: 65×65 heightfields per cell, all systems keyed per-cell.
- `components/sceneutil/waterutil.cpp` + `mwrender/water.cpp:440-470`: water is a FLAT QUAD positioned at the
  cell's water height — no mesh complexity.
- `components/esmterrain/storage.cpp:482-549`: per-layer blendmaps with edge bleed (splat weights).
- **Ember now:** per-streamed-tile flat water plane at the LOCAL water level (sea or lake —
  `GeoSample.WaterSurfaceMeters`), opaque material (the hand-rolled URP transparent recipe rendered black in
  player builds); 3-layer splat (biome/sand/rock) from sampler + slope.

## 3. Dwarf Fortress (`reference-data/dwarf-fortress-legacy`)
- **FACT:** the folder contains ZERO source files (verified: 0 × .c/.cpp/.h/.cs/.py) — DF is closed-source;
  only the binary + raw data files exist. "Copy DF code" is impossible; its MECHANISMS are public knowledge:
  layered worldgen fields (elevation → temperature → rainfall/drainage → biome), rivers flowing downhill from
  high-rainfall cells, multi-era simulated history with figures/sites/civilizations.
- **Ember equivalent (already ours, now actually USED):** the planet pipeline computes the same layers on a
  10242-tile icosphere — plate tectonics (`TectonicElevation`), climate (`ClimateStage`), hydrology
  (`HydrologyStage` → per-tile `Flow`/`IsRiver`/`IsLake`), resources (`IronOre`/`Coal`/...), settlements,
  multi-era history (`WorldHistorySimulation`, now 1200 years). This data was being DISCARDED after the
  128×64 projection; `GeneratedWorld.PlanetData` + `OverlandMapPlanetStore` now retain and serve it.

## 4. ONE-TRUTH AUDIT — worldgen → map → terrain (all readers share one source)
| Layer | Reads | Projection rule |
|---|---|---|
| Planet sim (SOURCE) | icosphere 10242 tiles | — |
| Geography raster 128×64 | planet | equirect cell centre → nearest tile (`PlanetToWorldMapper:388-410`) |
| Atlas image (M map + reveal) | planet (raster fallback for legacy) | equirect pixel centre → nearest tile (`PlanetImageSampler:55-80`, via `PlanetAtlas`) |
| Settlement pins | planet | cell centre → nearest tile → tile lat/lon → percent (`PlanetAtlas.TryGetTileAnchorPercent`) |
| 3D terrain + water | planet (raster fallback) | tile-frac → equirect lat/lon → nearest-tile IDW (`WorldGeoSampler` + `PlanetSurfaceSampler`) |
| Metres ↔ tiles | Domain constant | `WorldSpaceProjection` (40 km/tile, +Z=north, row 0=north) |

Remaining known seams (open work): river CHANNEL carving from `Flow`/`IsRiver` (data reachable, not yet in
terrain); char-creation reveal still samples the raster path; building interiors.

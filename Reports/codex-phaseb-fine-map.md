You are Codex (gpt-5.5, xhigh) in the Unity 6 CRPG repo at C:/Users/msbel/projects/alcyone-ember-rpg
(EmberCrpg). Quality bar: SOLID, design patterns, LOW lines of code, log + comment, deterministic. Verify
engine-free changes with `bash tools/validation/run-validation.sh --mode fallback`. Do NOT commit (the
orchestrator commits + builds). HARD CONSTRAINT: do NOT modify ANYTHING under
`Assets/Scripts/Presentation/Ember/WorldDirector/**` or `Assets/Scripts/Simulation/WorldDirector/**`
(another agent owns the procedural terrain there right now). Also do NOT touch
`Assets/Scripts/Presentation/Ember/Adapters/**` (just committed by another stream).

TASK — Phase B: a FINE overland map. The player wants a Daggerfall-style fine heightmap PICTURE, not the
current chunky 16x16 coloured cells. The M-key map is rendered by
`Assets/Scripts/Presentation/Ember/UI/OverlandMapPanel.cs` (the host calls its Render(map, playerTile,
locationName) on M). Today it draws a 16x16 grid of solid-colour cells — too blocky.

Replace the chunky cell grid with a FINE generated IMAGE:
- Build a Texture2D at ~512x512 (the world is 640km x 640km = 409,600 km2, so this reads as ~1.6km per pixel;
  512 keeps it fast — do NOT do 1000x1000 unless trivially cheap). Fill it by sampling the overland
  DETERMINISTICALLY per pixel: take the WorldState.Overland biome field (the 16x16 RegionTile biomes via
  OverlandMap.TileAt / TryGetTile) and interpolate it up to the fine resolution, then add deterministic
  Perlin/value noise (seeded from the world seed / RegionTile.PropVariationSeed) so it reads as a continuous
  biome/relief map, NOT blocky cells. Colour per biome — reuse the biome -> colour choices already in
  OverlandMapPanel so the palette is consistent. Apply the texture to a single full RawImage/Image instead of
  256 cell Images.
- Overlay the 56 OverlandSettlement markers (map.Settlements[i].TilePosition scaled to the fine image, as
  small gold dots) and the player's home-region marker (the playerTile already passed to Render, as a white
  dot/ring). Keep the existing header ("16x16 regions - N settlements - 409,600 km2") and the
  "You are in <town>" footer.
- PERFORMANCE: generate the Texture2D ONCE and cache it; only rebuild when the map identity changes (e.g.
  cache by a hash of width/height/seed/settlement-count). Opening M must stay instant after the first build.
- Determinism: same world/seed -> identical map image.

Engine-bound rendering in OverlandMapPanel is fine (Texture2D, validated by the Win64 build). RECOMMENDED:
put the pure pixel/biome/noise sampling in an engine-free class under
`Assets/Scripts/Simulation/Overland/**` (e.g. OverlandMapImageSampler returning a byte[] or Color32[] / a
biome-index grid) so it is UNIT-TESTABLE, and add an EditMode test (same seed -> identical bytes; different
seed -> different). OverlandMapPanel then just uploads it to a Texture2D.

Report the exact files you changed + a one-line why each. Do NOT commit. Do NOT touch WorldDirector/** or
Adapters/**.

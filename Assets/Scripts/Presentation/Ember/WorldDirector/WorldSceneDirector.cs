using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Facade/Director that REALIZES the player's current overland location into the live scene at runtime,
    /// straight from world data — no editor baking. It resolves the home region tile + settlement kind, asks
    /// the deterministic <see cref="SettlementLayoutStrategy"/> for a plan, then drives the runtime builders
    /// (ground → buildings → lighting → player rig). It owns no geometry itself; NPCs are added afterwards by
    /// the host's existing EmberGeneratedActorSpawner (which anchors on the "PlayerRig" this creates).
    ///
    /// This is the heart of "New Game generates the world you stand in": the world is data, and the location
    /// you occupy is built on demand and deterministically (same seed → same town).
    /// </summary>
    public static class WorldSceneDirector
    {
        public static void Realize(IWorldViewReadModel view)
        {
            if (view == null)
            {
                Debug.LogWarning("[WorldDirector] no read model — skipping realize.");
                return;
            }

            var map = view.Overland;
            if (map == null)
            {
                // BROKEN-state failsafe: never strand the player in a black void. Build the Perlin fallback
                // pad + lighting + a rig so the scene stays walkable, and say loudly that the world is missing
                // (this should be unreachable now that fast travel carries the adapter across the reload).
                Debug.LogError("[WorldDirector] BROKEN: no overland map in the generated-world scene — realizing an empty fallback pad.");
                var fallbackRoot = new GameObject("GeneratedLocation");
                var fallbackStreamer = new GameObject("TerrainStreamer");
                fallbackStreamer.transform.SetParent(fallbackRoot.transform, worldPositionStays: false);
                fallbackStreamer.AddComponent<TerrainStreamer>().Initialize(1u, BiomeKind.Plains);
                RuntimeLightingRig.Apply(fallbackRoot.transform, BiomeKind.Plains);
                RuntimePlayerRig.Build(new Vector3(0f, 0.2f, 0f), Quaternion.identity);
                return;
            }

            var homeTile = ResolveHomeTile(map, view.PlayerOverlandTile);
            var kind = ResolveKind(map, view.PlayerOverlandTile);
            string name = string.IsNullOrEmpty(view.StartingSettlementName) ? "the wilds" : view.StartingSettlementName;
            uint seed = homeTile.PropVariationSeed == 0u ? 1u : homeTile.PropVariationSeed;

            // Project-standard logger seam: Simulation code logs through EmberLog once a sink exists.
            if (EmberCrpg.Simulation.Diagnostics.EmberLog.Sink == null)
                EmberCrpg.Simulation.Diagnostics.EmberLog.Sink = Debug.Log;

            Debug.Log($"[WorldDirector] directing settlement '{name}' kind={kind} biome={homeTile.Biome} seed={seed}");

            // Population identity: an Inn must not draw a city crowd. The spawner is created by the host
            // AFTER this method, so the kind-aware cap travels via the static channel.
            RuntimeNpcDensity.Cap = NpcCapFor(kind);
            Debug.Log($"[WorldDirector] npc billboard cap for {kind}: {RuntimeNpcDensity.Cap}");

            var context = new SettlementContext(name, kind, homeTile.Biome, seed);
            var layout = SettlementLayoutStrategyFactory.For(kind).Plan(context);

            var root = new GameObject("GeneratedLocation");

            // Streaming terrain (Phase C): a bubble of seamless Unity-Terrain tiles loaded around the player as
            // they walk (Daggerfall-style), instead of one fixed plane. Flat where the settlement sits, gentle
            // hills outward, biome-textured, no hard edge — the ground "renders as you go".
            // Bind the streamed terrain to the SAME WorldGeography the atlas map renders from, so the ground
            // the player walks is the map (real elevation, sea, beaches) — not a detached Perlin pad.
            WorldGeoSampler.TryCreate(map, view.PlayerOverlandTile, seed, out var geoSampler);
            Debug.Log(geoSampler != null
                ? $"[WorldDirector] terrain bound to world geography (REAL — sea at {geoSampler.SeaLevelMeters:0.#}m rel.)"
                : "[WorldDirector] no geography snapshot — legacy Perlin terrain (PARTIAL).");

            var streamerGo = new GameObject("TerrainStreamer");
            streamerGo.transform.SetParent(root.transform, worldPositionStays: false);
            streamerGo.AddComponent<TerrainStreamer>().Initialize(seed, homeTile.Biome, geoSampler);

            for (int i = 0; i < layout.Buildings.Count; i++)
                RuntimeBuildingBuilder.Build(root.transform, layout.Buildings[i]);
            Debug.Log($"[WorldDirector] {layout.Buildings.Count} buildings built");

            RuntimeLightingRig.Apply(root.transform, homeTile.Biome);

            var spawn = new Vector3(layout.PlayerSpawnX, 0.2f, layout.PlayerSpawnZ);
            RuntimePlayerRig.Build(spawn, Quaternion.Euler(0f, layout.PlayerFacingDeg, 0f));

            // Realize READINESS REPORT (asset-gate v1): one summary line a playtest log is checked against.
            // v2 (queued) blocks behind the loading screen until missing forge sprites are generated.
            bool shore = geoSampler != null && geoSampler.HasLocalShore;
            Debug.Log($"[WorldDirector] realize complete for '{name}': kind={kind}, buildings={layout.Buildings.Count}, " +
                      $"geo={(geoSampler != null ? "REAL" : "LEGACY")}, localShore={shore}, npcCap={RuntimeNpcDensity.Cap}, rig at {spawn}.");
        }

        // Kind → billboard density: the visible half of settlement identity (layout size is the other half).
        private static int NpcCapFor(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.City: return 24;
                case SettlementKind.Town: return 16;
                case SettlementKind.Village: return 10;
                case SettlementKind.Hamlet: return 6;
                case SettlementKind.Inn: return 5;
                case SettlementKind.Shrine: return 4;
                case SettlementKind.Dungeon: return 3;
                default: return 10;
            }
        }

        private static RegionTile ResolveHomeTile(OverlandMap map, GridPosition tilePosition)
        {
            if (map.TryGetTile(tilePosition.X, tilePosition.Y, out var tile)) return tile;
            return map.TileAt(map.Width / 2, map.Height / 2);
        }

        private static SettlementKind ResolveKind(OverlandMap map, GridPosition tilePosition)
        {
            var settlements = map.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                var pos = settlements[i].TilePosition;
                if (pos.X == tilePosition.X && pos.Y == tilePosition.Y)
                    return settlements[i].Kind;
            }
            return SettlementKind.Village; // standing on open land → treat as a small village hub
        }
    }
}

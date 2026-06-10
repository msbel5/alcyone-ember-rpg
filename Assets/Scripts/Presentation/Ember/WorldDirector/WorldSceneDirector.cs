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
                Debug.LogWarning("[WorldDirector] no overland map — skipping realize (standalone scene?).");
                return;
            }

            var homeTile = ResolveHomeTile(map, view.PlayerOverlandTile);
            var kind = ResolveKind(map, view.PlayerOverlandTile);
            string name = string.IsNullOrEmpty(view.StartingSettlementName) ? "the wilds" : view.StartingSettlementName;
            uint seed = homeTile.PropVariationSeed == 0u ? 1u : homeTile.PropVariationSeed;

            Debug.Log($"[WorldDirector] directing settlement '{name}' kind={kind} biome={homeTile.Biome} seed={seed}");

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
            Debug.Log($"[WorldDirector] player rig at {spawn} — realize complete for '{name}'.");
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

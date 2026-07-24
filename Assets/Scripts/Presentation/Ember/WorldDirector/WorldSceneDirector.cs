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

            // F26: the first three shells become FUNCTIONAL — tavern (sleep), temple (heal),
            // shop (trade counter). A glowing sign cube keys each from the street.
            var functionalWorld = new Vector3[3];
            for (int i = 0; i < layout.Buildings.Count; i++)
            {
                var building = RuntimeBuildingBuilder.Build(root.transform, layout.Buildings[i]);
                if (building == null || i >= 3) continue;
                functionalWorld[i] = building.transform.position;
                AttachFunctionalRole(building, i);
            }
            if (layout.Buildings.Count >= 3)
            {
                RuntimeInteriorInfo.Record(functionalWorld[0], functionalWorld[1], functionalWorld[2]);
                var hostAdapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                    as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
                hostAdapter?.PinHostInsideTavern(functionalWorld[0]);
                Debug.Log("[WorldDirector] functional interiors: tavern/temple/shop on buildings 0/1/2.");
            }
            Debug.Log($"[WorldDirector] {layout.Buildings.Count} buildings built");

            // MINES (content phase): the planet's ore layer reaches the ground — a rich IronOre/Coal tile
            // under the settlement realizes a mine mouth at the town edge (the sim's mining worksites get
            // their visible anchor; the richer ore picks the face).
            if (EmberCrpg.Simulation.Overland.PlanetAtlas.TryGetTileOre(
                    map, view.PlayerOverlandTile.X, view.PlayerOverlandTile.Y, out double iron, out double coal)
                && (iron > 0.5d || coal > 0.5d))
            {
                float mineAngle = seed % 360u;
                RuntimeMineBuilder.Build(root.transform, layout.GroundRadius + 14f, mineAngle, coal >= iron);
                Debug.Log($"[WorldDirector] {(coal >= iron ? "coal" : "iron")} mine realized at town edge (iron={iron:0.00}, coal={coal:0.00}).");
            }

            // FIELDS (F7/economy realism): the farm BELT derives from population — DF ratios (1 farmer ≈ 10
            // plots ≈ feeds 3-7 people) scaled to a readable visual. Tier populations: Capital 180k, City
            // 75k, Town 6k, Village 650, Hamlet 200. Stalks still read the live PlantGrowth mirror.
            if (kind != SettlementKind.Dungeon && kind != SettlementKind.Shrine)
            {
                int pop = kind == SettlementKind.City ? 75000
                    : kind == SettlementKind.Town ? 6000
                    : kind == SettlementKind.Village ? 650
                    : kind == SettlementKind.Hamlet ? 200 : 350;
                int plots = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(pop) / 9f), 2, 12);
                // REFORM #1 (ARCHITECTURE_GAPS #4): the polar decor belt is RETIRED - crops render
                // at the sim plants' projected cells via SimFieldView (id-keyed, per-plant stage).
                var simFields = new GameObject("SimFields");
                simFields.transform.SetParent(root.transform, worldPositionStays: false);
                simFields.AddComponent<SimFieldView>();
                Debug.Log($"[WorldDirector] fields={plots} plots for pop={pop} ({kind}) — farm belt at the town edge.");
            }

            // F26: a glowing sign cube above the shell keys the role from the street; the matching
            // trigger view (E inside, chest-view family) does the actual work.
            static void AttachFunctionalRole(GameObject building, int roleIndex)
            {
                var signColor = roleIndex == 0 ? new Color(1.0f, 0.72f, 0.25f)   // tavern amber
                    : roleIndex == 1 ? new Color(0.92f, 0.94f, 1.0f)             // temple white
                    : new Color(0.35f, 0.85f, 0.40f);                            // shop green
                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = roleIndex == 0 ? "TavernSign" : roleIndex == 1 ? "TempleSign" : "ShopSign";
                Object.Destroy(sign.GetComponent<Collider>());
                sign.transform.SetParent(building.transform, worldPositionStays: false);
                sign.transform.localPosition = new Vector3(0f, 4.6f, 0f);
                sign.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                sign.GetComponent<MeshRenderer>().sharedMaterial = RuntimeMaterialPalette.Solid(signColor);
                var glow = sign.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = signColor;
                glow.range = 7f;
                glow.intensity = 1.6f;
                glow.shadows = LightShadows.None;

                if (roleIndex == 0) building.AddComponent<RuntimeTavernView>();
                else if (roleIndex == 1) building.AddComponent<RuntimeTempleView>();
                else building.AddComponent<RuntimeShopCounterView>();
            }

            // DUNGEON MOUTH: a Dungeon "settlement" is a delve, not a hamlet — realize a big dark cave mouth
            // by the plaza (the mine builder's construction at delve scale) so the location reads as what
            // the map legend promises. Interior cells are the queued v2.
            if (kind == SettlementKind.Dungeon)
            {
                float delveAngle = seed % 360u;
                RuntimeMineBuilder.Build(root.transform, 9f, delveAngle, coal: true);
                // F2/dungeon interiors v1: the mouth now leads into a torch-lit corridor and a chamber with
                // a loot chest — walk past the mound and into the dark. Encounters bind in the next F2 item.
                // F18: the barrow grew into the deterministic 5-10 room graph (rooms+corridors+boss).
                RuntimeDungeonBuilder.Build(root.transform, 9f, delveAngle, (int)seed);

                // F10→F18 dwellers: 0-2 Outlaws per room + the Warden (2× HP, 1.5× dmg) by the hoard.
                var dwellerAdapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                    as EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter;
                int dwellers = dwellerAdapter != null
                    ? dwellerAdapter.EnsureDungeonDwellers(
                        RuntimeDungeonLayoutInfo.DwellerSpots, RuntimeDungeonLayoutInfo.BossSpot,
                        RuntimeDungeonLayoutInfo.ArchetypeName) // F29: archetype picks the bestiary mix
                    : 0;
                Debug.Log($"[WorldDirector] delve dwellers ensured: +{dwellers} across {RuntimeDungeonLayoutInfo.RoomCount} rooms (idempotent — corpses stay down).");
            }

            // TRADE CART (F1/caravans): realized once, VISIBLE only while a caravan is at this site — the
            // CaravanCartView polls the mirror the adapter feeds each tick.
            RuntimeCaravanBuilder.Build(root.transform);
            Debug.Log("[WorldDirector] trade cart realized (visibility bound to the caravan mirror).");

            // REGION BANNER (political identity v1): a pole + flag at the plaza edge, coloured
            // deterministically from the tile's RegionId — neighbouring towns of the same region fly the
            // same colours, crossing a border changes them. Faction-level refinement is the queued v2.
            BuildRegionBanner(root.transform, homeTile.RegionId.Value);

            // Paved plaza: the spawn circle was bare ground pretending to be a square. A flat
            // stone disc marks the town heart every layout already keeps clear.
            var plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plaza.name = "PlazaFloor";
            Object.Destroy(plaza.GetComponent<Collider>());
            plaza.transform.SetParent(root.transform, worldPositionStays: false);
            plaza.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            plaza.transform.localScale = new Vector3(14f, 0.02f, 14f);
            plaza.GetComponent<MeshRenderer>().sharedMaterial =
                RuntimeMaterialPalette.Textured("wall_showroomoverview", new Color(0.52f, 0.50f, 0.47f), tiling: 3f);

            // PLAYTEST ("ortada masa gormuyorum, hic esya renderimiz yok"): the communal food
            // spot is a REAL table now - long top on two trestles, benches both sides, and a
            // stone well off-centre. Same flat-shaded primitive language as the buildings;
            // deterministic, zero assets. NPС seat cells ring this table at +/-1..2 m.
            static void PlazaProp(Transform parent, string propName, Vector3 pos, Vector3 size, Material mat)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = propName;
                box.transform.SetParent(parent, worldPositionStays: false);
                box.transform.localPosition = pos;
                box.transform.localScale = size;
                box.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            var plazaWood = RuntimeMaterialPalette.Solid(new Color(0.40f, 0.29f, 0.17f));
            var plazaWoodDark = RuntimeMaterialPalette.Solid(new Color(0.30f, 0.21f, 0.12f));
            var plazaStone = RuntimeMaterialPalette.Solid(new Color(0.47f, 0.46f, 0.43f));
            PlazaProp(root.transform, "TableTop", new Vector3(0f, 0.78f, 0f), new Vector3(3.0f, 0.10f, 1.1f), plazaWood);
            PlazaProp(root.transform, "TableTrestleA", new Vector3(-1.2f, 0.38f, 0f), new Vector3(0.18f, 0.76f, 0.9f), plazaWoodDark);
            PlazaProp(root.transform, "TableTrestleB", new Vector3(1.2f, 0.38f, 0f), new Vector3(0.18f, 0.76f, 0.9f), plazaWoodDark);
            PlazaProp(root.transform, "BenchNorth", new Vector3(0f, 0.24f, 1.05f), new Vector3(2.6f, 0.45f, 0.34f), plazaWoodDark);
            PlazaProp(root.transform, "BenchSouth", new Vector3(0f, 0.24f, -1.05f), new Vector3(2.6f, 0.45f, 0.34f), plazaWoodDark);
            var wellRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellRing.name = "WellRing";
            wellRing.transform.SetParent(root.transform, worldPositionStays: false);
            wellRing.transform.localPosition = new Vector3(4.6f, 0.45f, 3.8f);
            wellRing.transform.localScale = new Vector3(1.4f, 0.45f, 1.4f);
            wellRing.GetComponent<MeshRenderer>().sharedMaterial = plazaStone;
            PlazaProp(root.transform, "WellPost", new Vector3(4.6f, 1.45f, 3.8f), new Vector3(0.12f, 1.2f, 0.12f), plazaWoodDark);

            RuntimeLightingRig.Apply(root.transform, homeTile.Biome);

            var spawn = new Vector3(layout.PlayerSpawnX, 0.2f, layout.PlayerSpawnZ);
            // TAVAN-SPAWN FIX ("zindanlarin tavaninda goruyorum kendimi"): the fixed (0,0.2,0)
            // spawn sits INSIDE the crest-floated dungeon interior; CharacterController
            // depenetration ejected the player onto the ROOF stack. Spawn at the recorded mouth
            // instead — always on the entry floor, facing the corridor.
            if (kind == SettlementKind.Dungeon && RuntimeDungeonLayoutInfo.RoomCount > 0)
                // LIVE BUG ('dungeona isinlandik ama disina'): EntryWorld is the proof-camera
                // anchor JUST OUTSIDE the mouth (the interior digs -Z while this sits at +1.5),
                // and the mine's solid Mound collider stands on the same spot. StartRoomWorld is
                // the generator's entry-room centre - guaranteed clear, floor-level after crest.
                spawn = RuntimeDungeonLayoutInfo.StartRoomWorld + Vector3.up * 0.4f;
            RuntimePlayerSpawn.Record(spawn); // F15: the death-screen awaken teleports the rig back here
            RuntimePlayerRig.Build(spawn, Quaternion.Euler(0f, layout.PlayerFacingDeg, 0f));

            // F3/audio v1: procedural minimum sound set — wind ambience, motion footsteps, encounter sting.
            RuntimeAudioDirector.Attach(GameObject.Find("PlayerRig"));

            // F8/music: GemRB DAY/NIGHT/BATTLE slots, procedurally synthesized, slot follows sim hour +
            // the battle mirror, variant rotates — slot transitions logged for the shipcheck.
            RuntimeMusicDirector.Attach(GameObject.Find("PlayerRig"));

            // F3/swim v1: underwater blue fog below the local waterline (water index fed by the streamer).
            RuntimeWaterIndex.Clear(); // fresh location → fresh levels (travel reload)
            SwimView.Attach(GameObject.Find("PlayerRig"));

            // Realize READINESS REPORT (asset-gate v1): one summary line a playtest log is checked against.
            // v2 (queued) blocks behind the loading screen until missing forge sprites are generated.
            bool shore = geoSampler != null && geoSampler.HasLocalShore;
            Debug.Log($"[WorldDirector] realize complete for '{name}': kind={kind}, buildings={layout.Buildings.Count}, " +
                      $"geo={(geoSampler != null ? "REAL" : "LEGACY")}, localShore={shore}, npcCap={RuntimeNpcDensity.Cap}, rig at {spawn}.");
        }

        private static void BuildRegionBanner(Transform parent, ulong regionValue)
        {
            var root = new GameObject("RegionBanner");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = new Vector3(3.2f, 0f, 3.2f); // plaza edge, clear of the spawn

            float hue = ((regionValue * 47UL) % 360UL) / 360f;
            var flagColor = Color.HSVToRGB(hue, 0.62f, 0.78f);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, worldPositionStays: false);
            pole.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 4.2f, 0.12f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = RuntimeMaterialPalette.Solid(new Color(0.35f, 0.27f, 0.18f));

            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(root.transform, worldPositionStays: false);
            flag.transform.localPosition = new Vector3(0.66f, 3.6f, 0f);
            flag.transform.localScale = new Vector3(1.2f, 0.8f, 0.05f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = RuntimeMaterialPalette.Solid(flagColor);

            Debug.Log($"[WorldDirector] region banner raised (region {regionValue}, hue {hue:0.00}).");
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
                case SettlementKind.Dungeon: return 14; // F18: ≤9 dwellers + the Warden + plaza strays must all fit the nearest-N cull
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

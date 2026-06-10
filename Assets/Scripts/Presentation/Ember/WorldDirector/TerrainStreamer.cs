using System.Collections.Generic;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.WorldDirector;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Daggerfall-style streaming world: keeps a bubble of live terrain tiles around the player and
    /// loads/unloads them as the player walks, so the ground "renders as you go" and the world has no hard
    /// edge. With a <see cref="WorldGeoSampler"/> every tile samples the real world geography (and gets its
    /// biome from the overland tile it stands on); without one it falls back to single-biome Perlin hills.
    /// Manager/Observer pattern: one MonoBehaviour owns the tile dictionary and reacts to the player
    /// crossing a tile boundary.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerrainStreamer : MonoBehaviour
    {
        private float _tileSize = 256f;
        private int _viewRadius = 2; // (2r+1)^2 live tiles around the player
        private uint _seed;
        private BiomeKind _biome;
        private WorldGeoSampler _sampler;

        private Transform _player;
        private readonly Dictionary<Vector2Int, GameObject> _tiles = new Dictionary<Vector2Int, GameObject>();
        private Vector2Int _current = new Vector2Int(int.MinValue, int.MinValue);

        public void Initialize(uint seed, BiomeKind biome, WorldGeoSampler sampler = null, float tileSize = 256f, int viewRadius = 2)
        {
            _seed = seed;
            _biome = biome;
            _sampler = sampler;
            _tileSize = tileSize > 32f ? tileSize : 256f;
            _viewRadius = viewRadius < 1 ? 1 : viewRadius;

            // Build the initial bubble around the world origin NOW, so the player has ground the instant it
            // spawns (rather than one frame later when Update would first run).
            _current = new Vector2Int(0, 0);
            Refresh(_current);
            Debug.Log($"[WorldDirector] terrain streaming online (tile {_tileSize:0}m, {(_viewRadius * 2) + 1}x{(_viewRadius * 2) + 1} bubble, {(_sampler != null ? "geography-bound" : "Perlin fallback")})");
        }

        private void Update()
        {
            if (_player == null)
            {
                var rig = GameObject.Find("PlayerRig");
                if (rig == null) return;
                _player = rig.transform;
            }

            var tile = new Vector2Int(
                Mathf.FloorToInt(_player.position.x / _tileSize),
                Mathf.FloorToInt(_player.position.z / _tileSize));
            if (tile == _current) return;
            _current = tile;
            Refresh(tile);
        }

        // Generate any missing tiles in the bubble; destroy tiles that fell outside it.
        private void Refresh(Vector2Int centre)
        {
            for (int dx = -_viewRadius; dx <= _viewRadius; dx++)
            {
                for (int dz = -_viewRadius; dz <= _viewRadius; dz++)
                {
                    var key = new Vector2Int(centre.x + dx, centre.y + dz);
                    if (!_tiles.ContainsKey(key))
                        _tiles[key] = RuntimeTerrainBuilder.BuildTile(transform, key.x, key.y, _tileSize, TileBiome(key), _seed, _sampler);
                }
            }

            var stale = new List<Vector2Int>();
            foreach (var kv in _tiles)
            {
                if (Mathf.Abs(kv.Key.x - centre.x) > _viewRadius || Mathf.Abs(kv.Key.y - centre.y) > _viewRadius)
                    stale.Add(kv.Key);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                var go = _tiles[stale[i]];
                if (go != null)
                {
                    // Destroying the GameObject does NOT free the heightmap TerrainData — that leak is what
                    // made RAM grow (and the game stutter) the longer you walked. Free it explicitly. The
                    // shared per-biome material/layer are cached in RuntimeTerrainBuilder, so leave them.
                    var terrain = go.GetComponent<Terrain>();
                    if (terrain != null && terrain.terrainData != null) Destroy(terrain.terrainData);
                    Destroy(go);
                }
                _tiles.Remove(stale[i]);
            }
        }

        // Biome of the overland tile under this streamed tile's centre — the world stops being one biome.
        private BiomeKind TileBiome(Vector2Int key)
        {
            if (_sampler == null) return _biome;
            return _sampler.BiomeAt((key.x + 0.5d) * _tileSize, (key.y + 0.5d) * _tileSize);
        }
    }
}

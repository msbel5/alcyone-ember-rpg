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

        // ASYNC STREAMING ("oyun her tickte kasıyor" part 2): the pure-C# sampling half of a tile runs on a
        // background task (one in flight), only the Unity object assembly happens on the main thread. The
        // initial bubble stays synchronous so the player always spawns on solid ground. Thread-safety note:
        // only the single worker calls WorldGeoSampler.Sample during a job (PlanetSurfaceSampler's greedy-walk
        // cache is single-consumer); the main thread only calls BiomeAt, which touches disjoint state.
        private readonly Queue<Vector2Int> _pendingBuilds = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> _queued = new HashSet<Vector2Int>();
        private System.Threading.Tasks.Task<RuntimeTerrainBuilder.TilePrecompute> _buildJob;
        private Vector2Int _buildJobKey;
        private bool _initialBubbleBuilt;

        private void Update()
        {
            if (_player == null)
            {
                var rig = GameObject.Find("PlayerRig");
                if (rig == null) return;
                _player = rig.transform;
            }

            PumpStreaming();

            var tile = new Vector2Int(
                Mathf.FloorToInt(_player.position.x / _tileSize),
                Mathf.FloorToInt(_player.position.z / _tileSize));
            if (tile == _current) return;
            _current = tile;
            Refresh(tile);
        }

        private bool InBubble(Vector2Int key)
            => Mathf.Abs(key.x - _current.x) <= _viewRadius && Mathf.Abs(key.y - _current.y) <= _viewRadius;

        private void PumpStreaming()
        {
            if (_buildJob != null && _buildJob.IsCompleted)
            {
                try
                {
                    var pre = _buildJob.Result;
                    if (!_tiles.ContainsKey(_buildJobKey) && InBubble(_buildJobKey))
                        _tiles[_buildJobKey] = RuntimeTerrainBuilder.BuildTileFromPrecompute(
                            transform, _buildJobKey.x, _buildJobKey.y, _tileSize, TileBiome(_buildJobKey), pre);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[WorldDirector] tile precompute failed for {_buildJobKey}: {ex.Message}");
                }
                _buildJob = null;
            }

            while (_buildJob == null && _pendingBuilds.Count > 0)
            {
                var key = _pendingBuilds.Dequeue();
                _queued.Remove(key);
                if (_tiles.ContainsKey(key) || !InBubble(key)) continue;
                _buildJobKey = key;
                var sampler = _sampler;
                int x = key.x, z = key.y;
                float size = _tileSize;
                _buildJob = System.Threading.Tasks.Task.Run(() => RuntimeTerrainBuilder.Precompute(sampler, x, z, size));
            }
        }

        // Schedule missing tiles in the bubble (async on the geo path after the first bubble; sync legacy);
        // destroy tiles that fell outside it.
        private void Refresh(Vector2Int centre)
        {
            for (int dx = -_viewRadius; dx <= _viewRadius; dx++)
            {
                for (int dz = -_viewRadius; dz <= _viewRadius; dz++)
                {
                    var key = new Vector2Int(centre.x + dx, centre.y + dz);
                    if (_tiles.ContainsKey(key) || _queued.Contains(key)) continue;
                    if (_sampler == null || !_initialBubbleBuilt)
                    {
                        // Legacy Perlin is cheap; the FIRST geo bubble must also be synchronous so the player
                        // spawns on solid ground instead of falling through a not-yet-streamed tile.
                        _tiles[key] = RuntimeTerrainBuilder.BuildTile(transform, key.x, key.y, _tileSize, TileBiome(key), _seed, _sampler);
                        continue;
                    }
                    _pendingBuilds.Enqueue(key);
                    _queued.Add(key);
                }
            }
            _initialBubbleBuilt = true;

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

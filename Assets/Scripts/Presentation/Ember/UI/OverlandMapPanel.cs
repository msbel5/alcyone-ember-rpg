using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Simulation.Overland;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Full-screen overland map overlay, toggled with M. Makes the generated open world VISIBLE: a
    /// fine deterministic biome/relief image with settlement dots and the player's region marked white,
    /// plus a header (size / settlement count / km2) and a "you are in &lt;town&gt;" footer.
    ///
    /// Pattern: pure tick-driven view (same family as <see cref="EventLogHudPanel"/>). The host pushes
    /// the deterministic <see cref="OverlandMap"/> + the player's region tile via <see cref="Render"/>;
    /// the panel owns only presentation. It self-builds its uGUI tree in Awake so it needs no prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverlandMapPanel : MonoBehaviour
    {
        private const float MapPixels = 660f;   // square map area
        private const float SettlementMarkerPixels = 7f;
        private const float PlayerMarkerPixels = 16f;

        private TMP_Text _title;
        private TMP_Text _footer;
        private RectTransform _mapRect;
        private RawImage _mapImage;
        private Texture2D _mapTexture;
        private readonly List<Image> _settlementMarkers = new List<Image>(64);
        private Image _playerMarker;
        private Sprite _dotSprite;
        private Sprite _ringSprite;
        private ulong _cachedMapKey;
        private bool _hasCachedMap;

        public bool IsVisible => isActiveAndEnabled && gameObject.activeSelf;

        private void Awake()
        {
            BuildChrome();
            Hide();
        }

        public void Toggle()
        {
            if (gameObject.activeSelf) Hide();
            else Show();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Debug.Log("[OverlandMap] opened");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Paint the generated overland. Safe to call every frame the panel is open.</summary>
        public void Render(OverlandMap map, GridPosition playerTile, string locationName)
        {
            if (_mapRect == null) return;

            if (map == null)
            {
                if (_title != null) _title.text = "OVERLAND MAP — no world generated yet";
                if (_footer != null) _footer.text = "[M] close";
                if (_mapImage != null) _mapImage.enabled = false;
                HideMarkers();
                return;
            }

            EnsureMapTexture(map);
            EnsureSettlementMarkers(map);
            PositionPlayerMarker(playerTile, map.Width, map.Height);

            int settlements = map.Settlements.Count;
            // Distinct REGIONS (administrative groupings of the tile grid) so the map reports the SAME region +
            // settlement counts as the char-creation reveal — one authoritative world, not the raw 16x16 tile
            // dimensions which read as a different "region" number than the world the history simulated.
            var regionIds = new System.Collections.Generic.HashSet<EmberCrpg.Domain.Worldgen.RegionId>();
            for (int i = 0; i < map.Tiles.Count; i++) regionIds.Add(map.Tiles[i].RegionId);
            if (_title != null)
                _title.text = $"OVERLAND MAP - {regionIds.Count} regions - {settlements} settlements - 409,600 km2";
            if (_footer != null)
            {
                string where = string.IsNullOrEmpty(locationName)
                    ? $"region ({playerTile.X}, {playerTile.Y})"
                    : $"{locationName}  ·  region ({playerTile.X}, {playerTile.Y})";
                _footer.text = $"You are in {where}      [M] close";
            }
        }

        private void OnDestroy()
        {
            if (_mapTexture != null) Destroy(_mapTexture);
            DestroySprite(_dotSprite);
            DestroySprite(_ringSprite);
        }

        // ---- map image / markers ------------------------------------------------------------------

        private void EnsureMapTexture(OverlandMap map)
        {
            ulong mapKey = OverlandMapImageSampler.ComputeCacheKey(map);
            if (_hasCachedMap && _cachedMapKey == mapKey)
            {
                if (_mapImage != null) _mapImage.enabled = true;
                return;
            }

            var image = OverlandMapImageSampler.Sample(map);
            if (_mapTexture == null || _mapTexture.width != image.Width || _mapTexture.height != image.Height)
            {
                if (_mapTexture != null) Destroy(_mapTexture);
                _mapTexture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, mipChain: false)
                {
                    name = "OverlandMap_FineImage",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            _mapTexture.LoadRawTextureData(image.RgbaBytes);
            _mapTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            _mapImage.texture = _mapTexture;
            _mapImage.enabled = true;
            _cachedMapKey = image.CacheKey;
            _hasCachedMap = true;

            Debug.Log(
                $"[OverlandMap] built {image.Width}x{image.Height} fine map image " +
                $"({map.Width}x{map.Height} tiles, {map.Settlements.Count} settlements)");
        }

        private void EnsureSettlementMarkers(OverlandMap map)
        {
            while (_settlementMarkers.Count < map.Settlements.Count)
                _settlementMarkers.Add(CreateMarker("Settlement", _dotSprite, new Color(1f, 0.78f, 0.34f), SettlementMarkerPixels));

            for (int i = 0; i < _settlementMarkers.Count; i++)
            {
                bool active = i < map.Settlements.Count;
                _settlementMarkers[i].gameObject.SetActive(active);
                if (!active)
                    continue;
                PositionMarker((RectTransform)_settlementMarkers[i].transform, map.Settlements[i].TilePosition, map.Width, map.Height);
            }

            if (_playerMarker != null)
                _playerMarker.transform.SetAsLastSibling();
        }

        private void PositionPlayerMarker(GridPosition playerTile, int mapWidth, int mapHeight)
        {
            if (_playerMarker == null)
                return;

            _playerMarker.gameObject.SetActive(true);
            PositionMarker((RectTransform)_playerMarker.transform, playerTile, mapWidth, mapHeight);
        }

        private void HideMarkers()
        {
            for (int i = 0; i < _settlementMarkers.Count; i++)
                _settlementMarkers[i].gameObject.SetActive(false);
            if (_playerMarker != null)
                _playerMarker.gameObject.SetActive(false);
        }

        private Image CreateMarker(string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapRect, worldPositionStays: false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void PositionMarker(RectTransform marker, GridPosition tile, int mapWidth, int mapHeight)
        {
            int x = Mathf.Clamp(tile.X, 0, mapWidth - 1);
            int y = Mathf.Clamp(tile.Y, 0, mapHeight - 1);
            float px = (((x + 0.5f) / mapWidth) - 0.5f) * MapPixels;
            float py = (((y + 0.5f) / mapHeight) - 0.5f) * MapPixels;
            marker.anchoredPosition = new Vector2(px, py);
        }

        private static Sprite BuildMarkerSprite(string name, int size, bool ring)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = name + "_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float outer = size * 0.45f;
            float inner = ring ? size * 0.27f : -1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    bool visible = distance <= outer && (!ring || distance >= inner);
                    pixels[(y * size) + x] = visible ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
                return;
            var texture = sprite.texture;
            Destroy(sprite);
            if (texture != null)
                Destroy(texture);
        }

        // ---- chrome -------------------------------------------------------------------------------

        // Self-build the full-screen dimmer + centered title / map / footer. Mirrors the prefab-free
        // construction EventLogHudPanel uses so the panel drops into any scene's HUD canvas.
        private void BuildChrome()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var dim = gameObject.GetComponent<Image>();
            if (dim == null) dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.02f, 0.03f, 0.92f);
            dim.raycastTarget = true; // swallow clicks behind the map

            _dotSprite = BuildMarkerSprite("OverlandMap_Dot", 16, ring: false);
            _ringSprite = BuildMarkerSprite("OverlandMap_Ring", 24, ring: true);

            _title = NewText("Title", root, 22f, new Color(0.96f, 0.84f, 0.45f), TextAlignmentOptions.Center);
            var titleRect = (RectTransform)_title.transform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);
            titleRect.sizeDelta = new Vector2(MapPixels + 80f, 40f);
            _title.text = "OVERLAND MAP";

            var mapGo = new GameObject("FineMap", typeof(RectTransform), typeof(RawImage));
            mapGo.transform.SetParent(root, worldPositionStays: false);
            _mapRect = (RectTransform)mapGo.transform;
            _mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            _mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapRect.sizeDelta = new Vector2(MapPixels, MapPixels);
            _mapRect.anchoredPosition = new Vector2(0f, 8f);
            _mapImage = mapGo.GetComponent<RawImage>();
            _mapImage.raycastTarget = false;

            _playerMarker = CreateMarker("PlayerMarker", _ringSprite, Color.white, PlayerMarkerPixels);
            _playerMarker.gameObject.SetActive(false);

            _footer = NewText("Footer", root, 16f, new Color(0.86f, 0.88f, 0.82f), TextAlignmentOptions.Center);
            var footRect = (RectTransform)_footer.transform;
            footRect.anchorMin = new Vector2(0.5f, 0f);
            footRect.anchorMax = new Vector2(0.5f, 0f);
            footRect.pivot = new Vector2(0.5f, 0f);
            footRect.anchoredPosition = new Vector2(0f, 30f);
            footRect.sizeDelta = new Vector2(MapPixels + 80f, 30f);
            _footer.text = "[M] close";
        }

        private static TMP_Text NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}

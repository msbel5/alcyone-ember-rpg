using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Full-screen overland map overlay, toggled with M. Makes the generated open world VISIBLE: a
    /// biome-coloured 16x16 region grid with settlement tiles lit gold and the player's region marked
    /// white, plus a header (size / settlement count / km^2) and a "you are in &lt;town&gt;" footer.
    ///
    /// Pattern: pure tick-driven view (same family as <see cref="EventLogHudPanel"/>). The host pushes
    /// the deterministic <see cref="OverlandMap"/> + the player's region tile via <see cref="Render"/>;
    /// the panel owns only presentation. It self-builds its uGUI tree in Awake so it needs no prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverlandMapPanel : MonoBehaviour
    {
        private const float MapPixels = 660f;   // square region-grid area
        private const float CellSpacing = 2f;

        private TMP_Text _title;
        private TMP_Text _footer;
        private RectTransform _grid;
        private GridLayoutGroup _layout;
        private Image[] _cells;                  // one per region, indexed (y * width) + x
        private int _builtWidth, _builtHeight;

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
            if (_grid == null) return;

            if (map == null)
            {
                if (_title != null) _title.text = "OVERLAND MAP — no world generated yet";
                if (_footer != null) _footer.text = "[M] close";
                return;
            }

            EnsureCells(map.Width, map.Height);

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = _cells[(y * map.Width) + x];
                    if (cell == null) continue;

                    var tile = map.TileAt(x, y);
                    Color color = BiomeColor(tile.Biome);
                    if (tile.SettlementIds != null && tile.SettlementIds.Count > 0)
                        color = SettlementGlow(color);
                    if (x == playerTile.X && y == playerTile.Y)
                        color = Color.white; // the player's home region stands out
                    cell.color = color;
                }
            }

            int settlements = map.Settlements.Count;
            if (_title != null)
                _title.text = $"OVERLAND MAP — {map.Width}x{map.Height} regions · {settlements} settlements · 409,600 km²";
            if (_footer != null)
            {
                string where = string.IsNullOrEmpty(locationName)
                    ? $"region ({playerTile.X}, {playerTile.Y})"
                    : $"{locationName}  ·  region ({playerTile.X}, {playerTile.Y})";
                _footer.text = $"You are in {where}      [M] close";
            }
        }

        // ---- region-grid construction -------------------------------------------------------------

        // Build (or rebuild) one cell Image per region the first time we see a given map size.
        private void EnsureCells(int width, int height)
        {
            if (_cells != null && _builtWidth == width && _builtHeight == height)
                return;

            for (int i = _grid.childCount - 1; i >= 0; i--)
                Destroy(_grid.GetChild(i).gameObject);

            float usable = MapPixels - (CellSpacing * (Mathf.Max(width, height) - 1));
            float cell = usable / Mathf.Max(width, height);
            _layout.cellSize = new Vector2(cell, cell);
            _layout.spacing = new Vector2(CellSpacing, CellSpacing);
            _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _layout.constraintCount = width;

            _cells = new Image[width * height];
            for (int i = 0; i < _cells.Length; i++)
            {
                var go = new GameObject("Region", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_grid, worldPositionStays: false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _cells[i] = img;
            }

            _builtWidth = width;
            _builtHeight = height;
            Debug.Log($"[OverlandMap] built {width}x{height} region grid ({_cells.Length} cells)");
        }

        // ---- biome / settlement palette -----------------------------------------------------------

        private static Color BiomeColor(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Plains:   return new Color(0.42f, 0.52f, 0.28f);
                case BiomeKind.Forest:   return new Color(0.18f, 0.36f, 0.20f);
                case BiomeKind.Mountain: return new Color(0.48f, 0.47f, 0.52f);
                case BiomeKind.Coast:    return new Color(0.26f, 0.46f, 0.62f);
                case BiomeKind.Swamp:    return new Color(0.27f, 0.34f, 0.26f);
                case BiomeKind.Desert:   return new Color(0.74f, 0.66f, 0.42f);
                case BiomeKind.Tundra:   return new Color(0.68f, 0.72f, 0.75f);
                case BiomeKind.Ash:      return new Color(0.34f, 0.24f, 0.24f);
                default:                 return new Color(0.30f, 0.30f, 0.30f);
            }
        }

        // Pull a settlement tile toward ember-gold so towns read at a glance over the biome base.
        private static Color SettlementGlow(Color biome)
        {
            return Color.Lerp(biome, new Color(1f, 0.78f, 0.34f), 0.72f);
        }

        // ---- chrome -------------------------------------------------------------------------------

        // Self-build the full-screen dimmer + centered title / grid / footer. Mirrors the prefab-free
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

            _title = NewText("Title", root, 22f, new Color(0.96f, 0.84f, 0.45f), TextAlignmentOptions.Center);
            var titleRect = (RectTransform)_title.transform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);
            titleRect.sizeDelta = new Vector2(MapPixels + 80f, 40f);
            _title.text = "OVERLAND MAP";

            var gridGo = new GameObject("RegionGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(root, worldPositionStays: false);
            _grid = (RectTransform)gridGo.transform;
            _grid.anchorMin = new Vector2(0.5f, 0.5f);
            _grid.anchorMax = new Vector2(0.5f, 0.5f);
            _grid.pivot = new Vector2(0.5f, 0.5f);
            _grid.sizeDelta = new Vector2(MapPixels, MapPixels);
            _grid.anchoredPosition = new Vector2(0f, 8f);
            _layout = gridGo.GetComponent<GridLayoutGroup>();
            _layout.startCorner = GridLayoutGroup.Corner.LowerLeft; // tile (0,0) bottom-left, like the world
            _layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _layout.childAlignment = TextAnchor.MiddleCenter;

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

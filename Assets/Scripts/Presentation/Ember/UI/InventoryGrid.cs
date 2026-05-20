using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// 6-column grid that displays inventory slots. Each cell holds a sprite + a count
    /// label. Sprites are resolved by name through a registry so this panel does not
    /// hold a hard reference to the asset database at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryGrid : MonoBehaviour
    {
        public IInventorySource Source { get; set; }
        public ISpriteByName SpriteLookup { get; set; }

        [SerializeField] private int _columns = 6;
        [SerializeField] private int _maxRows = 6;
        [SerializeField] private float _cellSize = 64f;

        private readonly List<InventoryCell> _cells = new List<InventoryCell>();
        private GridLayoutGroup _layout;

        private void Awake()
        {
            _layout = GetComponentInChildren<GridLayoutGroup>(includeInactive: true);
            if (_layout == null) _layout = BuildGrid();
        }

        private void Start() { RebuildCells(); }

        private void Update()
        {
            if (Source == null) return;
            var items = Source.GetSlots();
            if (items == null) return;
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (i < items.Count)
                {
                    var slot = items[i];
                    cell.Image.sprite = SpriteLookup != null ? SpriteLookup.GetSprite(slot.IconName) : null;
                    cell.Image.color = cell.Image.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.1f);
                    cell.Count.text = slot.Count > 1 ? slot.Count.ToString() : string.Empty;
                }
                else
                {
                    cell.Image.sprite = null;
                    cell.Image.color = new Color(1f, 1f, 1f, 0.1f);
                    cell.Count.text = string.Empty;
                }
            }
        }

        private GridLayoutGroup BuildGrid()
        {
            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(_cellSize, _cellSize);
            grid.spacing = new Vector2(4f, 4f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = _columns;
            return grid;
        }

        private void RebuildCells()
        {
            _cells.Clear();
            var capacity = _columns * _maxRows;
            for (int i = 0; i < capacity; i++)
                _cells.Add(InventoryCell.Build(_layout.transform));
        }

        private sealed class InventoryCell
        {
            public Image Image;
            public Text Count;

            public static InventoryCell Build(Transform parent)
            {
                var go = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, worldPositionStays: false);
                var image = go.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.1f);

                var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
                countGo.transform.SetParent(go.transform, worldPositionStays: false);
                var rt = countGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.4f, 0f);
                rt.anchorMax = new Vector2(1f, 0.35f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var text = countGo.GetComponent<Text>();
                text.alignment = TextAnchor.LowerRight;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.color = new Color(0.95f, 0.95f, 0.88f);

                return new InventoryCell { Image = image, Count = text };
            }
        }
    }

    public readonly struct InventorySlot
    {
        public readonly string IconName;
        public readonly int Count;
        public InventorySlot(string iconName, int count) { IconName = iconName; Count = count; }
    }

    public interface IInventorySource
    {
        IReadOnlyList<InventorySlot> GetSlots();
    }

    public interface ISpriteByName
    {
        Sprite GetSprite(string name);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// 6-column grid that displays inventory slots. Each cell holds a sprite + a count
    /// label. Sprites are resolved by name through a registry so this panel does not
    /// hold a hard reference to the asset database at runtime.
    /// Now with TMP support and parchment styling.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryGrid : MonoBehaviour
    {
        public IInventorySource Source { get; set; }
        public ISpriteByName SpriteLookup { get; set; }

        [Header("Assets")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        [Header("Layout")]
        [SerializeField] private int _columns = 6;
        [SerializeField] private int _maxRows = 6;
        [SerializeField] private float _cellSize = 64f;

        private readonly List<InventoryCell> _cells = new List<InventoryCell>();
        private GridLayoutGroup _layout;
        private CanvasGroup _canvasGroup;
        private Image _background;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _background = GetComponent<Image>();
            if (_panelFrame != null && _background != null)
            {
                _background.sprite = _panelFrame;
                _background.type = Image.Type.Sliced;
            }

            _layout = GetComponentInChildren<GridLayoutGroup>(includeInactive: true);
            if (_layout == null) _layout = BuildGrid();
        }

        private void Start() { RebuildCells(); }

        private void OnEnable()
        {
            StartCoroutine(UiAnimationHelper.AnimateOpen(_canvasGroup, GetComponent<RectTransform>()));
        }

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

        public void Toggle()
        {
            if (gameObject.activeSelf) StartCoroutine(CloseRoutine());
            else gameObject.SetActive(true);
        }

        private System.Collections.IEnumerator CloseRoutine()
        {
            yield return UiAnimationHelper.AnimateClose(_canvasGroup, GetComponent<RectTransform>());
            gameObject.SetActive(false);
        }

        private GridLayoutGroup BuildGrid()
        {
            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(transform, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 12f);
            rt.offsetMax = new Vector2(-12f, -12f);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(_cellSize, _cellSize);
            grid.spacing = new Vector2(6f, 6f);
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
                _cells.Add(InventoryCell.Build(_layout.transform, _font));
        }

        private sealed class InventoryCell
        {
            public Image Image;
            public TMP_Text Count;

            public static InventoryCell Build(Transform parent, TMP_FontAsset font)
            {
                var go = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, worldPositionStays: false);
                var image = go.GetComponent<Image>();
                image.color = new Color(0.2f, 0.15f, 0.1f, 0.2f); // Darker base for slot

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, worldPositionStays: false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(4, 4);
                iconRt.offsetMax = new Vector2(-4, -4);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.color = new Color(1, 1, 1, 0.1f);

                var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
                countGo.transform.SetParent(go.transform, worldPositionStays: false);
                var rt = countGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.4f, 0f);
                rt.anchorMax = new Vector2(1.2f, 0.35f); // Move out slightly for better visibility
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var text = countGo.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.BottomRight;
                if (font != null) text.font = font;
                text.fontSize = 18; // Bigger count
                text.color = new Color(0.15f, 0.1f, 0.05f);

                return new InventoryCell { Image = iconImg, Count = text };
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

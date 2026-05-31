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
            else if (_background != null)
            {
                // BUG-6: no frame asset assigned — give the panel a solid dark backing so the grid reads
                // as a deliberate inventory window instead of the faint translucent ghost it used to be.
                _background.color = new Color(0.06f, 0.05f, 0.04f, 0.94f);
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
                // BUG-6: a clearly-visible slot — opaque dark base + a gold hairline border — so the grid
                // reads as real inventory cells instead of barely-there translucent squares.
                var go = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Outline));
                go.transform.SetParent(parent, worldPositionStays: false);
                var image = go.GetComponent<Image>();
                image.color = new Color(0.14f, 0.11f, 0.08f, 0.96f);
                var slotOutline = go.GetComponent<Outline>();
                slotOutline.effectColor = new Color(0.55f, 0.42f, 0.18f, 0.85f);
                slotOutline.effectDistance = new Vector2(1.5f, 1.5f);

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, worldPositionStays: false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(5, 5);
                iconRt.offsetMax = new Vector2(-5, -5);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.color = new Color(1, 1, 1, 0f); // empty until Update assigns a sprite

                var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
                countGo.transform.SetParent(go.transform, worldPositionStays: false);
                var rt = countGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.35f, 0.02f);
                rt.anchorMax = new Vector2(0.96f, 0.42f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var text = countGo.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.BottomRight;
                if (font != null) text.font = font;
                text.fontSize = 18;
                text.fontStyle = FontStyles.Bold;
                text.color = new Color(0.96f, 0.86f, 0.55f); // BUG-6: light gold, readable on the dark slot
                var countOutline = countGo.AddComponent<Outline>();
                countOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                countOutline.effectDistance = new Vector2(1f, 1f);

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

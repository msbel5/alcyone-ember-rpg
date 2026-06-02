using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: composite nav host. Why: one shell renders many discovered sections without naming them.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed partial class OptionsScreen : MonoBehaviour
    {
        private readonly List<UnityEngine.UI.Image> _navFills = new List<UnityEngine.UI.Image>();
        private IReadOnlyList<IOptionsSection> _sections;
        private RectTransform _navMount;
        private RectTransform _contentMount;
        private CanvasGroup _canvasGroup;
        private PauseMenu _owner;
        private TMP_FontAsset _font;
        private Sprite _panelFrame;
        private bool _isBuilt;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            Stretch((RectTransform)transform);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // Why: PauseMenu creates the host once, then injects the shared UI assets.
        public void Initialize(TMP_FontAsset font, Sprite panelFrame)
        {
            _font = font;
            _panelFrame = panelFrame;
        }

        // Why: first open discovers sections and builds the shell only once per runtime.
        public void Open(PauseMenu owner)
        {
            _owner = owner;
            if (!_isBuilt) BuildOnce();
            SetOwnerVisible(false);
            StopAllCoroutines();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            StartCoroutine(UiAnimationHelper.AnimateOpen(_canvasGroup, (RectTransform)transform));
        }

        private void Update()
        {
            if (!_canvasGroup.interactable) return;
            if (EmberInput.PauseDown || Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // Why: back/Esc should return to the pause menu without unpausing the game.
        public void Close()
        {
            if (!_canvasGroup.interactable) return;
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        // Why: shell construction is centralized so future sections only implement the contract.
        private void BuildOnce()
        {
            _isBuilt = true;
            _sections = OptionsSectionRegistry.Discover();
            var frame = Panel("Frame", transform, new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.86f));
            var title = Label(frame, "OPTIONS", 26, TextAlignmentOptions.Left, Gold);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -56f), new Vector2(-132f, -16f));
            var back = Button(frame, "BACK", Close, out _).GetComponent<RectTransform>();
            Place(back, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-112f, -54f), new Vector2(-24f, -16f));
            var body = Box("Body", frame, new Vector2(18f, 18f), new Vector2(-18f, -72f));
            _navMount = Box("Nav", body, Vector2.zero, Vector2.zero);
            Place(_navMount, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, 12f), new Vector2(224f, -12f));
            _contentMount = Box("Content", body, new Vector2(244f, 12f), new Vector2(-12f, -12f));
            LayoutNav();
            BuildSections();
        }

        // Why: the left rail needs a deterministic stack of discovered section tabs.
        private void LayoutNav()
        {
            var layout = _navMount.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        // Why: each discovered section becomes one tab without changing the host.
        private void BuildSections()
        {
            if (_sections.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            for (int i = 0; i < _sections.Count; i++)
            {
                int index = i;
                Button(_navMount, _sections[i].Title, () => ShowSection(index), out var fill);
                _navFills.Add(fill);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_navMount);
            ShowSection(0);
        }

        // Why: selecting a tab replaces only the content mount and highlights the active button.
        private void ShowSection(int index)
        {
            Clear(_contentMount);
            for (int i = 0; i < _navFills.Count; i++) _navFills[i].color = i == index ? PanelBrownHi : PanelBrown;
            _sections[index].Build(_contentMount);
        }

        // Why: the shell still needs a usable empty state before concrete section slices land.
        private void ShowEmptyState()
        {
            var label = Label(_contentMount, "(no option sections)", 18, TextAlignmentOptions.Center, ParchmentDim);
            Stretch(label.rectTransform);
        }

        // Why: destroy-and-detach avoids stale section widgets lingering in the content mount.
        private static void Clear(Transform mount)
        {
            while (mount.childCount > 0)
            {
                var child = mount.GetChild(0);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }
        }

        // Why: options should fully own input while open and hand it back cleanly on close.
        private IEnumerator CloseRoutine()
        {
            yield return UiAnimationHelper.AnimateClose(_canvasGroup, (RectTransform)transform);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            SetOwnerVisible(true);
        }

        // Why: the pause menu stays paused underneath, but should not read Esc while options are open.
        private void SetOwnerVisible(bool visible)
        {
            if (_owner == null) return;
            if (_owner.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
            _owner.enabled = visible;
        }
    }
}

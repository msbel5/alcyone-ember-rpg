using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Bootstrap;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.UI
{
/// <summary>
    /// Fallout 1 / Hitchhiker-style dialog scaffold. Shows the NPC line on top and a
    /// list of player Ask-About topics underneath. Drives the simulation by handing the
    /// topic key back to <see cref="IDialogSource"/>; the simulation decides the reply.
    /// Now with TMP support, typewriter effects, and character portraits.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogBoxPanel : MonoBehaviour
    {
        public IDialogSource Source { get; set; }

        [Header("Assets")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        [Header("Speeds")]
        [SerializeField] private float _charsPerSecond = 45f;

        private TMP_Text _npcLineLabel;
        private RectTransform _topicsRoot;
        private Image _portraitImage;
        private Image _frameImage;
        private CanvasGroup _canvasGroup;
        private readonly List<TMP_Text> _topicLabels = new List<TMP_Text>();
        
        private string _fullLineText;
        private string _displayedLineText;
        private float _typewriterElapsed;
        private bool _isTypewriting;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _frameImage = GetComponent<Image>();

            // DLG-SIZE-01 — single source of truth for the dialog box footprint.
            // The internal layout (BackingFill, frame, portrait, line, topics) is all
            // anchored RELATIVE to this RectTransform, so the on-screen dialog size is
            // exactly whatever rect this transform is given. Scenes authored wildly
            // different rects (TavernDialog 0.1→0.9, OracleShrine 0.2→0.8, TavernFlavour
            // 0.05→0.95) and EmberWorldHost.EnsureDialogBoxPanel stretched the runtime
            // fallback FULL-SCREEN — producing full-screen dialogs in some scenes, tiny
            // in others. Mirror EmberHud, which already self-pins its own RectTransform
            // in Awake: enforce ONE canonical bottom-centered CRPG dialog box here so the
            // result is identical in every scene regardless of how the panel was authored
            // or runtime-ensured. Bottom-centered, ~72% wide, lower ~35% of the screen.
            EnforceDialogBoxRect();

            // T-Dialog-AskAbout slice 1 — readability rebuild. The legacy panel:
            //   - used charcoal-brown #26190D text on the dark void background -> invisible
            //   - had a transparent panel background (scene-authored Image left empty)
            //   - portrait was 50% alpha white, washed-out
            // Fix at the source: add a solid dark backing + gold hairline border + parchment
            // text colors + portrait frame, all built procedurally so no scene reauthoring is
            // needed. The PRD ask-about modal (deterministic topics from conversation_state +
            // F4 hotkey + auto-close on dialog end) is slice 2; slice 1 is purely visual.

            // Solid dark backing — first child, renders behind everything else.
            BuildBackingFill(transform, sibling: 0);
            // Gold hairline frame — second child, hollow look on top of the fill.
            BuildHairlineFrame(transform, sibling: 1);

            if (_panelFrame != null && _frameImage != null)
            {
                _frameImage.sprite = _panelFrame;
                _frameImage.type = Image.Type.Sliced;
            }

            _portraitImage = BuildPortrait(transform);
            _npcLineLabel = BuildLine(transform, anchorMinX: 0.2f, anchorMaxX: 0.95f, anchorMinY: 0.55f, anchorMaxY: 0.95f, alignTop: true, fontSize: 22);
            _topicsRoot = BuildPanelRoot(transform, anchorMinX: 0.2f, anchorMaxX: 0.95f, anchorMinY: 0.05f, anchorMaxY: 0.5f);

            RebuildTopicLabels();
        }

        // DLG-SIZE-01 — canonical dialog-box footprint. Bottom-centered CRPG box:
        // anchors span the lower band of the screen, offsets zeroed so the box scales
        // cleanly with resolution. This OVERRIDES whatever rect the scene recipe or the
        // runtime EnsureDialogBoxPanel handed us, so the dialog is pixel-for-pixel the
        // same everywhere. Tuned to read as a proper dialog box (not full-screen, not a
        // tiny chip): ~72% screen width, occupying ~5%–40% up from the bottom edge.
        private static readonly Vector2 DialogAnchorMin = new Vector2(0.14f, 0.05f);
        private static readonly Vector2 DialogAnchorMax = new Vector2(0.86f, 0.40f);

        private void EnforceDialogBoxRect()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = DialogAnchorMin;
            rt.anchorMax = DialogAnchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        // Solid void-cool fill at 92% alpha so the dialog text always has a readable surface.
        private static void BuildBackingFill(Transform parent, int sibling)
        {
            var go = new GameObject("BackingFill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetSiblingIndex(sibling);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.051f, 0.051f, 0.071f, 0.92f); // #0D0D12 @ 92%
            img.raycastTarget = false;
        }

        // Gold hairline border (one thin Image per edge so we don't need a 9-slice sprite).
        private static void BuildHairlineFrame(Transform parent, int sibling)
        {
            var frame = new GameObject("HairlineFrame", typeof(RectTransform));
            frame.transform.SetParent(parent, worldPositionStays: false);
            frame.transform.SetSiblingIndex(sibling);
            var frt = (RectTransform)frame.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var gold = new Color(0.949f, 0.859f, 0.620f, 0.30f);
            AddEdge(frt, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f,  0f), gold);
            AddEdge(frt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f,  0f), new Vector2(0f,  2f), gold);
            AddEdge(frt, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f,  0f), new Vector2(2f,  0f), gold);
            AddEdge(frt, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f,  0f), gold);
        }

        private static void AddEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void OnEnable()
        {
            StartCoroutine(UiAnimationHelper.AnimateOpen(_canvasGroup, GetComponent<RectTransform>()));
            _typewriterElapsed = 0f;
            _displayedLineText = string.Empty;
            _isTypewriting = true;
        }

        private void Update()
        {
            if (Source == null) return;

            // Mami fix: show animated "thinking …" while the LLM async call
            // is in flight; the dot count cycles 1→2→3 every 0.3s.
            string targetLine;
            if (Source.IsThinking)
            {
                int dots = ((int)(Time.unscaledTime * 3.0f)) % 3 + 1;
                targetLine = "Thinking" + new string('.', dots);
            }
            else
            {
                targetLine = Source.GetCurrentLine();
            }
            if (targetLine != _fullLineText)
            {
                _fullLineText = targetLine;
                _displayedLineText = string.Empty;
                _typewriterElapsed = 0f;
                _isTypewriting = true;
            }

            if (_isTypewriting)
            {
                _typewriterElapsed += Time.unscaledDeltaTime;
                int charsToShow = Mathf.FloorToInt(_typewriterElapsed * _charsPerSecond);
                if (charsToShow >= _fullLineText.Length)
                {
                    _displayedLineText = _fullLineText;
                    _isTypewriting = false;
                }
                else
                {
                    _displayedLineText = _fullLineText.Substring(0, charsToShow);
                }

                if (EmberInput.AttackClick)
                {
                    _displayedLineText = _fullLineText;
                    _isTypewriting = false;
                }
            }

            _npcLineLabel.text = _displayedLineText;

            // Handle portrait
            // Audit (eighth pass D-P2): previously empty — wire portrait sprite
            // lookup through any ISpriteByName-capable source (typically
            // EmberWorldHost), then assign to a child Image named "Portrait"
            // (created lazily by BuildPortrait above).
            if (_portraitImage != null && Source is IDialogSourcePortrait portraitSource)
            {
                var portraitName = portraitSource.GetPortraitName();
                if (!string.IsNullOrEmpty(portraitName))
                {
                    Sprite portrait = null;
                    if (Source is EmberCrpg.Presentation.Ember.UI.ISpriteByName lookup)
                        portrait = lookup.GetSprite(portraitName);
                    if (portrait == null)
                    {
                        var host = EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost
                            .GetSpriteFromHost(portraitName);
                        portrait = host;
                    }
                    if (portrait != null)
                    {
                        _portraitImage.sprite = portrait;
                        _portraitImage.color = Color.white;
                    }
                }
            }

            var topics = Source.GetTopics();
            for (int i = 0; i < _topicLabels.Count; i++)
            {
                if (i < topics.Count)
                {
                    _topicLabels[i].text = $"{i + 1}. Ask about <color=#f1c40f>{topics[i]}</color>";
                }
                else
                {
                    _topicLabels[i].text = string.Empty;
                }
            }

            for (int i = 0; i < 9 && i < topics.Count; i++)
            {
                if (EmberInput.NumberKeyDown(i + 1))
                {
                    Source.SelectTopic(topics[i]);
                }
            }

            if (EmberInput.PauseDown)
            {
                Close();
            }
        }

        public void Close()
        {
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            yield return UiAnimationHelper.AnimateClose(_canvasGroup, GetComponent<RectTransform>());
            Source = null;
            gameObject.SetActive(false);
            
            if (!EmberWorldHost.IsModalOpen())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void RebuildTopicLabels()
        {
            foreach (var label in _topicLabels) if(label != null) Destroy(label.gameObject);
            _topicLabels.Clear();
            for (int i = 0; i < 6; i++)
            {
                var label = BuildLine(_topicsRoot, anchorMinX: 0f, anchorMaxX: 1f, anchorMinY: 1f - (i + 1) * 0.16f, anchorMaxY: 1f - i * 0.16f, alignTop: true, fontSize: 16);
                _topicLabels.Add(label);
            }
        }

        private TMP_Text BuildLine(Transform parent, float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY, bool alignTop, int fontSize)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.offsetMin = new Vector2(60f, 40f);
            rt.offsetMax = new Vector2(-60f, -40f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = alignTop ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;
            if (_font != null) text.font = _font;
            text.fontSize = fontSize;
            // T-Dialog-AskAbout slice 1 — parchment on the void backing for legibility.
            // Legacy "Deep Charcoal Brown" (0.15, 0.1, 0.05) was invisible on the dark world.
            text.color = new Color(0.949f, 0.859f, 0.620f, 1f); // #F2DB9E parchment
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(0, 0, 0, 220);
            return text;
        }

        private static Image BuildPortrait(Transform parent)
        {
            // Portrait container — panel-brown frame so the gray placeholder reads as a deliberate
            // bezel rather than a missing texture. The portrait itself sits inside, full alpha so
            // any real sprite loaded later shows at full strength.
            var frame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, worldPositionStays: false);
            var frt = (RectTransform)frame.transform;
            frt.anchorMin = new Vector2(0.02f, 0.55f);
            frt.anchorMax = new Vector2(0.18f, 0.95f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = new Color(0.180f, 0.140f, 0.090f, 0.92f); // #2E2417 panel-brown @ 92%
            frameImg.raycastTarget = false;

            // Gold hairline around the frame.
            BuildHairlineFrame(frt, sibling: 1);

            // Inner portrait image — sits inside the frame with a 4px bezel inset.
            var inner = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(frt, worldPositionStays: false);
            var rt = (RectTransform)inner.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            var img = inner.GetComponent<Image>();
            // Start the inner image fully transparent so the panel-brown frame reads as the
            // placeholder. Update() flips to Color.white when it actually assigns a sprite —
            // see the "img.color = Color.white;" line in the portrait-assignment block above.
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private static RectTransform BuildPanelRoot(Transform parent, float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY)
        {
            var go = new GameObject("Topics", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.offsetMin = new Vector2(60f, 40f);
            rt.offsetMax = new Vector2(-60f, -40f);
            return rt;
        }
    }

    // Audit (eighth pass B-P2): IDialogSource and IDialogSourcePortrait
    // moved to Assets/Scripts/Presentation/Ember/Adapters/IDialogSource.cs
    // in namespace EmberCrpg.Presentation.Ember.Adapters.
}


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
    public sealed partial class DialogBoxPanel : MonoBehaviour
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
        private Sprite _portraitFallbackSprite;

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
            if (_portraitImage != null)
            {
                var portraitName = DialogPortraitKey.FromSource(Source);
                var portrait = ResolvePortraitSprite(portraitName)
                    ?? ResolvePortraitSprite(DialogPortraitKey.Default)
                    ?? ResolveFallbackPortrait();

                if (portrait != null)
                {
                    _portraitImage.sprite = portrait;
                    _portraitImage.color = Color.white;
                    _portraitImage.preserveAspect = true;
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

        private Sprite ResolvePortraitSprite(string portraitName)
        {
            if (string.IsNullOrWhiteSpace(portraitName)) return null;
            if (!DialogPortraitKey.IsPortraitKey(portraitName)) return null;

            if (Source is EmberCrpg.Presentation.Ember.UI.ISpriteByName lookup)
            {
                var sprite = lookup.GetSprite(portraitName);
                if (sprite != null) return sprite;
            }

            return EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost.GetSpriteFromHost(portraitName);
        }

        private Sprite ResolveFallbackPortrait()
        {
            if (_portraitFallbackSprite != null) return _portraitFallbackSprite;

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color32[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    byte c = (byte)(x > 8 && x < 24 && y > 6 && y < 28 ? 190 : 120);
                    pixels[y * 32 + x] = new Color32(c, (byte)(c - 25), (byte)(c - 45), 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            _portraitFallbackSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _portraitFallbackSprite;
        }
    }

    // Audit (eighth pass B-P2): IDialogSource and IDialogSourcePortrait
    // moved to Assets/Scripts/Presentation/Ember/Adapters/IDialogSource.cs
    // in namespace EmberCrpg.Presentation.Ember.Adapters.
}

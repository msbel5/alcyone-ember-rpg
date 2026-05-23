using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Bootstrap;

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
            if (_panelFrame != null && _frameImage != null)
            {
                _frameImage.sprite = _panelFrame;
                _frameImage.type = Image.Type.Sliced;
            }

            _portraitImage = BuildPortrait(transform);
            _npcLineLabel = BuildLine(transform, anchorMinX: 0.2f, anchorMaxX: 0.95f, anchorMinY: 0.55f, anchorMaxY: 0.95f, alignTop: true, fontSize: 18);
            _topicsRoot = BuildPanelRoot(transform, anchorMinX: 0.2f, anchorMaxX: 0.95f, anchorMinY: 0.05f, anchorMaxY: 0.5f);
            
            RebuildTopicLabels();
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

            string targetLine = Source.GetCurrentLine();
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

                if (Input.GetMouseButtonDown(0))
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
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Source.SelectTopic(topics[i]);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
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
            text.color = new Color(0.15f, 0.1f, 0.05f); // Deep Charcoal Brown
return text;
        }

        private static Image BuildPortrait(Transform parent)
        {
            var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.55f);
            rt.anchorMax = new Vector2(0.18f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.5f); // Placeholder alpha
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


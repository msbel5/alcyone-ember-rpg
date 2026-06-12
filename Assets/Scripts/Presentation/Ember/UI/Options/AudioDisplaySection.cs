using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>
    /// F32: AUDIO &amp; DISPLAY — music/sfx volume and mouse sensitivity sliders (live, persisted
    /// via RuntimePlayerSettings) and a resolution cycler + apply. Auto-discovered by the
    /// options registry like every section.
    /// </summary>
    public sealed class AudioDisplaySection : IOptionsSection
    {
        private static readonly Color Parchment = new Color(0.949f, 0.859f, 0.620f, 1f);
        private static readonly Color ParchmentDim = new Color(0.902f, 0.851f, 0.702f, 1f);
        private static readonly Color PanelBrown = new Color(0.18f, 0.14f, 0.09f, 0.92f);
        private static readonly Color Gold = new Color(1f, 0.851f, 0.298f, 1f);

        public string Title => "Audio & Display";
        public int Order => 20;

        private TMP_FontAsset _font;
        private int _resolutionIndex = -1;
        private TextMeshProUGUI _resolutionLabel;

        public void Build(Transform contentMount)
        {
            var donorText = contentMount.GetComponentInParent<TMP_Text>()
                ?? Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            _font = donorText != null ? donorText.font : null;

            float y = -24f;
            y = AddSlider(contentMount, "Music volume", y, 0f, 1f, RuntimePlayerSettings.MusicVolume,
                v => { RuntimePlayerSettings.MusicVolume = v; RuntimePlayerSettings.Save(); });
            y = AddSlider(contentMount, "SFX volume", y, 0f, 1f, RuntimePlayerSettings.SfxVolume,
                v => { RuntimePlayerSettings.SfxVolume = v; RuntimePlayerSettings.Save(); });
            y = AddSlider(contentMount, "Mouse sensitivity", y, 0.2f, 3f, RuntimePlayerSettings.MouseSensitivity,
                v => { RuntimePlayerSettings.MouseSensitivity = v; RuntimePlayerSettings.Save(); });

            // Resolution: a cycler + apply (a dropdown from primitives buys nothing extra here).
            y -= 14f;
            _resolutionLabel = AddLabel(contentMount, CurrentResolutionText(), new Vector2(24f, y), 18, Parchment);
            AddButton(contentMount, "NEXT", new Vector2(440f, y), () =>
            {
                var all = Screen.resolutions;
                if (all == null || all.Length == 0) return;
                _resolutionIndex = (_resolutionIndex + 1 + all.Length) % all.Length;
                _resolutionLabel.text = $"Resolution: {all[_resolutionIndex].width}x{all[_resolutionIndex].height} (pending)";
            });
            AddButton(contentMount, "APPLY", new Vector2(560f, y), () =>
            {
                var all = Screen.resolutions;
                if (all == null || all.Length == 0 || _resolutionIndex < 0) return;
                var pick = all[_resolutionIndex];
                Screen.SetResolution(pick.width, pick.height, Screen.fullScreenMode);
                Debug.Log($"[Options] resolution applied: {pick.width}x{pick.height}");
                _resolutionLabel.text = CurrentResolutionText();
            });
        }

        private static string CurrentResolutionText()
            => $"Resolution: {Screen.width}x{Screen.height} ({Screen.fullScreenMode})";

        private float AddSlider(Transform parent, string title, float y, float min, float max,
            float value, System.Action<float> onChanged)
        {
            AddLabel(parent, title, new Vector2(24f, y), 18, Parchment);
            var valueLabel = AddLabel(parent, value.ToString("0.00"), new Vector2(620f, y), 18, Gold);

            var sliderGo = new GameObject(title + "Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            var rect = sliderGo.GetComponent<RectTransform>();
            Place(rect, new Vector2(240f, y - 4f), new Vector2(360f, 22f));

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = PanelBrown;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            StretchFull(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFull(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(1f, 0.62f, 0.25f, 0.9f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(sliderGo.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 26f);
            handle.GetComponent<Image>().color = Parchment;

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v =>
            {
                valueLabel.text = v.ToString("0.00");
                onChanged(v);
            });
            return y - 44f;
        }

        private TextMeshProUGUI AddLabel(Transform parent, string text, Vector2 pos, int size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (_font != null) label.font = _font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Place(go.GetComponent<RectTransform>(), pos, new Vector2(380f, 28f));
            return label;
        }

        private void AddButton(Transform parent, string text, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = PanelBrown;
            Place(go.GetComponent<RectTransform>(), pos, new Vector2(100f, 30f));
            var label = AddLabel(go.transform, text, Vector2.zero, 16, ParchmentDim);
            var labelRect = label.GetComponent<RectTransform>();
            StretchFull(labelRect);
            label.alignment = TextAlignmentOptions.Center;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
        }

        // Top-left anchored placement: x right, y down-negative — matches the scroll column flow.
        private static void Place(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

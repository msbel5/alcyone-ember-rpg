using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberWorldGenUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        private TMP_InputField _moodInput;
        private TMP_InputField _callingInput;
        private TMP_InputField _startInput;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, worldPositionStays: false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.2f);
            rt.anchorMax = new Vector2(0.8f, 0.8f);
            rt.sizeDelta = Vector2.zero;
            
            var img = root.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            if (_panelFrame != null) { img.sprite = _panelFrame; img.type = Image.Type.Sliced; }

            var layoutGo = new GameObject("Layout", typeof(RectTransform), typeof(VerticalLayoutGroup));
            layoutGo.transform.SetParent(root.transform, worldPositionStays: false);
            var layoutRt = layoutGo.GetComponent<RectTransform>();
            layoutRt.anchorMin = Vector2.zero;
            layoutRt.anchorMax = Vector2.one;
            layoutRt.offsetMin = new Vector2(40, 40);
            layoutRt.offsetMax = new Vector2(-40, -40);

            var vlg = layoutGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            CreateLabel(layoutGo.transform, "WORLD GENERATION");
            _moodInput = CreateInputField(layoutGo.transform, "What is the world's mood? (e.g. Grim, Vibrant)");
            _callingInput = CreateInputField(layoutGo.transform, "What is the player's calling? (e.g. Smith, Mage)");
            _startInput = CreateInputField(layoutGo.transform, "Where does fate begin? (e.g. Forge, Tavern)");

            var btnGo = new GameObject("BeginBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(layoutGo.transform, worldPositionStays: false);
            btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(BeginJourney);
            
            var btnText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            btnText.transform.SetParent(btnGo.transform, worldPositionStays: false);
            btnText.text = "BEGIN JOURNEY";
            btnText.alignment = TextAlignmentOptions.Center;
            if (_font != null) btnText.font = _font;
        }

        private void CreateLabel(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.alignment = TextAlignmentOptions.Center;
            if (_font != null) t.font = _font;
            t.fontSize = 24;
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholder)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, worldPositionStays: false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
            
            var input = go.GetComponent<TMP_InputField>();
            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, worldPositionStays: false);
            
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(textArea.transform, worldPositionStays: false);
            var tmp = text.GetComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.fontSize = 18;
            tmp.color = Color.white;

            input.textComponent = tmp;
            input.textViewport = textArea.GetComponent<RectTransform>();
            
            return input;
        }

        private void BeginJourney()
        {
            var adapter = EmberDomainAdapterLocator.Current;
            if (adapter != null)
            {
                adapter.SeedWorld(_moodInput.text, _callingInput.text, _startInput.text);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Faz3SmithingOverworld");
        }
    }
}

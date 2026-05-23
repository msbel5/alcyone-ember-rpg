using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.Adapters;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class CharacterCreationUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private string _firstSceneName = "Faz3SmithingOverworld";

        private readonly List<string> _answers = new List<string>();
        private readonly CharacterCreationViewModel _viewModel = new CharacterCreationViewModel();
        private RectTransform _content;
        private TMP_InputField _nameInput;
        private string _playerName = "Adventurer";
        private string _birthsignId = "the_lover";
        private string _selectedClassId;
        private int _questionIndex;

        private void Awake()
        {
            EnsureEventSystemExists();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            BuildShell();
            ShowNameStep();
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private void BuildShell()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.04f, 0.035f, 0.03f, 1f);

            CreateText(panel.transform, "Character Creation", 42, new Vector2(0.5f, 0.9f), new Vector2(900, 80));
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(panel.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0.18f, 0.12f);
            _content.anchorMax = new Vector2(0.82f, 0.82f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
        }

        private void ShowNameStep()
        {
            ClearContent();
            CreateLabel("Name");
            _nameInput = CreateInput("Adventurer");
            CreateButton("Continue", () =>
            {
                _playerName = string.IsNullOrWhiteSpace(_nameInput.text) ? "Adventurer" : _nameInput.text.Trim();
                ShowBirthsignStep();
            });
        }

        private void ShowBirthsignStep()
        {
            ClearContent();
            CreateLabel("Birthsign");
            foreach (var sign in _viewModel.Birthsigns)
            {
                var captured = sign;
                CreateButton($"{captured.Name}  {captured.PassiveBonus}", () =>
                {
                    _birthsignId = captured.Id;
                    _questionIndex = 0;
                    _answers.Clear();
                    ShowQuestionStep();
                });
            }
        }

        private void ShowQuestionStep()
        {
            ClearContent();
            var question = _viewModel.Questions[_questionIndex];
            CreateLabel($"{_questionIndex + 1}/10  {question.Prompt}");
            foreach (var choice in question.Choices)
            {
                var captured = choice;
                CreateButton($"{captured.Id.ToUpperInvariant()}. {captured.Text}", () =>
                {
                    _answers.Add(captured.Id);
                    _questionIndex++;
                    if (_questionIndex >= _viewModel.Questions.Count) ShowClassStep();
                    else ShowQuestionStep();
                });
            }
        }

        private void ShowClassStep()
        {
            ClearContent();
            var suggested = _viewModel.SuggestClass(_answers);
            if (string.IsNullOrEmpty(_selectedClassId))
                _selectedClassId = suggested.Id;
            CreateLabel($"Suggested Class: {suggested.Name}");
            foreach (var klass in _viewModel.Classes)
            {
                var captured = klass;
                var label = captured.Id == _selectedClassId ? captured.Name + " (Selected)" : captured.Name;
                CreateButton(label, () =>
                {
                    _selectedClassId = captured.Id;
                    ShowClassStep();
                });
            }
            CreateButton("Begin Adventure", BeginAdventure);
        }

        private void BeginAdventure()
        {
            var pending = EmberWorldGenIntent.Pending ?? new EmberWorldGenIntent(string.Empty, string.Empty, string.Empty);
            EmberWorldGenIntent.Pending = pending.WithCharacter(_playerName, _selectedClassId, _birthsignId, _answers.ToArray());
            SceneManager.LoadScene(_firstSceneName);
        }

        private void ClearContent()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
        }

        private void CreateLabel(string text)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(_content, false);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 54);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.95f, 0.86f, 0.62f);
            if (_font != null) tmp.font = _font;
        }

        private TMP_InputField CreateInput(string placeholder)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(_content, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(620, 48);
            go.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

            var input = go.GetComponent<TMP_InputField>();
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12, 4);
            textRt.offsetMax = new Vector2(-12, -4);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 22;
            tmp.color = Color.white;
            if (_font != null) tmp.font = _font;
            input.textComponent = tmp;
            input.text = placeholder;
            return input;
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_content, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 44);
            go.GetComponent<Image>().color = new Color(0.18f, 0.14f, 0.09f, 0.96f);
            go.GetComponent<Button>().onClick.AddListener(action);

            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(go.transform, false);
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (_font != null) tmp.font = _font;
        }

        private void CreateText(Transform parent, string text, int size, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = sizeDelta;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.82f, 0.32f);
            if (_font != null) tmp.font = _font;
        }
    }
}

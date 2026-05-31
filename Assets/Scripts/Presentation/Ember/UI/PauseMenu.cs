using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Save;

namespace EmberCrpg.Presentation.Ember.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _panelFrame;

        private CanvasGroup _canvasGroup;
        private bool _isPaused;
        private SaveSlotBrowserState _slotState;
        private TextMeshProUGUI _slotText;

        private void Awake()
        {
            // Codex ninth-pass A-P1 / D-P1: previously SetActive(false) on the
            // host GameObject — that turned Update() into dead code so Esc was
            // never read. Use CanvasGroup transparency + blocksRaycasts toggle
            // to "hide" while keeping the listener alive.
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            BuildMenu();
        }

        private void Update()
        {
            if (EmberInput.PauseDown)
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            RefreshSlotLabel();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            StartCoroutine(UiAnimationHelper.AnimateOpen(_canvasGroup, GetComponent<RectTransform>()));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            StartCoroutine(CloseRoutine());
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private System.Collections.IEnumerator CloseRoutine()
        {
            yield return UiAnimationHelper.AnimateClose(_canvasGroup, GetComponent<RectTransform>());
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void BuildMenu()
        {
            // Simple vertical layout
            var layoutGo = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            layoutGo.transform.SetParent(transform, worldPositionStays: false);
            var rt = layoutGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.2f);
            rt.anchorMax = new Vector2(0.65f, 0.8f);
            rt.sizeDelta = Vector2.zero;

            var group = layoutGo.GetComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlHeight = true;
            group.childControlWidth = true;

            CreateButton(layoutGo.transform, "RESUME", Resume);
            CreateButton(layoutGo.transform, "SAVE (F5)", InvokeSave);
            CreateButton(layoutGo.transform, "LOAD (F9)", InvokeLoad);
            _slotState = new SaveSlotBrowserState(10);
            _slotText = CreateLabel(layoutGo.transform, "Quick | Empty");
            CreateButton(layoutGo.transform, "PREV SLOT", PreviousSlot);
            CreateButton(layoutGo.transform, "NEXT SLOT", NextSlot);
            CreateButton(layoutGo.transform, "SAVE SLOT", SaveSelectedSlot);
            CreateButton(layoutGo.transform, "LOAD SLOT", LoadSelectedSlot);
            CreateButton(layoutGo.transform, "DELETE SLOT", DeleteSelectedSlot);
            CreateButton(layoutGo.transform, "MAIN MENU", () => { Time.timeScale = 1f; SceneManager.LoadScene(EmberScenes.MainMenu); });
            CreateButton(layoutGo.transform, "QUIT", Application.Quit);
            RefreshSlotLabel();
        }

        private static void InvokeSave()
        {
            var svc = Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.Save.EmberSaveService>(FindObjectsInactive.Include);
            if (svc != null) svc.Save();
        }

        private static void InvokeLoad()
        {
            var svc = Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.Save.EmberSaveService>(FindObjectsInactive.Include);
            if (svc != null) svc.Load();
        }

        private void PreviousSlot()
        {
            _slotState.MovePrevious();
            RefreshSlotLabel();
        }

        private void NextSlot()
        {
            _slotState.MoveNext();
            RefreshSlotLabel();
        }

        private void SaveSelectedSlot()
        {
            var svc = Object.FindFirstObjectByType<EmberSaveService>(FindObjectsInactive.Include);
            if (svc != null) svc.SaveSlot(_slotState.CurrentSlot);
            RefreshSlotLabel();
        }

        private void LoadSelectedSlot()
        {
            var svc = Object.FindFirstObjectByType<EmberSaveService>(FindObjectsInactive.Include);
            if (svc != null) svc.LoadSlot(_slotState.CurrentSlot);
            RefreshSlotLabel();
        }

        private void DeleteSelectedSlot()
        {
            var svc = Object.FindFirstObjectByType<EmberSaveService>(FindObjectsInactive.Include);
            if (svc != null) svc.DeleteSlot(_slotState.CurrentSlot);
            RefreshSlotLabel();
        }

        private void RefreshSlotLabel()
        {
            if (_slotText == null || _slotState == null) return;
            SaveSlotMetadata meta = null;
            try
            {
                var repo = new FileSaveRepository(Application.persistentDataPath);
                repo.TryLoadMetadata(_slotState.CurrentSlot, out meta);
            }
            catch { /* metadata is optional */ }
            _slotText.text = _slotState.DescribeCurrent(meta);
        }

        private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, worldPositionStays: false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            if (_panelFrame != null) { image.sprite = _panelFrame; image.type = Image.Type.Sliced; }

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(action);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, worldPositionStays: false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            if (_font != null) text.font = _font;
            text.fontSize = 18;
            text.color = Color.white;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string label)
        {
            var go = new GameObject("SAVE SLOT STATUS", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, worldPositionStays: false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            if (_font != null) text.font = _font;
            text.fontSize = 16;
            text.color = new Color(0.9f, 0.82f, 0.68f, 1f);
            return text;
        }
    }
}

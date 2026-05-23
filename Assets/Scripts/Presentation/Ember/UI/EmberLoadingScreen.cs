using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class EmberLoadingScreen : MonoBehaviour
    {
        public static EmberLoadingScreen Instance { get; private set; }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _backgroundSprite;

        private CanvasGroup _canvasGroup;
        private TMP_Text _flavorText;
        private Image _spinner;
        
        [System.Serializable]
        private class FlavorData { public string[] flavors; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Audit (eighth pass D-P1): when instantiated bare (no scene
            // authoring) the screen needs a Canvas + CanvasGroup to render.
            EnsureCanvasOnSelf();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            BuildUI();
            // Audit (eighth pass D-P1): when spawned by the main menu before
            // SceneManager.LoadScene, display immediately and self-destroy on
            // the next scene's first Update (or after a timeout).
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            PickRandomFlavor();
            StartCoroutine(AutoDismissRoutine());
        }

        private IEnumerator AutoDismissRoutine()
        {
            var startScene = SceneManager.GetActiveScene().buildIndex;
            float maxWait = 8f;
            float elapsed = 0f;
            while (elapsed < maxWait)
            {
                if (_spinner != null) _spinner.transform.Rotate(0, 0, -200 * Time.unscaledDeltaTime);
                elapsed += Time.unscaledDeltaTime;
                if (SceneManager.GetActiveScene().buildIndex != startScene)
                {
                    // Next scene reached; one extra frame so it can render, then dismiss.
                    yield return null;
                    break;
                }
                yield return null;
            }
            yield return StartCoroutine(Fade(_canvasGroup.alpha, 0f, 0.3f));
            if (Instance == this) Instance = null;
            Destroy(gameObject);
        }

        private void EnsureCanvasOnSelf()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Audit (ninth pass A-P3/D-P1): the explicit LoadScene(string)/LoadRoutine
        // pair was deleted — no production caller existed (Codex confirmed). The
        // Awake auto-display + AutoDismissRoutine is the only used entry path.

        private void PickRandomFlavor()
        {
            var json = Resources.Load<TextAsset>("loading-flavors");
            if (json != null)
            {
                var data = JsonUtility.FromJson<FlavorData>(json.text);
                if (data != null && data.flavors.Length > 0)
                {
                    _flavorText.text = data.flavors[Random.Range(0, data.flavors.Length)];
                }
            }
        }

        private IEnumerator Fade(float start, float end, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, end, t / duration);
                yield return null;
            }
            _canvasGroup.alpha = end;
        }

        private void BuildUI()
        {
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(transform, worldPositionStays: false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = Color.black;
            if (_backgroundSprite != null) bgImg.sprite = _backgroundSprite;
            bgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;

            var flavorGo = new GameObject("FlavorText", typeof(RectTransform), typeof(TextMeshProUGUI));
            flavorGo.transform.SetParent(transform, worldPositionStays: false);
            _flavorText = flavorGo.GetComponent<TextMeshProUGUI>();
            _flavorText.alignment = TextAlignmentOptions.Center;
            if (_font != null) _flavorText.font = _font;
            _flavorText.fontSize = 24;
            _flavorText.color = new Color(0.9f, 0.85f, 0.7f);
            
            var flavorRt = flavorGo.GetComponent<RectTransform>();
            flavorRt.anchorMin = new Vector2(0.1f, 0.1f);
            flavorRt.anchorMax = new Vector2(0.9f, 0.3f);

            var spinGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
            spinGo.transform.SetParent(transform, worldPositionStays: false);
            _spinner = spinGo.GetComponent<Image>();
            _spinner.color = new Color(0.9f, 0.85f, 0.7f, 0.5f);
            var spinRt = spinGo.GetComponent<RectTransform>();
            spinRt.anchorMin = spinRt.anchorMax = new Vector2(0.5f, 0.5f);
            spinRt.sizeDelta = new Vector2(64, 64);
        }
    }
}

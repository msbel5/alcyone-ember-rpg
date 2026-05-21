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

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            
            BuildUI();
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            gameObject.SetActive(true);
            PickRandomFlavor();
            
            yield return StartCoroutine(Fade(0f, 1f, 0.3f));
            
            float startTime = Time.realtimeSinceStartup;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                if (_spinner != null) _spinner.transform.Rotate(0, 0, -200 * Time.deltaTime);
                yield return null;
            }

            // Ensure min duration
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < 0.6f) yield return new WaitForSecondsRealtime(0.6f - elapsed);

            yield return StartCoroutine(Fade(1f, 0f, 0.3f));
            gameObject.SetActive(false);
        }

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

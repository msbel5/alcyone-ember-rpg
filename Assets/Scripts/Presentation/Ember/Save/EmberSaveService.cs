using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace EmberCrpg.Presentation.Ember.Save
{
    [Serializable]
    public class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public float playerYaw;
        public int tickIndex;
    }

    public sealed class EmberSaveService : MonoBehaviour
    {
        private const string SaveKey = "ember.save.v1";
        private UnityEngine.UI.Text _statusText;

        private void Awake()
        {
            var canvas = GameObject.Find("EmberHUD") ?? GameObject.FindAnyObjectByType<Canvas>()?.gameObject;
            if (canvas == null) return;

            var go = new GameObject("SaveStatus", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 100);
            rt.anchoredPosition = Vector2.zero;
            
            _statusText = go.GetComponent<UnityEngine.UI.Text>();
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 32;
            _statusText.color = new Color(1f, 1f, 1f, 0f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) Save();
            if (Input.GetKeyDown(KeyCode.F9)) Load();
        }

        private void Save()
        {
            var player = GameObject.Find("PlayerRig");
            if (player == null) return;

            int ticks = 0;
            var adapter = EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current;
            if (adapter != null) ticks = adapter.TickIndex;

            var data = new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = player.transform.position,
                playerYaw = player.transform.eulerAngles.y,
                tickIndex = ticks
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            ShowStatus("Saved.");
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<SaveData>(json);
            if (SceneManager.GetActiveScene().name != data.sceneName)
            {
                // To restore after load, we use a static or persistent object.
                // For this project, let's just use a static field to hold the data.
                _pendingLoad = data;
                SceneManager.LoadScene(data.sceneName);
            }
            else
            {
                RestorePosition(data);
                ShowStatus("Loaded.");
            }
        }

        private static SaveData _pendingLoad;

        private void Start()
        {
            if (_pendingLoad != null && _pendingLoad.sceneName == SceneManager.GetActiveScene().name)
            {
                RestorePosition(_pendingLoad);
                _pendingLoad = null;
                ShowStatus("Loaded.");
            }
        }

        private void RestorePosition(SaveData data)
        {
            var player = GameObject.Find("PlayerRig");
            if (player != null)
            {
                // Disable character controller while moving to prevent collision fighting
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                player.transform.position = data.playerPosition;
                player.transform.rotation = Quaternion.Euler(0, data.playerYaw, 0);
                
                if (cc != null) cc.enabled = true;

                var fps = player.GetComponent<EmberCrpg.Presentation.Ember.Camera.EmberFirstPersonController>();
                if (fps != null)
                {
                    fps.SyncYaw(data.playerYaw);
                }
            }
        }

        private void ShowStatus(string msg)
        {
            StopAllCoroutines();
            StartCoroutine(FadeStatus(msg));
        }

        private IEnumerator FadeStatus(string msg)
        {
            if (_statusText == null) yield break;
            _statusText.text = msg;
            float elapsed = 0;
            while (elapsed < 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = elapsed < 0.5f ? elapsed / 0.5f : (elapsed > 1.5f ? 1f - (elapsed - 1.5f) / 0.5f : 1f);
                _statusText.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            _statusText.color = new Color(1, 1, 1, 0);
        }
    }
}

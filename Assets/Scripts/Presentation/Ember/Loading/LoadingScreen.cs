using EmberCrpg.Ui.Foundation;
using EmberCrpg.Presentation.Ember.UI;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Loading
{
    public static class LoadingScreen
    {
        private static LoadingScreenController _controller;

        public static void Show(string title, string subtitle)
        {
            EnsureController().Show(title, subtitle);
        }

        public static void SetProgress(float normalized, string currentLabel)
        {
            EnsureController().SetProgress(normalized, currentLabel);
        }

        public static void LogLine(UiLogSeverity severity, string line)
        {
            EnsureController().LogLine(severity, line);
        }

        public static void ShowThumbnail(Texture2D texture, string caption)
        {
            EnsureController().ShowThumbnail(texture, caption);
        }

        public static void Hide()
        {
            if (_controller != null) _controller.Hide();
        }

        private static LoadingScreenController EnsureController()
        {
            VisibleUiSurface.Ensure();
            if (_controller != null) return _controller;
            var go = new GameObject("LoadingScreenController");
            if (Application.isPlaying) Object.DontDestroyOnLoad(go);
            _controller = go.AddComponent<LoadingScreenController>();
            return _controller;
        }
    }
}

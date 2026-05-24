using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        private IUiPanel _panel;

        public void Show(string title, string subtitle)
        {
            if (_panel == null) _panel = UiSurfaceLocator.Current?.Mount("LoadingScreen");
            _panel?.SetText("title", title);
            _panel?.SetText("subtitle", subtitle);
            _panel?.SetVisible("root", true);
        }

        public void SetProgress(float normalized, string currentLabel)
        {
            _panel?.SetProgress("progress", normalized);
            _panel?.SetText("current", currentLabel);
        }

        public void LogLine(UiLogSeverity severity, string line)
        {
            _panel?.LogLine("log", severity, line);
        }

        public void ShowThumbnail(Texture2D texture, string caption)
        {
            _panel?.SetThumbnail("thumbnail", texture);
            _panel?.SetText("caption", caption);
        }

        public void Hide()
        {
            if (_panel == null) return;
            UiSurfaceLocator.Current?.Unmount(_panel);
            _panel = null;
        }
    }
}

using EmberCrpg.Ui.Foundation;
using EmberCrpg.Presentation.Ember.UI;
using System.Collections.Generic;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Loading
{
    public sealed class LoadingScreenContext
    {
        public LoadingScreenContext(string areaId, string areaName, string loadingType)
        {
            AreaId = areaId ?? string.Empty;
            AreaName = areaName ?? string.Empty;
            LoadingType = string.IsNullOrWhiteSpace(loadingType) ? "load" : loadingType.Trim().ToLowerInvariant();
        }

        public string AreaId { get; }
        public string AreaName { get; }
        public string LoadingType { get; }

        public LoadingScreenContext WithAreaId(string areaId) => new LoadingScreenContext(areaId, AreaName, LoadingType);
        public LoadingScreenContext WithAreaName(string areaName) => new LoadingScreenContext(AreaId, areaName, LoadingType);
        public LoadingScreenContext WithLoadingType(string loadingType) => new LoadingScreenContext(AreaId, AreaName, loadingType);

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "area_id", AreaId },
                { "area_name", AreaName },
                { "loading_type", LoadingType },
            };
        }
    }

    public static class LoadingScreen
    {
        private static LoadingScreenController _controller;

        public static void Show(string title, string subtitle)
        {
            ShowForContext(new LoadingScreenContext(string.Empty, subtitle, "load"));
            EnsureController().SetTitle(title);
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
            if (_controller != null) _controller.Dismiss();
        }

        public static void ShowForContext(LoadingScreenContext context)
        {
            EnsureController().ShowForContext(context ?? new LoadingScreenContext(string.Empty, string.Empty, "load"));
        }

        public static void Dismiss()
        {
            if (_controller != null) _controller.Dismiss();
        }

        public static bool IsVisibleLoading()
        {
            return _controller != null && _controller.IsVisibleLoading();
        }

        public static void SetLoadingContext(LoadingScreenContext context)
        {
            EnsureController().SetLoadingContext(context ?? new LoadingScreenContext(string.Empty, string.Empty, "load"));
        }

        public static LoadingScreenContext GetLoadingContext()
        {
            return EnsureController().GetLoadingContext();
        }

        public static void SetAreaId(string areaId)
        {
            EnsureController().SetAreaId(areaId);
        }

        public static void SetAreaName(string areaName)
        {
            EnsureController().SetAreaName(areaName);
        }

        public static void SetLoadingType(string loadingType)
        {
            EnsureController().SetLoadingType(loadingType);
        }

        public static Texture2D LoadBackdropForArea(string areaId)
        {
            return EnsureController().LoadBackdropForArea(areaId);
        }

        public static void ApplyBackdrop(Texture2D texture)
        {
            EnsureController().ApplyBackdrop(texture);
        }

        public static void StartTipRotation()
        {
            EnsureController().StartTipRotation();
        }

        public static void StopTipRotation()
        {
            EnsureController().StopTipRotation();
        }

        public static void AdvanceTip()
        {
            EnsureController().AdvanceTip();
        }

        public static string GetCurrentTip()
        {
            return EnsureController().GetCurrentTip();
        }

        public static float GetProgress()
        {
            return EnsureController().GetProgress();
        }

        public static void TickEllipsisAnimation(float deltaTime)
        {
            EnsureController().TickEllipsisAnimation(deltaTime);
        }

        public static string BuildLoadingLabelText()
        {
            return EnsureController().BuildLoadingLabelText();
        }

        public static void FadeIn()
        {
            EnsureController().FadeIn();
        }

        public static void FadeOut()
        {
            EnsureController().FadeOut();
        }

        public static void SetInputBlocking(bool blocked)
        {
            EnsureController().SetInputBlocking(blocked);
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

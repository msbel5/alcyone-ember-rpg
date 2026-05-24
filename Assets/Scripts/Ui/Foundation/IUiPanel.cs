using System;
using UnityEngine;

namespace EmberCrpg.Ui.Foundation
{
    public interface IUiPanel : IDisposable
    {
        string Id { get; }
        string Kind { get; }
        void SetText(string slot, string text);
        void SetProgress(string slot, float normalized);
        void LogLine(string slot, UiLogSeverity severity, string line);
        void SetThumbnail(string slot, Texture2D texture);
        void SetVisible(string slot, bool visible);
        void SetButtonHandler(string slot, Action onClick);

        void SetImage(string slot, Texture2D texture);
        void AppendLog(string slot, UiLogSeverity severity, string line);
    }

    public enum UiLogSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3,
    }
}

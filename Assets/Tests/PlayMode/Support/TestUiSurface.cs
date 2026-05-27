using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Tests.PlayMode.Support
{
    public sealed class TestUiSurface : IUiSurface
    {
        public UiTokens Tokens { get; } = ScriptableObject.CreateInstance<UiTokens>();
        public TestUiPanel LastPanel { get; private set; }
        public IUiPanel Mount(string panelId) { LastPanel = new TestUiPanel(panelId); return LastPanel; }
        public void Unmount(IUiPanel panel) { }
        public void Clear() { LastPanel = null; }
        public void Dispose() { }
    }

    public sealed class TestUiPanel : IUiPanel
    {
        public TestUiPanel(string id) { Id = id; Kind = id; }
        public string Id { get; }
        public string Kind { get; }
        public string LogText { get; private set; } = string.Empty;
        public void SetText(string slot, string text) { }
        public void SetProgress(string slot, float normalized) { }
        public void LogLine(string slot, UiLogSeverity severity, string line) { LogText += "[" + severity.ToString().ToLowerInvariant() + "] " + line + "\n"; }
        public void SetThumbnail(string slot, Texture2D texture) { }
        public void SetThumbnailGrid(string slot, System.Collections.Generic.IReadOnlyList<Texture2D> textures) { }
        public void SetVisible(string slot, bool visible) { }
        public void SetButtonHandler(string slot, Action onClick) { }
        public void SetImage(string slot, Texture2D texture) => SetThumbnail(slot, texture);
        public void AppendLog(string slot, UiLogSeverity severity, string line) => LogLine(slot, severity, line);
        public void Dispose() { }
    }

    public sealed class TestAssetForge : IAssetForge
    {
        private readonly string _failRequestId;
        public TestAssetForge(string failRequestId) { _failRequestId = failRequestId; }
        public bool IsAvailable() => true;
        public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (request.RequestId == _failRequestId) return Task.FromResult(AssetGenerationResult.Failed(request.RequestId, "test_failure"));
            return Task.FromResult(new AssetGenerationResult(request.RequestId, new byte[] { 137, 80, 78, 71 }, "image/png", 1, true, ""));
        }
    }
}

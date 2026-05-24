using System;

namespace EmberCrpg.Ui.Foundation
{
    public interface IUiSurface : IDisposable
    {
        UiTokens Tokens { get; }
        IUiPanel Mount(string panelId);
        void Unmount(IUiPanel panel);
        void Clear();
    }
}

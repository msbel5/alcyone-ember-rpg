using EmberCrpg.Ui.Backends.UiToolkit;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    public static class VisibleUiSurface
    {
        public static IUiSurface Ensure()
        {
            if (UiSurfaceLocator.Current != null) return UiSurfaceLocator.Current;
            var go = new GameObject("VisibleGenerationUiSurface");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<UiToolkitSurface>();
            return UiSurfaceLocator.Current;
        }
    }
}

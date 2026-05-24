using EmberCrpg.Ui.Foundation;
using UnityEngine.UIElements;

namespace EmberCrpg.Ui.Backends.UiToolkit
{
    public static class UiToolkitThemeBinder
    {
        public static void Apply(VisualElement root, UiTokens tokens)
        {
            if (root == null || tokens == null) return;
            root.style.backgroundColor = tokens.Background;
            root.style.color = tokens.Text;
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberCrpg.Ui.Backends.UiToolkit
{
    [DisallowMultipleComponent]
    public sealed class UiToolkitSurface : MonoBehaviour, IUiSurface
    {
        [SerializeField] private UiTokens _tokens;
        private readonly List<IUiPanel> _panels = new List<IUiPanel>();
        private VisualElement _root;

        public UiTokens Tokens => _tokens;

        private void Awake()
        {
            if (_tokens == null) _tokens = ScriptableObject.CreateInstance<UiTokens>();
            var document = GetComponent<UIDocument>();
            if (document == null) document = gameObject.AddComponent<UIDocument>();
            if (document.panelSettings == null) document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _root = document.rootVisualElement;
            if (UiSurfaceLocator.Current == null) UiSurfaceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(UiSurfaceLocator.Current, this)) UiSurfaceLocator.Clear();
        }

        public IUiPanel Mount(string panelId)
        {
            var panel = new UiToolkitPanel(panelId, _root, _tokens);
            _panels.Add(panel);
            return panel;
        }

        public void Unmount(IUiPanel panel)
        {
            if (panel == null) return;
            _panels.Remove(panel);
            panel.Dispose();
        }

        public void Clear()
        {
            for (int i = _panels.Count - 1; i >= 0; i--) _panels[i].Dispose();
            _panels.Clear();
            _root?.Clear();
        }

        public void Dispose() => Clear();
    }
}

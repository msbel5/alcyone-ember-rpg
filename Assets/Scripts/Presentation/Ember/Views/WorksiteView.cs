using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// Visualises a worksite (forge, oven, stall) as a static marker plus an emissive
    /// pulse when work is active. The simulation host pushes one
    /// <see cref="WorksiteViewState"/> per tick to flip the active/idle visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorksiteView : MonoBehaviour
    {
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Color _idleEmission = new Color(0.1f, 0.05f, 0.02f);
        [SerializeField] private Color _activeEmission = new Color(1.4f, 0.7f, 0.2f);

        private WorksiteViewState _state;

        public void SetState(WorksiteViewState state)
        {
            _state = state;
            ApplyEmission();
        }

        private void Awake()
        {
            if (_bodyRenderer == null) _bodyRenderer = GetComponentInChildren<Renderer>();
            ApplyEmission();
        }

        private void ApplyEmission()
        {
            if (_bodyRenderer == null) return;
            var mat = _bodyRenderer.material;
            if (mat == null) return;
            var color = _state.IsActive ? _activeEmission : _idleEmission;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", color);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    public readonly struct WorksiteViewState
    {
        public readonly bool IsActive;
        public readonly int QueueDepth;
        public WorksiteViewState(bool isActive, int queueDepth)
        {
            IsActive = isActive;
            QueueDepth = queueDepth;
        }
    }
}

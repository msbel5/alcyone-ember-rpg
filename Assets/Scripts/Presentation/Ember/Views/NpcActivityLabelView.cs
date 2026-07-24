using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// PLAYTEST FIX ("npclerin ne yaptigi anlasilmiyor"): a small readable verb floating over a
    /// civilian billboard - "working", "eating", "resting" - fed per tick from the simulation via
    /// ActorViewState.Activity (ScheduleSystem truth, unlike the hour-guessing pose icons).
    /// Legacy TextMesh + builtin font: survives player builds without TMP Essentials.
    /// Culled past 22 m so the street does not become a wall of words.
    /// </summary>
    public sealed class NpcActivityLabelView : MonoBehaviour
    {
        private const float VisibleMeters = 22f;
        private TextMesh _label;
        private Renderer _renderer;
        private float _nextCullCheck;

        public void Bind()
        {
            var go = new GameObject("ActivityLabel");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            go.transform.localScale = Vector3.one * 0.035f;
            _label = go.AddComponent<TextMesh>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 48;
            _label.anchor = TextAnchor.LowerCenter;
            _label.alignment = TextAlignment.Center;
            _label.color = new Color(0.95f, 0.90f, 0.75f, 0.85f);
            _renderer = go.GetComponent<MeshRenderer>();
            if (_renderer != null && _label.font != null) _renderer.material = _label.font.material;
            // NO CameraFacingBillboard here: its convention points +Z at the camera, which
            // MIRRORS TextMesh glyphs ("idling" read "gnilbi" in the live playtest). We face
            // the camera ourselves below with the text-readable convention.
        }

        /// <summary>W32 DOC5 agentcheck: the ACTUAL TextMesh text — render-layer truth, not the pushed value.</summary>
        public string RenderedText => _label != null ? _label.text : null;

        public void SetActivity(string activity)
        {
            if (_label != null && _label.text != (activity ?? string.Empty))
                _label.text = activity ?? string.Empty;
        }

        private void Update()
        {
            var cam = UnityEngine.Camera.main; // fully qualified: an Ember.Camera namespace shadows the type
            if (cam == null || _label == null) return;
            // Readable-facing: +Z points AWAY from the camera so the glyphs are not mirrored.
            _label.transform.rotation =
                Quaternion.LookRotation(_label.transform.position - cam.transform.position);
            if (_renderer == null || Time.unscaledTime < _nextCullCheck) return;
            _nextCullCheck = Time.unscaledTime + 0.6f;
            bool near = (cam.transform.position - transform.position).sqrMagnitude < VisibleMeters * VisibleMeters;
            if (_renderer.enabled != near) _renderer.enabled = near;
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// Keeps a sprite upright while rotating it toward the active camera on the yaw axis.
    /// This matches the Daggerfall-style 2D character in a 3D world treatment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        [SerializeField] private bool _yawOnly = true;

        private Transform _cameraTransform;

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                var main = UnityEngine.Camera.main;
                if (main == null) return;
                _cameraTransform = main.transform;
            }

            var toCamera = _cameraTransform.position - transform.position;
            if (_yawOnly)
                toCamera.y = 0f;
            if (toCamera.sqrMagnitude <= 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}

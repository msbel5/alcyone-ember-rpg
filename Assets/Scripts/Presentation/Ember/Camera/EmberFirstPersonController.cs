using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Camera
{
    /// <summary>
    /// Morrowind/Daggerfall-style first-person controller. Yaw is applied to the rig root,
    /// pitch is applied to the eye camera, translation moves the rig along its local plane.
    /// Input is read from the legacy <c>Input</c> module so the controller works without
    /// any package dependency.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberFirstPersonController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _mouseSensitivity = 2.1f;
        [SerializeField] private float _pitchMinDegrees = -85f;
        [SerializeField] private float _pitchMaxDegrees = 85f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpForce = 7f;

        private Transform _eye;
        private CharacterController _controller;
        private float _yawDegrees;
        private float _pitchDegrees;
        private float _verticalVelocity;
        private bool _captureCursor = true;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera");
            _controller = GetComponent<CharacterController>();
            _yawDegrees = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            if (_captureCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            ApplyLook();
            ApplyMove();
            if (Input.GetKeyDown(KeyCode.F1)) ToggleCursor();
        }

        private void ApplyLook()
        {
            var mouseDeltaX = Input.GetAxis("Mouse X") * _mouseSensitivity;
            var mouseDeltaY = Input.GetAxis("Mouse Y") * _mouseSensitivity;
            _yawDegrees += mouseDeltaX;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - mouseDeltaY, _pitchMinDegrees, _pitchMaxDegrees);
            transform.rotation = Quaternion.Euler(0f, _yawDegrees, 0f);
            if (_eye != null) _eye.localRotation = Quaternion.Euler(_pitchDegrees, 0f, 0f);
        }

        private void ApplyMove()
        {
            var forward = Input.GetAxisRaw("Vertical");
            var right = Input.GetAxisRaw("Horizontal");
            var planar = (transform.forward * forward + transform.right * right).normalized;
            if (_controller == null)
            {
                transform.position += planar * (_moveSpeed * Time.deltaTime);
                return;
            }

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -1f;
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _verticalVelocity = _jumpForce;
                }
            }
            _verticalVelocity += _gravity * Time.deltaTime;

            var motion = planar * _moveSpeed;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
        }

        public void SyncYaw(float yaw)
        {
            _yawDegrees = yaw;
            transform.rotation = Quaternion.Euler(0f, _yawDegrees, 0f);
        }

        private void ToggleCursor()
        {
            _captureCursor = !_captureCursor;
            Cursor.lockState = _captureCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_captureCursor;
        }
    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Camera
{
    /// <summary>
    /// AAA Polished first-person controller.
    /// Implements: Mouse smoothing, WASD acceleration ramp, Sprint, and Frequency-modulated Headbob.
    /// Surface-aware footstep detection stub included.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class EmberFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _baseSpeed = 4.5f;
        [SerializeField] private float _sprintMultiplier = 1.6f;
        [SerializeField] private float _accelTime = 0.15f;
        [SerializeField] private float _decelTime = 0.1f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpForce = 7f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 4.5f; // Increased from 2.1
        [SerializeField] private float _lookSmoothing = 0.02f; // Reduced from 0.05 for snappier feel
[SerializeField] private float _pitchMinDegrees = -85f;
        [SerializeField] private float _pitchMaxDegrees = 85f;

        [Header("Headbob")]
        [SerializeField] private float _bobFrequency = 1.1f;
        [SerializeField] private float _bobVerticalAmplitude = 0.015f;
        [SerializeField] private float _bobHorizontalAmplitude = 0.008f;

        private Transform _eye;
        private CharacterController _controller;
        
        // State
        private float _yawDegrees;
        private float _pitchDegrees;
        private float _verticalVelocity;
        private bool _captureCursor = true;
        
        // Smoothing
        private Vector2 _currentMouseDelta;
        private Vector2 _mouseDeltaVelocity;
        private Vector3 _currentMoveVelocity;
        private Vector3 _moveVelocityDamp;
        
        // Bobbing
        private float _bobTimer;
        private Vector3 _eyeBaseLocalPos;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera") ?? GetComponentInChildren<UnityEngine.Camera>()?.transform;
            if (_eye != null) _eyeBaseLocalPos = _eye.localPosition;
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
            if (_eye == null) _eye = transform.Find("EyeCamera") ?? GetComponentInChildren<UnityEngine.Camera>()?.transform;
        }

        private void Update()
        {
            if (EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost.IsModalOpen()) return;

            ApplyLook();
            ApplyMove();
            ApplyHeadbob();
            
            if (Input.GetKeyDown(KeyCode.F1)) ToggleCursor();
        }

        private void ApplyLook()
        {
            if (_eye == null) return;

            Vector2 targetMouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * _mouseSensitivity;
            _currentMouseDelta = Vector2.SmoothDamp(_currentMouseDelta, targetMouseDelta, ref _mouseDeltaVelocity, _lookSmoothing);

            _yawDegrees += _currentMouseDelta.x;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - _currentMouseDelta.y, _pitchMinDegrees, _pitchMaxDegrees);
            
            transform.rotation = Quaternion.Euler(0f, _yawDegrees, 0f);
            _eye.localRotation = Quaternion.Euler(_pitchDegrees, 0f, 0f);
        }

        private void ApplyMove()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);

            Vector3 targetInput = (transform.forward * vertical + transform.right * horizontal).normalized;
            float targetSpeed = _baseSpeed * (isSprinting ? _sprintMultiplier : 1f);
            
            if (targetInput.magnitude < 0.1f) targetSpeed = 0f;

            _currentMoveVelocity = Vector3.SmoothDamp(_currentMoveVelocity, targetInput * targetSpeed, ref _moveVelocityDamp, targetSpeed > 0.1f ? _accelTime : _decelTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -1f;
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _verticalVelocity = _jumpForce;
                }
            }
            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 motion = _currentMoveVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
            
            if (_controller.isGrounded && _currentMoveVelocity.magnitude > 0.1f)
            {
                // Surface aware footstep check would go here (raycast down)
            }
        }

        private void ApplyHeadbob()
        {
            if (_eye == null) return;

            float speed = _currentMoveVelocity.magnitude;
            if (!_controller.isGrounded || speed < 0.1f)
            {
                _bobTimer = 0f;
                _eye.localPosition = Vector3.Lerp(_eye.localPosition, _eyeBaseLocalPos, Time.deltaTime * 5f);
                return;
            }

            float multiplier = Input.GetKey(KeyCode.LeftShift) ? 1.2f : 1.0f;
            _bobTimer += Time.deltaTime * _bobFrequency * multiplier;

            float vBob = Mathf.Sin(_bobTimer * 2f * Mathf.PI) * _bobVerticalAmplitude * multiplier;
            float hBob = Mathf.Cos(_bobTimer * Mathf.PI) * _bobHorizontalAmplitude * multiplier;

            _eye.localPosition = _eyeBaseLocalPos + new Vector3(hBob, vBob, 0f);
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


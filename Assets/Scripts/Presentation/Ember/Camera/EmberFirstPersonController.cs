using UnityEngine;
using EmberCrpg.Presentation.Ember.Inputs;

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
        [SerializeField] private float _mouseSensitivity = 2.0f; // ~2.0 feels right; 4.5 was too high
        private const float MaxMouseSensitivity = 2.0f; // cap: the 10 scenes baked an over-high 4.5
        [SerializeField] private float _lookSmoothing = 0.02f; // Reduced from 0.05 for snappier feel
        [SerializeField] private AnimationCurve _mouseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float _mouseCurveStrength = 0.5f;

[SerializeField] private float _pitchMinDegrees = -85f;
        [SerializeField] private float _pitchMaxDegrees = 85f;

        [Header("Headbob")]
        [SerializeField] private float _bobFrequency = 1.1f;
        [SerializeField] private float _bobVerticalAmplitude = 0.015f;
        [SerializeField] private float _bobHorizontalAmplitude = 0.008f;

        [Header("Footsteps")]
        [SerializeField] private float _footstepDistance = 2.2f;
        private float _distanceTravelled;


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
        
        private UnityEngine.Camera _cam;
        private float _baseFov;
        private float _fovVelocity;


        private void Awake()
        {
            _eye = transform.Find("EyeCamera") ?? GetComponentInChildren<UnityEngine.Camera>()?.transform;
            if (_eye != null) 
            {
                _eyeBaseLocalPos = _eye.localPosition;
                _cam = _eye.GetComponent<UnityEngine.Camera>();
                if (_cam != null) _baseFov = _cam.fieldOfView;
            }
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
            
            if (EmberInput.ToggleCursor) ToggleCursor();
        }

        private void ApplyLook()
        {
            if (_eye == null) return;

            float rawX = EmberInput.Look.x;
            float rawY = EmberInput.Look.y;

            // Apply Mouse Curve for non-linear response
            float mag = new Vector2(rawX, rawY).magnitude;
            float curvedMag = _mouseCurve.Evaluate(mag);
            float multiplier = Mathf.Lerp(1f, curvedMag / Mathf.Max(mag, 0.001f), _mouseCurveStrength);

            // Cap so a scene-baked over-high value (4.5) can't make the look feel twitchy.
            float sensitivity = Mathf.Min(_mouseSensitivity, MaxMouseSensitivity);
            Vector2 targetMouseDelta = new Vector2(rawX, rawY) * sensitivity * multiplier;
            _currentMouseDelta = Vector2.SmoothDamp(_currentMouseDelta, targetMouseDelta, ref _mouseDeltaVelocity, _lookSmoothing);

            _yawDegrees += _currentMouseDelta.x;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - _currentMouseDelta.y, _pitchMinDegrees, _pitchMaxDegrees);
            
            transform.rotation = Quaternion.Euler(0f, _yawDegrees, 0f);
            _eye.localRotation = Quaternion.Euler(_pitchDegrees, 0f, 0f);
        }

        private void ApplyMove()
        {
            float horizontal = EmberInput.Move.x;
            float vertical = EmberInput.Move.y;
            bool isSprinting = EmberInput.Sprint && vertical > 0.1f;

            Vector3 targetInput = (transform.forward * vertical + transform.right * horizontal).normalized;
            float targetSpeed = _baseSpeed * (isSprinting ? _sprintMultiplier : 1f);
            
            if (targetInput.magnitude < 0.1f) targetSpeed = 0f;

            _currentMoveVelocity = Vector3.SmoothDamp(_currentMoveVelocity, targetInput * targetSpeed, ref _moveVelocityDamp, targetSpeed > 0.1f ? _accelTime : _decelTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -1f;
                if (EmberInput.JumpKeyDown)
                {
                    _verticalVelocity = _jumpForce;
                }
            }
            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 motion = _currentMoveVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
            
            // FOV Kick logic
            if (_cam != null)
            {
                float targetFov = isSprinting ? _baseFov * 1.15f : _baseFov;
                _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, targetFov, ref _fovVelocity, 0.2f);
            }

            // Footsteps stub
            if (_controller.isGrounded && _currentMoveVelocity.magnitude > 0.5f)
            {
                _distanceTravelled += _currentMoveVelocity.magnitude * Time.deltaTime;
                if (_distanceTravelled >= _footstepDistance)
                {
                    _distanceTravelled = 0f;
                    EmitFootstep();
                }
            }
        }

        private void EmitFootstep()
        {
            // Raycast down for surface detection stub
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
            {
                // In P11 we will use hit.collider to determine surface type
                // Debug.Log($"Footstep on {hit.collider.name}");
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

            float multiplier = EmberInput.Sprint ? 1.2f : 1.0f;
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


using UnityEngine;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>Dependency-light third/first-person camera rig with smooth follow and spherecast collision intent.</summary>
    public sealed class CombatPlaygroundCameraRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Camera controlledCamera;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.V;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private bool lockCursorOnPlay = true;

        [Header("Third Person")]
        [SerializeField] private float thirdPersonDistance = 4.6f;
        [SerializeField] private float thirdPersonPivotHeight = 1.45f;
        [SerializeField] private float thirdPersonPitch = 18f;
        [SerializeField] private float positionSmoothTime = 0.065f;

        [Header("First Person")]
        [SerializeField] private Vector3 firstPersonLocalOffset = new Vector3(0f, 1.58f, 0.08f);

        [Header("Collision")]
        // PR#8 bot review fix: ~0 includes layer 2 (Ignore Raycast) which is the
        // reserved layer for objects the camera should NOT collide with (player
        // rig, trigger volumes, etc.). Mask out bit 2 so the camera doesn't get
        // yanked forward by its own capsule or invisible trigger geometry.
        [SerializeField] private LayerMask collisionMask = ~(1 << 2);
        [SerializeField] private float collisionRadius = 0.28f;
        [SerializeField] private float collisionBuffer = 0.12f;

        private Vector3 followVelocity;
        private float yaw;
        private float pitch;

        public CombatPlaygroundCameraMode Mode { get; private set; } = CombatPlaygroundCameraMode.ThirdPerson;
        public float PlanarYawDegrees => yaw;

        private void Awake()
        {
            if (controlledCamera == null)
                controlledCamera = GetComponentInChildren<Camera>();
            if (controlledCamera == null)
                controlledCamera = gameObject.AddComponent<Camera>();

            yaw = transform.eulerAngles.y;
            pitch = thirdPersonPitch;

            if (lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            ReadLookInput();
            if (Input.GetKeyDown(toggleKey))
                ToggleMode();

            if (Mode == CombatPlaygroundCameraMode.FirstPerson)
                ApplyFirstPerson();
            else
                ApplyThirdPerson();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null && Mathf.Approximately(yaw, 0f))
                yaw = target.eulerAngles.y;
        }

        public void ToggleMode()
        {
            Mode = Mode == CombatPlaygroundCameraMode.ThirdPerson ? CombatPlaygroundCameraMode.FirstPerson : CombatPlaygroundCameraMode.ThirdPerson;
            followVelocity = Vector3.zero;
        }

        private void ReadLookInput()
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -55f, 75f);
        }

        private void ApplyFirstPerson()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            controlledCamera.transform.SetPositionAndRotation(target.TransformPoint(firstPersonLocalOffset), rotation);
        }

        private void ApplyThirdPerson()
        {
            var pivot = target.position + Vector3.up * thirdPersonPivotHeight;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var desired = pivot + (rotation * new Vector3(0f, 0f, -thirdPersonDistance));
            var resolved = ResolveCollision(pivot, desired);

            controlledCamera.transform.position = Vector3.SmoothDamp(
                controlledCamera.transform.position,
                resolved,
                ref followVelocity,
                Mathf.Max(0.001f, positionSmoothTime));
            controlledCamera.transform.rotation = Quaternion.LookRotation((pivot - controlledCamera.transform.position).normalized, Vector3.up);
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            var toDesired = desired - pivot;
            var distance = toDesired.magnitude;
            if (distance <= 0.001f)
                return desired;

            if (Physics.SphereCast(pivot, collisionRadius, toDesired / distance, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                var safeDistance = Mathf.Max(0.25f, hit.distance - collisionBuffer);
                return pivot + (toDesired.normalized * safeDistance);
            }

            return desired;
        }
    }

    public enum CombatPlaygroundCameraMode
    {
        ThirdPerson = 0,
        FirstPerson = 1,
    }
}

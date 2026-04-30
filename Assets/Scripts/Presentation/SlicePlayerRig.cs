using EmberCrpg.Domain.Actors;
using UnityEngine;

// Design note:
// SlicePlayerRig is the thin Unity wrapper for WASD movement and mouse look in Sprint 1.
// Inputs: keyboard/mouse state plus grid snap requests from the controller.
// Outputs: first-person motion and a derived grid position for pure simulation state.
// Bible reference: PRD FR-08.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Simple first-person rig for the tiny vertical slice.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SlicePlayerRig : MonoBehaviour
    {
        private CharacterController _controller;
        private Camera _viewCamera;
        private float _pitch;
        private float _verticalSpeed;

        private const float MoveSpeed = 6f;
        private const float Gravity = -20f;
        private const float MouseSensitivity = 2f;

        private void Awake()
        {
            _controller = gameObject.GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.35f;
            _controller.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("SliceCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            _viewCamera = cameraObject.AddComponent<Camera>();
            _viewCamera.clearFlags = CameraClearFlags.Skybox;
            _viewCamera.tag = "MainCamera";

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            transform.Rotate(0f, Input.GetAxis("Mouse X") * MouseSensitivity, 0f);
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * MouseSensitivity, -80f, 80f);
            _viewCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

            var move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            move = move.normalized * MoveSpeed;

            if (_controller.isGrounded && _verticalSpeed < 0f)
                _verticalSpeed = -1f;
            _verticalSpeed += Gravity * Time.deltaTime;
            move.y = _verticalSpeed;
            _controller.Move(move * Time.deltaTime);
        }

        public void SnapToGrid(GridPosition grid)
        {
            transform.position = SliceWorldView.ToWorld(grid);
            _verticalSpeed = 0f;
        }

        public GridPosition ReadGridPosition()
        {
            var position = transform.position;
            return new GridPosition(
                Mathf.RoundToInt(position.x / SliceWorldView.CellSize),
                Mathf.RoundToInt(position.z / SliceWorldView.CellSize));
        }
    }
}

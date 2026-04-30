using EmberCrpg.Simulation.Movement;
using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>CharacterController-backed Sprint 4 player capsule with WASD, jump, and camera-relative motion.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class Sprint4PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedStickVelocity = -2f;
        [SerializeField] private float turnSmoothing = 14f;

        [Header("Runtime References")]
        [SerializeField] private Sprint4CameraRig cameraRig;
        [SerializeField] private Sprint4AnimatorDriver animatorDriver;

        private readonly Sprint4KinematicMotor motor = new Sprint4KinematicMotor();
        private CharacterController characterController;
        private Sprint4MotorState motorState;
        private Sprint4MotorStep lastStep;

        public Sprint4MotorState MotorState => motorState;
        public Sprint4MotorStep LastStep => lastStep;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.height = 1.85f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.925f, 0f);
            characterController.skinWidth = 0.04f;
            characterController.minMoveDistance = 0f;

            if (animatorDriver == null)
                animatorDriver = GetComponentInChildren<Sprint4AnimatorDriver>();

            motorState = new Sprint4MotorState(transform.position.ToSprint4(), 0f, characterController.isGrounded);
        }

        private void Start()
        {
            if (cameraRig == null)
                cameraRig = FindFirstObjectByType<Sprint4CameraRig>();
            if (cameraRig != null)
                cameraRig.SetTarget(transform);
        }

        private void Update()
        {
            var input = ReadInput();
            var settings = new Sprint4MotorSettings(moveSpeed, jumpHeight, gravity, groundedStickVelocity);
            motorState = new Sprint4MotorState(transform.position.ToSprint4(), motorState.VerticalVelocity, characterController.isGrounded);
            lastStep = motor.Plan(motorState, input, settings, Time.deltaTime);

            var collisionFlags = characterController.Move(lastStep.Displacement.ToUnity());
            var grounded = characterController.isGrounded || (collisionFlags & CollisionFlags.Below) != 0;
            motorState = motor.ResolveGrounding(lastStep.State, transform.position.ToSprint4(), grounded);
            lastStep = new Sprint4MotorStep(lastStep.Displacement, lastStep.PlanarVelocity, motorState, lastStep.JumpedThisFrame);

            RotateTowardMovement(lastStep.PlanarVelocity.ToUnity());

            if (animatorDriver != null)
                animatorDriver.Apply(lastStep, grounded, cameraRig != null && cameraRig.Mode == Sprint4CameraMode.FirstPerson);
        }

        private Sprint4MovementInput ReadInput()
        {
            var moveX = Input.GetAxisRaw("Horizontal");
            var moveZ = Input.GetAxisRaw("Vertical");

            // Pin direct WASD keys as a fallback if InputManager axes are edited later.
            if (Input.GetKey(KeyCode.A)) moveX -= 1f;
            if (Input.GetKey(KeyCode.D)) moveX += 1f;
            if (Input.GetKey(KeyCode.S)) moveZ -= 1f;
            if (Input.GetKey(KeyCode.W)) moveZ += 1f;

            return new Sprint4MovementInput(
                Mathf.Clamp(moveX, -1f, 1f),
                Mathf.Clamp(moveZ, -1f, 1f),
                cameraRig != null ? cameraRig.PlanarYawDegrees : transform.eulerAngles.y,
                Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space));
        }

        private void RotateTowardMovement(Vector3 planarVelocity)
        {
            planarVelocity.y = 0f;
            if (planarVelocity.sqrMagnitude < 0.0001f)
                return;

            var targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
        }
    }
}

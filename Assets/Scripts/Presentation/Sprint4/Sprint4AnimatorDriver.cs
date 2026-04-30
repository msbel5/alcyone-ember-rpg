using EmberCrpg.Simulation.Movement;
using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>Placeholder animator contract for Sprint 4 locomotion without depending on final art assets.</summary>
    public sealed class Sprint4AnimatorDriver : MonoBehaviour
    {
        public const string MoveSpeedParameter = "MoveSpeed";
        public const string GroundedParameter = "Grounded";
        public const string VerticalSpeedParameter = "VerticalSpeed";
        public const string FirstPersonParameter = "FirstPerson";
        public const string LocomotionStateParameter = "LocomotionState";
        public const string JumpTriggerParameter = "Jump";

        public static readonly int MoveSpeedHash = Animator.StringToHash(MoveSpeedParameter);
        public static readonly int GroundedHash = Animator.StringToHash(GroundedParameter);
        public static readonly int VerticalSpeedHash = Animator.StringToHash(VerticalSpeedParameter);
        public static readonly int FirstPersonHash = Animator.StringToHash(FirstPersonParameter);
        public static readonly int LocomotionStateHash = Animator.StringToHash(LocomotionStateParameter);
        public static readonly int JumpTriggerHash = Animator.StringToHash(JumpTriggerParameter);

        [SerializeField] private Animator animator;

        public Sprint4LocomotionState CurrentState { get; private set; } = Sprint4LocomotionState.Idle;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Apply(Sprint4MotorStep step, bool isGrounded, bool isFirstPerson)
        {
            CurrentState = ResolveState(step, isGrounded);
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            SetFloatIfPresent(MoveSpeedHash, step.PlanarVelocity.Magnitude);
            SetFloatIfPresent(VerticalSpeedHash, step.State.VerticalVelocity);
            SetBoolIfPresent(GroundedHash, isGrounded);
            SetBoolIfPresent(FirstPersonHash, isFirstPerson);
            SetIntegerIfPresent(LocomotionStateHash, (int)CurrentState);

            if (step.JumpedThisFrame && HasParameter(JumpTriggerHash, AnimatorControllerParameterType.Trigger))
                animator.SetTrigger(JumpTriggerHash);
        }

        private static Sprint4LocomotionState ResolveState(Sprint4MotorStep step, bool isGrounded)
        {
            if (!isGrounded && step.State.VerticalVelocity > 0.1f)
                return Sprint4LocomotionState.Jump;
            if (!isGrounded)
                return Sprint4LocomotionState.Fall;
            return step.PlanarVelocity.SqrMagnitude > 0.01f ? Sprint4LocomotionState.Move : Sprint4LocomotionState.Idle;
        }

        private void SetFloatIfPresent(int hash, float value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Float))
                animator.SetFloat(hash, value);
        }

        private void SetBoolIfPresent(int hash, bool value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Bool))
                animator.SetBool(hash, value);
        }

        private void SetIntegerIfPresent(int hash, int value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Int))
                animator.SetInteger(hash, value);
        }

        private bool HasParameter(int hash, AnimatorControllerParameterType type)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == hash && parameter.type == type)
                    return true;
            }

            return false;
        }
    }

    public enum Sprint4LocomotionState
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Fall = 3,
    }
}

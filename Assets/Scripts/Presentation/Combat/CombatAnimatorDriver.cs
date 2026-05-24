using EmberCrpg.Simulation.Movement;
using UnityEngine;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>Placeholder animator contract for combat locomotion without depending on final art assets.</summary>
    public sealed class CombatAnimatorDriver : MonoBehaviour
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

        public CombatLocomotionState CurrentState { get; private set; } = CombatLocomotionState.Idle;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Apply(CombatMotorStep step, bool isGrounded, bool isFirstPerson)
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

        private static CombatLocomotionState ResolveState(CombatMotorStep step, bool isGrounded)
        {
            if (!isGrounded && step.State.VerticalVelocity > 0.1f)
                return CombatLocomotionState.Jump;
            if (!isGrounded)
                return CombatLocomotionState.Fall;
            return step.PlanarVelocity.SqrMagnitude > 0.01f ? CombatLocomotionState.Move : CombatLocomotionState.Idle;
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

    public enum CombatLocomotionState
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Fall = 3,
    }
}

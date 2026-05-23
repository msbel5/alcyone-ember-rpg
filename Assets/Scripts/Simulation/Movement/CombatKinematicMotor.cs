using System;

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>
    /// Deterministic combat movement baseline. Unity adapters provide collision resolution;
    /// this class owns input normalization, yaw-relative planar velocity, jump impulse, and gravity.
    /// </summary>
    public sealed class CombatKinematicMotor
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        public CombatMotorStep Plan(CombatMotorState state, CombatMovementInput input, CombatMotorSettings settings, float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Movement delta time cannot be negative.");

            var planarVelocity = ToWorldPlanar(input.LocalPlanar, input.YawDegrees) * MathF.Max(0f, settings.MoveSpeed);
            var verticalVelocity = state.VerticalVelocity;
            var grounded = state.IsGrounded;
            var jumped = false;

            if (grounded && verticalVelocity < 0f)
                verticalVelocity = settings.GroundedStickVelocity;

            if (grounded && input.JumpPressed)
            {
                verticalVelocity = MathF.Sqrt(MathF.Max(0f, settings.JumpHeight * -2f * settings.Gravity));
                grounded = false;
                jumped = true;
            }

            verticalVelocity += settings.Gravity * deltaTime;

            var displacement = new CombatVector3(
                planarVelocity.X * deltaTime,
                verticalVelocity * deltaTime,
                planarVelocity.Z * deltaTime);

            var nextState = new CombatMotorState(state.Position + displacement, verticalVelocity, grounded);
            return new CombatMotorStep(displacement, planarVelocity, nextState, jumped);
        }

        public CombatMotorState ResolveGrounding(CombatMotorState state, CombatVector3 resolvedPosition, bool isGrounded)
        {
            var verticalVelocity = isGrounded && state.VerticalVelocity < 0f ? 0f : state.VerticalVelocity;
            return new CombatMotorState(resolvedPosition, verticalVelocity, isGrounded);
        }

        public static CombatVector3 ToWorldPlanar(CombatVector3 localPlanar, float yawDegrees)
        {
            var radians = yawDegrees * DegreesToRadians;
            var sin = MathF.Sin(radians);
            var cos = MathF.Cos(radians);

            var right = new CombatVector3(cos, 0f, -sin);
            var forward = new CombatVector3(sin, 0f, cos);
            return (right * localPlanar.X) + (forward * localPlanar.Z);
        }
    }
}

using System;

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>
    /// Deterministic Sprint 4 movement baseline. Unity adapters provide collision resolution;
    /// this class owns input normalization, yaw-relative planar velocity, jump impulse, and gravity.
    /// </summary>
    public sealed class Sprint4KinematicMotor
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        public Sprint4MotorStep Plan(Sprint4MotorState state, Sprint4MovementInput input, Sprint4MotorSettings settings, float deltaTime)
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

            var displacement = new Sprint4Vector3(
                planarVelocity.X * deltaTime,
                verticalVelocity * deltaTime,
                planarVelocity.Z * deltaTime);

            var nextState = new Sprint4MotorState(state.Position + displacement, verticalVelocity, grounded);
            return new Sprint4MotorStep(displacement, planarVelocity, nextState, jumped);
        }

        public Sprint4MotorState ResolveGrounding(Sprint4MotorState state, Sprint4Vector3 resolvedPosition, bool isGrounded)
        {
            var verticalVelocity = isGrounded && state.VerticalVelocity < 0f ? 0f : state.VerticalVelocity;
            return new Sprint4MotorState(resolvedPosition, verticalVelocity, isGrounded);
        }

        public static Sprint4Vector3 ToWorldPlanar(Sprint4Vector3 localPlanar, float yawDegrees)
        {
            var radians = yawDegrees * DegreesToRadians;
            var sin = MathF.Sin(radians);
            var cos = MathF.Cos(radians);

            var right = new Sprint4Vector3(cos, 0f, -sin);
            var forward = new Sprint4Vector3(sin, 0f, cos);
            return (right * localPlanar.X) + (forward * localPlanar.Z);
        }
    }
}

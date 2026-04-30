namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Minimal state carried between Sprint 4 kinematic movement ticks.</summary>
    public readonly struct Sprint4MotorState
    {
        public Sprint4MotorState(Sprint4Vector3 position, float verticalVelocity, bool isGrounded)
        {
            Position = position;
            VerticalVelocity = verticalVelocity;
            IsGrounded = isGrounded;
        }

        public static Sprint4MotorState GroundedAt(Sprint4Vector3 position)
            => new Sprint4MotorState(position, 0f, true);

        public Sprint4Vector3 Position { get; }
        public float VerticalVelocity { get; }
        public bool IsGrounded { get; }
    }
}

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Minimal state carried between combat kinematic movement ticks.</summary>
    public readonly struct CombatMotorState
    {
        public CombatMotorState(CombatVector3 position, float verticalVelocity, bool isGrounded)
        {
            Position = position;
            VerticalVelocity = verticalVelocity;
            IsGrounded = isGrounded;
        }

        public static CombatMotorState GroundedAt(CombatVector3 position)
            => new CombatMotorState(position, 0f, true);

        public CombatVector3 Position { get; }
        public float VerticalVelocity { get; }
        public bool IsGrounded { get; }
    }
}

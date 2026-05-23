namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Tuning values shared by the runtime CharacterController adapter and deterministic tests.</summary>
    public readonly struct CombatMotorSettings
    {
        public CombatMotorSettings(float moveSpeed, float jumpHeight, float gravity, float groundedStickVelocity)
        {
            MoveSpeed = moveSpeed;
            JumpHeight = jumpHeight;
            Gravity = gravity;
            GroundedStickVelocity = groundedStickVelocity;
        }

        public static CombatMotorSettings Default => new CombatMotorSettings(
            moveSpeed: 5f,
            jumpHeight: 1.35f,
            gravity: -24f,
            groundedStickVelocity: -2f);

        public float MoveSpeed { get; }
        public float JumpHeight { get; }
        public float Gravity { get; }
        public float GroundedStickVelocity { get; }
    }
}

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Output of one combat movement integration step.</summary>
    public readonly struct CombatMotorStep
    {
        public CombatMotorStep(CombatVector3 displacement, CombatVector3 planarVelocity, CombatMotorState state, bool jumpedThisFrame)
        {
            Displacement = displacement;
            PlanarVelocity = planarVelocity;
            State = state;
            JumpedThisFrame = jumpedThisFrame;
        }

        public CombatVector3 Displacement { get; }
        public CombatVector3 PlanarVelocity { get; }
        public CombatMotorState State { get; }
        public bool JumpedThisFrame { get; }
    }
}

namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Output of one Sprint 4 movement integration step.</summary>
    public readonly struct Sprint4MotorStep
    {
        public Sprint4MotorStep(Sprint4Vector3 displacement, Sprint4Vector3 planarVelocity, Sprint4MotorState state, bool jumpedThisFrame)
        {
            Displacement = displacement;
            PlanarVelocity = planarVelocity;
            State = state;
            JumpedThisFrame = jumpedThisFrame;
        }

        public Sprint4Vector3 Displacement { get; }
        public Sprint4Vector3 PlanarVelocity { get; }
        public Sprint4MotorState State { get; }
        public bool JumpedThisFrame { get; }
    }
}

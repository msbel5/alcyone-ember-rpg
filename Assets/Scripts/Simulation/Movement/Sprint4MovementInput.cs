namespace EmberCrpg.Simulation.Movement
{
    /// <summary>Engine-agnostic movement command for the Sprint 4 player baseline.</summary>
    public readonly struct Sprint4MovementInput
    {
        public Sprint4MovementInput(float moveX, float moveZ, float yawDegrees, bool jumpPressed)
        {
            MoveX = moveX;
            MoveZ = moveZ;
            YawDegrees = yawDegrees;
            JumpPressed = jumpPressed;
        }

        /// <summary>Left/right strafe intent, usually A/D or horizontal stick.</summary>
        public float MoveX { get; }

        /// <summary>Forward/back intent, usually W/S or vertical stick.</summary>
        public float MoveZ { get; }

        /// <summary>Camera/body yaw used to convert local input to world-space planar motion.</summary>
        public float YawDegrees { get; }

        /// <summary>True only on the frame the player requested a jump.</summary>
        public bool JumpPressed { get; }

        public Sprint4Vector3 LocalPlanar => Sprint4Vector3.ClampMagnitude(new Sprint4Vector3(MoveX, 0f, MoveZ), 1f);
    }
}

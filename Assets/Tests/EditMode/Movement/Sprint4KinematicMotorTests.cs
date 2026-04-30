using EmberCrpg.Simulation.Movement;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Movement
{
    public sealed class Sprint4KinematicMotorTests
    {
        [Test]
        public void Plan_ForwardInput_ProducesExpectedPositionDelta()
        {
            var motor = new Sprint4KinematicMotor();
            var state = Sprint4MotorState.GroundedAt(Sprint4Vector3.Zero);
            var input = new Sprint4MovementInput(moveX: 0f, moveZ: 1f, yawDegrees: 0f, jumpPressed: false);
            var settings = new Sprint4MotorSettings(moveSpeed: 5f, jumpHeight: 1.35f, gravity: 0f, groundedStickVelocity: 0f);

            var step = motor.Plan(state, input, settings, deltaTime: 0.5f);

            Assert.That(step.Displacement.X, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(step.Displacement.Y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(step.Displacement.Z, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(step.State.Position.Z, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void Plan_DiagonalInput_IsClampedToMoveSpeed()
        {
            var motor = new Sprint4KinematicMotor();
            var input = new Sprint4MovementInput(moveX: 1f, moveZ: 1f, yawDegrees: 0f, jumpPressed: false);
            var settings = new Sprint4MotorSettings(moveSpeed: 5f, jumpHeight: 1.35f, gravity: 0f, groundedStickVelocity: 0f);

            var step = motor.Plan(Sprint4MotorState.GroundedAt(Sprint4Vector3.Zero), input, settings, deltaTime: 1f);

            Assert.That(step.PlanarVelocity.Magnitude, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void Plan_JumpPressedWhileGrounded_AddsUpwardVelocity()
        {
            var motor = new Sprint4KinematicMotor();
            var input = new Sprint4MovementInput(moveX: 0f, moveZ: 0f, yawDegrees: 0f, jumpPressed: true);

            var step = motor.Plan(Sprint4MotorState.GroundedAt(Sprint4Vector3.Zero), input, Sprint4MotorSettings.Default, deltaTime: 0.02f);

            Assert.That(step.JumpedThisFrame, Is.True);
            Assert.That(step.State.VerticalVelocity, Is.GreaterThan(0f));
            Assert.That(step.Displacement.Y, Is.GreaterThan(0f));
        }

        [Test]
        public void ToWorldPlanar_YawNinety_MapsForwardToPositiveX()
        {
            var world = Sprint4KinematicMotor.ToWorldPlanar(new Sprint4Vector3(0f, 0f, 1f), 90f);

            Assert.That(world.X, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(world.Z, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}

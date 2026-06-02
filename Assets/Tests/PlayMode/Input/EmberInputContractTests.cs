using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Domain.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace EmberCrpg.Tests.PlayMode.Input
{
    public sealed class EmberInputContractTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Mouse _mouse;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            EmberInput.ResetForTests();
            EmberInput.EnableForTests();
        }

        [TearDown]
        public override void TearDown()
        {
            EmberInput.ResetForTests();
            EmberRuntimeOptionsProvider.ResetToDefaults();
            base.TearDown();
        }

        [Test]
        public void IdleFrame_ReturnsNeutralValues()
        {
            Assert.That(EmberInput.Move, Is.EqualTo(Vector2.zero));
            Assert.That(EmberInput.Look, Is.EqualTo(Vector2.zero));
            Assert.That(EmberInput.Sprint, Is.False);
            Assert.That(EmberInput.JumpDown, Is.False);
            Assert.That(EmberInput.Interact, Is.False);
            Assert.That(EmberInput.AttackClick, Is.False);
            Assert.That(EmberInput.NumberKeyDown(), Is.EqualTo(0));
            Assert.That(EmberInput.FunctionKeyDown(), Is.EqualTo(0));
            Assert.That(EmberInput.AxisRaw("Horizontal"), Is.EqualTo(0f));
        }

        [Test]
        public void MovementAndLook_ReadDeviceState()
        {
            Press(_keyboard.wKey);
            Assert.That(EmberInput.Move.y, Is.EqualTo(1f));
            Assert.That(EmberInput.AxisRaw("Vertical"), Is.EqualTo(1f));

            Press(_keyboard.dKey);
            Assert.That(EmberInput.Move.x, Is.EqualTo(1f));
            Assert.That(EmberInput.Axis("Horizontal"), Is.EqualTo(1f));

            InputSystem.QueueStateEvent(_mouse, new MouseState { delta = new Vector2(4f, -2f) });
            InputSystem.Update();
            Assert.That(EmberInput.Look, Is.EqualTo(new Vector2(4f, -2f)));
            Assert.That(EmberInput.LookSmoothed.x, Is.GreaterThan(0f));
            Assert.That(EmberInput.AxisRaw("Mouse X"), Is.EqualTo(4f));
        }

        [Test]
        public void SemanticButtons_MatchLegacyFacadeContract()
        {
            Press(_keyboard.leftShiftKey);
            Assert.That(EmberInput.Sprint, Is.True);
            Release(_keyboard.leftShiftKey);

            Press(_keyboard.spaceKey);
            Assert.That(EmberInput.JumpDown, Is.True);
            Assert.That(EmberInput.JumpKeyDown, Is.True);
            Release(_keyboard.spaceKey);

            AssertButton(_keyboard.eKey, () => EmberInput.Interact);
            AssertButton(_keyboard.f1Key, () => EmberInput.ToggleCursor);
            AssertButton(_keyboard.rKey, () => EmberInput.RegenWorld);
            AssertButton(_keyboard.tabKey, () => EmberInput.ToggleMap);
            AssertButton(_keyboard.f5Key, () => EmberInput.SaveQuick);
            AssertButton(_keyboard.f9Key, () => EmberInput.LoadQuick);

            Press(_keyboard.escapeKey);
            Assert.That(EmberInput.PauseDown, Is.True);
            Assert.That(EmberInput.PauseHeld, Is.True);
            Release(_keyboard.escapeKey);

            AssertButton(_keyboard.fKey, () => EmberInput.MeleeSwing);
        }

        [Test]
        public void MouseNumberFunctionAndPassthroughs_MatchContract()
        {
            Press(_mouse.leftButton);
            Assert.That(EmberInput.AttackClick, Is.True);
            Assert.That(EmberInput.MouseDown(0), Is.True);
            Release(_mouse.leftButton);

            Press(_mouse.rightButton);
            Assert.That(EmberInput.SecondaryClick, Is.True);
            Release(_mouse.rightButton);

            Press(_keyboard.digit3Key);
            Assert.That(EmberInput.NumberKeyDown(), Is.EqualTo(3));
            Assert.That(EmberInput.NumberKeyDown(3), Is.True);
            Assert.That(EmberInput.NumberKeyDown(10), Is.False);
            Release(_keyboard.digit3Key);

            Press(_keyboard.f7Key);
            Assert.That(EmberInput.FunctionKeyDown(), Is.EqualTo(7));
            Release(_keyboard.f7Key);

            Press(_keyboard.cKey);
            Assert.That(EmberInput.KeyDown(KeyCode.C), Is.True);
            Assert.That(EmberInput.Key(KeyCode.C), Is.True);
        }

        [Test]
        public void JumpBinding_CanBeRemappedViaRuntimeOptions()
        {
            var options = EmberRuntimeOptionsProvider.Current.Clone();
            options.Input.JumpPath = "<Keyboard>/j";
            EmberRuntimeOptionsProvider.Set(options);

            EmberInput.ResetForTests();
            EmberInput.EnableForTests();

            Press(_keyboard.spaceKey);
            Assert.That(EmberInput.JumpDown, Is.False);
            Assert.That(EmberInput.JumpKeyDown, Is.False);
            Release(_keyboard.spaceKey);

            Press(_keyboard.jKey);
            Assert.That(EmberInput.JumpDown, Is.True);
            Assert.That(EmberInput.JumpKeyDown, Is.True);
            Release(_keyboard.jKey);
        }

        private void AssertButton(ButtonControl control, System.Func<bool> read)
        {
            Press(control);
            Assert.That(read(), Is.True);
            Release(control);
        }
    }
}

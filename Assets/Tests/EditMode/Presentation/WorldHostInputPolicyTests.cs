#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember.Runtime;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class WorldHostInputPolicyTests
    {
        [Test]
        public void EscapeHold_ResetsWhileModalIsOpen()
        {
            var quitCalled = false;
            var timer = WorldHostInputPolicy.StepEscapeHoldTimer(
                currentTimer: 0.7f,
                modalOpen: true,
                pauseMenuPresent: false,
                pauseDown: false,
                pauseHeld: true,
                unscaledDeltaTime: 0.5f,
                holdQuitSeconds: 1f,
                onQuit: () => quitCalled = true);

            Assert.That(timer, Is.EqualTo(0f));
            Assert.That(quitCalled, Is.False);
        }

        [Test]
        public void EscapeHold_TriggersQuitAfterThreshold()
        {
            var quitCalled = false;
            var timer = WorldHostInputPolicy.StepEscapeHoldTimer(
                currentTimer: 0.8f,
                modalOpen: false,
                pauseMenuPresent: false,
                pauseDown: false,
                pauseHeld: true,
                unscaledDeltaTime: 0.4f,
                holdQuitSeconds: 1f,
                onQuit: () => quitCalled = true);

            Assert.That(timer, Is.EqualTo(0f));
            Assert.That(quitCalled, Is.True);
        }

        [Test]
        public void ResolveSelectedSpellSlot_PreservesSelectionWhenModalOpen()
        {
            var next = WorldHostInputPolicy.ResolveSelectedSpellSlot(
                modalOpen: true,
                currentSlot: 3,
                spellSlotCount: 5,
                numberKeyDown: _ => true);

            Assert.That(next, Is.EqualTo(3));
        }

        [Test]
        public void StepFateTimer_ExpiresAndInvokesCallback()
        {
            var expired = false;
            var next = WorldHostInputPolicy.StepFateTimer(0.2f, 0.25f, () => expired = true);
            Assert.That(next, Is.EqualTo(0f));
            Assert.That(expired, Is.True);
        }
    }
}
#endif

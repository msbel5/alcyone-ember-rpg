#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember.UI;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Ui
{
    public sealed class DialogCursorPolicyTests
    {
        [Test]
        public void ShouldLockAfterDialogClose_WhenNoPauseAndNoModal()
        {
            Assert.That(DialogCursorPolicy.ShouldLockAfterDialogClose(false, false), Is.True);
        }

        [Test]
        public void ShouldLockAfterDialogClose_WhenPauseMenuOpen()
        {
            Assert.That(DialogCursorPolicy.ShouldLockAfterDialogClose(false, true), Is.False);
        }

        [Test]
        public void ShouldLockAfterDialogClose_WhenDialogStillOpen()
        {
            Assert.That(DialogCursorPolicy.ShouldLockAfterDialogClose(true, false), Is.False);
        }
    }
}
#endif

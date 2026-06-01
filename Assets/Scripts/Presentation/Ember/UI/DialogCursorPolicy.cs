namespace EmberCrpg.Presentation.Ember.UI
{
    public static class DialogCursorPolicy
    {
        public static bool ShouldLockAfterDialogClose(bool isAnotherDialogOpen, bool isPauseMenuOpen)
        {
            return !isAnotherDialogOpen && !isPauseMenuOpen;
        }
    }
}

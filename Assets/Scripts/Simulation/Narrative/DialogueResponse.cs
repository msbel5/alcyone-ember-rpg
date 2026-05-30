namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>
    /// Immutable typed dialogue response (Spoken / Refused). ARCH-03: extracted from the deleted
    /// NpcDialogueService shell into its own file because it remains a useful shared value type
    /// (covered by the audit tests); the dead service that produced it was removed.
    /// </summary>
    public sealed class DialogueResponse
    {
        private DialogueResponse(string text, string refusalReason)
        {
            Text = text ?? string.Empty;
            RefusalReason = refusalReason ?? string.Empty;
        }

        public string Text { get; }
        public string RefusalReason { get; }
        public bool IsRefused => !string.IsNullOrEmpty(RefusalReason);

        public static DialogueResponse Spoken(string text) => new DialogueResponse(text, null);
        public static DialogueResponse Refused(string reason) => new DialogueResponse(null, reason);
    }
}

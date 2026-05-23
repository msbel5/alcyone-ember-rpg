using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Audit (eighth pass B-P2): moved out of DialogBoxPanel.cs so adapter
    /// types can implement it without taking a using on the UI namespace.
    /// The dialog source is a command-channel surface the simulation hands
    /// to the player: it knows the current NPC line, the available Ask-About
    /// topics, and which topic the player just picked.
    /// </summary>
    public interface IDialogSource
    {
        string GetCurrentLine();
        IReadOnlyList<string> GetTopics();
        void SelectTopic(string topicId);

        /// <summary>
        /// True while the dialog source is waiting on an async LLM (NPC line
        /// generation, DM ConsultFate response, etc.). Panels poll this to
        /// render a "thinking…" indicator instead of an empty / stale line.
        /// Default false for sources that never block.
        /// </summary>
        bool IsThinking => false;
    }

    /// <summary>
    /// Audit (eighth pass B-P2): portrait-aware dialog source. The lookup of
    /// the sprite by name is left to a separate ISpriteByName provider on
    /// the host so this interface stays free of UnityEngine types.
    /// </summary>
    public interface IDialogSourcePortrait : IDialogSource
    {
        string GetPortraitName();
    }
}

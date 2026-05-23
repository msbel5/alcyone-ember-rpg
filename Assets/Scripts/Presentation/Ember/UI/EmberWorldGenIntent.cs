namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Cross-scene handoff for the world-gen wizard's player input.
    /// EmberWorldHost.Awake consumes <see cref="Pending"/> once and clears it.
    /// Codex ninth-pass A-P1 fix: previously the SeedWorld call ran against
    /// the menu's transient adapter and was lost on LoadScene.
    ///
    /// Codex ninth-pass G-P2: extracted from EmberWorldGenUI.cs into its own
    /// file so the EditMode/fallback harness can compile and exercise the
    /// handoff contract without needing UnityEngine on the test path.
    /// </summary>
    public sealed class EmberWorldGenIntent
    {
        public static EmberWorldGenIntent Pending { get; set; }

        public string Mood { get; }
        public string Calling { get; }
        public string Start { get; }
        public string PlayerName { get; }
        public string CharacterClassId { get; }
        public string BirthsignId { get; }
        public string[] AnswerChoiceIds { get; }

        public EmberWorldGenIntent(string mood, string calling, string start)
            : this(mood, calling, start, string.Empty, string.Empty, string.Empty, null)
        {
        }

        public EmberWorldGenIntent(
            string mood,
            string calling,
            string start,
            string playerName,
            string characterClassId,
            string birthsignId,
            string[] answerChoiceIds)
        {
            Mood = mood ?? string.Empty;
            Calling = calling ?? string.Empty;
            Start = start ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            CharacterClassId = characterClassId ?? string.Empty;
            BirthsignId = birthsignId ?? string.Empty;
            AnswerChoiceIds = answerChoiceIds ?? new string[0];
        }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Mood)
            && string.IsNullOrEmpty(Calling)
            && string.IsNullOrEmpty(Start)
            && string.IsNullOrEmpty(PlayerName)
            && string.IsNullOrEmpty(CharacterClassId)
            && string.IsNullOrEmpty(BirthsignId)
            && AnswerChoiceIds.Length == 0;

        public EmberWorldGenIntent WithCharacter(string playerName, string characterClassId, string birthsignId, string[] answerChoiceIds)
        {
            return new EmberWorldGenIntent(Mood, Calling, Start, playerName, characterClassId, birthsignId, answerChoiceIds);
        }
    }
}

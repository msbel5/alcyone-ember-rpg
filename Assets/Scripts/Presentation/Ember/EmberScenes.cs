namespace EmberCrpg.Presentation.Ember
{
    /// <summary>
    /// EMB-056: the single registry of Unity scene names. Runtime/editor/diagnostic code must load
    /// scenes through these constants instead of bare string literals, so renaming or reordering a
    /// scene is a one-line change here (and a compile error at every stale call site) rather than a
    /// silent break in save/load, scene transitions, and the proof driver.
    ///
    /// Values MUST match the scene file names in Assets/Scenes/Ember/ and
    /// ProjectSettings/EditorBuildSettings.asset exactly.
    /// </summary>
    public static class EmberScenes
    {
        // Build scene chain (ProjectSettings/EditorBuildSettings.asset, in order)
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string CharacterCreation = "CharacterCreation";
        /// <summary>
        /// The near-empty scene the runtime World Scene Director fills from world data (the procedural
        /// "you stand in the generated starting settlement" entry).
        /// </summary>
        public const string GeneratedWorld = "GeneratedWorld";

        /// <summary>The default first gameplay scene a new game drops into.</summary>
        public const string FirstGameplayScene = GeneratedWorld;

        /// <summary>No baked gameplay scene tour remains; GeneratedWorld is runtime-populated.</summary>
        public static readonly string[] GameplayTour = { };
    }
}

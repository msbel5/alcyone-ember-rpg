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
        public const string SmithingOverworld = "SmithingOverworld";
        public const string ColonyNeeds = "ColonyNeeds";
        public const string SeasonFarm = "SeasonFarm";
        public const string TradeMarket = "TradeMarket";
        public const string CombatDungeon = "CombatDungeon";
        public const string RitualHall = "RitualHall";
        public const string TavernDialog = "TavernDialog";
        public const string OracleShrine = "OracleShrine";
        public const string ShowroomOverview = "ShowroomOverview";
        public const string TavernFlavour = "TavernFlavour";

        /// <summary>
        /// The near-empty scene the runtime World Scene Director fills from world data (the procedural
        /// "you stand in the generated starting settlement" entry). Not part of <see cref="GameplayTour"/>:
        /// the baked-scene proof is unchanged, and this scene's content is built at runtime, not authored.
        /// </summary>
        public const string GeneratedWorld = "GeneratedWorld";

        /// <summary>The default first gameplay scene a new game drops into.</summary>
        public const string FirstGameplayScene = SmithingOverworld;

        /// <summary>The 10 gameplay scenes the proof driver tours (build chain minus Boot/MainMenu/CharacterCreation).</summary>
        public static readonly string[] GameplayTour =
        {
            SmithingOverworld, TavernDialog, ColonyNeeds, CombatDungeon,
            OracleShrine, RitualHall, SeasonFarm, TradeMarket,
            ShowroomOverview, TavernFlavour,
        };
    }
}

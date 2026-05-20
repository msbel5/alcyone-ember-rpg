namespace EmberCrpg.Editor.Ember.AssetImport
{
    /// <summary>
    /// Coarse-grained asset kind derived from the import path. Each category maps to one
    /// import profile so the postprocessor stays declarative.
    /// </summary>
    public enum AssetCategory
    {
        Unknown,
        CharacterSprite,
        ItemIcon,
        SpellIcon,
        Portrait,
        Tile,
        UiBanner,
        UiCombatHud,
        UiStatusBar,
        UiStatusIcon,
        UiCommon,
        BodySilhouette,
        UiPlan,
    }
}

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Bakes the host scene for the runtime-generated world. Unlike every other recipe this authors NO
    /// content: the scene is intentionally near-empty — RunRecipe adds the EmberWorldHost (with the sprite
    /// registry wired) and that is all. At play time the runtime WorldSceneDirector builds the ground,
    /// building shells, lighting, and player rig from world data, so the location is generated, not authored.
    /// Baking this simply produces a blank, host-only GeneratedWorld.unity with the same provenance as the
    /// other scenes (so New Game can LoadScene it).
    /// </summary>
    public sealed class GeneratedWorldSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "GeneratedWorld";

        public void Build()
        {
            // Intentionally empty. RunRecipe() adds the EmberWorldHost; the runtime World Scene Director
            // fills the scene from worldgen data when New Game loads it.
        }
    }
}

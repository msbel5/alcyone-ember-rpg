namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    /// <summary>
    /// Single contract every scene recipe implements. A recipe is responsible for one
    /// phase and produces one .unity file under <see cref="Common.EmberAssetPaths.EmberScenesDir"/>.
    /// </summary>
    public interface IEmberSceneRecipe
    {
        /// <summary>Display name shown in the editor menu and used to derive the scene file name.</summary>
        string SceneName { get; }

        /// <summary>Builds the scene contents into the current empty editor scene.</summary>
        void Build();
    }
}

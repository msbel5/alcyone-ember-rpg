using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Creates a fresh empty editor scene with deterministic defaults: no skybox, no
    /// directional light, no main camera. Builders add what they need explicitly so a
    /// recipe is fully responsible for the contents of the scene it produces.
    /// </summary>
    public static class EmberSceneFactory
    {
        public static Scene CreateEmpty()
        {
            return EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
        }
    }
}

using System.IO;
using EmberCrpg.Editor.Ember.Menu;
using EmberCrpg.Presentation.Ember;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    public static class Pr214RescueProofAutomation
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const string OutputDir = "Reports/screens/rescue-pr214";

        public static void RunNow()
        {
            Directory.CreateDirectory(OutputDir);

            EmberSceneBuilderMenu.BuildCharacterCreation();
            Capture(EmberScenes.CharacterCreation, "character_creation.png");

            EmberSceneBuilderMenu.BuildGeneratedWorld();
            Capture(EmberScenes.GeneratedWorld, "generated_world_game.png");
            Capture(EmberScenes.GeneratedWorld, "generated_world_spawn_proof.png");
        }

        private static void Capture(string sceneName, string fileName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Ember/" + sceneName + ".unity");
            var camera = Camera.main;
            if (camera == null) return;

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var rt = new RenderTexture(Width, Height, 24);
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();

            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(Path.Combine(OutputDir, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(rt);
        }
    }
}

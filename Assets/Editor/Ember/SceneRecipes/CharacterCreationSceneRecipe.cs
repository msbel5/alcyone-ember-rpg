using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Editor.Ember.SceneBuilders;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneRecipes
{
    public sealed class CharacterCreationSceneRecipe : IEmberSceneRecipe
    {
        public string SceneName => "CharacterCreation";

        public void Build()
        {
            var canvas = new GameObject("CharacterCreationCanvas");
            canvas.AddComponent<CharacterCreationUI>();

            var cameraGo = new GameObject("CharacterCreationCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 1.6f, -6f);
            cameraGo.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.018f, 0.016f, 1f);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CharacterCreationFloor";
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.1f, 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = EmberSceneMaterialLibrary.Floor();

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "CharacterCreationBackdrop";
            backdrop.transform.position = new Vector3(0f, 2f, 3f);
            backdrop.transform.localScale = new Vector3(12f, 4f, 0.1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = EmberSceneMaterialLibrary.Wall();
        }
    }
}

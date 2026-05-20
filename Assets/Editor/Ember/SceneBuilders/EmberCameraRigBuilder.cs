using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builds the editor-time camera that ships inside a Morrowind/Daggerfall-style
    /// first-person rig. The rig is the camera's *root*; the camera transform is local.
    /// </summary>
    public static class EmberCameraRigBuilder
    {
        public static Camera AddFirstPersonCamera(
            GameObject parent,
            float fieldOfView,
            float nearClip,
            float farClip,
            Vector3 localEyePosition)
        {
            var go = new GameObject("EyeCamera");
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.localPosition = localEyePosition;
            go.tag = "MainCamera";

            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);

            go.AddComponent<AudioListener>();
            return camera;
        }
    }
}

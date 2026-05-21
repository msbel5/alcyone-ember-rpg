using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// AAA Polished rig builder. Includes Cinemachine, URP Volume, and polished scripts.
    /// </summary>
    public static class EmberPlayerRigBuilder
    {
        public const float DefaultCapsuleHeight = 1.85f;
        public const float DefaultCapsuleRadius = 0.35f;

        private const string ProfileGuid = "a1b2c3d4"; // We'll need the actual GUID or path

        public static GameObject BuildRig(Vector3 spawnPosition, Quaternion spawnRotation, float fov = 70f)
        {
            var rig = new GameObject("PlayerRig");
            rig.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var controller = rig.AddComponent<CharacterController>();
            controller.height = DefaultCapsuleHeight;
            controller.radius = DefaultCapsuleRadius;
            controller.center = new Vector3(0f, DefaultCapsuleHeight / 2f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(rig.transform, worldPositionStays: false);
            body.transform.localPosition = new Vector3(0f, DefaultCapsuleHeight / 2f, 0f);
            body.transform.localScale = new Vector3(DefaultCapsuleRadius * 2f, DefaultCapsuleHeight / 2f, DefaultCapsuleRadius * 2f);
            var bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) Object.DestroyImmediate(bodyCollider);

            Camera eyeCamera = EmberCameraRigBuilder.AddFirstPersonCamera(
                parent: rig,
                fieldOfView: fov,
                nearClip: 0.05f,
                farClip: 500f,
                localEyePosition: new Vector3(0f, DefaultCapsuleHeight * 0.95f, 0f));

            // AAA Polish: We'll skip CinemachineBrain for now as it overrides manual EyeCamera rotation
            // and the user wants manual look control back.
            // eyeCamera.gameObject.AddComponent<CinemachineBrain>();

            // Add Virtual Camera (Disabled, just for reference/future use)
            var vcamGo = new GameObject("PlayerVCam", typeof(CinemachineCamera));
            vcamGo.transform.SetParent(rig.transform, worldPositionStays: false);
            vcamGo.transform.localPosition = new Vector3(0f, DefaultCapsuleHeight * 0.95f, 0f);
            var vcam = vcamGo.GetComponent<CinemachineCamera>();
            vcam.Priority = 10;
            vcam.Lens.FieldOfView = fov;
            vcam.Lens.NearClipPlane = 0.05f;
            vcam.enabled = false;

            // Add Global Volume to scene if not present
            var volumeGo = GameObject.Find("GlobalVolume");
            if (volumeGo == null)
            {
                volumeGo = new GameObject("GlobalVolume", typeof(Volume));
                var volume = volumeGo.GetComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1;
                
                string profilePath = "Assets/Settings/EmberGlobalVolumeProfile.asset";
                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            }

            AddScriptByName(rig, "EmberFirstPersonController");
            AddScriptByName(rig, "EmberPlayerInteractRaycaster");
            AddScriptByName(rig, "EmberPlayerInventoryToggle");
            AddScriptByName(rig, "EmberPlayerSpellCaster");
            AddScriptByName(rig, "EmberPlayerMeleeSwing");
            
            return rig;
        }

        private static void AddScriptByName(GameObject host, string scriptName)
        {
            var type = System.Type.GetType($"EmberCrpg.Presentation.Ember.Camera.{scriptName}, EmberCrpg.Presentation");
            if (type == null)
                type = System.Type.GetType($"EmberCrpg.Presentation.Ember.Interaction.{scriptName}, EmberCrpg.Presentation");
            if (type == null)
                type = System.Type.GetType($"EmberCrpg.Presentation.Ember.Combat.{scriptName}, EmberCrpg.Presentation");
            if (type == null)
                type = System.Type.GetType($"EmberCrpg.Presentation.Ember.UI.{scriptName}, EmberCrpg.Presentation");
            
            if (type != null) host.AddComponent(type);
        }
    }
}


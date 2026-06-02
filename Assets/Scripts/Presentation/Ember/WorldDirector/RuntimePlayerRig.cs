using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Runtime first-person player rig: a capsule CharacterController + an eye camera + the five gameplay
    /// controller scripts. A faithful twin of the editor EmberPlayerRigBuilder.BuildRig (and the camera bits
    /// of EmberCameraRigBuilder), minus the editor-only VolumeProfile/Cinemachine wiring not needed at play
    /// time. Named "PlayerRig" so the existing EmberGeneratedActorSpawner finds it as its spawn anchor.
    /// </summary>
    public static class RuntimePlayerRig
    {
        private const float CapsuleHeight = 1.85f;
        private const float CapsuleRadius = 0.35f;

        public static GameObject Build(Vector3 spawnPosition, Quaternion spawnRotation, float fov = 70f)
        {
            // Idempotent: if a rig already exists (e.g. a re-realize), reuse it.
            var existing = GameObject.Find("PlayerRig");
            if (existing != null) return existing;

            var rig = new GameObject("PlayerRig");
            rig.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var marker = new GameObject("PlayerSpawn"); // the convention EmberSaveService uses to find the player
            marker.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var controller = rig.AddComponent<CharacterController>();
            controller.height = CapsuleHeight;
            controller.radius = CapsuleRadius;
            controller.center = new Vector3(0f, CapsuleHeight / 2f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;

            // First-person eye camera (twin of EmberCameraRigBuilder.AddFirstPersonCamera).
            var camGo = new GameObject("EyeCamera");
            camGo.transform.SetParent(rig.transform, worldPositionStays: false);
            camGo.transform.localPosition = new Vector3(0f, CapsuleHeight * 0.95f, 0f);
            camGo.tag = "MainCamera";
            // Fully-qualified: the EmberCrpg.Presentation.Ember.Camera namespace shadows the UnityEngine type.
            var camera = camGo.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            camGo.AddComponent<AudioListener>();

            // The 5 gameplay controllers live in the Presentation assembly; add by name like the editor rig.
            AddControllerByName(rig, "EmberFirstPersonController");
            AddControllerByName(rig, "EmberPlayerInteractRaycaster");
            AddControllerByName(rig, "EmberPlayerInventoryToggle");
            AddControllerByName(rig, "EmberPlayerSpellCaster");
            AddControllerByName(rig, "EmberPlayerMeleeSwing");
            return rig;
        }

        private static void AddControllerByName(GameObject host, string scriptName)
        {
            // Mirror the editor rig's namespace search so a controller moving namespaces does not silently break.
            string[] namespaces = { "Camera", "Interaction", "Combat", "UI" };
            for (int i = 0; i < namespaces.Length; i++)
            {
                var type = System.Type.GetType($"EmberCrpg.Presentation.Ember.{namespaces[i]}.{scriptName}, EmberCrpg.Presentation");
                if (type != null) { host.AddComponent(type); return; }
            }
            Debug.LogWarning("[WorldDirector] player controller type not found: " + scriptName);
        }
    }
}

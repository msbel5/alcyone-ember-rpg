using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>Creates a buildable Sprint 4 playground from a minimal scene asset.</summary>
    public static class Sprint4FoundationBootstrap
    {
        private const string GroundName = "Sprint4 Greybox Ground";
        private const string SceneNameToken = "Sprint4";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateFoundationIfNeeded()
        {
            // Codex audit (sixth pass E-P2 #E2): intent-marker is now the
            // sole opt-in. The previous scene-name fallback meant any scene
            // whose name happened to contain "Sprint4" got auto-bootstrapped,
            // which collided with Ember scenes during testing. New Sprint 4
            // playground scenes MUST author a Sprint4FoundationMarker at root;
            // the SceneNameToken constant is kept for ARCHITECTURE.md history
            // only.
            var existingController = Object.FindFirstObjectByType<Sprint4PlayerController>(FindObjectsInactive.Include);
            if (existingController != null) return;

            var marker = Object.FindFirstObjectByType<Sprint4FoundationMarker>(FindObjectsInactive.Include);
            if (marker == null) return;

            EnsureLight();
            EnsureGround();
            var player = CreatePlayer();
            CreateCameraRig(player.transform);
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
                return;

            var lightObject = new GameObject("Sprint4 Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void EnsureGround()
        {
            // Codex audit (second pass E-P2): previously this always called
            // CreatePrimitive, duplicating authored ground if the scene already
            // had a `Sprint4 Greybox Ground` GameObject. Probe by name first
            // so the bootstrap is idempotent and doesn't stack greyboxes when
            // a real authored Sprint 4 scene is later opened.
            if (GameObject.Find(GroundName) != null) return;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = GroundName;
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(24f, 0.2f, 24f);
        }

        private static GameObject CreatePlayer()
        {
            var player = new GameObject("Sprint4 Player Capsule");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.layer = 2; // Ignore Raycast, so the camera collision probe does not hit its own capsule.
            player.AddComponent<CharacterController>();
            player.AddComponent<Animator>();
            player.AddComponent<Sprint4AnimatorDriver>();
            player.AddComponent<Sprint4PlayerController>();
            player.AddComponent<Sprint4CombatInputAdapter>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Placeholder Body";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.925f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.925f, 0.7f);
            visual.layer = 2;
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            return player;
        }

        private static void CreateCameraRig(Transform target)
        {
            var rigObject = new GameObject("Sprint4 Camera Rig");
            rigObject.transform.position = target.position + new Vector3(0f, 2f, -5f);
            var camera = rigObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            var rig = rigObject.AddComponent<Sprint4CameraRig>();
            rig.SetTarget(target);
        }
    }
}

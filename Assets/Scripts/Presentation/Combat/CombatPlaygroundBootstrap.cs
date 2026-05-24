using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>Creates a buildable combat playground from a minimal scene asset.</summary>
    public static class CombatPlaygroundBootstrap
    {
        private const string GroundName = "Combat Playground Ground";
        private const string SceneNameToken = "CombatPlayground";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateFoundationIfNeeded()
        {
            var existingController = Object.FindFirstObjectByType<CombatPlaygroundController>(FindObjectsInactive.Include);
            if (existingController != null) return;

            var marker = Object.FindFirstObjectByType<CombatPlaygroundMarker>(FindObjectsInactive.Include);
            if (marker == null)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.name.Contains(SceneNameToken)) return;
            }

            EnsureLight();
            EnsureGround();
            var player = CreatePlayer();
            CreateCameraRig(player.transform);
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
                return;

            var lightObject = new GameObject("Combat Playground Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void EnsureGround()
        {
            // Codex audit (seventh pass E-P3 #17): the previous idempotency
            // guard probed `GameObject.Find(GroundName)` — fragile because
            // any unrelated GameObject sharing the name would suppress
            // spawn, and a renamed ground would silently get duplicated.
            // Switch to a component-marker probe: tag the spawned ground
            // with the CombatPlaygroundMarker (same component used as the
            // scene-level opt-in).
            // Component-based idempotency: every ground we spawn carries a
            // CombatGreyboxGroundMarker. Re-running the bootstrap finds the
            // marker and bails. Name kept for human-readable debugging only.
            foreach (var existing in Object.FindObjectsByType<CombatGreyboxGroundMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing != null) return;
            }
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = GroundName;
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(24f, 0.2f, 24f);
            ground.AddComponent<CombatGreyboxGroundMarker>();
        }

        private static GameObject CreatePlayer()
        {
            var player = new GameObject("Combat Playground Player Capsule");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.layer = 2; // Ignore Raycast, so the camera collision probe does not hit its own capsule.
            player.AddComponent<CharacterController>();
            player.AddComponent<Animator>();
            player.AddComponent<CombatAnimatorDriver>();
            player.AddComponent<CombatPlaygroundController>();
            player.AddComponent<CombatInputAdapter>();

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
            var rigObject = new GameObject("Combat Playground Camera Rig");
            rigObject.transform.position = target.position + new Vector3(0f, 2f, -5f);
            var camera = rigObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            var rig = rigObject.AddComponent<CombatPlaygroundCameraRig>();
            rig.SetTarget(target);
        }
    }
}

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
            // Codex audit (seventh pass — inline review on sixth-pass PR #198):
            // The sixth-pass dropped the SceneNameToken fallback entirely, but
            // the legacy Assets/Scenes/Sprint4Foundation.unity scene has no
            // Sprint4FoundationMarker authored, so the bootstrap stopped
            // producing the playground (no ground / player / camera rig) and
            // left the scene non-playable. Restore the scene-name fallback
            // for that one legacy scene — the marker is still the canonical
            // opt-in for NEW scenes and remains the first probe. Removal of
            // the fallback is gated on porting Sprint4Foundation.unity to
            // carry a Sprint4FoundationMarker (tracked under the Faz 13
            // cleanup ledger).
            var existingController = Object.FindFirstObjectByType<Sprint4PlayerController>(FindObjectsInactive.Include);
            if (existingController != null) return;

            var marker = Object.FindFirstObjectByType<Sprint4FoundationMarker>(FindObjectsInactive.Include);
            if (marker == null)
            {
                // Legacy scene fallback: Assets/Scenes/Sprint4Foundation.unity
                // ships without the marker; gate on the scene-name token so
                // it still bootstraps. Removal of this fallback is blocked on
                // porting that scene to carry a Sprint4FoundationMarker.
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

            var lightObject = new GameObject("Sprint4 Directional Light");
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
            // with the Sprint4FoundationMarker (same component used as the
            // scene-level opt-in). A future Sprint4-specific ground marker
            // can replace it without changing this guard.
            // Component-based idempotency: every ground we spawn carries a
            // Sprint4GreyboxGroundMarker. Re-running the bootstrap finds the
            // marker and bails. Name kept for human-readable debugging only.
            foreach (var existing in Object.FindObjectsByType<Sprint4GreyboxGroundMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing != null) return;
            }
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = GroundName;
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(24f, 0.2f, 24f);
            ground.AddComponent<Sprint4GreyboxGroundMarker>();
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

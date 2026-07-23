using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Diagnostics
{
    /// <summary>
    /// R2 pale-field probe — the LIVE-EDITOR session the protocol demands (blind build probes
    /// are barred). Drops a flag file at D:/proofs/r2/RUN, and on the next domain reload this
    /// runner enters play mode, boots a world the way the timelapse driver does, then:
    ///   1. dumps RenderSettings (fog/ambient/skybox),
    ///   2. raycasts the pale band (several screen heights) naming collider/renderer/shader,
    ///   3. BISECTS: toggles each root renderer group off, screenshotting every step —
    ///      the frame where the pale band vanishes names the culprit surface.
    /// Report: D:/proofs/r2/report.txt + numbered PNGs. Exits play mode and eats the flag.
    /// </summary>
    public static class R2ProbeRunner
    {
        private const string FlagPath = "D:/proofs/r2/RUN";
        private const string OutDir = "D:/proofs/r2";

        [InitializeOnLoadMethod]
        private static void Arm()
        {
            if (!File.Exists(FlagPath)) return;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    Debug.Log("[R2Probe] flag found - entering play mode.");
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            var host = new GameObject("R2ProbeHost");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<R2ProbeBehaviour>();
        }
    }

    public sealed class R2ProbeBehaviour : MonoBehaviour
    {
        private const string FlagPath = "D:/proofs/r2/RUN";
        private const string OutDir = "D:/proofs/r2";

        private void Start() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            var report = new StringBuilder();
            Directory.CreateDirectory(OutDir);
            yield return new WaitForSecondsRealtime(6f); // menu boot settles

            // Boot a world exactly like the timelapse driver.
            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending =
                new EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent("grim", "wanderer", "crossroads");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                EmberCrpg.Presentation.Ember.EmberScenes.GeneratedWorld);
            float deadline = Time.unscaledTime + 150f;
            while (!(EmberCrpg.Presentation.Ember.Adapters.EmberDomainAdapterLocator.Current
                     is EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter)
                   && Time.unscaledTime < deadline)
                yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForSecondsRealtime(8f);
            EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController.ActiveOpeningDismiss?.Invoke();
            yield return new WaitForSecondsRealtime(2f);

            // 1. Render settings dump.
            report.AppendLine($"fog={RenderSettings.fog} mode={RenderSettings.fogMode} color={RenderSettings.fogColor} density={RenderSettings.fogDensity} start={RenderSettings.fogStartDistance} end={RenderSettings.fogEndDistance}");
            report.AppendLine($"ambientMode={RenderSettings.ambientMode} ambientColor={RenderSettings.ambientLight} intensity={RenderSettings.ambientIntensity}");
            report.AppendLine($"skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name + "/" + RenderSettings.skybox.shader.name : "none")}");

            var cam = Camera.main;
            if (cam == null) { Finish(report, "NO CAMERA"); yield break; }
            report.AppendLine($"cam far={cam.farClipPlane} clearFlags={cam.clearFlags} bg={cam.backgroundColor}");

            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, "00_base.png"));
            yield return new WaitForSecondsRealtime(1f);

            // 2. Pale-band raycasts.
            for (int i = 0; i < 5; i++)
            {
                float fy = 0.50f + i * 0.04f;
                var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * fy, 0f));
                if (Physics.Raycast(ray, out var hit, 6000f))
                {
                    var rend = hit.collider.GetComponentInParent<Renderer>();
                    var mat = rend != null ? rend.sharedMaterial : null;
                    report.AppendLine($"ray y={fy:0.00}: HIT '{hit.collider.name}' d={hit.distance:0.0} root='{hit.collider.transform.root.name}' renderer='{(rend != null ? rend.name : "none")}' mat='{(mat != null ? mat.name : "none")}' shader='{(mat != null ? mat.shader.name : "none")}'");
                }
                else report.AppendLine($"ray y={fy:0.00}: NO HIT (sky/no-collider surface)");
            }

            // 3. Root-group bisect.
            var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Renderer>>();
            foreach (var rend in all)
            {
                string root = rend.transform.root.name;
                if (!groups.TryGetValue(root, out var list)) groups[root] = list = new System.Collections.Generic.List<Renderer>();
                list.Add(rend);
            }
            int index = 0;
            foreach (var kv in groups)
            {
                index++;
                report.AppendLine($"group {index:00} '{kv.Key}': {kv.Value.Count} renderers");
                if (kv.Value.Count > 400) { report.AppendLine($"  (skipped toggle - too many)"); continue; }
                foreach (var rend in kv.Value) if (rend != null) rend.enabled = false;
                yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, $"{index:00}_off_{Sanitize(kv.Key)}.png"));
                yield return new WaitForSecondsRealtime(0.6f);
                foreach (var rend in kv.Value) if (rend != null) rend.enabled = true;
                yield return null;
            }

            Finish(report, "done");
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Length > 40 ? name.Substring(0, 40) : name;
        }

        private static void Finish(StringBuilder report, string status)
        {
            report.AppendLine("status: " + status);
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
            if (File.Exists(FlagPath)) File.Delete(FlagPath);
            Debug.Log("[R2Probe] " + status + " - report written.");
            UnityEditor.EditorApplication.ExitPlaymode();
        }
    }
}

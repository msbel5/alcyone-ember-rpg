using System;
using System.IO;
using EmberCrpg.Editor.Ember.Menu;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    [InitializeOnLoad]
    public static class PlayabilityRescueAutomation
    {
        private const string RequestPath = "Temp/ember_playability_rebuild.request";
        private const string DonePath = "Reports/aaa_playability_rebuild_done.txt";

        static PlayabilityRescueAutomation()
        {
            EditorApplication.delayCall += RunIfRequested;
        }

        [MenuItem("Ember/Build Scene/AAA Playability Rescue Rebuild")]
        public static void RequestAndRun()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestPath, DateTime.UtcNow.ToString("O"));
            RunIfRequested();
        }

        public static void RunNow()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestPath, DateTime.UtcNow.ToString("O"));
            RunIfRequested();
        }

        private static void RunIfRequested()
        {
            if (!File.Exists(RequestPath)) return;
            File.Delete(RequestPath);
            try
            {
                EmberSceneBuilderMenu.BuildAll();
                EmberScreenshotCapture.CaptureAll();
                Directory.CreateDirectory("Reports");
                File.WriteAllText(DonePath, "ok " + DateTime.UtcNow.ToString("O"));
                Debug.Log("[PlayabilityRescue] Rebuilt gameplay scenes and refreshed screenshots.");
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory("Reports");
                File.WriteAllText(DonePath, "failed " + ex);
                Debug.LogException(ex);
            }
        }
    }
}

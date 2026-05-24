using System.IO;
using EmberCrpg.Simulation.Generation;
using UnityEditor;

namespace EmberCrpg.Editor.Ember.Forge
{
    public static class ScanMissingAssetsMenu
    {
        [MenuItem("Ember/Forge/Scan Missing Assets")]
        public static async void ScanMissingAssets()
        {
            var report = await AssetManifestScanner.ScanAsync(CoreAssetManifest.CreateDefault().Entries, Directory.GetCurrentDirectory(), System.Threading.CancellationToken.None);
            UnityEngine.Debug.Log("[ScanMissingAssets] total=" + report.Total + " cached=" + report.Cached + " missing=" + report.Missing);
            foreach (var row in report.Entries)
                UnityEngine.Debug.Log(row.EntryId + " | " + row.Category + " | " + row.State + " | " + row.Path + " | " + row.Reason);
        }
    }
}

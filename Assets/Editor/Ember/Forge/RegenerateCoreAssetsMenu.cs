using EmberCrpg.Simulation.Generation;
using UnityEditor;

namespace EmberCrpg.Editor.Ember.Forge
{
    public static class RegenerateCoreAssetsMenu
    {
        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Default Visible Subset")]
        public static void RegenerateDefaultSubset() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.DefaultSubset);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/All")]
        public static void RegenerateAll() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.All);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/NPC Billboards")]
        public static void RegenerateNpcBillboards() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.NpcBillboards);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Environment Surfaces")]
        public static void RegenerateEnvironmentSurfaces() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.EnvironmentSurfaces);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Items")]
        public static void RegenerateItems() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.Items);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Portraits")]
        public static void RegeneratePortraits() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.Portraits);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Icons")]
        public static void RegenerateIcons() => CoreAssetRegenerationRunner.Start(CoreAssetRegenerationScope.Icons);

        [MenuItem("Ember/Forge/Regenerate Core Assets (clear + rebuild)/Cancel Active Regeneration")]
        public static void Cancel() => CoreAssetRegenerationRunner.Cancel();

        public static void RegenerateDefaultSubsetBatch() => CoreAssetRegenerationRunner.RunBlocking(CoreAssetRegenerationScope.DefaultSubset);
    }
}

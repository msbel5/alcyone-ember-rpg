#if UNITY_EDITOR
using System.Linq;
using EmberCrpg.Presentation.Ember.Sprites;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.GeneratedAssetLibrary
{
    public sealed class GeneratedCoreAssetStoreTests
    {
        [Test]
        public void NormalizeKey_IsStableForCommonAssetIds()
        {
            Assert.That(GeneratedCoreAssetStore.NormalizeKey(" NPC-Guard "), Is.EqualTo("npc_guard"));
            Assert.That(GeneratedCoreAssetStore.NormalizeKey("Spell Fire"), Is.EqualTo("spell_fire"));
            Assert.That(GeneratedCoreAssetStore.NormalizeKey(null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void CoreCandidatePaths_PrioritizesRuntimeThenProjectThenStreamingAssets()
        {
            var paths = GeneratedCoreAssetStore.CoreCandidatePaths("npc_guard").ToArray();

            Assert.That(paths, Has.Length.EqualTo(3));
            Assert.That(paths[0], Does.Contain(Application.persistentDataPath));
            Assert.That(paths[1].Replace('\\', '/'), Does.Contain("/Assets/Generated/Core/npc_guard.png"));
            Assert.That(paths[2].Replace('\\', '/'), Does.Contain("/StreamingAssets/Generated/Core/npc_guard.png"));
        }
    }
}
#endif

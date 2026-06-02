#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember;
using EmberCrpg.Presentation.Ember.Runtime;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class EmberSceneCatalogTests
    {
        [Test]
        public void IsKnownBuildScene_UsesSharedSceneCatalog()
        {
            Assert.That(EmberSceneCatalog.IsKnownBuildScene(EmberScenes.SmithingOverworld), Is.True);
            Assert.That(EmberSceneCatalog.IsKnownBuildScene("__missing_scene__"), Is.False);
        }
    }
}
#endif

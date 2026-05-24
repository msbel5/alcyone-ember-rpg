using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Presentation.Ember.Boot;
using EmberCrpg.Tests.PlayMode.Support;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;

namespace EmberCrpg.Tests.PlayMode.Boot
{
    public sealed class BootSceneTest
    {
        [Test]
        public async Task BootFlow_ContinuesAfterGenerationFailureAndRequestsMainMenu()
        {
            UiSurfaceLocator.Clear();
            var surface = new TestUiSurface();
            UiSurfaceLocator.Register(surface);
            var result = await BootBootstrap.RunForTestsAsync(new TestAssetForge(failRequestId: "settings"));
            Assert.That(result.Started, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Succeeded, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Failed, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.RequestedScene, Is.EqualTo("MainMenu"));
            Assert.That(surface.LastPanel.LogText, Does.Contain("[error]"));
            UiSurfaceLocator.Clear();
        }
    }
}

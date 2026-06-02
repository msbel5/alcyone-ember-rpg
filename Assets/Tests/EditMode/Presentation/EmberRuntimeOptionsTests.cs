#if UNITY_INCLUDE_TESTS
using EmberCrpg.Domain.Configuration;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class EmberRuntimeOptionsTests
    {
        [TearDown]
        public void TearDown()
        {
            EmberRuntimeOptionsProvider.ResetToDefaults();
        }

        [Test]
        public void Defaults_PreserveCurrentDeterministicValues()
        {
            var options = EmberRuntimeOptionsProvider.Current;
            Assert.That(options.Boot.ForgeWaitFrames, Is.EqualTo(180));
            Assert.That(options.Boot.CoreTopUpMaxEntries, Is.EqualTo(3));
            Assert.That(options.Boot.PostGenerationDelayMs, Is.EqualTo(2500));

            Assert.That(options.WorldHost.FatePlaceholderSeconds, Is.EqualTo(3f));
            Assert.That(options.WorldHost.FateResolvedSeconds, Is.EqualTo(7f));
            Assert.That(options.WorldHost.EscapeHoldQuitSeconds, Is.EqualTo(1f));

            Assert.That(options.Tick.MinutesPerTick, Is.EqualTo(1));
            Assert.That(options.Tick.TicksPerDay, Is.EqualTo(240));
            Assert.That(options.Tick.TicksPerHour, Is.EqualTo(10));
        }

        [Test]
        public void Provider_CanSwapAndReset()
        {
            var baseline = EmberRuntimeOptionsProvider.Current;
            var menu = new MenuRuntimeOptions
            {
                FirstSceneDefault = baseline.Menu.FirstSceneDefault,
                DecorationRefreshSeconds = 3.5f,
                ForgeWaitFrames = baseline.Menu.ForgeWaitFrames,
                PreSceneDelayMs = baseline.Menu.PreSceneDelayMs,
                ScenarioReadyDelayMs = baseline.Menu.ScenarioReadyDelayMs,
                ScenarioManifestIds = baseline.Menu.ScenarioManifestIds,
            };
            var swapped = baseline.WithMenu(menu);
            EmberRuntimeOptionsProvider.Set(swapped);
            Assert.That(EmberRuntimeOptionsProvider.Current.Menu.DecorationRefreshSeconds, Is.EqualTo(3.5f));

            EmberRuntimeOptionsProvider.ResetToDefaults();
            Assert.That(EmberRuntimeOptionsProvider.Current.Menu.DecorationRefreshSeconds, Is.EqualTo(2f));
        }
    }
}
#endif

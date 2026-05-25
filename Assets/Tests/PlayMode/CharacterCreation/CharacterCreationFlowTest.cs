using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Tests.PlayMode.Support;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;
using System.Linq;

namespace EmberCrpg.Tests.PlayMode.CharacterCreation
{
    public sealed class CharacterCreationFlowTest
    {
        [SetUp]
        public void SetUp()
        {
            UiSurfaceLocator.Clear();
            UiSurfaceLocator.Register(new TestUiSurface());
        }

        [TearDown]
        public void TearDown() => UiSurfaceLocator.Clear();

        [Test]
        public void ControllerRunsImmersiveSixStepFlow()
        {
            var controller = CharacterCreationController.CreateForTests(42u, "{ invalid json }");

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.CommanderIdentity));
            Assert.That(controller.CanAdvance, Is.False);

            controller.SetAdvancedSettingsVisible(true);
            controller.SetAdapterOverride("scifi_frontier");
            controller.SetCommanderIdentity("Aria", "123", "tactician");
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.PersonalityQuestions));
            for (int i = 0; i < 10; i++)
                controller.SelectAnswerByIndex(i % 3);

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.WorldHistoryReveal));
            controller.SkipHistoryReveal();
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.StatRolling));
            Assert.That(controller.RollAllAttributes().Count, Is.EqualTo(6));
            Assert.That(controller.LogLines.Count(l => l.StartsWith("[roll]")), Is.GreaterThanOrEqualTo(6));
            controller.KeepThisRoll();
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.BuildSelection));
            controller.SelectClass("mage");
            controller.SelectAlignment("neutral_good");
            controller.ToggleSkill("insight");
            controller.ToggleSkill("deception");
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.DossierLaunch));
            Assert.That(controller.PortraitJson, Does.Contain("archetype_id"));
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();

            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.Complete));
            controller.RerollPortrait();
            controller.RerollPortrait();
            controller.RerollPortrait();
            Assert.That(controller.CanRerollPortrait, Is.False);
        }
    }
}

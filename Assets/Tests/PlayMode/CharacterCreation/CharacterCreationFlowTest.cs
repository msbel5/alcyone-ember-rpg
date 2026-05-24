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
        public void ControllerRequiresThreeSkillsAndLocksPortraitAfterThreeRerolls()
        {
            var controller = CharacterCreationController.CreateForTests(42u, "{ invalid json }");
            controller.SelectSkill("stealth");
            controller.SelectSkill("smithing");
            Assert.That(controller.CanAdvance, Is.False);
            controller.SelectSkill("lore");
            Assert.That(controller.CanAdvance, Is.True);
            controller.Continue();
            Assert.That(controller.RollAllAttributes().Count, Is.EqualTo(6));
            Assert.That(controller.LogLines.Count(l => l.StartsWith("[roll]")), Is.EqualTo(6));
            controller.ChooseBackground("smuggler");
            controller.Continue();
            Assert.That(controller.PortraitJson, Does.Contain("archetype_id"));
            controller.RerollPortrait();
            controller.RerollPortrait();
            controller.RerollPortrait();
            Assert.That(controller.CanRerollPortrait, Is.False);
        }
    }
}

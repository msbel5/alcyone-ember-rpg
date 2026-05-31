using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class PortraitPromptBuilderTests
    {
        [Test]
        public void Build_ComposesDeterministicSubject_FromNpcPromptJson()
        {
            var json = new NpcPromptJson(
                "humanoid_male",
                28,
                215,
                new[] { "wary", "soot-stained" },
                new[] { "scar", "iron earring" },
                "leather jerkin",
                "talisman",
                "ember-warm");

            var subject = PortraitPromptBuilder.Build(json);

            Assert.That(
                subject,
                Is.EqualTo("humanoid male with scar, iron earring, wearing leather jerkin, talisman, wary expression, ember warm palette (hues 28/215)"));
        }
    }
}

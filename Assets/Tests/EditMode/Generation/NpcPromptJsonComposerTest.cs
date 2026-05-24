using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class NpcPromptJsonComposerTest
    {
        [Test]
        public void ComposeIsDeterministicAndUsesJsonFields()
        {
            var json = NpcPromptJsonDefaults.FromSeed(7u, GenericNpcBaseManifest.CreateDefault());
            var a = LlmPromptComposer.Compose(json);
            var b = LlmPromptComposer.Compose(json);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Does.Contain(json.ArchetypeId));
            Assert.That(a, Does.Contain(json.WorldStyleAnchor));
        }
    }
}

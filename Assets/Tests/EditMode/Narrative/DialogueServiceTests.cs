using System.Collections.Generic;
using EmberCrpg.Domain.Narrative;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>
    /// DialogueTemplate rendering + registry. The NpcDialogueService shell these tests used to also
    /// cover was removed in ARCH-03 (zero production callers; superseded by the EMB-020
    /// ConversationState / NpcTopicCatalog dialogue path).
    /// </summary>
    public sealed class DialogueServiceTests
    {
        [Test]
        public void Template_RendersSubstitutions()
        {
            var t = new DialogueTemplate("default_weather", "Topic {topic} from {asker}");
            var rendered = t.Render(new Dictionary<string, string> { { "topic", "weather" }, { "asker", "Yorick" } });
            Assert.That(rendered, Is.EqualTo("Topic weather from Yorick"));
        }

        [Test]
        public void TemplateRegistry_RegisterAndGet()
        {
            var reg = new DialogueTemplateRegistry();
            reg.Register(new DialogueTemplate("a", "hello"));
            Assert.That(reg.Get("a").Template, Is.EqualTo("hello"));
            Assert.That(reg.Get("missing"), Is.Null);
        }
    }
}

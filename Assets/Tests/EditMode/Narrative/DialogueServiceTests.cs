using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Memory;
using EmberCrpg.Simulation.Narrative;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Narrative
{
    public sealed class DialogueServiceTests
    {
        private static readonly ActorId AskerId = new ActorId(10UL);
        private static readonly ActorId AskeeId = new ActorId(20UL);
        private static readonly TopicId WeatherTopic = new TopicId("weather");

        private static ActorRecord Make(ulong id, ActorMood mood) =>
            new ActorRecord(new ActorId(id), "n_" + id, ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0), accuracy: 10, dodge: 5, armor: 1, baseDamage: 3,
                mood: mood);

        private static TopicDef WeatherDef() =>
            new TopicDef(WeatherTopic, "the weather", null, "default_weather");

        // ----- DialogueTemplate -----
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

        // ----- NpcDialogueService -----
        [Test]
        public void Ask_Hostile_RefusesWithHostility()
        {
            var asker = Make(10UL, new ActorMood(70));
            var askee = Make(20UL, new ActorMood(70));
            var memory = new MemoryComponent(askee.Id);
            var templates = new DialogueTemplateRegistry();
            templates.Register(new DialogueTemplate("default_weather", "ok"));
            var factions = new FactionStore();
            factions.Add(new FactionRecord(new FactionId(1UL), "A", new string[0]));
            factions.Add(new FactionRecord(new FactionId(2UL), "B", new string[0]));
            factions.WithReputation(new FactionId(1UL), new FactionId(2UL), new FactionReputation(-90));

            var response = new NpcDialogueService(new MemoryRecallService(), templates)
                .Ask(asker, askee, memory, WeatherDef(), new FactionId(1UL), new FactionId(2UL), factions, default, default);

            Assert.That(response.Refused, Is.True);
            Assert.That(response.RefusalReason, Is.EqualTo("hostility"));
        }

        [Test]
        public void Ask_LowMood_Refuses()
        {
            var asker = Make(10UL, new ActorMood(70));
            var askee = Make(20UL, new ActorMood(10));
            var templates = new DialogueTemplateRegistry();
            templates.Register(new DialogueTemplate("default_weather", "ok"));

            var response = new NpcDialogueService(new MemoryRecallService(), templates)
                .Ask(asker, askee, null, WeatherDef(), default, default, null, default, default);

            Assert.That(response.Refused, Is.True);
            Assert.That(response.RefusalReason, Is.EqualTo("mood_too_low"));
        }

        [Test]
        public void Ask_NoMemory_UsesDefaultTemplate()
        {
            var asker = Make(10UL, new ActorMood(70));
            var askee = Make(20UL, new ActorMood(70));
            var templates = new DialogueTemplateRegistry();
            templates.Register(new DialogueTemplate("default_weather", "Greetings about {topic}"));

            var response = new NpcDialogueService(new MemoryRecallService(), templates)
                .Ask(asker, askee, null, WeatherDef(), default, default, null, default, default);

            Assert.That(response.Refused, Is.False);
            Assert.That(response.Text, Is.EqualTo("Greetings about weather"));
        }
    }
}

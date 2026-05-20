using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Simulation.Memory;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Memory
{
    public sealed class MemoryRecallServiceTests
    {
        private static readonly ActorId Owner = new ActorId(10UL);
        private static readonly ActorId Subject = new ActorId(20UL);
        private static readonly TopicId Crime = new TopicId("crime");
        private static readonly TopicId Trade = new TopicId("trade");

        [Test]
        public void Recall_FiltersByTopicAndRecency()
        {
            var c = new MemoryComponent(Owner);
            c.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(10), "old"));
            c.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(200), "new"));
            c.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(150), "other"));

            var recall = new MemoryRecallService().Recall(c, Crime, new GameTime(100));

            Assert.That(recall.Count, Is.EqualTo(1));
            Assert.That(recall[0].Detail, Is.EqualTo("new"));
        }

        [Test]
        public void Recall_EmptyTopic_ReturnsEmpty()
        {
            var c = new MemoryComponent(Owner);
            c.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(100), "x"));
            var recall = new MemoryRecallService().Recall(c, default, default);
            Assert.That(recall, Is.Empty);
        }

        [Test]
        public void HasRecentFact_TrueWhenAtOrAfterCutoff()
        {
            var c = new MemoryComponent(Owner);
            c.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(200), "n"));

            Assert.That(new MemoryRecallService().HasRecentFact(c, Crime, new GameTime(100)), Is.True);
            Assert.That(new MemoryRecallService().HasRecentFact(c, Crime, new GameTime(300)), Is.False);
            Assert.That(new MemoryRecallService().HasRecentFact(c, Trade, new GameTime(0)), Is.False);
        }
    }
}

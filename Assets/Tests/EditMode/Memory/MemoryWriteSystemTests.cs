using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Simulation.Memory;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Memory
{
    public sealed class MemoryWriteSystemTests
    {
        private static readonly ActorId Witness = new ActorId(10UL);
        private static readonly ActorId Subject = new ActorId(20UL);

        [Test]
        public void Record_AddsFactToComponent()
        {
            var memory = new MemoryComponent(Witness);
            new MemoryWriteSystem().Record(memory, new TopicId("event"), Subject, new GameTime(100), "saw it");

            Assert.That(memory.Count, Is.EqualTo(1));
            Assert.That(memory.Facts[0].Topic.Code, Is.EqualTo("event"));
            Assert.That(memory.Facts[0].AboutActor, Is.EqualTo(Subject));
            Assert.That(memory.Facts[0].Detail, Is.EqualTo("saw it"));
        }

        [Test]
        public void RecordCrime_UsesCrimeTopic()
        {
            var memory = new MemoryComponent(Witness);
            new MemoryWriteSystem().RecordCrime(memory, Subject, new GameTime(50), "stole bread");
            Assert.That(memory.Facts[0].Topic.Code, Is.EqualTo("crime"));
        }

        [Test]
        public void RecordTrade_UsesTradeTopic()
        {
            var memory = new MemoryComponent(Witness);
            new MemoryWriteSystem().RecordTrade(memory, Subject, new GameTime(60), "iron x3");
            Assert.That(memory.Facts[0].Topic.Code, Is.EqualTo("trade"));
        }

        [Test]
        public void Record_RejectsEmptyTopic()
        {
            var memory = new MemoryComponent(Witness);
            Assert.Throws<System.ArgumentException>(() =>
                new MemoryWriteSystem().Record(memory, default, Subject, default, "x"));
        }
    }
}

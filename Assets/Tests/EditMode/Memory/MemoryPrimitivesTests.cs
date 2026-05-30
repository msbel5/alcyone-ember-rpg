using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Memory
{
    /// <summary>Pins Phase 9 Atoms 4-5 memory primitives: MemoryFact + MemoryComponent.</summary>
    public sealed class MemoryPrimitivesTests
    {
        private static readonly ActorId Owner = new ActorId(10UL);
        private static readonly ActorId Subject = new ActorId(20UL);
        private static readonly TopicId Trade = new TopicId("trade");
        private static readonly TopicId Crime = new TopicId("crime");

        // ----- MemoryFact -----

        [Test]
        public void MemoryFact_RejectsEmptyRemembererOrTopic()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new MemoryFact(default, Trade, Subject, default, "x"));
            Assert.Throws<System.ArgumentException>(() =>
                new MemoryFact(Owner, default, Subject, default, "x"));
        }

        [Test]
        public void MemoryFact_StructuralEquality()
        {
            var a = new MemoryFact(Owner, Trade, Subject, new GameTime(100), "details");
            var b = new MemoryFact(Owner, Trade, Subject, new GameTime(100), "details");
            var c = new MemoryFact(Owner, Trade, Subject, new GameTime(100), "other");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // ----- MemoryComponent -----

        [Test]
        public void MemoryComponent_RejectsEmptyOwner()
        {
            Assert.Throws<System.ArgumentException>(() => new MemoryComponent(default));
        }

        [Test]
        public void Add_OwnerMismatch_Throws()
        {
            var component = new MemoryComponent(Owner);
            Assert.Throws<System.ArgumentException>(() =>
                component.Add(new MemoryFact(Subject, Trade, Owner, default, "")));
        }

        [Test]
        public void Query_ReturnsFactsByTopic_InInsertionOrder()
        {
            var component = new MemoryComponent(Owner);
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(10), "a"));
            component.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(20), "b"));
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(30), "c"));

            var trades = component.Query(Trade).ToList();

            Assert.That(trades.Count, Is.EqualTo(2));
            Assert.That(trades[0].Detail, Is.EqualTo("a"));
            Assert.That(trades[1].Detail, Is.EqualTo("c"));
        }

        [Test]
        public void MostRecent_ReturnsLatestByTotalMinutes()
        {
            var component = new MemoryComponent(Owner);
            component.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(50), "early"));
            component.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(200), "late"));
            component.Add(new MemoryFact(Owner, Crime, Subject, new GameTime(100), "middle"));

            var latest = component.MostRecent(Crime);

            Assert.That(latest.HasValue, Is.True);
            Assert.That(latest.Value.Detail, Is.EqualTo("late"));
        }

        [Test]
        public void MostRecent_NoMatchingTopic_ReturnsNull()
        {
            var component = new MemoryComponent(Owner);
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(10), "a"));

            Assert.That(component.MostRecent(Crime), Is.Null);
            Assert.That(component.MostRecent(default), Is.Null);
        }

        [Test]
        public void Forget_DropsFactsOlderThanCutoff()
        {
            var component = new MemoryComponent(Owner);
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(10), "old"));
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(50), "boundary"));
            component.Add(new MemoryFact(Owner, Trade, Subject, new GameTime(100), "new"));

            var removed = component.Forget(new GameTime(50));

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(component.Count, Is.EqualTo(2));
            Assert.That(component.Facts[0].Detail, Is.EqualTo("boundary"));
            Assert.That(component.Facts[1].Detail, Is.EqualTo("new"));
        }
    }
}

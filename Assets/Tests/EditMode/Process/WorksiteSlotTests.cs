using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>
    /// Pins WorksiteSlot factory output, equality, and validation guards.
    /// Closes CO-06 row in DOCS/sprint-faz-4-atom-map.md Debt ledger.
    /// </summary>
    public sealed class WorksiteSlotTests
    {
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Worksite = new GridPosition(5, 5);
        private static readonly GridPosition Queue = new GridPosition(5, 6);

        private static WorksiteRecord Furnace()
        {
            return new WorksiteRecord(Site, Worksite, WorksiteKind.Furnace, isActive: true);
        }

        [Test]
        public void FromWorksite_CopiesSiteAndPosition_AppliesTagAndQueue()
        {
            var slot = WorksiteSlot.FromWorksite(Furnace(), "furnace", Queue);

            Assert.That(slot.SiteId, Is.EqualTo(Site));
            Assert.That(slot.Position, Is.EqualTo(Worksite));
            Assert.That(slot.WorksiteTag, Is.EqualTo("furnace"));
            Assert.That(slot.QueuePosition, Is.EqualTo(Queue));
        }

        [Test]
        public void FromWorksite_NullRecord_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                WorksiteSlot.FromWorksite(null, "furnace", Queue));
        }

        [Test]
        public void FromWorksite_BlankTag_Throws()
        {
            Assert.Throws<System.ArgumentException>(() =>
                WorksiteSlot.FromWorksite(Furnace(), "", Queue));
            Assert.Throws<System.ArgumentException>(() =>
                WorksiteSlot.FromWorksite(Furnace(), "   ", Queue));
        }

        [Test]
        public void Equality_IsStructural()
        {
            var a = new WorksiteSlot(Site, Worksite, "furnace", Queue);
            var b = new WorksiteSlot(Site, Worksite, "furnace", Queue);
            var c = new WorksiteSlot(Site, Worksite, "bakery", Queue);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a != c, Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Constructor_NullTag_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new WorksiteSlot(Site, Worksite, null, Queue));
        }
    }
}

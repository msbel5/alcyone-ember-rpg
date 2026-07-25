using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// B10 §A2: the sim-blocker-cell store. Pins the O(1) contract, the revision bump on mutation
    /// (Stage B's future path-cache invalidation key), and the "large-coord safe" pack shape.
    /// </summary>
    public sealed class BlockedCellSetTests
    {
        [Test]
        public void Empty_ContainsNothing_AndRevisionIsZero()
        {
            var set = new BlockedCellSet();
            Assert.That(set.Contains(new GridPosition(0, 0)), Is.False);
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.Revision, Is.EqualTo(0L));
        }

        [Test]
        public void Add_MarksTheCell_AndBumpsRevision()
        {
            var set = new BlockedCellSet();
            set.Add(new GridPosition(3, 7));
            Assert.That(set.Contains(new GridPosition(3, 7)), Is.True);
            Assert.That(set.Contains(new GridPosition(3, 8)), Is.False);
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.Revision, Is.EqualTo(1L));
        }

        [Test]
        public void AddSameCellTwice_DoesNotBumpRevisionAgain()
        {
            // Revision is the Stage-B cache invalidation key — bumping it for no-op writes would
            // pointlessly evict live paths. Idempotency is a compile-in guarantee, not a hope.
            var set = new BlockedCellSet();
            set.Add(new GridPosition(3, 7));
            set.Add(new GridPosition(3, 7));
            Assert.That(set.Revision, Is.EqualTo(1L));
            Assert.That(set.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear_EmptiesTheSet_AndBumpsRevision()
        {
            var set = new BlockedCellSet();
            set.Add(new GridPosition(1, 1));
            var before = set.Revision;
            set.Clear();
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.Revision, Is.EqualTo(before + 1));
        }

        [Test]
        public void ClearOnEmpty_IsANoOp_AndDoesNotBumpRevision()
        {
            var set = new BlockedCellSet();
            set.Clear();
            Assert.That(set.Revision, Is.EqualTo(0L));
        }

        [Test]
        public void LargeCoordinates_PackWithoutCollision()
        {
            // Overland-scale cells: settlement.TileX * 40000 + 20000 lives around 20k..5M easily.
            // The long-stride pack MUST distinguish these — a 1e3 int stride would collide.
            var set = new BlockedCellSet();
            set.Add(new GridPosition(20000, 20000));
            set.Add(new GridPosition(20001, 19999)); // near-neighbour, distinct pack
            Assert.That(set.Contains(new GridPosition(20000, 20000)), Is.True);
            Assert.That(set.Contains(new GridPosition(20001, 19999)), Is.True);
            Assert.That(set.Contains(new GridPosition(20001, 20000)), Is.False);
            Assert.That(set.Count, Is.EqualTo(2));
        }
    }
}

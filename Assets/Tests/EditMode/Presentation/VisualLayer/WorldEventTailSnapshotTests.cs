using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins WorldEventLog tail HUD snapshot rows.</summary>
    public sealed class WorldEventTailSnapshotTests
    {
        [Test]
        public void NullLog_ProducesEmptySnapshot()
        {
            var snapshot = WorldEventTailSnapshot.FromLog(null, 10);
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void NonPositiveMaxRows_ProducesEmpty()
        {
            var log = new WorldEventLog();
            log.Append(new WorldEvent(default, WorldEventKind.ActorSpawned, default, new SiteId(1UL), "spawn"));

            Assert.That(WorldEventTailSnapshot.FromLog(log, 0).Rows, Is.Empty);
            Assert.That(WorldEventTailSnapshot.FromLog(log, -5).Rows, Is.Empty);
        }

        [Test]
        public void SmallerThanMax_ReturnsAllEvents_InOrder()
        {
            var log = new WorldEventLog();
            log.Append(new WorldEvent(default, WorldEventKind.ActorSpawned, default, new SiteId(1UL), "spawn"));
            log.Append(new WorldEvent(default, WorldEventKind.SiteEntered, default, new SiteId(1UL), "entered"));

            var snapshot = WorldEventTailSnapshot.FromLog(log, 10);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(2));
            Assert.That(snapshot.Rows[0].KindCode, Is.EqualTo("ActorSpawned"));
            Assert.That(snapshot.Rows[1].KindCode, Is.EqualTo("SiteEntered"));
        }

        [Test]
        public void LargerThanMax_TrimsToLatestTail()
        {
            var log = new WorldEventLog();
            log.Append(new WorldEvent(default, WorldEventKind.ActorSpawned, default, new SiteId(1UL), "first"));
            log.Append(new WorldEvent(default, WorldEventKind.SiteEntered, default, new SiteId(1UL), "second"));
            log.Append(new WorldEvent(default, WorldEventKind.RecipeCompleted, default, new SiteId(1UL), "third"));

            var snapshot = WorldEventTailSnapshot.FromLog(log, 2);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(2));
            Assert.That(snapshot.Rows[0].Reason, Is.EqualTo("second"));
            Assert.That(snapshot.Rows[1].Reason, Is.EqualTo("third"));
        }
    }
}

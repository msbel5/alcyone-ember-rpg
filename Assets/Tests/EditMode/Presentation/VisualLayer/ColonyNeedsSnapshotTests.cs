using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins read-only colony needs HUD snapshot rows.</summary>
    public sealed class ColonyNeedsSnapshotTests
    {
        private static ActorRecord MakeActor(ulong id, string name, ActorNeeds needs, ActorMood mood)
        {
            return new ActorRecord(
                new ActorId(id), name, ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 3,
                needs: needs,
                mood: mood);
        }

        [Test]
        public void NullStore_ProducesEmptySnapshot()
        {
            var snapshot = ColonyNeedsSnapshot.FromActors(null);
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void EmptyStore_ProducesEmptySnapshot()
        {
            var snapshot = ColonyNeedsSnapshot.FromActors(new ActorStore());
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void OneActor_SurfacesNeedAndMoodValues()
        {
            var store = new ActorStore();
            var needs = new ActorNeeds(new NeedValue(40), new NeedValue(20), new NeedValue(10));
            var mood = new ActorMood(35);
            store.Add(MakeActor(10UL, "Ada", needs, mood));

            var snapshot = ColonyNeedsSnapshot.FromActors(store);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(1));
            var row = snapshot.Rows[0];
            Assert.That(row.ActorName, Is.EqualTo("Ada"));
            Assert.That(row.Hunger, Is.EqualTo(40));
            Assert.That(row.Fatigue, Is.EqualTo(20));
            Assert.That(row.Thirst, Is.EqualTo(10));
            Assert.That(row.Mood, Is.EqualTo(35));
        }
    }
}

using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.VisualLayer;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>Pins faction relation HUD snapshot rows.</summary>
    public sealed class FactionRelationSnapshotTests
    {
        [Test]
        public void NullStore_ProducesEmptySnapshot()
        {
            var snapshot = FactionRelationSnapshot.FromStore(null);
            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void OneFaction_ProducesNoPairs()
        {
            var store = new FactionStore();
            store.Add(new FactionRecord(new FactionId(1UL), "Solo", new string[0]));

            var snapshot = FactionRelationSnapshot.FromStore(store);

            Assert.That(snapshot.Rows, Is.Empty);
        }

        [Test]
        public void TwoFactions_DefaultReputation_IsNeutral()
        {
            var store = new FactionStore();
            store.Add(new FactionRecord(new FactionId(1UL), "House A", new string[0]));
            store.Add(new FactionRecord(new FactionId(2UL), "House B", new string[0]));

            var snapshot = FactionRelationSnapshot.FromStore(store);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(1));
            var row = snapshot.Rows[0];
            Assert.That(row.Reputation, Is.EqualTo(0));
            Assert.That(row.RelationCode, Is.EqualTo("neutral"));
            Assert.That(row.FactionAName, Is.EqualTo("House A"));
            Assert.That(row.FactionBName, Is.EqualTo("House B"));
        }

        [Test]
        public void SetReputation_SurfacesValueAndRelationCode()
        {
            var store = new FactionStore();
            store.Add(new FactionRecord(new FactionId(1UL), "Allies", new string[0]));
            store.Add(new FactionRecord(new FactionId(2UL), "Friends", new string[0]));
            store.WithReputation(new FactionId(1UL), new FactionId(2UL), new FactionReputation(80));

            var snapshot = FactionRelationSnapshot.FromStore(store);

            var row = snapshot.Rows[0];
            Assert.That(row.Reputation, Is.EqualTo(80));
            Assert.That(row.RelationCode, Is.EqualTo("allied"));
        }
    }
}
